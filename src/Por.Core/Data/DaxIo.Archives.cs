using System;
using System.IO;

namespace Por.Core
{
    public static partial class DaxIo
    {
        public interface IDaxPlatform
        {
            void WriteChar(byte col, byte row, byte attr, byte count, byte ch);
            void BiosWriteChar(byte page, byte col, byte row, byte attr, byte count, byte ch);
            byte ReadGameKey();
            void ExitGame();
            void OverlayMessage(string text);
            string CurrentDirectory { get; }
            string FullPath(string path);
        }

        public static IDaxPlatform Platform { get; set; }

        private static void RequirePlatform()
        {
            if (Platform == null)
            {
                throw new InvalidOperationException("DaxIo.Platform is not set.");
            }
        }

        private static readonly byte[] familyState = new byte[C_Data.FAMILIES + 1];
        private static readonly byte[] familyDisks = new byte[C_Data.FAMILIES + 1];
        private static readonly byte[] remapFamily = new byte[C_Data.FAMILIES * C_Data.FAMILY_REMAPS + 1];
        private static readonly byte[] remapDisk = new byte[C_Data.FAMILIES * C_Data.FAMILY_REMAPS + 1];
        private static readonly byte[] remapFrom = new byte[C_Data.FAMILIES * C_Data.FAMILY_REMAPS + 1];
        private static readonly byte[] remapTo = new byte[C_Data.FAMILIES * C_Data.FAMILY_REMAPS + 1];
        private static byte remapCount;
        private static byte[] dsDaxCompressedFilePtr;

        public static void FreeRounded(ref byte[] p, ushort n)
        {
            p = null;
        }

        public static void AllocRounded(ref byte[] p, ushort n)
        {
            p = new byte[(n + 7) & 0xFFF8];
        }

        public static void WriteChar(byte col, byte row, byte attr, byte count, byte ch)
        {
            if (GDVars.video == null || GDVars.video.WriteChar == null)
            {
                throw new InvalidOperationException("GDVars.video.WriteChar delegate is not installed.");
            }
            GDVars.video.WriteChar(col, row, attr, count, ch);
        }

        public static void BiosWriteChar(byte page, byte col, byte row, byte attr, byte count, byte ch)
        {
            RequirePlatform();
            Platform.BiosWriteChar(page, col, row, attr, count, ch);
        }

        public static void WriteString(byte col, byte row, byte attr, string source)
        {
            if (source == null) return;
            int len = source.Length;
            if (len > 255) len = 255;
            for (int i = 0; i < len; i++)
            {
                WriteChar(col, row, attr, 1, (byte)source[i]);
                unchecked { col++; }
            }
        }

        public static void WriteBlock(byte col, byte row, byte attr, byte count)
        {
            WriteChar(col, row, attr, count, 219);
        }

        public static void PromptBox(byte attr, string message)
        {
            if (message != null && message.Length > 40)
            {
                message = message.Substring(0, 40);
            }
            WriteBlock(0, 24, 0, 40);
            WriteString(0, 24, attr, message);
            RequirePlatform();
            Platform.ReadGameKey();
            WriteBlock(0, 24, 0, 40);
        }

        public static void OverlayPrompt()
        {
            bool found;
            do
            {
                found = FileExists("game.ovr");
                if (!found)
                {
                    RequirePlatform();
                    Platform.OverlayMessage("Please insert overlay disk.");
                    Platform.ReadGameKey();
                }
            } while (!found);
        }

        public static char DataDrive()
        {
            if (GDVars.gameDir != null && GDVars.gameDir.Length > 0)
            {
                return UpCase(GDVars.gameDir[0]);
            }
            else
            {
                RequirePlatform();
                string dir = Platform.CurrentDirectory;
                if (dir != null && dir.Length > 0)
                {
                    return UpCase(dir[0]);
                }
                throw new InvalidOperationException("Cannot determine data drive: gameDir is empty and CurrentDirectory is invalid.");
            }
        }

        public static bool FileExists(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            try
            {
                FileStream fs = null;
                if (FileUtil.OpenReadOnly(out fs, name))
                {
                    fs.Close();
                    return true;
                }
                return false;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        public static bool OpenWithRetry(string fileName, string promptMsg, bool noPrompt, out FileStream f)
        {
            f = null;
            bool found;
            do
            {
                found = FileExists(fileName);
                if (!found && !noPrompt)
                {
                    RequirePlatform();
                    string full = Platform.FullPath(fileName);
                    if (full.Length > 29)
                    {
                        full = "..." + full.Substring(full.Length - 26, 26);
                    }
                    string msg = "Not found: " + full;
                    PromptBox(14, msg);
                    PromptBox(14, promptMsg);
                }
            } while (!found && !noPrompt);

            if (found)
            {
                FileStream fs = null;
                if (FileUtil.OpenReadOnly(out fs, fileName))
                {
                    f = fs;
                    return true;
                }
            }
            return false;
        }

        private static int ReadAvailable(Stream stream, byte[] buffer, int offset, int count)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int read = stream.Read(buffer, offset + totalRead, count - totalRead);
                if (read == 0) break;
                totalRead += read;
            }
            return totalRead;
        }

