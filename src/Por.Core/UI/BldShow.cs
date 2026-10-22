using System;
using static Por.Core.Types;

namespace Por.Core
{
    public static class BldShow
    {
        private const int SOURCE_DISKS = 8;
        private const int TITLE_ROW = 1;
        private const int STATUS_TOP = 3;
        private const int STATUS_BOTTOM = 9;
        private const int PANEL_TOP = 11;
        private const int PANEL_BOTTOM = 22;
        private const int MAKE_ROW = 4;
        private const int STEP_ROW = 5;
        private const int OVERALL_ROW = 7;
        private const int ETA_ROW = 8;
        private const int LABEL_WIDTH = 9;
        private const int LABEL_END = 13;
        private const int VALUE_COL = 15;
        private const int STEP_WIDTH = 13;
        private const int COUNT_WIDTH = 5;
        private const int BAR_COL = 21;
        private const int BAR_CELLS = 16;
        private const int BAR_PX = 128;
        private const int PANEL_FIRST = 13;
        private const int PANEL_ROWS = 7;
        private const int NAME_WIDTH = 8;
        private const int LEFT_NAME_COL = 3;
        private const int RIGHT_NAME_COL = 21;
        private const int NOTE_ROW = 21;
        private const int CELL_NONE = 0;
        private const int CELL_WAIT = 1;
        private const int CELL_CURRENT = 2;
        private const int CELL_DONE = 3;
        private const int CELL_FAILED = 4;
        private const int FRAME_BAR = 4;
        private const int FRAMES = 13;
        private const int FIELD_MAKE_LABEL = 0;
        private const int FIELD_MAKE = 1;
        private const int FIELD_STEP_LABEL = 2;
        private const int FIELD_STEP = 3;
        private const int FIELD_COUNT = 4;
        private const int FIELD_ETA = 5;
        private const int FIELDS = 6;
        private const int FIELD_WIDTH = 16;
        private const int NAME_NONE = 0;
        private const int NAME_SHOWN = 1;
        private const int NAME_FAILED = 2;
        private const int PHASE_READ = 0;
        private const int PHASE_WRITE = 1;
        private const int PHASE_CHECK = 2;
        private const int TICKS_PER_DAY = 0x1800B0;
        private const int ETA_FIRST = 36;
        private const int ETA_EVERY = 18;
        private const int HOLD_TICKS = 18;
        private const int ETA_MAX_MINUTES = 99;
        private const int ETA_MAX_TICKS = 200000;
        private const string TITLE = "Please wait, preparing files";
        private const string NOTE = "See DATABLD.LOG";
        private const string LOG_NAME = "DATABLD.LOG";
        private const string PROMPT = "press <enter>/<return> to continue";
        private const int WARN_ROW = 5;
        private const string WARN_LABEL = "Warning:";
        private const string WARN_TEXT = " Install folder is read only.";
        private const int WARN_NEXT_ROW = 7;
        private const string WARN_NEXT = "Game data fixes will be unavailable.";

        private const int Black = 0;
        private const int DarkGray = 8;
        private const int Yellow = 14;
        private const int LightGreen = 10;
        private const int LightRed = 12;
        private const int White = 15;
        private const int LightCyan = 11;

        private class TShowState
        {
            public int[,] size = new int[15, 9];
            public byte[,] place = new byte[15, 9];
            public byte[,] cell = new byte[15, 9];
            public byte[] name = new byte[15];
            public string[] shown = new string[6] { "", "", "", "", "", "" };
            public bool[] listed = new bool[15];
            public byte family;
            public int familyBytes;
            public int base_;
            public byte phase;
            public int phaseDone;
            public int phaseTotal;
            public int phaseNext;
            public int total;
            public int done;
            public ushort barPx;
            public int nextPx;
            public byte finished;
            public bool readOnly;
            public int elapsed;
            public int lastTick;
            public int etaAt;
            public TImgBuf art;
            public TImgBuf pic;
        }

        private static TShowState st = null;

        public static Func<long> MaxAvailable = null;
        private static bool KeyPending()
        {
            if (GameIo.Host == null) throw new NotSupportedException("Build display input host missing.");
            return GameIo.Host.KeyPressed;
        }

