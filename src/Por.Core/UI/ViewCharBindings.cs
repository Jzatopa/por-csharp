// Integration helpers for the ViewChar conversion (src/viewchar.pas): not a source unit of its own.
using static Por.Core.Types;

namespace Por.Core
{
    public static class ViewCharBindings
    {
        // Install ViewChar.ViewCharMenu and ViewChar.CannotCarryItem as the commerce boundary Shop, Temple and VaultMnu
        // reach them through (CommerceServices). Nothing calls this yet; a host does so at start-up.
        public static void BindCommerce()
        {
            CommerceServices.ViewCharacter = ViewChar.ViewCharMenu;
            CommerceServices.CannotCarryItem = ViewChar.CannotCarryItem;
        }

        // Install ViewChar as CharGen's view service (CharGenView.Host). Nothing calls this yet either.
        public static void BindCharGen()
        {
            CharGenView.Host = new CharGenViewAdapter();
        }

        private sealed class CharGenViewAdapter : ICharGenView
        {
            public void DisplayChar(TCharRecord c) { ViewChar.DisplayChar(c); }
            public void PrintStat(sbyte ability, bool highlighted) { ViewChar.PrintStat(ability, highlighted); }
            public void PrintCombatStats() { ViewChar.PrintCombatStats(); }
            public void PrintMoney() { ViewChar.PrintMoney(); }
            public void ViewCharMenu(ref bool changed) { ViewChar.ViewCharMenu(ref changed); }
        }
    }
}
