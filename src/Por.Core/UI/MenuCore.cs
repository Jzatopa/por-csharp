using System;
using System.Text;
using static Por.Core.Types;

namespace Por.Core
{
    public static class MenuCore
    {
        public static bool listOnly;
        public static bool arrowsToCaller;
        public static bool enterToCaller;
        public static string extraKeys = "";
        public static string unmarked = "";
        public static bool quickKeysToCaller;
        public static bool movesToCaller;
        public static TIdleProc idleHook;

        private static string ShortText(string text, int capacity) { return text.Length > capacity ? text.Substring(0, capacity) : text; }
        private static bool KeyPending() { if (GameIo.Host == null) throw new NotSupportedException("Menu input host missing."); return GameIo.Host.KeyPressed; }
        private static TMenuAuto dsMenuAutoAnswer = new TMenuAuto();

        public static TMenuAuto AutoAnswer => dsMenuAutoAnswer;

        public static TItemRec FindListMember(short n, TItemRec list)
        {
            int i = 0;
            TItemRec p = list;
            while (p != null && i != n)
            {
                p = p.next;
                i++;
            }
            return i == n ? p : null;
        }

        private static int ListCount(TItemRec list)
        {
            int n = 0;
            TItemRec p = list;
            while (p != null)
            {
                n++;
                p = p.next;
            }
            return n;
        }

