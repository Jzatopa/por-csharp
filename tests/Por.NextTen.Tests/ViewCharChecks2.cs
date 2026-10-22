// ViewChar (src/viewchar.pas) checks, part 2: the groups. Each runs oracle/port pairs (ViewCharChecks1.cs) over seeded
// scenarios, plus hand-worked cases whose expected values were worked out from the Pascal text.
using System;
using System.Collections.Generic;
using System.Linq;
using Por.Core;
using static Por.Core.Types;

static partial class Tests
{
    static TItemRec VcSomeItem(VcWorld w)
    {
        var c = GDVars.curChar;
        var l = new List<TItemRec>();
        for (var p = c.itemsAddr; p != null; p = p.next) l.Add(p);
        if (l.Count == 0 || w.R.Next(8) == 0) return VcItem(w.R);
        return l[w.R.Next(l.Count)];
    }

    // Give curChar several near-copies of one item, each differing at most in one compared field, so Join merges,
    // overflows past 255 and skips stacks.
    static void VcStacks(VcWorld w)
    {
        var c = GDVars.curChar;
        var r = w.R;
        var model = VcItem(r);
        model.charges = (byte)(r.Next(5) == 0 ? 2 : r.Next(0, 2));
        TItemRec head = null, tail = null;
        int n = r.Next(2, 8);
        for (int k = 0; k < n; k++)
        {
            var it = new TItemRec();
            CharHlp.CopyItem(model, it);
            it.next = null;
            it.quantity = (byte)(r.Next(4) == 0 ? 0 : r.Next(4) == 0 ? r.Next(150, 256) : r.Next(1, 120));
            it.equipped = (byte)(r.Next(3) == 0 ? r.Next(1, 4) : 0);
            it.unidMask = (byte)r.Next(0, 3); it.value = (ushort)r.Next(0, 500); it.name = "s" + k; it.inUse = (byte)r.Next(0, 2);
            unchecked { switch (r.Next(36))
            {
                case 0: it.nameIds[1]++; break;
                case 1: it.nameIds[2]++; break;
                case 2: it.nameIds[3]++; break;
                case 3: it.itemType++; break;
                case 4: it.plus++; break;
                case 5: it.saveBonus++; break;
                case 6: it.cursed ^= 1; break;
                case 7: it.weight++; break;
                case 8: it.charges++; break;
                case 9: it.effect++; break;
                case 10: it.special++; break;
            } }
            if (head == null) head = it; else tail.next = it;
            tail = it;
        }
        c.itemsAddr = head;
        c.numberOfItems = (byte)n;
    }

    static void VcSheet()
    {
        VcMany("DisplayChar", 600, (w, o) => o.DisplayChar(w.Any()), w => ViewChar.DisplayChar(w.Any()));
        VcMany("PrintMoney", 300, (w, o) => o.PrintMoney(), w => ViewChar.PrintMoney());
        VcMany("PrintStat", 600, (w, o) => { int k = w.R.Next(6); bool m = w.Coin(); o.PrintStat(k, m); },
               w => { int k = w.R.Next(6); bool m = w.Coin(); ViewChar.PrintStat((sbyte)k, m); });
        VcMany("PrintCombatStats", 600, (w, o) => o.PrintCombatStats(), w => ViewChar.PrintCombatStats());
    }

    static void VcPaladin()
    {
        VcMany("CanHealCure", 1500,
               (w, o) => { var c = w.Any(); w.L("result", o.PaladinReady(c), o.CanHeal(c), o.CanCure(c)); },
               w => { var c = w.Any(); w.L("result", ViewChar.PaladinReady(c), ViewChar.CanHeal(c), ViewChar.CanCure(c)); });
        // paladins whose week's cure count sits at, just under or just over what their level allows
        VcMany("CanCureEdge", 1500,
               (w, o) => { foreach (var c in w.All) w.L("result", o.CanHeal(c), o.CanCure(c)); },
               w => { foreach (var c in w.All) w.L("result", ViewChar.CanHeal(c), ViewChar.CanCure(c)); },
               w =>
               {
                   foreach (var c in w.All)
                   {
                       c.charClass = C_Class.PALADIN;
                       if (w.R.Next(4) != 0) c.status = 0;
                       c.classLevels[C_Level.PALADIN] = (sbyte)w.R.Next(-12, 40);
                       int near = (c.classLevels[C_Level.PALADIN] + 4) / 5 + w.R.Next(-1, 2);
                       c.effectsAddr = new TEffectRec { id = C_Effect.RECENT_CURE, strength = (byte)Math.Max(0, near), next = w.R.Next(3) == 0 ? new TEffectRec { id = C_Effect.RECENT_HEAL } : null };
                   }
               });
        VcMany("LayOnHands", 600, (w, o) => o.LayOnHands(w.Any()), w => ViewChar.LayOnHands(w.Any()));
        VcMany("CureDisease", 600, (w, o) => o.CureDisease(w.Any()), w => ViewChar.CureDisease(w.Any()));
    }

