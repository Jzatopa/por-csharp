// EffHndlr (src/effhndlr.pas) checks, part 2: the groups. Every handler runs through EhPair (oracle against port over the
// recording world); the tweaks push scenarios into branches the plain random world reaches rarely.
using System;
using System.Collections.Generic;
using System.Linq;
using Por.Core;
using static Por.Core.Types;

static partial class Tests
{
    static readonly string[] EhRollHandlers = { "EffBless", "EffCurse", "EffProtEvil", "EffProtGood", "EffResistCold", "EffResistFire", "EffShield",
        "EffInvisible", "EffEnfeeble", "EffBlind", "EffAlliedPrayer", "EffBestowCurse", "EffBlinking", "EffSlowed", "EffImmobile", "EffDustDisappear",
        "EffConPsnSave", "EffConSpellSave", "EffHalfFire", "EffHalfElec", "EffHalfCold", "EffNoCold", "EffNoFire", "EffEfreetiFire", "EffRingFireRes",
        "EffRegen3", "EffImmuneMM", "EffElfResist", "EffHalfElfResist", "EffNoSleepCharm", "EffNoParalysis", "EffNoParaPoison", "EffNoCharmPoison",
        "EffNull", "EffHelpless", "EffHasted", "EffPrayer", "EffDisplaced", "EffFriends" };
    static readonly string[] EhFoeHandlers = { "EffUndeadSlayer", "EffFlameTongue", "EffGnomeThac0", "EffDwarfThac0", "EffDwarfAcGiant", "EffGnomeAcBug" };
    static readonly string[] EhWeaponHandlers = { "EffProtMissiles", "EffKreenDodge", "EffBoulderDodge", "EffHalfBash", "EffHalfPierce", "EffHalfMagical",
        "EffNeedSilver", "EffNeedMagic", "EffSilverMagic", "EffHolyWater", "EffMummyFire" };
    static readonly string[] EhTimedHandlers = { "EffSlowPoisonHit", "EffSlowPoisonEnd", "EffDiseaseStr", "EffDiseaseHp", "EffDiseased", "EffRotNoHeal",
        "EffConRegen", "EffMummyRot", "EffHealWake", "EffTrollReturn", "EffTrollBurn", "EffTrollHeal", "EffTrollRegen", "EffRingInvis", "EffCamouflage",
        "EffCanFightOn", "EffFightOn", "EffAnimateDead", "EffGaseous", "EffTrainStart", "EffTrainManual" };
    static readonly string[] EhTouchHandlers = { "EffPoisonHit0", "EffPoisonHit4", "EffPoisonHit2", "EffPoisonHitM2", "EffKreenPara", "EffGhoulPara",
        "EffDriderPara", "EffFireTouch", "EffAcidSquirt", "EffBloodDrain", "EffBloodAttach", "EffBiteHold", "EffRotTouch", "EffAnhkhegAcid" };
    static readonly string[] EhGazeHandlers = { "EffDragonFear", "EffMummyFear", "EffPetrifyGaze", "EffCharmGaze", "EffGazeBlock", "EffCharm", "EffMirrorImage" };
    static readonly string[] EhDrainHandlers = { "EffDrainOne", "EffDrainTwo" };
    static readonly string[] EhItemHandlers = { "EffExtraStr", "EffGauntlets", "EffSpiritHammer", "EffHammerGone", "EffReadyItem", "EffAlignedWeapon",
        "EffBoulder", "EffSilence", "EffStinkCough" };
    static readonly string[] EhOtherHandlers = { "EffCastStink", "EffMagicRes50", "EffMagicRes85" };

    // statuses the handlers branch on, more often than the plain world gives them
    static void EhStatusMix(EhWorld w)
    {
        int[] st = { C_Status.OKAY, C_Status.ANIMATED, C_Status.UNCONSCIOUS, C_Status.DYING, C_Status.DEAD, C_Status.STONED, C_Status.GONE };
        foreach (var c in w.All) if (w.R.Next(2) == 0) c.status = (byte)st[w.R.Next(st.Length)];
    }

