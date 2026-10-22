// Source: Talonius pool-of-radiance pinned 7616920a (src/cbtacts.pas). Direct conversion by Claude (supervising session), on
// James's request without Qwen; no Qwen draft exists for this unit.
//
// Boundaries: CbtMap is bound by CbtMapBindings; FxEngine and Magic are not ported. Their routines are reached only through the three interfaces below
// (Map, Fx, Mag), and each throws NotSupportedException when no implementation is installed. Calls into converted units
// (CharDisp, TextRend, MenuCore, Border, SoundDrv, CharHlp's side-effecting routines, Pathing's arena walkers, AbilAdj)
// go through internal delegate fields that default to the converted routine, so tests can observe them (the CharDisp
// pattern). Pure predicates (CharHlp.IsHelpless, HasEffect, HasRangedWeapon, ..., Pathing.IsDirToward) and Dice are called
// directly.
using System;
using static Por.Core.Types;

namespace Por.Core
{
    /// <summary>The cbtmap.pas routines cbtacts.pas calls. Extends the CharDisp combat-map seam.</summary>
    public interface ICbtActsCombatMap : ICombatMapHost
    {
        void RedrawArenaCell(sbyte x, sbyte y);
        void BuildArenaArrays();
        void GetArenaContents(sbyte x, sbyte y, ref byte crIdx, ref byte terrIdx);
        void UpdateCombatMapTile(sbyte x, sbyte y, byte crIdx);
        void SetCreatureFacing(TCharRecord creature, byte dir, byte frame, byte eraseOnly);
        byte GetIconSize(TCharRecord creature);
        void CreatureDowned(TCharRecord creature);
        void DisplayMapViewCentered(TCharRecord creature, byte delta, bool cursor);
        void StartSpellPreview(TAimShape shape, byte radius, byte areaCell);
        void StopSpellPreview();
        byte CountTargets(TCharRecord creature, byte range);
        byte CalcDistance(TCharRecord creature, TCharRecord other);
    }

    /// <summary>The fxengine.pas routines cbtacts.pas calls.</summary>
    public interface ICbtActsEffects
    {
        void TriggerEffectsWithCondition(byte cond, TCharRecord creature);
        bool AttackRoll(TCharRecord attacker, TCharRecord defender, byte sixtyMinusAc);
        void PutOutOfPlayWithStatus(TCharRecord creature, byte status, string msg);
        void RemoveInvisibility(TCharRecord creature);
        void RemoveEffectsHp0(TCharRecord creature);
        void RemoveEffect(TCharRecord creature, byte effId, object eff);
        TCharRecord HoldTarget(TCharRecord creature, object p, bool removed, byte effId, ref bool released);
    }

    /// <summary>The magic.pas routines cbtacts.pas calls.</summary>
    public interface ICbtActsMagic
    {
        byte CastSpellMenu(ref int idx, ref bool notEmpty, ref TItemRec scroll);
        void UseItemSpell(TItemRec item, byte spell, bool announce, ref bool done);
        void CastSpellNC(byte spell, bool b, bool ai, ref bool sel);
    }

    public static class CbtActs
    {
        // ---- unported-unit seams ----
        public static ICbtActsCombatMap CombatMap;
        public static ICbtActsEffects Effects;
        public static ICbtActsMagic MagicUnit;

        private static ICbtActsCombatMap Map
        {
            get { if (CombatMap == null) throw new NotSupportedException("CbtActs: CbtMap is not bound; call CbtMapBindings.Bind."); return CombatMap; }
        }
        private static ICbtActsEffects Fx
        {
            get { if (Effects == null) throw new NotSupportedException("CbtActs: FxEngine is not ported; install CbtActs.Effects."); return Effects; }
        }
        private static ICbtActsMagic Mag
        {
            get { if (MagicUnit == null) throw new NotSupportedException("CbtActs: Magic is not ported; install CbtActs.MagicUnit."); return MagicUnit; }
        }

        // ---- converted-unit call points (default to the converted routine) ----
        internal delegate byte MenuHorizontalProc(string query, string menu, byte qCol, byte mCol, byte hlCol, bool reinterpret, bool animate, ref bool ext);
        internal delegate bool WalkLineProc(sbyte x, sbyte y, ref sbyte tx, ref sbyte ty, ref ushort dist, TArena view);
        internal delegate void PrependItemProc(ref TItemRec list, TItemRec item);

        internal static Action<TCharRecord, string, byte, bool> PrintNameStringCall = CharDisp.PrintNameString;
        internal static Action<TCharRecord, byte, byte, bool> DisplayCreatureNameCall = CharDisp.DisplayCreatureName;
        internal static Action ClearMessageWindowCall = CharDisp.ClearMessageWindow;
        internal static Action<TCharRecord> CombatPrintCreatureInfoCall = CharDisp.CombatPrintCreatureInfo;
        internal static Action<TCharRecord, bool, string> PrintNameStringAnimCall = CharDisp.PrintNameStringAnim;
        internal static Action<string> PrintBottomLineCall = CharDisp.PrintBottomLine;
        internal static Action DisplayCombatInterfaceCall = CharDisp.DisplayCombatInterface;
        internal static Action<byte, byte, byte, bool> CopyIconFacingCall = CharDisp.CopyIconFacing;
        internal static Action<byte> BuildIconFacingsCall = CharDisp.BuildIconFacings;
        internal static Action<sbyte, sbyte, sbyte, sbyte, byte, byte> CombatAnimateMovingIconCall = CharDisp.CombatAnimateMovingIcon;
        internal static Action<byte, byte, byte, byte, byte, bool, string> DisplayMessageWindowCall = TextRend.DisplayMessageWindow;
        internal static Action PauseOnlyCall = TextRend.PauseOnly;
        internal static Action<byte, byte, byte, string> DisplayStringCall = TextRend.DisplayString;
        internal static Action ClearBottomLineCall = TextRend.ClearBottomLine;
        internal static MenuHorizontalProc MenuHorizontalCall = MenuCore.MenuHorizontal;
        internal static Func<string, byte, byte, byte, byte> YesNoMenuCall = MenuCore.YesNoMenu;
        internal static Action<sbyte, sbyte, sbyte, sbyte> ClearBoxCall = Border.ClearBox;
        internal static Action<ushort> SoundCommandCall = SoundDrv.SoundCommand;
        internal static Action<ushort> SoundSetAutoStopCall = SoundDrv.SoundSetAutoStop;
        internal static Action<TCharRecord, byte> InflictDamageCall = CharHlp.InflictDamage;
        internal static Action<TCharRecord, byte> RemoveSpellCall = CharHlp.RemoveSpell;
        internal static Action<TCharRecord> RecalcStatsCall = CharHlp.RecalcStats;
        internal static Action CountTeamsCall = CharHlp.CountTeams;
        internal static PrependItemProc PrependItemCopyCall = CharHlp.PrependItemCopy;
        internal static Action<TCharRecord, TItemRec> RemoveItemCall = CharHlp.RemoveItem;
        internal static WalkLineProc WalkLineCall = Pathing.WalkLine;
        internal static Action<sbyte, sbyte, ushort, byte, byte, TArena> BuildTargetListCall = Pathing.BuildTargetList;
        internal static Action<sbyte, sbyte, byte> TargetAreaCall = Pathing.TargetArea;
        internal static Func<TCharRecord, sbyte> DexMissileAdjustCall = AbilAdj.DexMissileAdjust;

        // Crt.Delay
        internal static void Delay(ushort ms)
        {
            if (TextRend.DelayMilliseconds == null) throw new NotSupportedException("CbtActs: delay host missing.");
            TextRend.DelayMilliseconds(ms);
        }

        // Primary and secondary attack counts.
        internal static byte[] attacksMade = new byte[3]; // 1..2

        // Combat sprite cells the spell-aim preview draws with.
        internal const byte SPRITE_MISSILE = 18;
        internal const byte SPRITE_LIGHTNING = 19;
        internal const byte SPRITE_SPARKLE = 22;
        internal const byte SPRITE_BURST = 23;

        private const byte LightGreen = 10;
        private const byte LightMagenta = 13;
        private const byte White = 15;

        private static string Str(string s, int n) { return s.Length > n ? s.Substring(0, n) : s; }

        // TAttackPhaseView: acCurArr[0..1] overlays acCur/acBehind, curAtt[1..2] overlays curAttacks/curAttacks2.
        internal static byte CurAtt(TCharRecord p, int k)
        {
            if (k == 1) return p.curAttacks;
            if (k == 2) return p.curAttacks2;
            throw new IndexOutOfRangeException("curAtt index " + k);
        }
        internal static void SetCurAtt(TCharRecord p, int k, byte v)
        {
            if (k == 1) p.curAttacks = v;
            else if (k == 2) p.curAttacks2 = v;
            else throw new IndexOutOfRangeException("curAtt index " + k);
        }
        internal static byte AcCurArr(TCharRecord p, int k)
        {
            if (k == 0) return p.acCur;
            if (k == 1) return p.acBehind;
            throw new IndexOutOfRangeException("acCurArr index " + k);
        }

