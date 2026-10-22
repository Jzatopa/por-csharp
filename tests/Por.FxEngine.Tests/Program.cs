// Separately formulated source-contract expectations; same author as the port, not DOS execution.
using System;
using System.Collections.Generic;
using System.Linq;
using Por.Core;
using static Por.Core.Types;
static partial class Checks
{
 static long checks;static List<string> log=new List<string>();static List<int> handlers=new List<int>();
 static byte B(int n){return unchecked((byte)n);}static sbyte S(int n){return unchecked((sbyte)n);}
 static void Eq(long e,long a,string m){checks++;if(e!=a)throw new Exception(m+": expected "+e+", got "+a);}
 static void Yes(bool v,string m){checks++;if(!v)throw new Exception(m);}
 static void Rig()
 {
  if(GDVars.baseItems==null)GDVars.baseItems=new TBaseItems();if(GDVars.zone0==null)GDVars.zone0=new TGbvmZone0State();if(GDVars.zone1==null)GDVars.zone1=new TGbvmZone1();
  log.Clear();handlers.Clear();GDVars.curChar=null;GDVars.creatureList=null;GDVars.ifcState=0;GDVars.effectLock=0;GDVars.arenaCount=0;GDVars.downedCount=0;GDVars.targetCount=0;GDVars.targetList=new TTargetList();GDVars.arenaPtr=new TArena{cursorSize=1};GDVars.teamCount[0]=GDVars.teamCount[1]=10;GDVars.pendingDamage=0;GDVars.pendingEffectId=0;GDVars.elementMask=0;GDVars.curRow=13;GDVars.gSaveType=99;GDVars.gAttackRoll=0;GDVars.zone1[C_Gbvm.PARTY_SIZE]=0;GDVars.zone1[C_Gbvm.PARTY_HIT_MOD]=GDVars.zone1[C_Gbvm.MONSTER_HIT_MOD]=0;
  for(int i=0;i<7;i++)GDVars.zone0.words[C_Gbvm.CLOCK+i]=0;
  Array.Clear(GDVars.effFlags,0,GDVars.effFlags.Length);
  for(int i=0;i<GDVars.arenaLoc.Length;i++){GDVars.arenaLoc[i]=new TArenaLoc();GDVars.creatureArr[i]=null;}
  for(int i=0;i<GDVars.downed.Length;i++)GDVars.downed[i]=new TDownedRec();
  for(int y=0;y<25;y++)for(int x=0;x<50;x++)GDVars.arenaPtr.cells[y,x]=23;
  for(int i=0;i<GDVars.effectFns.Length;i++){int id=i;GDVars.effectFns[i]=(c,e,r)=>{handlers.Add(id);log.Add("handler:"+id+":"+r);};}
  FxEngine.PrintName=(c,s,row,p)=>log.Add("name:"+s+":"+row+":"+p);
  FxEngine.AnimateName=(c,e,s)=>log.Add("anim:"+e+":"+s);FxEngine.Message=(s,col,first)=>log.Add("msg:"+col+":"+first+":"+s);
  FxEngine.MessageWait=()=>log.Add("wait");FxEngine.ClearMessage=()=>log.Add("clear");FxEngine.BottomLine=s=>log.Add("bottom:"+s);FxEngine.Pause=()=>log.Add("pause");FxEngine.Flush=()=>log.Add("flush");FxEngine.RecalcStats=c=>log.Add("recalc");FxEngine.ItemLabel=(i,inv)=>log.Add("label:"+i.name);FxEngine.InflictDamage=CharHlp.InflictDamage;
  CbtMap.Flush=()=>log.Add("mapflush");CbtMap.CopyTile=(x,y,t,m)=>{};CbtMap.DrawSprite=(x,y,d,f,a)=>{};CbtMap.AutoStop=n=>log.Add("autostop:"+n);CbtMap.Sound=n=>log.Add("sound:"+n);CbtMap.Pause=()=>log.Add("mappause");CbtMap.BuildArenaArrays();
  Dice.RandomBelow=n=>0;
 }
 static TCharRecord Char(int slot=0,int x=3,int y=3,int team=0)
 {
  var c=new TCharRecord{enabled=1,hpCur=100,hpMax=100,status=C_Status.OKAY,strCur=12,conCur=12,hostile=S(team),iconDimensions=1,name="TEST",combatAddr=new TCombatRec()};
  if(slot>0){GDVars.creatureArr[slot]=c;GDVars.arenaLoc[slot]=new TArenaLoc{x=S(x),y=S(y),size=1};GDVars.arenaCount=B(Math.Max(slot,GDVars.arenaCount));}
  return c;
 }
 static TEffectRec Effect(TCharRecord c,int id,int duration=0,int strength=0,bool remove=false){var e=new TEffectRec{id=B(id),duration=unchecked((ushort)duration),strength=B(strength),onRemove=remove};if(c.effectsAddr==null)c.effectsAddr=e;else{var tail=c.effectsAddr;while(tail.next!=null)tail=tail.next;tail.next=e;}return e;}
 static List<TEffectRec> Nodes(TCharRecord c){var nodes=new List<TEffectRec>();for(var e=c.effectsAddr;e!=null;e=e.next){Yes(!nodes.Contains(e),"no effect cycle");nodes.Add(e);}return nodes;}
 static void Dispatch()
 {
  Rig();var c=Char();for(int id=0;id<256;id++){handlers.Clear();FxEngine.CallEffectHandler(B(id),c,null,false);Eq(id>=1&&id<=141?1:0,handlers.Count,"handler id bounds");}
  GDVars.effectFns[1]=null;bool missing=false;try{FxEngine.CallEffectHandler(1,c,null,false);}catch(NotSupportedException){missing=true;}Yes(missing,"missing EffHndlr explicit");
  Rig();c=Char();for(int id=1;id<=141;id++)Effect(c,id);
  string[] expected={"126","63,37,25,71","85,86,87,68,79,80,76,73,77","64,65,66,67,68,69,70,79,85,86,87","29,3,6","41,104,120,101,115,116,119,123,96,94,60,122,117","113,61,122,60,91,10,20,105,106,112,114,118,17,93,101,28","51,52,53,31","99,81,82,89,72,56","105,106,107,108,109,110,111,112,124,125","1,2,33,36,49,3,6,18,26","33,17,8,9,45,46,30","8,9,10,17,20,33,36,45,46,49,61,111,125,90,97","99,100,103,75,74","83,84,88,121","21,30,74,75,11","25,71,37,47,48,89","1,2,11","39,42,58","98,23,72,56,11"};
  for(int cond=0;cond<256;cond++){handlers.Clear();FxEngine.TriggerEffectsWithCondition(B(cond),c);Yes(string.Join(",",handlers)==(cond<20?expected[cond]:""),"condition ordered dispatch "+cond);}
  for(int cond=0;cond<20;cond++)for(int remove=0;remove<2;remove++){Rig();c=Char();var ids=expected[cond].Split(',').Select(int.Parse).ToList();foreach(int id in ids.Distinct())Effect(c,id);if(remove==1)FxEngine.RemoveEffect(c,B(ids[0]),null);handlers.Clear();FxEngine.TriggerEffectsWithCondition(B(cond),c);Yes(handlers.SequenceEqual(ids.Where(id=>remove==0||id!=ids[0])),"dispatch absent effects skipped");}
 }
 static void Lists()
 {
  Rig();var c=Char();var a=Effect(c,1,7,18,true);var b=Effect(c,2,8);var d=Effect(c,1,9);
  object seen=null;GDVars.effectFns[1]=(creature,p,r)=>{seen=p;Yes(r,"remove flag delivered");};FxEngine.RemoveEffect(c,1,new TEffectRec{id=1});Eq(3,Nodes(c).Count,"foreign node ignored");FxEngine.RemoveEffect(c,1,null);Yes(ReferenceEquals(seen,a)&&ReferenceEquals(c.effectsAddr,b),"first matching node removed");FxEngine.RemoveEffect(c,99,d);Yes(b.next==null,"explicit node independent of requested id");FxEngine.RemoveEffect(c,9,null);Eq(1,Nodes(c).Count,"missing id no-op");
  Rig();c=Char();a=Effect(c,1,1,0,true);b=Effect(c,2);d=Effect(c,3);GDVars.effectFns[1]=(cr,p,r)=>{a.onRemove=false;FxEngine.RemoveEffect(cr,1,a);FxEngine.RemoveEffect(cr,2,b);};FxEngine.RemoveEffect(c,1,a);Yes(ReferenceEquals(c.effectsAddr,d)&&d.next==null,"self and sibling removal callback safe");
  Rig();c=Char();a=Effect(c,1,1,0,true);object callbackNode=null;GDVars.effectFns[99]=(cr,p,r)=>{callbackNode=p;handlers.Add(99);};FxEngine.RemoveEffect(c,99,a);Yes(ReferenceEquals(callbackNode,a)&&handlers.Last()==99,"explicit removal dispatches requested id not node id");
  Rig();c=Char();Stats.ResetGame();GDVars.creatureList=c;GDVars.curChar=c;GDVars.zone1[C_Gbvm.PARTY_SIZE]=1;FxEngine.AddEffect(c,C_Effect.POISONED,1,0,false);Eq(1,Stats.game.counts[Stats.POISONED],"poison statistics counted");
  Rig();c=Char();for(int i=0;i<4;i++)Effect(c,C_Effect.INVISIBLE);FxEngine.RemoveInvisibility(c);Yes(c.effectsAddr==null,"all invisibility duplicates removed");
  Rig();c=Char();for(int i=1;i<=141;i++)Effect(c,i);FxEngine.RemoveEffectsHp0(c);foreach(byte id in GDConsts.hpZeroEffects.Skip(1))Yes(!Nodes(c).Any(e=>e.id==id),"hp-zero set removed");FxEngine.RemoveAiEffects(c);Yes(!Nodes(c).Any(e=>e.id==74||e.id==75),"AI hold effects removed");
  Rig();c=Char();Effect(c,1);FxEngine.AddEffect(c,2,65535,255,true);var nodes=Nodes(c);Eq(2,nodes.Count,"append count");Eq(65535,nodes[1].duration,"append word duration");Eq(255,nodes[1].strength,"append strength");Yes(nodes[1].onRemove&&nodes[1].next==null,"append flags tail");Yes(FxEngine.RemoveEffectWithMessage(c,2),"cure existing");Yes(!FxEngine.RemoveEffectWithMessage(c,2),"cure absent");
  Rig();c=Char();foreach(byte id in GDConsts.cureDiseaseEffects.Skip(1))Effect(c,id,1,0,true);Effect(c,1);Yes(FxEngine.HasDisease(c),"disease detection");foreach(byte id in GDConsts.cureDiseaseEffects.Skip(1))GDVars.effectFns[id]=(cr,p,r)=>Eq(1,GDVars.effectLock,"disease lock held during removal");FxEngine.RemoveDiseases(c);Eq(0,GDVars.effectLock,"disease lock released");Yes(!FxEngine.HasDisease(c)&&c.effectsAddr.id==1,"cure disease set only");
 }
 static void Auras()
 {
  foreach(int id in new[]{21,45,46,49})for(int distance=0;distance<9;distance++)for(int combat=0;combat<2;combat++)
  {
   Rig();var carrier=Char(1,3,3);var target=Char(2,3+distance,3);GDVars.creatureList=carrier;carrier.nextCharAddr=target;CbtMap.BuildArenaArrays();GDVars.ifcState=B(combat==1?C_Ifc.COMBAT:0);var effect=Effect(carrier,id);object received=null;GDVars.effectFns[id]=(c,p,r)=>{Yes(ReferenceEquals(c,target),"aura target argument");received=p;};
   for(int i=1;i<=72;i++){GDVars.targetList[i].idx=B(i%3);GDVars.targetList[i].dist=B(i*2);GDVars.targetList[i].dir=B(i+7);}var identities=(TCombatTarget[])GDVars.targetList.Data.Clone();GDVars.targetCount=65;
   FxEngine.TriggerEffect(target,B(id));bool hit=combat==0||distance<=(id==49?6:1);Yes(hit==ReferenceEquals(received,effect),"aura range/team-neutral source behavior");
   for(int i=1;i<=72;i++){Yes(ReferenceEquals(identities[i],GDVars.targetList[i]),"aura target records not aliased");Eq(i%3,GDVars.targetList[i].idx,"aura idx snapshot restored");Eq(B(i*2),GDVars.targetList[i].dist,"aura dist snapshot restored");Eq(B(i+7),GDVars.targetList[i].dir,"aura dir snapshot restored");}
   Eq(combat==0?65:distance<=(id==49?6:1)?2:1,GDVars.targetCount,"source aura leaves rebuilt count");
  }
  Rig();var own=Char();var e=Effect(own,21);object got=null;GDVars.effectFns[21]=(c,p,r)=>got=p;FxEngine.TriggerEffect(own,21);Yes(ReferenceEquals(got,e),"own aura without creature list");
 }
 static void Rolls()
 {
  Rig();var a=Char();var d=Char();a.thac0Cur=30;
  foreach(int team in new[]{0,1})foreach(int mod in new[]{0,1,127,128,255})for(int roll=1;roll<=20;roll++)for(int ac=0;ac<256;ac++)
  {
   a.hostile=S(team);GDVars.zone1[C_Gbvm.PARTY_HIT_MOD]=unchecked((ushort)(0xAB00+mod));GDVars.zone1[C_Gbvm.MONSTER_HIT_MOD]=unchecked((ushort)(0xCD00+(255-mod)));int r=roll;Dice.RandomBelow=n=>r-1;int raw=roll==20?100:roll;
   Eq(roll>1&&raw+30+S(team==0?mod:255-mod)>=ac?1:0,FxEngine.AttackRoll(a,d,B(ac))?1:0,"attack signed modifier/inclusive threshold");d.acCur=B(ac);Eq(roll>1&&raw+30>ac?1:0,FxEngine.AttackRollNoAttacker(d,30)?1:0,"script attack strict threshold");
  }
  for(int roll=1;roll<=20;roll++)foreach(int bonus in new[]{-128,-1,0,1,127})foreach(int mod in new[]{-128,-1,0,1,127})for(int type=-128;type<128;type+=7)
  {
   int r=roll;Dice.RandomBelow=n=>r-1;a.saveBonus=S(bonus);for(int i=0;i<5;i++)a.saves[i]=B(15+i);int effective=type<0||type>4?4:type;bool expected=roll==20||roll!=1&&roll+bonus+mod>=15+effective;Eq(expected?1:0,FxEngine.RollSavingThrow(a,S(type),S(mod))?1:0,"save extremes/type normalization");
  }
  Rig();a=Char();d=Char();Effect(a,1);Effect(d,25);GDVars.effectFns[1]=(c,p,r)=>{log.Add("attacker");GDVars.gAttackRoll=-1;};GDVars.effectFns[25]=(c,p,r)=>log.Add("defender");Dice.RandomBelow=n=>19;Yes(!FxEngine.AttackRoll(a,d,0),"effects can veto natural20");Yes(log.SequenceEqual(new[]{"attacker","defender"}),"attacker then defender order");
  log.Clear();Dice.RandomBelow=n=>0;FxEngine.AttackRoll(a,d,0);Eq(0,log.Count,"natural1 skips handlers");
  Rig();a=Char();GDVars.gSaveType=99;Dice.RandomBelow=n=>19;Yes(FxEngine.RollSavingThrow(a,0,0),"natural20 save");Eq(99,GDVars.gSaveType,"natural20 leaves save type untouched");
 }
 static void Strength()
 {
  Rig();for(int b=0;b<256;b++)for(int e=0;e<256;e++)Eq(B(b==18?e+1:b+100),FxEngine.StrengthModifier(B(b),B(e)),"strength encode byte wrap");
  for(int strength=0;strength<256;strength++){byte b=0,e=0;FxEngine.StrengthEffectToStrength(new TEffectRec{strength=B(strength)},ref b,ref e);int value=strength%128;Eq(value<=101?18:value-100,b,"strength decode base");Eq(value<=101?B(value-1):0,e,"strength decode exceptional");}
  for(int old=1;old<=25;old++)for(int desired=0;desired<32;desired++)foreach(int exc in new[]{0,50,100,255})
  {
   var c=Char();c.strCur=B(old);c.strExc=50;byte effect=0;bool expected=desired>=old&&!(desired==18&&exc<50);Eq(expected?1:0,FxEngine.AdjustForNewStrength(c,B(desired),B(exc),ref effect)?1:0,"new strength acceptance");Eq(expected?Math.Max(3,Math.Min(25,desired)):old,c.strCur,"new strength clamp");Eq(expected?FxEngine.StrengthModifier(B(old),50):B((desired==18?exc+1:desired+100)+128),effect,"strength restoration encoding");
  }
  var p=Char();p.strCur=18;p.strExc=80;var active=Effect(p,C_Effect.STRENGTH,0,110);byte back=0;Yes(FxEngine.AdjustForNewStrength(p,22,0,ref back),"supersede active strength");Eq(10+100,back,"superseded original restore");Eq(81+128,active.strength,"superseded stores previous current with marker");
 }
 static void ImmunityAndHealing()
 {
  Rig();var c=Char();for(int paladin=0;paladin<2;paladin++)for(int id=0;id<256;id++){c.classLevels[C_Level.PALADIN]=S(paladin);Eq(paladin==1&&new[]{34,43,44,50,57}.Contains(id)?1:0,FxEngine.DiseaseImmune(c,B(id))?1:0,"paladin exact disease set");}
  for(int raw=0;raw<256;raw++)foreach(bool saved in new[]{false,true}){Rig();c=Char();FxEngine.AddEffectWithMessage(c,1,5,7,true,B(raw),saved,"");Eq(saved&&raw==1?0:1,Nodes(c).Count,"raw Pascal Boolean equality");}
  foreach(int old in new[]{0,1,5,10})foreach(int duration in new[]{0,1,5,10}){Rig();c=Char();var first=Effect(c,1,old);FxEngine.AddEffectWithMessage(c,1,unchecked((ushort)duration),7,false,false,false,"");bool replace=old>0&&duration>old||duration==0;Eq(replace?1:2,Nodes(c).Count,"effect duration replacement rule");Yes(replace!=ReferenceEquals(first,c.effectsAddr),"duration replacement retains right identity");}
  Rig();c=Char();Effect(c,105);GDVars.effectFns[105]=(cr,e,r)=>GDVars.pendingEffectId=0;FxEngine.AddEffectWithMessage(c,1,5,0,false,false,false,"hi");Eq(1,Nodes(c).Count,"immunity veto prevents append");Yes(log.Last()=="name:is Unaffected:10:True","unaffected message");
  foreach(int status in Enumerable.Range(0,9))foreach(bool bypass in new[]{false,true})foreach(bool rot in new[]{false,true})foreach(int hp in new[]{0,1,127,255})foreach(int before in new[]{0,90,100,150})
  {
   Rig();c=Char();c.status=B(status);c.hpCur=B(before);c.hpMax=100;if(rot)Effect(c,50);bool allowed=new[]{0,1,4,5}.Contains(status)&&!((before>=100||!bypass)&&(rot||bypass));bool got=FxEngine.RestoreHitPoints(c,B(hp),bypass);Eq(allowed?1:0,got?1:0,"healing status/rot/bypass matrix");Eq(allowed?Math.Min(100,before+hp):before,c.hpCur,"healing capped before byte add");
  }
  Rig();c=Char();c.enabled=0;c.hpCur=0;c.status=C_Status.DYING;object wake=new object();GDVars.effectFns[C_Effect.HEAL_WAKE]=(cr,e,r)=>{wake=e;Yes(r,"wake removal flag");};Yes(FxEngine.RestoreHitPoints(c,1,false),"heal downed");Eq(C_Status.UNCONSCIOUS,c.status,"dying becomes unconscious");Yes(wake==null,"wake callback nil data");
 }
 static int Main(string[] args)
 {
  try{Action<string,Action> run=(n,a)=>{if(args.Length==0||args.Contains(n)){long start=checks;a();Console.WriteLine(n+": "+(checks-start)+" assertions PASS");}};run("Dispatch",Dispatch);run("Lists",Lists);run("Auras",Auras);run("Rolls",Rolls);run("Strength",Strength);run("ImmunityHealing",ImmunityAndHealing);run("Timeout",Timeout);run("Damage",Damage);run("Equipment",Equipment);run("HoldsRevive",HoldsRevive);run("Bindings",Bindings);Console.WriteLine("TOTAL "+checks+" assertions PASS");return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}
 }
}
