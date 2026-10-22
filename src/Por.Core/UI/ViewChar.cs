// Source: Talonius pool-of-radiance pinned 7616920a (src/viewchar.pas). Direct conversion by Claude (subagent of the
// supervising session), on James's instruction without Qwen; no Qwen draft exists for this unit. The first draft is kept
// as evidence/ten-b-2026-10-21/ViewChar/draft.cs and the repairs as supervisor.patch.
//
// Every unit viewchar.pas uses is converted, so there is no NotSupportedException seam of its own. Calls into converted
// units with side effects (TextRend, Border, CharDisp, MenuCore, Portrait, CbtMap palettes, FxEngine, Magic, Money,
// MoneyCal.ItemSalePrice, Vault, Stats, CharHlp.RecalcStats/RemoveItem/AppendItemCopy/ItemLabel) go through internal
// delegate fields that default to the converted routine, so tests can observe them (the CbtActs/Magic pattern). Pure
// helpers (CharHlp.IsNpc, HasEffect, MayTrade, CanUseItems, ItemReadable, CastableScroll, BaseItem, ItemWeight,
// MoneyCal.ParseMoneyType, StrUtil) are called directly.
using System;
using System.Globalization;
using static Por.Core.Types;

namespace Por.Core
{
    public static class ViewChar
    {
        // ---- converted-unit call points (default to the converted routine) ----
        internal delegate byte MenuHorizontalProc(string query, string menu, byte qCol, byte mCol, byte hlCol, bool reinterpret, bool animate, ref bool ext);
        internal delegate byte MenuVerticalProc(string query, string menu, byte qCol, byte mCol, byte hlCol, sbyte ulCol, sbyte ulRow, sbyte lrCol, sbyte lrRow, TItemRec list, bool addExit, ref bool redraw, ref short idx, ref TItemRec sel);
        internal delegate void SelectCharProc(string query, bool addExit, ref TCharRecord sel);
        internal delegate void FreeStringListProc(ref TItemRec list);
        internal delegate byte SpellMenuProc(byte menuType, byte selType, ref short idx, ref bool notEmpty);
        internal delegate void UseItemProc(TItemRec item, ref bool done);
        internal delegate void AppendItemCopyProc(ref TItemRec list, TItemRec item);
        internal delegate bool CheckOverloadProc(TCharRecord p, int w, ref ushort takable);
        internal delegate void TradeMoneyProc(ref TCharRecord giver, ref TCharRecord rcv, ushort qty, sbyte mType);

        internal static Action ClearScreenCall = TextRend.ClearScreen;
        internal static Action<byte, byte, byte, string> DisplayStringCall = TextRend.DisplayString;
        internal static Action<byte, byte, byte, byte, string> DisplayCenteredCall = TextRend.DisplayCentered;
        internal static Action<byte, byte, byte, byte, byte, bool, string> DisplayMessageWindowCall = TextRend.DisplayMessageWindow;
        internal static Action<byte, string> DisplayAndPauseCall = TextRend.DisplayAndPause;
        internal static Action PauseOnlyCall = TextRend.PauseOnly;
        internal static Action<sbyte, sbyte, sbyte, sbyte, byte> DrawBorderCall = Border.DrawBorder;
        internal static Action<sbyte, sbyte, sbyte, sbyte> ClearBoxCall = Border.ClearBox;
        internal static Action<sbyte, sbyte, sbyte, sbyte, byte, byte, string> DrawWindowCall = Border.DrawWindow;
        internal static Action<TCharRecord, byte, byte, bool> DisplayCreatureNameCall = CharDisp.DisplayCreatureName;
        internal static Action<TItemRec, byte, byte> PrintItemLabelCall = CharDisp.PrintItemLabel;
        internal static Action DisplayNoncombatInterfaceCall = CharDisp.DisplayNoncombatInterface;
        internal static SelectCharProc SelectCharCall = CharDisp.SelectChar;
        internal static Action<string> PrintBottomLineCall = CharDisp.PrintBottomLine;
        internal static Action<TCharRecord, string, byte, bool> PrintNameStringCall = CharDisp.PrintNameString;
        internal static Action ClearMessageWindowCall = CharDisp.ClearMessageWindow;
        internal static Action<TCharRecord, byte, byte> PrintArmorClassCall = CharDisp.PrintArmorClass;
        internal static Action<TCharRecord, byte, byte, bool> PrintHitPointsCall = CharDisp.PrintHitPoints;
        internal static Func<string, byte, byte, byte, byte> YesNoMenuCall = MenuCore.YesNoMenu;
        internal static MenuHorizontalProc MenuHorizontalCall = MenuCore.MenuHorizontal;
        internal static MenuVerticalProc MenuVerticalCall = MenuCore.MenuVertical;
        internal static FreeStringListProc FreeStringListCall = MenuCore.FreeStringList;
        internal static Func<bool> PlayerPortraitsHereCall = Portrait.PlayerPortraitsHere;
        internal static Action DisplayCharPortraitCall = Portrait.DisplayCharPortrait;
        internal static Action SetNoncombatPaletteCall = CbtMap.SetNoncombatPalette;
        internal static Action SetCombatPaletteCall = CbtMap.SetCombatPalette;
        internal static Func<TCharRecord, byte, bool, bool> RestoreHitPointsCall = FxEngine.RestoreHitPoints;
        internal static Action<TCharRecord, byte, ushort, byte, bool> AddEffectCall = FxEngine.AddEffect;
        internal static Func<TCharRecord, bool> HasDiseaseCall = FxEngine.HasDisease;
        internal static Action<TCharRecord> RemoveDiseasesCall = FxEngine.RemoveDiseases;
        internal static Action<TItemRec> ToggleReadyItemCall = FxEngine.ToggleReadyItem;
        internal static SpellMenuProc SpellMenuCall = Magic.SpellMenu;
        internal static UseItemProc UseItemCall = Magic.UseItem;
        internal static Action<TCharRecord> RecalcStatsCall = CharHlp.RecalcStats;
        internal static Action<TCharRecord, TItemRec> RemoveItemCall = CharHlp.RemoveItem;
        internal static AppendItemCopyProc AppendItemCopyCall = CharHlp.AppendItemCopy;
        internal static Action<TItemRec, bool> ItemLabelCall = CharHlp.ItemLabel;
        internal static Func<TCharRecord, Stats.TStatBlock> BlockOfCall = Stats.BlockOf;
        internal static Action<TCharRecord, byte, int> StatsCountCall = Stats.Count;
        internal static CheckOverloadProc CheckOverloadCall = Money.CheckOverload;
        internal static Func<TCharRecord, int, bool> PayGoldCall = Money.PayGold;
        internal static Func<ushort, string, byte, ushort> InputNumberMaxCall = Money.InputNumberMax;
        internal static TradeMoneyProc TradeMoneyCall = Money.TradeMoney;
        internal static Action<TCharRecord, ushort, sbyte> DropMoneyCall = Money.DropMoney;
        internal static Func<TItemRec, ushort> ItemSalePriceCall = MoneyCal.ItemSalePrice;
        internal static Func<bool> VaultHasRoomCall = Vault.HasRoom;
        internal static Action<TCharRecord, TItemRec> DepositItemCall = Vault.DepositItem;

