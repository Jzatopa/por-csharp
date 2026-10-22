// Source: Talonius pool-of-radiance pinned 7616920a (src/magic.pas). Direct conversion by Claude (supervising session), on
// James's instruction without Qwen; no Qwen draft exists for this unit. The first draft is kept as
// evidence/ten-b-2026-10-21/Magic/draft.cs and the repairs as supervisor.patch.
//
// Boundaries: FxEngine is not ported and CbtMap is not yet accepted. Their routines are reached only through IMagicEffects
// (Magic.Effects) and IMagicCombatMap (Magic.CombatMap); each throws NotSupportedException when nothing is installed.
// MagicBindings.BindCombatMap installs an adapter over the CbtMap port. Calls into converted units with side effects
// (CharDisp, TextRend, MenuCore, Border, SoundDrv, CharHlp.RemoveSpell/RemoveItem/RecalcStats/EndActions, Stats.Count,
// ClassCal.RecalcBaseSkills, Pathing.TargetArea) go through internal delegate fields that default to the converted routine,
// so tests can observe them. Pure helpers (CharHlp predicates, GetCasterLevel, Pathing's path walker, AbilAdj.ClampScore)
// and Dice are called directly.
using System;
using static Por.Core.Types;

namespace Por.Core
{
    /// <summary>The fxengine.pas routines magic.pas calls.</summary>
    public interface IMagicEffects
    {
        bool RollSavingThrow(TCharRecord creature, sbyte saveType, sbyte modifier);
        void TriggerEffectsWithCondition(byte cond, TCharRecord creature);
        bool AttackRoll(TCharRecord attacker, TCharRecord defender, byte sixtyMinusAc);
        void InflictNonWeaponDamage(TCharRecord creature, byte damage, byte saveReduction, bool saved);
        // saveNegates is the raw Pascal Boolean byte: spellTable's saveResult (0..3) is passed through Magic's Boolean
        // overlay unchanged, and FxEngine tests it with "= True", so 2 and 3 do not count as True there.
        void AddEffectWithMessage(TCharRecord creature, byte effId, ushort minutes, byte strength, bool onRemove, byte saveNegates, bool saved, string msg);
        void RemoveInvisibility(TCharRecord creature);
        bool RestoreHitPoints(TCharRecord creature, byte hp, bool bypassRot);
        void CallEffectHandler(byte effId, TCharRecord creature, object p, bool removed);
        bool AdjustForNewStrength(TCharRecord creature, byte baseStr, byte excStr, ref byte effStrength);
        void AddEffect(TCharRecord creature, byte effId, ushort minutes, byte strength, bool onRemove);
        bool RemoveEffectWithMessage(TCharRecord creature, byte effId);
        bool ReviveDeadCreature(TCharRecord creature, byte hp);
        void ConsiderStinkingCloud(TCharRecord creature);
        void RemoveEffect(TCharRecord creature, byte effId, object eff);
    }

    /// <summary>The cbtmap.pas routines magic.pas calls.</summary>
    public interface IMagicCombatMap
    {
        sbyte CombatCellX(TCharRecord creature);
        sbyte CombatCellY(TCharRecord creature);
        void SetCreatureFacing(TCharRecord creature, byte dir, byte frame, byte eraseOnly);
        bool CombatIconVisible(TCharRecord creature, byte whole);
        byte CountTargets(TCharRecord creature, byte range);
        void GetArenaContents(sbyte x, sbyte y, ref byte crIdx, ref byte terrIdx);
        bool IsValidCreatureLoc(TCharRecord creature, byte x, byte y, bool moving);
        void ScrollView(sbyte x, sbyte y, byte delta, byte dir);
    }

    public static class Magic
    {
        // ---- unported-unit seams ----
        public static IMagicEffects Effects;
        public static IMagicCombatMap CombatMap;

        private static IMagicEffects Fx
        {
            get { if (Effects == null) throw new NotSupportedException("Magic: FxEngine is not ported; install Magic.Effects."); return Effects; }
        }
        private static IMagicCombatMap Map
        {
            get { if (CombatMap == null) throw new NotSupportedException("Magic: CbtMap is not bound; call MagicBindings.BindCombatMap or install Magic.CombatMap."); return CombatMap; }
        }

        // ---- converted-unit call points (default to the converted routine) ----
        internal delegate byte MenuVerticalProc(string query, string menu, byte qCol, byte mCol, byte hlCol, sbyte ulCol, sbyte ulRow, sbyte lrCol, sbyte lrRow, TItemRec list, bool addExit, ref bool redraw, ref short idx, ref TItemRec sel);
        internal delegate void FreeStringListProc(ref TItemRec list);
        internal delegate void SelectCharProc(string query, bool addExit, ref TCharRecord sel);

        internal static MenuVerticalProc MenuVerticalCall = MenuCore.MenuVertical;
        internal static Func<string, byte, byte, byte, byte> YesNoMenuCall = MenuCore.YesNoMenu;
        internal static FreeStringListProc FreeStringListCall = MenuCore.FreeStringList;
        internal static Action<sbyte, sbyte, sbyte, sbyte, byte, byte, string> DrawWindowCall = Border.DrawWindow;
        internal static Action<sbyte, sbyte, sbyte, sbyte> ClearBoxCall = Border.ClearBox;
        internal static Action<byte, byte, byte, string> DisplayStringCall = TextRend.DisplayString;
        internal static Action PauseOnlyCall = TextRend.PauseOnly;
        internal static Action DisplayNoncombatInterfaceCall = CharDisp.DisplayNoncombatInterface;
        internal static SelectCharProc SelectCharCall = CharDisp.SelectChar;
        internal static Action<byte> BuildIconFacingsCall = CharDisp.BuildIconFacings;
        internal static Action<sbyte, sbyte, sbyte, sbyte, byte, byte> CombatAnimateMovingIconCall = CharDisp.CombatAnimateMovingIcon;
        internal static Action<string> PrintBottomLineCall = CharDisp.PrintBottomLine;
        internal static Action ClearMessageWindowCall = CharDisp.ClearMessageWindow;
        internal static Action<TCharRecord> PrintHpRestoreMsgCall = CharDisp.PrintHpRestoreMsg;
        internal static Action<TCharRecord, string, byte, bool> PrintNameStringCall = CharDisp.PrintNameString;
        internal static Action<TCharRecord, bool, string> PrintNameStringAnimCall = CharDisp.PrintNameStringAnim;
        internal static Action<TCharRecord, byte, byte, bool> DisplayCreatureNameCall = CharDisp.DisplayCreatureName;
        internal static Action DisplayCombatInterfaceCall = CharDisp.DisplayCombatInterface;
        internal static Action<TItemRec, byte, byte> PrintItemLabelCall = CharDisp.PrintItemLabel;
        internal static Action<ushort> SoundSetAutoStopCall = SoundDrv.SoundSetAutoStop;
        internal static Action<ushort> SoundCommandCall = SoundDrv.SoundCommand;
        internal static Action<TCharRecord, byte> RemoveSpellCall = CharHlp.RemoveSpell;
        internal static Action<TCharRecord, TItemRec> RemoveItemCall = CharHlp.RemoveItem;
        internal static Action<TCharRecord> RecalcStatsCall = CharHlp.RecalcStats;
        internal static Func<TCharRecord, bool> EndActionsCall = CharHlp.EndActions;
        internal static Action<TCharRecord, byte, int> StatsCountCall = Stats.Count;
        internal static Action<TCharRecord> RecalcBaseSkillsCall = ClassCal.RecalcBaseSkills;
        internal static Action<sbyte, sbyte, byte> TargetAreaCall = Pathing.TargetArea;

        // ---- unit state (implementation var section) ----
        // the menu string list
        internal static TItemRec spellList;
        // spell ID per menu line
        internal static byte[] spellIds = new byte[C_Spell.LINE_LAST + 1];
        // the scroll each Cast line reads from, null for a memorized spell
        internal static TItemRec[] spellScrolls = new TItemRec[C_Spell.LINE_LAST + 1];
        internal static TItemRec[] scrollPtrs = new TItemRec[49];
        internal static byte scrollCount;
        // Current item used for spell casting.
        internal static TItemRec dsCurrentScrollPtr;

        // ---- constants ----
        internal const byte LIGHTNING_BOLT_CELLS = 8;
        internal const byte MARK_NONE = 0;
        internal const byte MARK_KNOWN = 1;
        internal const byte MARK_NOT_KNOWN = 2;
        internal const char KNOWN_MARK = '-';
        internal const char NOT_KNOWN_MARK = '+';
        internal const char SCROLL_MARK = '-';
        internal const byte SPELL_LEVEL_LAST = 7;

        private const byte Black = 0;
        private const byte LightGreen = 10;
        private const byte LightMagenta = 13;
        private const byte Yellow = 14;
        private const byte White = 15;

        // Pascal String[n] assignment
        private static string Str(string s, int n) { return s.Length > n ? s.Substring(0, n) : s; }
        private static byte B(int v) { return unchecked((byte)v); }
        private static sbyte SB(int v) { return unchecked((sbyte)v); }

