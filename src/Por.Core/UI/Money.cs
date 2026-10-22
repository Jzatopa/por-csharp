// Source: Talonius pool-of-radiance pinned 7616920a (src/money.pas); Qwen3.8:27b initial draft, supervisor repaired.
using System;


namespace Por.Core
{
    public static class Money
    {
        private const byte Black = 0;
        private const byte LightGreen = 10;
        private const byte LightMagenta = 13;
        private const byte White = 15;

        private static int Random(int n)
        {
            if (Dice.RandomBelow == null)
                throw new InvalidOperationException();
            return Dice.RandomBelow(n);
        }

        internal static int CarryingCapacity(Types.TCharRecord p)
        {
            if (CharHlp.HasBagOfHolding(p))
                return AbilAdj.WeightAllowance(p) + 1500 + 5000;
            else
                return AbilAdj.WeightAllowance(p) + 1500;
        }

        internal static ushort SpareCapacity(Types.TCharRecord p)
        {
            int room = CarryingCapacity(p) - CharHlp.RawWeight(p);
            if (room < 0)
                room = 0;
            return unchecked((ushort)room);
        }

        public static bool CheckOverload(Types.TCharRecord p, int w, ref ushort takable)
        {
            takable = SpareCapacity(p);
            if (w > takable)
                return true;
            else
            {
                takable = 0;
                return false;
            }
        }

        public static void PayFromCreature(Types.TCharRecord creature, int gp)
        {
            Types.TMoney m = new Types.TMoney();
            MoneyCal.CoinsOf(creature, m);
            MoneyCal.MakeChange(MoneyCal.CopperValue(m) - gp * GDConsts.dsCoinValuationCopper[C_Money.GOLD], 65535, m);
            for (int i = C_Money.COPPER; i <= C_Money.PLATINUM; i++)
                creature.coins[i] = unchecked((ushort)m[i]);
            CharHlp.RecalcStats(creature);
        }

        public static void PayFromPool(int gp)
        {
            MoneyCal.MakeChange(MoneyCal.CopperValue(GDVars.treasureMoney) - gp * GDConsts.dsCoinValuationCopper[C_Money.GOLD], int.MaxValue, GDVars.treasureMoney);
        }

        internal static void GivePlatinum(Types.TCharRecord creature, ushort pp)
        {
            ushort takable = 0;
            if (CheckOverload(creature, pp, ref takable))
            {
                CharDisp.PrintBottomLine("Overloaded.  Money will be put in Pool.");
                creature.coins[C_Money.PLATINUM] = unchecked((ushort)(creature.coins[C_Money.PLATINUM] + takable));
                CharHlp.RecalcStats(creature);
                GDVars.treasureMoney[C_Money.PLATINUM] = pp + GDVars.treasureMoney[C_Money.PLATINUM] - takable;
            }
            else
            {
                creature.coins[C_Money.PLATINUM] = unchecked((ushort)(creature.coins[C_Money.PLATINUM] + pp));
                CharHlp.RecalcStats(creature);
            }
        }

        public static ushort InputNumberMax(ushort max, string query, byte color)
        {
            byte col;
            byte qLen;
            string maxStr;
            string s;
            byte ch;
            int v;
            ushort n;

            TextRend.ClearBottomLine();
            if (query.Length > 40) query = query.Substring(0, 40);   // String[40] value parameter truncates on entry
            TextRend.DisplayString(0, 24, color, query);
            qLen = unchecked((byte)query.Length);
            col = qLen;
            maxStr = max.ToString();
            s = "";

            do
            {
                ch = GameIo.ReadGameKey();
                if (ch >= '0' && ch <= '9')
                {
                    s = s + (char)ch;
                    if (s.Length > 6) s = s.Substring(0, 6);
                    v = int.Parse(s);
                    if (max >= v)
                        col = unchecked((byte)(col + 1));
                    else
                    {
                        s = maxStr;
                        col = unchecked((byte)(qLen + maxStr.Length));
                    }
                    TextRend.DisplayString(qLen, 24, White, s);
                }
                else if (ch == C_Keys.BACKSPACE)
                {
                    if (s.Length > 0)
                    {
                        s = s.Substring(0, s.Length - 1);
                        TextRend.PrintSpaces(col, 24, Black, 1);
                        col = unchecked((byte)(col - 1));
                    }
                }
            } while (ch != C_Keys.ENTER && ch != C_Keys.ESC);

            TextRend.ClearBottomLine();
            if (ch == C_Keys.ESC)
                n = 0;
            else
                n = s.Length == 0 ? (ushort)0 : unchecked((ushort)int.Parse(s));

            return n;
        }

