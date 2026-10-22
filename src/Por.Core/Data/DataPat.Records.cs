namespace Por.Core
{
    public static partial class DataPat
    {
        public struct TPatchPart
        {
            public ushort issue;
            public byte family;
            public byte disk;
            public byte id;
            public ushort size;
            public ushort offset;
            public byte origLen;
            public byte newLen;
        }

        public const int PART_COUNT = 59;
        public const int BYTE_COUNT = 1580;

        public static readonly TPatchPart[] parts = new TPatchPart[PART_COUNT + 1];

        static DataPat()
        {
            parts[1] = new TPatchPart { issue = 20, family = C_Data.FAMILY_ITEM, disk = 5, id = 36, size = 63, offset = 61, origLen = 2, newLen = 2 };
            parts[2] = new TPatchPart { issue = 27, family = C_Data.FAMILY_GEO, disk = 3, id = 0, size = 1026, offset = 235, origLen = 1, newLen = 1 };
            parts[3] = new TPatchPart { issue = 27, family = C_Data.FAMILY_GEO, disk = 3, id = 0, size = 1026, offset = 475, origLen = 1, newLen = 1 };
            parts[4] = new TPatchPart { issue = 28, family = C_Data.FAMILY_ECL, disk = 2, id = 20, size = 7679, offset = 6170, origLen = 63, newLen = 63 };
            parts[5] = new TPatchPart { issue = 29, family = C_Data.FAMILY_ECL, disk = 2, id = 20, size = 7679, offset = 7582, origLen = 32, newLen = 32 };
            parts[6] = new TPatchPart { issue = 29, family = C_Data.FAMILY_ECL, disk = 2, id = 20, size = 7679, offset = 1391, origLen = 10, newLen = 10 };
            parts[7] = new TPatchPart { issue = 29, family = C_Data.FAMILY_ECL, disk = 2, id = 20, size = 7679, offset = 1978, origLen = 10, newLen = 10 };
            parts[8] = new TPatchPart { issue = 29, family = C_Data.FAMILY_ECL, disk = 2, id = 20, size = 7679, offset = 4483, origLen = 10, newLen = 10 };
            parts[9] = new TPatchPart { issue = 29, family = C_Data.FAMILY_ECL, disk = 2, id = 20, size = 7679, offset = 4858, origLen = 10, newLen = 10 };
            parts[10] = new TPatchPart { issue = 31, family = C_Data.FAMILY_ECL, disk = 1, id = 18, size = 6550, offset = 22, origLen = 23, newLen = 23 };
            parts[11] = new TPatchPart { issue = 32, family = C_Data.FAMILY_ECL, disk = 1, id = 18, size = 6550, offset = 1264, origLen = 48, newLen = 48 };
            parts[12] = new TPatchPart { issue = 33, family = C_Data.FAMILY_ECL, disk = 1, id = 18, size = 6550, offset = 4520, origLen = 7, newLen = 7 };
            parts[13] = new TPatchPart { issue = 33, family = C_Data.FAMILY_ECL, disk = 1, id = 18, size = 6550, offset = 6550, origLen = 0, newLen = 26 };
            parts[14] = new TPatchPart { issue = 34, family = C_Data.FAMILY_ECL, disk = 4, id = 2, size = 6170, offset = 318, origLen = 4, newLen = 4 };
            parts[15] = new TPatchPart { issue = 34, family = C_Data.FAMILY_ECL, disk = 4, id = 2, size = 6170, offset = 359, origLen = 32, newLen = 32 };
            parts[16] = new TPatchPart { issue = 34, family = C_Data.FAMILY_ECL, disk = 4, id = 2, size = 6170, offset = 493, origLen = 12, newLen = 12 };
            parts[17] = new TPatchPart { issue = 34, family = C_Data.FAMILY_ECL, disk = 4, id = 2, size = 6170, offset = 562, origLen = 12, newLen = 12 };
            parts[18] = new TPatchPart { issue = 34, family = C_Data.FAMILY_ECL, disk = 4, id = 2, size = 6170, offset = 647, origLen = 12, newLen = 12 };
            parts[19] = new TPatchPart { issue = 34, family = C_Data.FAMILY_ECL, disk = 4, id = 2, size = 6170, offset = 728, origLen = 12, newLen = 12 };
            parts[20] = new TPatchPart { issue = 34, family = C_Data.FAMILY_ECL, disk = 4, id = 2, size = 6170, offset = 797, origLen = 12, newLen = 12 };
            parts[21] = new TPatchPart { issue = 34, family = C_Data.FAMILY_ECL, disk = 4, id = 2, size = 6170, offset = 6170, origLen = 0, newLen = 43 };
            parts[22] = new TPatchPart { issue = 35, family = C_Data.FAMILY_ECL, disk = 4, id = 2, size = 6213, offset = 75, origLen = 14, newLen = 14 };
            parts[23] = new TPatchPart { issue = 35, family = C_Data.FAMILY_ECL, disk = 4, id = 2, size = 6213, offset = 6213, origLen = 0, newLen = 65 };
            parts[24] = new TPatchPart { issue = 36, family = C_Data.FAMILY_ITEM, disk = 1, id = 6, size = 189, offset = 61, origLen = 2, newLen = 2 };
            parts[25] = new TPatchPart { issue = 37, family = C_Data.FAMILY_ECL, disk = 4, id = 2, size = 6278, offset = 5491, origLen = 7, newLen = 7 };
            parts[26] = new TPatchPart { issue = 37, family = C_Data.FAMILY_ECL, disk = 4, id = 2, size = 6278, offset = 5504, origLen = 1, newLen = 1 };
            parts[27] = new TPatchPart { issue = 37, family = C_Data.FAMILY_ECL, disk = 4, id = 2, size = 6278, offset = 5644, origLen = 1, newLen = 1 };
            parts[28] = new TPatchPart { issue = 38, family = C_Data.FAMILY_ECL, disk = 3, id = 14, size = 7332, offset = 4539, origLen = 87, newLen = 87 };
            parts[29] = new TPatchPart { issue = 40, family = C_Data.FAMILY_ECL, disk = 7, id = 26, size = 6569, offset = 5397, origLen = 1, newLen = 1 };
            parts[30] = new TPatchPart { issue = 40, family = C_Data.FAMILY_ECL, disk = 7, id = 26, size = 6569, offset = 5681, origLen = 1, newLen = 1 };
            parts[31] = new TPatchPart { issue = 40, family = C_Data.FAMILY_ECL, disk = 7, id = 26, size = 6569, offset = 5697, origLen = 1, newLen = 1 };
            parts[32] = new TPatchPart { issue = 41, family = C_Data.FAMILY_MONITM, disk = 7, id = 43, size = 189, offset = 113, origLen = 1, newLen = 1 };
            parts[33] = new TPatchPart { issue = 42, family = C_Data.FAMILY_ECL, disk = 8, id = 16, size = 5394, offset = 360, origLen = 1, newLen = 1 };
            parts[34] = new TPatchPart { issue = 43, family = C_Data.FAMILY_ECL, disk = 8, id = 16, size = 5394, offset = 177, origLen = 1, newLen = 1 };
            parts[35] = new TPatchPart { issue = 44, family = C_Data.FAMILY_ECL, disk = 6, id = 1, size = 7673, offset = 7115, origLen = 1, newLen = 1 };
            parts[36] = new TPatchPart { issue = 45, family = C_Data.FAMILY_ECL, disk = 6, id = 28, size = 7669, offset = 16, origLen = 2, newLen = 2 };
            parts[37] = new TPatchPart { issue = 45, family = C_Data.FAMILY_ECL, disk = 6, id = 28, size = 7669, offset = 2827, origLen = 2, newLen = 2 };
            parts[38] = new TPatchPart { issue = 45, family = C_Data.FAMILY_ECL, disk = 6, id = 28, size = 7669, offset = 7463, origLen = 110, newLen = 110 };
            parts[39] = new TPatchPart { issue = 46, family = C_Data.FAMILY_GEO, disk = 6, id = 28, size = 1026, offset = 517, origLen = 1, newLen = 1 };
            parts[40] = new TPatchPart { issue = 47, family = C_Data.FAMILY_MONCHA, disk = 6, id = 86, size = 285, offset = 0, origLen = 16, newLen = 16 };
            parts[41] = new TPatchPart { issue = 48, family = C_Data.FAMILY_ECL, disk = 4, id = 10, size = 7672, offset = 2788, origLen = 1, newLen = 1 };
            parts[42] = new TPatchPart { issue = 48, family = C_Data.FAMILY_ECL, disk = 4, id = 10, size = 7672, offset = 5161, origLen = 1, newLen = 1 };
            parts[43] = new TPatchPart { issue = 49, family = C_Data.FAMILY_ECL, disk = 2, id = 9, size = 6367, offset = 4918, origLen = 1, newLen = 1 };
            parts[44] = new TPatchPart { issue = 50, family = C_Data.FAMILY_ECL, disk = 2, id = 9, size = 6367, offset = 5114, origLen = 4, newLen = 4 };
            parts[45] = new TPatchPart { issue = 50, family = C_Data.FAMILY_ECL, disk = 2, id = 9, size = 6367, offset = 6367, origLen = 0, newLen = 10 };
            parts[46] = new TPatchPart { issue = 51, family = C_Data.FAMILY_ECL, disk = 2, id = 9, size = 6377, offset = 786, origLen = 2, newLen = 2 };
            parts[47] = new TPatchPart { issue = 52, family = C_Data.FAMILY_ECL, disk = 2, id = 9, size = 6377, offset = 6161, origLen = 6, newLen = 6 };
            parts[48] = new TPatchPart { issue = 52, family = C_Data.FAMILY_ECL, disk = 2, id = 9, size = 6377, offset = 6377, origLen = 0, newLen = 99 };
            parts[49] = new TPatchPart { issue = 54, family = C_Data.FAMILY_ECL, disk = 5, id = 5, size = 4943, offset = 4336, origLen = 1, newLen = 1 };
            parts[50] = new TPatchPart { issue = 55, family = C_Data.FAMILY_ECL, disk = 5, id = 7, size = 4105, offset = 3876, origLen = 1, newLen = 1 };
            parts[51] = new TPatchPart { issue = 70, family = C_Data.FAMILY_GEO, disk = 1, id = 31, size = 1026, offset = 1014, origLen = 1, newLen = 1 };
            parts[52] = new TPatchPart { issue = 70, family = C_Data.FAMILY_GEO, disk = 1, id = 31, size = 1026, offset = 1021, origLen = 1, newLen = 1 };
            parts[53] = new TPatchPart { issue = 203, family = C_Data.FAMILY_ITEM, disk = 2, id = 3, size = 252, offset = 109, origLen = 6, newLen = 6 };
            parts[54] = new TPatchPart { issue = 203, family = C_Data.FAMILY_MONITM, disk = 2, id = 82, size = 315, offset = 235, origLen = 6, newLen = 6 };
            parts[55] = new TPatchPart { issue = 355, family = C_Data.FAMILY_ECL, disk = 3, id = 11, size = 2171, offset = 488, origLen = 10, newLen = 10 };
            parts[56] = new TPatchPart { issue = 355, family = C_Data.FAMILY_ECL, disk = 3, id = 11, size = 2171, offset = 2171, origLen = 0, newLen = 49 };
            parts[57] = new TPatchPart { issue = 606, family = C_Data.FAMILY_ECL, disk = 3, id = 0, size = 7473, offset = 657, origLen = 2, newLen = 2 };
            parts[58] = new TPatchPart { issue = 606, family = C_Data.FAMILY_ECL, disk = 3, id = 0, size = 7473, offset = 7473, origLen = 0, newLen = 52 };
            parts[59] = new TPatchPart { issue = 670, family = C_Data.FAMILY_ECL, disk = 1, id = 18, size = 6576, offset = 4168, origLen = 12, newLen = 12 };
        }
    }
}
