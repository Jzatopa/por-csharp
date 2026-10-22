// Source: Talonius pool-of-radiance pinned 7616920a (src/effhndlr.pas). Direct conversion by Claude (supervising session), on
// James's instruction without Qwen; no Qwen draft exists for this unit. The first draft is kept as
// evidence/ten-b-2026-10-21/EffHndlr/draft.cs and the repairs as supervisor.patch.
//
// Boundaries: FxEngine is not ported. Its routines are reached only through IEffHndlrEffects (EffHndlr.Effects). CbtMap is
// reached through the existing ICbtActsCombatMap seam (EffHndlr.CombatMap); EffHndlrBindings.BindCombatMap shares the
// map CbtMapBindings installed for CbtActs. Each throws NotSupportedException when nothing is installed. Calls into
// converted units with side effects (CharDisp, TextRend, CharHlp.CountTeams/RemoveItem/AppendItemCopy/RecalcStats/
// EndActions, ClassCal.RecalcBaseSkills, Debug.PrintHpDebug, CbtActs.CanAttackTarget, Pathing.WalkLine) go through
// internal delegate fields that default to the converted routine, so tests can observe them. Pure helpers (CharHlp.HasEffect,
// IsNpc, WarriorLevel, EnemyTeam, FindDamagingRangedWeapon, BaseItem, GetCasterLevel, AbilAdj, ClassCal.ConHpTotal,
// StrUtil.NumStr) and Dice are called directly.
//
// Pascal's untyped Pointer p is object: a TEffectRec for an effect node, a TItemRec for the item handlers. A nil p, or a
// nil combatAddr/other where the source dereferences it, throws here (real-mode Pascal would read low memory instead);
// docs/NEXT-EFFHNDLR-2026-10-22.md lists those places.
using System;
using static Por.Core.Types;

namespace Por.Core
{
    /// <summary>The fxengine.pas routines effhndlr.pas calls.</summary>
    public interface IEffHndlrEffects
    {
        void AddEffect(TCharRecord creature, byte effId, ushort minutes, byte strength, bool onRemove);
        void RemoveEffectsHp0(TCharRecord creature);
        void TriggerEffectsWithCondition(byte cond, TCharRecord creature);
        void StrengthEffectToStrength(TEffectRec effect, ref byte baseStr, ref byte excStr);
        byte StrengthModifier(byte baseStr, byte excStr);
        void InflictNonWeaponDamage(TCharRecord creature, byte damage, byte saveReduction, bool saved);
        void RemoveEffect(TCharRecord creature, byte effId, object eff);
        void CallEffectHandler(byte effId, TCharRecord creature, object p, bool removed);
        bool RestoreHitPoints(TCharRecord creature, byte hp, bool bypassRot);
        bool RollSavingThrow(TCharRecord creature, sbyte saveType, sbyte modifier);
        TCharRecord HoldTarget(TCharRecord creature, object p, bool removed, byte effId, ref bool released);
        bool ReviveDeadCreature(TCharRecord creature, byte hp);
        void AddEffectWithMessage(TCharRecord creature, byte effId, ushort minutes, byte strength, bool onRemove, bool saveNegates, bool saved, string msg);
        bool DiseaseImmune(TCharRecord creature, byte effId);
        void PutOutOfPlayWithStatus(TCharRecord creature, byte status, string msg);
        bool AdjustForNewStrength(TCharRecord creature, byte baseStr, byte excStr, ref byte effStrength);
        void InflictItemShock(TCharRecord creature, TItemRec item, byte damage);
    }

    public static class EffHndlr
    {
        // ---- unported-unit seams ----
        public static IEffHndlrEffects Effects;
        public static ICbtActsCombatMap CombatMap;

        private static IEffHndlrEffects Fx
        {
            get { if (Effects == null) throw new NotSupportedException("EffHndlr: FxEngine is not ported; install EffHndlr.Effects."); return Effects; }
        }
        private static ICbtActsCombatMap Map
        {
            get { if (CombatMap == null) throw new NotSupportedException("EffHndlr: CbtMap is not bound; call EffHndlrBindings.BindCombatMap or install EffHndlr.CombatMap."); return CombatMap; }
        }

        // ---- converted-unit call points (default to the converted routine) ----
        internal delegate void AppendItemCopyProc(ref TItemRec list, TItemRec item);
        internal delegate bool WalkLineProc(sbyte x, sbyte y, ref sbyte tx, ref sbyte ty, ref ushort dist, TArena view);

        internal static Action<TCharRecord, string, byte, bool> PrintNameStringCall = CharDisp.PrintNameString;
        internal static Action<TCharRecord, bool, string> PrintNameStringAnimCall = CharDisp.PrintNameStringAnim;
        internal static Action ClearMessageWindowCall = CharDisp.ClearMessageWindow;
        internal static Action<TCharRecord> DisplayPartyCall = CharDisp.DisplayParty;
        internal static Action<TCharRecord> CombatPrintCreatureInfoCall = CharDisp.CombatPrintCreatureInfo;
        internal static Action<string> PrintBottomLineCall = CharDisp.PrintBottomLine;
        internal static Action<TCharRecord> PrintHpRestoreMsgCall = CharDisp.PrintHpRestoreMsg;
        internal static Action<byte> BuildIconFacingsCall = CharDisp.BuildIconFacings;
        internal static Action<sbyte, sbyte, sbyte, sbyte, byte, byte> CombatAnimateMovingIconCall = CharDisp.CombatAnimateMovingIcon;
        internal static Action DisplayCombatInterfaceCall = CharDisp.DisplayCombatInterface;
        internal static Action PauseOnlyCall = TextRend.PauseOnly;
        internal static Action CountTeamsCall = CharHlp.CountTeams;
        internal static Action<TCharRecord, TItemRec> RemoveItemCall = CharHlp.RemoveItem;
        internal static AppendItemCopyProc AppendItemCopyCall = CharHlp.AppendItemCopy;
        internal static Action<TCharRecord> RecalcStatsCall = CharHlp.RecalcStats;
        internal static Func<TCharRecord, bool> EndActionsCall = CharHlp.EndActions;
        internal static Action<TCharRecord> RecalcBaseSkillsCall = ClassCal.RecalcBaseSkills;
        internal static Action<string, byte, byte, byte> PrintHpDebugCall = Debug.PrintHpDebug;
        internal static Func<TCharRecord, TCharRecord, bool> CanAttackTargetCall = CbtActs.CanAttackTarget;
        internal static WalkLineProc WalkLineCall = Pathing.WalkLine;

        // ---- constants ----
        // TBaseItemRec.wpnClass melee damage type. Pierce and bash are distinct bits; slash is zero.
        internal const byte WPN_SLASH = 0;
        internal const byte WPN_PIERCE = 0x01;
        internal const byte WPN_BASH = 0x80;

        // Pascal String[n] assignment
        private static string Str(string s, int n) { return s.Length > n ? s.Substring(0, n) : s; }
        private static byte B(int v) { return unchecked((byte)v); }
        private static sbyte SB(int v) { return unchecked((sbyte)v); }
        private static short S(int v) { return unchecked((short)v); }
        // Types.PEffectRec(p): the cast alone never dereferences, as in Pascal
        private static TEffectRec Eff(object p) { return (TEffectRec)p; }

        // ---- implementation-only routines ----

        // Zero pendingDamage and pendingEffectId: always for effId 0, otherwise when effId is the id under the immunity check.
        internal static void NullifyAttack(byte effId)
        {
            if ((effId == 0) || (GDVars.pendingEffectId == effId))
            {
                GDVars.pendingDamage = 0;
                GDVars.pendingEffectId = 0;
            }
        }

        // FxEngine.AddEffect unless the effect lock is on; strength before minutes, onRemove always True.
        internal static bool AddEffectIfNoLock(TCharRecord creature, byte effId, byte strength, ushort minutes)
        {
            bool result;
            if (GDVars.effectLock != 0)
                result = false;
            else
            {
                result = true;
                Fx.AddEffect(creature, effId, minutes, strength, true);
            }
            return result;
        }

        internal static void InflictStatusMsg(TCharRecord creature, byte status, string msg)
        {
            msg = Str(msg, 40);
            PrintNameStringCall(creature, msg, 10, false);
            switch (creature.status)
            {
                case C_Status.STONED:
                case C_Status.DEAD:
                case C_Status.GONE:
                    return;
            }
            creature.status = status;
            creature.enabled = 0;
            creature.hpCur = 0;
            Fx.RemoveEffectsHp0(creature);
            Fx.TriggerEffectsWithCondition(C_Cond.HP_ZERO, creature);
            if (creature.enabled == 0)
                Map.CreatureDowned(creature);
            PauseOnlyCall();
            ClearMessageWindowCall();
            if (GDVars.ifcState != C_Ifc.COMBAT)
                DisplayPartyCall(GDVars.curChar);
        }

        // ---- handlers ----

        // Bless (eff 1)
        public static void EffBless(TCharRecord creature, object p, bool removed)
        {
            GDVars.dsCurrentMorale = B(GDVars.dsCurrentMorale + 5);
            GDVars.gAttackRoll = SB(GDVars.gAttackRoll + 1);
        }

        // Curse (eff 2)
        public static void EffCurse(TCharRecord creature, object p, bool removed)
        {
            if (GDVars.dsCurrentMorale < 5)
                GDVars.dsCurrentMorale = 0;
            else
                GDVars.dsCurrentMorale = B(GDVars.dsCurrentMorale - 5);
            GDVars.gAttackRoll = SB(GDVars.gAttackRoll - 1);
        }

        // Undead slaying weapon (eff 3)
        public static void EffUndeadSlayer(TCharRecord creature, object p, bool removed)
        {
            GDVars.dsOtherCreatureCurrentPtr = creature.combatAddr.other;
            // undead
            if (GDVars.dsOtherCreatureCurrentPtr.charType == 4)
            {
                GDVars.gAttackRoll = SB(GDVars.gAttackRoll + 2);
                GDVars.pendingDamage = B(GDVars.pendingDamage + 2);
            }
        }

