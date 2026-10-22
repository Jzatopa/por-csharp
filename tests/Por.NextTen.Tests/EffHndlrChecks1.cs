// EffHndlr (src/effhndlr.pas) checks, part 1: the recording world (fake FxEngine and CbtMap, logging UI and helper call
// points), the scenario builder, snapshots and the oracle/port pair runner. Expected behaviour comes from EhOracle
// (EffHndlrOracle.cs), a separate model written from the Pascal text. Each scenario is built twice from one seed, once
// for the oracle and once for the port; the exception type, the ordered call logs and the state snapshots must match.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Por.Core;
using static Por.Core.Types;

sealed class EhWorld : IEffHndlrEffects, ICbtActsCombatMap
{
    public readonly List<string> Log = new List<string>();
    public readonly Random R;
    public readonly List<TCharRecord> All = new List<TCharRecord>();
    readonly Dictionary<object, int> ids = new Dictionary<object, int>();
    int nextId;
    int steps;
    public EhWorld(int seed) { R = new Random(seed); }

    public int Id(object o) { int v; if (!ids.TryGetValue(o, out v)) { v = ++nextId; ids[o] = v; } return v; }
    public string F(object o)
    {
        if (o == null) return "nil";
        var c = o as TCharRecord; if (c != null) return "C" + Id(c);
        var e = o as TEffectRec; if (e != null) return "E" + Id(e) + ":" + e.id + "/" + e.strength + "/" + e.duration + "/" + (e.onRemove ? 1 : 0);
        var i = o as TItemRec; if (i != null) return "I" + Id(i) + ":" + i.itemType + "/" + i.equipped + "/" + i.effect;
        if (o is bool) return (bool)o ? "T" : "F";
        return o.ToString();
    }
    public void L(string name, params object[] a)
    {
        if (++steps > 200000) throw new InvalidOperationException("runaway call log");
        Log.Add(name + "(" + string.Join(",", a.Select(F)) + ")");
    }
    public bool Coin() { return R.Next(2) == 0; }
    public TCharRecord Any() { return All[R.Next(All.Count)]; }
    // a callee that runs other effects may leave dsOtherCreatureCurrentPtr elsewhere
    void Perturb() { if (R.Next(6) == 0) GDVars.dsOtherCreatureCurrentPtr = R.Next(5) == 0 ? null : Any(); }

    public int RandomBelow(int sides) { int v = sides > 0 ? R.Next(sides) : 0; L("rnd", sides, v); return v; }

    public static void Prepend(TCharRecord c, TEffectRec e) { e.next = c.effectsAddr; c.effectsAddr = e; }
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

