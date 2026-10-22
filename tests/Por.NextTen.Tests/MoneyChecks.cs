// Money checks, written from money.pas. Each routine is run against an independent re-implementation of the Pascal source (below)
// from identical, seeded party/pool/key/dice state; creature coins, the pool, screens, pictures, key reads and logged seam calls must agree.
// Shared, separately tested components used by both sides: CharHlp.RecalcStats, AbilAdj.WeightAllowance, MoneyCal.WhoPays/ParseMoneyType,
// MenuCore menus, TextRend/Border drawing, CharDisp.PrintBottomLine/DisplayCreatureName.
using System;
using System.Collections.Generic;
using System.Linq;
using Por.Core;

static partial class Tests
{
    static void MBase()
    {
        GDVars.baseItems = new Types.TBaseItems();
        for (int i = 0; i <= 128; i++) GDVars.baseItems[i] = new Types.TBaseItemRec();
    }
    static Types.TCharRecord MParty(Random r, int count, bool bags)
    {
        Types.TCharRecord head = null, prev = null;
        for (int i = 0; i < count; i++)
        {
            var c = MkChar("C" + i, 50, 5, 9, 1, 0);
            int k = r.Next(8);
            c.npc = (byte)(k < 4 ? 0 : k == 4 ? C_Npc.ANIMATED_PC : k == 5 ? C_Npc.FLAG | r.Next(100) : k == 6 ? C_Npc.ANIMATED_NPC : 0);
            c.useStrength = 1; c.strCur = (byte)r.Next(3, 26); c.strExc = (byte)r.Next(0, 101);
            for (int t = 0; t < 7; t++) c.coins[t] = (ushort)(r.Next(3) == 0 ? 0 : r.Next(3) == 0 ? r.Next(65536) : r.Next(0, 400));
            int items = r.Next(3) == 0 ? r.Next(1, 4) : 0; Types.TItemRec tail = null;
            for (int j = 0; j < items + (bags && r.Next(2) == 0 ? 1 : 0); j++)
            {
                var it = new Types.TItemRec { name = "I" + j, inUse = 1, itemType = (byte)r.Next(1, 20), weight = (ushort)r.Next(0, 300), quantity = (byte)(r.Next(3) == 0 ? r.Next(1, 5) : 0) };
                if (j == items) { it.equipped = 1; it.nameIds[1] = C_Item.NAME_HOLDING; it.weight = 15; it.quantity = 0; it.itemType = 1; }
                if (tail == null) c.itemsAddr = it; else tail.next = it; tail = it;
            }
            CharHlp.RecalcStats(c);
            if (prev == null) head = c; else prev.nextCharAddr = c; prev = c;
        }
        GDVars.creatureList = head;
        return head;
    }
    static Types.TCharRecord MPick(Types.TCharRecord head, Random r) { int n = 0; for (var p = head; p != null; p = p.nextCharAddr) n++; var q = head; for (int i = r.Next(n); i > 0; i--) q = q.nextCharAddr; return q; }
    static void MPool(Random r)
    {
        GDVars.treasureMoney = new Types.TMoney();
        for (int t = 0; t < 7; t++) GDVars.treasureMoney[t] = r.Next(3) == 0 ? 0 : r.Next(3) == 0 ? r.Next(0, 3000000) : r.Next(0, 3000);
        GDVars.treasureItems = r.Next(2) == 0 ? null : new Types.TItemRec { name = "x" };
    }
    static string MDump()
    {
        var sb = new System.Text.StringBuilder();
        for (var p = GDVars.creatureList; p != null; p = p.nextCharAddr)
            sb.Append(p.name + ":" + p.npc + ":" + string.Join(",", p.coins) + ":" + p.numberOfItems + ":" + string.Join(",", ItemsOf(p)) + ";");
        sb.Append("pool:" + string.Join(",", GDVars.treasureMoney.Data) + ":" + (GDVars.treasureItems == null));
        return sb.ToString();
    }
    static IEnumerable<string> ItemsOf(Types.TCharRecord p) { for (var i = p.itemsAddr; i != null; i = i.next) yield return i.name + "/" + i.itemType + "/" + i.value + "/" + i.weight + "/" + i.nameIds[3]; }
    static Action MSetup(int seed, int count, bool bags, Func<Random, Types.TCharRecord, int> extra = null)
    {
        return () =>
        {
            MBase(); var r = new Random(seed); var head = MParty(r, count, bags); MPool(r);
            GDVars.curChar = head == null ? null : MPick(head, r); GDVars.ifcState = (byte)(r.Next(4) == 0 ? C_Ifc.LOOT : r.Next(3) == 0 ? C_Ifc.SHOP_TEMPLE : r.Next(3) == 0 ? C_Ifc.VAULT : C_Ifc.INDOORS);
            Dice.RandomBelow = n => r.Next(n);
            if (extra != null) extra(r, head);
        };
    }
    static void MEnd() { cdLog.Add("state:" + MDump()); }
    static void MClean() { Dice.RandomBelow = null; GDVars.creatureList = null; GDVars.curChar = null; GDVars.treasureItems = null; CdRestore(); }

