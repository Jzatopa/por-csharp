// Source: Talonius pool-of-radiance pinned 7616920a (src/jrnlview.pas); Qwen3.8:27b initial draft, supervisor repaired.
using System;


namespace Por.Core
{
    public static class JrnlView
    {
        private const int PANE_RIGHT = 38;
        private const int PANE_LEFT = PANE_RIGHT - C_Jrnl.JOURNAL_WIDTH + 1;
        private const int LIST_LEFT = 1;
        private const int LIST_RIGHT = PANE_LEFT - 2;
        private const int TOP_ROW = 1;
        private const int BOTTOM_ROW = 22;
        private const int TITLE_ROW = 1;
        private const int TEXT_TOP = 3;
        private const int TEXT_ROWS = BOTTOM_ROW - TEXT_TOP + 1;
        private const int PREVIEW_DELAY = 25;

        private const byte White = 15;
        private const byte LightGreen = 10;
        private const byte LightMagenta = 13;

        internal static int sel;
        internal static int top;
        internal static int shown;
        internal static int lines;
        internal static int page;
        internal static int pages;
        internal static bool previewDue;
        internal static int movedAt;

        private static bool KeyPressed()
        {
            if (GameIo.Host == null)
                throw new NotSupportedException();
            return GameIo.Host.KeyPressed;
        }

        private static string CopyStr(string s, int n)
        {
            return s.Length > n ? s.Substring(0, n) : s;
        }

        internal static void DrawLabel(int n, byte color)
        {
            TextRend.DisplayString((byte)LIST_LEFT, (byte)(TOP_ROW + n - top), color,
                                   CopyStr(JrnlList.EntryLabel((short)n), LIST_RIGHT - LIST_LEFT + 1));
        }

        internal static void DrawList()
        {
            Border.ClearBox((sbyte)LIST_LEFT, (sbyte)TOP_ROW, (sbyte)LIST_RIGHT, (sbyte)BOTTOM_ROW);
            int n = top;
            while (n <= C_Jrnl.JOURNAL_COUNT && n - top <= BOTTOM_ROW - TOP_ROW)
            {
                if (n == sel)
                    DrawLabel(n, White);
                else
                    DrawLabel(n, LightGreen);
                n++;
            }
        }

        internal static void DrawMore(int row)
        {
            TextRend.DisplayCentered((byte)PANE_LEFT, (byte)PANE_RIGHT, (byte)row, White, "(more)");
        }

        internal static int PageStart(int p)
        {
            if (p <= 1)
                return 1;
            else
                return TEXT_ROWS + (p - 2) * (TEXT_ROWS - 2);
        }

        internal static void DrawPage()
        {
            Border.ClearBox((sbyte)PANE_LEFT, (sbyte)TITLE_ROW, (sbyte)PANE_RIGHT, (sbyte)TITLE_ROW);
            Border.ClearBox((sbyte)PANE_LEFT, (sbyte)TEXT_TOP, (sbyte)PANE_RIGHT, (sbyte)BOTTOM_ROW);
            string title = CopyStr(JrnlList.EntryTitle((short)shown), PANE_RIGHT - PANE_LEFT + 1);
            TextRend.DisplayCentered((byte)PANE_LEFT, (byte)PANE_RIGHT, (byte)TITLE_ROW, White, title);
            int row = TEXT_TOP;
            int lastRow = BOTTOM_ROW;
            if (page > 1)
            {
                DrawMore(TEXT_TOP);
                row++;
            }
            if (page < pages)
            {
                DrawMore(BOTTOM_ROW);
                lastRow--;
            }
            int line = PageStart(page);
            while (row <= lastRow && line <= lines)
            {
                TextRend.DisplayString((byte)PANE_LEFT, (byte)row, LightGreen,
                                       CopyStr(JrnlList.TextLine((short)shown, (short)line), PANE_RIGHT - PANE_LEFT + 1));
                line++;
                row++;
            }
        }

        internal static void ShowSelected()
        {
            previewDue = false;
            if (shown != sel)
            {
                shown = sel;
                lines = JrnlList.LineCount((short)shown);
                if (lines <= TEXT_ROWS)
                    pages = 1;
                else
                {
                    pages = 2;
                    while (lines > PageStart(pages) + TEXT_ROWS - 2)
                        pages++;
                }
                page = 1;
                DrawPage();
            }
            else if (page != 1)
            {
                page = 1;
                DrawPage();
            }
        }

        internal static void Moved(int old)
        {
            if (sel != old)
            {
                previewDue = true;
                movedAt = TextRend.GetTicks();
            }
        }

        internal static void TickPreview()
        {
            if (previewDue && !KeyPressed())
            {
                int now = TextRend.GetTicks();
                if (now < movedAt || now - movedAt >= PREVIEW_DELAY)
                    ShowSelected();
            }
        }

