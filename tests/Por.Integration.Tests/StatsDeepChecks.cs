using System;using System.IO;using System.Reflection;using Por.Core;
public static class StatsDeepChecks
{
 static void Yes(bool b,string s){IntegrationVerification.Yes(b,s);}
 static void Fresh()
 {
  // Test-only fresh-unit fixture: reproduce the source unit's initialization, not a production reset API.
  foreach(string name in new[]{"owner","ownerName","blocks","marked","markedWorth"}){
   var a=(Array)typeof(Stats).GetField(name,BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);Array.Clear(a,0,a.Length);
   if(name=="ownerName")for(int i=0;i<a.Length;i++)a.SetValue(string.Empty,i);
  }
  Stats.ResetGame();GDVars.zone1=new Types.TGbvmZone1();GDVars.zone1[C_Gbvm.PARTY_SIZE]=1;GDVars.creatureList=new Types.TCharRecord(){name="OWNER",randByte=9};GDVars.ifcState=C_Ifc.COMBAT;
 }
 static byte[] ExpectedGame()
 {
  using(var m=new MemoryStream())using(var w=new BinaryWriter(m)){
   ShortString(w,"STAT",4);w.Write((byte)1);ShortString(w,"",15);w.Write((byte)0);
   for(int i=1;i<=12;i++)w.Write(Stats.game.counts[i]);w.Write(Stats.game.kindCount);
   for(int i=1;i<=48;i++){ShortString(w,Stats.game.kills[i].name,15);w.Write(Stats.game.kills[i].count);}return m.ToArray();
  }
 }
 static void ShortString(BinaryWriter w,string s,int cap){int n=Math.Min(s.Length,cap);w.Write((byte)n);for(int i=0;i<cap;i++)w.Write(i<n?(byte)s[i]:(byte)0);}
 public static void Run()
 {
  Fresh();var actor=GDVars.creatureList;var block=Stats.BlockOf(actor);Stats.Count(actor,Stats.KILLS,7);actor.name="RENAMED";var reused=Stats.BlockOf(actor);
  Yes(ReferenceEquals(block,reused),"NewEntry keeps existing allocated block identity");Yes(reused.counts[Stats.KILLS]==0,"NewEntry clears reused block");
  int[] probes={int.MinValue,-int.MaxValue,-100,-1,0,1,100,int.MaxValue-1,int.MaxValue};
  foreach(string name in new[]{"AddTo","AddSigned"})foreach(int a in probes)foreach(int b in probes){
   long total=(long)a+b;int expected;
   if(name=="AddTo")expected=b<=0?a:total>int.MaxValue?int.MaxValue:unchecked((int)total);
   else expected=b>0&&total>int.MaxValue?int.MaxValue:b<0&&total< -int.MaxValue?-int.MaxValue:unchecked((int)total);
   object[] args={a,b};typeof(Stats).GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,args);Yes((int)args[0]==expected,name+" wide oracle saturation");
  }
  string dir=Path.Combine(Path.GetTempPath(),"por-stats-deep-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
  try{
   string p=Path.Combine(dir,"test.sts");var rng=new Random(761692);
   for(int trial=0;trial<100;trial++){
    Stats.ResetGame();for(int i=1;i<=12;i++)Stats.game.counts[i]=trial==0?int.MinValue:unchecked((int)((uint)rng.Next()^((uint)rng.Next()<<1)));
    Stats.game.kindCount=(byte)rng.Next(49);for(int i=1;i<=48;i++){int n=rng.Next(16);char[] cs=new char[n];for(int j=0;j<n;j++)cs[j]=(char)rng.Next(256);Stats.game.kills[i].name=new string(cs);Stats.game.kills[i].count=unchecked((int)((uint)rng.Next()^((uint)rng.Next()<<1)));}
    byte[] expected=ExpectedGame();Stats.WriteGame(p);byte[] actual=File.ReadAllBytes(p);Yes(actual.Length==1032,"packed record length");for(int i=0;i<expected.Length;i++)Yes(actual[i]==expected[i],"packed byte oracle");
    var old=Stats.game;Stats.ReadGame(p);Yes(ReferenceEquals(old,Stats.game),"ReadGame record assignment preserves global address");
    Stats.WriteGame(p);byte[] reread=File.ReadAllBytes(p);for(int i=0;i<expected.Length;i++)Yes(expected[i]==reread[i],"packed roundtrip raw-byte names");
   }
   Fresh();actor=GDVars.creatureList;actor.name="\u0082\u00ff";Stats.Count(actor,Stats.DAMAGE_DONE,123);Stats.WriteChar(actor,Path.Combine(dir,"character"));byte[] raw=File.ReadAllBytes(Path.Combine(dir,"character.sts"));Yes(raw[6]==2&&raw[7]==130&&raw[8]==255,"character names retain original byte values, not Unicode remapping");
   var stable=Stats.BlockOf(actor);Stats.ReadChar(actor,Path.Combine(dir,"character"));Yes(ReferenceEquals(stable,Stats.BlockOf(actor)),"ReadChar preserves allocated block address");Yes(stable.counts[Stats.DAMAGE_DONE]==123,"ReadChar copy contents");
   actor.randByte=10;Stats.ReadChar(actor,Path.Combine(dir,"character"));Yes(Stats.BlockOf(actor).counts[Stats.DAMAGE_DONE]==0,"randByte mismatch forgets old stats");
   actor.randByte=9;actor.name="different";Stats.ReadChar(actor,Path.Combine(dir,"character"));Yes(Stats.BlockOf(actor).counts[Stats.DAMAGE_DONE]==0,"name mismatch rejects file");
   string absent=Path.Combine(dir,"noowner");File.WriteAllBytes(absent+".sts",new byte[]{1});Stats.WriteChar(new Types.TCharRecord(){name="NOOWNER"},absent);Yes(!File.Exists(absent+".sts"),"erase old sidecar even without stats owner");
   Stats.ResetGame();Stats.game.kills[1].name=new string('X',30);Stats.WriteGame(p);raw=File.ReadAllBytes(p);Yes(raw[72]==15,"fixed-capacity kill name prefix truncated");
   Stats.game.kills[1].name="\u0100";try{Stats.WriteGame(p);throw new Exception("non-byte text accepted");}catch(ArgumentException){Yes(true,"non-byte text rejected");}
   byte[] valid=new byte[1032];valid[0]=4;valid[1]=83;valid[2]=84;valid[3]=65;valid[4]=84;valid[5]=1;valid[23]=77;
   foreach(int offset in new[]{0,6,72,92,1012}){byte[] bad=(byte[])valid.Clone();bad[offset]=255;File.WriteAllBytes(p,bad);Stats.game.counts[1]=88;Stats.ReadGame(p);Yes(Stats.game.counts[1]==0,"invalid short-string length rejected");}
   foreach(int offset in new[]{1,5}){byte[] bad=(byte[])valid.Clone();bad[offset]=offset==1?(byte)88:(byte)2;File.WriteAllBytes(p,bad);Stats.ReadGame(p);Yes(Stats.game.counts[1]==0,"wrong signature/version cannot import nonzero counters");}
   valid[71]=49;File.WriteAllBytes(p,valid);Stats.game.counts[1]=88;Stats.ReadGame(p);Yes(Stats.game.counts[1]==0,"kindCount >48 rejected");
  }finally{Directory.Delete(dir,true);}
 }
}
