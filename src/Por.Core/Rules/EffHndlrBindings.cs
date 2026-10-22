// Explicit integration for the EffHndlr port (src/effhndlr.pas). Names the effect handlers GameInit.InitSpellFns asks for
// (every effhndlr.pas handler, plus the three other units' handlers the same table installs) and shares the CbtMap seam
// CbtMapBindings installed for CbtActs. FxEngine is not ported, so EffHndlr.Effects stays unbound until a host installs it.
using System;
using static Por.Core.Types;

namespace Por.Core
{
    public static class EffHndlrBindings
    {
        /// <summary>Point EffHndlr's CbtMap seam at map, or at the map CbtMapBindings.Bind gave CbtActs.</summary>
        public static void BindCombatMap(ICbtActsCombatMap map = null)
        {
            if (map == null) map = CbtActs.CombatMap;
            if (map == null) throw new NotSupportedException("EffHndlrBindings: no CbtMap binding; call CbtMapBindings.Bind first or pass a map.");
            EffHndlr.CombatMap = map;
        }

        /// <summary>The effect handler GameInit names (e.g. "EffHndlr.EffBless"), or null for a name it does not know.</summary>
        public static TEffectHandler EffectHandler(string qualifiedName)
        {
            switch (qualifiedName)
            {
                case "CbtActs.BiteAndHold": return CbtActs.BiteAndHold;
                case "CbtActs.RearClawRake": return CbtActs.RearClawRake;
                case "EffHndlr.EffAcidSquirt": return EffHndlr.EffAcidSquirt;
                case "EffHndlr.EffAlignedWeapon": return EffHndlr.EffAlignedWeapon;
                case "EffHndlr.EffAlliedPrayer": return EffHndlr.EffAlliedPrayer;
                case "EffHndlr.EffAnhkhegAcid": return EffHndlr.EffAnhkhegAcid;
                case "EffHndlr.EffAnimateDead": return EffHndlr.EffAnimateDead;
                case "EffHndlr.EffBestowCurse": return EffHndlr.EffBestowCurse;
                case "EffHndlr.EffBiteHold": return EffHndlr.EffBiteHold;
                case "EffHndlr.EffBless": return EffHndlr.EffBless;
                case "EffHndlr.EffBlind": return EffHndlr.EffBlind;
                case "EffHndlr.EffBlinking": return EffHndlr.EffBlinking;
                case "EffHndlr.EffBloodAttach": return EffHndlr.EffBloodAttach;
                case "EffHndlr.EffBloodDrain": return EffHndlr.EffBloodDrain;
                case "EffHndlr.EffBoulder": return EffHndlr.EffBoulder;
                case "EffHndlr.EffBoulderDodge": return EffHndlr.EffBoulderDodge;
                case "EffHndlr.EffCamouflage": return EffHndlr.EffCamouflage;
                case "EffHndlr.EffCanFightOn": return EffHndlr.EffCanFightOn;
                case "EffHndlr.EffCastStink": return EffHndlr.EffCastStink;
                case "EffHndlr.EffCharm": return EffHndlr.EffCharm;
                case "EffHndlr.EffCharmGaze": return EffHndlr.EffCharmGaze;
                case "EffHndlr.EffConPsnSave": return EffHndlr.EffConPsnSave;
                case "EffHndlr.EffConRegen": return EffHndlr.EffConRegen;
                case "EffHndlr.EffConSpellSave": return EffHndlr.EffConSpellSave;
                case "EffHndlr.EffCurse": return EffHndlr.EffCurse;
                case "EffHndlr.EffDiseaseHp": return EffHndlr.EffDiseaseHp;
                case "EffHndlr.EffDiseaseStr": return EffHndlr.EffDiseaseStr;
                case "EffHndlr.EffDiseased": return EffHndlr.EffDiseased;
                case "EffHndlr.EffDisplaced": return EffHndlr.EffDisplaced;
                case "EffHndlr.EffDragonFear": return EffHndlr.EffDragonFear;
                case "EffHndlr.EffDrainOne": return EffHndlr.EffDrainOne;
                case "EffHndlr.EffDrainTwo": return EffHndlr.EffDrainTwo;
                case "EffHndlr.EffDriderPara": return EffHndlr.EffDriderPara;
                case "EffHndlr.EffDustDisappear": return EffHndlr.EffDustDisappear;
                case "EffHndlr.EffDwarfAcGiant": return EffHndlr.EffDwarfAcGiant;
                case "EffHndlr.EffDwarfThac0": return EffHndlr.EffDwarfThac0;
                case "EffHndlr.EffEfreetiFire": return EffHndlr.EffEfreetiFire;
                case "EffHndlr.EffElfResist": return EffHndlr.EffElfResist;
                case "EffHndlr.EffEnfeeble": return EffHndlr.EffEnfeeble;
                case "EffHndlr.EffExtraStr": return EffHndlr.EffExtraStr;
                case "EffHndlr.EffFightOn": return EffHndlr.EffFightOn;
                case "EffHndlr.EffFireTouch": return EffHndlr.EffFireTouch;
                case "EffHndlr.EffFlameTongue": return EffHndlr.EffFlameTongue;
                case "EffHndlr.EffFriends": return EffHndlr.EffFriends;
                case "EffHndlr.EffGaseous": return EffHndlr.EffGaseous;
                case "EffHndlr.EffGauntlets": return EffHndlr.EffGauntlets;
                case "EffHndlr.EffGazeBlock": return EffHndlr.EffGazeBlock;
                case "EffHndlr.EffGhoulPara": return EffHndlr.EffGhoulPara;
                case "EffHndlr.EffGnomeAcBug": return EffHndlr.EffGnomeAcBug;
                case "EffHndlr.EffGnomeThac0": return EffHndlr.EffGnomeThac0;
                case "EffHndlr.EffHalfBash": return EffHndlr.EffHalfBash;
                case "EffHndlr.EffHalfCold": return EffHndlr.EffHalfCold;
                case "EffHndlr.EffHalfElec": return EffHndlr.EffHalfElec;
                case "EffHndlr.EffHalfElfResist": return EffHndlr.EffHalfElfResist;
                case "EffHndlr.EffHalfFire": return EffHndlr.EffHalfFire;
                case "EffHndlr.EffHalfMagical": return EffHndlr.EffHalfMagical;
                case "EffHndlr.EffHalfPierce": return EffHndlr.EffHalfPierce;
                case "EffHndlr.EffHammerGone": return EffHndlr.EffHammerGone;
                case "EffHndlr.EffHasted": return EffHndlr.EffHasted;
                case "EffHndlr.EffHealWake": return EffHndlr.EffHealWake;
                case "EffHndlr.EffHelpless": return EffHndlr.EffHelpless;
                case "EffHndlr.EffHolyWater": return EffHndlr.EffHolyWater;
                case "EffHndlr.EffImmobile": return EffHndlr.EffImmobile;
                case "EffHndlr.EffImmuneMM": return EffHndlr.EffImmuneMM;
                case "EffHndlr.EffInvisible": return EffHndlr.EffInvisible;
                case "EffHndlr.EffKreenDodge": return EffHndlr.EffKreenDodge;
                case "EffHndlr.EffKreenPara": return EffHndlr.EffKreenPara;
                case "EffHndlr.EffMagicRes50": return EffHndlr.EffMagicRes50;
                case "EffHndlr.EffMagicRes85": return EffHndlr.EffMagicRes85;
                case "EffHndlr.EffMirrorImage": return EffHndlr.EffMirrorImage;
                case "EffHndlr.EffMummyFear": return EffHndlr.EffMummyFear;
                case "EffHndlr.EffMummyFire": return EffHndlr.EffMummyFire;
                case "EffHndlr.EffMummyRot": return EffHndlr.EffMummyRot;
                case "EffHndlr.EffNeedMagic": return EffHndlr.EffNeedMagic;
                case "EffHndlr.EffNeedSilver": return EffHndlr.EffNeedSilver;
                case "EffHndlr.EffNoCharmPoison": return EffHndlr.EffNoCharmPoison;
                case "EffHndlr.EffNoCold": return EffHndlr.EffNoCold;
                case "EffHndlr.EffNoFire": return EffHndlr.EffNoFire;
                case "EffHndlr.EffNoParaPoison": return EffHndlr.EffNoParaPoison;
                case "EffHndlr.EffNoParalysis": return EffHndlr.EffNoParalysis;
                case "EffHndlr.EffNoSleepCharm": return EffHndlr.EffNoSleepCharm;
                case "EffHndlr.EffNull": return EffHndlr.EffNull;
                case "EffHndlr.EffPetrifyGaze": return EffHndlr.EffPetrifyGaze;
                case "EffHndlr.EffPoisonHit0": return EffHndlr.EffPoisonHit0;
                case "EffHndlr.EffPoisonHit2": return EffHndlr.EffPoisonHit2;
                case "EffHndlr.EffPoisonHit4": return EffHndlr.EffPoisonHit4;
                case "EffHndlr.EffPoisonHitM2": return EffHndlr.EffPoisonHitM2;
                case "EffHndlr.EffPrayer": return EffHndlr.EffPrayer;
                case "EffHndlr.EffProtEvil": return EffHndlr.EffProtEvil;
                case "EffHndlr.EffProtGood": return EffHndlr.EffProtGood;
                case "EffHndlr.EffProtMissiles": return EffHndlr.EffProtMissiles;
                case "EffHndlr.EffReadyItem": return EffHndlr.EffReadyItem;
                case "EffHndlr.EffRegen3": return EffHndlr.EffRegen3;
                case "EffHndlr.EffResistCold": return EffHndlr.EffResistCold;
                case "EffHndlr.EffResistFire": return EffHndlr.EffResistFire;
                case "EffHndlr.EffRingFireRes": return EffHndlr.EffRingFireRes;
                case "EffHndlr.EffRingInvis": return EffHndlr.EffRingInvis;
                case "EffHndlr.EffRotNoHeal": return EffHndlr.EffRotNoHeal;
                case "EffHndlr.EffRotTouch": return EffHndlr.EffRotTouch;
                case "EffHndlr.EffShield": return EffHndlr.EffShield;
                case "EffHndlr.EffSilence": return EffHndlr.EffSilence;
                case "EffHndlr.EffSilverMagic": return EffHndlr.EffSilverMagic;
                case "EffHndlr.EffSlowPoisonEnd": return EffHndlr.EffSlowPoisonEnd;
                case "EffHndlr.EffSlowPoisonHit": return EffHndlr.EffSlowPoisonHit;
                case "EffHndlr.EffSlowed": return EffHndlr.EffSlowed;
                case "EffHndlr.EffSpiritHammer": return EffHndlr.EffSpiritHammer;
                case "EffHndlr.EffStinkCough": return EffHndlr.EffStinkCough;
                case "EffHndlr.EffTrainManual": return EffHndlr.EffTrainManual;
                case "EffHndlr.EffTrainStart": return EffHndlr.EffTrainStart;
                case "EffHndlr.EffTrollBurn": return EffHndlr.EffTrollBurn;
                case "EffHndlr.EffTrollHeal": return EffHndlr.EffTrollHeal;
                case "EffHndlr.EffTrollRegen": return EffHndlr.EffTrollRegen;
                case "EffHndlr.EffTrollReturn": return EffHndlr.EffTrollReturn;
                case "EffHndlr.EffUndeadSlayer": return EffHndlr.EffUndeadSlayer;
                case "Magic.DragonBreathHandler": return Magic.DragonBreathHandler;
                default: return null;
            }
        }
    }
}
