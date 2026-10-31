using Por.Desktop;using Por.Core;
int count=0;void Key(int key,int mod,params byte[] expected){count++;var actual=DesktopHost.TranslateKey(key,(ushort)mod);if(!actual.SequenceEqual(expected))throw new Exception($"Key {key} mod{mod} expected{string.Join(',',expected)} got{string.Join(',',actual)}");}
for(int key=97;key<=122;key++){Key(key,0,(byte)key);Key(key,1,(byte)(key-32));Key(key,0x2000,(byte)(key-32));Key(key,0x2001,(byte)key);Key(key,0x40,(byte)(key-96));}
Key(1073741886,0,0,63);Key(1073741887,0,0,64);Key(1073741889,0,0,66);Key(1073741906,0,0,72);Key(1073741905,0,0,80);Key(1073741904,0,0,75);Key(1073741903,0,0,77);Key(1073741898,0,0,71);Key(1073741901,0,0,79);Key(1073741899,0,0,73);Key(1073741902,0,0,81);Key(1073741897,0,0,82);Key(1073741900,0,0,83);
Key('q',0x100,0,16);Key('x',0x100,0,45);Key('m',0x100,0,50);Key('r',0x100,0,19);Key('a',0x100);Key(0x4000003a,0);Key(13,0,13);Key(27,0,27);Key(8,0,8);Key(9,0,9);Key(32,0,32);
Key('1',1,(byte)'!');Key('2',1,(byte)'@');Key(';',1,(byte)':');Key('\'',1,(byte)'"');Key('`',1,(byte)'~');
string plain="1234567890-=[]\\;',./`",shift="!@#$%^&*()_+{}|:\"<>?~";for(int i=0;i<plain.Length;i++){Key(plain[i],0,(byte)plain[i]);Key(plain[i],1,(byte)shift[i]);}
Console.WriteLine($"Desktop keyboard: {count} assertions PASS");
string recoveryRoot=Path.Combine(Path.GetTempPath(),"por-roster-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(recoveryRoot);
try{
 byte[] character=new byte[285];character[0]=9;System.Text.Encoding.ASCII.GetBytes("TEST HERO").CopyTo(character,1);string hero=Path.Combine(recoveryRoot,"TESTHERO.cha"); // Source SanitizeFileName removes the space.
 string expectedBase=DaxIo.SanitizeFileName(DaxIo.SanitizeString("TEST HERO","."));hero=Path.Combine(recoveryRoot,expectedBase+".cha");File.WriteAllBytes(hero,character);string roster=Path.Combine(recoveryRoot,"charlist.txt");File.WriteAllText(roster,"OTHER HERO\r\n");byte[] before=File.ReadAllBytes(roster);
 string[] recovered=RosterRecovery.Recover(recoveryRoot);if(!recovered.SequenceEqual(new[]{"TEST HERO"}))throw new Exception("orphan recovered");if(!character.SequenceEqual(File.ReadAllBytes(hero)))throw new Exception("character changed");if(!File.ReadAllText(roster).StartsWith("OTHER HERO\r\nTEST HERO\r\n"))throw new Exception("roster append changed previous names");string backup=Directory.GetFiles(recoveryRoot,"*.bak").Single();if(!before.SequenceEqual(File.ReadAllBytes(backup)))throw new Exception("backup differs");if(RosterRecovery.Recover(recoveryRoot).Length!=0)throw new Exception("recovery not idempotent");
 File.WriteAllBytes(Path.Combine(recoveryRoot,"BROKEN.cha"),new byte[284]);if(RosterRecovery.Recover(recoveryRoot).Length!=0)throw new Exception("short record accepted");character[0]=16;File.WriteAllBytes(Path.Combine(recoveryRoot,"INVALID.cha"),character);if(RosterRecovery.Recover(recoveryRoot).Length!=0)throw new Exception("invalid name length accepted");Console.WriteLine("Roster recovery: 7 checks PASS");
}finally{Directory.Delete(recoveryRoot,true);}

// Independent PIT frequency and timer-rate oracles, plus real converted tune playback.
int audioChecks=0;void CheckAudio(bool ok,string name){audioChecks++;if(!ok)throw new Exception("Audio: "+name);}
var synth=new SpeakerPcm();short[] samples=new short[48000];synth.Render(samples,samples.Length);CheckAudio(samples.All(x=>x==0),"initial silence");
synth.Write(0x43,0xB6);synth.Write(0x42,0xE8);synth.Write(0x42,3);synth.Write(0x61,3);synth.Render(samples,samples.Length);
int rises=Enumerable.Range(1,samples.Length-1).Count(i=>samples[i-1]<0&&samples[i]>0);CheckAudio(Math.Abs(rises-1193.182)<2,"PIT divisor1000 frequency");CheckAudio(samples.Max()==4500&&samples.Min()==-4500,"bounded amplitude");
synth.Write(0x61,0);synth.Render(samples,samples.Length);CheckAudio(samples.All(x=>x==0),"gate silence");
synth.Write(0x42,0);synth.Write(0x42,0);synth.Write(0x61,3);synth.Render(samples,samples.Length);rises=Enumerable.Range(1,samples.Length-1).Count(i=>samples[i-1]<0&&samples[i]>0);CheckAudio(rises>=18&&rises<=19,"zero PIT means65536");
var sequenced=new SpeakerPcm();var driver=sequenced.Driver;driver.SetSoundDevice(1);driver.InstallSoundTimer();driver.ProgramSpeakerVoices(21);sequenced.Render(samples,48000);CheckAudio(driver.Ticks==1193182/5041,"timer rational tick count");CheckAudio(samples.Any(x=>x!=0),"title tune emits PCM");
short[] split=new short[48000];var second=new SpeakerPcm();second.Driver.SetSoundDevice(1);second.Driver.InstallSoundTimer();second.Driver.ProgramSpeakerVoices(21);int offset=0;while(offset<split.Length){int n=Math.Min(137,split.Length-offset);short[] chunk=new short[n];second.Render(chunk,n);chunk.CopyTo(split,offset);offset+=n;}CheckAudio(samples.SequenceEqual(split),"chunk-independent timing and PCM");
driver.SetSoundOutputEnabled(0);sequenced.Render(samples,48000);CheckAudio(samples.Skip(204).All(x=>x==0),"mute after next sequencer tick");driver.SetSoundOutputEnabled(1);driver.ProgramSpeakerVoices(21);sequenced.Render(samples,48000);CheckAudio(samples.Any(x=>x!=0),"unmute/restart");driver.UninstallSoundTimer();ushort stopped=driver.Ticks;sequenced.Render(samples,48000);CheckAudio(driver.Ticks==stopped&&samples.All(x=>x==0),"uninstall stops timer and speaker");
bool rejected=false;try{sequenced.Write(0x999,0);}catch(NotSupportedException){rejected=true;}CheckAudio(rejected,"unknown port rejected");
Console.WriteLine($"Speaker PCM: {audioChecks} checks PASS");
FullAudioChecks.Run();
if(args.Contains("--audio-probe")){
 string mode=args.FirstOrDefault(a=>a.StartsWith("--audio-mode="))?.Split('=')[1]??"adlib";
 string realPack=args.FirstOrDefault(a=>a.StartsWith("--real-pack="))?.Substring("--real-pack=".Length);
 if(realPack!=null){realPack=Path.GetFullPath(realPack);EffectPack.Validate(File.ReadAllBytes(realPack));GDVars.gameDir=Path.GetDirectoryName(realPack)+Path.DirectorySeparatorChar;}
 string fixtureDir=null;
 if(args.Contains("--sample-fixture")){fixtureDir=Path.Combine(Path.GetTempPath(),"por-sb-fixture-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(fixtureDir);GDVars.gameDir=fixtureDir+Path.DirectorySeparatorChar;using var w=new BinaryWriter(File.Create(Path.Combine(fixtureDir,"SBSFX.DAT")));w.Write(new byte[]{83,66,70,88,2,2});w.Write((byte)6);w.Write((byte)0);w.Write((byte)156);w.Write((ushort)3000);w.Write((ushort)20);w.Write((byte)3);w.Write((byte)1);w.Write((byte)156);w.Write((ushort)501);w.Write((ushort)3020);for(int i=0;i<3000;i++)w.Write((byte)(128+80*Math.Sin(i*2*Math.PI/25)));w.Write((byte)128);for(int i=0;i<500;i++)w.Write((byte)(i%2==0?0x71:0xF9));}
 using var audio=new DesktopSound((text,code)=>throw new Exception("Unexpected shutdown"),mode);audio.Install();GDVars.soundMode=GDVars.sound.mode;GDVars.soundSaved=GDVars.sound.mode;GDVars.musicOn=1;GDVars.soundAutoStop=1;
 SoundDrv.PlayMusic(21);var timer=System.Diagnostics.Stopwatch.StartNew();while(timer.ElapsedMilliseconds<1200){audio.Service();Thread.Sleep(2);}SoundDrv.StopMusic();
 SoundDrv.SoundCommand(7); // effect6, blocking SoundDelay internally: must not deadlock.
 if(fixtureDir!=null){if(!audio.SamplePlaying)throw new Exception("Sample was truncated when the sequencer timer auto-stopped");var tailTimer=System.Diagnostics.Stopwatch.StartNew();while(tailTimer.ElapsedMilliseconds<300){audio.Service();Thread.Sleep(2);}if(audio.SamplePlaying)throw new Exception("Sample outlasted its source duration");Console.WriteLine("Native sampled-effect tail survives timer auto-stop PASS");GDVars.sound.Play(3);var effectTimer=System.Diagnostics.Stopwatch.StartNew();while(effectTimer.ElapsedMilliseconds<150){audio.Service();Thread.Sleep(2);}if(audio.EffectsStarted!=2)throw new Exception("Native PCM8/ADPCM4 fixture did not both play");Console.WriteLine("Native Sound Blaster PCM8 + Creative ADPCM4: 2 effects started PASS");Directory.Delete(fixtureDir,true);}
 if(realPack!=null){SoundDrv.SoundCommand(10);if(audio.EffectsStarted==0)throw new Exception("Real recorded footstep did not start");Console.WriteLine("Real recorded footstep through system device PASS; sampled effects="+audio.EffectsStarted);
  if(args.Contains("--all-recorded-effects")){
   byte[] pack=File.ReadAllBytes(realPack);var cues=new HashSet<byte>();for(int entry=6;entry<6+7*pack[5];entry+=7)cues.Add(pack[entry]);
   if(!Enumerable.Range(1,11).All(c=>cues.Contains((byte)c)))throw new Exception("All-cue probe requires the complete upstream1..11pack");
   for(ushort cue=1;cue<=11;cue++){
    int at=Enumerable.Range(0,pack[5]).Select(i=>6+7*i).Single(i=>pack[i]==cue);int length=pack[at+3]|pack[at+4]<<8;int decoded=pack[at+1]==1?2*(length-1):length;double expectedMs=decoded*(256-pack[at+2])/1000.0;
    long before=audio.EffectsStarted;var playback=System.Diagnostics.Stopwatch.StartNew();SoundDrv.SoundCommand((ushort)(cue+1));if(audio.EffectsStarted!=before+1)throw new Exception("Recorded cue did not start: "+cue);
    while(audio.SamplePlaying){audio.Service();Thread.Sleep(2);if(playback.ElapsedMilliseconds>5000)throw new Exception("Recorded cue did not finish: "+cue);}
    if(playback.Elapsed.TotalMilliseconds<expectedMs-75)throw new Exception("Recorded cue truncated: "+cue);
    Console.WriteLine("RECORDED cue="+cue+" format="+pack[at+1]+" decoded="+decoded+" expectedMs="+expectedMs.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+" observedMs="+playback.ElapsedMilliseconds+" PASS");
   }
  }
}
 GameIo.Host=null;SoundDrv.SoundExit();Console.WriteLine("SDL native queued title + blocking effect + exit PASS");
}
// Wiring review: the real desktop host composition plus the converted-unit bindings pass the startup seam check, and a
// missing host seam is named before the game body. Needs an SDL video driver (headless: SDL_VIDEODRIVER=dummy).
if(args.Contains("--startup-check")){
 string checkCaptures=Path.Combine(Path.GetTempPath(),"por-startup-check-"+Guid.NewGuid().ToString("N"));
 try{
  using(var host=new DesktopHost(checkCaptures)){
   DesktopComposition.InstallHosts(host);DesktopComposition.InstallSound(host,null);VidVga.Install();PlatformGapBindings.Bind();
   List<string> seen=null;bool body=false;FixMain.RuntimeStage=s=>body=true;
   try{FixMain.RunManaged(()=>{seen=StartupSeamCheck.Missing();StartupSeamCheck.Verify();throw new OperationCanceledException("startup check reached");});}catch(OperationCanceledException){}
   if(seen==null||seen.Count!=0||body)throw new Exception("Desktop startup check: "+(seen==null?"verifier not called":string.Join(", ",seen)));
   Options.Info=null;string named=null;body=false;
   try{FixMain.RunManaged(StartupSeamCheck.Verify);}catch(InvalidOperationException e){named=e.Message;}
   if(named==null||!named.Contains("Options.Info (host)")||body)throw new Exception("Desktop startup check did not name a removed host seam: "+named);
   Console.WriteLine("Desktop startup seam check: "+StartupSeamCheck.Required.Length+" required seams bound by the real composition; removed host seam named. PASS");
  }
 }finally{try{Directory.Delete(checkCaptures,true);}catch(IOException){}}
}
