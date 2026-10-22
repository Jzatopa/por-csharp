// Startup verification: after the application hosts and the converted-unit bindings are installed, and before the game
// loop, name every required seam that is still null. A missing seam fails at startup with its name instead of crashing
// later when play first reaches it.
using System;
using System.Collections.Generic;
namespace Por.Core
{
    public static class StartupSeamCheck
    {
        public sealed class Seam
        {
            public readonly string Name; public readonly string Owner; internal readonly Func<bool> IsBound;
            internal Seam(string name, string owner, Func<bool> isBound) { Name = name; Owner = owner; IsBound = isBound; }
        }

        private static Seam S(string name, string owner, Func<bool> isBound) { return new Seam(name, owner, isBound); }

        // Owner says who must install the seam: "host" is the application (Por.Desktop Program), the others are binding classes.
        public static readonly Seam[] Required =
        {
            S("GameIo.Host", "host", () => GameIo.Host != null),
            S("VideoHardware.Host", "host", () => VideoHardware.Host != null),
            S("Blit.Interrupt", "host", () => Blit.Interrupt != null),
            S("DaxIo.Platform", "host", () => DaxIo.Platform != null),
            S("GameInit.Services", "host", () => GameInit.Services != null),
            S("Options.Info", "host", () => Options.Info != null),
            S("DataBld.Platform", "host", () => DataBld.Platform != null),
            S("TextRend.ReadBiosTicks", "host", () => TextRend.ReadBiosTicks != null),
            S("TextRend.DelayMilliseconds", "host", () => TextRend.DelayMilliseconds != null),
            S("BldShow.MaxAvailable", "host", () => BldShow.MaxAvailable != null),
            S("Portrait.MaxAvailable", "host", () => Portrait.MaxAvailable != null),
            S("SoundDrv.Host", "host", () => SoundDrv.Host != null),
            S("GDVars.sound.Play", "host (SndSpk/SndNone.Install)", () => GDVars.sound != null && GDVars.sound.Play != null),
            S("GDVars.video.Start", "host (VidVga.Install)", () => GDVars.video != null && GDVars.video.Start != null),
            S("GDVars.video.ShowPic", "host (VidVga.Install)", () => GDVars.video != null && GDVars.video.ShowPic != null),
            S("GDVars.video.CellFill", "host (VidVga.Install)", () => GDVars.video != null && GDVars.video.CellFill != null),
            S("GDVars.video.FlushSpans", "host (VidVga.Install)", () => GDVars.video != null && GDVars.video.FlushSpans != null),
            S("FixMain.Services", "TenDBindings", () => FixMain.Services != null),
            S("TenDBindings.MainMenu", "CharGenBindings", () => TenDBindings.MainMenu != null),
            S("EclVm.Units", "EclBindings", () => EclVm.Units != null),
            S("EclVm.Units.EclVars", "EclBindings", () => EclVm.Units != null && EclVm.Units.EclVars != null),
            S("EclVm.Units.EclMenu", "EclBindings", () => EclVm.Units != null && EclVm.Units.EclMenu != null),
            S("EclVm.Units.SaveLoad", "TenDBindings", () => EclVm.Units != null && EclVm.Units.SaveLoad != null),
            S("EclVm.Units.CharDisp", "EasiestTenBindings", () => EclVm.Units != null && EclVm.Units.CharDisp != null),
            S("EclVm.Units.CharHlp", "EasiestTenBindings", () => EclVm.Units != null && EclVm.Units.CharHlp != null),
            S("EclVm.Units.FxEngine", "FxEngineBindings", () => EclVm.Units != null && EclVm.Units.FxEngine != null),
            S("EclVm.Units.Explore", "TenDBindings", () => EclVm.Units != null && EclVm.Units.Explore != null),
            S("EclVm.Units.Game", "TenDBindings", () => EclVm.Units != null && EclVm.Units.Game != null),
            S("EvtKind.EclVarsUnit", "EclBindings", () => EvtKind.EclVarsUnit != null),
            S("EclVars.diskHook", "EclBindings", () => EclVars.diskHook != null),
            S("EclVars.wallSetHook", "EclBindings", () => EclVars.wallSetHook != null),
            S("EclVars.RecalcStats", "EclBindings", () => EclVars.RecalcStats != null),
            S("EclMenu.Host", "FinalGameplayBindings", () => EclMenu.Host != null),
            S("GameTime.Services", "FinalGameplayBindings", () => GameTime.Services != null),
            S("Temple.Effects", "TempleBindings", () => Temple.Effects != null),
            S("Explore.Services", "FinalGameplayBindings", () => Explore.Services != null),
            S("PostCbt.Services", "FinalGameplayBindings", () => PostCbt.Services != null),
            S("Combat.Services", "CbtMapBindings", () => Combat.Services != null),
            S("CbtActs.CombatMap", "CbtMapBindings", () => CbtActs.CombatMap != null),
            S("CbtActs.Effects", "FxEngineBindings", () => CbtActs.Effects != null),
            S("CbtActs.MagicUnit", "MagicBindings", () => CbtActs.MagicUnit != null),
            S("Magic.Effects", "FxEngineBindings", () => Magic.Effects != null),
            S("Magic.CombatMap", "MagicBindings", () => Magic.CombatMap != null),
            S("EffHndlr.Effects", "FxEngineBindings", () => EffHndlr.Effects != null),
            S("EffHndlr.CombatMap", "EffHndlrBindings", () => EffHndlr.CombatMap != null),
            S("CharDisp.CombatMap", "CbtMapBindings", () => CharDisp.CombatMap != null),
            S("CbtMap.boltTracer", "MagicBindings", () => CbtMap.boltTracer != null),
            S("CharEdit.Services", "CharGenBindings", () => CharEdit.Services != null),
            S("CharGenView.Host", "ViewCharBindings", () => CharGenView.Host != null),
            S("CompTool.EditCharacter", "TenDBindings", () => CompTool.EditCharacter != null),
            S("HeapErr.SaveGame", "TenDBindings", () => HeapErr.SaveGame != null),
            S("CommerceServices.ViewCharacter", "ViewCharBindings", () => CommerceServices.ViewCharacter != null),
            S("CommerceServices.CannotCarryItem", "ViewCharBindings", () => CommerceServices.CannotCarryItem != null),
            S("CommerceServices.LootTake", "TenDBindings", () => CommerceServices.LootTake != null),
            S("MemShow (measurements or platform message)", "PlatformGapBindings", () => MemShow.UnavailableMessage != null
                || (MemShow.GetOverlayBuffer != null && MemShow.GetOverlayRetry != null && MemShow.GetFreeMemory != null && MemShow.GetLargestBlock != null)),
            S("CharacterRecordCodec.ResolveEquippedItem", "PlatformGapBindings", () => CharacterRecordCodec.ResolveEquippedItem != null),
        };

        public static List<string> Missing()
        {
            var missing = new List<string>();
            foreach (Seam s in Required) if (!s.IsBound()) missing.Add(s.Name + " (" + s.Owner + ")");
            return missing;
        }

        public static void Verify()
        {
            List<string> missing = Missing();
            if (missing.Count != 0)
                throw new InvalidOperationException("Startup check failed: required seams are not bound: " + string.Join(", ", missing.ToArray())
                    + ". The game would fail when play reached them.");
        }
    }
}