    // ---- IEffHndlrEffects ----
    public void AddEffect(TCharRecord c, byte id, ushort min, byte str, bool onRem)
    {
        var e = new TEffectRec { id = id, duration = min, strength = str, onRemove = onRem };
        Id(e);
        L("AddFx", c, id, min, str, onRem, GDVars.effectLock);
        Prepend(c, e);
    }
    public void RemoveEffectsHp0(TCharRecord c) { L("RemoveHp0", c, c.status, c.enabled, c.hpCur); }
    public void TriggerEffectsWithCondition(byte cond, TCharRecord c)
    {
        L("Trigger", cond, c, c.status, c.enabled);
        if (R.Next(4) == 0) c.enabled = 1;
        Perturb();
    }
    public void StrengthEffectToStrength(TEffectRec e, ref byte b, ref byte x)
    {
        L("StrOf", e, b, x);
        b = (byte)(15 + e.strength % 6);
        x = (byte)(e.strength * 37 % 101);
    }
    public byte StrengthModifier(byte b, byte x) { byte r = unchecked((byte)(b * 3 + x)); L("StrMod", b, x, r); return r; }
    public void InflictNonWeaponDamage(TCharRecord c, byte dmg, byte red, bool saved)
    {
        L("NonWeapon", c, dmg, red, saved, GDVars.elementMask, GDVars.dsCurrentSpellBeing);
        if (c != null && R.Next(4) == 0) c.enabled = 0;
    }
    public void RemoveEffect(TCharRecord c, byte id, object eff)
    {
        bool r = Unlink(c, (TEffectRec)eff, id);
        L("RemoveFx", c, id, eff, r, GDVars.effectLock);
    }
    public void CallEffectHandler(byte id, TCharRecord c, object p, bool removed) { L("CallFx", id, c, p, removed); }
    public bool RestoreHitPoints(TCharRecord c, byte hp, bool bypass) { bool r = Coin(); L("Restore", c, hp, bypass, r); return r; }
    public bool RollSavingThrow(TCharRecord c, sbyte t, sbyte m)
    {
        bool r = Coin();
        L("Save", c, t, m, r, GDVars.dsCurrentSpellBeing);
        Perturb();
        return r;
    }
    public TCharRecord HoldTarget(TCharRecord c, object p, bool removed, byte id, ref bool released)
    {
        L("Hold", c, p, removed, id, released);
        TCharRecord r = null;
        released = false;
        int k = R.Next(3);
        if (k == 0) { r = Any(); GDVars.dsOtherCreatureCurrentPtr = r; }
        else if (k == 1) { released = true; GDVars.dsOtherCreatureCurrentPtr = Any(); }
        L("Hold=", r, released);
        return r;
    }
    public bool ReviveDeadCreature(TCharRecord c, byte hp) { bool r = Coin(); L("Revive", c, hp, r); return r; }
    public void AddEffectWithMessage(TCharRecord c, byte id, ushort min, byte str, bool onRem, bool neg, bool saved, string msg)
    {
        L("AddFxMsg", c, id, min, str, onRem, neg, saved, msg, GDVars.dsCurrentSpellBeing);
        if (!(saved && neg)) { var e = new TEffectRec { id = id, duration = min, strength = str, onRemove = onRem }; Id(e); Prepend(c, e); }
    }
    public bool DiseaseImmune(TCharRecord c, byte id) { bool r = R.Next(3) == 0; L("DiseaseImmune", c, id, r); return r; }
    public void PutOutOfPlayWithStatus(TCharRecord c, byte status, string msg) { L("OutOfPlay", c, status, msg); }
    public bool AdjustForNewStrength(TCharRecord c, byte b, byte x, ref byte enc)
    {
        L("AdjustStr", c, b, x, enc);
        enc = (byte)R.Next(256);
        bool r = Coin();
        L("AdjustStr=", enc, r);
        return r;
    }
    public void InflictItemShock(TCharRecord c, TItemRec item, byte dmg) { L("ItemShock", c, item, dmg, GDVars.elementMask); }

    // ---- ICbtActsCombatMap (the routines EffHndlr uses; the rest are not called) ----
    public void CreatureDowned(TCharRecord c) { L("Downed", c); }
    public int FixedDistance = -1;
    public byte CalcDistance(TCharRecord c, TCharRecord o) { byte r = FixedDistance >= 0 ? (byte)FixedDistance : (byte)R.Next(6); L("Dist", c, o, r); return r; }
    public byte CombatSlot(TCharRecord c) { byte r = R.Next(4) == 0 ? (byte)0 : (byte)R.Next(1, 73); L("Slot", c, r); return r; }
    public sbyte CombatCellX(TCharRecord c) { sbyte r = (sbyte)R.Next(0, 50); L("CellX", c, r); return r; }
    public sbyte CombatCellY(TCharRecord c) { sbyte r = (sbyte)R.Next(0, 25); L("CellY", c, r); return r; }
    public void DisplayMapViewCentered(TCharRecord c, byte delta, bool cursor) { L("Centered", c, delta, cursor); }
    public void SetCombatPalette() { throw new NotImplementedException(); }
    public bool ViewCoordsValid(sbyte x, sbyte y) { throw new NotImplementedException(); }
    public bool CombatIconVisible(TCharRecord c, byte whole) { throw new NotImplementedException(); }
    public void ScrollView(sbyte x, sbyte y, byte d, byte dir) { throw new NotImplementedException(); }
    public void RedrawArenaCell(sbyte x, sbyte y) { throw new NotImplementedException(); }
    public void BuildArenaArrays() { throw new NotImplementedException(); }
    public void GetArenaContents(sbyte x, sbyte y, ref byte c, ref byte t) { throw new NotImplementedException(); }
    public void UpdateCombatMapTile(sbyte x, sbyte y, byte c) { throw new NotImplementedException(); }
    public void SetCreatureFacing(TCharRecord c, byte d, byte f, byte e) { throw new NotImplementedException(); }
    public byte GetIconSize(TCharRecord c) { throw new NotImplementedException(); }
    public void StartSpellPreview(TAimShape s, byte r, byte a) { throw new NotImplementedException(); }
    public void StopSpellPreview() { throw new NotImplementedException(); }
    public byte CountTargets(TCharRecord c, byte r) { throw new NotImplementedException(); }

