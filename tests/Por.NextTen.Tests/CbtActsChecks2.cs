// CbtActs checks, part 2: the check groups. Every scenario is built from a seed, run through the oracle model of
// part 1 and through Rules/CbtActs.cs, and compared (see CbBoth).
using System;
using System.Collections.Generic;
using System.Linq;
using Por.Core;
using static Por.Core.Types;

static partial class Tests
{
    static TCharRecord cA, cB, cD;
    static TItemRec cW;
    static int cN1, cN2, cN3;
    static TAimTarget cT;

    static int CbN(int n) { return n; }

    // A creature's weapon kit: 0 none, 1 club, 2 dagger, 3 long sword (36), 4 bow with arrows, 5 bow without arrows,
    // 6 darts, 7 sling, 8 javelin, 9 hand axe, 10 holy water, 11 spear (base range 0), 12 plate armour + club,
    // 13 leather armour + short sword
    static void Arm(TCharRecord c, int kit, Random r)
    {
        int qty = r.Next(0, 5);
        switch (kit)
        {
            case 1: c.equipped[0] = Item(c.name + "club", C_Item.CLUB, 1); break;
            case 2: c.equipped[0] = Item(c.name + "dagger", C_Item.DAGGER, 1); break;
            case 3: c.equipped[0] = Item(c.name + "sword", 36, 1); break;
            case 4: c.equipped[0] = Item(c.name + "bow", BOW, 1); c.equipped[C_Equip.ARROW] = Item(c.name + "arrows", C_Item.ARROW, qty); break;
            case 5: c.equipped[0] = Item(c.name + "bow", BOW, 1); break;
            case 6: c.equipped[0] = Item(c.name + "darts", C_Item.DART, qty); break;
            case 7: c.equipped[0] = Item(c.name + "sling", C_Item.SLING, 1); break;
            case 8: c.equipped[0] = Item(c.name + "javelin", C_Item.JAVELIN, qty); break;
            case 9: c.equipped[0] = Item(c.name + "axe", C_Item.HAND_AXE, qty); break;
            case 10: c.equipped[0] = Item(c.name + "water", C_Item.HOLY_WATER_VIAL, qty); break;
            case 11: c.equipped[0] = Item(c.name + "spear", C_Item.SPEAR, 1); break;
            case 12: c.equipped[0] = Item(c.name + "club", C_Item.CLUB, 1); c.equipped[C_Equip.ARMOR] = Item(c.name + "plate", ARMOR_PLATE, 1); break;
            case 13: c.equipped[0] = Item(c.name + "ssword", C_Item.SHORT_SWORD, 1); c.equipped[C_Equip.ARMOR] = Item(c.name + "leather", C_Item.LEATHER_ARMOR, 1); break;
        }
    }
    static TItemRec MissileOf(TCharRecord c)
    {
        TItemRec w = null;
        if (CharHlp.HasRangedWeapon(c)) CharHlp.FindDamagingRangedWeapon(c, ref w);
        return w;
    }
    static void AddEffect(TCharRecord c, int id, int strength) { c.effectsAddr = new TEffectRec { id = (byte)id, strength = (byte)strength, next = c.effectsAddr }; }

