using System.Diagnostics;
using Por.Core;
using static Por.Core.Types;
namespace Por.UpstreamTools;

public sealed class ManagedSoundBenchmarkHost : ISoundBenchmarkHost, ISoundTimerPlatform, ISoundPortBus
{
 readonly Stopwatch clock=Stopwatch.StartNew();readonly SoundDriver driver;
 double nextTick;long biosTicks;public long Writes {get;private set;}public long Interrupts {get;private set;}
 public ManagedSoundBenchmarkHost(){driver=new SoundDriver(this,this);TimerVector=()=>biosTicks++;}
 public Action TimerVector {get;set;}
 public long BiosTicks=>driver.Hooked?biosTicks:(long)(clock.Elapsed.TotalSeconds*18.2065);
 public void Poll(){if(!driver.Hooked)return;double now=clock.Elapsed.TotalSeconds;while(now>=nextTick){TimerVector();Interrupts++;nextTick+=5041.0/1193182.0;}}
 public void SetDevice(ushort mode)=>driver.SetSoundDevice(mode);
 public void InstallTimer(){biosTicks=(long)(clock.Elapsed.TotalSeconds*18.2065);driver.InstallSoundTimer();nextTick=clock.Elapsed.TotalSeconds+5041.0/1193182.0;}
 public void UninstallTimer()=>driver.UninstallSoundTimer();
 public void Play(ushort tune){if(driver.Device==0)driver.ProgramTandyVoices(tune);else driver.ProgramSpeakerVoices(tune);}
 public byte Read(ushort port)=>0; // Benchmark trace/RAM bus, not physical port access.
 public void Write(ushort port,byte value){Writes++;}
 public void GetTime(out ushort h,out ushort m,out ushort s,out ushort c){var t=DateTime.Now;h=(ushort)t.Hour;m=(ushort)t.Minute;s=(ushort)t.Second;c=(ushort)(t.Millisecond/10);}
 public void Shutdown(byte[] message,byte code)=>throw new InvalidOperationException("Unexpected sequencer shutdown in benchmark");
}

// Managed RAM-bus timings include these bus operations, not a DOSBox CPU or display driver.
public sealed class ManagedVideoBenchmarkHost : IVideoBenchmarkHost,IVideoHardware
{
 readonly Stopwatch clock=Stopwatch.StartNew();readonly byte[] memory=new byte[1048576];
 readonly byte[][] planes=Enumerable.Range(0,4).Select(_=>new byte[65536]).ToArray();readonly byte[] latch=new byte[4];
 byte seqIndex,gcIndex,map=15,writeMode;bool ega;IVideoHardware oldHost;Action<byte,TInterruptRegisters> oldInterrupt;
 TImgBuf tile;readonly TColorMap noMap=new();
 public long Writes {get;private set;}public long PortWrites {get;private set;}
 public long Ticks=>(long)(clock.Elapsed.TotalSeconds*18.2065);
 public void Poll(){} // Stopwatch advances independently; no timer or device to service.
 public void Initialize(char mode)
 {
  oldHost=VideoHardware.Host;oldInterrupt=Blit.Interrupt;VideoHardware.Host=this;Blit.Interrupt=Interrupt;
  ega=mode=='E';switch(mode){case 'E':VidEga.Install();break;case 'T':VidTdy.Install();break;default:VidCga.Install();break;}
  GDVars.curVideoMode=3;Blit.SetVideoMode(GDVars.video.biosMode);GDVars.video.InitRowTables();GDVars.video.Start();
  GDVars.gameFont=new TFontArr();for(int g=0;g<GDVars.gameFont.Data.Length;g++)for(int row=0;row<8;row++)GDVars.gameFont[g,row]=0x5A;
  Blit.AllocPic(168,21,1,0,ref GDVars.dsViewImageBufferPtr);
  Array.Clear(GDVars.spanActive);Array.Clear(GDVars.spanScreenOfs);Array.Fill(GDVars.spanFirstOfs,ushort.MaxValue);Array.Clear(GDVars.spanLastOfs);
  Blit.AllocPic(8,1,1,1,ref tile);
 }
 public void Restore(){try{Blit.RestoreVideoMode();}finally{VideoHardware.Host=oldHost;Blit.Interrupt=oldInterrupt;}}
 public void RenderChar(byte col,byte row,byte attr,byte count,byte glyph,byte flags)=>TextRend.RenderChar(col,row,attr,count,glyph,flags);
 public void BlitTile(int x,int y)=>Blit.BlitTileCell((short)x,(short)y,0,1,tile,GDVars.dsViewImageBufferPtr);
 public void FillView()=>Blit.FillPicRect(0,0,21,168,1,noMap,GDVars.dsViewImageBufferPtr);
 public void FlushView()=>Blit.FlushView();
 public byte ReadByte(ushort segment,ushort offset){int a=((segment<<4)+offset)&1048575;if(ega&&a>=0xA0000&&a<0xB0000){for(int p=0;p<4;p++)latch[p]=planes[p][a-0xA0000];return latch[0];}return memory[a];}
 public void WriteByte(ushort segment,ushort offset,byte value){Writes++;int a=((segment<<4)+offset)&1048575;if(ega&&a>=0xA0000&&a<0xB0000){for(int p=0;p<4;p++)if((map&(1<<p))!=0)planes[p][a-0xA0000]=writeMode==1?latch[p]:writeMode==2?((value&(1<<p))!=0?(byte)255:(byte)0):value;}else memory[a]=value;}
 public void WritePort(ushort port,byte value){PortWrites++;if(port==0x3C4)seqIndex=value;if(port==0x3CE)gcIndex=value;if(port==0x3C5&&seqIndex==2)map=value;if(port==0x3CF&&gcIndex==5)writeMode=value;}
 public void Interrupt(byte number,TInterruptRegisters r){if(number!=0x10)throw new NotSupportedException("Only video BIOS is modeled");int fn=r.ax>>8;if(fn==0){Array.Clear(memory);foreach(var plane in planes)Array.Clear(plane);return;}if(fn==5||fn==0x10)return;throw new NotSupportedException($"Video BIOS {r.ax:X4} not modeled");}
}