        // Start training with a Manual of Bodily Health (eff 4): add eff 7 for 43200 minutes
        public static void EffTrainStart(TCharRecord creature, object p, bool removed)
        {
            PrintNameStringCall(creature, "starts to train", 10, true);
            Fx.AddEffect(creature, C_Effect.TRAIN_MANUAL, 43200, 255, true);
        }

        // Flame Tongue weapon (eff 6)
        public static void EffFlameTongue(TCharRecord creature, object p, bool removed)
        {
            byte bonus;
            GDVars.dsOtherCreatureCurrentPtr = creature.combatAddr.other;
            switch (GDVars.dsOtherCreatureCurrentPtr.charType)
            {
                // regenerating
                case 10: bonus = 1; break;
                // cold-based / avian
                case 9:
                case 12: bonus = 2; break;
                // undead
                case 4: bonus = 3; break;
                default: bonus = 0; break;
            }
            GDVars.gAttackRoll = SB(GDVars.gAttackRoll + bonus);
            GDVars.pendingDamage = B(GDVars.pendingDamage + bonus);
            // magical fire
            GDVars.elementMask = C_Effect.ELEM_FIRE | C_Effect.ELEM_MAGIC;
        }

        // Manual of Bodily Health training done (eff 7): +1 Constitution and the matching hit points, hpMax capped at 255;
        // at Constitution 20 or more a creature without eff 62 gets it.
        public static void EffTrainManual(TCharRecord creature, object p, bool removed)
        {
            short hpGain;
            TEffectRec effect;
            PrintNameStringCall(creature, "is hardier", 10, true);
            creature.conCur = AbilAdj.ClampScore(S(creature.conCur + 1));
            if ((creature.conCur >= 20) && !CharHlp.HasEffect(creature, C_Effect.CON_REGEN, out effect))
                Fx.AddEffect(creature, C_Effect.CON_REGEN, AbilAdj.ConRegenMinutes(creature.conCur), 255, true);
            // Integer arithmetic; conCur - 1 is passed to a Byte parameter
            hpGain = S(ClassCal.ConHpTotal(creature, creature.conCur) - ClassCal.ConHpTotal(creature, B(creature.conCur - 1)));
            if (hpGain > 255 - creature.hpMax)
                hpGain = S(255 - creature.hpMax);
            if (hpGain > 0)
            {
                creature.hpMax = B(creature.hpMax + hpGain);
                creature.hpCur = B(creature.hpCur + hpGain);
            }
        }

        // Protection from Evil (eff 8, 45)
        public static void EffProtEvil(TCharRecord creature, object p, bool removed)
        {
            if ((GDVars.curChar.alignment == C_Align.LAWFUL_EVIL) ||
                (GDVars.curChar.alignment == C_Align.NEUTRAL_EVIL) ||
                (GDVars.curChar.alignment == C_Align.CHAOTIC_EVIL))
            {
                GDVars.gSaveRoll = S(GDVars.gSaveRoll + 2);
                GDVars.gAttackRoll = SB(GDVars.gAttackRoll - 2);
            }
        }

        // Protection from Good (eff 9, 46)
        public static void EffProtGood(TCharRecord creature, object p, bool removed)
        {
            if ((GDVars.curChar.alignment == C_Align.LAWFUL_GOOD) ||
                (GDVars.curChar.alignment == C_Align.NEUTRAL_GOOD) ||
                (GDVars.curChar.alignment == C_Align.CHAOTIC_GOOD))
            {
                GDVars.gSaveRoll = S(GDVars.gSaveRoll + 2);
                GDVars.gAttackRoll = SB(GDVars.gAttackRoll - 2);
            }
        }

        // Resist Cold (eff 10)
        public static void EffResistCold(TCharRecord creature, object p, bool removed)
        {
            if ((GDVars.elementMask & C_Effect.ELEM_COLD) != 0)
            {
                GDVars.pendingDamage = B(GDVars.pendingDamage / 2);
                GDVars.gSaveRoll = S(GDVars.gSaveRoll + 3);
            }
        }

        // Charm (eff 11). strength: bit 5 = applied mark, bit 6 = saved hostility, bit 7 = charmer side
        public static void EffCharm(TCharRecord creature, object p, bool removed)
        {
            if (removed)
            {
                creature.hostile = SB((Eff(p).strength & 0x40) / 64);
                if (creature.npc == C_Npc.ANIMATED_PC)
                    creature.npc = 0;
            }
            else
            {
                if ((Eff(p).strength & 0x20) == 0)
                {
                    Eff(p).strength = B(Eff(p).strength + 0x20 + creature.hostile * 64);
                    creature.hostile = SB(Eff(p).strength / 128);
                    creature.quickfight = 1;
                    // C_Npc.FLAG clear
                    if (!CharHlp.IsNpc(creature))
                        creature.npc = C_Npc.ANIMATED_PC;
                    creature.combatAddr.other = null;
                    CountTeamsCall();
                }
                GDVars.dsCurrentMorale = 100;
            }
        }

        // Enlarge and Strength (eff 12, 38). Sets Strength to this node's value, then applies the strongest other Enlarge
        // or Strength node, which takes over the Strength to restore. A node whose strength byte is above $7F is ignored.
        public static void EffExtraStr(TCharRecord creature, object p, bool removed)
        {
            // Pascal leaves base and exc unset before the first call; StrengthEffectToStrength writes both
            byte bse = 0;
            byte exc = 0;
            byte bestBase;
            byte bestExc;
            TEffectRec q;
            TEffectRec best;
            if (Eff(p).strength > 0x7F) return;
            Fx.StrengthEffectToStrength(Eff(p), ref bse, ref exc);
            creature.strCur = AbilAdj.ClampScore(bse);
            creature.strExc = exc;
            best = null;
            bestBase = 0;
            bestExc = 0;
            q = creature.effectsAddr;
            while (q != null)
            {
                if ((q.id == C_Effect.ENLARGE) || (q.id == C_Effect.STRENGTH))
                    if (!ReferenceEquals(q, Eff(p)))
                    {
                        Fx.StrengthEffectToStrength(q, ref bse, ref exc);
                        if ((bse > bestBase) || ((bestBase == 18) && (exc > bestExc)))
                        {
                            bestBase = bse;
                            bestExc = exc;
                            best = q;
                        }
                    }
                q = q.next;
            }
            if (best != null)
            {
                best.strength = Fx.StrengthModifier(creature.strCur, creature.strExc);
                creature.strCur = AbilAdj.ClampScore(bestBase);
                creature.strExc = bestExc;
            }
        }

        // Friends (eff 14)
        public static void EffFriends(TCharRecord creature, object p, bool removed)
        {
            creature.chaCur = AbilAdj.ClampScore(Eff(p).strength);
        }

        // Slow Poison periodic damage (eff 15): re-arm for 10 minutes and deal 1 damage while HP > 1
        public static void EffSlowPoisonHit(TCharRecord creature, object p, bool removed)
        {
            if (AddEffectIfNoLock(creature, C_Effect.SLOW_POISON_HIT, Eff(p).strength, 10))
                if (creature.hpCur > 1)
                {
                    GDVars.elementMask = 0;
                    Fx.InflictNonWeaponDamage(creature, 1, 0, false);
                    if (GDVars.ifcState != C_Ifc.COMBAT)
                        DisplayPartyCall(GDVars.curChar);
                }
        }

        // Shield (eff 17)
        public static void EffShield(TCharRecord creature, object p, bool removed)
        {
            // 60-biased AC 3
            if (creature.acCur < 57)
                creature.acCur = 57;
            GDVars.gSaveRoll = S(GDVars.gSaveRoll + 1);
            if (GDVars.dsCurrentSpellBeing == C_Spell.MAGIC_MISSILE)
                GDVars.pendingDamage = 0;
        }

        // Gnome THAC0 bonus (eff 18)
        public static void EffGnomeThac0(TCharRecord creature, object p, bool removed)
        {
            GDVars.dsOtherCreatureCurrentPtr = creature.combatAddr.other;
            if (GDVars.dsOtherCreatureCurrentPtr.charType <= 1)
                if (GDVars.dsOtherCreatureCurrentPtr.iconDimensions == 1)
                    // 5 = High(gnomeFoes)
                    for (int i = 1; i <= 5; i++)
                        if (GDVars.dsOtherCreatureCurrentPtr.name == GDConsts.gnomeFoes[i])
                            GDVars.gAttackRoll = SB(GDVars.gAttackRoll + 1);
        }

        // Resist Fire (eff 20)
        public static void EffResistFire(TCharRecord creature, object p, bool removed)
        {
            if ((GDVars.elementMask & C_Effect.ELEM_FIRE) != 0)
            {
                GDVars.pendingDamage = B(GDVars.pendingDamage / 2);
                GDVars.gSaveRoll = S(GDVars.gSaveRoll + 3);
            }
        }

        // Silence 15' (eff 21)
        public static void EffSilence(TCharRecord creature, object p, bool removed)
        {
            if (creature.combatAddr.itemUseAllowed != 0)
                PrintNameStringCall(creature, "is silenced", 10, true);
            creature.combatAddr.itemUseAllowed = 0;
            creature.combatAddr.castAllowed = 0;
        }

        // Slow Poison expiration (eff 22): a creature still poisoned (eff 55) dies; eff 15 is removed under the effect lock
        public static void EffSlowPoisonEnd(TCharRecord creature, object p, bool removed)
        {
            TEffectRec effect;
            if (CharHlp.HasEffect(creature, C_Effect.POISONED, out effect))
                InflictStatusMsg(creature, C_Status.DEAD, "dies from poison");
            GDVars.effectLock = 1;
            Fx.RemoveEffect(creature, C_Effect.SLOW_POISON_HIT, null);
            GDVars.effectLock = 0;
        }