        private static int MulDiv(int a, int b, int c)
        {
            if (a <= 0 || b <= 0 || c <= 0)
                return 0;

            byte doublings = 0;
            while (a > 46340 || b > 46340)
            {
                if (a > b)
                    a >>= 1;
                else
                    b >>= 1;

                if (c > 1)
                    c >>= 1;
                else
                    doublings++;
            }

            int q = a * b / c;
            while (doublings > 0)
            {
                q = unchecked(q * 2);
                doublings--;
            }
            return q;
        }

        private static void DrawText(byte col, byte row, byte color, string s)
        {
            if (col < 40 && row < 25)
            {
                int len = s.Length;
                int maxLen = 40 - col;
                if (len > maxLen) len = maxLen;
                if (len > 0)
                    TextRend.DisplayString(col, row, color, s.Substring(0, len));
            }
        }

        private static void MakeFrame(byte frame, byte left, byte right, byte fill, byte fg, byte bg, bool outline)
        {
            TPixelRow px = new TPixelRow();
            for (int y = 0; y <= 7; y++)
            {
                for (int x = 0; x <= 7; x++)
                {
                    px[x] = Black;
                    if (y >= 1 && y <= 6 && x >= left && x <= right)
                    {
                        if (outline)
                        {
                            if (y == 1 || y == 6 || x == left || x == right)
                                px[x] = fg;
                        }
                        else if (x - left < fill)
                            px[x] = fg;
                        else
                            px[x] = bg;
                    }
                }
                if (frame < FRAMES && y <= 7)
                    VidOps.PutPixelRow(st.art, frame, (byte)y, ref px);
            }
        }

        private static void ShowFrame(byte col, byte row, byte frame)
        {
            if (col > 39 || row > 24 || frame >= FRAMES) return;
            int srcOffset = frame * st.art.imgSize;
            for (int i = 0; i < st.pic.imgSize; i++)
                st.pic.data[i] = st.art.data[srcOffset + i];
            Blit.ShowPic((short)col, (short)row, st.pic);
        }

        private static bool FamilyPlace(byte fam, out byte row, out byte col)
        {
            row = 0;
            col = 0;
            if (fam < 1 || fam > 2 * PANEL_ROWS) return false;
            if (fam <= PANEL_ROWS)
            {
                row = (byte)(PANEL_FIRST + fam - 1);
                col = (byte)LEFT_NAME_COL;
            }
            else
            {
                row = (byte)(PANEL_FIRST + fam - PANEL_ROWS - 1);
                col = (byte)RIGHT_NAME_COL;
            }
            return true;
        }

        private static void SetCell(byte fam, byte disk, byte state)
        {
            if (fam < 1 || fam > 14 || disk < 1 || disk > SOURCE_DISKS) return;
            if (st.place[fam, disk] == 0 || st.cell[fam, disk] == state) return;
            byte row, col;
            if (!FamilyPlace(fam, out row, out col)) return;
            st.cell[fam, disk] = state;
            col = (byte)(col + NAME_WIDTH + st.place[fam, disk] - 1);
            if (state != CELL_NONE)
                ShowFrame(col, row, (byte)(state - CELL_WAIT));
        }

        private static void SetCells(byte fam, byte state)
        {
            for (int disk = 1; disk <= SOURCE_DISKS; disk++)
                SetCell(fam, (byte)disk, state);
        }

        private static void SetName(byte fam, byte state)
        {
            if (fam < 1 || fam > 14) return;
            if (!st.listed[fam] || st.name[fam] == state) return;
            byte row, col;
            if (!FamilyPlace(fam, out row, out col)) return;
            st.name[fam] = state;
            byte color;
            if (state == NAME_FAILED)
                color = LightRed;
            else
                color = LightGreen;
            DrawText(col, row, color, StrUtil.PadRight(DataArc.MergedName(fam), (byte)(NAME_WIDTH - 1)));
        }

        private static void Field(byte field, byte col, byte row, byte color, string s, byte width)
        {
            if (field >= FIELDS) return;
            if (width > FIELD_WIDTH)
                width = (byte)FIELD_WIDTH;
            s = StrUtil.PadRight(s, width);
            for (int i = 1; i <= width; i++)
            {
                if (i > st.shown[field].Length || st.shown[field][i - 1] != s[i - 1])
                {
                    if (col + i - 1 < 40)
                        TextRend.RenderChar((byte)(col + i - 1), row, color, 1, (byte)s[i - 1], 1);
                }
            }
            st.shown[field] = s;
        }