    static void EhRolls()
    {
        foreach (var h in EhRollHandlers)
        {
            int id = h == "EffHasted" ? 39 : h == "EffPrayer" ? 49 : h == "EffDisplaced" ? 89 : h == "EffFriends" ? 14 : 0;
            EhMany(h, 400, EhOn(id), w =>
            {
                // pendingDamage and the dice count near each other, so the per-die floors are reached
                if (w.R.Next(2) == 0) { GDVars.gDamageDice = (byte)w.R.Next(0, 6); GDVars.pendingDamage = (byte)w.R.Next(0, 16); }
                if (w.R.Next(3) == 0) GDVars.gAttackRoll = 0;
            });
        }
    }

    static void EhFoes()
    {
        foreach (var h in EhFoeHandlers)
            EhMany(h, 600, EhOn(0), w =>
            {
                foreach (var c in w.All)
                    if (w.R.Next(3) != 0) { c.charType = (byte)(w.R.Next(4) == 0 ? 10 : w.R.Next(3)); c.iconDimensions = (byte)(w.R.Next(3) == 0 ? 0x82 : 1); }
            });
    }

    static void EhWeapons()
    {
        foreach (var h in EhWeaponHandlers)
            EhMany(h, 600, EhOn(0), w =>
            {
                var cur = GDVars.curChar;
                if (w.R.Next(3) == 0 && cur.equipped[C_Equip.WEAPON] != null) cur.equipped[C_Equip.WEAPON].plus = 0;
                if (w.R.Next(4) == 0 && cur.equipped[C_Equip.WEAPON] != null) cur.equipped[C_Equip.WEAPON].nameIds[3] = C_Item.NAME_SILVER;
                if (w.R.Next(4) == 0) GDVars.curChar.race = 0;
            });
        EhAvoidSweep();
    }

    // AvoidRanged's percentage at and around each caller's threshold (worked from the Pascal: avoided when 1d100 <= pct,
    // the attacker armed and more than one cell away).
    static void EhAvoidSweep()
    {
        foreach (var t in new[] { Tuple.Create("EffBoulderDodge", 50), Tuple.Create("EffKreenDodge", 60), Tuple.Create("EffProtMissiles", 100) })
            for (int roll = t.Item2 - 1; roll <= Math.Min(100, t.Item2 + 1); roll++)
                foreach (int dist in new[] { 1, 2 })
                {
                    var w = EhBuild(40 + roll);
                    EhInstall(w);
                    w.FixedDistance = dist;
                    var att = w.All[0];
                    var target = w.All[1];
                    GDVars.curChar = att;
                    att.equipped[C_Equip.WEAPON] = new TItemRec { itemType = C_Item.BOULDER };
                    att.equipped[C_Equip.ARROW] = null; att.equipped[C_Equip.BOLT] = null;
                    GDVars.baseItems.Data[C_Item.BOULDER].props = 0;
                    att.combatAddr.attackPhase = 1; GDVars.hitsMade[1] = 2;
                    GDVars.pendingDamage = 9; GDVars.gAttackRoll = 5;
                    int r = roll;
                    Dice.RandomBelow = s => r - 1;
                    EhPortCall(t.Item1)(target, null, false);
                    bool avoided = dist > 1 && roll <= t.Item2;
                    string what = t.Item1 + " roll " + roll + " dist " + dist;
                    Eq(avoided ? 0 : 9, GDVars.pendingDamage, what + " damage");
                    Eq(avoided ? -1 : 5, GDVars.gAttackRoll, what + " roll");
                    Eq(avoided ? 1 : 2, GDVars.hitsMade[1], what + " hits");
                }
    }