        // Spiritual Hammer (eff 23). Removal deletes the hammer; otherwise one is created (+1, special = eff 137) when
        // missing and the creature has fewer than 16 items.
        public static void EffSpiritHammer(TCharRecord creature, object p, bool removed)
        {
            TItemRec item;
            byte found;
            item = creature.itemsAddr;
            found = 0;
            while ((item != null) && (found == 0))
                if ((item.itemType == C_Item.HAMMER) && (item.nameIds[3] == C_Item.NAME_SPIRITUAL))
                    found = 1;
                else
                    item = item.next;
            if (removed && (item != null))
                RemoveItemCall(creature, item);
            if (!removed)
                if (found == 0)
                    if (creature.numberOfItems < 16)
                    {
                        // GetMem + FillChar(0): a zeroed record
                        item = new TItemRec();
                        item.itemType = C_Item.HAMMER;
                        item.nameIds[2] = C_Item.NAME_HAMMER;
                        item.nameIds[3] = C_Item.NAME_SPIRITUAL;
                        item.plus = 1;
                        item.effect = C_Effect.SPIRIT_HAMMER;
                        item.special = C_Effect.SPIRIT_HAMMER_GONE;
                        AppendItemCopyCall(ref creature.itemsAddr, item);
                        // FreeMem: the temporary is dropped; the list holds a copy
                        PrintNameStringCall(creature, "Gains an item", 10, true);
                    }
            RecalcStatsCall(creature);
        }

        // Invisibility (eff 25)
        public static void EffInvisible(TCharRecord creature, object p, bool removed)
        {
            TEffectRec effect;
            if (!CharHlp.HasEffect(GDVars.curChar, C_Effect.SEE_INVISIBLE, out effect))
                GDVars.dsCurrentSuppressTargetingFlag = 1;
            GDVars.gAttackRoll = SB(GDVars.gAttackRoll - 4);
        }

        // Dwarf THAC0 bonus (eff 26)
        public static void EffDwarfThac0(TCharRecord creature, object p, bool removed)
        {
            GDVars.dsOtherCreatureCurrentPtr = creature.combatAddr.other;
            if (GDVars.dsOtherCreatureCurrentPtr.charType <= 1)
                if (GDVars.dsOtherCreatureCurrentPtr.iconDimensions == 1)
                    // 11 = High(dwarfFoes)
                    for (int i = 1; i <= 11; i++)
                        if (GDVars.dsOtherCreatureCurrentPtr.name == GDConsts.dwarfFoes[i])
                            GDVars.gAttackRoll = SB(GDVars.gAttackRoll + 1);
        }

        // Mirror Image (eff 28). strength = images left; a single-target spell is absorbed unless 1d(images + 1) rolls 1.
        public static void EffMirrorImage(TCharRecord creature, object p, bool removed)
        {
            if (Dice.RollDice(1, B(Eff(p).strength + 1)) > 1)
                if (GDVars.dsCurrentSpellBeing > 0)
                    if (GDVars.gMultiTarget == 0)
                    {
                        NullifyAttack(0);
                        PrintNameStringCall(creature, "lost an image", 10, true);
                        Eff(p).strength = B(Eff(p).strength - 1);
                        if (Eff(p).strength == 0)
                            Fx.RemoveEffect(creature, C_Effect.MIRROR_IMAGE, null);
                    }
        }

        // Ray of Enfeeblement (eff 29): pendingDamage loses a quarter (rounded down)
        public static void EffEnfeeble(TCharRecord creature, object p, bool removed)
        {
            GDVars.pendingDamage = B(GDVars.pendingDamage - GDVars.pendingDamage / 4);
        }

        // Coughing from Stinking Cloud (eff 30)
        public static void EffStinkCough(TCharRecord creature, object p, bool removed)
        {
            if (creature.combatAddr.itemUseAllowed != 0)
                PrintNameStringCall(creature, "is coughing", 10, true);
            creature.combatAddr.itemUseAllowed = 0;
            creature.combatAddr.castAllowed = 0;
            RecalcStatsCall(creature);
            if (creature.acBehind > 52)
                creature.acBehind = B(creature.acBehind - 2);
            else
                creature.acBehind = 50;
            creature.acCur = creature.acBehind;
            if (creature == GDVars.curChar)
                CombatPrintCreatureInfoCall(creature);
        }

        // Animate Dead (eff 32). Fires on REMOVAL of the animation
        public static void EffAnimateDead(TCharRecord creature, object p, bool removed)
        {
            Eff(p).onRemove = false;
            if (GDVars.effectLock == 0)
                InflictStatusMsg(creature, C_Status.DEAD, "collapses");
            creature.hostile = SB(Eff(p).strength / 16);
            creature.quickfight = 1;
            creature.levelUndead = 0;
            creature.attackLevel = B(CharHlp.WarriorLevel(creature));
            creature.movementBase = 12;
            if (creature.npc == C_Npc.ANIMATED_PC)
                creature.npc = 0;
            creature.charType = 0;
        }

        // Blind (eff 33)
        public static void EffBlind(TCharRecord creature, object p, bool removed)
        {
            GDVars.gAttackRoll = SB(GDVars.gAttackRoll - 4);
            creature.acCur = B(creature.acCur - 4);
            creature.acBehind = B(creature.acBehind - 4);
            GDVars.gSaveRoll = S(GDVars.gSaveRoll - 4);
        }

        // Diseased (eff 34): chains 43 + 44
        public static void EffDiseased(TCharRecord creature, object p, bool removed)
        {
            Fx.CallEffectHandler(C_Effect.DISEASE_STR, creature, p, removed);
            Fx.CallEffectHandler(C_Effect.DISEASE_HP, creature, p, removed);
        }

        // Allied Prayer (eff 35): +1 to saving throws and attack rolls
        public static void EffAlliedPrayer(TCharRecord creature, object p, bool removed)
        {
            GDVars.gSaveRoll = S(GDVars.gSaveRoll + 1);
            GDVars.gAttackRoll = SB(GDVars.gAttackRoll + 1);
        }

        // Bestow Curse (eff 36)
        public static void EffBestowCurse(TCharRecord creature, object p, bool removed)
        {
            GDVars.gAttackRoll = SB(GDVars.gAttackRoll - 4);
            GDVars.gSaveRoll = S(GDVars.gSaveRoll - 4);
        }

        // Blinking (eff 37)
        public static void EffBlinking(TCharRecord creature, object p, bool removed)
        {
            if (creature.combatAddr.initiative > 0)
            {
                GDVars.dsCurrentSuppressTargetingFlag = 1;
                GDVars.gAttackRoll = -1;
            }
        }

        // Hasted (eff 39). Ages the creature one year the first time it fires (strength bit 4), and doubles
        // dsCurrentAttackCurrent
        public static void EffHasted(TCharRecord creature, object p, bool removed)
        {
            if ((Eff(p).strength & 0x10) == 0)
            {
                Eff(p).strength = B(Eff(p).strength + 0x10);
                PrintNameStringCall(creature, "ages", 10, true);
                creature.age = unchecked((ushort)(creature.age + 1));
            }
            GDVars.dsCurrentAttackCurrent = B(GDVars.dsCurrentAttackCurrent * 2);
        }

        // Cast Stinking Cloud (eff 40). Removes the cloud this caster is responsible for (strength upper nibble = its idx),
        // restores or corpse-marks its cells, unlinks the node, then repaints every remaining cloud
        public static void EffCastStink(TCharRecord creature, object p, bool removed)
        {
            TStinkRec q;
            TStinkRec node;
            byte found;
            byte slotId;
            byte xx;
            byte yy;
            slotId = B(Eff(p).strength / 16);
            found = 0;
            node = GDVars.stinkHead;
            while ((node != null) && (found == 0))
                if ((node.caster == creature) && (node.idx == slotId))
                    found = 1;
                else
                    node = node.next;
            if (found == 0) return;
            PrintBottomLineCall("The air clears a little...");
            // 4 = High(TStinkRec.exists)
            for (int i = 1; i <= 4; i++)
                if (node.exists[i] != 0)
                {
                    xx = B(node.ulX + GDConsts.dirDX[GDConsts.cloudDir[i]]);
                    yy = B(node.ulY + GDConsts.dirDY[GDConsts.cloudDir[i]]);
                    found = 0;
                    // the Pascal loop reuses slotId as its counter
                    for (int s = 1; s <= GDVars.downedCount; s++)
                        if (GDVars.downed[s].cr != null)
                            if (GDVars.downed[s].x == xx)
                                if (GDVars.downed[s].y == yy)
                                    found = 1;
                    if (found != 0)
                        GDVars.arenaPtr.cells[yy, xx] = C_Terr.CORPSE;
                    else
                        GDVars.arenaPtr.cells[yy, xx] = node.under[i];
                }
            q = GDVars.stinkHead;
            if (q == node)
                GDVars.stinkHead = q.next;
            else
                while (q.next != node)
                    q = q.next;
            q.next = node.next;
            // FreeMem(node): the node is dropped
            q = GDVars.stinkHead;
            while (q != null)
            {
                // 4 = High(TStinkRec.exists); the index is the unnarrowed Integer sum here
                for (int i = 1; i <= 4; i++)
                    if (q.exists[i] != 0)
                        GDVars.arenaPtr.cells[q.ulY + GDConsts.dirDY[GDConsts.cloudDir[i]],
                                              q.ulX + GDConsts.dirDX[GDConsts.cloudDir[i]]] = C_Terr.STINK_CLOUD;
                q = q.next;
            }
        }

        // Avoid the attack with pct% chance, only when the attacker (curChar) has a weapon equipped and is more than one
        // cell away: zero the damage, force a miss and take the hit back off hitsMade for the attacker's attack phase.
        internal static void AvoidRanged(TCharRecord creature, byte pct)
        {
            byte phase;
            if (GDVars.curChar.equipped[C_Equip.WEAPON] != null)
                if (Map.CalcDistance(GDVars.curChar, creature) > 1)
                    if (Dice.RollDice(1, 100) <= pct)
                    {
                        PrintNameStringCall(creature, "Avoids it", 10, true);
                        GDVars.pendingDamage = 0;
                        GDVars.gAttackRoll = -1;
                        phase = GDVars.curChar.combatAddr.attackPhase;
                        // 1..2 = the bounds of hitsMade
                        if ((phase >= 1) && (phase <= 2))
                            if (GDVars.hitsMade[phase] > 0)
                                GDVars.hitsMade[phase] = B(GDVars.hitsMade[phase] - 1);
                    }
        }

