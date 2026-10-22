// VcOracle: a separate model of viewchar.pas, written from the Pascal text (not from ViewChar.cs). It calls the world's
// fakes directly in the order the Pascal makes the calls. It keeps its own copies of the small pure helpers (IsNpc,
// MayTrade, HasEffect, PadLeft, Copy, ZeroPad2, the String[40] cut); it shares CharHlp.ItemReadable, CanUseItems,
// CastableScroll, BaseItem, ItemWeight and CopyItem and MoneyCal.ParseMoneyType with the port (accepted with their own
// units).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Por.Core;
using static Por.Core.Types;

sealed class VcOracle
{
    readonly VcWorld w;
    public TCharRecord tradeTarget;
    public VcOracle(VcWorld w) { this.w = w; }

    const byte LG = 10, LM = 13, YEL = 14, WH = 15;
    static byte b(int v) { return unchecked((byte)v); }
    static string N(long v) { return v.ToString(CultureInfo.InvariantCulture); }
    static string Cut40(string s) { return s.Length <= 40 ? s : s.Remove(40); }
    static string PadL(string s, int width) { if (s.Length > width) s = s.Remove(width); return s.PadLeft(width); }
    static string PCopy(string s, int count) { if (count <= 0) return ""; return count >= s.Length ? s : s.Remove(count); }
    static bool Npc(TCharRecord c) { return c.npc >= 0x80; }
    static bool Trades(TCharRecord c) { return !Npc(c) || c.enabled == 0 || c.status == C_Status.ANIMATED; }
    static TEffectRec Fx(TCharRecord c, int id) { for (var e = c.effectsAddr; e != null; e = e.next) if (e.id == id) return e; return null; }
    static bool ExitKey(byte ch) { return ch == 0 || ch == 'E'; }
    static bool Combat { get { return GDVars.ifcState == C_Ifc.COMBAT; } }
    static bool InVault { get { return GDVars.ifcState == C_Ifc.VAULT; } }
    static bool InShop { get { return GDVars.ifcState == C_Ifc.SHOP_TEMPLE; } }
    static TCharRecord Cur { get { return GDVars.curChar; } }
    void Below(bool clear, string s) { w.DisplayMessageWindow(1, 21, 38, 22, YEL, clear, s); }
    void Frame() { w.ClearScreen(); w.DrawBorder(1, 1, 38, 22, 0); }
    byte YN(string q) { return w.YesNo(q, LM, LG, WH); }

    // ---- the character sheet ----
    public void DisplayChar(TCharRecord c)
    {
        Frame();
        w.DisplayCreatureName(c, 1, 1, false);
        if (Npc(c)) w.DisplayString(b(c.name.Length + 3), 1, LG, "(NPC)");
        string g = GDConsts.genderNames[c.gender];
        string r = GDConsts.raceNames[c.race];
        w.DisplayString(1, 3, WH, g);
        w.DisplayString(b(g.Length + 2), 3, WH, r);
        w.DisplayString(b(g.Length + r.Length + 3), 3, WH, "Age " + N(c.age));
        w.DisplayString(1, 4, WH, GDConsts.alignNames[c.alignment]);
        w.DisplayString(1, 5, WH, GDConsts.classNames[c.charClass]);
        string[] abil = { "STR ", "INT ", "WIS ", "DEX ", "CON ", "CHA " };
        for (int k = 0; k < 6; k++)
        {
            w.DisplayString(1, b(7 + k), LG, abil[k]);
            PrintStat(k, false);
        }
        PrintMoney();
        w.DisplayString(1, 15, WH, "Level");
        w.DisplayString(7, 15, WH, string.Join("/", c.classLevels.Where(v => v > 0).Select(v => N(v))));
        w.DisplayString(17, 15, WH, "Exp " + N(c.experience));
        PrintCombatStats();
        var weapon = c.equipped[0];
        var armor = c.equipped[2];
        if (weapon != null) { w.DisplayString(1, 20, WH, "Weapon"); w.PrintItemLabel(weapon, 8, 20); }
        if (armor != null) { w.DisplayString(2, 21, WH, "Armor"); w.PrintItemLabel(armor, 8, 21); }
        w.DisplayString(1, 22, WH, "Status");
        w.DisplayString(8, 22, LG, GDConsts.statusStrs[c.status]);
        if (GDVars.portraitsOn != 0)
            if (w.PlayerPortraitsHere()) w.DisplayCharPortrait();
    }

