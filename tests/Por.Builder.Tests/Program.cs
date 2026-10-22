using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Por.Core;
class BuildVerification
{
 static int assertions,groups,failed;
 static string ev="evidence/next-ten-pass-2026-10-21";
 static void Yes(bool ok,string msg){assertions++;if(!ok)throw new Exception(msg);}
 static void Run(string name,Action a){groups++;try{a();Console.WriteLine("PASS "+name);}catch(Exception e){failed++;Console.WriteLine("FAIL "+name+": "+e);}}
 const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Static;
 static object Call(string name,params object[] args){try{return typeof(DataBld).GetMethod(name,flags).Invoke(null,args);}catch(TargetInvocationException e){throw e.InnerException;}}
 static void Set(string name,object value){typeof(DataBld).GetField(name,flags).SetValue(null,value);}
 static object Get(string name){return typeof(DataBld).GetField(name,flags).GetValue(null);}
 static void Init(Platform p){DataBld.Platform=p;Set("failNote","");Set("writeNote","");Set("memNote","");Set("sourceCount",(ushort)0);Set("blockCount",(ushort)0);Set("remapCount",(byte)0);Set("bufA",new byte[4096]);Set("bufB",new byte[4096]);var ct=typeof(DataBld).GetNestedType("TCrcTable",BindingFlags.NonPublic|BindingFlags.Public);Set("crcTable",Activator.CreateInstance(ct,true));Call("InitCrcTable");}
 class Platform:IDataBuildPlatform
 {
  public long Available=long.MaxValue;public int calls,opens,finishes;public byte lastDrive;public bool ThrowOpen;public string FaultMode="",Phase="";
  void Fault(string mode){if(FaultMode==mode)throw new InvalidOperationException("test backend fault "+mode);}public List<string> events=new List<string>();
  public long MaxAvailable{get{return Available;}}public int DiskFree(byte d){calls++;lastDrive=d;return -1;}
  public void ClearBottomLine(){calls++;}public void DisplayString(byte x,byte y,byte c,string s){calls++;Yes(s.Length<=40,"progress truncation");Yes(x==0&&y==24&&c==10,"progress placement");}
  public void Open(bool[] s,bool w){calls++;opens++;events.Add("open:"+w);if(ThrowOpen)throw new InvalidOperationException("test platform Open failure");}public void Advance(ushort c){calls++;Fault("Advance:"+Phase);}public void Reading(byte f,byte d){calls++;Phase="read";Fault("Reading");}public void Writing(int n){calls++;Phase="write";Fault("Writing");}public void Fixing(byte f,byte d){calls++;}public void Checking(){calls++;Phase="check";Fault("Checking");}public void StartFamily(byte f){calls++;events.Add("start:"+f);}public void EndFamily(byte f,bool s){calls++;events.Add("end:"+f+":"+s);}public void Finish(bool f){calls++;finishes++;events.Add("finish:"+f);}
 }
 static bool Sound(string path){Init(new Platform());return (bool)Call("ArchiveSound",path);}
 static string Hash(byte[] b){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","").ToLowerInvariant();}
 static string Temp(){string p=Path.Combine(Path.GetTempPath(),"por-builder-check-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(p);return p+Path.DirectorySeparatorChar;}
 static void WithDir(Action<string> a){string p=Temp();try{GDVars.gameDir=p;DaxIo.ResetArchiveCache();a(p);}finally{Directory.Delete(p,true);DaxIo.ResetArchiveCache();}}
 static byte[] Pack(byte[] data){var a=new List<byte>();for(int i=0;i<data.Length;i+=127){int n=Math.Min(127,data.Length-i);a.Add((byte)(n-1));a.AddRange(data.Skip(i).Take(n));}return a.ToArray();}
 struct Block{public byte id;public ushort raw;public byte[] packed;public Block(int id,byte[] data){this.id=(byte)id;raw=(ushort)data.Length;packed=Pack(data);}public Block(int id,ushort raw,byte[] p){this.id=(byte)id;this.raw=raw;packed=p;}}
 static byte[] Archive(params Block[] blocks){using(var m=new MemoryStream())using(var w=new BinaryWriter(m)){w.Write((ushort)(blocks.Length*9));int off=0;foreach(var b in blocks){w.Write(b.id);w.Write(off);w.Write(b.raw);w.Write((ushort)b.packed.Length);off+=b.packed.Length;}foreach(var b in blocks)w.Write(b.packed);return m.ToArray();}}
 static void Build(string dir,Platform p){DataBld.Platform=p;GDVars.gameDir=dir;DaxIo.ResetArchiveCache();DataBld.BuildDataFiles();}
 static uint Crc(byte[] data){uint c=0xffffffff;foreach(byte b in data){c^=b;for(int k=0;k<8;k++)c=(c&1)!=0?(c>>1)^0xedb88320:c>>1;}return ~c;}
 static void Tables()
 {
  using(var doc=JsonDocument.Parse(File.ReadAllText(Path.Combine(ev,"patch-source.json"))))
  {
   var rs=doc.RootElement.GetProperty("records");Yes(DataPat.PART_COUNT==59&&DataPat.BYTE_COUNT==1580,"counts");Yes(DataPat.parts.Length==60,"original record indices");int i=1;
   foreach(var r in rs.EnumerateArray()){object p=DataPat.parts[i++];foreach(var kv in r.EnumerateObject()){var f=p.GetType().GetField(kv.Name);Yes(f!=null&&Convert.ToInt32(f.GetValue(p))==kv.Value.GetInt32(),"record "+i+" "+kv.Name);Yes(f.FieldType==(kv.Name=="issue"||kv.Name=="size"||kv.Name=="offset"?typeof(ushort):typeof(byte)),"record width "+kv.Name);}}
   int n=0;foreach(var b in doc.RootElement.GetProperty("bytes").EnumerateArray())Yes(DataPat.patchBytes[n++]==b.GetInt32(),"patch byte "+n);Yes(n==DataPat.patchBytes.Length,"patch exact extent");
   Yes((ushort)Call("MaxRawSize",(byte)1)==9986,"actual SCRIPT_FIRST..LAST extent+leader");Yes((ushort)Call("MaxRawSize",(byte)9)==35718,"loadable limit");
  }
 }
 static void StateBoundary()
 {
  var platform=new Platform();Init(platform);string oldDir=GDVars.gameDir;var oldCulture=System.Globalization.CultureInfo.CurrentCulture;
  try{System.Globalization.CultureInfo.CurrentCulture=new System.Globalization.CultureInfo("tr-TR");for(int i=0;i<26;i++){GDVars.gameDir=((char)('a'+i))+":";Yes((bool)Call("HasRoom",(byte)4),"unknown disk free accepted");Yes(platform.lastDrive==i+1,"ASCII drive fold independent ofculture");}}
  finally{GDVars.gameDir=oldDir;System.Globalization.CultureInfo.CurrentCulture=oldCulture;}
  Yes(typeof(DataBld).GetField("lastError",flags).FieldType==typeof(short)&&typeof(DataBld).GetField("n",flags).FieldType==typeof(short),"source Integer fields signed16");
  Call("Fail",new string('x',90));Yes(((string)Get("failNote")).Length==80,"String80 failure truncation");Call("Fail","later");Yes(((string)Get("failNote"))[0]=='x',"firstfailure retained");
  var type=typeof(DataBld).GetNestedType("TCrc",BindingFlags.Public|BindingFlags.NonPublic);object crc=Activator.CreateInstance(type);type.GetProperty("value").SetValue(crc,unchecked((int)0xffff0000));type.GetProperty("lo").SetValue(crc,(ushort)0x1234);Yes((int)type.GetProperty("value").GetValue(crc)==unchecked((int)0xffff1234),"CRC low view write preserveshigh");type.GetProperty("hi").SetValue(crc,(ushort)0xabcd);Yes((int)type.GetProperty("value").GetValue(crc)==unchecked((int)0xabcd1234),"CRC high view preserveslow");Yes((ushort)type.GetProperty("lo").GetValue(crc)==0x1234&&(ushort)type.GetProperty("hi").GetValue(crc)==0xabcd,"CRC view reads");
 }
 static byte[] DecodeReference(byte[] p,int n){var list=new List<byte>();for(int i=0;i<p.Length;){int b=p[i++];if(b<=126){int len=Math.Min(b+1,p.Length-i);for(int k=0;k<len;k++)list.Add(p[i+k]);i+=len;}else if(b>=129&&i<p.Length){byte x=p[i++];for(int k=0;k<256-b;k++)list.Add(x);}}var r=new byte[n];for(int i=0;i<Math.Min(n,list.Count);i++)r[i]=list[i];return r;}
 static void Codec()
 {
  Init(new Platform());var random=new Random(38127);
  for(int i=0;i<1300;i++)
  {int n=i<128?i:i==128?64000:i==129?64001:random.Next(4096);byte[] raw=new byte[n];random.NextBytes(raw);Set("failNote","");object[] a={raw,(ushort)n,null,(ushort)0};Call("EncodeRuns",a);var packed=(byte[])a[2];int len=(ushort)a[3];if(n==64001){Yes(packed==null&&len==0&&(string)Get("failNote")!="","oversize pack rejected");continue;}Yes(len==n+(n+126)/127,"literal size");if(n==0)Yes(packed==null,"zero raw null packed");else Yes(packed.SequenceEqual(Pack(raw)),"literal encoding bytes");byte[] output=Enumerable.Repeat((byte)0xee,n).ToArray();Call("DecodeRuns",packed??new byte[0],(ushort)len,output,(ushort)n);Yes(output.SequenceEqual(raw),"literal roundtrip");}
  for(int i=0;i<4096;i++){int n=random.Next(600);byte[] input=new byte[n];random.NextBytes(input);if(i<256)input=new[]{(byte)i,(byte)29};int raw=random.Next(900);byte[] output=new byte[raw];Call("DecodeRuns",input,(ushort)input.Length,output,(ushort)raw);Yes(output.SequenceEqual(DecodeReference(input,raw)),"decode source oracle");}
  var crcType=typeof(DataBld).GetNestedType("TCrc",BindingFlags.Public|BindingFlags.NonPublic);
  for(int i=0;i<1000;i++){byte[] data=new byte[random.Next(6000)];random.NextBytes(data);object crc=Activator.CreateInstance(crcType);crcType.GetProperty("value").SetValue(crc,-1);object[] args={crc,data,(ushort)data.Length};Call("AddCrc",args);int value=(int)crcType.GetProperty("value").GetValue(args[0]);Yes(unchecked((uint)~value)==Crc(data),"CRC polynomial oracle");}
 }
 static void AtomicPatch()
 {
  Init(new Platform());ushort first=2,pos=4;var raw=new byte[1100];raw[235]=48;raw[475]=48;byte[] spare=new byte[1100],original=raw;string why="";object[] args={(byte)2,first,pos,raw,spare,(ushort)1100,(ushort)1026,why};Yes((bool)Call("ApplyFix",args),"atomic two-part source fix accepts");var fixedRaw=(byte[])args[3];Yes(!ReferenceEquals(fixedRaw,original)&&ReferenceEquals(args[4],original),"success swaps buffers");Yes(fixedRaw[235]==144&&fixedRaw[475]==144&&original[235]==48&&original[475]==48,"both walls fixed; input no mutation");
  raw=new byte[1100];raw[235]=48;raw[475]=49;original=raw;args=new object[]{(byte)2,first,pos,raw,new byte[1100],(ushort)1100,(ushort)1026,""};Yes(!(bool)Call("ApplyFix",args),"mismatch second part rejects");Yes(ReferenceEquals(args[3],original)&&original[235]==48&&original[475]==49&&(ushort)args[6]==1026,"failure no partial commit");
 }
 static Dictionary<byte,Block> ReadBlocks(string path){byte[] data=File.ReadAllBytes(path);int size=BitConverter.ToUInt16(data,0);var result=new Dictionary<byte,Block>();for(int i=2;i<2+size;i+=9){int off=BitConverter.ToInt32(data,i+1),raw=BitConverter.ToUInt16(data,i+5),packed=BitConverter.ToUInt16(data,i+7);result.Add(data[i],new Block(data[i],(ushort)raw,data.Skip(2+size+off).Take(packed).ToArray()));}return result;}
 static byte[] Raw(Dictionary<byte,Block> b,byte id){return DecodeReference(b[id].packed,b[id].raw);}
 static void Synthetic()
 {
  WithDir(dir=>{
   var p=new Platform();byte[] a={2,3,4},b={9,8,7};File.WriteAllBytes(dir+"PIC1.DAX",Archive(new Block(0,a),new Block(5,a)));File.WriteAllBytes(dir+"PIC2.DAX",Archive(new Block(5,b)));File.WriteAllBytes(dir+"PIC3.DAX",Archive(new Block(5,b)));var before=Hash(File.ReadAllBytes(dir+"PIC1.DAX"));Build(dir,p);Yes(File.Exists(dir+"PIC.DAX"),"synthetic merged created");var blocks=ReadBlocks(dir+"PIC.DAX");Yes(blocks.Count==6,"dedup collision blocks plus three metadata");Yes(Raw(blocks,254).SequenceEqual(new byte[]{2,5,1,3,5,1}),"two disks share one spare, skip zero");Yes(Raw(blocks,5).SequenceEqual(a)&&Raw(blocks,1).SequenceEqual(b),"original/remapped bytes");Yes(before==Hash(File.ReadAllBytes(dir+"PIC1.DAX")),"input unchanged");byte[] whole=File.ReadAllBytes(dir+"PIC.DAX");Yes(BitConverter.ToUInt32(whole,whole.Length-4)==Crc(whole.Take(whole.Length-6).ToArray()),"whole archive CRC");Yes(Sound(dir+"PIC.DAX"),"archive readback CRC valid");whole[whole.Length-7]^=1;File.WriteAllBytes(dir+"BAD.DAX",whole);Yes(!Sound(dir+"BAD.DAX"),"CRC corruption rejected");DataBld.Platform=null;var timestamp=File.GetLastWriteTimeUtc(dir+"PIC.DAX");DataBld.BuildDataFiles();Yes(timestamp==File.GetLastWriteTimeUtc(dir+"PIC.DAX"),"up-to-date no UI or rewrite");
  });
  WithDir(dir=>{File.WriteAllBytes(dir+"PIC1.DAX",Archive(new Block(5,2,new byte[]{1,7,7})));File.WriteAllBytes(dir+"PIC2.DAX",Archive(new Block(5,2,new byte[]{254,7})));Build(dir,new Platform());Yes(ReadBlocks(dir+"PIC.DAX").ContainsKey(254),"same decoded bytes/different packed bytes remain collision");});
  WithDir(dir=>{File.WriteAllBytes(dir+"pic.dax",new byte[]{1,2,3});Build(dir,new Platform());Yes(!File.Exists(dir+"pic.dax")&&!File.Exists(dir+"PIC.DAX"),"stale lowercase merged removed with no per-disk sources");});
  WithDir(dir=>{File.WriteAllBytes(dir+"ITEM1.DAX",Archive(new Block(253,new byte[]{1})));Build(dir,new Platform());Yes(!File.Exists(dir+"ITEM.DAX")&&File.ReadAllText(dir+"DATABLD.LOG").Contains("uses block 253"),"reserved source ID fails family");});
  WithDir(dir=>{File.WriteAllBytes(dir+"GEO1.DAX",new byte[]{10,0,0,0});Build(dir,new Platform());Yes(!File.Exists(dir+"GEO.DAX")&&File.ReadAllText(dir+"DATABLD.LOG").Contains("damaged"),"malformed directory rejected");});
  foreach(long available in new long[]{24575,24576})WithDir(dir=>{File.WriteAllBytes(dir+"PIC1.DAX",Archive(new Block(1,new byte[]{3})));Build(dir,new Platform{Available=available});Yes(File.Exists(dir+"PIC.DAX")== (available==24576),"source memory threshold "+available);});
  WithDir(dir=>{File.WriteAllBytes(dir+"PIC1.DAX",Archive(new Block(1,new byte[]{3})));DataBld.Platform=null;try{DataBld.BuildDataFiles();throw new Exception("missing adapter silently accepted");}catch(InvalidOperationException){Yes(Directory.GetFiles(dir).Length==1,"missing platform guard beforewrites");}});
  WithDir(dir=>{File.WriteAllBytes(dir+"PIC1.DAX",Archive(new Block(1,new byte[]{3})));var p=new Platform{Available=1};Build(dir,p);Yes(!File.Exists(dir+"PIC.DAX")&&File.ReadAllText(dir+"DATABLD.LOG").Contains("not enough memory"),"memory threshold adapter");});
  WithDir(dir=>{File.WriteAllBytes(dir+"PIC1.DAX",Archive(new Block(1,new byte[]{3})));Directory.CreateDirectory(dir+"DATABLD.$$$");var p=new Platform();Build(dir,p);Yes(!File.Exists(dir+"PIC.DAX")&&p.events.Contains("open:False")&&File.ReadAllText(dir+"DATABLD.LOG").Contains("not tried"),"write preflight skips reading/building");});
 }
 static void Lifetime()
 {
  WithDir(dir=>{File.WriteAllBytes(dir+"PIC1.DAX",Archive(new Block(1,new byte[]{3})));var p=new Platform{ThrowOpen=true};try{Build(dir,p);throw new Exception("platform error swallowed");}catch(InvalidOperationException e){Yes(e.Message=="test platform Open failure","backend failure propagated");}using(var f=new FileStream(dir+"DATABLD.LOG",FileMode.Open,FileAccess.ReadWrite,FileShare.None)){Yes(f.Length>=0,"log handle released afterfailure");}Yes(Get("crcTable")==null&&Get("sources")==null&&Get("blocks")==null&&Get("bufA")==null&&Get("bufB")==null,"workingreferences released afterfailure");});
 }
 static void BackendStreamFailures()
 {
  foreach(string mode in new[]{"Reading","Advance:read","Writing","Advance:write","Advance:check"})WithDir(dir=>{
   File.WriteAllBytes(dir+"PIC1.DAX",Archive(new Block(1,new byte[]{3,4,5})));var p=new Platform{FaultMode=mode};try{Build(dir,p);throw new Exception("backend fault missed "+mode);}catch(InvalidOperationException e){Yes(e.Message=="test backend fault "+mode,"backendfault propagated "+mode);}
   var leaked=Directory.GetFiles("/proc/self/fd").Select(f=>new FileInfo(f).LinkTarget).Where(t=>t!=null&&t.StartsWith(dir,StringComparison.Ordinal)).ToArray();Yes(leaked.Length==0,"no leaked staging FD after "+mode+": "+string.Join(",",leaked));
  });
 }
 static void Capacity()
 {
  foreach(int n in new[]{16,17})WithDir(dir=>{var original=new List<Block>();var other=new List<Block>();for(int i=0;i<n;i++){original.Add(new Block(i,new byte[]{1}));other.Add(new Block(i,new byte[]{2}));}File.WriteAllBytes(dir+"PIC1.DAX",Archive(original.ToArray()));File.WriteAllBytes(dir+"PIC2.DAX",Archive(other.ToArray()));Build(dir,new Platform());Yes(File.Exists(dir+"PIC.DAX")== (n==16),"remap capacity "+n);if(n==16)Yes(Raw(ReadBlocks(dir+"PIC.DAX"),254).Length==48,"sixteen remaps exact");else Yes(File.ReadAllText(dir+"DATABLD.LOG").Contains("too many collisions"),"seventeenth remap diagnosis");});
  WithDir(dir=>{var blocks=new List<Block>();for(int i=1;i<=252;i++)blocks.Add(new Block(i,new byte[]{1}));File.WriteAllBytes(dir+"PIC1.DAX",Archive(blocks.ToArray()));File.WriteAllBytes(dir+"PIC2.DAX",Archive(new Block(1,new byte[]{2})));Build(dir,new Platform());Yes(!File.Exists(dir+"PIC.DAX")&&File.ReadAllText(dir+"DATABLD.LOG").Contains("no spare block id left"),"spare zero and reserved IDs excluded");});
  foreach(int n in new[]{509,510})WithDir(dir=>{File.WriteAllBytes(dir+"PIC1.DAX",Archive(Enumerable.Range(0,n).Select(_=>new Block(1,new byte[]{1})).ToArray()));Build(dir,new Platform());Yes(File.Exists(dir+"PIC.DAX")== (n==509),"source directory block capacity "+n);});
 }
 static void Original()
 {
  string media=(Environment.GetEnvironmentVariable("POR_MEDIA_ROOT")??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads","poolofrad"));using(var doc=JsonDocument.Parse(File.ReadAllText(Path.Combine(ev,"original-build-oracle.json"))))
  WithDir(dir=>{
   var inputs=new Dictionary<string,string>();foreach(var family in doc.RootElement.EnumerateArray())foreach(var input in family.GetProperty("inputs").EnumerateArray()){string name=input.GetString();if(inputs.ContainsKey(name))continue;string origin=Path.Combine(media,name);inputs.Add(name,Hash(File.ReadAllBytes(origin)));File.Copy(origin,dir+name.ToUpperInvariant());}
   var platform=new Platform();Build(dir,platform);int families=0;
   foreach(var family in doc.RootElement.EnumerateArray()){string name=family.GetProperty("name").GetString();Yes(File.Exists(dir+name),"original family created "+name);Yes(Hash(File.ReadAllBytes(dir+name))==family.GetProperty("sha256").GetString(),"independent original builder hash "+name);Yes(new FileInfo(dir+name).Length==family.GetProperty("length").GetInt32(),"independent size "+name);Yes(Sound(dir+name),"whole original archive CRC "+name);families++;}
   foreach(var input in inputs){Yes(Hash(File.ReadAllBytes(dir+input.Key.ToUpperInvariant()))==input.Value,"copy unchanged "+input.Key);Yes(Hash(File.ReadAllBytes(Path.Combine(media,input.Key)))==input.Value,"original unchanged "+input.Key);}
   DataBld.Platform=null;var stamps=Directory.GetFiles(dir).ToDictionary(x=>x,x=>File.GetLastWriteTimeUtc(x));DataBld.BuildDataFiles();Yes(stamps.All(x=>File.GetLastWriteTimeUtc(x.Key)==x.Value),"all current returns silently");Yes(platform.finishes==1,"finish once");Console.WriteLine("Original copied-media families="+families+"; SHA256 oracle exact; originals read-only");
  });
 }
 static void Main(string[] args){if(args.Length>0)ev=args[0];Run("59 patch parts and 1580 bytes source inventory",Tables);Run("state widths/drive case/CRC aliases",StateBoundary);Run("CRC and encode/decode independent oracle",Codec);Run("atomic multipart patch/value identity",AtomicPatch);Run("synthetic builder/archive/failure behavior",Synthetic);Run("backend failure handle lifetime",Lifetime);Run("backend source/output stream failures",BackendStreamFailures);Run("source block/remap/spare capacity",Capacity);Run("14 original families copied-media build hashes",Original);Console.WriteLine("Builder assertions="+assertions+" groups="+groups+" failed="+failed);if(failed>0)Environment.Exit(1);}
}
