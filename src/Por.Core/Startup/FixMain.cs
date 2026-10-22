// Direct conversion of pinned src/fixmain.pas. No executable/desktop completion is implied.
using System;
namespace Por.Core
{
    public interface IFixMainServices
    {
        void GameInit2();
        void MainMenu();
        bool QuickLoadAsked { get; set; }
        void QuickLoad();
        void FreeParty();
        void ResetGameState();
    }
    public static class FixMain
    {
        public static IFixMainServices Services;
        // Optional runtime observability; does not replace any game routine.
        public static Action<string> RuntimeStage;
        internal static Action BuildDataFiles = DataBld.BuildDataFiles;
        internal static Action Introduction = Intro.Introduction;
        internal static Action GameLoop = EclVm.GameLoop;
        internal static Action ForgetVault = Vault.ForgetSessionVault;
        internal static Action SetViewColors = () => View3D.SetView3DColors(11,0,7,6);
        internal static void RunBody()
        {
            IFixMainServices s = Services;
            if (s == null) throw new NotSupportedException("FixMain: GameInit/CharGen/Explore/SaveLoad services not bound.");
            RuntimeStage?.Invoke("init"); s.GameInit2(); RuntimeStage?.Invoke("data"); BuildDataFiles();
            if (!GDVars.stingArg)
            {
                RuntimeStage?.Invoke("intro"); Introduction();
                if (GameIo.Host == null) throw new NotSupportedException("FixMain: input host not installed.");
                while (GameIo.Host.KeyPressed) GameIo.DiscardPendingKeys();
            }
            ForgetVault();
            while (true)
            {
                RuntimeStage?.Invoke("menu"); s.MainMenu(); SetViewColors(); RuntimeStage?.Invoke("game"); GameLoop();
                while (s.QuickLoadAsked) { s.QuickLoadAsked = false; s.QuickLoad(); GameLoop(); }
                s.FreeParty(); ForgetVault(); s.ResetGameState();
            }
        }
        // Managed application entry: platform selects video/sound/paths first. DOS overlays and EMS are replaced by CLR loading, not simulated.
        public static void RunManaged() { RunManaged(null); }
        // verifyBindings runs after the converted-unit bindings are installed and before the game body (StartupSeamCheck.Verify).
        public static void RunManaged(Action verifyBindings)
        {
            try { TenDBindings.BindUnits(); if (verifyBindings != null) verifyBindings(); RunBody(); }
            finally { try { SndSb.ExitIfInstalled(); } finally { if (SoundDrv.Host != null) SoundDrv.SoundExit(); } }
        }
        public static void Run(string firstArgument) { Run(firstArgument, ""); }
        public static void Run(string firstArgument, string secondArgument)
        {
            // The source's initialization section runs GameBoot before the program body.
            try { GameBoot.Initialize(firstArgument); TenDBindings.SecondArgument = secondArgument; TenDBindings.BindUnits(); RunBody(); }
            finally { try { SndSb.ExitIfInstalled(); } finally { if (SoundDrv.Host != null) SoundDrv.SoundExit(); } }
        }
    }
}