    static void VcStatsPages()
    {
        VcMany("StatsPage", 800, (w, o) => o.StatsPage(w.Any()), w => ViewChar.StatsPage(w.Any()));
        // a long kills list and a long run of menu keys reach Next, Prev and the scroll limits
        VcMany("StatsPageLong", 600, (w, o) => o.StatsPage(w.Any()), w => ViewChar.StatsPage(w.Any()),
               w => { w.Budget = 60; foreach (var bl in w.Blocks.Values) bl.kindCount = (byte)w.R.Next(30, 49); });
        // scripted scrolling on a long kills list: Kills, then Down, Up, Next and Prev in random order
        VcMany("StatsPageScroll", 600, (w, o) => o.StatsPage(w.Any()), w => ViewChar.StatsPage(w.Any()),
               w =>
               {
                   foreach (var bl in w.Blocks.Values) bl.kindCount = (byte)w.R.Next(20, 49);
                   w.Script = new Queue<Tuple<byte, bool>>();
                   w.Script.Enqueue(Tuple.Create((byte)'K', false));
                   var keys = new[] { Tuple.Create((byte)C_Keys.DOWN, true), Tuple.Create((byte)C_Keys.UP, true), Tuple.Create((byte)'N', false), Tuple.Create((byte)'P', false) };
                   for (int k = w.R.Next(4, 20); k > 0; k--) w.Script.Enqueue(keys[w.R.Next(keys.Length)]);
               });
        VcMany("MorePage", 600, (w, o) => o.MorePage(), w => ViewChar.MorePage());
    }

    static void VcMenu()
    {
        VcMany("ViewCharMenu", 1500, (w, o) => { bool d = w.Coin(); o.ViewCharMenu(ref d); w.L("done", d); },
               w => { bool d = w.Coin(); ViewChar.ViewCharMenu(ref d); w.L("done", d); });
        // paladins out of combat, so Heal and Cure appear
        VcMany("ViewCharMenuPaladin", 600, (w, o) => { bool d = false; o.ViewCharMenu(ref d); w.L("done", d); },
               w => { bool d = false; ViewChar.ViewCharMenu(ref d); w.L("done", d); },
               w => { GDVars.ifcState = (byte)(w.Coin() ? C_Ifc.VAULT : C_Ifc.CAMPING); foreach (var c in w.All) { c.charClass = C_Class.PALADIN; c.status = 0; } });
    }

    static void VcItems()
    {
        VcMany("UseItemMenu", 2000, (w, o) => { bool d = w.R.Next(8) == 0; o.UseItemMenu(ref d); w.L("done", d); },
               w => { bool d = w.R.Next(8) == 0; ViewChar.UseItemMenu(ref d); w.L("done", d); });
        VcMany("UseItemMenuShop", 800, (w, o) => { bool d = false; o.UseItemMenu(ref d); w.L("done", d); },
               w => { bool d = false; ViewChar.UseItemMenu(ref d); w.L("done", d); },
               w => { GDVars.ifcState = (byte)(w.Coin() ? C_Ifc.SHOP_TEMPLE : C_Ifc.VAULT); w.Budget = 40; });
        // in the vault, with a count one below the list (as before RecalcStats): the deposit's own redraw request is
        // the only one, since the count then matches the shortened list
        VcMany("UseItemMenuVault", 800, (w, o) => { bool d = false; o.UseItemMenu(ref d); w.L("done", d); },
               w => { bool d = false; ViewChar.UseItemMenu(ref d); w.L("done", d); },
               w =>
               {
                   GDVars.ifcState = C_Ifc.VAULT; w.Budget = 3;
                   int n = 0; for (var p = GDVars.curChar.itemsAddr; p != null; p = p.next) { n++; p.equipped = 0; }
                   GDVars.curChar.numberOfItems = (byte)Math.Max(1, n - 1);
               });
    }

