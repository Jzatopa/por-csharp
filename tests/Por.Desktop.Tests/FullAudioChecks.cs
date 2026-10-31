using Por.Core;using Por.Desktop;
static class FullAudioChecks
{
 public static void Run(){int count=0;void Check(bool ok,string name){count++;if(!ok)throw new Exception("Full audio: "+name);}
  var psg=new TandyPcm();var wave=new short[48000];for(int i=0;i<wave.Length;i++)wave[i]=(short)psg.Sample();Check(wave.All(x=>x==0),"PSG initial silence");
  // 10-bit period 254 -> 440.396Hz at input clock/(32*N).
  psg.Write(0x8E);psg.Write(15);psg.Write(0x90);for(int i=0;i<wave.Length;i++)wave[i]=(short)psg.Sample();int rises=Enumerable.Range(1,wave.Length-1).Count(i=>wave[i-1]<0&&wave[i]>0);Check(Math.Abs(rises-440.396)<2,"PSG tone frequency");Check(wave.Max()==2200&&wave.Min()==-2200,"PSG tone gain");psg.Write(0x91);Check(Math.Abs(Math.Abs(psg.Sample())/2200.0-Math.Pow(10,-.1))<.001,"PSG 2dB volume");psg.Silence();Check(psg.Sample()==0,"PSG silence");
  psg.Write(0xE4);psg.Write(0xF0);for(int i=0;i<wave.Length;i++)wave[i]=(short)psg.Sample();Check(wave.Distinct().Count()==2,"PSG white noise");psg.Silence();Check(psg.Sample()==0,"noise mute");
  foreach(ushort mode in new ushort[]{0,1,3}){using var synth=new SpeakerPcm();synth.Driver.SetSoundDevice(mode);if(mode==3){synth.Driver.OplReset();synth.Write(0x388,4);synth.Write(0x389,0x21);Check(synth.Read(0x388)==0xC0,"OPL timer detection");synth.Write(0x388,4);synth.Write(0x389,0x80);Check(synth.Read(0x388)==0,"OPL timer reset");}
   synth.Driver.InstallSoundTimer();if(mode==1)synth.Driver.ProgramSpeakerVoices(21);else synth.Driver.ProgramTandyVoices(21);synth.Render(wave,48000);Check(wave.Any(x=>x!=0),"title PCM mode "+mode);Check(synth.Driver.Ticks==236,"timer mode "+mode);synth.Driver.SetSoundOutputEnabled(0);synth.Render(wave,48000);Check(wave.Skip(24000).All(x=>x==0),"mute mode "+mode);synth.Driver.SetSoundOutputEnabled(1);
   if(mode==1)synth.Driver.ProgramSpeakerVoices(22);else synth.Driver.ProgramTandyVoices(22);synth.Render(wave,48000);Check(wave.Any(x=>x!=0),"battle music mode "+mode);synth.Driver.UninstallSoundTimer();synth.Render(wave,48000);Check(wave.Skip(24000).All(x=>x==0),"shutdown mode "+mode);
  }
  Check(BlasterPcm.Decode(new byte[]{0,128,255},0).SequenceEqual(new short[]{-16384,0,16256}),"PCM8 unsigned decode");
  // Reference=128; nibble 7 grows scale, then 0 reduces it; followed by negative 15,0.
  Check(BlasterPcm.Decode(new byte[]{128,0x70,0xF0},1).SequenceEqual(new short[]{896,1024,128,256}),"Creative ADPCM reference/adaptation/nibble order");
  Check(BlasterPcm.Decode(new byte[]{255,0x77,0x77},1).All(x=>x==16256),"ADPCM upper clamp");
  Check(BlasterPcm.Decode(new byte[]{0,0xFF,0xFF},1).All(x=>x==-16384),"ADPCM lower clamp");
  using(var synth=new SpeakerPcm()){var sb=synth.Blaster;sb.WritePort(0x226,1);sb.WritePort(0x226,0);Check(sb.ReadPort(0x22E)==128&&sb.ReadPort(0x22A)==0xAA,"DSP detection");
   var block=sb.Allocate(256);for(int i=0;i<256;i++)block.Data[i]=(byte)i;
   // Actual DMA channel1 sequence, 256 samples at10000Hz (TC156).
   foreach(var pair in new(int,byte)[]{(10,5),(12,0),(11,0x49),(2,0),(2,0),(0x83,1),(3,255),(3,0),(10,1),(0x22C,0xD1),(0x22C,0x40),(0x22C,156),(0x22C,0x14),(0x22C,255),(0x22C,0)})sb.WritePort((ushort)pair.Item1,pair.Item2);
   synth.Render(wave,48000);Check(sb.EffectsStarted==1,"DMA starts one effect");Check(wave.Take(1229).Any(x=>x!=0)&&wave.Skip(1230).All(x=>x==0),"PCM sample duration/rate and completion");sb.Stop();Check(sb.Sample()==0,"DSP stop");
  }
  foreach(ushort mode in new ushort[]{0,3}){
   using var one=new SpeakerPcm();using var two=new SpeakerPcm();foreach(var chip in new[]{one,two}){chip.Driver.SetSoundDevice(mode);if(mode==3)chip.Driver.OplReset();chip.Driver.InstallSoundTimer();chip.Driver.ProgramTandyVoices(21);}
   short[] whole=new short[48000],pieces=new short[48000];one.Render(whole,whole.Length);for(int pos=0;pos<pieces.Length;){int n=Math.Min(137,pieces.Length-pos);var piece=new short[n];two.Render(piece,n);piece.CopyTo(pieces,pos);pos+=n;}Check(whole.SequenceEqual(pieces),"chunk invariance mode "+mode);
  }
  string folder=Path.Combine(Path.GetTempPath(),"por-pack-check-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
  try{byte[] pack={83,66,70,88,2,1,6,0,156,2,0,13,0,128,255};EffectPack.Validate(pack);Check(true,"valid effect pack");var bad=(byte[])pack.Clone();bad[11]=99;bool rejected=false;try{EffectPack.Validate(bad);}catch(InvalidDataException){rejected=true;}Check(rejected,"out-of-range effect rejected");
   Check(EffectPack.SelectMode("auto",folder)=="adlib","no pack auto selects AdLib");Check(EffectPack.SelectMode("tandy",folder)=="tandy","explicit mode retained");string source=Path.Combine(folder,"source.pack"),dest=Path.Combine(folder,"sbsfx.dat");File.WriteAllBytes(source,pack);File.WriteAllText(dest,"old");EffectPack.Import(source,folder);Check(File.ReadAllBytes(dest).SequenceEqual(pack)&&!File.Exists(Path.Combine(folder,"SBSFX.DAT")),"case-preserving pack import");Check(File.ReadAllText(Directory.GetFiles(folder,"*.bak").Single())=="old","prior pack backed up");EffectPack.Import(source,folder);Check(Directory.GetFiles(folder,"*.bak").Length==1,"identical import does not overwrite");Check(EffectPack.SelectMode("auto",folder)=="blaster","pack auto selects Blaster");
  }finally{Directory.Delete(folder,true);}
  int arrangements=0;foreach(ushort mode in new ushort[]{0,1,3}){using var countChip=new SpeakerPcm();int tunes=(mode==1?(FourVoiceSequencer)countChip.Driver.Speaker:countChip.Driver.Tandy).Arrangement.Songs.GetLength(0);for(ushort tune=0;tune<tunes;tune++){using var chip=new SpeakerPcm();chip.Driver.SetSoundDevice(mode);if(mode==3)chip.Driver.OplReset();chip.Driver.InstallSoundTimer();if(mode==1)chip.Driver.ProgramSpeakerVoices(tune);else chip.Driver.ProgramTandyVoices(tune);chip.Render(wave,12000);Check(chip.Driver.Ticks==59,"arrangement timer/bus mode "+mode+" tune "+tune);arrangements++;}}
  Console.WriteLine($"Arrangement bus/render coverage: {arrangements} mode/tune pairs");
  Console.WriteLine($"Full audio: {count} checks PASS");
 }
}
