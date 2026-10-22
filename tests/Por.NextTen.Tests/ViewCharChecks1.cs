// ViewChar (src/viewchar.pas) checks, part 1: the recording world (logging UI, menus, FxEngine, Magic, Money, Vault,
// Stats and CharHlp call points), the scenario builder, snapshots and the oracle/port pair runner. Expected behaviour
// comes from VcOracle (ViewCharOracle.cs), a separate model written from the Pascal text. Each scenario is built twice
// from one seed, once for the oracle and once for the port; the exception type, the ordered call logs and the state
// snapshots must match.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Por.Core;
using static Por.Core.Types;

sealed class VcWorld
{
    public readonly List<string> Log = new List<string>();
    public readonly Random R;
    public readonly List<TCharRecord> All = new List<TCharRecord>();
    public readonly Dictionary<TCharRecord, Stats.TStatBlock> Blocks = new Dictionary<TCharRecord, Stats.TStatBlock>();
    readonly Dictionary<object, int> ids = new Dictionary<object, int>(new RefEq());
    int nextId;
    int steps;
    // menus and pickers answer at random until the budget is spent, then Exit (or nil)
    public int Budget = 30;
    // keys MenuH answers first, in order (key, extended), before the random ones
    public Queue<Tuple<byte, bool>> Script;
    int menus;
    public VcWorld(int seed) { R = new Random(seed); }

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
        var e = o as TEffectRec; if (e != null) return "E" + Id(e) + ":" + e.id + "/" + e.strength;
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

    public string ItemSig(TItemRec i)
    {
        return F(i) + "{" + i.name + "|" + i.inUse + "|" + i.itemType + "|" + i.nameIds[1] + "," + i.nameIds[2] + "," + i.nameIds[3] + "|" + i.plus + "|" +
               i.saveBonus + "|" + i.equipped + "|" + i.unidMask + "|" + i.cursed + "|" + i.weight + "|" + i.quantity + "|" + i.value + "|" +
               i.variantData[0] + "," + i.variantData[1] + "," + i.variantData[2] + "}";
    }
    public string ListSig(TItemRec p)
    {
        var sb = new StringBuilder("[");
        int n = 0;
        for (; p != null && n < 300; p = p.next, n++) sb.Append(ItemSig(p)).Append(';');
        return sb.Append(']').ToString();
    }
    public string Snap(TCharRecord tradeTarget)
    {
        var sb = new StringBuilder();
        foreach (var c in All)
        {
            sb.Append(F(c)).Append(':').Append(c.name).Append(" hp").Append(c.hpCur).Append(" n").Append(c.numberOfItems)
              .Append(" coins").Append(string.Join(",", c.coins)).Append(" mem").Append(string.Join(",", c.memorizedSpells.Data))
              .Append(" eq").Append(c.equipped[C_Equip.WEAPON] == null ? "-" : F(c.equipped[C_Equip.WEAPON]))
              .Append(c.equipped[C_Equip.ARMOR] == null ? "-" : F(c.equipped[C_Equip.ARMOR])).Append(" fx");
            for (var e = c.effectsAddr; e != null; e = e.next) sb.Append(F(e)).Append('/').Append(e.duration).Append(e.onRemove ? "r" : "").Append(';');
            sb.Append(" items").Append(ListSig(c.itemsAddr)).Append(" next").Append(F(c.nextCharAddr)).Append('\n');
        }
        sb.Append("cur ").Append(F(GDVars.curChar)).Append(" list ").Append(F(GDVars.creatureList)).Append(" trade ").Append(F(tradeTarget))
          .Append(" pool ").Append(string.Join(",", GDVars.treasureMoney.Data)).Append(" col ").Append(GDVars.curCol).Append(" row ").Append(GDVars.curRow)
          .Append(" redrawInv ").Append(GDVars.dsRedrawInventoryMenuFlag).Append(" horiz ").Append(GDVars.horizChoice)
          .Append(" extra ").Append(MenuCore.extraKeys).Append(" ifc ").Append(GDVars.ifcState);
        return sb.ToString();
    }