        private static void Status(byte labelField, byte valueField, byte row, string lbl, string value)
        {
            Field(labelField, (byte)(LABEL_END - LABEL_WIDTH + 1), row, (byte)Yellow, StrUtil.PadLeft(lbl, (byte)LABEL_WIDTH), (byte)LABEL_WIDTH);
            Field(valueField, (byte)VALUE_COL, row, (byte)LightCyan, value, (byte)STEP_WIDTH);
        }

        private static void ShowCount()
        {
            Field((byte)FIELD_COUNT, (byte)VALUE_COL, (byte)OVERALL_ROW, (byte)LightCyan,
                StrUtil.PadLeft(StrUtil.NumStr(st.finished), 2) + "/" + StrUtil.NumStr(14), (byte)COUNT_WIDTH);
        }

        private static void ShowBar(ushort px)
        {
            if (px > BAR_PX)
                px = (ushort)BAR_PX;
            if (px == st.barPx) return;

            ushort first, last;
            if (px > st.barPx)
            {
                first = (ushort)(st.barPx / 8);
                last = (ushort)((px - 1) / 8);
            }
            else
            {
                first = (ushort)(px / 8);
                last = (ushort)((st.barPx - 1) / 8);
            }

            if (last >= BAR_CELLS)
                last = (ushort)(BAR_CELLS - 1);

            for (int c = first; c <= last; c++)
            {
                int fill = px - c * 8;
                if (fill < 0) fill = 0;
                else if (fill > 8) fill = 8;
                ShowFrame((byte)(BAR_COL + c), (byte)OVERALL_ROW, (byte)(FRAME_BAR + fill));
            }
            st.barPx = px;
        }

        private static void ShowEta()
        {
            int secs = MulDiv(st.elapsed, st.total - st.done, st.done);
            if (secs > ETA_MAX_TICKS)
                secs = ETA_MAX_TICKS;
            secs = (secs * 10 + 91) / 182;
            int mins = secs / 60;
            if (mins > ETA_MAX_MINUTES)
            {
                mins = ETA_MAX_MINUTES;
                secs = 59;
            }
            else
            {
                secs = secs % 60;
            }
            string s = StrUtil.ZeroPad2((ushort)secs);
            Field((byte)FIELD_ETA, (byte)VALUE_COL, (byte)ETA_ROW, (byte)LightCyan, StrUtil.NumStr(mins) + ":" + s, (byte)COUNT_WIDTH);
        }

        private static void Tick()
        {
            int now = TextRend.TickCount();
            int delta;
            if (now >= st.lastTick)
                delta = now - st.lastTick;
            else
                delta = now + TICKS_PER_DAY - st.lastTick;

            if (delta < 0 || delta > TICKS_PER_DAY)
                delta = 0;

            st.elapsed = unchecked(st.elapsed + delta);
            st.lastTick = now;

            if (st.elapsed >= ETA_FIRST && st.elapsed - st.etaAt >= ETA_EVERY && st.done > 0)
            {
                st.etaAt = st.elapsed;
                ShowEta();
            }
        }

        private static void SetDone(int units)
        {
            if (units > st.total)
                units = st.total;
            if (units > st.done)
            {
                st.done = units;
                if (st.done >= st.nextPx)
                {
                    int px = MulDiv(BAR_PX, st.done, st.total);
                    ShowBar((ushort)px);
                    st.nextPx = MulDiv(st.total, px + 1, BAR_PX);
                    if (st.nextPx <= st.done)
                        st.nextPx = st.done + 1;
                }
            }
            Tick();
        }

        private static void Step(string lbl, string fileName)
        {
            Status((byte)FIELD_STEP_LABEL, (byte)FIELD_STEP, (byte)STEP_ROW, lbl, fileName);
        }

        private static void Recount()
        {
            int start = unchecked(st.base_ + (int)st.phase * st.familyBytes);
            int part;
            if (st.phaseDone >= st.phaseTotal)
                part = st.familyBytes;
            else
                part = MulDiv(st.familyBytes, st.phaseDone, st.phaseTotal);

            SetDone(start + part);

            if (st.nextPx - start >= st.familyBytes)
                st.phaseNext = st.phaseTotal;
            else
                st.phaseNext = MulDiv(st.nextPx - start, st.phaseTotal, st.familyBytes);

            if (st.phaseNext <= st.phaseDone)
                st.phaseNext = st.phaseDone + 1;
        }

        private static void StartPhase(byte phase, int total)
        {
            st.phase = phase;
            st.phaseDone = 0;
            st.phaseTotal = total;
            Recount();
        }

