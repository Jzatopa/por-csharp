// EffHndlr oracle: a separate model of every effhndlr.pas routine, written from the Pascal text (not from the port). It
// drives the same recording world as the port, so the two produce comparable call logs. Effect numbers are the literal
// values the source comments give, a cross-check on the C_Effect constants. The small pure helpers (HasEffect, ClampScore,
// the Constitution tables, IsNpc, WarriorLevel, EnemyTeam, NumStr, the dice) are modelled here; CharHlp's
// FindDamagingRangedWeapon/BaseItem/GetCasterLevel and ClassCal.ConHpTotal are shared with the port (accepted and tested
// with their own units).
using System;
using Por.Core;
using static Por.Core.Types;

sealed class EhOracle
{
    readonly EhWorld W;
    public EhOracle(EhWorld w) { W = w; }

    static byte U8(int v) { return unchecked((byte)v); }
    static sbyte S8(int v) { return unchecked((sbyte)v); }
    static short S16(int v) { return unchecked((short)v); }
    static TEffectRec N(object p) { return (TEffectRec)p; }
    static TCharRecord Other { get { return GDVars.dsOtherCreatureCurrentPtr; } set { GDVars.dsOtherCreatureCurrentPtr = value; } }

    // Dice.RollDice: n rolls of RandomBelow(sides) + 1, summed in a Byte
    int Roll(int n, int sides) { int t = 0; for (int i = 0; i < n; i++) t = (t + W.RandomBelow(sides) + 1) & 0xFF; return t; }
    int DamageRoll(int n, int sides) { GDVars.gDamageDice = U8(n); return Roll(n, sides); }

    void Atk(int d) { GDVars.gAttackRoll = S8(GDVars.gAttackRoll + d); }
    void Sv(int d) { GDVars.gSaveRoll = S16(GDVars.gSaveRoll + d); }
    static void Halve() { GDVars.pendingDamage = U8(GDVars.pendingDamage >> 1); }
    static bool Elem(int bits) { return (GDVars.elementMask & bits) != 0; }

    static bool Has(TCharRecord c, int id) { for (var e = c.effectsAddr; e != null; e = e.next) if (e.id == id) return true; return false; }
    static int Clamp(int s) { return s < 3 ? 3 : (s > 25 ? 25 : s); }
    static int RegenMinutes(int con) { return con < 20 ? 0 : (con > 25 ? 10 : (26 - con) * 10); }
    static int PoisonBonus(int con) { return con >= 25 ? 4 : con >= 23 ? 3 : con >= 21 ? 2 : con >= 19 ? 1 : 0; }
    static int ConSave(int con) { return con * 2 / 7; }
    static bool Npc(TCharRecord c) { return (c.npc & 0x80) != 0; }
    static int Warrior(TCharRecord c)
    {
        int b = Math.Max(c.classLevels[2], Math.Max(c.classLevels[3], c.classLevels[4]));
        return b < 0 ? 0 : b;
    }
    static int EnemyOf(TCharRecord c) { return c.hostile == 0 ? 1 : 0; }

    static void Nullify(int id)
    {
        if (id != 0 && GDVars.pendingEffectId != id) return;
        GDVars.pendingDamage = 0;
        GDVars.pendingEffectId = 0;
    }

    bool ArmUnlessLocked(TCharRecord c, int id, int strength, int minutes)
    {
        if (GDVars.effectLock != 0) return false;
        W.AddEffect(c, U8(id), (ushort)minutes, U8(strength), true);
        return true;
    }

    void Fall(TCharRecord c, int status, string msg)
    {
        W.PrintName(c, msg.Length > 40 ? msg.Substring(0, 40) : msg, 10, false);
        if (c.status == C_Status.DEAD || c.status == C_Status.STONED || c.status == C_Status.GONE) return;
        c.status = U8(status);
        c.enabled = 0;
        c.hpCur = 0;
        W.RemoveEffectsHp0(c);
        W.TriggerEffectsWithCondition(13, c);
        if (c.enabled == 0) W.CreatureDowned(c);
        W.PauseOnly();
        W.ClearMsg();
        if (GDVars.ifcState != C_Ifc.COMBAT) W.DisplayParty(GDVars.curChar);
    }

    static TItemRec DamagingWeapon(TCharRecord c)
    {
        TItemRec wpn = c.equipped[0];
        if (wpn == null) return null;
        TItemRec sel = null;
        if (CharHlp.FindDamagingRangedWeapon(c, ref sel) && sel != null) return sel;
        return wpn;
    }

    void Avoid(TCharRecord c, int pct)
    {
        if (GDVars.curChar.equipped[0] == null) return;
        if (W.CalcDistance(GDVars.curChar, c) < 2) return;
        if (Roll(1, 100) > pct) return;
        W.PrintName(c, "Avoids it", 10, true);
        GDVars.pendingDamage = 0;
        GDVars.gAttackRoll = -1;
        int ph = GDVars.curChar.combatAddr.attackPhase;
        if ((ph == 1 || ph == 2) && GDVars.hitsMade[ph] != 0) GDVars.hitsMade[ph] = U8(GDVars.hitsMade[ph] - 1);
    }

