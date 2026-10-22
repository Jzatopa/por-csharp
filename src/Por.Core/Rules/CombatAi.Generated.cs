// Pinned Pascal transcription; regenerate with scripts/translate-final-combat.py CombatAi
using System;using System.Linq;using static Por.Core.Types;
namespace Por.Core{public static partial class CombatAi{
private static byte lastMoveDir=default(byte);
private static byte stuckCount=default(byte);
private static bool quickShown=default(bool);
public static bool QuickLine(){
bool __result_QuickLine=default(bool);
TCharRecord p=null;
bool quick=default(bool);

{
quick=false;
p=GDVars.creatureList;
while((p!=null)){
if((((p.quickfight!=0))&&CharHlpComesUnderPlayerCall(p))){quick=true;}
p=p.nextCharAddr;
}
TextRendClearBottomLineCall();
__result_QuickLine=quick;
quickShown=quick;
if(!(quick)){return __result_QuickLine;}
TextRendDisplayStringCall(unchecked((byte)(0)),unchecked((byte)(24)),unchecked((byte)(13)),"Quick: ");
TextRendDisplayStringCall(unchecked((byte)(7)),unchecked((byte)(24)),unchecked((byte)(15)),"On (Space) ");
TextRendDisplayStringCall(unchecked((byte)(18)),unchecked((byte)(24)),unchecked((byte)(13)),"Magic: ");
if(GDVars.quickMagic){TextRendDisplayStringCall(unchecked((byte)(25)),unchecked((byte)(24)),unchecked((byte)(15)),"On (Alt-M)");}else {TextRendDisplayStringCall(unchecked((byte)(25)),unchecked((byte)(24)),unchecked((byte)(15)),"Off (Alt-M)");}
return __result_QuickLine;
}
}








public static void CombatAiControl(TCharRecord creature){
byte stance=default(byte);
bool done=default(bool);
byte spell=default(byte);

{
done=CombatAi.CheckKeystroke(creature);
if(CombatAi.QuickLine()){

}
CharDispClearMessageWindowCall();
if((creature.enabled==0)){done=CharHlpEndActionsCall(creature);}
stance=creature.combatAddr.aiStance;
if(((((stance==0))||((stance>4)))||((DiceRollDiceCall(unchecked((byte)(1)),unchecked((byte)(4)))==1)))){
stance=DiceRollDiceCall(unchecked((byte)(1)),unchecked((byte)(8)));
if((stance==8)){stance=unchecked((byte)((DiceRollDiceCall(unchecked((byte)(1)),unchecked((byte)(2)))+4)));}else {stance=DiceRollDiceCall(unchecked((byte)(1)),unchecked((byte)(4)));}
}
creature.combatAddr.aiStance=stance;
if(!(done)){done=CombatAi.CheckMorale(creature);}
if((((creature.combatAddr.fleeing!=0))&&((creature.combatAddr.turned==0)))){CharDispPrintNameStringCall(creature,"flees in panic",unchecked((byte)(10)),true);}
if(done){return;}
if((creature.combatAddr.spellCast>0)){
spell=creature.combatAddr.spellCast;
creature.combatAddr.spellCast=unchecked((byte)(0));
MagicCastSpellNCCall(spell,true,true,ref done);
done=CharHlpEndActionsCall(creature);
}else {if(CombatAi.SelectItemToUse(creature)){done=CharHlpEndActionsCall(creature);}else {if(CombatAi.TurnUndeadAI(creature)){done=CharHlpEndActionsCall(creature);}else {if(CombatAi.SelectSpellToCast(creature)){done=true;}else {
CombatAi.EquipWeaponAndShield(creature);
done=CombatAi.CheckKeystroke(creature);
while(!(done)){if((CbtActsAcquireTargetCall(creature,unchecked((byte)(255)),unchecked((byte)(1)),unchecked((byte)(0)))&&((creature.combatAddr.initiative>0)))){done=CombatAi.AiMoveAttack(creature);}else {done=CombatAi.FinishRound(creature);}}
}}}}
}
}
public static bool TurnUndeadAI(TCharRecord creature){
bool __result_TurnUndeadAI=default(bool);
object tgt=null;

{
if(((((creature.combatAddr.usedTurnUndead==0))&&((CbtActsTurnLevelCall(creature)>0)))&&CbtActsFindTurnUndeadTargetCall(creature,ref tgt))){
__result_TurnUndeadAI=true;
CbtActsCombatTurnUndeadCall(creature);
}else {__result_TurnUndeadAI=false;}
return __result_TurnUndeadAI;
}
}
public static bool DecideCastSpell(TCharRecord creature,byte spell,byte minPri){
bool __result_DecideCastSpell=default(bool);
object tgt=null;
byte cnt=default(byte);
byte j=default(byte);

{
__result_DecideCastSpell=false;
if((GDConsts.spellTable[spell].aiPriority<minPri)){return __result_DecideCastSpell;}
if((((((spell!=C_Spell.CURE_LIGHT_WOUNDS))&&((GDConsts.spellTable[spell].offensive==0))))||((((spell==C_Spell.CURE_LIGHT_WOUNDS))&&CbtActsFindCureTargetCall(creature,ref tgt))))){__result_DecideCastSpell=true;}else {
cnt=CbtMapCountTargetsCall(creature,CharHlpCalcSpellRangeCall(spell));
if((cnt>0)){if((GDConsts.spellTable[spell].aiRadius==0)){__result_DecideCastSpell=true;}else {
{int __g2=cnt;for(int __g1=1;__g1<=__g2;__g1++){j=unchecked((byte)(__g1));if(CbtActsAreaHitsNonEnemyCall(GDVars.arenaLoc[GDVars.dsPotentialTargetCreature[j]].x,GDVars.arenaLoc[GDVars.dsPotentialTargetCreature[j]].y,GDConsts.spellTable[spell].aiRadius)){return __result_DecideCastSpell;}}}
__result_DecideCastSpell=true;
}}
}
return __result_DecideCastSpell;
}
}
public static bool SelectItemToUse(TCharRecord creature){
bool __result_SelectItemToUse=default(bool);
byte lowPri=default(byte);
byte roll=default(byte);
byte i=default(byte);
byte slot=default(byte);
byte effect=default(byte);
TItemRec item=null;
TItemRec chosen=null;
bool used=default(bool);

{
__result_SelectItemToUse=false;
chosen=null;
lowPri=unchecked((byte)(7));
roll=DiceRollDiceCall(unchecked((byte)(1)),unchecked((byte)(7)));
if(((((creature.combatAddr.itemUseAllowed!=0))&&((GDVars.teamCount[CharHlpEnemyTeamCall(creature)]>0)))&&((GDVars.zone0.antimagic==0)))){int __g4=roll;for(int __g3=1;__g3<=__g4;__g3++){i=unchecked((byte)(__g3));{
item=creature.itemsAddr;
while((((item!=null))&&((chosen==null)))){
effect=item.effect;
slot=CharHlpBaseItemCall(item.itemType).slot;
if(((((!(CharHlpItemReadableCall(item)))&&((item.special<128)))&&((item.equipped!=0)))&&((effect>0)))){
if((effect>56)){effect=unchecked((byte)((effect-23)));}
if(CombatAi.DecideCastSpell(creature,effect,lowPri)){chosen=item;}
}
item=item.next;
}
lowPri=unchecked((byte)((lowPri-1)));
}}}
if((chosen!=null)){
MagicUseItemCall(chosen,ref used);
__result_SelectItemToUse=true;
}
return __result_SelectItemToUse;
}
}
public static bool SelectSpellToCast(TCharRecord creature){
bool __result_SelectSpellToCast=default(bool);
byte[] list=new byte[(20)+1];
byte lowPri=default(byte);
byte roll=default(byte);
bool ext=default(bool);
byte tries=default(byte);
byte inner=default(byte);
byte count=default(byte);
byte k=default(byte);
byte spell=default(byte);
byte chosen=default(byte);

{
count=unchecked((byte)(0));
if((creature.combatAddr.castAllowed!=0)){int __g6=((21/1)-1);for(int __g5=0;__g5<=__g6;__g5++){k=unchecked((byte)(__g5));if((((creature.memorizedSpells[k]>0))&&((creature.memorizedSpells[k]<=67)))){
list[count]=creature.memorizedSpells[k];
count=unchecked((byte)((count+1)));
}}}
chosen=unchecked((byte)(0));
lowPri=unchecked((byte)(7));
roll=DiceRollDiceCall(unchecked((byte)(1)),unchecked((byte)(7)));
tries=unchecked((byte)(1));
if((count>0)){if((CharHlpIsNpcCall(creature)||GDVars.quickMagic)){if((GDVars.teamCount[CharHlpEnemyTeamCall(creature)]>0)){while((((tries<=roll))&&((chosen==0)))){
inner=unchecked((byte)(1));
while((((inner<4))&&((chosen==0)))){
k=unchecked((byte)((DiceRollDiceCall(unchecked((byte)(1)),count)-1)));
spell=list[k];
if(CombatAi.DecideCastSpell(creature,spell,lowPri)){chosen=spell;}
inner=unchecked((byte)((inner+1)));
}
lowPri=unchecked((byte)((lowPri-1)));
tries=unchecked((byte)((tries+1)));
}}}}
if((chosen>0)){CbtActsCastSpellCombatCall(chosen,unchecked((byte)(1)),ref ext);}else {ext=false;}
__result_SelectSpellToCast=ext;
return __result_SelectSpellToCast;
}
}
public static bool CanStepDir(TCharRecord creature,byte dir,ref bool edge){
bool __result_CanStepDir=default(bool);
TEffectRec effect=null;
byte crIdx=default(byte);
byte terrIdx=default(byte);
byte stepCost=default(byte);

{
edge=false;
__result_CanStepDir=false;
CbtMapProbeDirectionCall(creature,dir,out crIdx,out terrIdx);
if((terrIdx==0)){
edge=true;
return __result_CanStepDir;
}
if((crIdx!=0)){return __result_CanStepDir;}
if((GDConsts.terrainInfo[terrIdx].cost>=creature.combatAddr.movement)){return __result_CanStepDir;}
stepCost=unchecked((byte)(2));
if((dir&1)!=0){stepCost=unchecked((byte)(3));}
stepCost=unchecked((byte)((stepCost*GDConsts.terrainInfo[terrIdx].cost)));
if((terrIdx==C_Terr.STINK_CLOUD)){if(((((!(CharHlpHasEffectCall(creature,unchecked((byte)(C_Effect.ANIMATE_DEAD)),out effect)))&&(!(CharHlpHasEffectCall(creature,unchecked((byte)(C_Effect.STINK_COUGH)),out effect))))&&(!(CharHlpHasEffectCall(creature,unchecked((byte)(C_Effect.NO_PARA_POISON)),out effect))))&&(!(CharHlpHasEffectCall(creature,unchecked((byte)(C_Effect.NO_CHARM_POISON)),out effect))))){stepCost=unchecked((byte)((creature.combatAddr.movement+1)));}}
if((creature.combatAddr.movement>=stepCost)){__result_CanStepDir=true;}
return __result_CanStepDir;
}
}
public static bool AiCanStep(TCharRecord creature,byte attempt,byte baseDir,ref bool edge){
bool __result_AiCanStep=default(bool);


{
__result_AiCanStep=CombatAi.CanStepDir(creature,unchecked((byte)((((baseDir+GDConsts.aiDirOffsets[creature.combatAddr.aiStance,attempt]))%8))),ref edge);
return __result_AiCanStep;
}
}
public static void AiMoveStep(TCharRecord creature){
byte dir=default(byte);
byte adjDir=default(byte);
byte attempt=default(byte);
bool edge=default(bool);
bool done=default(bool);
bool routed=default(bool);
byte slot=default(byte);
byte targetSlot=default(byte);

{
if(!(quickShown)){TextRendDisplayStringCall(unchecked((byte)(0)),unchecked((byte)(24)),unchecked((byte)(10)),(("Move/Attack, Move Left = "+StrUtilNumStrCall((creature.combatAddr.movement/2)))+' '));}
if(CombatAi.CheckKeystroke(creature)){return;}
if((((((creature.combatAddr.movement/2)>0))&&((creature.combatAddr.initiative>0)))&&((((creature.combatAddr.fleeing!=0))||((creature.charClass!=C_Class.MAGE)))))){
routed=false;
if((creature.combatAddr.fleeing==0)){
dir=CbtActsDirTowardTargetCall(creature,creature.combatAddr.other);
if((creature.combatAddr.other!=null)){
slot=CbtMapCombatSlotCall(creature);
targetSlot=CbtMapCombatSlotCall(creature.combatAddr.other);
if((((slot!=0))&&((targetSlot!=0)))){routed=(PathingRouteStepCall(slot,targetSlot,CbtMap.OccupantAt,ref adjDir)&&CombatAi.CanStepDir(creature,adjDir,ref edge));}
}
}else {
creature.combatAddr.aiStance=DiceRollDiceCall(unchecked((byte)(1)),unchecked((byte)(2)));
dir=GDVars.gFacing;
if((creature.hostile==0)){dir=unchecked((byte)((dir+4)));}
dir=unchecked((byte)((dir%8)));
}
edge=false;
done=false;
attempt=unchecked((byte)(1));
if(routed){
lastMoveDir=adjDir;
stuckCount=unchecked((byte)(0));
}else {
while(((((attempt<6))&&(!(done)))&&(!(CombatAi.AiCanStep(creature,attempt,dir,ref edge))))){if((((creature.combatAddr.fleeing!=0))&&edge)){done=CbtActsCombatFleeAttemptCall(creature);}else {attempt=unchecked((byte)((attempt+1)));}}
if(done){
creature.combatAddr.movement=unchecked((byte)(0));
creature.combatAddr.fleeing=unchecked((byte)(0));
done=CharHlpEndActionsCall(creature);
}else {
if((attempt<6)){adjDir=unchecked((byte)((((dir+GDConsts.aiDirOffsets[creature.combatAddr.aiStance,attempt]))%8)));}else {adjDir=dir;}
if((((attempt==6))||((lastMoveDir==(((adjDir+4))%8))))){
stuckCount=unchecked((byte)((stuckCount+1)));
creature.combatAddr.aiStance=unchecked((byte)(((creature.combatAddr.aiStance%6)+1)));
if((stuckCount>1)){
creature.combatAddr.other=null;
if((stuckCount>2)){
creature.combatAddr.movement=unchecked((byte)(0));
done=true;
}else {if(!(CbtActsAcquireTargetCall(creature,unchecked((byte)(255)),unchecked((byte)(1)),unchecked((byte)(0))))){done=CombatAi.FinishRound(creature);}}
}
}
if((attempt<6)){lastMoveDir=adjDir;}else {done=true;}
}
}
if(!(done)){
CbtMapSetCreatureFacingCall(creature,adjDir,unchecked((byte)(0)),unchecked((byte)(0)));
CbtActsMoveOpportunityAttacksCall(creature,creature.combatAddr.direction);
if((creature.enabled==0)){done=CharHlpEndActionsCall(creature);}else {
CbtActsCombatMoveCreatureCall(creature,creature.combatAddr.direction);
if((creature.enabled==0)){done=CharHlpEndActionsCall(creature);}
FxEngineConsiderStinkingCloudCall(creature);
}
}
}else {done=CombatAi.FinishRound(creature);}
}
}
public static bool AiMoveAttack(TCharRecord creature){
bool __result_AiMoveAttack=default(bool);
bool done=default(bool);
bool inRange=default(bool);
bool canAct=default(bool);
byte weaponRange=default(byte);
ushort range=default(ushort);
byte cnt=default(byte);
byte pick=default(byte);
TItemRec weapon=null;
TCharRecord target=null;
sbyte tx=default(sbyte);
sbyte ty=default(sbyte);
byte loopCount=default(byte);

{
lastMoveDir=unchecked((byte)(C_Dir.CENTER));
stuckCount=unchecked((byte)(0));
loopCount=unchecked((byte)(0));
canAct=true;
done=false;
FxEngineTriggerEffectsWithConditionCall(unchecked((byte)(C_Cond.MONSTER_SPECIAL)),creature);
if((((creature.hostile==0))&&CombatBandageDyingCall(true))){done=CharHlpEndActionsCall(creature);}
if((creature.combatAddr.initiative==0)){canAct=false;}
while(((!(done))&&canAct)){
if((creature.combatAddr.fleeing!=0)){while(((((creature.combatAddr.movement>0))&&((creature.combatAddr.initiative>0)))&&((creature.combatAddr.initiative<20)))){CombatAi.AiMoveStep(creature);}}
if((((creature.combatAddr.initiative==0))||((creature.combatAddr.initiative==20)))){canAct=false;}else {done=false;}
if(((!(done))&&canAct)){
loopCount=unchecked((byte)((loopCount+1)));
if((loopCount>20)){
done=true;
canAct=(CombatAi.FinishRound(creature)==false);
}
inRange=false;
weaponRange=unchecked((byte)(1));
if((creature.equipped[C_Equip.WEAPON]!=null)){weaponRange=unchecked((byte)((CharHlpBaseItemCall(creature.equipped[C_Equip.WEAPON].itemType).range-1)));}
if((((weaponRange==0))||((weaponRange==255)))){weaponRange=unchecked((byte)(1));}
target=creature.combatAddr.other;
if((((target!=null))&&((((target.enabled==0))||((target.hostile==0)))))){target=null;}
if((target!=null)){if(CbtActsCanAttackTargetCall(creature,target)){
tx=CbtMapCombatCellXCall(target);
ty=CbtMapCombatCellYCall(target);
range=unchecked((ushort)(weaponRange));
if(PathingWalkLineCall(CbtMapCombatCellXCall(creature),CbtMapCombatCellYCall(creature),ref tx,ref ty,ref range,GDVars.arenaPtr)){if(((range/2)<=weaponRange)){inRange=true;}}
}}
if(!(inRange)){
cnt=CbtMapCountTargetsCall(creature,weaponRange);
if((cnt==0)){
if(CbtActsAcquireTargetCall(creature,unchecked((byte)(255)),unchecked((byte)(0)),unchecked((byte)(0)))){CombatAi.AiMoveStep(creature);}else {done=CombatAi.FinishRound(creature);}
}else {
pick=DiceRollDiceCall(unchecked((byte)(1)),cnt);
target=GDVars.creatureArr[GDVars.dsPotentialTargetCreature[pick]];
if(((CharHlpHasRangedWeaponCall(creature)&&(!(CharHlpHasThrowableWeaponCall(creature))))&&((CbtMapCountTargetsCall(creature,unchecked((byte)(1)))>0)))){
CombatAi.EquipWeaponAndShield(creature);
done=true;
}else {if((((CbtMapCalcDistanceCall(creature,target)==1))||CbtActsCanAttackTargetCall(creature,target))){inRange=true;}}
}
}
if(inRange){CbtMapScrollViewCall(CbtMapCombatCellXCall(creature),CbtMapCombatCellYCall(creature),unchecked((byte)(2)),CbtActsDirTowardTargetCall(creature,target));}
if(inRange){
if(CbtActsSweepAttemptCall(creature,target)){done=CharHlpEndActionsCall(creature);}else {
CbtActsNoteAttackDirectionCall(creature,target);
weapon=null;
if(CharHlpHasRangedWeaponCall(creature)){
inRange=CharHlpFindDamagingRangedWeaponCall(creature,ref weapon);
if(CharHlpHasThrowableWeaponCall(creature)){if((CbtMapCalcDistanceCall(creature,target)==1)){weapon=null;}}
}
CbtActsCombatAttackCall(creature,target,false,weapon,ref done);
if(done){canAct=false;}else {if((target.enabled==0)){done=true;}}
}
}
}
}
__result_AiMoveAttack=(canAct==false);
return __result_AiMoveAttack;
}
}
public static bool FinishRound(TCharRecord creature){
bool __result_FinishRound=default(bool);


{
CharDispClearMessageWindowCall();
if(((CharHlpIsHelplessCall(creature)||CharHlpHasRangedWeaponCall(creature))||((creature.combatAddr.initiative==0)))){__result_FinishRound=CharHlpEndActionsCall(creature);}else {
__result_FinishRound=CharHlpGuardCall(creature);
CharDispPrintBottomLineCall("Guarding");
}
return __result_FinishRound;
}
}
public static bool CheckKeystroke(TCharRecord creature){
bool __result_CheckKeystroke=default(bool);
char ch=default(char);
bool ext=default(bool);

{
__result_CheckKeystroke=false;
if(GameIo.Host.KeyPressed){
ch=unchecked((char)(GameIoReadGameKeyCall()));
ext=(ch==0);
if(ext){ch=unchecked((char)(GameIoReadGameKeyCall()));}
{var __g7=ch;if(__g7==C_Keys.ALT_M){if(ext){CombatAi.ToggleQuickMagic();}}else if(__g7==' '){
CombatAi.AllToManual();
if((creature.quickfight==0)){
creature.combatAddr.initiative=unchecked((sbyte)(20));
__result_CheckKeystroke=true;
}
}else if(__g7==C_Keys.ALT_X){if(ext){if(CbtActsStingVictoryCheatCall()){

}}}}
}
return __result_CheckKeystroke;
}
}
public static bool CheckMorale(TCharRecord creature){
bool __result_CheckMorale=default(bool);
byte enemyMove=default(byte);
byte moraleSave=default(byte);
short hpPct=default(short);

{
__result_CheckMorale=false;
creature.combatAddr.fleeing=unchecked((byte)(0));
FxEngineRemoveAiEffectsCall(creature);
if((creature.combatAddr.turned!=0)){
creature.combatAddr.fleeing=unchecked((byte)(1));
CharDispPrintNameStringCall(creature,"is forced to flee",unchecked((byte)(10)),true);
return __result_CheckMorale;
}
if(!(CharHlpIsNpcCall(creature))){return __result_CheckMorale;}
GDVars.dsCurrentMorale=unchecked((byte)((((creature.npc&C_Npc.MORALE_MASK))*2)));
if((GDVars.dsCurrentMorale>102)){GDVars.dsCurrentMorale=unchecked((byte)(0));}
FxEngineTriggerEffectsWithConditionCall(unchecked((byte)(C_Cond.MORALE_CHECK)),creature);
if((creature.hpMax==0)){hpPct=unchecked((short)(0));}else {hpPct=unchecked((short)(((creature.hpCur*100)/creature.hpMax)));}
if((((GDVars.dsCurrentMorale>=(100-hpPct)))&&((GDVars.dsCurrentMorale!=0)))){return __result_CheckMorale;}
moraleSave=GDVars.dsCurrentMorale;
GDVars.dsCurrentMorale=GDVars.hostileHpPct;
FxEngineTriggerEffectsWithConditionCall(unchecked((byte)(C_Cond.MORALE_CHECK)),creature);
if(((((GDVars.dsCurrentMorale>=(100-GDVars.zone1[C_Gbvm.MORALE_THRESHOLD])))&&((GDVars.dsCurrentMorale!=0)))&&((creature.hostile!=0)))){return __result_CheckMorale;}
enemyMove=CbtActsHighestEnemyMovementCall(creature);
if((enemyMove<=(CbtActsCalcCreatureMovementCall(creature)/2))){
creature.combatAddr.fleeing=unchecked((byte)(1));
FxEngineRemoveEffectCall(creature,unchecked((byte)(C_Effect.BITE_HOLD)),null);
FxEngineRemoveEffectCall(creature,unchecked((byte)(C_Effect.BLOOD_DRAIN)),null);
}else {if((creature.intCur>5)){
FxEnginePutOutOfPlayWithStatusCall(creature,unchecked((byte)(C_Status.UNCONSCIOUS)),"Surrenders");
__result_CheckMorale=CharHlpEndActionsCall(creature);
}}
return __result_CheckMorale;
}
}
public static byte CalcWeaponPriority(TCharRecord creature,TItemRec item){
byte __result_CalcWeaponPriority=default(byte);
byte pri=default(byte);
TBaseItemRec @base=new TBaseItemRec();

{
@base=CharHlpBaseItemCall(item.itemType);
pri=unchecked((byte)((@base.dmgSmDice*@base.dmgSmRolls)));
if((item.plus>0)){pri=unchecked((byte)((pri+(item.plus*8))));}
if((@base.dmgSmMod>0)){pri=unchecked((byte)((pri+(@base.dmgSmMod*2))));}
if(((((item.itemType==C_Item.HOLY_WATER_VIAL))&&((creature.combatAddr.other!=null)))&&((creature.combatAddr.other.levelUndead>0)))){pri=unchecked((byte)(8));}
if((((@base.props&C_Item.PROP_FIRES_AMMO))>0)){pri=unchecked((byte)((pri+(((@base.rateOfFire-1))*2))));}
if((@base.hands<=1)){pri=unchecked((byte)((pri+3)));}
if(((@base.hands+creature.handsEquipped)>3)){pri=unchecked((byte)(0));}
if((item.special==C_Effect.ALIGNED_WEAPON)){if((((item.effect&15))!=creature.alignment)){pri=unchecked((byte)(0));}}
if((item.effect==83)){pri=unchecked((byte)(0));}
if((item.cursed!=0)){pri=unchecked((byte)(0));}
__result_CalcWeaponPriority=pri;
return __result_CalcWeaponPriority;
}
}
public static void EquipWeaponAndShield(TCharRecord creature){
TItemRec bestRanged=null;
TItemRec bestMelee=null;
TItemRec bestShield=null;
TItemRec item=null;
TItemRec ammo=null;
byte bestRangedPri=default(byte);
byte bestMeleePri=default(byte);
byte bestShieldPri=default(byte);
byte pri=default(byte);
byte itType=default(byte);
byte props=default(byte);
bool needSwap=default(bool);
bool changed=default(bool);
bool trueRanged=default(bool);
bool hasAmmo=default(bool);

{
if((creature.equipped[C_Equip.WEAPON]!=null)){creature.handsEquipped=unchecked((byte)((creature.handsEquipped-CharHlpBaseItemCall(creature.equipped[C_Equip.WEAPON].itemType).hands)));}
if((creature.equipped[C_Equip.SHIELD]!=null)){creature.handsEquipped=unchecked((byte)((creature.handsEquipped-CharHlpBaseItemCall(creature.equipped[C_Equip.SHIELD].itemType).hands)));}
bestRanged=null;
bestMelee=null;
bestShield=null;
bestRangedPri=unchecked((byte)(1));
bestMeleePri=unchecked((byte)((creature.unarmedDiceA[1]*creature.unarmedRollsA[1])));
if((creature.unarmedModA[1]>0)){bestMeleePri=unchecked((byte)((bestMeleePri+(creature.unarmedModA[1]*2))));}
bestShieldPri=unchecked((byte)(0));
item=creature.itemsAddr;
while((item!=null)){
itType=item.itemType;
if((CharHlpBaseItemCall(itType).slot==C_Equip.WEAPON)){if((((CharHlpBaseItemCall(itType).classMask&creature.itemLimits))!=0)){
pri=CombatAi.CalcWeaponPriority(creature,item);
if((((((CharHlpBaseItemCall(itType).props&C_Item.PROP_FIRES_AMMO))!=0))||((((CharHlpBaseItemCall(itType).props&C_Item.PROP_THROWN))!=0)))){if((pri>bestRangedPri)){
bestRanged=item;
bestRangedPri=pri;
}}
if((((CharHlpBaseItemCall(itType).props&C_Item.PROP_FIRES_AMMO))==0)){if((pri>bestMeleePri)){
bestMelee=item;
bestMeleePri=pri;
}}
}}
if((CharHlpBaseItemCall(itType).slot==C_Equip.SHIELD)){if((((CharHlpBaseItemCall(itType).classMask&creature.itemLimits))>0)){
if((item.plus>=0)){pri=unchecked((byte)((item.plus+1)));}else {pri=unchecked((byte)(0));}
if((pri>bestShieldPri)){
bestShield=item;
bestShieldPri=pri;
}
}}
item=item.next;
}
if(((((bestRanged!=null))&&((CharHlpBaseItemCall(bestRanged.itemType).range>1)))&&((((CharHlpBaseItemCall(bestRanged.itemType).props&C_Item.PROPS_THROWABLE))==C_Item.PROPS_THROWABLE)))){trueRanged=true;}else {trueRanged=false;}
hasAmmo=false;
ammo=null;
props=unchecked((byte)(0));
if((bestRanged!=null)){
props=CharHlpBaseItemCall(bestRanged.itemType).props;
if((((props&C_Item.PROP_THROWN))!=0)){ammo=bestRanged;}
if((((props&C_Item.PROP_FIRES_AMMO))!=0)){
if((((props&C_Item.PROP_ARROWS))!=0)){ammo=creature.equipped[C_Equip.ARROW];}
if((((props&C_Item.PROP_QUARRELS))!=0)){ammo=creature.equipped[C_Equip.BOLT];}
}
}
if((((ammo!=null))||((props==10)))){hasAmmo=true;}
if((((((bestRanged!=null))&&((bestRangedPri>(bestMeleePri/2))))&&hasAmmo)&&((trueRanged||((CbtMapCountTargetsCall(creature,unchecked((byte)(1)))==0)))))){item=bestRanged;}else {item=bestMelee;}
changed=false;
needSwap=true;
if((creature.equipped[C_Equip.WEAPON]!=null)){if((((creature.equipped[C_Equip.WEAPON]==item))||((creature.equipped[C_Equip.WEAPON].cursed!=0)))){needSwap=false;}}
if(needSwap){
if((creature.equipped[C_Equip.WEAPON]!=null)){FxEngineToggleReadyItemCall(creature.equipped[C_Equip.WEAPON]);}
CharHlpRecalcStatsCall(creature);
if((creature.equipped[C_Equip.SHIELD]!=null)){if((creature.equipped[C_Equip.SHIELD].cursed==0)){creature.handsEquipped=unchecked((byte)((creature.handsEquipped-CharHlpBaseItemCall(creature.equipped[C_Equip.SHIELD].itemType).hands)));}}
if((item!=null)){FxEngineToggleReadyItemCall(item);}
changed=true;
}
CharHlpRecalcStatsCall(creature);
CbtActsSetCurAttacksX2Call(creature);
needSwap=true;
if((creature.equipped[C_Equip.SHIELD]!=null)){if((((creature.equipped[C_Equip.SHIELD]==bestShield))||((creature.equipped[C_Equip.SHIELD].cursed!=0)))){needSwap=false;}}
if((creature.handsEquipped>2)){
if((((creature.equipped[C_Equip.SHIELD]==null))||((creature.equipped[C_Equip.SHIELD].cursed!=0)))){
if((item!=null)){FxEngineToggleReadyItemCall(item);}
changed=true;
}else {
FxEngineToggleReadyItemCall(creature.equipped[C_Equip.SHIELD]);
changed=true;
}
}else {if((((creature.handsEquipped<2))&&needSwap)){
if((creature.equipped[C_Equip.SHIELD]!=null)){FxEngineToggleReadyItemCall(creature.equipped[C_Equip.SHIELD]);}
CharHlpRecalcStatsCall(creature);
if((bestShield!=null)){FxEngineToggleReadyItemCall(bestShield);}
changed=true;
}}
CharHlpRecalcStatsCall(creature);
if(changed){CharDispCombatPrintCreatureInfoCall(creature);}
}
}
public static void ToggleQuickMagic(){


{
GDVars.quickMagic=(GDVars.quickMagic==false);
if(!(CombatAi.QuickLine())){if(GDVars.quickMagic){CharDispPrintBottomLineCall("Magic On");}else {CharDispPrintBottomLineCall("Magic Off");}}
}
}
public static void AllToManual(){
TCharRecord p=null;

{
p=GDVars.creatureList;
while((p!=null)){
if(CharHlpComesUnderPlayerCall(p)){p.quickfight=unchecked((byte)(0));}
p=p.nextCharAddr;
}
}
}
static CombatAi(){quickShown=false;}
}}
