using System;
using static Por.Core.Types;

namespace Por.Core
{
    public static class TextRend
    {
        // Desktop host supplies BIOS ticks and CRT-style delay; no simulated default.
        public static Func<int> ReadBiosTicks;
        public static Action<ushort> DelayMilliseconds;

        private static string ShortString(string value, int capacity)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            return value.Length > capacity ? value.Substring(0, capacity) : value;
        }

        public static void LoadGameFont()
        {
            ushort compSize;
            byte[] dataPtr;
            DaxIo.LoadDax("8X8d1", ".dax", 201, out compSize, out dataPtr);
            if (compSize == 0) return;
            if (GDVars.gameFont == null) throw new InvalidOperationException("gameFont is null");
            int n = compSize;
            int maxN = 177 * 8;
            if (n > maxN) n = maxN;
            for (int i = 0; i < n; i++)
            {
                int row = i / 8;
                int col = i % 8;
                GDVars.gameFont.Data[row][col] = dataPtr[i];
            }
        }

        public static void RenderChar(byte col, byte row, byte color, byte count, byte ch, byte isASCII)
        {
            if (col > 39 || row > 24 || GDVars.gameFont == null) return;
            byte charIdx;
            if (isASCII != 0)
            {
                char glyphChar = (char)ch;
                if (glyphChar >= 'a' && glyphChar <= 'z') glyphChar = (char)(glyphChar - 32);
                charIdx = (byte)(glyphChar % 64);
            }
            else
            {
                if (ch >= 177) return;
                charIdx = ch;
            }
            int c = col;
            for (int i = 1; i <= count; i++)
            {
                GDVars.video.DrawGlyph(unchecked((byte)c), row, color, charIdx);
                c++;
            }
        }

        public static void PrintSpaces(byte col, byte row, byte color, byte count)
        {
            if (col < 0 || col > 39 || row < 0 || row > 24) return;
            RenderChar(col, row, color, count, 32, 1);
        }

        public static void ClearBottomLine()
        {
            PrintSpaces(0, 24, 0, 40);
        }

        public static void DisplayString(byte col, byte row, byte color, string source)
        {
            source = ShortString(source, 255);
            if (col > 39 || row > 39) return;
            int n = source.Length;
            int c = col;
            for (int i = 0; i < n; i++)
            {
                RenderChar(unchecked((byte)c), row, color, 1, unchecked((byte)source[i]), 1);
                c++;
            }
        }

        public static void DisplayCentered(byte left, byte right, byte row, byte color, string s)
        {
            s = ShortString(s, 255);
            byte width = unchecked((byte)(right - left + 1));
            if (s.Length > width)
            {
                s = s.Substring(0, width);
            }
            int offset = (width - s.Length) / 2;
            DisplayString(unchecked((byte)(left + offset)), row, color, s);
        }

        public static void DisplayMessageWindow(byte ulCol, byte ulRow, byte lrCol, byte lrRow, byte color, bool clearFirst, string source)
        {
            source = ShortString(source, 255);
            if (ulCol > lrCol || ulRow > lrRow || lrCol > 39 || lrRow > 24) return;
            if (GDVars.curCol < ulCol || GDVars.curCol > lrCol || GDVars.curRow < ulRow || GDVars.curRow > lrRow)
            {
                GDVars.curCol = ulCol;
                GDVars.curRow = ulRow;
            }
            if (clearFirst)
            {
                ClearRect((sbyte)ulCol, (sbyte)ulRow, (sbyte)lrCol, (sbyte)lrRow);
                GDVars.curCol = ulCol;
                GDVars.curRow = ulRow;
            }
            int idx = 1;
            int len = source.Length;
            if (len == 0) return;
            int width = lrCol - ulCol + 1;
            while (true)
            {
                int lineEnd = idx;
                while (lineEnd < len && GDConsts.breakChars.Contains(source[lineEnd - 1]))
                    lineEnd++;
                while (lineEnd < len && !GDConsts.breakChars.Contains(source[lineEnd - 1]) && source[lineEnd - 1] != ' ')
                    lineEnd++;
                if (source[lineEnd - 1] != ' ')
                {
                    while (lineEnd + 1 < len && GDConsts.breakChars.Contains(source[lineEnd]))
                        lineEnd++;
                }
                if (GDVars.curCol <= lrCol && (lineEnd - idx + (source[lineEnd - 1] != ' ' ? 1 : 0) > width))
                {
                    lineEnd = idx + lrCol - GDVars.curCol;
                }
                if (lineEnd - idx + GDVars.curCol > lrCol)
                {
                    if (lineEnd - idx + GDVars.curCol == lrCol + 1 && source[lineEnd - 1] == ' ')
                    {
                        lineEnd--;
                        PrintCharInWindow(ref idx, lineEnd, source, color, lrCol);
                    }
                    GDVars.curCol = ulCol;
                    GDVars.curRow++;
                    while (idx < len && source[idx - 1] == ' ')
                        idx++;
                    if (GDVars.curRow > lrRow && (idx < len || source[idx - 1] != ' '))
                    {
                        GDVars.curCol = ulCol;
                        GDVars.curRow = ulRow;
                        DisplayAndPause(13, "Press any key to continue");
                        ClearRect((sbyte)ulCol, (sbyte)ulRow, (sbyte)lrCol, (sbyte)lrRow);
                        PrintCharInWindow(ref idx, lineEnd, source, color, lrCol);
                    }
                    else if (GDVars.curRow > lrRow)
                    {
                        return;
                    }
                }
                else
                {
                    PrintCharInWindow(ref idx, lineEnd, source, color, lrCol);
                }
                if (idx > len) break;
            }
            if (GDVars.curCol > lrCol)
            {
                GDVars.curCol = ulCol;
                GDVars.curRow++;
            }
        }

