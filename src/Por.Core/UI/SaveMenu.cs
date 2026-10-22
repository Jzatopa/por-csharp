// Source: Talonius pool-of-radiance pinned 7616920a (src/savemenu.pas); Qwen3.8:27b initial draft, supervisor repaired.
using System;
using System.IO;
using System.Text;


namespace Por.Core
{
    public static class SaveMenu
    {
        public const string SLOT_LETTERS = "ABCDEFGHIJQ";
        public const int DESC_LEN = 30;
        public const int AREA_LEN = 18;
        public const int DESC_SHOWN = 27;

        private const string INFO_SIG = "SINF";
        private const int INFO_VERSION = 1;
        private const byte TITLE_ROW = 1;
        private const byte LIST_TOP = 3;
        private const byte LIST_BOTTOM = 22;
        private const byte VISIBLE = 6;
        private const byte LEFT_COL = 2;
        private const byte DESC_COL = 5;
        private const byte RIGHT_COL = 37;
        private const byte POP_DESC_ROW = 8;
        private const byte OPT_NEXT = 1;
        private const byte OPT_PREV = 2;
        private const byte OPT_TAKE = 3;
        private const byte OPT_EXIT = 4;

        private const byte White = 15;
        private const byte LightGreen = 10;
        private const byte LightMagenta = 13;
        private const byte Yellow = 14;

        private const int PREV_SCRIPT_OFS = 484;
        private const int MAP_ID_OFS = 394;

        private static char[] listedChars = new char[17];
        private static byte count;
        private static bool[] used = new bool[17];
        private static string[] descs = new string[17];
        private static string[] areas = new string[17];
        private static string[] clocks = new string[17];
        private static string[] dates = new string[17];
        private static int[] times = new int[17];
        private static byte sel;
        private static byte top;

