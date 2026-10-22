using Por.Core;
namespace Por.Desktop;

// Desktop model of PIT channel 2 / port 61, not an OPL or Tandy emulator.
// All sequencing and rendering run on the game thread; SDL only consumes queued PCM.
public sealed class SpeakerPcm : ISoundPortBus, ISoundTimerPlatform
{
 public const int SampleRate=48000, PitClock=1193182;
 public readonly SoundDriver Driver;
 public Action TimerVector {get;set;} = () => {}; // BIOS time is independently derived from Stopwatch.
 public Action<byte[],byte> Exit;
 int divisor=65536,low;bool high;byte gate;double phase;long timerPhase;
 public long SamplesRendered {get;private set;}
 public SpeakerPcm(){Driver=new SoundDriver(this,this);}
 public byte Read(ushort port)=>port==0x61?gate:throw new NotSupportedException($"Unsupported speaker read {port:X}");
 public void Write(ushort port,byte value)
 {
  switch(port){
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
   buffer[i]=(gate&3)==3?(short)(phase<(divisor+1)/2?4500:-4500):(short)0;
   if((gate&1)!=0)phase=(phase+(double)PitClock/SampleRate)%divisor;SamplesRendered++;
  }
 }
 public void GetTime(out ushort h,out ushort m,out ushort s,out ushort cs){var t=DateTime.Now;h=(ushort)t.Hour;m=(ushort)t.Minute;s=(ushort)t.Second;cs=(ushort)(t.Millisecond/10);}
 public void Shutdown(byte[] message,byte code){if(Exit==null)throw new InvalidOperationException("Audio shutdown not bound");Exit(message,code);}
}