    public void PrintMoney()
    {
        w.ClearBox(12, 7, 26, 14);
        int row = 7;
        foreach (int m in new[] { 6, 5, 4, 3, 2, 1, 0 })
        {
            int held = Cur.coins[m];
            if (held == 0) continue;
            string name = GDConsts.moneyNames[m];
            w.DisplayString(b(20 - name.Length), b(row), LG, name);
            w.DisplayString(21, b(row), LG, N(held));
            row++;
        }
    }

    public void PrintStat(int k, bool modifying)
    {
        byte colour = modifying ? LM : LG;
        int row = k + 7;
        w.ClearBox(5, (sbyte)row, 11, (sbyte)row);
        int v = Cur.stats[k];
        w.DisplayString(b(v < 10 ? 6 : 5), b(row), colour, N(v));
        if (k == 0 && Cur.strCur == 18 && Cur.strExc != 0)
            w.DisplayString(7, 7, colour, "(" + (Cur.strExc % 100).ToString("00", CultureInfo.InvariantCulture) + ")");
    }

    public void PrintCombatStats()
    {
        var p = Cur;
        w.RecalcStats(p);
        w.DisplayString(1, 17, WH, "AC    ");
        w.PrintArmorClass(p, 4, 17);
        w.DisplayString(1, 18, WH, "HP    ");
        w.PrintHitPoints(p, 4, 18, false);
        w.DisplayString(9, 17, WH, "THAC0   ");
        w.DisplayString(15, 17, LG, N(60 - p.thac0Cur));
        int mod = p.curModA[1];
        string dmg = N(p.curRollsA[1]) + "d" + N(p.curDiceA[1]) + (mod > 0 ? "+" + N(mod) : mod < 0 ? "-" + N(-mod) : "");
        w.DisplayString(8, 18, WH, "Damage        ");
        w.DisplayString(15, 18, LG, Cut40(dmg));
        w.DisplayString(22, 17, WH, "Encumbrance      ");
        w.DisplayString(34, 17, LG, N(p.encumbrance));
        int mv = p.movementCur;
        if (Fx(p, C_Effect.SLOWED) != null) mv = mv / 2;
        if (Fx(p, C_Effect.HASTED) != null) mv = (mv * 2) & 0xFF;
        w.DisplayString(25, 18, WH, "Movement   ");
        w.DisplayString(34, 18, LG, N(mv));
    }

    // ---- paladin powers ----
    public bool PaladinReady(TCharRecord p) { return p.charClass == 3 && p.status == 0 && !Combat; }
    public bool CanHeal(TCharRecord p) { return PaladinReady(p) && Fx(p, 140) == null; }
    public bool CanCure(TCharRecord p)
    {
        var e = Fx(p, 141);
        int used = e == null ? 0 : e.strength;
        int lv = p.classLevels[3] - 1;
        // Pascal div truncates toward zero
        int allowed = (lv < 0 ? -((-lv) / 5) : lv / 5) + 1;
        return PaladinReady(p) && allowed > 0 && used < allowed;
    }

    public void LayOnHands(TCharRecord p)
    {
        w.DisplayNoncombatInterface();
        var t = GDVars.creatureList;
        w.SelectChar("Heal whom? ", true, ref t);
        if (t == null) return;
        int before = t.hpCur;
        bool ok = w.RestoreHitPoints(t, b(2 * p.classLevels[3]), false);
        if (ok) w.StatsCount(p, 4, t.hpCur - before);
        w.PrintBottomLine(t.name + (ok ? " feels better" : " is unaffected"));
        w.AddEffect(p, 140, 1440, 255, false);
    }

