// Magic (src/magic.pas) checks, part 1: the recording world (fake FxEngine and CbtMap, logging UI), scenario builder,
// snapshots and the oracle/port pair runner. Expected behaviour comes from MgOracle (MagicOracle.cs), a separate model
// written from the Pascal text; each scenario is built twice from one seed, once for the oracle and once for the port,
// and the results, the ordered call logs and the state snapshots must match.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Por.Core;
using static Por.Core.Types;

sealed class MgWorld : IMagicEffects, IMagicCombatMap
{
    public readonly List<string> Log = new List<string>();
    public Random R;
    public readonly List<TCharRecord> All = new List<TCharRecord>();
    public readonly Dictionary<TCharRecord, int> Slot = new Dictionary<TCharRecord, int>();
    public readonly Dictionary<TCharRecord, sbyte> X = new Dictionary<TCharRecord, sbyte>();
    public readonly Dictionary<TCharRecord, sbyte> Y = new Dictionary<TCharRecord, sbyte>();
    int steps;

    public static string F(object o)
    {
        if (o == null) return "nil";
        var c = o as TCharRecord; if (c != null) return c.name;
        var i = o as TItemRec; if (i != null) return "I:" + i.name + "/" + i.inUse;
        var e = o as TEffectRec; if (e != null) return "E:" + e.id + "/" + e.strength + "/" + e.duration + "/" + e.onRemove;
        if (o is bool) return (bool)o ? "T" : "F";
        return o.ToString();
    }
    public void L(string name, params object[] a)
    {
        if (++steps > 100000) throw new InvalidOperationException("runaway call log");
        Log.Add(name + "(" + string.Join(",", a.Select(F)) + ")");
    }
    public bool Coin() { return R.Next(2) == 0; }
    public static string ListSig(TItemRec p)
    {
        var sb = new StringBuilder("[");
        int n = 0;
        for (; p != null && n < 300; p = p.next, n++) sb.Append(p.name).Append('#').Append(p.inUse).Append(';');
        return sb.Append(']').ToString();
    }

    // ---- effect lists (the fake FxEngine keeps them so later predicates see its changes) ----
    public static void AddFx(TCharRecord c, byte id, ushort minutes, byte strength, bool onRemove)
    {
        c.effectsAddr = new TEffectRec { id = id, duration = minutes, strength = strength, onRemove = onRemove, next = c.effectsAddr };
    }
    public static bool Unlink(TCharRecord c, TEffectRec target, byte id)
    {
        TEffectRec prev = null;
        for (var p = c.effectsAddr; p != null; prev = p, p = p.next)
            if (target != null ? ReferenceEquals(p, target) : p.id == id)
            {
                if (prev == null) c.effectsAddr = p.next; else prev.next = p.next;
                return true;
            }
        return false;
    }

    // ---- IMagicEffects ----
    public bool RollSavingThrow(TCharRecord c, sbyte t, sbyte m) { bool r = Coin(); L("Save", c, t, m, r); return r; }
    public void TriggerEffectsWithCondition(byte cond, TCharRecord c) { L("Trigger", cond, c); }
    public bool AttackRoll(TCharRecord a, TCharRecord d, byte ac) { bool r = R.Next(3) != 0; L("AttackRoll", a, d, ac, r); return r; }
    public void InflictNonWeaponDamage(TCharRecord c, byte dmg, byte red, bool saved) { L("NonWeapon", c, dmg, red, saved, GDVars.elementMask, GDVars.gMultiTarget); }
    public void AddEffectWithMessage(TCharRecord c, byte id, ushort min, byte str, bool onRem, byte neg, bool saved, string msg)
    {
        L("AddFxMsg", c, id, min, str, onRem, neg, saved, msg, GDVars.gMultiTarget);
        if (!(saved && neg == 1)) AddFx(c, id, min, str, onRem);
    }
    public void RemoveInvisibility(TCharRecord c) { L("RemoveInvis", c); }
    public bool RestoreHitPoints(TCharRecord c, byte hp, bool bypass) { bool r = Coin(); L("Restore", c, hp, bypass, r); return r; }
    public void CallEffectHandler(byte id, TCharRecord c, object p, bool removed) { L("CallFx", id, c, p, removed); }
    public bool AdjustForNewStrength(TCharRecord c, byte bs, byte ex, ref byte enc) { L("AdjustStr", c, bs, ex, enc); enc = (byte)R.Next(256); bool r = Coin(); L("AdjustStr=", enc, r); return r; }
    public void AddEffect(TCharRecord c, byte id, ushort min, byte str, bool onRem) { L("AddFx", c, id, min, str, onRem); AddFx(c, id, min, str, onRem); }
    public bool RemoveEffectWithMessage(TCharRecord c, byte id) { bool r = Unlink(c, null, id); L("RemoveFxMsg", c, id, r, GDVars.effectLock); return r; }
    public bool ReviveDeadCreature(TCharRecord c, byte hp) { bool r = Coin(); L("Revive", c, hp, r); if (r) { c.status = 0; c.hpCur = hp; } return r; }
    public void ConsiderStinkingCloud(TCharRecord c) { L("ConsiderStink", c); }
    public void RemoveEffect(TCharRecord c, byte id, object eff) { bool r = Unlink(c, (TEffectRec)eff, id); L("RemoveFx", c, id, eff, r, GDVars.effectLock); }

