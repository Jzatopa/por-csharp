using System;
using System.IO;
using System.Security;

namespace Por.Core
{
    public static partial class DataBld
    {
        internal static bool ChooseIds(byte fam)
        {
            bool[] used = new bool[256];
            bool[] taken = new bool[256];
            ushort[] owner = new ushort[256];

            for (int idx = 1; idx <= sourceCount; idx++)
                used[sources[idx].id] = true;

            int i = 1;
            while (i <= sourceCount && (failNote == null || failNote.Length == 0))
            {
                byte id = sources[i].id;
                if (owner[id] == 0)
                {
                    blockCount++;
                    blocks[blockCount] = sources[i];
                    owner[id] = blockCount;
                }
                else if (!SameBlock(fam, ref blocks[owner[id]], ref sources[i]) && (failNote == null || failNote.Length == 0))
                {
                    ushort hit = 0;
                    for (int r = 1; r <= remapCount; r++)
                    {
                        if (hit == 0 && remapFrom[r] == id && (failNote == null || failNote.Length == 0))
                        {
                            if (SameBlock(fam, ref blocks[remapBlock[r]], ref sources[i]))
                                hit = unchecked((ushort)r);
                        }
                    }

                    if (remapCount == MAX_REMAPS)
                    {
                        Fail("too many collisions");
                    }
                    else if (failNote == null || failNote.Length == 0)
                    {
                        remapCount++;
                        remapDisk[remapCount] = sources[i].disk;
                        remapFrom[remapCount] = id;

                        if (hit > 0)
                        {
                            remapTo[remapCount] = remapTo[hit];
                            remapBlock[remapCount] = remapBlock[hit];
                        }
                        else
                        {
                            int spare = 1;
                            while (spare < C_Data.SOURCES_BLOCK && (used[spare] || taken[spare]))
                                spare++;

                            if (spare >= C_Data.SOURCES_BLOCK)
                            {
                                Fail("no spare block id left");
                            }
                            else
                            {
                                taken[spare] = true;
                                blockCount++;
                                blocks[blockCount] = sources[i];
                                blocks[blockCount].id = unchecked((byte)spare);
                                remapTo[remapCount] = unchecked((byte)spare);
                                remapBlock[remapCount] = blockCount;
                            }
                        }
                    }
                }
                i++;
            }

            return failNote == null || failNote.Length == 0;
        }

        internal static bool CopyBlock(byte fam, ref TBlock b, FileStream dst, ref TCrc crc)
        {
            bool ok;
            if (b.disk == 0)
            {
                ok = WriteBytes(dst, b.mem, b.packedSize);
                if (ok)
                {
                    AddCrc(ref crc, b.mem, b.packedSize);
                    RequirePlatform();
                    Platform.Advance(b.packedSize);
                    ok = IoOk();
                }
            }
            else
            {
                string path = GamePath(SourceName(fam, b.disk));
                FileStream src;
                ok = OpenInput(path, out src);
                if (ok)
                {
                    try
                    {
                        ushort left = b.packedSize;
                        int pos = b.start;
                        while (ok && left > 0)
                        {
                            ushort part = left;
                            if (part > CHUNK_SIZE)
                                part = CHUNK_SIZE;

                            ok = ReadAt(src, pos, bufA, part);
                            if (ok)
                            {
                                ok = WriteBytes(dst, bufA, part);
                                if (ok)
                                {
                                    AddCrc(ref crc, bufA, part);
                                    RequirePlatform();
                                    Platform.Advance(part);
                                    ok = IoOk();
                                }
                            }
                            pos += part;
                            left = unchecked((ushort)(left - part));
                        }
                    }
                    finally
                    {
                        CloseFile(src);
                        n = lastError;
                    }
                }
            }
            return ok;
        }

        internal static bool WriteArchive(byte fam)
        {
            lastError = 0;
            string path = GamePath(archiveName + ".$$$");
            FileStream dst;
            bool ok = CreateOutput(path, out dst);
            if (ok)
            {
                try
                {
                    TCrc crc = new TCrc();
                    crc.value = -1;
                    ushort dirSize = unchecked((ushort)(blockCount * 9));
                    byte[] dirSizeBuf = new byte[2];
                    WriteLE16(dirSizeBuf, 0, dirSize);
                    ok = WriteBytes(dst, dirSizeBuf, 2);
                    if (ok)
                    {
                        AddCrc(ref crc, dirSizeBuf, 2);
                        int nextOffset = 0;
                        for (int idx = 1; idx <= blockCount; idx++)
                        {
                            byte[] eBuf = new byte[9];
                            eBuf[0] = blocks[idx].id;
                            WriteLE32(eBuf, 1, nextOffset);
                            WriteLE16(eBuf, 5, blocks[idx].rawSize);
                            WriteLE16(eBuf, 7, blocks[idx].packedSize);
                            ok = WriteBytes(dst, eBuf, 9);
                            if (ok)
                            {
                                AddCrc(ref crc, eBuf, 9);
                                nextOffset = unchecked(nextOffset + blocks[idx].packedSize);
                            }
                        }
                        if (ok)
                        {
                            RequirePlatform();
                            Platform.Writing(nextOffset);
                            ok = IoOk();
                        }
                        int i = 1;
                        while (ok && i < blockCount)
                        {
                            ok = CopyBlock(fam, ref blocks[i], dst, ref crc);
                            i++;
                        }
                        if (ok)
                        {
                            byte[] version = new byte[VERSION_PACKED];
                            version[0] = unchecked((byte)(VERSION_RAW - 1));
                            version[1] = unchecked((byte)C_Data.VERSION);
                            crc.value = ~crc.value;
                            WriteLE32(version, 2, crc.value);
                            ok = WriteBytes(dst, version, VERSION_PACKED);
                            if (ok)
                                ok = IoOk();
                        }
                    }
                }
                finally
                {
                    CloseFile(dst);
                    ok = IoOk() && ok;
                }
            }
            if (!ok)
            {
                if (lastError != 0)
                    FailWriting("writing " + archiveName + ".$$$: DOS error " + lastError.ToString());
                else
                    FailWriting("writing " + archiveName + ".$$$");
            }
            return ok;
        }

    }
}