        // ---- unit state (implementation var section) ----
        // the last character traded to, offered first by the next trade; ViewCharMenu starts it at curChar
        internal static TCharRecord tradeTarget;

        // ---- constants ----
        // the kills page: two columns of KILL_ROWS rows, an entry a column wide, a "(more)" row above and below
        internal const byte KILL_TOP = 5;
        internal const byte KILL_ROWS = 16;
        internal const byte KILL_COL_WIDTH = 18;
        internal const byte KILLS_SHOWN = 2 * KILL_ROWS;

        private const byte Black = 0;
        private const byte LightGreen = 10;
        private const byte LightMagenta = 13;
        private const byte Yellow = 14;
        private const byte White = 15;

        private static byte B(int v) { return unchecked((byte)v); }
        private static sbyte SB(int v) { return unchecked((sbyte)v); }
        private static ushort W(int v) { return unchecked((ushort)v); }
        // Pascal String[40] assignment
        private static string Str40(string s) { return s.Length > 40 ? s.Substring(0, 40) : s; }
        // Pascal Copy(s, index, count)
        private static string Copy(string s, int index, int count)
        {
            if (count <= 0 || index > s.Length) return string.Empty;
            if (index < 1) index = 1;
            if (index - 1 + count > s.Length) count = s.Length - index + 1;
            return s.Substring(index - 1, count);
        }
        private static bool IsKey(byte ch, char k) { return ch == (byte)k; }

        // What a shopkeeper or an item prompt says, in yellow on the bottom two rows (1, 21 to 38, 22).
        internal static void SayBelow(bool clearFirst, string s)
        {
            DisplayMessageWindowCall(1, 21, 38, 22, Yellow, clearFirst, s);
        }

        // Draw creature's character sheet; the portrait is curChar's (Portrait.DisplayCharPortrait), which every caller shows.
        public static void DisplayChar(TCharRecord creature)
        {
            byte row;
            byte col;
            string s;
            bool multi;
            ClearScreenCall();
            DrawBorderCall(C_Msg.TEXT_LEFT, C_Msg.TEXT_TOP, C_Msg.TEXT_RIGHT, C_Msg.TEXT_BOTTOM, 0);
            DisplayCreatureNameCall(creature, 1, 1, false);
            // C_Npc.FLAG set: not a party member
            if (CharHlp.IsNpc(creature))
                DisplayStringCall(B(creature.name.Length + 3), 1, LightGreen, "(NPC)");
            col = 1;
            s = GDConsts.genderNames[creature.genderIdx];
            DisplayStringCall(col, 3, White, s);
            col = B(col + s.Length + 1);
            s = GDConsts.raceNames[creature.race];
            DisplayStringCall(col, 3, White, s);
            col = B(col + s.Length + 1);
            DisplayStringCall(col, 3, White, "Age " + StrUtil.NumStr(creature.age));
            s = GDConsts.alignNames[creature.alignmentIdx];
            DisplayStringCall(1, 4, White, s);
            s = GDConsts.classNames[creature.charClass];
            DisplayStringCall(1, 5, White, s);
            // the six ability scores
            for (int i = C_Abil.STRENGTH; i <= C_Abil.CHARISMA; i++)
            {
                switch (i)
                {
                    case C_Abil.STRENGTH: s = "STR "; break;
                    case C_Abil.INTELLIGENCE: s = "INT "; break;
                    case C_Abil.WISDOM: s = "WIS "; break;
                    case C_Abil.DEXTERITY: s = "DEX "; break;
                    case C_Abil.CONSTITUTION: s = "CON "; break;
                    case C_Abil.CHARISMA: s = "CHA "; break;
                }
                DisplayStringCall(1, B(i + 7), LightGreen, s);
                PrintStat(SB(i), false);
            }
            PrintMoney();
            DisplayStringCall(1, 15, White, "Level");
            multi = false;
            s = "";
            // one entry per class the character has levels in, joined with '/'
            for (int j = C_Level.CLERIC; j <= C_Level.MONK; j++)
                if (creature.classLevels[j] > 0)
                {
                    if (multi)
                        s = s + "/";
                    s = s + StrUtil.NumStr(creature.classLevels[j]);
                    multi = true;
                }
            DisplayStringCall(7, 15, White, s);
            DisplayStringCall(17, 15, White, "Exp " + StrUtil.NumStr(creature.experience));
            PrintCombatStats();
            row = 20;
            if (creature.equipped[C_Equip.WEAPON] != null)
            {
                DisplayStringCall(1, row, White, "Weapon");
                PrintItemLabelCall(creature.equipped[C_Equip.WEAPON], 8, row);
            }
            row = B(row + 1);
            if (creature.equipped[C_Equip.ARMOR] != null)
            {
                // 'Armor' starts one column right of 'Weapon' so the two labels end in the same column
                DisplayStringCall(2, row, White, "Armor");
                PrintItemLabelCall(creature.equipped[C_Equip.ARMOR], 8, row);
            }
            row = B(row + 1);
            DisplayStringCall(1, row, White, "Status");
            DisplayStringCall(8, row, LightGreen, GDConsts.statusStrs[creature.status]);
            if ((GDVars.portraitsOn != 0) && PlayerPortraitsHereCall())
                DisplayCharPortraitCall();
        }