        // The item that deals damage: CharHlp.FindDamagingRangedWeapon's pick, otherwise the equipped weapon; nil when no
        // weapon is equipped
        internal static TItemRec FindDamagingWeapon(TCharRecord creature)
        {
            TItemRec sel;
            bool ok;
            sel = null;
            if (creature.equipped[C_Equip.WEAPON] != null)
            {
                ok = CharHlp.FindDamagingRangedWeapon(creature, ref sel);
                if ((ok == false) || (sel == null))
                    sel = creature.equipped[C_Equip.WEAPON];
            }
            return sel;
        }

        // Protection from Normal Missiles (eff 41)
        public static void EffProtMissiles(TCharRecord creature, object p, bool removed)
        {
            if (FindDamagingWeapon(GDVars.curChar) != null)
                if (FindDamagingWeapon(GDVars.curChar).plus == 0)
                    AvoidRanged(creature, 100);
        }

        // Slowed (eff 42)
        public static void EffSlowed(TCharRecord creature, object p, bool removed)
        {
            GDVars.dsCurrentAttackCurrent = B(GDVars.dsCurrentAttackCurrent / 2);
        }

        // Disease Strength drain (eff 43): re-arm for 60 minutes and lose 1 Strength; at Strength 3 become helpless
        public static void EffDiseaseStr(TCharRecord creature, object p, bool removed)
        {
            TEffectRec effect;
            if (AddEffectIfNoLock(creature, C_Effect.DISEASE_STR, Eff(p).strength, 60))
            {
                if (creature.strCur > 3)
                {
                    PrintNameStringCall(creature, "is weakened", 10, true);
                    creature.strCur = B(creature.strCur - 1);
                }
                else if (!CharHlp.HasEffect(creature, C_Effect.HELPLESS, out effect))
                    Fx.AddEffect(creature, C_Effect.HELPLESS, 0, 255, false);
            }
        }

        // Disease hit point drain (eff 44): re-arm for 10 minutes and deal 1 damage; at 1 HP become helpless
        public static void EffDiseaseHp(TCharRecord creature, object p, bool removed)
        {
            TEffectRec effect;
            if (AddEffectIfNoLock(creature, C_Effect.DISEASE_HP, Eff(p).strength, 10))
            {
                if (creature.hpCur > 1)
                {
                    GDVars.elementMask = 0;
                    Fx.InflictNonWeaponDamage(creature, 1, 0, false);
                    if (GDVars.ifcState != C_Ifc.COMBAT)
                        DisplayPartyCall(GDVars.curChar);
                }
                else if (!CharHlp.HasEffect(creature, C_Effect.HELPLESS, out effect))
                    Fx.AddEffect(creature, C_Effect.HELPLESS, 0, 255, false);
            }
        }

        // Dwarf/gnome AC bonus vs giants (eff 47). The attacker is curChar: its name, type and size are tested.
        public static void EffDwarfAcGiant(TCharRecord creature, object p, bool removed)
        {
            byte ct = GDVars.curChar.charType;
            if ((ct == 0) || (ct == 1) || (ct == 2) || (ct == 10))
            {
                int dim = GDVars.curChar.iconDimensions & 0x7F;
                if ((dim == 1) || (dim == 2))
                    // SizeOf(giantFoes) div SizeOf(giantFoes[1]) = 5
                    for (int i = 1; i <= GDConsts.giantFoes.Length - 1; i++)
                        if (GDVars.curChar.name == GDConsts.giantFoes[i])
                            GDVars.gAttackRoll = SB(GDVars.gAttackRoll - 4);
            }
        }

        // Gnome AC bonus vs bugbears/gnolls (eff 48)
        public static void EffGnomeAcBug(TCharRecord creature, object p, bool removed)
        {
            if (GDVars.curChar.charType == 1)
                if ((GDVars.curChar.name == "BUGBEAR") || (GDVars.curChar.name == "GNOLL"))
                    GDVars.gAttackRoll = SB(GDVars.gAttackRoll - 4);
        }

        // Prayer (eff 49). strength bit 4 = the caster's side: that side gets EffAlliedPrayer, all others -1 to both rolls
        public static void EffPrayer(TCharRecord creature, object p, bool removed)
        {
            byte side;
            side = B((Eff(p).strength & 0x10) / 16);
            if (B(creature.hostile) == side)
                EffAlliedPrayer(creature, p, removed);
            else
            {
                GDVars.gAttackRoll = SB(GDVars.gAttackRoll - 1);
                GDVars.gSaveRoll = S(GDVars.gSaveRoll - 1);
            }
        }

        // Mummy rot healing prevention (eff 50): re-arms itself for 14400 minutes, then restores 1 HP (bypassing the rot block)
        public static void EffRotNoHeal(TCharRecord creature, object p, bool removed)
        {
            if (AddEffectIfNoLock(creature, C_Effect.ROT_NO_HEAL, Eff(p).strength, 14400))
                if (Fx.RestoreHitPoints(creature, 1, true))
                    PrintHpRestoreMsgCall(creature);
        }

        // Helpless (eff 31, 51, 52, 53)
        public static void EffHelpless(TCharRecord creature, object p, bool removed)
        {
            EndActionsCall(creature);
        }

        // Wearing Ring of Invisibility (eff 56)
        public static void EffRingInvis(TCharRecord creature, object p, bool removed)
        {
            Fx.AddEffect(creature, C_Effect.INVISIBLE, 1, 12, false);
        }

        // Mummy rot degeneration (eff 57). strength upper nibble = the countdown. Each firing costs 2 Charisma; the
        // creature re-arms for 43200 minutes while the count exceeds 1, then dies
        public static void EffMummyRot(TCharRecord creature, object p, bool removed)
        {
            if (GDVars.effectLock != 0) return;
            PrintNameStringCall(creature, "Rots...", 10, true);
            creature.chaCur = AbilAdj.ClampScore(S(creature.chaCur - 2));
            if (Eff(p).strength / 16 > 1)
            {
                Eff(p).strength = B(Eff(p).strength - 0x10);
                AddEffectIfNoLock(creature, C_Effect.MUMMY_ROT, Eff(p).strength, 43200);
            }
            else
                InflictStatusMsg(creature, C_Status.GONE, "Dies rots away");
        }

        // Immobile (eff 58)
        public static void EffImmobile(TCharRecord creature, object p, bool removed)
        {
            creature.combatAddr.movement = 0;
            if (GDVars.dsCurrentCreatureCalculatingFlag != 0)
                GDVars.dsCurrentAttackCurrent = 0;
        }

        // Troll: add regeneration (eff 59): add eff 98 permanently
        public static void EffTrollRegen(TCharRecord creature, object p, bool removed)
        {
            Fx.AddEffect(creature, C_Effect.REGEN3, 0, 255, false);
        }

        // Ring of Fire Resistance (eff 61): against fire, -2 damage per die (minimum 1 per die, never more than the damage
        // was) and +4 to save; non-magical fire is nullified
        public static void EffRingFireRes(TCharRecord creature, object p, bool removed)
        {
            if ((GDVars.elementMask & C_Effect.ELEM_FIRE) != 0)
            {
                for (int i = 1; i <= GDVars.gDamageDice; i++)
                    if (GDVars.pendingDamage >= GDVars.gDamageDice + 2)
                        GDVars.pendingDamage = B(GDVars.pendingDamage - 2);
                    else if (GDVars.pendingDamage > GDVars.gDamageDice)
                        GDVars.pendingDamage = GDVars.gDamageDice;
                GDVars.gSaveRoll = S(GDVars.gSaveRoll + 4);
                if ((GDVars.elementMask & C_Effect.ELEM_MAGIC) == 0)
                    NullifyAttack(0);
            }
        }

        // Regenerating from high Con (eff 62): re-arm for the Constitution's interval (60 once it has fallen below 20) and
        // restore 1 HP
        public static void EffConRegen(TCharRecord creature, object p, bool removed)
        {
            ushort minutes;
            minutes = AbilAdj.ConRegenMinutes(creature.conCur);
            if (minutes == 0)
                minutes = 60;
            if (AddEffectIfNoLock(creature, C_Effect.CON_REGEN, Eff(p).strength, minutes))
                if (Fx.RestoreHitPoints(creature, 1, true))
                    PrintHpRestoreMsgCall(creature);
        }

        // Poison the attacker's combat target unless it saves vs. paralyzation/poison with saveMod and its Constitution's
        // poison bonus; it gets eff 55 and is killed.
        internal static void PoisonTouch(TCharRecord creature, sbyte saveMod)
        {
            GDVars.dsOtherCreatureCurrentPtr = creature.combatAddr.other;
            if (!Fx.RollSavingThrow(GDVars.dsOtherCreatureCurrentPtr, C_Save.PARALYZE,
                                    SB(saveMod + AbilAdj.ConPoisonSave(GDVars.dsOtherCreatureCurrentPtr.conCur))))
            {
                PrintNameStringCall(GDVars.dsOtherCreatureCurrentPtr, "is Poisoned", 10, false);
                PauseOnlyCall();
                Fx.AddEffect(GDVars.dsOtherCreatureCurrentPtr, C_Effect.POISONED, 0, 255, false);
                InflictStatusMsg(GDVars.dsOtherCreatureCurrentPtr, C_Status.DEAD, "is killed");
            }
        }

        // Paralyze the attacker's combat target for minutes unless it saves vs. paralyzation; saveMod is unused.
        internal static void ParalysisTouch(TCharRecord creature, ushort minutes, sbyte saveMod)
        {
            GDVars.dsOtherCreatureCurrentPtr = creature.combatAddr.other;
            if (!Fx.RollSavingThrow(GDVars.dsOtherCreatureCurrentPtr, C_Save.PARALYZE, 0))
            {
                PrintNameStringAnimCall(GDVars.dsOtherCreatureCurrentPtr, true, "is Paralyzed");
                Fx.AddEffect(GDVars.dsOtherCreatureCurrentPtr, C_Effect.PARALYSIS, minutes, 12, false);
            }
        }

        // Melee poison (eff 64): the target saves at +0
        public static void EffPoisonHit0(TCharRecord creature, object p, bool removed) { PoisonTouch(creature, 0); }

