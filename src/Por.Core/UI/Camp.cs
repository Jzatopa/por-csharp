// Source: Talonius pool-of-radiance pinned 7616920a (src/camp.pas). Direct conversion by Claude (subagent of the
// supervising session), on James's instruction without Qwen; no Qwen draft exists for this unit. The first draft is kept
// as evidence/ten-b-2026-10-21/Camp/draft.cs and the repairs as supervisor.patch.
//
// Every unit camp.pas uses is converted (CharGen since checkpoint 124), so there is no NotSupportedException seam of its
// own. Calls into converted units with side effects (TextRend, Border, CharDisp, MenuCore, Portrait, Magic, FxEngine,
// GameTime, UsualSpl, Party.SelectPrevNextChar, SaveLoad, GameIo, CompTool, Options, CharGen, ViewChar, Dice,
// CharHlp.RecalcStats) go through internal delegate fields that default to the converted routine, so tests can observe
// them. Pure helpers (CharHlp.ItemReadable, HasEffect, Party.LastCreature, Clock.SetFromMinutes, StrUtil) are called
// directly.
using System;
using static Por.Core.Types;

namespace Por.Core
{
    public static class Camp
    {
        // ---- converted-unit call points (default to the converted routine) ----
        internal delegate byte MenuHorizontalProc(string query, string menu, byte qCol, byte mCol, byte hlCol, bool reinterpret, bool animate, ref bool ext);
        internal delegate byte MenuVerticalProc(string query, string menu, byte qCol, byte mCol, byte hlCol, sbyte ulCol, sbyte ulRow, sbyte lrCol, sbyte lrRow, TItemRec list, bool addExit, ref bool redraw, ref short idx, ref TItemRec sel);
        internal delegate void AppendMenuLineProc(ref TItemRec head, ref TItemRec tail, string name, bool title);
        internal delegate void FreeStringListProc(ref TItemRec list);
        internal delegate byte SpellMenuProc(byte menuType, byte selType, ref short idx, ref bool notEmpty);
        internal delegate byte CastSpellMenuProc(ref short idx, ref bool notEmpty, ref TItemRec scroll);
        internal delegate void CastSpellNCProc(byte spell, bool b, bool ai, ref bool sel);
        internal delegate void UseItemSpellProc(TItemRec item, byte spell, bool announce, ref bool done);
        internal delegate bool GetUsualSpellsProc(TCharRecord creature, ref TSpellSlots spells);
        internal delegate void SetUsualSpellsProc(TCharRecord creature, ref TSpellSlots spells);
        internal delegate void ViewCharMenuProc(ref bool done);

        internal static Action<byte, byte, byte, string> DisplayStringCall = TextRend.DisplayString;
        internal static Action<byte, bool, string> ShowMessageCall = TextRend.ShowMessage;
        internal static Action ClearBottomLineCall = TextRend.ClearBottomLine;
        internal static Action<sbyte, sbyte, sbyte, sbyte, byte> DrawBorderCall = Border.DrawBorder;
        internal static Action<sbyte, sbyte, sbyte, sbyte> ClearBoxCall = Border.ClearBox;
        internal static Action<sbyte, sbyte, sbyte, sbyte, byte, byte, string> DrawWindowCall = Border.DrawWindow;
        internal static Action<TCharRecord, string, byte, bool> PrintNameStringCall = CharDisp.PrintNameString;
        internal static Action DisplayNoncombatInterfaceCall = CharDisp.DisplayNoncombatInterface;
        internal static Action PrintMiscInfoCall = CharDisp.PrintMiscInfo;
        internal static Action<TCharRecord> DisplayPartyCall = CharDisp.DisplayParty;
        internal static Action MessageWaitCall = CharDisp.MessageWait;
        internal static Action ClearMessageWindowCall = CharDisp.ClearMessageWindow;
        internal static Action<string> PrintBottomLineCall = CharDisp.PrintBottomLine;
        internal static Action<string> PrintMessageWaitCall = CharDisp.PrintMessageWait;
        internal static MenuHorizontalProc MenuHorizontalCall = MenuCore.MenuHorizontal;
        internal static MenuVerticalProc MenuVerticalCall = MenuCore.MenuVertical;
        internal static Func<string, byte, byte, byte, byte> YesNoMenuCall = MenuCore.YesNoMenu;
        internal static AppendMenuLineProc AppendMenuLineCall = MenuCore.AppendMenuLine;
        internal static FreeStringListProc FreeStringListCall = MenuCore.FreeStringList;
        internal static Func<bool> PlayerPortraitsHereCall = Portrait.PlayerPortraitsHere;
        internal static SpellMenuProc SpellMenuCall = Magic.SpellMenu;
        internal static CastSpellMenuProc CastSpellMenuCall = Magic.CastSpellMenu;
        internal static CastSpellNCProc CastSpellNCCall = Magic.CastSpellNC;
        internal static UseItemSpellProc UseItemSpellCall = Magic.UseItemSpell;
        internal static Action CureDiseaseHandlerCall = Magic.CureDiseaseHandler;
        internal static Action CureBlindnessHandlerCall = Magic.CureBlindnessHandler;
        internal static Action RemoveCurseSpellCall = Magic.RemoveCurseSpell;
        internal static Action<TCharRecord> RecalcStatsCall = CharHlp.RecalcStats;
        internal static Func<TCharRecord, byte, bool, bool> RestoreHitPointsCall = FxEngine.RestoreHitPoints;
        internal static Func<bool, bool> CampRestCall = GameTime.CampRest;
        internal static GetUsualSpellsProc GetUsualSpellsCall = UsualSpl.GetUsualSpells;
        internal static SetUsualSpellsProc SetUsualSpellsCall = UsualSpl.SetUsualSpells;
        internal static Action<char> SelectPrevNextCharCall = Party.SelectPrevNextChar;
        internal static Action<bool, bool> RemoveCreatureCall = SaveLoad.RemoveCreature;
        internal static Action SaveGameCall = SaveLoad.SaveGame;
        internal static Action ExitGameCall = GameIo.ExitGame;
        internal static Action ToolsMenuCall = CompTool.ToolsMenu;
        internal static Action OptionsMenuCall = Options.OptionsMenu;
        internal static Action CustomizeIconCall = CharGen.CustomizeIcon;
        internal static Action CustomizePortraitCall = CharGen.CustomizePortrait;
        internal static ViewCharMenuProc ViewCharMenuCall = ViewChar.ViewCharMenu;
        internal static Func<byte, byte, byte> RollDiceCall = Dice.RollDice;