        private static char HorizontalMenu(string query, string menu, byte qCol, byte mCol, byte hlCol, bool reinterpret, bool animate, bool takeOption, ref bool ext)
        {
            query = ShortText(query, 40); menu = ShortText(menu, 40); extraKeys = ShortText(extraKeys, 16); unmarked = ShortText(unmarked, 4);
            int frameTick;
            int tick30;
            int tick80;
            char ch = '\0';
            byte j;
            byte i;
            bool done = false;
            byte mLen = (byte)menu.Length;
            byte qLen = (byte)query.Length;
            TOptRange[] opt = new TOptRange[20];
            byte optCnt = 0;
            bool enterOk = (mCol != 0) || (hlCol != 0);

            // ParseMenu
            for (int oi = 0; oi < opt.Length; oi++) opt[oi] = new TOptRange();
            byte idx = 1;
            byte len = (byte)menu.Length;
            for (i = 1; i <= len; i++)
            {
                char c = menu[i - 1];
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z'))
                {
                    if (opt[idx - 1].first == 0)
                    {
                        opt[idx - 1].first = i;
                    }
                    else if (idx < 20)
                    {
                        opt[idx - 1].last = unchecked((byte)(i - 2));
                        idx++;
                        opt[idx - 1].first = i;
                    }
                }
            }
            opt[idx - 1].last = len;
            optCnt = idx;

            if (GDVars.horizChoice < 1 || GDVars.horizChoice > optCnt)
                GDVars.horizChoice = 1;

            ext = false;
            frameTick = TextRend.GetTicks();
            tick30 = unchecked(TextRend.GetTicks() + 30);
            tick80 = tick30 + 50;

            if (query.Length != 0)
                TextRend.DisplayString(0, 24, qCol, query);

            // DisplayOption
            void DisplayOption(byte cur)
            {
                if (mLen > 0)
                {
                    for (int displayIndex = 1; displayIndex <= mLen; displayIndex++)
                    {
                        char mc = menu[displayIndex - 1];
                        if ((opt[cur - 1].first <= displayIndex) && (opt[cur - 1].last >= displayIndex) && (hlCol != 0))
                        {
                            TextRend.RenderChar((byte)(qLen + displayIndex - 1), 24, hlCol, 1, (byte)mc, 1);
                        }
                        else if (((mc >= '0' && mc <= '9') || (mc >= 'A' && mc <= 'Z')) && (unmarked.IndexOf(mc) == -1))
                        {
                            TextRend.RenderChar((byte)(qLen + displayIndex - 1), 24, hlCol, 1, (byte)mc, 1);
                        }
                        else
                        {
                            TextRend.RenderChar((byte)(qLen + displayIndex - 1), 24, mCol, 1, (byte)mc, 1);
                        }
                    }
                    if (qLen + mLen <= 39)
                    {
                        TextRend.RenderChar((byte)(qLen + mLen), 24, 0, (byte)(39 - mLen - qLen + 1), 32, 1);
                    }
                }
            }

            DisplayOption(GDVars.horizChoice);

            void StepOption(bool toNext)
            {
                if (toNext)
                {
                    if (GDVars.horizChoice == optCnt)
                        GDVars.horizChoice = 1;
                    else
                        GDVars.horizChoice++;
                }
                else if (GDVars.horizChoice == 1)
                    GDVars.horizChoice = optCnt;
                else
                    GDVars.horizChoice--;
                DisplayOption(GDVars.horizChoice);
            }

            while (!done)
            {
                if (idleHook != null)
                    idleHook();

                if (TextRend.GetTicks() < frameTick)
                    frameTick = TextRend.GetTicks();

                if (((GDVars.zone0.savedWalls.menuAnimation != 0) || animate) && (GDVars.animData.current > 0))
                {
                    Portrait.DisplayImageBuf(3, 3, animate, GDVars.animData.frames[GDVars.animData.current].pic);
                    if ((TextRend.GetTicks() - frameTick >= GDVars.animData.frames[GDVars.animData.current].time * 10) || (GDVars.zone0.savedWalls.menuAnimation != 0))
                    {
                        GDVars.animData.current++;
                        if (GDVars.animData.current > GDVars.animData.count)
                            GDVars.animData.current = 1;
                        frameTick = TextRend.GetTicks();
                    }
                }

                if ((dsMenuAutoAnswer.timeout > 0) && (TextRend.GetTicks() - frameTick >= dsMenuAutoAnswer.timeout))
                {
                    ch = dsMenuAutoAnswer.key;
                    done = true;
                }
                else if (KeyPending())
                {
                    ch = (char)GameIo.ReadGameKey();

                    if (GDVars.bottomLineTaken)
                    {
                        GDVars.bottomLineTaken = false;
                        if (query.Length != 0)
                            TextRend.DisplayString(0, 24, qCol, query);
                        DisplayOption(GDVars.horizChoice);
                    }

                    if (ch == '\0')
                    {
                        ch = (char)GameIo.ReadGameKey();
                        if (((ch == C_Keys.LEFT) || (ch == C_Keys.RIGHT)) && !arrowsToCaller)
                        {
                            StepOption(ch == C_Keys.RIGHT);
                        }
                        else if (((ch == C_Keys.F5) || (ch == C_Keys.F8)) && !quickKeysToCaller)
                        {
                            if (GDVars.ifcState != C_Ifc.COMBAT)
                            {
                                TextRend.DisplayAndPause(14, "Quick save and load unavailable.");
                                TextRend.ClearBottomLine();
                                if (query.Length != 0)
                                    TextRend.DisplayString(0, 24, qCol, query);
                                DisplayOption(GDVars.horizChoice);
                            }
                        }
                        else if (reinterpret)
                        {
                            ext = true;
                            done = true;
                        }
                    }
                    else
                    {
                        if (ch == C_Keys.ESC)
                        {
                            done = true;
                            ch = (char)C_Keys.NONE;
                        }
                        else if (ch == C_Keys.ENTER)
                        {
                            if (enterOk)
                            {
                                if (takeOption && !enterToCaller && (opt[GDVars.horizChoice - 1].first != 0))
                                    ch = menu[opt[GDVars.horizChoice - 1].first - 1];
                                else
                                    ch = (char)C_Keys.ENTER;
                                done = true;
                            }
                        }
                        else if (ch == ',')
                        {
                            StepOption(false);
                        }
                        else if (ch == '.')
                        {
                            StepOption(true);
                        }
                        else
                        {
                            char upCh = (ch >= 'a' && ch <= 'z') ? (char)(ch - 32) : ch;
                            ch = upCh;
                            if (upCh == ' ' || (upCh >= '0' && upCh <= '9') || (upCh >= 'A' && upCh <= 'Z'))
                            {
                                if (upCh == ' ')
                                {
                                    done = true;
                                }
                                else
                                {
                                    for (i = 1; i <= mLen; i++)
                                    {
                                        if (menu[i - 1] == upCh && unmarked.IndexOf(upCh) == -1)
                                        {
                                            done = true;
                                            j = 1;
                                            while (j < optCnt && opt[j - 1].first != i)
                                                j++;
                                            GDVars.horizChoice = j;
                                            DisplayOption(GDVars.horizChoice);
                                        }
                                    }
                                }
                            }

                            if (upCh != ' ' && extraKeys.IndexOf(upCh) != -1)
                                done = true;

                            if (reinterpret)
                            {
                                if ((upCh >= '1' && upCh <= '9') || upCh == '\\')
                                {
                                    if (upCh == '\\')
                                        ch = (char)55;
                                    else
                                        ch = (char)GDConsts.keypadScan[upCh - '1'];
                                    ext = true;
                                    done = true;
                                }
                            }
                        }
                    }
                }
            }

            arrowsToCaller = false;
            enterToCaller = false;
            extraKeys = "";
            unmarked = "";
            quickKeysToCaller = false;
            GDVars.zone0.savedWalls.menuAnimation = 0;

            return ch;
        }