        // Print nonzero coin holdings, highest type first: names right-aligned to end at column 19, amounts from column 21.
        // Reads curChar.
        public static void PrintMoney()
        {
            byte row;
            string s;
            ClearBoxCall(12, 7, 26, 14);
            row = 7;
            for (int m = C_Money.JEWELRY; m >= C_Money.COPPER; m--)
                if (GDVars.curChar.coins[m] > 0)
                {
                    s = Str40(GDConsts.moneyNames[m]);
                    DisplayStringCall(B(20 - s.Length), row, LightGreen, s);
                    DisplayStringCall(21, row, LightGreen, StrUtil.NumStr(GDVars.curChar.coins[m]));
                    row = B(row + 1);
                }
        }

        // True when p is a paladin who can use a paladin's powers now: okay, and out of combat.
        internal static bool PaladinReady(TCharRecord p)
        {
            return (p.charClass == C_Class.PALADIN) && (p.status == C_Status.OKAY) && (GDVars.ifcState != C_Ifc.COMBAT);
        }

        // True when paladin p has not laid on hands in the last day.
        internal static bool CanHeal(TCharRecord p)
        {
            TEffectRec effect;
            return PaladinReady(p) && !CharHlp.HasEffect(p, C_Effect.RECENT_HEAL, out effect);
        }

        // True when paladin p has a cure left this week: one a week, and one more for every five levels after the first.
        internal static bool CanCure(TCharRecord p)
        {
            TEffectRec effect;
            byte used;
            sbyte allowed;
            used = 0;
            if (CharHlp.HasEffect(p, C_Effect.RECENT_CURE, out effect))
                used = effect.strength;
            allowed = SB((p.classLevels[C_Level.PALADIN] - 1) / 5 + 1);
            return PaladinReady(p) && (allowed > 0) && (used < allowed);
        }

        // Lay on hands: 2 hit points per paladin level to the party member chosen; RECENT_HEAL for 1440 minutes at 255.
        internal static void LayOnHands(TCharRecord p)
        {
            TCharRecord tgt;
            byte hpBefore;
            DisplayNoncombatInterfaceCall();
            tgt = GDVars.creatureList;
            SelectCharCall("Heal whom? ", true, ref tgt);
            if (tgt == null)
                return;
            hpBefore = tgt.hpCur;
            if (RestoreHitPointsCall(tgt, B(p.classLevels[C_Level.PALADIN] * 2), false))
            {
                StatsCountCall(p, Stats.HEALED_GIVEN, tgt.hpCur - hpBefore);
                PrintBottomLineCall(tgt.name + " feels better");
            }
            else
                PrintBottomLineCall(tgt.name + " is unaffected");
            AddEffectCall(p, C_Effect.RECENT_HEAL, 1440, 255, false);
        }

        // Cure disease: remove the disease effects from the member chosen, asking first when there are none, and spend one
        // of the week's cures.
        internal static void CureDisease(TCharRecord p)
        {
            TCharRecord tgt;
            TEffectRec effect;
            byte ch;
            DisplayNoncombatInterfaceCall();
            tgt = GDVars.creatureList;
            SelectCharCall("Cure whom? ", true, ref tgt);
            if (tgt == null)
                return;
            ch = (byte)'Y';
            if (!HasDiseaseCall(tgt))
            {
                PrintNameStringCall(tgt, "is not diseased", 0, false);
                ch = YesNoMenuCall("cure anyway: ", LightMagenta, LightGreen, White);
                ClearMessageWindowCall();
            }
            if (ch != (byte)'Y')
                return;
            RemoveDiseasesCall(tgt);
            if (CharHlp.HasEffect(p, C_Effect.RECENT_CURE, out effect))
                effect.strength = B(effect.strength + 1);
            else
                AddEffectCall(p, C_Effect.RECENT_CURE, 10080, 1, false);
            PrintBottomLineCall(tgt.name + " is cured");
        }

        // Draw creature's name and then title on row 2 of a cleared, bordered screen.
        internal static void StatsHeading(TCharRecord creature, string title)
        {
            title = Str40(title);
            ClearScreenCall();
            DrawBorderCall(C_Msg.TEXT_LEFT, C_Msg.TEXT_TOP, C_Msg.TEXT_RIGHT, C_Msg.TEXT_BOTTOM, 0);
            DisplayCreatureNameCall(creature, 2, 2, false);
            DisplayStringCall(B(creature.name.Length + 3), 2, White, title);
        }

        // The Game page: the creature's twelve counters in this game, coin in gold; b nil reads every counter as 0.
        internal static void DrawGameStats(TCharRecord creature, Stats.TStatBlock b)
        {
            int n;
            string s;
            StatsHeading(creature, "Game");
            for (int i = 1; i <= Stats.STAT_COUNT; i++)
            {
                n = 0;
                if (b != null)
                    n = b.counts[i];
                if (i == Stats.COIN_LOOTED)
                    n = n / GDConsts.dsCoinValuationCopper[C_Money.GOLD];
                s = n.ToString(CultureInfo.InvariantCulture);
                DisplayStringCall(3, B(3 + i), White, Stats.NAMES[i]);
                DisplayStringCall(B(36 - s.Length), B(3 + i), LightGreen, s);
            }
        }