        // ---- constants ----
        // CanDoSpellActivity's activity selector
        internal const byte ACT_CAST = 1;
        internal const byte ACT_MEMORIZE = 2;
        internal const byte ACT_SCRIBE = 3;
        // SizeOf(memorizedSpells), SizeOf(spells), SizeOf(spellsKnown), the spellTable length
        private const int MEM_SIZE = SPELL_SLOT_LAST + 1;
        private const int SCROLL_SIZE = 3;
        private const int KNOWN_SIZE = 56;
        private const int SPELL_TABLE_SIZE = 67;

        private const byte LightCyan = 11;
        private const byte LightGreen = 10;
        private const byte LightMagenta = 13;
        private const byte Yellow = 14;
        private const byte White = 15;

        private static byte B(int v) { return unchecked((byte)v); }
        private static ushort W(int v) { return unchecked((ushort)v); }
        private static string Str(string s, int n) { return s.Length > n ? s.Substring(0, n) : s; }
        private static bool IsKey(byte ch, char k) { return ch == (byte)k; }

        // Preparation time in minutes: 15 per pending spell level, plus four hours if any is pending, or six if any is
        // level 3 or higher; memorizing and scribing (readable items only) both count. The base hours go to spellPrepTime.
        internal static ushort CampSpellPrepTime(TCharRecord creature)
        {
            byte maxMem;
            byte maxScr;
            byte cntMem;
            byte cntScr;
            TItemRec p;
            byte lvl;
            byte bse;
            maxMem = 0;
            maxScr = 0;
            cntMem = 0;
            cntScr = 0;
            for (int i = 0; i <= MEM_SIZE - 1; i++)
                if (creature.memorizedSpells[i] > C_Spell.ID_MASK)
                {
                    lvl = B(GDConsts.spellTable[creature.memorizedSpells[i] & C_Spell.ID_MASK].levelSp);
                    if (lvl > maxMem) maxMem = lvl;
                    cntMem = B(cntMem + lvl);
                }
            p = creature.itemsAddr;
            while (p != null)
            {
                if (CharHlp.ItemReadable(p))
                    for (int i = 1; i <= SCROLL_SIZE; i++)
                        if (p.spells[i] > C_Spell.ID_MASK)
                        {
                            lvl = B(GDConsts.spellTable[p.spells[i] & C_Spell.ID_MASK].levelSp);
                            if (lvl > maxScr) maxScr = lvl;
                            cntScr = B(cntScr + lvl);
                        }
                p = p.next;
            }
            bse = 0;
            if ((cntMem > 0) || (cntScr > 0))
                bse = 4;
            if ((maxMem > 2) || (maxScr > 2))
                bse = 6;
            creature.spellPrepTime = bse;
            return W(bse * 60 + cntMem * 15 + cntScr * 15);
        }

        // Clear every slot marked for memorizing and the base prep time.
        internal static void ResetMemorize(TCharRecord creature)
        {
            for (int i = 0; i <= MEM_SIZE - 1; i++)
                if (creature.memorizedSpells[i] > C_Spell.ID_MASK)
                    creature.memorizedSpells[i] = 0;
            creature.spellPrepTime = 0;
        }

        // Strip the pending flag from every scroll spell on every readable item.
        internal static void ResetScribe(TCharRecord creature)
        {
            TItemRec p;
            p = creature.itemsAddr;
            while (p != null)
            {
                if (CharHlp.ItemReadable(p))
                    for (int i = 1; i <= SCROLL_SIZE; i++)
                        p.spells[i] = B(p.spells[i] & C_Spell.ID_MASK);
                p = p.next;
            }
        }

        // Run ResetMemorize and ResetScribe on every creature in the list.
        internal static void ResetPartySpellActivities()
        {
            TCharRecord p;
            p = GDVars.creatureList;
            while (p != null)
            {
                ResetMemorize(p);
                ResetScribe(p);
                p = p.nextCharAddr;
            }
        }

        // How many of the current character's memorized and pending spells are of this class and level.
        internal static byte SlotsUsed(sbyte lvl, sbyte typ)
        {
            byte count;
            byte id;
            count = 0;
            for (int i = 0; i <= MEM_SIZE - 1; i++)
            {
                id = B(GDVars.curChar.memorizedSpells[i] & C_Spell.ID_MASK);
                if ((id >= 1) && (id <= SPELL_TABLE_SIZE))
                    if ((GDConsts.spellTable[id].levelSp == lvl) && (GDConsts.spellTable[id].castClassSp == typ))
                        count = B(count + 1);
            }
            return count;
        }

        // Put spell id, pending, in the current character's first empty memorized-spell slot.
        internal static void PutPending(byte id, ref bool placed)
        {
            int i;
            i = 0;
            while ((i < MEM_SIZE) && (GDVars.curChar.memorizedSpells[i] != 0))
                i++;
            placed = i < MEM_SIZE;
            if (placed)
                GDVars.curChar.memorizedSpells[i] = B(id + C_Spell.PENDING_FLAG);
        }

        // The allowance for a class and level minus the memorized and pending spells of it, or 0 when none is left.
        internal static byte SpellSlotsRemaining(sbyte lvl, sbyte typ)
        {
            byte count;
            count = SlotsUsed(lvl, typ);
            if (count >= GDVars.curChar.slots[typ, lvl])
                return 0;
            else
                return B(GDVars.curChar.slots[typ, lvl] - count);
        }