        // Melee poison, +4 save (eff 65)
        public static void EffPoisonHit4(TCharRecord creature, object p, bool removed) { PoisonTouch(creature, 4); }

        // Melee poison, +2 save (eff 66)
        public static void EffPoisonHit2(TCharRecord creature, object p, bool removed) { PoisonTouch(creature, 2); }

        // Thri-kreen paralysis (eff 67): 2d8 minutes
        public static void EffKreenPara(TCharRecord creature, object p, bool removed)
        {
            ParalysisTouch(creature, Dice.RollDice(2, 8), 0);
        }

        // Ghoul paralysis (eff 68): 63 minutes, not vs. elves
        public static void EffGhoulPara(TCharRecord creature, object p, bool removed)
        {
            if (creature.combatAddr.other.race != C_Race.ELF)
                ParalysisTouch(creature, 63, 0);
        }

        // Drider paralysis (eff 69): 1d9+10 minutes. The -2 save modifier is passed but ParalysisTouch ignores it
        public static void EffDriderPara(TCharRecord creature, object p, bool removed)
        {
            ParalysisTouch(creature, unchecked((ushort)(Dice.RollDice(1, 9) + 10)), -2);
        }

        // Melee poison, -2 save (eff 70)
        public static void EffPoisonHitM2(TCharRecord creature, object p, bool removed) { PoisonTouch(creature, -2); }

        // Dust of Disappearance (eff 71)
        public static void EffDustDisappear(TCharRecord creature, object p, bool removed)
        {
            GDVars.dsCurrentSuppressTargetingFlag = 1;
            GDVars.gAttackRoll = SB(GDVars.gAttackRoll - 4);
        }

        // Camouflaged / Cloak of Elvenkind (eff 72): 95% chance of invisibility (eff 25) for 1 minute
        public static void EffCamouflage(TCharRecord creature, object p, bool removed)
        {
            if (Dice.RollDice(1, 100) <= 95)
                Fx.AddEffect(creature, C_Effect.INVISIBLE, 1, 12, false);
        }

        // Blood drain (eff 75). strength = victim's creature index; duration's upper bits = the x16 damage budget.
        public static void EffBloodDrain(TCharRecord creature, object p, bool removed)
        {
            bool done;
            byte dmg;
            // Pascal leaves released unset; HoldTarget writes it first
            bool released = false;
            if (Fx.HoldTarget(creature, p, removed, C_Effect.BLOOD_DRAIN, ref released) == null)
            {
                if (released && (creature.enabled != 0))
                    if (GDVars.dsOtherCreatureCurrentPtr.enabled != 0)
                        creature.combatAddr.turned = 1;
            }
            else
            {
                PrintNameStringCall(creature, "Sucks some Blood", 10, true);
                dmg = Dice.RollDamageDice(1, 4);
                GDVars.elementMask = 0;
                Fx.InflictNonWeaponDamage(GDVars.dsOtherCreatureCurrentPtr, dmg, 0, false);
                done = EndActionsCall(creature);
                if ((GDVars.dsOtherCreatureCurrentPtr.enabled != 0) && (Eff(p).duration / 16 > dmg))
                    Eff(p).duration = unchecked((ushort)(Eff(p).duration - dmg * 16));
                else
                    Fx.RemoveEffect(creature, C_Effect.BLOOD_DRAIN, p);
            }
        }

        // Blood drain attachment (eff 76): add eff 75 on the attacker with a 12 hp budget and immobilize the target.
        public static void EffBloodAttach(TCharRecord creature, object p, bool removed)
        {
            byte slot;
            GDVars.dsOtherCreatureCurrentPtr = creature.combatAddr.other;
            if (GDVars.dsOtherCreatureCurrentPtr == null)
                return;
            slot = Map.CombatSlot(GDVars.dsOtherCreatureCurrentPtr);
            if (slot == 0)
                return;
            // a 12 hp budget in the upper bits of the duration
            Fx.AddEffect(creature, C_Effect.BLOOD_DRAIN, 12 * 16 + 15, slot, true);
            Fx.AddEffect(GDVars.dsOtherCreatureCurrentPtr, C_Effect.IMMOBILE, 0, 255, false);
            Fx.CallEffectHandler(C_Effect.IMMOBILE, GDVars.dsOtherCreatureCurrentPtr, null, false);
        }

        // Bite attach (eff 77): add eff 74 on the biter and immobilize the target; not vs. Strength 19+ or no combat slot
        public static void EffBiteHold(TCharRecord creature, object p, bool removed)
        {
            byte slot;
            GDVars.dsOtherCreatureCurrentPtr = creature.combatAddr.other;
            if (GDVars.dsOtherCreatureCurrentPtr == null)
                return;
            slot = Map.CombatSlot(GDVars.dsOtherCreatureCurrentPtr);
            if (slot == 0)
                return;
            if (GDVars.dsOtherCreatureCurrentPtr.strCur < 19)
            {
                Fx.AddEffect(creature, C_Effect.BITE_HOLD, 0, slot, true);
                Fx.AddEffect(GDVars.dsOtherCreatureCurrentPtr, C_Effect.IMMOBILE, 0, 255, false);
                Fx.CallEffectHandler(C_Effect.IMMOBILE, GDVars.dsOtherCreatureCurrentPtr, null, false);
                PrintNameStringCall(GDVars.dsOtherCreatureCurrentPtr, "is held fast", 10, true);
            }
        }

        // Healed from unconscious (eff 78): revive at the current HP, or retry in 1 minute if that fails
        public static void EffHealWake(TCharRecord creature, object p, bool removed)
        {
            bool dummy;
            if (!Fx.ReviveDeadCreature(creature, creature.hpCur))
                dummy = AddEffectIfNoLock(creature, C_Effect.HEAL_WAKE, Eff(p).strength, 1);
        }

        // Melee fire touch (eff 79): +2d10 fire
        public static void EffFireTouch(TCharRecord creature, object p, bool removed)
        {
            GDVars.elementMask = C_Effect.ELEM_FIRE | C_Effect.ELEM_MAGIC;
            Fx.InflictNonWeaponDamage(creature.combatAddr.other, Dice.RollDamageDice(2, 10), 0, false);
        }

        // Melee acid squirt (eff 80): +1d4 acid
        public static void EffAcidSquirt(TCharRecord creature, object p, bool removed)
        {
            GDVars.elementMask = C_Effect.ELEM_ACID;
            Fx.InflictNonWeaponDamage(creature.combatAddr.other, Dice.RollDamageDice(1, 4), 0, false);
        }

        // Dragon fear aura (eff 81): each enabled enemy saves vs. spell or, by highest level, is paralyzed (0..3) or
        // cursed (4, 5); level 6+ is unaffected
        public static void EffDragonFear(TCharRecord creature, object p, bool removed)
        {
            TCharRecord q;
            bool saved;
            q = GDVars.creatureList;
            while (q != null)
            {
                if (q.enabled != 0)
                    if (CharHlp.EnemyTeam(creature) == q.hostile)
                    {
                        saved = Fx.RollSavingThrow(q, C_Save.SPELL, 0);
                        switch (q.levelHighest)
                        {
                            case 0:
                            case 1:
                            case 2:
                            case 3:
                                Fx.AddEffectWithMessage(q, C_Effect.PARALYSIS, 0, 12, false, true, saved, "is paralyzed with fear");
                                break;
                            case 4:
                            case 5:
                                Fx.AddEffectWithMessage(q, C_Effect.CURSE, 0, 12, false, true, saved, "is weakened by fear");
                                break;
                        }
                    }
                q = q.nextCharAddr;
            }
        }

        // Mummy fear aura (eff 82). Removes an eff-82 node from EVERY creature; each enabled enemy then saves vs.
        // paralyzation (+1 if its team has 6+ members, +2 if human) or is paralyzed with fear for 1d4 minutes
        public static void EffMummyFear(TCharRecord creature, object p, bool removed)
        {
            TCharRecord q;
            byte bonus;
            bool saved;
            q = GDVars.creatureList;
            while (q != null)
            {
                Fx.RemoveEffect(q, C_Effect.MUMMY_FEAR, null);
                if (q.enabled != 0)
                    if (CharHlp.EnemyTeam(creature) == q.hostile)
                    {
                        bonus = 0;
                        if (GDVars.teamCount[q.hostile] >= 6)
                            bonus = B(bonus + 1);
                        if (q.race == C_Race.HUMAN)
                            bonus = B(bonus + 2);
                        saved = Fx.RollSavingThrow(q, C_Save.PARALYZE, SB(bonus));
                        Fx.AddEffectWithMessage(q, C_Effect.PARALYSIS, Dice.RollDice(1, 4), 12, false, true, saved,
                                                "is paralyzed with fear");
                    }
                q = q.nextCharAddr;
            }
        }

        // Petrifying gaze (eff 83): the target saves vs. petrification or is stoned. Effect 127 on the gazer marks the gaze
        // reflectable: an equipped "Mirror" item turns it back on the gazer
        public static void EffPetrifyGaze(TCharRecord creature, object p, bool removed)
        {
            TItemRec item;
            bool reflected;
            TEffectRec effect;
            GDVars.dsOtherCreatureCurrentPtr = creature.combatAddr.other;
            PrintNameStringCall(creature, "gazes...", 10, false);
            BuildIconFacingsCall(18);
            CombatAnimateMovingIconCall(Map.CombatCellX(creature), Map.CombatCellY(creature),
                                        Map.CombatCellX(GDVars.dsOtherCreatureCurrentPtr),
                                        Map.CombatCellY(GDVars.dsOtherCreatureCurrentPtr), 4, 45);
            if (CharHlp.HasEffect(creature, C_Effect.GAZE_REFLECTABLE, out effect))
            {
                reflected = false;
                item = GDVars.dsOtherCreatureCurrentPtr.itemsAddr;
                while ((item != null) && (reflected == false))
                {
                    if (item.equipped != 0)
                        // "Mirror"
                        if ((item.nameIds[1] == C_Item.NAME_MIRROR) || (item.nameIds[2] == C_Item.NAME_MIRROR) ||
                            (item.nameIds[3] == C_Item.NAME_MIRROR))
                        {
                            PrintNameStringCall(GDVars.dsOtherCreatureCurrentPtr, "reflects it!", 12, false);
                            CombatAnimateMovingIconCall(Map.CombatCellX(GDVars.dsOtherCreatureCurrentPtr),
                                                        Map.CombatCellY(GDVars.dsOtherCreatureCurrentPtr),
                                                        Map.CombatCellX(creature), Map.CombatCellY(creature), 4, 45);
                            reflected = true;
                            GDVars.dsOtherCreatureCurrentPtr = creature;
                        }
                    item = item.next;
                }
            }
            if (!Fx.RollSavingThrow(GDVars.dsOtherCreatureCurrentPtr, C_Save.PETRIFY, 0))
                InflictStatusMsg(GDVars.dsOtherCreatureCurrentPtr, C_Status.STONED, "is Stoned");
        }