    // Random creature fields that the unit reads.
    static void Fill(TCharRecord c, Random r)
    {
        c.curAttacks = (byte)r.Next(0, 4); c.curAttacks2 = (byte)r.Next(0, 3); c.attacks = (byte)r.Next(0, 6); c.attacks2 = (byte)r.Next(0, 4);
        c.curRollsA[1] = (byte)r.Next(0, 4); c.curDiceA[1] = (byte)r.Next(1, 11); c.curModA[1] = (sbyte)r.Next(-6, 7);
        c.curRollsA[2] = (byte)r.Next(0, 3); c.curDiceA[2] = (byte)r.Next(1, 7); c.curModA[2] = (sbyte)r.Next(-3, 4);
        c.hpCur = (byte)r.Next(1, 40); c.hpMax = (byte)Math.Max(c.hpCur, r.Next(1, 60)); c.acCur = (byte)r.Next(40, 70); c.acBehind = (byte)r.Next(0, 70);
        c.movementCur = (byte)r.Next(0, 14); c.attackLevel = (byte)r.Next(0, 8); c.levelHighest = (sbyte)(r.Next(3) == 0 ? 0 : r.Next(1, 9));
        c.iconDimensions = (byte)(r.Next(3) == 0 ? r.Next(256) : 1); c.dexCur = (byte)r.Next(3, 41); c.quickfight = (byte)r.Next(2);
        var k = c.combatAddr;
        k.attackPhase = (byte)r.Next(1, 3); k.sweepBudget = (byte)r.Next(0, 6); k.movement = (byte)r.Next(0, 8); k.guarding = (byte)(r.Next(3) == 0 ? 1 : 0);
        k.madeAttack = (byte)r.Next(2); k.direction = (byte)r.Next(8); k.flankCount = (byte)r.Next(0, 4); k.flankSpread = (byte)r.Next(8);
        k.initiative = (sbyte)r.Next(0, 12); k.castAllowed = (byte)r.Next(2); k.itemUseAllowed = (byte)r.Next(2); k.spellCast = (byte)(r.Next(4) == 0 ? r.Next(1, 20) : 0);
        for (int l = 0; l < 8; l++) c.classLevels[l] = (sbyte)(r.Next(3) == 0 ? r.Next(-1, 13) : 0);
        if (r.Next(5) == 0) AddEffect(c, GDConsts.helplessFx[r.Next(1, 5)], 1);
        if (r.Next(5) == 0) AddEffect(c, C_Effect.MIRROR_IMAGE, r.Next(1, 4));
    }

    // Distinct cells around the centre of a 7x7 box (so the fake view test can fail).
    static void Scatter(Random r, TCharRecord[] cs, int box)
    {
        var used = new HashSet<int>();
        foreach (var c in cs)
        {
            int x, y;
            do { x = 10 + r.Next(box); y = 10 + r.Next(box); } while (!used.Add(x * 100 + y));
            cbPos[c] = new KeyValuePair<int, int>(x, y);
        }
    }

    static Action<byte, TCharRecord> VetoHook(Random r, int delta, TCharRecord[] cs)
    {
        var vt = new HashSet<string>(); var va = new HashSet<string>();
        foreach (var c in cs) { if (r.Next(6) == 0) vt.Add(c.name); foreach (var d in cs) if (r.Next(8) == 0) va.Add(c.name + ">" + d.name); }
        return (cond, c) =>
        {
            if (cond == C_Cond.MOVE_ATTACK_RATE) { CbLog("flag" + GDVars.dsCurrentCreatureCalculatingFlag); GDVars.dsCurrentAttackCurrent = unchecked((byte)(GDVars.dsCurrentAttackCurrent + delta)); }
            if (cond == C_Cond.TARGET_VETO && vt.Contains(c.name)) GDVars.dsCurrentSuppressTargetingFlag = 1;
            if (cond == C_Cond.ATTACKER_VETO && c.combatAddr.other != null && va.Contains(c.name + ">" + c.combatAddr.other.name)) GDVars.dsCurrentSuppressTargetingFlag = 1;
        };
    }

    static TCharRecord[] World(int sd, int party, int mons, int box)
    {
        var r = new Random(sd);
        CbReset((uint)sd);
        var cs = new TCharRecord[party + mons];
        for (int i = 0; i < cs.Length; i++)
        {
            cs[i] = Cr((i < party ? "P" : "M") + i, i < party ? 0 : 1, 0, 0);
            Fill(cs[i], r);
            Arm(cs[i], r.Next(14), r);
        }
        Scatter(r, cs, box);
        Place(cs);
        GDVars.roundCounter = (byte)r.Next(256);
        GDVars.zone1[C_Gbvm.SURPRISE] = (ushort)r.Next(4);
        GDVars.zone1[C_Gbvm.MOD_PARTY_MOVE] = (ushort)(r.Next(4) == 0 ? r.Next(65536) : r.Next(4));
        cbHitOn = r.Next(1, 22);
        GDVars.dsCurrentSuppressTargetingFlag = (byte)r.Next(2);   // CanAttackTarget must clear it itself
        cbHook = VetoHook(r, r.Next(-2, 4), cs);
        if (r.Next(4) == 0) cbHp0Hook = c => { c.enabled = 1; };
        foreach (var c in cs) if (r.Next(5) == 0) cbHidden.Add(c);
        return cs;
    }

