using System;
using static Por.Core.Types;

namespace Por.Core
{
    public static class Border
    {
        private const ushort BORDER_SYM_BASE = 276;
        private const sbyte LAST_COL = 39;
        private const sbyte LAST_ROW = 24;

        public static void DrawBorder(sbyte ulCol, sbyte ulRow, sbyte lrCol, sbyte lrRow, byte corner)
        {
            unchecked
            {
                ulCol = (sbyte)(ulCol - 1);
                ulRow = (sbyte)(ulRow - 1);
                lrCol = (sbyte)(lrCol + 1);
                lrRow = (sbyte)(lrRow + 1);
            }

            ushort symBase = (ushort)(corner * 3 + BORDER_SYM_BASE);

            Sym8x8.Put8x8Sym(ulCol, ulRow, (ushort)(symBase + 0), false, false);
            Sym8x8.Put8x8Sym(lrCol, ulRow, (ushort)(symBase + 0), false, false);
            Sym8x8.Put8x8Sym(ulCol, lrRow, (ushort)(symBase + 0), false, false);
            Sym8x8.Put8x8Sym(lrCol, lrRow, (ushort)(symBase + 0), false, false);

            int i;
            for (i = unchecked((sbyte)(ulCol + 1)); i <= unchecked((sbyte)(lrCol - 1)); i++)
            {
                Sym8x8.Put8x8Sym((sbyte)i, ulRow, (ushort)(symBase + 2), false, false);
                Sym8x8.Put8x8Sym((sbyte)i, lrRow, (ushort)(symBase + 2), false, false);
            }

            for (i = unchecked((sbyte)(ulRow + 1)); i <= unchecked((sbyte)(lrRow - 1)); i++)
            {
                Sym8x8.Put8x8Sym(ulCol, (sbyte)i, (ushort)(symBase + 1), false, false);
                Sym8x8.Put8x8Sym(lrCol, (sbyte)i, (ushort)(symBase + 1), false, false);
            }
        }

        public static void ClearBox(sbyte ulCol, sbyte ulRow, sbyte lrCol, sbyte lrRow)
        {
            if (ulCol < 0) ulCol = 0;
            if (ulRow < 0) ulRow = 0;
            if (lrCol > LAST_COL) lrCol = LAST_COL;
            if (lrRow > LAST_ROW) lrRow = LAST_ROW;

            TextRend.ClearRect(ulCol, ulRow, lrCol, lrRow);
        }

        public static void DrawWindow(sbyte ulCol, sbyte ulRow, sbyte lrCol, sbyte lrRow, byte corner, byte color, string title)
        {
            ClearBox(ulCol, ulRow, lrCol, lrRow);
            DrawBorder(ulCol, ulRow, lrCol, lrRow, corner);

            if (title != null && title.Length > 0)
            {
                string truncated = title.Length > 40 ? title.Substring(0, 40) : title;
                byte left = unchecked((byte)ulCol);
                byte right = unchecked((byte)(lrCol - 1));
                byte row = unchecked((byte)ulRow);
                TextRend.DisplayCentered(left, right, row, color, truncated);
            }
        }
    }
}
