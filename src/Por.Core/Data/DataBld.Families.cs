using System;
using System.IO;
using System.Security;

namespace Por.Core
{
    public static partial class DataBld
    {
        internal static bool ApplyPatches(byte fam)
        {
            bool ok = true;
            ushort pos = 0;
            for (ushort p = 1; p <= DataPat.PART_COUNT; p++)
            {
                if (ok && DataPat.parts[p].family == fam)
                {
                    bool firstOnBlock = true;
                    for (ushort q = 1; q < p; q++)
                    {
                        if (SameBlockPart(q, p))
                            firstOnBlock = false;
                    }
                    if (firstOnBlock)
                        ok = PatchBlock(fam, p, pos);
                }
                pos = unchecked((ushort)(pos + DataPat.parts[p].origLen + DataPat.parts[p].newLen));
            }
            return ok;
        }

        internal static bool BuildArchive(byte fam)
        {
            bool ok = ChooseIds(fam);
            if (ok)
                ok = ApplyPatches(fam);
            if (ok)
            {
                AddMemoryBlock(C_Data.SOURCES_BLOCK, sourceList, sourceListLen);
                if (remapCount > 0)
                {
                    byte[] table = new byte[3 * remapCount];
                    for (ushort r = 1; r <= remapCount; r++)
                    {
                        table[3 * (r - 1)] = remapDisk[r];
                        table[3 * (r - 1) + 1] = remapFrom[r];
                        table[3 * (r - 1) + 2] = remapTo[r];
                    }
                    AddMemoryBlock(C_Data.REMAP_BLOCK, table, unchecked((ushort)(3 * remapCount)));
                }
                byte[] version = new byte[VERSION_RAW];
                AddMemoryBlock(C_Data.VERSION_BLOCK, version, VERSION_RAW);
                ok = (failNote == null || failNote.Length == 0);
            }
            if (ok)
            {
                Progress(archiveName + ".DAX: writing");
                ok = WriteArchive(fam);
            }
            if (ok)
            {
                Progress(archiveName + ".DAX: checking");
                RequirePlatform();
                Platform.Checking();
                ok = ArchiveSound(GamePath(archiveName + ".$$$"));
                if (!ok)
                    Fail(archiveName + ".$$$ did not read back whole");
            }
            if (ok)
            {
                RemoveFile(archiveName + ".DAX");
                string srcPath = GamePath(archiveName + ".$$$");
                string dstPath = GamePath(archiveName + ".DAX");
                lastError = 0;
                try
                {
                    System.IO.File.Move(srcPath, dstPath);
                    ioSuccess = true;
                }
                catch (Exception ex)
                {
                    if (ex is IOException || ex is UnauthorizedAccessException || ex is SecurityException || ex is ArgumentException || ex is NotSupportedException)
                    {
                        SetIoError(ex);
                    }
                    else
                    {
                        throw;
                    }
                }
                ok = IoOk();
                if (!ok)
                    FailWriting("renaming " + archiveName + ".$$$: DOS error " + lastError.ToString());
            }
            return ok;
        }

        internal static void UpdateFamily(byte fam)
        {
            archiveName = DataArc.MergedName(fam);
            failNote = "";
            blockCount = 0;
            remapCount = 0;
            if (writeNote != null && writeNote.Length > 0)
            {
                Fail("not tried: " + writeNote);
            }
            else if (memNote != null && memNote.Length > 0)
            {
                Fail("not tried: " + memNote);
            }
            else if (HasRoom(fam) && CanWrite() && ReadSources(fam))
            {
                if (sourceCount == 0)
                {
                    LogLine(archiveName + ".DAX skipped: no per-disk archives");
                    RemoveFile(archiveName + ".$$$");
                    RemoveFile(archiveName + ".DAX");
                }
                else
                {
                    for (ushort i = 1; i <= sourceCount; i++)
                    {
                        if (sources[i].id >= C_Data.SOURCES_BLOCK)
                            Fail("a per-disk archive uses block " + sources[i].id.ToString());
                    }
                    if (failNote == null || failNote.Length == 0)
                    {
                        if (BuildArchive(fam))
                        {
                            LogLine(archiveName + ".DAX built: " + blockCount.ToString() + " blocks, " + remapCount.ToString() + " collisions");
                            builtCount++;
                        }
                    }
                }
            }
            if (failNote != null && failNote.Length > 0)
            {
                LogLine(archiveName + ".DAX FAILED: " + failNote);
                RemoveFile(archiveName + ".$$$");
                RemoveFile(archiveName + ".DAX");
                failedCount++;
            }
            for (int i = blockCount; i >= 1; i--)
            {
                if (blocks[i].disk == 0 && blocks[i].packedSize > 0)
                {
                    blocks[i].mem = null;
                }
            }
        }
    }
}