        public static byte SpellSelection(byte selType, ref short idx, ref TItemRec scroll)
        {
            TItemRec sel;
            TItemRec p;
            string prompt;
            string title;
            byte count;
            byte bottomRow;
            byte ch;
            bool addExit;
            bool redraw;
            byte result;
            switch (selType)
            {
                case 1: title = "Cast"; break;
                case 2: title = "Memorize"; break;
                case 3: title = "Scribe"; break;
                case 4: title = "Learn"; break;
                default: title = ""; break;
            }
            if (selType >= 1 && selType <= 4)
                prompt = "Choose Spell: ";
            else
                prompt = "";
            if (selType == 2)
                bottomRow = 15;
            else
                bottomRow = 22;
            if (selType == 4)
                addExit = false;
            else
                addExit = true;
            redraw = false;
            if (idx < 0)
            {
                DrawWindowCall(1, 3, 38, SB(bottomRow), 0, Black, "");
                redraw = true;
                // open at the top of the list on its first spell
                GDVars.vertTop = 0;
                idx = 0;
                p = spellList;
                while ((p != null) && (p.inUse != 0))
                {
                    p = p.next;
                    idx = unchecked((short)(idx + 1));
                }
                if (p == null)
                    idx = 0;
            }
            if ((selType == 4) || (selType == 1))
                redraw = true;
            sel = null;
            do
            {
                MenuCore.listOnly = selType == 5;
                ch = MenuVerticalCall(prompt, title, LightMagenta, LightGreen, White, 1, 5, 38,
                                      SB(bottomRow), spellList, addExit, ref redraw, ref idx, ref sel);
            } while (!(ch == C_Keys.NONE || ch == C_Keys.ENTER || ch == 'C' || ch == 'E' || ch == 'L' || ch == 'M' || ch == 'S'));
            p = spellList;
            count = 0;
            while (p != sel)
            {
                if (p.inUse == 0)
                    count = B(count + 1);
                p = p.next;
            }
            scroll = null;
            if ((ch == C_Keys.NONE || ch == 'E') || (count > C_Spell.LINE_LAST))
                result = C_Spell.NONE;
            else
            {
                result = spellIds[count];
                scroll = spellScrolls[count];
            }
            if (selType == 3)
            {
                // 48 = High(scrollPtrs)
                if (count <= 48)
                    dsCurrentScrollPtr = scrollPtrs[count];
                else
                    dsCurrentScrollPtr = null;
            }
            FreeStringListCall(ref spellList);
            return result;
        }

        internal static void AddSpellToList(TCharRecord creature, byte spell, byte mark)
        {
            TItemRec p;
            sbyte count;
            sbyte lastLevel;
            p = spellList;
            if (spellList == null)
            {
                p = new TItemRec();
                spellList = p;
                lastLevel = 0;
                count = 0;
            }
            else
            {
                count = 1;
                while (p.next != null)
                {
                    if (p.inUse == 0)
                        count = SB(count + 1);
                    p = p.next;
                }
                // a full list takes no more lines
                if (count > C_Spell.LINE_LAST)
                    return;
                lastLevel = GDConsts.spellTable[spellIds[count - 1]].levelSp;
                p.next = new TItemRec();
                p = p.next;
            }
            p.next = null;
            if (GDConsts.spellTable[spell & C_Spell.ID_MASK].levelSp != lastLevel)
            {
                p.name = Str(GDConsts.spellLevelNames[GDConsts.spellTable[spell & C_Spell.ID_MASK].levelSp], 40);
                p.inUse = 1;
                p.next = new TItemRec();
                p = p.next;
                p.next = null;
            }
            if (spell > C_Spell.ID_MASK)
                p.name = " *";
            else
                p.name = " " + SpellMark(creature, spell, mark);
            p.name = Str(p.name + GDConsts.spellNames[spell & C_Spell.ID_MASK], 40);
            p.inUse = 0;
            spellIds[count] = B(spell & C_Spell.ID_MASK);
        }

        internal static char SpellMark(TCharRecord creature, byte spell, byte mark)
        {
            char result = ' ';
            byte id;
            bool learnable;
            id = B(spell & C_Spell.ID_MASK);
            if ((id >= 1) && (id <= creature.spellsKnown.Length - 1))
            {
                switch (mark)
                {
                    case MARK_KNOWN:
                        if (creature.spellsKnown[id] != 0)
                            result = KNOWN_MARK;
                        break;
                    case MARK_NOT_KNOWN:
                        if (creature.spellsKnown[id] == 0)
                        {
                            learnable = false;
                            TSpellRec s = GDConsts.spellTable[id];
                            if ((s.levelSp >= 1) && (s.levelSp <= 3))
                            {
                                switch (s.castClassSp)
                                {
                                    case 0: learnable = creature.classLevels[C_Level.CLERIC] > 0; break;
                                    case 1: learnable = creature.classLevels[C_Level.MAGE] > 0; break;
                                }
                            }
                            if (learnable)
                                result = NOT_KNOWN_MARK;
                        }
                        break;
                }
            }
            return result;
        }

        internal static void AddScrollSpells(TCharRecord creature, bool forScribe, byte mark)
        {
            if (CharHlp.ScrollLegible(creature, dsCurrentScrollPtr))
            {
                // 3 = High(TItemRec.spells)
                for (byte i = 1; i <= 3; i++)
                {
                    if ((forScribe && (dsCurrentScrollPtr.spells[i] > C_Spell.PENDING_FLAG)) ||
                        (!forScribe && (dsCurrentScrollPtr.spells[i] > 0)))
                    {
                        AddSpellToList(creature, dsCurrentScrollPtr.spells[i], mark);
                        // 48 = High(scrollPtrs)
                        if (scrollCount <= 48)
                        {
                            scrollPtrs[scrollCount] = dsCurrentScrollPtr;
                            scrollCount = B(scrollCount + 1);
                        }
                    }
                }
            }
        }

        internal static void AddAllScrollSpells(TCharRecord creature, bool forScribe, byte mark)
        {
            dsCurrentScrollPtr = creature.itemsAddr;
            while (dsCurrentScrollPtr != null)
            {
                if (CharHlp.ItemReadable(dsCurrentScrollPtr))
                    AddScrollSpells(creature, forScribe, mark);
                dsCurrentScrollPtr = dsCurrentScrollPtr.next;
            }
        }

        internal static void AddScrollLine(TCharRecord creature, TItemRec item, byte slot, ref byte lines)
        {
            TItemRec p;
            if (lines <= C_Spell.LINE_LAST)
            {
                AddSpellToList(creature, item.spells[slot], MARK_NONE);
                spellScrolls[lines] = item;
                lines = B(lines + 1);
                p = spellList;
                while (p.next != null)
                    p = p.next;
                if (p.name.Length >= 2)
                    p.name = p.name.Substring(0, 1) + SCROLL_MARK + p.name.Substring(2);
            }
        }

        internal static void AddAvailableSpells(TCharRecord creature)
        {
            byte id;
            byte lines;
            bool memOk;
            bool scrollOk;
            TItemRec item;
            memOk = (GDVars.ifcState != C_Ifc.COMBAT) || (creature.combatAddr.castAllowed != 0);
            scrollOk = CharHlp.CanUseItems(creature);
            lines = 0;
            for (byte lvl = 1; lvl <= SPELL_LEVEL_LAST; lvl++)
            {
                if (memOk)
                {
                    for (byte i = 0; i <= creature.memorizedSpells.Data.Length - 1; i++)
                    {
                        id = creature.memorizedSpells[i];
                        if ((id >= 1) && (id <= C_Spell.LAST))
                            if ((GDConsts.spellTable[id].levelSp == lvl) && (lines <= C_Spell.LINE_LAST))
                            {
                                AddSpellToList(creature, id, MARK_NONE);
                                lines = B(lines + 1);
                            }
                    }
                }
                if (scrollOk)
                {
                    item = creature.itemsAddr;
                    while (item != null)
                    {
                        if (CharHlp.CastableScroll(creature, item))
                            for (byte i = 1; i <= 3; i++)
                            {
                                id = CharHlp.ScrollSpellAt(item, i);
                                if (id != C_Spell.NONE)
                                    if (GDConsts.spellTable[id].levelSp == lvl)
                                        AddScrollLine(creature, item, i, ref lines);
                            }
                        item = item.next;
                    }
                }
            }
        }

