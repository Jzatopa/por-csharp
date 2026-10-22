using System;
using System.IO;
using System.Security;

namespace Por.Core
{
    public static partial class DataBld
    {
        internal static bool CrcOfFile(FileStream f, int size, out int crc)
        {
            TCrc c = new TCrc();
            c.value = -1;
            int pos = 0;
            bool ok = true;
            while (ok && pos < size)
            {
                ushort part = CHUNK_SIZE;
                if (size - pos < part)
                    part = unchecked((ushort)(size - pos));
                ok = ReadAt(f, pos, bufA, part);
                AddCrc(ref c, bufA, part);
                RequirePlatform();
                Platform.Advance(part);
                pos += part;
            }
            crc = ~c.value;
            return ok;
        }

        internal static string SourceName(byte fam, byte disk)
        {
            return DataArc.SourceName(fam, disk) + ".DAX";
        }

        internal static string GamePath(string name)
        {
            return GDVars.gameDir + name;
        }

        internal static void RemoveFile(string name)
        {
            FileUtil.DeleteFile(GamePath(name));
            n = lastError;
        }

        internal static bool HasFiles(byte fam)
        {
            bool found = DataArc.FileLength(GamePath(DataArc.MergedName(fam) + ".DAX")) >= 0;
            for (byte disk = 1; disk <= SOURCE_DISKS; disk++)
            {
                if (!found)
                    found = DataArc.FileLength(GamePath(SourceName(fam, disk))) >= 0;
            }
            return found;
        }

        internal static bool HasRoom(byte fam)
        {
            int need = ROOM_MARGIN;
            for (byte disk = 1; disk <= SOURCE_DISKS; disk++)
            {
                int size = DataArc.FileLength(GamePath(SourceName(fam, disk)));
                if (size > 0)
                    need = unchecked(need + size);
            }
            int free = -1;
            string dir = GDVars.gameDir;
            if (dir.Length >= 2 && dir[1] == ':' && (dir[0] >= 'A' && dir[0] <= 'Z' || dir[0] >= 'a' && dir[0] <= 'z'))
            {
                byte drive = unchecked((byte)(dir[0] >= 'a' ? dir[0] - 'a' + 1 : dir[0] - 'A' + 1));
                RequirePlatform();
                free = Platform.DiskFree(drive);
            }
            if (free >= 0 && free < need)
            {
                Fail("not enough room on the disk: " + need.ToString() + " bytes wanted, " + free.ToString() + " free");
            }
            return free < 0 || free >= need;
        }

        internal static bool CanWrite()
        {
            lastError = 0;
            string path = GamePath(archiveName + ".$$$");
            FileStream f;
            bool ok = CreateOutput(path, out f);
            if (ok)
            {
                CloseFile(f);
                ok = IoOk();
            }
            if (!ok)
            {
                FailWriting("writing " + archiveName + ".$$$: DOS error " + lastError.ToString());
            }
            return ok;
        }

        internal static bool ReadSource(FileStream f, byte fam, byte disk)
        {
            lastError = 0;
            int size = unchecked((int)f.Length);
            byte[] dirSizeBuf = new byte[2];
            bool ok = IoOk() && ReadAt(f, 0, dirSizeBuf, 2);
            ushort dirSize = ReadLE16(dirSizeBuf, 0);
            int dataStart = unchecked(2 + dirSize);
            ok = ok && (dirSize % 9 == 0) && (dataStart <= size);
            int i = 1;
            while (ok && i <= dirSize / 9)
            {
                byte[] eBuf = new byte[9];
                ok = ReadAt(f, unchecked(2 + (i - 1) * 9), eBuf, 9);
                if (ok)
                {
                    byte eId = eBuf[0];
                    int eOffset = ReadLE32(eBuf, 1);
                    ushort eRawSize = ReadLE16(eBuf, 5);
                    ushort ePackedSize = ReadLE16(eBuf, 7);
                    ok = ok && (eOffset >= 0) && (unchecked(dataStart + eOffset + ePackedSize) <= size);
                    if (ok)
                    {
                        if (sourceCount + 3 >= MAX_BLOCKS)
                        {
                            Fail("too many blocks");
                            ok = false;
                        }
                        else
                        {
                            sourceCount++;
                            sources[sourceCount].id = eId;
                            sources[sourceCount].rawSize = eRawSize;
                            sources[sourceCount].packedSize = ePackedSize;
                            sources[sourceCount].disk = disk;
                            sources[sourceCount].start = unchecked(dataStart + eOffset);
                            sources[sourceCount].mem = null;
                        }
                    }
                }
                i++;
            }
            int crc = 0;
            ok = ok && CrcOfFile(f, size, out crc);
            if (ok && (sourceListLen + C_Data.SOURCE_RECORD <= SOURCES_SIZE))
            {
                sourceList[sourceListLen] = disk;
                WriteLE32(sourceList, sourceListLen + 1, size);
                WriteLE32(sourceList, sourceListLen + 5, crc);
                sourceListLen += C_Data.SOURCE_RECORD;
            }
            if (!ok)
            {
                if (lastError != 0)
                    Fail("reading " + SourceName(fam, disk) + ": DOS error " + lastError.ToString());
                else
                    Fail(SourceName(fam, disk) + " is damaged");
            }
            return ok;
        }

        internal static bool ReadSources(byte fam)
        {
            sourceCount = 0;
            sourceListLen = 0;
            bool ok = true;
            for (byte disk = 1; disk <= SOURCE_DISKS; disk++)
            {
                if (ok)
                {
                    string path = GamePath(SourceName(fam, disk));
                    FileStream f;
                    if (OpenInput(path, out f))
                    {
                        try
                        {
                            Progress(archiveName + ".DAX: reading " + SourceName(fam, disk));
                            RequirePlatform();
                            Platform.Reading(fam, disk);
                            ok = ReadSource(f, fam, disk);
                        }
                        finally
                        {
                            CloseFile(f);
                            n = lastError;
                        }
                    }
                }
            }
            return ok;
        }
    }
}
