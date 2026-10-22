// CharDisp checks, written from chardisp.pas. Each routine is run against an oracle script that performs the primitive
// drawing calls (TextRend/Border) with the arguments the Pascal source gives; screens, symbol pictures, logged seam calls
// and key reads must agree.
using System;
using System.Collections.Generic;
using System.Linq;
using Por.Core;

class FakeCbt : ICombatMapHost
{
    public List<string> Log = new List<string>();
    public bool Visible = true; public int CellX = 5, CellY = 6; public byte Slot = 1;
    public void SetCombatPalette() { Log.Add("palette"); }
    public bool ViewCoordsValid(sbyte x, sbyte y) { return x >= 0 && x <= 6 && y >= 0 && y <= 6; }
    public bool CombatIconVisible(Types.TCharRecord c, byte whole) { Log.Add("visible:" + whole); return Visible; }
    public void ScrollView(sbyte x, sbyte y, byte d, byte dir)
    {
        Log.Add("scroll:" + x + ":" + y + ":" + d + ":" + dir);
        GDVars.arenaPtr.ox = unchecked((sbyte)(x - 3)); GDVars.arenaPtr.oy = unchecked((sbyte)(y - 3));
    }
    public sbyte CombatCellX(Types.TCharRecord c) { return (sbyte)CellX; }
    public sbyte CombatCellY(Types.TCharRecord c) { return (sbyte)CellY; }
    public byte CombatSlot(Types.TCharRecord c) { return Slot; }
}

class CdRun
{
    public Scr scr; public List<string> pics, log, snaps = new List<string>(); public int reads;
}

static partial class Tests
{
    static List<string> cdLog;
    static FakeCbt cdCbt;
    const int LR = 12, LC = 11, BK = 0;

    static string SnapOf(Scr s)
    {
        var sb = new System.Text.StringBuilder();
        for (int y = 0; y < 25; y++) for (int x = 0; x < 40; x++) { string c = s.Cell(x, y); if (c != "") sb.Append(x + "," + y + "=" + c + ";"); }
        return sb.ToString();
    }
    static string Nm(Types.TImgBuf b)
    {
        if (b == null) return "null"; if (ReferenceEquals(b, GDVars.dsMoveIconSheet)) return "sheet";
        if (ReferenceEquals(b, GDVars.dsIconImageBufferPtr)) return "icon"; if (ReferenceEquals(b, GDVars.dsViewImageBufferPtr)) return "view"; return "other";
    }
    static void CdSetup()
    {
        SmSetup(); SmDone();
        cdLog = new List<string>(); cdCbt = new FakeCbt(); cdCbt.Log = cdLog; CharDisp.CombatMap = cdCbt;
        GDVars.dsMoveIconSheet = new Types.TImgBuf { data = new byte[16] }; GDVars.dsIconImageBufferPtr = new Types.TImgBuf { data = new byte[16] }; GDVars.dsViewImageBufferPtr = new Types.TImgBuf { data = new byte[16] };
        CharDisp.BlitClipped = (x, y, t, f, g, d) => cdLog.Add("blit:" + x + ":" + y + ":" + t + ":" + f + ":" + Nm(g) + ":" + Nm(d));
        CharDisp.FlushViewCall = () => cdLog.Add("flush");
        CharDisp.SoundSetAutoStopCall = n => cdLog.Add("autostop:" + n);
        CharDisp.SoundCommandCall = n => cdLog.Add("snd:" + n);
        CharDisp.DisplayMapViewCall = () => cdLog.Add("map");
        CharDisp.DisplayWildMapViewCall = () => cdLog.Add("wildmap");
        CharDisp.DrawWildWindowsCall = () => cdLog.Add("wildwin");
        CharDisp.LoadPortraitBufsCall = (a, b) => cdLog.Add("lpb:" + a + ":" + b);
        CharDisp.DisplayPortraitBufsCall = (c, r, b) => cdLog.Add("dpb:" + c + ":" + r + ":" + b);
        CharDisp.LoadAnimationCall = (string n, byte id, byte t, ref Types.TAnimRec a) => cdLog.Add("anim:" + n + ":" + id + ":" + t);
        CharDisp.DisplayImageBufCall = (c, r, an, pic) => cdLog.Add("dib:" + c + ":" + r + ":" + an + ":" + (pic == null ? "null" : "pic"));
        CharDisp.DelayMs = ms => cdLog.Add("delay:" + ms);
        TextRend.DelayMilliseconds = ms => cdLog.Add("pause:" + ms + ":" + SnapOf(smscr)); GDVars.gameDelay = 3;
        GDVars.arenaPtr = new Types.TArena();
    }
    static void CdRestore()
    {
        CharDisp.BlitClipped = Blit.BlitTileClipped; CharDisp.FlushViewCall = Blit.FlushView; CharDisp.SoundSetAutoStopCall = SoundDrv.SoundSetAutoStop; CharDisp.SoundCommandCall = SoundDrv.SoundCommand;
        CharDisp.DisplayMapViewCall = MapView.DisplayMapView; CharDisp.DisplayWildMapViewCall = MapView.DisplayWildMapView; CharDisp.DrawWildWindowsCall = Wilds.DrawWildWindows;
        CharDisp.LoadPortraitBufsCall = Portrait.LoadPortraitBufs; CharDisp.DisplayPortraitBufsCall = Portrait.DisplayPortraitBufs; CharDisp.LoadAnimationCall = Portrait.LoadAnimation; CharDisp.DisplayImageBufCall = Portrait.DisplayImageBuf;
        CharDisp.CombatMap = null; TextRend.DelayMilliseconds = null;
        CharDisp.DelayMs = ms => { if (TextRend.DelayMilliseconds == null) throw new NotSupportedException("no delay host"); TextRend.DelayMilliseconds(ms); };
    }
    static CdRun CdOnce(Action setup, Action act, byte[] keys)
    {
        CdSetup(); setup();
        smio.Keys.Clear(); if (keys != null) foreach (byte k in keys) smio.Keys.Enqueue(k);
        var r = new CdRun();
        smio.OnRead = () => { r.snaps.Add(SnapOf(smscr)); cdLog.Add("read"); };
        act();
        smio.OnRead = null;
        r.scr = smscr; r.pics = smpics.ToList(); r.log = cdLog.ToList(); r.reads = smio.Reads;
        return r;
    }
    // run the routine and its oracle from identical state; every observable must agree
    static void CdSame(string what, Action setup, Action actual, Action oracle, byte[] keys = null)
    {
        var a = CdOnce(setup, actual, keys); var b = CdOnce(setup, oracle, keys);
        string d = a.scr.Diff(b.scr, 0, 24);
        Yes(d == null, what + ": screen " + d + "\nACTUAL " + SnapOf(a.scr).Substring(0, Math.Min(700, SnapOf(a.scr).Length)) + "\nORACLE " + SnapOf(b.scr).Substring(0, Math.Min(700, SnapOf(b.scr).Length)) + "\nlogA " + string.Join("|", a.log) + "\nlogB " + string.Join("|", b.log));
        EqS(string.Join("|", b.pics), string.Join("|", a.pics), what + ": symbol pictures");
        EqS(string.Join("|", b.log), string.Join("|", a.log), what + ": calls");
        Eq(b.snaps.Count, a.snaps.Count, what + ": reads");
        for (int i = 0; i < a.snaps.Count; i++) EqS(b.snaps[i], a.snaps[i], what + ": screen at read " + i);
    }
    static Types.TCharRecord MkChar(string name, int ac, int hp, int max, int enabled, int hostile)
    {
        var c = new Types.TCharRecord { name = name, acCur = (byte)ac, hpCur = (byte)hp, hpMax = (byte)max, enabled = (byte)enabled, hostile = (sbyte)hostile };
        return c;
    }
    static Action<int, int, int, string> DS = (c, r, k, s) => TextRend.DisplayString((byte)c, (byte)r, (byte)k, s);
    static void CB(int l, int t, int r, int b) { Border.ClearBox(SB(l), SB(t), SB(r), SB(b)); }
    static int NameColor(Types.TCharRecord c) { return c.enabled == 0 ? LR : c.hostile == 1 ? 14 : CharHlp.CanTrain(c) ? 13 : LC; }