        // FillChar(tgt, SizeOf(TAimTarget), 0)
        private static void ClearAim(TAimTarget t) { t.cr = null; t.x = 0; t.y = 0; t.pad = 0; }

        private static bool IsDeadStatus(byte s) { return s == C_Status.DEAD || s == C_Status.STONED || s == C_Status.GONE; }

        public static void CombatRoundStartSetup(TCharRecord creature)
        {
            TCombatRec c = creature.combatAddr;
            c.spellCast = 0;
            c.castAllowed = 1;
            c.itemUseAllowed = 1;
            c.madeAttack = 0;
            c.attackPhase = 2;
            SetCurAttacksX2(creature);
            GDVars.dsCurrentAttackCurrent = creature.attacks2;
            GDVars.dsCurrentCreatureCalculatingFlag = 0;
            Fx.TriggerEffectsWithCondition(C_Cond.MOVE_ATTACK_RATE, creature);
            creature.curAttacks2 = AdjustedAttacks(GDVars.dsCurrentAttackCurrent);
            c.sweepBudget = creature.attackLevel;
            if (creature.enabled != 0)
            {
                c.initiative = unchecked((sbyte)(Dice.RollDice(1, 6) + DexMissileAdjustCall(creature)));
                if (c.initiative < 1)
                    c.initiative = 1;
                // Surprise mask: bit 0 for the party, bit 1 for monsters
                if (unchecked((byte)((creature.hostile + 1) & (short)GDVars.zone1[C_Gbvm.SURPRISE])) != 0)
                    c.initiative = unchecked((sbyte)(c.initiative - 6));
                if (c.initiative < 0 || c.initiative > 20)
                    c.initiative = 0;
            }
            else
                c.initiative = 0;
            creature.combatAddr.movement = CalcCreatureMovement(creature);
        }

        public static byte CalcCreatureMovement(TCharRecord creature)
        {
            byte m = creature.movementCur;
            if (creature.hostile == 0)
                m = unchecked((byte)(m + GDVars.zone1[C_Gbvm.MOD_PARTY_MOVE]));
            GDVars.dsCurrentCreatureCalculatingFlag = 1;
            if (m < 1 || m > 96)
                m = 1;
            GDVars.dsCurrentAttackCurrent = unchecked((byte)(m * 2));
            Fx.TriggerEffectsWithCondition(C_Cond.MOVE_ATTACK_RATE, creature);
            GDVars.dsCurrentCreatureCalculatingFlag = 0;
            return GDVars.dsCurrentAttackCurrent;
        }

        internal static void CalcWeaponDamage(TCharRecord att, TCharRecord def, byte idx)
        {
            int dmg = att.curModA[idx] + Dice.RollDamageDice(att.curRollsA[idx], att.curDiceA[idx]);
            if (dmg < 0)
                dmg = 0;
            if (dmg > 255)
                dmg = 255;
            GDVars.pendingDamage = (byte)dmg;
            if (IsBackstab(att, def))
                GDVars.pendingDamage = unchecked((byte)(GDVars.pendingDamage * ((unchecked((ushort)att.classLevels[C_Level.THIEF]) + 3) / 4 + 1)));
            if (att.classLevels[C_Level.RANGER] > 0 && Map.CalcDistance(att, def) <= 1)
                for (int i = 1; i <= GDConsts.rangerFoes.Length - 1; i++)
                    if (def.name == GDConsts.rangerFoes[i])
                    {
                        if (GDVars.pendingDamage > 255 - att.classLevels[C_Level.RANGER])
                            GDVars.pendingDamage = 255;
                        else
                            GDVars.pendingDamage = unchecked((byte)(GDVars.pendingDamage + att.classLevels[C_Level.RANGER]));
                    }
            GDVars.elementMask = 0;
            Fx.TriggerEffectsWithCondition(C_Cond.WEAPON_DAMAGE_ATT, att);
            Fx.TriggerEffectsWithCondition(C_Cond.WEAPON_DAMAGE_DEF, def);
        }

        internal static void ProcessAttackResult(TCharRecord att, TCharRecord def, byte atkType, byte dmg, byte dmgDisp, bool isHit)
        {
            byte row;
            string verb;
            switch (atkType)
            {
                case 2: verb = "-Backstabs-"; break;
                case 3: verb = "slays helpless"; break;
                default: verb = "Attacks"; break;
            }
            PrintNameStringCall(att, verb, 10, false);
            row = 12;
            DisplayCreatureNameCall(def, 23, row, false);
            row = unchecked((byte)(row + 1));
            if (atkType == 1)
                verb = "(from behind) ";
            else
                verb = "";
            if (isHit)
            {
                if (atkType == 3)
                    verb = "with one cruel blow";
                else
                {
                    verb = Str(verb + "Hitting for " + StrUtil.NumStr(dmgDisp), 80);
                    if (dmgDisp == 1)
                        verb = Str(verb + " point ", 80);
                    else
                        verb = Str(verb + " points ", 80);
                    verb = Str(verb + "of damage", 80);
                }
                InflictDamageCall(def, dmg);
            }
            else
                verb = Str(verb + "and Misses", 80);
            DisplayMessageWindowCall(23, row, 38, unchecked((byte)(row + 3)), LightGreen, true, verb);
            row = unchecked((byte)(GDVars.curRow + 1));
            if (dmg > 0)
            {
                def.combatAddr.castAllowed = 0;
                if (def.combatAddr.spellCast > 0)
                {
                    PauseOnlyCall();
                    PrintNameStringCall(def, "lost a spell", 12, true);
                    RemoveSpellCall(def, def.combatAddr.spellCast);
                    def.combatAddr.spellCast = 0;
                }
                else
                    PauseOnlyCall();
            }
            else
                PauseOnlyCall();
            if (def.enabled == 0)
            {
                PrintNameStringCall(def, "goes down", row, false);
                row = unchecked((byte)(row + 2));
                if (def.status == C_Status.DYING)
                    DisplayStringCall(23, row, LightGreen, "and is Dying");
                if (IsDeadStatus(def.status))
                    PrintNameStringCall(def, "is killed", row, false);
                // dead store: row is never read again
                row = unchecked((byte)(row + 2));
                Fx.RemoveEffectsHp0(def);
                Fx.TriggerEffectsWithCondition(C_Cond.HP_ZERO, def);
                if (def.enabled == 0)
                    Map.CreatureDowned(def);
                else
                    PauseOnlyCall();
            }
            ClearMessageWindowCall();
        }

        internal static void GuardingAttacks(TCharRecord creature)
        {
            bool done = false;
            byte cnt = Map.CountTargets(creature, 1);
            for (int i = 1; i <= cnt; i++)
                if (creature.enabled != 0)
                {
                    TCharRecord p = GDVars.creatureArr[GDVars.dsPotentialTargetCreature[i]];
                    if (p.combatAddr.guarding != 0 && !CharHlp.IsHelpless(p))
                    {
                        Map.ScrollView(Map.CombatCellX(creature), Map.CombatCellY(creature), 2, C_Dir.CENTER);
                        p.combatAddr.guarding = 0;
                        NoteAttackDirection(p, creature);
                        CombatAttack(p, creature, false, null, ref done);
                    }
                }
        }

        public static void CombatMoveCreature(TCharRecord creature, byte dir)
        {
            byte cost;
            if ((dir & 1) != 0)
                cost = 3;
            else
                cost = 2;
            if (cost > creature.combatAddr.movement)
            {
                creature.combatAddr.movement = 0;
                return;
            }
            creature.combatAddr.movement = unchecked((byte)(creature.combatAddr.movement - cost));
            byte slot = Map.CombatSlot(creature);
            if (slot == 0)
            {
                // not on the map: nothing to move
                creature.combatAddr.movement = 0;
                return;
            }
            sbyte x = GDVars.arenaLoc[slot].x;
            sbyte y = GDVars.arenaLoc[slot].y;
            sbyte nx = unchecked((sbyte)(x + GDConsts.dirDX[dir]));
            sbyte ny = unchecked((sbyte)(y + GDConsts.dirDY[dir]));
            // cost is reused as the ScrollView delta: 1 under manual control, 3 under quickfight
            cost = 1;
            if (creature.quickfight != 0)
            {
                cost = 3;
                if (!Map.ViewCoordsValid(unchecked((sbyte)(nx - GDVars.arenaPtr.ox)), unchecked((sbyte)(ny - GDVars.arenaPtr.oy))))
                    Map.ScrollView(x, y, 2, C_Dir.CENTER);
            }
            Map.UpdateCombatMapTile(0, 0, slot);
            GDVars.arenaLoc[slot].x = nx;
            GDVars.arenaLoc[slot].y = ny;
            Map.BuildArenaArrays();
            Map.ScrollView(nx, ny, cost, C_Dir.CENTER);
            creature.combatAddr.flankCount = 0;
            creature.combatAddr.flankSpread = 0;
            SoundCommandCall(GDConsts.soundIds.combat[C_Sound.STEP]);
            GuardingAttacks(creature);
            if (creature.enabled == 0 || CharHlp.IsHelpless(creature))
                creature.combatAddr.movement = 0;
        }