    // ---- converted-unit call points ----
    public void PrintName(TCharRecord c, string s, byte row, bool pause) { L("PrintName", c, s, row, pause, GDVars.ifcState); }
    public void PrintNameAnim(TCharRecord c, bool eff, string s) { L("PrintNameAnim", c, eff, s); }
    public void ClearMsg() { L("ClearMsg"); }
    public void DisplayParty(TCharRecord hl) { L("DisplayParty", hl); }
    public void CombatInfo(TCharRecord c) { L("CombatInfo", c, c.acCur); }
    public void BottomLine(string s) { L("BottomLine", s); }
    public void HpRestoreMsg(TCharRecord c) { L("HpRestoreMsg", c); }
    public void IconFacings(byte cell) { L("IconFacings", cell); }
    public void Animate(sbyte a, sbyte b, sbyte c, sbyte d, byte f, byte pace) { L("Animate", a, b, c, d, f, pace); }
    public void CombatInterface() { L("CombatInterface", GDVars.ifcState); }
    public void PauseOnly() { L("Pause"); }
    public void CountTeams() { L("CountTeams"); }
    public void RemoveItem(TCharRecord c, TItemRec it)
    {
        L("RemoveItem", c, it);
        TItemRec prev = null;
        for (var p = c.itemsAddr; p != null; prev = p, p = p.next)
            if (ReferenceEquals(p, it)) { if (prev == null) c.itemsAddr = p.next; else prev.next = p.next; break; }
    }
    public void AppendItemCopy(ref TItemRec list, TItemRec item)
    {
        L("AppendItem", item, item.name, item.nameIds[1], item.nameIds[2], item.nameIds[3], item.plus, item.special, item.charges, item.inUse, item.next);
        var n = new TItemRec { name = item.name, inUse = item.inUse, itemType = item.itemType, plus = item.plus, saveBonus = item.saveBonus,
                               equipped = item.equipped, unidMask = item.unidMask, cursed = item.cursed, weight = item.weight, quantity = item.quantity, value = item.value };
        Array.Copy(item.nameIds, n.nameIds, 4); Array.Copy(item.variantData, n.variantData, 3);
        Id(n);
        if (list == null) list = n; else { var p = list; while (p.next != null) p = p.next; p.next = n; }
    }
    public void RecalcStats(TCharRecord c) { L("RecalcStats", c); }
    public bool EndActions(TCharRecord c) { L("EndActions", c); c.combatAddr.initiative = 0; c.combatAddr.movement = 0; return true; }
    public void RecalcBaseSkills(TCharRecord c) { L("RecalcBase", c); }
    public void HpDebug(string m, byte a, byte b, byte c) { L("HpDebug", m, a, b, c); }
    public bool CanAttack(TCharRecord a, TCharRecord b) { bool r = R.Next(4) != 0; L("CanAttack", a, b, r); return r; }
    public bool WalkLine(sbyte x, sbyte y, ref sbyte tx, ref sbyte ty, ref ushort dist, TArena view)
    {
        L("WalkLine", x, y, tx, ty, dist, view == GDVars.arenaPtr);
        tx = (sbyte)R.Next(0, 50); ty = (sbyte)R.Next(0, 25); dist = (ushort)R.Next(0, 20);
        bool r = R.Next(4) != 0; L("WalkLine=", r); return r;
    }