        public static bool GenerateSpellList(TCharRecord creature, byte menuType)
        {
            bool result = false;
            TItemRec p;
            spellList = null;
            for (int i = 0; i <= C_Spell.LINE_LAST; i++)
            {
                spellIds[i] = 0;
                spellScrolls[i] = null;
            }
            // every list starts with no scrolls recorded: High(scrollPtrs), 49 slots
            for (int i = 0; i <= 48; i++)
                scrollPtrs[i] = null;
            scrollCount = 0;
            switch (menuType)
            {
                case 7:
                    AddAvailableSpells(creature);
                    break;
                case 0:
                    for (byte i = 0; i <= SPELL_SLOT_LAST; i++)
                        if ((creature.memorizedSpells[i] > 0) && (creature.memorizedSpells[i] < C_Spell.PENDING_FLAG))
                            AddSpellToList(creature, creature.memorizedSpells[i], MARK_NONE);
                    break;
                case 5:
                    for (byte i = 0; i <= SPELL_SLOT_LAST; i++)
                        if (creature.memorizedSpells[i] > C_Spell.ID_MASK)
                            AddSpellToList(creature, creature.memorizedSpells[i], MARK_NONE);
                    break;
                // every known spell, Restoration (56) too
                case 1:
                    for (byte i = 1; i <= creature.spellsKnown.Length - 1; i++)
                        if (creature.spellsKnown[i] != 0)
                            AddSpellToList(creature, i, MARK_NONE);
                    break;
                case 2:
                    AddScrollSpells(creature, false, MARK_NOT_KNOWN);
                    break;
                case 3:
                    AddAllScrollSpells(creature, false, MARK_KNOWN);
                    break;
                case 6:
                    AddAllScrollSpells(creature, true, MARK_NONE);
                    break;
                // only spells inside slots[0..1, 1..3]
                case 4:
                    for (byte i = 1; i <= 56; i++)
                    {
                        TSpellRec s = GDConsts.spellTable[i];
                        if ((s.castClassSp >= 0) && (s.castClassSp <= 1) && (s.levelSp >= 1) && (s.levelSp <= 3))
                            if ((creature.slots[s.castClassSp, s.levelSp] > 0) && (creature.spellsKnown[i] == 0))
                                AddSpellToList(creature, i, MARK_NONE);
                    }
                    break;
            }
            // a list of headings alone has nothing to pick: it counts as empty
            p = spellList;
            while ((p != null) && (p.inUse != 0))
                p = p.next;
            if (p != null)
                result = true;
            else if (spellList != null)
            {
                FreeStringListCall(ref spellList);
                spellList = null;
            }
            return result;
        }

        internal static ushort CalcSpellDuration(byte spell)
        {
            ushort d;
            switch (spell)
            {
                // Cause Disease
                case 40: d = unchecked((ushort)(Dice.RollDice(1, 6) * 10)); break;
                // Potion of Speed, Wand of Paralyzation
                case 57: case 61: d = Dice.RollDice(5, 4); break;
                // Giant Strength
                case 59: d = unchecked((ushort)(Dice.RollDice(1, 4) * 10 + 40)); break;
                // Dust of Disappearance
                case 63:
                    if (GDVars.ifcState == C_Ifc.COMBAT)
                        d = unchecked((ushort)(Dice.RollDice(2, 10) * 10));
                    else
                        d = unchecked((ushort)((Dice.RollDice(1, 10) + 10) * 10));
                    break;
                // Manual of Bodily Health: 1440 minutes
                case 67: d = 1440; break;
                default:
                    d = unchecked((ushort)(GDConsts.spellTable[spell].durFixed + GDConsts.spellTable[spell].durVar * CharHlp.GetCasterLevel(spell)));
                    break;
            }
            return d;
        }

        internal static void ResolveSpell(byte spell, byte eff, bool onRemove, byte damage, byte elem, string msg)
        {
            byte effLevel;
            TCharRecord tgt;
            bool saved;
            msg = Str(msg, 40);
            if (damage == 0)
                GDVars.elementMask = 0;
            else
                GDVars.elementMask = elem;
            if (GDVars.spellTargetCount == 0)
                return;
            if (eff == 0)
                effLevel = CharHlp.GetCasterLevel(spell);
            else
                effLevel = eff;
            byte last = GDVars.spellTargetCount;
            for (int i = 1; i <= last; i++)
            {
                if (GDVars.dsTargetCreaturePtr[i] != null)
                {
                    tgt = GDVars.dsTargetCreaturePtr[i];
                    // Pascal's "not saveEffect" in a condition is a jump on the raw byte: any nonzero saveResult rolls
                    if (!GDConsts.spellTable[spell].saveEffect)
                        saved = false;
                    else
                        saved = Fx.RollSavingThrow(tgt, SB(GDConsts.spellTable[spell].saveType), 0);
                    if (GDConsts.spellTable[GDVars.dsCurrentSpellBeing].rangeFixed == 255)
                    {
                        RecalcStatsCall(tgt);
                        Fx.TriggerEffectsWithCondition(C_Cond.DEFENDER_AC, tgt);
                        if (!Fx.AttackRoll(GDVars.curChar, tgt, tgt.acCur))
                        {
                            damage = 0;
                            saved = true;
                        }
                    }
                    if (damage > 0)
                        Fx.InflictNonWeaponDamage(tgt, damage, GDConsts.spellTable[spell].saveResult, saved);
                    if (GDConsts.spellTable[spell].effect > 0)
                        Fx.AddEffectWithMessage(tgt, GDConsts.spellTable[spell].effect, CalcSpellDuration(spell), effLevel,
                                                onRemove, GDConsts.spellTable[spell].saveResult, saved, msg);
                }
            }
            GDVars.elementMask = 0;
        }

        public static void NoncombatCastTargetMenu(byte spell, bool b, ref bool cast)
        {
            if (GDVars.castTarget == null)
                GDVars.castTarget = GDVars.curChar;
            GDVars.dsTargetCreaturePtr[1] = GDVars.curChar;
            GDVars.spellTargetCount = 1;
            cast = true;
            switch (GDConsts.spellTable[spell].target)
            {
                case 1:
                    break;
                case 2:
                    DisplayNoncombatInterfaceCall();
                    SelectCharCall("Cast Spell on whom", true, ref GDVars.castTarget);
                    if (GDVars.castTarget == null)
                    {
                        GDVars.dsTargetCreaturePtr[1] = null;
                        GDVars.spellTargetCount = 0;
                        cast = false;
                    }
                    else
                        GDVars.dsTargetCreaturePtr[1] = GDVars.castTarget;
                    break;
                case 4:
                    GDVars.dsTargetCreaturePtr[GDVars.spellTargetCount] = GDVars.creatureList;
                    while (GDVars.dsTargetCreaturePtr[GDVars.spellTargetCount].nextCharAddr != null)
                    {
                        GDVars.dsTargetCreaturePtr[GDVars.spellTargetCount + 1] =
                            GDVars.dsTargetCreaturePtr[GDVars.spellTargetCount].nextCharAddr;
                        GDVars.spellTargetCount = B(GDVars.spellTargetCount + 1);
                    }
                    break;
                default:
                    cast = false;
                    break;
            }
        }

        public static void CastSpellNC(byte spell, bool b, bool ai, ref bool sel)
        {
            bool ok;
            sbyte x;
            sbyte y;
            byte k;
            TCharRecord p;
            p = GDVars.curChar;
            ok = true;
            if ((GDVars.ifcState != C_Ifc.COMBAT) && (GDConsts.spellTable[spell].target == 0))
            {
                if (GDVars.usingItem == false)
                {
                    ShowCombatOnlySpell(spell);
                    if (YesNoMenuCall("Lose it? ", LightMagenta, White, LightGreen) == 'Y')
                        RemoveSpellCall(GDVars.curChar, spell);
                }
                else
                {
                    DisplayStringCall(1, 19, LightGreen, "That Item");
                    DisplayStringCall(1, 20, LightGreen, "is a combat-only item...");
                    if (YesNoMenuCall("Use it? ", LightMagenta, White, LightGreen) == 'Y')
                        sel = true;
                }
                ai = false;
                ok = false;
            }
            if (ai)
                if (GDVars.usingItem == false)
                    PrintCastSpellMsg(p, "casts", spell);
            while (ok)
            {
                GDVars.castTargetMenu(spell, b, ref sel);
                if (sel)
                {
                    ok = false;
                    if (GDVars.ifcState == C_Ifc.COMBAT)
                    {
                        BuildIconFacingsCall(18);
                        x = Map.CombatCellX(p);
                        y = Map.CombatCellY(p);
                        k = 0;
                        while (!Pathing.IsDirToward(x, y, GDVars.gTargetX, GDVars.gTargetY, k))
                            k = B(k + 1);
                        Map.SetCreatureFacing(p, k, 1, 0);
                        SoundSetAutoStopCall(0);
                        switch (spell)
                        {
                            case C_Spell.FIREBALL: SoundCommandCall(GDConsts.soundIds.combat[C_Sound.FIREBALL]); break;
                            case C_Spell.LIGHTNING_BOLT: SoundCommandCall(GDConsts.soundIds.combat[C_Sound.LIGHTNING]); break;
                            default: SoundCommandCall(GDConsts.soundIds.commands[C_Sound.CAST]); break;
                        }
                        CombatAnimateMovingIconCall(x, y, GDVars.gTargetX, GDVars.gTargetY, 4, 30);
                        SoundSetAutoStopCall(10);
                        if (Map.CombatIconVisible(p, 0))
                        {
                            Map.SetCreatureFacing(p, p.combatAddr.direction, 1, 1);
                            Map.SetCreatureFacing(p, p.combatAddr.direction, 0, 0);
                        }
                    }
                    Fx.RemoveInvisibility(p);
                    if (GDVars.usingItem == false)
                    {
                        RemoveSpellCall(GDVars.curChar, spell);
                        StatsCountCall(p, Stats.SPELLS_CAST, 1);
                    }
                    GDVars.dsCurrentSpellBeing = spell;
                    GDVars.spellFns[GDVars.dsCurrentSpellBeing]();
                    GDVars.dsCurrentSpellBeing = C_Spell.NONE;
                    GDVars.gMultiTarget = 0;
                }
                else
                {
                    if (GDVars.ifcState != C_Ifc.COMBAT)
                        ok = false;
                    else if (b || (YesNoMenuCall("Abort Spell? ", Yellow, LightGreen, White) == 'Y'))
                    {
                        PrintBottomLineCall("Spell Aborted");
                        if (GDVars.usingItem == false)
                            RemoveSpellCall(GDVars.curChar, spell);
                        ok = false;
                    }
                }
            }
            ClearMessageWindowCall();
            if (GDVars.ifcState == C_Ifc.COMBAT)
                ClearBoxCall(0, 23, 39, 23);
        }