        // Whether the current character can cast, memorize or scribe; on failure, its name and the reason.
        internal static bool CanDoSpellActivity(byte activity)
        {
            string msg;
            msg = "";
            if ((activity == ACT_CAST) && (GDVars.zone0.antimagic > 0))
                msg = "cannot cast spells in this area";
            else if ((GDVars.curChar.status == C_Status.ANIMATED) || (GDVars.curChar.enabled == 0))
            {
                msg = "is in no condition to ";
                switch (activity)
                {
                    case ACT_CAST: msg = Str(msg + "cast any spells", 40); break;
                    case ACT_MEMORIZE: msg = Str(msg + "memorize spells", 40); break;
                    case ACT_SCRIBE: msg = Str(msg + "scribe any scrolls", 40); break;
                }
            }
            if (msg == "")
                return true;
            else
            {
                PrintNameStringCall(GDVars.curChar, msg, 10, true);
                return false;
            }
        }

        // Loop the Cast list; cast a memorized spell through Magic.CastSpellNC, or a scroll spell from its scroll as Use
        // does, followed by CharHlp.RecalcStats. An empty list prints 'has no spells memorized'.
        public static void NoncombatCastMenu()
        {
            short idx;
            bool notEmpty = false;
            bool redraw;
            // CastSpellNC's var out, unread
            bool castSel = false;
            byte spellId;
            // the scroll the spell is cast from, nil for a memorized one
            TItemRec scroll = null;
            TCharRecord caster;
            redraw = false;
            idx = -1;
            GDVars.castTarget = null;
            if (CanDoSpellActivity(ACT_CAST))
            {
                do
                {
                    spellId = CastSpellMenuCall(ref idx, ref notEmpty, ref scroll);
                    if (spellId != C_Spell.NONE)
                    {
                        redraw = true;
                        DrawBorderCall(C_Msg.TEXT_LEFT, C_Msg.MESSAGE_TOP, C_Msg.TEXT_RIGHT, C_Msg.TEXT_BOTTOM, 0);
                        ClearBoxCall(C_Msg.TEXT_LEFT, C_Msg.MESSAGE_TOP, C_Msg.TEXT_RIGHT, C_Msg.TEXT_BOTTOM);
                        if (scroll == null)
                            CastSpellNCCall(spellId, false, true, ref castSel);
                        else
                        {
                            caster = GDVars.curChar;
                            UseItemSpellCall(scroll, spellId, true, ref castSel);
                            // recount the items and their weight, as Items > Use does: the scroll may be gone
                            RecalcStatsCall(caster);
                        }
                        idx = -1;
                    }
                    else
                    {
                        if (notEmpty)
                            redraw = true;
                        else
                            PrintNameStringCall(GDVars.curChar, "has no spells memorized", 10, true);
                    }
                } while (spellId != C_Spell.NONE);
            }
            if (redraw)
                DisplayNoncombatInterfaceCall();
        }

        // Show remaining spell slots by class and level; True whenever the character has any spell allowance.
        internal static bool DisplaySpellSlots()
        {
            string s = "";
            string[,] slotStr = new string[4, 2];
            byte[] canDo = new byte[2];
            byte col;
            byte row;
            bool any;
            any = false;
            for (int typ = 0; typ <= 1; typ++)
            {
                canDo[typ] = 0;
                for (int lvl = 1; lvl <= 3; lvl++)
                {
                    slotStr[lvl, typ] = Str(StrUtil.NumStr(SpellSlotsRemaining((sbyte)lvl, (sbyte)typ)), 2);
                    if (GDVars.curChar.slots[typ, lvl] == 0)
                        slotStr[lvl, typ] = " ";
                    else
                    {
                        canDo[typ] = 1;
                        any = true;
                    }
                }
            }
            if (any)
            {
                PrintNameStringCall(GDVars.curChar, "can memorize:", 10, false);
                row = 3;
                for (int typ = 0; typ <= 1; typ++)
                {
                    if (canDo[typ] != 0)
                    {
                        switch (typ)
                        {
                            case 0: s = "    Cleric Spells:"; break;
                            case 1: s = "Magic-User Spells:"; break;
                        }
                        DisplayStringCall(1, B(row + 17), LightGreen, s);
                        col = 19;
                        for (int lvl = 1; lvl <= 3; lvl++)
                        {
                            DisplayStringCall(B(col + 1), B(row + 17), LightGreen, slotStr[lvl, typ]);
                            col = B(col + 3);
                        }
                        row = B(row + 1);
                    }
                }
            }
            return any;
        }

        // Seed the rest clock from the party's longest spell-prep time, run GameTime.CampRest, then clear the clock.
        internal static void CampRestMenu(ref bool interrupted)
        {
            TCharRecord p;
            ushort total;
            ushort t;
            total = 0;
            p = GDVars.creatureList;
            while (p != null)
            {
                t = CampSpellPrepTime(p);
                if (total < t) total = t;
                p = p.nextCharAddr;
            }
            Clock.SetFromMinutes(GDVars.dsRestingTimeGbvm, total);
            interrupted = CampRestCall(true);
            ClearClock(GDVars.dsRestingTimeGbvm);
            PrintMiscInfoCall();
        }

        // FillChar(clock, SizeOf(clock), 0)
        private static void ClearClock(TClock clock)
        {
            for (int i = 0; i < clock.Data.Length; i++)
                clock.Data[i] = 0;
        }

        // Sort memorized spells by their masked IDs, keeping the pending flag attached to each spell.
        internal static void SortMemorizedSpells()
        {
            byte tmp;
            for (int i = 0; i <= MEM_SIZE - 2; i++)
                for (int j = i; j <= MEM_SIZE - 1; j++)
                    if ((GDVars.curChar.memorizedSpells[i] & C_Spell.ID_MASK) > (GDVars.curChar.memorizedSpells[j] & C_Spell.ID_MASK))
                    {
                        tmp = GDVars.curChar.memorizedSpells[i];
                        GDVars.curChar.memorizedSpells[i] = GDVars.curChar.memorizedSpells[j];
                        GDVars.curChar.memorizedSpells[j] = tmp;
                    }
        }

        // Remember the current character's memorized and pending spells as its usual spells.
        internal static void RememberUsualSpells()
        {
            TSpellSlots spells = new TSpellSlots();
            for (int i = 0; i <= MEM_SIZE - 1; i++)
                spells[i] = B(GDVars.curChar.memorizedSpells[i] & C_Spell.ID_MASK);
            SetUsualSpellsCall(GDVars.curChar, ref spells);
        }