    // ---------------- groups ----------------
    static void CbtRoundStart()
    {
        for (int s = 1; s <= 1500; s++)
        {
            int sd = 1000 + s;
            Action b = () => { var w = World(sd, 1, 1, 3); cA = w[new Random(sd).Next(2)]; cA.enabled = (byte)(sd % 5 == 0 ? 0 : 1); if (sd % 3 == 0) { cA.movementCur = (byte)(94 + sd % 5); GDVars.zone1[C_Gbvm.MOD_PARTY_MOVE] = 0; } };
            CbBoth("RoundStart " + sd, b, () => { ORoundStart(cA); return ""; }, () => { CbtActs.CombatRoundStartSetup(cA); return ""; });
            CbBoth("CalcMove " + sd, b, () => OCalcMove(cA).ToString(), () => CbtActs.CalcCreatureMovement(cA).ToString());
            CbBoth("SetCur " + sd, b, () => { OSetCur(cA); return ""; }, () => { CbtActs.SetCurAttacksX2(cA); return ""; });
        }
        for (int rc = 0; rc < 2; rc++)
            for (int n = 0; n < 256; n++)
            {
                GDVars.roundCounter = (byte)rc;
                Eq((n + rc) % 256 / 2, CbtActs.AdjustedAttacks((byte)n), "AdjustedAttacks " + n + " round " + rc);
            }
    }

    static void CbtDamage()
    {
        for (int s = 1; s <= 2500; s++)
        {
            int sd = 5000 + s;
            Action b = () =>
            {
                var w = World(sd, 1, 1, 3); var r = new Random(sd * 7);
                cA = w[0]; cB = w[1];
                if (r.Next(2) == 0) { cA = w[1]; cB = w[0]; }
                if (r.Next(2) == 0) cB.name = GDConsts.rangerFoes[r.Next(1, 17)];
                cA.classLevels[C_Level.THIEF] = (sbyte)r.Next(-1, 13); cA.classLevels[C_Level.RANGER] = (sbyte)(r.Next(2) == 0 ? r.Next(1, 128) : 0);
                cB.combatAddr.flankCount = (byte)r.Next(4);
                if (r.Next(2) == 0) { cA.equipped[C_Equip.ARMOR] = null; if (r.Next(2) == 0) cA.equipped[0] = null; }
                if (r.Next(2) == 0) cB.combatAddr.direction = ODir(cA, cB);
                cN1 = r.Next(256); cN2 = r.Next(1, 3);
                cA.curModA[cN2] = (sbyte)r.Next(-128, 128);
                cA.curRollsA[cN2] = (byte)r.Next(0, 60); cA.curDiceA[cN2] = (byte)r.Next(1, 20);
            };
            CbBoth("WeaponDamage " + sd, b, () => { OWeaponDamage(cA, cB, cN2); return ""; }, () => { CbtActs.CalcWeaponDamage(cA, cB, (byte)cN2); return ""; });
            CbBoth("Backstab " + sd, b, () => OBackstab(cA, cB).ToString(), () => CbtActs.IsBackstab(cA, cB).ToString());
            CbBoth("ModDef " + sd, b, () => OModDef(cA, cB, (byte)cN1).ToString(), () => { byte d = (byte)cN1; CbtActs.ModifyDefenseForDistance(cA, cB, ref d); return d.ToString(); });
        }
    }

    static void CbtResult()
    {
        for (int s = 1; s <= 3000; s++)
        {
            int sd = 9000 + s;
            Action b = () =>
            {
                var w = World(sd, 1, 1, 3); var r = new Random(sd * 3);
                cA = w[0]; cB = w[1]; cN1 = r.Next(5); cN2 = r.Next(3) == 0 ? 0 : r.Next(256); cN3 = r.Next(3) == 0 ? 1 : r.Next(256);
                cB.status = (byte)(r.Next(3) == 0 ? r.Next(9) : 0); if (r.Next(4) == 0) cB.enabled = 0;
                cD = r.Next(2) == 0 ? cA : null;
            };
            CbBoth("Result " + sd, b, () => { OResult(cA, cB, cN1, cN2, cN3, cD != null); return ""; },
                   () => { CbtActs.ProcessAttackResult(cA, cB, (byte)cN1, (byte)cN2, (byte)cN3, cD != null); return ""; });
        }
    }