    // ---- snapshot ----
    public string Snap()
    {
        var sb = new StringBuilder();
        foreach (var c in All)
        {
            sb.Append(F(c)).Append('{').Append(c.name).Append('|')
              .Append(string.Join(",", new int[] { c.strCur, c.strExc, c.conCur, c.chaCur, c.hpMax, c.hpCur, c.hpRolled, c.status, c.enabled, c.hostile, c.npc,
                  c.quickfight, c.charType, c.iconDimensions, c.levelHighest, c.levelUndead, c.drainedLevels, c.drainedHps, c.age, c.acCur, c.acBehind,
                  c.attackLevel, c.movementBase, c.alignment, c.race, c.numberOfItems, c.experience }))
              .Append("|L").Append(string.Join(",", c.classLevels));
            if (c.combatAddr != null)
            {
                var k = c.combatAddr;
                sb.Append("|K").Append(F(k.other)).Append(',').Append(string.Join(",", new int[] { k.movement, k.itemUseAllowed, k.castAllowed, k.turned, k.initiative, k.attackPhase, k.overkill }));
            }
            sb.Append("|E");
            int n = 0;
            for (var e = c.effectsAddr; e != null && n < 200; e = e.next, n++) sb.Append(F(e)).Append(';');
            sb.Append("|I");
            n = 0;
            for (var i = c.itemsAddr; i != null && n < 200; i = i.next, n++)
                sb.Append(F(i)).Append(':').Append(i.name).Append('/').Append(string.Join(".", i.nameIds)).Append('/').Append(i.plus).Append('/').Append(string.Join(".", i.variantData)).Append(';');
            sb.Append("|W").Append(F(c.equipped[C_Equip.WEAPON])).Append("}\n");
        }
        sb.Append("G:").Append(string.Join(",", new long[] { GDVars.dsCurrentMorale, GDVars.gAttackRoll, GDVars.pendingDamage, GDVars.pendingEffectId, GDVars.effectLock,
            GDVars.elementMask, GDVars.gSaveRoll, GDVars.dsCurrentSpellBeing, GDVars.ifcState, GDVars.dsCurrentSuppressTargetingFlag, GDVars.gMultiTarget,
            GDVars.dsCurrentAttackCurrent, GDVars.downedCount, GDVars.hitsMade[1], GDVars.hitsMade[2], GDVars.gDamageDice, GDVars.dsCurrentCreatureCalculatingFlag,
            GDVars.teamCount[0], GDVars.teamCount[1], GDVars.roundCounter, GDVars.gSaveType, GDVars.usingItem ? 1 : 0, GDVars.stingHpFlag, GDVars.dsRedrawInventoryMenuFlag }))
          .Append("|other=").Append(F(GDVars.dsOtherCreatureCurrentPtr)).Append("|cur=").Append(F(GDVars.curChar)).Append("|list=").Append(F(GDVars.creatureList)).Append('\n');
        sb.Append("S:");
        int m = 0;
        for (var s = GDVars.stinkHead; s != null && m < 50; s = s.next, m++)
            sb.Append(Id(s)).Append(':').Append(F(s.caster)).Append('/').Append(s.idx).Append('/').Append(s.ulX).Append('/').Append(s.ulY).Append('/')
              .Append(string.Join(".", s.exists)).Append('/').Append(string.Join(".", s.under)).Append(';');
        sb.Append('\n');
        if (GDVars.arenaPtr != null)
        {
            var flat = new byte[25 * 50];
            Buffer.BlockCopy(GDVars.arenaPtr.cells, 0, flat, 0, flat.Length);
            sb.Append("A:").Append(Convert.ToBase64String(flat)).Append('\n');
        }
        return sb.ToString();
    }
}

// One scenario: the creature and arguments a handler gets.
sealed class EhCase
{
    public TCharRecord Creature;
    public object P;
    public bool Removed;
}

static partial class Tests
{
    static readonly string[] EhNames = { "KOBOLD", "KOBOLD LEADER", "GOBLIN", "GOBLIN LEADER", "GOBLIN GUARD", "ORC", "ORC LEADER", "HOBGOBLIN",
        "HOBGOBLIN CHIEF", "GAGOOL", "MACE", "HOBGOBLIN LDR", "GRISHNAK", "OGRE", "OGRE LEADER", "TROLL", "FIRE GIANT", "HILL GIANT", "BUGBEAR", "GNOLL",
        "ALIAS", "TYR", "KOBOLDS", "ogre", "" };
    static readonly int[] EhEffIds = { C_Effect.CON_REGEN, C_Effect.POISONED, C_Effect.HELPLESS, C_Effect.SEE_INVISIBLE, C_Effect.ROT_NO_HEAL,
        C_Effect.REGEN3, C_Effect.TROLL_REGEN, C_Effect.GAZE_REFLECTABLE, C_Effect.CHARM, C_Effect.STRENGTH, C_Effect.ENLARGE, C_Effect.MIRROR_IMAGE,
        C_Effect.SLOW_POISON_HIT, C_Effect.MUMMY_FEAR, C_Effect.CAN_FIGHT_ON, C_Effect.BLOOD_DRAIN, C_Effect.ANHKHEG_ACID, C_Effect.ACID_SQUIRT,
        C_Effect.SPIRIT_HAMMER, C_Effect.MUMMY_ROT };
    static readonly int[] EhItemTypes = { C_Item.HAMMER, C_Item.HOLY_WATER_VIAL, C_Item.FLASK_OF_OIL, C_Item.BOULDER, C_Item.LARGE_BOULDER, 1, 2, 30, 47, 129 };
    static readonly int[] EhNameIds = { C_Item.NAME_MIRROR, C_Item.NAME_SILVER, C_Item.NAME_SPIRITUAL, C_Item.NAME_HOLY_SYMBOL, C_Item.NAME_WOODEN, C_Item.NAME_HAMMER, 0, 7 };