        public static void MoveOpportunityAttacks(TCharRecord creature, byte dir)
        {
            byte[] adj = new byte[13]; // 1..12
            TEffectRec effect;
            byte slot = Map.CombatSlot(creature);
            if (slot == 0)
                return;
            byte cntBefore = Map.CountTargets(creature, 1);
            if (cntBefore == 0)
                return;
            for (int i = 1; i <= cntBefore; i++)
                adj[i] = GDVars.dsPotentialTargetCreature[i];
            // probe: step the icon, recount, step back
            GDVars.arenaLoc[slot].x = unchecked((sbyte)(GDVars.arenaLoc[slot].x + GDConsts.dirDX[dir]));
            GDVars.arenaLoc[slot].y = unchecked((sbyte)(GDVars.arenaLoc[slot].y + GDConsts.dirDY[dir]));
            byte cntAfter = Map.CountTargets(creature, 1);
            GDVars.arenaLoc[slot].x = unchecked((sbyte)(GDVars.arenaLoc[slot].x - GDConsts.dirDX[dir]));
            GDVars.arenaLoc[slot].y = unchecked((sbyte)(GDVars.arenaLoc[slot].y - GDConsts.dirDY[dir]));
            for (int i = 1; i <= cntBefore; i++)
            {
                bool still = false;
                for (int j = 1; j <= cntAfter; j++)
                    if (GDVars.dsPotentialTargetCreature[j] == adj[i])
                        still = true;
                if (still)
                    adj[i] = 0;
            }
            for (int i = 1; i <= cntBefore; i++)
                if (adj[i] > 0)
                    if (creature.enabled != 0)
                    {
                        bool attacked = false;
                        TCharRecord p = GDVars.creatureArr[adj[i]];
                        if (!CharHlp.IsHelpless(p))
                            if (CanAttackTarget(p, creature))
                                if (!CharHlp.HasEffect(p, C_Effect.BLOOD_DRAIN, out effect))
                                    if (!CharHlp.HasEffect(p, C_Effect.BITE_HOLD, out effect))
                                    {
                                        int aLo = p.combatAddr.direction + 6, aHi = p.combatAddr.direction + 10;
                                        for (int a = aLo; a <= aHi; a++)
                                            if (!attacked)
                                                if (p.combatAddr.initiative > 0
                                                    || p.combatAddr.flankCount == 0
                                                    || Pathing.IsDirToward(Map.CombatCellX(p), Map.CombatCellY(p), Map.CombatCellX(creature),
                                                                           Map.CombatCellY(creature), unchecked((byte)(a % 8))))
                                                {
                                                    byte phase = 1;
                                                    if (p.attacks == 0)
                                                        phase = 2;
                                                    for (int k = 1; k <= 2; k++)
                                                        if (CurAtt(p, k) > 0)
                                                            phase = (byte)k;
                                                    if (CurAtt(p, phase) == 0)
                                                        SetCurAtt(p, phase, 1);
                                                    p.combatAddr.attackPhase = phase;
                                                    TCharRecord savedOther = p.combatAddr.other;
                                                    CombatAttack(p, creature, true, null, ref GDVars.doneRoundFlag);
                                                    attacked = true;
                                                    p.combatAddr.other = savedOther;
                                                    if (creature.enabled != 0)
                                                        CombatPrintCreatureInfoCall(creature);
                                                }
                                    }
                    }
        }

        public static bool CombatFleeAttempt(TCharRecord creature)
        {
            bool ok = false;
            if (Map.CountTargets(creature, 255) == 0)
                ok = true;
            else
            {
                byte halfMove = unchecked((byte)(CalcCreatureMovement(creature) / 2));
                byte enemyMove = HighestEnemyMovement(creature);
                if (enemyMove < halfMove)
                    ok = true;
                else if (enemyMove == halfMove)
                    if (Dice.RollDice(1, 2) == 1)
                        ok = true;
            }
            if (ok)
                Fx.PutOutOfPlayWithStatus(creature, C_Status.RUNNING, "Got Away");
            else
                PrintBottomLineCall("Escape is blocked");
            return CharHlp.EndActions(creature);
        }

        public static void SetCurAttacksX2(TCharRecord creature)
        {
            bool ranged = false;
            byte saved = creature.curAttacks;
            TItemRec weapon = null;
            if (CharHlp.HasRangedWeapon(creature) && CharHlp.FindDamagingRangedWeapon(creature, ref weapon))
            {
                ranged = true;
                byte rof = CharHlp.BaseItem(creature.equipped[C_Equip.WEAPON].itemType).rateOfFire;
                if (rof < 2)
                    rof = 2;
                GDVars.dsCurrentAttackCurrent = rof;
            }
            else
                GDVars.dsCurrentAttackCurrent = creature.attacks;
            GDVars.dsCurrentCreatureCalculatingFlag = 0;
            Fx.TriggerEffectsWithCondition(C_Cond.MOVE_ATTACK_RATE, creature);
            byte adj = AdjustedAttacks(GDVars.dsCurrentAttackCurrent);
            if (ranged)
                if (weapon != null)
                {
                    byte qty = 1;
                    if (weapon != null)
                        if (weapon.quantity > qty)
                            qty = weapon.quantity;
                    if (qty < adj)
                        adj = qty;
                }
            if (creature.combatAddr.madeAttack == 0 || adj < saved
                || (creature.combatAddr.madeAttack != 0 && adj < saved * 2 && !ranged))
                creature.curAttacks = adj;
        }

        internal static byte AdjustedAttacks(byte n)
        {
            byte v = n;
            if ((GDVars.roundCounter & 1) != 0)
                v = unchecked((byte)(v + 1));
            return (byte)(v / 2);
        }

        public static bool SweepAttempt(TCharRecord att, TCharRecord def)
        {
            if (att.curAttacks >= att.combatAddr.sweepBudget)
                return false;
            if (def.levelHighest != 0)
                return false;
            if (Map.CalcDistance(att, def) != 1)
                return false;
            byte cnt = Map.CountTargets(att, 1);
            byte weak = 0;
            byte defIdx = 0;
            TCharRecord p;
            for (int i = 1; i <= cnt; i++)
            {
                p = GDVars.creatureArr[GDVars.dsPotentialTargetCreature[i]];
                if (p == def)
                    defIdx = (byte)i;
                if (p.levelHighest == 0)
                    weak = unchecked((byte)(weak + 1));
            }
            if (defIdx == 0)
                return false;
            if (weak <= att.curAttacks)
                return false;
            if (weak > att.combatAddr.sweepBudget)
                weak = att.combatAddr.sweepBudget;
            PrintNameStringCall(att, "sweeps", 10, true);
            p = GDVars.creatureArr[GDVars.dsPotentialTargetCreature[1]];
            if (p != def)
            {
                GDVars.dsPotentialTargetCreature[defIdx] = GDVars.dsPotentialTargetCreature[1];
                GDVars.dsPotentialTargetCreature[1] = Map.CombatSlot(def);
            }
            for (int i = 1; i <= cnt; i++)
                if (weak > 0)
                    if (GDVars.creatureArr[GDVars.dsPotentialTargetCreature[i]].levelHighest == 0)
                    {
                        p = GDVars.creatureArr[GDVars.dsPotentialTargetCreature[i]];
                        NoteAttackDirection(att, p);
                        att.curAttacks = 1;
                        CombatAttack(att, p, false, null, ref GDVars.doneRoundFlag);
                        weak = unchecked((byte)(weak - 1));
                    }
            return true;
        }

        public static bool CanAttackTarget(TCharRecord creature, TCharRecord other)
        {
            if (other == null)
                return false;
            if (creature == other)
                return true;
            GDVars.dsCurrentSuppressTargetingFlag = 0;
            Fx.TriggerEffectsWithCondition(C_Cond.TARGET_VETO, other);
            if (GDVars.dsCurrentSuppressTargetingFlag == 0)
            {
                TCharRecord saved = creature.combatAddr.other;
                creature.combatAddr.other = other;
                Fx.TriggerEffectsWithCondition(C_Cond.ATTACKER_VETO, creature);
                creature.combatAddr.other = saved;
            }
            return GDVars.dsCurrentSuppressTargetingFlag == 0;
        }