        // Charming gaze (eff 84): with a clear line to an attackable target within Charm Person's range, apply Charm Person
        // (save vs. spell at -2). The spell being cast is Charm Person only while the charm is applied, then restored.
        public static void EffCharmGaze(TCharRecord creature, object p, bool removed)
        {
            ushort dist;
            TEffectRec effect;
            sbyte x;
            sbyte y;
            byte savedSpell;
            GDVars.dsOtherCreatureCurrentPtr = creature.combatAddr.other;
            x = Map.CombatCellX(GDVars.dsOtherCreatureCurrentPtr);
            y = Map.CombatCellY(GDVars.dsOtherCreatureCurrentPtr);
            if (!CanAttackTargetCall(creature, GDVars.dsOtherCreatureCurrentPtr)) return;
            // the range budget in cells, read by WalkLine on entry
            dist = GDConsts.spellTable[C_Spell.CHARM_PERSON].rangeFixed;
            if (!WalkLineCall(Map.CombatCellX(creature), Map.CombatCellY(creature), ref x, ref y, ref dist,
                              GDVars.arenaPtr)) return;
            PrintNameStringCall(creature, "Gazes...", 10, false);
            BuildIconFacingsCall(18);
            CombatAnimateMovingIconCall(Map.CombatCellX(creature), Map.CombatCellY(creature),
                                        Map.CombatCellX(GDVars.dsOtherCreatureCurrentPtr),
                                        Map.CombatCellY(GDVars.dsOtherCreatureCurrentPtr), 4, 45);
            // Charm Person
            savedSpell = GDVars.dsCurrentSpellBeing;
            GDVars.dsCurrentSpellBeing = C_Spell.CHARM_PERSON;
            Fx.AddEffectWithMessage(GDVars.dsOtherCreatureCurrentPtr, C_Effect.CHARM, 0, B(creature.hostile * 128 + 12),
                                    true, true,
                                    Fx.RollSavingThrow(creature.combatAddr.other, C_Save.SPELL, -2),
                                    "is charmed");
            if (CharHlp.HasEffect(GDVars.dsOtherCreatureCurrentPtr, C_Effect.CHARM, out effect))
                Fx.CallEffectHandler(C_Effect.CHARM, GDVars.dsOtherCreatureCurrentPtr, effect, false);
            GDVars.dsCurrentSpellBeing = savedSpell;
        }

        // Drain levels from a creature; an animated creature, or one with no class levels, is destroyed instead.
        internal static void DrainLevels(TCharRecord creature, byte levels)
        {
            string numStr;
            int xpTry = 0;
            int xpMin = 0;
            byte totalLevels;
            byte hpPerLevel;
            sbyte c;

            // Which class to drain: scanning classes 0..7, take each one whose level is >= the best so far and whose XP
            // table entry for that level is >= the best so far (or is -1); later classes win ties. xpMin is left at the
            // chosen entry. When no class qualifies (every level negative) the Pascal result is undefined; this throws.
            sbyte ClassToDrain()
            {
                sbyte result = 0;
                bool assigned = false;
                sbyte bestLvl;
                sbyte lvl;
                xpMin = 0;
                bestLvl = 0;
                for (sbyte jj = C_Level.CLERIC; jj <= C_Level.MONK; jj++)
                {
                    lvl = creature.classLevels[jj];
                    // xp[] runs to level 9; a level above it reads level 9's entry
                    if (lvl > 9)
                        xpTry = GDConsts.dsExperienceSpellsClass[jj].xp[9];
                    else if (lvl > 1)
                        xpTry = GDConsts.dsExperienceSpellsClass[jj].xp[lvl];
                    else
                        xpTry = 0;
                    if (lvl >= bestLvl)
                        if ((xpTry >= xpMin) || (xpTry == -1))
                        {
                            xpMin = xpTry;
                            result = jj;
                            assigned = true;
                            bestLvl = lvl;
                        }
                }
                if (!assigned)
                    throw new InvalidOperationException("EffHndlr.DrainLevels: no class to drain; the original function result is undefined");
                return result;
            }

            if (levels > 1)
                numStr = Str(StrUtil.NumStr(levels) + " levels", 40);
            else
                numStr = "a level";
            PrintNameStringCall(creature, "is drained " + numStr, 10, true);
            if (creature.status == C_Status.ANIMATED)
            {
                InflictStatusMsg(creature, C_Status.GONE, "is dead forever");
                creature.hpMax = 0;
                return;
            }
            for (int i = 1; i <= levels; i++)
            {
                if (creature.status != C_Status.GONE)
                {
                    creature.drainedLevels = B(creature.drainedLevels + 1);
                    totalLevels = 0;
                    for (int j = C_Level.CLERIC; j <= C_Level.MONK; j++)
                        totalLevels = B(totalLevels + creature.classLevels[j]);
                    // a creature with no class levels has no level to lose, and no divisor
                    if (totalLevels == 0)
                    {
                        InflictStatusMsg(creature, C_Status.GONE, "is dead forever");
                        creature.hpMax = 0;
                        return;
                    }
                    hpPerLevel = B(creature.hpMax / totalLevels);
                    if (GDVars.stingHpFlag != 0)
                        PrintHpDebugCall("max,cur,lost", creature.hpMax, creature.hpCur, creature.drainedHps);
                    for (int k = 1; k <= hpPerLevel; k++)
                    {
                        if (creature.hpMax > 0)
                            creature.hpMax = B(creature.hpMax - 1);
                        if (creature.hpRolled > 0)
                            creature.hpRolled = B(creature.hpRolled - 1);
                        if (creature.hpCur > 0)
                            creature.hpCur = B(creature.hpCur - 1);
                        creature.drainedHps = B(creature.drainedHps + 1);
                    }
                    if (creature.levelHighest < 2)
                    {
                        InflictStatusMsg(creature, C_Status.GONE, "is dead forever");
                        creature.hpMax = 0;
                        if (creature.classLevels[ClassToDrain()] > 0)
                            if (levels == 2)
                                creature.classLevels[ClassToDrain()] = 0;
                    }
                    else
                    {
                        c = ClassToDrain();
                        creature.classLevels[c] = SB(creature.classLevels[c] - 1);
                        if (creature.hpCur == 0)
                            if (creature.status != C_Status.DEAD)
                                InflictStatusMsg(creature, C_Status.DEAD, "is killed");
                        RecalcBaseSkillsCall(creature);
                    }
                    creature.levelHighest = creature.classLevels[ClassToDrain()];
                    creature.experience = xpMin;
                    RecalcStatsCall(creature);
                }
            }
        }

        // Melee level drain (eff 85): the target loses one level
        public static void EffDrainOne(TCharRecord creature, object p, bool removed)
        {
            DrainLevels(creature.combatAddr.other, 1);
        }

        // Melee level drain (eff 86): the target loses two levels
        public static void EffDrainTwo(TCharRecord creature, object p, bool removed)
        {
            DrainLevels(creature.combatAddr.other, 2);
        }

        // Melee mummy rot (eff 87): unless the target has eff 50 or is immune to disease, add eff 50 and eff 57, seeding
        // eff 57's 1d6 countdown in the upper nibble
        public static void EffRotTouch(TCharRecord creature, object p, bool removed)
        {
            TEffectRec effect;
            GDVars.dsOtherCreatureCurrentPtr = creature.combatAddr.other;
            if (Fx.DiseaseImmune(GDVars.dsOtherCreatureCurrentPtr, C_Effect.ROT_NO_HEAL))
                return;
            if (!CharHlp.HasEffect(GDVars.dsOtherCreatureCurrentPtr, C_Effect.ROT_NO_HEAL, out effect))
            {
                Fx.AddEffect(GDVars.dsOtherCreatureCurrentPtr, C_Effect.ROT_NO_HEAL, 14400, 255, true);
                Fx.AddEffect(GDVars.dsOtherCreatureCurrentPtr, C_Effect.MUMMY_ROT, 43200,
                             B(Dice.RollDice(1, 6) * 16 + 15), true);
                PrintNameStringCall(GDVars.dsOtherCreatureCurrentPtr, "is Diseased", 10, true);
            }
        }

        // Displaced (eff 89). strength bit 4 = "already displaced an attack this round"
        public static void EffDisplaced(TCharRecord creature, object p, bool removed)
        {
            if ((GDVars.roundCounter == 0) && (GDVars.gAttackRoll == 0))
                Eff(p).strength = B(Eff(p).strength & 0xF);
            else if ((Eff(p).strength & 0x10) == 0)
            {
                GDVars.gAttackRoll = -1;
                Eff(p).strength = B(Eff(p).strength | 0x10);
            }
        }

        // Con poison saving throw bonus (eff 90)
        public static void EffConPsnSave(TCharRecord creature, object p, bool removed)
        {
            if (GDVars.gSaveType == C_Save.PARALYZE)
                GDVars.gSaveRoll = S(GDVars.gSaveRoll + AbilAdj.ConSaveBonus(creature.conCur));
        }

        // Immunity to electricity + Magic Missile (eff 91)
        public static void EffImmuneMM(TCharRecord creature, object p, bool removed)
        {
            if (GDVars.dsCurrentSpellBeing == C_Spell.MAGIC_MISSILE)
            {
                NullifyAttack(0);
                PrintNameStringCall(creature, "is unaffected", 10, true);
            }
            else if ((GDVars.elementMask & C_Effect.ELEM_ELEC) != 0)
                NullifyAttack(0);
        }