        // The Kills page from entry top of order (b's kinds, sorted by name).
        internal static void DrawKills(TCharRecord creature, Stats.TStatBlock b, byte[] order, byte kinds, byte top)
        {
            byte i;
            byte width;
            string s;
            StatsHeading(creature, "Kills");
            if (kinds == 0)
            {
                DisplayStringCall(3, KILL_TOP, LightGreen, "None");
                return;
            }
            width = 1;
            for (int n = 1; n <= kinds; n++)
                if (StrUtil.NumStr(b.kills[n].count).Length > width)
                    width = B(StrUtil.NumStr(b.kills[n].count).Length);
            if (top > 1)
                DisplayCenteredCall(1, 38, KILL_TOP - 1, White, "(more)");
            for (int k = 0; k <= KILLS_SHOWN - 1; k++)
                if (top + k <= kinds)
                {
                    i = order[top + k];
                    s = Str40(StrUtil.PadLeft(StrUtil.NumStr(b.kills[i].count), width) + " " +
                              Copy(b.kills[i].name, 1, KILL_COL_WIDTH - 2 - width));
                    DisplayStringCall(B(2 + (k % 2) * KILL_COL_WIDTH), B(KILL_TOP + k / 2), LightGreen, s);
                }
            if (top + KILLS_SHOWN <= kinds)
                DisplayCenteredCall(1, 38, KILL_TOP + KILL_ROWS, White, "(more)");
        }

        // The statistics pages for creature: Game and Kills. Runs until Exit or Escape; the caller redraws.
        internal static void StatsPage(TCharRecord creature)
        {
            Stats.TStatBlock b;
            byte kinds;
            byte[] order = new byte[Stats.KINDS + 1];
            byte t;
            short top;
            bool onKills;
            string menu;
            byte ch;
            bool ext = false;
            // a creature outside the party has no block: its counters read 0
            b = BlockOfCall(creature);
            kinds = 0;
            if (b != null)
                kinds = b.kindCount;
            for (int i = 1; i <= kinds; i++)
                order[i] = B(i);
            for (int i = 1; i <= kinds; i++)
                for (int j = i + 1; j <= kinds; j++)
                    if (string.CompareOrdinal(b.kills[order[j]].name, b.kills[order[i]].name) < 0)
                    {
                        t = order[i];
                        order[i] = order[j];
                        order[j] = t;
                    }
            onKills = false;
            top = 1;
            GDVars.horizChoice = 1;
            do
            {
                if (onKills)
                {
                    DrawKills(creature, b, order, kinds, B(top));
                    if (kinds > KILLS_SHOWN)
                        menu = "Game Kills Next Prev Exit";
                    else
                        menu = "Game Kills Exit";
                }
                else
                {
                    DrawGameStats(creature, b);
                    menu = "Game Kills Exit";
                }
                ch = MenuHorizontalCall("Stats: ", menu, LightMagenta, LightGreen, White, true, false, ref ext);
                if (ext)
                {
                    // Up and Down scroll the kills a row
                    if (onKills && (ch == C_Keys.UP) && (top > 1))
                        top = unchecked((short)(top - 2));
                    else if (onKills && (ch == C_Keys.DOWN) && (top + KILLS_SHOWN <= kinds))
                        top = unchecked((short)(top + 2));
                }
                else
                    switch (ch)
                    {
                        case (byte)'G':
                            onKills = false;
                            break;
                        case (byte)'K':
                            onKills = true;
                            top = 1;
                            break;
                        case (byte)'N':
                            if (onKills && (top + KILLS_SHOWN <= kinds))
                                top = unchecked((short)(top + KILLS_SHOWN));
                            break;
                        case (byte)'P':
                            if (onKills)
                            {
                                top = unchecked((short)(top - KILLS_SHOWN));
                                if (top < 1)
                                    top = 1;
                            }
                            break;
                    }
            } while (!((!ext) && ((ch == C_Keys.NONE) || IsKey(ch, 'E'))));
        }

        private static readonly string[] SaveLabels =
        {
            "Paralysis, Poison, Death", "Petrification, Polymorph", "Rod, Staff, Wand", "Breath Weapon", "Spell"
        };

        // The More Info page for the current character: the five saving throws, each the d20 roll needed, held to 2..20.
        internal static void MorePage()
        {
            TCharRecord p;
            short need;
            string s;
            byte ch;
            bool ext = false;
            p = GDVars.curChar;
            RecalcStatsCall(p);
            ClearScreenCall();
            DrawBorderCall(C_Msg.TEXT_LEFT, C_Msg.TEXT_TOP, C_Msg.TEXT_RIGHT, C_Msg.TEXT_BOTTOM, 0);
            DisplayCreatureNameCall(p, 2, 2, false);
            DisplayStringCall(2, 4, White, "Saving Throws");
            for (int i = C_Save.PARALYZE; i <= C_Save.SPELL; i++)
            {
                need = unchecked((short)(p.saves[i] - p.saveBonus));
                if (need < 2)
                    need = 2;
                if (need > 20)
                    need = 20;
                s = StrUtil.NumStr(need);
                DisplayStringCall(3, B(6 + i), White, SaveLabels[i]);
                DisplayStringCall(B(32 - s.Length), B(6 + i), LightGreen, s);
            }
            do
            {
                ch = MenuHorizontalCall("More Info: ", "Stats Exit", LightMagenta, LightGreen, White, false, false, ref ext);
                if (IsKey(ch, 'S'))
                {
                    StatsPage(p);
                    ch = (byte)'E';
                }
            } while (!((ch == C_Keys.NONE) || IsKey(ch, 'E')));
        }

        // Draw "More Info" under the character sheet's portrait frame, the M in white as a hotkey is.
        internal static void DrawMoreInfoLabel()
        {
            DisplayStringCall(29, 13, White, "M");
            DisplayStringCall(30, 13, LightGreen, "ore Info");
        }

