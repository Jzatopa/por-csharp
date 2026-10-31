using System.Buffers.Binary;
using System.Reflection;
using Por.Core;
using Por.UpstreamTools;
static class Tests
{
 static int checks;static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
 static void Fails(Action action){bool caught=false;try{action();}catch(ArgumentException){caught=true;}catch(InvalidDataException){caught=true;}Check(caught,"Malformed input must be rejected");}
 sealed class TraceTimer:ISoundTimerPlatform
 {
  public Action TimerVector{get;set;}
  public void GetTime(out ushort h,out ushort m,out ushort s,out ushort c){h=m=s=c=0;}
  public void Shutdown(byte[] message,byte code)=>throw new Exception("Unexpected shutdown");
 }
 sealed class SoundHost:ISoundBenchmarkHost
 {
  public long BiosTicks{get;private set;} public List<string> Calls=new();public void Poll(){BiosTicks++;}public void SetDevice(ushort mode)=>Calls.Add("device:"+mode);public void InstallTimer()=>Calls.Add("install");public void UninstallTimer()=>Calls.Add("uninstall");public void Play(ushort tune)=>Calls.Add("play:"+tune);
 }
 sealed class VideoHost:IVideoBenchmarkHost
 {
  public long Ticks{get;private set;}public int Texts,Tiles,Fills,Flushes,Restores;public void Poll(){Ticks++;}public void Initialize(char mode){}public void Restore(){Restores++;}
  public void RenderChar(byte col,byte row,byte attr,byte count,byte glyph,byte flags){int n=Texts%1000;Check(col==n%40&&row==n/40&&attr==15&&count==1&&glyph=='A'+n%26&&flags==1,"1000 text workload source coordinates");Texts++;}
  public void BlitTile(int x,int y){int n=Tiles%441;Check(x==n%21&&y==n/21,"441 tile workload order");Tiles++;}public void FillView(){Fills++;}public void FlushView(){Flushes++;}
 }
 static void Main()
 {
  foreach(char device in new[]{'T','P','x'}){
   var h=new SoundHost();var rows=SoundBenchmark.Run(h,device,3);
   Check(rows.Select(r=>r.State).SequenceEqual(new[]{"off","silent","music"}),"sound state ordering");Check(rows.All(r=>r.Count==3&&r.CostHundredthsPercent==0),"source busy loop and cost arithmetic");Check(h.Calls.SequenceEqual(new[]{"device:"+(device=='T'?0:1),"install","play:0","play:21","play:0","uninstall"}),"real sequence timer lifetime");
  }
  foreach(char mode in new[]{'C','E','T','x'}){
   var h=new VideoHost();var rows=VideoBenchmark.Run(h,mode,2);
   Check(rows.Select(r=>r.Workload).SequenceEqual(new[]{"text1000","tiles441","fillview","fillflush"}),"four source workloads");Check(rows.All(r=>r.Runs==2&&r.BiosTicks==2&&Math.Abs(r.RunsPerSecond-18.2065)<1e-9),"source rate formula");Check(h.Texts==2000&&h.Tiles==882&&h.Fills==4&&h.Flushes==2&&h.Restores==1,"workload count and cleanup");
  }
  Fails(()=>SoundBenchmark.Run(new SoundHost(),'P',0));Fails(()=>VideoBenchmark.Run(new VideoHost(),'C',0));
  for(int cap=0;cap<=64;cap++){
   var trace=new SoundTrace();var words=Enumerable.Repeat((ushort)0xABCD,66).ToArray();trace.SetTraceBuffer(words,(ushort)cap);
   for(int i=0;i<80;i++)trace.Write((ushort)(0x388+i),(byte)i);
   Check(trace.Count==cap&&trace.Read(0x61)==0,"SndTrc saturated count and input zero");
   for(int i=0;i<66;i++)Check(words[i]==(i<cap?(ushort)(((0x88+i)&255)<<8|i):0xABCD),"source trace packing and capacity guard");
   trace.SetTraceBuffer(words,0);trace.Write(0x61,255);Check(trace.Count==0,"trace reset and zero capacity");
  }
  // Real sequencer -> new bounded SndTrc adapter, not direct test writes alone.
  var recorder=new SoundTrace();var buffer=new ushort[1024];recorder.SetTraceBuffer(buffer,1024);
  var timer=new TraceTimer();var driver=new SoundDriver(recorder,timer);driver.OplReset();
  Check(recorder.Count>560,"real OPL reset fills the larger source buffer");
  driver.SetSoundDevice(0);driver.ProgramTandyVoices(1);recorder.SetTraceBuffer(buffer,64);driver.TraceTick();
  Check(recorder.Count>0&&recorder.Count<64,"real driver tick records into caller buffer");
  byte[] exe=new byte[96];exe[0]=(byte)'M';exe[1]=(byte)'Z';exe[8]=2;exe[48]=0xCD;exe[49]=0x3F;BinaryPrimitives.WriteUInt16LittleEndian(exe.AsSpan(56),100);BinaryPrimitives.WriteUInt16LittleEndian(exe.AsSpan(58),7);
  var sizes=LegacyTools.OverlaySizes(exe,"00010H 00000H 00020H TEST CODE\n00000H 00000H 00010H RES CODE");Check(sizes.Count==1&&sizes["TEST"]==107,"TPOV overlay code+fixups and MZ header offset");Fails(()=>LegacyTools.OverlaySizes(new byte[2],""));
  var report=LegacyTools.OverlayReport("{\"x\":{\"golden\":{\"checkpoints\":{\"overlay-loads\":{\"text\":[\"Overlay loads 12\"]}}},\"candidate\":{\"checkpoints\":{\"overlay-loads\":{\"text\":[\"overlay loads 9\"]}}}},\"empty\":{}}").ToArray();Check(report.Length==1&&report[0].EndsWith("(-3)"),"overlay-load report handles missing data and signed delta");
  var assembly=Assembly.GetExecutingAssembly();int configs=0,scenarios=0;
  foreach(string name in assembly.GetManifestResourceNames()){
   using var stream=assembly.GetManifestResourceStream(name);using var reader=new StreamReader(stream);string text=reader.ReadToEnd();
   if(name.EndsWith(".CFG")){var cfg=LegacyConfig.Parse(text);cfg.InstallEngineVideo();Check(GDVars.video.name==(cfg.Video=='T'?"Tandy":cfg.Video=='C'?"CGA":cfg.Video=='E'?"EGA":"VGA"),"legacy cfg installs actual source video driver");Check(cfg.AudioMode=="speaker"&&!cfg.FullIntro,"source sound/intro config interpretation");configs++;}
   else {var steps=Scenario.Parse(text);Check(steps.Count>0&&steps[0].Command=="boot","original scenario parses without loss");scenarios++;}
  }
  Check(configs==4&&scenarios==13,"all upstream scenario/config inputs embedded");
  Fails(()=>LegacyConfig.Parse("X\nP\nx\ny\nN"));Fails(()=>Scenario.Parse("boot\nunknown x"));Fails(()=>Scenario.Parse("wait menu"));
  Console.WriteLine($"Upstream tools: {checks} checks PASS; {configs} configs / {scenarios} original scenario definitions (not scenario runtime passes)");
 }
}