        public static bool OpenDax(string dirName, string ext, out ushort headerSize, out FileStream f1, out FileStream f2)
        {
            headerSize = 0;
            f1 = null;
            f2 = null;
            string path = dirName + ext;
            string prompt = GDConsts.insertDiskMsg[GDVars.virtualDisk] + DataDrive() + ":";
            
            if (OpenWithRetry(path, prompt, GDVars.dsSuppressingInsertDiskFlag, out f1))
            {
                FileStream fs2 = null;
                if (FileUtil.OpenReadOnly(out fs2, path))
                {
                    f2 = fs2;
                    byte[] buf = new byte[2];
                    int nread = ReadAvailable(f2, buf, 0, 2);

                    if (nread == 2)
                    {
                        headerSize = (ushort)(buf[0] | (buf[1] << 8));
                        long fileSize = f2.Length;
                        if ((long)headerSize + 2 <= fileSize)
                        {
                            unchecked { headerSize += 2; }
                            f1.Seek(headerSize, SeekOrigin.Begin);
                            return true;
                        }
                    }
                    CloseDax(f1, f2);
                    f1 = null;
                    f2 = null;
                }
                else
                {
                    CloseDax(f1, null);
                    f1 = null;
                }
            }
            return false;
        }

        public static void CloseDax(FileStream f1, FileStream f2)
        {
            if (f1 != null)
            {
                try { f1.Close(); } catch (IOException) { }
            }
            if (f2 != null)
            {
                try { f2.Close(); } catch (IOException) { }
            }
        }

        private static bool DiskHasSource(byte family, byte disk)
        {
            return (familyDisks[family] & (1 << (disk - 1))) != 0;
        }

        private static bool SourcesMatch(byte family, byte[] data, ushort size)
        {
            familyDisks[family] = 0;
            bool ok = (size % C_Data.SOURCE_RECORD) == 0;
            int i = 0;
            while (ok && i < size)
            {
                byte d = data[i];
                ok = (d >= 1) && (d <= 8);
                if (ok)
                {
                    int listed = data[i + 1] | (data[i + 2] << 8) | (data[i + 3] << 16) | (data[i + 4] << 24);
                    familyDisks[family] |= (byte)(1 << (d - 1));
                    string srcName = GDVars.gameDir + DataArc.SourceName(family, d) + ".DAX";
                    long len = DataArc.FileLength(srcName);
                    ok = (len == listed);
                }
                i += C_Data.SOURCE_RECORD;
            }

            for (byte d = 1; d <= 8; d++)
            {
                if (ok && !DiskHasSource(family, d))
                {
                    string srcName = GDVars.gameDir + DataArc.SourceName(family, d) + ".DAX";
                    long len = DataArc.FileLength(srcName);
                    ok = (len < 0);
                }
            }
            return ok;
        }

        public static bool FamilyUsable(byte family)
        {
            if (familyState[family] == 0)
            {
                familyState[family] = 2;
                string mergedName = DataArc.MergedName(family);
                string path = GDVars.gameDir + mergedName + ".DAX";
                
                if (DataArc.ArchiveIntact(path))
                {
                    ushort size;
                    byte[] data;
                    LoadDax(mergedName, ".dax", C_Data.VERSION_BLOCK, out size, out data);
                    if (data != null)
                    {
                        if (size >= 1 && data[0] == C_Data.VERSION)
                        {
                            FreeRounded(ref data, size);
                            LoadDax(mergedName, ".dax", C_Data.SOURCES_BLOCK, out size, out data);
                            if (data != null)
                            {
                                if (SourcesMatch(family, data, size))
                                {
                                    familyState[family] = 1;
                                }
                            }
                        }
                        if (data != null)
                        {
                            FreeRounded(ref data, size);
                        }
                    }
                }

                if (familyState[family] == 1)
                {
                    ushort size;
                    byte[] data;
                    LoadDax(mergedName, ".dax", C_Data.REMAP_BLOCK, out size, out data);
                    if (data != null)
                    {
                        byte first = remapCount;
                        int i = 0;
                        while (i + 2 < size && familyState[family] == 1)
                        {
                            byte d = data[i];
                            byte from = data[i + 1];
                            byte to = data[i + 2];
                            
                            if (d >= 1 && d <= 8 && from < C_Data.SOURCES_BLOCK && to < C_Data.SOURCES_BLOCK)
                            {
                                if (remapCount == C_Data.FAMILIES * C_Data.FAMILY_REMAPS)
                                {
                                    familyState[family] = 2;
                                    remapCount = first;
                                }
                                else
                                {
                                    remapCount++;
                                    remapFamily[remapCount] = family;
                                    remapDisk[remapCount] = d;
                                    remapFrom[remapCount] = from;
                                    remapTo[remapCount] = to;
                                }
                            }
                            i += 3;
                        }
                        FreeRounded(ref data, size);
                    }
                }
            }
            return familyState[family] == 1;
        }

