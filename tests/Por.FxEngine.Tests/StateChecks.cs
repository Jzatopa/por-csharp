using System;
using System.Collections.Generic;
using System.Linq;
using Por.Core;
using static Por.Core.Types;
static partial class Checks
{
 static void Timeout()
 {
  var rng=new Random(83711);
  for(int trial=0;trial<1800;trial++)
  {
   Rig();var c=Char();GDVars.creatureList=c;int n=rng.Next(1,27);var input=new List<int>();var refs=new List<TEffectRec>();
   for(int i=0;i<n;i++){int duration=rng.Next(31);input.Add(duration);refs.Add(Effect(c,i+1,duration));}
   int amount=rng.Next(32),unit=rng.Next(4);int elapsed=unit==0?amount/10:unit==1?amount:unit==2?amount*10:amount*60;
   FxEngine.TimeOutEffects(B(amount),S(unit));var expected=input.Select((duration,i)=>new{duration,i}).Where(t=>t.duration==0||t.duration>elapsed).ToList();var actual=Nodes(c);Eq(expected.Count,actual.Count,"timeout survivor count");
   for(int i=0;i<expected.Count;i++){Yes(ReferenceEquals(refs[expected[i].i],actual[i]),"timeout survivor identity/order");Eq(expected[i].duration==0?0:expected[i].duration-elapsed,actual[i].duration,"timeout unit conversion");}
   if(elapsed>0)Eq(expected.Any(t=>t.duration>0)?1:0,GDVars.effFlags[1],"timeout remaining flag");
  }
  Rig();var a=Char();var b=Char();a.nextCharAddr=b;GDVars.creatureList=a;GDVars.ifcState=C_Ifc.CAMPING;GDVars.zone1[C_Gbvm.PARTY_SIZE]=2;GDVars.effFlags[1]=0;GDVars.effFlags[2]=1;var x=Effect(a,1,5);var y=Effect(b,1,5);FxEngine.TimeOutEffects(1,1);Eq(5,x.duration,"camp skips inactive flag");Eq(4,y.duration,"camp visits active flag");
  Rig();a=Char();GDVars.creatureList=a;GDVars.ifcState=C_Ifc.CAMPING;GDVars.zone1[C_Gbvm.PARTY_SIZE]=0;GDVars.effFlags[1]=1;x=Effect(a,1,3);FxEngine.TimeOutEffects(1,1);Eq(2,x.duration,"zero party post-test scans first flag");
  Rig();a=Char();GDVars.creatureList=a;for(int i=0;i<33;i++)Effect(a,1,1);FxEngine.TimeOutEffects(10,1);Eq(1,Nodes(a).Count,"expiration step caps at32");Eq(1,GDVars.effFlags[1],"overflow expiration retains flag");FxEngine.TimeOutEffects(1,1);Eq(0,Nodes(a).Count,"next step removes deferred expiration");
  Rig();a=Char();GDVars.creatureList=a;x=Effect(a,1,1,0,true);y=Effect(a,2,1,0,true);var z=Effect(a,3,1,0,true);GDVars.effectFns[1]=(c,p,r)=>{FxEngine.RemoveEffect(c,2,y);z.duration=19;Effect(c,4,7);};FxEngine.TimeOutEffects(1,1);var survivors=Nodes(a);Yes(survivors.SequenceEqual(new[]{z,z.next}),"callback sibling removal/new node safe");Eq(19,z.duration,"changed expiry duration is not removed");Eq(7,z.next.duration,"new callback effect not aged immediately");Eq(1,GDVars.effFlags[1],"callback additions keep timed flag");
  Rig();a=Char();GDVars.creatureList=a;x=Effect(a,1,1,0,true);y=Effect(a,2,1);GDVars.effectFns[1]=(c,p,r)=>y.id=3;FxEngine.TimeOutEffects(1,1);Yes(ReferenceEquals(a.effectsAddr,y),"changed expiry id skips stale removal");
  Rig();a=Char();GDVars.creatureList=a;GDVars.effFlags[0]=97;Effect(a,1,20);FxEngine.TimeOutEffects(1,1);Eq(97,GDVars.effFlags[0],"flag padding untouched");
  for(int unit=0;unit<=6;unit++)
  {
   Rig();a=Char();a.age=39;GDVars.creatureList=a;int[] clock={9,9,5,23,29,11,4};for(int i=0;i<7;i++)GDVars.zone0.words[C_Gbvm.CLOCK+i]=unchecked((ushort)clock[i]);
   int[] expected=(int[])clock.Clone();expected[unit]++;int[] scales={10,10,6,24,30,12};for(int i=0;i<6;i++)if(expected[i]>=scales[i]){expected[i]-=scales[i];expected[i+1]++;}
   FxEngine.AdvanceTime(1,S(unit));for(int i=0;i<7;i++)Eq(expected[i],GDVars.zone0.words[C_Gbvm.CLOCK+i],"clock carry words");Eq(unit<6?40:39,a.age,"year rollover age hook");
  }
 }
 static void Damage()
 {
  foreach(int reduction in new[]{0,1,2,3})foreach(bool saved in new[]{false,true})foreach(int damage in new[]{0,1,2,3,99,100,255})foreach(int element in new[]{0,1,2,4,8,16,17,3})
  {
   Rig();var c=Char();GDVars.elementMask=B(element);int received=-1;FxEngine.InflictDamage=(cr,n)=>received=n;FxEngine.InflictNonWeaponDamage(c,B(damage),B(reduction),saved);
   int expected=saved&&reduction==1?0:saved&&reduction==2?damage/2:damage;Eq(expected,GDVars.pendingDamage,"post-save pending damage");Eq(expected==0?-1:expected,received,"damage application only when nonzero");if(expected==0){Eq(0,log.Count,"zero damage no display");continue;}
   string expectedText="takes "+expected+(expected==1?" point of damage ":" points of damage ");int stripped=element&~C_Effect.ELEM_MAGIC;string suffix=stripped==C_Effect.ELEM_FIRE?"from Fire":stripped==C_Effect.ELEM_COLD?"from Cold":stripped==C_Effect.ELEM_ELEC?"from Electricity":stripped==C_Effect.ELEM_ACID?"from Acid":"";if((element&C_Effect.ELEM_MAGIC)==element)suffix+="from Magic";
   Yes(log[0]=="anim:False:"+expectedText+suffix,"element and singular/plural damage text");Yes(log.Last()=="clear","damage clears message last");
  }
  Rig();var a=Char();foreach(int status in new[]{6,7,8}){a.status=B(status);log.Clear();GDVars.pendingDamage=91;FxEngine.InflictNonWeaponDamage(a,99,0,false);Eq(91,GDVars.pendingDamage,"dead target no-op");Eq(0,log.Count,"dead target no message");}
  Rig();a=Char();Effect(a,17);GDVars.effectFns[17]=(cr,p,r)=>GDVars.pendingDamage=9;int got=0;FxEngine.InflictDamage=(cr,n)=>got=n;FxEngine.InflictNonWeaponDamage(a,100,2,true);Eq(4,got,"effect mutation precedes save reduction");
  Rig();a=Char();a.hpCur=10;GDVars.ifcState=C_Ifc.COMBAT;a.combatAddr.spellCast=3;a.combatAddr.castAllowed=1;a.memorizedSpells[0]=3;FxEngine.InflictNonWeaponDamage(a,1,0,false);Eq(9,a.hpCur,"real CharHlp damage");Eq(0,a.combatAddr.castAllowed,"damage disables casting");Eq(0,a.combatAddr.spellCast,"damage loses pending spell");Eq(0,a.memorizedSpells[0],"spell removed from memorized slots");
  Rig();a=Char(1);a.hpCur=1;GDVars.ifcState=C_Ifc.COMBAT;GDVars.creatureList=a;CbtMap.BuildArenaArrays();FxEngine.InflictNonWeaponDamage(a,2,0,false);Eq(C_Status.DYING,a.status,"real damage dying status");Eq(0,GDVars.arenaLoc[1].size,"real downed map removal");Eq(1,GDVars.downedCount,"real downed corpse");Yes(log.Contains("autostop:10"),"real downed timer restored");
  Rig();a=Char(1);a.hpCur=1;GDVars.ifcState=C_Ifc.COMBAT;Effect(a,99);GDVars.effectFns[99]=(cr,p,r)=>cr.enabled=1;FxEngine.InflictNonWeaponDamage(a,2,0,false);Eq(1,a.enabled,"HP-zero callback can keep creature up");Eq(0,GDVars.downedCount,"revived by HP-zero skips downed call");Yes(log.Contains("pause"),"HP-zero recovery pause");
  Rig();a=Char();var item=new TItemRec{name="ORB "};FxEngine.InflictDamage=(c,n)=>{};FxEngine.InflictItemShock(a,item,2);Yes(log.SequenceEqual(new[]{"label:ORB ","msg:10:True:ORB ","msg:15:False:shocks ","msg:10:False:TEST","msg:15:False: for 2 damage, and has been unequipped.","wait","clear"}),"item shock exact coloured message order");
  Rig();a=Char(1);GDVars.ifcState=C_Ifc.COMBAT;CbtMap.BuildArenaArrays();GDVars.curChar=a;Effect(a,C_Effect.STINK_COUGH);handlers.Clear();FxEngine.ConsiderStinkingCloud(a);Eq(0,handlers.Count,"cloud check normal terrain no-op");a.effectsAddr=null;GDVars.arenaPtr.cells[3,3]=30;Dice.RandomBelow=n=>n-1;FxEngine.ConsiderStinkingCloud(a);Eq(30,a.effectsAddr.id,"cloud successful save cough");Eq(1,a.effectsAddr.duration,"cloud successful save duration");Yes(handlers.Last()==30,"cloud invokes result handler");
  Rig();a=Char(1);GDVars.ifcState=C_Ifc.COMBAT;CbtMap.BuildArenaArrays();GDVars.arenaPtr.cells[3,3]=30;Dice.RandomBelow=n=>0;FxEngine.ConsiderStinkingCloud(a);Eq(31,a.effectsAddr.id,"cloud failed save nausea");Eq(2,a.effectsAddr.duration,"cloud failed save 1d4+1");
 }
 static void Equipment()
 {
  for(int equipped=0;equipped<2;equipped++)for(int cursed=0;cursed<2;cursed++)for(int hands=0;hands<4;hands++)foreach(int slot in new[]{0,8,9,10,11,12})foreach(bool occupied in new[]{false,true})foreach(bool allowed in new[]{false,true})foreach(bool quick in new[]{false,true})
  {
   Rig();var c=Char();GDVars.curChar=c;GDVars.ifcState=C_Ifc.COMBAT;c.quickfight=B(quick?1:0);c.itemLimits=1;c.handsEquipped=B(hands);GDVars.baseItems[1]=new TBaseItemRec{slot=B(slot),hands=1,classMask=B(allowed?1:2)};var item=new TItemRec{itemType=1,equipped=B(equipped),cursed=B(cursed),special=132};if(occupied){c.equipped[slot==9?10:slot]=new TItemRec{name="OLD"};if(slot==9)c.equipped[9]=new TItemRec{name="OLD"};}
   FxEngine.ToggleReadyItem(item);
   if(equipped!=0){Eq(cursed!=0?1:0,item.equipped,"unready curse rule");Eq(cursed!=0?0:1,handlers.Count,"unready callback only on success");}
   else{bool busy=occupied&&(slot<=9);bool success=allowed&&!busy&&hands+1<=2;Eq(success?1:0,item.equipped,"equipment validation");if(!allowed)Yes(log.Last()=="bottom:Wrong Class","class overrides slot/hands");else if(busy)Yes(log.Last()=="bottom:already using OLD","slot overrides hands");else if(!success&&quick)Eq(0,log.Count,"quickfight suppresses hands-full message");}
  }
  foreach(int type in new[]{C_Item.ARROW,C_Item.QUARREL}){Rig();var c=Char();GDVars.curChar=c;c.itemLimits=255;GDVars.baseItems[type]=new TBaseItemRec{slot=9,hands=0,classMask=255};int ammo=type==C_Item.ARROW?11:12;c.equipped[ammo]=new TItemRec{name="AMMO"};var item=new TItemRec{itemType=B(type)};FxEngine.ToggleReadyItem(item);Yes(log.Last()=="bottom:already using AMMO","ammo-specific occupied slot");}
 }
 static void HoldsRevive()
 {
  foreach(bool removed in new[]{false,true})foreach(bool ownerUp in new[]{false,true})foreach(bool victimUp in new[]{false,true})foreach(int index in new[]{0,1,2,72,73,255})
  {
   Rig();var c=Char(1);var target=Char(2,4,3);c.enabled=B(ownerUp?1:0);target.enabled=B(victimUp?1:0);var hold=Effect(c,74,5,index,true);Effect(target,58);bool released=true;var result=FxEngine.HoldTarget(c,hold,removed,74,ref released);
   bool valid=index>0&&index<=2;if(!valid){Yes(result==null&&!released,"invalid hold index no release");Yes(removed||c.effectsAddr==null,"invalid hold removed safely");continue;}
   var victim=GDVars.creatureArr[index];bool release=removed||!ownerUp||victim.enabled==0;Eq(release?1:0,released?1:0,"hold release state");Yes(release?result==null:ReferenceEquals(result,victim),"hold victim pointer");
  }
  foreach(int status in new[]{C_Status.RUNNING,C_Status.DEAD}){Rig();var c=Char(1);GDVars.creatureList=c;GDVars.ifcState=C_Ifc.COMBAT;CbtMap.BuildArenaArrays();c.hpCur=50;FxEngine.PutOutOfPlayWithStatus(c,B(status),new string('x',50));Eq(0,c.enabled,"out-of-play disables");Eq(status==C_Status.RUNNING?50:0,c.hpCur,"running keeps hitpoints");Eq(0,GDVars.arenaLoc[1].size,"out-of-play clears map");Yes(log.Contains("name:"+new string('x',40)+":10:True"),"status message short-string cap");}
  Rig();var a=Char(1);GDVars.ifcState=C_Ifc.COMBAT;GDVars.creatureList=a;CbtMap.RegisterDowned(a,3,3);GDVars.arenaLoc[1].size=0;CbtMap.BuildArenaArrays();a.enabled=0;a.status=C_Status.DEAD;Yes(FxEngine.ReviveDeadCreature(a,17),"revive real map accepts corpse");Eq(17,a.hpCur,"revive hp");Eq(1,a.enabled,"revive enabled");Eq(0,a.status,"revive status");Eq(1,CbtMap.OccupantAt(3,3),"revive map occupied");Yes(GDVars.downed[1].cr==null,"revive clears corpse record");
  Rig();a=Char(1);Char(2);GDVars.ifcState=C_Ifc.COMBAT;GDVars.arenaLoc[1].size=0;CbtMap.BuildArenaArrays();a.enabled=0;a.status=6;Yes(!FxEngine.ReviveDeadCreature(a,20),"revive blocked by other creature");Eq(0,a.enabled,"failed revive leaves disabled");
 }
 static void Bindings()
 {
  Rig();FxEngineBindings.Bind();var combat=Combat.Services;FxEngineBindings.Bind();Yes(ReferenceEquals(combat,Combat.Services),"bindings idempotent combined combat owner");var c=Char();Magic.Effects.AddEffect(c,1,7,3,false);Eq(1,c.effectsAddr.id,"Magic real effects");CbtActs.Effects.RemoveEffect(c,1,null);Yes(c.effectsAddr==null,"CbtActs real effect removal");GDVars.creatureList=c;Effect(c,1,7);EclVm.Units.FxEngine.AdvanceTime(1,1);Eq(6,c.effectsAddr.duration,"VM real effect clock");Combat.Services.AdvanceTime(1,1);Eq(5,c.effectsAddr.duration,"Combat real effect clock");
  Rig();c=Char();GDVars.creatureList=c;Effect(c,1,500);GDVars.zone3=new TZone3();GDVars.partyDefeated=false;GDVars.debugTraceEnabled=false;GDVars.protTextShown=0;EclVm.ResetUnitState();EclBindings.BindConvertedUnits();FxEngineBindings.Bind();
  // Real interpreter -> real EclVars operand decoder -> FxEngine, including >255 minute chunking.
  ushort address=0x9900;byte[] script={0x34,2,44,1,0};for(int i=0;i<script.Length;i++)GDVars.zone3[address+i]=script[i];EclVm.CallEclScript(address);
  Eq(200,c.effectsAddr.duration,"real VM CLOCK expires effects in255+45 chunks");Eq(5,GDVars.zone0.words[C_Gbvm.CLOCK+3],"real VM CLOCK five hours");Eq(address+5,GDVars.eclIp,"real VM reaches EXIT");
  Effect(c,2);GDVars.effectFns[2]=null;bool missing=false;try{Magic.Effects.CallEffectHandler(2,c,null,false);}catch(NotSupportedException){missing=true;}Yes(missing,"binding does not fake EffHndlr");
 }
}