        public static void CombatTurnUndead(TCharRecord creature)
        {
            PrintNameStringCall(creature, "turns undead...", 10, false);
            ClearBottomLineCall();
            PauseOnlyCall();
            bool any = false;
            bool stop = false;
            object tgt = null;
            sbyte col, row, turnVal;
            creature.combatAddr.usedTurnUndead = 1;
            byte destroys = 6;
            byte attempts = Dice.RollDice(1, 12);
            byte roll = Dice.RollDice(1, 20);
            sbyte tl = TurnLevel(creature);
            if (tl >= 1 && tl <= 8)
                col = TurnLevel(creature);
            else if (tl >= 9 && tl <= 13)
                col = 9;
            else
                col = 10;
            while (FindTurnUndeadTarget(creature, ref tgt) && attempts > 0 && stop == false)
            {
                TCharRecord t = (TCharRecord)tgt;
                // FindTurnUndeadTarget admits undead levels up to 12; the table's rows stop at 10
                row = t.levelUndead;
                if (row > 10)
                    row = 10;
                turnVal = GDConsts.turnTable[row, col];
                if (roll >= Math.Abs((int)turnVal))
                {
                    any = true;
                    Map.DisplayMapViewCentered(t, 3, false);
                    CombatPrintCreatureInfoCall(t);
                    if (turnVal > 0)
                    {
                        t.combatAddr.turned = 1;
                        PrintNameStringAnimCall(t, true, "is turned");
                    }
                    else
                    {
                        PrintNameStringCall(t, "Is destroyed", 10, false);
                        Map.CreatureDowned(t);
                        t.status = C_Status.GONE;
                        t.enabled = 0;
                    }
                    if (destroys > 0)
                        destroys = unchecked((byte)(destroys - 1));
                    attempts = unchecked((byte)(attempts - 1));
                    if (attempts == 0)
                        if (destroys > 0)
                            if (turnVal < 0)
                                attempts = unchecked((byte)(attempts + 1));
                    ClearMessageWindowCall();
                }
                else
                    stop = true;
            }
            if (!any)
                PrintBottomLineCall("Nothing Happens...");
            CountTeamsCall();
            GDVars.doneRoundFlag = CharHlp.EndActions(creature);
            ClearMessageWindowCall();
        }

        public static sbyte TurnLevel(TCharRecord creature)
        {
            sbyte lvl = creature.classLevels[C_Level.CLERIC];
            if (creature.classLevels[C_Level.PALADIN] - 2 > lvl)
                lvl = unchecked((sbyte)(creature.classLevels[C_Level.PALADIN] - 2));
            if (lvl < 0)
                lvl = 0;
            return lvl;
        }

        public static bool FindTurnUndeadTarget(TCharRecord creature, ref object tgt)
        {
            sbyte best = 13;
            tgt = null;
            byte cnt = Map.CountTargets(creature, 255);
            bool result = false;
            for (int i = 1; i <= cnt; i++)
            {
                TCharRecord p = GDVars.creatureArr[GDVars.dsPotentialTargetCreature[i]];
                if (p.combatAddr.turned == 0 && p.levelUndead > 0 && p.levelUndead < best)
                {
                    best = p.levelUndead;
                    tgt = p;
                    result = true;
                }
            }
            return result;
        }

        internal static void DoAttack(TCharRecord att, TCharRecord def, bool fromBehind, ref bool done)
        {
            bool behind = fromBehind;
            done = false;
            GDVars.hitsMade[1] = 0;
            GDVars.hitsMade[2] = 0;
            attacksMade[1] = 0;
            attacksMade[2] = 0;
            bool anyHit = false;
            bool defDowned = false;
            GDVars.pendingDamage = 0;
            att.combatAddr.madeAttack = 1;
            if (CharHlp.IsHelpless(def))
            {
                SoundCommandCall(GDConsts.soundIds.combat[C_Sound.IMPACT]);
                while (att.combatAddr.attackPhase > 1 && CurAtt(att, att.combatAddr.attackPhase) == 0)
                    att.combatAddr.attackPhase = unchecked((byte)(att.combatAddr.attackPhase - 1));
                attacksMade[att.combatAddr.attackPhase] = unchecked((byte)(attacksMade[att.combatAddr.attackPhase] + 1));
                byte slay;
                if (def.hpCur > 250)
                    slay = 255;
                else
                    slay = (byte)(def.hpCur + 5);
                ProcessAttackResult(att, def, 3, slay, 1, true);
                Fx.RemoveInvisibility(att);
                for (int ph = 1; ph <= 2; ph++)
                    SetCurAtt(att, ph, 0);
                anyHit = true;
                done = true;
            }
            else
            {
                if (att.equipped[C_Equip.WEAPON] != null
                    && (def.iconDimensions > 0x80 || (def.iconDimensions & 7) > 1))
                {
                    // large target: swap in the large-damage dice
                    TBaseItemRec bas = CharHlp.BaseItem(att.equipped[C_Equip.WEAPON].itemType);
                    att.curRollsA[1] = bas.dmgLgRolls;
                    att.curDiceA[1] = bas.dmgLgDice;
                    att.curModA[1] = unchecked((sbyte)(att.curModA[1] - bas.dmgSmMod + bas.dmgLgMod));
                }
                RecalcStatsCall(def);
                Fx.TriggerEffectsWithCondition(C_Cond.DEFENDER_AC, def);
                int behindIdx = 0;
                byte defense;
                if (IsBackstab(att, def))
                    defense = unchecked((byte)(def.acBehind - 2));
                else
                {
                    if (def.combatAddr.flankCount > 1)
                        if (DirTowardTarget(att, def) == def.combatAddr.direction && def.combatAddr.flankSpread > 4)
                            behind = true;
                    if (behind)
                        behindIdx = 1;
                    defense = AcCurArr(def, behindIdx);
                }
                ModifyDefenseForDistance(att, def, ref defense);
                byte atkType = 0;
                if (behind)
                    atkType = 1;
                if (IsBackstab(att, def))
                    atkType = 2;
                byte startPhase = att.combatAddr.attackPhase;
                for (int phase = startPhase; phase >= 1; phase--)
                    while (CurAtt(att, phase) > 0 && defDowned == false)
                    {
                        SetCurAtt(att, phase, unchecked((byte)(CurAtt(att, phase) - 1)));
                        att.combatAddr.attackPhase = (byte)phase;
                        attacksMade[phase] = unchecked((byte)(attacksMade[phase] + 1));
                        if (Fx.AttackRoll(att, def, defense) || CharHlp.IsHelpless(def))
                        {
                            anyHit = true;
                            TEffectRec image;
                            if (CharHlp.HasEffect(def, C_Effect.MIRROR_IMAGE, out image)
                                && Dice.RollDice(1, unchecked((byte)(image.strength + 1))) > 1)
                            {
                                // the blow strikes an image
                                SoundCommandCall(GDConsts.soundIds.combat[C_Sound.MISS]);
                                ProcessAttackResult(att, def, atkType, 0, 0, false);
                                PrintNameStringCall(def, "lost an image", 10, true);
                                image.strength = unchecked((byte)(image.strength - 1));
                                if (image.strength == 0)
                                    Fx.RemoveEffect(def, C_Effect.MIRROR_IMAGE, null);
                            }
                            else
                            {
                                GDVars.hitsMade[phase] = unchecked((byte)(GDVars.hitsMade[phase] + 1));
                                SoundCommandCall(GDConsts.soundIds.combat[C_Sound.IMPACT]);
                                CalcWeaponDamage(att, def, (byte)phase);
                                ProcessAttackResult(att, def, atkType, GDVars.pendingDamage, GDVars.pendingDamage, true);
                                if (def.enabled != 0)
                                    // C_Cond.HIT_PHASE_1/2
                                    Fx.TriggerEffectsWithCondition((byte)(phase + 1), att);
                                if (def.enabled == 0)
                                    defDowned = true;
                            }
                        }
                    }
                if (!anyHit)
                {
                    SoundCommandCall(GDConsts.soundIds.combat[C_Sound.MISS]);
                    ProcessAttackResult(att, def, atkType, 0, 0, false);
                }
                done = true;
                for (int ph = 1; ph <= 2; ph++)
                    if (CurAtt(att, ph) > 0)
                        done = false;
                // sweep budget spent
                att.combatAddr.sweepBudget = 0;
            }
            if (done)
                done = CharHlp.EndActions(att);
        }

        public static void NoteAttackDirection(TCharRecord att, TCharRecord def)
        {
            def.combatAddr.flankCount = unchecked((byte)(def.combatAddr.flankCount + 1));
            byte d = DirTowardTarget(def, att);
            byte rel = unchecked((byte)((d - def.combatAddr.direction + 8) % 8));
            if (rel > 4)
                rel = (byte)(8 - rel);
            def.combatAddr.flankSpread = (byte)((def.combatAddr.flankSpread + rel) % 8);
        }