        // Half damage from fire (eff 93)
        public static void EffHalfFire(TCharRecord creature, object p, bool removed)
        {
            if ((GDVars.elementMask & C_Effect.ELEM_FIRE) != 0)
                GDVars.pendingDamage = B(GDVars.pendingDamage / 2);
        }

        // Half damage from blunt/piercing (eff 94)
        public static void EffHalfBash(TCharRecord creature, object p, bool removed)
        {
            TItemRec w;
            w = FindDamagingWeapon(GDVars.curChar);
            if (w != null)
                if ((CharHlp.BaseItem(w.itemType).wpnClass & (WPN_PIERCE | WPN_BASH)) != 0)
                    GDVars.pendingDamage = B(GDVars.pendingDamage / 2);
        }

        // Fight on at 0 HP (eff 95): when it expires, a creature still fighting falls dead
        public static void EffFightOn(TCharRecord creature, object p, bool removed)
        {
            Eff(p).onRemove = false;
            if (creature.enabled != 0)
                InflictStatusMsg(creature, C_Status.DEAD, "Falls dead");
        }

        // Immune to weapons neither magical nor silver, and to attacks without a weapon (eff 96)
        public static void EffNeedSilver(TCharRecord creature, object p, bool removed)
        {
            TItemRec w;
            w = FindDamagingWeapon(GDVars.curChar);
            if ((w == null) || ((w.plus == 0) && (w.nameIds[3] != C_Item.NAME_SILVER)))
                GDVars.pendingDamage = 0;
        }

        // Con spell/wand save bonus (eff 97)
        public static void EffConSpellSave(TCharRecord creature, object p, bool removed)
        {
            if ((GDVars.gSaveType == C_Save.SPELL) || (GDVars.gSaveType == C_Save.WAND))
                GDVars.gSaveRoll = S(GDVars.gSaveRoll + AbilAdj.ConSaveBonus(creature.conCur));
        }

        // Regenerate 3 hp per round (eff 98)
        public static void EffRegen3(TCharRecord creature, object p, bool removed)
        {
            // capped before the add (Word arithmetic), so hpCur 253..255 cannot wrap the Byte
            if (creature.hpCur + 3 > creature.hpMax)
                creature.hpCur = creature.hpMax;
            else
                creature.hpCur = B(creature.hpCur + 3);
        }

        // Can fight on (eff 99). Revives a dying (overkill < 6) or unconscious creature and grants eff 95 for 1d4+1
        // minutes, then removes itself
        public static void EffCanFightOn(TCharRecord creature, object p, bool removed)
        {
            byte hp;
            hp = 0;
            if (creature.status == C_Status.DYING)
                if (creature.combatAddr.overkill < 6)
                    hp = B(6 - creature.combatAddr.overkill);
            if (creature.status == C_Status.UNCONSCIOUS)
                hp = 6;
            if (hp > 0)
                if (Fx.ReviveDeadCreature(creature, hp))
                {
                    Fx.AddEffect(creature, C_Effect.FIGHT_ON, unchecked((ushort)(Dice.RollDice(1, 4) + 1)), 255, true);
                    Eff(p).onRemove = false;
                    Fx.RemoveEffect(creature, C_Effect.CAN_FIGHT_ON, p);
                }
        }

        // Troll vulnerability to fire/acid (eff 100): damage neither fire nor acid adds eff 102 (back in 3d6 minutes)
        public static void EffTrollBurn(TCharRecord creature, object p, bool removed)
        {
            if (((GDVars.elementMask & C_Effect.ELEM_FIRE) == 0) && ((GDVars.elementMask & C_Effect.ELEM_ACID) == 0))
                Fx.AddEffect(creature, C_Effect.TROLL_RETURN, Dice.RollDice(3, 6), 255, true);
        }

        // Troll: add regeneration (eff 101): unless it has eff 98 or eff 59 already, add eff 59 for 3 minutes
        public static void EffTrollHeal(TCharRecord creature, object p, bool removed)
        {
            TEffectRec effect;
            if (!CharHlp.HasEffect(creature, C_Effect.REGEN3, out effect))
                if (!CharHlp.HasEffect(creature, C_Effect.TROLL_REGEN, out effect))
                    Fx.AddEffect(creature, C_Effect.TROLL_REGEN, 3, 255, true);
        }

        // Troll returning from going down (eff 102): revive at full HP, or retry in 1 minute if that fails
        public static void EffTrollReturn(TCharRecord creature, object p, bool removed)
        {
            bool dummy;
            if (!Fx.ReviveDeadCreature(creature, creature.hpMax))
                dummy = AddEffectIfNoLock(creature, C_Effect.TROLL_RETURN, Eff(p).strength, 1);
        }

        // Gaseous form (eff 103): the creature escapes and is put out of play as fled
        public static void EffGaseous(TCharRecord creature, object p, bool removed)
        {
            Eff(p).onRemove = false;
            Map.DisplayMapViewCentered(creature, 3, false);
            Fx.PutOutOfPlayWithStatus(creature, C_Status.RUNNING, "Turns gaseous and escapes");
        }

        // Thri-kreen missile evasion (eff 104)
        public static void EffKreenDodge(TCharRecord creature, object p, bool removed)
        {
            AvoidRanged(creature, 60);
        }

        // pct is the magic resistance against an 11th-level caster: chance := pct + (11 - lvl) * 5 within 0..100, and a
        // 1d100 roll <= chance nullifies. Only an effect under the immunity check or magic damage is resisted. An item counts
        // as a caster of at most level 6.
        internal static void MagicResistance(byte pct)
        {
            byte lvl;
            short chance;
            lvl = CharHlp.GetCasterLevel(GDVars.dsCurrentSpellBeing);
            if (GDVars.usingItem && (lvl > 6))
                lvl = 6;
            chance = S(pct + (11 - lvl) * 5);
            if (chance < 0)
                chance = 0;
            else if (chance > 100)
                chance = 100;
            if ((GDVars.pendingEffectId == 0) && ((GDVars.elementMask & C_Effect.ELEM_MAGIC) == 0))
                return;
            if (Dice.RollDice(1, 100) <= chance)
                NullifyAttack(0);
        }

        // Magic resistance 50% (eff 105)
        public static void EffMagicRes50(TCharRecord creature, object p, bool removed) { MagicResistance(50); }

        // Magic resistance 85% (eff 106)
        public static void EffMagicRes85(TCharRecord creature, object p, bool removed) { MagicResistance(85); }

        // Elf 90% resistance to sleep/charm (eff 107)
        public static void EffElfResist(TCharRecord creature, object p, bool removed)
        {
            if (Dice.RollDice(1, 100) <= 90)
            {
                NullifyAttack(C_Effect.SLEEP);
                NullifyAttack(C_Effect.CHARM);
            }
        }

        // Immunity to sleep/charm (eff 108)
        public static void EffNoSleepCharm(TCharRecord creature, object p, bool removed)
        {
            NullifyAttack(C_Effect.CHARM);
            NullifyAttack(C_Effect.SLEEP);
        }

        // Immunity to paralysis (eff 109)
        public static void EffNoParalysis(TCharRecord creature, object p, bool removed)
        {
            NullifyAttack(C_Effect.PARALYSIS);
        }

        // Immunity to cold (eff 110)
        public static void EffNoCold(TCharRecord creature, object p, bool removed)
        {
            if ((GDVars.elementMask & C_Effect.ELEM_COLD) != 0)
                NullifyAttack(0);
        }

        // Immunity to paralysis + poison (eff 111). Saves of type 0 succeed automatically
        public static void EffNoParaPoison(TCharRecord creature, object p, bool removed)
        {
            NullifyAttack(C_Effect.POISONED);
            NullifyAttack(C_Effect.PARALYSIS);
            if (GDVars.gSaveType == C_Save.PARALYZE)
                GDVars.gSaveRoll = 100;
        }

        // Immunity to fire (eff 112)
        public static void EffNoFire(TCharRecord creature, object p, bool removed)
        {
            if ((GDVars.elementMask & C_Effect.ELEM_FIRE) != 0)
                NullifyAttack(0);
        }

        // Efreeti fire resistance (eff 113): -1 per die, minimum = dice count
        public static void EffEfreetiFire(TCharRecord creature, object p, bool removed)
        {
            if ((GDVars.elementMask & C_Effect.ELEM_FIRE) != 0)
                for (int i = 1; i <= GDVars.gDamageDice; i++)
                    // only above the floor, so damage of 0 cannot wrap the Byte to 255
                    if (GDVars.pendingDamage > GDVars.gDamageDice)
                        GDVars.pendingDamage = B(GDVars.pendingDamage - 1);
        }

        // Half damage from electricity (eff 114)
        public static void EffHalfElec(TCharRecord creature, object p, bool removed)
        {
            if ((GDVars.elementMask & C_Effect.ELEM_ELEC) != 0)
                GDVars.pendingDamage = B(GDVars.pendingDamage / 2);
        }

        // Half damage from piercing/slashing (eff 115)
        public static void EffHalfPierce(TCharRecord creature, object p, bool removed)
        {
            TItemRec w;
            w = FindDamagingWeapon(GDVars.curChar);
            if (w != null)
                if ((CharHlp.BaseItem(w.itemType).wpnClass == WPN_SLASH) ||
                    ((CharHlp.BaseItem(w.itemType).wpnClass & WPN_PIERCE) != 0))
                    GDVars.pendingDamage = B(GDVars.pendingDamage / 2);
        }

        // Half damage from magical weapons (eff 116)
        public static void EffHalfMagical(TCharRecord creature, object p, bool removed)
        {
            TItemRec w;
            w = FindDamagingWeapon(GDVars.curChar);
            if (w != null)
                if (w.plus > 0)
                    GDVars.pendingDamage = B(GDVars.pendingDamage / 2);
        }

        // Vulnerability to holy water (eff 117): a holy water vial does 1d6+1
        public static void EffHolyWater(TCharRecord creature, object p, bool removed)
        {
            TItemRec item;
            item = GDVars.curChar.equipped[C_Equip.WEAPON];
            if (item != null)
                if (item.itemType == C_Item.HOLY_WATER_VIAL)
                    GDVars.pendingDamage = B(Dice.RollDamageDice(1, 6) + 1);
        }

