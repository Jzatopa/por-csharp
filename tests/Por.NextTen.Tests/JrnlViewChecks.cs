// JrnlView checks, written from jrnlview.pas. The whole Journal session is run twice from identical scripted keys, gaps and clock:
// once through JrnlView.JournalMenu, once through an independent re-implementation of the Pascal unit (below, using TextRend/Border and the
// shared MenuCore/JrnlList). Screens at every key read, border symbols, key polls, clock reads and the exit state must agree.
using System;
using System.Collections.Generic;
using System.Linq;
using Por.Core;

class JvIo : IGameIoHost
{
    public List<KeyValuePair<byte, int>> Script = new List<KeyValuePair<byte, int>>(); // key, idle polls before it becomes available
    public int Pos, GapLeft, Polls, Cur, Step, Reads; bool justRead; public Action OnRead; public List<string> Log;
    public void Start() { Pos = 0; GapLeft = Script.Count > 0 ? Script[0].Value : 0; }
    public bool KeyPressed
    {
        get
        {
            Polls++; Cur = unchecked(Cur + Step); Log.Add("poll");
            if (Polls > 3000000) throw new Exception("menu starved of keys");
            if (justRead) { justRead = false; return false; }
            if (Pos >= Script.Count) return false;
            if (GapLeft > 0) { GapLeft--; return false; }
            return true;
        }
    }
    public byte ReadKey()
    {
        Reads++; if (OnRead != null) OnRead();
        if (Pos >= Script.Count) throw new Exception("keys exhausted");
        byte k = Script[Pos].Key; Pos++; GapLeft = Pos < Script.Count ? Script[Pos].Value : 0; justRead = k != 0; Log.Add("key:" + k); return k;
    }
    public void SoundCommand(ushort c) { }
    public void SoundSuspend() { }
    public void Interrupt(byte n, Types.TInterruptRegisters r) { }
    public void Halt(int c) { throw new Halted(); }
    public void WriteLine(string m) { }
    public System.IO.TextWriter OpenDebugLog(string p) { throw new NotSupportedException(); }
}
static class KvpExt { public static byte Key(this KeyValuePair<byte, int> p) { return p.Key; } }