        public static void ShowCombatOnlySpell(byte spell)
        {
            DisplayStringCall(1, 19, LightGreen, GDConsts.spellNames[spell]);
            DisplayStringCall(1, 20, LightGreen, "is a combat-only spell...");
        }

        internal static void CastOneSide(sbyte hostFlag, string msg)
        {
            GDVars.gMultiTarget = 1;
            byte last = GDVars.spellTargetCount;
            for (int i = 1; i <= last; i++)
            {
                if ((GDVars.dsTargetCreaturePtr[i].hostile != hostFlag) ||
                    ((GDVars.dsCurrentSpellBeing == C_Spell.BLESS) && (GDVars.ifcState == C_Ifc.COMBAT) &&
                     (Map.CountTargets(GDVars.dsTargetCreaturePtr[i], 1) > 0)))
                    GDVars.dsTargetCreaturePtr[i] = null;
            }
            ResolveSpell(GDVars.dsCurrentSpellBeing, 0, false, 0, 0, msg);
        }

        public static void BlessHandler()
        {
            CastOneSide(GDVars.curChar.hostile, "is Blessed");
        }

        public static void CurseHandler()
        {
            CastOneSide(CharHlp.EnemyTeam(GDVars.curChar), "is Cursed");
        }

        public static void CureLightWoundsHandler()
        {
            if (GDVars.spellTargetCount == 0)
                return;
            if (Fx.RestoreHitPoints(GDVars.dsTargetCreaturePtr[1], Dice.RollDice(1, 8), false))
                PrintHpRestoreMsgCall(GDVars.dsTargetCreaturePtr[1]);
        }

        public static void CauseLightWoundsHandler()
        {
            ResolveSpell(GDVars.dsCurrentSpellBeing, 0, false, Dice.RollDamageDice(1, 8), C_Effect.ELEM_MAGIC, "");
        }

        public static void DetectionHandler()
        {
            ResolveSpell(GDVars.dsCurrentSpellBeing, 0, false, 0, 0, "is affected");
        }

        public static void ProtectionHandler()
        {
            ResolveSpell(GDVars.dsCurrentSpellBeing, 0, false, 0, 0, "is protected");
        }

        public static void ResistColdHandler()
        {
            ResolveSpell(GDVars.dsCurrentSpellBeing, 0, false, 0, 0, "is cold-resistant");
        }

        public static void BurningHandsHandler()
        {
            ResolveSpell(GDVars.dsCurrentSpellBeing, 0, false, CharHlp.GetCasterLevel(GDVars.dsCurrentSpellBeing),
                         C_Effect.ELEM_FIRE | C_Effect.ELEM_MAGIC, "");
        }

        public static void CharmPersonHandler()
        {
            TEffectRec effect;
            TCharRecord tgt;
            tgt = GDVars.dsTargetCreaturePtr[1];
            if ((tgt.charType > 1) || (tgt.iconDimensions > 1))
                PrintNameStringCall(tgt, "is unaffected", 10, true);
            else
            {
                ResolveSpell(GDVars.dsCurrentSpellBeing,
                             B(GDVars.curChar.hostile * 128 + CharHlp.GetCasterLevel(GDVars.dsCurrentSpellBeing)), true, 0,
                             0, "is charmed");
                if (CharHlp.HasEffect(tgt, C_Effect.CHARM, out effect))
                    Fx.CallEffectHandler(C_Effect.CHARM, GDVars.dsTargetCreaturePtr[1], effect, false);
            }
        }

        public static void EnlargeHandler()
        {
            byte baseStr;
            byte excStr;
            byte enc = 0;
            byte lvl;
            baseStr = 18;
            lvl = CharHlp.GetCasterLevel(GDVars.dsCurrentSpellBeing);
            switch (lvl)
            {
                case 1: excStr = 0; break;
                case 2: excStr = 1; break;
                case 3: excStr = 51; break;
                case 4: excStr = 76; break;
                case 5: excStr = 91; break;
                case 6: excStr = 100; break;
                default:
                    if (lvl > 6)
                        excStr = 100;
                    else
                        excStr = 0;
                    break;
            }
            if (Fx.AdjustForNewStrength(GDVars.dsTargetCreaturePtr[1], baseStr, excStr, ref enc))
                PrintNameStringCall(GDVars.dsTargetCreaturePtr[1], "is stronger", 10, true);
            Fx.AddEffect(GDVars.dsTargetCreaturePtr[1], C_Effect.ENLARGE,
                         CalcSpellDuration(GDVars.dsCurrentSpellBeing), enc, true);
        }

        public static void ReduceHandler()
        {
            if (GDVars.spellTargetCount > 0)
                if (!Fx.RollSavingThrow(GDVars.dsTargetCreaturePtr[1], C_Save.SPELL, 0))
                    if (Fx.RemoveEffectWithMessage(GDVars.dsTargetCreaturePtr[1], C_Effect.ENLARGE))
                        PrintNameStringCall(GDVars.dsTargetCreaturePtr[1], "has been reduced", 10, true);
        }

        public static void FriendsHandler()
        {
            byte oldCha;
            oldCha = GDVars.dsTargetCreaturePtr[1].chaCur;
            GDVars.dsTargetCreaturePtr[1].chaCur = AbilAdj.ClampScore(unchecked((short)(oldCha + Dice.RollDice(2, 4))));
            ResolveSpell(GDVars.dsCurrentSpellBeing, oldCha, true, 0, 0, "is friendly");
        }

        public static void MagicMissileHandler()
        {
            byte cl;
            cl = CharHlp.GetCasterLevel(GDVars.dsCurrentSpellBeing);
            ResolveSpell(GDVars.dsCurrentSpellBeing, 0, false, B(cl / 2 + Dice.RollDamageDice(B(cl / 2), 4)),
                         C_Effect.ELEM_MAGIC, "");
        }

        public static void ShieldHandler()
        {
            ResolveSpell(GDVars.dsCurrentSpellBeing, 0, false, 0, 0, "is shielded");
        }

        public static void ShockingGraspHandler()
        {
            ResolveSpell(GDVars.dsCurrentSpellBeing, 0, false,
                         B(Dice.RollDamageDice(1, 8) + CharHlp.GetCasterLevel(GDVars.dsCurrentSpellBeing)),
                         C_Effect.ELEM_ELEC | C_Effect.ELEM_MAGIC, "");
        }

        public static void SleepHandler()
        {
            byte pool;
            byte cost;
            TEffectRec effect;
            GDVars.gMultiTarget = 1;
            pool = Dice.RollDice(4, 4);
            byte last = GDVars.spellTargetCount;
            for (int i = 1; i <= last; i++)
            {
                switch (GDVars.dsTargetCreaturePtr[i].levelHighest)
                {
                    case 0: case 1: cost = 1; break;
                    case 2: cost = 2; break;
                    case 3: cost = 4; break;
                    case 4: cost = 6; break;
                    case 5:
                        if (GDVars.dsTargetCreaturePtr[i].race == C_Race.MONSTER)
                            cost = 10;
                        else
                            cost = 20;
                        break;
                    default: cost = 20; break;
                }
                if ((GDVars.dsTargetCreaturePtr[i].status != C_Status.ANIMATED) &&
                    (!CharHlp.HasEffect(GDVars.dsTargetCreaturePtr[i], C_Effect.SLEEP, out effect)) &&
                    (pool >= cost))
                    pool = B(pool - cost);
                else
                    GDVars.dsTargetCreaturePtr[i] = null;
            }
            ResolveSpell(GDVars.dsCurrentSpellBeing, 0, false, 0, 0, "falls asleep");
        }

        public static void HoldPersonHandler()
        {
            TCharRecord tgt;
            bool saved;
            sbyte smod;
            switch (GDVars.spellTargetCount)
            {
                case 1:
                    if (GDVars.dsCurrentSpellBeing == C_Spell.HOLD_PERSON)
                        smod = -2;
                    else
                        smod = -3;
                    break;
                case 2: smod = -1; break;
                default: smod = 0; break;
            }
            for (int i = GDVars.spellTargetCount; i >= 1; i--)
            {
                if (GDVars.dsTargetCreaturePtr[i] != null)
                {
                    tgt = GDVars.dsTargetCreaturePtr[i];
                    if (i < GDVars.spellTargetCount)
                    {
                        SoundSetAutoStopCall(0);
                        SoundCommandCall(GDConsts.soundIds.commands[C_Sound.CAST]);
                        BuildIconFacingsCall(18);
                        CombatAnimateMovingIconCall(Map.CombatCellX(GDVars.curChar), Map.CombatCellY(GDVars.curChar),
                                                    Map.CombatCellX(tgt), Map.CombatCellY(tgt), 4, 30);
                        SoundSetAutoStopCall(10);
                    }
                    saved = Fx.RollSavingThrow(tgt, SB(GDConsts.spellTable[GDVars.dsCurrentSpellBeing].saveType), smod);
                    if ((tgt.charType > 1) || (tgt.iconDimensions > 1))
                        saved = true;
                    Fx.AddEffectWithMessage(tgt, GDConsts.spellTable[GDVars.dsCurrentSpellBeing].effect,
                                            CalcSpellDuration(GDVars.dsCurrentSpellBeing),
                                            CharHlp.GetCasterLevel(GDVars.dsCurrentSpellBeing), false,
                                            GDConsts.spellTable[GDVars.dsCurrentSpellBeing].saveResult, saved, "is held");
                }
            }
        }