        public static void CombatAttack(TCharRecord att, TCharRecord def, bool fromBehind, TItemRec weapon, ref bool done)
        {
            byte dir;
            if (def.combatAddr.flankCount < 3 && !fromBehind)
            {
                dir = DirTowardTarget(def, att);
                if (Map.CombatIconVisible(def, 0))
                    Map.SetCreatureFacing(def, dir, 0, 0);
                else
                    def.combatAddr.direction = dir;
            }
            dir = DirTowardTarget(att, def);
            CombatPrintCreatureInfoCall(att);
            Map.SetCreatureFacing(att, dir, 1, 0);
            att.combatAddr.other = def;
            Delay(100);
            if (weapon != null)
                RangedAttackAnim(att, def, weapon);
            // a sling's stone animates a second time
            if (att.equipped[C_Equip.WEAPON] != null && att.equipped[C_Equip.WEAPON].itemType == C_Item.SLING)
                RangedAttackAnim(att, def, att.equipped[C_Equip.WEAPON]);
            done = true;
            if (att.curAttacks > 0 || att.curAttacks2 > 0)
            {
                TCharRecord savedCur = GDVars.curChar;
                GDVars.curChar = att;
                DoAttack(att, def, fromBehind, ref done);
                if (weapon != null)
                {
                    if (weapon.quantity > 0)
                        weapon.quantity = unchecked((byte)(weapon.quantity - attacksMade[1]));
                    if (weapon.quantity == 0)
                    {
                        if (CharHlp.HasThrowableWeapon(att) && weapon.special != C_Effect.SPIRIT_HAMMER_GONE)
                        {
                            // the expended thrown weapon drops into the loot pile
                            PrependItemCopyCall(ref GDVars.treasureItems, weapon);
                            TItemRec item = GDVars.treasureItems;
                            item.equipped = 0;
                            RemoveItemCall(att, weapon);
                            GDVars.lastMissileItem = item;
                        }
                        else
                            RemoveItemCall(att, weapon);
                    }
                }
                RecalcStatsCall(att);
                GDVars.curChar = savedCur;
            }
            if (done)
                done = CharHlp.EndActions(att);
            if (Map.CombatIconVisible(att, 0))
            {
                Map.SetCreatureFacing(att, att.combatAddr.direction, 1, 1);
                Map.SetCreatureFacing(att, att.combatAddr.direction, 0, 0);
            }
        }

        internal static void ModifyDefenseForDistance(TCharRecord att, TCharRecord def, ref byte defense)
        {
            sbyte subrange;
            sbyte dist = unchecked((sbyte)Map.CalcDistance(att, def));
            if (CharHlp.HasRangedWeapon(att))
                subrange = unchecked((sbyte)((CharHlp.BaseItem(att.equipped[C_Equip.WEAPON].itemType).range - 1) / 3));
            else
                subrange = dist;
            if (dist - subrange > 0)
            {
                dist = unchecked((sbyte)(dist - subrange));
                defense = unchecked((byte)(defense + 2));
            }
            if (dist - subrange > 0)
            {
                dist = unchecked((sbyte)(dist - subrange));
                // cumulative with the first +2
                defense = unchecked((byte)(defense + 3));
            }
        }

        public static bool FindCureTarget(TCharRecord creature, ref object tgt)
        {
            object best = null;
            byte downIdx = 0;
            byte bestHp = 255;
            byte crIdx = 0, terrIdx = 0;
            for (int d = 0; d <= C_Dir.CENTER; d++)
            {
                sbyte nx = unchecked((sbyte)(Map.CombatCellX(creature) + GDConsts.dirDX[d]));
                sbyte ny = unchecked((sbyte)(Map.CombatCellY(creature) + GDConsts.dirDY[d]));
                Map.GetArenaContents(nx, ny, ref crIdx, ref terrIdx);
                if (crIdx > 0)
                {
                    // the var param is the scratch
                    tgt = GDVars.creatureArr[crIdx];
                    TCharRecord t = (TCharRecord)tgt;
                    if (t.hostile == creature.hostile)
                        if (t.hpCur < t.hpMax)
                            if (t.hpCur < bestHp || (t == creature && t.hpCur < t.hpMax / 2))
                            {
                                best = tgt;
                                bestHp = t.hpCur;
                            }
                }
                else if (terrIdx == C_Terr.CORPSE)
                    for (int j = 1; j <= GDVars.downedCount; j++)
                        if (GDVars.downed[j].cr != null)
                            if (!IsDeadStatus(GDVars.downed[j].cr.status))
                                if (GDVars.downed[j].x == nx)
                                    if (GDVars.downed[j].y == ny)
                                        downIdx = (byte)j;
            }
            if (bestHp < 8 || downIdx == 0)
                tgt = best;
            else
                tgt = GDVars.downed[downIdx].cr;
            return tgt != null;
        }

        internal static bool PickSpellTarget(byte spell, bool ai, byte ground, ref TAimTarget tgt)
        {
            bool found = false;
            if (!ai)
            {
                CombatAimMenu(GDVars.curChar, CharHlp.CalcSpellRange(spell), 0, ground, ref found, ref tgt);
                GDVars.curChar.combatAddr.other = (TCharRecord)tgt.cr;
            }
            else
            {
                if (GDConsts.spellTable[spell].offensive == 0)
                {
                    tgt.cr = GDVars.curChar;
                    if (spell != C_Spell.CURE_LIGHT_WOUNDS || FindCureTarget(GDVars.curChar, ref tgt.cr))
                    {
                        tgt.x = Map.CombatCellX((TCharRecord)tgt.cr);
                        tgt.y = Map.CombatCellY((TCharRecord)tgt.cr);
                        found = true;
                    }
                }
                else
                {
                    byte tries = 20;
                    while (tries > 0 && found == false)
                    {
                        bool ok = true;
                        if (AcquireTarget(GDVars.curChar, CharHlp.CalcSpellRange(spell), 0, 1))
                        {
                            TCharRecord p = GDVars.curChar.combatAddr.other;
                            if (GDConsts.spellTable[spell].aiRadius > 0)
                            {
                                if (AreaHitsNonEnemy(Map.CombatCellX(p), Map.CombatCellY(p), GDConsts.spellTable[spell].aiRadius))
                                    ok = false;
                            }
                            if (CharHlp.IsHelpless(p))
                                for (int i = 1; i <= GDConsts.helplessFx.Length - 1; i++)
                                    if (GDConsts.spellTable[spell].effect == GDConsts.helplessFx[i])
                                        ok = false;
                            if (ok)
                            {
                                tgt.cr = GDVars.curChar.combatAddr.other;
                                tgt.x = Map.CombatCellX((TCharRecord)tgt.cr);
                                tgt.y = Map.CombatCellY((TCharRecord)tgt.cr);
                                found = true;
                            }
                        }
                        tries = unchecked((byte)(tries - 1));
                    }
                }
            }
            if (found)
            {
                GDVars.gTargetX = tgt.x;
                GDVars.gTargetY = tgt.y;
                return true;
            }
            ClearAim(tgt);
            return false;
        }

        internal static byte AimMarker(byte spell)
        {
            byte result = SPRITE_SPARKLE;
            if (spell >= 1 && spell <= GDConsts.spellTable.Length - 1)
                if (GDConsts.spellTable[spell].offensive == 1 && GDConsts.spellTable[spell].target == 0)
                    result = SPRITE_MISSILE;
            return result;
        }

        public static void CombatCastTargetMenu(byte spell, bool b, ref bool cast)
        {
            TAimTarget sel = new TAimTarget();
            bool picked;
            cast = true;
            GDVars.spellTargetCount = 0;
            GDVars.gMultiTarget = 0;
            GDVars.gTargetX = Map.CombatCellX(GDVars.curChar);
            GDVars.gTargetY = Map.CombatCellY(GDVars.curChar);
            int area = GDConsts.spellTable[spell].area & 0xF;
            if (area == 0)
            {
                // self only
                GDVars.dsTargetCreaturePtr[1] = GDVars.curChar;
                GDVars.spellTargetCount = 1;
            }
            else if (area == 15)
            {
                // the picked creature, or an area at the picked cell
                if (!b)
                    Map.StartSpellPreview(TAimShape.AimBlast, 0, AimMarker(spell));
                picked = PickSpellTarget(spell, b, 0, ref sel);
                Map.StopSpellPreview();
                if (picked)
                {
                    if (GDVars.curChar.combatAddr.other != null)
                    {
                        GDVars.dsTargetCreaturePtr[1] = GDVars.curChar.combatAddr.other;
                        GDVars.spellTargetCount = 1;
                    }
                    else
                    {
                        // (area and $F) shr 4 is always 0: a zero radius
                        TargetAreaCall(GDVars.gTargetX, GDVars.gTargetY, (byte)((GDConsts.spellTable[spell].area & 0xF) >> 4));
                        GDVars.gMultiTarget = 1;
                    }
                }
                else
                    cast = false;
            }
            else if (area >= 8 && area <= 14)
            {
                if (!b)
                {
                    byte radius = (byte)(GDConsts.spellTable[spell].area & 7);
                    if (spell == C_Spell.LIGHTNING_BOLT)
                        Map.StartSpellPreview(TAimShape.AimBolt, 0, SPRITE_LIGHTNING);
                    else if (spell == C_Spell.STINKING_CLOUD)
                        Map.StartSpellPreview(TAimShape.AimCloud, 0, SPRITE_SPARKLE);
                    else if (spell == C_Spell.FIREBALL || spell == C_Spell.ITEM_FIREBALL)
                    {
                        if (GDVars.zone0.view3dFlag == 0)
                            radius = 2;
                        Map.StartSpellPreview(TAimShape.AimBlast, radius, SPRITE_BURST);
                    }
                    else
                        Map.StartSpellPreview(TAimShape.AimBlast, radius, AimMarker(spell));
                }
                picked = PickSpellTarget(spell, b, 1, ref sel);
                Map.StopSpellPreview();
                if (picked)
                {
                    TargetAreaCall(GDVars.gTargetX, GDVars.gTargetY, (byte)(GDConsts.spellTable[spell].area & 7));
                    GDVars.gMultiTarget = 1;
                }
                else
                    cast = false;
            }
            else
            {
                // 1..7: (area and 3) + 1 individual targets
                byte remaining = (byte)((GDConsts.spellTable[spell].area & 3) + 1);
                byte k = 0;
                while (remaining > 0)
                {
                    if (!b)
                        Map.StartSpellPreview(TAimShape.AimBlast, 0, AimMarker(spell));
                    picked = PickSpellTarget(spell, b, 0, ref sel);
                    Map.StopSpellPreview();
                    if (picked)
                    {
                        bool dup = false;
                        for (int j = 1; j <= k; j++)
                            if (GDVars.dsTargetCreaturePtr[j] == sel.cr)
                                dup = true;
                        if (!dup)
                        {
                            k = unchecked((byte)(k + 1));
                            GDVars.dsTargetCreaturePtr[k] = (TCharRecord)sel.cr;
                            remaining = unchecked((byte)(remaining - 1));
                        }
                        else if (!b)
                            PrintBottomLineCall("Already been targeted");
                        else
                            remaining = unchecked((byte)(remaining - 1));
                        Map.RedrawArenaCell(Map.CombatCellX(GDVars.dsTargetCreaturePtr[k]), Map.CombatCellY(GDVars.dsTargetCreaturePtr[k]));
                    }
                    else
                        remaining = unchecked((byte)(remaining - 1));
                }
                GDVars.spellTargetCount = k;
                if (k == 0)
                    cast = false;
            }
        }

