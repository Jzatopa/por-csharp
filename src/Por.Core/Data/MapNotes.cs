using System;

namespace Por.Core
{
    public static partial class MapNotes
    {
        public const int MAX_NOTES = 128;
        public const int NOTE_LEN = 30;

        private const int NOTE_SIZE = 35;
        private const int RecSize = 7 + (MAX_NOTES * NOTE_SIZE); // 4487

        private static readonly byte[] rec = new byte[RecSize];

        static MapNotes()
        {
            Clear();
        }

        public static void Clear()
        {
            Array.Clear(rec, 0, rec.Length);
            rec[0] = 4;
            rec[1] = (byte)'N';
            rec[2] = (byte)'O';
            rec[3] = (byte)'T';
            rec[4] = (byte)'E';
            rec[5] = 1; // Version
            // rec[6] is count, already 0
        }

        private static byte Find(byte s, byte m, sbyte nx, sbyte ny)
        {
            byte count = rec[6];
            for (byte i = 1; i <= count; i++)
            {
                int offset = 7 + (i - 1) * NOTE_SIZE;
                byte script = rec[offset];
                byte map = rec[offset + 1];
                byte x = rec[offset + 2];
                byte y = rec[offset + 3];

                if (script == s && map == m && x == nx && y == ny)
                {
                    return i;
                }
            }
            return 0;
        }

        public static string Note(byte script, byte map, sbyte x, sbyte y)
        {
            byte i = Find(script, map, x, y);
            if (i == 0)
                return "";

            int offset = 7 + (i - 1) * NOTE_SIZE;
            byte len = rec[offset + 4];
            if (len > NOTE_LEN) len = NOTE_LEN;

            char[] chars = new char[len];
            for (int j = 0; j < len; j++)
            {
                chars[j] = (char)rec[offset + 5 + j];
            }
            return new string(chars);
        }

        public static bool SetNote(byte script, byte map, sbyte x, sbyte y, string text)
        {
            if (text == null) text = "";
            foreach (char c in text)
            {
                if (c > 255)
                    return false;
            }

            byte i = Find(script, map, x, y);

            if (text.Length == 0)
            {
                if (i != 0)
                {
                    int offsetI = 7 + (i - 1) * NOTE_SIZE;
                    int offsetLast = 7 + (rec[6] - 1) * NOTE_SIZE;
                    Array.Copy(rec, offsetLast, rec, offsetI, NOTE_SIZE);
                    rec[6]--;
                }
                return true;
            }

            if (i == 0)
            {
                if (rec[6] >= MAX_NOTES)
                    return false;

                rec[6]++;
                i = rec[6];

                int newOffset = 7 + (i - 1) * NOTE_SIZE;
                rec[newOffset] = script;
                rec[newOffset + 1] = map;
                rec[newOffset + 2] = unchecked((byte)x);
                rec[newOffset + 3] = unchecked((byte)y);
            }

            int offset = 7 + (i - 1) * NOTE_SIZE;
            byte len = (byte)Math.Min(text.Length, NOTE_LEN);
            rec[offset + 4] = len;

            for (int j = 0; j < len; j++)
            {
                rec[offset + 5 + j] = (byte)text[j];
            }

            return true;
        }

        public static void WriteSlot(string fn)
        {
            FileUtil.WriteRecord(fn, rec, (ushort)RecSize);
        }

        public static void ReadSlot(string fn)
        {
            bool readOk = FileUtil.ReadRecord(fn, rec, (ushort)RecSize);
            if (!readOk ||
                rec[0] != 4 ||
                rec[1] != (byte)'N' || rec[2] != (byte)'O' || rec[3] != (byte)'T' || rec[4] != (byte)'E' ||
                rec[5] != 1 ||
                rec[6] > MAX_NOTES)
            {
                Clear();
            }
        }
    }
}