    static void EhTimed()
    {
        foreach (var h in EhTimedHandlers)
        {
            int id = h == "EffMummyRot" ? 57 : h == "EffCanFightOn" ? 99 : 0;
            EhMany(h, 500, EhOn(id), w =>
            {
                EhStatusMix(w);
                if (w.R.Next(3) == 0) foreach (var c in w.All) { c.conCur = (byte)w.R.Next(17, 27); c.strCur = (byte)w.R.Next(2, 6); }
            });
        }
    }

    static void EhTouch()
    {
        foreach (var h in EhTouchHandlers)
            EhMany(h, 600, EhOn(h == "EffBloodDrain" ? 75 : h == "EffAnhkhegAcid" ? 121 : 0), w =>
            {
                EhStatusMix(w);
                if (w.R.Next(2) == 0) foreach (var c in w.All) c.enabled = 1;
                if (w.R.Next(3) == 0) foreach (var c in w.All) c.race = (sbyte)C_Race.ELF;
            });
    }

    static void EhGaze()
    {
        foreach (var h in EhGazeHandlers)
        {
            int id = h == "EffCharm" ? 11 : h == "EffMirrorImage" ? 28 : 0;
            Func<EhWorld, EhCase> make = w =>
            {
                var c = w.Any();
                if (w.Coin()) { var e = EhEffect(w, C_Effect.GAZE_REFLECTABLE); EhWorld.Prepend(c, e); }
                return new EhCase { Creature = c, P = EhNode(w, c, id), Removed = w.Coin() };
            };
            EhMany(h, 800, make, w =>
            {
                foreach (var c in w.All)
                {
                    if (w.R.Next(2) == 0) c.enabled = 1;
                    for (var it = c.itemsAddr; it != null; it = it.next)
                        if (w.R.Next(3) == 0) { it.equipped = 1; it.nameIds[w.R.Next(1, 4)] = (byte)(w.R.Next(2) == 0 ? C_Item.NAME_MIRROR : C_Item.NAME_HOLY_SYMBOL); if (w.R.Next(2) == 0) it.nameIds[3] = C_Item.NAME_WOODEN; }
                }
                if (w.R.Next(2) == 0) GDVars.dsCurrentSuppressTargetingFlag = 0;
                if (w.R.Next(2) == 0) { GDVars.gMultiTarget = 0; if (GDVars.dsCurrentSpellBeing == 0) GDVars.dsCurrentSpellBeing = 3; }
            });
        }
    }

    static void EhDrain()
    {
        foreach (var h in EhDrainHandlers)
            EhMany(h, 2500, EhOn(0), w =>
            {
                EhStatusMix(w);
                foreach (var c in w.All)
                {
                    if (w.R.Next(2) == 0) for (int j = 0; j <= 7; j++) c.classLevels[j] = (sbyte)(w.R.Next(3) == 0 ? w.R.Next(0, 13) : 0);
                    if (w.R.Next(3) == 0) c.levelHighest = (sbyte)w.R.Next(0, 4);
                    if (w.R.Next(3) == 0) c.hpCur = (byte)w.R.Next(0, 4);
                }
            });
    }