        public static void ResistFireHandler()
        {
            ResolveSpell(GDVars.dsCurrentSpellBeing, 0, false, 0, 0, "is fire resistant");
        }

        public static void SilenceHandler()
        {
            ResolveSpell(GDVars.dsCurrentSpellBeing, 0, false, 0, 0, "is silenced");
        }

        public static void SlowPoisonHandler()
        {
            TEffectRec effect;
            TCharRecord tgt;
            tgt = GDVars.dsTargetCreaturePtr[1];
            if (tgt.status == C_Status.ANIMATED)
                GDVars.dsTargetCreaturePtr[1] = null;
            else if (CharHlp.HasEffect(tgt, C_Effect.POISONED, out effect))
            {
                if (tgt.hpCur == 0)
                    tgt.hpCur = 1;
                ResolveSpell(GDVars.dsCurrentSpellBeing, 255, true, 0, 0, "is affected");
                if (!Fx.ReviveDeadCreature(tgt, tgt.hpCur))
                    Fx.AddEffect(tgt, C_Effect.HEAL_WAKE, 1, 0, true);
                Fx.AddEffect(tgt, C_Effect.SLOW_POISON_HIT, 10, 255, true);
            }
        }

        public static void SnakeCharmHandler()
        {
            byte pool;
            TCharRecord p;
            pool = GDVars.curChar.hpCur;
            GDVars.spellTargetCount = 0;
            p = GDVars.creatureList;
            while (p != null)
            {
                // charType 14: snake
                if (p.charType == 14)
                    if (pool >= p.hpCur)
                    {
                        pool = B(pool - p.hpCur);
                        GDVars.spellTargetCount = B(GDVars.spellTargetCount + 1);
                        GDVars.dsTargetCreaturePtr[GDVars.spellTargetCount] = p;
                    }
                p = p.nextCharAddr;
            }
            ResolveSpell(GDVars.dsCurrentSpellBeing, 0, false, 0, 0, "is charmed");
        }

        public static void SpiritualHammerHandler()
        {
            ResolveSpell(GDVars.dsCurrentSpellBeing, 0, true, 0, 0, "");
            Fx.CallEffectHandler(C_Effect.SPIRIT_HAMMER, GDVars.dsTargetCreaturePtr[1], null, false);
        }

        public static void InvisibilityHandler()
        {
            ResolveSpell(GDVars.dsCurrentSpellBeing, 0, false, 0, 0, "is invisible");
        }

        public static void KnockHandler()
        {
            ResolveSpell(GDVars.dsCurrentSpellBeing, 0, false, 0, 0, "Knock-Knock");
        }

        public static void MirrorImageHandler()
        {
            ResolveSpell(GDVars.dsCurrentSpellBeing, Dice.RollDice(1, 4), false, 0, 0, "is duplicated");
        }

        public static void RayOfEnfeeblementHandler()
        {
            ResolveSpell(GDVars.dsCurrentSpellBeing, 0, false, 0, 0, "is weakened");
        }

        public static void StinkingCloudHandler()
        {
            TStinkRec p2;
            TStinkRec node;
            byte[] crIdx = new byte[5]; // 1..4
            byte myClouds;
            byte terr = 0;
            byte cl;
            byte d;
            GDVars.gMultiTarget = 1;
            cl = CharHlp.GetCasterLevel(GDVars.dsCurrentSpellBeing);
            myClouds = 0;
            node = GDVars.stinkHead;
            if (GDVars.stinkHead == null)
            {
                GDVars.stinkHead = new TStinkRec();
                node = GDVars.stinkHead;
            }
            else
            {
                while (node.next != null)
                {
                    if (node.caster == GDVars.curChar)
                        myClouds = B(myClouds + 1);
                    node = node.next;
                }
                node.next = new TStinkRec();
                node = node.next;
            }
            // FillChar(node^, SizeOf(TStinkRec), 0): the new node is already all zero
            Fx.AddEffect(GDVars.curChar, C_Effect.CAST_STINK, cl, B(cl + myClouds * 16), true);
            node.caster = GDVars.curChar;
            node.idx = myClouds;
            node.ulX = GDVars.gTargetX;
            node.ulY = GDVars.gTargetY;
            for (int k = 1; k <= 4; k++)
            {
                d = GDConsts.cloudDir[k];
                Map.GetArenaContents(SB(GDVars.gTargetX + GDConsts.dirDX[d]), SB(GDVars.gTargetY + GDConsts.dirDY[d]), ref crIdx[k],
                                     ref terr);
                if ((terr > 0) && (GDConsts.terrainInfo[terr].cost < 255))
                    node.exists[k] = 1;
                else
                    node.exists[k] = 0;
                if (terr == C_Terr.STINK_CLOUD)
                {
                    p2 = GDVars.stinkHead;
                    while (p2 != null)
                    {
                        if (p2 != node)
                            for (int j = 1; j <= 4; j++)
                                if ((p2.exists[j] != 0) &&
                                    (p2.ulX + GDConsts.dirDX[GDConsts.cloudDir[j]] == GDVars.gTargetX + GDConsts.dirDX[d]) &&
                                    (p2.ulY + GDConsts.dirDY[GDConsts.cloudDir[j]] == GDVars.gTargetY + GDConsts.dirDY[d]))
                                    if (p2.under[j] != C_Terr.STINK_CLOUD)
                                        terr = p2.under[j];
                        p2 = p2.next;
                    }
                }
                else if (terr == C_Terr.CORPSE)
                {
                    for (int j = 1; j <= GDVars.downedCount; j++)
                        if (GDVars.downed[j].x == GDVars.gTargetX + GDConsts.dirDX[d])
                            if (GDVars.downed[j].y == GDVars.gTargetY + GDConsts.dirDY[d])
                                terr = GDVars.downed[j].terrain;
                }
                node.under[k] = terr;
                if (node.exists[k] != 0)
                    GDVars.arenaPtr.cells[GDVars.gTargetY + GDConsts.dirDY[d],
                                          GDVars.gTargetX + GDConsts.dirDX[d]] = C_Terr.STINK_CLOUD;
            }
            PrintNameStringCall(GDVars.curChar, "Creates a noxious cloud", 10, false);
            Map.ScrollView(GDVars.gTargetX, GDVars.gTargetY, 255, C_Dir.CENTER);
            PauseOnlyCall();
            ClearMessageWindowCall();
            for (int k = 1; k <= 4; k++)
                if (crIdx[k] > 0)
                    Fx.ConsiderStinkingCloud(GDVars.creatureArr[crIdx[k]]);
        }

        public static void StrengthHandler()
        {
            TCharRecord tgt;
            byte exStr;
            byte roll;
            byte newStr;
            byte enc = 0;
            exStr = 0;
            roll = 0;
            tgt = GDVars.dsTargetCreaturePtr[1];
            if (tgt.classLevels[C_Level.MAGE] > 0)
                roll = Dice.RollDice(1, 4);
            if ((tgt.classLevels[C_Level.CLERIC] > 0) || (tgt.classLevels[C_Level.THIEF] > 0))
                roll = Dice.RollDice(1, 6);
            if (CharHlp.WarriorLevel(tgt) > 0)
                roll = Dice.RollDice(1, 8);
            newStr = B(tgt.strCur + roll);
            if (newStr > 18)
            {
                if (CharHlp.WarriorLevel(tgt) > 0)
                {
                    // 10 exceptional points per point above 18
                    exStr = B(tgt.strExc + (newStr - 18) * 10);
                    // Percentile cap 100
                    if (exStr > 100)
                        exStr = 100;
                    newStr = 18;
                }
                else
                    newStr = 18;
            }
            if (Fx.AdjustForNewStrength(tgt, newStr, exStr, ref enc))
                PrintNameStringCall(tgt, "is stronger", 10, true);
            Fx.AddEffect(tgt, C_Effect.STRENGTH, CalcSpellDuration(GDVars.dsCurrentSpellBeing), enc, true);
        }

