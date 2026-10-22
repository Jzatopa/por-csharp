using Por.Core;using Por.Desktop;
static class Program
{
 static string Value(string[] args,string name,string fallback){int i=Array.IndexOf(args,name);if(i<0)return fallback;if(i+1==args.Length)throw new ArgumentException("Missing "+name);return args[i+1];}
 static string Canonical(string path)
 {
  string result=Path.GetPathRoot(Path.GetFullPath(path));foreach(string part in Path.GetFullPath(path).Substring(result.Length).Split(Path.DirectorySeparatorChar,StringSplitOptions.RemoveEmptyEntries)){result=Path.Combine(result,part);var info=new DirectoryInfo(result);if(info.LinkTarget!=null)result=info.ResolveLinkTarget(true).FullName;}return result.TrimEnd(Path.DirectorySeparatorChar);
 }
 static bool Within(string path,string parent)=>path==parent||path.StartsWith(parent+Path.DirectorySeparatorChar,StringComparison.Ordinal);
 static int Main(string[] args)
 {
  string crashDirectory=null;
  try{
   if(args.Contains("--help")){Console.WriteLine("Por.Desktop --data ORIGINAL_DIRECTORY [--state WRITABLE_DIRECTORY] [--captures DIRECTORY] [--short-intro] [--no-audio]\nLinux SDL2 desktop; PC Speaker audio enabled. F12 saves a framebuffer capture. All game writes stay in --state.");return 0;}
   string original=Canonical(Value(args,"--data",Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads/poolofrad")));
   string state=Canonical(Value(args,"--state",Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"goldenbox/por-csharp")));
   if(!Directory.Exists(original))throw new DirectoryNotFoundException(original);
   if(Within(state,original)||Within(original,state))throw new ArgumentException("State and original installation must be separate directories, neither containing the other.");
   Directory.CreateDirectory(state);string data=Path.Combine(state,"data"),saves=Path.Combine(state,"saves");Directory.CreateDirectory(data);Directory.CreateDirectory(saves);
   if(Within(Canonical(data),original)||Within(Canonical(saves),original))throw new ArgumentException("Writable directories cannot link to original media.");
   foreach(string file in Directory.EnumerateFiles(original)){string dest=Path.Combine(data,Path.GetFileName(file));if(new FileInfo(dest).LinkTarget!=null)throw new IOException("State data must not contain file symlinks: "+dest);if(!File.Exists(dest))File.Copy(file,dest);}
   string captures=Canonical(Value(args,"--captures",Path.Combine(state,"captures")));if(Within(captures,original))throw new ArgumentException("Captures cannot be written to original media.");
   string logs=Path.Combine(state,"logs");if(Within(Canonical(logs),original))throw new ArgumentException("Logs cannot be written to original media.");Directory.CreateDirectory(logs);crashDirectory=logs;
   string[] recovered=RosterRecovery.Recover(saves);if(recovered.Length>0)Console.WriteLine("RECOVERY restored "+recovered.Length+" orphan character(s) to roster; character files unchanged.");
   Directory.SetCurrentDirectory(data);using var host=new DesktopHost(captures);
   Console.WriteLine("DATA copied/read-write "+data);Console.WriteLine("SAVES "+saves);Console.WriteLine("Platform: SDL2 VGA, managed startup (no DOS overlay/EMS emulation)");
   GDConsts.GAME_VERSION="C# desktop preview";DesktopComposition.InstallHosts(host);
   using var audio=args.Contains("--no-audio")?null:new DesktopSound(host.Shutdown);host.Audio=audio;
   DesktopComposition.InstallSound(host,audio);if(audio==null)Console.WriteLine("AUDIO disabled by --no-audio");
   GDVars.soundMode=GDVars.sound.mode;GDVars.soundSaved=GDVars.sound.mode;
   GDVars.gameDir=data+Path.DirectorySeparatorChar;GDVars.dsSaveDirectory=saves+Path.DirectorySeparatorChar;GDVars.fullIntro=!args.Contains("--short-intro");GDVars.stingArg=false;GDVars.savedVideoMode=3;GDVars.curVideoMode=3;VidVga.Install();Blit.SetVideoMode(GDVars.video.biosMode);GDVars.video.Start();
   PlatformGapBindings.Bind();
   try{Console.WriteLine("STATE managed-boot");FixMain.RunManaged(StartupSeamCheck.Verify);}catch(GameExit exit){Console.WriteLine("EXIT "+exit.Code);return exit.Code;}catch(Exception){try{host.Capture("crash");}catch{}throw;}
   throw new InvalidOperationException("Game main loop returned unexpectedly.");
  }catch(Exception ex){Console.Error.WriteLine(ex);if(crashDirectory!=null){try{string file=Path.Combine(crashDirectory,"crash-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff")+".txt");File.WriteAllText(file,"Script="+GDVars.eclScriptId+" IP="+GDVars.eclIp+" Map="+GDVars.gMapX+","+GDVars.gMapY+"\n"+ex);Console.Error.WriteLine("Crash details: "+file);}catch(IOException){}}return 1;}
 }
}