    // ---- independent models ----
    static bool OBag(Types.TCharRecord c) { for (var i = c.itemsAddr; i != null; i = i.next) if (i.equipped != 0 && i.nameIds[1] == 186) return true; return false; }
    static int OCap(Types.TCharRecord c) { return AbilAdj.WeightAllowance(c) + 1500 + (OBag(c) ? 5000 : 0); }
    static int ORaw(Types.TCharRecord c) { int w = 0; for (var i = c.itemsAddr; i != null; i = i.next) w += i.quantity > 0 ? i.quantity * i.weight : i.weight; foreach (var k in c.coins) w += k; return w; }
    static ushort OSpare(Types.TCharRecord c) { int room = OCap(c) - ORaw(c); if (room < 0) room = 0; return (ushort)(room & 0xFFFF); }
    static bool OOver(Types.TCharRecord c, int w, ref ushort takable) { takable = OSpare(c); if (w > takable) return true; takable = 0; return false; }
    static int[] OChange(int rem, long cap)
    {
        var m = new int[7]; if (rem < 0) rem = 0;
        for (int i = 4; i >= 0; i--) { int val = new[] { 1, 10, 100, 200, 1000 }[i]; long n = rem / val; if (n > cap) n = cap; m[i] = (int)n; rem = unchecked((int)(rem - n * val)); }
        return m;
    }
    static int OCopper(IEnumerable<int> m) { int t = 0, k = 0; foreach (var v in m) { if (k < 5) t = unchecked(t + new[] { 1, 10, 100, 200, 1000 }[k] * v); k++; } return t; }
    static void OPayCreature(Types.TCharRecord c, int gp)
    {
        var m = new int[7]; for (int i = 0; i <= 4; i++) m[i] = c.coins[i];
        var res = OChange(unchecked(OCopper(m) - gp * 200), 65535);
        for (int i = 0; i <= 4; i++) c.coins[i] = (ushort)(res[i] & 0xFFFF);
        CharHlp.RecalcStats(c);
    }
    static void OPayPool(int gp)
    {
        var res = OChange(unchecked(OCopper(GDVars.treasureMoney.Data) - gp * 200), int.MaxValue);
        for (int i = 0; i <= 4; i++) GDVars.treasureMoney[i] = res[i];
    }
    static void OGivePlatinum(Types.TCharRecord c, int pp)
    {
        ushort takable = 0;
        if (OOver(c, pp, ref takable))
        {
            CharDisp.PrintBottomLine("Overloaded.  Money will be put in Pool.");
            c.coins[4] = (ushort)((c.coins[4] + takable) & 0xFFFF); CharHlp.RecalcStats(c);
            GDVars.treasureMoney[4] = pp + GDVars.treasureMoney[4] - takable;
        }
        else { c.coins[4] = (ushort)((c.coins[4] + pp) & 0xFFFF); CharHlp.RecalcStats(c); }
    }
    // Pascal Val of a digit string into a LongInt/Word (empty gives 0)
    static int OVal(string s) { long v = 0; foreach (char ch in s) v = v * 10 + (ch - '0'); return (int)v; }
    static ushort OInput(int max, string query, int color)
    {
        TextRend.ClearBottomLine(); if (query.Length > 40) query = query.Substring(0, 40); TextRend.DisplayString(0, 24, (byte)color, query);
        int qLen = query.Length, col = qLen; string maxStr = max.ToString(), s = ""; byte ch;
        do
        {
            ch = GameIo.ReadGameKey();
            if (ch >= '0' && ch <= '9')
            {
                s += (char)ch; if (s.Length > 6) s = s.Substring(0, 6);
                if (max >= OVal(s)) col = (col + 1) & 255; else { s = maxStr; col = (qLen + maxStr.Length) & 255; }
                TextRend.DisplayString((byte)qLen, 24, 15, s);
            }
            else if (ch == 8)
            {
                if (s.Length > 0) { s = s.Substring(0, s.Length - 1); TextRend.PrintSpaces((byte)col, 24, 0, 1); col = (col - 1) & 255; }
            }
        } while (ch != 13 && ch != 27);
        TextRend.ClearBottomLine();
        return ch == 27 ? (ushort)0 : (ushort)(OVal(s) & 0xFFFF);
    }
    static void OTake(Types.TCharRecord p, int qty, int t)
    {
        ushort takable = 0;
        if (OOver(p, qty, ref takable)) CharDisp.PrintBottomLine("Overloaded");
        else
        {
            if (qty > GDVars.treasureMoney[t]) qty = GDVars.treasureMoney[t] & 0xFFFF;
            GDVars.treasureMoney[t] = GDVars.treasureMoney[t] - qty;
            p.coins[t] = (ushort)((p.coins[t] + qty) & 0xFFFF); CharHlp.RecalcStats(p);
        }
    }
    static int OHitEq;
    static void OShare()
    {
        int cnt = 0; for (var q = GDVars.creatureList; q != null; q = q.nextCharAddr) if (q.npc < 0x80) cnt++;
        if (cnt == 0) return;
        var share = new int[7]; var rem = new int[7];
        for (int i = 0; i < 7; i++) if (GDVars.treasureMoney[i] > 0) { share[i] = GDVars.treasureMoney[i] / cnt; rem[i] = GDVars.treasureMoney[i] % cnt; }
        ushort takable = 0;
        for (var p = GDVars.creatureList; p != null; p = p.nextCharAddr)
        {
            if (p.npc >= 0x80) continue;
            for (int i = 6; i >= 0; i--)
            {
                if (!OOver(p, share[i], ref takable))
                {
                    p.coins[i] = (ushort)((share[i] + p.coins[i]) & 0xFFFF); CharHlp.RecalcStats(p);
                    if (rem[i] > 0 && !OOver(p, rem[i], ref takable)) { p.coins[i] = (ushort)((p.coins[i] + 1) & 0xFFFF); CharHlp.RecalcStats(p); rem[i]--; }
                }
                else { p.coins[i] = (ushort)((p.coins[i] + takable) & 0xFFFF); rem[i] = rem[i] + share[i] - takable; CharHlp.RecalcStats(p); }
            }
        }
        for (int i = 6; i >= 0; i--)
            if (rem[i] > 0)
                for (var p = GDVars.creatureList; p != null; p = p.nextCharAddr)
                {
                    int tk = OSpare(p);
                    if (tk > 0)
                    {
                        if (rem[i] == tk) OHitEq++;
                        if (rem[i] > tk) { p.coins[i] = (ushort)((p.coins[i] + tk) & 0xFFFF); CharHlp.RecalcStats(p); rem[i] -= tk; }
                        else { p.coins[i] = (ushort)((p.coins[i] + rem[i]) & 0xFFFF); CharHlp.RecalcStats(p); rem[i] = 0; }
                    }
                }
        for (int i = 0; i < 7; i++) GDVars.treasureMoney[i] = rem[i];
    }
    static void OTakeMenu()
    {
        Border.DrawWindow(1, 1, 38, 22, 0, 10, "");
        short idx = 0; bool exitF;
        do
        {
            bool redraw = true; Types.TItemRec p = null, sel = null;
            for (int i = 6; i >= 0; i--)
                if (GDVars.treasureMoney[i] > 0)
                {
                    var n = new Types.TItemRec { name = GDConsts.moneyNames[i] + " " + GDVars.treasureMoney[i], inUse = 0 };
                    n.next = p; p = n;   // newest node is the head; the first node ends the list
                }
            sel = p;
            byte ch = MenuCore.MenuVertical("Select type of coin", "", 13, 10, 15, 2, 2, 15, 8, p, true, ref redraw, ref idx, ref sel);
            if (sel == null || ch == 0) exitF = true;
            else
            {
                string numStr = ""; int mType = MoneyCal.ParseMoneyType(sel.name, ref numStr);
                numStr = "How much " + numStr + "will you take? ";
                int n = GDVars.treasureMoney[mType] > 65535 ? 65535 : GDVars.treasureMoney[mType];
                n = OInput(n, numStr, 10);
                OTake(GDVars.curChar, n, mType);
                exitF = true; for (int i = 0; i <= 6; i++) if (GDVars.treasureMoney[i] > 0) exitF = false;
            }
        } while (!exitF);
    }
    static bool OGoBack(string said, string ask)
    {
        TextRend.ShowMessage(10, true, said); TextRend.ShowMessage(15, false, ask);
        if (EclMenu.MenuHorizontalCS("", "~Yes ~No", 13, 10, 15, false, false) == 1) return false;
        TextRend.ClearRect(1, 17, 38, 22); return true;
    }
    static int OMsgTop() { return C_Msg.MESSAGE_TOP; }
    static void OAppraise(ref bool ok)
    {
        var cur = GDVars.curChar; bool ext = false; byte ch;
        ushort takable = 0; string actionStr;
        Func<Types.TCharRecord, bool> hasRoom = c => { CharHlp.RecalcStats(c); return c.numberOfItems < 16 && !OOver(c, 1, ref takable); };
        Action<int, int> sellOrKeep = (nameId, worth) =>
        {
            Types.TCharRecord holder = null;
            if (hasRoom(cur)) holder = cur;
            else
            {
                TextRend.DisplayString(1, 14, 15, cur.name + " doesn't have space");
                for (var c = GDVars.creatureList; c != null && holder == null; c = c.nextCharAddr)
                    if (c != cur && c.npc < 0x80 && hasRoom(c)) holder = c;
                TextRend.DisplayString(1, 15, 15, holder != null ? "Keep will give to " + holder.name : "No one else has space either");
            }
            actionStr = holder != null ? "Sell Keep" : "Sell";
            byte k = MenuCore.MenuHorizontal("You can: ", actionStr, 13, 10, 15, true, false, ref ext);
            if (k == 'K' && holder != null)
            {
                var gem = new Types.TItemRec { itemType = 70, value = (ushort)worth, weight = 1 }; gem.nameIds[3] = (byte)nameId;
                Types.TItemRec tail = holder.itemsAddr;
                var cp = new Types.TItemRec { itemType = 70, value = (ushort)worth, weight = 1, name = gem.name, inUse = gem.inUse }; cp.nameIds[3] = (byte)nameId;
                if (tail == null) holder.itemsAddr = cp; else { while (tail.next != null) tail = tail.next; tail.next = cp; }
                CharHlp.RecalcStats(holder);
            }
            else OGivePlatinum(cur, worth / 5);
        };
        ok = true;
        if (cur.coins[5] == 0 && cur.coins[6] == 0) { CharDisp.PrintBottomLine("No Gems or Jewelry"); ok = false; return; }
        bool exitF;
        do
        {
            if (cur.coins[5] == 0 && cur.coins[6] == 0) exitF = true;
            else
            {
                exitF = false;
                string gemStr = cur.coins[5].ToString(), jwlStr = cur.coins[6].ToString();
                gemStr = cur.coins[5] == 0 ? "" : cur.coins[5] == 1 ? gemStr + " Gem" : gemStr + " Gems";
                jwlStr = cur.coins[6] == 0 ? "" : cur.coins[6] == 1 ? jwlStr + " piece of Jewelry" : jwlStr + " pieces of Jewelry";
                TextRend.ClearRect(1, 1, 38, 22); CharDisp.DisplayCreatureName(cur, 1, 1, false);
                TextRend.DisplayString(1, 7, 15, "You have a fine collection of:"); TextRend.DisplayString(1, 9, 15, gemStr); TextRend.DisplayString(1, 10, 15, jwlStr);
                string menu = ""; if (cur.coins[5] != 0) menu = "Gems "; if (cur.coins[6] != 0) menu += "Jewelry "; menu += "Exit";
                ch = MenuCore.MenuHorizontal("Appraise: ", menu, 13, 10, 15, true, false, ref ext);
                if (ch == 'G' && cur.coins[5] != 0)
                {
                    cur.coins[5]--; int r = Dice.RollDice(1, 100); int v = r <= 25 ? 10 : r <= 50 ? 50 : r <= 70 ? 100 : r <= 90 ? 500 : r <= 99 ? 1000 : 5000;
                    TextRend.DisplayString(1, 12, 15, "The Gem is Valued at " + v + " gp."); sellOrKeep(101, v);
                }
                else if (ch == 'J' && cur.coins[6] != 0)
                {
                    cur.coins[6]--; int r = Dice.RollDice(1, 100); int v;
                    if (r <= 10) v = Dice.RandomBelow(900) + 100; else if (r <= 20) v = Dice.RandomBelow(1000) + 200; else if (r <= 40) v = Dice.RandomBelow(1500) + 300;
                    else if (r <= 50) v = Dice.RandomBelow(2500) + 500; else if (r <= 70) v = Dice.RandomBelow(5000) + 1000; else if (r <= 90) v = Dice.RandomBelow(6000) + 2000; else v = Dice.RandomBelow(10000) + 2000;
                    TextRend.DisplayString(1, 12, 15, "The Jewel is Valued at " + v + " gp."); sellOrKeep(214, v);
                }
                else if (ch == 'E' || ch == 0) exitF = true;
                CharHlp.RecalcStats(cur);
            }
        } while (!exitF);
    }

