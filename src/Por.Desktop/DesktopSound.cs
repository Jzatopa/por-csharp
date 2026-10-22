using System.Diagnostics;
using Por.Core;
namespace Por.Desktop;

// Polling facade also advances the timer inside SoundDelay's tight tick-poll loop.
public sealed class DesktopSound : ISoundDriverHost, IDisposable
{
 readonly SpeakerPcm pcm=new();readonly Stopwatch clock=new();readonly short[] buffer=new short[2048];uint device;long rendered;bool servicing;
 public DesktopSound(Action<byte[],byte> shutdown)
 {
  pcm.Exit=shutdown;
  if(Sdl.SDL_InitSubSystem(0x10)!=0)throw new InvalidOperationException("SDL audio: "+Sdl.Error);
  var desired=new Sdl.AudioSpec{Frequency=SpeakerPcm.SampleRate,Format=0x8010,Channels=1,Samples=1024};
  device=Sdl.SDL_OpenAudioDevice(IntPtr.Zero,0,ref desired,out var actual,0);
  if(device==0)throw new InvalidOperationException("Cannot open audio device: "+Sdl.Error+" (use --no-audio to play silently)");
  // No allowed format changes: queued data is signed little-endian 16-bit mono 48 kHz.
  Sdl.SDL_PauseAudioDevice(device,0);clock.Start();Console.WriteLine("AUDIO PC Speaker / 48000 Hz mono / SDL2 queued output / backend="+Sdl.AudioDriver);
 }
 public void Service()
 {
  if(servicing||device==0)return;servicing=true;
  try{
   long target=(long)(clock.Elapsed.TotalSeconds*SpeakerPcm.SampleRate);
   if(target-rendered<128)return; // Batch sub-3ms writes, including SoundDelay tick polling.
   while(rendered<target){int n=(int)Math.Min(buffer.Length,target-rendered);pcm.Render(buffer,n);rendered+=n;
    // Keep sequencer time advancing through stalls without playing an old seconds-long backlog.
    if(target-rendered<SpeakerPcm.SampleRate/4){if(Sdl.SDL_GetQueuedAudioSize(device)>SpeakerPcm.SampleRate/2)Sdl.SDL_ClearQueuedAudio(device);if(Sdl.SDL_QueueAudio(device,buffer,(uint)(n*2))!=0)throw new InvalidOperationException(Sdl.Error);}
   }
  }finally{servicing=false;}
 }
 SoundDriver Driver {get{Service();return pcm.Driver;}}
 public void InstallSoundTimer()=>Driver.InstallSoundTimer();
 public void UninstallSoundTimer()=>Driver.UninstallSoundTimer();
 public void ProgramSpeakerVoices(ushort tune)=>Driver.ProgramSpeakerVoices(tune);
 public void ProgramTandyVoices(ushort tune)=>Driver.ProgramTandyVoices(tune);
 public void OplWrite(ushort r,ushort v)=>throw new NotSupportedException("Desktop OPL synthesis is not implemented.");
 public void OplReset()=>throw new NotSupportedException("Desktop OPL synthesis is not implemented.");
 public void SetSoundOutputEnabled(ushort enabled)=>Driver.SetSoundOutputEnabled(enabled);
 public ushort SoundTimerTicks()=>Driver.SoundTimerTicks();
 public bool SoundTimerHooked()=>Driver.SoundTimerHooked();
 public void SetSoundDevice(ushort mode){if(mode!=C_Sound.MODE_SPEAKER&&mode!=C_Sound.MODE_DISABLED)throw new NotSupportedException("Desktop audio supports PC Speaker only.");Driver.SetSoundDevice(mode);}
 public ushort SoundVoiceTune(ushort voice)=>Driver.SoundVoiceTune(voice);
 public void GetTime(out ushort h,out ushort m,out ushort s,out ushort cs){Service();pcm.GetTime(out h,out m,out s,out cs);}
 public void Dispose(){if(device==0)return;pcm.Driver.UninstallSoundTimer();Sdl.SDL_ClearQueuedAudio(device);Sdl.SDL_CloseAudioDevice(device);device=0;}
}