        internal static void MoveTo(int n)
        {
            int old = sel;
            if (n > C_Jrnl.JOURNAL_COUNT)
                n = C_Jrnl.JOURNAL_COUNT;
            if (n < 1)
                n = 1;
            if (n < top || n > top + BOTTOM_ROW - TOP_ROW)
            {
                if (n < top)
                    top = n;
                else
                    top = n - (BOTTOM_ROW - TOP_ROW);
                sel = n;
                DrawList();
            }
            else
            {
                DrawLabel(sel, LightGreen);
                sel = n;
                DrawLabel(sel, White);
            }
            Moved(old);
        }

        internal static void ScrollColumn(bool down)
        {
            int old = sel;
            int row = sel - top;
            if (down)
                top = top + BOTTOM_ROW - TOP_ROW + 1;
            else
                top = top - (BOTTOM_ROW - TOP_ROW + 1);
            if (top > C_Jrnl.JOURNAL_COUNT - (BOTTOM_ROW - TOP_ROW))
                top = C_Jrnl.JOURNAL_COUNT - (BOTTOM_ROW - TOP_ROW);
            if (top < 1)
                top = 1;
            sel = top + row;
            if (sel > C_Jrnl.JOURNAL_COUNT)
                sel = C_Jrnl.JOURNAL_COUNT;
            DrawList();
            Moved(old);
        }

        public static void JournalMenu()
        {
            string menu = "";
            byte ch = 0;
            byte lastKey = (byte)'S';
            bool ext = false;
            bool reading = false;
            byte savedChoice = GDVars.horizChoice;

            byte OptionOf(byte key)
            {
                byte result = 1;
                byte n = 0;
                for (int i = 0; i < menu.Length; i++)
                {
                    if (menu[i] >= 'A' && menu[i] <= 'Z')
                    {
                        n++;
                        if (menu[i] == key)
                            result = n;
                    }
                }
                return result;
            }

            sel = 1;
            top = 1;
            shown = 0;
            lines = 0;
            page = 1;
            pages = 1;
            reading = false;
            lastKey = (byte)'S';

            // the list's box goes first, then the text box and the title box, so the gold corners stay where the borders meet
            Border.ClearBox((sbyte)C_Msg.TEXT_LEFT, (sbyte)C_Msg.TEXT_TOP, (sbyte)C_Msg.TEXT_RIGHT, (sbyte)C_Msg.TEXT_BOTTOM);
            Border.DrawBorder((sbyte)LIST_LEFT, (sbyte)TOP_ROW, (sbyte)LIST_RIGHT, (sbyte)BOTTOM_ROW, 0);
            Border.DrawBorder((sbyte)PANE_LEFT, (sbyte)TEXT_TOP, (sbyte)PANE_RIGHT, (sbyte)BOTTOM_ROW, 0);
            Border.DrawBorder((sbyte)PANE_LEFT, (sbyte)TITLE_ROW, (sbyte)PANE_RIGHT, (sbyte)TITLE_ROW, 0);
            DrawList();
            previewDue = true;
            movedAt = TextRend.GetTicks();
            MenuCore.idleHook = new Types.TIdleProc(TickPreview);

            do
            {
                menu = "Select";
                if (reading && page < pages)
                    menu = menu + " Next";
                if (reading && page > 1)
                    menu = menu + " Prev";
                menu = menu + " Exit";
                GDVars.horizChoice = OptionOf(lastKey);
                ch = MenuCore.MenuHorizontal("", menu, LightMagenta, LightGreen, White, true, false, ref ext);
                if (ext)
                {
                    if (ch == C_Keys.UP || ch == C_Keys.HOME)
                    {
                        if (sel > 1)
                            MoveTo(sel - 1);
                        else
                            MoveTo(C_Jrnl.JOURNAL_COUNT);
                    }
                    else if (ch == C_Keys.DOWN || ch == C_Keys.END_KEY)
                    {
                        if (sel < C_Jrnl.JOURNAL_COUNT)
                            MoveTo(sel + 1);
                        else
                            MoveTo(1);
                    }
                    else if (ch == C_Keys.PGUP)
                    {
                        ScrollColumn(false);
                    }
                    else if (ch == C_Keys.PGDN)
                    {
                        ScrollColumn(true);
                    }
                    if (previewDue)
                    {
                        reading = false;
                        lastKey = (byte)'S';
                    }
                }
                else
                {
                    lastKey = ch;
                    if (ch == (byte)'S')
                    {
                        ShowSelected();
                        reading = true;
                    }
                    else if (ch == (byte)'N')
                    {
                        if (page < pages)
                        {
                            page++;
                            DrawPage();
                        }
                    }
                    else if (ch == (byte)'P')
                    {
                        if (page > 1)
                        {
                            page--;
                            DrawPage();
                        }
                    }
                }
            } while (ext || !(ch == C_Keys.NONE || ch == (byte)'E'));

            MenuCore.idleHook = null;
            GDVars.horizChoice = savedChoice;
            CharDisp.DisplayNoncombatInterface();
        }
    }
}