    // ---- groups ----
    static void MoneyCapacity()
    {
        try
        {
            int tested = 0, over = 0, edge = 0;
            for (int n = 0; n < 600; n++)
            {
                MSetup(n, 1 + n % 5, true)();
                for (var c = GDVars.creatureList; c != null; c = c.nextCharAddr)
                {
                    Eq(OCap(c), Money.CarryingCapacity(c), "capacity " + n); Eq(OSpare(c), Money.SpareCapacity(c), "spare " + n);
                    int sp = OSpare(c);
                    foreach (int w in new[] { 0, 1, sp - 1, sp, sp + 1, sp + 2, 65535, 70000, -5, rng.Next(0, 5000) })
                    {
                        ushort a = 77, b = 99; bool ra = Money.CheckOverload(c, w, ref a), rb = OOver(c, w, ref b);
                        Yes(ra == rb && a == b, "CheckOverload n=" + n + " w=" + w + " spare=" + sp + " got " + ra + "/" + a + " expected " + rb + "/" + b);
                        tested++; if (ra) over++; if (w == sp || w == sp + 1) edge++;
                    }
                }
            }
            Yes(over > 500 && tested - over > 500 && edge > 500, "capacity coverage " + over + "/" + tested + " edge " + edge);
        }
        finally { MClean(); }
    }