    // ---- IMagicCombatMap ----
    public sbyte CombatCellX(TCharRecord c) { sbyte v; if (!X.TryGetValue(c, out v)) v = 0; L("CellX", c, v); return v; }
    public sbyte CombatCellY(TCharRecord c) { sbyte v; if (!Y.TryGetValue(c, out v)) v = 0; L("CellY", c, v); return v; }
    public void SetCreatureFacing(TCharRecord c, byte dir, byte frame, byte erase) { L("Facing", c, dir, frame, erase); }
    public bool CombatIconVisible(TCharRecord c, byte whole) { bool r = Coin(); L("IconVisible", c, whole, r); return r; }
    public byte CountTargets(TCharRecord c, byte range) { byte r = (byte)R.Next(3); L("CountTargets", c, range, r); return r; }
    public byte Occupant(int x, int y)
    {
        // a creature with icon size 2 covers the 2x2 block from its cell, as on the combat map
        foreach (var kv in Slot)
        {
            int d = kv.Key.iconDimensions > 1 ? 1 : 0;
            if (x >= X[kv.Key] && x <= X[kv.Key] + d && y >= Y[kv.Key] && y <= Y[kv.Key] + d) return (byte)kv.Value;
        }
        return 0;
    }
    public void GetArenaContents(sbyte x, sbyte y, ref byte crIdx, ref byte terr)
    {
        if (x < 0 || x > 49 || y < 0 || y > 24) { crIdx = 0; terr = 0; }
        else { crIdx = Occupant(x, y); terr = GDVars.arenaPtr.cells[y, x]; }
        L("Contents", x, y, crIdx, terr);
    }
    public bool IsValidCreatureLoc(TCharRecord c, byte x, byte y, bool moving) { bool r = R.Next(4) != 0; L("ValidLoc", c, x, y, moving, r); return r; }
    public void ScrollView(sbyte x, sbyte y, byte delta, byte dir) { L("ScrollView", x, y, delta, dir); }