        public static void TradeMoney(ref Types.TCharRecord giver, ref Types.TCharRecord rcv, ushort qty, sbyte mType)
        {
            if (qty <= SpareCapacity(rcv))
            {
                giver.coins[mType] = unchecked((ushort)(giver.coins[mType] - qty));
                CharHlp.RecalcStats(giver);
                rcv.coins[mType] = unchecked((ushort)(rcv.coins[mType] + qty));
                CharHlp.RecalcStats(rcv);
            }
            else
                CharDisp.PrintBottomLine("Overloaded");
        }

        public static void PoolMoney()
        {
            Types.TCharRecord p = GDVars.creatureList;
            while (p != null)
            {
                if (p.npc == 0 || p.npc == C_Npc.ANIMATED_PC)
                {
                    for (int i = C_Money.COPPER; i <= C_Money.JEWELRY; i++)
                    {
                        GDVars.treasureMoney[i] = p.coins[i] + GDVars.treasureMoney[i];
                    }
                    Array.Clear(p.coins, 0, p.coins.Length);
                    CharHlp.RecalcStats(p);
                }
                p = p.nextCharAddr;
            }
        }

        internal static byte CountSharers()
        {
            Types.TCharRecord p = GDVars.creatureList;
            byte n = 0;
            while (p != null)
            {
                if (!CharHlp.IsNpc(p))
                    n = unchecked((byte)(n + 1));
                p = p.nextCharAddr;
            }
            return n;
        }

        public static void ShareMoney()
        {
            Types.TCharRecord p = GDVars.creatureList;
            byte cnt = CountSharers();
            if (cnt == 0) return;

            int[] share = new int[7];
            int[] rem = new int[7];
            Array.Clear(share, 0, 7);
            Array.Clear(rem, 0, 7);

            for (int i = C_Money.COPPER; i <= C_Money.JEWELRY; i++)
            {
                if (GDVars.treasureMoney[i] > 0)
                {
                    share[i] = GDVars.treasureMoney[i] / cnt;
                    rem[i] = GDVars.treasureMoney[i] % cnt;
                }
                else
                {
                    share[i] = 0;
                    rem[i] = 0;
                }
            }

            // pass 1: shares
            p = GDVars.creatureList;
            while (p != null)
            {
                if (!CharHlp.IsNpc(p))
                {
                    for (int i = C_Money.JEWELRY; i >= C_Money.COPPER; i--)
                    {
                        ushort takable = 0;
                        if (!CheckOverload(p, share[i], ref takable))
                        {
                            p.coins[i] = unchecked((ushort)(share[i] + p.coins[i]));
                            CharHlp.RecalcStats(p);
                            if (rem[i] > 0)
                            {
                                if (!CheckOverload(p, rem[i], ref takable))
                                {
                                    p.coins[i] = unchecked((ushort)(p.coins[i] + 1));
                                    CharHlp.RecalcStats(p);
                                    rem[i]--;
                                }
                            }
                        }
                        else
                        {
                            p.coins[i] = unchecked((ushort)(p.coins[i] + takable));
                            rem[i] = rem[i] + share[i] - takable;
                            CharHlp.RecalcStats(p);
                        }
                    }
                }
                p = p.nextCharAddr;
            }

            // pass 2: leftovers
            for (int i = C_Money.JEWELRY; i >= C_Money.COPPER; i--)
            {
                if (rem[i] > 0)
                {
                    p = GDVars.creatureList;
                    while (p != null)
                    {
                        ushort takable = SpareCapacity(p);
                        if (takable > 0)
                        {
                            if (rem[i] > takable)
                            {
                                p.coins[i] = unchecked((ushort)(p.coins[i] + takable));
                                CharHlp.RecalcStats(p);
                                rem[i] = rem[i] - takable;
                            }
                            else
                            {
                                p.coins[i] = unchecked((ushort)(p.coins[i] + rem[i]));
                                CharHlp.RecalcStats(p);
                                rem[i] = 0;
                            }
                        }
                        p = p.nextCharAddr;
                    }
                }
            }

            // return the rest to the pool
            for (int i = C_Money.COPPER; i <= C_Money.JEWELRY; i++)
                GDVars.treasureMoney[i] = rem[i];
        }