        public static void AnimateDeadHandler()
        {
            byte strength;
            byte n;
            TCharRecord p;
            GDVars.gMultiTarget = 1;
            n = CharHlp.GetCasterLevel(GDVars.dsCurrentSpellBeing);
            GDVars.spellTargetCount = 0;
            p = GDVars.creatureList;
            while ((p != null) && (n > 0))
            {
                if (p.status == C_Status.DEAD)
                    // Human
                    if (p.charType == 0)
                        if (Map.IsValidCreatureLoc(p, B(Map.CombatCellX(p)), B(Map.CombatCellY(p)), true))
                        {
                            strength = B(p.hostile * 16 + CharHlp.GetCasterLevel(GDVars.dsCurrentSpellBeing));
                            p.hostile = GDVars.curChar.hostile;
                            p.quickfight = 1;
                            p.levelUndead = 2;
                            p.attackLevel = 0;
                            p.movementBase = 6;
                            for (int i = 0; i <= SPELL_SLOT_LAST; i++)
                                p.memorizedSpells[i] = 0;
                            if (CharHlp.IsNpc(p))
                                p.npc = C_Npc.ANIMATED_NPC;
                            else
                                p.npc = C_Npc.ANIMATED_PC;
                            // Undead
                            p.charType = 4;
                            if (GDVars.ifcState == C_Ifc.COMBAT)
                                p.combatAddr.other = null;
                            n = B(n - 1);
                            if (Fx.ReviveDeadCreature(p, p.hpMax))
                            {
                                Fx.AddEffectWithMessage(p, C_Effect.ANIMATE_DEAD, 0, strength, true, 0, false, "is animated");
                                p.status = C_Status.ANIMATED;
                            }
                        }
                p = p.nextCharAddr;
            }
        }

        public static void CureBlindnessHandler()
        {
            if (Fx.RemoveEffectWithMessage(GDVars.dsTargetCreaturePtr[1], C_Effect.BLIND))
                PrintNameStringAnimCall(GDVars.dsTargetCreaturePtr[1], true, "can see");
        }

        public static void CauseBlindnessHandler()
        {
            ResolveSpell(GDVars.dsCurrentSpellBeing, 0, false, 0, 0, "is blind");
        }

        internal static bool RemoveDiseaseEffects(TCharRecord tgt)
        {
            bool result = false;
            GDVars.effectLock = 1;
            if (Fx.RemoveEffectWithMessage(tgt, C_Effect.DISEASED))
                result = true;
            if (Fx.RemoveEffectWithMessage(tgt, C_Effect.DISEASE_STR))
            {
                result = true;
                Fx.RemoveEffect(tgt, C_Effect.DISEASE_HP, null);
                Fx.RemoveEffect(tgt, C_Effect.HELPLESS, null);
            }
            if (Fx.RemoveEffectWithMessage(tgt, C_Effect.ROT_NO_HEAL))
            {
                result = true;
                Fx.RemoveEffect(tgt, C_Effect.MUMMY_ROT, null);
            }
            GDVars.effectLock = 0;
            return result;
        }

        public static void CureDiseaseHandler()
        {
            bool junk;
            junk = RemoveDiseaseEffects(GDVars.dsTargetCreaturePtr[1]);
        }

        public static void CauseDiseaseHandler()
        {
            ResolveSpell(GDVars.dsCurrentSpellBeing, 0, true, 0, 0, "is diseased");
        }

        public static void DispelMagicHandler()
        {
            byte chance;
            byte effLevel;
            byte cl;
            TCharRecord tgt;
            bool removed;
            TEffectRec effect;
            TEffectRec nxt;
            GDVars.gMultiTarget = 1;
            cl = CharHlp.GetCasterLevel(GDVars.dsCurrentSpellBeing);
            byte last = GDVars.spellTargetCount;
            for (int i = 1; i <= last; i++)
            {
                removed = false;
                if (i <= ARENA_SLOTS)
                    tgt = GDVars.dsTargetCreaturePtr[i];
                else
                    tgt = null;
                if (tgt != null)
                    effect = tgt.effectsAddr;
                else
                    effect = null;
                while (effect != null)
                {
                    nxt = effect.next;
                    if ((effect.strength < 255) && !(effect.id == C_Effect.RECENT_HEAL || effect.id == C_Effect.RECENT_CURE))
                    {
                        // dispel treats the low nibble as caster level
                        effLevel = B(effect.strength & 0xF);
                        if (cl > effLevel)
                            chance = B((cl - effLevel) * 5 + 50);
                        else if (cl < effLevel)
                            chance = B(50 - (effLevel - cl) * 2);
                        else
                            chance = 50;
                        if (Dice.RollDice(1, 100) <= chance)
                        {
                            Fx.RemoveEffect(tgt, effect.id, effect);
                            removed = true;
                        }
                    }
                    effect = nxt;
                }
                if (removed)
                    PrintNameStringAnimCall(tgt, true, "is affected");
            }
        }

        public static void PrayerHandler()
        {
            ResolveSpell(GDVars.dsCurrentSpellBeing,
                         B(GDVars.curChar.hostile * 16 + CharHlp.GetCasterLevel(GDVars.dsCurrentSpellBeing)), false, 0,
                         0, "is praying");
        }

        public static void RemoveCurseSpell()
        {
            TItemRec item;
            bool found;
            if (Fx.RemoveEffectWithMessage(GDVars.dsTargetCreaturePtr[1], C_Effect.BESTOW_CURSE))
                PrintNameStringAnimCall(GDVars.dsTargetCreaturePtr[1], true, "is un-cursed");
            else
            {
                found = false;
                item = GDVars.dsTargetCreaturePtr[1].itemsAddr;
                while ((item != null) && (found == false))
                {
                    if (item.cursed != 0)
                    {
                        found = true;
                        item.cursed = 0;
                    }
                    item = item.next;
                }
                if (found)
                    PrintNameStringAnimCall(GDVars.dsTargetCreaturePtr[1], true, "'s item is un-cursed");
            }
        }

        public static void BestowCurseHandler()
        {
            ResolveSpell(GDVars.dsCurrentSpellBeing, 0, false, 0, 0, "has been cursed!");
        }

        public static void BlinkHandler()
        {
            ResolveSpell(GDVars.dsCurrentSpellBeing, 0, false, 0, 0, "is blinking");
        }

        public static void FireballHandler()
        {
            byte cl;
            GDVars.gMultiTarget = 1;
            if (GDVars.dsCurrentSpellBeing == C_Spell.ITEM_FIREBALL)
                cl = B(Dice.RollDice(1, 3) * 2 + 1);
            else
                cl = CharHlp.GetCasterLevel(GDVars.dsCurrentSpellBeing);
            if (GDVars.zone0.view3dFlag == 0)
            {
                TargetAreaCall(GDVars.gTargetX, GDVars.gTargetY, 2);
            }
            Map.ScrollView(GDVars.gTargetX, GDVars.gTargetY, 0, C_Dir.CENTER);
            ResolveSpell(GDVars.dsCurrentSpellBeing, 0, false, Dice.RollDamageDice(cl, 6),
                         C_Effect.ELEM_FIRE | C_Effect.ELEM_MAGIC, "");
        }

        internal static void HasteSlowLogic(byte removeId, sbyte hostFlag, string msg)
        {
            byte cl;
            GDVars.gMultiTarget = 1;
            cl = CharHlp.GetCasterLevel(GDVars.dsCurrentSpellBeing);
            byte last = GDVars.spellTargetCount;
            for (int i = 1; i <= last; i++)
            {
                if ((GDVars.dsTargetCreaturePtr[i].hostile == hostFlag) && (cl > 0))
                {
                    cl = B(cl - 1);
                    if (Fx.RemoveEffectWithMessage(GDVars.dsTargetCreaturePtr[i], removeId))
                        GDVars.dsTargetCreaturePtr[i] = null;
                }
                else
                    GDVars.dsTargetCreaturePtr[i] = null;
            }
            ResolveSpell(GDVars.dsCurrentSpellBeing, 0, false, 0, 0, msg);
            last = GDVars.spellTargetCount;
            for (int i = 1; i <= last; i++)
                if (GDVars.dsTargetCreaturePtr[i] != null)
                    Fx.TriggerEffectsWithCondition(C_Cond.MOVE_ATTACK_RATE, GDVars.dsTargetCreaturePtr[i]);
        }

        public static void HasteHandler()
        {
            HasteSlowLogic(C_Effect.SLOWED, GDVars.curChar.hostile, "is Hasted");
        }

        internal static void LightningAttackAt(sbyte x, sbyte y, byte damage, sbyte saveType, ref bool blocked)
        {
            TCharRecord tgt;
            byte crIdx = 0;
            byte terr = 0;
            blocked = false;
            Map.GetArenaContents(x, y, ref crIdx, ref terr);
            if (terr > 0)
                if (GDConsts.terrainInfo[terr].cost == 255)
                    blocked = true;
            if (crIdx > 0)
            {
                tgt = GDVars.creatureArr[crIdx];
                GDVars.elementMask = C_Effect.ELEM_ELEC | C_Effect.ELEM_MAGIC;
                Fx.InflictNonWeaponDamage(tgt, damage, 2, Fx.RollSavingThrow(tgt, saveType, 0));
                BuildIconFacingsCall(19);
                GDVars.elementMask = 0;
            }
        }

        internal static void LightningPath(byte boltLen, byte damage, sbyte saveType, bool bounce)
        {
            TCellList cells = new TCellList();
            WalkBolt(boltLen, damage, saveType, bounce, false, ref cells);
        }