        internal static Func<string, int> MakeDir = (path) =>
        {
            if (System.IO.Directory.Exists(path)) return 5;
            string parent = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parent) && !System.IO.Directory.Exists(parent)) return 3;
            try { System.IO.Directory.CreateDirectory(path); return 0; }
            catch (Exception) { return 3; }
        };

        private static string Fit(string s, int n)
        {
            if (s == null) return "";
            if (s.Length <= n) return s;
            return s.Substring(0, n);
        }

        private static string SlotFile(byte letter, string ext)
        {
            return GDVars.dsSaveDirectory + "savgam" + (char)letter + ext;
        }

        private static string Listed()
        {
            if (count == 0) return "";
            return new string(listedChars, 1, count);
        }

        private static int Pos(byte start, string s)
        {
            if (s == null || s.Length == 0) return 0;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == (char)start) return i + 1;
            }
            return 0;
        }

        private static void EncodeSlotInfo(byte[] rec, string desc, string area)
        {
            for (int i = 0; i < 56; i++) rec[i] = 0;
            rec[0] = 4;
            rec[1] = (byte)'S';
            rec[2] = (byte)'I';
            rec[3] = (byte)'N';
            rec[4] = (byte)'F';
            rec[5] = (byte)INFO_VERSION;
            int dLen = Math.Min(desc.Length, DESC_LEN);
            rec[6] = (byte)dLen;
            for (int i = 0; i < dLen; i++) rec[7 + i] = (byte)desc[i];
            int aLen = Math.Min(area.Length, AREA_LEN);
            rec[37] = (byte)aLen;
            for (int i = 0; i < aLen; i++) rec[38 + i] = (byte)area[i];
        }

        private static bool DecodeSlotInfo(byte[] rec, out string desc, out string area)
        {
            desc = "";
            area = "";
            if (rec.Length < 56) return false;
            if (rec[0] != 4) return false;
            if (rec[1] != (byte)'S' || rec[2] != (byte)'I' || rec[3] != (byte)'N' || rec[4] != (byte)'F') return false;
            if (rec[5] != (byte)INFO_VERSION) return false;
            int dLen = rec[6];
            if (dLen > DESC_LEN) return false;
            int aLen = rec[37];
            if (aLen > AREA_LEN) return false;
            char[] dc = new char[dLen];
            for (int i = 0; i < dLen; i++) dc[i] = (char)rec[7 + i];
            desc = new string(dc);
            char[] ac = new char[aLen];
            for (int i = 0; i < aLen; i++) ac[i] = (char)rec[38 + i];
            area = new string(ac);
            return true;
        }

        public static bool ReadSlotInfo(byte letter, out string desc, out string area)
        {
            desc = "";
            area = "";
            byte[] rec = new byte[56];
            if (!FileUtil.ReadRecord(SlotFile(letter, ".inf"), rec, 56)) return false;
            if (!DecodeSlotInfo(rec, out desc, out area))
            {
                desc = "";
                area = "";
                return false;
            }
            return true;
        }

        public static void WriteSlotInfo(byte letter, string desc, string area)
        {
            byte[] rec = new byte[56];
            EncodeSlotInfo(rec, desc, area);
            FileUtil.WriteRecord(SlotFile(letter, ".inf"), rec, 56);
        }

        internal static int PackDosTime(DateTime dt)
        {
            return unchecked((int)(((uint)(dt.Year - 1980) << 25) | ((uint)dt.Month << 21) | ((uint)dt.Day << 16) | ((uint)dt.Hour << 11) | ((uint)dt.Minute << 5) | ((uint)(dt.Second / 2))));
        }

        internal static void UnpackDosTime(int t, out int year, out int month, out int day, out int hour, out int min, out int sec)
        {
            uint u = unchecked((uint)t);
            year = (int)((u >> 25) & 0x7F) + 1980;
            month = (int)((u >> 21) & 15);
            day = (int)((u >> 16) & 31);
            hour = (int)((u >> 11) & 31);
            min = (int)((u >> 5) & 63);
            sec = (int)(u & 31) * 2;
        }

        private static void ReadSlot(byte i, byte letter)
        {
            listedChars[i] = (char)letter;
            used[i] = false;
            times[i] = 0;
            clocks[i] = "";
            dates[i] = "";
            descs[i] = "";
            areas[i] = "";
            FileStream f = null;
            if (FileUtil.OpenReadOnly(out f, SlotFile(letter, ".dat")))
            {
                used[i] = true;
                DateTime dt = System.IO.File.GetLastWriteTime(f.Name);
                int t = PackDosTime(dt);
                times[i] = t;
                UnpackDosTime(t, out int year, out int month, out int day, out int hour, out int min, out int sec);
                clocks[i] = Fit(StrUtil.ZeroPad2((ushort)hour) + ":" + StrUtil.ZeroPad2((ushort)min), 5);
                dates[i] = Fit(year.ToString() + "/" + StrUtil.ZeroPad2((ushort)month) + "/" + StrUtil.ZeroPad2((ushort)day), 10);
                if (!ReadSlotInfo(letter, out string d, out string a))
                {
                    f.Seek(1 + PREV_SCRIPT_OFS, System.IO.SeekOrigin.Begin);
                    byte[] buf1 = new byte[2];
                    int n1 = f.Read(buf1, 0, 2);
                    f.Seek(1 + MAP_ID_OFS, System.IO.SeekOrigin.Begin);
                    byte[] buf2 = new byte[2];
                    int n2 = f.Read(buf2, 0, 2);
                    if (n1 == 2 && n2 == 2)
                    {
                        byte script = buf1[0];
                        byte mapWord = buf2[0];
                        areas[i] = Fit(AreaName.AreaFor(script, mapWord), AREA_LEN);
                    }
                    else
                    {
                        areas[i] = Fit("UNKNOWN AREA", AREA_LEN);
                    }
                }
                else
                {
                    descs[i] = Fit(d, DESC_LEN);
                    areas[i] = Fit(a, AREA_LEN);
                }
                f.Close();
            }
        }

        private static void DrawMore(byte row)
        {
            TextRend.DisplayCentered(1, 38, row, White, "(more)");
        }

        private static void DrawList()
        {
            Border.ClearBox(1, (sbyte)LIST_TOP, 38, (sbyte)LIST_BOTTOM);
            if (top > 1)
                DrawMore(LIST_TOP);
            for (byte k = 0; k < VISIBLE; k++)
            {
                byte i = unchecked((byte)(top + k));
                if (i <= count)
                {
                    byte color = (i == sel) ? White : LightGreen;
                    byte row = unchecked((byte)(LIST_TOP + 1 + 3 * k));
                    TextRend.DisplayString(LEFT_COL, row, White, listedChars[i].ToString());
                    TextRend.DisplayString(unchecked((byte)(LEFT_COL + 1)), row, color, ":");
                    if (used[i])
                    {
                        TextRend.DisplayString(DESC_COL, row, color, Fit(descs[i], DESC_SHOWN));
                        TextRend.DisplayString(unchecked((byte)(RIGHT_COL + 1 - clocks[i].Length)), row, color, clocks[i]);
                        TextRend.DisplayString(DESC_COL, unchecked((byte)(row + 1)), color, areas[i]);
                        TextRend.DisplayString(unchecked((byte)(RIGHT_COL + 1 - dates[i].Length)), unchecked((byte)(row + 1)), color, dates[i]);
                    }
                    else
                    {
                        TextRend.DisplayString(DESC_COL, row, color, "EMPTY");
                    }
                }
            }
            if (top + VISIBLE - 1 < count)
                DrawMore(LIST_BOTTOM);
        }

        private static void MoveTo(int i)
        {
            if (i < 1)
                i = count;
            else if (i > count)
                i = 1;
            sel = unchecked((byte)i);
            if (sel < top)
                top = sel;
            if (sel > top + VISIBLE - 1)
                top = unchecked((byte)(sel - VISIBLE + 1));
            DrawList();
        }

        public static byte ChooseSlot(bool saving, byte start)
        {
            byte chosen = 0;
            string letters = SLOT_LETTERS;
            count = 0;
            for (int i = 1; i <= letters.Length; i++)
            {
                ReadSlot(unchecked((byte)(count + 1)), unchecked((byte)letters[i - 1]));
                if (saving || used[unchecked((byte)(count + 1))])
                {
                    count = unchecked((byte)(count + 1));
                }
            }
            if (count == 0)
            {
                return 0;
            }
            sel = 1;
            int newest = 0;
            for (int i = 1; i <= count; i++)
            {
                if (used[i] && ((newest == 0) || (times[i] > newest)))
                {
                    newest = times[i];
                    sel = unchecked((byte)i);
                }
            }
            int pos = Pos(start, Listed());
            if (pos > 0)
            {
                sel = unchecked((byte)pos);
            }
            top = 1;
            TextRend.ClearScreen();
            Border.DrawBorder(1, (sbyte)LIST_TOP, 38, (sbyte)LIST_BOTTOM, 0);
            Border.DrawBorder(1, (sbyte)TITLE_ROW, 38, (sbyte)TITLE_ROW, 0);
            TextRend.DisplayString(unchecked((byte)(1 + (38 - 19) / 2)), TITLE_ROW, White, "Load/Save Game Menu");
            MoveTo(sel);
            string action = saving ? "Save" : "Load";
            chosen = (byte)' ';
            GDVars.horizChoice = OPT_TAKE;
            while (true)
            {
                MenuCore.extraKeys = Listed();
                MenuCore.unmarked = "E";
                MenuCore.enterToCaller = true;
                bool ext = false;
                byte ch = MenuCore.MenuHorizontal("Game: ", "Next Prev " + action + " Exit", LightMagenta, LightGreen, White, true, false, ref ext);
                if (!ext && ch == C_Keys.ENTER)
                {
                    switch (GDVars.horizChoice)
                    {
                        case OPT_NEXT:
                            ch = (byte)'N';
                            break;
                        case OPT_PREV:
                            ch = (byte)'P';
                            break;
                        case OPT_TAKE:
                            ch = unchecked((byte)action[0]);
                            break;
                        default:
                            ch = C_Keys.NONE;
                            break;
                    }
                }
                if (ext)
                {
                    switch (ch)
                    {
                        case C_Keys.UP:
                        case C_Keys.HOME:
                            MoveTo(sel - 1);
                            break;
                        case C_Keys.DOWN:
                        case C_Keys.END_KEY:
                            MoveTo(sel + 1);
                            break;
                        case C_Keys.PGUP:
                            if (sel > VISIBLE)
                            {
                                MoveTo(sel - VISIBLE);
                            }
                            else
                            {
                                MoveTo(1);
                            }
                            break;
                        case C_Keys.PGDN:
                            if (sel + VISIBLE < count)
                            {
                                MoveTo(sel + VISIBLE);
                            }
                            else
                            {
                                MoveTo(count);
                            }
                            break;
                    }
                }
                else if (ch == C_Keys.NONE)
                {
                    chosen = C_Keys.NONE;
                }
                else if (ch != (byte)' ' && Pos(ch, Listed()) > 0)
                {
                    chosen = ch;
                }
                else if (ch == (byte)'N')
                {
                    MoveTo(sel + 1);
                }
                else if (ch == (byte)'P')
                {
                    MoveTo(sel - 1);
                }
                else if (ch == unchecked((byte)action[0]))
                {
                    chosen = unchecked((byte)Listed()[sel - 1]);
                }
                if (chosen != (byte)' ')
                {
                    break;
                }
            }
            TextRend.ClearBottomLine();
            return chosen;
        }

        private static void DrawDescLine(string shown)
        {
            TextRend.DisplayString(LEFT_COL, POP_DESC_ROW, White, shown);
            for (int i = shown.Length + 1; i <= DESC_SHOWN; i++)
                TextRend.DisplayString(unchecked((byte)(LEFT_COL + i - 1)), POP_DESC_ROW, LightGreen, ".");
        }

        public static bool AskDescription(byte letter, string area, ref string desc)
        {
            area = Fit(area, AREA_LEN);
            Border.ClearBox(1, (sbyte)LIST_TOP, 38, (sbyte)LIST_BOTTOM);
            string defaultDesc = "SAVE GAME " + (char)letter;
            TextRend.DisplayString(LEFT_COL, LIST_TOP + 1, LightGreen, defaultDesc);
            TextRend.DisplayString(LEFT_COL, LIST_TOP + 2, LightGreen, "Location: " + area);
            TextRend.DisplayString(LEFT_COL, LIST_TOP + 4, LightGreen, "Description:");
            string s = desc.Length > DESC_SHOWN ? desc.Substring(0, DESC_SHOWN) : desc;
            TextRend.ClearBottomLine();
            TextRend.DisplayString(0, 24, LightMagenta, "Save: ");
            TextRend.DisplayString(6, 24, LightGreen, "Exit");
            byte key;
            do
            {
                if (s.Length == 0)
                    DrawDescLine(defaultDesc);
                else
                    DrawDescLine(s);
                key = GameIo.ReadGameKey();
                if (key == 0)
                {
                    key = GameIo.ReadGameKey();
                }
                else if (key >= ' ' && key <= 'z')
                {
                    if (s.Length < DESC_SHOWN)
                    {
                        char c = (char)key;
                        if (c >= 'a' && c <= 'z') c = (char)(c - 'a' + 'A');
                        s += c;
                    }
                }
                else if (key == C_Keys.BACKSPACE)
                {
                    if (s.Length > 0)
                        s = s.Substring(0, s.Length - 1);
                }
            } while (key != C_Keys.ENTER && key != C_Keys.ESC);
            TextRend.ClearBottomLine();
            if (s.Length == 0)
                s = defaultDesc;
            if (key == C_Keys.ENTER)
                desc = Fit(s, DESC_LEN);
            return key == C_Keys.ENTER;
        }

        public static string SaveDiskMsg()
        {
            string dir = GDVars.dsSaveDirectory;
            char drive = dir.Length > 0 ? dir[0] : (char)0;
            return "Put save disk in " + drive + ":";
        }

        public static void CheckSaveDir()
        {
            string dir = GDVars.dsSaveDirectory;
            if (dir.Length <= 3) return;
            int ioRes;
            do
            {
                for (int i = 4; i <= dir.Length - 1; i++)
                {
                    if (dir[i - 1] == '\\' || dir[i - 1] == '/')
                    {
                        ioRes = MakeDir(dir.Substring(0, i - 1));
                    }
                }
                ioRes = MakeDir(dir.Substring(0, dir.Length - 1));
                if (ioRes != 0 && ioRes != 5)
                    TextRend.DisplayAndPause(Yellow, SaveDiskMsg());
            } while (ioRes != 0 && ioRes != 5);
        }
    }
}