    static void EhItems()
    {
        EhMany("EffExtraStr", 1500, EhOn(38), w =>
        {
            foreach (var c in w.All)
                for (int k = w.R.Next(0, 4); k > 0; k--) EhWorld.Prepend(c, EhEffect(w, w.R.Next(2) == 0 ? C_Effect.STRENGTH : C_Effect.ENLARGE));
        });
        EhMany("EffExtraStr", 500, EhOn(12));
        EhMany("EffGauntlets", 1500, EhOn(0), w =>
        {
            foreach (var c in w.All)
            {
                if (w.R.Next(2) == 0) { c.strCur = 18; c.strExc = 100; }
                for (int k = w.R.Next(0, 4); k > 0; k--) EhWorld.Prepend(c, EhEffect(w, C_Effect.STRENGTH));
            }
        });
        EhMany("EffSpiritHammer", 1000, EhOn(23), w =>
        {
            foreach (var c in w.All)
                for (var it = c.itemsAddr; it != null; it = it.next)
                    if (w.R.Next(3) == 0) { it.itemType = C_Item.HAMMER; if (w.R.Next(2) == 0) it.nameIds[3] = C_Item.NAME_SPIRITUAL; }
        });
        Func<EhWorld, EhCase> withItem = w =>
        {
            var c = w.Any();
            TItemRec it;
            if (c.itemsAddr != null && w.Coin()) { it = c.itemsAddr; int s = w.R.Next(3); while (s-- > 0 && it.next != null) it = it.next; }
            else it = EhItem(w);
            if (w.R.Next(3) == 0) it.effect = (byte)((it.effect & 0xF0) | c.alignment);
            return new EhCase { Creature = c, P = w.R.Next(40) == 0 ? null : it, Removed = w.Coin() };
        };
        EhMany("EffReadyItem", 600, withItem);
        EhMany("EffAlignedWeapon", 600, withItem);
        EhMany("EffBoulder", 600, withItem, w => { foreach (var c in w.All) c.strCur = (byte)w.R.Next(17, 21); });
        EhMany("EffHammerGone", 300, EhOn(0));
        EhMany("EffSilence", 300, EhOn(0));
        EhMany("EffStinkCough", 500, EhOn(0), w => { if (w.R.Next(2) == 0) GDVars.curChar = w.All[0]; });
    }

    static void EhCloud()
    {
        Func<EhWorld, EhCase> make = w =>
        {
            int nc = 0;
            for (var s = GDVars.stinkHead; s != null; s = s.next) nc++;
            if (nc == 0 || w.R.Next(5) == 0) { var c0 = w.Any(); return new EhCase { Creature = c0, P = EhNode(w, c0, 40), Removed = w.Coin() }; }
            var node = GDVars.stinkHead; int skip = w.R.Next(nc); while (skip-- > 0) node = node.next;
            var e = EhEffect(w, 40);
            e.strength = (byte)(node.idx * 16 + w.R.Next(16));
            return new EhCase { Creature = node.caster, P = e, Removed = w.Coin() };
        };
        EhMany("EffCastStink", 3000, make);
    }

    static void EhResist()
    {
        foreach (var h in new[] { "EffMagicRes50", "EffMagicRes85" })
            EhMany(h, 1000, EhOn(0), w =>
            {
                if (w.R.Next(4) != 0 && GDVars.dsCurrentSpellBeing == 0) GDVars.dsCurrentSpellBeing = (byte)w.R.Next(1, 68);
                foreach (var c in w.All) for (int j = 0; j <= 7; j++) if (w.R.Next(2) == 0) c.classLevels[j] = (sbyte)w.R.Next(-1, 40);
            });
        EhResistCases();
    }