    public void CureDisease(TCharRecord p)
    {
        w.DisplayNoncombatInterface();
        var t = GDVars.creatureList;
        w.SelectChar("Cure whom? ", true, ref t);
        if (t == null) return;
        if (!w.HasDisease(t))
        {
            w.PrintNameString(t, "is not diseased", 0, false);
            byte answer = w.YesNo("cure anyway: ", LM, LG, WH);
            w.ClearMessageWindow();
            if (answer != 'Y') return;
        }
        w.RemoveDiseases(t);
        var e = Fx(p, 141);
        if (e != null) e.strength = b(e.strength + 1);
        else w.AddEffect(p, 141, 10080, 1, false);
        w.PrintBottomLine(t.name + " is cured");
    }

    // ---- statistics pages ----
    void Heading(TCharRecord c, string title)
    {
        Frame();
        w.DisplayCreatureName(c, 2, 2, false);
        w.DisplayString(b(c.name.Length + 3), 2, WH, title);
    }

    public void GamePage(TCharRecord c, Stats.TStatBlock blk)
    {
        Heading(c, "Game");
        for (int i = 1; i <= 12; i++)
        {
            int n = blk == null ? 0 : blk.counts[i];
            if (i == 9) n = n / 200; // gold is worth 200 copper (dsCoinValuationCopper)
            string s = N(n);
            w.DisplayString(3, b(i + 3), WH, Stats.NAMES[i]);
            w.DisplayString(b(36 - s.Length), b(i + 3), LG, s);
        }
    }

    public void KillsPage(TCharRecord c, Stats.TStatBlock blk, int[] sorted, int kinds, int top)
    {
        Heading(c, "Kills");
        if (kinds == 0) { w.DisplayString(3, 5, LG, "None"); return; }
        int width = Math.Max(1, Enumerable.Range(1, kinds).Max(i => N(blk.kills[i].count).Length));
        if (top > 1) w.DisplayCentered(1, 38, 4, WH, "(more)");
        for (int k = 0; k < 32 && top + k <= kinds; k++)
        {
            var kill = blk.kills[sorted[top + k - 1]];
            string line = Cut40(PadL(N(kill.count), width) + " " + PCopy(kill.name, 16 - width));
            w.DisplayString(b(k % 2 == 0 ? 2 : 20), b(5 + k / 2), LG, line);
        }
        if (kinds >= top + 32) w.DisplayCentered(1, 38, 21, WH, "(more)");
    }

    public void StatsPage(TCharRecord c)
    {
        var blk = w.BlockOf(c);
        int kinds = blk == null ? 0 : blk.kindCount;
        // the Pascal's exchange sort; equal names keep the exchange order, so model the same exchanges (0-based here)
        var sorted = new int[kinds];
        for (int i = 0; i < kinds; i++) sorted[i] = i + 1;
        for (int i = 0; i < kinds; i++)
            for (int j = i + 1; j < kinds; j++)
                if (string.CompareOrdinal(blk.kills[sorted[j]].name, blk.kills[sorted[i]].name) < 0)
                {
                    int t = sorted[i]; sorted[i] = sorted[j]; sorted[j] = t;
                }
        bool kills = false;
        int top = 1;
        GDVars.horizChoice = 1;
        while (true)
        {
            string menu;
            if (kills)
            {
                KillsPage(c, blk, sorted, kinds, top & 0xFF);
                menu = kinds > 32 ? "Game Kills Next Prev Exit" : "Game Kills Exit";
            }
            else
            {
                GamePage(c, blk);
                menu = "Game Kills Exit";
            }
            bool ext = false;
            byte ch = w.MenuH("Stats: ", menu, LM, LG, WH, true, false, ref ext);
            if (ext)
            {
                if (kills && ch == C_Keys.UP && top > 1) top -= 2;
                else if (kills && ch == C_Keys.DOWN && top + 32 <= kinds) top += 2;
                continue;
            }
            if (ch == 'G') kills = false;
            else if (ch == 'K') { kills = true; top = 1; }
            else if (ch == 'N') { if (kills && top + 32 <= kinds) top += 32; }
            else if (ch == 'P') { if (kills) top = Math.Max(1, top - 32); }
            if (ExitKey(ch)) return;
        }
    }