        // True when the current character has a slot of this class and level left; bounds the level and class.
        internal static bool SlotFree(sbyte lvl, sbyte typ)
        {
            bool result = false;
            if ((lvl >= 1) && (lvl <= 3) && (typ >= 0) && (typ <= 1))
                result = SlotsUsed(lvl, typ) < GDVars.curChar.slots[typ, lvl];
            return result;
        }

        // Mark as pending each usual spell the character lacks, still knows and has a free slot for, in turn.
        internal static void QueueUsualSpells()
        {
            TSpellSlots usual = new TSpellSlots();
            TSpellSlots have = new TSpellSlots();
            byte id;
            bool found;
            bool placed = false;
            if (GetUsualSpellsCall(GDVars.curChar, ref usual))
            {
                for (int i = 0; i <= MEM_SIZE - 1; i++)
                    have[i] = B(GDVars.curChar.memorizedSpells[i] & C_Spell.ID_MASK);
                for (int i = 0; i <= MEM_SIZE - 1; i++)
                {
                    id = usual[i];
                    if ((id > 0) && (id <= KNOWN_SIZE))
                    {
                        // a copy already memorized or pending covers this one
                        found = false;
                        for (int j = 0; j <= MEM_SIZE - 1; j++)
                            if ((!found) && (have[j] == id))
                            {
                                have[j] = 0;
                                found = true;
                            }
                        if ((!found) && (GDVars.curChar.spellsKnown[id] != 0))
                            if (SlotFree(GDConsts.spellTable[id].levelSp, GDConsts.spellTable[id].castClassSp))
                                PutPending(id, ref placed);
                    }
                }
                SortMemorizedSpells();
            }
        }

        // The Memorize menu: queue the usual spells when nothing is pending, confirm, then pick spells while slots remain.
        internal static void CampMemorizeMenu()
        {
            bool done;
            bool notEmpty = false;
            bool redraw;
            byte spellId;
            short idx;
            bool pending;
            bool placed = false;
            if (CanDoSpellActivity(ACT_MEMORIZE))
            {
                done = false;
                pending = false;
                for (int i = 0; i <= MEM_SIZE - 1; i++)
                    if (GDVars.curChar.memorizedSpells[i] > C_Spell.ID_MASK)
                        pending = true;
                if (!pending)
                    QueueUsualSpells();
                idx = -1;
                spellId = SpellMenuCall(5, 5, ref idx, ref notEmpty);
                redraw = true;
                if (notEmpty)
                {
                    if (YesNoMenuCall("Memorize These Spells? ", Yellow, LightGreen, White) == (byte)'N')
                        ResetMemorize(GDVars.curChar);
                    else
                    {
                        RememberUsualSpells();
                        done = true;
                    }
                }
                else
                    redraw = false;
                idx = -1;
                while (done == false)
                {
                    done = DisplaySpellSlots() == false;
                    if (done)
                        PrintNameStringCall(GDVars.curChar, "cannot memorize any spells", 10, true);
                    else
                    {
                        spellId = SpellMenuCall(1, 2, ref idx, ref notEmpty);
                        redraw = true;
                        if (spellId == C_Spell.NONE)
                            done = true;
                        else if (SpellSlotsRemaining(GDConsts.spellTable[spellId].levelSp,
                                                     GDConsts.spellTable[spellId].castClassSp) > 0)
                        {
                            // Sorting puts empty spell slots first; a full array takes nothing
                            PutPending(spellId, ref placed);
                            if (placed)
                                SortMemorizedSpells();
                        }
                    }
                }
                if (idx != -1)
                {
                    idx = -1;
                    spellId = SpellMenuCall(5, 5, ref idx, ref notEmpty);
                    if (notEmpty)
                        if (YesNoMenuCall("Memorize these spells? ", Yellow, LightGreen, White) == (byte)'N')
                            ResetMemorize(GDVars.curChar);
                        else
                            RememberUsualSpells();
                }
                if (redraw)
                    DisplayNoncombatInterfaceCall();
            }
        }

        // The Scribe menu: confirm pending scribing, then mark scroll spells, rejecting known, pending or unavailable ones.
        internal static void CampScribeMenu()
        {
            bool done;
            bool notEmpty = false;
            bool redraw;
            byte spellId;
            int i;
            short idx;
            TItemRec p;
            bool found;
            if (!CanDoSpellActivity(ACT_SCRIBE))
                return;
            if (GDVars.curChar.classLevels[C_Level.MAGE] <= 0)
            {
                ShowMessageCall(LightGreen, true, "Scribing a scroll requires arcane training.");
                MessageWaitCall();
                ClearMessageWindowCall();
            }
            else
            {
                done = false;
                idx = -1;
                spellId = SpellMenuCall(6, 0, ref idx, ref notEmpty);
                redraw = true;
                if (notEmpty)
                {
                    if (YesNoMenuCall("Scribe These Spells? ", Yellow, LightGreen, White) == (byte)'N')
                        ResetScribe(GDVars.curChar);
                    else
                        done = true;
                }
                else
                    redraw = false;
                idx = -1;
                while (done == false)
                {
                    spellId = SpellMenuCall(3, 3, ref idx, ref notEmpty);
                    if (spellId == C_Spell.NONE)
                    {
                        done = true;
                        if (notEmpty == false)
                            PrintNameStringCall(GDVars.curChar, "has no copyable scrolls.", 10, true);
                        else
                            redraw = true;
                    }
                    else if ((spellId > KNOWN_SIZE) || (GDConsts.spellTable[spellId].levelSp < 1) ||
                             (GDConsts.spellTable[spellId].levelSp > 3) || (GDConsts.spellTable[spellId].castClassSp < 0) ||
                             (GDConsts.spellTable[spellId].castClassSp > 1))
                    {
                        // a scroll byte past the spells a character can know, or a spell with no memorize slot
                        redraw = true;
                        PrintBottomLineCall("You can not scribe that spell.");
                    }
                    else
                    {
                        redraw = true;
                        if (GDVars.curChar.spellsKnown[spellId] != 0)
                            PrintBottomLineCall("You already know that spell.");
                        else
                        {
                            p = GDVars.curChar.itemsAddr;
                            found = false;
                            while ((p != null) && (found == false))
                            {
                                if (CharHlp.ItemReadable(p))
                                    for (i = 1; i <= SCROLL_SIZE; i++)
                                        if ((p.spells[i] > C_Spell.ID_MASK) &&
                                            ((p.spells[i] & C_Spell.ID_MASK) == spellId))
                                        {
                                            PrintBottomLineCall("You are already scribing that spell.");
                                            found = true;
                                        }
                                p = p.next;
                            }
                            if (found == false)
                            {
                                if (GDVars.curChar.slots[GDConsts.spellTable[spellId].castClassSp,
                                                         GDConsts.spellTable[spellId].levelSp] > 0)
                                {
                                    p = GDVars.curChar.itemsAddr;
                                    while ((p != null) && (found == false))
                                    {
                                        i = 1;
                                        do
                                        {
                                            if (p.spells[i] == spellId)
                                            {
                                                p.spells[i] = B(p.spells[i] | C_Spell.PENDING_FLAG);
                                                found = true;
                                            }
                                            i++;
                                        } while (!((i > SCROLL_SIZE) || found));
                                        p = p.next;
                                    }
                                }
                                else
                                    PrintBottomLineCall("You can not scribe that spell.");
                            }
                        }
                    }
                }
                if (idx != -1)
                {
                    idx = -1;
                    spellId = SpellMenuCall(6, 0, ref idx, ref notEmpty);
                    if (notEmpty)
                        if (YesNoMenuCall("Scribe these spells? ", Yellow, LightGreen, White) == (byte)'N')
                            ResetScribe(GDVars.curChar);
                }
                if (redraw)
                    DisplayNoncombatInterfaceCall();
            }
        }