        private static bool Active()
        {
            if (st == null) return false;
            return !st.readOnly;
        }

        public static void Open(bool[] stale, bool writable)
        {
            st = null;
            if (MaxAvailable == null)
                throw new NotSupportedException();

            long maxAvail = MaxAvailable();
            if (maxAvail < 2460) return;

            st = new TShowState();
            st.art = new TImgBuf();
            st.pic = new TImgBuf();

            Blit.AllocPic(8, 1, FRAMES, 0, ref st.art);
            Blit.AllocPic(8, 1, 1, 0, ref st.pic);

            MakeFrame((byte)(CELL_WAIT - CELL_WAIT), 1, 6, 0, (byte)DarkGray, (byte)Black, true);
            MakeFrame((byte)(CELL_CURRENT - CELL_WAIT), 1, 6, 6, (byte)Yellow, (byte)Black, false);
            MakeFrame((byte)(CELL_DONE - CELL_WAIT), 1, 6, 6, (byte)LightGreen, (byte)Black, false);
            MakeFrame((byte)(CELL_FAILED - CELL_WAIT), 1, 6, 6, (byte)LightRed, (byte)Black, false);
            for (int f = 0; f <= 8; f++)
                MakeFrame((byte)(FRAME_BAR + f), 0, 7, (byte)f, (byte)LightGreen, (byte)DarkGray, false);

            for (int fam = 1; fam <= 14; fam++)
            {
                int n = 0;
                int bytes = 0;
                for (int disk = 1; disk <= SOURCE_DISKS; disk++)
                {
                    st.size[fam, disk] = DataArc.FileLength(GDVars.gameDir + DataArc.SourceName((byte)fam, (byte)disk) + ".DAX");
                    if (st.size[fam, disk] >= 0)
                    {
                        n++;
                        st.place[fam, disk] = (byte)n;
                        bytes = unchecked(bytes + st.size[fam, disk]);
                    }
                }
                if (stale[fam])
                    st.total = unchecked(st.total + 3 * bytes);
                else
                    st.finished++;
                st.listed[fam] = stale[fam] || (n > 0);
            }

            st.lastTick = TextRend.TickCount();
            st.readOnly = !writable;

            TextRend.ClearRect(0, 0, 39, 23);
            Border.DrawBorder(1, (sbyte)TITLE_ROW, 38, (sbyte)TITLE_ROW, 0);
            Border.DrawBorder(1, (sbyte)STATUS_TOP, 38, (sbyte)STATUS_BOTTOM, 0);
            Border.DrawBorder(1, (sbyte)PANEL_TOP, 38, (sbyte)PANEL_BOTTOM, 0);
            TextRend.DisplayCentered(1, 38, (byte)TITLE_ROW, (byte)White, TITLE);

            if (st.readOnly)
            {
                int col = 1 + (38 - (WARN_LABEL.Length + WARN_TEXT.Length)) / 2;
                DrawText((byte)col, (byte)WARN_ROW, (byte)LightRed, WARN_LABEL);
                DrawText((byte)(col + WARN_LABEL.Length), (byte)WARN_ROW, (byte)Yellow, WARN_TEXT);
                TextRend.DisplayCentered(1, 38, (byte)WARN_NEXT_ROW, (byte)Yellow, WARN_NEXT);
            }
            else
            {
                Status((byte)FIELD_MAKE_LABEL, (byte)FIELD_MAKE, (byte)MAKE_ROW, "Creating:", "");
                DrawText((byte)(LABEL_END - LABEL_WIDTH + 1), (byte)OVERALL_ROW, (byte)Yellow, StrUtil.PadLeft("Overall:", (byte)LABEL_WIDTH));
                DrawText((byte)(LABEL_END - LABEL_WIDTH + 1), (byte)ETA_ROW, (byte)Yellow, StrUtil.PadLeft("ETA:", (byte)LABEL_WIDTH));
                ShowCount();
                st.barPx = (ushort)BAR_PX;
                ShowBar(0);
                st.nextPx = 1;
            }

            for (int fam = 1; fam <= 14; fam++)
            {
                if (!stale[fam])
                {
                    SetName((byte)fam, (byte)NAME_SHOWN);
                    SetCells((byte)fam, (byte)CELL_DONE);
                }
                else if (st.readOnly)
                {
                    SetName((byte)fam, (byte)NAME_FAILED);
                    SetCells((byte)fam, (byte)CELL_FAILED);
                }
                else
                {
                    SetName((byte)fam, (byte)NAME_SHOWN);
                    SetCells((byte)fam, (byte)CELL_WAIT);
                }
            }
        }