    public void MorePage()
    {
        var p = Cur;
        w.RecalcStats(p);
        Frame();
        w.DisplayCreatureName(p, 2, 2, false);
        w.DisplayString(2, 4, WH, "Saving Throws");
        string[] labels = { "Paralysis, Poison, Death", "Petrification, Polymorph", "Rod, Staff, Wand", "Breath Weapon", "Spell" };
        for (int i = 0; i < 5; i++)
        {
            int need = Math.Min(20, Math.Max(2, p.saves[i] - p.saveBonus));
            w.DisplayString(3, b(6 + i), WH, labels[i]);
            w.DisplayString(b(32 - N(need).Length), b(6 + i), LG, N(need));
        }
        while (true)
        {
            bool ext = false;
            byte ch = w.MenuH("More Info: ", "Stats Exit", LM, LG, WH, false, false, ref ext);
            if (ch == 'S') { StatsPage(p); return; }
            if (ExitKey(ch)) return;
        }
    }

    void MoreInfoLabel() { w.DisplayString(29, 13, WH, "M"); w.DisplayString(30, 13, LG, "ore Info"); }

    // ---- the character-sheet menu ----
    public void ViewCharMenu(ref bool done)
    {
        if (Combat) w.SetNoncombatPalette();
        done = false;
        tradeTarget = Cur;
        var shown = Cur;
        DisplayChar(shown);
        MoreInfoLabel();
        byte ch = (byte)' ';
        while (!ExitKey(ch) && !done)
        {
            bool spells = shown.memorizedSpells.Data.Any(x => x != 0);
            bool money = shown.coins.Any(x => x != 0);
            var words = new List<string>();
            if (shown.itemsAddr != null) words.Add("Items");
            if (spells) words.Add("Spells");
            if (Trades(shown) && money && !Combat) words.Add("Trade");
            if (money) words.Add(InVault ? "Deposit" : "Drop");
            if (CanHeal(shown)) words.Add("Heal");
            if (CanCure(shown)) words.Add("Cure");
            words.Add("Exit");
            MenuCore.extraKeys = "M";
            bool ext = false;
            ch = w.MenuH("", Cut40(string.Join(" ", words)), LM, LG, WH, false, false, ref ext);
            bool redisplay = true;
            if (ch == 'I') UseItemMenu(ref done);
            else if (ch == 'S') { short idx = -1; w.SpellMenu(0, 0, ref idx, ref spells); }
            else if (ch == 'T') TradeTreasureMenu();
            else if (ch == 'D') { DropMoneyMenu(); PrintMoney(); redisplay = false; }
            else if (ch == 'H') { if (CanHeal(shown)) LayOnHands(shown); }
            else if (ch == 'C') { if (CanCure(shown)) CureDisease(shown); }
            else if (ch == 'M') MorePage();
            else redisplay = false;
            if (redisplay && !done)
            {
                shown = Cur;
                DisplayChar(shown);
                MoreInfoLabel();
            }
        }
        if (Combat) w.SetCombatPalette();
        w.DisplayNoncombatInterface();
    }

    // ---- inventory ----
    public bool CheckItemAvail(TItemRec it)
    {
        bool ok;
        if (it.equipped != 0) { w.PrintBottomLine("Must be unreadied"); ok = false; }
        else if (CharHlp.ItemReadable(it) && (it.charges >= 0x80 || it.effect >= 0x80 || it.special >= 0x80))
        {
            w.DisplayCreatureName(Cur, 1, 21, false);
            GDVars.curCol = b(Cur.name.Length + 2);
            GDVars.curRow = 21;
            Below(false, " was going to scribe from that scroll");
            ok = YN("is it Okay to lose it? ") == 'Y';
        }
        else ok = true;
        w.ClearBox(1, 21, 38, 22);
        return ok;
    }