    static void CbtDoAttack()
    {
        for (int s = 1; s <= 3000; s++)
        {
            int sd = 20000 + s;
            Action b = () =>
            {
                var w = World(sd, 1, 1, 3); var r = new Random(sd * 5);
                cA = w[0]; cB = w[1]; if (r.Next(2) == 0) { cA = w[1]; cB = w[0]; }
                if (r.Next(3) == 0) cB.combatAddr.direction = ODir(cA, cB);
                if (r.Next(3) == 0) { cA.classLevels[C_Level.THIEF] = (sbyte)r.Next(1, 13); cA.equipped[C_Equip.ARMOR] = null; }
                cB.hpCur = (byte)(r.Next(6) == 0 ? r.Next(240, 256) : r.Next(1, 30));
                cB.combatAddr.flankSpread = (byte)r.Next(8);
                if (r.Next(4) == 0) cA.combatAddr.attackPhase = (byte)r.Next(0, 3);
                cN1 = r.Next(2);
            };
            CbBoth("DoAttack " + sd, b, () => ODoAttack(cA, cB, cN1 == 1).ToString(),
                   () => { bool d = false; CbtActs.DoAttack(cA, cB, cN1 == 1, ref d); return d.ToString(); });
        }
    }

    static void CbtAttack()
    {
        for (int s = 1; s <= 2500; s++)
        {
            int sd = 30000 + s;
            Action b = () =>
            {
                var w = World(sd, 1, 2, 3); var r = new Random(sd * 11);
                cA = w[0]; cB = w[1 + r.Next(2)]; if (r.Next(2) == 0) { var t = cA; cA = cB; cB = t; }
                Arm(cA, new[] { 4, 6, 7, 8, 9, 10, 1 }[r.Next(7)], r);
                cW = r.Next(4) == 0 ? null : MissileOf(cA);
                if (cW != null && r.Next(4) == 0) cW.special = C_Effect.SPIRIT_HAMMER_GONE;
                if (cW != null) cW.quantity = (byte)r.Next(0, 4);
                if (r.Next(3) == 0) { var tr = Item("old", 3, 1); GDVars.treasureItems = tr; }
                cN1 = r.Next(2);
            };
            CbBoth("CombatAttack " + sd, b, () => OCombatAttack(cA, cB, cN1 == 1, cW).ToString(),
                   () => { bool d = false; CbtActs.CombatAttack(cA, cB, cN1 == 1, cW, ref d); return d.ToString(); });
        }
        // the missile flight art for every direction and missile family
        int[] kinds = { C_Item.DART, C_Item.JAVELIN, C_Item.QUARREL, C_Item.SPEAR, C_Item.ARROW, C_Item.HAND_AXE, C_Item.CLUB, C_Item.HAMMER,
                        C_Item.HOLY_WATER_VIAL, C_Item.FLASK_OF_OIL, C_Item.SLING, 3, BOW };
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                foreach (int k in kinds)
                {
                    int kk = k, ddx = dx, ddy = dy;
                    Action b = () => { CbReset(7); cA = Cr("A", 0, 20, 12); cB = Cr("B", 1, 20 + 3 * ddx, 12 + 3 * ddy); Place(cA, cB); cW = Item("m", kk, 1); };
                    CbBoth("Anim " + k + " " + dx + "," + dy, b, () => { OAnim(cA, cB, cW); return ""; }, () => { CbtActs.RangedAttackAnim(cA, cB, cW); return ""; });
                }
    }

    static void CbtMove()
    {
        for (int s = 1; s <= 2000; s++)
        {
            int sd = 40000 + s;
            Action b = () =>
            {
                var w = World(sd, 2, 4, 4); var r = new Random(sd * 13);
                cA = w[r.Next(w.Length)]; cN1 = r.Next(8);
                if (r.Next(10) == 0) { cA.combatAddr.movement = 9; }
                foreach (var c in w) if (r.Next(3) == 0) c.combatAddr.guarding = 1;
                foreach (var c in w) { if (r.Next(8) == 0) AddEffect(c, C_Effect.BLOOD_DRAIN, 1); if (r.Next(8) == 0) AddEffect(c, C_Effect.BITE_HOLD, 1); }
                if (r.Next(12) == 0) { GDVars.arenaCount = (byte)(GDVars.arenaCount - 1); if (GDVars.creatureArr[GDVars.arenaCount + 1] != cA) cA = w[w.Length - 1]; }
            };
            CbBoth("Move " + sd, b, () => { OMove(cA, (byte)cN1); return ""; }, () => { CbtActs.CombatMoveCreature(cA, (byte)cN1); return ""; });
            CbBoth("Opportunity " + sd, b, () => { OOpportunity(cA, (byte)cN1); return ""; }, () => { CbtActs.MoveOpportunityAttacks(cA, (byte)cN1); return ""; });
            CbBoth("Guarding " + sd, b, () => { OGuarding(cA); return ""; }, () => { CbtActs.GuardingAttacks(cA); return ""; });
        }
    }