        // Recalculate curChar's stats, then display armor class, hit points, THAC0, damage, encumbrance and movement.
        public static void PrintCombatStats()
        {
            TCharRecord p;
            byte row;
            byte col;
            byte mv;
            string s;
            TEffectRec effect;
            p = GDVars.curChar;
            RecalcStatsCall(p);
            row = 17;
            DisplayStringCall(1, row, White, "AC    ");
            PrintArmorClassCall(p, 4, row);
            DisplayStringCall(1, B(row + 1), White, "HP    ");
            PrintHitPointsCall(p, 4, B(row + 1), false);
            col = 8;
            DisplayStringCall(B(col + 1), row, White, "THAC0   ");
            DisplayStringCall(B(col + 7), row, LightGreen, StrUtil.NumStr(60 - p.thac0Cur));
            s = Str40(StrUtil.NumStr(p.curRollsA[1]) + "d" + StrUtil.NumStr(p.curDiceA[1]));
            if (p.curModA[1] > 0)
                s = Str40(s + "+" + StrUtil.NumStr(p.curModA[1]));
            if (p.curModA[1] < 0)
                s = Str40(s + "-" + StrUtil.NumStr(Math.Abs((int)p.curModA[1])));
            DisplayStringCall(col, B(row + 1), White, "Damage        ");
            DisplayStringCall(B(col + 7), B(row + 1), LightGreen, s);
            col = 22;
            DisplayStringCall(col, row, White, "Encumbrance      ");
            DisplayStringCall(B(col + 12), row, LightGreen, StrUtil.NumStr(p.encumbrance));
            mv = p.movementCur;
            if (CharHlp.HasEffect(p, C_Effect.SLOWED, out effect))
                mv = B(mv / 2);
            if (CharHlp.HasEffect(p, C_Effect.HASTED, out effect))
                mv = B(mv * 2);
            DisplayStringCall(B(col + 3), B(row + 1), White, "Movement   ");
            DisplayStringCall(B(col + 12), B(row + 1), LightGreen, StrUtil.NumStr(mv));
        }

        // Print stat statIdx (0..5) of curChar in light green, or light magenta while modifying.
        public static void PrintStat(sbyte statIdx, bool modifying)
        {
            string s;
            byte col;
            byte hlCol;
            if (modifying)
                hlCol = LightMagenta;
            else
                hlCol = LightGreen;
            col = 5;
            ClearBoxCall(SB(col), SB(statIdx + 7), 11, SB(statIdx + 7));
            if (GDVars.curChar.stats[statIdx] < 10)
                col = B(col + 1);
            DisplayStringCall(col, B(statIdx + 7), hlCol, StrUtil.NumStr(GDVars.curChar.stats[statIdx]));
            if ((statIdx == C_Abil.STRENGTH) && (GDVars.curChar.strCur == 18) && (GDVars.curChar.strExc > 0))
            {
                // 100 shows as 00
                s = Str40(StrUtil.ZeroPad2(GDVars.curChar.strExc));
                DisplayStringCall(7, 7, hlCol, "(" + s + ")");
            }
        }

        // Run the character-sheet menu until Escape, Exit or done.
        public static void ViewCharMenu(ref bool done)
        {
            byte ch;
            string s;
            bool hasMoney;
            bool hasSpells;
            bool ext = false;
            byte i;
            short idx;
            TCharRecord shown;
            if (GDVars.ifcState == C_Ifc.COMBAT)
                SetNoncombatPaletteCall();
            ch = (byte)' ';
            done = false;
            tradeTarget = GDVars.curChar;
            shown = GDVars.curChar;
            DisplayChar(shown);
            DrawMoreInfoLabel();
            while (!((ch == C_Keys.NONE) || IsKey(ch, 'E')) && !done)
            {
                s = "";
                hasSpells = false;
                hasMoney = false;
                // every memorized-spell slot
                for (int n = 0; n <= SPELL_SLOT_LAST; n++)
                    if (shown.memorizedSpells[n] > 0)
                        hasSpells = true;
                // every coin type, gems and jewelry included
                for (int j = C_Money.COPPER; j <= C_Money.JEWELRY; j++)
                    if (shown.coins[j] > 0)
                        hasMoney = true;
                if (shown.itemsAddr != null)
                    s = Str40(s + "Items ");
                if (hasSpells)
                    s = Str40(s + "Spells ");
                if (CharHlp.MayTrade(shown) && hasMoney && (GDVars.ifcState != C_Ifc.COMBAT))
                    s = Str40(s + "Trade ");
                // in the vault, dropped money is deposited there (Money.DropMoney)
                if (hasMoney && (GDVars.ifcState == C_Ifc.VAULT))
                    s = Str40(s + "Deposit ");
                else if (hasMoney)
                    s = Str40(s + "Drop ");
                if (CanHeal(shown))
                    s = Str40(s + "Heal ");
                if (CanCure(shown))
                    s = Str40(s + "Cure ");
                s = Str40(s + "Exit");
                MenuCore.extraKeys = "M";
                ch = MenuHorizontalCall("", s, LightMagenta, LightGreen, White, false, false, ref ext);
                idx = -1;
                switch (ch)
                {
                    case (byte)'I': UseItemMenu(ref done); break;
                    case (byte)'S': i = SpellMenuCall(0, 0, ref idx, ref hasSpells); break;
                    case (byte)'T': TradeTreasureMenu(); break;
                    case (byte)'D':
                        DropMoneyMenu();
                        PrintMoney();
                        break;
                    case (byte)'H':
                        if (CanHeal(shown))
                            LayOnHands(shown);
                        break;
                    case (byte)'C':
                        if (CanCure(shown))
                            CureDisease(shown);
                        break;
                    case (byte)'M': MorePage(); break;
                }
                if (!done && (IsKey(ch, 'I') || IsKey(ch, 'S') || IsKey(ch, 'T') || IsKey(ch, 'H') || IsKey(ch, 'C') || IsKey(ch, 'M')))
                {
                    shown = GDVars.curChar;
                    DisplayChar(shown);
                    DrawMoreInfoLabel();
                }
            }
            if (GDVars.ifcState == C_Ifc.COMBAT)
                SetCombatPaletteCall();
            DisplayNoncombatInterfaceCall();
        }

        // True when the item may leave curChar's inventory. Equipped items must be unreadied first; a scroll with pending
        // scribing asks before discarding that work.
        internal static bool CheckItemAvail(TItemRec item)
        {
            bool result = false;
            if (item.equipped != 0)
                PrintBottomLineCall("Must be unreadied");
            else if (!CharHlp.ItemReadable(item))
                result = true;
            else if ((item.charges > 0x7F) || (item.effect > 0x7F) || (item.special > 0x7F))
            {
                DisplayCreatureNameCall(GDVars.curChar, 1, 21, false);
                GDVars.curCol = B(GDVars.curChar.name.Length + 2);
                GDVars.curRow = 21;
                SayBelow(false, " was going to scribe from that scroll");
                if (YesNoMenuCall("is it Okay to lose it? ", LightMagenta, LightGreen, White) == (byte)'Y')
                    result = true;
            }
            else
                result = true;
            ClearBoxCall(1, 21, 38, 22);
            return result;
        }

