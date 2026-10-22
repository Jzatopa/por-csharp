// Camp (src/camp.pas) checks, part 1: the recording world (logging UI, menus, Magic, FxEngine, GameTime, UsualSpl, Party,
// SaveLoad, GameIo and the other call points), the scenario builder, snapshots and the oracle/port pair runner. Expected
// behaviour comes from CpOracle (CampOracle.cs), a separate model written from the Pascal text. Each scenario is built
// twice from one seed, once for the oracle and once for the port; the exception type, the ordered call logs and the
// state snapshots must match.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Por.Core;
using static Por.Core.Types;

sealed class CpWorld
{
    public readonly List<string> Log = new List<string>();
    public readonly Random R;
    public readonly List<TCharRecord> All = new List<TCharRecord>();
    public readonly Dictionary<TCharRecord, byte[]> Usual = new Dictionary<TCharRecord, byte[]>();
    readonly Dictionary<object, int> ids = new Dictionary<object, int>(new RefEq());
    int nextId;
    int steps;
    public int Budget = 30;
    int menus;
    // keys MenuH answers first, in order (key, extended), before the random ones
    public Queue<Tuple<byte, bool>> Script;
    // ids SpellMenu answers first, in order
    public Queue<byte> SpellScript;
    public CpWorld(int seed) { R = new Random(seed); }

    sealed class RefEq : IEqualityComparer<object>
    {
        public new bool Equals(object a, object b) { return ReferenceEquals(a, b); }
        public int GetHashCode(object o) { return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(o); }
    }
    public int Id(object o) { int v; if (!ids.TryGetValue(o, out v)) { v = ++nextId; ids[o] = v; } return v; }
    public string F(object o)
    {
        if (o == null) return "nil";
        var c = o as TCharRecord; if (c != null) return "C" + Id(c);
        var i = o as TItemRec; if (i != null) return "I" + Id(i);
        var s = o as string; if (s != null) return "\"" + s + "\"";
        if (o is bool) return (bool)o ? "T" : "F";
        return o.ToString();
    }
    public void L(string name, params object[] a)
    {
        if (++steps > 20000) throw new InvalidOperationException("runaway call log");
        Log.Add(name + "(" + string.Join(",", a.Select(F)) + ")");
    }
    public bool Coin() { return R.Next(2) == 0; }
    public TCharRecord Any() { return All[R.Next(All.Count)]; }
    bool Spent() { return ++menus > Budget; }
    public string ListSig(TItemRec p)
    {
        var sb = new StringBuilder("[");
        int n = 0;
        for (; p != null && n < 300; p = p.next, n++) sb.Append(p.name).Append('#').Append(p.inUse).Append(';');
        return sb.Append(']').ToString();
    }
    public string Mem(TCharRecord c) { return c == null ? "-" : string.Join(".", c.memorizedSpells.Data); }
    public string Snap()
    {
        var sb = new StringBuilder();
        foreach (var c in All)
        {
            sb.Append(F(c)).Append(':').Append(c.name).Append(" st").Append(c.status).Append(" hp").Append(c.hpCur).Append('/').Append(c.hpMax)
              .Append(" prep").Append(c.spellPrepTime).Append(" mem").Append(Mem(c)).Append(" fx");
            for (var e = c.effectsAddr; e != null; e = e.next) sb.Append(e.id).Append('/').Append(e.strength).Append(';');
            sb.Append(" items");
            for (var p = c.itemsAddr; p != null; p = p.next)
                sb.Append(F(p)).Append('{').Append(p.itemType).Append('|').Append(p.variantData[0]).Append(',').Append(p.variantData[1]).Append(',').Append(p.variantData[2]).Append("};");
            sb.Append(" next").Append(F(c.nextCharAddr));
            byte[] u;
            if (Usual.TryGetValue(c, out u)) sb.Append(" usual").Append(string.Join(".", u));
            sb.Append('\n');
        }
        sb.Append("cur ").Append(F(GDVars.curChar)).Append(" list ").Append(F(GDVars.creatureList)).Append(" castTarget ").Append(F(GDVars.castTarget))
          .Append(" ifc ").Append(GDVars.ifcState).Append(" heal ").Append(GDVars.healTicks).Append(" rest ").Append(string.Join(",", GDVars.dsRestingTimeGbvm.Data))
          .Append(" target1 ").Append(F(GDVars.dsTargetCreaturePtr[1])).Append(" companion ").Append(GDVars.companionOn);
        return sb.ToString();
    }

