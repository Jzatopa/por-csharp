// The desktop's host installation, moved out of Program.Main unchanged so tests can run the same composition.
using Por.Core;
namespace Por.Desktop;
public static class DesktopComposition
{
 public static void InstallHosts(DesktopHost host)
 {
  FixMain.RuntimeStage=host.OnStage;GameIo.Host=host;VideoHardware.Host=host;Blit.Interrupt=host.Interrupt;DaxIo.Platform=host;GameInit.Services=host;Options.Info=host;DataBld.Platform=new BuildPlatform(host);TextRend.ReadBiosTicks=host.BiosTicks;TextRend.DelayMilliseconds=host.Delay;BldShow.MaxAvailable=()=>host.AvailableBytes;Portrait.MaxAvailable=()=>host.AvailableBytes;
 }
 // audio null means --no-audio.
 public static void InstallSound(DesktopHost host,DesktopSound audio)
 {
  if(audio==null){SoundDrv.Host=new SoundDriver(host,host);SndNone.Install();}else{SoundDrv.Host=audio;SndSpk.Install();}
 }
}