        internal static void WalkBolt(byte boltLen, byte damage, sbyte saveType, bool bounce, bool trace, ref TCellList cells)
        {
            TPathReq req;
            TPathReq req2;
            sbyte x0;
            sbyte y0;
            sbyte prevX;
            sbyte prevY;
            sbyte sign;
            bool blocked = false;
            bool valid;
            bool doBounce;
            byte prevCr = 0;
            byte crIdx = 0;
            byte terr = 0;
            byte budget;
            sbyte savedX;
            sbyte savedY;
            byte savedMulti;
            cells.count = 0;
            savedX = GDVars.gTargetX;
            savedY = GDVars.gTargetY;
            savedMulti = GDVars.gMultiTarget;
            if (!trace)
                BuildIconFacingsCall(19);
            Map.GetArenaContents(GDVars.gTargetX, GDVars.gTargetY, ref prevCr, ref terr);
            sign = 1;
            x0 = Map.CombatCellX(GDVars.curChar);
            y0 = Map.CombatCellY(GDVars.curChar);
            doBounce = bounce;
            if (x0 == GDVars.gTargetX)
                if (y0 == GDVars.gTargetY)
                    return;
            budget = B(boltLen * 2);
            GDVars.gMultiTarget = 1;
            while (budget > 0)
            {
                req = new TPathReq();
                req.sx = GDVars.gTargetX;
                req.sy = GDVars.gTargetY;
                req.ex = unchecked((short)(GDVars.gTargetX + (GDVars.gTargetX - x0) * sign * budget));
                req.ey = unchecked((short)(GDVars.gTargetY + (GDVars.gTargetY - y0) * sign * budget));
                Pathing.PathFind(ref req);
                do
                {
                    prevX = SB(req.cx);
                    prevY = SB(req.cy);
                    if ((req.sx != req.ex) || (req.sy != req.ey))
                    {
                        do
                        {
                            valid = Pathing.NextPathStep(ref req);
                            Map.GetArenaContents(SB(req.cx), SB(req.cy), ref crIdx, ref terr);
                            if (trace && valid && (terr != 0) && (cells.count < 64))
                            {
                                cells.count = B(cells.count + 1);
                                cells.x[cells.count] = SB(req.cx);
                                cells.y[cells.count] = SB(req.cy);
                            }
                        } while (!((valid == false) || ((crIdx > 0) && (crIdx != prevCr)) || (terr == 0) ||
                                   (GDConsts.terrainInfo[terr].cost > 1) || (req.dist >= budget)));
                    }
                    prevCr = crIdx;
                    if (terr == 0)
                        budget = 0;
                    if (trace)
                    {
                        // LightningAttackAt's test
                        blocked = false;
                        if (terr > 0)
                            blocked = GDConsts.terrainInfo[terr].cost == 255;
                    }
                    else
                    {
                        CombatAnimateMovingIconCall(prevX, prevY, SB(req.cx), SB(req.cy), 4, 50);
                        LightningAttackAt(SB(req.cx), SB(req.cy), damage, saveType, ref blocked);
                    }
                    if (blocked)
                    {
                        GDVars.gTargetX = SB(req.cx);
                        GDVars.gTargetY = SB(req.cy);
                        req2 = new TPathReq();
                        req2.sx = GDVars.gTargetX;
                        req2.sy = GDVars.gTargetY;
                        req2.ex = x0;
                        req2.ey = y0;
                        Pathing.PathFind(ref req2);
                        while (Pathing.NextPathStep(ref req2)) { }
                        if (doBounce)
                            if (req2.dist <= 8)
                                req.dist = B(req.dist + 8);
                        sign = SB(0 - sign);
                        doBounce = false;
                        prevCr = 0;
                    }
                    if (req.dist < budget)
                        budget = B(budget - req.dist);
                    else
                        budget = 0;
                } while (!(blocked || (budget == 0)));
            }
            GDVars.gMultiTarget = 0;
            if (trace)
            {
                GDVars.gTargetX = savedX;
                GDVars.gTargetY = savedY;
                GDVars.gMultiTarget = savedMulti;
            }
        }

        public static void LightningBoltHandler()
        {
            byte dmg;
            bool junk = false;
            dmg = Dice.RollDice(CharHlp.GetCasterLevel(GDVars.dsCurrentSpellBeing), 6);
            LightningAttackAt(GDVars.gTargetX, GDVars.gTargetY, dmg, C_Save.SPELL, ref junk);
            LightningPath(LIGHTNING_BOLT_CELLS, dmg, C_Save.SPELL, true);
        }

        public static void TraceLightningBolt(sbyte x, sbyte y, ref TCellList cells)
        {
            sbyte savedX;
            sbyte savedY;
            savedX = GDVars.gTargetX;
            savedY = GDVars.gTargetY;
            GDVars.gTargetX = x;
            GDVars.gTargetY = y;
            WalkBolt(LIGHTNING_BOLT_CELLS, 0, C_Save.SPELL, true, true, ref cells);
            GDVars.gTargetX = savedX;
            GDVars.gTargetY = savedY;
        }

        public static void SlowHandler()
        {
            HasteSlowLogic(C_Effect.HASTED, CharHlp.EnemyTeam(GDVars.curChar), "is Slowed");
        }

        public static void RestorationHandler()
        {
            TCharRecord tgt;
            byte share;
            sbyte bestLvl;
            sbyte lvl;
            int bestXp;
            sbyte bestCls;
            tgt = GDVars.dsTargetCreaturePtr[1];
            if (tgt.drainedLevels > 0)
            {
                share = B(tgt.drainedHps / tgt.drainedLevels);
                tgt.hpMax = B(tgt.hpMax + share);
                tgt.hpCur = B(tgt.hpCur + share);
                tgt.hpRolled = B(tgt.hpRolled + share);
                tgt.drainedHps = B(tgt.drainedHps - share);
                tgt.drainedLevels = B(tgt.drainedLevels - 1);
                // 13: above any class level
                bestLvl = 13;
                // sentinel above any table entry
                bestXp = 10000000;
                // no class picked yet
                bestCls = -1;
                for (sbyte cls = C_Level.CLERIC; cls <= C_Level.MONK; cls++)
                {
                    lvl = tgt.classLevels[cls];
                    // xp[] runs to level 9
                    if ((lvl > 0) && (lvl < 9) && (lvl <= bestLvl) && (GDConsts.dsExperienceSpellsClass[cls].xp[lvl + 1] > 0) &&
                        (GDConsts.dsExperienceSpellsClass[cls].xp[lvl + 1] < bestXp) &&
                        (!CharHlp.AtLevelCap(tgt, cls)))
                    {
                        bestLvl = lvl;
                        bestCls = cls;
                        bestXp = GDConsts.dsExperienceSpellsClass[cls].xp[lvl + 1];
                    }
                }
                if (bestCls >= 0)
                {
                    tgt.classLevels[bestCls] = SB(tgt.classLevels[bestCls] + 1);
                    if (tgt.experience < bestXp)
                        tgt.experience = bestXp;
                }
                RecalcBaseSkillsCall(tgt);
                PrintNameStringCall(tgt, "is restored", 10, true);
            }
        }

        public static void PotionOfSpeedHandler()
        {
            if (!Fx.RemoveEffectWithMessage(GDVars.dsTargetCreaturePtr[1], C_Effect.SLOWED))
                ResolveSpell(GDVars.dsCurrentSpellBeing, 0, false, 0, 0, "is Speedy");
        }

        public static void KeoghtomsOintmentHandler()
        {
            TCharRecord tgt;
            TEffectRec effect;
            bool slowed;
            tgt = GDVars.dsTargetCreaturePtr[1];
            slowed = CharHlp.HasEffect(tgt, C_Effect.SLOW_POISON_END, out effect) ||
                     CharHlp.HasEffect(tgt, C_Effect.SLOW_POISON_HIT, out effect);
            if (Fx.RemoveEffectWithMessage(tgt, C_Effect.POISONED))
            {
                GDVars.effectLock = 1;
                Fx.RemoveEffect(tgt, C_Effect.SLOW_POISON_END, null);
                GDVars.effectLock = 0;
                if ((tgt.status == C_Status.DEAD) && !slowed)
                {
                    if (tgt.hpCur == 0)
                        tgt.hpCur = 1;
                    if (!Fx.ReviveDeadCreature(tgt, tgt.hpCur))
                        Fx.AddEffect(tgt, C_Effect.HEAL_WAKE, 1, 0, true);
                }
            }
            else if (!RemoveDiseaseEffects(tgt))
            {
                if (Fx.RestoreHitPoints(GDVars.dsTargetCreaturePtr[1], B(Dice.RollDice(1, 4) + 8), false))
                    PrintNameStringAnimCall(GDVars.dsTargetCreaturePtr[1], true, "is Healed");
            }
        }

        public static void GiantStrengthHandler()
        {
            byte enc = 0;
            if (Fx.AdjustForNewStrength(GDVars.dsTargetCreaturePtr[1], 21, 0, ref enc))
                PrintNameStringCall(GDVars.dsTargetCreaturePtr[1], "is stronger", 10, true);
            Fx.AddEffect(GDVars.dsTargetCreaturePtr[1], C_Effect.STRENGTH,
                         CalcSpellDuration(GDVars.dsCurrentSpellBeing), enc, true);
        }

        public static void JavelinOfLightningHandler()
        {
            bool junk = false;
            LightningAttackAt(GDVars.gTargetX, GDVars.gTargetY, B(Dice.RollDice(1, 6) + 20), C_Save.SPELL, ref junk);
            LightningPath(3, 20, C_Save.SPELL, false);
        }