    void Poison(TCharRecord c, int mod)
    {
        Other = c.combatAddr.other;
        if (W.RollSavingThrow(Other, 0, S8(mod + PoisonBonus(Other.conCur)))) return;
        W.PrintName(Other, "is Poisoned", 10, false);
        W.PauseOnly();
        W.AddEffect(Other, 55, 0, 255, false);
        Fall(Other, C_Status.DEAD, "is killed");
    }

    void Paralyze(TCharRecord c, int minutes)
    {
        Other = c.combatAddr.other;
        if (W.RollSavingThrow(Other, 0, 0)) return;
        W.PrintNameAnim(Other, true, "is Paralyzed");
        W.AddEffect(Other, 52, (ushort)minutes, 12, false);
    }

    void Resistance(int pct)
    {
        int lvl = CharHlp.GetCasterLevel(GDVars.dsCurrentSpellBeing);
        if (GDVars.usingItem && lvl > 6) lvl = 6;
        int chance = Math.Min(100, Math.Max(0, pct + 5 * (11 - lvl)));
        if (GDVars.pendingEffectId == 0 && !Elem(8)) return;
        if (Roll(1, 100) <= chance) Nullify(0);
    }

    // ClassToDrain: the class and its XP-table entry
    static int Choose(TCharRecord c, out int xp)
    {
        int best = -1, bestLvl = 0, xpMin = 0;
        for (int k = 0; k < 8; k++)
        {
            int lvl = c.classLevels[k];
            int x = lvl > 9 ? GDConsts.dsExperienceSpellsClass[k].xp[9] : (lvl > 1 ? GDConsts.dsExperienceSpellsClass[k].xp[lvl] : 0);
            if (lvl >= bestLvl && (x >= xpMin || x == -1)) { best = k; bestLvl = lvl; xpMin = x; }
        }
        if (best < 0) throw new InvalidOperationException("no class");
        xp = xpMin;
        return best;
    }

    void Drain(TCharRecord c, int levels)
    {
        W.PrintName(c, "is drained " + (levels > 1 ? levels + " levels" : "a level"), 10, true);
        if (c.status == C_Status.ANIMATED) { Fall(c, C_Status.GONE, "is dead forever"); c.hpMax = 0; return; }
        for (int n = 0; n < levels; n++)
        {
            if (c.status == C_Status.GONE) continue;
            c.drainedLevels = U8(c.drainedLevels + 1);
            int total = 0;
            foreach (sbyte l in c.classLevels) total += l;
            total &= 0xFF;
            if (total == 0) { Fall(c, C_Status.GONE, "is dead forever"); c.hpMax = 0; return; }
            int per = c.hpMax / total;
            if (GDVars.stingHpFlag != 0) W.HpDebug("max,cur,lost", c.hpMax, c.hpCur, c.drainedHps);
            // per steps of "decrement unless zero" on each, and per increments of drainedHps
            c.hpMax = U8(c.hpMax - Math.Min((int)c.hpMax, per));
            c.hpRolled = U8(c.hpRolled - Math.Min((int)c.hpRolled, per));
            c.hpCur = U8(c.hpCur - Math.Min((int)c.hpCur, per));
            c.drainedHps = U8(c.drainedHps + per);
            int xp;
            if (c.levelHighest < 2)
            {
                Fall(c, C_Status.GONE, "is dead forever");
                c.hpMax = 0;
                int k = Choose(c, out xp);
                if (c.classLevels[k] > 0 && levels == 2) c.classLevels[k] = 0;
            }
            else
            {
                int k = Choose(c, out xp);
                c.classLevels[k] = S8(c.classLevels[k] - 1);
                if (c.hpCur == 0 && c.status != C_Status.DEAD) Fall(c, C_Status.DEAD, "is killed");
                W.RecalcBaseSkills(c);
            }
            c.levelHighest = c.classLevels[Choose(c, out xp)];
            c.experience = xp;
            W.RecalcStats(c);
        }
    }

    static bool NameIn(string name, string[] list, int count)
    {
        for (int i = 1; i <= count; i++) if (name == list[i]) return true;
        return false;
    }
    static int CountIn(string name, string[] list, int count)
    {
        int n = 0;
        for (int i = 1; i <= count; i++) if (name == list[i]) n++;
        return n;
    }

