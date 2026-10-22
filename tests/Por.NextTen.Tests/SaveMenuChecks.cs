// SaveMenu checks, written from savemenu.pas. The screen is rebuilt from the glyph/clear hooks and compared with a model
// of the Pascal drawing code; keys are scripted through a fake GameIo host.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Por.Core;

class SmIo : IGameIoHost
{
    public Queue<byte> Keys = new Queue<byte>();
    public bool Pending;
    public Action OnRead;
    public int Reads;
    bool justRead; int starve;
    // true while keys remain, except straight after a nonzero key, so GameIo's drain loop never swallows queued keys
    public bool KeyPressed { get { if (justRead) { justRead = false; return false; } if (Keys.Count == 0 && ++starve > 2000) throw new Exception("menu starved of keys"); return Keys.Count > 0; } }
    public byte ReadKey() { Reads++; if (OnRead != null) OnRead(); if (Keys.Count == 0) throw new Exception("keys exhausted"); byte k = Keys.Dequeue(); justRead = k != 0; return k; }
    public void SoundCommand(ushort c) { }
    public void SoundSuspend() { }
    public void Interrupt(byte n, Types.TInterruptRegisters r) { }
    public void Halt(int c) { throw new Halted(); }
    public void WriteLine(string m) { }
    public TextWriter OpenDebugLog(string p) { throw new NotSupportedException(); }
}

class Scr
{
    public int[,] idx = new int[40, 25];
    public int[,] col = new int[40, 25];
    public Scr() { Clear(); }
    public void Clear() { for (int x = 0; x < 40; x++) for (int y = 0; y < 25; y++) { idx[x, y] = -1; col[x, y] = 0; } }
    public void Glyph(int x, int y, int c, int g) { if (x < 40 && y < 25) { idx[x, y] = g; col[x, y] = c; } }
    public void Rect(int l, int t, int r, int b) { for (int x = l; x <= r; x++) for (int y = t; y <= b; y++) if (x < 40 && y < 25) idx[x, y] = -1; }
    public static int G(char c) { if (c >= 'a' && c <= 'z') c = (char)(c - 32); return c % 64; }
    public void Put(int x, int y, int color, string s) { for (int i = 0; i < s.Length; i++) Glyph(x + i, y, color, G(s[i])); }
    public string Cell(int x, int y) { return (idx[x, y] == -1 || idx[x, y] == 32) ? "" : idx[x, y] + "/" + col[x, y]; }
    public string Diff(Scr o, int y0, int y1)
    {
        for (int y = y0; y <= y1; y++) for (int x = 0; x < 40; x++) if (Cell(x, y) != o.Cell(x, y)) return "cell " + x + "," + y + " got [" + Cell(x, y) + "] expected [" + o.Cell(x, y) + "]";
        return null;
    }
}

class SM { public char L; public bool Used; public string Desc = "", Area = "", Clock = "", Date = ""; public int Time; }

static partial class Tests
{
    static SmIo smio;
    static Scr smscr;
    static List<string> smpics;
    static int smClears;
    static string smDir;
    const string SL = "ABCDEFGHIJQ";

    static void SmSetup()
    {
        smio = new SmIo(); GameIo.Host = smio;
        GDVars.zone0 = new Types.TGbvmZone0State(); GDVars.gameFont = new Types.TFontArr();
        GDVars.horizChoice = 1; GDVars.vertTop = 0; GDVars.bottomLineTaken = false; GDVars.ifcState = 0; GDVars.animData = new Types.TAnimRec();
        MenuCore.AutoAnswer.timeout = 0; MenuCore.listOnly = false; MenuCore.arrowsToCaller = false; MenuCore.enterToCaller = false;
        MenuCore.extraKeys = ""; MenuCore.unmarked = ""; MenuCore.quickKeysToCaller = false; MenuCore.movesToCaller = false;
        smscr = new Scr(); smpics = new List<string>(); smClears = 0;
        GDVars.video.DrawGlyph = (x, y, c, g) => smscr.Glyph(x, y, c, g);
        GDVars.video.ClearRect = (l, t, r, b) => smscr.Rect(l, t, r, b);
        GDVars.video.ClearScreen = () => { smClears++; smscr.Clear(); };
        GDVars.dsCurrent8x8SymbolPtr2 = new Types.TImgBuf { data = new byte[32] };
        var p = new Types.TImgBuf { width = 1, height = 8, count = 31, imgSize = 1 };
        for (int i = 0; i < 31; i++) p.data[i] = (byte)i;
        GDVars.dsSymbolSetPtrs[4] = p;
        GDVars.video.ShowPic = (x, y, pic) => smpics.Add(x + "," + y + "," + pic.data[0]);
        TextRend.ReadBiosTicks = () => 0;
        smDir = Path.Combine(Path.GetTempPath(), "por-sm-" + Guid.NewGuid().ToString("N")) + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(smDir);
        GDVars.dsSaveDirectory = smDir;
    }
    static void SmDone() { try { Directory.Delete(smDir, true); } catch (Exception) { } }