        public static void CastSpellCombat(byte spell, byte ai, ref bool cast)
        {
            TCharRecord caster = GDVars.curChar;
            bool notEmpty = true;
            int idx = -1;
            cast = false;
            byte spellId = spell;
            TItemRec scroll = null;
            if (spellId == C_Spell.NONE)
            {
                spellId = Mag.CastSpellMenu(ref idx, ref notEmpty, ref scroll);
                if (spellId == C_Spell.NONE)
                    scroll = null;
            }
            if (spellId > C_Spell.NONE)
                if (GDConsts.spellTable[spellId].whenCast == 0)
                {
                    PrintBottomLineCall("Camp Only Spell");
                    spellId = C_Spell.NONE;
                    scroll = null;
                }
            if (scroll != null)
            {
                Mag.UseItemSpell(scroll, spellId, true, ref cast);
                RecalcStatsCall(caster);
                SetCurAttacksX2(caster);
                if (!cast)
                    DisplayCombatInterfaceCall();
                return;
            }
            if (ai == 0)
            {
                DisplayCombatInterfaceCall();
                Map.DisplayMapViewCentered(caster, 3, true);
                CombatPrintCreatureInfoCall(caster);
            }
            if (spellId > C_Spell.NONE)
            {
                byte castTime = (byte)(GDConsts.spellTable[spellId].castTime / 3);
                if (castTime == 0)
                {
                    Mag.CastSpellNC(spellId, ai != 0, true, ref cast);
                    cast = CharHlp.EndActions(caster);
                }
                else
                {
                    cast = true;
                    PrintNameStringCall(caster, "Begins Casting", 10, true);
                    caster.combatAddr.spellCast = spellId;
                    if (caster.combatAddr.initiative > castTime)
                        caster.combatAddr.initiative = unchecked((sbyte)(caster.combatAddr.initiative - castTime));
                    else
                        caster.combatAddr.initiative = 1;
                }
            }
        }

        internal static bool IsBackstab(TCharRecord att, TCharRecord def)
        {
            bool can = false;
            byte wItem = 0;
            TItemRec weapon = att.equipped[C_Equip.WEAPON];
            if (weapon != null)
                wItem = weapon.itemType;
            TItemRec armor = att.equipped[C_Equip.ARMOR];
            if (att.classLevels[C_Level.THIEF] > 0)
                if (armor == null || armor.itemType == C_Item.LEATHER_ARMOR)
                    if (weapon == null || wItem == C_Item.CLUB || wItem == C_Item.DAGGER
                        || (wItem >= C_Item.BROAD_SWORD && wItem <= C_Item.SHORT_SWORD))
                        can = true;
            return can && def.combatAddr.flankCount > 1 && DirTowardTarget(att, def) == def.combatAddr.direction;
        }

        public static byte DirTowardTarget(TCharRecord creature, TCharRecord other)
        {
            sbyte x1 = Map.CombatCellX(creature);
            sbyte y1 = Map.CombatCellY(creature);
            sbyte x2 = Map.CombatCellX(other);
            sbyte y2 = Map.CombatCellY(other);
            byte d = 0;
            while (!Pathing.IsDirToward(x1, y1, x2, y2, d))
                d = unchecked((byte)(d + 1));
            return d;
        }

        internal static void RangedAttackAnim(TCharRecord att, TCharRecord def, TItemRec weapon)
        {
            byte dir = DirTowardTarget(att, def);
            byte frames = 1;
            byte pace = 10;
            byte sym = 13;
            ushort size = Blit.MaskBytes(GDVars.dsMoveIconSheet.imgSize);
            switch (weapon.itemType)
            {
                // straight-flying missiles
                case C_Item.DART:
                case C_Item.JAVELIN:
                case C_Item.QUARREL:
                case C_Item.SPEAR:
                case C_Item.ARROW:
                    if ((dir & 1) != 0)
                    {
                        if (dir == C_Dir.SOUTHEAST || dir == C_Dir.SOUTHWEST)
                            CopyIconFacingCall((byte)(sym + 1), 1, 0, dir == C_Dir.SOUTHWEST);
                        else
                            CopyIconFacingCall((byte)(sym + 1), 0, 0, dir == C_Dir.NORTHWEST);
                    }
                    else
                        CopyIconFacingCall((byte)(sym + dir % 4), (byte)(dir / 4), 0, false);
                    break;
                // spinning thrown weapons
                case C_Item.HAND_AXE:
                case C_Item.CLUB:
                case C_Item.HAMMER:
                    BuildIconFacingsCall((byte)(sym + 3));
                    frames = 4;
                    pace = 50;
                    break;
                // holy water / oil flask
                case C_Item.HOLY_WATER_VIAL:
                case C_Item.FLASK_OF_OIL:
                    BuildIconFacingsCall((byte)(sym + 4));
                    frames = 4;
                    pace = 50;
                    break;
                default:
                    if (weapon.itemType == C_Item.SLING)
                        sym = (byte)(sym + 1);
                    CopyIconFacingCall((byte)(sym + 7), 0, 0, false);
                    CopyIconFacingCall((byte)(sym + 7), 1, 1, false);
                    frames = 2;
                    pace = 20;
                    break;
            }
            SoundSetAutoStopCall(0);
            SoundCommandCall(GDConsts.soundIds.combat[C_Sound.RANGED_START]);
            CombatAnimateMovingIconCall(Map.CombatCellX(att), Map.CombatCellY(att), Map.CombatCellX(def), Map.CombatCellY(def), frames, pace);
            SoundCommandCall(GDConsts.soundIds.combat[C_Sound.RANGED_END]);
            SoundSetAutoStopCall(10);
        }

        public static void CalcHostileHpPercent()
        {
            ushort maxSum = 0;
            ushort curSum = 0;
            TCharRecord p = GDVars.creatureList;
            while (p != null)
            {
                if (p.hostile == 1)
                {
                    if (p.enabled != 0)
                        curSum = unchecked((ushort)(curSum + p.hpCur));
                    maxSum = unchecked((ushort)(maxSum + p.hpMax));
                }
                p = p.nextCharAddr;
            }
            if (maxSum > 0)
                GDVars.hostileHpPct = unchecked((byte)((int)curSum * 20 / maxSum * 5));
        }

        public static byte HighestEnemyMovement(TCharRecord creature)
        {
            byte best = 0;
            TCharRecord p = GDVars.creatureList;
            while (p != null)
            {
                if (CharHlp.EnemyTeam(creature) == p.hostile && p.enabled != 0)
                {
                    byte m = (byte)(CalcCreatureMovement(p) / 2);
                    if (m > best)
                        best = m;
                }
                p = p.nextCharAddr;
            }
            return best;
        }

