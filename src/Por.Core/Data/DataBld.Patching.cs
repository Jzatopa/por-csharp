using System;
using System.IO;
using System.Security;

namespace Por.Core
{
    public static partial class DataBld
    {
        internal static bool ApplyFix(byte fam, ushort first, ushort pos, ref byte[] raw, ref byte[] spare, ushort room, ref ushort rawLen, ref string why)
        {
            why = null;
            int len = rawLen;
            bool ok = true;
            ushort p = first;
            ushort q = pos;
            Array.Copy(raw, 0, spare, 0, rawLen);

            while (ok && p <= DataPat.PART_COUNT && SameFix(first, p))
            {
                DataPat.TPatchPart part = DataPat.parts[p];
                ok = (part.size == len)
                    && (unchecked((int)part.offset + part.origLen <= len))
                    && (unchecked((int)q + part.origLen + part.newLen <= DataPat.BYTE_COUNT));

                int k = 0;
                while (ok && k < part.origLen)
                {
                    ok = spare[part.offset + k] == DataPat.patchBytes[q + k];
                    k++;
                }

                if (!ok)
                {
                    why = "the block differs";
                }
                else
                {
                    int newLen = len - part.origLen + part.newLen;
                    ok = (newLen <= MaxRawSize(fam)) && (newLen <= room);
                    if (!ok)
                    {
                        why = "the block would be too long";
                    }
                    else
                    {
                        int moveLen = len - part.offset - part.origLen;
                        if (moveLen > 0)
                        {
                            Array.Copy(spare, part.offset + part.origLen, spare, part.offset + part.newLen, moveLen);
                        }
                        for (int i = 0; i < part.newLen; i++)
                            spare[part.offset + i] = DataPat.patchBytes[q + part.origLen + i];
                        len = newLen;
                    }
                }
                q = unchecked((ushort)(q + part.origLen + part.newLen));
                p++;
            }

            if (ok)
            {
                byte[] t = raw;
                raw = spare;
                spare = t;
                rawLen = unchecked((ushort)len);
            }
            return ok;
        }

        internal static bool PatchBlock(byte fam, ushort first, ushort pos)
        {
            byte disk = DataPat.parts[first].disk;
            byte id = DataPat.parts[first].id;
            Progress(archiveName + ".DAX: fixing " + SourceName(fam, disk) + " block " + id.ToString());
            RequirePlatform();
            Platform.Fixing(fam, disk);

            bool ok = true;
            bool anyFixed = false;
            bool held = false;
            string note = "";
            ushort target = ResolveBlock(disk, id);

            if (target == 0)
            {
                note = "the block is not there";
            }
            else if (BlockShared(disk, id, target))
            {
                target = SplitBlock(disk, id, target, ref note);
            }

            int room = 0;
            if (target > 0)
            {
                room = blocks[target].rawSize;
                for (ushort p = first; p <= DataPat.PART_COUNT; p++)
                {
                    if (SameBlockPart(first, p))
                        room += DataPat.parts[p].newLen;
                }
                if (room > MAX_ENCODED)
                    room = MAX_ENCODED;

                if (blocks[target].disk == 0 || blocks[target].packedSize == 0 || blocks[target].rawSize == 0 || blocks[target].rawSize > MAX_ENCODED)
                {
                    note = "the block cannot be patched";
                }
                else
                {
                    ok = Reserve(blocks[target].packedSize + 2 * room + 24);
                }
            }

            byte[] packedBuf = null;
            byte[] raw = null;
            byte[] spare = null;
            ushort packedLen = 0;
            ushort rawLen = 0;

            if (ok && (note == "" || note == null))
            {
                packedLen = blocks[target].packedSize;
                rawLen = blocks[target].rawSize;
                try
                {
                    packedBuf = new byte[packedLen];
                    raw = new byte[room];
                    spare = new byte[room];
                    held = true;
                }
                catch (OutOfMemoryException)
                {
                    Fail("not enough memory");
                    ok = false;
                }
            }

            if (ok && held)
            {
                lastError = 0;
                string path = GamePath(SourceName(fam, blocks[target].disk));
                FileStream f;
                if (OpenInput(path, out f))
                {
                    ok = IoOk();
                    if (ok)
                    {
                        ok = ReadAt(f, blocks[target].start, packedBuf, packedLen);
                    }
                    CloseFile(f);
                    n = lastError;
                }
                else
                {
                    ok = false;
                }

                if (ok)
                {
                    DecodeRuns(packedBuf, packedLen, raw, rawLen);
                }
                else
                {
                    Fail("reading " + SourceName(fam, blocks[target].disk) + " for a fix: DOS error " + lastError.ToString());
                }
            }

            if (ok)
            {
                ushort q = pos;
                for (ushort p = first; p <= DataPat.PART_COUNT; p++)
                {
                    if (SameBlockPart(first, p) && ((p == first) || !SameFix(unchecked((ushort)(p - 1)), p)))
                    {
                        if (note != "" && note != null)
                        {
                            LogLine(archiveName + ".DAX: " + IssueName(DataPat.parts[p].issue) + " skipped: " + note);
                        }
                        else
                        {
                            string why = null;
                            if (ApplyFix(fam, p, q, ref raw, ref spare, unchecked((ushort)room), ref rawLen, ref why))
                            {
                                anyFixed = true;
                                LogLine(archiveName + ".DAX: " + IssueName(DataPat.parts[p].issue) + " applied");
                            }
                            else
                            {
                                LogLine(archiveName + ".DAX: " + IssueName(DataPat.parts[p].issue) + " skipped: " + why);
                            }
                        }
                    }
                    q = unchecked((ushort)(q + DataPat.parts[p].origLen + DataPat.parts[p].newLen));
                }

                if (anyFixed)
                {
                    blocks[target].disk = 0;
                    blocks[target].start = 0;
                    blocks[target].rawSize = rawLen;
                    EncodeRuns(raw, rawLen, out blocks[target].mem, out blocks[target].packedSize);
                    ok = (failNote == null || failNote.Length == 0);
                }
            }

            if (held)
            {
                spare = null;
                raw = null;
            }

            return ok;
        }
    }
}
