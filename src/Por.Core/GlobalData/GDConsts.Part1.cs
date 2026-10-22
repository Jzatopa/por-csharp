namespace Por.Core
{
    public static partial class GDConsts
    {
        public static string GAME_VERSION = "unmanaged build version not supplied (BC)";

        public static byte[] cureDiseaseEffects = new byte[7];
        public static string[] templeSpells = new string[11];
        public static byte[,] aiDirOffsets = new byte[7, 6];
        public static byte[,] ringDirTbl = new byte[4, 4];
        public static byte[,] ringStart = new byte[4, 4];
        public static byte[] quadDir = new byte[4];
        public static sbyte[,] startQX = new sbyte[2, 4];
        public static sbyte[,] startQY = new sbyte[2, 4];
        public static Types.TBandRec[,] rowBand = new Types.TBandRec[5, 6];
        public static string[] gnomeFoes = new string[6];
        public static string[] dwarfFoes = new string[12];
        public static string[] giantFoes = new string[6];
        public static string[] rangerFoes = new string[17];
        public static Types.TTurnUndeadTable turnTable = new Types.TTurnUndeadTable();
        public static string menu3D = "Area Cast View Encamp Search Look Jrnl";
        public static string menuWild = "Cast View Encamp Search Look Jrnl";
        public static string campMenu = "Save View Magic Rest Alter Fix Exit";
        public static string magicMenu = "Cast Memorize Scribe Display Rest Exit";
        public static string alterMenu = "Order Drop Char Settings Exit";
        public static string[] orderMenus = new string[2];
        public static byte[] usabilityBit = new byte[8];
        public static byte[] classBit = new byte[17];
        public static string menuKeys = "C D M T V A R L S B G E J";
        public static Types.TMenuItem[] menuItems = new Types.TMenuItem[12];
        public static byte[] hpDice = new byte[8];
        public static byte[] hpSides = new byte[8];
        public static char[] iconHeightSuffix = new char[3];
        public static byte[,] npcPortraitHeads = new byte[2, 9];
        public static byte[,] npcClericBodies = new byte[2, 6];
        public static byte[,] npcFighterBodies = new byte[2, 6];
        public static byte[,] npcMageBodies = new byte[2, 6];
        public static byte[,] npcThiefBodies = new byte[2, 6];
        public static byte[] yarBlueGreenTiles = new byte[11];
        public static byte[] yarGreenTiles = new byte[18];
        public static Types.TColorTab yarRemapFrom = new Types.TColorTab();
        public static Types.TColorTab yarBlueGreenTo = new Types.TColorTab();
        public static Types.TColorTab yarGreenTo = new Types.TColorTab();

        private static readonly bool _part1Initialized = InitializePart1();
        private static bool InitializePart1()
        {
            cureDiseaseEffects[1] = (byte)C_Effect.HELPLESS;
            cureDiseaseEffects[2] = (byte)C_Effect.DISEASED;
            cureDiseaseEffects[3] = (byte)C_Effect.DISEASE_STR;
            cureDiseaseEffects[4] = (byte)C_Effect.DISEASE_HP;
            cureDiseaseEffects[5] = (byte)C_Effect.ROT_NO_HEAL;
            cureDiseaseEffects[6] = (byte)C_Effect.MUMMY_ROT;

            templeSpells[1] = "Cure Blindness";
            templeSpells[2] = "Cure Disease";
            templeSpells[3] = "Cure Light Wounds";
            templeSpells[4] = "Cure Serious Wounds";
            templeSpells[5] = "Cure Critical Wounds";
            templeSpells[6] = "Neutralize Poison";
            templeSpells[7] = "Raise Dead";
            templeSpells[8] = "Remove Curse";
            templeSpells[9] = "Stone to Flesh";
            templeSpells[10] = "Exit";

            aiDirOffsets[1, 1] = 8; aiDirOffsets[1, 2] = 7; aiDirOffsets[1, 3] = 6; aiDirOffsets[1, 4] = 1; aiDirOffsets[1, 5] = 2;
            aiDirOffsets[2, 1] = 8; aiDirOffsets[2, 2] = 1; aiDirOffsets[2, 3] = 2; aiDirOffsets[2, 4] = 7; aiDirOffsets[2, 5] = 6;
            aiDirOffsets[3, 1] = 7; aiDirOffsets[3, 2] = 1; aiDirOffsets[3, 3] = 8; aiDirOffsets[3, 4] = 6; aiDirOffsets[3, 5] = 2;
            aiDirOffsets[4, 1] = 1; aiDirOffsets[4, 2] = 7; aiDirOffsets[4, 3] = 8; aiDirOffsets[4, 4] = 2; aiDirOffsets[4, 5] = 6;
            aiDirOffsets[5, 1] = 8; aiDirOffsets[5, 2] = 7; aiDirOffsets[5, 3] = 6; aiDirOffsets[5, 4] = 5; aiDirOffsets[5, 5] = 4;
            aiDirOffsets[6, 1] = 8; aiDirOffsets[6, 2] = 1; aiDirOffsets[6, 3] = 2; aiDirOffsets[6, 4] = 3; aiDirOffsets[6, 5] = 4;

            ringDirTbl[0, 0] = 8; ringDirTbl[0, 1] = 4; ringDirTbl[0, 2] = 6; ringDirTbl[0, 3] = 2;
            ringDirTbl[1, 0] = 8; ringDirTbl[1, 1] = 6; ringDirTbl[1, 2] = 4; ringDirTbl[1, 3] = 0;
            ringDirTbl[2, 0] = 8; ringDirTbl[2, 1] = 0; ringDirTbl[2, 2] = 6; ringDirTbl[2, 3] = 2;
            ringDirTbl[3, 0] = 8; ringDirTbl[3, 1] = 2; ringDirTbl[3, 2] = 0; ringDirTbl[3, 3] = 4;

            ringStart[0, 0] = 0; ringStart[0, 1] = 0; ringStart[0, 2] = 2; ringStart[0, 3] = 6;
            ringStart[1, 0] = 2; ringStart[1, 1] = 2; ringStart[1, 2] = 0; ringStart[1, 3] = 4;
            ringStart[2, 0] = 4; ringStart[2, 1] = 4; ringStart[2, 2] = 2; ringStart[2, 3] = 6;
            ringStart[3, 0] = 6; ringStart[3, 1] = 6; ringStart[3, 2] = 4; ringStart[3, 3] = 0;

            quadDir[0] = 7; quadDir[1] = 2; quadDir[2] = 3; quadDir[3] = 6;

            startQX[0, 0] = 5; startQX[0, 1] = 4; startQX[0, 2] = 5; startQX[0, 3] = 6;
            startQX[1, 0] = 3; startQX[1, 1] = 8; startQX[1, 2] = 7; startQX[1, 3] = 2;

            startQY[0, 0] = 3; startQY[0, 1] = 2; startQY[0, 2] = 2; startQY[0, 3] = 3;
            startQY[1, 0] = 0; startQY[1, 1] = 2; startQY[1, 2] = 5; startQY[1, 3] = 3;

            rowBand[0, 0] = new Types.TBandRec { lo = 1, hi = 0 };
            rowBand[0, 1] = new Types.TBandRec { lo = 1, hi = 0 };
            rowBand[0, 2] = new Types.TBandRec { lo = 1, hi = 0 };
            rowBand[0, 3] = new Types.TBandRec { lo = 2, hi = 9 };
            rowBand[0, 4] = new Types.TBandRec { lo = 3, hi = 10 };
            rowBand[0, 5] = new Types.TBandRec { lo = 4, hi = 10 };

            rowBand[1, 0] = new Types.TBandRec { lo = 0, hi = 2 };
            rowBand[1, 1] = new Types.TBandRec { lo = 0, hi = 3 };
            rowBand[1, 2] = new Types.TBandRec { lo = 1, hi = 4 };
            rowBand[1, 3] = new Types.TBandRec { lo = 2, hi = 5 };
            rowBand[1, 4] = new Types.TBandRec { lo = 3, hi = 6 };
            rowBand[1, 5] = new Types.TBandRec { lo = 4, hi = 7 };

            rowBand[2, 0] = new Types.TBandRec { lo = 0, hi = 6 };
            rowBand[2, 1] = new Types.TBandRec { lo = 0, hi = 7 };
            rowBand[2, 2] = new Types.TBandRec { lo = 1, hi = 8 };
            rowBand[2, 3] = new Types.TBandRec { lo = 1, hi = 0 };
            rowBand[2, 4] = new Types.TBandRec { lo = 1, hi = 0 };
            rowBand[2, 5] = new Types.TBandRec { lo = 1, hi = 0 };

            rowBand[3, 0] = new Types.TBandRec { lo = 3, hi = 6 };
            rowBand[3, 1] = new Types.TBandRec { lo = 4, hi = 7 };
            rowBand[3, 2] = new Types.TBandRec { lo = 5, hi = 8 };
            rowBand[3, 3] = new Types.TBandRec { lo = 6, hi = 9 };
            rowBand[3, 4] = new Types.TBandRec { lo = 7, hi = 10 };
            rowBand[3, 5] = new Types.TBandRec { lo = 8, hi = 10 };

            rowBand[4, 0] = new Types.TBandRec { lo = 0, hi = 6 };
            rowBand[4, 1] = new Types.TBandRec { lo = 0, hi = 7 };
            rowBand[4, 2] = new Types.TBandRec { lo = 1, hi = 8 };
            rowBand[4, 3] = new Types.TBandRec { lo = 2, hi = 9 };
            rowBand[4, 4] = new Types.TBandRec { lo = 3, hi = 10 };
            rowBand[4, 5] = new Types.TBandRec { lo = 4, hi = 10 };

            gnomeFoes[1] = "KOBOLD";
            gnomeFoes[2] = "KOBOLD LEADER";
            gnomeFoes[3] = "GOBLIN";
            gnomeFoes[4] = "GOBLIN LEADER";
            gnomeFoes[5] = "GOBLIN GUARD";

            dwarfFoes[1] = "ORC";
            dwarfFoes[2] = "ORC LEADER";
            dwarfFoes[3] = "GOBLIN";
            dwarfFoes[4] = "GOBLIN LEADER";
            dwarfFoes[5] = "HOBGOBLIN";
            dwarfFoes[6] = "HOBGOBLIN CHIEF";
            dwarfFoes[7] = "GAGOOL";
            dwarfFoes[8] = "MACE";
            dwarfFoes[9] = "GOBLIN GUARD";
            dwarfFoes[10] = "HOBGOBLIN LDR";
            dwarfFoes[11] = "GRISHNAK";

            giantFoes[1] = "OGRE";
            giantFoes[2] = "OGRE LEADER";
            giantFoes[3] = "TROLL";
            giantFoes[4] = "FIRE GIANT";
            giantFoes[5] = "HILL GIANT";

            rangerFoes[1] = "KOBOLD";
            rangerFoes[2] = "KOBOLD LEADER";
            rangerFoes[3] = "GOBLIN GUARD";
            rangerFoes[4] = "GOBLIN LEADER";
            rangerFoes[5] = "ORC";
            rangerFoes[6] = "ORC LEADER";
            rangerFoes[7] = "HOBGOBLIN";
            rangerFoes[8] = "HOBGOBLIN LDR";
            rangerFoes[9] = "GNOLL";
            rangerFoes[10] = "BUGBEAR";
            rangerFoes[11] = "ETTIN";
            rangerFoes[12] = "OGRE";
            rangerFoes[13] = "OGRE LEADER";
            rangerFoes[14] = "TROLL";
            rangerFoes[15] = "HILL GIANT";
            rangerFoes[16] = "FIRE GIANT";

            turnTable.Data[1, 1] = 10; turnTable.Data[1, 2] = 7; turnTable.Data[1, 3] = 4; turnTable.Data[1, 4] = 1; turnTable.Data[1, 5] = 1; turnTable.Data[1, 6] = 0; turnTable.Data[1, 7] = 0; turnTable.Data[1, 8] = -1; turnTable.Data[1, 9] = -1; turnTable.Data[1, 10] = -1;
            turnTable.Data[2, 1] = 13; turnTable.Data[2, 2] = 10; turnTable.Data[2, 3] = 7; turnTable.Data[2, 4] = 1; turnTable.Data[2, 5] = 1; turnTable.Data[2, 6] = 0; turnTable.Data[2, 7] = 0; turnTable.Data[2, 8] = 0; turnTable.Data[2, 9] = -1; turnTable.Data[2, 10] = -1;
            turnTable.Data[3, 1] = 16; turnTable.Data[3, 2] = 13; turnTable.Data[3, 3] = 10; turnTable.Data[3, 4] = 4; turnTable.Data[3, 5] = 1; turnTable.Data[3, 6] = 1; turnTable.Data[3, 7] = 0; turnTable.Data[3, 8] = 0; turnTable.Data[3, 9] = 0; turnTable.Data[3, 10] = -1;
            turnTable.Data[4, 1] = 19; turnTable.Data[4, 2] = 16; turnTable.Data[4, 3] = 13; turnTable.Data[4, 4] = 7; turnTable.Data[4, 5] = 4; turnTable.Data[4, 6] = 1; turnTable.Data[4, 7] = 1; turnTable.Data[4, 8] = 0; turnTable.Data[4, 9] = 0; turnTable.Data[4, 10] = -1;
            turnTable.Data[5, 1] = 20; turnTable.Data[5, 2] = 19; turnTable.Data[5, 3] = 16; turnTable.Data[5, 4] = 10; turnTable.Data[5, 5] = 7; turnTable.Data[5, 6] = 4; turnTable.Data[5, 7] = 1; turnTable.Data[5, 8] = 1; turnTable.Data[5, 9] = 0; turnTable.Data[5, 10] = 0;
            turnTable.Data[6, 1] = 99; turnTable.Data[6, 2] = 20; turnTable.Data[6, 3] = 19; turnTable.Data[6, 4] = 13; turnTable.Data[6, 5] = 10; turnTable.Data[6, 6] = 7; turnTable.Data[6, 7] = 4; turnTable.Data[6, 8] = 1; turnTable.Data[6, 9] = 1; turnTable.Data[6, 10] = 0;
            turnTable.Data[7, 1] = 99; turnTable.Data[7, 2] = 99; turnTable.Data[7, 3] = 20; turnTable.Data[7, 4] = 16; turnTable.Data[7, 5] = 13; turnTable.Data[7, 6] = 10; turnTable.Data[7, 7] = 7; turnTable.Data[7, 8] = 4; turnTable.Data[7, 9] = 1; turnTable.Data[7, 10] = 0;
            turnTable.Data[8, 1] = 99; turnTable.Data[8, 2] = 99; turnTable.Data[8, 3] = 99; turnTable.Data[8, 4] = 20; turnTable.Data[8, 5] = 16; turnTable.Data[8, 6] = 13; turnTable.Data[8, 7] = 10; turnTable.Data[8, 8] = 7; turnTable.Data[8, 9] = 4; turnTable.Data[8, 10] = 1;
            turnTable.Data[9, 1] = 99; turnTable.Data[9, 2] = 99; turnTable.Data[9, 3] = 99; turnTable.Data[9, 4] = 99; turnTable.Data[9, 5] = 20; turnTable.Data[9, 6] = 16; turnTable.Data[9, 7] = 13; turnTable.Data[9, 8] = 10; turnTable.Data[9, 9] = 7; turnTable.Data[9, 10] = 1;
            turnTable.Data[10, 1] = 99; turnTable.Data[10, 2] = 99; turnTable.Data[10, 3] = 99; turnTable.Data[10, 4] = 99; turnTable.Data[10, 5] = 99; turnTable.Data[10, 6] = 20; turnTable.Data[10, 7] = 16; turnTable.Data[10, 8] = 13; turnTable.Data[10, 9] = 10; turnTable.Data[10, 10] = 4;

            orderMenus[0] = "Select Exit";
            orderMenus[1] = "Place Exit";

            usabilityBit[0] = (byte)(1 << 1);
            usabilityBit[1] = (byte)(1 << 4);
            usabilityBit[2] = (byte)(1 << 3);
            usabilityBit[3] = (byte)(1 << 6);
            usabilityBit[4] = (byte)(1 << 6);
            usabilityBit[5] = (byte)(1 << 0);
            usabilityBit[6] = (byte)(1 << 2);
            usabilityBit[7] = (byte)(1 << 5);

            classBit[0] = 2;
            classBit[1] = 2;
            classBit[2] = 8;
            classBit[3] = 8;
            classBit[4] = 8;
            classBit[5] = 1;
            classBit[6] = 4;
            classBit[7] = 6;
            classBit[8] = 16;
            classBit[9] = 16;
            classBit[10] = 16;
            classBit[11] = 32;
            classBit[12] = 32;
            classBit[13] = 32;
            classBit[14] = 64;
            classBit[15] = 64;
            classBit[16] = 64;

            menuItems[0] = new Types.TMenuItem { name = "Create New Character", enabled = 1 };
            menuItems[1] = new Types.TMenuItem { name = "Drop Character", enabled = 0 };
            menuItems[2] = new Types.TMenuItem { name = "Modify Character", enabled = 0 };
            menuItems[3] = new Types.TMenuItem { name = "Train Character", enabled = 0 };
            menuItems[4] = new Types.TMenuItem { name = "View Character", enabled = 0 };
            menuItems[5] = new Types.TMenuItem { name = "Add Character to Party", enabled = 1 };
            menuItems[6] = new Types.TMenuItem { name = "Remove Character from Party", enabled = 0 };
            menuItems[7] = new Types.TMenuItem { name = "Load Saved Game", enabled = 1 };
            menuItems[8] = new Types.TMenuItem { name = "Save Current Game", enabled = 0 };
            menuItems[9] = new Types.TMenuItem { name = "BEGIN Adventuring", enabled = 0 };
            menuItems[10] = new Types.TMenuItem { name = "Game Settings", enabled = 1 };
            menuItems[11] = new Types.TMenuItem { name = "Exit to DOS", enabled = 1 };

            hpDice[0] = 1;
            hpDice[1] = 1;
            hpDice[2] = 1;
            hpDice[3] = 1;
            hpDice[4] = 2;
            hpDice[5] = 1;
            hpDice[6] = 1;
            hpDice[7] = 2;

            hpSides[0] = 8;
            hpSides[1] = 8;
            hpSides[2] = 10;
            hpSides[3] = 10;
            hpSides[4] = 8;
            hpSides[5] = 4;
            hpSides[6] = 6;
            hpSides[7] = 4;

            iconHeightSuffix[1] = 'S';
            iconHeightSuffix[2] = 'T';

            npcPortraitHeads[0, 1] = 1; npcPortraitHeads[0, 2] = 2; npcPortraitHeads[0, 3] = 3; npcPortraitHeads[0, 4] = 4; npcPortraitHeads[0, 5] = 5; npcPortraitHeads[0, 6] = 8; npcPortraitHeads[0, 7] = 11; npcPortraitHeads[0, 8] = 12;
            npcPortraitHeads[1, 1] = 6; npcPortraitHeads[1, 2] = 7; npcPortraitHeads[1, 3] = 9; npcPortraitHeads[1, 4] = 10; npcPortraitHeads[1, 5] = 13; npcPortraitHeads[1, 6] = 14; npcPortraitHeads[1, 7] = 6; npcPortraitHeads[1, 8] = 7;

            npcClericBodies[0, 1] = 2; npcClericBodies[0, 2] = 4; npcClericBodies[0, 3] = 2; npcClericBodies[0, 4] = 4; npcClericBodies[0, 5] = 2;
            npcClericBodies[1, 1] = 2; npcClericBodies[1, 2] = 4; npcClericBodies[1, 3] = 8; npcClericBodies[1, 4] = 2; npcClericBodies[1, 5] = 4;

            npcFighterBodies[0, 1] = 1; npcFighterBodies[0, 2] = 3; npcFighterBodies[0, 3] = 4; npcFighterBodies[0, 4] = 5; npcFighterBodies[0, 5] = 6;
            npcFighterBodies[1, 1] = 7; npcFighterBodies[1, 2] = 9; npcFighterBodies[1, 3] = 11; npcFighterBodies[1, 4] = 7; npcFighterBodies[1, 5] = 9;

            npcMageBodies[0, 1] = 2; npcMageBodies[0, 2] = 2; npcMageBodies[0, 3] = 2; npcMageBodies[0, 4] = 2; npcMageBodies[0, 5] = 2;
            npcMageBodies[1, 1] = 8; npcMageBodies[1, 2] = 10; npcMageBodies[1, 3] = 12; npcMageBodies[1, 4] = 8; npcMageBodies[1, 5] = 10;

            npcThiefBodies[0, 1] = 6; npcThiefBodies[0, 2] = 6; npcThiefBodies[0, 3] = 6; npcThiefBodies[0, 4] = 6; npcThiefBodies[0, 5] = 6;
            npcThiefBodies[1, 1] = 8; npcThiefBodies[1, 2] = 10; npcThiefBodies[1, 3] = 8; npcThiefBodies[1, 4] = 10; npcThiefBodies[1, 5] = 8;

            yarBlueGreenTiles[0] = 101;
            yarBlueGreenTiles[1] = 102;
            yarBlueGreenTiles[2] = 103;
            yarBlueGreenTiles[3] = 104;
            yarBlueGreenTiles[4] = 105;
            yarBlueGreenTiles[5] = 106;
            yarBlueGreenTiles[6] = 107;
            yarBlueGreenTiles[7] = 113;
            yarBlueGreenTiles[8] = (byte)(128 + 96);
            yarBlueGreenTiles[9] = (byte)(128 + 97);
            yarBlueGreenTiles[10] = (byte)(128 + 98);

            yarGreenTiles[0] = 86;
            yarGreenTiles[1] = 87;
            yarGreenTiles[2] = 88;
            yarGreenTiles[3] = 89;
            yarGreenTiles[4] = 108;
            yarGreenTiles[5] = 109;
            yarGreenTiles[6] = 110;
            yarGreenTiles[7] = 111;
            yarGreenTiles[8] = 112;
            yarGreenTiles[9] = 114;
            yarGreenTiles[10] = 115;
            yarGreenTiles[11] = 116;
            yarGreenTiles[12] = 117;
            yarGreenTiles[13] = 118;
            yarGreenTiles[14] = 119;
            yarGreenTiles[15] = (byte)(128 + 95);
            yarGreenTiles[16] = (byte)(128 + 115);
            yarGreenTiles[17] = (byte)(128 + 117);

            yarRemapFrom.Data[0] = 0;
            yarRemapFrom.Data[1] = 1;
            yarRemapFrom.Data[2] = 2;
            yarRemapFrom.Data[3] = 3;
            yarRemapFrom.Data[4] = 4;
            yarRemapFrom.Data[5] = 5;
            yarRemapFrom.Data[6] = 6;
            yarRemapFrom.Data[7] = 7;
            yarRemapFrom.Data[8] = 8;
            yarRemapFrom.Data[9] = 9;
            yarRemapFrom.Data[10] = 10;
            yarRemapFrom.Data[11] = 11;
            yarRemapFrom.Data[12] = 12;
            yarRemapFrom.Data[13] = 13;
            yarRemapFrom.Data[14] = 14;
            yarRemapFrom.Data[15] = 15;

            yarBlueGreenTo.Data[0] = 1;
            yarBlueGreenTo.Data[1] = 1;
            yarBlueGreenTo.Data[2] = 2;
            yarBlueGreenTo.Data[3] = 3;
            yarBlueGreenTo.Data[4] = 4;
            yarBlueGreenTo.Data[5] = 5;
            yarBlueGreenTo.Data[6] = 6;
            yarBlueGreenTo.Data[7] = 2;
            yarBlueGreenTo.Data[8] = 8;
            yarBlueGreenTo.Data[9] = 9;
            yarBlueGreenTo.Data[10] = 10;
            yarBlueGreenTo.Data[11] = 11;
            yarBlueGreenTo.Data[12] = 12;
            yarBlueGreenTo.Data[13] = 13;
            yarBlueGreenTo.Data[14] = 14;
            yarBlueGreenTo.Data[15] = 15;

            yarGreenTo.Data[0] = 0;
            yarGreenTo.Data[1] = 1;
            yarGreenTo.Data[2] = 2;
            yarGreenTo.Data[3] = 3;
            yarGreenTo.Data[4] = 4;
            yarGreenTo.Data[5] = 5;
            yarGreenTo.Data[6] = 6;
            yarGreenTo.Data[7] = 2;
            yarGreenTo.Data[8] = 8;
            yarGreenTo.Data[9] = 9;
            yarGreenTo.Data[10] = 10;
            yarGreenTo.Data[11] = 11;
            yarGreenTo.Data[12] = 12;
            yarGreenTo.Data[13] = 13;
            yarGreenTo.Data[14] = 14;
            yarGreenTo.Data[15] = 15;
            return true;
        }
    }
}