    // MagicResistance worked from the Pascal: chance = pct + (11 - lvl) * 5 kept within 0..100, an item caster counted at
    // most level 6; nullified when 1d100 <= chance.
    static void EhResistCases()
    {
        int special = 0;
        for (int i = 1; i <= 67; i++) if (GDConsts.spellTable[i].castClass == C_Spell.CLASS_SPECIAL) { special = i; break; }
        Yes(special > 0, "a special-class spell exists");
        // (spell, item, cleric level, pct, roll, resisted)
        var cases = new[]
        {
            Tuple.Create(1, false, 30, 50, 1, false),     // 50 + (11 - 30) * 5 = -45 -> 0: even a 1 fails
            Tuple.Create(1, false, 21, 50, 1, false),     // 50 + (11 - 21) * 5 = 0: a 1 is not <= 0
            Tuple.Create(1, false, 20, 50, 5, true),      // 50 - 45 = 5
            Tuple.Create(1, false, 20, 50, 6, false),
            Tuple.Create(1, false, 0, 50, 100, true),     // 50 + 55 = 105 -> 100
            Tuple.Create(1, false, 1, 50, 100, true),     // 50 + 50 = 100
            Tuple.Create(1, false, 2, 50, 96, false),     // 50 + 45 = 95
            Tuple.Create(1, false, 2, 50, 95, true),
            Tuple.Create(1, false, 11, 85, 85, true),
            Tuple.Create(1, false, 11, 85, 86, false),
            Tuple.Create(special, true, 0, 50, 75, true),  // special 12 capped to 6 for an item: 50 + 25 = 75
            Tuple.Create(special, true, 0, 50, 76, false),
            Tuple.Create(special, false, 0, 50, 46, false), // special 12 without an item: 50 - 5 = 45
            Tuple.Create(special, false, 0, 50, 45, true),
            Tuple.Create(1, true, 20, 50, 75, true),       // a non-special item spell is level 6: 75
            Tuple.Create(1, true, 20, 50, 76, false),
        };
        foreach (var k in cases)
        {
            var w = EhBuild(60);
            EhInstall(w);
            var c = w.All[0];
            GDVars.curChar = c; GDVars.dsCurrentSpellBeing = (byte)k.Item1; GDVars.usingItem = k.Item2;
            c.classLevels[C_Level.CLERIC] = (sbyte)k.Item3; c.classLevels[C_Level.MAGE] = (sbyte)k.Item3;
            GDVars.pendingEffectId = 0; GDVars.elementMask = C_Effect.ELEM_MAGIC; GDVars.pendingDamage = 7;
            int roll = k.Item5;
            Dice.RandomBelow = s => roll - 1;
            if (k.Item4 == 50) EffHndlr.EffMagicRes50(c, null, false); else EffHndlr.EffMagicRes85(c, null, false);
            bool expectZero = k.Item6;
            Eq(expectZero ? 0 : 7, GDVars.pendingDamage, "resist spell " + k.Item1 + " item " + k.Item2 + " lvl " + k.Item3 + " pct " + k.Item4 + " roll " + roll);
        }
    }

