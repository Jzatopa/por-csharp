// Direct supervisor conversion of pinned src/comptool.pas (7616920).
using System;
using static Por.Core.Types;
namespace Por.Core
{
    public static class CompTool
    {
        // The only unconverted unit this screen needs; missing editor never succeeds silently.
        public static Action EditCharacter;
        private const int MAP_COL = 2, MAP_ROW = 3, LEGEND_COL = 20, NOTE_ROW = 20, SYM_SET = 4;
        private const ushort SYM_ARROW = 256, SYM_WALLS = 260;
        private const byte Black = 0, Blue = 1, Green = 2, Cyan = 3, Red = 4, Magenta = 5,
            Brown = 6, DarkGray = 8, LightBlue = 9, LightGreen = 10, LightGray = 7,
            LightMagenta = 13, Yellow = 14, White = 15;
        private static readonly byte[] KindColours = { 0, Blue, Cyan, Red, Brown, Green, Magenta, LightBlue };
        private static readonly byte[] KindColoursCga = { 0, Cyan, Cyan, Magenta, White, White, Magenta, White };
        private static readonly string[] KindNames = { "", "event", "message", "trap", "treasure", "service", "fight", "exit" };

        internal static void DrawTools()
        {
            TextRend.ClearScreen();
            Border.DrawBorder(C_Msg.TEXT_LEFT, C_Msg.TEXT_TOP, C_Msg.TEXT_RIGHT, C_Msg.TEXT_BOTTOM, 0);
            TextRend.DisplayCentered(C_Msg.TEXT_LEFT, C_Msg.TEXT_RIGHT, 2, White, "Companion Tools");
            TextRend.DisplayString(3, 5, LightGreen, "Map        the whole area");
            TextRend.DisplayString(3, 6, LightGreen, "Notes      your notes on it");
            TextRend.DisplayString(3, 7, LightGreen, "Character  edit a character");
        }

        internal static void ShowSymbol(sbyte col, sbyte row, ushort symbol, bool dim, byte fill, bool noted, bool cursor)
        {
            TImgBuf symbols = GDVars.dsSymbolSetPtrs[SYM_SET], glyph = GDVars.dsCurrent8x8SymbolPtr2;
            if (symbols == null || glyph == null) return;
            // Pascal Move copies exactly imgSize; map is an inline value, not a shared reference.
            Array.Copy(symbols.data, unchecked((ushort)(symbol - GDConsts.symSetFirst[SYM_SET])) * symbols.imgSize,
                       glyph.data, 0, symbols.imgSize);
            Array.Copy(symbols.map.Data, glyph.map.Data, 8);
            var from = new TColorTab(); var to = new TColorTab();
            for (int c = 0; c < 16; c++) from[c] = to[c] = (byte)c;
            if (GDVars.video.colours >= 16)
            {
                if (dim) { to[DarkGray] = Black; to[LightGray] = DarkGray; to[White] = DarkGray; }
            }
            else if (!dim) to[LightGray] = White;
            if (fill != DarkGray) to[DarkGray] = fill;
            if (noted) { to[LightGray] = GDVars.video.colours >= 16 ? Yellow : LightMagenta; to[White] = to[LightGray]; }
            if (cursor) { to[DarkGray] = White; to[LightGray] = Black; to[White] = Black; }
            bool remap = false;
            for (int c = 0; c < 16; c++) if (to[c] != c) remap = true;
            if (remap) Blit.RemapPic(from, to, 0, false, glyph);
            Blit.ShowPic(col, row, glyph);
        }

        internal static byte KindColour(byte kind) { return GDVars.video.colours >= 16 ? KindColours[kind] : KindColoursCga[kind]; }

        internal static void DrawSquare(sbyte mx, sbyte my, bool cursor)
        {
            byte script = GDVars.eclScriptId, map = GDVars.zone0.mapId.lo;
            ushort sym = SYM_WALLS;
            if (View3D.GetWallType(mx, my, C_Dir.NORTH) > 0) sym++;
            if (View3D.GetWallType(mx, my, C_Dir.EAST) > 0) sym += 2;
            if (View3D.GetWallType(mx, my, C_Dir.SOUTH) > 0) sym += 4;
            if (View3D.GetWallType(mx, my, C_Dir.WEST) > 0) sym += 8;
            if (mx == GDVars.gMapX && my == GDVars.gMapY) sym = (ushort)(SYM_ARROW + GDVars.gFacing / 2);
            byte kind = EvtKind.Kind(View3D.GetMapEvent(mx, my));
            byte fill = kind == EvtKind.KIND_NONE ? DarkGray : KindColour(kind);
            ShowSymbol(unchecked((sbyte)(MAP_COL + mx)), unchecked((sbyte)(MAP_ROW + my)), sym,
                !Explored.Visited(script, map, mx, my), fill, MapNotes.Note(script, map, mx, my) != "", cursor);
        }