        private static readonly string[] DetailLabels =
        {
            null, "itemtype:", "namenum(1):", "namenum(2):", "namenum(3):", "plus:", "plussave:", "ready:", "identified:",
            "cursed:", "value:", "charges:", "effect:", "special:", "dice large:", "sides large:"
        };

        // STING's raw item viewer (V in the Items menu): the item's record fields until a key.
        internal static void ViewItemDetail(TItemRec it)
        {
            int[] v = new int[16];
            string s;
            v[1] = it.itemType;
            v[2] = it.nameIds[1];
            v[3] = it.nameIds[2];
            v[4] = it.nameIds[3];
            v[5] = unchecked((byte)it.plus);
            v[6] = unchecked((byte)it.saveBonus);
            v[7] = it.equipped;
            v[8] = it.unidMask;
            v[9] = it.cursed;
            v[10] = it.value;
            v[11] = it.charges;
            v[12] = it.effect;
            v[13] = it.special;
            v[14] = CharHlp.BaseItem(it.itemType).dmgLgRolls;
            v[15] = CharHlp.BaseItem(it.itemType).dmgLgDice;
            ClearScreenCall();
            DrawBorderCall(C_Msg.TEXT_LEFT, C_Msg.TEXT_TOP, C_Msg.TEXT_RIGHT, C_Msg.TEXT_BOTTOM, 0);
            for (int i = 1; i <= 15; i++)
            {
                s = v[i].ToString(CultureInfo.InvariantCulture);
                DisplayStringCall(2, B(i + 1), LightGreen, DetailLabels[i]);
                DisplayStringCall(20, B(i + 1), LightGreen, s);
            }
            DisplayAndPauseCall(LightGreen, "press a key");
        }

        // Run curChar's inventory menu until Escape, done, or no items remain.
        public static void UseItemMenu(ref bool done)
        {
            byte ch;
            string s;
            bool needRedraw;
            bool vmRedraw;
            TItemRec p;
            TItemRec sel;
            short idx;
            TCharRecord creature;
            byte itemCount;
            creature = GDVars.curChar;
            ch = (byte)' ';
            sel = creature.itemsAddr;
            idx = 0;
            vmRedraw = true;
            needRedraw = true;
            // the list as well as the count: a stale count over a nil list would never leave the loop
            while (!((ch == C_Keys.NONE) || IsKey(ch, 'E')) && !done && (creature.numberOfItems > 0) &&
                   (creature.itemsAddr != null))
            {
                itemCount = creature.numberOfItems;
                if (creature.itemsAddr != null)
                {
                    s = "Ready";
                    if (CharHlp.CanUseItems(creature))
                        s = Str40(s + " Use");
                    if (CharHlp.MayTrade(creature) && (GDVars.ifcState != C_Ifc.COMBAT))
                        s = Str40(s + " Trade");
                    if (GDVars.ifcState == C_Ifc.VAULT)
                        s = Str40(s + " Deposit");
                    else
                        s = Str40(s + " Drop");
                    if (creature.numberOfItems < 16)
                        s = Str40(s + " Halve");
                    s = Str40(s + " Join");
                    // the characters that may trade may sell, but only in a shop
                    if (CharHlp.MayTrade(creature) && (GDVars.ifcState == C_Ifc.SHOP_TEMPLE))
                        s = Str40(s + " Sell");
                    if (GDVars.ifcState == C_Ifc.SHOP_TEMPLE)
                        s = Str40(s + " Id");
                    // rebuild every item name
                    p = creature.itemsAddr;
                    while (p != null)
                    {
                        ItemLabelCall(p, true);
                        p = p.next;
                    }
                    if (needRedraw || (GDVars.dsRedrawInventoryMenuFlag != 0))
                    {
                        ClearScreenCall();
                        DrawBorderCall(C_Msg.TEXT_LEFT, C_Msg.TEXT_TOP, C_Msg.TEXT_RIGHT, C_Msg.TEXT_BOTTOM, 0);
                        DisplayCreatureNameCall(creature, 1, 1, true);
                        DisplayStringCall(B(creature.name.Length + 4), 1, LightGreen, "Items");
                        DrawWindowCall(1, 3, 38, 22, 0, Black, "");
                        DisplayStringCall(1, 3, White, "Ready Item");
                        vmRedraw = true;
                        needRedraw = false;
                        GDVars.dsRedrawInventoryMenuFlag = 0;
                    }
                    // STING: V, not on the menu line, shows the selected item's raw record
                    if (GDVars.stingArg)
                        MenuCore.extraKeys = "V";
                    ch = MenuVerticalCall("", s, LightMagenta, LightGreen, White, 1, 5, 38, 22,
                                          creature.itemsAddr, true, ref vmRedraw, ref idx, ref sel);
                    if (sel != null)
                        switch (ch)
                        {
                            case (byte)'V':
                                if (GDVars.stingArg)
                                {
                                    ViewItemDetail(sel);
                                    needRedraw = true;
                                }
                                break;
                            case (byte)'R':
                                ToggleReadyItemCall(sel);
                                break;
                            // a scroll the Cast list offers (CharHlp.CastableScroll) need not be readied; other items must be
                            case (byte)'U':
                                if ((sel.equipped == 0) && !CharHlp.CastableScroll(creature, sel))
                                {
                                    PrintBottomLineCall("Must be Readied");
                                    ch = (byte)' ';
                                }
                                else if (CharHlp.ItemReadable(sel) || ((sel.effect > 0) && (sel.special < 0x80)))
                                {
                                    UseItemCall(sel, ref done);
                                    if (GDVars.ifcState != C_Ifc.COMBAT)
                                        // outside combat, using an item does not end the menu
                                        done = false;
                                    if (!done)
                                        needRedraw = true;
                                }
                                break;
                            case (byte)'T':
                                if (CheckItemAvail(sel))
                                    ItemTradeMenu(sel);
                                else
                                    ch = (byte)' ';
                                needRedraw = true;
                                break;
                            case (byte)'D':
                                if ((GDVars.ifcState == C_Ifc.VAULT) && CheckItemAvail(sel))
                                {
                                    // the vault keeps it, with the game it is saved with
                                    if (VaultHasRoomCall())
                                    {
                                        DepositItemCall(GDVars.curChar, sel);
                                        vmRedraw = true;
                                    }
                                    else
                                        DisplayAndPauseCall(Yellow, "The vault is full.");
                                }
                                else if (CheckItemAvail(sel))
                                {
                                    // the item label ends in a space
                                    ItemLabelCall(sel, false);
                                    SayBelow(true, "Your " + sel.name + "will be gone forever");
                                    if (YesNoMenuCall("Drop It? ", LightMagenta, LightGreen, White) == (byte)'Y')
                                    {
                                        RemoveItemCall(GDVars.curChar, sel);
                                        vmRedraw = true;
                                    }
                                    ClearBoxCall(1, 21, 38, 22);
                                }
                                else
                                    ch = (byte)' ';
                                break;
                            case (byte)'H':
                                HalveItem(sel);
                                break;
                            case (byte)'J':
                                JoinItem(sel);
                                break;
                            case (byte)'S':
                                if (CheckItemAvail(sel))
                                    SellItem(sel);
                                else
                                    ch = (byte)' ';
                                break;
                            case (byte)'I':
                                IdentifyItem(sel, ref vmRedraw);
                                break;
                        }
                    RecalcStatsCall(creature);
                }
                if (creature.numberOfItems != itemCount)
                    vmRedraw = true;
            }
        }