    public void Run(string h, TCharRecord c, object p, bool removed)
    {
        switch (h)
        {
            case "EffBless": GDVars.dsCurrentMorale = U8(GDVars.dsCurrentMorale + 5); Atk(1); break;
            case "EffCurse": GDVars.dsCurrentMorale = U8(Math.Max(0, GDVars.dsCurrentMorale - 5)); Atk(-1); break;
            case "EffUndeadSlayer":
                Other = c.combatAddr.other;
                if (Other.charType == 4) { Atk(2); GDVars.pendingDamage = U8(GDVars.pendingDamage + 2); }
                break;
            case "EffTrainStart": W.PrintName(c, "starts to train", 10, true); W.AddEffect(c, 7, 43200, 255, true); break;
            case "EffFlameTongue":
            {
                Other = c.combatAddr.other;
                int t = Other.charType;
                int bonus = t == 10 ? 1 : (t == 9 || t == 12) ? 2 : t == 4 ? 3 : 0;
                Atk(bonus); GDVars.pendingDamage = U8(GDVars.pendingDamage + bonus);
                GDVars.elementMask = 1 | 8;
                break;
            }
            case "EffTrainManual":
            {
                W.PrintName(c, "is hardier", 10, true);
                c.conCur = U8(Clamp(c.conCur + 1));
                if (c.conCur >= 20 && !Has(c, 62)) W.AddEffect(c, 62, (ushort)RegenMinutes(c.conCur), 255, true);
                int gain = ClassCal.ConHpTotal(c, c.conCur) - ClassCal.ConHpTotal(c, U8(c.conCur - 1));
                gain = Math.Min(gain, 255 - c.hpMax);
                if (gain > 0) { c.hpMax = U8(c.hpMax + gain); c.hpCur = U8(c.hpCur + gain); }
                break;
            }
            case "EffProtEvil":
            {
                int a = GDVars.curChar.alignment;
                if (a == C_Align.LAWFUL_EVIL || a == C_Align.NEUTRAL_EVIL || a == C_Align.CHAOTIC_EVIL) { Sv(2); Atk(-2); }
                break;
            }
            case "EffProtGood":
            {
                int a = GDVars.curChar.alignment;
                if (a == C_Align.LAWFUL_GOOD || a == C_Align.NEUTRAL_GOOD || a == C_Align.CHAOTIC_GOOD) { Sv(2); Atk(-2); }
                break;
            }
            case "EffResistCold": if (Elem(2)) { Halve(); Sv(3); } break;
            case "EffResistFire": if (Elem(1)) { Halve(); Sv(3); } break;
            case "EffCharm":
            {
                var e = N(p);
                if (removed)
                {
                    c.hostile = S8((e.strength >> 6) & 1);
                    if (c.npc == 0xB3) c.npc = 0;
                    break;
                }
                if ((e.strength & 0x20) == 0)
                {
                    e.strength = U8(e.strength + 32 + 64 * c.hostile);
                    c.hostile = S8(e.strength >> 7);
                    c.quickfight = 1;
                    if (!Npc(c)) c.npc = 0xB3;
                    c.combatAddr.other = null;
                    W.CountTeams();
                }
                GDVars.dsCurrentMorale = 100;
                break;
            }
            case "EffExtraStr":
            {
                var e = N(p);
                if (e.strength >= 0x80) break;
                byte b = 0, x = 0;
                W.StrengthEffectToStrength(e, ref b, ref x);
                c.strCur = U8(Clamp(b));
                c.strExc = x;
                TEffectRec best = null;
                int bb = 0, bx = 0;
                for (var q = c.effectsAddr; q != null; q = q.next)
                {
                    if (q.id != 12 && q.id != 38) continue;
                    if (ReferenceEquals(q, e)) continue;
                    W.StrengthEffectToStrength(q, ref b, ref x);
                    if (b > bb || (bb == 18 && x > bx)) { bb = b; bx = x; best = q; }
                }
                if (best == null) break;
                best.strength = W.StrengthModifier(c.strCur, c.strExc);
                c.strCur = U8(Clamp(bb));
                c.strExc = U8(bx);
                break;
            }
            case "EffFriends": c.chaCur = U8(Clamp(N(p).strength)); break;
            case "EffSlowPoisonHit":
                if (ArmUnlessLocked(c, 15, N(p).strength, 10) && c.hpCur > 1)
                {
                    GDVars.elementMask = 0;
                    W.InflictNonWeaponDamage(c, 1, 0, false);
                    if (GDVars.ifcState != C_Ifc.COMBAT) W.DisplayParty(GDVars.curChar);
                }
                break;
            case "EffShield":
                c.acCur = (byte)Math.Max(57, (int)c.acCur);
                Sv(1);
                if (GDVars.dsCurrentSpellBeing == 15) GDVars.pendingDamage = 0;
                break;
            case "EffGnomeThac0":
                Other = c.combatAddr.other;
                if (Other.charType <= 1 && Other.iconDimensions == 1) Atk(CountIn(Other.name, GDConsts.gnomeFoes, 5));
                break;
            case "EffDwarfThac0":
                Other = c.combatAddr.other;
                if (Other.charType <= 1 && Other.iconDimensions == 1) Atk(CountIn(Other.name, GDConsts.dwarfFoes, 11));
                break;
            case "EffSilence":
            case "EffStinkCough":
            {
                bool cough = h == "EffStinkCough";
                if (c.combatAddr.itemUseAllowed != 0) W.PrintName(c, cough ? "is coughing" : "is silenced", 10, true);
                c.combatAddr.itemUseAllowed = 0;
                c.combatAddr.castAllowed = 0;
                if (!cough) break;
                W.RecalcStats(c);
                c.acBehind = c.acBehind > 52 ? U8(c.acBehind - 2) : (byte)50;
                c.acCur = c.acBehind;
                if (ReferenceEquals(c, GDVars.curChar)) W.CombatInfo(c);
                break;
            }
            case "EffSlowPoisonEnd":
                if (Has(c, 55)) Fall(c, C_Status.DEAD, "dies from poison");
                GDVars.effectLock = 1;
                W.RemoveEffect(c, 15, null);
                GDVars.effectLock = 0;
                break;
            case "EffSpiritHammer":
            {
                TItemRec hammer = null;
                for (var it = c.itemsAddr; it != null; it = it.next)
                    if (it.itemType == C_Item.HAMMER && it.nameIds[3] == 243) { hammer = it; break; }
                if (removed)
                {
                    if (hammer != null) W.RemoveItem(c, hammer);
                }
                else if (hammer == null && c.numberOfItems < 16)
                {
                    var t = new TItemRec();
                    t.itemType = C_Item.HAMMER;
                    t.nameIds[2] = C_Item.NAME_HAMMER;
                    t.nameIds[3] = 243;
                    t.plus = 1;
                    t.effect = 23;
                    t.special = 137;
                    W.AppendItemCopy(ref c.itemsAddr, t);
                    W.PrintName(c, "Gains an item", 10, true);
                }
                W.RecalcStats(c);
                break;
            }
            case "EffInvisible":
                if (!Has(GDVars.curChar, 24)) GDVars.dsCurrentSuppressTargetingFlag = 1;
                Atk(-4);
                break;
            case "EffMirrorImage":
            {
                var e = N(p);
                int roll = Roll(1, (e.strength + 1) & 0xFF);
                if (roll < 2 || GDVars.dsCurrentSpellBeing == 0 || GDVars.gMultiTarget != 0) break;
                Nullify(0);
                W.PrintName(c, "lost an image", 10, true);
                e.strength = U8(e.strength - 1);
                if (e.strength == 0) W.RemoveEffect(c, 28, null);
                break;
            }
            case "EffEnfeeble": GDVars.pendingDamage = U8(GDVars.pendingDamage - (GDVars.pendingDamage >> 2)); break;
            case "EffAnimateDead":
            {
                var e = N(p);
                e.onRemove = false;
                if (GDVars.effectLock == 0) Fall(c, C_Status.DEAD, "collapses");
                c.hostile = S8(e.strength >> 4);
                c.quickfight = 1;
                c.levelUndead = 0;
                c.attackLevel = U8(Warrior(c));
                c.movementBase = 12;
                if (c.npc == 0xB3) c.npc = 0;
                c.charType = 0;
                break;
            }
            case "EffBlind": Atk(-4); c.acCur = U8(c.acCur - 4); c.acBehind = U8(c.acBehind - 4); Sv(-4); break;
            case "EffDiseased": W.CallEffectHandler(43, c, p, removed); W.CallEffectHandler(44, c, p, removed); break;
            case "EffAlliedPrayer": Sv(1); Atk(1); break;
            case "EffBestowCurse": Atk(-4); Sv(-4); break;
            case "EffBlinking": if (c.combatAddr.initiative > 0) { GDVars.dsCurrentSuppressTargetingFlag = 1; GDVars.gAttackRoll = -1; } break;
            case "EffHasted":
            {
                var e = N(p);
                if ((e.strength & 0x10) == 0)
                {
                    e.strength = U8(e.strength | 0x10);
                    W.PrintName(c, "ages", 10, true);
                    c.age = unchecked((ushort)(c.age + 1));
                }
                GDVars.dsCurrentAttackCurrent = U8(GDVars.dsCurrentAttackCurrent << 1);
                break;
            }
            case "EffCastStink": CastStink(c, N(p)); break;
            case "EffProtMissiles":
                if (DamagingWeapon(GDVars.curChar) != null && DamagingWeapon(GDVars.curChar).plus == 0) Avoid(c, 100);
                break;
            case "EffSlowed": GDVars.dsCurrentAttackCurrent = U8(GDVars.dsCurrentAttackCurrent >> 1); break;
            case "EffDiseaseStr":
                if (!ArmUnlessLocked(c, 43, N(p).strength, 60)) break;
                if (c.strCur > 3) { W.PrintName(c, "is weakened", 10, true); c.strCur = U8(c.strCur - 1); }
                else if (!Has(c, 31)) W.AddEffect(c, 31, 0, 255, false);
                break;
            case "EffDiseaseHp":
                if (!ArmUnlessLocked(c, 44, N(p).strength, 10)) break;
                if (c.hpCur > 1)
                {
                    GDVars.elementMask = 0;
                    W.InflictNonWeaponDamage(c, 1, 0, false);
                    if (GDVars.ifcState != C_Ifc.COMBAT) W.DisplayParty(GDVars.curChar);
                }
                else if (!Has(c, 31)) W.AddEffect(c, 31, 0, 255, false);
                break;
            case "EffDwarfAcGiant":
            {
                var a = GDVars.curChar;
                bool typeOk = a.charType == 0 || a.charType == 1 || a.charType == 2 || a.charType == 10;
                int dim = a.iconDimensions & 0x7F;
                if (typeOk && (dim == 1 || dim == 2)) Atk(-4 * CountIn(a.name, GDConsts.giantFoes, 5));
                break;
            }
            case "EffGnomeAcBug":
                if (GDVars.curChar.charType == 1 && (GDVars.curChar.name == "BUGBEAR" || GDVars.curChar.name == "GNOLL")) Atk(-4);
                break;
            case "EffPrayer":
                if ((c.hostile & 0xFF) == ((N(p).strength >> 4) & 1)) { Sv(1); Atk(1); }
                else { Atk(-1); Sv(-1); }
                break;
            case "EffRotNoHeal":
            case "EffConRegen":
            {
                bool rot = h == "EffRotNoHeal";
                int minutes = 14400;
                if (!rot) { minutes = RegenMinutes(c.conCur); if (minutes == 0) minutes = 60; }
                if (ArmUnlessLocked(c, rot ? 50 : 62, N(p).strength, minutes) && W.RestoreHitPoints(c, 1, true)) W.HpRestoreMsg(c);
                break;
            }
            case "EffHelpless": W.EndActions(c); break;
            case "EffRingInvis": W.AddEffect(c, 25, 1, 12, false); break;
            case "EffMummyRot":
            {
                if (GDVars.effectLock != 0) break;
                W.PrintName(c, "Rots...", 10, true);
                c.chaCur = U8(Clamp(c.chaCur - 2));
                var e = N(p);
                if ((e.strength >> 4) >= 2)
                {
                    e.strength = U8(e.strength - 16);
                    ArmUnlessLocked(c, 57, e.strength, 43200);
                }
                else Fall(c, C_Status.GONE, "Dies rots away");
                break;
            }
            case "EffImmobile":
                c.combatAddr.movement = 0;
                if (GDVars.dsCurrentCreatureCalculatingFlag != 0) GDVars.dsCurrentAttackCurrent = 0;
                break;
            case "EffTrollRegen": W.AddEffect(c, 98, 0, 255, false); break;
            case "EffRingFireRes":
            {
                if (!Elem(1)) break;
                int dice = GDVars.gDamageDice;
                int d = GDVars.pendingDamage;
                for (int i = 0; i < dice; i++)
                {
                    if (d >= dice + 2) d -= 2;
                    else if (d > dice) d = dice;
                }
                GDVars.pendingDamage = U8(d);
                Sv(4);
                if (!Elem(8)) Nullify(0);
                break;
            }
            case "EffPoisonHit0": Poison(c, 0); break;
            case "EffPoisonHit4": Poison(c, 4); break;
            case "EffPoisonHit2": Poison(c, 2); break;
            case "EffPoisonHitM2": Poison(c, -2); break;
            case "EffKreenPara": Paralyze(c, Roll(2, 8)); break;
            case "EffGhoulPara": if (c.combatAddr.other.race != C_Race.ELF) Paralyze(c, 63); break;
            case "EffDriderPara": Paralyze(c, Roll(1, 9) + 10); break;
            case "EffDustDisappear": GDVars.dsCurrentSuppressTargetingFlag = 1; Atk(-4); break;
            case "EffCamouflage": if (Roll(1, 100) <= 95) W.AddEffect(c, 25, 1, 12, false); break;
            case "EffBloodDrain":
            {
                bool released = false;
                var held = W.HoldTarget(c, p, removed, 75, ref released);
                if (held == null)
                {
                    if (released && c.enabled != 0 && Other.enabled != 0) c.combatAddr.turned = 1;
                    break;
                }
                W.PrintName(c, "Sucks some Blood", 10, true);
                int dmg = DamageRoll(1, 4);
                GDVars.elementMask = 0;
                W.InflictNonWeaponDamage(Other, U8(dmg), 0, false);
                W.EndActions(c);
                var e = N(p);
                if (Other.enabled != 0 && (e.duration >> 4) > dmg) e.duration = unchecked((ushort)(e.duration - 16 * dmg));
                else W.RemoveEffect(c, 75, p);
                break;
            }
            case "EffBloodAttach":
            case "EffBiteHold":
            {
                bool bite = h == "EffBiteHold";
                Other = c.combatAddr.other;
                if (Other == null) break;
                int slot = W.CombatSlot(Other);
                if (slot == 0) break;
                if (bite && Other.strCur >= 19) break;
                if (bite) W.AddEffect(c, 74, 0, U8(slot), true);
                else W.AddEffect(c, 75, 207, U8(slot), true);
                W.AddEffect(Other, 58, 0, 255, false);
                W.CallEffectHandler(58, Other, null, false);
                if (bite) W.PrintName(Other, "is held fast", 10, true);
                break;
            }
            case "EffHealWake": if (!W.ReviveDeadCreature(c, c.hpCur)) ArmUnlessLocked(c, 78, N(p).strength, 1); break;
            case "EffTrollReturn": if (!W.ReviveDeadCreature(c, c.hpMax)) ArmUnlessLocked(c, 102, N(p).strength, 1); break;
            case "EffFireTouch":
            {
                GDVars.elementMask = 1 | 8;
                var target = c.combatAddr.other;
                W.InflictNonWeaponDamage(target, U8(DamageRoll(2, 10)), 0, false);
                break;
            }
            case "EffAcidSquirt":
            {
                GDVars.elementMask = 0x10;
                var target = c.combatAddr.other;
                W.InflictNonWeaponDamage(target, U8(DamageRoll(1, 4)), 0, false);
                break;
            }
            case "EffDragonFear":
                for (var q = GDVars.creatureList; q != null; q = q.nextCharAddr)
                {
                    if (q.enabled == 0 || EnemyOf(c) != q.hostile) continue;
                    bool saved = W.RollSavingThrow(q, 4, 0);
                    int lv = q.levelHighest;
                    if (lv >= 0 && lv <= 3) W.AddEffectWithMessage(q, 52, 0, 12, false, true, saved, "is paralyzed with fear");
                    else if (lv == 4 || lv == 5) W.AddEffectWithMessage(q, 2, 0, 12, false, true, saved, "is weakened by fear");
                }
                break;
            case "EffMummyFear":
                for (var q = GDVars.creatureList; q != null; q = q.nextCharAddr)
                {
                    W.RemoveEffect(q, 82, null);
                    if (q.enabled == 0 || EnemyOf(c) != q.hostile) continue;
                    int bonus = (GDVars.teamCount[q.hostile] >= 6 ? 1 : 0) + (q.race == C_Race.HUMAN ? 2 : 0);
                    bool saved = W.RollSavingThrow(q, 0, S8(bonus));
                    W.AddEffectWithMessage(q, 52, (ushort)Roll(1, 4), 12, false, true, saved, "is paralyzed with fear");
                }
                break;
            case "EffPetrifyGaze":
            {
                Other = c.combatAddr.other;
                W.PrintName(c, "gazes...", 10, false);
                W.IconFacings(18);
                Glance(c, Other, 4, 45);
                if (Has(c, 127))
                {
                    for (var it = Other.itemsAddr; it != null; it = it.next)
                    {
                        if (it.equipped == 0) continue;
                        if (it.nameIds[1] != 118 && it.nameIds[2] != 118 && it.nameIds[3] != 118) continue;
                        W.PrintName(Other, "reflects it!", 12, false);
                        Glance(Other, c, 4, 45);
                        Other = c;
                        break;
                    }
                }
                if (!W.RollSavingThrow(Other, 1, 0)) Fall(Other, C_Status.STONED, "is Stoned");
                break;
            }
            case "EffCharmGaze":
            {
                Other = c.combatAddr.other;
                sbyte tx = W.CombatCellX(Other), ty = W.CombatCellY(Other);
                if (!W.CanAttack(c, Other)) break;
                ushort range = GDConsts.spellTable[10].rangeFixed;
                sbyte fx = W.CombatCellX(c), fy = W.CombatCellY(c);
                if (!W.WalkLine(fx, fy, ref tx, ref ty, ref range, GDVars.arenaPtr)) break;
                W.PrintName(c, "Gazes...", 10, false);
                W.IconFacings(18);
                Glance(c, Other, 4, 45);
                byte keep = GDVars.dsCurrentSpellBeing;
                GDVars.dsCurrentSpellBeing = 10;
                var victim = Other;
                byte str = U8(128 * c.hostile + 12);
                bool saved = W.RollSavingThrow(c.combatAddr.other, 4, -2);
                W.AddEffectWithMessage(victim, 11, 0, str, true, true, saved, "is charmed");
                TEffectRec found = null;
                for (var e = Other.effectsAddr; e != null; e = e.next) if (e.id == 11) { found = e; break; }
                if (found != null) W.CallEffectHandler(11, Other, found, false);
                GDVars.dsCurrentSpellBeing = keep;
                break;
            }
            case "EffDrainOne": Drain(c.combatAddr.other, 1); break;
            case "EffDrainTwo": Drain(c.combatAddr.other, 2); break;
            case "EffRotTouch":
            {
                Other = c.combatAddr.other;
                if (W.DiseaseImmune(Other, 50)) break;
                if (Has(Other, 50)) break;
                W.AddEffect(Other, 50, 14400, 255, true);
                var target = Other;
                int seedCount = Roll(1, 6);
                W.AddEffect(target, 57, 43200, U8(16 * seedCount + 15), true);
                W.PrintName(Other, "is Diseased", 10, true);
                break;
            }
            case "EffDisplaced":
            {
                // ready-item path passes nil; the port documents that stray nil^ store as a no-op (campaign crash 2026-10-29)
                if (p == null) break;
                var e = N(p);
                if (GDVars.roundCounter == 0 && GDVars.gAttackRoll == 0) e.strength = U8(e.strength & 15);
                else if ((e.strength & 16) == 0) { GDVars.gAttackRoll = -1; e.strength = U8(e.strength + 16); }
                break;
            }
            case "EffConPsnSave": if (GDVars.gSaveType == 0) Sv(ConSave(c.conCur)); break;
            case "EffConSpellSave": if (GDVars.gSaveType == 4 || GDVars.gSaveType == 2) Sv(ConSave(c.conCur)); break;
            case "EffImmuneMM":
                if (GDVars.dsCurrentSpellBeing == 15) { Nullify(0); W.PrintName(c, "is unaffected", 10, true); }
                else if (Elem(4)) Nullify(0);
                break;
            case "EffHalfFire": if (Elem(1)) Halve(); break;
            case "EffHalfElec": if (Elem(4)) Halve(); break;
            case "EffHalfCold": if (Elem(2)) Halve(); break;
            case "EffHalfBash":
            {
                var w = DamagingWeapon(GDVars.curChar);
                if (w != null && (CharHlp.BaseItem(w.itemType).wpnClass & 0x81) != 0) Halve();
                break;
            }
            case "EffHalfPierce":
            {
                var w = DamagingWeapon(GDVars.curChar);
                if (w == null) break;
                int k = CharHlp.BaseItem(w.itemType).wpnClass;
                if (k == 0 || (k & 1) == 1) Halve();
                break;
            }
            case "EffHalfMagical": { var w = DamagingWeapon(GDVars.curChar); if (w != null && w.plus >= 1) Halve(); break; }
            case "EffFightOn": N(p).onRemove = false; if (c.enabled != 0) Fall(c, C_Status.DEAD, "Falls dead"); break;
            case "EffNeedSilver":
            {
                var w = DamagingWeapon(GDVars.curChar);
                bool hurts = w != null && (w.plus != 0 || w.nameIds[3] == 177);
                if (!hurts) GDVars.pendingDamage = 0;
                break;
            }
            case "EffRegen3": c.hpCur = (c.hpCur + 3 > c.hpMax) ? c.hpMax : U8(c.hpCur + 3); break;
            case "EffCanFightOn":
            {
                int hp = 0;
                if (c.status == C_Status.DYING && c.combatAddr.overkill < 6) hp = 6 - c.combatAddr.overkill;
                if (c.status == C_Status.UNCONSCIOUS) hp = 6;
                if (hp <= 0 || !W.ReviveDeadCreature(c, U8(hp))) break;
                W.AddEffect(c, 95, (ushort)(Roll(1, 4) + 1), 255, true);
                N(p).onRemove = false;
                W.RemoveEffect(c, 99, p);
                break;
            }
            case "EffTrollBurn": if (!Elem(1) && !Elem(0x10)) W.AddEffect(c, 102, (ushort)Roll(3, 6), 255, true); break;
            case "EffTrollHeal": if (!Has(c, 98) && !Has(c, 59)) W.AddEffect(c, 59, 3, 255, true); break;
            case "EffGaseous":
                N(p).onRemove = false;
                W.DisplayMapViewCentered(c, 3, false);
                W.PutOutOfPlayWithStatus(c, C_Status.RUNNING, "Turns gaseous and escapes");
                break;
            case "EffKreenDodge": Avoid(c, 60); break;
            case "EffMagicRes50": Resistance(50); break;
            case "EffMagicRes85": Resistance(85); break;
            case "EffElfResist": if (Roll(1, 100) <= 90) { Nullify(53); Nullify(11); } break;
            case "EffHalfElfResist": if (Roll(1, 100) <= 30) { Nullify(11); Nullify(53); } break;
            case "EffNoSleepCharm": Nullify(11); Nullify(53); break;
            case "EffNoParalysis": Nullify(52); break;
            case "EffNoCold": if (Elem(2)) Nullify(0); break;
            case "EffNoFire": if (Elem(1)) Nullify(0); break;
            case "EffNoParaPoison": Nullify(55); Nullify(52); if (GDVars.gSaveType == 0) GDVars.gSaveRoll = 100; break;
            case "EffNoCharmPoison": Nullify(11); Nullify(53); Nullify(52); Nullify(55); if (GDVars.gSaveType == 0) GDVars.gSaveRoll = 100; break;
            case "EffEfreetiFire":
                if (Elem(1))
                {
                    // gDamageDice passes, each taking one off while above the floor: down to the floor at most
                    int d = GDVars.pendingDamage, floor = GDVars.gDamageDice;
                    if (d > floor) d = Math.Max(floor, d - floor);
                    GDVars.pendingDamage = U8(d);
                }
                break;
            case "EffHolyWater":
            {
                var it = GDVars.curChar.equipped[0];
                if (it != null && it.itemType == 85) GDVars.pendingDamage = U8(DamageRoll(1, 6) + 1);
                break;
            }
            case "EffNeedMagic":
            {
                var w = DamagingWeapon(GDVars.curChar);
                if ((w == null || w.plus == 0) && (GDVars.curChar.race > 0 || GDVars.curChar.levelHighest < 4)) GDVars.pendingDamage = 0;
                break;
            }
            case "EffBoulderDodge":
            {
                var it = GDVars.curChar.equipped[0];
                if (it != null && (it.itemType == 87 || it.itemType == 88)) Avoid(c, 50);
                break;
            }
            case "EffAnhkhegAcid":
            {
                Other = c.combatAddr.other;
                if (Other == null) break;
                if (Other.enabled == 0) break;
                if (Roll(1, 100) > 25) break;
                if (W.CalcDistance(c, Other) > 3) break;
                W.EndActions(c);
                W.PrintName(c, "Spits Acid", 10, true);
                W.IconFacings(23);
                Glance(c, Other, 1, 30);
                var target = Other;
                int dmg = DamageRoll(8, 4);
                bool saved = W.RollSavingThrow(Other, 3, 0);
                W.InflictNonWeaponDamage(target, U8(dmg), 2, saved);
                W.RemoveEffect(c, 121, p);
                W.RemoveEffect(c, 80, null);
                break;
            }
            case "EffMummyFire":
            {
                var it = GDVars.curChar.equipped[0];
                if (it != null && it.itemType == 86) GDVars.pendingDamage = U8(DamageRoll(3, 8));
                if (Elem(1 | 8)) GDVars.pendingDamage = U8(GDVars.pendingDamage + GDVars.gDamageDice);
                break;
            }
            case "EffSilverMagic":
            {
                var w = DamagingWeapon(GDVars.curChar);
                if (w == null) GDVars.pendingDamage = 0;
                else if (w.plus == 0) { if (w.nameIds[3] == 177) Halve(); else GDVars.pendingDamage = 0; }
                break;
            }
            case "EffGazeBlock":
            {
                Other = c.combatAddr.other;
                for (var it = Other.itemsAddr; it != null && GDVars.dsCurrentSuppressTargetingFlag == 0; it = it.next)
                {
                    if (it.equipped == 0) continue;
                    for (int j = 1; j <= 3; j++)
                        if (it.nameIds[j] == 118 || (it.nameIds[j] == 152 && it.nameIds[3] == 252)) GDVars.dsCurrentSuppressTargetingFlag = 1;
                }
                break;
            }
            case "EffReadyItem":
            {
                var it = (TItemRec)p;
                if (removed) W.RemoveEffect(c, it.effect, null);
                else { W.AddEffect(c, it.effect, 0, 12, false); W.CallEffectHandler(it.effect, c, null, false); }
                break;
            }
            case "EffGauntlets":
            {
                if (removed)
                {
                    if (c.strCur != 18 || c.strExc != 100) break;
                    for (var q = c.effectsAddr; q != null; q = q.next)
                        if (q.id == 38 && q.strength < 128) { W.RemoveEffect(c, 38, q); break; }
                    break;
                }
                byte enc = 0;
                if (W.AdjustForNewStrength(c, 18, 100, ref enc))
                {
                    byte ifc = GDVars.ifcState;
                    if (ifc == C_Ifc.COMBAT) GDVars.ifcState = C_Ifc.CAMPING;
                    W.PrintName(c, "is stronger", 10, true);
                    GDVars.ifcState = ifc;
                }
                GDVars.dsRedrawInventoryMenuFlag = 1;
                W.AddEffect(c, 38, 0, enc, true);
                break;
            }
            case "EffAlignedWeapon":
            {
                var it = (TItemRec)p;
                if (removed || (it.effect & 15) == c.alignment) break;
                it.equipped = 0;
                GDVars.elementMask = 8;
                if (GDVars.ifcState == C_Ifc.COMBAT) W.CombatInterface();
                W.InflictItemShock(c, it, U8(it.effect >> 4));
                GDVars.dsRedrawInventoryMenuFlag = 1;
                break;
            }
            case "EffBoulder":
            {
                var it = (TItemRec)p;
                if (removed || c.strCur >= 19) break;
                it.equipped = 0;
                W.BottomLine("Must have Giant Strength");
                break;
            }
            case "EffHammerGone": if (removed) W.RemoveEffect(c, 23, null); break;
            case "EffNull": break;
            default: throw new Exception("oracle has no " + h);
        }
    }