    static void MoneyPay()
    {
        try
        {
            for (int n = 0; n < 500; n++)
            {
                int cnt = 1 + n % 3; var rr = new Random(n * 5 + 1); int gp = rr.Next(4) == 0 ? rr.Next(0, 30) : rr.Next(0, 1200); if (rr.Next(8) == 0) gp = rr.Next(0, 100000);
                int payer = rr.Next(4);
                CdSame("PayFromCreature " + n, MSetup(n, cnt, false), () => { Money.PayFromCreature(GDVars.curChar, gp); MEnd(); }, () => { OPayCreature(GDVars.curChar, gp); MEnd(); });
                CdSame("PayFromPool " + n, MSetup(n, cnt, false), () => { Money.PayFromPool(gp); MEnd(); }, () => { OPayPool(gp); MEnd(); });
                CdSame("PayBy " + n, MSetup(n, cnt, false), () => { Money.PayBy(GDVars.curChar, (byte)payer, gp); MEnd(); }, () => { if (payer == 1) OPayCreature(GDVars.curChar, gp); else if (payer == 2) OPayPool(gp); MEnd(); });
                bool ra = false, rb = false;
                CdSame("PayGold " + n, MSetup(n, cnt, false), () => { ra = Money.PayGold(GDVars.curChar, gp); cdLog.Add("ret:" + ra); MEnd(); },
                    () => { byte who = MoneyCal.WhoPays(GDVars.curChar, gp); if (who == 0) CharDisp.PrintBottomLine("Not enough money."); else if (who == 1) OPayCreature(GDVars.curChar, gp); else if (who == 2) OPayPool(gp); rb = who != 0; cdLog.Add("ret:" + rb); MEnd(); });
            }
        }
        finally { MClean(); }
    }

