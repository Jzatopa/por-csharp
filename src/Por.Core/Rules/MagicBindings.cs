// Explicit integration for the Magic port (src/magic.pas). Binds Magic's CbtMap seam to the CbtMap port, offers Magic to
// CbtActs through ICbtActsMagic, and names the spell handlers GameInit.InitSpellFns asks for. FxEngine is not ported, so
// Magic.Effects stays unbound until a host installs it.
using System;
using static Por.Core.Types;

namespace Por.Core
{
    public static class MagicBindings
    {
        public static void BindCombatMap() { Magic.CombatMap = new MapAdapter(); }
        public static void BindCbtActs() { CbtActs.MagicUnit = new CbtActsAdapter(); }
        public static void InstallLightningTracer() { CbtMap.boltTracer = Magic.TraceLightningBolt; }

        /// <summary>The spell handler GameInit names (e.g. "BlessHandler"), or null for a name magic.pas does not define.</summary>
        public static TSpellProc SpellHandler(string name)
        {
            switch (name)
            {
                case "BlessHandler": return Magic.BlessHandler;
                case "CurseHandler": return Magic.CurseHandler;
                case "CureLightWoundsHandler": return Magic.CureLightWoundsHandler;
                case "CauseLightWoundsHandler": return Magic.CauseLightWoundsHandler;
                case "DetectionHandler": return Magic.DetectionHandler;
                case "ProtectionHandler": return Magic.ProtectionHandler;
                case "ResistColdHandler": return Magic.ResistColdHandler;
                case "BurningHandsHandler": return Magic.BurningHandsHandler;
                case "CharmPersonHandler": return Magic.CharmPersonHandler;
                case "EnlargeHandler": return Magic.EnlargeHandler;
                case "ReduceHandler": return Magic.ReduceHandler;
                case "FriendsHandler": return Magic.FriendsHandler;
                case "MagicMissileHandler": return Magic.MagicMissileHandler;
                case "ShieldHandler": return Magic.ShieldHandler;
                case "ShockingGraspHandler": return Magic.ShockingGraspHandler;
                case "SleepHandler": return Magic.SleepHandler;
                case "HoldPersonHandler": return Magic.HoldPersonHandler;
                case "ResistFireHandler": return Magic.ResistFireHandler;
                case "SilenceHandler": return Magic.SilenceHandler;
                case "SlowPoisonHandler": return Magic.SlowPoisonHandler;
                case "SnakeCharmHandler": return Magic.SnakeCharmHandler;
                case "SpiritualHammerHandler": return Magic.SpiritualHammerHandler;
                case "InvisibilityHandler": return Magic.InvisibilityHandler;
                case "KnockHandler": return Magic.KnockHandler;
                case "MirrorImageHandler": return Magic.MirrorImageHandler;
                case "RayOfEnfeeblementHandler": return Magic.RayOfEnfeeblementHandler;
                case "StinkingCloudHandler": return Magic.StinkingCloudHandler;
                case "StrengthHandler": return Magic.StrengthHandler;
                case "AnimateDeadHandler": return Magic.AnimateDeadHandler;
                case "CureBlindnessHandler": return Magic.CureBlindnessHandler;
                case "CauseBlindnessHandler": return Magic.CauseBlindnessHandler;
                case "CureDiseaseHandler": return Magic.CureDiseaseHandler;
                case "CauseDiseaseHandler": return Magic.CauseDiseaseHandler;
                case "DispelMagicHandler": return Magic.DispelMagicHandler;
                case "PrayerHandler": return Magic.PrayerHandler;
                case "RemoveCurseSpell": return Magic.RemoveCurseSpell;
                case "BestowCurseHandler": return Magic.BestowCurseHandler;
                case "BlinkHandler": return Magic.BlinkHandler;
                case "FireballHandler": return Magic.FireballHandler;
                case "HasteHandler": return Magic.HasteHandler;
                case "LightningBoltHandler": return Magic.LightningBoltHandler;
                case "SlowHandler": return Magic.SlowHandler;
                case "RestorationHandler": return Magic.RestorationHandler;
                case "PotionOfSpeedHandler": return Magic.PotionOfSpeedHandler;
                case "KeoghtomsOintmentHandler": return Magic.KeoghtomsOintmentHandler;
                case "GiantStrengthHandler": return Magic.GiantStrengthHandler;
                case "JavelinOfLightningHandler": return Magic.JavelinOfLightningHandler;
                case "WandParalyzationHandler": return Magic.WandParalyzationHandler;
                case "PotionOfHealingHandler": return Magic.PotionOfHealingHandler;
                case "DustDisappearanceHandler": return Magic.DustDisappearanceHandler;
                case "WandMagicMissilesHandler": return Magic.WandMagicMissilesHandler;
                case "UnusedSpellHandler": return Magic.UnusedSpellHandler;
                case "ManualBodilyHealthHandler": return Magic.ManualBodilyHealthHandler;
                default: return null;
            }
        }

        private sealed class MapAdapter : IMagicCombatMap
        {
            public sbyte CombatCellX(TCharRecord c) { return CbtMap.CombatCellX(c); }
            public sbyte CombatCellY(TCharRecord c) { return CbtMap.CombatCellY(c); }
            public void SetCreatureFacing(TCharRecord c, byte dir, byte frame, byte erase) { CbtMap.SetCreatureFacing(c, dir, frame, erase); }
            public bool CombatIconVisible(TCharRecord c, byte whole) { return CbtMap.CombatIconVisible(c, whole); }
            public byte CountTargets(TCharRecord c, byte range) { return CbtMap.CountTargets(c, range); }
            public void GetArenaContents(sbyte x, sbyte y, ref byte crIdx, ref byte terr) { CbtMap.GetArenaContents(x, y, out crIdx, out terr); }
            public bool IsValidCreatureLoc(TCharRecord c, byte x, byte y, bool moving) { return CbtMap.IsValidCreatureLoc(c, x, y, moving); }
            public void ScrollView(sbyte x, sbyte y, byte delta, byte dir) { CbtMap.ScrollView(x, y, delta, dir); }
        }

        // CbtActs holds Pascal's Integer idx as int; Magic keeps it 16-bit.
        private sealed class CbtActsAdapter : ICbtActsMagic
        {
            public byte CastSpellMenu(ref int idx, ref bool notEmpty, ref TItemRec scroll)
            {
                short i = unchecked((short)idx);
                byte r = Magic.CastSpellMenu(ref i, ref notEmpty, ref scroll);
                idx = i;
                return r;
            }
            public void UseItemSpell(TItemRec item, byte spell, bool announce, ref bool done) { Magic.UseItemSpell(item, spell, announce, ref done); }
            public void CastSpellNC(byte spell, bool b, bool ai, ref bool sel) { Magic.CastSpellNC(spell, b, ai, ref sel); }
        }
    }
}