    void Glance(TCharRecord from, TCharRecord to, byte frames, byte pace)
    {
        sbyte a = W.CombatCellX(from), b = W.CombatCellY(from), x = W.CombatCellX(to), y = W.CombatCellY(to);
        W.Animate(a, b, x, y, frames, pace);
    }

    void CastStink(TCharRecord c, TEffectRec e)
    {
        int slot = e.strength >> 4;
        TStinkRec node = null;
        for (var s = GDVars.stinkHead; s != null; s = s.next)
            if (ReferenceEquals(s.caster, c) && s.idx == slot) { node = s; break; }
        if (node == null) return;
        W.BottomLine("The air clears a little...");
        for (int i = 1; i <= 4; i++)
        {
            if (node.exists[i] == 0) continue;
            int d = GDConsts.cloudDir[i];
            int xx = (node.ulX + GDConsts.dirDX[d]) & 0xFF, yy = (node.ulY + GDConsts.dirDY[d]) & 0xFF;
            bool corpse = false;
            for (int k = 1; k <= GDVars.downedCount; k++)
            {
                var dn = GDVars.downed[k];
                if (dn.cr != null && dn.x == xx && dn.y == yy) corpse = true;
            }
            GDVars.arenaPtr.cells[yy, xx] = corpse ? (byte)31 : node.under[i];
        }
        if (ReferenceEquals(GDVars.stinkHead, node)) GDVars.stinkHead = node.next;
        else
        {
            var prev = GDVars.stinkHead;
            while (!ReferenceEquals(prev.next, node)) prev = prev.next;
            prev.next = node.next;
        }
        for (var s = GDVars.stinkHead; s != null; s = s.next)
            for (int i = 1; i <= 4; i++)
                if (s.exists[i] != 0)
                {
                    int d = GDConsts.cloudDir[i];
                    GDVars.arenaPtr.cells[s.ulY + GDConsts.dirDY[d], s.ulX + GDConsts.dirDX[d]] = 30;
                }
    }
}