    static void MoneyMove()
    {
        try
        {
            for (int n = 0; n < 600; n++)
            {
                var rr = new Random(n * 3 + 2); int cnt = 2 + n % 3; int mt = rr.Next(7); int qty = rr.Next(3) == 0 ? rr.Next(65536) : rr.Next(0, 500); int pp = rr.Next(3) == 0 ? rr.Next(65536) : rr.Next(0, 800);
                CdSame("TradeMoney " + n, MSetup(n, cnt, true), () => { var g = GDVars.creatureList; var rc = GDVars.creatureList.nextCharAddr; Money.TradeMoney(ref g, ref rc, (ushort)qty, (sbyte)mt); MEnd(); },
                    () => {
                        var g = GDVars.creatureList; var rc = GDVars.creatureList.nextCharAddr;
                        if (qty <= OSpare(rc)) { g.coins[mt] = (ushort)((g.coins[mt] - qty) & 0xFFFF); CharHlp.RecalcStats(g); rc.coins[mt] = (ushort)((rc.coins[mt] + qty) & 0xFFFF); CharHlp.RecalcStats(rc); } else CharDisp.PrintBottomLine("Overloaded"); MEnd(); });
                CdSame("DropMoney " + n, MSetup(n, cnt, false), () => { Money.DropMoney(GDVars.curChar, (ushort)qty, (sbyte)mt); MEnd(); },
                    () => { var p = GDVars.curChar; p.coins[mt] = (ushort)((p.coins[mt] - qty) & 0xFFFF); CharHlp.RecalcStats(p); if (GDVars.ifcState == 6 || GDVars.ifcState == 1 || GDVars.ifcState == 8) GDVars.treasureMoney[mt] += qty; MEnd(); });
                CdSame("TakeMoney " + n, MSetup(n, cnt, true), () => { Money.TakeMoney(GDVars.curChar, (ushort)qty, (sbyte)mt); MEnd(); }, () => { OTake(GDVars.curChar, qty, mt); MEnd(); });
                foreach (int d in new[] { 1, 0, -1 })
                    CdSame("TakeMoney pool+" + d + " " + n, MSetup(n, cnt, true), () => { int q = Math.Max(0, Math.Min(65535, GDVars.treasureMoney[mt] + d)); Money.TakeMoney(GDVars.curChar, (ushort)q, (sbyte)mt); MEnd(); },
                        () => { int q = Math.Max(0, Math.Min(65535, GDVars.treasureMoney[mt] + d)); OTake(GDVars.curChar, q, mt); MEnd(); });
                CdSame("GivePlatinum " + n, MSetup(n, cnt, true), () => { Money.GivePlatinum(GDVars.curChar, (ushort)pp); MEnd(); }, () => { OGivePlatinum(GDVars.curChar, pp); MEnd(); });
                CdSame("PoolMoney " + n, MSetup(n, cnt + 2, true), () => { Money.PoolMoney(); MEnd(); },
                    () => {
                        for (var p = GDVars.creatureList; p != null; p = p.nextCharAddr)
                            if (p.npc == 0 || p.npc == 0xB3) { for (int i = 0; i < 7; i++) GDVars.treasureMoney[i] = p.coins[i] + GDVars.treasureMoney[i]; for (int i = 0; i < 7; i++) p.coins[i] = 0; CharHlp.RecalcStats(p); }
                        MEnd(); });
                CdSame("ShareMoney " + n, MSetup(n, n % 7, true), () => { Money.ShareMoney(); MEnd(); }, () => { OShare(); MEnd(); });
                CdSame("TradeMoney exact " + n, MSetup(n, cnt, true), () => { var g = GDVars.creatureList; var rc = g.nextCharAddr; Money.TradeMoney(ref g, ref rc, Money.SpareCapacity(rc), (sbyte)mt); MEnd(); },
                    () => { var g = GDVars.creatureList; var rc = g.nextCharAddr; ushort q = OSpare(rc); g.coins[mt] = (ushort)((g.coins[mt] - q) & 0xFFFF); CharHlp.RecalcStats(g); rc.coins[mt] = (ushort)((rc.coins[mt] + q) & 0xFFFF); CharHlp.RecalcStats(rc); MEnd(); });
                bool ta = false, ia = false, tb = false, ib = false;
                CdSame("DetermineTreasure " + n, MSetup(n, 1, false), () => { ta = true; ia = true; Money.DetermineTreasure(ref ta, ref ia); cdLog.Add(ta + "/" + ia); },
                    () => { tb = false; for (int i = 0; i < 7; i++) if (GDVars.treasureMoney[i] != 0) tb = true; ib = GDVars.treasureItems != null; cdLog.Add(tb + "/" + ib); });
            }
        }
        finally { MClean(); }
    }