    static void CbtFlee()
    {
        for (int s = 1; s <= 2500; s++)
        {
            int sd = 50000 + s;
            Action b = () =>
            {
                var w = World(sd, 2, 3, 6); var r = new Random(sd * 17);
                cA = w[r.Next(w.Length)];
                foreach (var c in w) { if (r.Next(4) == 0) c.enabled = 0; c.movementCur = (byte)r.Next(0, 16); c.hpMax = (byte)r.Next(0, 256); c.hpCur = (byte)r.Next(0, 256); }
                if (r.Next(5) == 0) foreach (var c in w) c.hostile = 0;
            };
            CbBoth("Flee " + sd, b, () => OFlee(cA).ToString(), () => CbtActs.CombatFleeAttempt(cA).ToString());
            CbBoth("Highest " + sd, b, () => OHighest(cA).ToString(), () => CbtActs.HighestEnemyMovement(cA).ToString());
            CbBoth("HpPct " + sd, b, () => { OHpPct(); return ""; }, () => { CbtActs.CalcHostileHpPercent(); return ""; });
        }
        // the Word sums: 300 hostiles of 255/255 hp wrap curSum and maxSum
        Action big = () =>
        {
            CbReset(3); var cs = new TCharRecord[300];
            for (int i = 0; i < 300; i++) { cs[i] = Cr("H" + i, 1, 0, 0); cs[i].hpCur = 255; cs[i].hpMax = 255; cs[i].enabled = (byte)(i % 3 == 0 ? 0 : 1); }
            for (int i = 0; i < 299; i++) cs[i].nextCharAddr = cs[i + 1];
            GDVars.creatureList = cs[0];
        };
        CbBoth("HpPct wrap", big, () => { OHpPct(); return ""; }, () => { CbtActs.CalcHostileHpPercent(); return ""; });
    }

    static void CbtSweep()
    {
        for (int s = 1; s <= 2500; s++)
        {
            int sd = 60000 + s;
            Action b = () =>
            {
                var w = World(sd, 1, 6, 3); var r = new Random(sd * 19);
                cA = w[0]; cB = w[1 + r.Next(6)]; if (r.Next(6) == 0) cB = w[0];
                foreach (var c in w) if (r.Next(2) == 0) c.levelHighest = 0;
                cA.combatAddr.sweepBudget = (byte)r.Next(0, 8); cA.curAttacks = (byte)r.Next(0, 3);
                if (r.Next(4) == 0) { cA = w[1]; cB = w[2]; }
            };
            CbBoth("Sweep " + sd, b, () => OSweep(cA, cB).ToString(), () => CbtActs.SweepAttempt(cA, cB).ToString());
            CbBoth("Note " + sd, b, () => { ONote(cA, cB); return ""; }, () => { CbtActs.NoteAttackDirection(cA, cB); return ""; });
            CbBoth("CanAttack " + sd, b, () => OCanAttack(cA, cB).ToString(), () => CbtActs.CanAttackTarget(cA, cB).ToString());
        }
        Action b2 = () => { World(1, 1, 1, 3); cA = GDVars.creatureArr[1]; };
        CbBoth("CanAttack nil", b2, () => OCanAttack(cA, null).ToString(), () => CbtActs.CanAttackTarget(cA, null).ToString());
        // every relative cell on a 15x15 field, including the same cell
        for (int x = 0; x < 15; x++)
            for (int y = 0; y < 15; y++)
            {
                CbReset(1); var a = Cr("A", 0, 7, 7); var o = Cr("B", 1, x, y); Place(a, o);
                Eq(ODir(a, o), CbtActs.DirTowardTarget(a, o), "DirToward " + x + "," + y);
            }
    }