        public static bool CheckAttackAlly(TCharRecord att, TCharRecord def)
        {
            if (CharHlp.EnemyTeam(def) == att.hostile || att.quickfight != 0)
                return true;
            if (YesNoMenuCall("Attack Ally: ", LightMagenta, LightGreen, White) != (byte)'Y')
                return false;
            GDVars.zone1[C_Gbvm.ATTACKED_ALLIES] = 1;
            TCharRecord p = GDVars.creatureList;
            while (p != null)
            {
                // NPC flag (high bit) set
                if (p.status == C_Status.OKAY && CharHlp.IsNpc(p))
                {
                    p.hostile = 1;
                    p.combatAddr.other = null;
                }
                p = p.nextCharAddr;
            }
            CountTeamsCall();
            return true;
        }

        internal static byte DirOfKey(byte ch)
        {
            switch (ch)
            {
                case C_Keys.UP: return C_Dir.NORTH;
                case C_Keys.PGUP: return C_Dir.NORTHEAST;
                case C_Keys.RIGHT: return C_Dir.EAST;
                case C_Keys.PGDN: return C_Dir.SOUTHEAST;
                case C_Keys.DOWN: return C_Dir.SOUTH;
                case C_Keys.END_KEY: return C_Dir.SOUTHWEST;
                case C_Keys.LEFT: return C_Dir.WEST;
                case C_Keys.HOME: return C_Dir.NORTHWEST;
                default: return C_Dir.CENTER;
            }
        }

        internal static byte AimTargetMenu(TCharRecord cur, TCharRecord tgt, byte range, byte weaponMode, byte ground, ref byte dir)
        {
            bool ext = false;
            TItemRec weapon = null;
            byte ch;
            string menu = "";
            byte dist = Map.CalcDistance(cur, tgt);
            if (weaponMode != 0)
                DisplayStringCall(0, 23, LightGreen, "Range = " + StrUtil.NumStr(dist) + "  ");
            if (dist <= range)
            {
                if (weaponMode == 0)
                    menu = "Target ";
                else if (cur != tgt)
                {
                    if (!CharHlp.HasRangedWeapon(cur))
                        menu = "Target ";
                    else if (CharHlp.FindDamagingRangedWeapon(cur, ref weapon))
                        if (Map.CountTargets(cur, 1) == 0 || CharHlp.HasThrowableWeapon(cur))
                            menu = "Target ";
                }
            }
            menu = Str("Next Prev Manual " + menu + "Center Exit", 40);
            Map.DisplayMapViewCentered(tgt, 3, true);
            CombatPrintCreatureInfoCall(tgt);
            do
            {
                MenuCore.arrowsToCaller = true;
                ch = MenuHorizontalCall("Aim: ", menu, LightMagenta, LightGreen, White, true, false, ref ext);
                dir = C_Dir.CENTER;
                if (ext)
                    dir = DirOfKey(ch);
            } while (!(!ext || dir != C_Dir.CENTER));
            return ch;
        }

        internal static void ConfirmAttackTarget(TCharRecord cur, TCharRecord tgt, byte weaponMode, ref bool done, ref TAimTarget sel)
        {
            TItemRec weapon = null;
            done = true;
            if (weaponMode == 1)
                if (!CheckAttackAlly(cur, tgt))
                    done = false;
            if (done)
            {
                sel.cr = tgt;
                sel.x = Map.CombatCellX(tgt);
                sel.y = Map.CombatCellY(tgt);
                GDVars.arenaPtr.cursorOn = false;
                Map.ScrollView(unchecked((sbyte)(GDVars.arenaPtr.ox + 3)), unchecked((sbyte)(GDVars.arenaPtr.oy + 3)), 3, C_Dir.CENTER);
                if (weaponMode == 1)
                {
                    if (SweepAttempt(cur, tgt))
                        done = CharHlp.EndActions(cur);
                    else
                    {
                        NoteAttackDirection(cur, tgt);
                        if (CharHlp.HasRangedWeapon(cur))
                            if (CharHlp.FindDamagingRangedWeapon(cur, ref weapon))
                                if (CharHlp.HasThrowableWeapon(cur))
                                    if (Map.CalcDistance(cur, tgt) == 1)
                                        weapon = null;
                        CombatAttack(cur, tgt, false, weapon, ref done);
                    }
                }
            }
            else
                ClearAim(sel);
        }

        internal static void ManualAimCursor(TCharRecord cur, TCharRecord underCursor, byte range, byte weaponMode, byte ground, byte startDir,
                                             ref bool done, ref TAimTarget sel)
        {
            TItemRec weapon = null;
            byte crIdx = 0, terrIdx = 0;
            ClearAim(sel);
            sbyte cx = Map.CombatCellX(underCursor);
            sbyte cy = Map.CombatCellY(underCursor);
            byte ch = (byte)' ';
            byte dir = startDir;
            done = false;
            GDVars.arenaPtr.cursorOn = true;
            GDVars.arenaPtr.cursorSize = 1;
            while (!(ch == C_Keys.NONE || ch == (byte)'E' || ch == (byte)'T'))
            {
                Map.ScrollView(cx, cy, 3, dir);
                cx = unchecked((sbyte)(cx + GDConsts.dirDX[dir]));
                cy = unchecked((sbyte)(cy + GDConsts.dirDY[dir]));
                if (cx < 0)
                    cx = 0;
                if (cy < 0)
                    cy = 0;
                if (cx > 49)
                    cx = 49;
                if (cy > 24)
                    cy = 24;
                Map.GetArenaContents(cx, cy, ref crIdx, ref terrIdx);
                bool valid = false;
                ushort dist = 255;
                sbyte wx = cx;
                sbyte wy = cy;
                if (WalkLineCall(Map.CombatCellX(cur), Map.CombatCellY(cur), ref wx, ref wy, ref dist, GDVars.arenaPtr))
                {
                    valid = true;
                    if (weaponMode != 0)
                        DisplayStringCall(0, 23, LightGreen, "Range = " + StrUtil.NumStr(dist / 2) + "  ");
                }
                else if (weaponMode != 0)
                    ClearBoxCall(0, 23, 39, 23);
                dist = (ushort)(dist / 2);
                underCursor = null;
                if (valid)
                {
                    if (crIdx > 0)
                        underCursor = GDVars.creatureArr[crIdx];
                    else if (terrIdx == C_Terr.CORPSE)
                        for (int j = 1; j <= GDVars.downedCount; j++)
                            if (GDVars.downed[j].x == cx)
                                if (GDVars.downed[j].y == cy)
                                    underCursor = GDVars.downed[j].cr;
                }
                if (underCursor != null)
                    CombatPrintCreatureInfoCall(underCursor);
                else
                    ClearBoxCall(23, 1, 38, 21);
                if (range < dist || GDConsts.terrainInfo[terrIdx].cost == 255)
                    valid = false;
                if (underCursor != null)
                {
                    if (!CanAttackTarget(cur, underCursor))
                        valid = false;
                    if (weaponMode == 1)
                    {
                        if (cur == underCursor || (crIdx == 0 && terrIdx == C_Terr.CORPSE))
                            valid = false;
                        else if (CharHlp.HasRangedWeapon(cur))
                            if (!CharHlp.FindDamagingRangedWeapon(cur, ref weapon)
                                || (Map.CountTargets(cur, 1) > 0 && !CharHlp.HasThrowableWeapon(cur)))
                                valid = false;
                    }
                }
                else if (ground == 0)
                    valid = false;
                string menu = "Center Exit";
                if (valid)
                    menu = Str("Target " + menu, 40);
                MenuCore.arrowsToCaller = true;
                ch = MenuHorizontalCall("(Use Cursor keys) ", menu, LightMagenta, LightGreen, White, true, false, ref GDVars.doneRoundFlag);
                if (ch == (byte)'T')
                {
                    GDVars.arenaPtr.cursorOn = false;
                    if (valid)
                    {
                        sel.x = cx;
                        sel.y = cy;
                        if (underCursor != null)
                            sel.cr = underCursor;
                        else
                            sel.cr = null;
                        if (weaponMode == 1)
                            ConfirmAttackTarget(cur, (TCharRecord)sel.cr, weaponMode, ref done, ref sel);
                        else
                            done = true;
                    }
                    if (valid == false || done == false)
                    {
                        Map.RedrawArenaCell(cx, cy);
                        done = false;
                        ClearAim(sel);
                    }
                }
                else if (ch == C_Keys.NONE || ch == (byte)'E')
                {
                    Map.RedrawArenaCell(cx, cy);
                    ClearAim(sel);
                    done = false;
                }
                else if (ch == (byte)'C')
                {
                    Map.ScrollView(cx, cy, 0, C_Dir.CENTER);
                    dir = C_Dir.CENTER;
                }
                else
                    dir = DirOfKey(ch);
            }
        }