        // Half damage from cold (eff 118)
        public static void EffHalfCold(TCharRecord creature, object p, bool removed)
        {
            if ((GDVars.elementMask & C_Effect.ELEM_COLD) != 0)
                GDVars.pendingDamage = B(GDVars.pendingDamage / 2);
        }

        // Immunity to non-magical weapons and unarmed attacks (eff 119): only vs PC races or attackers below level 4
        public static void EffNeedMagic(TCharRecord creature, object p, bool removed)
        {
            TItemRec w;
            w = FindDamagingWeapon(GDVars.curChar);
            if ((w == null) || (w.plus == 0))
                if ((GDVars.curChar.race > C_Race.MONSTER) || (GDVars.curChar.levelHighest < 4))
                    GDVars.pendingDamage = 0;
        }

        // Boulder evasion (eff 120): 50% chance to avoid a thrown C_Item.BOULDER or C_Item.LARGE_BOULDER
        public static void EffBoulderDodge(TCharRecord creature, object p, bool removed)
        {
            TItemRec item;
            item = GDVars.curChar.equipped[C_Equip.WEAPON];
            if (item != null)
                if ((item.itemType == C_Item.BOULDER) || (item.itemType == C_Item.LARGE_BOULDER))
                    AvoidRanged(creature, 50);
        }

        // Anhkheg acid squirt (eff 121): 25% chance, range < 4, 8d4, save vs. breath for half; the effect removes itself
        // (and eff 80) after firing
        public static void EffAnhkhegAcid(TCharRecord creature, object p, bool removed)
        {
            bool dummy;
            GDVars.dsOtherCreatureCurrentPtr = creature.combatAddr.other;
            // other is nil before the anhkheg's first target, and may be a creature since taken out of play
            if ((GDVars.dsOtherCreatureCurrentPtr == null) || (GDVars.dsOtherCreatureCurrentPtr.enabled == 0)) return;
            if (Dice.RollDice(1, 100) > 25) return;
            if (Map.CalcDistance(creature, GDVars.dsOtherCreatureCurrentPtr) >= 4) return;
            dummy = EndActionsCall(creature);
            PrintNameStringCall(creature, "Spits Acid", 10, true);
            BuildIconFacingsCall(23);
            CombatAnimateMovingIconCall(Map.CombatCellX(creature), Map.CombatCellY(creature),
                                        Map.CombatCellX(GDVars.dsOtherCreatureCurrentPtr),
                                        Map.CombatCellY(GDVars.dsOtherCreatureCurrentPtr), 1, 30);
            Fx.InflictNonWeaponDamage(GDVars.dsOtherCreatureCurrentPtr, Dice.RollDamageDice(8, 4), 2,
                                      Fx.RollSavingThrow(GDVars.dsOtherCreatureCurrentPtr, C_Save.BREATH, 0));
            Fx.RemoveEffect(creature, C_Effect.ANHKHEG_ACID, p);
            Fx.RemoveEffect(creature, C_Effect.ACID_SQUIRT, null);
        }

        // Mummy vulnerability to fire (eff 122): a flask of oil does 3d8; magic or fire damage +1 per die
        public static void EffMummyFire(TCharRecord creature, object p, bool removed)
        {
            TItemRec item;
            item = GDVars.curChar.equipped[C_Equip.WEAPON];
            if (item != null)
                if (item.itemType == C_Item.FLASK_OF_OIL)
                    GDVars.pendingDamage = Dice.RollDamageDice(3, 8);
            if ((GDVars.elementMask & (C_Effect.ELEM_FIRE | C_Effect.ELEM_MAGIC)) != 0)
                GDVars.pendingDamage = B(GDVars.pendingDamage + GDVars.gDamageDice);
        }

        // Immune unless silver (half) or magical (full); no damage without a weapon (eff 123)
        public static void EffSilverMagic(TCharRecord creature, object p, bool removed)
        {
            TItemRec w;
            w = FindDamagingWeapon(GDVars.curChar);
            if (w != null)
            {
                if ((w.plus == 0) && (w.nameIds[3] == C_Item.NAME_SILVER))
                    GDVars.pendingDamage = B(GDVars.pendingDamage / 2);
                else if (w.plus == 0)
                    GDVars.pendingDamage = 0;
            }
            else
                GDVars.pendingDamage = 0;
        }

        // Half-elf 30% resistance (eff 124)
        public static void EffHalfElfResist(TCharRecord creature, object p, bool removed)
        {
            if (Dice.RollDice(1, 100) <= 30)
            {
                NullifyAttack(C_Effect.CHARM);
                NullifyAttack(C_Effect.SLEEP);
            }
        }

        // Immunity to sleep/charm/para/poison (eff 125). Saves of type 0 succeed automatically
        public static void EffNoCharmPoison(TCharRecord creature, object p, bool removed)
        {
            NullifyAttack(C_Effect.CHARM);
            NullifyAttack(C_Effect.SLEEP);
            NullifyAttack(C_Effect.PARALYSIS);
            NullifyAttack(C_Effect.POISONED);
            if (GDVars.gSaveType == C_Save.PARALYZE)
                GDVars.gSaveRoll = 100;
        }

        // Suppressible gaze (eff 126): blocked by an equipped "Mirror" component or Wooden Holy Symbol on the target
        public static void EffGazeBlock(TCharRecord creature, object p, bool removed)
        {
            TItemRec item;
            GDVars.dsOtherCreatureCurrentPtr = creature.combatAddr.other;
            item = GDVars.dsOtherCreatureCurrentPtr.itemsAddr;
            while ((item != null) && (GDVars.dsCurrentSuppressTargetingFlag == 0))
            {
                if (item.equipped != 0)
                    // 3 = High(TItemRec.nameIds)
                    for (int j = 1; j <= 3; j++)
                        if (((item.nameIds[j] == C_Item.NAME_HOLY_SYMBOL) && (item.nameIds[3] == C_Item.NAME_WOODEN)) ||
                            (item.nameIds[j] == C_Item.NAME_MIRROR))
                            GDVars.dsCurrentSuppressTargetingFlag = 1;
                item = item.next;
            }
        }

        // Ready item (eff 128, 129, 130, 133, 134, 136, 138, 139): p is the ITEM. Readying adds its effect byte as a
        // permanent effect (strength 12) and calls that handler; unreadying removes it.
        public static void EffReadyItem(TCharRecord creature, object p, bool removed)
        {
            TItemRec item;
            item = (TItemRec)p;
            if (removed)
                Fx.RemoveEffect(creature, item.effect, null);
            else
            {
                Fx.AddEffect(creature, item.effect, 0, 12, false);
                Fx.CallEffectHandler(item.effect, creature, null, false);
            }
        }

        // Gauntlets of Ogre Power (eff 131). Ready: Str 18/00 plus a permanent eff 38 node holding the Strength to restore.
        // Unready: while the creature is still at 18/00, remove the first eff 38 node that is not superseded.
        public static void EffGauntlets(TCharRecord creature, object p, bool removed)
        {
            // Pascal leaves enc unset; AdjustForNewStrength is expected to write it
            byte enc = 0;
            bool isOgre;
            bool found;
            TEffectRec q;
            TEffectRec next;
            if (removed)
            {
                found = false;
                q = creature.effectsAddr;
                if ((creature.strCur == 18) && (creature.strExc == 100))
                    isOgre = true;
                else
                    isOgre = false;
                while ((q != null) && (found == false))
                {
                    // taken before RemoveEffect frees q
                    next = q.next;
                    if ((q.id == C_Effect.STRENGTH) && isOgre && (q.strength < 0x80))
                    {
                        Fx.RemoveEffect(creature, C_Effect.STRENGTH, q);
                        found = true;
                    }
                    q = next;
                }
            }
            else
            {
                if (Fx.AdjustForNewStrength(creature, 18, 100, ref enc))
                {
                    if (GDVars.ifcState == C_Ifc.COMBAT)
                    {
                        GDVars.ifcState = C_Ifc.CAMPING;
                        PrintNameStringCall(creature, "is stronger", 10, true);
                        GDVars.ifcState = C_Ifc.COMBAT;
                    }
                    else
                        PrintNameStringCall(creature, "is stronger", 10, true);
                }
                GDVars.dsRedrawInventoryMenuFlag = 1;
                Fx.AddEffect(creature, C_Effect.STRENGTH, 0, enc, true);
            }
        }

        // Aligned weapon (eff 132). The item's effect byte holds the required alignment in its low nibble; a wielder of
        // another alignment has it unreadied and takes magic damage equal to the upper nibble
        public static void EffAlignedWeapon(TCharRecord creature, object p, bool removed)
        {
            TItemRec item;
            byte dmg;
            item = (TItemRec)p;
            if (removed) return;
            if ((item.effect & 0xF) != creature.alignment)
            {
                item.equipped = 0;
                dmg = B(item.effect / 16);
                GDVars.elementMask = C_Effect.ELEM_MAGIC;
                if (GDVars.ifcState == C_Ifc.COMBAT)
                    DisplayCombatInterfaceCall();
                Fx.InflictItemShock(creature, item, dmg);
                GDVars.dsRedrawInventoryMenuFlag = 1;
            }
        }

        // Boulder (eff 135): readying it without Strength 19+ unreadies it
        public static void EffBoulder(TCharRecord creature, object p, bool removed)
        {
            TItemRec item;
            item = (TItemRec)p;
            if (removed) return;
            if (creature.strCur < 19)
            {
                item.equipped = 0;
                PrintBottomLineCall("Must have Giant Strength");
            }
        }

        // Spiritual Hammer unreadied (eff 137)
        public static void EffHammerGone(TCharRecord creature, object p, bool removed)
        {
            if (removed)
                Fx.RemoveEffect(creature, C_Effect.SPIRIT_HAMMER, null);
        }

        // the do-nothing handler (eff 5, 13, 16, 19, 24, 27, 54, 55, 60, 63, 92, 127)
        public static void EffNull(TCharRecord creature, object p, bool removed)
        {
        }
    }
}