    // ---- converted-unit call points ----
    public byte MenuV(string q, string m, byte qc, byte mc, byte hc, sbyte a, sbyte b, sbyte c, sbyte d, TItemRec list, bool addExit, ref bool redraw, ref short idx, ref TItemRec sel)
    {
        L("MenuV", q, m, qc, mc, hc, a, b, c, d, ListSig(list), addExit, redraw, idx, sel, MenuCore.listOnly, GDVars.vertTop);
        var nodes = new List<TItemRec>();
        for (var p = list; p != null; p = p.next) nodes.Add(p);
        byte[] keys = { 0, 13, (byte)'C', (byte)'E', (byte)'L', (byte)'M', (byte)'S', (byte)'X', 27, (byte)'N' };
        byte key = keys[R.Next(keys.Length)];
        if (R.Next(4) == 0 || nodes.Count == 0) sel = null; else sel = nodes[R.Next(nodes.Count)];
        idx = (short)R.Next(-1, 40);
        redraw = Coin();
        return key;
    }
    public byte YesNo(string q, byte a, byte b, byte c) { byte r = (byte)(Coin() ? 'Y' : 'N'); L("YesNo", q, a, b, c, (char)r); return r; }
    public void SelectChar(string q, bool addExit, ref TCharRecord sel)
    {
        L("SelectChar", q, addExit, sel);
        sel = R.Next(3) == 0 ? null : All[R.Next(All.Count)];
    }
    public void CastUI(byte spell, bool b, ref bool sel)
    {
        L("CastUI", spell, b, sel, GDVars.usingItem);
        sel = R.Next(3) != 0;
        GDVars.gTargetX = (sbyte)R.Next(0, 50); GDVars.gTargetY = (sbyte)R.Next(0, 25);
    }
    public void RemoveItem(TCharRecord c, TItemRec it)
    {
        L("RemoveItem", c, it);
        TItemRec prev = null;
        for (var p = c.itemsAddr; p != null; prev = p, p = p.next)
            if (ReferenceEquals(p, it)) { if (prev == null) c.itemsAddr = p.next; else prev.next = p.next; break; }
    }
}

static partial class Tests
{
    static MgWorld MW;
    static int mgExceptions, mgRuns; static long mgLogLines;

    // Base item slots used by the builder: 1 mage scroll, 2 cleric scroll, 3 other readable, 4 wand (slot 0), 5 slot 5
    static readonly byte[] MgSlots = { 0, C_Item.SLOT_MAGE_SCROLL, C_Item.SLOT_CLERIC_SCROLL, C_Item.SLOT_LAST_READABLE, 0, 5 };
    static readonly byte[] MgEffIds = { 11, 12, 15, 16, 22, 23, 33, 34, 36, 39, 42, 43, 44, 50, 53, 55, 57, 88, 140, 141, 5, 9 };
    static readonly byte[] MgTerrains = { 1, 5, 2, 5, 5, 30, 31, 0, 10, 20, 40, 64, 5, 5 };
    static readonly string[] MgNames = { "Al", "Bertrand", "Cy", "Dorothea of the Vale", "Ed", "Flint", "Gwyneth", "Hal", "Ivo", "Jo", "Kara", "Lem" };

