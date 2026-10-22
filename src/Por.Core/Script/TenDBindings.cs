// Explicit integration for the directly converted ten-D units; no unconverted gameplay services are fabricated.
using System;
namespace Por.Core
{
    public static class TenDBindings
    {
        public static Action MainMenu;
        public static string SecondArgument="";
        public static void BindUnits()
        {
            var currentGame = EclVm.Units == null ? null : EclVm.Units.Game;
            EasiestTenBindings.BindUnits();
            if(currentGame is GameAdapter) EclVm.Units.Game = currentGame;
            EclVm.Units.SaveLoad=new SaveAdapter();EclVm.Units.Explore=new ExploreAdapter();
            if(!(EclVm.Units.Game is GameAdapter))EclVm.Units.Game=new GameAdapter(EclVm.Units.Game);
            if(!(FixMain.Services is MainAdapter))FixMain.Services=new MainAdapter(FixMain.Services);
            FxEngineBindings.Bind();
            CharGenBindings.Bind();
            FinalGameplayBindings.Bind();
            ViewCharBindings.BindCommerce();ViewCharBindings.BindCharGen();
            CompTool.EditCharacter=CharEdit.EditMenu;HeapErr.SaveGame=SaveLoad.SaveGame;
            CommerceServices.LootTake=(money,items)=>PostCbt.LootTakeMenu(ref money,ref items);
        }
        private sealed class SaveAdapter:ISaveLoadUnit
        {
            public void RemoveCreature(bool suppress,bool free){SaveLoad.RemoveCreature(suppress,free);}public void CheckFileElseQuit(string name){SaveLoad.CheckFileElseQuit(name);}
        }
        private sealed class ExploreAdapter:IExploreUnit
        {
            public char MapMainMenu(){return Explore.MapMainMenu();}public void ResolveMovement(){Explore.ResolveMovement();}public bool QuickLoadAsked{get{return Explore.quickLoadAsked;}}
        }
        private sealed class MainAdapter:IFixMainServices
        {
            private readonly IFixMainServices old;public MainAdapter(IFixMainServices previous){old=previous;}
            public void GameInit2(){GameInit.GameInit2(SecondArgument);}public void MainMenu(){if(TenDBindings.MainMenu!=null)TenDBindings.MainMenu();else if(old!=null)old.MainMenu();else throw new NotSupportedException("CharGen.MainMenu not bound.");}
            public bool QuickLoadAsked{get{return Explore.quickLoadAsked;}set{Explore.quickLoadAsked=value;}}
            public void QuickLoad(){SaveLoad.QuickLoad();}public void FreeParty(){SaveLoad.FreeParty();}public void ResetGameState(){GameInit.ResetGameState();}
        }
        private sealed class GameAdapter:IEclVmGameUnits
        {
            private readonly IEclVmGameUnits old;public GameAdapter(IEclVmGameUnits previous){old=previous;}
            public void PostCombat(){PostCbt.PostCombat();}public void CombatMain(){Combat.CombatMain();}
            public void ShopMainMenu(){old.ShopMainMenu();}public void VaultMenu(){old.VaultMenu();}public void TempleMainMenu(){old.TempleMainMenu();}public void CharGenMainMenu(){CharGen.MainMenu();}
            public void WinGame(){old.WinGame();}public void YarashComplete(bool animate){old.YarashComplete(animate);}public void CampMainMenu(ref bool interrupted){Camp.CampMainMenu(ref interrupted);}public void DisplayMapView(){old.DisplayMapView();}public void SoundCommand(ushort cmd){old.SoundCommand(cmd);}
        }
    }
}
