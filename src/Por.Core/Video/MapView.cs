// Source: Talonius pool-of-radiance pinned 7616920a (src/mapview.pas); Qwen3.8:27b initial draft, supervisor repaired.
using System;


namespace Por.Core
{
    public static class MapView
    {
        private const byte Black = 0;
        private const byte Blue = 1;
        private const byte Brown = 6;
        private const byte LightGray = 7;
        private const byte DarkGray = 8;
        private const byte White = 15;

        // Test/platform seams: each defaults to the converted routine the Pascal unit calls.
        internal static Action<short, short> BuildWildernessMap = Wilds.BuildWildernessMap;
        internal static Func<sbyte, sbyte, byte> GetMapEvent = View3D.GetMapEvent;
        internal static Action<byte, byte, byte, byte> SetView3DColors = View3D.SetView3DColors;
        internal static Action<sbyte, sbyte, byte> Display3DMapView = View3D.Display3DMapView;
        internal static Action<sbyte, sbyte, byte, byte, byte> DrawIconAnimSprite = MapTiles.DrawIconAnimSprite;
        internal static Action FlushView = Blit.FlushView;

        public static void DisplayWildMapView()
        {
            if (!(GDVars.gLocType >= C_Area.LOC_WILD_WEST && GDVars.gLocType <= C_Area.LOC_WILD_EAST))
                return;

            sbyte wx = unchecked((sbyte)(GDConsts.wildColOfs[GDVars.gLocType] + GDVars.zone0.partyX.value));
            if (wx < 2)
            {
            }
            else if (wx < 42)
            {
                wx = 2;
            }
            else if (wx < 43)
            {
                wx = 3;
            }
            else
            {
                wx = 4;
            }

            byte wy;
            if (GDVars.zone0.partyY.value < 2)
                wy = unchecked((byte)GDVars.zone0.partyY.value);
            else if (GDVars.zone0.partyY.value < 34)
                wy = 2;
            else if (GDVars.zone0.partyY.value < 35)
                wy = 3;
            else
                wy = 4;

            DrawIconAnimSprite(wx, (sbyte)wy, GDVars.gFacing, (byte)(GDVars.dsWildAnimFrame % 2), (byte)(GDVars.dsWildAnimFrame / 2 + 9));
            GDVars.dsWildAnimFrame = (byte)((GDVars.dsWildAnimFrame + 1) % 6);
            FlushView();
        }

        private static byte CeilColor(byte w)
        {
            switch (w)
            {
                case 8:
                    return Black;
                case 65:
                    return Black;
                case 232:
                    return White;
                default:
                    return White;
            }
        }

        private static byte FloorColor(byte w)
        {
            switch (w)
            {
                case 8:
                    return DarkGray;
                case 65:
                    return LightGray;
                case 232:
                    return LightGray;
                default:
                    return LightGray;
            }
        }

        private static byte WallColor(byte w)
        {
            switch (w)
            {
                case 11:
                    return DarkGray;
                case 9:
                    return Brown;
                case 219:
                    return Brown;
                case 6:
                    return Blue;
                default:
                    return Brown;
            }
        }

        public static void DisplayMapView()
        {
            sbyte wx;
            sbyte wy;
            byte loc = GDVars.gLocType;
            if (GDVars.zone0.view3dFlag != 0)
                loc = C_Area.LOC_3D_MAP;

            if (loc == C_Area.LOC_3D_MAP)
            {
                GDVars.eventAtCell = GetMapEvent(GDVars.gMapX, GDVars.gMapY);
                if (GDVars.eventAtCell > 0x7F)
                    GDVars.dsColorSkyCeiling = GDConsts.skyCeilingColors[unchecked((byte)(GDVars.zone0.alternateSkyColor & 0x0F))];
                else
                    GDVars.dsColorSkyCeiling = GDConsts.skyCeilingColors[unchecked((byte)(GDVars.zone0.skyCeilingColor & 0x0F))];

                if (GDVars.picRegionW)
                {
                    GDVars.dsWallColorA = CeilColor(GDVars.ceilFloorColorSel);
                    GDVars.dsWallColorB = FloorColor(GDVars.ceilFloorColorSel);
                    GDVars.dsWallColorC = WallColor(GDVars.wallColorSel);
                }

                SetView3DColors(GDVars.dsColorSkyCeiling, GDVars.dsWallColorA, GDVars.dsWallColorB, GDVars.dsWallColorC);
                if (GDVars.zone0.noAutomap != 0)
                    GDVars.automapOn = false;
                Display3DMapView(GDVars.gMapX, GDVars.gMapY, GDVars.gFacing);
            }
            else if (loc >= C_Area.LOC_WILD_WEST && loc <= C_Area.LOC_WILD_EAST)
            {
                wx = unchecked((sbyte)(GDConsts.wildColOfs[loc] + GDVars.zone0.partyX.value - 1 - 1));
                if (wx < 0) wx = 0;
                if (wx > 39) wx = 39;

                wy = unchecked((sbyte)(GDVars.zone0.partyY.value - 1 - 1));
                if (wy < 0) wy = 0;
                if (wy > 31) wy = 31;

                BuildWildernessMap(wx, wy);
                DisplayWildMapView();
            }
        }
    }
}