        // The label CampDisplayMenu lists for effect id, or null when the effect is hidden.
        private static string EffectLabel(byte id)
        {
            string s;
            bool found;
            int k;
            if ((id == C_Effect.BLESS) || (id == C_Effect.CURSE) || (id == C_Effect.DETECT_MAGIC) ||
                ((id >= C_Effect.PROT_EVIL) && (id <= C_Effect.FRIENDS)) || (id == C_Effect.READ_MAGIC) ||
                (id == C_Effect.SHIELD) || ((id >= C_Effect.FIND_TRAPS) && (id <= C_Effect.INVISIBLE)) ||
                (id == C_Effect.MIRROR_IMAGE) || (id == C_Effect.ENFEEBLE) ||
                ((id >= C_Effect.ANIMATE_DEAD) && (id <= C_Effect.DISEASED)) ||
                ((id >= C_Effect.BESTOW_CURSE) && (id <= C_Effect.HASTED)) || (id == C_Effect.PROT_MISSILES) ||
                (id == C_Effect.SLOWED) || (id == C_Effect.PROT_EVIL_10FT) || (id == C_Effect.PROT_GOOD_10FT) ||
                (id == C_Effect.PRAYER) || ((id >= C_Effect.SNAKE_CHARM) && (id <= C_Effect.SLEEP)))
            {
                // Find the named spell that produces this effect. Only IDs 1..56 have spell names.
                s = "";
                k = 1;
                found = false;
                while ((k <= 56) && (found == false))
                    if (GDConsts.spellTable[k].effect == id)
                    {
                        s = Str(GDConsts.spellNames[k], 40);
                        found = true;
                    }
                    else
                        k++;
                if (found == false)
                    s = "Funky--" + StrUtil.NumStr(id);
                return s;
            }
            switch (id)
            {
                case C_Effect.TRAIN_START: return "Studying Manual of Bodily Health";
                case C_Effect.TRAIN_MANUAL: return "Training with Manual of Bodily Health";
                case C_Effect.FEATHER_FALL: return "Feather Fall";
                case C_Effect.HELPLESS: return "Helpless";
                case C_Effect.ALLIED_PRAYER: return "Prayer";
                case C_Effect.DISEASE_HP: return "Cause Disease";
                case C_Effect.ROT_NO_HEAL: return "Dreaded Mummy Disease";
                case C_Effect.DRAGON_AURA: return "Repulsed";
                case C_Effect.POISONED: return "Poisoned";
                case C_Effect.TROLL_REGEN: return "Regenerating";
                case C_Effect.RING_FIRE_RES: return "Fire Resistance";
                case C_Effect.DUST_DISAPPEAR: return "Invisible";
                case C_Effect.CAMOUFLAGE: return "Camouflaged";
                case C_Effect.DISPLACED: return "Displaced";
            }
            return null;
        }

        // Build the effect-display list with a heading for each character, and show it.
        internal static void CampDisplayMenu()
        {
            TEffectRec effect;
            TItemRec head;
            TItemRec cur;
            TCharRecord ch;
            byte cnt;
            string s;
            bool menuRedraw;
            short idx;
            byte c;
            head = new TItemRec();
            cur = head;
            cur.inUse = 0;
            cur.name = " ";
            cur.next = null;
            ch = GDVars.creatureList;
            while (ch != null)
            {
                cnt = 0;
                AppendMenuLineCall(ref head, ref cur, ch.name, true);
                effect = ch.effectsAddr;
                while (effect != null)
                {
                    s = EffectLabel(effect.id);
                    if (s != null)
                    {
                        cnt = B(cnt + 1);
                        AppendMenuLineCall(ref head, ref cur, " " + s, false);
                    }
                    effect = effect.next;
                }
                if (cnt == 0)
                    AppendMenuLineCall(ref head, ref cur, " <No Spell Effects>", false);
                AppendMenuLineCall(ref head, ref cur, " ", false);
                ch = ch.nextCharAddr;
            }
            menuRedraw = true;
            idx = 0;
            DrawWindowCall(C_Msg.TEXT_LEFT, C_Msg.TEXT_TOP, C_Msg.TEXT_RIGHT, C_Msg.TEXT_BOTTOM, 0, White, "Display");
            c = MenuVerticalCall("", "", LightCyan, LightGreen, White, 1, 4, 38, 22, head, true, ref menuRedraw, ref idx, ref cur);
            FreeStringListCall(ref head);
            DisplayNoncombatInterfaceCall();
        }