    public void ViewItemDetail(TItemRec it)
    {
        var bi = CharHlp.BaseItem(it.itemType);
        var rows = new[]
        {
            Tuple.Create("itemtype:", (int)it.itemType), Tuple.Create("namenum(1):", (int)it.nameIds[1]),
            Tuple.Create("namenum(2):", (int)it.nameIds[2]), Tuple.Create("namenum(3):", (int)it.nameIds[3]),
            Tuple.Create("plus:", it.plus & 0xFF), Tuple.Create("plussave:", it.saveBonus & 0xFF),
            Tuple.Create("ready:", (int)it.equipped), Tuple.Create("identified:", (int)it.unidMask),
            Tuple.Create("cursed:", (int)it.cursed), Tuple.Create("value:", (int)it.value),
            Tuple.Create("charges:", (int)it.charges), Tuple.Create("effect:", (int)it.effect),
            Tuple.Create("special:", (int)it.special), Tuple.Create("dice large:", (int)bi.dmgLgRolls),
            Tuple.Create("sides large:", (int)bi.dmgLgDice)
        };
        Frame();
        for (int i = 0; i < rows.Length; i++)
        {
            w.DisplayString(2, b(i + 2), LG, rows[i].Item1);
            w.DisplayString(20, b(i + 2), LG, N(rows[i].Item2));
        }
        w.DisplayAndPause(LG, "press a key");
    }

    public void UseItemMenu(ref bool done)
    {
        var c = Cur;
        byte ch = (byte)' ';
        TItemRec sel = c.itemsAddr;
        short idx = 0;
        bool vmRedraw = true, needRedraw = true;
        while (!ExitKey(ch) && !done && c.numberOfItems != 0 && c.itemsAddr != null)
        {
            int countBefore = c.numberOfItems;
            var words = new List<string> { "Ready" };
            if (CharHlp.CanUseItems(c)) words.Add("Use");
            if (Trades(c) && !Combat) words.Add("Trade");
            words.Add(InVault ? "Deposit" : "Drop");
            if (c.numberOfItems <= 15) words.Add("Halve");
            words.Add("Join");
            if (Trades(c) && InShop) words.Add("Sell");
            if (InShop) words.Add("Id");
            for (var p = c.itemsAddr; p != null; p = p.next) w.ItemLabel(p, true);
            if (needRedraw || GDVars.dsRedrawInventoryMenuFlag != 0)
            {
                Frame();
                w.DisplayCreatureName(c, 1, 1, true);
                w.DisplayString(b(c.name.Length + 4), 1, LG, "Items");
                w.DrawWindow(1, 3, 38, 22, 0, 0, "");
                w.DisplayString(1, 3, WH, "Ready Item");
                vmRedraw = true;
                needRedraw = false;
                GDVars.dsRedrawInventoryMenuFlag = 0;
            }
            if (GDVars.stingArg) MenuCore.extraKeys = "V";
            ch = w.MenuV("", Cut40(string.Join(" ", words)), LM, LG, WH, 1, 5, 38, 22, c.itemsAddr, true, ref vmRedraw, ref idx, ref sel);
            if (sel != null)
            {
                char k = (char)ch;
                if (k == 'V')
                {
                    if (GDVars.stingArg) { ViewItemDetail(sel); needRedraw = true; }
                }
                else if (k == 'R') w.ToggleReady(sel);
                else if (k == 'U')
                {
                    if (sel.equipped == 0 && !CharHlp.CastableScroll(c, sel)) { w.PrintBottomLine("Must be Readied"); ch = (byte)' '; }
                    else if (CharHlp.ItemReadable(sel) || (sel.effect != 0 && sel.special <= 0x7F))
                    {
                        w.UseItem(sel, ref done);
                        if (!Combat) done = false;
                        if (!done) needRedraw = true;
                    }
                }
                else if (k == 'T')
                {
                    if (CheckItemAvail(sel)) ItemTradeMenu(sel); else ch = (byte)' ';
                    needRedraw = true;
                }
                else if (k == 'D')
                {
                    bool vaultOk = InVault && CheckItemAvail(sel);
                    if (vaultOk)
                    {
                        if (w.VaultHasRoom()) { w.Deposit(Cur, sel); vmRedraw = true; }
                        else w.DisplayAndPause(YEL, "The vault is full.");
                    }
                    else if (CheckItemAvail(sel))
                    {
                        w.ItemLabel(sel, false);
                        Below(true, "Your " + sel.name + "will be gone forever");
                        if (YN("Drop It? ") == 'Y') { w.RemoveItem(Cur, sel); vmRedraw = true; }
                        w.ClearBox(1, 21, 38, 22);
                    }
                    else ch = (byte)' ';
                }
                else if (k == 'H') HalveItem(sel);
                else if (k == 'J') JoinItem(sel);
                else if (k == 'S') { if (CheckItemAvail(sel)) SellItem(sel); else ch = (byte)' '; }
                else if (k == 'I') IdentifyItem(sel, ref vmRedraw);
            }
            w.RecalcStats(c);
            if (c.numberOfItems != countBefore) vmRedraw = true;
        }
    }