    static void CbtTurn()
    {
        for (int s = 1; s <= 2500; s++)
        {
            int sd = 70000 + s;
            Action b = () =>
            {
                var w = World(sd, 1, 6, 8); var r = new Random(sd * 23);
                cA = w[0]; cA.classLevels[C_Level.CLERIC] = (sbyte)r.Next(-1, 16); cA.classLevels[C_Level.PALADIN] = (sbyte)r.Next(-1, 16);
                foreach (var c in w.Skip(1)) { c.levelUndead = (sbyte)(r.Next(4) == 0 ? 0 : r.Next(1, 14)); c.combatAddr.turned = (byte)(r.Next(5) == 0 ? 1 : 0); }
                if (r.Next(4) == 0) { cA = w[1]; cA.classLevels[C_Level.CLERIC] = (sbyte)r.Next(0, 20); }
            };
            CbBoth("Turn " + sd, b, () => { OTurn(cA); return ""; }, () => { CbtActs.CombatTurnUndead(cA); return ""; });
            CbBoth("TurnLevel " + sd, b, () => OTurnLevel(cA).ToString(), () => CbtActs.TurnLevel(cA).ToString());
            CbBoth("FindTurn " + sd, b, () => Nm(OFindTurn(cA)), () => { object t = cB; bool f = CbtActs.FindTurnUndeadTarget(cA, ref t); return f ? Nm(t) : (t == null ? "nil" : "BAD"); });
        }
    }

    static void CbtTurnMany()
    {
        for (int s = 1; s <= 400; s++)
        {
            int sd = 75000 + s;
            Action b = () =>
            {
                var r = new Random(sd); CbReset((uint)sd);
                var cs = new List<TCharRecord>();
                var cl = Cr("Cleric", 0, 3, 3); cl.classLevels[C_Level.CLERIC] = (sbyte)r.Next(6, 16); cs.Add(cl);
                int n = r.Next(7, 14);
                for (int i = 0; i < n; i++) { var u = Cr("U" + i, 1, 0, 0); u.levelUndead = (sbyte)r.Next(1, 4); cs.Add(u); }
                Scatter(r, cs.ToArray(), 6);
                Place(cs.ToArray());
                cA = cl;
            };
            CbBoth("TurnMany " + sd, b, () => { OTurn(cA); return ""; }, () => { CbtActs.CombatTurnUndead(cA); return ""; });
        }
    }

    static void CbtSpells()
    {
        for (int s = 1; s <= 2500; s++)
        {
            int sd = 80000 + s;
            Action b = () =>
            {
                var w = World(sd, 3, 3, 3); var r = new Random(sd * 29);
                cA = w[r.Next(w.Length)];
                GDVars.curChar = cA;
                foreach (var c in w) { c.hpMax = (byte)r.Next(1, 30); c.hpCur = (byte)r.Next(0, c.hpMax + 1); }
                // corpses on free cells around cA
                int n = r.Next(0, 4); GDVars.downedCount = (byte)n;
                for (int j = 1; j <= n; j++)
                {
                    var body = Cr("D" + j, cA.hostile, 0, 0); body.status = (byte)r.Next(9); if (r.Next(5) == 0) body = null;
                    GDVars.downed[j] = new TDownedRec { cr = body, x = (sbyte)(cbPos[cA].Key + r.Next(-1, 2)), y = (sbyte)(cbPos[cA].Value + r.Next(-1, 2)) };
                    cbTerr[GDVars.downed[j].x * 100 + GDVars.downed[j].y] = r.Next(3) == 0 ? (byte)5 : (byte)C_Terr.CORPSE;
                }
                cN1 = r.Next(1, 68); cN2 = r.Next(1, 5);
                if (r.Next(4) == 0) cN1 = C_Spell.CURE_LIGHT_WOUNDS;
                GDVars.zone0.view3dFlag = (ushort)r.Next(2);
            };
            CbBoth("FindCure " + sd, b, () => { object t = cB; bool f = OFindCure(cA, ref t); return f + Nm(t); }, () => { object t = cB; bool f = CbtActs.FindCureTarget(cA, ref t); return f + Nm(t); });
            CbBoth("CastMenuAI " + sd, b, () => OCastMenu(cN1, true).ToString(), () => { bool c = false; CbtActs.CombatCastTargetMenu((byte)cN1, true, ref c); return c.ToString(); });
            CbBoth("AreaHits " + sd, b, () => OAreaHits((sbyte)cbPos[cA].Key, (sbyte)cbPos[cA].Value, (byte)cN2).ToString(),
                   () => CbtActs.AreaHitsNonEnemy((sbyte)cbPos[cA].Key, (sbyte)cbPos[cA].Value, (byte)cN2).ToString());
        }
        for (int sp = 0; sp <= 255; sp++) Eq(OAimMarker(sp), CbtActs.AimMarker((byte)sp), "AimMarker " + sp);
        // CastSpellCombat: given spell or Cast list, scrolls, camp-only spells, AI flag, casting time
        for (int s = 1; s <= 2500; s++)
        {
            int sd = 90000 + s;
            Action b = () =>
            {
                var w = World(sd, 1, 1, 3); var r = new Random(sd * 31);
                cA = w[0]; GDVars.curChar = cA;
                cN1 = r.Next(3) == 0 ? 0 : r.Next(1, 68); cN2 = r.Next(2) == 0 ? 0 : r.Next(1, 3);
                cbSpellPick.Enqueue((byte)(r.Next(4) == 0 ? 0 : r.Next(1, 68)));
                cbScroll = r.Next(3) == 0 ? Item("scroll", 60, 1) : null;
                cbUseResult = r.Next(2) == 0; cbCastNcResult = r.Next(2) == 0;
                cA.combatAddr.initiative = (sbyte)r.Next(-3, 21);
            };
            CbBoth("CastSpell " + sd, b, () => OCastSpell(cN1, cN2).ToString(), () => { bool c = true; CbtActs.CastSpellCombat((byte)cN1, (byte)cN2, ref c); return c.ToString(); });
        }
    }