    static TCharRecord MgCreature(Random b, int k)
    {
        var c = new TCharRecord();
        c.name = MgNames[k % MgNames.Length] + k;
        c.hostile = (sbyte)(b.Next(5) == 0 ? b.Next(-1, 3) : b.Next(2));
        int[] types = { 0, 0, 0, 1, 2, 4, 14, 14, 5 };
        c.charType = (byte)types[b.Next(types.Length)];
        c.iconDimensions = (byte)(b.Next(4) == 0 ? 2 : 1);
        int[] stats = { 0, 0, 0, C_Status.ANIMATED, C_Status.DEAD, C_Status.DEAD, 2, 4 };
        c.status = (byte)stats[b.Next(stats.Length)];
        c.hpMax = (byte)b.Next(1, 120); c.hpCur = (byte)(b.Next(4) == 0 ? 0 : b.Next(0, c.hpMax + 1)); c.hpRolled = (byte)b.Next(256);
        c.levelHighest = (sbyte)b.Next(-1, 9);
        c.race = (sbyte)b.Next(0, 8);
        c.chaCur = (byte)b.Next(3, 26); c.strCur = (byte)(b.Next(8) == 0 ? b.Next(240, 256) : b.Next(3, 26)); c.strExc = (byte)(b.Next(3) == 0 ? b.Next(0, 101) : 0);
        c.quickfight = (byte)b.Next(2); c.levelUndead = (sbyte)b.Next(0, 3); c.attackLevel = (byte)b.Next(10); c.movementBase = (byte)b.Next(3, 13);
        c.npc = (byte)(b.Next(3) == 0 ? 0x80 + b.Next(0, 0x7F) : b.Next(0, 0x80));
        c.experience = b.Next(0, 200000);
        c.itemLimits = (byte)b.Next(256);
        c.drainedLevels = (byte)(b.Next(3) == 0 ? b.Next(1, 4) : 0); c.drainedHps = (byte)b.Next(0, 60);
        c.acCur = (byte)b.Next(40, 70);
        for (int i = 0; i < 8; i++) c.classLevels[i] = (sbyte)(b.Next(3) == 0 ? b.Next(1, 13) : (b.Next(10) == 0 ? -1 : 0));
        for (int i = 0; i <= 20; i++) { int r = b.Next(6); c.memorizedSpells[i] = (byte)(r == 0 ? b.Next(1, 57) : r == 1 ? 0x80 | b.Next(0, 57) : r == 2 ? b.Next(1, 57) : 0); }
        for (int i = 1; i <= 56; i++) c.spellsKnown[i] = (byte)(b.Next(3) == 0 ? b.Next(1, 3) : 0);
        for (int r = 0; r <= 1; r++) for (int col = 1; col <= 3; col++) c.slots[r, col] = (byte)(b.Next(2) == 0 ? 0 : b.Next(1, 4));
        c.combatAddr = new TCombatRec { castAllowed = (byte)b.Next(2), direction = (byte)b.Next(8) };
        c.combatAddr.other = b.Next(2) == 0 ? null : c;
        c.enabled = (byte)(b.Next(5) == 0 ? 0 : 1); c.combatAddr.itemUseAllowed = (byte)b.Next(2);
        TItemRec last = null;
        int items = b.Next(0, 5);
        for (int i = 0; i < items; i++)
        {
            var it = new TItemRec();
            it.name = c.name + "-item" + i;
            it.itemType = (byte)b.Next(1, 6);
            int[] sp = { 0, 0, 3, 10, 21, 34, 47, 51, 56, 0x80 | 15, 0x80 | 23, 60, 64, 1, 45, 12, 0x80, 0x80 | 36 };
            for (int s = 1; s <= 3; s++) it.spells[s] = (byte)(b.Next(3) == 0 ? b.Next(0, 256) : sp[b.Next(sp.Length)]);
            it.nameIds[2] = (byte)b.Next(205, 216);
            it.unidMask = (byte)(b.Next(2) == 0 ? 0 : 1);
            it.equipped = (byte)b.Next(2); it.cursed = (byte)(b.Next(4) == 0 ? 1 : 0); it.quantity = (byte)b.Next(0, 4);
            if (last == null) c.itemsAddr = it; else last.next = it;
            last = it;
        }
        int effs = b.Next(0, 5);
        for (int i = 0; i < effs; i++)
            MgWorld.AddFx(c, MgEffIds[b.Next(MgEffIds.Length)], (ushort)b.Next(0, 300), (byte)(b.Next(4) == 0 ? 255 : b.Next(0, 255)), b.Next(2) == 0);
        return c;
    }