    static T EhPick<T>(Random r, T[] a) { return a[r.Next(a.Length)]; }
    static int EhByte(Random r) { int k = r.Next(8); return k == 0 ? 0 : k == 1 ? 255 : k == 2 ? r.Next(250, 256) : k == 3 ? r.Next(0, 6) : r.Next(256); }

    static TEffectRec EhEffect(EhWorld w, int id)
    {
        var r = w.R;
        int str = r.Next(5) == 0 ? r.Next(256) : r.Next(5) == 0 ? r.Next(0x70, 0x90) : r.Next(0x80);
        var e = new TEffectRec { id = (byte)id, strength = (byte)str, duration = (ushort)(r.Next(3) == 0 ? r.Next(65536) : r.Next(0, 400)), onRemove = r.Next(2) == 0 };
        w.Id(e);
        return e;
    }

    static TItemRec EhItem(EhWorld w)
    {
        var r = w.R;
        var it = new TItemRec { name = "it" + r.Next(10), itemType = (byte)EhPick(r, EhItemTypes), plus = (sbyte)(r.Next(3) == 0 ? r.Next(-1, 4) : 0),
                                equipped = (byte)r.Next(2), inUse = (byte)r.Next(2) };
        for (int k = 1; k <= 3; k++) it.nameIds[k] = (byte)(r.Next(3) == 0 ? EhPick(r, EhNameIds) : r.Next(256));
        it.variantData[0] = (byte)r.Next(256); it.variantData[1] = (byte)(r.Next(3) == 0 ? r.Next(9) + 16 * r.Next(16) : r.Next(256)); it.variantData[2] = (byte)r.Next(256);
        w.Id(it);
        return it;
    }

    static TCharRecord EhCreature(EhWorld w)
    {
        var r = w.R;
        var c = new TCharRecord();
        w.All.Add(c); w.Id(c);
        c.name = EhPick(r, EhNames);
        c.strCur = (byte)(r.Next(3) == 0 ? r.Next(256) : r.Next(2, 21)); c.strExc = (byte)r.Next(0, 101);
        c.conCur = (byte)(r.Next(4) == 0 ? r.Next(256) : r.Next(3, 26)); c.chaCur = (byte)r.Next(0, 26);
        c.hpMax = (byte)EhByte(r); c.hpCur = (byte)(r.Next(3) == 0 ? EhByte(r) : r.Next(c.hpMax + 1)); c.hpRolled = (byte)EhByte(r);
        int[] st = { 0, 0, 0, 1, 3, 4, 5, 6, 7, 8 };
        c.status = (byte)EhPick(r, st); c.enabled = (byte)r.Next(2);
        c.hostile = (sbyte)(r.Next(25) == 0 ? -1 : r.Next(2));
        int[] npcs = { 0, 0, 0x80, C_Npc.ANIMATED_PC, 0x7F, 0xFF };
        c.npc = (byte)EhPick(r, npcs); c.quickfight = (byte)r.Next(2);
        int[] types = { 0, 1, 2, 4, 9, 10, 12, 3 };
        c.charType = (byte)(r.Next(8) == 0 ? r.Next(256) : EhPick(r, types));
        int[] dims = { 1, 1, 2, 0x81, 0x82, 3, 0 };
        c.iconDimensions = (byte)EhPick(r, dims);
        for (int j = 0; j <= 7; j++) c.classLevels[j] = (sbyte)(r.Next(3) == 0 ? r.Next(-1, 13) : r.Next(4) == 0 ? r.Next(1, 10) : 0);
        c.levelHighest = (sbyte)(r.Next(4) == 0 ? r.Next(-2, 13) : r.Next(0, 10));
        c.levelUndead = (sbyte)r.Next(-1, 5);
        c.experience = r.Next(4) == 0 ? -r.Next(100) : r.Next(300000);
        c.drainedLevels = (byte)EhByte(r); c.drainedHps = (byte)EhByte(r);
        c.age = (ushort)(r.Next(5) == 0 ? 65535 : r.Next(0, 300));
        c.acCur = (byte)(r.Next(3) == 0 ? r.Next(256) : r.Next(46, 66)); c.acBehind = (byte)(r.Next(3) == 0 ? r.Next(256) : r.Next(46, 66));
        c.attackLevel = (byte)r.Next(20); c.movementBase = (byte)r.Next(20);
        c.alignment = (byte)r.Next(0, 10); c.race = (sbyte)r.Next(0, 9);
        c.numberOfItems = (byte)(r.Next(3) == 0 ? r.Next(14, 18) : r.Next(0, 20));
        c.combatAddr = new TCombatRec { movement = (byte)r.Next(20), itemUseAllowed = (byte)r.Next(2), castAllowed = (byte)r.Next(2), turned = (byte)r.Next(2),
                                        initiative = (sbyte)r.Next(-3, 4), attackPhase = (byte)r.Next(0, 4), overkill = (byte)r.Next(0, 13) };
        int ne = r.Next(0, 6);
        for (int k = 0; k < ne; k++)
        {
            var e = EhEffect(w, r.Next(5) == 0 ? r.Next(1, 142) : EhPick(r, EhEffIds));
            e.next = c.effectsAddr; c.effectsAddr = e;
        }
        int ni = r.Next(0, 5);
        TItemRec last = null;
        for (int k = 0; k < ni; k++)
        {
            var it = EhItem(w);
            if (last == null) c.itemsAddr = it; else last.next = it;
            last = it;
        }
        return c;
    }

