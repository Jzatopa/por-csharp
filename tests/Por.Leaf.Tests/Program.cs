using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Text.Json;using System.Security.Cryptography;using Por.Core;
class LeafVerification
{
 static string ev="evidence/full-port-2026-10-21";static int assertions,groups,failed;
 static void Yes(bool x,string label){assertions++;if(!x)throw new Exception(label);}
 static void Run(string label,Action a){groups++;try{a();Console.WriteLine("PASS "+label);}catch(Exception e){failed++;Console.WriteLine("FAIL "+label+": "+e);}}
 static void Temp(Action<string> a){string d=Path.Combine(Path.GetTempPath(),"por-leaf-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(d);try{a(d+Path.DirectorySeparatorChar);}finally{Directory.Delete(d,true);}}
 static string Hash(byte[] b){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","").ToLowerInvariant();}
 static byte[] Header(int size,string sig){var b=new byte[size];b[0]=4;for(int i=0;i<4;i++)b[i+1]=(byte)sig[i];b[5]=1;return b;}
 static void Area()
 {
  var names=JsonSerializer.Deserialize<string[]>(File.ReadAllText(ev+"/area-source.json"));GDVars.zone0=new Types.TGbvmZone0State();for(int s=0;s<256;s++)for(int m=0;m<256;m++){string expected=s<30?names[s]:"UNKNOWN AREA";if(s==3&&m==3)expected="VALJEVO CASTLE NW";if(s==3&&m==5)expected="VALJEVO CASTLE SE";if(s==16&&m==30)expected="LIZARD CATACOMBS";if(s==29&&m==32)expected="KUTO'S CATACOMBS";if(s==24&&m==24)expected="TEMPLE OF BANE";Yes(AreaName.AreaFor((byte)s,(byte)m)==expected,"area exhaustive");GDVars.zone0.previousScriptId=(ushort)(0xaa00|s);GDVars.zone0.mapId.lo=(byte)m;Yes(AreaName.CurrentArea()==expected,"current low script/map");}
 }
 static void Explore()
 {
  GDVars.zone0=new Types.TGbvmZone0State();Explored.Clear();GDVars.eclScriptId=1;GDVars.zone0.mapId.lo=2;
  for(int x=-128;x<128;x++)for(int y=-128;y<128;y++)Explored.Mark((sbyte)x,(sbyte)y);
  for(int x=-128;x<128;x++)for(int y=-128;y<128;y++)Yes(Explored.Visited(1,2,(sbyte)x,(sbyte)y)==(x>=0&&x<16&&y>=0&&y<16),"visited coordinate domain");
  Temp(d=>{
   Explored.WriteSlot(d+"all");byte[] expected=Header(2183,"EXPL");expected[6]=1;expected[7]=1;expected[8]=2;for(int i=9;i<41;i++)expected[i]=255;Yes(File.ReadAllBytes(d+"all").SequenceEqual(expected),"explored packed complete cells");
   var rng=new Random(72);for(int sample=0;sample<40;sample++){byte[] raw=new byte[2183];rng.NextBytes(raw);Array.Copy(Header(7,"EXPL"),raw,7);raw[6]=(byte)rng.Next(65);for(int i=0;i<raw[6];i++){raw[7+i*34]=(byte)i;raw[8+i*34]=(byte)(255-i);}File.WriteAllBytes(d+"raw",raw);Explored.ReadSlot(d+"raw");Explored.WriteSlot(d+"out");Yes(File.ReadAllBytes(d+"out").SequenceEqual(raw),"preserve all raw map bytes");for(int i=0;i<raw[6];i++)for(int x=0;x<16;x++)for(int y=0;y<16;y++){int off=9+i*34+y*2;Yes(Explored.Visited((byte)i,(byte)(255-i),(sbyte)x,(sbyte)y)==((BitConverter.ToUInt16(raw,off)&(1<<x))!=0),"raw row/source index");}}
   foreach(int bad in new[]{0,1,4,5,6}){byte[] raw=Header(2183,"EXPL");raw[bad]=255;File.WriteAllBytes(d+"bad",raw);Explored.ReadSlot(d+"bad");Explored.WriteSlot(d+"out");Yes(File.ReadAllBytes(d+"out").SequenceEqual(Header(2183,"EXPL")),"invalid map clears");}
   Explored.Clear();for(int i=0;i<65;i++){GDVars.eclScriptId=(byte)i;GDVars.zone0.mapId.lo=1;Explored.Mark(1,2);}Yes(Explored.Visited(63,1,1,2)&&!Explored.Visited(64,1,1,2),"area capacity64");
  });
 }
 static void Notes()
 {
  MapNotes.Clear();Temp(d=>{using(var doc=JsonDocument.Parse(File.ReadAllText(ev+"/mapnotes-oracle.json")))foreach(var op in doc.RootElement.EnumerateArray()){byte s=op.GetProperty("script").GetByte(),m=op.GetProperty("map").GetByte();sbyte x=op.GetProperty("x").GetSByte(),y=op.GetProperty("y").GetSByte();bool ok=MapNotes.SetNote(s,m,x,y,op.GetProperty("text").GetString());Yes(ok==op.GetProperty("ok").GetBoolean(),"note operation result");Yes(MapNotes.Note(s,m,x,y)==op.GetProperty("note").GetString(),"note lookup/value");if(op.GetProperty("sha256").ValueKind!=JsonValueKind.Null){MapNotes.WriteSlot(d+"slot");Yes(Hash(File.ReadAllBytes(d+"slot"))==op.GetProperty("sha256").GetString(),"note raw state machine hash");}}
   byte[] raw=Header(4487,"NOTE");raw[6]=1;raw[7]=7;raw[8]=8;raw[9]=9;raw[10]=10;raw[11]=255;for(int i=0;i<30;i++)raw[12+i]=(byte)(i+128);File.WriteAllBytes(d+"raw",raw);MapNotes.ReadSlot(d+"raw");string expected=new string(Enumerable.Range(128,30).Select(i=>(char)i).ToArray());Yes(MapNotes.Note(7,8,9,10)==expected,"bounded result corrupt length");MapNotes.WriteSlot(d+"out");Yes(File.ReadAllBytes(d+"out").SequenceEqual(raw),"malformed length retained onrawroundtrip");
   foreach(int index in new[]{0,1,5,6}){raw=Header(4487,"NOTE");raw[index]=255;File.WriteAllBytes(d+"bad",raw);MapNotes.ReadSlot(d+"bad");MapNotes.WriteSlot(d+"out");Yes(File.ReadAllBytes(d+"out").SequenceEqual(Header(4487,"NOTE")),"invalid note header clears");}
  });
 }
 static byte[] Mem(string name,byte id,Types.TSpellSlots slots){byte[] b=Header(44,"USPL");b[6]=(byte)name.Length;for(int i=0;i<name.Length;i++)b[7+i]=(byte)name[i];b[22]=id;for(int i=0;i<=20;i++)b[23+i]=slots[i];return b;}
 static void Usual()
 {
  GDVars.zone1=new Types.TGbvmZone1();GDVars.zone1[C_Gbvm.PARTY_SIZE]=8;var chars=Enumerable.Range(0,10).Select(i=>new Types.TCharRecord{name="OWNER"+i,randByte=(byte)(i+90)}).ToArray();for(int i=0;i<7;i++)chars[i].nextCharAddr=chars[i+1];GDVars.creatureList=chars[0];var spell=new Types.TSpellSlots();for(int j=1;j<=20;j++)spell[j]=(byte)(j+128);var result=new Types.TSpellSlots();var same=result;
  Yes(!UsualSpl.GetUsualSpells(chars[0],ref result),"no cache initially");UsualSpl.SetUsualSpells(chars[0],ref spell);spell[0]=3;spell[1]=7;Yes(UsualSpl.GetUsualSpells(chars[0],ref result)&&ReferenceEquals(same,result)&&result[1]==129,"deep Set/Get in-placecopy");result[2]=8;UsualSpl.GetUsualSpells(chars[0],ref result);Yes(result[2]==130,"Get does not share cachearray");chars[0].name="RENAMED";Yes(!UsualSpl.GetUsualSpells(chars[0],ref result),"ownername required");UsualSpl.ForgetUsualSpells(chars[0]);
  Temp(d=>{var rng=new Random(731);for(int t=0;t<100;t++){var c=chars[t%8];c.name=new string(Enumerable.Range(0,15).Select(i=>(char)((i*17+t*13)&255)).ToArray());c.randByte=(byte)t;for(int i=0;i<=20;i++)spell[i]=(byte)rng.Next(256);UsualSpl.SetUsualSpells(c,ref spell);UsualSpl.WriteUsualSpells(c,d+"owner");byte[] expected=Mem(c.name,c.randByte,spell);Yes(File.ReadAllBytes(d+"owner.mem").SequenceEqual(expected),"packed MEM source oracle");UsualSpl.ForgetUsualSpells(c);UsualSpl.ReadUsualSpells(c,d+"owner");Yes(UsualSpl.GetUsualSpells(c,ref result)&&Enumerable.Range(1,20).All(i=>result[i]==spell[i]),"MEM exact roundtrip inclzero namebyte");foreach(int offset in new[]{0,1,5,6,22}){byte[] bad=(byte[])expected.Clone();bad[offset]=255;File.WriteAllBytes(d+"owner.mem",bad);UsualSpl.ReadUsualSpells(c,d+"owner");Yes(!UsualSpl.GetUsualSpells(c,ref result),"malformed signature/version/name/random clears "+offset);}}
   UsualSpl.ForgetUsualSpells(chars[0]);File.Delete(d+"owner.mem");File.WriteAllBytes(d+"OWNER.MEM",new byte[]{1});UsualSpl.WriteUsualSpells(chars[0],d+"owner");Yes(!File.Exists(d+"OWNER.MEM"),"old memo erased via DOS adapter evennocache");
  });
  foreach(var c in chars)UsualSpl.ForgetUsualSpells(c);foreach(var c in chars.Take(8))UsualSpl.SetUsualSpells(c,ref spell);UsualSpl.SetUsualSpells(chars[8],ref spell);Yes(!UsualSpl.GetUsualSpells(chars[0],ref result)&&UsualSpl.GetUsualSpells(chars[8],ref result),"allpartyfullslot1eviction");UsualSpl.SetUsualSpells(chars[9],ref spell);Yes(!UsualSpl.GetUsualSpells(chars[8],ref result)&&UsualSpl.GetUsualSpells(chars[9],ref result),"nonpartyfirsteviction");
 }
 static void RandomItems()
 {
  using(var doc=JsonDocument.Parse(File.ReadAllText(ev+"/randitem-oracle.json")))
  {
   foreach(var c in doc.RootElement.GetProperty("cases").EnumerateArray())
   {
    uint state=c.GetProperty("seed").GetUInt32();var bounds=new List<int>();Dice.RandomBelow=bound=>{state=unchecked(state*1664525+1013904223);bounds.Add(bound);return (int)(state%(uint)bound);};var item=new Types.TItemRec{name="POISON",inUse=255,next=new Types.TItemRec(),itemType=255,plus=-128,saveBonus=-128,equipped=255,unidMask=255,cursed=255,weight=65535,quantity=255,value=65535};for(int i=1;i<=3;i++){item.nameIds[i]=255;item.spells[i]=255;}var alias=item.spells;var names=item.nameIds;
    RandItem.GenerateRandomItem(c.GetProperty("kind").GetByte(),item);var x=c.GetProperty("expected");Yes(item.itemType==x.GetProperty("itemType").GetByte()&&item.plus==x.GetProperty("plus").GetInt32()&&item.saveBonus==x.GetProperty("saveBonus").GetInt32()&&item.unidMask==x.GetProperty("unidMask").GetByte(),"itemtype/bonus/flags source oracle");Yes(item.weight==x.GetProperty("weight").GetUInt16()&&item.quantity==x.GetProperty("quantity").GetByte()&&item.value==x.GetProperty("value").GetUInt16(),"item weight/quantity/value source oracle");var ns=x.GetProperty("nameIds");var ss=x.GetProperty("spells");for(int j=1;j<=3;j++){Yes(item.nameIds[j]==ns[j-1].GetByte(),"item names");Yes(item.spells[j]==ss[j-1].GetByte()&&alias[j]==ss[j-1].GetByte(),"item variant alias");}Yes(item.name==""&&item.inUse==0&&item.next==null&&item.equipped==0&&item.cursed==0,"whole itemreset");Yes(bounds.SequenceEqual(c.GetProperty("bounds").EnumerateArray().Select(a=>a.GetInt32())),"source RNG countandbounds");
   }
   int template=1;foreach(var t in doc.RootElement.GetProperty("templates").EnumerateArray()){var item=new Types.TItemRec{name="KEEP",inUse=77,itemType=91,equipped=44,cursed=12,unidMask=9,next=new Types.TItemRec()};var next=item.next;RandItem.ApplyTemplate(item,(byte)template);Yes(item.name=="KEEP"&&item.inUse==77&&item.itemType==91&&item.equipped==44&&item.cursed==12&&item.unidMask==9&&ReferenceEquals(item.next,next),"template onlyassignssourcefields");for(int j=1;j<=3;j++)Yes(item.nameIds[j]==(t.GetProperty("nameIds")[j-1].GetUInt16()&255)&&item.spells[j]==(t.GetProperty("properties")[j-1].GetUInt16()&255),"template data exact");template++;}
  }
  Dice.RandomBelow=null;
 }
 static void Main(){Run("all area/script byte pairs",Area);Run("exploration raw format/cells/capacity",Explore);Run("mapnotes independent state-machine operations",Notes);Run("usual spells owner identity/sidecar codec",Usual);Run("random items all kinds/RNG/source templates",RandomItems);Console.WriteLine("Leaf assertions="+assertions+" groups="+groups+" failed="+failed);if(failed>0)Environment.Exit(1);}
}