    // One scenario world. Everything below comes from seed, so two calls build identical, separate worlds.
    static MgWorld MgBuild(int seed)
    {
        var b = new Random(seed);
        var w = new MgWorld { R = new Random(seed * 31 + 7) };
        var dice = new Random(seed ^ 0x5A5A);
        Dice.RandomBelow = s => dice.Next(s);
        GDVars.baseItems = new TBaseItems();
        for (int i = 1; i < MgSlots.Length; i++) { GDVars.baseItems[i].slot = MgSlots[i]; GDVars.baseItems[i].classMask = (byte)b.Next(256); }
        GDVars.zone0 = new TGbvmZone0State();
        GDVars.zone0.view3dFlag = (ushort)(b.Next(2) == 0 ? 0 : 1);
        // terrain costs 2 and 254 (the shipped table has only 1 and 255) so the cost comparisons are checked at their edges
        GDConsts.terrainInfo[20] = new TTerrainRec { cost = 2 }; GDConsts.terrainInfo[40] = new TTerrainRec { cost = 254 };
        GDVars.arenaPtr = new TArena();
        for (int y = 0; y < 25; y++) for (int x = 0; x < 50; x++) GDVars.arenaPtr.cells[y, x] = MgTerrains[b.Next(MgTerrains.Length)];
        int n = b.Next(3, 10);
        TCharRecord prev = null;
        Array.Clear(GDVars.creatureArr, 0, GDVars.creatureArr.Length);
        int slot = 0;
        for (int k = 0; k < n; k++)
        {
            var c = MgCreature(b, k);
            w.All.Add(c);
            if (prev == null) GDVars.creatureList = c; else prev.nextCharAddr = c;
            prev = c;
            if (b.Next(5) != 0)
            {
                slot++;
                GDVars.creatureArr[slot] = c; w.Slot[c] = slot;
                w.X[c] = (sbyte)b.Next(0, 50); w.Y[c] = (sbyte)b.Next(0, 25);
            }
        }
        GDVars.arenaCount = (byte)slot;
        GDVars.curChar = w.All[b.Next(n)];
        GDVars.ifcState = (byte)(b.Next(2) == 0 ? C_Ifc.COMBAT : b.Next(0, 5));
        GDVars.usingItem = b.Next(3) == 0;
        GDVars.roundCounter = (byte)(b.Next(2) == 0 ? 0 : b.Next(1, 9));
        GDVars.castTarget = b.Next(2) == 0 ? null : w.All[b.Next(n)];
        GDVars.breathAim = b.Next(2) == 0;
        GDVars.gTargetX = (sbyte)b.Next(0, 50); GDVars.gTargetY = (sbyte)b.Next(0, 25);
        GDVars.gMultiTarget = (byte)b.Next(2); GDVars.elementMask = (byte)b.Next(16); GDVars.effectLock = 0;
        GDVars.vertTop = (short)b.Next(0, 5); GDVars.gDamageDice = 0;
        GDVars.dsCurrentSpellBeing = (byte)b.Next(1, 68);
        Array.Clear(GDVars.dsTargetCreaturePtr, 0, GDVars.dsTargetCreaturePtr.Length);
        int tc = b.Next(0, Math.Min(n, 6) + 1);
        for (int i = 1; i <= tc; i++) GDVars.dsTargetCreaturePtr[i] = b.Next(8) == 0 ? null : w.All[b.Next(n)];
        GDVars.spellTargetCount = (byte)tc;
        // existing clouds
        GDVars.stinkHead = null;
        TStinkRec lastCloud = null;
        int clouds = b.Next(0, 4);
        for (int i = 0; i < clouds; i++)
        {
            var s = new TStinkRec { caster = b.Next(2) == 0 ? GDVars.curChar : w.All[b.Next(n)], ulX = (sbyte)b.Next(1, 48), ulY = (sbyte)b.Next(1, 23), idx = (byte)i };
            for (int j = 1; j <= 4; j++) { s.exists[j] = (byte)b.Next(2); s.under[j] = MgTerrains[b.Next(MgTerrains.Length)]; }
            if (lastCloud == null) GDVars.stinkHead = s; else lastCloud.next = s;
            lastCloud = s;
        }
        GDVars.downedCount = (byte)b.Next(0, 9);
        for (int j = 1; j <= 8; j++) GDVars.downed[j] = new TDownedRec { cr = w.All[b.Next(n)], x = (sbyte)b.Next(0, 50), y = (sbyte)b.Next(0, 25), terrain = MgTerrains[b.Next(MgTerrains.Length)] };
        MenuCore.listOnly = b.Next(2) == 0;
        // unit state starts empty on both sides
        Magic.spellList = null; Array.Clear(Magic.spellIds, 0, Magic.spellIds.Length); Array.Clear(Magic.spellScrolls, 0, Magic.spellScrolls.Length);
        Array.Clear(Magic.scrollPtrs, 0, Magic.scrollPtrs.Length); Magic.scrollCount = 0; Magic.dsCurrentScrollPtr = null;
        MgOracle.Reset();
        MgInstall(w);
        return w;
    }