    // Hand-worked cases from the Pascal text, independent of the oracle.
    static void EhExamples()
    {
        var w = EhBuild(77);
        EhInstall(w);
        var c = w.All[0];
        // NullifyAttack: id 0 always; otherwise only the matching pending id
        GDVars.pendingDamage = 9; GDVars.pendingEffectId = C_Effect.SLEEP;
        EffHndlr.NullifyAttack(C_Effect.CHARM);
        Eq(9, GDVars.pendingDamage, "nullify other id keeps damage");
        EffHndlr.NullifyAttack(C_Effect.SLEEP);
        Eq(0, GDVars.pendingDamage, "nullify matching id"); Eq(0, GDVars.pendingEffectId, "nullify clears id");
        // EffRingFireRes: 3 dice, 10 damage -> 4; 3 dice, 4 damage -> 3 (floor at the dice count)
        GDVars.elementMask = C_Effect.ELEM_FIRE | C_Effect.ELEM_MAGIC; GDVars.gDamageDice = 3; GDVars.pendingDamage = 10; GDVars.gSaveRoll = 0;
        EffHndlr.EffRingFireRes(c, null, false);
        Eq(4, GDVars.pendingDamage, "ring fire 10/3d"); Eq(4, GDVars.gSaveRoll, "ring fire save");
        GDVars.pendingDamage = 4; EffHndlr.EffRingFireRes(c, null, false);
        Eq(3, GDVars.pendingDamage, "ring fire floor");
        // EffEfreetiFire: 3 dice, 10 damage -> 7; 2 damage stays 2
        GDVars.elementMask = C_Effect.ELEM_FIRE; GDVars.pendingDamage = 10; EffHndlr.EffEfreetiFire(c, null, false);
        Eq(7, GDVars.pendingDamage, "efreeti 10/3d");
        GDVars.pendingDamage = 2; EffHndlr.EffEfreetiFire(c, null, false);
        Eq(2, GDVars.pendingDamage, "efreeti below floor");
        // EffRegen3 at 254/255 caps; 10/20 -> 13
        c.hpMax = 255; c.hpCur = 254; EffHndlr.EffRegen3(c, null, false); Eq(255, c.hpCur, "regen cap");
        c.hpMax = 20; c.hpCur = 10; EffHndlr.EffRegen3(c, null, false); Eq(13, c.hpCur, "regen +3");
        // EffEnfeeble: 13 - 3 = 10
        GDVars.pendingDamage = 13; EffHndlr.EffEnfeeble(c, null, false); Eq(10, GDVars.pendingDamage, "enfeeble");
        // EffBless wraps the Byte morale and the ShortInt attack roll
        GDVars.dsCurrentMorale = 253; GDVars.gAttackRoll = 127; EffHndlr.EffBless(c, null, false);
        Eq(2, GDVars.dsCurrentMorale, "bless morale wrap"); Eq(-128, GDVars.gAttackRoll, "bless roll wrap");
        // EffCurse floors morale at 0
        GDVars.dsCurrentMorale = 3; EffHndlr.EffCurse(c, null, false); Eq(0, GDVars.dsCurrentMorale, "curse floor");
        // EffShield: AC raised to 57, magic missile nullified
        c.acCur = 50; GDVars.dsCurrentSpellBeing = C_Spell.MAGIC_MISSILE; GDVars.pendingDamage = 8; EffHndlr.EffShield(c, null, false);
        Eq(57, c.acCur, "shield ac"); Eq(0, GDVars.pendingDamage, "shield mm");
        // EffCharm applied: strength 12 + $20 + hostile*64, hostile from bit 7
        var e = new TEffectRec { id = C_Effect.CHARM, strength = 0x80 + 12 };
        c.hostile = 1; c.npc = 0; EffHndlr.EffCharm(c, e, false);
        Eq(0x80 + 12 + 0x20 + 0x40, e.strength, "charm strength"); Eq(1, c.hostile, "charm side"); Eq(C_Npc.ANIMATED_PC, c.npc, "charm npc");
        Eq(100, GDVars.dsCurrentMorale, "charm morale");
        EffHndlr.EffCharm(c, e, true); Eq(1, c.hostile, "charm restore hostility"); Eq(0, c.npc, "charm restore npc");
        // MagicResistance chance: pct 50 vs a level-1 cleric caster -> 100 (clamped); vs level 20 -> 5
        GDVars.dsCurrentSpellBeing = 1; GDVars.usingItem = false; GDVars.curChar = c; GDVars.pendingEffectId = 0; GDVars.elementMask = C_Effect.ELEM_MAGIC;
        c.classLevels[C_Level.CLERIC] = 20;
        int calls = 0; Dice.RandomBelow = s => { calls++; return 5; };   // a roll of 6 > 5: not resisted
        GDVars.pendingDamage = 7; EffHndlr.EffMagicRes50(c, null, false); Eq(7, GDVars.pendingDamage, "resist lvl 20 roll 6 vs 5");
        Dice.RandomBelow = s => 4;   // a roll of 5 <= 5: resisted
        EffHndlr.EffMagicRes50(c, null, false); Eq(0, GDVars.pendingDamage, "resist lvl 20 roll 5");
        Eq(1, calls, "one roll");
        GDVars.pendingDamage = 7; GDVars.pendingEffectId = 0; GDVars.elementMask = 0;
        EffHndlr.EffMagicRes85(c, null, false); Eq(7, GDVars.pendingDamage, "resist: no effect, no magic -> untouched");
    }

    static void EhSeams()
    {
        var w = EhBuild(5);
        EhInstall(w);
        EffHndlr.Effects = null;
        bool threw = false;
        try { EffHndlr.EffTrainStart(w.All[0], null, false); } catch (NotSupportedException) { threw = true; }
        Yes(threw, "unbound FxEngine seam throws");
        EhInstall(w);
        EffHndlr.CombatMap = null;
        threw = false;
        try { EffHndlr.EffGaseous(w.All[0], new TEffectRec(), false); } catch (NotSupportedException) { threw = true; }
        Yes(threw, "unbound CbtMap seam throws");
    }

