// Direct conversion of pinned src/vaultmnu.pas (7616920).
using System;
namespace Por.Core
{
    public static class VaultMnu
    {
        internal static Action DisplayInterface = CharDisp.DisplayNoncombatInterface;
        internal static Action<Types.TCharRecord> DisplayParty = CharDisp.DisplayParty;
        public static void VaultMenu()
        {
            GDVars.ifcState = C_Ifc.VAULT; DisplayInterface(); DisplayParty(GDVars.curChar); Vault.ReadVault();
            TextRend.ShowMessage(10,true,Vault.WasVisited() && Dice.RandomBelow(20) == 0 ?
                "OH, YOU'RE ALIVE... I MEAN  WELCOME BACK!" : "WELCOME TO THE HOARDER'S RESPITE. LET ME LIGHTEN YOUR LOAD.");
            bool done = false, ext = false;
            do
            {
                bool money = false, items = false; Money.DetermineTreasure(ref money,ref items);
                string menu = "View" + (money || items ? " Take" : "") + " Pool" + (money ? " Share" : "") + " Exit";
                byte c = MenuCore.MenuHorizontal("",menu,13,10,15,true,false,ref ext);
                if (ext) { if (c == C_Keys.HOME || c == C_Keys.END_KEY) Party.SelectPrevNextChar((char)c); }
                else switch (c)
                {
                    case (byte)'V': CommerceServices.View(ref ext); break;
                    case (byte)'T': if (money || items) { CommerceServices.Take(money,items); DisplayInterface(); } break;
                    case (byte)'P': Money.PoolMoney(); break;
                    case (byte)'S': if (money) Money.ShareMoney(); break;
                    case (byte)'E': case C_Keys.NONE: done = true; break;
                }
                DisplayParty(GDVars.curChar);
            } while (!done);
            Vault.WriteVault();
        }
    }
}
