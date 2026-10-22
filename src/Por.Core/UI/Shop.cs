// Direct conversion of pinned src/shop.pas (7616920); required ViewChar boundary in CommerceServices.
using System;
using static Por.Core.Types;
namespace Por.Core
{
    public static class Shop
    {
        // Real dependency defaults, replaceable for ordered-call verification.
        internal static Action DisplayInterface = CharDisp.DisplayNoncombatInterface;
        internal static Action<TCharRecord> DisplayParty = CharDisp.DisplayParty;
        internal static Action<string> BottomLine = CharDisp.PrintBottomLine;
        internal static Action<TItemRec,bool> ItemLabel = CharHlp.ItemLabel;
        internal static void ShopBuyItemSelection(ref byte c, ref TItemRec itemToBuy, ref TItemRec cursor, ref short idx)
        {
            for (TItemRec p = GDVars.treasureItems; p != null; p = p.next)
            {
                if (p.value == 0) p.value = 1;
                string price = p.value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                char[] line = new string(' ',30).ToCharArray();
                price.CopyTo(0,line,30-price.Length,price.Length);
                int len = Math.Min(p.name.Length,29-price.Length);
                p.name.CopyTo(0,line,0,len); p.name = new string(line);
            }
            Border.DrawWindow(1,1,38,22,0,15,"Shop");
            bool redraw = true;
            c = MenuCore.MenuVertical("Items: ","Buy",13,10,15,1,4,38,22,GDVars.treasureItems,true,ref redraw,ref idx,ref cursor);
            itemToBuy = cursor;
            for (TItemRec p = GDVars.treasureItems; p != null; p = p.next) ItemLabel(p,false);
        }
        public static void GiveItemCopyToCreature(TItemRec item, ref bool failed)
        {
            if (CommerceServices.CannotCarry(GDVars.curChar,item)) { BottomLine("OverLoaded"); failed = true; }
            else { failed = false; CharHlp.AppendItemCopy(ref GDVars.curChar.itemsAddr,item); CharHlp.RecalcStats(GDVars.curChar); }
        }
        internal static void ShopBuyMenu()
        {
            TItemRec item = null, cursor = GDVars.treasureItems; short idx = 0; bool done;
            do
            {
                byte c = 0; ShopBuyItemSelection(ref c,ref item,ref cursor,ref idx);
                done = c != 'B' && c != C_Keys.ENTER;
                if (!done)
                {
                    ushort price = item.value; byte payer = MoneyCal.WhoPays(GDVars.curChar,price);
                    if (payer == MoneyCal.PAY_NOBODY) BottomLine("Not enough money.");
                    else { bool failed = false; GiveItemCopyToCreature(item,ref failed); if (!failed) Money.PayBy(GDVars.curChar,payer,price); }
                }
            } while (!done);
        }
        public static void CounterMenu(bool atTemple, TMenuAction command)
        {
            GDVars.ifcState = C_Ifc.SHOP_TEMPLE; DisplayInterface(); DisplayParty(GDVars.curChar);
            Array.Clear(GDVars.treasureMoney.Data,0,GDVars.treasureMoney.Data.Length);
            string first = atTemple ? "Heal" : "Buy";
            if (!atTemple) for (TItemRec p = GDVars.treasureItems; p != null; p = p.next) ItemLabel(p,false);
            bool done = false, ext = false, apprOk = false;
            do
            {
                bool hasMoney = false, hasItems = false; Money.DetermineTreasure(ref hasMoney,ref hasItems);
                string menu = first + (hasMoney ? " View Take Pool Share Appraise Exit" : " View Pool Appraise Exit");
                byte c = MenuCore.MenuHorizontal("",menu,13,10,15,true,false,ref ext);
                switch (c)
                {
                    case (byte)'B': if (!atTemple && !ext) command(); break;
                    case (byte)'H': if (atTemple && !ext) command(); break;
                    case (byte)'V': CommerceServices.View(ref ext); break;
                    case (byte)'T': Money.TakeMoneyMenu(); break;
                    case (byte)'P': if (!ext) Money.PoolMoney(); break;
                    case (byte)'S': Money.ShareMoney(); break;
                    case (byte)'A': Money.AppraiseMenu(ref apprOk); break;
                    case (byte)'E':
                        Money.DetermineTreasure(ref hasMoney,ref hasItems);
                        done = !hasMoney || !Money.GoBackForIt(atTemple ?
                            "As you leave a priest says, \"Excuse me but you have left some money here\" " :
                            "As you Leave the Shopkeeper says, \"Excuse me but you have Left Some Money here.\"  ",
                            atTemple ? "Do you want to go back and retrieve your money?" : "Do you want to go back and get your Money?");
                        break;
                    case C_Keys.HOME: case C_Keys.END_KEY: Party.SelectPrevNextChar((char)c); break;
                }
                if (c == 'B' || c == 'T' || (c == 'A' && apprOk)) DisplayInterface();
                DisplayParty(GDVars.curChar);
            } while (!done);
        }
        public static void ShopMainMenu() { CounterMenu(false, ShopBuyMenu); }
    }
}