static partial class Tests
{
    // ---- independent model of jrnlview.pas ----
    const int JPR = 38, JPL = JPR - 27 + 1, JLL = 1, JLR = JPL - 2, JTOP = 1, JBOT = 22, JTITLE = 1, JTXT = 3, JROWS = JBOT - JTXT + 1, JDELAY = 25, JCOUNT = 99;
    static int jSel, jTop, jShown, jLines, jPage, jPages, jMoved; static bool jDue;
    static string JCopy(string s, int n) { return s.Length <= n ? s : s.Substring(0, n); }
    static void JLabel(int n, int color) { TextRend.DisplayString((byte)JLL, (byte)(JTOP + n - jTop), (byte)color, JCopy(JrnlList.EntryLabel((short)n), JLR - JLL + 1)); }
    static void JList()
    {
        Border.ClearBox((sbyte)JLL, (sbyte)JTOP, (sbyte)JLR, (sbyte)JBOT);
        int n = jTop; while (n <= JCOUNT && n - jTop <= JBOT - JTOP) { JLabel(n, n == jSel ? 15 : 10); n++; }
    }
    static int JStart(int p) { return p <= 1 ? 1 : JROWS + (p - 2) * (JROWS - 2); }
    static void JMore(int row) { TextRend.DisplayCentered((byte)JPL, (byte)JPR, (byte)row, 15, "(more)"); }
    static void JPageDraw()
    {
        Border.ClearBox((sbyte)JPL, (sbyte)JTITLE, (sbyte)JPR, (sbyte)JTITLE);
        Border.ClearBox((sbyte)JPL, (sbyte)JTXT, (sbyte)JPR, (sbyte)JBOT);
        TextRend.DisplayCentered((byte)JPL, (byte)JPR, (byte)JTITLE, 15, JCopy(JrnlList.EntryTitle((short)jShown), JPR - JPL + 1));
        int row = JTXT, last = JBOT;
        if (jPage > 1) { JMore(JTXT); row++; }
        if (jPage < jPages) { JMore(JBOT); last--; }
        int line = JStart(jPage);
        while (row <= last && line <= jLines) { TextRend.DisplayString((byte)JPL, (byte)row, 10, JCopy(JrnlList.TextLine((short)jShown, (short)line), JPR - JPL + 1)); line++; row++; }
    }
    static void JShow()
    {
        jDue = false;
        if (jShown != jSel)
        {
            jShown = jSel; jLines = JrnlList.LineCount((short)jShown);
            if (jLines <= JROWS) jPages = 1; else { jPages = 2; while (jLines > JStart(jPages) + JROWS - 2) jPages++; }
            jPage = 1; JPageDraw();
        }
        else if (jPage != 1) { jPage = 1; JPageDraw(); }
    }
    static void JMoved(int old) { if (jSel != old) { jDue = true; jMoved = TextRend.GetTicks(); } }
    static void JTick()
    {
        if (jDue && !GameIo.Host.KeyPressed)
        {
            int now = TextRend.GetTicks();
            if (now < jMoved || now - jMoved >= JDELAY) JShow();
        }
    }
    static void JMoveTo(int n)
    {
        int old = jSel; if (n > JCOUNT) n = JCOUNT; if (n < 1) n = 1;
        if (n < jTop || n > jTop + JBOT - JTOP) { jTop = n < jTop ? n : n - (JBOT - JTOP); jSel = n; JList(); }
        else { JLabel(jSel, 10); jSel = n; JLabel(jSel, 15); }
        JMoved(old);
    }
    static void JScroll(bool down)
    {
        int old = jSel, row = jSel - jTop;
        jTop = down ? jTop + JBOT - JTOP + 1 : jTop - (JBOT - JTOP + 1);
        if (jTop > JCOUNT - (JBOT - JTOP)) jTop = JCOUNT - (JBOT - JTOP);
        if (jTop < 1) jTop = 1;
        jSel = jTop + row; if (jSel > JCOUNT) jSel = JCOUNT;
        JList(); JMoved(old);
    }
    static void JOracle()
    {
        byte saved = GDVars.horizChoice; jSel = 1; jTop = 1; jShown = 0; jLines = 0; jPage = 1; jPages = 1; bool reading = false; char lastKey = 'S';
        Border.ClearBox(1, 1, 38, 22); Border.DrawBorder((sbyte)JLL, (sbyte)JTOP, (sbyte)JLR, (sbyte)JBOT, 0);
        Border.DrawBorder((sbyte)JPL, (sbyte)JTXT, (sbyte)JPR, (sbyte)JBOT, 0); Border.DrawBorder((sbyte)JPL, (sbyte)JTITLE, (sbyte)JPR, (sbyte)JTITLE, 0);
        JList(); jDue = true; jMoved = TextRend.GetTicks(); MenuCore.idleHook = JTick;
        byte ch; bool ext = false; string menu;
        do
        {
            menu = "Select"; if (reading && jPage < jPages) menu += " Next"; if (reading && jPage > 1) menu += " Prev"; menu += " Exit";
            int opt = 1, cnt = 0; foreach (char c in menu) if (c >= 'A' && c <= 'Z') { cnt++; if (c == lastKey) opt = cnt; }
            GDVars.horizChoice = (byte)opt;
            ch = MenuCore.MenuHorizontal("", menu, 13, 10, 15, true, false, ref ext);
            if (ext)
            {
                if (ch == 72 || ch == 71) { if (jSel > 1) JMoveTo(jSel - 1); else JMoveTo(JCOUNT); }
                else if (ch == 80 || ch == 79) { if (jSel < JCOUNT) JMoveTo(jSel + 1); else JMoveTo(1); }
                else if (ch == 73) JScroll(false);
                else if (ch == 81) JScroll(true);
                if (jDue) { reading = false; lastKey = 'S'; }
            }
            else
            {
                lastKey = (char)ch;
                if (ch == 'S') { JShow(); reading = true; }
                else if (ch == 'N') { if (jPage < jPages) { jPage++; JPageDraw(); } }
                else if (ch == 'P') { if (jPage > 1) { jPage--; JPageDraw(); } }
            }
        } while (ext || !(ch == 0 || ch == 'E'));
        MenuCore.idleHook = null; GDVars.horizChoice = saved; CharDisp.DisplayNoncombatInterface();
    }