        public static void ResetArchiveCache()
        {
            Array.Clear(familyState, 0, familyState.Length);
            Array.Clear(familyDisks, 0, familyDisks.Length);
            remapCount = 0;
        }

        private static byte RemapId(byte family, byte disk, byte id)
        {
            byte result = id;
            for (int i = 1; i <= remapCount; i++)
            {
                if (remapFamily[i] == family && remapDisk[i] == disk && remapFrom[i] == id)
                {
                    result = remapTo[i];
                }
            }
            return result;
        }

        public static string ArchiveName(string fileName)
        {
            byte disk = 0;
            byte family = DataArc.MatchFamily(fileName, ref disk);
            if (family > 0)
            {
                if (FamilyUsable(family) && DiskHasSource(family, disk))
                {
                    return DataArc.MergedName(family);
                }
            }
            return fileName;
        }

        public static bool DiskHasBlock(string fileName, byte fileID)
        {
            return DataArc.HasBlock(GDVars.gameDir + fileName + ".DAX", fileID);
        }

        public static void LoadDax(string fileName, string ext, byte fileID, out ushort sizeRead, out byte[] data)
        {
            sizeRead = 0;
            data = null;
            
            byte disk = 0;
            byte family = DataArc.MatchFamily(fileName, ref disk);
            
            if (family > 0 && fileID < C_Data.SOURCES_BLOCK)
            {
                if (FamilyUsable(family) && DiskHasSource(family, disk))
                {
                    fileID = RemapId(family, disk, fileID);
                    fileName = DataArc.MergedName(family);
                }
            }

            string path = GDVars.gameDir + fileName + ext;
            ushort hdrSize;
            FileStream f1, f2;
            
            if (!OpenDax(path, "", out hdrSize, out f1, out f2))
            {
                return;
            }

            bool cut = false;
            byte id = 0;
            int offset = 0;
            ushort compSize = 0;
            
            try
            {
                do
                {
                    byte[] bufId = new byte[1];
                    byte[] bufOff = new byte[4];
                    byte[] bufSize = new byte[2];
                    byte[] bufComp = new byte[2];
                    
                    int n1 = ReadAvailable(f2, bufId, 0, 1);
                    int n2 = ReadAvailable(f2, bufOff, 0, 4);
                    int n3 = ReadAvailable(f2, bufSize, 0, 2);
                    int n4 = ReadAvailable(f2, bufComp, 0, 2);

                    cut = (n4 < 2);
                    
                    if (!cut)
                    {
                        id = bufId[0];
                        offset = bufOff[0] | (bufOff[1] << 8) | (bufOff[2] << 16) | (bufOff[3] << 24);
                        sizeRead = (ushort)(bufSize[0] | (bufSize[1] << 8));
                        compSize = (ushort)(bufComp[0] | (bufComp[1] << 8));
                        
                        if (compSize > 36000)
                        {
                            string numStr = compSize.ToString();
                            PromptBox(14, "tempsize " + numStr);
                            RequirePlatform();
                            Platform.ExitGame();
                            throw new InvalidOperationException("Game exited due to tempsize prompt.");
                        }
                    }
                } while (!cut && id != fileID && f2.Position < hdrSize);

                if (cut || id != fileID)
                {
                    sizeRead = 0;
                    data = null;
                }
                else
                {
                    AllocRounded(ref dsDaxCompressedFilePtr, compSize);
                    data = new byte[sizeRead];
                    
                    f1.Seek(unchecked((int)hdrSize + offset), SeekOrigin.Begin);
                    int nRead = ReadAvailable(f1, dsDaxCompressedFilePtr, 0, compSize);
                    
                    byte[] actual = new byte[nRead];
                    Array.Copy(dsDaxCompressedFilePtr, actual, nRead);
                    
                    data = DecompressDax(actual, sizeRead);
                    
                    FreeRounded(ref dsDaxCompressedFilePtr, compSize);
                }
            }
            finally
            {
                CloseDax(f1, f2);
            }
        }

        public static void PushState(byte v)
        {
            GDVars.prevVirtualDisk = GDVars.virtualDisk;
            GDVars.virtualDisk = v;
        }

        public static void PopState()
        {
            GDVars.virtualDisk = GDVars.prevVirtualDisk;
        }

        static DaxIo()
        {
            ResetArchiveCache();
        }
    }
}