    static void MgInstall(MgWorld w)
    {
        MW = w;
        MgOracle.W = w;
        Magic.Effects = w; Magic.CombatMap = w;
        Magic.MenuVerticalCall = w.MenuV;
        Magic.YesNoMenuCall = w.YesNo;
        Magic.FreeStringListCall = (ref TItemRec l) => w.L("FreeList", MgWorld.ListSig(l));
        Magic.DrawWindowCall = (a, b, c, d, e, f, s) => w.L("DrawWindow", a, b, c, d, e, f, s);
        Magic.ClearBoxCall = (a, b, c, d) => w.L("ClearBox", a, b, c, d);
        Magic.DisplayStringCall = (c, r, col, s) => w.L("DS", c, r, col, s);
        Magic.PauseOnlyCall = () => w.L("Pause");
        Magic.DisplayNoncombatInterfaceCall = () => w.L("NoncombatIfc");
        Magic.SelectCharCall = w.SelectChar;
        Magic.BuildIconFacingsCall = a => w.L("BuildIcon", a);
        Magic.CombatAnimateMovingIconCall = (a, b, c, d, f, p) => w.L("Fly", a, b, c, d, f, p);
        Magic.PrintBottomLineCall = s => w.L("Bottom", s);
        Magic.ClearMessageWindowCall = () => w.L("CMW");
        Magic.PrintHpRestoreMsgCall = c => w.L("HpRestoreMsg", c);
        Magic.PrintNameStringCall = (c, s, r, p) => w.L("PNS", c, s, r, p);
        Magic.PrintNameStringAnimCall = (c, e, s) => w.L("PNSA", c, e, s);
        Magic.DisplayCreatureNameCall = (c, col, r, p) => w.L("DCN", c, col, r, p);
        Magic.DisplayCombatInterfaceCall = () => w.L("CombatIfc");
        Magic.PrintItemLabelCall = (it, c, r) => w.L("ItemLabel", it, c, r);
        Magic.SoundSetAutoStopCall = s => w.L("AutoStop", s);
        Magic.SoundCommandCall = s => w.L("Snd", s);
        Magic.RemoveSpellCall = (c, s) => w.L("RemoveSpell", c, s);
        Magic.RemoveItemCall = w.RemoveItem;
        Magic.RecalcStatsCall = c => w.L("RecalcStats", c);
        Magic.EndActionsCall = c => { bool r = w.Coin(); w.L("EndActions", c, r); return r; };
        Magic.StatsCountCall = (c, s, k) => w.L("StatsCount", c, s, k);
        Magic.RecalcBaseSkillsCall = c => w.L("RecalcBaseSkills", c);
        Magic.TargetAreaCall = (x, y, r) => w.L("TargetArea", x, y, r);
        GDVars.castTargetMenu = w.CastUI;
        for (int i = 1; i <= 67; i++) { int k = i; GDVars.spellFns[i] = () => w.L("SpellFn", k, GDVars.dsCurrentSpellBeing, GDVars.usingItem, GDVars.gMultiTarget); }
    }

    static string MgUnitState(TItemRec list, byte[] ids, TItemRec[] scrolls, TItemRec[] ptrs, byte count, TItemRec cur)
    {
        var sb = new StringBuilder();
        sb.Append("list=").Append(MgWorld.ListSig(list));
        sb.Append(" ids=").Append(string.Join(",", ids));
        sb.Append(" scrolls=").Append(string.Join(",", scrolls.Select(MgWorld.F)));
        sb.Append(" ptrs=").Append(string.Join(",", ptrs.Select(MgWorld.F)));
        sb.Append(" count=").Append(count).Append(" cur=").Append(MgWorld.F(cur));
        return sb.ToString();
    }

