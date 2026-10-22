// Self-authored independent source models for the direct ten-D batch.
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Por.Core;
class RestHost : IRestServices,IExploreServices
{
 public List<string> Log=new List<string>();public bool Healed;public Action<byte,sbyte> OnAdvance;
 public bool RestoreHitPoints(Types.TCharRecord c,byte hp,bool message){Log.Add("heal:"+c.name);return Healed;}
 public void AdvanceTime(byte amount,sbyte unit){Log.Add("time:"+amount+":"+unit);if(OnAdvance!=null)OnAdvance(amount,unit);}
 public void PrintCastSpellMsg(Types.TCharRecord c,string m,byte spell){Log.Add(m+":"+spell);}
 public void RemoveSpellFromScroll(Types.TCharRecord c,Types.TItemRec item,byte spell){for(int i=1;i<=3;i++)if((item.spells[i]&127)==spell){item.spells[i]=0;break;}if(item.spells[1]==0&&item.spells[2]==0&&item.spells[3]==0)CharHlp.RemoveItem(c,item);}
 public void NoncombatCastMenu(){Log.Add("cast");}public void ViewCharacter(ref bool done){Log.Add("view");done=true;}
}
class DspHost : ISoundBlasterHost
{
 public List<string> Writes=new List<string>();public List<int> Reads=new List<int>();public string Env="";public ushort Base=0x220;public bool Busy,Present=true;public int ReadyAfter;public int polls;
 public Queue<int> Addresses=new Queue<int>();public int Frees;public int Available=65535;
 public byte ReadPort(ushort p){Reads.Add(p);if(p==Base+12)return (byte)(Busy?128:0);if(p==Base+14)return (byte)(Present&&polls++>=ReadyAfter?128:0);if(p==Base+10)return 0xAA;return 0;}
 public void WritePort(ushort p,byte v){Writes.Add(p+"="+v);}public string BlasterEnvironment{get{return Env;}}public int MaxAvailable{get{return Available;}}
 public SoundDmaBlock Allocate(ushort size){return new SoundDmaBlock{Data=new byte[size],PhysicalAddress=Addresses.Count>0?Addresses.Dequeue():0x20000};}public void Free(SoundDmaBlock b){Frees++;}
}
class OptionInfo : IOptionsInfo
{
 public int AvailableBytes{get{return 234567;}}public int LargestBlock{get{return 123456;}}public string SecondArgument{get;set;}public string BuildCommit{get{return "7616920a";}}
}
class InitServices : IGameInitServices
{
 public List<string> Log=new List<string>();public Types.TSpellProc SpellHandler(string name){Log.Add("resolve:"+name);return ()=>Log.Add("spell:"+name);}
 public Types.TEffectHandler EffectHandler(string name){Log.Add("resolve:"+name);return (c,e,b)=>Log.Add("effect:"+name);}
 public Types.TCastUI CastTargetMenu{get{return (byte s,bool b,ref bool d)=>{Log.Add("target");};}}
 public void InstallLightningTracer(){Log.Add("bolt");}public void SeedRandom(bool fixedSeed){Log.Add("seed:"+fixedSeed);}
}
static partial class Tests
{
 static void DSetup(){ResetWorld();CdSetup();GDVars.zone1=new Types.TGbvmZone1();GDVars.zone2=new Types.TZone2();GDVars.zone3=new Types.TZone3();GDVars.map3d=new Types.TGeoMap();GDVars.curChar=Ch();GDVars.creatureList=null;GDVars.zone1[C_Gbvm.PARTY_SIZE]=0;GDVars.stingArg=false;GDVars.duelFlag=false;GDVars.soundMode=C_Sound.MODE_DISABLED;}
 static void DRest()
 {
  var host=new RestHost();GameTime.Services=host;
  // Independent mixed-radix borrow model for normalized clocks; all minute/hour boundaries and a sample of days.
  foreach(int days in new[]{0,1,29,99})for(int hours=0;hours<24;hours++)for(int mins=0;mins<60;mins++)foreach(byte amount in new byte[]{0,1,5,59,255})
  {
   GDVars.dsRestingTimeGbvm=new Types.TClock();var t=GDVars.dsRestingTimeGbvm;t[4]=(ushort)days;t[3]=(ushort)hours;t[2]=(ushort)(mins/10);t[1]=(ushort)(mins%10);
   int expected=Math.Max(0,days*1440+hours*60+mins-amount);GameTime.DecrementRestTime(amount,1);
   Eq(expected/1440,t[4],"rest days");Eq(expected/60%24,t[3],"rest hours");Eq(expected/10%6,t[2],"rest turns");Eq(expected%10,t[1],"rest minutes");
  }
  DSetup();GDVars.dsRestingTimeGbvm=new Types.TClock();GDVars.dsRestingTimeGbvm[4]=29;GDVars.dsRestingTimeGbvm[5]=4;GameTime.ValidateRestTime();Eq(99,GDVars.dsRestingTimeGbvm[4],"rest cap");Eq(0,GDVars.dsRestingTimeGbvm[5],"months folded");
  for(int raw=0;raw<256;raw++)foreach(bool initial in new[]{false,true})
  {
   var c=Ch();c.memorizedSpells[0]=(byte)raw;c.memorizedSpells[1]=131;bool done=initial;host.Log.Clear();byte level=GameTime.CheckMemorizeDone(c,ref done);
   bool valid=raw>127&&(raw&127)>=1&&(raw&127)<=C_Spell.LAST;
   if(initial){Eq(valid?GDConsts.spellTable[raw&127].level:GDConsts.spellTable[3].level,level,"pending lookahead");Eq(raw,c.memorizedSpells[0],"lookahead does not finish");}
   else{Eq(valid?raw-128:raw,c.memorizedSpells[0],"first pending completed");Yes(done,"completion sets done");Eq(1,host.Log.Count,"one message");}
  }
  DSetup();GDVars.baseItems[1].slot=C_Item.SLOT_MAGE_SCROLL;var cr=Ch();var scroll=new Types.TItemRec{itemType=1};scroll.spells[1]=129;scroll.spells[2]=130;cr.itemsAddr=scroll;bool complete=false;host.Log.Clear();byte lev=GameTime.CheckScribeDone(cr,ref complete);Yes(complete&&cr.spellsKnown[1]==1&&scroll.spells[1]==0,"scribe finishes first");Eq(GDConsts.spellTable[2].level,lev,"scribe returns next level");
  foreach(int start in new[]{0,1,2,255}){DSetup();var c=Ch();GDVars.creatureList=c;c.memorizedSpells[0]=131;GameTime.prepTimers[1]=(byte)start;GameTime.AdvanceSpellPrep();Eq(start==0?255:start==1?0:start-1,GameTime.prepTimers[1],"byte prep wrap/reload");Eq(start==1?3:131,c.memorizedSpells[0],"only zero crossing completes");}
  DSetup();var head=Ch();head.spellPrepTime=1;head.memorizedSpells[0]=131;GDVars.creatureList=head;byte tick=11;GameTime.AdvanceSpellPrepBase(ref tick);Eq(0,tick,"hour tick wraps");Eq(GDConsts.spellTable[3].level*3,GameTime.prepTimers[1],"base expiry seeds level");Eq(131,head.memorizedSpells[0],"base lookup not completion");
  DSetup();GameTime.Services=host;host.Log.Clear();GDVars.dsRestingTimeGbvm=new Types.TClock();GDVars.dsRestingTimeGbvm[1]=7;GDVars.healTicks=0;GDVars.zone1[C_Gbvm.REST_SAFE_TIME]=1;GDVars.zone1[C_Gbvm.REST_INTERRUPT_PCT]=100;Dice.RandomBelow=n=>0;bool interrupted=GameTime.CampRest(false);Yes(interrupted,"random rest interruption");Eq(1,host.Log.Count(s=>s.StartsWith("time:")),"one five-minute tick");Eq(2,GDVars.dsRestingTimeGbvm[1],"partial duration retained");Eq(0,GDVars.dsSuppressDrawingBorderFlag,"border suppression restored");
 }
 static void DSound()
 {
  var h=new DspHost();SndSb.Host=h;SndSb.ResetForTests();
  foreach(var test in new[]{new[]{"","544","1"},new[]{"A240 I7 D3 T3","576","3"},new[]{"a330 d0","816","0"},new[]{"Azz D4","544","1"},new[]{"AFFF0 D2","65520","2"}}){h.Env=test[0];SndSb.ReadBlaster();Eq(int.Parse(test[1]),SndSb.deviceBase,"BLASTER base");Eq(int.Parse(test[2]),SndSb.dma,"BLASTER DMA");}
  h.Env="";SndSb.ReadBlaster();h.Reads.Clear();Yes(SndSb.DspReset(),"DSP AA detection");Eq(10,h.Reads.Count,"8 reset waits + ready/read");EqS("550=1|550=0",string.Join("|",h.Writes),"reset pulse");h.Present=false;h.Reads.Clear();Yes(!SndSb.DspReset(),"absent DSP");Eq(2008,h.Reads.Count,"bounded reset polls");h.Busy=true;h.Reads.Clear();Yes(!SndSb.DspWrite(99),"write times out");Eq(2000,h.Reads.Count,"bounded write polls");h.Busy=false;
  foreach(int low in new[]{0,1,65520,65535})foreach(int len in new[]{0,1,16,17,65535})Yes(SndSb.Crosses(new SoundDmaBlock{PhysicalAddress=0x30000+low},0,(ushort)len)==((long)low+len>65536),"DMA boundary");
  h.Addresses.Enqueue(0x1FFF0);h.Addresses.Enqueue(0x2FFF0);h.Addresses.Enqueue(0x30000);var block=SndSb.PlaceBlock(32);Eq(0x30000,block.PhysicalAddress,"bounce tries next page");Eq(2,h.Frees,"crossing blocks held then freed");
  // Small independent sample container: header plus two records; second entry is malformed.
  byte[] data=new byte[40];Encoding.ASCII.GetBytes("SBFX").CopyTo(data,0);data[4]=2;data[5]=2;data[6]=1;data[7]=1;data[8]=177;data[9]=4;data[11]=20;data[13]=2;data[14]=9;data[16]=2;data[18]=38;data[20]=11;data[21]=22;data[22]=33;data[23]=44;
  SndSb.OpenEffects=n=>new MemoryStream(data);SndSb.LoadEffects();Yes(SndSb.store!=null&&SndSb.effects[1].Start==20&&SndSb.effects[2].Start==0,"sample validation");
  SndSb.deviceBase=0x220;SndSb.dma=1;h.Writes.Clear();h.Busy=false;SndSb.store.PhysicalAddress=0x2FFEA;SndSb.bounce=new SoundDmaBlock{Data=new byte[4],PhysicalAddress=0x40020};Yes(SndSb.PlayEffect(SndSb.effects[1]),"sample DMA");Yes(SndSb.bounce.Data.SequenceEqual(new byte[]{11,22,33,44}),"crossing bytes copied");
  EqS("556=208|10=5|12=0|11=73|2=32|2=0|131=4|3=3|3=0|10=1|556=64|556=177|556=117|556=3|556=0",string.Join("|",h.Writes),"DMA/DSP ordered protocol");
  SndSb.bounce=null;Yes(!SndSb.PlayEffect(SndSb.effects[1]),"crossing without bounce falls back");SndSb.store.PhysicalAddress=0x20000;h.Busy=true;Yes(SndSb.PlayEffect(SndSb.effects[1]),"source returns success despite DSP timeout");h.Busy=false;
  for(int len=0;len<20;len++){SndSb.ResetForTests();SndSb.OpenEffects=n=>new MemoryStream(new byte[len]);SndSb.LoadEffects();Yes(SndSb.store==null,"short/bad header ignored");}
  SoundSetup();SndSb.Host=h;h.Present=true;int reads=0;SndOpl.ReadPort=p=>(byte)(reads++==0?0:192);SndSb.Install();EqS("Sound Blaster",GDVars.sound.name,"SB install");Yes(GDVars.sound.singleStep,"one step sound");h.Writes.Clear();SndSb.ExitIfInstalled();EqS("556=208",string.Join("|",h.Writes),"exit stops DMA");SndSb.ResetForTests();
 }
 static void DOptions()
 {
  DSetup();Options.Info=new OptionInfo{SecondArgument="NORAND"};GDVars.sound.mode=C_Sound.MODE_DISABLED;string dir=Path.Combine(Path.GetTempPath(),"por-options-"+Guid.NewGuid().ToString("N"))+Path.DirectorySeparatorChar;Directory.CreateDirectory(dir);GDVars.dsSaveDirectory=dir;
  File.WriteAllText(dir+"POOLOPT.CFG"," speed = 9\nMONSTERS=off\nFUTURE=Keep me\nMUSIC=on\nSOUND=0\nPORTRAITS=no\nQUICKMAGIC=1\nCOMPANION=1\n");GDVars.portraitsOn=1;Options.LoadOptions();Eq(9,GDVars.gameDelay,"speed parsed");Eq(0,GDVars.monsterPics,"flag off");Eq(1,GDVars.portraitsOn,"invalid value keeps default");Eq(1,GDVars.quickMagicOn,"numeric on");Eq(1,GDVars.companionOn,"companion on");Options.SaveOptions();var lines=File.ReadAllLines(dir+"POOLOPT.CFG");Eq(9,lines.Length,"eight settings plus unknown");EqS("FUTURE=Keep me",lines[8],"unknown line retained");Yes(!File.Exists(dir+"POOLOPT.$$$"),"temporary gone after replace");
  foreach(string v in new[]{"-1","10","a","","ON"}){GDVars.gameDelay=5;Options.ReadValue(1,v);Eq(5,GDVars.gameDelay,"invalid speed");}
  for(int v=0;v<256;v++){GDVars.monsterPics=(byte)v;Options.ChangeSetting(2);Eq(v==0?1:0,GDVars.monsterPics,"noncanonical flag toggle");}
  foreach(int v in new[]{0,1,999,1000,1234567,int.MinValue}){string x=v.ToString(System.Globalization.CultureInfo.InvariantCulture);for(int i=x.Length-3;i>0;i-=3)x=x.Insert(i,",");EqS(x,Options.Grouped(v),"grouped source sign quirk");}
  GDVars.stingArg=true;EqS("STING ",Options.NoteOnRow(9),"bottom note");EqS("NORAND ",Options.NoteOnRow(8),"next note");EqS("",Options.NoteOnRow(7),"no third note");
  var longlines=Enumerable.Repeat("UNKNOWN=X",100).Concat(new[]{"SPEED=2"});File.WriteAllLines(dir+"POOLOPT.CFG",longlines);GDVars.gameDelay=7;Options.LoadOptions();Eq(7,GDVars.gameDelay,"100-line cap");
  GDVars.stingArg=false;GDVars.horizChoice=4;smio.Keys.Enqueue(13);smio.Keys.Enqueue((byte)'E');Options.OptionsMenu();Eq(8,GDVars.gameDelay,"menu Enter slower");Eq(4,GDVars.horizChoice,"selection restored");
 }
}
static partial class Tests
{
 static void DInit()
 {
  DSetup();var h=new InitServices();GameInit.Services=h;GameInit.InitSpellFns();GameInit.InitEffectHandlers();h.Log.Clear();
  foreach(string line in File.ReadAllLines("evidence/easiest-ten-d-2026-10-21/handler-oracle.tsv"))
  {
   var p=line.Split('\t');int slot=p[1].StartsWith("C_Effect.")?(int)typeof(C_Effect).GetField(p[1].Substring(9)).GetRawConstantValue():int.Parse(p[1]);
   if(p[0]=="spellFns"){GDVars.spellFns[slot]();EqS("spell:"+p[3],h.Log.Last(),"spell slot "+slot);}
   else{GDVars.effectFns[slot](null,null,false);EqS("effect:"+p[2]+"."+p[3],h.Log.Last(),"effect slot "+slot);}
  }
  var pic=new Types.TImgBuf();GDVars.ds2dMapTilePtr=pic;GDVars.gameDelay=8;GDVars.companionOn=1;GDVars.zone0.bytes[0]=99;GDVars.zone1.Data[0]=17;GDVars.zone1[C_Gbvm.PARTY_SIZE]=3;GDVars.zone2.Data[0]=19;GDVars.zone2[0x9700]=30;GDVars.zone3.Data[0]=23;GDVars.zone3[C_Gbvm.SCRIPT_FIRST]=25;
  GameInit.ResetGameState();Eq(0,GDVars.zone0.bytes.Sum(x=>(int)x),"zone0 clear");Eq(17,GDVars.zone1.Data[0],"zone1 padding untouched");Eq(19,GDVars.zone2.Data[0],"zone2 padding untouched");Eq(23,GDVars.zone3.Data[0],"script padding untouched");Eq(0,GDVars.zone2[0x9700],"zone2 window cleared");Eq(0,GDVars.zone3[C_Gbvm.SCRIPT_FIRST],"script window cleared");Eq(15,GDVars.gMapX,"reset position");Eq(C_Dir.WEST,GDVars.gFacing,"reset facing");Yes(GDVars.doorOpen.All(x=>x==1),"doors initialized");Yes(ReferenceEquals(pic,GDVars.ds2dMapTilePtr)&&GDVars.gameDelay==8&&GDVars.companionOn==1,"reset leaves pictures/settings");
  byte[] data=new byte[2+128*16];for(int i=2;i<data.Length;i++)data[i]=(byte)((i-2)%256);GameInit.ReadItems(new MemoryStream(data));for(int i=0;i<128;i++){Eq(i*16%256,GDVars.baseItems[i].slot,"item slot");Eq(unchecked((sbyte)((i*16+11)%256)),GDVars.baseItems[i].dmgSmMod,"signed damage");}Eq(0,GDVars.baseItems[128].props,"overflow missing remains zero");
  DSetup();VidVga.Install();CdSetup();GDVars.soundMode=C_Sound.MODE_DISABLED;GDVars.gameDir="GAME/";var log=new List<string>();GameInit.LoadFont=()=>{Yes(GDVars.gameDir=="","resource path cleared");log.Add("font");};GameInit.LoadSymbols=(id,slot)=>log.Add("symbol:"+id+":"+slot);GameInit.LoadPic=(string n,byte id,bool mask,byte trans,ref Types.TImgBuf b)=>{log.Add("pic:"+n);b=new Types.TImgBuf();};GameInit.LoadIcon=(n,id,slot)=>log.Add("icon:"+id+":"+slot);GameInit.OpenItems=()=>new MemoryStream(data);GameInit.LoadOptionsCall=()=>{Yes(GDVars.gameDir=="GAME/","path restored before options");log.Add("options");};h.Log.Clear();GameInit.GameInit2("NORAND");
  EqS("font|symbol:202:4|symbol:203:0|pic:sqrpaci|"+string.Join("|",Enumerable.Range(0,12).Select(i=>"icon:"+i+":"+(i+13)))+"|icon:25:25|options",string.Join("|",log),"resource load order");Yes(h.Log.Contains("seed:True")&&h.Log.Last()=="bolt","seed before bindings/tracer last");Eq(168,GDVars.dsViewImageBufferPtr.height,"view buffer height");Yes(GDVars.spanFirstOfs.All(x=>x==65535),"span starts FFFF");Eq(1,GDVars.portraitsOn,"default settings");Eq(1,GDVars.vertTop,"reset state after allocate");Yes(GDVars.gbStrings.Data.All(x=>x==""),"strings cleared");
  GameInit.Services=null;bool missing=false;try{GameInit.InitSpellFns();}catch(NotSupportedException){missing=true;}Yes(missing,"unbound real spell handlers rejected");
 }
 static void DCharEdit()
 {
  DSetup();for(int status=0;status<16;status++)foreach(int hp in new[]{0,1,7,255})
  {
   GDVars.curChar=Ch();GDVars.curChar.status=(byte)status;GDVars.curChar.hpCur=(byte)hp;GDVars.curChar.hpMax=12;CharEdit.HealthAction((byte)'H');Eq(12,GDVars.curChar.hpCur,"heal hp");Eq(status==C_Status.UNCONSCIOUS||status==C_Status.DYING?C_Status.OKAY:status,GDVars.curChar.status,"heal status limited");
   GDVars.curChar.status=(byte)status;GDVars.curChar.hpCur=(byte)hp;CharEdit.HealthAction((byte)'S');int next=status==C_Status.OKAY?C_Status.UNCONSCIOUS:status==C_Status.UNCONSCIOUS?C_Status.DYING:status==C_Status.DYING?C_Status.DEAD:status==C_Status.DEAD?C_Status.STONED:status==C_Status.STONED?C_Status.GONE:C_Status.OKAY;
   Eq(next,GDVars.curChar.status,"status cycle");Eq(next==C_Status.OKAY?Math.Max(1,hp):0,GDVars.curChar.hpCur,"status hp invariant");
  }
  var c=Ch();c.classLevels[0]=1;c.classLevels[2]=3;c.classLevels[7]=0;GDVars.curChar=c;sbyte[] held=new sbyte[8];Eq(2,CharEdit.HeldClasses(held),"held classes count");Eq(0,held[0],"first held");Eq(2,held[1],"second held");
  foreach(sbyte cls in new sbyte[]{C_Level.FIGHTER,C_Level.CLERIC,C_Level.MAGE})foreach(byte con in new byte[]{3,10,15,18,25})foreach(byte count in new byte[]{1,2,3})
  {
   c=Ch();GDVars.curChar=c;c.classLevels[cls]=2;c.conCur=con;c.hpMax=30;c.hpCur=21;c.hpRolled=20;c.levelHighest=4;
   int avg=GDConsts.hpSides[cls]/2+1,loss=Math.Max(1,(avg+ClassCal.ConHpPerDie(cls,con))/count);int expected=30-loss;
   CharEdit.LowerLevel(cls,count);Eq(1,c.classLevels[cls],"lower one level");Eq(expected,c.hpMax,"average hp loss");Eq(Math.Max(0,expected-9),c.hpCur,"damage preserved");Eq(1,c.levelHighest,"level recalculated");if(cls==C_Level.MAGE)Eq(1,c.mageSpells[1],"first mage slot");
  }
  foreach(string str in new[]{""," ","1x","2147483648"}){int n;Yes(!CharEdit.Parse(str,out n),"invalid xp/count parse");}int value;Yes(CharEdit.Parse("+123",out value)&&value==123,"signed decimal parse");Yes(CharEdit.Parse("$123",out value)&&value==291,"Pascal hexadecimal Val");
  GDVars.curChar=null;CharEdit.Services=null;CharEdit.EditMenu();Yes(true,"nil editor no host required");
 }
}
static partial class Tests
{
 static void DSave()
 {
  DSetup();var random=new Random(7616920);
  foreach(int fixture in Enumerable.Range(0,400))
  {
   byte[] b=new byte[285];random.NextBytes(b);b[0]=(byte)(fixture%16);for(int i=b[0]+1;i<16;i++)b[i]=0;
   // Source primary pointer ranges; source-derived layout audited independently in the earlier types gate.
   foreach(var field in System.Text.Json.JsonDocument.Parse(File.ReadAllText("evidence/ecl-vars-menu-2026-10-21/character-layout.json")).RootElement.GetProperty("fields").EnumerateArray())if(field.GetProperty("kind").GetString()=="pointer")Array.Clear(b,field.GetProperty("offset").GetInt32(),field.GetProperty("size").GetInt32());
   var c=new Types.TCharRecord();CharacterRecordCodec.DecodeMonster(b,0,c);byte[] encoded=SaveRecordCodec.Encode(c);for(int i=0;i<285;i++)Eq(b[i],encoded[i],"packed record byte roundtrip "+i);
  }
  string dir=Path.Combine(Path.GetTempPath(),"por-save-"+Guid.NewGuid().ToString("N"))+Path.DirectorySeparatorChar;Directory.CreateDirectory(dir);GDVars.dsSaveDirectory=dir;
  SaveLoad.OpenRead=(name,msg,np)=>File.Exists(name)?new FileStream(name,FileMode.Open,FileAccess.Read,FileShare.Read):null;
  var one=Ch();one.name="ONE";one.strCur=18;one.conCur=15;one.hpCur=11;one.hpMax=20;one.classLevels[C_Level.FIGHTER]=3;one.itemsAddr=new Types.TItemRec{itemType=1,name="ITEM",value=60000,weight=4,plus=-5,next=new Types.TItemRec{itemType=2,name="B"}};one.effectsAddr=new Types.TEffectRec{id=33,duration=65535,strength=255,onRemove=true};one.quickfight=1;one.nextCharAddr=new Types.TCharRecord{name="OTHER"};one.equipped[1]=one.itemsAddr;
  SaveLoad.SaveChar(one,"CHRDATQ1");Eq(285,new FileInfo(dir+"CHRDATQ1.sav").Length,"character packed length");Eq(126,new FileInfo(dir+"CHRDATQ1.itm").Length,"streamed items");Eq(9,new FileInfo(dir+"CHRDATQ1.spc").Length,"effects length");Yes(ReferenceEquals(one.equipped[1],one.itemsAddr)&&one.nextCharAddr!=null,"save never mutates pointers");
  var loaded=Ch();GDVars.quickEnds=1;Yes(SaveLoad.LoadChar("CHRDATQ1",loaded,true,true),"reload character");EqS("ONE",loaded.name,"reload name");Eq(11,loaded.hpCur,"reload hp");Eq(60000,loaded.itemsAddr.value,"reload Word value");Eq(-5,loaded.itemsAddr.plus,"reload signed plus");Eq(65535,loaded.effectsAddr.duration,"reload effect duration");Yes(loaded.effectsAddr.onRemove&&loaded.nextCharAddr==null&&loaded.combatAddr==null,"cleared live pointers");Eq(0,loaded.quickfight,"quick ends clears player quick");
  using(var f=new FileStream(dir+"CHRDATQ1.itm",FileMode.Append))f.WriteByte(99);Yes(SaveLoad.LoadChar("CHRDATQ1",loaded,true,true)&&loaded.itemsAddr.next.next==null,"trailing partial item ignored");
  File.WriteAllBytes(dir+"SHORT.sav",new byte[284]);Yes(!SaveLoad.LoadChar("SHORT",loaded,true,true)&&loaded.itemsAddr==null,"short character rejected before lists");
  // Save exactly the original logical zone windows and normalized fixed-size name slots.
  DSetup();GDVars.dsSaveDirectory=dir;GDVars.creatureList=one;one.nextCharAddr=null;GDVars.curChar=one;GDVars.zone1[C_Gbvm.PARTY_SIZE]=1;GDVars.virtualDisk=3;GDVars.eclScriptId=7;GDVars.gLocType=C_Area.LOC_3D_MAP;GDVars.ifcState=C_Ifc.INDOORS;GDVars.gMapX=4;GDVars.gMapY=5;GDVars.gFacing=6;GDVars.wallAhead=8;GDVars.eventAtCell=9;GDVars.musicOn=1;GDVars.monsterPics=1;GDVars.portraitsOn=1;GDVars.gameDelay=4;
  for(int i=0;i<2048;i++)GDVars.zone0.bytes[i]=(byte)(i%251);GDVars.zone0.yarashPyramidLeft=0;for(int i=0x6B00;i<=0x6EFF;i++)GDVars.zone1[i]=(ushort)(i^0x1234);GDVars.zone1[C_Gbvm.PARTY_SIZE]=1;for(int i=0x9700;i<=0x98FF;i++)GDVars.zone2[i]=(ushort)(i^0xABCD);for(int i=C_Gbvm.SCRIPT_FIRST;i<=C_Gbvm.SCRIPT_LAST;i++)GDVars.zone3[i]=(byte)(i%239);
  ushort[] priorWalls=Enumerable.Range(0,6).Select(i=>GDVars.zone0.words[C_Gbvm.WALLSLOT_ID+1+i]).ToArray();for(int i=1;i<=3;i++){GDVars.dsWallSetCache[i].wallDefId=-1;GDVars.dsWallSetCache[i].symbolSlot=(short)i;}
  SaveLoad.SaveToSlot('Q',"TEST SAVE","AREA");byte[] world=File.ReadAllBytes(dir+"savgamQ.dat");Eq(13137,world.Length,"original save minimum, not padded arrays");Eq(13137,SaveLoad.SaveMinSize(),"minimum size formula");Eq(3,world[0],"virtual disk byte");
  for(int i=0;i<6;i++)Eq(priorWalls[i],GDVars.zone0.words[C_Gbvm.WALLSLOT_ID+1+i],"saved wall aliases restored");
  byte[] expected0=(byte[])GDVars.zone0.bytes.Clone();for(int i=1;i<=3;i++){int a=(C_Gbvm.WALLSLOT_ID+i-0x4900)*2;expected0[a]=255;expected0[a+1]=255;int b=(C_Gbvm.WALLSLOT_SLOT+i-0x4900)*2;expected0[b]=(byte)i;expected0[b+1]=0;}
  for(int i=0;i<2048;i++)Eq(expected0[i],world[i+1],"zone0 exact bytes");
  byte[] z1=new byte[2048],z2=new byte[1024];Buffer.BlockCopy(GDVars.zone1.Data,0x6B00*2,z1,0,2048);Buffer.BlockCopy(GDVars.zone2.Data,0x9700*2,z2,0,1024);
  Yes(world.Skip(2049).Take(2048).SequenceEqual(z1),"zone1 logical window");Yes(world.Skip(4097).Take(1024).SequenceEqual(z2),"zone2 logical window");Yes(world.Skip(5121).Take(7680).SequenceEqual(GDVars.zone3.Data.Skip(0x9900).Take(7680)),"SSI script bytes only");Eq(4,world[12801],"position offset");Eq(5,world[12802],"position y offset");Eq(1,world[12808],"saved member count");Eq(8,world[12809],"fixed short name length");EqS("CHRDATQ1",Encoding.ASCII.GetString(world,12810,8),"member filename");Yes(world.Skip(12818).All(x=>x==0),"name unused bytes cleared");
  Yes(SaveLoad.QuickLoadable(),"quick file loadable");int icons=0;SaveLoad.LoadIcons=b=>icons++;SaveLoad.LoadMap=id=>{};SaveLoad.LoadWalls=(id,slot)=>{};SaveLoad.CompleteYarash=b=>{throw new Exception("wrong Yarash flag");};GDVars.creatureList=null;GDVars.curChar=null;GDVars.gameDelay=9;GDVars.musicOn=0;SaveLoad.LoadSlot('Q');Eq(1,GDVars.zone1[C_Gbvm.PARTY_SIZE],"party reloaded");Eq(1,icons,"icon reload count");EqS("ONE",GDVars.curChar.name,"current party head");Eq(9,GDVars.gameDelay,"settings not overwritten by save");Eq(0,GDVars.musicOn,"music remains global");Eq(0,GDVars.zone0.savedWalls.menuAnimation,"saved animation alias cleared");Eq(0,GDVars.curChar.orderNumber,"lowest free slot");
  var second=Ch();second.name="TWO";SaveLoad.AddCharToParty(second);Eq(1,second.orderNumber,"next free slot");SaveLoad.RemoveCreature(false,false);Yes(ReferenceEquals(GDVars.curChar,GDVars.creatureList)&&GDVars.creatureList.nextCharAddr==null,"remove middle makes previous current");
  byte old=GDVars.gFacing;File.WriteAllBytes(dir+"savgamA.dat",new byte[13136]);smio.Keys.Enqueue(13);SaveLoad.LoadSlot('A');Eq(old,GDVars.gFacing,"short save does not alter world");
 }
}
static partial class Tests
{
 static void DExplore()
 {
  DSetup();var host=new RestHost();Explore.Services=host;Explore.Sound=x=>host.Log.Add("sound:"+x);Explore.PrintInfo=()=>host.Log.Add("info");Explore.Draw3D=(x,y,f)=>host.Log.Add("draw:"+f);Explore.DisplayInterface=()=>host.Log.Add("interface");Explore.DisplayWilderness=()=>host.Log.Add("wild");
  // Every barrier byte, cardinal facing, passability, and boundary coordinate; other planes unchanged.
  for(int value=0;value<256;value++)foreach(byte facing in new byte[]{0,2,4,6,8})foreach(bool open in new[]{false,true})foreach(sbyte x in new sbyte[]{-1,0,15,16})
  {
   GDVars.map3d.barriers[0,0]=(byte)value;GDVars.map3d.barriers[0,15]=(byte)value;GDVars.map3d.wallsNE[0,0]=99;Explore.ChangeBarrier(x,0,facing,open);
   int expected=value;if(x>=0&&x<=15&&facing<8){int shift=facing;expected=(value&~(3<<shift))|(open?1<<shift:0);}Eq(expected,GDVars.map3d.barriers[0,x==15?15:0],"barrier bit ownership");Eq(99,GDVars.map3d.wallsNE[0,0],"wall plane immutable");
  }
  foreach(bool locked in new[]{false,true})for(int str=0;str<256;str++)foreach(byte exc in new byte[]{0,50,51,90,91,99,100,255})foreach(int roll in new[]{1,2,3,4,5,6,7,8,9,10,11,12,19,20,23,24})
  {
   var p=Ch();p.strCur=(byte)str;p.strExc=exc;GDVars.creatureList=p;GDVars.curChar=p;GDVars.gMapX=2;GDVars.gMapY=2;GDVars.gFacing=C_Dir.NORTH;GDVars.map3d.wallsNE[2,2]=16;GDVars.map3d.barriers[2,2]=(byte)(locked?3:2);GDVars.doorOpen[0]=1;int called=0;
   Dice.RandomBelow=n=>{called++;return Math.Min(n,roll)-1;};
   int sides=0,limit=0;bool disable=false;
   if(locked){if(str==18&&exc>=91){sides=6;limit=exc<100?1:2;}else if(str>=19){var table=new[]{Tuple.Create(20,6,3),Tuple.Create(22,6,4),Tuple.Create(23,6,5),Tuple.Create(24,8,7),Tuple.Create(255,10,9)};var e=table.First(t=>str<=t.Item1);sides=e.Item2;limit=e.Item3;}else disable=true;}
   else if(str>=3){if(str<18){sides=6;limit=str<=7?1:str<=15?2:3;}else if(str==18){sides=6;limit=exc<=50?3:exc<=99?4:5;}else{var table=new[]{Tuple.Create(20,8),Tuple.Create(21,10),Tuple.Create(23,12),Tuple.Create(24,20),Tuple.Create(255,24)};sides=table.First(t=>str<=t.Item1).Item2;limit=sides-1;}}
   bool expected=sides>0&&Math.Min(sides,roll)<=limit;Yes(Explore.BashDoor()==expected,"strength door odds");Eq(sides>0?1:0,called,"exact RNG eligibility");Eq(disable?0:1,GDVars.doorOpen[0],"bash eligibility flag");if(expected){Eq(1,GDVars.map3d.barriers[2,2]&3,"own barrier opened");Eq(1,(GDVars.map3d.barriers[1,2]>>4)&3,"opposite side opened");}
  }
  DSetup();Explore.Services=host;var a=Ch();a.status=C_Status.DEAD;a.thiefSkills[2]=100;var b=Ch();b.thiefSkills[2]=100;a.nextCharAddr=b;GDVars.creatureList=a;GDVars.gFacing=0;int rolls=0;Dice.RandomBelow=n=>{rolls++;return 0;};Yes(Explore.PickLock(),"living picker succeeds");Eq(2,rolls,"dead picker still consumes source RNG");Eq(0,GDVars.doorOpen[1],"one pick attempt");
  DSetup();Explore.Services=host;GDVars.gMapX=15;GDVars.gMapY=0;GDVars.gFacing=2;GDVars.map3d.barriers[0,15]=4;Explore.MoveForward();Eq(1,GDVars.zone1[C_Gbvm.LEAVE_MAP],"edge exit flag");Eq(15,GDVars.gMapX,"edge does not wrap until actual move");
  GDVars.zone1[C_Gbvm.SEARCH_FLAGS]=1;GDVars.sound.singleStep=true;host.Log.Clear();Explore.MovePartyForward3D();Eq(0,GDVars.gMapX,"actual step wraps");EqS("time:1:2",string.Join("|",host.Log),"search turn and single-step driver");
  DSetup();Explore.Services=host;GDVars.gLocType=C_Area.LOC_WILD_WEST;GDVars.gFacing=C_Dir.NORTH;int col=5+GDConsts.wildColOfs[C_Area.LOC_WILD_WEST];byte savedTile=GDConsts.wildMap[4,col];
  var blocked=new HashSet<int>(new[]{78,188,245}.Concat(Enumerable.Range(126,3)).Concat(Enumerable.Range(130,3)).Concat(Enumerable.Range(168,5)).Concat(Enumerable.Range(174,6)).Concat(Enumerable.Range(221,2)).Concat(Enumerable.Range(225,9)));
  try{for(int tile=0;tile<256;tile++)foreach(bool riverOpen in new[]{false,true}){GDConsts.wildMap[4,col]=(byte)tile;GDVars.zone0.partyX.value=5;GDVars.zone0.partyY.value=5;GDVars.zone0.yarashPyramidLeft=(ushort)(riverOpen?1:0);host.Log.Clear();smio.Keys.Clear();smio.Keys.Enqueue(13);Explore.MovePartyWilderness();bool moved=!blocked.Contains(tile)&&(!(tile>=102&&tile<=108||tile==114)||riverOpen);Eq(moved?4:5,GDVars.zone0.partyY.value,"wilderness terrain movement");Eq(moved?1:0,host.Log.Count(x=>x=="time:12:3"),"wilderness travel time only after movement");}}finally{GDConsts.wildMap[4,col]=savedTile;}
  DSetup();Explore.Services=host;GDVars.gLocType=C_Area.LOC_3D_MAP;GDVars.gMapX=2;GDVars.gMapY=2;GDVars.gFacing=0;GDVars.zone1[C_Gbvm.MOVE_LOCKED]=255;GDVars.animData=new Types.TAnimRec();GDVars.dsPortraitCache[1].pictureId=3;GDVars.dsPortraitCache[2].pictureId=4;host.Log.Clear();Explore.ResolveMovement();Eq(0,GDVars.zone1[C_Gbvm.MOVE_LOCKED],"movement lock clears without step");Yes(host.Log.Count==0&&GDVars.dsPortraitCache[1].pictureId==255&&GDVars.dsPortraitCache[2].pictureId==255,"locked move still frees portraits");
  DSetup();Explore.Services=host;GDVars.gLocType=C_Area.LOC_3D_MAP;GDVars.zone1[C_Gbvm.SEARCH_FLAGS]=0;GDVars.scriptEntryAddrs[C_Ecl.ENTRY_SEARCH]=0x9A55;smio.Keys.Enqueue((byte)'L');host.Log.Clear();Eq('L',Explore.MapMainMenu(),"look ends menu");Eq(2,GDVars.zone1[C_Gbvm.SEARCH_FLAGS],"look sets search bit");Eq(0x9A55,GDVars.eclIp,"search script target");Yes(host.Log.Contains("time:1:2"),"look costs turn");
 }
 static void DPost()
 {
  // Independent outcome classification over all two-member status combinations, flags and hostile/NPC roles.
  for(int sa=0;sa<9;sa++)for(int sb=0;sb<9;sb++)foreach(bool npc in new[]{false,true})foreach(bool hostile in new[]{false,true})foreach(bool enabled in new[]{false,true})
  {
   DSetup();var a=Ch();var b=Ch();a.name="A";b.name="B";a.status=(byte)sa;b.status=(byte)sb;a.npc=(byte)(npc?128:0);a.hostile=(sbyte)(hostile?1:0);a.enabled=(byte)(enabled?1:0);a.nextCharAddr=b;GDVars.creatureList=a;GDVars.curChar=a;GDVars.zone1[C_Gbvm.PARTY_SIZE]=2;PostCbt.nonEscapedHostileFound=false;
   bool wins=sa==C_Status.OKAY||sa==C_Status.ANIMATED||sb==C_Status.OKAY||sb==C_Status.ANIMATED;
   Func<int,bool> alive=s=>s==C_Status.OKAY||s==C_Status.ANIMATED||s==C_Status.RUNNING;
   bool defeated=!((alive(sa)&&!npc&&!hostile)||alive(sb));bool fled=!wins&&(sa==C_Status.RUNNING||sb==C_Status.RUNNING);
   PostCbt.DetermineCombatResults();Yes(GDVars.partyDefeated==defeated,"party defeat classification");Yes(PostCbt.combatWon==wins&&PostCbt.partyFled==fled,"win/flee classification");
   var expected=new List<Types.TCharRecord>();if(!defeated){if(!fled||sa==C_Status.RUNNING)expected.Add(a);if(!fled||sb==C_Status.RUNNING)expected.Add(b);}var actual=new List<Types.TCharRecord>();for(var c=GDVars.creatureList;c!=null;c=c.nextCharAddr)actual.Add(c);
   Yes(expected.SequenceEqual(actual),"party survivor identities/order");Eq(expected.Count,GDVars.zone1[C_Gbvm.PARTY_SIZE],"party size after outcome");
   foreach(var c in actual){int old=ReferenceEquals(c,a)?sa:sb;int status=old;if(old==C_Status.RUNNING)status=C_Status.OKAY;else if(!fled&&old==C_Status.DYING)status=C_Status.UNCONSCIOUS;else if(!fled&&old==C_Status.UNCONSCIOUS&&c.hpCur>0)status=C_Status.OKAY;Eq(status,c.status,"post outcome status");}
  }
  DSetup();var p=Ch();p.name="PLAYER";var enemy=Ch();enemy.name="MONSTER";enemy.hostile=1;enemy.status=C_Status.DEAD;enemy.enabled=0;enemy.xpAward=100;enemy.xpBonusPerHp=2;enemy.hpRolled=10;enemy.coins[3]=40;enemy.itemsAddr=new Types.TItemRec{value=1,plus=2,equipped=1};p.nextCharAddr=enemy;GDVars.creatureList=p;GDVars.curChar=p;GDVars.zone1[C_Gbvm.PARTY_SIZE]=1;PostCbt.nonAnimCount=0;int share=0;PostCbt.DetermineLootExp(ref share);Eq(960,share,"monster + gold + magic xp");Yes(GDVars.treasureItems!=enemy.itemsAddr&&GDVars.treasureItems.equipped==0,"loot copied and unreadied");Eq(40,GDVars.treasureMoney[3],"loot money");
  GDVars.zone1[C_Gbvm.PARTY_SIZE]=0;PostCbt.nonAnimCount=0;share=123;GDVars.treasureItems=null;Array.Clear(GDVars.treasureMoney.Data,0,7);PostCbt.DetermineLootExp(ref share);Eq(0,share,"no sharers avoids zero divide");
  DSetup();p=Ch();p.name="PLAYER";p.quickfight=1;p.combatAddr=new Types.TCombatRec();enemy=Ch();enemy.enabled=0;enemy.hostile=1;enemy.combatAddr=new Types.TCombatRec{notPcNpc=1};p.nextCharAddr=enemy;GDVars.creatureList=p;GDVars.curChar=p;GDVars.zone1[C_Gbvm.PARTY_SIZE]=1;GDVars.quickEnds=1;PostCbt.PostCombatCreatureCleanup();Yes(p.nextCharAddr==null&&p.combatAddr==null&&GDVars.curChar==p,"combat state cleaned");Eq(1,GDVars.zone1[C_Gbvm.MONSTERS_DEFEATED],"defeated monster count");Eq(1,GDVars.zone1[C_Gbvm.PARTY_SIZE],"monster removal doesn't shrink party");Eq(0,p.quickfight,"manual after fight");
  DSetup();GDVars.duelFlag=true;GDVars.zone1[C_Gbvm.MIRROR_DUEL]=1;p=Ch();p.levelHighest=7;GDVars.curChar=p;PostCbt.DetermineLootExp(ref share);Eq(700,share,"mirror duel award");
 }
}
public class CombatProxy : System.Reflection.DispatchProxy
{
 public Func<string,object[],object> OnCall;
 protected override object Invoke(System.Reflection.MethodInfo method,object[] args)
 {
  if(OnCall!=null)return OnCall(method.Name,args);
  throw new Exception("Unexpected combat dependency: "+method.Name);
 }
}
static partial class Tests
{
 static void DCombat()
 {
  // Initiative oracle: choose maximum positive initiative, then maximum roll, then latest list position.
  var r=new Random(640);
  for(int n=0;n<3000;n++)
  {
   DSetup();var list=new List<Types.TCharRecord>();int count=1+r.Next(8);var rolls=new Queue<int>();var vals=new List<int>();
   for(int i=0;i<count;i++){var c=Ch();c.combatAddr=new Types.TCombatRec{initiative=(sbyte)r.Next(-3,21)};if(i>0)list[i-1].nextCharAddr=c;list.Add(c);int roll=r.Next(1,101);rolls.Enqueue(roll);vals.Add(roll);}
   GDVars.creatureList=list[0];Dice.RandomBelow=max=>rolls.Dequeue()-1;var winner=list.Select((c,i)=>new{c,i,roll=vals[i]}).Where(x=>x.c.combatAddr.initiative>0).OrderBy(x=>x.c.combatAddr.initiative).ThenBy(x=>x.roll).ThenBy(x=>x.i).LastOrDefault();Types.TCharRecord actual=list[0];Combat.NextCreature(ref actual);Yes(ReferenceEquals(winner==null?null:winner.c,actual),"initiative priority/ties");Eq(0,rolls.Count,"one RNG per listed creature");
  }
  DSetup();var a=Ch();a.name="A";a.status=C_Status.DYING;a.combatAddr=new Types.TCombatRec{overkill=4};var b=Ch();b.name="B";b.status=C_Status.DYING;b.combatAddr=new Types.TCombatRec{overkill=7};var enemy=Ch();enemy.hostile=1;enemy.status=C_Status.DYING;enemy.combatAddr=new Types.TCombatRec{notPcNpc=1,overkill=5};a.nextCharAddr=enemy;enemy.nextCharAddr=b;GDVars.creatureList=a;var messages=new List<string>();Combat.NameMessage=(c,s,row,pause)=>messages.Add(c.name+":"+s);Yes(Combat.BandageDying(false)&&messages.Count==0,"query no bandage");Yes(Combat.BandageDying(true),"bandage found");Eq(C_Status.UNCONSCIOUS,a.status,"first ally bandaged");Eq(C_Status.DYING,b.status,"only one ally bandaged");Eq(0,a.combatAddr.overkill,"overkill cleared");Eq(5,enemy.combatAddr.overkill,"enemy ignored");EqS("A:is bandaged",messages.Single(),"bandage message");
  a.hostile=0;a.combatAddr.other=b;b.hostile=0;Combat.TurnOnAuto(a);Yes(a.quickfight==1&&a.combatAddr.other==null,"auto drops same-side target");a.combatAddr.other=enemy;Combat.TurnOnAuto(a);Yes(a.combatAddr.other==enemy,"auto keeps enemy lock");
  var service=System.Reflection.DispatchProxy.Create<ICombatServices,CombatProxy>();var proxy=(CombatProxy)(object)service;Combat.Services=service;var log=new List<string>();
  Types.TCastUI target=(byte sp,bool flag,ref bool done)=>{};
  proxy.OnCall=(name,args)=>{log.Add(name);switch(name){case "get_CombatTargetMenu":case "get_NoncombatTargetMenu":return target;case "CellX":case "CellY":return (sbyte)1;case "Slot":return (byte)1;case "TurnLevel":return (byte)0;case "IsValidCreatureLoc":return true;default:return null;}};
  DSetup();var c2=Ch();c2.combatAddr=new Types.TCombatRec{initiative=5};c2.quickfight=1;GDVars.creatureList=c2;Combat.PrintInfo=c=>log.Add("info");log.Clear();Combat.CreatureAction(c2);EqS("TriggerEffects|DisplayMapCentered|info|TriggerEffects|AiControl|CellX|CellY|RedrawCell",string.Join("|",log),"one AI turn ordered trace");
  c2.enabled=0;c2.combatAddr.initiative=12;c2.combatAddr.guarding=1;Combat.ManualControl(c2);Eq(0,c2.combatAddr.initiative,"disabled ends turn");Eq(0,c2.combatAddr.guarding,"disabled clears guard");
  c2.enabled=1;c2.combatAddr.spellCast=3;c2.combatAddr.initiative=8;log.Clear();Combat.ManualControl(c2);Eq(0,c2.combatAddr.spellCast,"spell cleared before resolution");Eq(0,c2.combatAddr.initiative,"resolved spell ends actions");EqS("CastSpellNC",string.Join("|",log),"pending spell resolution only");
  DSetup();c2=Ch();c2.combatAddr=new Types.TCombatRec{movement=4};GDVars.creatureList=c2;smio.Keys.Enqueue(0);smio.Keys.Enqueue(C_Keys.RIGHT);bool movingDone=true;
  proxy.OnCall=(name,args)=>{log.Add(name);switch(name){case "CellX":case "CellY":return (sbyte)2;case "ProbeDirection":args[2]=(byte)0;args[3]=(byte)1;return null;case "MoveCreature":((Types.TCharRecord)args[0]).combatAddr.movement=1;return null;default:return null;}};GDConsts.terrainInfo[1].cost=1;Combat.MoveMenu(c2,ref movingDone);Eq(0,c2.combatAddr.movement,"remaining half-move discarded");Yes(!movingDone,"movement exhaustion doesn't itself end actions");
  // Full loop with two recording AI turns and real initiative/team selection.
  DSetup();a=Ch();enemy=Ch();a.quickfight=enemy.quickfight=1;a.name="A";enemy.name="E";a.combatAddr=new Types.TCombatRec();enemy.combatAddr=new Types.TCombatRec{notPcNpc=1};enemy.hostile=1;a.nextCharAddr=enemy;GDVars.creatureList=a;GDVars.curChar=a;GDVars.zone1[C_Gbvm.PARTY_SIZE]=1;GDVars.arenaPtr=new Types.TArena();GDVars.suppressContinuePrompt=1;GDVars.stinkHead=new Types.TStinkRec{next=new Types.TStinkRec()};Dice.RandomBelow=max=>0;log.Clear();
  proxy.OnCall=(name,args)=>{log.Add(name);switch(name){case "get_CombatTargetMenu":case "get_NoncombatTargetMenu":return target;case "CombatStart":GDVars.teamCount[0]=GDVars.teamCount[1]=1;return null;case "RoundStart":((Types.TCharRecord)args[0]).combatAddr.initiative=10;return null;case "AiControl":var c=(Types.TCharRecord)args[0];c.combatAddr.initiative=0;if(c.hostile==1)c.enabled=0;return null;case "CellX":case "CellY":return (sbyte)1;default:return null;}};
  Combat.CombatMain();Eq(2,log.Count(x=>x=="AiControl"),"two initiatives acted");Eq(1,GDVars.roundCounter,"one round advanced");Yes(GDVars.arenaPtr==null&&GDVars.stinkHead==null&&ReferenceEquals(target,GDVars.castTargetMenu),"combat cleanup and cast-target restoration");Yes(log.Contains("SetNoncombatPalette"),"noncombat palette restored");
 }
}
class EditHost : ICharEditServices
{
 public List<string> Log=new List<string>();public void DisplayChar(Types.TCharRecord c){Log.Add("display");}public void ModifyCharacter(bool hp){Log.Add("modify:"+hp);}public void GainLevels(byte classes,bool training){Log.Add("gain:"+classes+":"+training);for(int i=0;i<8;i++)if((GDConsts.classBit[i]&classes)!=0)GDVars.curChar.classLevels[i]++;}
}
static partial class Tests
{
 static void DMenusAndBindings()
 {
  DSetup();var h=new EditHost();CharEdit.Services=h;GDVars.curChar=Ch();GDVars.curChar.name="EDIT";GDVars.creatureList=GDVars.curChar;smio.Keys.Enqueue((byte)'A');smio.Keys.Enqueue((byte)'E');CharEdit.EditMenu();EqS("display|modify:True|display",string.Join("|",h.Log),"editor dispatch with no experience gate");
  DSetup();CharEdit.Services=h;GDVars.curChar=Ch();GDVars.curChar.classLevels[C_Level.FIGHTER]=2;GDVars.curChar.hpMax=30;GDVars.curChar.hpCur=20;GDVars.curChar.experience=10000;smio.Keys.Enqueue(0);smio.Keys.Enqueue(C_Keys.LEFT);smio.Keys.Enqueue((byte)'D');CharEdit.LevelsTool();Eq(1,GDVars.curChar.classLevels[C_Level.FIGHTER],"level tool left");Eq(GDConsts.dsExperienceSpellsClass[C_Level.FIGHTER].xp[2]-1,GDVars.curChar.experience,"level range adjusts XP");
  DSetup();CharEdit.Services=h;var item=new Types.TItemRec{plus=3,value=300,itemType=1};item.nameIds[3]=(byte)(C_Item.NAME_PLUS_BASE+3);for(int i=0;i<2;i++){smio.Keys.Enqueue(0);smio.Keys.Enqueue(C_Keys.LEFT);}for(int i=0;i<7;i++){smio.Keys.Enqueue(0);smio.Keys.Enqueue(C_Keys.RIGHT);}smio.Keys.Enqueue((byte)'T');Yes(CharEdit.ChoosePlus(item),"take plus selection");Eq(5,item.plus,"plus clamps at five");Eq(500,item.value,"per-plus value");Eq(C_Item.NAME_PLUS_BASE+5,item.nameIds[3],"plus name follows");
  DSetup();CharEdit.Services=h;GDVars.curChar=Ch();GDVars.curChar.strCur=18;GDVars.curChar.useStrength=1;GDVars.creatureList=GDVars.curChar;smio.Keys.Enqueue(13);smio.Keys.Enqueue((byte)'1');smio.Keys.Enqueue((byte)'2');smio.Keys.Enqueue((byte)'3');smio.Keys.Enqueue(13);smio.Keys.Enqueue(27);CharEdit.MoneyTool();Eq(123,GDVars.curChar.coins[0],"coin tool typed quantity");
  DSetup();CharEdit.Services=h;GDVars.curChar=Ch();GDVars.curChar.spellsKnown[1]=0;smio.Keys.Enqueue(13);smio.Keys.Enqueue(27);CharEdit.SpellList(0);Eq(1,GDVars.curChar.spellsKnown[1],"known-spell toggle");
  DSetup();CharEdit.Services=h;GDVars.curChar=Ch();Dice.RandomBelow=n=>0;smio.Keys.Enqueue(27);CharEdit.AddItem();Yes(GDVars.curChar.itemsAddr==null,"cancelled generated add list does not give item");
  DSetup();FixMain.Services=null;EclVm.Units=new EclVmUnits();TenDBindings.MainMenu=null;TenDBindings.BindUnits();var game=EclVm.Units.Game;var main=FixMain.Services;TenDBindings.BindUnits();Yes(ReferenceEquals(game,EclVm.Units.Game)&&ReferenceEquals(main,FixMain.Services),"idempotent integration without decorator growth");
  Explore.quickLoadAsked=true;Yes(EclVm.Units.Explore.QuickLoadAsked&&FixMain.Services.QuickLoadAsked,"shared quick-load handoff flag");FixMain.Services.QuickLoadAsked=false;Yes(!Explore.quickLoadAsked,"main clears shared request");
  GDVars.creatureList=null;GDVars.curChar=null;CompTool.EditCharacter();Yes(true,"CompTool now bound to real nil-safe editor");
  Yes(TenDBindings.MainMenu!=null&&TenDBindings.MainMenu.Method.DeclaringType==typeof(CharGen),"converted CharGen main menu bound");int menus=0;TenDBindings.MainMenu=()=>menus++;FixMain.Services.MainMenu();Eq(1,menus,"CharGen main-menu adapter");TenDBindings.MainMenu=null;
  GDVars.creatureList=Ch();GDVars.curChar=GDVars.creatureList;GDVars.zone1[C_Gbvm.PARTY_SIZE]=1;EclVm.Units.SaveLoad.RemoveCreature(false,false);Yes(GDVars.creatureList==null&&GDVars.curChar==null&&GDVars.zone1[C_Gbvm.PARTY_SIZE]==0,"VM save-load actual removal");
 }
}