    static void VcItemOps()
    {
        VcMany("CheckItemAvail", 1500, (w, o) => w.L("result", o.CheckItemAvail(VcSomeItem(w))),
               w => w.L("result", ViewChar.CheckItemAvail(VcSomeItem(w))));
        VcMany("HalveItem", 800, (w, o) => o.HalveItem(VcSomeItem(w)), w => ViewChar.HalveItem(VcSomeItem(w)));
        VcMany("JoinItem", 1500, (w, o) => o.JoinItem(VcSomeItem(w)), w => ViewChar.JoinItem(VcSomeItem(w)));
        VcMany("JoinItemStacks", 2500, (w, o) => o.JoinItem(VcSomeItem(w)), w => ViewChar.JoinItem(VcSomeItem(w)), VcStacks);
        VcMany("SellItem", 1500, (w, o) => o.SellItem(VcSomeItem(w)), w => ViewChar.SellItem(VcSomeItem(w)));
        VcMany("IdentifyItem", 800, (w, o) => { bool f = w.Coin(); o.IdentifyItem(VcSomeItem(w), ref f); w.L("info", f); },
               w => { bool f = w.Coin(); ViewChar.IdentifyItem(VcSomeItem(w), ref f); w.L("info", f); });
        VcMany("ItemTradeMenu", 800, (w, o) => { o.tradeTarget = w.Any(); o.ItemTradeMenu(VcSomeItem(w)); },
               w => { ViewChar.tradeTarget = w.Any(); ViewChar.ItemTradeMenu(VcSomeItem(w)); });
        // inventories around the 16-item limit
        VcMany("CannotCarryItemFull", 600, (w, o) => w.L("result", o.CannotCarryItem(w.Any(), VcSomeItem(w))),
               w => w.L("result", ViewChar.CannotCarryItem(w.Any(), VcSomeItem(w))),
               w =>
               {
                   foreach (var c in w.All)
                   {
                       int n = w.R.Next(13, 19);
                       c.itemsAddr = null;
                       for (int k = 0; k < n; k++) { var it = VcItem(w.R); it.next = c.itemsAddr; c.itemsAddr = it; }
                       c.numberOfItems = (byte)n;
                   }
               });
        VcMany("ViewItemDetail", 400, (w, o) => o.ViewItemDetail(VcSomeItem(w)), w => ViewChar.ViewItemDetail(VcSomeItem(w)));
        VcMany("CannotCarryItem", 800, (w, o) => w.L("result", o.CannotCarryItem(w.Any(), VcSomeItem(w))),
               w => w.L("result", ViewChar.CannotCarryItem(w.Any(), VcSomeItem(w))));
    }

    static void VcCoins()
    {
        VcMany("PickCoinType", 600, (w, o) => { TItemRec l; var r = o.PickCoinType(out l); w.L("pick", r, w.ListSig(l)); },
               w => { TItemRec l = null; var r = ViewChar.PickCoinType(ref l); w.L("pick", r, w.ListSig(l)); });
        VcMany("TradeTreasureMenu", 1200, (w, o) => { o.tradeTarget = w.Any(); o.TradeTreasureMenu(); },
               w => { ViewChar.tradeTarget = w.Any(); ViewChar.TradeTreasureMenu(); });
        VcMany("DropMoneyMenu", 1200, (w, o) => o.DropMoneyMenu(), w => ViewChar.DropMoneyMenu());
    }