        private static void PrintCharInWindow(ref int idx, int lineEnd, string source, byte color, byte lrCol)
        {
            while (idx <= lineEnd && GDVars.curCol <= lrCol)
            {
                RenderChar(GDVars.curCol, GDVars.curRow, color, 1, (byte)source[idx - 1], 1);
                if (GDVars.msgDelayFlag != 0)
                {
                    ushort delay = (ushort)(GDVars.gameDelay * 10);
                    Delay(delay);
                }
                idx++;
                GDVars.curCol++;
            }
        }

        public static void ShowMessage(byte color, bool clearFirst, string s)
        {
            DisplayMessageWindow(C_Msg.TEXT_LEFT, C_Msg.MESSAGE_TOP, C_Msg.TEXT_RIGHT, C_Msg.TEXT_BOTTOM, color, clearFirst, s);
        }

        public static string ReadString(string query, byte color, byte maxLen)
        {
            query = ShortString(query, 40);
            ClearBottomLine();
            DisplayString(0, 24, color, query);
            int col = query.Length;
            string reply = "";
            char key;
            do
            {
                key = (char)GameIo.ReadGameKey();
                if (key >= ' ' && key <= 'z')
                {
                    if (reply.Length < maxLen)
                    {
                        reply += key;
                        col++;
                        DisplayString(unchecked((byte)col), 24, 15, key.ToString());
                    }
                }
                else if (key == C_Keys.BACKSPACE)
                {
                    if (reply.Length > 0)
                    {
                        reply = reply.Substring(0, reply.Length - 1);
                        PrintSpaces(unchecked((byte)col), 24, 0, 1);
                        col--;
                    }
                }
            } while (key != C_Keys.ENTER && key != C_Keys.ESC);
            ClearBottomLine();
            char[] chars = reply.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i] >= 'a' && chars[i] <= 'z')
                    chars[i] = (char)(chars[i] - 32);
            }
            reply = new string(chars);
            if (reply.Length > 40) reply = reply.Substring(0, 40);
            return reply;
        }

        public static ushort InputUint16(string query, byte color)
        {
            while (true)
            {
                string reply = ReadString(query, color, 6);
                int num;
                int code;
                if (TryParseVal(reply, out num, out code) && code == 0 && num >= 0 && num <= 65535)
                {
                    return (ushort)num;
                }
            }
        }

        private static bool TryParseVal(string s, out int value, out int code)
        {
            value = 0;
            code = 0;
            int i = 0;
            int len = s.Length;
            while (i < len && s[i] == ' ') i++;
            if (i >= len) { code = 1; return false; }
            bool negative = false;
            if (s[i] == '+' || s[i] == '-')
            {
                negative = (s[i] == '-');
                i++;
            }
            if (i >= len) { code = 1; return false; }
            int base_ = 10;
            if (s[i] == '$')
            {
                base_ = 16;
                i++;
            }
            if (i >= len) { code = 1; return false; }
            long val = 0;
            bool hasDigit = false;
            while (i < len)
            {
                char c = s[i];
                int d;
                if (c >= '0' && c <= '9') d = c - '0';
                else if (c >= 'a' && c <= 'f') d = c - 'a' + 10;
                else if (c >= 'A' && c <= 'F') d = c - 'A' + 10;
                else break;
                if (d >= base_) break;
                val = val * base_ + d;
                if (val > 2147483647L) { code = 1; return false; }
                hasDigit = true;
                i++;
            }
            if (!hasDigit) { code = 1; return false; }
            if (i < len) { code = 1; return false; }
            if (negative) val = -val;
            if (val < int.MinValue || val > int.MaxValue) { code = 1; return false; }
            value = (int)val;
            return true;
        }

        public static void DisplayAndPause(byte color, string msg)
        {
            msg = ShortString(msg, 40);
            ClearBottomLine();
            DisplayString(0, 24, color, msg);
            GameIo.ReadGameKey();
        }

        public static int TickCount()
        {
            if (ReadBiosTicks == null) throw new NotSupportedException();
            int again = ReadBiosTicks();
            int now;
            do
            {
                now = again;
                again = ReadBiosTicks();
            } while (now != again);
            return now;
        }

        public static int GetTicks()
        {
            int now = TickCount();
            return (now / 10000) * 54925 + (now % 10000) * 54925 / 10000;
        }

        public static void ClearScreen()
        {
            GDVars.video.ClearScreen();
        }

        public static void PrintAndPause(string msg, byte color)
        {
            msg = ShortString(msg, 40);
            ClearBottomLine();
            DisplayString(0, 24, color, msg);
            PauseOnly();
            ClearBottomLine();
        }

        public static void PauseOnly()
        {
            if (DelayMilliseconds == null) throw new NotSupportedException();
            ushort delay = (ushort)(GDVars.gameDelay * 225);
            DelayMilliseconds(delay);
        }

        public static void ClearRect(sbyte ulCol, sbyte ulRow, sbyte lrCol, sbyte lrRow)
        {
            if (ulCol < 0 || ulRow < 0 || ulCol > lrCol || ulRow > lrRow || lrCol > 39 || lrRow > 24) return;
            GDVars.video.ClearRect((byte)ulCol, (byte)ulRow, (byte)lrCol, (byte)lrRow);
        }

        private static void Delay(ushort ms)
        {
            if (DelayMilliseconds == null) throw new NotSupportedException();
            DelayMilliseconds(ms);
        }
    }
}