    static List<KeyValuePair<byte, int>> JvScript(Random r, int style)
    {
        var s = new List<KeyValuePair<byte, int>>();
        Action<int, int> ext = (code, gap) => { s.Add(new KeyValuePair<byte, int>(0, gap)); s.Add(new KeyValuePair<byte, int>((byte)code, 0)); };
        int ops = 1 + r.Next(style == 0 ? 12 : 40);
        for (int i = 0; i < ops; i++)
        {
            int gap = r.Next(3) == 0 ? r.Next(4, 30) : r.Next(3) == 0 ? r.Next(0, 4) : 0;
            int rep = r.Next(6) == 0 ? r.Next(2, style == 2 ? 60 : 25) : 1;
            int pick = r.Next(14);
            for (int k = 0; k < rep; k++)
            {
                int g = k == 0 ? gap : 0;
                switch (pick)
                {
                    case 0: ext(72, g); break;
                    case 1: case 2: ext(80, g); break;
                    case 3: ext(71, g); break;
                    case 4: ext(79, g); break;
                    case 5: ext(73, g); break;
                    case 6: ext(81, g); break;
                    case 7: case 8: s.Add(new KeyValuePair<byte, int>((byte)'S', g)); break;
                    case 9: s.Add(new KeyValuePair<byte, int>((byte)'N', g)); break;
                    case 10: s.Add(new KeyValuePair<byte, int>((byte)'P', g)); break;
                    case 11: s.Add(new KeyValuePair<byte, int>(13, g)); break;
                    case 12: ext(r.Next(2) == 0 ? 75 : 77, g); break;
                    default: s.Add(new KeyValuePair<byte, int>((byte)(r.Next(2) == 0 ? ',' : '.'), g)); break;
                }
            }
        }
        s.Add(new KeyValuePair<byte, int>(r.Next(3) == 0 ? (byte)27 : (byte)'E', r.Next(3) == 0 ? r.Next(30) : 0));
        return s;
    }

    static int jvSessionsWithPages, jvWithPreview, jvWrapped;
    static void JrnlViewSessions()
    {
        var oldRead = TextRend.ReadBiosTicks;
        try
        {
            for (int n = 0; n < 700; n++)
            {
                var r = new Random(n * 13 + 7); int style = n % 3; var script = JvScript(r, style);
                int start = r.Next(4) == 0 ? 1573000 : r.Next(0, 1500000), step = r.Next(4) == 0 ? 0 : r.Next(1, 14); int choice = r.Next(1, 4);
                var snaps = new string[2]; var logs = new string[2]; var pics = new string[2]; var fin = new string[2]; var draws = new string[2];
                for (int run = 0; run < 2; run++)
                {
                    SmSetup(); SmDone();
                    var io = new JvIo { Script = script, Cur = start, Step = step, Log = new List<string>() }; io.Start();
                    GameIo.Host = io; TextRend.ReadBiosTicks = () => io.Cur; GDVars.horizChoice = (byte)choice; GDVars.ifcState = C_Ifc.MAIN_MENU;
                    var dl = new List<string>(); var g0 = GDVars.video.DrawGlyph; var r0 = GDVars.video.ClearRect;
                    GDVars.video.DrawGlyph = (x, y, c, g) => { dl.Add("g" + x + "," + y + "," + c + "," + g); g0(x, y, c, g); };
                    GDVars.video.ClearRect = (cl, ct, cr, cb) => { dl.Add("r" + cl + "," + ct + "," + cr + "," + cb); r0(cl, ct, cr, cb); };
                    var shots = new List<string>(); io.OnRead = () => shots.Add(SnapOf(smscr));
                    try { if (run == 0) JrnlView.JournalMenu(); else JOracle(); }
                    catch (Exception e) { throw new Exception("session " + n + " run " + run + ": " + e.Message); }
                    snaps[run] = string.Join("#", shots) + "#" + SnapOf(smscr); logs[run] = string.Join("|", io.Log); pics[run] = string.Join("|", smpics); draws[run] = string.Join(";", dl);
                    fin[run] = "choice=" + GDVars.horizChoice + " hook=" + (MenuCore.idleHook == null) + " polls=" + io.Polls + " cur=" + io.Cur + " pos=" + io.Pos;
                    if (run == 0 && JrnlView.pages > 1) jvSessionsWithPages++;
                    GameIo.Host = null;
                }
                string tag = "journal session " + n + " style=" + style + " keys=" + script.Count + " step=" + step;
                EqS(logs[1], logs[0], tag + ": poll/key log"); EqS(fin[1], fin[0], tag + ": exit state"); EqS(pics[1], pics[0], tag + ": border symbols"); EqS(draws[1], draws[0], tag + ": draw call sequence");
                var a = snaps[0].Split('#'); var b = snaps[1].Split('#'); Eq(b.Length, a.Length, tag + ": reads");
                for (int i = 0; i < a.Length; i++) EqS(b[i], a[i], tag + ": screen at read " + i);
                Yes(fin[0].Contains("hook=True") && fin[0].Contains("choice=" + choice), tag + ": hook cleared, choice restored");
            }
        }
        finally { GameIo.Host = null; TextRend.ReadBiosTicks = oldRead; MenuCore.idleHook = null; }
        { int exactly20 = 0, over20 = 0, over38 = 0; for (short i = 1; i <= 99; i++) { int lc = JrnlList.LineCount(i); if (lc == 20) exactly20++; if (lc > 20) over20++; if (lc > 38) over38++; }
          Yes(exactly20 == 0 && over20 > 5 && over38 > 0, "journal data: no item has exactly TEXT_ROWS (20) lines, so `lines < TEXT_ROWS` is an equivalent mutant; items over 20 lines = " + over20 + ", over 38 = " + over38); }
        Yes(jvSessionsWithPages >= 30, "journal coverage: sessions that reached a multi-page item = " + jvSessionsWithPages);
    }