    // Hand-worked cases: expected values computed by hand from the Pascal text.
    static void VcExamples()
    {
        var w = VcBuild(1);
        VcInstall(w);
        var c = w.All[0];
        GDVars.curChar = c;
        // JoinItem: 200 + 100 overflows; the first stack fills to 255 and the second keeps 45 and becomes the destination
        var a = new TItemRec { quantity = 200, itemType = 3, weight = 1 };
        var b2 = new TItemRec { quantity = 100, itemType = 3, weight = 1 };
        var d = new TItemRec { quantity = 10, itemType = 3, weight = 1 };
        a.next = b2; b2.next = d; c.itemsAddr = a; c.numberOfItems = 3;
        ViewChar.JoinItem(a);
        Eq(255, a.quantity, "join fills the first stack");
        Eq(55, b2.quantity, "join: 45 overflow then the third stack's 10 merge into it");
        Yes(b2.next == null && a.next == b2, "join removes the merged third stack");
        // a stack with 2 charges never joins
        var e1 = new TItemRec { quantity = 5, charges = 2 }; var e2 = new TItemRec { quantity = 5, charges = 2 };
        e1.next = e2; c.itemsAddr = e1; ViewChar.JoinItem(e1);
        Eq(5, e1.quantity, "two charges: no join"); Yes(e1.next == e2, "two charges: both kept");
        // a single item (quantity 0) is left alone
        var z = new TItemRec { quantity = 0 }; var z2 = new TItemRec { quantity = 4 }; z.next = z2; c.itemsAddr = z;
        ViewChar.JoinItem(z); Eq(0, z.quantity, "quantity 0 not joined"); Eq(4, z2.quantity, "other stack kept");
        // HalveItem: 7 -> 4 kept, 3 in the new unreadied stack right after
        var h = new TItemRec { quantity = 7, equipped = 1 }; var after = new TItemRec(); h.next = after;
        ViewChar.HalveItem(h);
        Eq(4, h.quantity, "halve keeps the rest"); Eq(3, h.next.quantity, "halve copy holds half"); Eq(0, h.next.equipped, "copy unreadied");
        Yes(h.next.next == after, "copy linked before the old next");
        var one = new TItemRec { quantity = 1 }; ViewChar.HalveItem(one); Yes(one.next == null && one.quantity == 1, "one item cannot be halved");
        // CanCure: paladin level 6 -> (6-1) div 5 + 1 = 2 cures; level 0 -> 1; level -6 -> (-7) div 5 + 1 = 0
        GDVars.ifcState = C_Ifc.CAMPING;
        var p = new TCharRecord { charClass = C_Class.PALADIN, status = 0 };
        p.classLevels[C_Level.PALADIN] = 6;
        p.effectsAddr = new TEffectRec { id = C_Effect.RECENT_CURE, strength = 1 };
        Yes(ViewChar.CanCure(p), "level 6 paladin has a second cure");
        p.effectsAddr.strength = 2; Yes(!ViewChar.CanCure(p), "level 6 paladin has no third cure");
        p.effectsAddr = null; p.classLevels[C_Level.PALADIN] = 0; Yes(ViewChar.CanCure(p), "level 0 still has one cure");
        p.classLevels[C_Level.PALADIN] = -6; Yes(!ViewChar.CanCure(p), "level -6: no cure");
        p.classLevels[C_Level.PALADIN] = -5; Yes(!ViewChar.CanCure(p), "level -5: (-6) div 5 + 1 = 0 cures");
        p.classLevels[C_Level.PALADIN] = -3; Yes(ViewChar.CanCure(p), "level -3: (-4) div 5 + 1 = 1 cure");
        GDVars.ifcState = C_Ifc.COMBAT; Yes(!ViewChar.CanCure(p) && !ViewChar.CanHeal(p), "no paladin powers in combat");
        // PrintStat: exceptional strength 100 shows as (00), 5 as (05); a score under 10 starts a column later
        w.Log.Clear();
        c.strCur = 18; c.strExc = 100; ViewChar.PrintStat(0, false);
        Yes(w.Log.Contains("Str(7,7,10,\"(00)\")"), "strength 18/00");
        c.strExc = 5; w.Log.Clear(); ViewChar.PrintStat(0, true);
        Yes(w.Log.Contains("Str(7,7,13,\"(05)\")") && w.Log.Contains("Str(5,7,13,\"18\")"), "strength 18/05 while modifying");
        c.intCur = 9; w.Log.Clear(); ViewChar.PrintStat(1, false);
        Yes(w.Log.Contains("Str(6,8,10,\"9\")"), "a one-digit score starts at column 6");
        // the menu line is a String[40]: a paladin in the vault with items, spells and money loses the last letter of Exit
        GDVars.ifcState = C_Ifc.VAULT;
        var q = new TCharRecord { name = "Q", charClass = C_Class.PALADIN, status = 0, npc = 0 };
        q.itemsAddr = new TItemRec { quantity = 1 }; q.numberOfItems = 1; q.memorizedSpells[3] = 5; q.coins[C_Money.GOLD] = 9;
        q.classLevels[C_Level.PALADIN] = 1;
        GDVars.curChar = q; GDVars.creatureList = q; w.All.Clear(); w.All.Add(q); w.Budget = 0; w.Log.Clear();
        bool done = false; ViewChar.ViewCharMenu(ref done);
        Yes(w.Log.Any(x => x.StartsWith("MenuH(\"\",\"Items Spells Trade Deposit Heal Cure Exi\",")), "vault paladin menu cut to 40 characters");
        // SellItem: a price of 23 pays 4 platinum and 3 gold
        w = VcBuild(2); VcInstall(w); c = w.All[0]; GDVars.curChar = c;
        for (int m = 0; m <= 6; m++) c.coins[m] = 0;
        ViewChar.ItemSalePriceCall = it => 23; ViewChar.YesNoMenuCall = (s1, x, y, x3) => (byte)'Y';
        ushort room = 2;
        ViewChar.CheckOverloadCall = new ViewChar.CheckOverloadProc((TCharRecord cc, int wt, ref ushort t) => { Eq(7, wt, "sale weighs plat + gold coins"); t = room; return true; });
        int pool = GDVars.treasureMoney[C_Money.PLATINUM];
        ViewChar.SellItem(new TItemRec());
        Eq(2, c.coins[C_Money.PLATINUM], "overloaded: platinum up to what fits"); Eq(3, c.coins[C_Money.GOLD], "gold added even when overloaded");
        Eq(pool + 2, GDVars.treasureMoney[C_Money.PLATINUM], "the rest of the platinum goes to the pool");
        // CannotCarryItem: 16 items is full even when the weight fits
        ViewChar.CheckOverloadCall = new ViewChar.CheckOverloadProc((TCharRecord cc, int wt, ref ushort t) => { t = 0; return false; });
        ViewChar.RecalcStatsCall = cc => { };
        c.numberOfItems = 16; Yes(ViewChar.CannotCarryItem(c, new TItemRec()), "16 items: cannot carry");
        c.numberOfItems = 15; Yes(!ViewChar.CannotCarryItem(c, new TItemRec()), "15 items: can carry");
    }