        public static void DropMoney(Types.TCharRecord p, ushort qty, sbyte mType)
        {
            p.coins[mType] = unchecked((ushort)(p.coins[mType] - qty));
            CharHlp.RecalcStats(p);
            if (GDVars.ifcState == C_Ifc.LOOT || GDVars.ifcState == C_Ifc.SHOP_TEMPLE || GDVars.ifcState == C_Ifc.VAULT)
                GDVars.treasureMoney[mType] = GDVars.treasureMoney[mType] + qty;
        }

        internal static void TakeMoney(Types.TCharRecord p, ushort qty, sbyte mType)
        {
            ushort takable = 0;
            if (CheckOverload(p, qty, ref takable))
                CharDisp.PrintBottomLine("Overloaded");
            else
            {
                if (qty > GDVars.treasureMoney[mType])
                    qty = unchecked((ushort)GDVars.treasureMoney[mType]);
                GDVars.treasureMoney[mType] = GDVars.treasureMoney[mType] - qty;
                p.coins[mType] = unchecked((ushort)(p.coins[mType] + qty));
                CharHlp.RecalcStats(p);
            }
        }

        public static void TakeMoneyMenu()
        {
            int i;
            sbyte mType;
            Types.TItemRec p;
            Types.TItemRec sel;
            Types.TItemRec save;
            short idx;
            string numStr = "";
            ushort n;
            bool exitF;
            bool redraw;
            byte ch;

            Border.DrawWindow((sbyte)C_Msg.TEXT_LEFT, (sbyte)C_Msg.TEXT_TOP, (sbyte)C_Msg.TEXT_RIGHT, (sbyte)C_Msg.TEXT_BOTTOM, 0, LightGreen, "");
            idx = 0;
            do
            {
                redraw = true;
                p = null;
                sel = null;
                for (i = C_Money.JEWELRY; i >= C_Money.COPPER; i--)
                {
                    if (GDVars.treasureMoney[i] > 0)
                    {
                        if (p == null)
                        {
                            p = new Types.TItemRec();
                            p.next = null;
                        }
                        else
                        {
                            sel = p;
                            p = new Types.TItemRec();
                            p.next = sel;
                        }
                        numStr = GDVars.treasureMoney[i].ToString();
                        p.name = GDConsts.moneyNames[i] + " " + numStr;
                        p.inUse = 0;
                    }
                }
                sel = p;
                save = p;
                ch = MenuCore.MenuVertical("Select type of coin", "", LightMagenta, LightGreen, White, 2, 2, 15, 8, p, true, ref redraw, ref idx, ref sel);
                if (sel == null || ch == C_Keys.NONE)
                {
                    MenuCore.FreeStringList(ref save);
                    exitF = true;
                }
                else
                {
                    exitF = false;
                    mType = MoneyCal.ParseMoneyType(sel.name, ref numStr);
                    numStr = "How much " + numStr + "will you take? ";
                    if (GDVars.treasureMoney[mType] > 65535)
                        n = 65535;
                    else
                        n = unchecked((ushort)GDVars.treasureMoney[mType]);
                    n = InputNumberMax(n, numStr, LightGreen);
                    TakeMoney(GDVars.curChar, n, mType);
                    MenuCore.FreeStringList(ref save);
                    exitF = true;
                    for (i = C_Money.COPPER; i <= C_Money.JEWELRY; i++)
                    {
                        if (GDVars.treasureMoney[i] > 0)
                            exitF = false;
                    }
                }
            } while (!exitF);
        }

        public static void DetermineTreasure(ref bool treasure, ref bool items)
        {
            int i;
            treasure = false;
            items = false;
            for (i = C_Money.COPPER; i <= C_Money.JEWELRY; i++)
            {
                if (GDVars.treasureMoney[i] != 0)
                    treasure = true;
            }
            if (GDVars.treasureItems != null)
                items = true;
        }

        public static bool GoBackForIt(string said, string ask)
        {
            TextRend.ShowMessage(LightGreen, true, said);
            TextRend.ShowMessage(White, false, ask);
            if (EclMenu.MenuHorizontalCS("", "~Yes ~No", LightMagenta, LightGreen, White, false, false) == 1)
                return false;
            else
            {
                TextRend.ClearRect((sbyte)C_Msg.TEXT_LEFT, (sbyte)C_Msg.MESSAGE_TOP, (sbyte)C_Msg.TEXT_RIGHT, (sbyte)C_Msg.TEXT_BOTTOM);
                return true;
            }
        }

