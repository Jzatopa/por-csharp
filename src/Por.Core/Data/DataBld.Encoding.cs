using System;
using System.IO;
using System.Security;

namespace Por.Core
{
    public static partial class DataBld
    {
        internal static bool SameBlock(byte fam, ref TBlock a, ref TBlock b)
        {
            bool same = (a.packedSize == b.packedSize) && (a.rawSize == b.rawSize);
            if (same)
            {
                lastError = 0;
                string pathA = GamePath(SourceName(fam, a.disk));
                string pathB = GamePath(SourceName(fam, b.disk));
                FileStream fa = null;
                FileStream fb = null;
                bool okA = OpenInput(pathA, out fa);
                bool okB = false;
                if (okA && IoOk())
                {
                    okB = OpenInput(pathB, out fb);
                }
                if (okA && okB && IoOk())
                {
                    ushort left = a.packedSize;
                    int posA = a.start;
                    int posB = b.start;
                    while (same && left > 0)
                    {
                        ushort part = left;
                        if (part > CHUNK_SIZE)
                            part = CHUNK_SIZE;
                        if (ReadAt(fa, posA, bufA, part) && ReadAt(fb, posB, bufB, part))
                        {
                            int i = 0;
                            while (same && i < part)
                            {
                                same = bufA[i] == bufB[i];
                                i++;
                            }
                        }
                        else
                        {
                            same = false;
                        }
                        posA += part;
                        posB += part;
                        left = unchecked((ushort)(left - part));
                    }
                }
                if (fb != null) CloseFile(fb);
                if (fa != null) CloseFile(fa);
                n = lastError;
                if (lastError != 0)
                {
                    Fail("comparing " + SourceName(fam, b.disk) + ": DOS error " + lastError.ToString());
                    same = false;
                }
            }
            return same;
        }

        internal static void EncodeRuns(byte[] raw, ushort rawSize, out byte[] packedBuf, out ushort packedSize)
        {
            packedBuf = null;
            packedSize = 0;
            if (rawSize > MAX_ENCODED)
            {
                Fail("a block is too large to pack");
            }
            else if (rawSize > 0)
            {
                packedSize = unchecked((ushort)(rawSize + (rawSize + RUN_SIZE - 1) / RUN_SIZE));
                if (Reserve(packedSize))
                {
                    try
                    {
                        packedBuf = new byte[packedSize];
                    }
                    catch (OutOfMemoryException)
                    {
                        Fail("not enough memory");
                        packedBuf = null;
                        packedSize = 0;
                    }
                }
                else
                {
                    packedSize = 0;
                }
            }
            if (packedSize > 0 && packedBuf != null)
            {
                int i = 0;
                int o = 0;
                while (packedSize > 0 && i < rawSize)
                {
                    int run = rawSize - i;
                    if (run > RUN_SIZE)
                        run = RUN_SIZE;
                    packedBuf[o] = unchecked((byte)(run - 1));
                    for (int k = 0; k < run; k++)
                        packedBuf[o + 1 + k] = raw[i + k];
                    o += run + 1;
                    i += run;
                }
            }
        }

        internal static void AddMemoryBlock(byte blockId, byte[] raw, ushort size)
        {
            blockCount++;
            blocks[blockCount].id = blockId;
            blocks[blockCount].rawSize = size;
            blocks[blockCount].disk = 0;
            blocks[blockCount].start = 0;
            EncodeRuns(raw, size, out blocks[blockCount].mem, out blocks[blockCount].packedSize);
        }

        internal static bool ArchiveSound(string path)
        {
            bool ok = false;
            if (DataArc.ArchiveIntact(path))
            {
                FileStream f = null;
                if (OpenInput(path, out f))
                {
                    if (IoOk())
                    {
                        try
                        {
                            long size = f.Length;
                            byte[] version = new byte[VERSION_PACKED];
                            int stored = 0;
                            ok = ReadAt(f, unchecked((int)(size - VERSION_PACKED)), version, VERSION_PACKED)
                                 && (version[0] == VERSION_RAW - 1)
                                 && (version[1] == C_Data.VERSION);
                            if (ok)
                            {
                                stored = ReadLE32(version, 2);
                                int crc;
                                ok = CrcOfFile(f, unchecked((int)(size - VERSION_PACKED)), out crc) && (crc == stored);
                            }
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