    // ---- TextRend, Border, CharDisp, Portrait ----
    public void DisplayString(byte c, byte r, byte col, string s) { L("Str", c, r, col, s); }
    public void ShowMessage(byte col, bool clear, string s) { L("ShowMsg", col, clear, s); }
    public void ClearBottomLine() { L("ClearBottom"); }
    public void DrawBorder(sbyte a, sbyte b, sbyte c, sbyte d, byte k) { L("Border", a, b, c, d, k); }
    public void ClearBox(sbyte a, sbyte b, sbyte c, sbyte d) { L("ClearBox", a, b, c, d); }
    public void DrawWindow(sbyte a, sbyte b, sbyte c, sbyte d, byte k, byte col, string t) { L("Window", a, b, c, d, k, col, t); }
    public void PrintNameString(TCharRecord c, string s, byte row, bool pause) { L("NameStr", c, s, row, pause); }
    public void DisplayNoncombatInterface() { L("Noncombat"); }
    public void PrintMiscInfo() { L("MiscInfo"); }
    public void DisplayParty(TCharRecord c) { L("Party", c, GDVars.creatureList); }
    public void MessageWait() { L("MsgWait"); }
    public void ClearMessageWindow() { L("ClearMsg"); }
    public void PrintBottomLine(string s) { L("Bottom", s); }
    public void PrintMessageWait(string s) { L("PrintMsgWait", s); }
    public bool PlayerPortraitsHere() { bool r = Coin(); L("PortraitsHere", r); return r; }

    // ---- MenuCore ----
    static readonly byte[] ExtKeys = { C_Keys.UP, C_Keys.DOWN, C_Keys.HOME, C_Keys.END_KEY, C_Keys.F6, C_Keys.F6, (byte)'E', 0, (byte)'H' };
    byte PickKey(string menu)
    {
        var keys = new List<byte> { 0, (byte)'E', (byte)'E', (byte)'Q' };
        foreach (var t in (menu ?? "").Split(' ')) if (t.Length > 0) { keys.Add((byte)t[0]); keys.Add((byte)t[0]); keys.Add((byte)t[0]); }
        return keys[R.Next(keys.Count)];
    }
    public byte MenuH(string q, string m, byte qc, byte mc, byte hc, bool reint, bool anim, ref bool ext)
    {
        L("MenuH", q, m, qc, mc, hc, reint, anim, GDVars.curChar);
        byte r;
        if (Script != null && Script.Count > 0) { var k = Script.Dequeue(); r = k.Item1; ext = k.Item2; }
        else if (Spent()) { ext = false; r = (byte)'E'; }
        else if (R.Next(5) == 0) { ext = true; r = ExtKeys[R.Next(ExtKeys.Length)]; }
        else { ext = false; r = PickKey(m); }
        L("MenuH=", r, ext);
        return r;
    }
    public byte MenuV(string q, string m, byte qc, byte mc, byte hc, sbyte a, sbyte b, sbyte c, sbyte d, TItemRec list, bool addExit, ref bool redraw, ref short idx, ref TItemRec sel)
    {
        L("MenuV", q, m, qc, mc, hc, a, b, c, d, ListSig(list), addExit, redraw, idx, sel == null ? "nil" : sel.name);
        var nodes = new List<TItemRec>();
        for (var p = list; p != null; p = p.next) nodes.Add(p);
        sel = (nodes.Count == 0 || R.Next(4) == 0) ? null : nodes[R.Next(nodes.Count)];
        idx = (short)R.Next(-1, 20);
        redraw = Coin();
        byte key = (byte)(Coin() ? 'E' : 0);
        L("MenuV=", key, sel == null ? "nil" : sel.name, idx, redraw);
        return key;
    }
    public byte YesNo(string q, byte a, byte b, byte c) { byte r = (byte)(Coin() ? 'Y' : 'N'); L("YesNo", q, a, b, c, (char)r); return r; }
    public void AppendMenuLine(ref TItemRec head, ref TItemRec tail, string name, bool title)
    {
        var n = new TItemRec { name = name, inUse = (byte)(title ? 1 : 0) };
        if (head == null) head = n; else tail.next = n;
        tail = n;
        L("Append", name, title);
    }
    public void FreeStringList(ref TItemRec list) { L("FreeList", ListSig(list)); }

