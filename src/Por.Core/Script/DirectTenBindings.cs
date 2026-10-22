// Direct-ten integration: route converted game services without discarding other existing bindings.
using System;
namespace Por.Core
{
    public static class DirectTenBindings
    {
        public static void BindGameUnits()
        {
            if (EclVm.Units == null) EclVm.Units = new EclVmUnits();
            if (!(EclVm.Units.Game is Converted)) EclVm.Units.Game = new Converted(EclVm.Units.Game);
        }
        private sealed class Converted : IEclVmGameUnits
        {
            private readonly IEclVmGameUnits previous;
            public Converted(IEclVmGameUnits old) { previous = old; }
            public void ShopMainMenu() { Shop.ShopMainMenu(); }
            public void VaultMenu() { VaultMnu.VaultMenu(); }
            public void TempleMainMenu() { Temple.TempleMainMenu(); }
            public void PostCombat() { if (previous == null) throw new NotSupportedException("Game.PostCombat not bound."); previous.PostCombat(); }
            public void CombatMain() { if (previous == null) throw new NotSupportedException("Game.CombatMain not bound."); previous.CombatMain(); }
            public void CharGenMainMenu() { if (previous == null) throw new NotSupportedException("Game.CharGenMainMenu not bound."); previous.CharGenMainMenu(); }
            public void WinGame() { Ending.WinGame(); }
            public void YarashComplete(bool animate) { Ending.YarashComplete(animate); }
            public void CampMainMenu(ref bool interrupted) { if (previous == null) throw new NotSupportedException("Game.CampMainMenu not bound."); previous.CampMainMenu(ref interrupted); }
            public void DisplayMapView() { MapView.DisplayMapView(); }
            public void SoundCommand(ushort cmd) { SoundDrv.SoundCommand(cmd); }
        }
    }
}
