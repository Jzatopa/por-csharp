using static Por.Core.Types;
namespace Por.Core
{
    // Installs only converted services. Effect handlers remain explicitly supplied by EffHndlr/GameInit.
    public static class FxEngineBindings
    {
        public static void Bind()
        {
            var effects=new Adapter();Magic.Effects=effects;CbtActs.Effects=effects;EffHndlr.Effects=effects;
            if(EclVm.Units==null)EclVm.Units=new EclVmUnits();EclVm.Units.FxEngine=effects;
            CbtMapBindings.Bind();CbtMapBindings.BindEffects();
        }
        private sealed class Adapter:IMagicEffects,ICbtActsEffects,IFxEngineUnit,IEffHndlrEffects
        {
            public void StrengthEffectToStrength(TEffectRec e,ref byte b,ref byte x){FxEngine.StrengthEffectToStrength(e,ref b,ref x);}
            public byte StrengthModifier(byte b,byte x){return FxEngine.StrengthModifier(b,x);}
            public bool DiseaseImmune(TCharRecord c,byte id){return FxEngine.DiseaseImmune(c,id);}
            public void InflictItemShock(TCharRecord c,TItemRec i,byte d){FxEngine.InflictItemShock(c,i,d);}
            public void AddEffectWithMessage(TCharRecord c,byte id,ushort m,byte s,bool remove,bool negate,bool saved,string msg){FxEngine.AddEffectWithMessage(c,id,m,s,remove,negate,saved,msg);}
            public bool RollSavingThrow(TCharRecord c,sbyte type,sbyte modifier){return FxEngine.RollSavingThrow(c,type,modifier);}
            public void TriggerEffectsWithCondition(byte cond,TCharRecord c){FxEngine.TriggerEffectsWithCondition(cond,c);}
            public bool AttackRoll(TCharRecord a,TCharRecord d,byte ac){return FxEngine.AttackRoll(a,d,ac);}
            public bool AttackRollNoAttacker(TCharRecord d,byte thac0){return FxEngine.AttackRollNoAttacker(d,thac0);}
            public void AdvanceTime(byte amount,sbyte unit){FxEngine.AdvanceTime(amount,unit);}
            public void InflictNonWeaponDamage(TCharRecord c,byte damage,byte reduction,bool saved){FxEngine.InflictNonWeaponDamage(c,damage,reduction,saved);}
            public void AddEffectWithMessage(TCharRecord c,byte id,ushort minutes,byte strength,bool onRemove,byte saveNegates,bool saved,string msg){FxEngine.AddEffectWithMessage(c,id,minutes,strength,onRemove,saveNegates,saved,msg);}
            public void RemoveInvisibility(TCharRecord c){FxEngine.RemoveInvisibility(c);}
            public bool RestoreHitPoints(TCharRecord c,byte hp,bool bypass){return FxEngine.RestoreHitPoints(c,hp,bypass);}
            public void CallEffectHandler(byte id,TCharRecord c,object p,bool removed){FxEngine.CallEffectHandler(id,c,p,removed);}
            public bool AdjustForNewStrength(TCharRecord c,byte baseStr,byte excStr,ref byte strength){return FxEngine.AdjustForNewStrength(c,baseStr,excStr,ref strength);}
            public void AddEffect(TCharRecord c,byte id,ushort minutes,byte strength,bool onRemove){FxEngine.AddEffect(c,id,minutes,strength,onRemove);}
            public bool RemoveEffectWithMessage(TCharRecord c,byte id){return FxEngine.RemoveEffectWithMessage(c,id);}
            public bool ReviveDeadCreature(TCharRecord c,byte hp){return FxEngine.ReviveDeadCreature(c,hp);}
            public void ConsiderStinkingCloud(TCharRecord c){FxEngine.ConsiderStinkingCloud(c);}
            public void RemoveEffect(TCharRecord c,byte id,object effect){FxEngine.RemoveEffect(c,id,effect);}
            public void PutOutOfPlayWithStatus(TCharRecord c,byte status,string msg){FxEngine.PutOutOfPlayWithStatus(c,status,msg);}
            public void RemoveEffectsHp0(TCharRecord c){FxEngine.RemoveEffectsHp0(c);}
            public TCharRecord HoldTarget(TCharRecord c,object p,bool removed,byte id,ref bool released){return FxEngine.HoldTarget(c,p,removed,id,ref released);}
        }
    }
}
