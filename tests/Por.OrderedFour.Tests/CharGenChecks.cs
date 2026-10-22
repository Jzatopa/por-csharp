using System;using System.Collections.Generic;using System.Linq;using Por.Core;using static Por.Core.Types;
static partial class Tests
{
 sealed class View:ICharGenView{public List<string> Log=new List<string>();public void DisplayChar(TCharRecord c){Log.Add("display");}public void PrintStat(sbyte a,bool h){Log.Add("stat:"+a+":"+h);}public void PrintCombatStats(){Log.Add("combat");}public void PrintMoney(){Log.Add("money");}public void ViewCharMenu(ref bool c){Log.Add("view");}}
 static Queue<Tuple<byte,bool>> keys;static List<string> draw;static List<TImgBuf> pictures;
 static void Key(int code,bool ext=false){keys.Enqueue(Tuple.Create(B(code),ext));}
 static TCharRecord GenRig()
 {
  if(GDVars.zone0==null)GDVars.zone0=new TGbvmZone0State();if(GDVars.zone1==null)GDVars.zone1=new TGbvmZone1();
  var c=new TCharRecord{race=7,charClass=2,genderSp=0,iconSize=2,portraitHead=1,portraitBody=1,iconDimensions=1,hpMax=10,hpCur=7,hpRolled=8,name="TEST",combatAddr=new TCombatRec(),enabled=1};c.classLevels[2]=1;for(int i=0;i<6;i++)c.stats[i]=12;for(int i=1;i<=6;i++)c.iconColors1[i]=B(i*17);c.npcTreasureShare=7;
  GDVars.curChar=c;GDVars.creatureList=null;GDVars.virtualDisk=3;GDVars.ifcState=0;GDVars.zone1[C_Gbvm.PARTY_SIZE]=0;GDVars.horizChoice=3;keys=new Queue<Tuple<byte,bool>>();draw=new List<string>();pictures=new List<TImgBuf>();CharGenView.Host=new View();
  CharGen.Horizontal=(string q,string m,byte a,byte b,byte h,bool r,bool anim,ref bool ext)=>{if(keys.Count==0)throw new Exception("unscripted CharGen input "+q+" / "+m);var k=keys.Dequeue();ext=k.Item2;return k.Item1;};CharGen.YesNo=(q,a,b,h)=>(byte)'Y';
  CharGen.RecalcStats=cr=>{};CharGen.PrintHitPoints=(cr,col,row,hot)=>{};CharGen.ClearBox=(a,b,r,d)=>{};CharGen.DrawBorder=(a,b,r,d,col)=>{};CharGen.DrawWindow=(a,b,r,d,bg,col,s)=>{};CharGen.DisplayString=(x,y,col,s)=>draw.Add(x+":"+y+":"+col+":"+s);CharGen.DisplayCentered=(a,b,y,col,s)=>draw.Add("center:"+y+":"+s);CharGen.ClearScreen=()=>{};CharGen.ClearBottomLine=()=>{};CharGen.PrintAndPause=(s,col)=>draw.Add(s);CharGen.PortraitCount=body=>(byte)(body?21:41);CharGen.DisplayImage=(x,y,flag,p)=>{};CharGen.ShowPic=(x,y,p)=>{};
  VidVga.Install();GDVars.video.hasPalette=false;GDVars.picBytes=0;
  CharGen.LoadPic=(string name,byte id,bool mask,byte trans,ref TImgBuf pic)=>{Blit.FreePic(ref pic);Blit.AllocPic((ushort)(name=="CHEAD"?8:24),3,1,1,ref pic);for(int i=0;i<pic.data.Length;i++)pic.data[i]=B((id+i)%16);for(int i=0;i<8;i++)pic.map[i]=B(2*i+(2*i+1)*16);pictures.Add(pic);};
  CharGen.DisplayParty=cr=>{};CharGen.DisplayCreatureName=(cr,x,y,b)=>{};CharGen.PrintBottomLine=s=>draw.Add(s);CharGen.DisplayAndPause=(col,s)=>draw.Add(s);CharGen.LoadCharacterIcon=b=>{};
  Dice.RandomBelow=n=>n==0?0:n-1;CharGen.SaveCharCall=(cr,s)=>{};CharGen.FreeCreatureCall=SaveLoad.FreeCreature;return c;
 }
 static void CharGenModify()
 {
  GenRig();for(int race=-128;race<128;race++)for(int cls=-128;cls<128;cls++){GDVars.curChar.race=unchecked((sbyte)race);GDVars.curChar.charClass=unchecked((sbyte)cls);Eq(race>=1&&race<=7&&cls>=0&&cls<=16?1:0,CharGen.Modifiable()?1:0,"modifiable exhaustive race/class");}
  for(int race=1;race<=7;race++)for(int ability=0;ability<6;ability++)foreach(bool levelled in new[]{false,true})foreach(bool keep in new[]{false,true})foreach(int direction in new[]{75,77})
  {
   var c=GenRig();c.race=unchecked((sbyte)race);byte before=c.stats[ability];int hp=c.hpMax,cur=c.hpCur;for(int i=0;i<ability;i++)Key(80,true);Key(direction,true);Key(keep?'K':'E');CharGen.ModifyCharacter(levelled);
   int minimum=Math.Max(ClassCal.RaceAbilityMin(c.race,c.genderSp,unchecked((sbyte)ability)),GDConsts.classMin[c.charClass].values[ability]),maximum=ClassCal.RaceAbilityMax(c.race,c.genderSp,unchecked((sbyte)ability));int expected=!keep?before:direction==75?Math.Max(before-1,minimum):Math.Min(before+1,maximum);Eq(expected,c.stats[ability],"modify selected stat/keep/undo");for(int i=0;i<6;i++)if(i!=ability)Eq(12,c.stats[i],"modify leaves other abilities");if(!keep){Eq(hp,c.hpMax,"undo maximum HP");Eq(levelled?cur:hp,c.hpCur,"undo levelled damage");}Eq(keep&&!levelled?1:7,c.npcTreasureShare,"modify treasure-share rule");
  }
  var ch=GenRig();ch.clericSpells[0]=99;ch.clericSpells[1]=2;ch.clericSpells[2]=3;ch.clericSpells[3]=4;Key(80,true);Key(80,true);Key(77,true);Key('E');CharGen.ModifyCharacter(true);Eq(99,ch.clericSpells[0],"cleric padding untouched");Eq(2,ch.clericSpells[1],"cleric slot1 restored");Eq(3,ch.clericSpells[2],"cleric slot2 restored");Eq(4,ch.clericSpells[3],"cleric slot3 restored");
  ch=GenRig();ch.race=0;CharGen.ModifyCharacter(false);Eq(1,draw.Count,"unmodifiable returns before input");
 }
 static void CharGenPickers()
 {
  foreach(bool undo in new[]{false,true})foreach(int moves in new[]{0,1,13,14,15,31,32,33})
  {
   var c=GenRig();c.iconHead=0;var colors=Enumerable.Range(1,6).Select(i=>c.iconColors1[i]).ToArray();int baseline=GDVars.picBytes;Key(13);for(int i=0;i<moves;i++)Key(77,true);Key(13);Key(undo?'E':'K');CharGen.CustomizeIcon();Eq(undo?0:moves%14,c.iconHead,"icon grid head wrap and undo");Eq(2,c.iconSize,"icon other fields preserved");for(int i=1;i<=6;i++)Eq(colors[i-1],c.iconColors1[i],"icon colors copied by value");Eq(baseline,GDVars.picBytes,"icon buffers all freed");Eq(3,GDVars.horizChoice,"icon horizontal choice restored");Eq(0,keys.Count,"icon consumes scripted keys only");
  }
  foreach(int moves in new[]{0,1,11,12,19,20,31,32,35}){var c=GenRig();Key(80,true);Key(13);for(int i=0;i<moves;i++)Key(77,true);Key(13);Key('K');CharGen.CustomizeIcon();Eq(moves%32,c.iconBody,"weapon multiple-page grid wrapping");Eq(0,GDVars.picBytes,"weapon buffers freed");}
  for(int part=3;part<=9;part++)foreach(bool second in new[]{false,true})
  {
   var c=GenRig();int[] slots={6,1,4,4,5,2,3},nibble={2,2,0,1,2,2,2};int which=nibble[part-3]==2?(second?1:0):nibble[part-3];int slot=slots[part-3],old=c.iconColors1[slot],value=which==0?old%16:old/16;for(int i=0;i<part;i++)Key(80,true);Key(13);if(second)Key(' ');Key(77,true);Key(13);Key('K');CharGen.CustomizeIcon();int expected=which==0?(old&240)|((value+1)%16):(old&15)|(((value+1)%16)*16);Eq(expected,c.iconColors1[slot],"color nibble option/space selection");
  }
  foreach(int moves in new[]{0,1,40,41,42}){var c=GenRig();c.portraitHead=0;for(int i=0;i<moves;i++)Key('N');Key('B');Key('P');Key('K');CharGen.CustomizePortrait();Eq(1+moves%41,c.portraitHead,"portrait head normalization/wrap");Eq(21,c.portraitBody,"portrait body previous wraps");Eq(0,GDVars.picBytes,"portrait six buffers freed");}
 }
 static void CharGenPixels()
 {
  var c=GenRig();byte[] ready=null;var originals=new List<Tuple<TImgBuf,byte[]>>();
  CharGen.LoadPic=(string name,byte id,bool mask,byte trans,ref TImgBuf p)=>{Blit.FreePic(ref p);Blit.AllocPic((ushort)(name=="CHEAD"?8:24),3,1,1,ref p);for(int i=0;i<p.data.Length;i++)p.data[i]=B((id+(name=="CHEAD"?2:1)*i)%16);for(int i=0;i<8;i++)p.map[i]=B(2*i+(2*i+1)*16);originals.Add(Tuple.Create(p,(byte[])p.data.Clone()));};
  CharGen.ShowPic=(x,y,p)=>{if(x==3&&y==3)ready=(byte[])p.data.Clone();};Key('K');CharGen.CustomizeIcon();Yes(ready!=null,"actual icon preview captured");var mapping=Enumerable.Range(0,16).ToArray();for(int i=1;i<=6;i++){mapping[GDConsts.defIconCol[i]&15]=c.iconColors1[i]&15;mapping[(GDConsts.defIconCol[i]+8)&15]=c.iconColors1[i]>>4;}
  for(int i=0;i<576;i++)Eq(mapping[(i<8*24?2*i:i)%16],ready[i],"actual merged/recoloured preview pixels");foreach(var item in originals){Yes(item.Item2.SequenceEqual(item.Item1.data),"icon loaded atlas immutable");for(int i=0;i<8;i++)Eq(B(2*i+(2*i+1)*16),item.Item1.map[i],"icon inline source map not aliased");}Eq(0,GDVars.picBytes,"pixel fixture all buffers freed");
 }
 static void CharGenMenus()
 {
  var c=GenRig();GDVars.creatureList=c;Key('B');CharGen.MainMenu();Eq(C_Ifc.INDOORS,GDVars.ifcState,"main Begin leaves main-menu state");
  for(int race=1;race<=7;race++)for(int cls=0;cls<8;cls++)for(int level=1;level<=8;level++)for(int strength=16;strength<=18;strength++)
  {
   c=GenRig();c.race=unchecked((sbyte)race);c.strCur=B(strength);Array.Clear(c.classLevels,0,8);c.classLevels[cls]=unchecked((sbyte)level);c.coins[3]=1000;c.experience=GDConsts.dsExperienceSpellsClass[cls].xp[level+1];GDVars.zone1[C_Gbvm.TRAIN_CLASS_MASK]=255;int asked=0;CharGen.YesNo=(q,a,b,h)=>{asked++;return (byte)'N';};
   bool capped=false;
   if(cls==2)capped=race==1&&(level==8&&strength==17||level==7&&strength<17)||race==2&&(level==7||level==6&&strength==17||level==5&&strength<17)||race==C_Race.GNOME&&(level==6||level==5&&strength<18)||race==C_Race.HALFELF&&(level==8||level==7&&strength==17||level==6&&strength<17)||race==5&&(level==6||level==5&&strength==17||level==4&&strength<17);
   if(race==C_Race.HALFELF&&cls==0&&level==5)capped=true;if(race==C_Race.HALFELF&&cls==4&&(level==8||level==7&&strength==17||level==6&&strength<17))capped=true;
   CharGen.TrainingMenu();Eq(!capped&&c.experience>0?1:0,asked,"training racial cap/XP checks before confirmation "+race+","+cls+","+level+","+strength);
  }
  foreach(bool evil in new[]{false,true})foreach(bool paladin in new[]{false,true})foreach(int size in new[]{0,1,5,6,7,8})
  {
   c=GenRig();GDVars.zone1[C_Gbvm.PARTY_SIZE]=unchecked((ushort)size);TCharRecord tail=null;for(int i=0;i<size;i++){var member=new TCharRecord{name="OLD"+i,randByte=B(i),combatAddr=new TCombatRec()};if(i==0&&evil)member.alignment=2;if(tail==null)GDVars.creatureList=member;else tail.nextCharAddr=member;tail=member;}
   var entry=new TItemRec{name="NEW"};CharGen.LoadRoster=(ref TItemRec list)=>list=entry;string marked="";CharGen.SaveRoster=list=>marked=list.name;CharGen.LoadCharacter=(name,cr,flag,noprompt)=>{cr.name="NEW";cr.randByte=99;if(paladin)cr.classLevels[3]=1;return true;};int n=0;CharGen.Vertical=(string q,string m,byte a,byte b,byte h,sbyte x,sbyte y,sbyte r,sbyte bottom,TItemRec list,bool exit,ref bool redraw,ref short index,ref TItemRec selected)=>{selected=entry;return B(n++==0?13:'E');};
   CharGen.AddCharacterMenu();bool accepted=size==0||size<6&&!(evil&&paladin);Eq(accepted?size+1:size,GDVars.zone1[C_Gbvm.PARTY_SIZE],"party capacity and paladin/evil restriction");Yes((marked.StartsWith("* "))==accepted,"roster mark reflects acceptance");
  }
 }
 static void CharGenCreateTrain()
 {
  for(int racePick=1;racePick<=6;racePick++)
  {
   GenRig();TCharRecord saved=null;CharGen.SaveCharCall=(c,n)=>saved=c;CharGen.FreeCreatureCall=c=>{};
   CharGen.Vertical=(string q,string m,byte a,byte b,byte h,sbyte x,sbyte y,sbyte r,sbyte bottom,TItemRec list,bool exit,ref bool redraw,ref short index,ref TItemRec selected)=>{index=(short)(list.name=="Pick Race"?racePick:1);selected=MenuCore.FindListMember(index,list);return 13;};
   // Confirm rolled stats, then portrait and icon. Name input is a real TextRend boundary, set below.
   Key('K');Key('E');CharGen.ReadName=(q,col,len)=>"NEW HERO";
   CharGen.CreateCharacterMenu();Yes(saved!=null,"creation reaches save");Eq(racePick==6?7:racePick,saved.race,"race pick omits half-orc");Eq(1,saved.enabled,"new character enabled");Eq(1,saved.levelHighest,"new first level");Eq(1,saved.iconDimensions,"new single-cell footprint");Eq(100,saved.hpCur*100/saved.hpMax,"creation full HP");Yes(saved.name=="NEW HERO","creation name");for(int i=0;i<6;i++)Yes(saved.stats[i]>=GDConsts.classMin[saved.charClass].values[i],"creation class minimum");
  }
  foreach(int cls in new[]{0,2,5,7})foreach(bool restore in new[]{false,true})
  {
   var c=GenRig();Array.Clear(c.classLevels,0,c.classLevels.Length);c.classLevels[cls]=1;c.drainedLevels=2;c.drainedHps=10;c.hpMax=10;c.hpCur=7;c.hpRolled=8;int calls=0;CharGen.SpellMenu=(byte mode,byte cl,ref short index,ref bool notEmpty)=>{calls++;notEmpty=false;return 0;};CharGen.GainLevels(GDConsts.classBit[cls],restore);Eq(2,c.classLevels[cls],"gain selected class");Eq(restore?1:2,c.drainedLevels,"restore drain count");Eq(restore?5:10,c.drainedHps,"restore proportional drained HP");Eq(3,c.hpMax-c.hpCur,"gain levels retains damage");Eq(cls==5?1:0,calls,"new mage level requests spell");
  }
 }
}