    // Build a whole world from the seed: creatures, globals, arena, clouds, corpses, base items. Every global the unit
    // reads or writes is set here, so the oracle and port runs start from identical state.
    static EhWorld EhBuild(int seed)
    {
        var w = new EhWorld(seed);
        var r = w.R;
        int n = r.Next(3, 8);
        for (int k = 0; k < n; k++) EhCreature(w);
        for (int k = 0; k < n; k++)
        {
            var c = w.All[k];
            c.nextCharAddr = k + 1 < n ? w.All[k + 1] : null;
            c.combatAddr.other = r.Next(8) == 0 ? null : w.Any();
            int pick = r.Next(4);
            if (pick == 0) c.equipped[C_Equip.WEAPON] = null;
            else if (pick == 1 || c.itemsAddr == null) c.equipped[C_Equip.WEAPON] = EhItem(w);
            else { var it = c.itemsAddr; int s = r.Next(4); while (s-- > 0 && it.next != null) it = it.next; c.equipped[C_Equip.WEAPON] = it; }
            c.equipped[C_Equip.ARROW] = r.Next(3) == 0 ? EhItem(w) : null;
            c.equipped[C_Equip.BOLT] = r.Next(3) == 0 ? EhItem(w) : null;
        }
        GDVars.creatureList = w.All[0];
        GDVars.curChar = w.Any();
        GDVars.dsOtherCreatureCurrentPtr = r.Next(5) == 0 ? null : w.Any();
        GDVars.arenaCount = (byte)n;
        for (int k = 1; k <= ARENA_SLOTS; k++) GDVars.creatureArr[k] = k <= n ? w.All[k - 1] : null;
        var bi = new TBaseItems();
        int[] classes = { 0, 1, 0x80, 0x81, 2 };
        for (int k = 0; k < 129; k++)
        {
            bi.Data[k].wpnClass = (byte)(r.Next(5) == 0 ? r.Next(256) : EhPick(r, classes));
            bi.Data[k].props = (byte)(r.Next(3) == 0 ? 0 : r.Next(256));
        }
        GDVars.baseItems = bi;
        GDVars.dsCurrentMorale = (byte)EhByte(r);
        GDVars.gAttackRoll = (sbyte)(r.Next(4) == 0 ? r.Next(-128, 128) : r.Next(4) == 0 ? 0 : r.Next(-3, 21));
        GDVars.pendingDamage = (byte)EhByte(r);
        int[] pend = { 0, 0, C_Effect.SLEEP, C_Effect.CHARM, C_Effect.PARALYSIS, C_Effect.POISONED, 7 };
        GDVars.pendingEffectId = (byte)EhPick(r, pend);
        GDVars.effectLock = (byte)(r.Next(4) == 0 ? 1 : 0);
        int[] masks = { 0, C_Effect.ELEM_FIRE, C_Effect.ELEM_COLD, C_Effect.ELEM_ELEC, C_Effect.ELEM_MAGIC, C_Effect.ELEM_ACID, C_Effect.ELEM_FIRE | C_Effect.ELEM_MAGIC };
        GDVars.elementMask = (byte)(r.Next(5) == 0 ? r.Next(256) : EhPick(r, masks));
        GDVars.gSaveRoll = (short)(r.Next(5) == 0 ? r.Next(-32768, 32768) : r.Next(-5, 25));
        int[] spells = { 0, C_Spell.MAGIC_MISSILE, C_Spell.CHARM_PERSON, 1, 3, 47, 60, 65 };
        GDVars.dsCurrentSpellBeing = (byte)(r.Next(4) == 0 ? r.Next(1, 68) : EhPick(r, spells));
        int[] ifcs = { C_Ifc.COMBAT, C_Ifc.COMBAT, C_Ifc.CAMPING, 1, 3 };
        GDVars.ifcState = (byte)EhPick(r, ifcs);
        GDVars.dsCurrentSuppressTargetingFlag = (byte)(r.Next(3) == 0 ? 1 : 0);
        GDVars.gMultiTarget = (byte)(r.Next(3) == 0 ? 1 : 0);
        GDVars.dsCurrentAttackCurrent = (byte)EhByte(r);
        GDVars.hitsMade[1] = (byte)r.Next(0, 3); GDVars.hitsMade[2] = (byte)r.Next(0, 3);
        GDVars.gDamageDice = (byte)(r.Next(5) == 0 ? r.Next(256) : r.Next(0, 9));
        GDVars.dsCurrentCreatureCalculatingFlag = (byte)r.Next(2);
        GDVars.teamCount[0] = (byte)r.Next(0, 10); GDVars.teamCount[1] = (byte)r.Next(0, 10);
        GDVars.roundCounter = (byte)(r.Next(2) == 0 ? 0 : r.Next(256));
        GDVars.gSaveType = (byte)r.Next(0, 6);
        GDVars.usingItem = r.Next(3) == 0;
        GDVars.stingHpFlag = (byte)r.Next(2);
        GDVars.dsRedrawInventoryMenuFlag = (byte)r.Next(2);
        var ar = new TArena();
        for (int y = 0; y < 25; y++) for (int x = 0; x < 50; x++) ar.cells[y, x] = (byte)r.Next(1, 40);
        GDVars.arenaPtr = ar;
        // clouds
        GDVars.stinkHead = null;
        int nc = r.Next(0, 5);
        TStinkRec lastS = null;
        for (int k = 0; k < nc; k++)
        {
            var s = new TStinkRec { caster = w.Any(), idx = (byte)r.Next(0, 6), ulX = (sbyte)(r.Next(20) == 0 ? r.Next(-2, 52) : r.Next(1, 48)),
                                    ulY = (sbyte)(r.Next(20) == 0 ? r.Next(-2, 27) : r.Next(1, 23)) };
            for (int i = 1; i <= 4; i++) { s.exists[i] = (byte)(r.Next(4) == 0 ? 0 : r.Next(1, 3)); s.under[i] = (byte)r.Next(1, 40); }
            w.Id(s);
            if (lastS == null) GDVars.stinkHead = s; else lastS.next = s;
            lastS = s;
        }
        GDVars.downedCount = (byte)(r.Next(30) == 0 ? 9 : r.Next(0, 9));
        for (int k = 1; k <= 8; k++)
        {
            var d = new TDownedRec { cr = r.Next(4) == 0 ? null : w.Any(), terrain = (byte)r.Next(40) };
            if (lastS != null && r.Next(2) == 0)
            {
                var s = GDVars.stinkHead; int skip = r.Next(nc); while (skip-- > 0) s = s.next;
                int i = r.Next(1, 5);
                d.x = (sbyte)(s.ulX + GDConsts.dirDX[GDConsts.cloudDir[i]]); d.y = (sbyte)(s.ulY + GDConsts.dirDY[GDConsts.cloudDir[i]]);
            }
            else { d.x = (sbyte)r.Next(-1, 50); d.y = (sbyte)r.Next(-1, 25); }
            GDVars.downed[k] = d;
        }
        return w;
    }