        public static void StartFamily(byte fam)
        {
            if (!Active() || fam < 1 || fam > 14) return;
            st.family = fam;
            st.familyBytes = 0;
            for (int disk = 1; disk <= SOURCE_DISKS; disk++)
            {
                if (st.size[fam, disk] > 0)
                    st.familyBytes = unchecked(st.familyBytes + st.size[fam, disk]);
            }
            st.base_ = st.done;
            Status((byte)FIELD_MAKE_LABEL, (byte)FIELD_MAKE, (byte)MAKE_ROW, "Creating:", DataArc.MergedName(fam) + ".DAX");
            StartPhase((byte)PHASE_READ, st.familyBytes);
        }

        public static void Reading(byte fam, byte disk)
        {
            if (!Active() || fam < 1 || fam > 14) return;
            for (int d = 1; d <= SOURCE_DISKS; d++)
            {
                if (d < disk)
                    SetCell(fam, (byte)d, (byte)CELL_DONE);
                else if (d == disk)
                    SetCell(fam, (byte)d, (byte)CELL_CURRENT);
            }
            Step("Reading:", DataArc.SourceName(fam, disk) + ".DAX");
            Tick();
        }

        public static void Fixing(byte fam, byte disk)
        {
            if (!Active() || fam < 1 || fam > 14) return;
            SetCells(fam, (byte)CELL_DONE);
            Step("Fixing:", DataArc.SourceName(fam, disk) + ".DAX");
            Tick();
        }

        public static void Writing(int bytes)
        {
            if (!Active()) return;
            SetCells(st.family, (byte)CELL_DONE);
            Step("Writing:", DataArc.MergedName(st.family) + ".DAX");
            StartPhase((byte)PHASE_WRITE, bytes);
        }

        public static void Checking()
        {
            if (!Active()) return;
            Step("Checking:", DataArc.MergedName(st.family) + ".DAX");
            StartPhase((byte)PHASE_CHECK, st.phaseTotal);
        }

        public static void Advance(ushort bytes)
        {
            if (!Active()) return;
            st.phaseDone = unchecked(st.phaseDone + bytes);
            if (st.phaseDone >= st.phaseNext)
                Recount();
            else
                Tick();
        }

        public static void EndFamily(byte fam, bool ok)
        {
            if (!Active() || fam < 1 || fam > 14) return;
            if (ok)
                SetCells(fam, (byte)CELL_DONE);
            else
            {
                SetCells(fam, (byte)CELL_FAILED);
                SetName(fam, (byte)NAME_FAILED);
                Step("", "");
            }
            st.finished++;
            ShowCount();
            SetDone(unchecked(st.base_ + 3 * st.familyBytes));
        }

        public static void Finish(bool failed)
        {
            if (st == null) return;

            if (!st.readOnly)
            {
                SetDone(st.total);
                ShowBar((ushort)BAR_PX);
                Status((byte)FIELD_MAKE_LABEL, (byte)FIELD_MAKE, (byte)MAKE_ROW, "", "");
                Step("", "");
                Field((byte)FIELD_ETA, (byte)VALUE_COL, (byte)ETA_ROW, (byte)LightCyan, "0:00", (byte)COUNT_WIDTH);
            }

            if (failed || st.readOnly)
            {
                if (!st.readOnly && DataArc.FileLength(GDVars.gameDir + LOG_NAME) > 0)
                    TextRend.DisplayCentered(1, 38, (byte)NOTE_ROW, (byte)LightRed, NOTE);

                while (KeyPending())
                    GameIo.DiscardPendingKeys();

                TextRend.ClearBottomLine();
                TextRend.DisplayString(0, 24, (byte)White, PROMPT);

                char key;
                do
                {
                    key = (char)GameIo.ReadGameKey();
                } while (key != '\r');

                TextRend.ClearBottomLine();
            }
            else
            {
                int start = st.elapsed;
                while (st.elapsed - start < HOLD_TICKS)
                    Tick();
            }

            TextRend.ClearScreen();
            Blit.FreePic(ref st.pic);
            Blit.FreePic(ref st.art);
            st = null;
        }
    }
}