    static byte[] cbKeyPool = { (byte)'N', (byte)'P', (byte)'M', (byte)'T', (byte)'C', (byte)'E', 0, 72, 80, 75, 77, 71, 79, 73, 81, (byte)'X' };

    static void CbtAim()
    {
        for (int s = 1; s <= 2500; s++)
        {
            int sd = 100000 + s;
            Action b = () =>
            {
                var w = World(sd, 2, 4, 4); var r = new Random(sd * 37);
                cA = w[r.Next(2)]; GDVars.curChar = cA;
                for (int i = 0; i < 30; i++) { byte k = cbKeyPool[r.Next(cbKeyPool.Length)]; cbKeys.Enqueue(new KeyValuePair<byte, bool>(k, k >= 71 && k <= 81 ? r.Next(4) != 0 : r.Next(8) == 0)); }
                for (int i = 0; i < 10; i++) cbYes.Enqueue(r.Next(2) == 0 ? (byte)'Y' : (byte)'N');
                for (int i = 0; i < 6; i++) cbBlocked.Add((10 + r.Next(6)) * 100 + 10 + r.Next(6));
                for (int i = 0; i < 4; i++) cbTerr[(10 + r.Next(6)) * 100 + 10 + r.Next(6)] = r.Next(2) == 0 ? (byte)1 : (byte)C_Terr.CORPSE;
                int n = r.Next(0, 3); GDVars.downedCount = (byte)n;
                for (int j = 1; j <= n; j++) { var body = Cr("D" + j, 1, 0, 0); GDVars.downed[j] = new TDownedRec { cr = body, x = (sbyte)(10 + r.Next(6)), y = (sbyte)(10 + r.Next(6)) }; }
                foreach (var c in w) { c.npc = (byte)(r.Next(3) == 0 ? 0x80 : 0); if (r.Next(4) == 0) c.status = C_Status.DYING; }
                cN1 = r.Next(4) == 0 ? 255 : r.Next(0, 8); cN2 = r.Next(2); cN3 = r.Next(2);
                // a weapon aim never allows a bare cell (Combat passes groundOk 0): with both, Pascal would read nil^.hostile
                if (cN2 == 1) cN3 = 0;
                cT = r.Next(5) == 0 ? null : new TAimTarget { cr = cA, x = 3, y = 4, pad = 9 };
            };
            CbBoth("AimMenu " + sd, b,
                   () => { var t = cT ?? new TAimTarget(); bool d = OAimMenu(cA, cN1, cN2, cN3, t); return d + Nm(t.cr) + t.x + "," + t.y + "/" + t.pad; },
                   () => { var t = cT; bool d = false; CbtActs.CombatAimMenu(cA, (byte)cN1, (byte)cN2, (byte)cN3, ref d, ref t); return d + Nm(t.cr) + t.x + "," + t.y + "/" + t.pad; });
            CbBoth("CheckAlly " + sd, b, () => OCheckAlly(cA, GDVars.creatureArr[1 + sd % 6]).ToString(), () => CbtActs.CheckAttackAlly(cA, GDVars.creatureArr[1 + sd % 6]).ToString());
            CbBoth("CastMenuManual " + sd, b, () => OCastMenu(1 + sd % 67, false).ToString(), () => { bool c = false; CbtActs.CombatCastTargetMenu((byte)(1 + sd % 67), false, ref c); return c.ToString(); });
        }
        for (int k = 0; k < 256; k++) Eq(ODirKey(k), CbtActs.DirOfKey((byte)k), "DirOfKey " + k);
    }