    static byte[] MDigits(Random r)
    {
        var k = new List<byte>(); int ops = r.Next(1, 14);
        for (int i = 0; i < ops; i++) { int t = r.Next(10); if (t < 7) k.Add((byte)('0' + r.Next(10))); else if (t < 9) k.Add(8); else k.Add((byte)'x'); }
        k.Add((byte)(r.Next(5) == 0 ? 27 : 13)); return k.ToArray();
    }
    static void MoneyInput()
    {
        try
        {
            for (int n = 0; n < 1500; n++)
            {
                var rr = new Random(n * 9 + 4); int max = rr.Next(4) == 0 ? rr.Next(65536) : rr.Next(0, 3000); if (rr.Next(10) == 0) max = 65535; if (rr.Next(10) == 0) max = 0;
                string q = "How many" + rr.Next(10) + "? "; if (rr.Next(15) == 0) q = new string('Q', new[] { 39, 40, 41, 42, 45 }[rr.Next(5)]); int color = rr.Next(1, 16); var keys = MDigits(rr);
                ushort ra = 0, rb = 0;
                CdSame("InputNumberMax " + n + " max=" + max + " keys=" + string.Join(",", keys), MSetup(n, 1, false), () => { ra = Money.InputNumberMax((ushort)max, q, (byte)color); cdLog.Add("ret:" + ra); },
                    () => { rb = OInput(max, q, color); cdLog.Add("ret:" + rb); }, keys);
            }
        }
        finally { MClean(); }
    }