    static byte[] Inf(string desc, string area, int dl = -1, int al = -1, int ver = 1)
    {
        var r = new byte[56];
        r[0] = 4; r[1] = (byte)'S'; r[2] = (byte)'I'; r[3] = (byte)'N'; r[4] = (byte)'F'; r[5] = (byte)ver;
        r[6] = (byte)(dl >= 0 ? dl : desc.Length); for (int i = 0; i < desc.Length && i < 30; i++) r[7 + i] = (byte)desc[i];
        r[37] = (byte)(al >= 0 ? al : area.Length); for (int i = 0; i < area.Length && i < 18; i++) r[38 + i] = (byte)area[i];
        return r;
    }
    static string Fp(string name) { return smDir + name; }
    static string RandTxt(int n) { var c = new char[n]; for (int i = 0; i < n; i++) c[i] = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 .-"[rng.Next(39)]; return new string(c); }
    static int Dos(DateTime d) { return unchecked((int)((((uint)(d.Year - 1980) << 9 | (uint)d.Month << 5 | (uint)d.Day) << 16) | ((uint)d.Hour << 11 | (uint)d.Minute << 5 | (uint)(d.Second / 2)))); }
    static string Fit(string s, int n) { return s.Length > n ? s.Substring(0, n) : s; }
    static string P2(int n) { return n.ToString("00"); }

    static void SaveMenuInfo()
    {
        SmSetup();
        try
        {
            // write: exact bytes
            foreach (var d in new[] { "", "A", "SAVE GAME A", RandTxt(30), RandTxt(40) })
                foreach (var a in new[] { "", "TEMPLE", RandTxt(18), RandTxt(25) })
                {
                    SaveMenu.WriteSlotInfo((byte)'C', d, a);
                    byte[] got = File.ReadAllBytes(Directory.GetFiles(smDir).Single(f => Path.GetFileName(f).ToLower() == "savgamc.inf"));
                    string dd = Fit(d, 30), aa = Fit(a, 18);
                    var exp = Inf(dd, aa);
                    Eq(56, got.Length, "inf size");
                    Yes(got.SequenceEqual(exp), "inf bytes " + d + "|" + a);
                    string rd, ra;
                    Yes(SaveMenu.ReadSlotInfo((byte)'C', out rd, out ra), "read back");
                    EqS(dd, rd, "desc"); EqS(aa, ra, "area");
                }
            // read: validity rules
            Action<string, byte[], bool> rd1 = (what, bytes, ok) =>
            {
                File.WriteAllBytes(Fp("savgamD.inf"), bytes);
                string d = "x", a = "y";
                bool r = SaveMenu.ReadSlotInfo((byte)'D', out d, out a);
                Eq(ok ? 1 : 0, r ? 1 : 0, what);
                if (!ok) { EqS("", d, what + " desc empty"); EqS("", a, what + " area empty"); }
            };
            rd1("valid", Inf("HELLO", "AREA"), true);
            rd1("max lens", Inf(RandTxt(30), RandTxt(18)), true);
            rd1("ver0", Inf("H", "A", -1, -1, 0), false);
            rd1("ver2", Inf("H", "A", -1, -1, 2), false);
            rd1("dlen31", Inf("H", "A", 31), false);
            rd1("dlen255", Inf("H", "A", 255), false);
            rd1("dlen30", Inf("H", "A", 30), true);
            rd1("alen19", Inf("H", "A", -1, 19), false);
            rd1("alen18", Inf("H", "A", -1, 18), true);
            for (int i = 0; i < 5; i++) { var b = Inf("H", "A"); b[i] ^= 0x20; rd1("sig byte " + i, b, false); }
            { var b = Inf("H", "A"); b[0] = 3; rd1("siglen3", b, false); }
            { var b = Inf("H", "A"); b[5] = 1; b[40] = 77; b[20] = 99; rd1("garbage past strings", b, true); }
            rd1("short55", new byte[55], false);
            rd1("long57", Inf("H", "A").Concat(new byte[] { 0 }).ToArray(), false);
            rd1("empty", new byte[0], false);
            string d0, a0; Yes(!SaveMenu.ReadSlotInfo((byte)'Q', out d0, out a0), "missing"); EqS("", d0, "m d"); EqS("", a0, "m a");
            // reading content
            File.WriteAllBytes(Fp("savgamD.inf"), Inf("MY DESC", "MY AREA"));
            Yes(SaveMenu.ReadSlotInfo((byte)'D', out d0, out a0) && d0 == "MY DESC" && a0 == "MY AREA", "content");
        }
        finally { SmDone(); }
    }

    static void SaveMenuTime()
    {
        for (int n = 0; n < 20000; n++)
        {
            var d = new DateTime(1980 + rng.Next(128), 1 + rng.Next(12), 1 + rng.Next(28), rng.Next(24), rng.Next(60), rng.Next(60));
            int p = SaveMenu.PackDosTime(d);
            Eq(Dos(d), p, "pack " + d);
            int y, mo, dy, h, mi, s;
            SaveMenu.UnpackDosTime(p, out y, out mo, out dy, out h, out mi, out s);
            Eq(d.Year, y, "year"); Eq(d.Month, mo, "month"); Eq(d.Day, dy, "day"); Eq(d.Hour, h, "hour"); Eq(d.Minute, mi, "min"); Eq(d.Second / 2 * 2, s, "sec");
        }
        int yy, m2, d2, h2, i2, s2;
        SaveMenu.UnpackDosTime(0, out yy, out m2, out d2, out h2, out i2, out s2);
        Eq(1980, yy, "zero year"); Eq(0, m2, "zero month");
    }

    // ---- slot fixtures and model
    static List<SM> MakeSlots(bool saving, Dictionary<char, SM> all, int yearHi)
    {
        all.Clear();
        foreach (char L in SL)
        {
            var s = new SM { L = L };
            if (rng.Next(2) == 0)
            {
                s.Used = true;
                int len = new[] { 0, 100, 396, 486, 487, 600 }[rng.Next(6)];
                var dat = new byte[len]; rng.NextBytes(dat);
                File.WriteAllBytes(Fp("savgam" + L + ".dat"), dat);
                var dt = new DateTime(1985 + rng.Next(yearHi - 1985), 1 + rng.Next(12), 1 + rng.Next(28), rng.Next(24), rng.Next(60), 2 * rng.Next(30));
                File.SetLastWriteTime(Fp("savgam" + L + ".dat"), dt);
                s.Time = Dos(dt); s.Clock = P2(dt.Hour) + ":" + P2(dt.Minute); s.Date = dt.Year + "/" + P2(dt.Month) + "/" + P2(dt.Day);
                int v = rng.Next(4);
                if (v == 0) { s.Desc = RandTxt(rng.Next(31)); s.Area = RandTxt(rng.Next(19)); File.WriteAllBytes(Fp("savgam" + L + ".inf"), Inf(s.Desc, s.Area)); }
                else
                {
                    if (v == 1) File.WriteAllBytes(Fp("savgam" + L + ".inf"), Inf("X", "Y", 31));
                    s.Desc = "";
                    s.Area = len < 487 ? "UNKNOWN AREA" : Fit(AreaName.AreaFor(dat[1 + 484], dat[1 + 394]), 18);
                }
            }
            all[L] = s;
        }
        return SL.Where(c => saving || all[c].Used).Select(c => all[c]).ToList();
    }
}