        // Select a trade recipient, check carrying capacity, then copy the item to the recipient and remove it from curChar.
        internal static void ItemTradeMenu(TItemRec item)
        {
            TCharRecord tgt;
            tgt = tradeTarget;
            DisplayNoncombatInterfaceCall();
            SelectCharCall("Trade with Whom?", true, ref tgt);
            if (tgt != null)
            {
                tradeTarget = tgt;
                if (CannotCarryItem(tgt, item))
                    PrintBottomLineCall("Overloaded");
                else
                {
                    AppendItemCopyCall(ref tgt.itemsAddr, item);
                    RemoveItemCall(GDVars.curChar, item);
                    RecalcStatsCall(tgt);
                }
            }
        }

        // Split an item stack: a copy holding half the quantity (rounded down), unreadied, is linked in right after the
        // original, which keeps the rest.
        internal static void HalveItem(TItemRec item)
        {
            TItemRec newIt;
            byte half;
            byte remainder;
            half = B(item.quantity / 2);
            if (half > 0)
            {
                remainder = B(item.quantity - half);
                newIt = new TItemRec();
                CharHlp.CopyItem(item, newIt);
                newIt.quantity = half;
                newIt.equipped = 0;
                newIt.next = item.next;
                item.quantity = remainder;
                item.next = newIt;
            }
            else
                PrintBottomLineCall("Can't halve that");
        }

        // Merge into item every other matching stack of curChar's with fewer than 2 charges, overflowing past 255 into the
        // stack being merged, which then becomes the destination.
        internal static void JoinItem(TItemRec item)
        {
            TItemRec cur;
            TItemRec next;
            TItemRec dst;
            byte qty;
            if (item.quantity == 0) return;
            dst = item;
            qty = dst.quantity;
            cur = GDVars.curChar.itemsAddr;
            while (cur != null)
            {
                next = cur.next;
                if ((cur != dst) && (cur.quantity > 0) && (cur.nameIds[1] == dst.nameIds[1]) &&
                    (cur.nameIds[2] == dst.nameIds[2]) && (cur.nameIds[3] == dst.nameIds[3]) &&
                    (cur.itemType == dst.itemType) && (cur.plus == dst.plus) && (cur.saveBonus == dst.saveBonus) &&
                    (cur.cursed == dst.cursed) && (cur.weight == dst.weight) && (cur.charges == dst.charges) &&
                    (cur.charges < 2) && (cur.effect == dst.effect) && (cur.special == dst.special))
                {
                    if (qty + cur.quantity <= 255)
                    {
                        qty = B(qty + cur.quantity);
                        if (cur.equipped != 0)
                            dst.equipped = cur.equipped;
                        RemoveItemCall(GDVars.curChar, cur);
                    }
                    else
                    {
                        dst.quantity = 255;
                        cur.quantity = B(cur.quantity - (255 - qty));
                        qty = cur.quantity;
                        dst = cur;
                    }
                }
                cur = next;
            }
            dst.quantity = qty;
        }

        // Offer MoneyCal.ItemSalePrice for the item; pay as platinum (price div 5) plus gold (price mod 5); when overloaded,
        // the platinum the character cannot take goes to the treasure pool.
        internal static void SellItem(TItemRec item)
        {
            ushort price;
            ushort plat;
            ushort gold;
            // a var out of Money.CheckOverload, which always writes it
            ushort takable = 0;
            price = ItemSalePriceCall(item);
            ItemLabelCall(item, false);
            SayBelow(true, "I'll give you " + StrUtil.NumStr(price) + " gold pieces for your " + item.name);
            if (YesNoMenuCall("Is It a Deal? ", LightMagenta, LightGreen, White) == (byte)'Y')
            {
                PrintBottomLineCall("Sold!");
                RemoveItemCall(GDVars.curChar, item);
                plat = W(price / 5);
                gold = W(price % 5);
                if (CheckOverloadCall(GDVars.curChar, gold + plat, ref takable))
                {
                    PrintBottomLineCall("Overloaded.  Money will be put in pool.");
                    if (takable > plat)
                        GDVars.curChar.coins[C_Money.PLATINUM] = W(GDVars.curChar.coins[C_Money.PLATINUM] + plat);
                    else
                    {
                        GDVars.curChar.coins[C_Money.PLATINUM] = W(GDVars.curChar.coins[C_Money.PLATINUM] + takable);
                        GDVars.treasureMoney[C_Money.PLATINUM] = unchecked(GDVars.treasureMoney[C_Money.PLATINUM] + (plat - takable));
                    }
                    // added even when overloaded
                    GDVars.curChar.coins[C_Money.GOLD] = W(GDVars.curChar.coins[C_Money.GOLD] + gold);
                }
                else
                {
                    GDVars.curChar.coins[C_Money.PLATINUM] = W(GDVars.curChar.coins[C_Money.PLATINUM] + plat);
                    GDVars.curChar.coins[C_Money.GOLD] = W(GDVars.curChar.coins[C_Money.GOLD] + gold);
                }
            }
            ClearBoxCall(1, 21, 38, 22);
        }