        // The magic menu ('Cast Memorize Scribe Display Rest Exit'); extended keys switch character.
        internal static void CampMagicMenu(ref bool interrupted)
        {
            byte c;
            bool ext = false;
            c = (byte)' ';
            while ((interrupted == false) && !((c == C_Keys.NONE) || IsKey(c, 'E')))
            {
                c = MenuHorizontalCall("", GDConsts.magicMenu, LightMagenta, LightGreen, White, true, true, ref ext);
                if (ext)
                {
                    SelectPrevNextCharCall((char)c);
                    DisplayPartyCall(GDVars.curChar);
                }
                else
                    switch (c)
                    {
                        case (byte)'C': NoncombatCastMenu(); break;
                        case (byte)'M': CampMemorizeMenu(); break;
                        case (byte)'S': CampScribeMenu(); break;
                        case (byte)'D': CampDisplayMenu(); break;
                        case (byte)'R': CampRestMenu(ref interrupted); break;
                    }
            }
        }

        // Move the current character one position earlier in the party list; if already first, it wraps to the end.
        internal static void OrderMoveUp()
        {
            TCharRecord prev;
            TCharRecord p;
            TCharRecord savedNext;
            p = GDVars.creatureList;
            prev = null;
            if (p == GDVars.curChar)
                p = Party.LastCreature();
            else
                while (p.nextCharAddr != GDVars.curChar)
                {
                    prev = p;
                    p = p.nextCharAddr;
                }
            savedNext = GDVars.curChar.nextCharAddr;
            if (GDVars.creatureList == GDVars.curChar)
            {
                // first -> last (p walked to the tail above)
                if (savedNext != null)
                    GDVars.creatureList = savedNext;
                p.nextCharAddr = GDVars.curChar;
                GDVars.curChar.nextCharAddr = null;
            }
            else
            {
                // p = predecessor: unlink and reinsert one earlier
                p.nextCharAddr = savedNext;
                GDVars.curChar.nextCharAddr = p;
                if ((p == GDVars.creatureList) || (prev == null))
                    GDVars.creatureList = GDVars.curChar;
                else
                    prev.nextCharAddr = GDVars.curChar;
            }
        }

        // Move the current character one position later in the party list; if already last, it wraps to the front.
        internal static void OrderMoveDown()
        {
            TCharRecord p;
            TCharRecord next;
            p = GDVars.creatureList;
            if (p != GDVars.curChar)
                while (p.nextCharAddr != GDVars.curChar)
                    p = p.nextCharAddr;
            next = GDVars.curChar.nextCharAddr;
            if (next == null)
            {
                GDVars.curChar.nextCharAddr = GDVars.creatureList;
                GDVars.creatureList = GDVars.curChar;
                p.nextCharAddr = next;
            }
            else
            {
                GDVars.curChar.nextCharAddr = next.nextCharAddr;
                if (GDVars.creatureList == GDVars.curChar)
                    GDVars.creatureList = next;
                if (p != GDVars.curChar)
                    p.nextCharAddr = next;
                next.nextCharAddr = GDVars.curChar;
            }
        }

        // Reorder the party through Select and Place modes.
        internal static void CampOrderMenu()
        {
            sbyte selected;
            bool ext = false;
            byte c;
            selected = 0;
            c = (byte)' ';
            while (!((c == C_Keys.NONE) || IsKey(c, 'E')))
            {
                c = MenuHorizontalCall("Party Order: ", GDConsts.orderMenus[selected], LightMagenta, LightGreen,
                                       White, true, true, ref ext);
                if (ext)
                {
                    if (selected == 0)
                    {
                        SelectPrevNextCharCall((char)c);
                        DisplayPartyCall(GDVars.curChar);
                    }
                    else
                    {
                        switch (c)
                        {
                            case C_Keys.HOME:
                            case C_Keys.UP:
                                OrderMoveUp();
                                break;
                            case C_Keys.END_KEY:
                            case C_Keys.DOWN:
                                OrderMoveDown();
                                break;
                        }
                        DisplayPartyCall(GDVars.curChar);
                    }
                }
                else
                    if (IsKey(c, 'P') || IsKey(c, 'S'))
                    {
                        selected = (sbyte)(selected == 0 ? 1 : 0);
                        if (selected != 0)
                            PrintNameStringCall(GDVars.curChar, "has been selected", 10, false);
                        else
                            ClearMessageWindowCall();
                    }
            }
        }

        // Drop the current character from the party after confirmation; for the last member, offer to quit to DOS.
        internal static void CampDropMenu()
        {
            if ((GDVars.curChar.nextCharAddr == null) && (GDVars.curChar == GDVars.creatureList))
            {
                // last member: quit-to-DOS confirm
                if (YesNoMenuCall("quit TO DOS: ", Yellow, LightGreen, White) == (byte)'Y')
                {
                    RemoveCreatureCall(false, true);
                    ExitGameCall();
                }
                else
                    return;
                return;
            }
            PrintNameStringCall(GDVars.curChar, "will be gone", 10, false);
            if (YesNoMenuCall("Drop from party? ", Yellow, LightGreen, White) == (byte)'Y')
            {
                if (GDVars.curChar.enabled != 0)
                    PrintNameStringCall(GDVars.curChar, "bids you farewell.", 10, true);
                else
                    PrintNameStringCall(GDVars.curChar, "is dumped in a ditch.", 10, true);
                RemoveCreatureCall(false, true);
                ClearBoxCall(17, 1, 38, 11);
                DisplayPartyCall(GDVars.curChar);
            }
            else
                PrintNameStringCall(GDVars.curChar, "Breathes A sigh of relief.", 10, true);
        }