    public void ItemTradeMenu(TItemRec it)
    {
        var t = tradeTarget;
        w.DisplayNoncombatInterface();
        w.SelectChar("Trade with Whom?", true, ref t);
        if (t == null) return;
        tradeTarget = t;
        if (CannotCarryItem(t, it)) { w.PrintBottomLine("Overloaded"); return; }
        w.AppendItemCopy(ref t.itemsAddr, it);
        w.RemoveItem(Cur, it);
        w.RecalcStats(t);
    }

    public void HalveItem(TItemRec it)
    {
        int q = it.quantity;
        if (q < 2) { w.PrintBottomLine("Can't halve that"); return; }
        var copy = new TItemRec();
        CharHlp.CopyItem(it, copy);
        copy.quantity = b(q / 2);
        copy.equipped = 0;
        it.quantity = b(q - q / 2);
        copy.next = it.next;
        it.next = copy;
    }

    static bool SameStack(TItemRec a, TItemRec d)
    {
        return a.nameIds[1] == d.nameIds[1] && a.nameIds[2] == d.nameIds[2] && a.nameIds[3] == d.nameIds[3] &&
               a.itemType == d.itemType && a.plus == d.plus && a.saveBonus == d.saveBonus && a.cursed == d.cursed &&
               a.weight == d.weight && a.charges == d.charges && a.effect == d.effect && a.special == d.special;
    }

    public void JoinItem(TItemRec it)
    {
        if (it.quantity == 0) return;
        var into = it;
        int total = it.quantity;
        // the walk follows the links as they were before each merge removed a node
        var node = Cur.itemsAddr;
        while (node != null)
        {
            var after = node.next;
            if (!ReferenceEquals(node, into) && node.quantity != 0 && node.charges <= 1 && SameStack(node, into))
            {
                if (total + node.quantity > 255)
                {
                    into.quantity = 255;
                    node.quantity = b(total + node.quantity - 255);
                    total = node.quantity;
                    into = node;
                }
                else
                {
                    total += node.quantity;
                    if (node.equipped != 0) into.equipped = node.equipped;
                    w.RemoveItem(Cur, node);
                }
            }
            node = after;
        }
        into.quantity = b(total);
    }

    public void SellItem(TItemRec it)
    {
        int price = w.SalePrice(it);
        w.ItemLabel(it, false);
        Below(true, "I'll give you " + N(price) + " gold pieces for your " + it.name);
        if (YN("Is It a Deal? ") == 'Y')
        {
            w.PrintBottomLine("Sold!");
            w.RemoveItem(Cur, it);
            int plat = price / 5, gold = price % 5;
            ushort room = 0;
            int toChar = plat, toPool = 0;
            if (w.CheckOverload(Cur, plat + gold, ref room))
            {
                w.PrintBottomLine("Overloaded.  Money will be put in pool.");
                if (room <= plat) { toChar = room; toPool = plat - room; }
            }
            Cur.coins[4] = (ushort)((Cur.coins[4] + toChar) & 0xFFFF);
            GDVars.treasureMoney[4] = unchecked(GDVars.treasureMoney[4] + toPool);
            Cur.coins[3] = (ushort)((Cur.coins[3] + gold) & 0xFFFF);
        }
        w.ClearBox(1, 21, 38, 22);
    }