    static void CharDispNumbers()
    {
        try
        {
            for (int ac = 0; ac < 256; ac++)
            {
                int col = rng.Next(37), row = rng.Next(25);
                var c = MkChar("X", ac, 1, 1, 1, 0);
                CdSame("ac " + ac, () => { }, () => CharDisp.PrintArmorClass(c, (byte)col, (byte)row), () => DS(col, row, 10, (ac > 60 ? "-" : "") + Math.Abs(ac - 60)));
            }
            for (int hp = 0; hp < 256; hp += (hp < 20 || hp > 240 ? 1 : 7))
                foreach (int max in new[] { 0, 1, hp - 1, hp, hp + 1, 255, 99 })
                    foreach (bool pink in new[] { false, true })
                    {
                        if (max < 0 || max > 255) continue;
                        int col = rng.Next(40), row = rng.Next(25); var c = MkChar("X", 0, hp, max, 1, 0);
                        CdSame("hp " + hp + "/" + max, () => { }, () => CharDisp.PrintHitPoints(c, (byte)col, (byte)row, pink),
                            () => DS(col, row, pink ? 13 : hp < max ? 14 : 10, hp.ToString()));
                    }
        }
        finally { CdRestore(); }
    }

    static void CharDispName()
    {
        try
        {
            for (int n = 0; n < 1500; n++)
            {
                int en = rng.Next(3) == 0 ? 0 : 1 + rng.Next(3), host = new[] { -1, 0, 1, 2 }[rng.Next(4)];
                var c = MkChar(RandTxt(rng.Next(41)).Replace("  ", "-"), 0, 1, 1, en, host);
                // level and class data decide CanTrain; vary what it reads
                if (rng.Next(2) == 0) { c.classLevels[C_Level.CLERIC] = 1; c.experience = 2000000000; }
                int col = rng.Next(256), row = rng.Next(30); bool poss = rng.Next(2) == 0;
                int color = NameColor(c);
                CdSame("name " + n, () => { }, () => CharDisp.DisplayCreatureName(c, (byte)col, (byte)row, poss),
                    () => { DS(col, row, color, c.name); if (poss) DS(B(col + c.name.Length), row, color, "'s"); });
            }
        }
        finally { CdRestore(); }
    }
}