    // ---- TextRend, Border, CharDisp, Portrait, CbtMap ----
    public void ClearScreen() { L("ClearScreen"); }
    public void DisplayString(byte c, byte r, byte col, string s) { L("Str", c, r, col, s); }
    public void DisplayCentered(byte a, byte b, byte r, byte col, string s) { L("Centered", a, b, r, col, s); }
    public void DisplayMessageWindow(byte a, byte b, byte c, byte d, byte col, bool clear, string s) { L("MsgWin", a, b, c, d, col, clear, s, GDVars.curCol, GDVars.curRow); }
    public void DisplayAndPause(byte col, string s) { L("AndPause", col, s); }
    public void PauseOnly() { L("PauseOnly"); }
    public void DrawBorder(sbyte a, sbyte b, sbyte c, sbyte d, byte k) { L("Border", a, b, c, d, k); }
    public void ClearBox(sbyte a, sbyte b, sbyte c, sbyte d) { L("ClearBox", a, b, c, d); }
    public void DrawWindow(sbyte a, sbyte b, sbyte c, sbyte d, byte k, byte col, string t) { L("Window", a, b, c, d, k, col, t); }
    public void DisplayCreatureName(TCharRecord c, byte col, byte row, bool poss) { L("Name", c, col, row, poss); }
    public void PrintItemLabel(TItemRec i, byte col, byte row) { L("ItemLabelAt", i, col, row); }
    public void DisplayNoncombatInterface() { L("Noncombat"); }
    public void PrintBottomLine(string s) { L("Bottom", s); }
    public void PrintNameString(TCharRecord c, string s, byte row, bool pause) { L("NameStr", c, s, row, pause); }
    public void ClearMessageWindow() { L("ClearMsg"); }
    public void PrintArmorClass(TCharRecord c, byte col, byte row) { L("AC", c, col, row); }
    public void PrintHitPoints(TCharRecord c, byte col, byte row, bool hot) { L("HP", c, col, row, hot); }
    public bool PlayerPortraitsHere() { bool r = Coin(); L("PortraitsHere", r); return r; }
    public void DisplayCharPortrait() { L("Portrait", GDVars.curChar); }
    public void SetNoncombatPalette() { L("NoncombatPalette"); }
    public void SetCombatPalette() { L("CombatPalette"); }

    // ---- MenuCore ----
    public void SelectChar(string q, bool addExit, ref TCharRecord sel)
    {
        L("SelectChar", q, addExit, sel);
        sel = (Spent() || R.Next(5) == 0) ? null : Any();
        L("SelectChar=", sel);
    }
    public byte YesNo(string q, byte a, byte b, byte c) { byte r = (byte)(R.Next(3) != 0 ? 'Y' : 'N'); L("YesNo", q, a, b, c, (char)r); return r; }
    static readonly byte[] ExtKeys = { C_Keys.UP, C_Keys.DOWN, C_Keys.HOME, C_Keys.END_KEY, C_Keys.F6, (byte)'E', 0 };
    byte PickKey(string menu, string extra)
    {
        var keys = new List<byte> { 0, (byte)'E', (byte)'E', (byte)'Q' };
        foreach (var t in (menu ?? "").Split(' ')) if (t.Length > 0) { keys.Add((byte)t[0]); keys.Add((byte)t[0]); keys.Add((byte)t[0]); }
        foreach (var ch in extra) keys.Add((byte)ch);
        return keys[R.Next(keys.Count)];
    }
    public byte MenuH(string q, string m, byte qc, byte mc, byte hc, bool reint, bool anim, ref bool ext)
    {
        L("MenuH", q, m, qc, mc, hc, reint, anim, MenuCore.extraKeys, GDVars.horizChoice);
        byte r;
        if (Script != null && Script.Count > 0) { var k = Script.Dequeue(); r = k.Item1; ext = k.Item2; }
        else if (Spent()) { ext = false; r = (byte)'E'; }
        else if (R.Next(5) == 0) { ext = true; r = ExtKeys[R.Next(ExtKeys.Length)]; }
        else { ext = false; r = PickKey(m, MenuCore.extraKeys + "S"); }
        L("MenuH=", r, ext);
        return r;
    }
    public byte MenuV(string q, string m, byte qc, byte mc, byte hc, sbyte a, sbyte b, sbyte c, sbyte d, TItemRec list, bool addExit, ref bool redraw, ref short idx, ref TItemRec sel)
    {
        L("MenuV", q, m, qc, mc, hc, a, b, c, d, ListSig(list), addExit, redraw, idx, sel, MenuCore.extraKeys);
        var nodes = new List<TItemRec>();
        for (var p = list; p != null; p = p.next) nodes.Add(p);
        byte key;
        if (Spent()) { key = (byte)'E'; sel = null; }
        else
        {
            key = PickKey(m, "VVI");
            sel = (nodes.Count == 0 || R.Next(6) == 0) ? null : nodes[R.Next(nodes.Count)];
        }
        idx = (short)R.Next(-1, 20);
        redraw = Coin();
        L("MenuV=", key, sel, idx, redraw);
        return key;
    }
    public void FreeStringList(ref TItemRec list) { L("FreeList", ListSig(list)); }