    // ---- Magic ----
    // a spell id for the menu kind: the Memorize pick offers spells with a memorize slot, Scribe offers scroll bytes
    byte SpellFor(byte mt, byte st)
    {
        if (R.Next(4) == 0) return 0;
        if (mt == 1 && st == 2)
        {
            var ok = new List<byte>();
            for (int k = 1; k <= 56; k++) { var t = GDConsts.spellTable[k]; if (t.levelSp >= 1 && t.levelSp <= 3 && t.castClassSp >= 0 && t.castClassSp <= 1) ok.Add((byte)k); }
            return ok[R.Next(ok.Count)];
        }
        if (mt == 3 && R.Next(6) == 0) return (byte)R.Next(57, 128);
        return (byte)R.Next(1, 68);
    }
    public byte SpellMenu(byte mt, byte st, ref short idx, ref bool notEmpty)
    {
        L("SpellMenu", mt, st, idx, GDVars.curChar, Mem(GDVars.curChar));
        byte r = (SpellScript != null && SpellScript.Count > 0) ? SpellScript.Dequeue() : Spent() ? (byte)0 : SpellFor(mt, st);
        notEmpty = Coin();
        if (R.Next(3) != 0) idx = (short)R.Next(-1, 10);
        L("SpellMenu=", r, idx, notEmpty);
        return r;
    }
    public byte CastSpellMenu(ref short idx, ref bool notEmpty, ref TItemRec scroll)
    {
        L("CastSpellMenu", idx, GDVars.curChar, GDVars.castTarget);
        byte r = (Spent() || R.Next(3) == 0) ? (byte)0 : (byte)R.Next(1, 57);
        notEmpty = Coin();
        var items = new List<TItemRec>();
        for (var p = GDVars.curChar.itemsAddr; p != null; p = p.next) items.Add(p);
        scroll = (items.Count == 0 || Coin()) ? null : items[R.Next(items.Count)];
        idx = (short)R.Next(-1, 10);
        L("CastSpellMenu=", r, idx, notEmpty, scroll);
        return r;
    }
    public void CastSpellNC(byte spell, bool b, bool ai, ref bool sel) { L("CastNC", spell, b, ai, GDVars.curChar); sel = Coin(); }
    public void UseItemSpell(TItemRec it, byte spell, bool announce, ref bool done)
    {
        L("UseItemSpell", it, spell, announce, GDVars.curChar);
        done = Coin();
        if (R.Next(3) == 0)
        {
            // the scroll may be used up, and the character may change
            var c = GDVars.curChar; TItemRec prev = null;
            for (var p = c.itemsAddr; p != null; prev = p, p = p.next)
                if (ReferenceEquals(p, it)) { if (prev == null) c.itemsAddr = p.next; else prev.next = p.next; break; }
        }
        if (R.Next(4) == 0) GDVars.curChar = Any();
    }
    public void CureDisease() { L("CureDiseaseHandler", GDVars.dsTargetCreaturePtr[1]); }
    public void CureBlindness() { L("CureBlindnessHandler", GDVars.dsTargetCreaturePtr[1]); }
    public void RemoveCurse() { L("RemoveCurseSpell", GDVars.dsTargetCreaturePtr[1]); }

