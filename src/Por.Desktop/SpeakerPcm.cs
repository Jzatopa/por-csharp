using Por.Core;
namespace Por.Desktop;

// Desktop bus and mixer for PIT channel 2, SN76496, OPL and virtual Sound Blaster.
// All sequencing and rendering run on the game thread; SDL only consumes queued PCM.
public sealed class SpeakerPcm : ISoundPortBus, ISoundTimerPlatform, IDisposable
{
 public const int SampleRate=48000, PitClock=1193182;
 public readonly SoundDriver Driver;
 public readonly OplPcm Opl=new();public readonly TandyPcm Tandy=new();public readonly BlasterPcm Blaster;
 public Action TimerVector {get;set;} = () => {}; // BIOS time is independently derived from Stopwatch.
 public Action<byte[],byte> Exit;
 int divisor=65536,low;bool high;byte gate;double phase;long timerPhase;
 public long SamplesRendered {get;private set;}
 public SpeakerPcm(){Driver=new SoundDriver(this,this);Blaster=new BlasterPcm(this);}
 public byte Read(ushort port)=>port==0x61?gate:port==0x388?Opl.Read():throw new NotSupportedException($"Unsupported speaker read {port:X}");
 public void Write(ushort port,byte value)
 {
  switch(port){
   case 0x388:case 0x389:Opl.Write(port,value);return;
   case 0xC0:Tandy.Write(value);return;
   case 0x20: return; // PIC EOI has no desktop interrupt controller side effect.
   case 0x43:
    if(value==0xB6){high=false;return;}if(value==0x36)return;
    throw new NotSupportedException($"Unsupported PIT command {value:X}");
   case 0x40: return; // Channel 0 rate is fixed by SoundDriver's installed timer: 0x13B1.
   case 0x42:
    if(!high){low=value;high=true;}else{divisor=low|(value<<8);if(divisor==0)divisor=65536;high=false;phase=0;}return;
   case 0x61: if((value&1)==0)phase=0;gate=value;return;
   default: throw new NotSupportedException($"Unsupported desktop sound port {port:X}");
  }
 }
 public void Render(short[] buffer,int count)
 {
  if(count<0||count>buffer.Length)throw new ArgumentOutOfRangeException(nameof(count));
  for(int i=0;i<count;i++){
   if(Driver.Hooked){timerPhase+=PitClock;while(timerPhase>=5041L*SampleRate){timerPhase-=5041L*SampleRate;TimerVector?.Invoke();}}
   else timerPhase=0;
   // Bipolar, conservative-volume square wave. Speaker cabinet acoustics are not modeled.
   int speaker=(gate&3)==3?(phase<(divisor+1)/2?4500:-4500):0;
   int tandy=Tandy.Sample(),opl=Opl.Sample(),blaster=Blaster.Sample();
   int mix=Driver.Device switch{0=>(Driver.Hooked?tandy:0)+speaker,3=>(Driver.Hooked?opl:0)+speaker, _=>speaker};
   buffer[i]=Driver.State.OutputOn==0?(short)0:(short)Math.Clamp(mix+blaster,short.MinValue,short.MaxValue);
   if((gate&1)!=0)phase=(phase+(double)PitClock/SampleRate)%divisor;SamplesRendered++;
  }
 }
 public void Dispose(){Driver.UninstallSoundTimer();Blaster.Stop();Opl.Dispose();}
 public void GetTime(out ushort h,out ushort m,out ushort s,out ushort cs){var t=DateTime.Now;h=(ushort)t.Hour;m=(ushort)t.Minute;s=(ushort)t.Second;cs=(ushort)(t.Millisecond/10);}
 public void Shutdown(byte[] message,byte code){if(Exit==null)throw new InvalidOperationException("Audio shutdown not bound");Exit(message,code);}
}
