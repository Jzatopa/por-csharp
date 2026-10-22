// Source: Talonius pool-of-radiance pinned 7616920a (src/wilds.pas); Qwen3.8:27b initial draft, supervisor repaired.
using System;


namespace Por.Core
{
    public static class Wilds
    {
        private const int WILD_SILVER_DRAGON_DEN = 1;
        private const int WILD_ZHENTIL_OUTPOST = 2;
        private const int WILD_BUCCANEERS_BASE = 3;
        private const int WILD_NOMAD_CAMP = 4;
        private const int WILD_KOBOLD_CAVES = 5;
        private const int WILD_UNNAMED_SITE = 6;
        private const int WILD_LIZARD_MAN_KEEP = 7;
        private const int WILD_SORCERERS_ISLAND = 8;
        private const int WILD_PHLAN = 9;

        private const byte Black = 0;

        // Test seams: the unit calls MapTiles.Copy2DTile and Border.DrawWindow; a test substitutes recorders. Production leaves the defaults.
        internal static Action<sbyte, sbyte, byte, sbyte> Copy2DTile = MapTiles.Copy2DTile;
        internal static Action<sbyte, sbyte, sbyte, sbyte, byte, byte, string> DrawWindow = Border.DrawWindow;

        public static void BuildWildernessMap(short ulX, short ulY)
        {
            if (ulX < 0) ulX = 0;
            if (ulX > 39) ulX = 39;
            if (ulY < 0) ulY = 0;
            if (ulY > 31) ulY = 31;

            sbyte viewY = 0;
            for (byte row = 0; row <= 4; row++)
            {
                sbyte viewX = 0;
                for (byte col = 0; col <= 4; col++)
                {
                    sbyte mapX = unchecked((sbyte)(col + ulX));
                    sbyte mapY = unchecked((sbyte)(row + ulY));
                    byte icon = unchecked((byte)(GDConsts.wildMap[mapY, mapX] - 1));

                    for (sbyte k = 1; k <= 9; k++)
                    {
                        if ((mapX == GDConsts.wildernessSpecialLocations[k].x) &&
                            (mapY == GDConsts.wildernessSpecialLocations[k].y))
                        {
                            switch (k)
                            {
                                case WILD_BUCCANEERS_BASE:
                                    if (GDVars.zone0.buccaneersBaseGate != 255)
                                        icon = unchecked((byte)(GDConsts.wildernessSpecialLocations[k].icon - 1));
                                    break;
                                case WILD_NOMAD_CAMP:
                                    if ((GDVars.zone0.nomadCampPresent != 0) ||
                                        ((GDVars.zone0.nomadCampFlags & 8) != 0) ||
                                        (GDVars.zone0.nomadCampGate == 0))
                                        icon = unchecked((byte)(GDConsts.wildernessSpecialLocations[k].icon - 1));
                                    break;
                                case WILD_KOBOLD_CAVES:
                                    if ((GDVars.zone0.easternSitesGate & 2) == 0)
                                        icon = unchecked((byte)(GDConsts.wildernessSpecialLocations[k].icon - 1));
                                    break;
                                case WILD_UNNAMED_SITE:
                                    icon = unchecked((byte)(GDConsts.wildernessSpecialLocations[k].icon - 1));
                                    break;
                                case WILD_LIZARD_MAN_KEEP:
                                    if ((GDVars.zone0.easternSitesGate & 1) == 0)
                                        icon = unchecked((byte)(GDConsts.wildernessSpecialLocations[k].icon - 1));
                                    break;
                                case WILD_SORCERERS_ISLAND:
                                case WILD_PHLAN:
                                    if (GDVars.zone0.yarashMissionDone > 253)
                                        icon = unchecked((byte)(GDConsts.wildernessSpecialLocations[k].icon - 1));
                                    break;
                            }
                        }
                    }

                    Copy2DTile(unchecked((sbyte)(viewX + 1 - 1)), unchecked((sbyte)(viewY + 1 - 1)), icon, 1);
                    viewX = unchecked((sbyte)(viewX + 3));
                }
                viewY = unchecked((sbyte)(viewY + 3));
            }
        }

        public static void DrawWildWindows()
        {
            DrawWindow(1, 1, 15, 15, 0, Black, "");
            DrawWindow(17, 1, 38, 15, 0, Black, "");
            DrawWindow((sbyte)C_Msg.TEXT_LEFT, (sbyte)C_Msg.MESSAGE_TOP, (sbyte)C_Msg.TEXT_RIGHT, (sbyte)C_Msg.TEXT_BOTTOM, 0, Black, "");
        }
    }
}