    sealed class EhInitHost : IGameInitServices
    {
        public TSpellProc SpellHandler(string name) { return MagicBindings.SpellHandler(name); }
        public TEffectHandler EffectHandler(string q) { return EffHndlrBindings.EffectHandler(q); }
        public TCastUI CastTargetMenu { get { return (byte s, bool b, ref bool sel) => { }; } }
        public void InstallLightningTracer() { }
        public void SeedRandom(bool d) { }
    }

    static void EhBindings()
    {
        // every effhndlr.pas handler is named, resolves, and is covered by a group
        var groups = new HashSet<string>(EhRollHandlers.Concat(EhFoeHandlers).Concat(EhWeaponHandlers).Concat(EhTimedHandlers).Concat(EhTouchHandlers)
                                         .Concat(EhGazeHandlers).Concat(EhDrainHandlers).Concat(EhItemHandlers).Concat(EhOtherHandlers));
        Eq(112, groups.Count, "handlers covered by groups");
        foreach (var h in groups)
        {
            var d = EffHndlrBindings.EffectHandler("EffHndlr." + h);
            Yes(d != null, "binding " + h);
            EqS(h, d.Method.Name, "binding name " + h);
            EqS("EffHndlr", d.Method.DeclaringType.Name, "binding unit " + h);
        }
        EqS("DragonBreathHandler", EffHndlrBindings.EffectHandler("Magic.DragonBreathHandler").Method.Name, "magic handler");
        EqS("RearClawRake", EffHndlrBindings.EffectHandler("CbtActs.RearClawRake").Method.Name, "cbtacts rake");
        EqS("BiteAndHold", EffHndlrBindings.EffectHandler("CbtActs.BiteAndHold").Method.Name, "cbtacts bite");
        Yes(EffHndlrBindings.EffectHandler("EffHndlr.Nope") == null, "unknown name");
        Yes(EffHndlrBindings.EffectHandler("EffHndlr.NullifyAttack") == null, "private routine not offered");
        // GameInit fills all 141 effect slots through the bindings, with the handler gameinit.pas names for a sample
        var old = GameInit.Services;
        try
        {
            GameInit.Services = new EhInitHost();
            GameInit.InitSpellFns();
            GameInit.InitEffectHandlers();
            for (int i = C_Effect.BLESS; i <= C_Effect.RECENT_CURE; i++) Yes(GDVars.effectFns[i] != null, "effectFns " + i);
            EqS("EffBless", GDVars.effectFns[1].Method.Name, "eff 1");
            EqS("EffExtraStr", GDVars.effectFns[12].Method.Name, "eff 12");
            EqS("EffExtraStr", GDVars.effectFns[38].Method.Name, "eff 38");
            EqS("EffHelpless", GDVars.effectFns[53].Method.Name, "eff 53");
            EqS("EffReadyItem", GDVars.effectFns[139].Method.Name, "eff 139");
            EqS("EffNull", GDVars.effectFns[127].Method.Name, "eff 127");
            EqS("EffHammerGone", GDVars.effectFns[137].Method.Name, "eff 137");
        }
        finally { GameInit.Services = old; }
        // BindCombatMap: shares CbtActs' map, or takes one, and refuses when there is none
        var w = EhBuild(9);
        var keep = CbtActs.CombatMap;
        try
        {
            CbtActs.CombatMap = null;
            bool threw = false;
            try { EffHndlrBindings.BindCombatMap(); } catch (NotSupportedException) { threw = true; }
            Yes(threw, "no map to bind");
            CbtActs.CombatMap = w;
            EffHndlrBindings.BindCombatMap();
            Yes(ReferenceEquals(EffHndlr.CombatMap, w), "shares CbtActs map");
            var w2 = EhBuild(10);
            EffHndlrBindings.BindCombatMap(w2);
            Yes(ReferenceEquals(EffHndlr.CombatMap, w2), "explicit map");
        }
        finally { CbtActs.CombatMap = keep; }
    }
}