        // Identify an item for 200 gold. newInfo is set, never cleared, when identification reveals new information.
        internal static void IdentifyItem(TItemRec item, ref bool newInfo)
        {
            bool paid;
            paid = false;
            ItemLabelCall(item, false);
            SayBelow(true, "For 200 gold pieces I'll identify your " + item.name);
            if (YesNoMenuCall("Is It a Deal? ", LightMagenta, LightGreen, White) == (byte)'Y')
            {
                paid = PayGoldCall(GDVars.curChar, 200);
            }
            if (paid)
            {
                if (item.unidMask == 0)
                    SayBelow(true, "I can't tell anything new about your " + item.name);
                else
                {
                    item.unidMask = 0;
                    ItemLabelCall(item, false);
                    SayBelow(true, "It looks like some sort of " + item.name);
                    newInfo = true;
                }
            }
            if (paid)
                PauseOnlyCall();
            ClearBoxCall(1, 21, 38, 22);
        }

        // Show the current character's coins as a menu (jewelry first) and return the line picked, or nil on Escape. list
        // receives the menu list (nil when there is no coin).
        internal static TItemRec PickCoinType(ref TItemRec list)
        {
            TItemRec p;
            TItemRec prev;
            short idx;
            bool redraw;
            byte ch;
            p = null;
            for (int m = C_Money.COPPER; m <= C_Money.JEWELRY; m++)
                if (GDVars.curChar.coins[m] != 0)
                {
                    // a menu-list node: only the name, inUse and next fields of a TItemRec
                    prev = p;
                    p = new TItemRec();
                    p.next = prev;
                    p.name = Str40(StrUtil.PadLeft(GDConsts.moneyNames[m], 8) + " " + StrUtil.NumStr(GDVars.curChar.coins[m]));
                    p.inUse = 0;
                }
            list = p;
            prev = null;
            idx = 0;
            redraw = true;
            ch = MenuVerticalCall("Select type of coin", "", LightMagenta, LightGreen, White, 12, 7, 25, 13, p, true,
                                  ref redraw, ref idx, ref prev);
            return prev;
        }

        // Trade coins from curChar to another character, choosing the recipient (from tradeTarget), then a coin type and
        // amount, repeating while coins remain. Escaping the coin menu goes back to choosing the recipient.
        internal static void TradeTreasureMenu()
        {
            TCharRecord tgt;
            sbyte mType;
            TItemRec prev;
            TItemRec list = null;
            string s2;
            ushort amount;
            bool exitOuter;
            bool exitInner;
            do
            {
                tgt = tradeTarget;
                DisplayNoncombatInterfaceCall();
                SelectCharCall("Trade to?", true, ref tgt);
                if (tgt == null)
                    exitOuter = true;
                else
                {
                    DisplayChar(GDVars.curChar);
                    do
                    {
                        PrintMoney();
                        tradeTarget = tgt;
                        prev = PickCoinType(ref list);
                        if (prev == null)
                        {
                            exitInner = true;
                            exitOuter = false;
                        }
                        else
                        {
                            s2 = "";
                            mType = MoneyCal.ParseMoneyType(prev.name, ref s2);
                            s2 = "How much " + s2 + "will you trade? ";
                            amount = InputNumberMaxCall(GDVars.curChar.coins[mType], s2, 10);
                            TradeMoneyCall(ref GDVars.curChar, ref tgt, amount, mType);
                            exitInner = true;
                            exitOuter = true;
                            for (int m = C_Money.COPPER; m <= C_Money.JEWELRY; m++)
                                if (GDVars.curChar.coins[m] > 0)
                                {
                                    exitOuter = false;
                                    exitInner = false;
                                }
                        }
                        if (list != null)
                            FreeStringListCall(ref list);
                    } while (!exitInner);
                }
            } while (!exitOuter);
        }

        // Choose a coin type and amount to drop from the character's holdings, repeating until Escape or no coins remain.
        internal static void DropMoneyMenu()
        {
            sbyte mType;
            TItemRec prev;
            TItemRec list = null;
            bool exitFlag;
            string s2;
            ushort amount;
            do
            {
                PrintMoney();
                prev = PickCoinType(ref list);
                if (prev == null)
                    exitFlag = true;
                else
                {
                    s2 = "";
                    mType = MoneyCal.ParseMoneyType(prev.name, ref s2);
                    if (GDVars.ifcState == C_Ifc.VAULT)
                        s2 = "How much " + s2 + "will you deposit? ";
                    else
                        s2 = "How much " + s2 + "will you drop? ";
                    amount = InputNumberMaxCall(GDVars.curChar.coins[mType], s2, 10);
                    DropMoneyCall(GDVars.curChar, amount, mType);
                    exitFlag = true;
                    for (int m = C_Money.COPPER; m <= C_Money.JEWELRY; m++)
                        if (GDVars.curChar.coins[m] > 0)
                            exitFlag = false;
                }
                if (list != null)
                    FreeStringListCall(ref list);
            } while (!exitFlag);
        }

        // True when the creature already holds 16 or more items, or the item's weight is more than it can still carry
        // (Money.CheckOverload). Recalculates the creature's stats first.
        public static bool CannotCarryItem(TCharRecord creature, TItemRec item)
        {
            bool f;
            int w;
            // a var out of Money.CheckOverload, which always writes it
            ushort takable = 0;
            RecalcStatsCall(creature);
            f = false;
            if (creature.numberOfItems > 15)
                f = true;
            w = CharHlp.ItemWeight(item);
            if (CheckOverloadCall(creature, w, ref takable))
                f = true;
            return f;
        }
    }
}