    // ---- CharHlp, FxEngine, GameTime, UsualSpl, Party, SaveLoad, GameIo, the menus, Dice ----
    public void RecalcStats(TCharRecord c) { L("Recalc", c); }
    public bool RestoreHitPoints(TCharRecord c, byte hp, bool bypass)
    {
        bool r = R.Next(4) != 0;
        if (r) c.hpCur = (byte)Math.Min(c.hpMax, c.hpCur + hp);
        L("Restore", c, hp, bypass, r);
        return r;
    }
    public bool CampRest(bool interactive)
    {
        L("CampRest", interactive, string.Join(",", GDVars.dsRestingTimeGbvm.Data));
        // a completed rest counts the clock down to zero; a broken-off one may leave some of it
        for (int i = 0; i < 7; i++) GDVars.dsRestingTimeGbvm[i] = 0;
        if (R.Next(3) == 0) GDVars.dsRestingTimeGbvm[R.Next(1, 5)] = (ushort)R.Next(1, 3);
        bool r = R.Next(4) == 0;
        L("CampRest=", r);
        return r;
    }
    public bool GetUsual(TCharRecord c, ref TSpellSlots s)
    {
        byte[] u;
        bool r = Usual.TryGetValue(c, out u);
        if (r) for (int i = 0; i <= 20; i++) s[i] = u[i];
        L("GetUsual", c, r);
        return r;
    }
    public void SetUsual(TCharRecord c, ref TSpellSlots s) { Usual[c] = (byte[])s.Data.Clone(); L("SetUsual", c, string.Join(".", s.Data)); }
    public void SelectPrevNextChar(char k)
    {
        L("SelectPrevNext", (int)k, GDVars.curChar);
        var list = new List<TCharRecord>();
        for (var p = GDVars.creatureList; p != null && list.Count < 20; p = p.nextCharAddr) list.Add(p);
        int i = list.IndexOf(GDVars.curChar);
        if (list.Count == 0) GDVars.curChar = null;
        else if (k == C_Keys.UP || k == C_Keys.HOME) GDVars.curChar = list[(i - 1 + list.Count) % list.Count];
        else if (k == C_Keys.DOWN || k == C_Keys.END_KEY) GDVars.curChar = list[(i + 1) % list.Count];
        else GDVars.curChar = list[0];
    }
    public void RemoveCreature(bool suppress, bool free)
    {
        L("RemoveCreature", suppress, free, GDVars.curChar);
        var c = GDVars.curChar;
        TCharRecord prev = null;
        for (var p = GDVars.creatureList; p != null; prev = p, p = p.nextCharAddr)
            if (ReferenceEquals(p, c)) { if (prev == null) GDVars.creatureList = p.nextCharAddr; else prev.nextCharAddr = p.nextCharAddr; break; }
        GDVars.curChar = c.nextCharAddr ?? GDVars.creatureList;
        c.nextCharAddr = null;
        All.Remove(c);
    }
    public void SaveGame() { L("SaveGame"); }
    public void ExitGame() { L("ExitGame"); throw new Halted(); }
    public void ToolsMenu() { L("ToolsMenu"); }
    public void OptionsMenu() { L("OptionsMenu"); }
    public void CustomizeIcon() { L("CustomizeIcon", GDVars.curChar); }
    public void CustomizePortrait() { L("CustomizePortrait", GDVars.curChar); }
    public void ViewCharMenu(ref bool done) { L("ViewCharMenu", GDVars.curChar); done = Coin(); }
    public byte RollDice(byte n, byte s) { byte r = (byte)R.Next(1, s + 1); L("Roll", n, s, r); return r; }
}

static partial class Tests
{
    static int cpLastLog;

    static void CpInstall(CpWorld w)
    {
        Camp.DisplayStringCall = w.DisplayString;
        Camp.ShowMessageCall = w.ShowMessage;
        Camp.ClearBottomLineCall = w.ClearBottomLine;
        Camp.DrawBorderCall = w.DrawBorder;
        Camp.ClearBoxCall = w.ClearBox;
        Camp.DrawWindowCall = w.DrawWindow;
        Camp.PrintNameStringCall = w.PrintNameString;
        Camp.DisplayNoncombatInterfaceCall = w.DisplayNoncombatInterface;
        Camp.PrintMiscInfoCall = w.PrintMiscInfo;
        Camp.DisplayPartyCall = w.DisplayParty;
        Camp.MessageWaitCall = w.MessageWait;
        Camp.ClearMessageWindowCall = w.ClearMessageWindow;
        Camp.PrintBottomLineCall = w.PrintBottomLine;
        Camp.PrintMessageWaitCall = w.PrintMessageWait;
        Camp.MenuHorizontalCall = w.MenuH;
        Camp.MenuVerticalCall = w.MenuV;
        Camp.YesNoMenuCall = w.YesNo;
        Camp.AppendMenuLineCall = w.AppendMenuLine;
        Camp.FreeStringListCall = w.FreeStringList;
        Camp.PlayerPortraitsHereCall = w.PlayerPortraitsHere;
        Camp.SpellMenuCall = w.SpellMenu;
        Camp.CastSpellMenuCall = w.CastSpellMenu;
        Camp.CastSpellNCCall = w.CastSpellNC;
        Camp.UseItemSpellCall = w.UseItemSpell;
        Camp.CureDiseaseHandlerCall = w.CureDisease;
        Camp.CureBlindnessHandlerCall = w.CureBlindness;
        Camp.RemoveCurseSpellCall = w.RemoveCurse;
        Camp.RecalcStatsCall = w.RecalcStats;
        Camp.RestoreHitPointsCall = w.RestoreHitPoints;
        Camp.CampRestCall = w.CampRest;
        Camp.GetUsualSpellsCall = w.GetUsual;
        Camp.SetUsualSpellsCall = w.SetUsual;
        Camp.SelectPrevNextCharCall = w.SelectPrevNextChar;
        Camp.RemoveCreatureCall = w.RemoveCreature;
        Camp.SaveGameCall = w.SaveGame;
        Camp.ExitGameCall = w.ExitGame;
        Camp.ToolsMenuCall = w.ToolsMenu;
        Camp.OptionsMenuCall = w.OptionsMenu;
        Camp.CustomizeIconCall = w.CustomizeIcon;
        Camp.CustomizePortraitCall = w.CustomizePortrait;
        Camp.ViewCharMenuCall = w.ViewCharMenu;
        Camp.RollDiceCall = w.RollDice;
    }

