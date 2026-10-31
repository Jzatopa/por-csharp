using System.Diagnostics;
using Por.Core;
namespace Por.Desktop;

// Polling facade also advances the timer inside SoundDelay's tight tick-poll loop.
public sealed class DesktopSound : ISoundDriverHost, IDisposable
{
 readonly SpeakerPcm pcm=new();readonly Stopwatch clock=new();readonly short[] buffer=new short[2048];uint device;long rendered;bool servicing;
 public long EffectsStarted=>pcm.Blaster.EffectsStarted;
 public bool SamplePlaying=>pcm.Blaster.SamplePlaying;
 public string Mode {get;}
 public DesktopSound(Action<byte[],byte> shutdown,string mode="adlib")
 {
  if(mode!="speaker"&&mode!="tandy"&&mode!="adlib"&&mode!="blaster")throw new ArgumentException("Audio mode must be speaker, tandy, adlib or blaster");
  Mode=mode;pcm.Exit=shutdown;
  if(Sdl.SDL_InitSubSystem(0x10)!=0)throw new InvalidOperationException("SDL audio: "+Sdl.Error);
  var desired=new Sdl.AudioSpec{Frequency=SpeakerPcm.SampleRate,Format=0x8010,Channels=1,Samples=1024};
  device=Sdl.SDL_OpenAudioDevice(IntPtr.Zero,0,ref desired,out var actual,0);
  if(device==0)throw new InvalidOperationException("Cannot open audio device: "+Sdl.Error+" (use --no-audio to play silently)");
  // No allowed format changes: queued data is signed little-endian 16-bit mono 48 kHz.
  Sdl.SDL_PauseAudioDevice(device,0);clock.Start();Console.WriteLine("AUDIO "+Mode+" / 48000 Hz mono / SDL2 queued output / backend="+Sdl.AudioDriver);
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
 // DSP playback is independent of the sequencer IRQ. Auto-stop may unhook
 // that IRQ after100ms while a longer sample is still playing. Dispose/Play(0)
 // stop the DSP; timer uninstall must not truncate a recorded effect.
 public void UninstallSoundTimer()=>Driver.UninstallSoundTimer();
 public void ProgramSpeakerVoices(ushort tune)=>Driver.ProgramSpeakerVoices(tune);
 public void ProgramTandyVoices(ushort tune)=>Driver.ProgramTandyVoices(tune);
 public void OplWrite(ushort r,ushort v)=>Driver.OplWrite(r,v);
 public void OplReset()=>Driver.OplReset();
 public void SetSoundOutputEnabled(ushort enabled)=>Driver.SetSoundOutputEnabled(enabled);
 public ushort SoundTimerTicks()=>Driver.SoundTimerTicks();
 public bool SoundTimerHooked()=>Driver.SoundTimerHooked();
 public void SetSoundDevice(ushort mode)=>Driver.SetSoundDevice(mode);
 public void Install(){SoundDrv.Host=this;SndOpl.ReadPort=pcm.Read;SndSb.Host=pcm.Blaster;switch(Mode){case "speaker":SndSpk.Install();break;case "tandy":SndTdy.Install();break;case "adlib":SndOpl.Install();break;case "blaster":SndSb.Install();break;}}
 public ushort SoundVoiceTune(ushort voice)=>Driver.SoundVoiceTune(voice);
 public void GetTime(out ushort h,out ushort m,out ushort s,out ushort cs){Service();pcm.GetTime(out h,out m,out s,out cs);}
 public void Dispose(){if(device==0)return;pcm.Dispose();Sdl.SDL_ClearQueuedAudio(device);Sdl.SDL_CloseAudioDevice(device);device=0;}
}
