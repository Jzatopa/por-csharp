namespace Por.Core
{
    public static partial class GDConsts
    {
        public static string[] dirNames = new string[8]
        {
            "N", "NE", "E", "SE", "S", "SW", "W", "NW"
        };

        public static byte[,] dirByDelta = new byte[3, 3]
        {
            { 7, 0, 1 },
            { 6, 8, 2 },
            { 5, 4, 3 }
        };

        public static byte[] egaPlaneMask = new byte[4]
        {
            0x08, 0x04, 0x02, 0x01
        };

        public static ushort[] cgaColorPat = new ushort[4]
        {
            0x0000, 0x5555, 0xAAAA, 0xFFFF
        };

        public static byte[] attrTable = new byte[16]
        {
            0, 0, 0, 1, 2, 2, 2, 3,
            3, 1, 1, 1, 2, 2, 3, 3
        };

        public static ushort[] cgaPixelMask = new ushort[8]
        {
            0xC000, 0x3000, 0x0C00, 0x0300, 0x00C0, 0x0030, 0x000C, 0x0003
        };

        public static byte[] egaPlaneBit = new byte[4]
        {
            0x01, 0x02, 0x04, 0x08
        };

        public static byte[,] cgaPixPos = new byte[4, 4]
        {
            { 0x00, 0x00, 0x00, 0x00 },
            { 0x40, 0x10, 0x04, 0x01 },
            { 0x80, 0x20, 0x08, 0x02 },
            { 0xC0, 0x30, 0x0C, 0x03 }
        };

        public static Types.TColorMap dsIconColorMap = new Types.TColorMap(new byte[8]
        {
            0x00, 0x11, 0x22, 0x12, 0x00, 0x21, 0x30, 0x33
        });

        public class SoundIds
        {
            public ushort[] commands;
            public ushort damage;
            public ushort[] combat;
        }

        public static SoundIds soundIds = new SoundIds
        {
            commands = new ushort[4] { 255, 0, 1, 2 },
            damage = 3,
            combat = new ushort[11] { 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 }
        };

        public static string[] insertDiskMsg = new string[9]
        {
            null,
            "Insert disk 1 in drive ",
            "Insert disk 2 in drive ",
            "Insert disk 3 in drive ",
            "Insert disk 4 in drive ",
            "Insert disk 5 in drive ",
            "Insert disk 6 in drive ",
            "Insert disk 7 in drive ",
            "Insert disk 8 in drive "
        };

        public static ushort[] symSetFirst = new ushort[5]
        {
            1, 46, 116, 186, 256
        };

        public static ushort[] symSetLast = new ushort[5]
        {
            45, 115, 185, 255, 286
        };

        public static sbyte[] dirDX = new sbyte[9]
        {
            0, 1, 1, 1, 0, -1, -1, -1, 0
        };

        public static sbyte[] dirDY = new sbyte[9]
        {
            -1, -1, 0, 1, 1, 1, 0, -1, 0
        };

        public static Types.TTerrainRec[] terrainInfo = new Types.TTerrainRec[66]
        {
            null,
            new Types.TTerrainRec { cost = 255, clearance = 0, blocking = 2, tileId = 0 },
            new Types.TTerrainRec { cost = 255, clearance = 0, blocking = 2, tileId = 1 },
            new Types.TTerrainRec { cost = 255, clearance = 0, blocking = 2, tileId = 2 },
            new Types.TTerrainRec { cost = 255, clearance = 0, blocking = 2, tileId = 3 },
            new Types.TTerrainRec { cost = 1, clearance = 0, blocking = 0, tileId = 4 },
            new Types.TTerrainRec { cost = 255, clearance = 0, blocking = 2, tileId = 5 },
            new Types.TTerrainRec { cost = 255, clearance = 0, blocking = 2, tileId = 6 },
            new Types.TTerrainRec { cost = 255, clearance = 0, blocking = 2, tileId = 7 },
            new Types.TTerrainRec { cost = 1, clearance = 0, blocking = 0, tileId = 8 },
            new Types.TTerrainRec { cost = 255, clearance = 0, blocking = 2, tileId = 9 },
            new Types.TTerrainRec { cost = 1, clearance = 0, blocking = 0, tileId = 10 },
            new Types.TTerrainRec { cost = 255, clearance = 0, blocking = 2, tileId = 11 },
            new Types.TTerrainRec { cost = 1, clearance = 0, blocking = 0, tileId = 12 },
            new Types.TTerrainRec { cost = 255, clearance = 0, blocking = 2, tileId = 13 },
            new Types.TTerrainRec { cost = 1, clearance = 0, blocking = 0, tileId = 14 },
            new Types.TTerrainRec { cost = 255, clearance = 0, blocking = 2, tileId = 15 },
            new Types.TTerrainRec { cost = 1, clearance = 0, blocking = 0, tileId = 16 },
            new Types.TTerrainRec { cost = 255, clearance = 0, blocking = 2, tileId = 17 },
            new Types.TTerrainRec { cost = 255, clearance = 0, blocking = 2, tileId = 18 },
            new Types.TTerrainRec { cost = 255, clearance = 0, blocking = 2, tileId = 19 },
            new Types.TTerrainRec { cost = 255, clearance = 0, blocking = 2, tileId = 20 },
            new Types.TTerrainRec { cost = 255, clearance = 0, blocking = 2, tileId = 21 },
            new Types.TTerrainRec { cost = 1, clearance = 0, blocking = 0, tileId = 22 },
            new Types.TTerrainRec { cost = 1, clearance = 0, blocking = 0, tileId = 23 },
            new Types.TTerrainRec { cost = 255, clearance = 0, blocking = 2, tileId = 24 },
            new Types.TTerrainRec { cost = 1, clearance = 0, blocking = 0, tileId = 34 },
            new Types.TTerrainRec { cost = 1, clearance = 0, blocking = 0, tileId = 35 },
            new Types.TTerrainRec { cost = 1, clearance = 0, blocking = 0, tileId = 36 },
            new Types.TTerrainRec { cost = 1, clearance = 0, blocking = 0, tileId = 37 },
            new Types.TTerrainRec { cost = 1, clearance = 0, blocking = 0, tileId = 38 },
            new Types.TTerrainRec { cost = 1, clearance = 0, blocking = 0, tileId = 39 },
            new Types.TTerrainRec { cost = 255, clearance = 0, blocking = 2, tileId = 0 },
            new Types.TTerrainRec { cost = 255, clearance = 0, blocking = 2, tileId = 1 },
            new Types.TTerrainRec { cost = 255, clearance = 0, blocking = 2, tileId = 2 },
            new Types.TTerrainRec { cost = 255, clearance = 0, blocking = 2, tileId = 3 },
            new Types.TTerrainRec { cost = 1, clearance = 0, blocking = 0, tileId = 4 },
            new Types.TTerrainRec { cost = 1, clearance = 0, blocking = 0, tileId = 5 },
            new Types.TTerrainRec { cost = 1, clearance = 0, blocking = 0, tileId = 6 },
            new Types.TTerrainRec { cost = 1, clearance = 0, blocking = 0, tileId = 7 },
            new Types.TTerrainRec { cost = 255, clearance = 0, blocking = 0, tileId = 8 },
            new Types.TTerrainRec { cost = 255, clearance = 0, blocking = 0, tileId = 9 },
            new Types.TTerrainRec { cost = 1, clearance = 0, blocking = 0, tileId = 10 },
            new Types.TTerrainRec { cost = 1, clearance = 0, blocking = 0, tileId = 11 },
            new Types.TTerrainRec { cost = 1, clearance = 0, blocking = 0, tileId = 12 },
            new Types.TTerrainRec { cost = 1, clearance = 0, blocking = 0, tileId = 13 },
            new Types.TTerrainRec { cost = 1, clearance = 0, blocking = 0, tileId = 14 },
            new Types.TTerrainRec { cost = 1, clearance = 0, blocking = 0, tileId = 15 },
            new Types.TTerrainRec { cost = 1, clearance = 0, blocking = 0, tileId = 16 },
            new Types.TTerrainRec { cost = 1, clearance = 0, blocking = 0, tileId = 17 },
            new Types.TTerrainRec { cost = 255, clearance = 0, blocking = 0, tileId = 18 },
            new Types.TTerrainRec { cost = 255, clearance = 0, blocking = 0, tileId = 19 },
            new Types.TTerrainRec { cost = 1, clearance = 0, blocking = 0, tileId = 20 },
            new Types.TTerrainRec { cost = 1, clearance = 0, blocking = 0, tileId = 21 },
            new Types.TTerrainRec { cost = 1, clearance = 0, blocking = 0, tileId = 22 },
            new Types.TTerrainRec { cost = 255, clearance = 0, blocking = 0, tileId = 23 },
            new Types.TTerrainRec { cost = 255, clearance = 0, blocking = 0, tileId = 24 },
            new Types.TTerrainRec { cost = 1, clearance = 0, blocking = 0, tileId = 25 },
            new Types.TTerrainRec { cost = 1, clearance = 0, blocking = 0, tileId = 26 },
            new Types.TTerrainRec { cost = 1, clearance = 0, blocking = 0, tileId = 27 },
            new Types.TTerrainRec { cost = 255, clearance = 0, blocking = 0, tileId = 28 },
            new Types.TTerrainRec { cost = 255, clearance = 0, blocking = 2, tileId = 29 },
            new Types.TTerrainRec { cost = 255, clearance = 0, blocking = 2, tileId = 30 },
            new Types.TTerrainRec { cost = 255, clearance = 0, blocking = 2, tileId = 31 },
            new Types.TTerrainRec { cost = 1, clearance = 0, blocking = 0, tileId = 32 },
            new Types.TTerrainRec { cost = 255, clearance = 0, blocking = 2, tileId = 33 }
        };

        public static Types.TIconCoordinateDelta[,] iconDelta = new Types.TIconCoordinateDelta[5, 4]
        {
            { null, null, null, null },
            { new Types.TIconCoordinateDelta { dx = 0, dy = 0 }, new Types.TIconCoordinateDelta { dx = -1, dy = -1 }, new Types.TIconCoordinateDelta { dx = -1, dy = -1 }, new Types.TIconCoordinateDelta { dx = -1, dy = -1 } },
            { new Types.TIconCoordinateDelta { dx = 0, dy = 0 }, new Types.TIconCoordinateDelta { dx = 0, dy = 1 }, new Types.TIconCoordinateDelta { dx = -1, dy = -1 }, new Types.TIconCoordinateDelta { dx = -1, dy = -1 } },
            { new Types.TIconCoordinateDelta { dx = 0, dy = 0 }, new Types.TIconCoordinateDelta { dx = 1, dy = 0 }, new Types.TIconCoordinateDelta { dx = -1, dy = -1 }, new Types.TIconCoordinateDelta { dx = -1, dy = -1 } },
            { new Types.TIconCoordinateDelta { dx = 0, dy = 0 }, new Types.TIconCoordinateDelta { dx = 1, dy = 0 }, new Types.TIconCoordinateDelta { dx = 0, dy = 1 }, new Types.TIconCoordinateDelta { dx = 1, dy = 1 } }
        };

        public static int HELPLESS_FX_COUNT = 4;

        public static byte[] helplessFx = new byte[5]
        {
            0,
            C_Effect.SNAKE_CHARM,
            C_Effect.PARALYSIS,
            C_Effect.SLEEP,
            C_Effect.HELPLESS
        };

        public static byte[] headTbl = new byte[42]
        {
            0,
            0, 8, 9, 13, 16, 18, 22,
            34, 45, 51, 53, 57, 67, 68,
            24, 32, 42, 44, 55, 63, 71,
            1, 3, 5, 6, 10, 11, 12,
            14, 25, 48, 52, 54, 56, 59,
            61, 62, 69, 70, 72, 74
        };

        public static byte[] bodyTbl = new byte[22]
        {
            0,
            1, 2, 3, 4, 7, 8, 18,
            24, 26, 33, 35, 37, 9, 31,
            32, 34, 5, 6, 13, 25, 30
        };

        public static int DISK3_HEADS = 21;

        public static int DISK3_BODIES = 16;

        public static byte[] keypadScan = new byte[9]
        {
            79, 80, 81, 75, 32, 77, 71, 72, 73
        };

        public static byte[] cloudDir = new byte[5]
        {
            0,
            8, 2, 3, 4
        };

        public static string[] spellNames = new string[57]
        {
            null,
            "Bless",
            "Curse",
            "Cure Light Wounds",
            "Cause Light Wounds",
            "Detect Magic",
            "Protection From Evil",
            "Protection from Good",
            "Resist Cold",
            "Burning Hands",
            "Charm Person",
            "Detect Magic",
            "Enlarge",
            "Reduce",
            "Friends",
            "Magic Missile",
            "Protection From Evil",
            "Protection From Good",
            "Read Magic",
            "Shield",
            "Shocking Grasp",
            "Sleep",
            "Find Traps",
            "Hold Person",
            "Resist Fire",
            "Silence, 15' Radius",
            "Slow Poison",
            "Snake Charm",
            "Spiritual Hammer",
            "Detect Invisibility",
            "Invisibility",
            "Knock",
            "Mirror Image",
            "Ray of Enfeeblement",
            "Stinking Cloud",
            "Strength",
            "Animate Dead",
            "Cure Blindness",
            "Cause Blindness",
            "Cure Disease",
            "Cause Disease",
            "Dispel Magic",
            "Prayer",
            "Remove Curse",
            "Bestow Curse",
            "Blink",
            "Dispel Magic",
            "Fireball",
            "Haste",
            "Hold Person",
            "Invisibility, 10' Radius",
            "Lightning Bolt",
            "Protection From Evil, 10' Radius",
            "Protection From Good, 10' Radius",
            "Protection From Normal Missiles",
            "Slow",
            "Restoration"
        };

        public static Types.TSpellRec[] spellTable = new Types.TSpellRec[68]
        {
            null,
            new Types.TSpellRec { castClass = 0, level = 1, rangeFixed = 6, rangeVar = 0, durFixed = 6, durVar = 0, area = 10, target = 4, saveResult = 0, saveType = 4, effect = 1, whenCast = 2, castTime = 10, aiPriority = 2, offensive = 0, aiRadius = 0 },
            new Types.TSpellRec { castClass = 0, level = 1, rangeFixed = 6, rangeVar = 0, durFixed = 6, durVar = 0, area = 10, target = 0, saveResult = 0, saveType = 4, effect = 2, whenCast = 1, castTime = 10, aiPriority = 3, offensive = 1, aiRadius = 0 },
            new Types.TSpellRec { castClass = 0, level = 1, rangeFixed = 0, rangeVar = 0, durFixed = 0, durVar = 0, area = 4, target = 2, saveResult = 0, saveType = 4, effect = 0, whenCast = 2, castTime = 5, aiPriority = 1, offensive = 0, aiRadius = 0 },
            new Types.TSpellRec { castClass = 0, level = 1, rangeFixed = 255, rangeVar = 0, durFixed = 0, durVar = 0, area = 4, target = 0, saveResult = 0, saveType = 4, effect = 0, whenCast = 1, castTime = 5, aiPriority = 2, offensive = 1, aiRadius = 0 },
            new Types.TSpellRec { castClass = 0, level = 1, rangeFixed = 3, rangeVar = 0, durFixed = 10, durVar = 0, area = 0, target = 1, saveResult = 0, saveType = 4, effect = 5, whenCast = 2, castTime = 1, aiPriority = 0, offensive = 0, aiRadius = 0 },
            new Types.TSpellRec { castClass = 0, level = 1, rangeFixed = 0, rangeVar = 0, durFixed = 0, durVar = 3, area = 4, target = 2, saveResult = 0, saveType = 4, effect = 8, whenCast = 2, castTime = 4, aiPriority = 1, offensive = 0, aiRadius = 0 },
            new Types.TSpellRec { castClass = 0, level = 1, rangeFixed = 0, rangeVar = 0, durFixed = 0, durVar = 3, area = 4, target = 2, saveResult = 0, saveType = 4, effect = 9, whenCast = 2, castTime = 4, aiPriority = 2, offensive = 0, aiRadius = 0 },
            new Types.TSpellRec { castClass = 0, level = 1, rangeFixed = 0, rangeVar = 0, durFixed = 0, durVar = 10, area = 4, target = 2, saveResult = 0, saveType = 4, effect = 10, whenCast = 2, castTime = 10, aiPriority = 0, offensive = 0, aiRadius = 0 },
            new Types.TSpellRec { castClass = 1, level = 1, rangeFixed = 0, rangeVar = 0, durFixed = 0, durVar = 0, area = 4, target = 0, saveResult = 0, saveType = 4, effect = 0, whenCast = 1, castTime = 1, aiPriority = 4, offensive = 1, aiRadius = 0 },
            new Types.TSpellRec { castClass = 1, level = 1, rangeFixed = 12, rangeVar = 0, durFixed = 0, durVar = 0, area = 4, target = 0, saveResult = 1, saveType = 4, effect = 11, whenCast = 1, castTime = 1, aiPriority = 6, offensive = 1, aiRadius = 0 },
            new Types.TSpellRec { castClass = 1, level = 1, rangeFixed = 0, rangeVar = 0, durFixed = 0, durVar = 2, area = 0, target = 1, saveResult = 0, saveType = 4, effect = 5, whenCast = 2, castTime = 1, aiPriority = 0, offensive = 0, aiRadius = 0 },
            new Types.TSpellRec { castClass = 1, level = 1, rangeFixed = 0, rangeVar = 2, durFixed = 0, durVar = 10, area = 4, target = 2, saveResult = 0, saveType = 4, effect = 12, whenCast = 2, castTime = 1, aiPriority = 0, offensive = 0, aiRadius = 0 },
            new Types.TSpellRec { castClass = 1, level = 1, rangeFixed = 0, rangeVar = 2, durFixed = 0, durVar = 10, area = 4, target = 2, saveResult = 1, saveType = 4, effect = 13, whenCast = 2, castTime = 1, aiPriority = 0, offensive = 1, aiRadius = 0 },
            new Types.TSpellRec { castClass = 1, level = 1, rangeFixed = 0, rangeVar = 0, durFixed = 0, durVar = 1, area = 0, target = 1, saveResult = 0, saveType = 4, effect = 14, whenCast = 0, castTime = 1, aiPriority = 0, offensive = 0, aiRadius = 0 },
            new Types.TSpellRec { castClass = 1, level = 1, rangeFixed = 6, rangeVar = 4, durFixed = 0, durVar = 0, area = 4, target = 0, saveResult = 0, saveType = 4, effect = 0, whenCast = 1, castTime = 1, aiPriority = 6, offensive = 1, aiRadius = 0 },
            new Types.TSpellRec { castClass = 1, level = 1, rangeFixed = 0, rangeVar = 0, durFixed = 0, durVar = 2, area = 4, target = 2, saveResult = 0, saveType = 4, effect = 8, whenCast = 2, castTime = 1, aiPriority = 1, offensive = 0, aiRadius = 0 },
            new Types.TSpellRec { castClass = 1, level = 1, rangeFixed = 0, rangeVar = 0, durFixed = 0, durVar = 2, area = 4, target = 2, saveResult = 0, saveType = 4, effect = 9, whenCast = 2, castTime = 1, aiPriority = 2, offensive = 0, aiRadius = 0 },
            new Types.TSpellRec { castClass = 1, level = 1, rangeFixed = 0, rangeVar = 0, durFixed = 0, durVar = 2, area = 0, target = 1, saveResult = 0, saveType = 4, effect = 16, whenCast = 0, castTime = 10, aiPriority = 0, offensive = 0, aiRadius = 0 },
            new Types.TSpellRec { castClass = 1, level = 1, rangeFixed = 0, rangeVar = 0, durFixed = 0, durVar = 5, area = 0, target = 1, saveResult = 0, saveType = 4, effect = 17, whenCast = 2, castTime = 1, aiPriority = 3, offensive = 0, aiRadius = 0 },
            new Types.TSpellRec { castClass = 1, level = 1, rangeFixed = 255, rangeVar = 0, durFixed = 0, durVar = 0, area = 4, target = 0, saveResult = 0, saveType = 4, effect = 0, whenCast = 1, castTime = 1, aiPriority = 4, offensive = 1, aiRadius = 0 },
            new Types.TSpellRec { castClass = 1, level = 1, rangeFixed = 3, rangeVar = 4, durFixed = 0, durVar = 5, area = 9, target = 0, saveResult = 0, saveType = 4, effect = 53, whenCast = 1, castTime = 1, aiPriority = 7, offensive = 1, aiRadius = 1 },
            new Types.TSpellRec { castClass = 0, level = 2, rangeFixed = 0, rangeVar = 0, durFixed = 30, durVar = 0, area = 0, target = 1, saveResult = 0, saveType = 4, effect = 19, whenCast = 0, castTime = 5, aiPriority = 0, offensive = 0, aiRadius = 0 },
            new Types.TSpellRec { castClass = 0, level = 2, rangeFixed = 6, rangeVar = 0, durFixed = 4, durVar = 1, area = 6, target = 0, saveResult = 1, saveType = 4, effect = 52, whenCast = 1, castTime = 5, aiPriority = 7, offensive = 1, aiRadius = 0 },
            new Types.TSpellRec { castClass = 0, level = 2, rangeFixed = 0, rangeVar = 0, durFixed = 0, durVar = 10, area = 4, target = 2, saveResult = 0, saveType = 4, effect = 20, whenCast = 2, castTime = 5, aiPriority = 2, offensive = 0, aiRadius = 0 },
            new Types.TSpellRec { castClass = 0, level = 2, rangeFixed = 12, rangeVar = 0, durFixed = 0, durVar = 2, area = 31, target = 0, saveResult = 3, saveType = 4, effect = 21, whenCast = 1, castTime = 5, aiPriority = 4, offensive = 1, aiRadius = 1 },
            new Types.TSpellRec { castClass = 0, level = 2, rangeFixed = 0, rangeVar = 0, durFixed = 0, durVar = 60, area = 4, target = 2, saveResult = 0, saveType = 4, effect = 22, whenCast = 2, castTime = 1, aiPriority = 0, offensive = 0, aiRadius = 0 },
            new Types.TSpellRec { castClass = 0, level = 2, rangeFixed = 3, rangeVar = 0, durFixed = 0, durVar = 0, area = 240, target = 0, saveResult = 0, saveType = 4, effect = 51, whenCast = 1, castTime = 5, aiPriority = 0, offensive = 1, aiRadius = 0 },
            new Types.TSpellRec { castClass = 0, level = 2, rangeFixed = 3, rangeVar = 0, durFixed = 0, durVar = 1, area = 0, target = 1, saveResult = 0, saveType = 4, effect = 23, whenCast = 2, castTime = 5, aiPriority = 1, offensive = 0, aiRadius = 0 },
            new Types.TSpellRec { castClass = 1, level = 2, rangeFixed = 0, rangeVar = 4, durFixed = 0, durVar = 5, area = 0, target = 1, saveResult = 0, saveType = 4, effect = 24, whenCast = 2, castTime = 2, aiPriority = 1, offensive = 0, aiRadius = 0 },
            new Types.TSpellRec { castClass = 1, level = 2, rangeFixed = 0, rangeVar = 0, durFixed = 0, durVar = 0, area = 4, target = 2, saveResult = 0, saveType = 4, effect = 25, whenCast = 2, castTime = 2, aiPriority = 2, offensive = 0, aiRadius = 0 },
            new Types.TSpellRec { castClass = 1, level = 2, rangeFixed = 0, rangeVar = 0, durFixed = 0, durVar = 0, area = 0, target = 0, saveResult = 0, saveType = 4, effect = 0, whenCast = 0, castTime = 1, aiPriority = 0, offensive = 0, aiRadius = 0 },
            new Types.TSpellRec { castClass = 1, level = 2, rangeFixed = 0, rangeVar = 0, durFixed = 0, durVar = 2, area = 0, target = 1, saveResult = 0, saveType = 4, effect = 28, whenCast = 2, castTime = 2, aiPriority = 3, offensive = 0, aiRadius = 0 },
            new Types.TSpellRec { castClass = 1, level = 2, rangeFixed = 1, rangeVar = 1, durFixed = 0, durVar = 1, area = 4, target = 0, saveResult = 1, saveType = 4, effect = 29, whenCast = 1, castTime = 2, aiPriority = 2, offensive = 1, aiRadius = 0 },
            new Types.TSpellRec { castClass = 1, level = 2, rangeFixed = 3, rangeVar = 0, durFixed = 0, durVar = 1, area = 9, target = 0, saveResult = 3, saveType = 0, effect = 30, whenCast = 1, castTime = 2, aiPriority = 7, offensive = 1, aiRadius = 1 },
            new Types.TSpellRec { castClass = 1, level = 2, rangeFixed = 0, rangeVar = 0, durFixed = 0, durVar = 60, area = 0, target = 2, saveResult = 0, saveType = 4, effect = 38, whenCast = 0, castTime = 10, aiPriority = 0, offensive = 0, aiRadius = 0 },
            new Types.TSpellRec { castClass = 0, level = 3, rangeFixed = 1, rangeVar = 0, durFixed = 0, durVar = 0, area = 240, target = 4, saveResult = 0, saveType = 4, effect = 32, whenCast = 2, castTime = 10, aiPriority = 2, offensive = 0, aiRadius = 0 },
            new Types.TSpellRec { castClass = 0, level = 3, rangeFixed = 0, rangeVar = 0, durFixed = 0, durVar = 0, area = 4, target = 2, saveResult = 0, saveType = 4, effect = 0, whenCast = 2, castTime = 10, aiPriority = 0, offensive = 0, aiRadius = 0 },
            new Types.TSpellRec { castClass = 0, level = 3, rangeFixed = 255, rangeVar = 0, durFixed = 0, durVar = 0, area = 4, target = 0, saveResult = 1, saveType = 4, effect = 33, whenCast = 1, castTime = 10, aiPriority = 3, offensive = 1, aiRadius = 0 },
            new Types.TSpellRec { castClass = 0, level = 3, rangeFixed = 0, rangeVar = 0, durFixed = 0, durVar = 0, area = 0, target = 2, saveResult = 0, saveType = 4, effect = 0, whenCast = 0, castTime = 100, aiPriority = 0, offensive = 0, aiRadius = 0 },
            new Types.TSpellRec { castClass = 0, level = 3, rangeFixed = 255, rangeVar = 0, durFixed = 0, durVar = 0, area = 4, target = 0, saveResult = 1, saveType = 4, effect = 34, whenCast = 1, castTime = 100, aiPriority = 4, offensive = 1, aiRadius = 0 },
            new Types.TSpellRec { castClass = 0, level = 3, rangeFixed = 6, rangeVar = 0, durFixed = 0, durVar = 0, area = 9, target = 2, saveResult = 0, saveType = 4, effect = 0, whenCast = 2, castTime = 4, aiPriority = 5, offensive = 1, aiRadius = 1 },
            new Types.TSpellRec { castClass = 0, level = 3, rangeFixed = 0, rangeVar = 0, durFixed = 0, durVar = 1, area = 0, target = 1, saveResult = 0, saveType = 4, effect = 49, whenCast = 2, castTime = 6, aiPriority = 6, offensive = 0, aiRadius = 0 },
            new Types.TSpellRec { castClass = 0, level = 3, rangeFixed = 0, rangeVar = 0, durFixed = 0, durVar = 0, area = 4, target = 2, saveResult = 0, saveType = 4, effect = 0, whenCast = 2, castTime = 6, aiPriority = 0, offensive = 0, aiRadius = 0 },
            new Types.TSpellRec { castClass = 0, level = 3, rangeFixed = 255, rangeVar = 0, durFixed = 0, durVar = 10, area = 4, target = 0, saveResult = 1, saveType = 4, effect = 36, whenCast = 1, castTime = 6, aiPriority = 4, offensive = 1, aiRadius = 0 },
            new Types.TSpellRec { castClass = 1, level = 3, rangeFixed = 0, rangeVar = 0, durFixed = 0, durVar = 1, area = 0, target = 1, saveResult = 0, saveType = 4, effect = 37, whenCast = 1, castTime = 1, aiPriority = 2, offensive = 0, aiRadius = 0 },
            new Types.TSpellRec { castClass = 1, level = 3, rangeFixed = 12, rangeVar = 0, durFixed = 0, durVar = 1, area = 9, target = 2, saveResult = 0, saveType = 4, effect = 0, whenCast = 2, castTime = 3, aiPriority = 2, offensive = 1, aiRadius = 1 },
            new Types.TSpellRec { castClass = 1, level = 3, rangeFixed = 10, rangeVar = 1, durFixed = 0, durVar = 0, area = 11, target = 0, saveResult = 2, saveType = 4, effect = 0, whenCast = 1, castTime = 3, aiPriority = 7, offensive = 1, aiRadius = 3 },
            new Types.TSpellRec { castClass = 1, level = 3, rangeFixed = 6, rangeVar = 0, durFixed = 3, durVar = 1, area = 10, target = 4, saveResult = 0, saveType = 4, effect = 39, whenCast = 2, castTime = 3, aiPriority = 3, offensive = 0, aiRadius = 0 },
            new Types.TSpellRec { castClass = 1, level = 3, rangeFixed = 12, rangeVar = 0, durFixed = 0, durVar = 2, area = 7, target = 0, saveResult = 1, saveType = 4, effect = 52, whenCast = 1, castTime = 3, aiPriority = 6, offensive = 1, aiRadius = 0 },
            new Types.TSpellRec { castClass = 1, level = 3, rangeFixed = 0, rangeVar = 0, durFixed = 0, durVar = 0, area = 9, target = 4, saveResult = 0, saveType = 4, effect = 25, whenCast = 2, castTime = 3, aiPriority = 1, offensive = 0, aiRadius = 0 },
            new Types.TSpellRec { castClass = 1, level = 3, rangeFixed = 4, rangeVar = 1, durFixed = 0, durVar = 0, area = 8, target = 0, saveResult = 2, saveType = 4, effect = 0, whenCast = 1, castTime = 3, aiPriority = 7, offensive = 1, aiRadius = 0 },
            new Types.TSpellRec { castClass = 1, level = 3, rangeFixed = 0, rangeVar = 0, durFixed = 0, durVar = 2, area = 4, target = 2, saveResult = 0, saveType = 4, effect = 45, whenCast = 2, castTime = 3, aiPriority = 1, offensive = 0, aiRadius = 0 },
            new Types.TSpellRec { castClass = 1, level = 3, rangeFixed = 0, rangeVar = 0, durFixed = 0, durVar = 2, area = 4, target = 2, saveResult = 0, saveType = 4, effect = 46, whenCast = 2, castTime = 3, aiPriority = 2, offensive = 0, aiRadius = 0 },
            new Types.TSpellRec { castClass = 1, level = 3, rangeFixed = 0, rangeVar = 0, durFixed = 0, durVar = 10, area = 4, target = 2, saveResult = 0, saveType = 4, effect = 41, whenCast = 2, castTime = 3, aiPriority = 3, offensive = 0, aiRadius = 0 },
            new Types.TSpellRec { castClass = 1, level = 3, rangeFixed = 9, rangeVar = 1, durFixed = 3, durVar = 1, area = 10, target = 0, saveResult = 0, saveType = 4, effect = 42, whenCast = 1, castTime = 3, aiPriority = 5, offensive = 1, aiRadius = 0 },
            new Types.TSpellRec { castClass = 0, level = 7, rangeFixed = 0, rangeVar = 0, durFixed = 0, durVar = 0, area = 4, target = 2, saveResult = 0, saveType = 4, effect = 0, whenCast = 2, castTime = 6, aiPriority = 0, offensive = 0, aiRadius = 0 },
            new Types.TSpellRec { castClass = 2, level = 6, rangeFixed = 0, rangeVar = 0, durFixed = 0, durVar = 0, area = 0, target = 1, saveResult = 0, saveType = 4, effect = 39, whenCast = 2, castTime = 0, aiPriority = 3, offensive = 0, aiRadius = 0 },
            new Types.TSpellRec { castClass = 2, level = 6, rangeFixed = 0, rangeVar = 0, durFixed = 0, durVar = 0, area = 4, target = 2, saveResult = 0, saveType = 4, effect = 0, whenCast = 2, castTime = 0, aiPriority = 1, offensive = 0, aiRadius = 0 },
            new Types.TSpellRec { castClass = 2, level = 6, rangeFixed = 0, rangeVar = 0, durFixed = 0, durVar = 0, area = 0, target = 1, saveResult = 0, saveType = 4, effect = 38, whenCast = 2, castTime = 0, aiPriority = 1, offensive = 0, aiRadius = 0 },
            new Types.TSpellRec { castClass = 2, level = 6, rangeFixed = 4, rangeVar = 4, durFixed = 0, durVar = 0, area = 4, target = 0, saveResult = 2, saveType = 4, effect = 0, whenCast = 1, castTime = 0, aiPriority = 7, offensive = 1, aiRadius = 0 },
            new Types.TSpellRec { castClass = 2, level = 6, rangeFixed = 6, rangeVar = 0, durFixed = 0, durVar = 0, area = 4, target = 0, saveResult = 1, saveType = 0, effect = 52, whenCast = 1, castTime = 0, aiPriority = 7, offensive = 1, aiRadius = 0 },
            new Types.TSpellRec { castClass = 2, level = 6, rangeFixed = 0, rangeVar = 0, durFixed = 0, durVar = 0, area = 0, target = 1, saveResult = 0, saveType = 4, effect = 39, whenCast = 2, castTime = 0, aiPriority = 1, offensive = 0, aiRadius = 0 },
            new Types.TSpellRec { castClass = 2, level = 6, rangeFixed = 0, rangeVar = 0, durFixed = 0, durVar = 0, area = 7, target = 4, saveResult = 0, saveType = 4, effect = 71, whenCast = 2, castTime = 0, aiPriority = 2, offensive = 0, aiRadius = 0 },
            new Types.TSpellRec { castClass = 2, level = 6, rangeFixed = 7, rangeVar = 0, durFixed = 0, durVar = 0, area = 11, target = 0, saveResult = 2, saveType = 4, effect = 0, whenCast = 1, castTime = 0, aiPriority = 7, offensive = 1, aiRadius = 3 },
            new Types.TSpellRec { castClass = 2, level = 6, rangeFixed = 12, rangeVar = 0, durFixed = 0, durVar = 0, area = 4, target = 0, saveResult = 0, saveType = 4, effect = 0, whenCast = 1, castTime = 0, aiPriority = 6, offensive = 1, aiRadius = 0 },
            new Types.TSpellRec { castClass = 2, level = 6, rangeFixed = 0, rangeVar = 0, durFixed = 0, durVar = 0, area = 0, target = 1, saveResult = 0, saveType = 4, effect = 0, whenCast = 2, castTime = 0, aiPriority = 0, offensive = 0, aiRadius = 0 },
            new Types.TSpellRec { castClass = 2, level = 6, rangeFixed = 0, rangeVar = 0, durFixed = 0, durVar = 0, area = 0, target = 1, saveResult = 0, saveType = 4, effect = 4, whenCast = 0, castTime = 0, aiPriority = 0, offensive = 0, aiRadius = 0 }
        };
    }
}