    // ---- FxEngine ----
    public bool RestoreHitPoints(TCharRecord c, byte hp, bool bypass)
    {
        bool r = R.Next(3) != 0;
        if (r) c.hpCur = (byte)Math.Min(255, c.hpCur + R.Next(0, hp + 1));
        L("Restore", c, hp, bypass, r, c.hpCur);
        return r;
    }
    public void AddEffect(TCharRecord c, byte id, ushort min, byte str, bool onRem)
    {
        var e = new TEffectRec { id = id, duration = min, strength = str, onRemove = onRem, next = c.effectsAddr };
        c.effectsAddr = e;
        L("AddFx", c, id, min, str, onRem);
    }
    public bool HasDisease(TCharRecord c) { bool r = Coin(); L("HasDisease", c, r); return r; }
    public void RemoveDiseases(TCharRecord c) { L("RemoveDiseases", c); }
    // an item's effect handler may ask for the inventory to be redrawn (EffHndlr's ExtraStr does)
    public void ToggleReady(TItemRec i) { i.equipped = (byte)(i.equipped == 0 ? 1 : 0); if (R.Next(3) == 0) GDVars.dsRedrawInventoryMenuFlag = 1; L("ToggleReady", i, i.equipped); }

    // ---- Magic ----
    public byte SpellMenu(byte mt, byte st, ref short idx, ref bool notEmpty)
    {
        L("SpellMenu", mt, st, idx, notEmpty, GDVars.curChar);
        notEmpty = Coin();
        idx = (short)R.Next(-1, 10);
        byte r = (byte)R.Next(0, 60);
        L("SpellMenu=", r, idx, notEmpty);
        return r;
    }
    public void UseItem(TItemRec it, ref bool done)
    {
        L("UseItem", it, done, GDVars.curChar, GDVars.ifcState);
        done = Coin();
        if (R.Next(4) == 0) GDVars.dsRedrawInventoryMenuFlag = 1;
        if (R.Next(3) == 0) RemoveItem(GDVars.curChar, it);
        L("UseItem=", done);
    }