    public void IdentifyItem(TItemRec it, ref bool newInfo)
    {
        w.ItemLabel(it, false);
        Below(true, "For 200 gold pieces I'll identify your " + it.name);
        bool paid = YN("Is It a Deal? ") == 'Y' && w.PayGold(Cur, 200);
        if (paid)
        {
            if (it.unidMask != 0)
            {
                it.unidMask = 0;
                w.ItemLabel(it, false);
                Below(true, "It looks like some sort of " + it.name);
                newInfo = true;
            }
            else Below(true, "I can't tell anything new about your " + it.name);
            w.PauseOnly();
        }
        w.ClearBox(1, 21, 38, 22);
    }

    // ---- coins ----
    public TItemRec PickCoinType(out TItemRec list)
    {
        TItemRec head = null;
        for (int m = 0; m <= 6; m++)
        {
            int held = Cur.coins[m];
            if (held == 0) continue;
            head = new TItemRec { next = head, name = Cut40(PadL(GDConsts.moneyNames[m], 8) + " " + N(held)), inUse = 0 };
        }
        list = head;
        TItemRec pick = null;
        short idx = 0;
        bool redraw = true;
        w.MenuV("Select type of coin", "", LM, LG, WH, 12, 7, 25, 13, head, true, ref redraw, ref idx, ref pick);
        return pick;
    }

    bool AnyCoins() { return Cur.coins.Any(x => x != 0); }

    public void TradeTreasureMenu()
    {
        while (true)
        {
            var t = tradeTarget;
            w.DisplayNoncombatInterface();
            w.SelectChar("Trade to?", true, ref t);
            if (t == null) return;
            DisplayChar(Cur);
            bool leaveAll = false;
            while (true)
            {
                PrintMoney();
                tradeTarget = t;
                TItemRec list;
                var pick = PickCoinType(out list);
                bool leaveInner;
                if (pick == null) { leaveInner = true; leaveAll = false; }
                else
                {
                    string kind = "";
                    sbyte m = MoneyCal.ParseMoneyType(pick.name, ref kind);
                    ushort amount = w.InputNumberMax(Cur.coins[m], "How much " + kind + "will you trade? ", 10);
                    var giver = Cur;
                    w.TradeMoney(ref giver, ref t, amount, m);
                    GDVars.curChar = giver;
                    leaveAll = !AnyCoins();
                    leaveInner = leaveAll;
                }
                if (list != null) w.FreeStringList(ref list);
                if (leaveInner) break;
            }
            if (leaveAll) return;
        }
    }

    public void DropMoneyMenu()
    {
        while (true)
        {
            PrintMoney();
            TItemRec list;
            var pick = PickCoinType(out list);
            bool leave = true;
            if (pick != null)
            {
                string kind = "";
                sbyte m = MoneyCal.ParseMoneyType(pick.name, ref kind);
                string q = "How much " + kind + (InVault ? "will you deposit? " : "will you drop? ");
                ushort amount = w.InputNumberMax(Cur.coins[m], q, 10);
                w.DropMoney(Cur, amount, m);
                leave = !AnyCoins();
            }
            if (list != null) w.FreeStringList(ref list);
            if (leave) return;
        }
    }

    public bool CannotCarryItem(TCharRecord c, TItemRec it)
    {
        w.RecalcStats(c);
        bool full = c.numberOfItems >= 16;
        ushort room = 0;
        bool heavy = w.CheckOverload(c, CharHlp.ItemWeight(it), ref room);
        return full || heavy;
    }
}