    // a memorized-spell byte: empty, a spell, or a pending spell
    static byte CpSpellByte(Random r)
    {
        int k = r.Next(10);
        if (k < 4) return 0;
        byte id = (byte)r.Next(1, 57);
        return (byte)(k < 7 ? id : id | 0x80);
    }

    static CpWorld CpBuild(int seed)
    {
        var w = new CpWorld(seed);
        var r = w.R;
        GDVars.zone0 = new TGbvmZone0State();
        GDVars.zone0.antimagic = (ushort)(r.Next(6) == 0 ? r.Next(1, 3) : 0);
        int[] ifcs = { C_Ifc.OUTDOORS, C_Ifc.INDOORS, C_Ifc.CAMPING, C_Ifc.VAULT };
        GDVars.ifcState = (byte)ifcs[r.Next(ifcs.Length)];
        GDVars.healTicks = (ushort)r.Next(0, 500);
        GDVars.companionOn = (byte)r.Next(0, 2);
        GDVars.dsRestingTimeGbvm = new TClock();
        for (int i = 0; i < 7; i++) GDVars.dsRestingTimeGbvm[i] = (ushort)r.Next(0, 4);
        GDVars.baseItems = new TBaseItems();
        for (int t = 0; t <= 128; t++)
            GDVars.baseItems.Data[t].slot = (byte)(r.Next(2) == 0 ? r.Next(C_Item.SLOT_MAGE_SCROLL, C_Item.SLOT_LAST_READABLE + 1) : r.Next(0, 11));
        int n = r.Next(6) == 0 ? r.Next(8, 11) : r.Next(1, 7);
        TCharRecord prev = null;
        for (int k = 0; k < n; k++)
        {
            var c = new TCharRecord();
            c.name = VcName(r, 1, 15);
            c.status = (byte)(r.Next(2) == 0 ? C_Status.OKAY : r.Next(0, 9));
            c.enabled = (byte)(r.Next(6) == 0 ? 0 : 1);
            c.hpMax = (byte)r.Next(1, 100); c.hpCur = (byte)(r.Next(3) == 0 ? c.hpMax : r.Next(0, c.hpMax + 1));
            c.classLevels[C_Level.CLERIC] = (sbyte)(r.Next(2) == 0 ? r.Next(1, 12) : r.Next(-1, 1));
            c.classLevels[C_Level.MAGE] = (sbyte)(r.Next(2) == 0 ? r.Next(1, 12) : r.Next(-1, 1));
            for (int s = 1; s <= 3; s++) { c.clericSpells[s] = (byte)(r.Next(3) == 0 ? 0 : r.Next(0, 5)); c.mageSpells[s] = (byte)(r.Next(3) == 0 ? 0 : r.Next(0, 5)); }
            for (int s = 1; s <= 56; s++) c.spellsKnown[s] = (byte)(r.Next(3) == 0 ? 1 : 0);
            for (int s = 0; s <= 20; s++) c.memorizedSpells[s] = CpSpellByte(r);
            if (r.Next(8) == 0) c.memorizedSpells[r.Next(21)] = 0x7F;
            c.spellPrepTime = (byte)r.Next(0, 7);
            int items = r.Next(0, 5);
            TItemRec tail = null;
            for (int j = 0; j < items; j++)
            {
                var it = new TItemRec { itemType = (byte)r.Next(0, 8), name = "i" + j };
                for (int s = 1; s <= 3; s++) it.spells[s] = r.Next(25) == 0 ? (byte)r.Next(0, 256) : CpSpellByte(r);
                if (tail == null) c.itemsAddr = it; else tail.next = it;
                tail = it;
            }
            int[] fx = { C_Effect.DISEASED, C_Effect.DISEASE_STR, C_Effect.ROT_NO_HEAL, C_Effect.BLIND, C_Effect.BESTOW_CURSE, C_Effect.BLESS,
                         C_Effect.PROT_EVIL, C_Effect.TRAIN_START, C_Effect.DISPLACED, C_Effect.HASTED, 43, 60, 90, 120 };
            for (int j = r.Next(0, 4); j > 0; j--)
                c.effectsAddr = new TEffectRec { id = (byte)(r.Next(4) == 0 ? r.Next(0, 142) : fx[r.Next(fx.Length)]), strength = (byte)r.Next(0, 9), next = c.effectsAddr };
            if (prev == null) GDVars.creatureList = c; else prev.nextCharAddr = c;
            prev = c;
            w.All.Add(c);
            if (r.Next(3) == 0)
            {
                var u = new byte[21];
                for (int s = 0; s <= 20; s++) u[s] = (byte)(r.Next(3) == 0 ? r.Next(0, 60) : 0);
                w.Usual[c] = u;
            }
        }
        GDVars.curChar = w.All[r.Next(w.All.Count)];
        GDVars.castTarget = r.Next(2) == 0 ? null : w.Any();
        GDVars.dsTargetCreaturePtr[1] = null;
        w.Budget = r.Next(1, 30);
        return w;
    }