    // ---- CharHlp ----
    public void RecalcStats(TCharRecord c)
    {
        int n = 0;
        for (var p = c.itemsAddr; p != null && n < 300; p = p.next) n++;
        c.numberOfItems = (byte)n;
        L("Recalc", c, n);
    }
    public void RemoveItem(TCharRecord c, TItemRec it)
    {
        TItemRec prev = null;
        bool found = false;
        for (var p = c.itemsAddr; p != null; prev = p, p = p.next)
            if (ReferenceEquals(p, it)) { if (prev == null) c.itemsAddr = p.next; else prev.next = p.next; found = true; break; }
        if (found) c.numberOfItems = unchecked((byte)(c.numberOfItems - 1));
        for (int s = 0; s < c.equipped.Length; s++) if (ReferenceEquals(c.equipped[s], it)) c.equipped[s] = null;
        L("RemoveItem", c, it, found);
    }
    public void AppendItemCopy(ref TItemRec list, TItemRec it)
    {
        var n = new TItemRec();
        CharHlp.CopyItem(it, n);
        n.next = null;
        if (list == null) list = n;
        else { var p = list; while (p.next != null) p = p.next; p.next = n; }
        L("AppendCopy", it, n);
    }
    public void ItemLabel(TItemRec it, bool inv)
    {
        it.name = "N" + it.itemType + "." + it.nameIds[1] + "." + it.unidMask + (inv ? "*" : "") + " ";
        L("ItemLabel", it, inv);
    }

    // ---- Stats ----
    public Stats.TStatBlock BlockOf(TCharRecord c) { Stats.TStatBlock b; Blocks.TryGetValue(c, out b); L("BlockOf", c, b != null); return b; }
    public void StatsCount(TCharRecord c, byte stat, int n) { L("Count", c, stat, n); }

    // ---- Money, MoneyCal, Vault ----
    public bool CheckOverload(TCharRecord c, int w, ref ushort takable)
    {
        bool r = Coin();
        takable = r ? (ushort)(R.Next(2) == 0 ? Math.Max(0, w - R.Next(0, 6)) : R.Next(0, 4000)) : (ushort)0;
        L("Overload", c, w, r, takable);
        return r;
    }
    public bool PayGold(TCharRecord c, int gp) { bool r = Coin(); L("PayGold", c, gp, r); return r; }
    public ushort InputNumberMax(ushort max, string q, byte col) { ushort v = (ushort)R.Next(0, max + 1); L("InputMax", max, q, col, v); return v; }
    public void TradeMoney(ref TCharRecord giver, ref TCharRecord rcv, ushort qty, sbyte m)
    {
        L("TradeMoney", giver, rcv, qty, m);
        int q = Math.Min(qty, (int)giver.coins[m]);
        giver.coins[m] = (ushort)(giver.coins[m] - q);
        rcv.coins[m] = unchecked((ushort)(rcv.coins[m] + q));
    }
    public void DropMoney(TCharRecord c, ushort qty, sbyte m)
    {
        L("DropMoney", c, qty, m, GDVars.ifcState);
        c.coins[m] = (ushort)(c.coins[m] - Math.Min(qty, (int)c.coins[m]));
    }
    public ushort SalePrice(TItemRec it) { ushort r = unchecked((ushort)(it.value * 7 + it.quantity * 3 + R.Next(0, 3))); L("SalePrice", it, r); return r; }
    public bool VaultHasRoom() { bool r = Coin(); L("VaultRoom", r); return r; }
    public void Deposit(TCharRecord c, TItemRec it)
    {
        L("Deposit", c, it);
        TItemRec prev = null;
        for (var p = c.itemsAddr; p != null; prev = p, p = p.next)
            if (ReferenceEquals(p, it)) { if (prev == null) c.itemsAddr = p.next; else prev.next = p.next; c.numberOfItems = unchecked((byte)(c.numberOfItems - 1)); break; }
    }
}

static partial class Tests
{
    static int vcLastLog;