    static byte[] MMenuKeys(Random r)
    {
        var k = new List<byte>(); int ops = r.Next(0, 5);
        for (int i = 0; i < ops; i++) { int t = r.Next(4); if (t == 0) { k.Add(0); k.Add(80); } else if (t == 1) { k.Add(0); k.Add(72); } else if (t == 2) k.Add((byte)'x'); else { k.Add(0); k.Add(75); } }
        k.Add(13); k.AddRange(MDigits(r));
        return k.ToArray();
    }
    static void MoneyMenus()
    {
        try
        {
            for (int n = 0; n < 600; n++)
            {
                var rr = new Random(n * 11 + 6); var keys = new List<byte>(); for (int i = 0; i < rr.Next(1, 4); i++) keys.AddRange(MMenuKeys(rr)); for (int i = 0; i < 40; i++) keys.Add(27);
                CdSame("TakeMoneyMenu " + n, MSetup(n, 1 + n % 3, true), () => { Money.TakeMoneyMenu(); MEnd(); }, () => { OTakeMenu(); MEnd(); }, keys.ToArray());
                var gk = new List<byte>(); for (int i = 0; i < rr.Next(0, 3); i++) gk.Add((byte)(rr.Next(2) == 0 ? 'x' : 'z')); byte[] ends = { (byte)'Y', (byte)'N', 13, 27, (byte)'y', (byte)'n' }; gk.Add(ends[rr.Next(ends.Length)]); gk.AddRange(new byte[] { 27, 27, (byte)'N', (byte)'Y' });
                bool ga = false, gb = false; string said = "Treasure" + n, ask = "Go back?";
                CdSame("GoBackForIt " + n, MSetup(n, 1, false), () => { TextRend.DisplayString(1, 5, 15, "UPPER TEXT"); ga = Money.GoBackForIt(said, ask); cdLog.Add("ret:" + ga); }, () => { TextRend.DisplayString(1, 5, 15, "UPPER TEXT"); gb = OGoBack(said, ask); cdLog.Add("ret:" + gb); }, gk.ToArray());
            }
        }
        finally { MClean(); }
    }