    static string MgSnap(MgWorld w, string unit)
    {
        var sb = new StringBuilder();
        foreach (var c in w.All)
        {
            sb.Append(c.name).Append(':').Append(c.hostile).Append(',').Append(c.charType).Append(',').Append(c.iconDimensions).Append(',').Append(c.status)
              .Append(',').Append(c.hpMax).Append(',').Append(c.hpCur).Append(',').Append(c.hpRolled).Append(',').Append(c.levelHighest).Append(',').Append(c.chaCur)
              .Append(',').Append(c.strCur).Append(',').Append(c.strExc).Append(',').Append(c.quickfight).Append(',').Append(c.levelUndead).Append(',').Append(c.attackLevel)
              .Append(',').Append(c.movementBase).Append(',').Append(c.npc).Append(',').Append(c.experience).Append(',').Append(c.drainedLevels).Append(',').Append(c.drainedHps)
              .Append(" cl=").Append(string.Join(".", c.classLevels)).Append(" mem=").Append(string.Join(".", c.memorizedSpells.Data))
              .Append(" known=").Append(string.Join("", c.spellsKnown)).Append(" other=").Append(MgWorld.F(c.combatAddr.other)).Append(" dir=").Append(c.combatAddr.direction);
            sb.Append(" items=");
            for (var it = c.itemsAddr; it != null; it = it.next)
                sb.Append(it.name).Append('/').Append(it.cursed).Append('/').Append(it.unidMask).Append('/').Append(it.quantity).Append('/').Append(string.Join(".", it.variantData)).Append('/').Append(it.nameIds[2]).Append(';');
            sb.Append(" fx=");
            for (var e = c.effectsAddr; e != null; e = e.next) sb.Append(MgWorld.F(e)).Append(';');
            sb.Append('\n');
        }
        sb.Append("targets=").Append(string.Join(",", GDVars.dsTargetCreaturePtr.Select(MgWorld.F))).Append(" count=").Append(GDVars.spellTargetCount);
        sb.Append(" multi=").Append(GDVars.gMultiTarget).Append(" tx=").Append(GDVars.gTargetX).Append(" ty=").Append(GDVars.gTargetY).Append(" elem=").Append(GDVars.elementMask)
          .Append(" lock=").Append(GDVars.effectLock).Append(" castTarget=").Append(MgWorld.F(GDVars.castTarget)).Append(" being=").Append(GDVars.dsCurrentSpellBeing)
          .Append(" using=").Append(GDVars.usingItem).Append(" vertTop=").Append(GDVars.vertTop).Append(" breathAim=").Append(GDVars.breathAim)
          .Append(" dd=").Append(GDVars.gDamageDice).Append(" listOnly=").Append(MenuCore.listOnly).Append('\n');
        for (var s = GDVars.stinkHead; s != null; s = s.next)
            sb.Append("cloud ").Append(MgWorld.F(s.caster)).Append(' ').Append(s.idx).Append(' ').Append(s.ulX).Append(',').Append(s.ulY).Append(' ')
              .Append(string.Join(".", s.under)).Append(' ').Append(string.Join(".", s.exists)).Append('\n');
        var cells = new StringBuilder();
        for (int y = 0; y < 25; y++) for (int x = 0; x < 50; x++) cells.Append((char)('0' + GDVars.arenaPtr.cells[y, x] % 64));
        sb.Append(cells).Append('\n').Append(unit);
        return sb.ToString();
    }

    static string MgPortState() { return MgUnitState(Magic.spellList, Magic.spellIds, Magic.spellScrolls, Magic.scrollPtrs, Magic.scrollCount, Magic.dsCurrentScrollPtr); }

    // Build the scenario twice; run the oracle on one and the port on the other; compare result, call log and state.
    static void MgPair(int seed, Action<MgWorld> setup, Func<string> oracle, Func<string> port, Func<string> label)
    {
        string what = null;
        string r1, r2;
        var w1 = MgBuild(seed); setup(w1);
        try { r1 = oracle(); } catch (Exception e) when (!(e is InvalidOperationException)) { r1 = "EXC " + e.GetType().Name; if (Environment.GetEnvironmentVariable("MGDEBUG") != null) Console.WriteLine(e); }
        string s1 = MgSnap(w1, MgOracle.State());
        var w2 = MgBuild(seed); setup(w2);
        try { r2 = port(); } catch (Exception e) when (!(e is InvalidOperationException)) { r2 = "EXC " + e.GetType().Name; if (Environment.GetEnvironmentVariable("MGDEBUG") != null) Console.WriteLine(e); }
        string s2 = MgSnap(w2, MgPortState());
        what = label();
        mgRuns++; mgLogLines += w1.Log.Count; if (r1.StartsWith("EXC ")) mgExceptions++;
        EqS(r1, r2, what + " seed " + seed + " result");
        Eq(w1.Log.Count, w2.Log.Count, what + " seed " + seed + " call count\nORACLE:\n" + string.Join("\n", w1.Log) + "\nPORT:\n" + string.Join("\n", w2.Log));
        for (int i = 0; i < w1.Log.Count; i++) EqS(w1.Log[i], w2.Log[i], what + " seed " + seed + " call " + i);
        EqS(s1, s2, what + " seed " + seed + " state");
    }