        public static byte MenuHorizontal(string query, string menu, byte qCol, byte mCol, byte hlCol, bool reinterpret, bool animate, ref bool ext)
        {
            char result = HorizontalMenu(query, menu, qCol, mCol, hlCol, reinterpret, animate, true, ref ext);
            return unchecked((byte)result);
        }

        public static byte MenuVertical(string query, string menu, byte qCol, byte mCol, byte hlCol, sbyte ulCol, sbyte ulRow, sbyte lrCol, sbyte lrRow, TItemRec list, bool addExit, ref bool redraw, ref short idx, ref TItemRec sel)
        {
            query = ShortText(query, 40); menu = ShortText(menu, 40);
            string keep = ShortText(extraKeys, 16);
            extraKeys = "";
            bool toCaller = movesToCaller; movesToCaller = false;
            if (list == null) { listOnly = false; idx = 0; sel = null; return 0; }
            GDVars.horizChoice = 1;
            byte width = unchecked((byte)(lrCol - ulCol + 1));
            int count = ListCount(list), rows = lrRow - ulRow + 1, headerCnt = 0;
            for (var p = list; p != null && p.inUse != 0; p = p.next) headerCnt++;
            short cursor = idx;
            bool repaint = redraw;
            TItemRec selected = sel;
            TItemRec Member(int at) { return FindListMember(unchecked((short)at), list); }
            void DrawPage(int top)
            {
                Border.ClearBox(ulCol, ulRow, lrCol, lrRow);
                var p = Member(top);
                int row = ulRow;
                while (p != null && row <= lrRow)
                {
                    TextRend.DisplayString(unchecked((byte)ulCol), unchecked((byte)row), p.inUse != 0 ? qCol : mCol, p.name);
                    if (p.name.Length < width) TextRend.RenderChar(unchecked((byte)(ulCol + p.name.Length)), unchecked((byte)row), 0, unchecked((byte)(width - p.name.Length)), 32, 1);
                    p = p.next; row = unchecked((sbyte)(row + 1));
                }
            }
            void Highlight(int at, bool bright)
            {
                var p = Member(at); string s = ShortText(p.name, 40);
                int first = 1, last = s.Length;
                while (first < s.Length && s[first - 1] == ' ') first++;
                while (last > 1 && s[last - 1] == ' ') last--;
                int length = unchecked((byte)(last - first + 1));
                string text = first > s.Length || length == 0 ? "" : s.Substring(first - 1, Math.Min(length, s.Length - first + 1));
                byte color = bright ? hlCol : p.inUse != 0 ? qCol : mCol;
                TextRend.DisplayString(unchecked((byte)(ulCol + first - 1)), unchecked((byte)(ulRow + at - GDVars.vertTop)), color, text);
            }
            void SkipLabels(bool down)
            {
                int k = 0;
                while (k < rows && Member(cursor).inUse != 0)
                {
                    k++;
                    cursor = unchecked((short)(cursor + (down ? 1 : -1)));
                    if (down)
                    {
                        if (GDVars.vertTop + rows - 1 < cursor || count - 1 < cursor) cursor = GDVars.vertTop;
                    }
                    else
                    {
                        if (cursor < GDVars.vertTop) cursor = unchecked((short)(GDVars.vertTop + rows - 1));
                        if (count - 1 < cursor) cursor = unchecked((short)(count - 1));
                    }
                }
            }
            void MoveLine(bool down)
            {
                cursor = unchecked((short)(cursor + (down ? 1 : -1)));
                if (down)
                {
                    if (GDVars.vertTop + rows - 1 < cursor || count - 1 < cursor) cursor = GDVars.vertTop;
                }
                else
                {
                    if (cursor < GDVars.vertTop) cursor = unchecked((short)(GDVars.vertTop + rows - 1));
                    if (count - 1 < cursor) cursor = unchecked((short)(count - 1));
                }
                SkipLabels(down);
            }
            void MovePage(bool down)
            {
                int relative = cursor - GDVars.vertTop;
                GDVars.vertTop = unchecked((short)(GDVars.vertTop + (down ? rows : -rows)));
                if (down) { if (count - rows < GDVars.vertTop) GDVars.vertTop = unchecked((short)(count - rows)); }
                else if (GDVars.vertTop < 0) GDVars.vertTop = 0;
                cursor = unchecked((short)(GDVars.vertTop + relative));
                SkipLabels(down); DrawPage(GDVars.vertTop);
            }
            try
            {
                if (GDVars.vertTop > cursor) { GDVars.vertTop = cursor; repaint = true; }
                if (GDVars.vertTop > count) { GDVars.vertTop = 0; repaint = true; }
                cursor = unchecked((short)(cursor + 1)); MoveLine(false);
                if (repaint) DrawPage(GDVars.vertTop);
                repaint = false;
                if (listOnly)
                {
                    listOnly = false;
                    if (count <= rows) { selected = null; return 0; }
                }
                while (true)
                {
                    Highlight(cursor, true);
                    string hMenu = menu;
                    bool hasNext = count - rows > GDVars.vertTop, hasPrev = GDVars.vertTop > headerCnt;
                    if (hasNext) hMenu = ShortText(hMenu + " Next", 40);
                    if (hasPrev) hMenu = ShortText(hMenu + " Prev", 40);
                    if (addExit) hMenu = ShortText(hMenu + " Exit", 40);
                    extraKeys = keep;
                    bool ext = false;
                    char ch = HorizontalMenu(query, hMenu, qCol, mCol, hlCol, true, false, false, ref ext);
                    Highlight(cursor, false);
                    int oldIdx = cursor, oldTop = GDVars.vertTop;
                    if (ext)
                    {
                        switch ((int)ch)
                        {
                            case C_Keys.HOME: case C_Keys.UP: MoveLine(false); break;
                            case C_Keys.END_KEY: case C_Keys.DOWN: MoveLine(true); break;
                            case C_Keys.PGUP: if (hasPrev) MovePage(false); break;
                            case C_Keys.PGDN: if (hasNext) MovePage(true); break;
                        }
                    }
                    else
                    {
                        if (ch == 'P') { if (hasPrev) MovePage(false); }
                        else if (ch == 'N') { if (hasNext) MovePage(true); }
                        else if (ch == C_Keys.ESC || ch == 'E' || ch == C_Keys.NONE) { selected = null; return 0; }
                        else { selected = Member(cursor); return unchecked((byte)ch); }
                    }
                    if (toCaller && (cursor != oldIdx || GDVars.vertTop != oldTop)) { selected = Member(cursor); return unchecked((byte)ch); }
                }
            }
            finally { idx = cursor; redraw = repaint; sel = selected; }
        }