    static void CbtAcquire()
    {
        for (int s = 1; s <= 3000; s++)
        {
            int sd = 110000 + s;
            Action b = () =>
            {
                var w = World(sd, 2, 5, 7); var r = new Random(sd * 41);
                cA = w[r.Next(w.Length)];
                cA.combatAddr.other = r.Next(3) == 0 ? null : w[r.Next(w.Length)];
                foreach (var c in w) if (r.Next(5) == 0) c.enabled = 0;
                cN1 = r.Next(4) == 0 ? 255 : r.Next(0, 6); cN2 = r.Next(2); cN3 = r.Next(2);
            };
            CbBoth("Acquire " + sd, b, () => OAcquire(cA, (byte)cN1, (byte)cN2, (byte)cN3).ToString(), () => CbtActs.AcquireTarget(cA, (byte)cN1, (byte)cN2, (byte)cN3).ToString());
        }
    }

    static void CbtHandlers()
    {
        for (int s = 1; s <= 2000; s++)
        {
            int sd = 120000 + s;
            Action b = () =>
            {
                var w = World(sd, 2, 2, 3); var r = new Random(sd * 43);
                cA = w[2]; cB = w[r.Next(2)]; cA.combatAddr.other = cB;
                GDVars.hitsMade[1] = (byte)r.Next(4);
                if (r.Next(4) == 0) cB.enabled = 0;
                cbHoldVictim = cB; cbHoldNil = r.Next(3) == 0;
                cN1 = r.Next(2);
                GDVars.stingArg = r.Next(3) != 0;
                if (r.Next(4) == 0) GDVars.arenaCount = (byte)(GDVars.arenaCount - 1);
                GDVars.arenaPtr.ox = (sbyte)r.Next(-5, 40); GDVars.arenaPtr.oy = (sbyte)r.Next(-5, 20);
            };
            CbBoth("Rake " + sd, b, () => { ORake(cA); return ""; }, () => { CbtActs.RearClawRake(cA, null, false); return ""; });
            CbBoth("Bite " + sd, b, () => { OBite(cA, "eff", cN1 == 1); return ""; }, () => { CbtActs.BiteAndHold(cA, "eff", cN1 == 1); return ""; });
            CbBoth("Sting " + sd, b, () => OSting().ToString(), () => CbtActs.StingVictoryCheat().ToString());
        }
    }

    static void Throws(Action a, string what)
    {
        bool threw = false;
        try { a(); } catch (NotSupportedException) { threw = true; }
        Yes(threw, what + " throws NotSupportedException");
    }

    static void CbtSeams()
    {
        World(5, 1, 1, 3);
        var p = GDVars.creatureArr[1]; var m = GDVars.creatureArr[2];
        GDVars.curChar = p;
        CbtActs.CombatMap = null;
        p.classLevels[C_Level.RANGER] = 1; p.classLevels[C_Level.THIEF] = 0;
        Throws(() => CbtActs.CalcWeaponDamage(p, m, 1), "no CbtMap: CalcWeaponDamage (ranger path)");
        Throws(() => CbtActs.CombatFleeAttempt(p), "no CbtMap: flee");
        Throws(() => CbtActs.DirTowardTarget(p, m), "no CbtMap: DirTowardTarget");
        GDVars.stingArg = true;
        Throws(() => CbtActs.StingVictoryCheat(), "no CbtMap: sting");
        CbInstall(); CbtActs.Effects = null;
        Throws(() => CbtActs.CalcCreatureMovement(p), "no FxEngine: CalcCreatureMovement");
        Throws(() => CbtActs.CanAttackTarget(p, m), "no FxEngine: CanAttackTarget");
        Yes(!CbtActs.CanAttackTarget(p, null) && CbtActs.CanAttackTarget(p, p), "CanAttackTarget answers nil and self before reaching FxEngine");
        CbInstall(); CbtActs.MagicUnit = null;
        Throws(() => { bool c = false; CbtActs.CastSpellCombat(0, 0, ref c); }, "no Magic: CastSpellCombat from the list");
        CbInstall(); TextRend.DelayMilliseconds = null;
        Throws(() => { bool d = false; CbtActs.CombatAttack(p, m, false, null, ref d); }, "no delay host: CombatAttack");
        CbInstall();
    }
}
