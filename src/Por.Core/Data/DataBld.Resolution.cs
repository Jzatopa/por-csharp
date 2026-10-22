using System;
using System.IO;
using System.Security;

namespace Por.Core
{
    public static partial class DataBld
    {
        internal static void DecodeRuns(byte[] packed, ushort packedSize, byte[] raw, ushort rawSize)
        {
            for (int clearIndex = 0; clearIndex < rawSize; clearIndex++)
                raw[clearIndex] = 0;
            int i = 0;
            int o = 0;
            while (i < packedSize && o < rawSize)
            {
                byte b = packed[i];
                i++;
                if (b <= 0x7E)
                {
                    int run = b + 1;
                    if (run > packedSize - i)
                        run = packedSize - i;
                    if (run > rawSize - o)
                        run = rawSize - o;
                    for (int k = 0; k < run; k++)
                        raw[o + k] = packed[i + k];
                    i += run;
                    o += run;
                }
                else if (b >= 0x81 && i < packedSize)
                {
                    int run = 256 - b;
                    if (run > rawSize - o)
                        run = rawSize - o;
                    byte val = packed[i];
                    for (int k = 0; k < run; k++)
                        raw[o + k] = val;
                    i++;
                    o += run;
                }
            }
        }

        internal static string IssueName(ushort issue)
        {
            return "#" + issue.ToString();
        }

        internal static ushort MaxRawSize(byte fam)
        {
            if (fam == C_Data.FAMILY_ECL)
                return unchecked((ushort)(2 + C_Gbvm.SCRIPT_LAST - C_Gbvm.SCRIPT_FIRST + 1));
            else
                return unchecked((ushort)MAX_LOADABLE);
        }

        internal static bool SameBlockPart(ushort a, ushort b)
        {
            return DataPat.parts[a].family == DataPat.parts[b].family
                && DataPat.parts[a].disk == DataPat.parts[b].disk
                && DataPat.parts[a].id == DataPat.parts[b].id;
        }

        internal static bool SameFix(ushort a, ushort b)
        {
            return DataPat.parts[a].issue == DataPat.parts[b].issue && SameBlockPart(a, b);
        }

        internal static ushort RemapOf(byte disk, byte id)
        {
            ushort found = 0;
            for (ushort r = 1; r <= remapCount; r++)
            {
                if (remapDisk[r] == disk && remapFrom[r] == id)
                    found = r;
            }
            return found;
        }

        internal static ushort ResolveBlock(byte disk, byte id)
        {
            ushort found = 0;
            ushort r = RemapOf(disk, id);
            if (r > 0)
            {
                found = remapBlock[r];
            }
            else
            {
                bool listed = false;
                for (ushort i = 1; i <= sourceCount; i++)
                {
                    if (sources[i].disk == disk && sources[i].id == id)
                        listed = true;
                }
                if (listed)
                {
                    for (ushort i = 1; i <= blockCount; i++)
                    {
                        if (blocks[i].id == id)
                            found = i;
                    }
                }
            }
            return found;
        }

        internal static bool BlockShared(byte disk, byte id, ushort target)
        {
            bool shared = false;
            for (ushort i = 1; i <= sourceCount; i++)
            {
                if (!shared && sources[i].id == id && sources[i].disk != disk)
                    shared = ResolveBlock(sources[i].disk, id) == target;
            }
            return shared;
        }

        internal static ushort SplitBlock(byte disk, byte id, ushort target, ref string why)
        {
            why = null;
            bool[] used = new bool[256];
            for (ushort i = 1; i <= sourceCount; i++)
                used[sources[i].id] = true;
            for (ushort i = 1; i <= blockCount; i++)
                used[blocks[i].id] = true;

            ushort spare = 1;
            while (spare < C_Data.SOURCES_BLOCK && used[spare])
                spare++;

            ushort r = RemapOf(disk, id);

            if (blocks[target].disk == 0)
            {
                why = "the block is not in its archive";
            }
            else if (spare >= C_Data.SOURCES_BLOCK)
            {
                why = "no spare block id left";
            }
            else if (r == 0 && remapCount == MAX_REMAPS)
            {
                why = "too many collisions";
            }
            else if (blockCount + 3 >= MAX_BLOCKS)
            {
                why = "too many blocks";
            }
            else
            {
                blockCount++;
                blocks[blockCount] = blocks[target];
                blocks[blockCount].id = unchecked((byte)spare);
                if (r == 0)
                {
                    remapCount++;
                    r = remapCount;
                    remapDisk[r] = disk;
                    remapFrom[r] = id;
                }
                remapTo[r] = unchecked((byte)spare);
                remapBlock[r] = blockCount;
                return blockCount;
            }
            return 0;
        }
    }
}