        public static byte YesNoMenu(string query, byte qCol, byte mCol, byte hlCol)
        {
            bool ext = false;
            char ch;
            GDVars.horizChoice = 2;
            do
            {
                ch = (char)MenuHorizontal(query, "Yes No", qCol, mCol, hlCol, false, false, ref ext);
            } while (ch != 'Y' && ch != 'N');
            return unchecked((byte)ch);
        }

        public static void AllocStringList(short n, ref TItemRec list)
        {
            list = new TItemRec();
            list.next = null;
            list.inUse = 0;
            list.name = "";
            TItemRec p = list;
            for (int i = 2; i <= n; i++)
            {
                p.next = new TItemRec();
                p = p.next;
                p.next = null;
                p.inUse = 0;
                p.name = "";
            }
        }

        public static void AppendMenuLine(ref TItemRec head, ref TItemRec tail, string name, bool title)
        {
            TItemRec n = new TItemRec();
            n.next = null;
            n.name = ShortText(name, 40);
            n.inUse = (byte)(title ? 1 : 0);
            if (head == null)
                head = n;
            else
                tail.next = n;
            tail = n;
        }

        public static void FreeStringList(ref TItemRec list)
        {
            // GC handles memory; leave dangling pointers as per source
        }

        static MenuCore()
        {
            listOnly = false;
            arrowsToCaller = false;
            movesToCaller = false;
            extraKeys = "";
            unmarked = "";
            quickKeysToCaller = false;
            idleHook = null;
        }
    }
}