    // the preview waits PREVIEW_DELAY = 25 TextRend.GetTicks units; check the edge exactly, with the midnight wrap and the key-waiting rule
    static void JrnlViewPreviewEdge()
    {
        var oldBios = TextRend.ReadBiosTicks; var oldHost = GameIo.Host;
        try
        {
            int hits = 0, cur = 0;
            for (int b0 = 0; b0 < 600; b0 += 3)
            {
                cur = b0; TextRend.ReadBiosTicks = () => cur;
                int movedAt = TextRend.GetTicks();
                for (int b = b0; b < b0 + 14; b++)
                {
                    cur = b; int diff = TextRend.GetTicks() - movedAt;
                    foreach (bool key in new[] { false, true })
                        foreach (bool due in new[] { false, true })
                        {
                            var io = new JvIo { Script = new List<KeyValuePair<byte, int>>(), Log = new List<string>() };
                            if (key) io.Script.Add(new KeyValuePair<byte, int>(65, 0)); io.Start(); GameIo.Host = io;
                            JrnlView.sel = 7; JrnlView.shown = 7; JrnlView.page = 1; JrnlView.pages = 1; JrnlView.top = 1; JrnlView.previewDue = due; JrnlView.movedAt = movedAt;
                            SmSetup(); SmDone(); GameIo.Host = io; TextRend.ReadBiosTicks = () => cur;
                            JrnlView.page = 2; JrnlView.pages = 3; JrnlView.shown = 7; JrnlView.lines = 40;
                            JrnlView.TickPreview();
                            bool fired = JrnlView.page == 1 && !JrnlView.previewDue && due;   // ShowSelected on the shown item with page 2 turns back to page 1
                            bool expect = due && !key && diff >= 25;
                            Yes(fired == expect || (!due && JrnlView.page == 2), "preview edge diff=" + diff + " key=" + key + " due=" + due);
                            if (due && !key) { Eq(expect ? 1 : 2, JrnlView.page, "preview fires at diff " + diff); if (diff == 25 || diff == 24 || diff == 26) hits++; }
                            else Eq(2, JrnlView.page, "preview must not fire (due=" + due + " key=" + key + ")");
                        }
                }
            }
            Yes(hits == 0, "tick differences of 24, 25 or 26 do not occur: GetTicks moves in steps of 5 or 6 per BIOS tick, so PREVIEW_DELAY 24/25/26 and >= vs > are indistinguishable (documented equivalent mutants); hits=" + hits);
            int firedN = 0, held = 0; for (int bb = 0; bb < 3000; bb += 7) { cur = bb; int m0 = TextRend.GetTicks(); for (int k = 1; k <= 12; k++) { cur = bb + k; int d = TextRend.GetTicks() - m0; if (d >= 21 && d <= 22) held++; if (d >= 27 && d <= 28) firedN++; } }
            Yes(held > 100 && firedN > 100, "edge neighbours 21/22 (not yet) and 27/28 (due) were tested: " + held + "/" + firedN);
            // the tick count restarting at midnight shows the item at once
            cur = 100; TextRend.ReadBiosTicks = () => cur;
            SmSetup(); SmDone(); var io2 = new JvIo { Script = new List<KeyValuePair<byte, int>>(), Log = new List<string>() }; io2.Start(); GameIo.Host = io2; TextRend.ReadBiosTicks = () => cur;
            JrnlView.sel = 7; JrnlView.shown = 7; JrnlView.page = 2; JrnlView.pages = 3; JrnlView.lines = 40; JrnlView.top = 1; JrnlView.previewDue = true; JrnlView.movedAt = TextRend.GetTicks() + 5;
            JrnlView.TickPreview(); Eq(1, JrnlView.page, "midnight wrap shows the item");
        }
        finally { GameIo.Host = oldHost; TextRend.ReadBiosTicks = oldBios; MenuCore.idleHook = null; }
    }
}