    // An effect-node argument: one of the creature's own nodes, a detached node, or (rarely) nil.
    static object EhNode(EhWorld w, TCharRecord c, int id)
    {
        var r = w.R;
        int k = r.Next(40);
        if (k == 0) return null;
        if (k < 20 && c.effectsAddr != null)
        {
            var e = c.effectsAddr; int s = r.Next(4); while (s-- > 0 && e.next != null) e = e.next;
            return e;
        }
        var n = EhEffect(w, id);
        if (r.Next(2) == 0) EhWorld.Prepend(c, n);
        return n;
    }

    static int EhHash(string t) { int h = 17; foreach (char ch in t) h = unchecked(h * 31 + ch); return h; }

    static EhWorld ehCurrent;
    static int ehLastLog;

    // Point the port's seams and call points at the world.
    static void EhInstall(EhWorld w)
    {
        ehCurrent = w;
        Dice.RandomBelow = w.RandomBelow;
        EffHndlr.Effects = w;
        EffHndlr.CombatMap = w;
        EffHndlr.PrintNameStringCall = w.PrintName;
        EffHndlr.PrintNameStringAnimCall = w.PrintNameAnim;
        EffHndlr.ClearMessageWindowCall = w.ClearMsg;
        EffHndlr.DisplayPartyCall = w.DisplayParty;
        EffHndlr.CombatPrintCreatureInfoCall = w.CombatInfo;
        EffHndlr.PrintBottomLineCall = w.BottomLine;
        EffHndlr.PrintHpRestoreMsgCall = w.HpRestoreMsg;
        EffHndlr.BuildIconFacingsCall = w.IconFacings;
        EffHndlr.CombatAnimateMovingIconCall = w.Animate;
        EffHndlr.DisplayCombatInterfaceCall = w.CombatInterface;
        EffHndlr.PauseOnlyCall = w.PauseOnly;
        EffHndlr.CountTeamsCall = w.CountTeams;
        EffHndlr.RemoveItemCall = w.RemoveItem;
        EffHndlr.AppendItemCopyCall = w.AppendItemCopy;
        EffHndlr.RecalcStatsCall = w.RecalcStats;
        EffHndlr.EndActionsCall = w.EndActions;
        EffHndlr.RecalcBaseSkillsCall = w.RecalcBaseSkills;
        EffHndlr.PrintHpDebugCall = w.HpDebug;
        EffHndlr.CanAttackTargetCall = w.CanAttack;
        EffHndlr.WalkLineCall = w.WalkLine;
    }