        /// <summary>tgt is the Pascal untyped var parameter; every caller passes a TAimTarget. A null reference is given fresh
        /// storage (the Pascal parameter always names storage).</summary>
        public static void CombatAimMenu(TCharRecord creature, byte range, byte weaponMode, byte groundOk, ref bool done, ref TAimTarget tgt)
        {
            TCombatTarget[] targets = new TCombatTarget[73]; // 1..72
            for (int i = 0; i < 73; i++) targets[i] = new TCombatTarget();
            byte tgtCount = 0;
            byte curIdx;
            sbyte skipStep;
            // Pascal leaves menuStep uninitialized until the first N/P/M/T/C; see the pass doc (an Exit on the first menu
            // round only feeds it to an index advance whose result is discarded).
            sbyte menuStep = 0;
            sbyte curX = 0, curY = 0, nextX = 0, nextY = 0;
            TCharRecord p;
            byte ch;
            byte keyDir = 0;

            // Build the parent routine's aiming list from targets within range 127.
            void BuildAimTargets(TCharRecord c)
            {
                BuildTargetListCall(Map.CombatCellX(c), Map.CombatCellY(c), 127, 255, Map.GetIconSize(c), GDVars.arenaPtr);
                tgtCount = GDVars.targetCount;
                for (int i = 1; i <= tgtCount; i++)
                {
                    TCombatTarget s = GDVars.targetList[i];
                    targets[i] = new TCombatTarget { idx = s.idx, dist = s.dist, dir = s.dir };
                }
            }

            TCharRecord AdvanceAimTarget(sbyte step, bool animate)
            {
                if (animate)
                {
                    curX = GDVars.arenaLoc[targets[curIdx].idx].x;
                    curY = GDVars.arenaLoc[targets[curIdx].idx].y;
                }
                else
                    Map.RedrawArenaCell(curX, curY);
                curIdx = unchecked((byte)(curIdx + step));
                if (curIdx == 0)
                    curIdx = tgtCount;
                if (curIdx > tgtCount)
                    curIdx = 1;
                TCharRecord r = GDVars.creatureArr[targets[curIdx].idx];
                nextX = GDVars.arenaLoc[targets[curIdx].idx].x;
                nextY = GDVars.arenaLoc[targets[curIdx].idx].y;
                if (animate)
                {
                    CombatAnimateMovingIconCall(curX, curY, nextX, nextY, 1, 0);
                    curX = nextX;
                    curY = nextY;
                }
                return r;
            }

            // the aim cursor icon
            CopyIconFacingCall(25, 0, 0, false);
            if (tgt == null)
                tgt = new TAimTarget();
            ClearAim(tgt);
            done = false;
            byte effRange = range;
            if (effRange == 255)
            {
                if (creature.equipped[C_Equip.WEAPON] != null)
                    effRange = unchecked((byte)(CharHlp.BaseItem(creature.equipped[C_Equip.WEAPON].itemType).range - 1));
                else
                    effRange = 1;
            }
            if (effRange == 0 || effRange == 255)
                effRange = 1;
            BuildAimTargets(creature);
            curIdx = 1;
            skipStep = 0;
            p = AdvanceAimTarget(skipStep, true);
            skipStep = 1;
            ch = (byte)' ';
            while (done == false && !(ch == C_Keys.NONE || ch == (byte)'E'))
            {
                if (!CanAttackTarget(creature, p))
                    p = AdvanceAimTarget(skipStep, false);
                else
                {
                    ch = AimTargetMenu(creature, p, effRange, weaponMode, groundOk, ref keyDir);
                    // a cursor key goes straight to manual aim
                    if (keyDir != C_Dir.CENTER)
                        ch = (byte)'M';
                    switch (ch)
                    {
                        case (byte)'N':
                            skipStep = 1;
                            menuStep = 1;
                            break;
                        case (byte)'P':
                            skipStep = -1;
                            menuStep = -1;
                            break;
                        case (byte)'M':
                            ManualAimCursor(creature, p, effRange, weaponMode, groundOk, keyDir, ref done, ref tgt);
                            CopyIconFacingCall(25, 0, 0, false);
                            BuildAimTargets(creature);
                            menuStep = 0;
                            break;
                        case (byte)'T':
                            ConfirmAttackTarget(creature, p, weaponMode, ref done, ref tgt);
                            CopyIconFacingCall(25, 0, 0, false);
                            BuildAimTargets(creature);
                            menuStep = 0;
                            break;
                        case (byte)'C':
                            Map.ScrollView(Map.CombatCellX(p), Map.CombatCellY(p), 0, C_Dir.CENTER);
                            menuStep = 0;
                            break;
                    }
                    Map.RedrawArenaCell(Map.CombatCellX(p), Map.CombatCellY(p));
                    p = AdvanceAimTarget(menuStep, done == false && !(ch == C_Keys.NONE || ch == (byte)'E'));
                }
            }
            if (weaponMode != 0)
                ClearBoxCall(0, 23, 39, 23);
        }

        public static bool AcquireTarget(TCharRecord creature, byte range, byte relaxed, byte forceNew)
        {
            bool pass = false;
            bool found = false;
            bool second = false;
            TCharRecord p = creature.combatAddr.other;
            if (forceNew != 0
                || (p != null && (p.hostile == creature.hostile || p.enabled == 0 || !CanAttackTarget(creature, p))))
                creature.combatAddr.other = null;
            if (creature.combatAddr.other != null)
                found = true;
            while (!found && !second)
            {
                second = pass;
                if (pass && forceNew == 0)
                    GDVars.arenaPtr.distMode = 1;
                byte tries = 20;
                byte cnt = Map.CountTargets(creature, range);
                while (tries > 0 && !found && cnt > 0)
                {
                    tries = unchecked((byte)(tries - 1));
                    byte pick = Dice.RollDice(1, cnt);
                    if (GDVars.dsPotentialTargetCreature[pick] > 0)
                    {
                        p = GDVars.creatureArr[GDVars.dsPotentialTargetCreature[pick]];
                        if ((relaxed != 0 && GDVars.arenaPtr.distMode != 0) || CanAttackTarget(creature, p))
                        {
                            found = true;
                            creature.combatAddr.other = p;
                        }
                        else
                        {
                            GDVars.dsPotentialTargetCreature[pick] = 0;
                            // pick is reused as the index: do any targets remain?
                            for (pick = 1; pick <= cnt; pick++)
                                if (GDVars.dsPotentialTargetCreature[pick] > 0)
                                    found = true;
                            if (found)
                                found = false;
                            else
                                cnt = 0;
                        }
                    }
                }
                if (!pass)
                    pass = true;
            }
            GDVars.arenaPtr.distMode = 0;
            return found;
        }

        public static void RearClawRake(TCharRecord creature, object p, bool removed)
        {
            bool done = false;
            if (GDVars.hitsMade[1] == 2)
                if (creature.combatAddr.other.enabled != 0)
                {
                    creature.combatAddr.attackPhase = 2;
                    creature.curAttacks2 = 2;
                    creature.curRollsA[2] = 2;
                    creature.curDiceA[2] = 4;
                    creature.curAttacks = 0;
                    PrintNameStringCall(creature, "Rakes...", 10, true);
                    CombatAttack(creature, creature.combatAddr.other, false, null, ref done);
                    done = CharHlp.EndActions(creature);
                }
        }

        public static void BiteAndHold(TCharRecord creature, object p, bool removed)
        {
            bool done = false;
            bool released = false;
            if (Fx.HoldTarget(creature, p, removed, C_Effect.BITE_HOLD, ref released) != null)
            {
                creature.curAttacks = 1;
                creature.curAttacks2 = 0;
                creature.curRollsA[1] = 3;
                creature.curDiceA[1] = 4;
                CombatAttack(creature, GDVars.dsOtherCreatureCurrentPtr, true, null, ref done);
                done = CharHlp.EndActions(creature);
                if (GDVars.dsOtherCreatureCurrentPtr.enabled == 0)
                    Fx.RemoveEffect(creature, C_Effect.BITE_HOLD, p);
            }
        }

        public static bool StingVictoryCheat()
        {
            if (!GDVars.stingArg)
                return false;
            PrintBottomLineCall("The Gods intervene!");
            TCharRecord p = GDVars.creatureList;
            while (p != null)
            {
                if (p.hostile == 1)
                {
                    p.enabled = 0;
                    p.status = C_Status.DEAD;
                    byte slot = Map.CombatSlot(p);
                    // slot 0: not on the map
                    if (slot > 0)
                        GDVars.arenaLoc[slot].size = 0;
                }
                bool done = CharHlp.EndActions(p);
                p = p.nextCharAddr;
            }
            Map.ScrollView(unchecked((sbyte)(GDVars.arenaPtr.ox + 3)), unchecked((sbyte)(GDVars.arenaPtr.oy + 3)), 255, C_Dir.CENTER);
            return true;
        }

        public static bool AreaHitsNonEnemy(sbyte x, sbyte y, byte radius)
        {
            bool result = false;
            BuildTargetListCall(x, y, radius, 255, 1, GDVars.arenaPtr);
            for (int i = 1; i <= GDVars.targetCount; i++)
                if (GDVars.creatureArr[GDVars.targetList[i].idx].hostile != CharHlp.EnemyTeam(GDVars.curChar))
                    result = true;
            return result;
        }
    }
}