    static void VcInstall(VcWorld w)
    {
        ViewChar.ClearScreenCall = w.ClearScreen;
        ViewChar.DisplayStringCall = w.DisplayString;
        ViewChar.DisplayCenteredCall = w.DisplayCentered;
        ViewChar.DisplayMessageWindowCall = w.DisplayMessageWindow;
        ViewChar.DisplayAndPauseCall = w.DisplayAndPause;
        ViewChar.PauseOnlyCall = w.PauseOnly;
        ViewChar.DrawBorderCall = w.DrawBorder;
        ViewChar.ClearBoxCall = w.ClearBox;
        ViewChar.DrawWindowCall = w.DrawWindow;
        ViewChar.DisplayCreatureNameCall = w.DisplayCreatureName;
        ViewChar.PrintItemLabelCall = w.PrintItemLabel;
        ViewChar.DisplayNoncombatInterfaceCall = w.DisplayNoncombatInterface;
        ViewChar.SelectCharCall = w.SelectChar;
        ViewChar.PrintBottomLineCall = w.PrintBottomLine;
        ViewChar.PrintNameStringCall = w.PrintNameString;
        ViewChar.ClearMessageWindowCall = w.ClearMessageWindow;
        ViewChar.PrintArmorClassCall = w.PrintArmorClass;
        ViewChar.PrintHitPointsCall = w.PrintHitPoints;
        ViewChar.YesNoMenuCall = w.YesNo;
        ViewChar.MenuHorizontalCall = w.MenuH;
        ViewChar.MenuVerticalCall = w.MenuV;
        ViewChar.FreeStringListCall = w.FreeStringList;
        ViewChar.PlayerPortraitsHereCall = w.PlayerPortraitsHere;
        ViewChar.DisplayCharPortraitCall = w.DisplayCharPortrait;
        ViewChar.SetNoncombatPaletteCall = w.SetNoncombatPalette;
        ViewChar.SetCombatPaletteCall = w.SetCombatPalette;
        ViewChar.RestoreHitPointsCall = w.RestoreHitPoints;
        ViewChar.AddEffectCall = w.AddEffect;
        ViewChar.HasDiseaseCall = w.HasDisease;
        ViewChar.RemoveDiseasesCall = w.RemoveDiseases;
        ViewChar.ToggleReadyItemCall = w.ToggleReady;
        ViewChar.SpellMenuCall = w.SpellMenu;
        ViewChar.UseItemCall = w.UseItem;
        ViewChar.RecalcStatsCall = w.RecalcStats;
        ViewChar.RemoveItemCall = w.RemoveItem;
        ViewChar.AppendItemCopyCall = w.AppendItemCopy;
        ViewChar.ItemLabelCall = w.ItemLabel;
        ViewChar.BlockOfCall = w.BlockOf;
        ViewChar.StatsCountCall = w.StatsCount;
        ViewChar.CheckOverloadCall = w.CheckOverload;
        ViewChar.PayGoldCall = w.PayGold;
        ViewChar.InputNumberMaxCall = w.InputNumberMax;
        ViewChar.TradeMoneyCall = w.TradeMoney;
        ViewChar.DropMoneyCall = w.DropMoney;
        ViewChar.ItemSalePriceCall = w.SalePrice;
        ViewChar.VaultHasRoomCall = w.VaultHasRoom;
        ViewChar.DepositItemCall = w.Deposit;
    }

    static readonly string VcLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghij";
    static string VcName(Random r, int min, int max)
    {
        int n = r.Next(min, max + 1);
        var sb = new StringBuilder();
        for (int i = 0; i < n; i++) sb.Append(VcLetters[r.Next(VcLetters.Length)]);
        return sb.ToString();
    }

