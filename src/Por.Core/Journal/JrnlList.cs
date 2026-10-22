using System;

namespace Por.Core
{
    public static partial class JrnlList
    {
        private const int LINES_PER_CHUNK = 9;

        public static byte EntryKind(short index)
        {
            if (index >= 1 && index <= 18)
                return (byte)C_Jrnl.KIND_PROCLAMATION;
            if (index >= 19 && index <= 76)
                return (byte)C_Jrnl.KIND_ENTRY;
            if (index >= 77 && index <= 99)
                return (byte)C_Jrnl.KIND_TALE;
            return (byte)C_Jrnl.KIND_NONE;
        }

        public static short EntryNumber(short index)
        {
            switch (index)
            {
                case 1: return 59;
                case 2: return 64;
                case 3: return 78;
                case 4: return 101;
                case 5: return 109;
                case 6: return 110;
                case 7: return 114;
                case 8: return 120;
                case 9: return 126;
                case 10: return 129;
                case 11: return 134;
                case 12: return 154;
                case 13: return 156;
                case 14: return 170;
                case 15: return 190;
                case 16: return 201;
                case 17: return 204;
                case 18: return 214;
                default:
                    if (index >= 19 && index <= 76)
                        return (short)(index - 18);
                    if (index >= 77 && index <= 99)
                        return (short)(index - 76);
                    return 0;
            }
        }

        public static string EntryLabel(short index)
        {
            string digits = EntryNumber(index).ToString();
            switch (EntryKind(index))
            {
                case (byte)C_Jrnl.KIND_PROCLAMATION:
                    return RomanNumeral(EntryNumber(index));
                case (byte)C_Jrnl.KIND_ENTRY:
                    return "ENTRY " + digits;
                case (byte)C_Jrnl.KIND_TALE:
                    return "TALE " + digits;
                default:
                    return "";
            }
        }

        public static string EntryTitle(short index)
        {
            string digits = EntryNumber(index).ToString();
            switch (EntryKind(index))
            {
                case (byte)C_Jrnl.KIND_PROCLAMATION:
                    return "PROCLAMATION " + RomanNumeral(EntryNumber(index));
                case (byte)C_Jrnl.KIND_ENTRY:
                    return "JOURNAL ENTRY " + digits;
                case (byte)C_Jrnl.KIND_TALE:
                    return "TAVERN TALE " + digits;
                default:
                    return "";
            }
        }

        public static bool IsImageOnly(short index)
        {
            switch (index)
            {
                case 22:
                case 28:
                case 33:
                case 40:
                case 46:
                case 47:
                case 55:
                case 59:
                case 60:
                case 76:
                    return true;
                default:
                    return false;
            }
        }

        public static short LineCount(short index)
        {
            if (index >= 1 && index <= C_Jrnl.PART1_LAST)
                return Journal1.LineCount(index);
            if (index > C_Jrnl.PART1_LAST && index <= C_Jrnl.PART2_LAST)
                return Journal2.LineCount(index);
            if (index > C_Jrnl.PART2_LAST && index <= C_Jrnl.PART3_LAST)
                return Journal3.LineCount(index);
            if (index > C_Jrnl.PART3_LAST && index <= C_Jrnl.JOURNAL_COUNT)
                return Journal4.LineCount(index);
            return 0;
        }

        public static string TextLine(short index, short line)
        {
            if (line < 1 || line > LineCount(index))
                return "";

            string chunk = TextChunk(index, (short)((line - 1) / LINES_PER_CHUNK));
            int skip = (line - 1) % LINES_PER_CHUNK;
            for (int i = 0; i < skip; i++)
            {
                int pos = chunk.IndexOf('\r');
                if (pos >= 0)
                    chunk = chunk.Substring(pos + 1);
            }
            int stop = chunk.IndexOf('\r');
            if (stop >= 0)
                chunk = chunk.Substring(0, stop);
            return chunk;
        }

        private static string RomanNumeral(short value)
        {
            string numeral = "";
            Take(ref value, 1000, "M", ref numeral);
            Take(ref value, 900, "CM", ref numeral);
            Take(ref value, 500, "D", ref numeral);
            Take(ref value, 400, "CD", ref numeral);
            Take(ref value, 100, "C", ref numeral);
            Take(ref value, 90, "XC", ref numeral);
            Take(ref value, 50, "L", ref numeral);
            Take(ref value, 40, "XL", ref numeral);
            Take(ref value, 10, "X", ref numeral);
            Take(ref value, 9, "IX", ref numeral);
            Take(ref value, 5, "V", ref numeral);
            Take(ref value, 4, "IV", ref numeral);
            Take(ref value, 1, "I", ref numeral);
            return numeral;
        }

        private static void Take(ref short value, int amount, string symbol, ref string numeral)
        {
            while (value >= amount)
            {
                value = (short)(value - amount);
                numeral += symbol;
            }
        }

        private static string TextChunk(short index, short chunk)
        {
            if (index >= 1 && index <= C_Jrnl.PART1_LAST)
                return Journal1.TextChunk(index, chunk);
            if (index > C_Jrnl.PART1_LAST && index <= C_Jrnl.PART2_LAST)
                return Journal2.TextChunk(index, chunk);
            if (index > C_Jrnl.PART2_LAST && index <= C_Jrnl.PART3_LAST)
                return Journal3.TextChunk(index, chunk);
            if (index > C_Jrnl.PART3_LAST && index <= C_Jrnl.JOURNAL_COUNT)
                return Journal4.TextChunk(index, chunk);
            return "";
        }
    }
}