        public static void WandParalyzationHandler()
        {
            ResolveSpell(GDVars.dsCurrentSpellBeing, 0, false, 0, 0, "is paralyzed");
        }

        public static void PotionOfHealingHandler()
        {
            if (Fx.RestoreHitPoints(GDVars.dsTargetCreaturePtr[1], B(Dice.RollDice(2, 4) + 2), false))
                PrintNameStringAnimCall(GDVars.dsTargetCreaturePtr[1], true, "is Healed");
        }

        public static void DustDisappearanceHandler()
        {
            ResolveSpell(GDVars.dsCurrentSpellBeing, 0, false, 0, 0, "is invisible");
        }

        public static void WandMagicMissilesHandler()
        {
            ResolveSpell(GDVars.dsCurrentSpellBeing, 0, false, B(Dice.RollDamageDice(2, 4) + 2), 8, "");
        }

        public static void UnusedSpellHandler()
        {
        }

        public static void ManualBodilyHealthHandler()
        {
            ResolveSpell(GDVars.dsCurrentSpellBeing, 255, true, 0, 0, "is Reading");
        }

        public static void DragonBreathHandler(TCharRecord creature, object p, bool removed)
        {
            bool junk = false;
            sbyte x;
            sbyte y;
            if (GDVars.roundCounter != 0)
                if (Dice.RollDice(1, 100) <= 50)
                    return;
            x = Map.CombatCellX(creature);
            y = Map.CombatCellY(creature);
            PrintNameStringCall(creature, "Breathes!", 10, true);
            GDVars.castTargetMenu(51, true, ref GDVars.breathAim);
            GDVars.gTargetX = SB(x + Pathing.SignOf(unchecked((short)(GDVars.gTargetX - x))));
            GDVars.gTargetY = SB(y + Pathing.SignOf(unchecked((short)(GDVars.gTargetY - y))));
            if (GDVars.gTargetX == x + 1)
                GDVars.gTargetX = SB(GDVars.gTargetX + 1);
            if (GDVars.gTargetY == y + 1)
                GDVars.gTargetY = SB(GDVars.gTargetY + 1);
            Fx.RemoveInvisibility(creature);
            BuildIconFacingsCall(19);
            CombatAnimateMovingIconCall(x, y, GDVars.gTargetX, GDVars.gTargetY, 4, 50);
            LightningAttackAt(GDVars.gTargetX, GDVars.gTargetY, creature.hpMax, C_Save.BREATH, ref junk);
            LightningPath(10, creature.hpMax, C_Save.BREATH, false);
            // Remaining breath charges
            TEffectRec e = (TEffectRec)p;
            if (e.strength > 253)
                e.strength = B(e.strength - 1);
            else
                Fx.RemoveEffect(creature, C_Effect.DRAGON_BREATH, p);
            junk = EndActionsCall(creature);
        }

        public static void RemoveSpellFromScroll(object cr, object it, byte spell)
        {
            byte sp;
            byte found;
            TItemRec item = (TItemRec)it;
            sp = B(spell & C_Spell.ID_MASK);
            found = 0;
            // 3 = High(TItemRec.spells)
            for (byte i = 1; i <= 3; i++)
                if ((item.spells[i] & C_Spell.ID_MASK) == sp)
                    found = i;
            if (found == 0)
                return;
            item.spells[found] = 0;
            // scroll spell-count name component
            item.nameIds[2] = B(item.nameIds[2] - 1);
            if (item.nameIds[2] < C_Item.NAME_WITH_1_SPELL)
                RemoveItemCall((TCharRecord)cr, item);
        }

        public static void PrintCastSpellMsg(TCharRecord creature, string msg, byte spell)
        {
            msg = Str(msg, 40);
            if (GDVars.ifcState == C_Ifc.COMBAT)
            {
                PrintNameStringCall(creature, "Casts a Spell", 10, true);
                ClearBoxCall(0, 23, 39, 23);
                DisplayStringCall(0, 23, LightGreen, "Spell:" + GDConsts.spellNames[spell]);
            }
            else
            {
                ClearBoxCall(1, 18, 38, 22);
                DisplayCreatureNameCall(creature, 1, 19, false);
                DisplayStringCall(B(creature.name.Length + 2), 19, LightGreen, msg);
                DisplayStringCall(1, 20, LightGreen, GDConsts.spellNames[spell]);
                PauseOnlyCall();
                ClearMessageWindowCall();
            }
        }

        internal static byte ChooseSpell(byte menuType, byte selType, ref short idx, ref bool notEmpty, ref TItemRec scroll)
        {
            string s;
            byte result;
            switch (menuType)
            {
                case 0: s = "in Memory"; break;
                case 1: s = "in Spell Book"; break;
                case 2: s = "on Scroll"; break;
                case 3: s = "on Scrolls"; break;
                case 4: s = "to choose from"; break;
                case 5: s = "to be memorized"; break;
                case 6: s = "to be scribed"; break;
                case 7: s = ""; break;
                default: s = ""; break;
            }
            notEmpty = GenerateSpellList(GDVars.curChar, menuType);
            if (notEmpty)
            {
                if (idx < 0)
                {
                    DrawWindowCall(1, 1, 38, 1, 0, Black, "");
                    DisplayCreatureNameCall(GDVars.curChar, 1, 1, true);
                    if (menuType == 7)
                        s = "Available Spells";
                    else
                        s = Str("Spells " + s, 40);
                    DisplayStringCall(B(GDVars.curChar.name.Length + 4), 1, LightGreen, s);
                }
                result = SpellSelection(selType, ref idx, ref scroll);
            }
            else
            {
                scroll = null;
                result = 0;
            }
            return result;
        }

        public static byte SpellMenu(byte menuType, byte selType, ref short idx, ref bool notEmpty)
        {
            TItemRec scroll = null;
            return ChooseSpell(menuType, selType, ref idx, ref notEmpty, ref scroll);
        }

        public static byte CastSpellMenu(ref short idx, ref bool notEmpty, ref TItemRec scroll)
        {
            return ChooseSpell(7, 1, ref idx, ref notEmpty, ref scroll);
        }

        public static void UseItemSpell(TItemRec item, byte spell, bool announce, ref bool done)
        {
            byte id;
            id = spell;
            if (!CharHlp.ItemReadable(item) && (spell > 56))
                id = B(spell - 23);
            // SizeOf(spellTable) div SizeOf(spellTable[1]) = 67
            if ((id < 1) || (id > GDConsts.spellTable.Length - 1))
            {
                done = false;
                return;
            }
            if (CharHlp.ItemReadable(item) && (GDVars.ifcState != C_Ifc.COMBAT) &&
                (GDConsts.spellTable[spell].target == 0))
            {
                ShowCombatOnlySpell(spell);
                done = YesNoMenuCall("Use it? ", LightMagenta, White, LightGreen) == 'Y';
                ClearMessageWindowCall();
                if (done)
                    RemoveSpellFromScroll(GDVars.curChar, item, spell);
                return;
            }
            if ((GDVars.ifcState == C_Ifc.COMBAT) && (GDVars.curChar.quickfight == 0))
                DisplayCombatInterfaceCall();
            if (!CharHlp.ItemReadable(item))
            {
                if (spell > 56)
                    // item effect -> spell id
                    spell = B(spell - 23);
                PrintNameStringCall(GDVars.curChar, "uses an item", 10, false);
                if (GDVars.ifcState == C_Ifc.COMBAT)
                {
                    DisplayStringCall(0, 23, LightGreen, "Item:");
                    PrintItemLabelCall(item, 5, 23);
                }
                else
                    PrintItemLabelCall(item, 1, 22);
                PauseOnlyCall();
                ClearMessageWindowCall();
            }
            GDVars.usingItem = true;
            if (announce)
                PrintCastSpellMsg(GDVars.curChar, "casts", spell);
            CastSpellNC(spell, GDVars.curChar.quickfight != 0, false, ref done);
            GDVars.usingItem = false;
            if (GDVars.ifcState == C_Ifc.COMBAT)
                if (GDConsts.spellTable[spell].whenCast != 0)
                    done = EndActionsCall(GDVars.curChar);
            if (done)
            {
                if (CharHlp.ItemReadable(item))
                    RemoveSpellFromScroll(GDVars.curChar, item, spell);
                else if (item.charges > 0)
                {
                    if (item.quantity > 1)
                        item.quantity = B(item.quantity - 1);
                    else
                    {
                        item.charges = B(item.charges - 1);
                        if (item.charges == 0)
                            RemoveItemCall(GDVars.curChar, item);
                    }
                }
            }
        }

        public static void UseItem(TItemRec item, ref bool done)
        {
            byte spell;
            short idx;
            bool notEmpty = false;
            idx = -1;
            GDVars.usingItem = false;
            spell = 0;
            if (CharHlp.ItemReadable(item))
            {
                dsCurrentScrollPtr = item;
                spell = SpellMenu(2, 1, ref idx, ref notEmpty);
            }
            else if ((item.effect > 0) && (item.special < 0x80))
                spell = item.effect;
            if (spell == 0)
                done = false;
            else
                UseItemSpell(item, spell, false, ref done);
        }
    }
}