    // An item; similar items (same type and ids) are common, so Join finds stacks.
    static TItemRec VcItem(Random r)
    {
        var i = new TItemRec();
        i.itemType = (byte)r.Next(0, 6);
        i.nameIds[1] = (byte)r.Next(0, 3); i.nameIds[2] = (byte)r.Next(0, 2); i.nameIds[3] = (byte)r.Next(0, 2);
        i.plus = (sbyte)r.Next(-1, 2); i.saveBonus = (sbyte)r.Next(0, 2);
        i.equipped = (byte)(r.Next(4) == 0 ? r.Next(1, 3) : 0);
        i.unidMask = (byte)(r.Next(3) == 0 ? r.Next(1, 8) : 0);
        i.cursed = (byte)(r.Next(8) == 0 ? 1 : 0);
        i.weight = (ushort)r.Next(0, 3);
        int q = r.Next(6);
        i.quantity = (byte)(q == 0 ? 0 : q == 1 ? 1 : q == 2 ? r.Next(200, 256) : q == 3 ? r.Next(100, 160) : r.Next(2, 30));
        i.value = (ushort)r.Next(0, 9000);
        i.charges = (byte)(r.Next(3) == 0 ? r.Next(0, 256) : r.Next(0, 3));
        i.effect = (byte)(r.Next(3) == 0 ? r.Next(0, 256) : 0);
        i.special = (byte)(r.Next(3) == 0 ? r.Next(0, 256) : r.Next(4) == 0 ? 0x7F : r.Next(0, 2) * 0x80);
        i.name = "raw" + r.Next(100);
        i.inUse = (byte)r.Next(0, 2);
        return i;
    }