        internal static void DrawMap()
        {
            EvtKind.Prepare();
            bool[] used = new bool[EvtKind.KIND_EXIT + 1];
            TextRend.ClearScreen();
            Border.DrawBorder(C_Msg.TEXT_LEFT, C_Msg.TEXT_TOP, C_Msg.TEXT_RIGHT, C_Msg.TEXT_BOTTOM, 0);
            TextRend.DisplayString(MAP_COL, 1, White, AreaName.CurrentArea());
            for (sbyte my = 0; my < 16; my++) for (sbyte mx = 0; mx < 16; mx++)
            {
                DrawSquare(mx, my, false);
                byte kind = EvtKind.Kind(View3D.GetMapEvent(mx, my));
                if (kind != EvtKind.KIND_NONE) used[kind] = true;
            }
            TextRend.DisplayString(LEGEND_COL, MAP_ROW, LightGreen, "Bright: visited");
            TextRend.DisplayString(LEGEND_COL, MAP_ROW + 1, DarkGray, "Dim: not yet");
            TextRend.DisplayString(LEGEND_COL, MAP_ROW + 2, Yellow, "Yellow: a note");
            TextRend.DisplayString(LEGEND_COL, MAP_ROW + 3, LightGreen, "Party at " + StrUtil.NumStr(GDVars.gMapX) + "," + StrUtil.NumStr(GDVars.gMapY));
            byte row = MAP_ROW + 5;
            for (byte kind = EvtKind.KIND_EVENT; kind <= EvtKind.KIND_EXIT; kind++) if (used[kind])
            {
                ShowSymbol(LEGEND_COL, (sbyte)row, SYM_WALLS, false, KindColour(kind), false, false);
                TextRend.DisplayString(LEGEND_COL + 2, row, LightGreen, KindNames[kind]);
                row++;
            }
        }

        internal static bool OnAreaMap()
        {
            bool result = GDVars.gLocType == C_Area.LOC_3D_MAP;
            if (!result)
            {
                TextRend.DisplayAndPause(Yellow, "The wilderness has its own map.");
                TextRend.ClearBottomLine();
            }
            return result;
        }

        internal static void MapTool()
        {
            if (!OnAreaMap()) return;
            DrawMap(); byte ch; bool ext = false;
            do { ch = MenuCore.MenuHorizontal("Map: ", "Exit", LightMagenta, LightGreen, White, false, false, ref ext); }
            while (ch != C_Keys.NONE && ch != 'E');
            TextRend.ClearBottomLine();
        }

        internal static void NotesTool()
        {
            if (!OnAreaMap()) return;
            byte script = GDVars.eclScriptId, map = GDVars.zone0.mapId.lo;
            DrawMap(); sbyte cx = GDVars.gMapX, cy = GDVars.gMapY;
            byte ch; bool ext = false;
            do
            {
                DrawSquare(cx, cy, true);
                TextRend.ClearRect(MAP_COL, NOTE_ROW, C_Msg.TEXT_RIGHT - 1, NOTE_ROW);
                TextRend.DisplayString(MAP_COL, NOTE_ROW, LightGreen, StrUtil.NumStr(cx) + "," + StrUtil.NumStr(cy) + " ");
                TextRend.DisplayString(MAP_COL + 6, NOTE_ROW, Yellow, MapNotes.Note(script, map, cx, cy));
                MenuCore.arrowsToCaller = true;
                ch = MenuCore.MenuHorizontal("Notes: ", "Write Clear Exit", LightMagenta, LightGreen, White, true, false, ref ext);
                sbyte nx = cx, ny = cy;
                if (ext)
                {
                    switch (ch)
                    {
                        case C_Keys.LEFT: if (cx > 0) nx--; break;
                        case C_Keys.RIGHT: if (cx < 15) nx++; break;
                        case C_Keys.UP: if (cy > 0) ny--; break;
                        case C_Keys.DOWN: if (cy < 15) ny++; break;
                    }
                }
                else if (ch == 'W')
                {
                    string text = TextRend.ReadString("Note: ", LightGreen, MapNotes.NOTE_LEN);
                    if (text != "" && !MapNotes.SetNote(script, map, cx, cy, text))
                        TextRend.DisplayAndPause(Yellow, "The notes are full.");
                }
                else if (ch == 'C') MapNotes.SetNote(script, map, cx, cy, "");
                if (nx != cx || ny != cy || !ext)
                {
                    DrawSquare(cx, cy, false); cx = nx; cy = ny;
                }
            } while (ext || (ch != C_Keys.NONE && ch != 'E'));
            TextRend.ClearBottomLine();
        }

        public static void ToolsMenu()
        {
            if (GDVars.companionOn == 0) return;
            DrawTools(); byte ch; bool ext = false;
            do
            {
                ch = MenuCore.MenuHorizontal("Tools: ", "Map Notes Character Exit", LightMagenta, LightGreen, White, false, false, ref ext);
                switch (ch)
                {
                    case (byte)'M': MapTool(); DrawTools(); break;
                    case (byte)'C':
                        if (EditCharacter == null) throw new NotSupportedException("CompTool needs CharEdit.EditMenu (CompTool.EditCharacter).");
                        EditCharacter(); DrawTools(); break;
                    case (byte)'N': NotesTool(); DrawTools(); break;
                }
            } while (ch != C_Keys.NONE && ch != 'E');
            TextRend.ClearBottomLine();
        }
    }
}