        // The Alter menu ('Order Drop Char Settings Exit').
        internal static void CampAlterMenu()
        {
            byte c;
            byte c2;
            bool ext = false;
            string s;
            c = (byte)' ';
            while (!((c == C_Keys.NONE) || IsKey(c, 'E')))
            {
                c = MenuHorizontalCall("Alter: ", GDConsts.alterMenu, LightMagenta, LightGreen, White, true, true,
                                       ref ext);
                if (ext)
                {
                    SelectPrevNextCharCall((char)c);
                    DisplayPartyCall(GDVars.curChar);
                }
                else
                    switch (c)
                    {
                        case (byte)'O': CampOrderMenu(); break;
                        case (byte)'D': CampDropMenu(); break;
                        case (byte)'C':
                            do
                            {
                                if (PlayerPortraitsHereCall())
                                    s = "Icon Portrait Exit";
                                else
                                    s = "Icon Exit";
                                c2 = MenuHorizontalCall("Character: ", s, LightMagenta, LightGreen, White, true, true, ref ext);
                                if (ext)
                                {
                                    SelectPrevNextCharCall((char)c2);
                                    DisplayPartyCall(GDVars.curChar);
                                }
                                else
                                    switch (c2)
                                    {
                                        case (byte)'I':
                                            CustomizeIconCall();
                                            DisplayNoncombatInterfaceCall();
                                            break;
                                        case (byte)'P':
                                            if (PlayerPortraitsHereCall())
                                            {
                                                CustomizePortraitCall();
                                                DisplayNoncombatInterfaceCall();
                                            }
                                            break;
                                    }
                            } while (!((!ext) && ((c2 == C_Keys.NONE) || IsKey(c2, 'E'))));
                            break;
                        case (byte)'S':
                            OptionsMenuCall();
                            DisplayNoncombatInterfaceCall();
                            break;
                    }
            }
        }

        // Fix: rest while the party's clerics cure and heal everyone (see camp.pas for the rules).
        internal static void CampFixMenu(ref bool interrupted)
        {
            // the members planned for; a longer creature list is not looked at past this
            const int MAX_PARTY = 8;
            // Conditions, cured in this order.
            const byte FIX_DISEASE = 1;
            const byte FIX_BLIND = 2;
            const byte FIX_CURSE = 3;
            TCharRecord[] members = new TCharRecord[MAX_PARTY + 1];
            byte[] l1Free = new byte[MAX_PARTY + 1];
            byte[] l3Free = new byte[MAX_PARTY + 1];
            byte[] l1Casts = new byte[MAX_PARTY + 1];
            byte[] l3Casts = new byte[MAX_PARTY + 1];
            bool[,] cures = new bool[MAX_PARTY + 1, FIX_CURSE + 1];
            TCharRecord p;
            byte n;
            bool anyCure;
            bool anyCondition;
            ushort need;
            ushort wanted;
            ushort healCasts;
            ushort healPool;
            ushort total;
            ushort hp;
            bool done;
            byte clerics;
            byte ableClerics;

            // Fix treats the statuses RestoreHitPoints accepts.
            bool Treatable(TCharRecord creature)
            {
                return (creature.status == C_Status.OKAY) || (creature.status == C_Status.ANIMATED) ||
                       (creature.status == C_Status.UNCONSCIOUS) || (creature.status == C_Status.DYING);
            }

            bool HasCondition(TCharRecord creature, byte cond)
            {
                TEffectRec effect;
                switch (cond)
                {
                    case FIX_DISEASE:
                        return CharHlp.HasEffect(creature, C_Effect.DISEASED, out effect) ||
                               CharHlp.HasEffect(creature, C_Effect.DISEASE_STR, out effect) ||
                               CharHlp.HasEffect(creature, C_Effect.ROT_NO_HEAL, out effect);
                    case FIX_BLIND:
                        return CharHlp.HasEffect(creature, C_Effect.BLIND, out effect);
                    default:
                        return CharHlp.HasEffect(creature, C_Effect.BESTOW_CURSE, out effect);
                }
            }

            byte CureSpell(byte cond)
            {
                switch (cond)
                {
                    case FIX_DISEASE: return C_Spell.CURE_DISEASE;
                    case FIX_BLIND: return C_Spell.CURE_BLINDNESS;
                    default: return C_Spell.REMOVE_CURSE;
                }
            }

            // Minutes member j needs to memorize its casts again.
            ushort CasterTime(byte j)
            {
                ushort t;
                t = 0;
                if (l3Casts[j] > 0)
                    t = 360;
                else if (l1Casts[j] > 0)
                    t = 240;
                return W(t + 15 * l1Casts[j] + 45 * l3Casts[j]);
            }

            // Give one cast to the least busy member with a free slot for it: spell 0 is Cure Light Wounds, any other
            // a level-3 cure the member must know. False when nobody can cast it.
            bool Assign(byte spell)
            {
                byte best;
                bool free;
                best = 0;
                for (int j = 1; j <= n; j++)
                {
                    if (spell == 0)
                        free = l1Free[j] > 0;
                    else
                        free = (l3Free[j] > 0) && (members[j].spellsKnown[spell] != 0);
                    if (free)
                        if ((best == 0) || (CasterTime(B(j)) < CasterTime(best))) best = B(j);
                }
                if (best != 0)
                    if (spell == 0)
                    {
                        l1Free[best] = B(l1Free[best] - 1);
                        l1Casts[best] = B(l1Casts[best] + 1);
                    }
                    else
                    {
                        l3Free[best] = B(l3Free[best] - 1);
                        l3Casts[best] = B(l3Casts[best] + 1);
                    }
                return best != 0;
            }

            interrupted = false;
            n = 0;
            need = 0;
            clerics = 0;
            ableClerics = 0;
            p = GDVars.creatureList;
            while ((p != null) && (n < MAX_PARTY))
            {
                n = B(n + 1);
                members[n] = p;
                l1Free[n] = 0;
                l3Free[n] = 0;
                l1Casts[n] = 0;
                l3Casts[n] = 0;
                if (p.classLevels[C_Level.CLERIC] > 0)
                {
                    clerics = B(clerics + 1);
                    if (p.status == C_Status.OKAY) ableClerics = B(ableClerics + 1);
                }
                if (p.status == C_Status.OKAY)
                {
                    if (p.spellsKnown[C_Spell.CURE_LIGHT_WOUNDS] != 0) l1Free[n] = p.clericSpells[1];
                    l3Free[n] = p.clericSpells[3];
                }
                if (Treatable(p) && (p.hpCur < p.hpMax)) need = W(need + (p.hpMax - p.hpCur));
                p = p.nextCharAddr;
            }
            if (clerics == 0)
            {
                PrintMessageWaitCall("No clerics available to heal.");
                return;
            }
            if (ableClerics == 0)
            {
                PrintMessageWaitCall("No cleric is in a condition to heal.");
                return;
            }
            // Plan the cures, then as many Cure Light Wounds as the wounds need at 4.5 hp each.
            anyCure = false;
            anyCondition = false;
            for (byte kind = FIX_DISEASE; kind <= FIX_CURSE; kind++)
                for (int i = 1; i <= n; i++)
                {
                    cures[i, kind] = false;
                    if (Treatable(members[i]))
                        if (HasCondition(members[i], kind))
                        {
                            anyCondition = true;
                            if (Assign(CureSpell(kind)))
                            {
                                cures[i, kind] = true;
                                anyCure = true;
                            }
                        }
                }
            wanted = W((2 * need + 8) / 9);
            healCasts = 0;
            while (healCasts < wanted)
                if (Assign(0))
                    healCasts = W(healCasts + 1);
                else
                    wanted = healCasts;
            if ((healCasts == 0) && !anyCure)
            {
                // the dead and the like are not Fix's to treat, so they leave nothing to do
                if ((need == 0) && !anyCondition)
                    PrintMessageWaitCall("Further healing would do no good.");
                else
                    PrintMessageWaitCall("Further healing isn't possible.");
                return;
            }
            total = 0;
            for (int i = 1; i <= n; i++)
                if (total < CasterTime(B(i))) total = CasterTime(B(i));
            Clock.SetFromMinutes(GDVars.dsRestingTimeGbvm, total);
            interrupted = CampRestCall(false);
            done = (GDVars.dsRestingTimeGbvm[C_Time.DAYS] == 0) && (GDVars.dsRestingTimeGbvm[C_Time.HOURS] == 0) &&
                   (GDVars.dsRestingTimeGbvm[C_Time.TURNS] == 0) && (GDVars.dsRestingTimeGbvm[C_Time.MINUTES] == 0);
            ClearClock(GDVars.dsRestingTimeGbvm);
            if (done && !interrupted)
            {
                for (int i = 1; i <= n; i++)
                    for (byte kind = FIX_DISEASE; kind <= FIX_CURSE; kind++)
                        if (cures[i, kind])
                        {
                            GDVars.dsTargetCreaturePtr[1] = members[i];
                            switch (kind)
                            {
                                case FIX_DISEASE: CureDiseaseHandlerCall(); break;
                                case FIX_BLIND: CureBlindnessHandlerCall(); break;
                                case FIX_CURSE: RemoveCurseSpellCall(); break;
                            }
                        }
                healPool = 0;
                for (int cast = 1; cast <= healCasts; cast++) healPool = W(healPool + RollDiceCall(1, 8));
                for (int i = 1; i <= n; i++)
                {
                    p = members[i];
                    if (Treatable(p) && (p.hpCur < p.hpMax) && (healPool > 0))
                    {
                        hp = W(p.hpMax - p.hpCur);
                        if (hp > healPool) hp = healPool;
                        if (RestoreHitPointsCall(p, B(hp), false)) healPool = W(healPool - hp);
                    }
                }
            }
            DisplayPartyCall(GDVars.curChar);
            PrintMiscInfoCall();
        }