    // Build the scenario twice, run the oracle on one copy and the port on the other, and compare. Returns true when
    // both sides threw (the same exception type).
    static bool EhPair(int seed, string handler, Func<EhWorld, EhCase> make, Action<EhWorld> tweak = null)
    {
        var a = EhBuild(seed);
        if (tweak != null) tweak(a);
        var ca = make(a);
        string beforeA = a.Snap();
        string exA = null;
        Dice.RandomBelow = a.RandomBelow;
        try { new EhOracle(a).Run(handler, ca.Creature, ca.P, ca.Removed); }
        catch (Exception e) { exA = e.GetType().Name; }
        string snapA = a.Snap();

        var b = EhBuild(seed);
        if (tweak != null) tweak(b);
        var cb = make(b);
        EqS(beforeA, b.Snap(), handler + " seed " + seed + " identical start");
        EhInstall(b);
        string exB = null;
        try { EhPortCall(handler)(cb.Creature, cb.P, cb.Removed); }
        catch (Exception e) { exB = e.GetType().Name; if (e is NotSupportedException || e is NotImplementedException) throw; }
        string snapB = b.Snap();

        string what = handler + " seed " + seed;
        EqS(exA ?? "-", exB ?? "-", what + " exception");
        Eq(a.Log.Count, b.Log.Count, what + " log length\nA:" + string.Join("\n", a.Log) + "\nB:" + string.Join("\n", b.Log));
        for (int i = 0; i < a.Log.Count; i++) EqS(a.Log[i], b.Log[i], what + " call " + i);
        EqS(snapA, snapB, what + " state");
        ehLastLog = a.Log.Count;
        return exA != null;
    }

    static TEffectHandler EhPortCall(string handler)
    {
        var h = EffHndlrBindings.EffectHandler("EffHndlr." + handler);
        if (h == null) throw new Exception("no port handler " + handler);
        return h;
    }

    // Run n scenarios for a handler; at most a quarter may end in the shared exception.
    static void EhMany(string handler, int n, Func<EhWorld, EhCase> make, Action<EhWorld> tweak = null)
    {
        int threw = 0;
        long logs = 0;
        for (int s = 0; s < n; s++)
        {
            if (EhPair((EhHash(handler) & 0xFFFF) ^ (s * 7919 + 13), handler, make, tweak)) threw++;
            logs += ehLastLog;
        }
        if (Environment.GetEnvironmentVariable("EH_STATS") != null) Console.WriteLine("  " + handler + ": threw " + threw + "/" + n + ", mean log " + (logs / (double)n).ToString("0.0"));
        Yes(threw * 4 <= n, handler + ": " + threw + " of " + n + " scenarios threw");
    }

    // A case on a random creature with an effect-node p (of the handler's own id when given).
    static Func<EhWorld, EhCase> EhOn(int effId)
    {
        return w => { var c = w.Any(); return new EhCase { Creature = c, P = EhNode(w, c, effId), Removed = w.Coin() }; };
    }

    static void EhGuarded(Action group)
    {
        var saved = new List<Tuple<System.Reflection.FieldInfo, object, object>>();
        foreach (var t in new[] { typeof(GDVars), typeof(EffHndlr), typeof(Dice) })
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
