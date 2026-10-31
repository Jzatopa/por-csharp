using System;
using static Por.Core.Types;
namespace Por.Core
{
    // Explicit, idempotent integration. Unrelated combat services remain delegated to their prior owner.
    public static class CbtMapBindings
    {
        // Optional read-only host diagnostics; null in normal runtime. No input or state override.
        public static Action<string> ManualActionObserved;
        public static void Bind()
        {
            MagicBindings.BindCombatMap();
            if(CbtMap.boltTracer==null)MagicBindings.InstallLightningTracer();
            var map=new MapAdapter();CharDisp.CombatMap=map;CbtActs.CombatMap=map;
            if(!(Combat.Services is CombatAdapter))Combat.Services=new CombatAdapter(Combat.Services);
        }
        internal static void BindEffects()
        {
            if(!(Combat.Services is CombatAdapter))Bind();
            ((CombatAdapter)Combat.Services).EffectsBound=true;
        }
        public static void BindGameplay()
        {
            Bind();((CombatAdapter)Combat.Services).GameplayBound=true;
        }
        private sealed class MapAdapter:ICbtActsCombatMap
        {
            public void SetCombatPalette(){CbtMap.SetCombatPalette();}
            public bool ViewCoordsValid(sbyte x,sbyte y){return CbtMap.ViewCoordsValid(x,y);}
            public bool CombatIconVisible(TCharRecord c,byte whole){return CbtMap.CombatIconVisible(c,whole);}
            public void ScrollView(sbyte x,sbyte y,byte delta,byte dir){CbtMap.ScrollView(x,y,delta,dir);}
            public sbyte CombatCellX(TCharRecord c){return CbtMap.CombatCellX(c);}
            public sbyte CombatCellY(TCharRecord c){return CbtMap.CombatCellY(c);}
            public byte CombatSlot(TCharRecord c){return CbtMap.CombatSlot(c);}
            public void RedrawArenaCell(sbyte x,sbyte y){CbtMap.RedrawArenaCell(x,y);}
            public void BuildArenaArrays(){CbtMap.BuildArenaArrays();}
            public void GetArenaContents(sbyte x,sbyte y,ref byte c,ref byte t){CbtMap.GetArenaContents(x,y,out c,out t);}
            public void UpdateCombatMapTile(sbyte x,sbyte y,byte c){CbtMap.UpdateCombatMapTile(x,y,c);}
            public void SetCreatureFacing(TCharRecord c,byte dir,byte frame,byte erase){CbtMap.SetCreatureFacing(c,dir,frame,erase);}
            public byte GetIconSize(TCharRecord c){return CbtMap.GetIconSize(c);}
            public void CreatureDowned(TCharRecord c){CbtMap.CreatureDowned(c);}
            public void DisplayMapViewCentered(TCharRecord c,byte delta,bool cursor){CbtMap.DisplayMapViewCentered(c,delta,cursor);}
            public void StartSpellPreview(TAimShape shape,byte radius,byte art){CbtMap.StartSpellPreview(shape,radius,art);}
            public void StopSpellPreview(){CbtMap.StopSpellPreview();}
            public byte CountTargets(TCharRecord c,byte range){return CbtMap.CountTargets(c,range);}
            public byte CalcDistance(TCharRecord c,TCharRecord other){return CbtMap.CalcDistance(c,other);}
        }
        private sealed class CombatAdapter:ICombatServices
        {
            private readonly ICombatServices previous;
            internal bool EffectsBound,GameplayBound;
            public CombatAdapter(ICombatServices old){previous=old;}
            private ICombatServices Other{get{if(previous==null)throw new NotSupportedException("Combat: non-map gameplay services are not bound.");return previous;}}
            public void SetNoncombatPalette(){CbtMap.SetNoncombatPalette();}
            public void DisplayMapCentered(TCharRecord c,byte margin,bool cursor){CbtMap.DisplayMapViewCentered(c,margin,cursor);}
            public void RedrawCell(sbyte x,sbyte y){CbtMap.RedrawArenaCell(x,y);}
            public sbyte CellX(TCharRecord c){return CbtMap.CombatCellX(c);}
            public sbyte CellY(TCharRecord c){return CbtMap.CombatCellY(c);}
            public byte Slot(TCharRecord c){return CbtMap.CombatSlot(c);}
            public void ScrollView(sbyte x,sbyte y,byte margin,byte direction){CbtMap.ScrollView(x,y,margin,direction);}
            public void UpdateTile(sbyte x,sbyte y,byte slot){CbtMap.UpdateCombatMapTile(x,y,slot);}
            public bool IsValidCreatureLoc(TCharRecord c,sbyte x,sbyte y,bool check){return CbtMap.IsValidCreatureLoc(c,unchecked((byte)x),unchecked((byte)y),check);}
            public void SetFacing(TCharRecord c,byte dir,byte a,byte b){CbtMap.SetCreatureFacing(c,dir,a,b);}
            public void ProbeDirection(TCharRecord c,byte dir,out byte creature,out byte terrain){CbtMap.ProbeDirection(c,dir,out creature,out terrain);}
            public TCastUI CombatTargetMenu{get{return GameplayBound?(TCastUI)CbtActs.CombatCastTargetMenu:Other.CombatTargetMenu;}}
            public TCastUI NoncombatTargetMenu{get{return GameplayBound?(TCastUI)Magic.NoncombatCastTargetMenu:Other.NoncombatTargetMenu;}}
            public void CombatStart(){if(GameplayBound)CombatSt.CombatStart();else Other.CombatStart();}
            public void RoundStart(TCharRecord c){if(GameplayBound)CbtActs.CombatRoundStartSetup(c);else Other.RoundStart(c);}
            public void TriggerEffects(byte condition,TCharRecord c){if(EffectsBound)FxEngine.TriggerEffectsWithCondition(condition,c);else Other.TriggerEffects(condition,c);}
            public void ConsiderStinkingCloud(TCharRecord c){if(EffectsBound)FxEngine.ConsiderStinkingCloud(c);else Other.ConsiderStinkingCloud(c);}
            public void AdvanceTime(byte amount,sbyte unit){if(EffectsBound)FxEngine.AdvanceTime(amount,unit);else Other.AdvanceTime(amount,unit);}
            public void CalcHostileHpPercent(){if(GameplayBound)CbtActs.CalcHostileHpPercent();else Other.CalcHostileHpPercent();}
            public void AiControl(TCharRecord c){if(GameplayBound)CombatAi.CombatAiControl(c);else Other.AiControl(c);}
            public void ToggleQuickMagic(){if(GameplayBound)CombatAi.ToggleQuickMagic();else Other.ToggleQuickMagic();}
            public void AllToManual(){if(GameplayBound)CombatAi.AllToManual();else Other.AllToManual();}
            public void CastSpellNC(byte spell,bool a,bool b,ref bool done){if(GameplayBound)Magic.CastSpellNC(spell,a,b,ref done);else Other.CastSpellNC(spell,a,b,ref done);if(ManualActionObserved!=null&&GDVars.curChar!=null&&GDVars.curChar.quickfight==0)ManualActionObserved.Invoke("manual-cast-resolved");}
            public void ViewCharacter(ref bool done){if(GameplayBound)ViewChar.ViewCharMenu(ref done);else Other.ViewCharacter(ref done);}
            public void UseItem(ref bool done){if(GameplayBound)ViewChar.UseItemMenu(ref done);else Other.UseItem(ref done);}
            public void SetCurAttacksX2(TCharRecord c){if(GameplayBound)CbtActs.SetCurAttacksX2(c);else Other.SetCurAttacksX2(c);}
            public void AimMenu(TCharRecord c,byte spell,byte mode,byte range,ref bool done,ref object target){if(GameplayBound){TAimTarget aim=target as TAimTarget;CbtActs.CombatAimMenu(c,spell,mode,range,ref done,ref aim);target=aim;ManualActionObserved?.Invoke("manual-aim-resolved");}else Other.AimMenu(c,spell,mode,range,ref done,ref target);}
            public void CastSpellCombat(byte spell,byte item,ref bool done){if(GameplayBound)CbtActs.CastSpellCombat(spell,item,ref done);else Other.CastSpellCombat(spell,item,ref done);ManualActionObserved?.Invoke("manual-cast-selected");}
            public void TurnUndead(TCharRecord c){if(GameplayBound)CbtActs.CombatTurnUndead(c);else Other.TurnUndead(c);ManualActionObserved?.Invoke("manual-turn-resolved");}
            public byte TurnLevel(TCharRecord c){return GameplayBound?unchecked((byte)CbtActs.TurnLevel(c)):Other.TurnLevel(c);}
            public bool StingVictoryCheat(){return GameplayBound?CbtActs.StingVictoryCheat():Other.StingVictoryCheat();}
            public bool FleeAttempt(TCharRecord c){return GameplayBound?CbtActs.CombatFleeAttempt(c):Other.FleeAttempt(c);}
            public void MoveOpportunityAttacks(TCharRecord c,byte direction){if(GameplayBound)CbtActs.MoveOpportunityAttacks(c,direction);else Other.MoveOpportunityAttacks(c,direction);}
            public void MoveCreature(TCharRecord c,byte direction){if(GameplayBound)CbtActs.CombatMoveCreature(c,direction);else Other.MoveCreature(c,direction);}
            public bool CheckAttackAlly(TCharRecord a,TCharRecord d){return GameplayBound?CbtActs.CheckAttackAlly(a,d):Other.CheckAttackAlly(a,d);}
            public bool SweepAttempt(TCharRecord a,TCharRecord d){return GameplayBound?CbtActs.SweepAttempt(a,d):Other.SweepAttempt(a,d);}
            public void NoteAttackDirection(TCharRecord a,TCharRecord d){if(GameplayBound)CbtActs.NoteAttackDirection(a,d);else Other.NoteAttackDirection(a,d);}
            public void CombatAttack(TCharRecord a,TCharRecord d,bool free,object missile,ref bool done){if(GameplayBound)CbtActs.CombatAttack(a,d,free,(TItemRec)missile,ref done);else Other.CombatAttack(a,d,free,missile,ref done);}
        }
    }
}