        // Run the camp menu, preserving the interface state and clearing rest timers and pending spell activities.
        public static void CampMainMenu(ref bool interrupted)
        {
            byte savedIfc;
            byte c;
            // ViewCharMenu's var out
            bool viewDone = false;
            bool ext = false;
            savedIfc = GDVars.ifcState;
            GDVars.ifcState = C_Ifc.CAMPING;
            GDVars.healTicks = 0;
            ClearClock(GDVars.dsRestingTimeGbvm);
            DisplayNoncombatInterfaceCall();
            ClearBoxCall(C_Msg.TEXT_LEFT, C_Msg.MESSAGE_TOP, C_Msg.TEXT_RIGHT, C_Msg.TEXT_BOTTOM);
            DisplayStringCall(1, 18, LightGreen, "The party makes camp...");
            ResetPartySpellActivities();
            interrupted = false;
            c = (byte)' ';
            while ((interrupted == false) && !((c == C_Keys.NONE) || IsKey(c, 'E')))
            {
                // 'Camp:' plus the menu fills the 40-column bottom line exactly, so the prompt has no space after it
                c = MenuHorizontalCall("Camp:", GDConsts.campMenu, LightMagenta, LightGreen, White, true, true,
                                       ref ext);
                if (ext)
                {
                    if (c == C_Keys.F6)
                    {
                        // the Companion Tools, while the setting is on
                        if (GDVars.companionOn != 0)
                        {
                            ToolsMenuCall();
                            DisplayNoncombatInterfaceCall();
                        }
                    }
                    else
                    {
                        SelectPrevNextCharCall((char)c);
                        DisplayPartyCall(GDVars.curChar);
                    }
                }
                else
                    switch (c)
                    {
                        case (byte)'S':
                            SaveGameCall();
                            // the save menu takes the whole screen
                            DisplayNoncombatInterfaceCall();
                            if (YesNoMenuCall("Quit TO DOS ", Yellow, LightGreen, White) == (byte)'Y')
                                ExitGameCall();
                            break;
                        case (byte)'V': ViewCharMenuCall(ref viewDone); break;
                        case (byte)'M': CampMagicMenu(ref interrupted); break;
                        case (byte)'R': CampRestMenu(ref interrupted); break;
                        case (byte)'A': CampAlterMenu(); break;
                        case (byte)'F': CampFixMenu(ref interrupted); break;
                    }
            }
            ResetPartySpellActivities();
            GDVars.castTarget = null;
            GDVars.ifcState = savedIfc;
            PrintMiscInfoCall();
            ClearMessageWindowCall();
            ClearBottomLineCall();
        }
    }
}