    static void MoneyAppraise()
    {
        try
        {
            for (int n = 0; n < 1200; n++)
            {
                var rr = new Random(n * 17 + 3); int cnt = 1 + n % 4; int gems = rr.Next(3) == 0 ? 0 : rr.Next(1, 6), jwl = rr.Next(3) == 0 ? 0 : rr.Next(1, 6);
                var keys = new List<byte>(); int ops = rr.Next(1, 14); byte[] pool = { (byte)'G', (byte)'J', (byte)'S', (byte)'K', (byte)'G', (byte)'J', (byte)'S', (byte)'x', 13, 27 };
                for (int i = 0; i < ops; i++) keys.Add(pool[rr.Next(pool.Length)]); for (int i = 0; i < 30; i++) { keys.Add((byte)'S'); keys.Add((byte)'E'); }
                Func<Random, Types.TCharRecord, int> extra = (r, head) =>
                {
                    GDVars.curChar.coins[5] = (ushort)gems; GDVars.curChar.coins[6] = (ushort)jwl;
                    if (r.Next(3) == 0) { var c = GDVars.curChar; Types.TItemRec t = null; int nItems = r.Next(2) == 0 ? 15 : 16; for (int i = 0; i < nItems; i++) { var it = new Types.TItemRec { name = "F" + i, inUse = 1, itemType = 2, weight = 1 }; if (t == null) c.itemsAddr = it; else t.next = it; t = it; } }
                    if (r.Next(3) == 0) GDVars.curChar.coins[4] = 60000;
                    for (var c = GDVars.creatureList; c != null; c = c.nextCharAddr) CharHlp.RecalcStats(c);
                    return 0;
                };
                bool oa = false, ob = false;
                CdSame("AppraiseMenu " + n + " gems=" + gems + " jwl=" + jwl, MSetup(n, cnt, true, extra), () => { Money.AppraiseMenu(ref oa); cdLog.Add("ok:" + oa); MEnd(); }, () => { OAppraise(ref ob); cdLog.Add("ok:" + ob); MEnd(); }, keys.ToArray());
            }
        }
        finally { MClean(); }
    }
}

static partial class Tests
{
    static void MoneyShareEdge()
    {
        try
        {
            int hits = 0;
            for (int n = 0; n < 40000; n++)
            {
                int cnt = 2 + n % 3;
                Action setup = () =>
                {
                    MBase(); var r = new Random(n * 7 + 5); var head = MParty(r, cnt, false);
                    for (var c = head; c != null; c = c.nextCharAddr) { c.strCur = 3; c.strExc = 0; for (int t = 0; t < 7; t++) c.coins[t] = (ushort)(r.Next(2) == 0 ? 0 : r.Next(0, 60)); c.itemsAddr = null; c.numberOfItems = 0; CharHlp.RecalcStats(c); }
                    MPool(r); for (int t = 0; t < 7; t++) GDVars.treasureMoney[t] = r.Next(3) == 0 ? 0 : r.Next(0, 4000);
                    GDVars.curChar = head;
                };
                setup(); OHitEq = 0; OShare(); if (OHitEq == 0) continue; hits++;
                CdSame("ShareMoney edge " + n, setup, () => { Money.ShareMoney(); MEnd(); }, () => { OShare(); MEnd(); });
            }
            Yes(hits >= 3, "equal-leftover cases found: " + hits);
        }
        finally { MClean(); }
    }
}
