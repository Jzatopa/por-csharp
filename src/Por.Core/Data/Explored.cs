using System;

namespace Por.Core
{
    public static partial class Explored
    {
        public const int MAX_AREAS = 64;
        private const string SIG = "EXPL";
        private const byte VERSION = 1;

        // TArea: script(1) + map(1) + cells[16] of Word(2) = 34 bytes
        private const int TAreaSize = 34;

        // TExploredFile: len(1) + sig(4) + version(1) + count(1) + areas[64]*34 = 7 + 2176 = 2183 bytes
        private const int RecSize = 2183;

        private static readonly byte[] rec = new byte[RecSize];

        static Explored()
        {
            Clear();
        }

        public static void Clear()
        {
            Array.Clear(rec, 0, rec.Length);
            rec[0] = 4;
            rec[1] = (byte)'E';
            rec[2] = (byte)'X';
            rec[3] = (byte)'P';
            rec[4] = (byte)'L';
            rec[5] = VERSION;
            // rec[6] is count, already 0
        }

        private static byte Find(byte script, byte map)
        {
            byte count = rec[6];
            for (byte i = 1; i <= count; i++)
            {
                int areaOffset = 7 + (i - 1) * TAreaSize;
                byte areaScript = rec[areaOffset];
                byte areaMap = rec[areaOffset + 1];
                if (areaScript == script && areaMap == map)
                {
                    return i;
                }
            }
            return 0;
        }

        public static void Mark(sbyte x, sbyte y)
        {
            if (x < 0 || x > 15 || y < 0 || y > 15)
                return;

            byte script = GDVars.eclScriptId;
            byte map = GDVars.zone0.mapId.lo;

            byte i = Find(script, map);
            if (i == 0)
            {
                if (rec[6] >= MAX_AREAS)
                    return;
                rec[6]++;
                i = rec[6];
                int newAreaOffset = 7 + (i - 1) * TAreaSize;
                rec[newAreaOffset] = script;
                rec[newAreaOffset + 1] = map;
            }

            int areaOffset = 7 + (i - 1) * TAreaSize;
            int cellOffset = areaOffset + 2 + (y * 2);
            ushort current = (ushort)(rec[cellOffset] | (rec[cellOffset + 1] << 8));
            ushort bit = (ushort)(1 << x);
            ushort updated = (ushort)(current | bit);
            rec[cellOffset] = (byte)(updated & 0xFF);
            rec[cellOffset + 1] = (byte)((updated >> 8) & 0xFF);
        }

        public static bool Visited(byte script, byte map, sbyte x, sbyte y)
        {
            if (x < 0 || x > 15 || y < 0 || y > 15)
                return false;

            byte i = Find(script, map);
            if (i != 0)
            {
                int areaOffset = 7 + (i - 1) * TAreaSize;
                int cellOffset = areaOffset + 2 + (y * 2);
                ushort current = (ushort)(rec[cellOffset] | (rec[cellOffset + 1] << 8));
                ushort bit = (ushort)(1 << x);
                return (current & bit) != 0;
            }

            return false;
        }

        public static void WriteSlot(string fn)
        {
            FileUtil.WriteRecord(fn, rec, (ushort)RecSize);
        }

        public static void ReadSlot(string fn)
        {
            bool readOk = FileUtil.ReadRecord(fn, rec, (ushort)RecSize);
            if (!readOk)
            {
                Clear();
                return;
            }

            if (rec[0] != 4 ||
                rec[1] != (byte)'E' || rec[2] != (byte)'X' || rec[3] != (byte)'P' || rec[4] != (byte)'L' ||
                rec[5] != VERSION ||
                rec[6] > MAX_AREAS)
            {
                Clear();
            }
        }
    }
}
