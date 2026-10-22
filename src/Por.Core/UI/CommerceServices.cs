// Required boundaries for the still-unconverted ViewChar/PostCbt units; shared support, not another source unit.
using System;
namespace Por.Core
{
    public static class CommerceServices
    {
        public delegate void ViewProc(ref bool exit);
        public static ViewProc ViewCharacter;
        public static Func<Types.TCharRecord, Types.TItemRec, bool> CannotCarryItem;
        public static Action<bool, bool> LootTake;
        internal static void View(ref bool flag) { if (ViewCharacter == null) throw new NotSupportedException("ViewChar.ViewCharMenu not bound."); ViewCharacter(ref flag); }
        internal static bool CannotCarry(Types.TCharRecord c, Types.TItemRec i) { if (CannotCarryItem == null) throw new NotSupportedException("ViewChar.CannotCarryItem not bound."); return CannotCarryItem(c, i); }
        internal static void Take(bool money, bool items) { if (LootTake == null) throw new NotSupportedException("PostCbt.LootTakeMenu not bound."); LootTake(money, items); }
    }
}