    static VcWorld VcBuild(int seed)
    {
        var w = new VcWorld(seed);
        var r = w.R;
        GDVars.zone0 = new TGbvmZone0State();
        GDVars.zone0.antimagic = (ushort)(r.Next(5) == 0 ? 1 : 0);
        int[] ifcs = { C_Ifc.COMBAT, C_Ifc.VAULT, C_Ifc.SHOP_TEMPLE, C_Ifc.CAMPING, C_Ifc.OUTDOORS, C_Ifc.INDOORS };
        GDVars.ifcState = (byte)ifcs[r.Next(ifcs.Length)];
        GDVars.portraitsOn = (byte)r.Next(0, 2);
        GDVars.stingArg = r.Next(2) == 0;
        GDVars.curCol = (byte)r.Next(0, 40); GDVars.curRow = (byte)r.Next(0, 25);
        GDVars.dsRedrawInventoryMenuFlag = (byte)(r.Next(4) == 0 ? 1 : 0);
        GDVars.horizChoice = (byte)r.Next(0, 5);
        GDVars.treasureMoney = new TMoney();
        for (int m = 0; m <= 6; m++) GDVars.treasureMoney[m] = r.Next(0, 3) == 0 ? 0 : r.Next(0, 100000);
        MenuCore.extraKeys = "";
        GDVars.baseItems = new TBaseItems();
        for (int t = 0; t <= 128; t++)
        {
            var bi = GDVars.baseItems.Data[t];
            bi.slot = (byte)(r.Next(3) == 0 ? r.Next(C_Item.SLOT_MAGE_SCROLL, C_Item.SLOT_LAST_READABLE + 1) : r.Next(0, 16));
            bi.classMask = (byte)r.Next(0, 256);
            bi.dmgLgRolls = (byte)r.Next(0, 256); bi.dmgLgDice = (byte)r.Next(0, 256);
        }
        int n = r.Next(1, 5);
        TCharRecord prev = null;
        for (int k = 0; k < n; k++)
        {
            var c = new TCharRecord();
            c.name = VcName(r, 1, 15);
            c.gender = (byte)r.Next(0, 2); c.race = (sbyte)r.Next(0, 8); c.age = (ushort)r.Next(0, 65536);
            c.alignment = (byte)r.Next(0, 9);
            c.charClass = (sbyte)(r.Next(3) == 0 ? C_Class.PALADIN : r.Next(0, 17));
            for (int j = 0; j <= 7; j++) c.classLevels[j] = (sbyte)(r.Next(3) == 0 ? r.Next(-3, 20) : 0);
            if (r.Next(2) == 0) c.classLevels[C_Level.PALADIN] = (sbyte)r.Next(-8, 40);
            c.experience = r.Next(3) == 0 ? -r.Next(0, 1000) : r.Next(0, 3000000);
            c.status = (byte)(r.Next(2) == 0 ? C_Status.OKAY : r.Next(0, 9));
            c.npc = (byte)(r.Next(3) == 0 ? r.Next(0x80, 0x100) : r.Next(0, 0x80));
            c.enabled = (byte)r.Next(0, 2);
            c.strCur = (byte)(r.Next(3) == 0 ? 18 : r.Next(3, 26)); c.intCur = (byte)r.Next(3, 19); c.wisCur = (byte)r.Next(3, 19);
            c.dexCur = (byte)r.Next(3, 19); c.conCur = (byte)r.Next(3, 19); c.chaCur = (byte)r.Next(3, 19);
            c.strExc = (byte)(r.Next(2) == 0 ? 0 : r.Next(1, 101));
            c.thac0Cur = (byte)r.Next(30, 80);
            c.curRollsA[1] = (byte)r.Next(0, 5); c.curDiceA[1] = (byte)r.Next(0, 13); c.curModA[1] = (sbyte)r.Next(-128, 128);
            c.encumbrance = (ushort)r.Next(0, 65536); c.movementCur = (byte)r.Next(0, 256);
            for (int s = 0; s <= 4; s++) c.saves[s] = (byte)r.Next(0, 30);
            c.saveBonus = (sbyte)r.Next(-10, 15);
            c.hpMax = (byte)r.Next(1, 120); c.hpCur = (byte)r.Next(0, c.hpMax + 1);
            for (int m = 0; m <= 6; m++) c.coins[m] = (ushort)(r.Next(2) == 0 ? 0 : r.Next(1, 65536));
            if (r.Next(4) == 0) for (int m = 0; m <= 6; m++) c.coins[m] = 0;
            for (int s = 0; s <= 20; s++) c.memorizedSpells[s] = (byte)(r.Next(4) == 0 ? r.Next(1, 256) : 0);
            if (r.Next(4) == 0) { for (int s = 0; s <= 20; s++) c.memorizedSpells[s] = 0; if (r.Next(3) != 0) c.memorizedSpells[r.Next(21)] = (byte)(r.Next(2) == 0 ? 1 : 0x80); }
            int items = r.Next(0, 7);
            TItemRec tail = null;
            for (int j = 0; j < items; j++) { var it = VcItem(r); if (tail == null) c.itemsAddr = it; else tail.next = it; tail = it; }
            c.numberOfItems = (byte)(r.Next(6) == 0 ? r.Next(0, 20) : items);
            var list = new List<TItemRec>();
            for (var p = c.itemsAddr; p != null; p = p.next) list.Add(p);
            if (list.Count > 0 && r.Next(2) == 0) c.equipped[C_Equip.WEAPON] = list[r.Next(list.Count)];
            if (list.Count > 0 && r.Next(2) == 0) c.equipped[C_Equip.ARMOR] = list[r.Next(list.Count)];
            int[] fx = { C_Effect.RECENT_HEAL, C_Effect.RECENT_CURE, C_Effect.SLOWED, C_Effect.HASTED, 1, 7 };
            for (int j = r.Next(0, 4); j > 0; j--)
                c.effectsAddr = new TEffectRec { id = (byte)fx[r.Next(fx.Length)], strength = (byte)r.Next(0, 256), duration = (ushort)r.Next(0, 9000), next = c.effectsAddr };
            c.itemLimits = (byte)r.Next(0, 256);
            c.combatAddr = new TCombatRec { itemUseAllowed = (byte)r.Next(0, 2) };
            if (prev == null) GDVars.creatureList = c; else prev.nextCharAddr = c;
            prev = c;
            w.All.Add(c);
            if (r.Next(4) != 0)
            {
                var b = new Stats.TStatBlock();
                for (int i = 1; i <= Stats.STAT_COUNT; i++) b.counts[i] = r.Next(4) == 0 ? r.Next(int.MinValue, int.MaxValue) : r.Next(-50, 5000);
                int kinds = r.Next(3) == 0 ? r.Next(0, 4) : r.Next(0, Stats.KINDS + 1);
                bool small = r.Next(3) == 0;
                b.kindCount = (byte)kinds;
                for (int i = 1; i <= Stats.KINDS; i++)
                {
                    b.kills[i].name = VcName(r, 0, 15);
                    b.kills[i].count = small ? r.Next(0, 10) : r.Next(5) == 0 ? r.Next(int.MinValue, int.MaxValue) : r.Next(0, 300);
                }
                w.Blocks[c] = b;
            }
        }
        GDVars.curChar = w.All[r.Next(w.All.Count)];
        w.Budget = r.Next(1, 30);
        return w;
    }

