namespace Por.Core
{
    public static partial class GDConsts
    {
        public static ushort[] yarViewX = new ushort[9];
        public static ushort[] yarViewY = new ushort[9];
        public static Types.TRandomItemTemplate[] randomItemTemplates = new Types.TRandomItemTemplate[20];
        public static string[] spellLevelNames = new string[10];
        public static byte[] hpZeroEffects = new byte[17];
        public static byte[] aiRoundRemoveEffects = new byte[3];
        public static byte[] skyCeilingColors = new byte[16];
        public static Types.TSpecLoc[] wildernessSpecialLocations = new Types.TSpecLoc[10];
        public static Types.TColorTab animRemapFrom = new Types.TColorTab();
        public static Types.TColorTab animRemapTo = new Types.TColorTab();
        public static Types.TViewSlotGeom viewSlotGeom = new Types.TViewSlotGeom();
        public static Types.TColorTab iconColorIdentity = new Types.TColorTab();
        public static Types.TColorTab iconColorPinkToDarkGray = new Types.TColorTab();
        public static Types.TColorTab iconColorPinkToBlack = new Types.TColorTab();

        private static readonly bool _part2Initialized = InitializePart2();
        private static bool InitializePart2()
        {
            yarViewX[0] = 17;
            yarViewX[1] = 17;
            yarViewX[2] = 17;
            yarViewX[3] = 19;
            yarViewX[4] = 19;
            yarViewX[5] = 19;
            yarViewX[6] = 23;
            yarViewX[7] = 23;
            yarViewX[8] = 23;

            yarViewY[0] = 15;
            yarViewY[1] = 15;
            yarViewY[2] = 15;
            yarViewY[3] = 20;
            yarViewY[4] = 20;
            yarViewY[5] = 20;
            yarViewY[6] = 25;
            yarViewY[7] = 25;
            yarViewY[8] = 25;

            for (int i = 0; i < 20; i++)
            {
                randomItemTemplates[i] = new Types.TRandomItemTemplate();
            }

            randomItemTemplates[1] = new Types.TRandomItemTemplate
            {
                nameIds = new ushort[] { 0, 187, 167, 64 },
                weight = 1,
                value = 800,
                properties = new ushort[] { 0, 3, 3, 0 }
            };

            randomItemTemplates[2] = new Types.TRandomItemTemplate
            {
                nameIds = new ushort[] { 0, 239, 167, 64 },
                weight = 1,
                value = 1100,
                properties = new ushort[] { 0, 1, 82, 0 }
            };

            randomItemTemplates[3] = new Types.TRandomItemTemplate
            {
                nameIds = new ushort[] { 0, 185, 167, 64 },
                weight = 1,
                value = 400,
                properties = new ushort[] { 0, 1, 85, 0 }
            };

            randomItemTemplates[4] = new Types.TRandomItemTemplate
            {
                nameIds = new ushort[] { 0, 173, 167, 64 },
                weight = 1,
                value = 450,
                properties = new ushort[] { 0, 1, 80, 0 }
            };

            randomItemTemplates[5] = new Types.TRandomItemTemplate
            {
                nameIds = new ushort[] { 0, 240, 167, 65 },
                weight = 1,
                value = 2100,
                properties = new ushort[] { 0, 56, 56, 0 }
            };

            randomItemTemplates[6] = new Types.TRandomItemTemplate
            {
                nameIds = new ushort[] { 0, 238, 167, 66 },
                weight = 1,
                value = 5000,
                properties = new ushort[] { 0, 0, 27, 128 }
            };

            randomItemTemplates[7] = new Types.TRandomItemTemplate
            {
                nameIds = new ushort[] { 0, 157, 167, 69 },
                weight = 1,
                value = 6000,
                properties = new ushort[] { 0, 20, 51, 0 }
            };

            randomItemTemplates[8] = new Types.TRandomItemTemplate
            {
                nameIds = new ushort[] { 0, 206, 167, 69 },
                weight = 1,
                value = 11000,
                properties = new ushort[] { 0, 30, 88, 0 }
            };

            randomItemTemplates[9] = new Types.TRandomItemTemplate
            {
                nameIds = new ushort[] { 0, 225, 167, 69 },
                weight = 1,
                value = 5000,
                properties = new ushort[] { 0, 20, 84, 0 }
            };

            randomItemTemplates[10] = new Types.TRandomItemTemplate
            {
                nameIds = new ushort[] { 0, 226, 167, 100 },
                weight = 10,
                value = 15000,
                properties = new ushort[] { 0, 0, 38, 131 }
            };

            randomItemTemplates[11] = new Types.TRandomItemTemplate
            {
                nameIds = new ushort[] { 0, 0, 111, 182 },
                weight = 1,
                value = 10000,
                properties = new ushort[] { 0, 5, 81, 0 }
            };

            randomItemTemplates[12] = new Types.TRandomItemTemplate
            {
                nameIds = new ushort[] { 0, 228, 167, 119 },
                weight = 1,
                value = 16000,
                properties = new ushort[] { 0, 20, 87, 0 }
            };

            randomItemTemplates[13] = new Types.TRandomItemTemplate
            {
                nameIds = new ushort[] { 0, 170, 167, 88 },
                weight = 25,
                value = 17500,
                properties = new ushort[] { 0, 0, 89, 133 }
            };

            randomItemTemplates[14] = new Types.TRandomItemTemplate
            {
                nameIds = new ushort[] { 0, 157, 167, 21 },
                weight = 20,
                value = 3000,
                properties = new ushort[] { 0, 1, 83, 0 }
            };

            randomItemTemplates[15] = new Types.TRandomItemTemplate
            {
                nameIds = new ushort[] { 0, 242, 167, 69 },
                weight = 1,
                value = 5000,
                properties = new ushort[] { 0, 20, 47, 0 }
            };

            randomItemTemplates[16] = new Types.TRandomItemTemplate
            {
                nameIds = new ushort[] { 0, 205, 167, 66 },
                weight = 1,
                value = 5000,
                properties = new ushort[] { 0, 0, 61, 129 }
            };

            randomItemTemplates[17] = new Types.TRandomItemTemplate
            {
                nameIds = new ushort[] { 0, 227, 167, 66 },
                weight = 1,
                value = 7500,
                properties = new ushort[] { 0, 0, 56, 139 }
            };

            randomItemTemplates[18] = new Types.TRandomItemTemplate
            {
                nameIds = new ushort[] { 0, 186, 167, 73 },
                weight = 150,
                value = 25000,
                properties = new ushort[] { 0, 0, 0, 0 }
            };

            randomItemTemplates[19] = new Types.TRandomItemTemplate
            {
                nameIds = new ushort[] { 0, 229, 167, 88 },
                weight = 25,
                value = 6000,
                properties = new ushort[] { 0, 0, 72, 134 }
            };

            spellLevelNames[0] = "";
            spellLevelNames[1] = "1st Level";
            spellLevelNames[2] = "2nd Level";
            spellLevelNames[3] = "3rd Level";
            spellLevelNames[4] = "4th Level";
            spellLevelNames[5] = "5th Level";
            spellLevelNames[6] = "6th Level";
            spellLevelNames[7] = "7th Level";
            spellLevelNames[8] = "8th Level";
            spellLevelNames[9] = "9th Level";

            hpZeroEffects[0] = 0;
            hpZeroEffects[1] = C_Effect.TRAIN_MANUAL;
            hpZeroEffects[2] = C_Effect.CHARM;
            hpZeroEffects[3] = C_Effect.STINK_COUGH;
            hpZeroEffects[4] = C_Effect.HELPLESS;
            hpZeroEffects[5] = C_Effect.ANIMATE_DEAD;
            hpZeroEffects[6] = C_Effect.SNAKE_CHARM;
            hpZeroEffects[7] = C_Effect.PARALYSIS;
            hpZeroEffects[8] = C_Effect.SLEEP;
            hpZeroEffects[9] = C_Effect.DRAGON_AURA;
            hpZeroEffects[10] = C_Effect.IMMOBILE;
            hpZeroEffects[11] = C_Effect.TROLL_REGEN;
            hpZeroEffects[12] = C_Effect.FIGHT_ON;
            hpZeroEffects[13] = C_Effect.REGEN3;
            hpZeroEffects[14] = C_Effect.SPIRIT_HAMMER_GONE;
            hpZeroEffects[15] = C_Effect.BITE_HOLD;
            hpZeroEffects[16] = C_Effect.BLOOD_DRAIN;

            aiRoundRemoveEffects[0] = 0;
            aiRoundRemoveEffects[1] = C_Effect.BITE_HOLD;
            aiRoundRemoveEffects[2] = C_Effect.BLOOD_DRAIN;

            skyCeilingColors[0] = 0;
            skyCeilingColors[1] = 15;
            skyCeilingColors[2] = 12;
            skyCeilingColors[3] = 11;
            skyCeilingColors[4] = 13;
            skyCeilingColors[5] = 10;
            skyCeilingColors[6] = 9;
            skyCeilingColors[7] = 14;
            skyCeilingColors[8] = 0;
            skyCeilingColors[9] = 15;
            skyCeilingColors[10] = 12;
            skyCeilingColors[11] = 11;
            skyCeilingColors[12] = 13;
            skyCeilingColors[13] = 10;
            skyCeilingColors[14] = 9;
            skyCeilingColors[15] = 14;

            for (int i = 0; i < 10; i++)
            {
                wildernessSpecialLocations[i] = new Types.TSpecLoc();
            }

            wildernessSpecialLocations[1] = new Types.TSpecLoc { x = 10, y = 9, icon = 240 };
            wildernessSpecialLocations[2] = new Types.TSpecLoc { x = 3, y = 32, icon = 2 };
            wildernessSpecialLocations[3] = new Types.TSpecLoc { x = 12, y = 31, icon = 2 };
            wildernessSpecialLocations[4] = new Types.TSpecLoc { x = 25, y = 11, icon = 1 };
            wildernessSpecialLocations[5] = new Types.TSpecLoc { x = 32, y = 15, icon = 165 };
            wildernessSpecialLocations[6] = new Types.TSpecLoc { x = 33, y = 23, icon = 157 };
            wildernessSpecialLocations[7] = new Types.TSpecLoc { x = 37, y = 8, icon = 147 };
            wildernessSpecialLocations[8] = new Types.TSpecLoc { x = 19, y = 16, icon = 247 };
            wildernessSpecialLocations[9] = new Types.TSpecLoc { x = 25, y = 27, icon = 256 };

            animRemapFrom.Data[0] = 0;
            animRemapFrom.Data[1] = 1;
            animRemapFrom.Data[2] = 2;
            animRemapFrom.Data[3] = 3;
            animRemapFrom.Data[4] = 4;
            animRemapFrom.Data[5] = 5;
            animRemapFrom.Data[6] = 6;
            animRemapFrom.Data[7] = 7;
            animRemapFrom.Data[8] = 8;
            animRemapFrom.Data[9] = 9;
            animRemapFrom.Data[10] = 10;
            animRemapFrom.Data[11] = 11;
            animRemapFrom.Data[12] = 12;
            animRemapFrom.Data[13] = 13;
            animRemapFrom.Data[14] = 14;
            animRemapFrom.Data[15] = 15;

            animRemapTo.Data[0] = 0;
            animRemapTo.Data[1] = 1;
            animRemapTo.Data[2] = 2;
            animRemapTo.Data[3] = 3;
            animRemapTo.Data[4] = 4;
            animRemapTo.Data[5] = 5;
            animRemapTo.Data[6] = 6;
            animRemapTo.Data[7] = 7;
            animRemapTo.Data[8] = 8;
            animRemapTo.Data[9] = 9;
            animRemapTo.Data[10] = 10;
            animRemapTo.Data[11] = 11;
            animRemapTo.Data[12] = 12;
            animRemapTo.Data[13] = 0;
            animRemapTo.Data[14] = 14;
            animRemapTo.Data[15] = 15;

            viewSlotGeom.start[0] = 0;
            viewSlotGeom.start[1] = 2;
            viewSlotGeom.start[2] = 6;
            viewSlotGeom.start[3] = 10;
            viewSlotGeom.start[4] = 22;
            viewSlotGeom.start[5] = 38;
            viewSlotGeom.start[6] = 54;
            viewSlotGeom.start[7] = 110;
            viewSlotGeom.start[8] = 132;
            viewSlotGeom.start[9] = 154;

            viewSlotGeom.cols[0] = 1;
            viewSlotGeom.cols[1] = 1;
            viewSlotGeom.cols[2] = 1;
            viewSlotGeom.cols[3] = 3;
            viewSlotGeom.cols[4] = 2;
            viewSlotGeom.cols[5] = 2;
            viewSlotGeom.cols[6] = 7;
            viewSlotGeom.cols[7] = 2;
            viewSlotGeom.cols[8] = 2;
            viewSlotGeom.cols[9] = 1;

            viewSlotGeom.rows[0] = 2;
            viewSlotGeom.rows[1] = 4;
            viewSlotGeom.rows[2] = 4;
            viewSlotGeom.rows[3] = 4;
            viewSlotGeom.rows[4] = 8;
            viewSlotGeom.rows[5] = 8;
            viewSlotGeom.rows[6] = 8;
            viewSlotGeom.rows[7] = 11;
            viewSlotGeom.rows[8] = 11;
            viewSlotGeom.rows[9] = 2;

            viewSlotGeom.xCol[0] = 5;
            viewSlotGeom.xCol[1] = 4;
            viewSlotGeom.xCol[2] = 6;
            viewSlotGeom.xCol[3] = 4;
            viewSlotGeom.xCol[4] = 2;
            viewSlotGeom.xCol[5] = 7;
            viewSlotGeom.xCol[6] = 2;
            viewSlotGeom.xCol[7] = 0;
            viewSlotGeom.xCol[8] = 9;
            viewSlotGeom.xCol[9] = 5;

            viewSlotGeom.yRow[0] = 4;
            viewSlotGeom.yRow[1] = 3;
            viewSlotGeom.yRow[2] = 3;
            viewSlotGeom.yRow[3] = 3;
            viewSlotGeom.yRow[4] = 1;
            viewSlotGeom.yRow[5] = 1;
            viewSlotGeom.yRow[6] = 1;
            viewSlotGeom.yRow[7] = 0;
            viewSlotGeom.yRow[8] = 0;
            viewSlotGeom.yRow[9] = 4;

            iconColorIdentity.Data[0] = 0;
            iconColorIdentity.Data[1] = 1;
            iconColorIdentity.Data[2] = 2;
            iconColorIdentity.Data[3] = 3;
            iconColorIdentity.Data[4] = 4;
            iconColorIdentity.Data[5] = 5;
            iconColorIdentity.Data[6] = 6;
            iconColorIdentity.Data[7] = 7;
            iconColorIdentity.Data[8] = 8;
            iconColorIdentity.Data[9] = 9;
            iconColorIdentity.Data[10] = 10;
            iconColorIdentity.Data[11] = 11;
            iconColorIdentity.Data[12] = 12;
            iconColorIdentity.Data[13] = 13;
            iconColorIdentity.Data[14] = 14;
            iconColorIdentity.Data[15] = 15;

            iconColorPinkToDarkGray.Data[0] = 0;
            iconColorPinkToDarkGray.Data[1] = 1;
            iconColorPinkToDarkGray.Data[2] = 2;
            iconColorPinkToDarkGray.Data[3] = 3;
            iconColorPinkToDarkGray.Data[4] = 4;
            iconColorPinkToDarkGray.Data[5] = 5;
            iconColorPinkToDarkGray.Data[6] = 6;
            iconColorPinkToDarkGray.Data[7] = 7;
            iconColorPinkToDarkGray.Data[8] = 8;
            iconColorPinkToDarkGray.Data[9] = 9;
            iconColorPinkToDarkGray.Data[10] = 10;
            iconColorPinkToDarkGray.Data[11] = 11;
            iconColorPinkToDarkGray.Data[12] = 12;
            iconColorPinkToDarkGray.Data[13] = 8;
            iconColorPinkToDarkGray.Data[14] = 14;
            iconColorPinkToDarkGray.Data[15] = 15;

            iconColorPinkToBlack.Data[0] = 0;
            iconColorPinkToBlack.Data[1] = 1;
            iconColorPinkToBlack.Data[2] = 2;
            iconColorPinkToBlack.Data[3] = 3;
            iconColorPinkToBlack.Data[4] = 4;
            iconColorPinkToBlack.Data[5] = 5;
            iconColorPinkToBlack.Data[6] = 6;
            iconColorPinkToBlack.Data[7] = 7;
            iconColorPinkToBlack.Data[8] = 8;
            iconColorPinkToBlack.Data[9] = 9;
            iconColorPinkToBlack.Data[10] = 10;
            iconColorPinkToBlack.Data[11] = 11;
            iconColorPinkToBlack.Data[12] = 12;
            iconColorPinkToBlack.Data[13] = 0;
            iconColorPinkToBlack.Data[14] = 14;
            iconColorPinkToBlack.Data[15] = 15;
            return true;
        }
    }
}