    // Run a Magic group without leaking globals into later groups.
    static void MgGuarded(Action group)
    {
        var rb = Dice.RandomBelow; var bi = GDVars.baseItems; var z0 = GDVars.zone0; var ar = GDVars.arenaPtr; var cl = GDVars.creatureList; var cc = GDVars.curChar;
        var ca = (TCharRecord[])GDVars.creatureArr.Clone(); byte ac = GDVars.arenaCount; byte ifc = GDVars.ifcState; bool ui = GDVars.usingItem; byte rc = GDVars.roundCounter;
        var ct = GDVars.castTarget; bool ba = GDVars.breathAim; sbyte gx = GDVars.gTargetX, gy = GDVars.gTargetY; byte gm = GDVars.gMultiTarget, em = GDVars.elementMask, el = GDVars.effectLock;
        short vt = GDVars.vertTop; byte gdd = GDVars.gDamageDice, sb = GDVars.dsCurrentSpellBeing, stc = GDVars.spellTargetCount;
        var dtp = (TCharRecord[])GDVars.dsTargetCreaturePtr.Clone(); var sh = GDVars.stinkHead; byte dc = GDVars.downedCount; var dn = (TDownedRec[])GDVars.downed.Clone();
        bool lo = MenuCore.listOnly; var ctm = GDVars.castTargetMenu; var sf = (TSpellProc[])GDVars.spellFns.Clone(); var bt = CbtMap.boltTracer; var cm = CbtActs.MagicUnit;
        var fields = typeof(Magic).GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                                  .Where(f => !f.IsLiteral && !f.IsInitOnly).ToArray();
        var values = fields.Select(f => f.GetValue(null)).ToArray();
        var terrains = (TTerrainRec[])GDConsts.terrainInfo.Clone();
        var ids = (byte[])Magic.spellIds.Clone(); var scr = (TItemRec[])Magic.spellScrolls.Clone(); var ptr = (TItemRec[])Magic.scrollPtrs.Clone();
        try { group(); }
        finally
        {
            Dice.RandomBelow = rb; GDVars.baseItems = bi; GDVars.zone0 = z0; GDVars.arenaPtr = ar; GDVars.creatureList = cl; GDVars.curChar = cc;
            Array.Copy(ca, GDVars.creatureArr, ca.Length); GDVars.arenaCount = ac; GDVars.ifcState = ifc; GDVars.usingItem = ui; GDVars.roundCounter = rc;
            GDVars.castTarget = ct; GDVars.breathAim = ba; GDVars.gTargetX = gx; GDVars.gTargetY = gy; GDVars.gMultiTarget = gm; GDVars.elementMask = em; GDVars.effectLock = el;
            GDVars.vertTop = vt; GDVars.gDamageDice = gdd; GDVars.dsCurrentSpellBeing = sb; GDVars.spellTargetCount = stc;
            Array.Copy(dtp, GDVars.dsTargetCreaturePtr, dtp.Length); GDVars.stinkHead = sh; GDVars.downedCount = dc; Array.Copy(dn, GDVars.downed, dn.Length);
            MenuCore.listOnly = lo; GDVars.castTargetMenu = ctm; Array.Copy(sf, GDVars.spellFns, sf.Length); CbtMap.boltTracer = bt; CbtActs.MagicUnit = cm;
            for (int i = 0; i < fields.Length; i++) fields[i].SetValue(null, values[i]);
            Array.Copy(terrains, GDConsts.terrainInfo, terrains.Length);
            Array.Copy(ids, Magic.spellIds, ids.Length); Array.Copy(scr, Magic.spellScrolls, scr.Length); Array.Copy(ptr, Magic.scrollPtrs, ptr.Length);
        }
    }
}