        public static void PayBy(Types.TCharRecord buyer, byte payer, int gp)
        {
            switch (payer)
            {
                case MoneyCal.PAY_CHARACTER:
                    PayFromCreature(buyer, gp);
                    break;
                case MoneyCal.PAY_POOL:
                    PayFromPool(gp);
                    break;
            }
        }

        public static bool PayGold(Types.TCharRecord buyer, int gp)
        {
            byte payer;
            payer = MoneyCal.WhoPays(buyer, gp);
            if (payer == MoneyCal.PAY_NOBODY)
                CharDisp.PrintBottomLine("Not enough money.");
            else
                PayBy(buyer, payer, gp);
            return payer != MoneyCal.PAY_NOBODY;
        }

        public static void AppraiseMenu(ref bool ok)
        {
            string gemStr = "";
            string jwlStr = "";
            byte ch = 0;
            string menu = "";
            string actionStr = "";
            byte r = 0;
            ushort value = 0;
            ushort takable = 0;
            Types.TItemRec gem = null;
            bool exitF = false;
            bool ext = false;

            bool HasRoom(Types.TCharRecord creature)
            {
                CharHlp.RecalcStats(creature);
                return (creature.numberOfItems < 16) && !CheckOverload(creature, 1, ref takable);
            }

            void SellOrKeep(byte nameId, ushort worth)
            {
                Types.TCharRecord holder = null;
                Types.TCharRecord c = null;

                if (HasRoom(GDVars.curChar))
                {
                    holder = GDVars.curChar;
                }
                else
                {
                    TextRend.DisplayString(1, 14, White, GDVars.curChar.name + " doesn't have space");
                    c = GDVars.creatureList;
                    while (c != null && holder == null)
                    {
                        if (c != GDVars.curChar && !CharHlp.IsNpc(c) && HasRoom(c))
                        {
                            holder = c;
                        }
                        c = c.nextCharAddr;
                    }
                    if (holder != null)
                    {
                        TextRend.DisplayString(1, 15, White, "Keep will give to " + holder.name);
                    }
                    else
                    {
                        TextRend.DisplayString(1, 15, White, "No one else has space either");
                    }
                }

                if (holder != null)
                {
                    actionStr = "Sell Keep";
                }
                else
                {
                    actionStr = "Sell";
                }

                ch = MenuCore.MenuHorizontal("You can: ", actionStr, LightMagenta, LightGreen, White, true, false, ref ext);

                if (ch == 'K' && holder != null)
                {
                    gem = new Types.TItemRec();
                    gem.next = null;
                    gem.equipped = 0;
                    gem.nameIds[3] = nameId;
                    gem.nameIds[2] = 0;
                    gem.nameIds[1] = 0;
                    gem.itemType = 70;
                    gem.value = worth;
                    gem.weight = 1;
                    gem.unidMask = 0;
                    CharHlp.AppendItemCopy(ref holder.itemsAddr, gem);
                    CharHlp.RecalcStats(holder);
                }
                else
                {
                    GivePlatinum(GDVars.curChar, unchecked((ushort)(worth / 5)));
                }
            }

            ok = true;
            if (GDVars.curChar.coins[C_Money.GEMS] == 0 && GDVars.curChar.coins[C_Money.JEWELRY] == 0)
            {
                CharDisp.PrintBottomLine("No Gems or Jewelry");
                ok = false;
            }
            else
            {
                do
                {
                    if (GDVars.curChar.coins[C_Money.GEMS] == 0 && GDVars.curChar.coins[C_Money.JEWELRY] == 0)
                    {
                        exitF = true;
                    }
                    else
                    {
                        exitF = false;
                        gemStr = GDVars.curChar.coins[C_Money.GEMS].ToString();
                        jwlStr = GDVars.curChar.coins[C_Money.JEWELRY].ToString();

                        if (GDVars.curChar.coins[C_Money.GEMS] == 0)
                        {
                            gemStr = "";
                        }
                        else if (GDVars.curChar.coins[C_Money.GEMS] == 1)
                        {
                            gemStr = gemStr + " Gem";
                        }
                        else
                        {
                            gemStr = gemStr + " Gems";
                        }

                        if (GDVars.curChar.coins[C_Money.JEWELRY] == 0)
                        {
                            jwlStr = "";
                        }
                        else if (GDVars.curChar.coins[C_Money.JEWELRY] == 1)
                        {
                            jwlStr = jwlStr + " piece of Jewelry";
                        }
                        else
                        {
                            jwlStr = jwlStr + " pieces of Jewelry";
                        }

                        TextRend.ClearRect((sbyte)C_Msg.TEXT_LEFT, (sbyte)C_Msg.TEXT_TOP, (sbyte)C_Msg.TEXT_RIGHT, (sbyte)C_Msg.TEXT_BOTTOM);
                        CharDisp.DisplayCreatureName(GDVars.curChar, 1, 1, false);
                        TextRend.DisplayString(1, 7, White, "You have a fine collection of:");
                        TextRend.DisplayString(1, 9, White, gemStr);
                        TextRend.DisplayString(1, 10, White, jwlStr);

                        menu = "";
                        if (GDVars.curChar.coins[C_Money.GEMS] != 0)
                        {
                            menu = "Gems ";
                        }
                        if (GDVars.curChar.coins[C_Money.JEWELRY] != 0)
                        {
                            menu = menu + "Jewelry ";
                        }
                        menu = menu + "Exit";

                        ch = MenuCore.MenuHorizontal("Appraise: ", menu, LightMagenta, LightGreen, White, true, false, ref ext);

                        if (ch == 'G')
                        {
                            if (GDVars.curChar.coins[C_Money.GEMS] != 0)
                            {
                                GDVars.curChar.coins[C_Money.GEMS] = unchecked((ushort)(GDVars.curChar.coins[C_Money.GEMS] - 1));
                                r = Dice.RollDice(1, 100);
                                if (r >= 1 && r <= 25)
                                {
                                    value = 10;
                                }
                                else if (r >= 26 && r <= 50)
                                {
                                    value = 50;
                                }
                                else if (r >= 51 && r <= 70)
                                {
                                    value = 100;
                                }
                                else if (r >= 71 && r <= 90)
                                {
                                    value = 500;
                                }
                                else if (r >= 91 && r <= 99)
                                {
                                    value = 1000;
                                }
                                else if (r == 100)
                                {
                                    value = 5000;
                                }
                                gemStr = value.ToString();
                                gemStr = "The Gem is Valued at " + gemStr + " gp.";
                                TextRend.DisplayString(1, 12, White, gemStr);
                                SellOrKeep(101, value);
                            }
                        }
                        else if (ch == 'J')
                        {
                            if (GDVars.curChar.coins[C_Money.JEWELRY] != 0)
                            {
                                GDVars.curChar.coins[C_Money.JEWELRY] = unchecked((ushort)(GDVars.curChar.coins[C_Money.JEWELRY] - 1));
                                r = Dice.RollDice(1, 100);
                                if (r >= 1 && r <= 10)
                                {
                                    value = unchecked((ushort)(Random(900) + 100));
                                }
                                else if (r >= 11 && r <= 20)
                                {
                                    value = unchecked((ushort)(Random(1000) + 200));
                                }
                                else if (r >= 21 && r <= 40)
                                {
                                    value = unchecked((ushort)(Random(1500) + 300));
                                }
                                else if (r >= 41 && r <= 50)
                                {
                                    value = unchecked((ushort)(Random(2500) + 500));
                                }
                                else if (r >= 51 && r <= 70)
                                {
                                    value = unchecked((ushort)(Random(5000) + 1000));
                                }
                                else if (r >= 71 && r <= 90)
                                {
                                    value = unchecked((ushort)(Random(6000) + 2000));
                                }
                                else if (r >= 91 && r <= 100)
                                {
                                    value = unchecked((ushort)(Random(10000) + 2000));
                                }
                                jwlStr = value.ToString();
                                gemStr = "The Jewel is Valued at " + jwlStr + " gp.";
                                TextRend.DisplayString(1, 12, White, gemStr);
                                SellOrKeep(214, value);
                            }
                        }
                        else if (ch == 'E' || ch == C_Keys.NONE)
                        {
                            exitF = true;
                        }

                        CharHlp.RecalcStats(GDVars.curChar);
                    }
                } while (!exitF);
            }
        }
    }
}