    // Run the oracle on one copy of the scenario and the port on another; compare exceptions, call logs and state.
    static bool VcPair(int seed, string what, Action<VcWorld, VcOracle> oracle, Action<VcWorld> port, Action<VcWorld> tweak = null)
    {
        var a = VcBuild(seed);
        if (tweak != null) tweak(a);
        var o = new VcOracle(a);
        string beforeA = a.Snap(null);
        string exA = null;
        try { oracle(a, o); }
        catch (Exception e)
        {
            if (e is InvalidOperationException && e.Message == "runaway call log") throw;
            exA = e.GetType().Name;
            if (Environment.GetEnvironmentVariable("VC_EX") != null) Console.WriteLine("  " + what + " seed " + seed + ": " + e.GetType().Name + " " + e.StackTrace.Split('\n')[0]);
        }
        string snapA = a.Snap(o.tradeTarget);

        var b = VcBuild(seed);
        if (tweak != null) tweak(b);
        EqS(beforeA, b.Snap(null), what + " seed " + seed + " identical start");
        VcInstall(b);
        ViewChar.tradeTarget = null;
        string exB = null;
        try { port(b); }
        catch (Exception e) { exB = e.GetType().Name; if (e is NotSupportedException || e is NotImplementedException) throw; }
        string snapB = b.Snap(ViewChar.tradeTarget);

        what = what + " seed " + seed;
        EqS(exA ?? "-", exB ?? "-", what + " exception");
        int n = Math.Min(a.Log.Count, b.Log.Count);
        for (int i = 0; i < n; i++)
            if (a.Log[i] != b.Log[i])
                throw new Exception("FAILED: " + what + " call " + i + "\nA:" + string.Join("\n", a.Log.Skip(Math.Max(0, i - 6)).Take(10)) + "\nB:" + string.Join("\n", b.Log.Skip(Math.Max(0, i - 6)).Take(10)));
        checks += n;
        Eq(a.Log.Count, b.Log.Count, what + " log length");
        EqS(snapA, snapB, what + " state");
        vcLastLog = a.Log.Count;
        return exA != null;
    }

    // n scenarios; at most maxThrowPct percent may end in the shared exception.
    static void VcMany(string what, int n, Action<VcWorld, VcOracle> oracle, Action<VcWorld> port, Action<VcWorld> tweak = null, int maxThrowPct = 25)
    {
        int threw = 0;
        long logs = 0;
        int h = 17;
        foreach (char ch in what) h = unchecked(h * 31 + ch);
        for (int s = 0; s < n; s++)
        {
            if (VcPair((h & 0xFFFF) ^ (s * 7919 + 13), what, oracle, port, tweak)) threw++;
            logs += vcLastLog;
        }
        if (Environment.GetEnvironmentVariable("VC_STATS") != null) Console.WriteLine("  " + what + ": threw " + threw + "/" + n + ", mean log " + (logs / (double)n).ToString("0.0"));
        Yes(threw * 100 <= n * maxThrowPct, what + ": " + threw + " of " + n + " scenarios threw");
    }

    static void VcGuarded(Action group)
    {
        var saved = new List<Tuple<System.Reflection.FieldInfo, object, object>>();
        foreach (var t in new[] { typeof(GDVars), typeof(ViewChar), typeof(MenuCore) })
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