    static void VcSeamsAndBindings()
    {
        // every call point defaults to the converted routine named in viewchar.pas
        var expect = new Dictionary<string, string>
        {
            {"ClearScreenCall","TextRend.ClearScreen"},{"DisplayStringCall","TextRend.DisplayString"},{"DisplayCenteredCall","TextRend.DisplayCentered"},
            {"DisplayMessageWindowCall","TextRend.DisplayMessageWindow"},{"DisplayAndPauseCall","TextRend.DisplayAndPause"},{"PauseOnlyCall","TextRend.PauseOnly"},
            {"DrawBorderCall","Border.DrawBorder"},{"ClearBoxCall","Border.ClearBox"},{"DrawWindowCall","Border.DrawWindow"},
            {"DisplayCreatureNameCall","CharDisp.DisplayCreatureName"},{"PrintItemLabelCall","CharDisp.PrintItemLabel"},
            {"DisplayNoncombatInterfaceCall","CharDisp.DisplayNoncombatInterface"},{"SelectCharCall","CharDisp.SelectChar"},
            {"PrintBottomLineCall","CharDisp.PrintBottomLine"},{"PrintNameStringCall","CharDisp.PrintNameString"},
            {"ClearMessageWindowCall","CharDisp.ClearMessageWindow"},{"PrintArmorClassCall","CharDisp.PrintArmorClass"},
            {"PrintHitPointsCall","CharDisp.PrintHitPoints"},{"YesNoMenuCall","MenuCore.YesNoMenu"},{"MenuHorizontalCall","MenuCore.MenuHorizontal"},
            {"MenuVerticalCall","MenuCore.MenuVertical"},{"FreeStringListCall","MenuCore.FreeStringList"},
            {"PlayerPortraitsHereCall","Portrait.PlayerPortraitsHere"},{"DisplayCharPortraitCall","Portrait.DisplayCharPortrait"},
            {"SetNoncombatPaletteCall","CbtMap.SetNoncombatPalette"},{"SetCombatPaletteCall","CbtMap.SetCombatPalette"},
            {"RestoreHitPointsCall","FxEngine.RestoreHitPoints"},{"AddEffectCall","FxEngine.AddEffect"},{"HasDiseaseCall","FxEngine.HasDisease"},
            {"RemoveDiseasesCall","FxEngine.RemoveDiseases"},{"ToggleReadyItemCall","FxEngine.ToggleReadyItem"},
            {"SpellMenuCall","Magic.SpellMenu"},{"UseItemCall","Magic.UseItem"},{"RecalcStatsCall","CharHlp.RecalcStats"},
            {"RemoveItemCall","CharHlp.RemoveItem"},{"AppendItemCopyCall","CharHlp.AppendItemCopy"},{"ItemLabelCall","CharHlp.ItemLabel"},
            {"BlockOfCall","Stats.BlockOf"},{"StatsCountCall","Stats.Count"},{"CheckOverloadCall","Money.CheckOverload"},
            {"PayGoldCall","Money.PayGold"},{"InputNumberMaxCall","Money.InputNumberMax"},{"TradeMoneyCall","Money.TradeMoney"},
            {"DropMoneyCall","Money.DropMoney"},{"ItemSalePriceCall","MoneyCal.ItemSalePrice"},{"VaultHasRoomCall","Vault.HasRoom"},
            {"DepositItemCall","Vault.DepositItem"},
        };
        int seen = 0;
        foreach (var f in typeof(ViewChar).GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic))
        {
            var dl = f.GetValue(null) as Delegate;
            if (dl == null || !f.Name.EndsWith("Call")) continue;
            seen++;
            Yes(expect.ContainsKey(f.Name), "call point " + f.Name + " expected");
            EqS(expect[f.Name], dl.Method.DeclaringType.Name + "." + dl.Method.Name, f.Name + " default");
        }
        Eq(expect.Count, seen, "call points");
        CommerceServices.ViewCharacter = null; CommerceServices.CannotCarryItem = null;
        try
        {
            ViewCharBindings.BindCommerce();
            EqS("ViewChar.ViewCharMenu", CommerceServices.ViewCharacter.Method.DeclaringType.Name + "." + CommerceServices.ViewCharacter.Method.Name, "commerce view");
            EqS("ViewChar.CannotCarryItem", CommerceServices.CannotCarryItem.Method.DeclaringType.Name + "." + CommerceServices.CannotCarryItem.Method.Name, "commerce carry");
        }
        finally { CommerceServices.ViewCharacter = null; CommerceServices.CannotCarryItem = null; }
        // CharGen's view service forwards each call to ViewChar
        var saved = CharGenView.Host;
        try
        {
            var w = VcBuild(3);
            VcInstall(w);
            ViewCharBindings.BindCharGen();
            Yes(CharGenView.Host != null, "chargen view bound");
            w.Log.Clear(); CharGenView.PrintMoney();
            Yes(w.Log.Count > 0 && w.Log[0] == "ClearBox(12,7,26,14)", "PrintMoney forwarded");
            w.Log.Clear(); CharGenView.PrintStat(2, true);
            Yes(w.Log.Count > 1 && w.Log[0] == "ClearBox(5,9,11,9)" && w.Log[1].StartsWith("Str(") && w.Log[1].Contains(",9,13,"), "PrintStat forwarded with its index and highlight");
            w.Log.Clear(); CharGenView.PrintCombatStats();
            Yes(w.Log.Count > 0 && w.Log[0].StartsWith("Recalc("), "PrintCombatStats forwarded");
            w.Log.Clear(); CharGenView.DisplayChar(w.All[0]);
            Yes(w.Log.Count > 2 && w.Log[2] == "Name(" + w.F(w.All[0]) + ",1,1,F)", "DisplayChar forwarded with its creature");
            w.Budget = 0; w.Log.Clear(); bool ch = true; CharGenView.ViewCharMenu(ref ch);
            Yes(!ch && w.Log.Last() == "Noncombat()", "ViewCharMenu forwarded");
        }
        finally { CharGenView.Host = saved; }
    }
}