    static bool CpPair(int seed, string what, Action<CpWorld, CpOracle> oracle, Action<CpWorld> port, Action<CpWorld> tweak = null)
    {
        var a = CpBuild(seed);
        if (tweak != null) tweak(a);
        string beforeA = a.Snap();
        string exA = null;
        try { oracle(a, new CpOracle(a)); }
        catch (Exception e)
        {
            if (e is InvalidOperationException && e.Message == "runaway call log") throw;
            exA = e.GetType().Name;
            if (Environment.GetEnvironmentVariable("CP_EX") != null) Console.WriteLine("  " + what + " seed " + seed + ": " + e.GetType().Name + " " + e.StackTrace.Split('\n')[0]);
        }
        string snapA = a.Snap();

        var b = CpBuild(seed);
        if (tweak != null) tweak(b);
        EqS(beforeA, b.Snap(), what + " seed " + seed + " identical start");
        CpInstall(b);
        string exB = null;
        try { port(b); }
        catch (Exception e) { exB = e.GetType().Name; if (e is NotSupportedException || e is NotImplementedException) throw; }
        string snapB = b.Snap();

        what = what + " seed " + seed;
        EqS(exA ?? "-", exB ?? "-", what + " exception");
        int n = Math.Min(a.Log.Count, b.Log.Count);
        for (int i = 0; i < n; i++)
            if (a.Log[i] != b.Log[i])
                throw new Exception("FAILED: " + what + " call " + i + "\nA:" + string.Join("\n", a.Log.Skip(Math.Max(0, i - 6)).Take(10)) + "\nB:" + string.Join("\n", b.Log.Skip(Math.Max(0, i - 6)).Take(10)));
        checks += n;
        Eq(a.Log.Count, b.Log.Count, what + " log length");
        EqS(snapA, snapB, what + " state");
        cpLastLog = a.Log.Count;
        return exA != null && exA != "Halted";
    }

    static void CpMany(string what, int n, Action<CpWorld, CpOracle> oracle, Action<CpWorld> port, Action<CpWorld> tweak = null, int maxThrowPct = 25)
    {
        int threw = 0;
        long logs = 0;
        int h = 23;
        foreach (char ch in what) h = unchecked(h * 31 + ch);
        for (int s = 0; s < n; s++)
        {
            if (CpPair((h & 0xFFFF) ^ (s * 7919 + 13), what, oracle, port, tweak)) threw++;
            logs += cpLastLog;
        }
        if (Environment.GetEnvironmentVariable("CP_STATS") != null) Console.WriteLine("  " + what + ": threw " + threw + "/" + n + ", mean log " + (logs / (double)n).ToString("0.0"));
        Yes(threw * 100 <= n * maxThrowPct, what + ": " + threw + " of " + n + " scenarios threw");
    }

    static void CpGuarded(Action group)
    {
        var saved = new List<Tuple<System.Reflection.FieldInfo, object, object>>();
        foreach (var t in new[] { typeof(GDVars), typeof(Camp) })
            foreach (var f in t.GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
            {
                if (f.IsLiteral || f.IsInitOnly) continue;
                object v = f.GetValue(null);
                var arr = v as Array;
                saved.Add(Tuple.Create(f, v, arr != null ? arr.Clone() : null));
            }
        try { group(); }
        finally
        {
            foreach (var s in saved)
            {
                s.Item1.SetValue(null, s.Item2);
                if (s.Item3 != null) Array.Copy((Array)s.Item3, (Array)s.Item2, ((Array)s.Item3).Length);
            }
        }
    }
}
