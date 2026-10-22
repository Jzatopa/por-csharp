using System.Diagnostics;using Por.Core;using static Por.Core.Types;
namespace Por.Desktop;
public sealed class GameExit:Exception{public int Code;public GameExit(int code){Code=code;}}
public sealed class DesktopHost:IVideoHardware,IGameIoHost,DaxIo.IDaxPlatform,IGameInitServices,IOptionsInfo,ISoundTimerPlatform,ISoundPortBus,IDisposable
{
 public readonly byte[] Frame=new byte[65536];readonly uint[] rgb=new uint[64000],palette=new uint[256];readonly Queue<byte> keys=new();readonly Stopwatch clock=Stopwatch.StartNew();readonly string captures;IntPtr window,renderer,texture;int dacIndex,dacComponent;byte red,green,mode=3;long lastPresent;bool dirty=true;int shot;string stage="";bool stageCaptured;public Action TimerVector{get;set;}
 public DesktopHost(string captureDirectory)
 {
  captures=captureDirectory;Directory.CreateDirectory(captures);
  if(Sdl.SDL_Init(0x20)!=0)throw new InvalidOperationException(Sdl.Error);
  window=Sdl.SDL_CreateWindow("Pool of Radiance — C# Desktop",0x2fff0000,0x2fff0000,960,720,0x24);if(window==IntPtr.Zero)throw new InvalidOperationException(Sdl.Error);
  renderer=Sdl.SDL_CreateRenderer(window,-1,1);if(renderer==IntPtr.Zero)throw new InvalidOperationException(Sdl.Error);
  texture=Sdl.SDL_CreateTexture(renderer,0x16362004,1,320,200);if(texture==IntPtr.Zero)throw new InvalidOperationException(Sdl.Error);
  int[] ega={0x000000,0x0000AA,0x00AA00,0x00AAAA,0xAA0000,0xAA00AA,0xAA5500,0xAAAAAA,0x555555,0x5555FF,0x55FF55,0x55FFFF,0xFF5555,0xFF55FF,0xFFFF55,0xFFFFFF};for(int i=0;i<256;i++)palette[i]=0xff000000u|(uint)ega[i%16];
 }
 void Decode(){for(int i=0;i<64000;i++)rgb[i]=palette[Frame[i]];}
 public void Present(bool force=false){if(!force&&clock.ElapsedMilliseconds-lastPresent<16)return;lastPresent=clock.ElapsedMilliseconds;if(dirty||force){Decode();Sdl.SDL_UpdateTexture(texture,IntPtr.Zero,rgb,320*4);dirty=false;}Sdl.SDL_RenderClear(renderer);Sdl.SDL_RenderCopy(renderer,texture,IntPtr.Zero,IntPtr.Zero);Sdl.SDL_RenderPresent(renderer);}
 public void Capture(string label){Decode();string file=Path.Combine(captures,$"{shot++:D3}-{label}.png");Png.Save(file,rgb,320,200);File.WriteAllText(Path.ChangeExtension(file,".json"),System.Text.Json.JsonSerializer.Serialize(new{stage,x=GDVars.gMapX,y=GDVars.gMapY,facing=GDVars.gFacing,script=GDVars.eclScriptId,eclIp=GDVars.eclIp,interfaceState=GDVars.ifcState,arenaCount=GDVars.arenaCount,round=GDVars.roundCounter,clock=GDVars.zone0==null?Array.Empty<int>():Enumerable.Range(0,7).Select(i=>(int)GDVars.zone0.words[C_Gbvm.CLOCK+i]).ToArray(),hp=GDVars.curChar==null?0:GDVars.curChar.hpCur,partySize=GDVars.zone1==null?0:GDVars.zone1[C_Gbvm.PARTY_SIZE]}));Console.WriteLine("CAPTURE "+file);}
 public DesktopSound Audio;
 public void Pump()
 {
  Audio?.Service();
  while(Sdl.SDL_PollEvent(out var e)!=0){if(e.Type==0x100)throw new GameExit(0);if(e.Type!=0x300||e.Repeat!=0)continue;if(e.Key==1073741893){Capture("keyboard-f12");continue;}foreach(byte b in TranslateKey(e.Key,e.Mod))keys.Enqueue(b);Console.WriteLine($"KEY sym={e.Key} mod={e.Mod}");}
  Present();
  if(!stageCaptured&&stage=="intro"){stageCaptured=true;Capture("title");Console.WriteLine("READY intro");}
 }
 public void OnStage(string value){stage=value;stageCaptured=false;Console.WriteLine("STATE "+value);}
 public static byte[] TranslateKey(int key,ushort mod)
 {
  if((mod&0x300)!=0){int scan=key switch{113=>16,120=>45,109=>50,114=>19,_=>-1};return scan<0?Array.Empty<byte>():new byte[]{0,(byte)scan};}
  if((mod&0xc0)!=0&&key>=97&&key<=122)return new[]{(byte)(key-96)};
  int extended=key switch{1073741886=>63,1073741887=>64,1073741889=>66,1073741906=>72,1073741905=>80,1073741904=>75,1073741903=>77,1073741898=>71,1073741901=>79,1073741899=>73,1073741902=>81,1073741897=>82,1073741900=>83,_=>-1};if(extended>=0)return new byte[]{0,(byte)extended};
  if(key==13||key==27||key==8||key==9||key==32)return new[]{(byte)key};
  if(key>=33&&key<=126){char ch=(char)key;if(ch>='a'&&ch<='z'&&(((mod&3)!=0)^((mod&0x2000)!=0)))ch=char.ToUpperInvariant(ch);if((mod&3)!=0){const string plain="1234567890-=[]\\;',./`";const string shifted="!@#$%^&*()_+{}|:\"<>?~";int i=plain.IndexOf(ch);if(i>=0)ch=shifted[i];}return new[]{(byte)ch};}return Array.Empty<byte>();
 }
 public bool KeyPressed{get{Pump();if(!stageCaptured&&(stage=="menu"||stage=="explore")){stageCaptured=true;Capture(stage=="menu"?"main-menu":"exploration");Console.WriteLine("READY "+stage);}if(keys.Count==0)Thread.Sleep(1);return keys.Count>0;}}
 public byte ReadKey(){while(!KeyPressed)Thread.Sleep(1);return keys.Dequeue();}
 public int BiosTicks(){Pump();return (int)(clock.Elapsed.TotalSeconds*1193182/65536)%0x1800b0;}
 public void Delay(ushort ms){long end=clock.ElapsedMilliseconds+ms;do{Pump();Thread.Sleep(1);}while(clock.ElapsedMilliseconds<end);}
 public byte ReadByte(ushort segment,ushort offset){if(segment!=0xA000)throw new NotSupportedException($"Video segment {segment:X}");return Frame[offset];}
 public void WriteByte(ushort segment,ushort offset,byte value){if(segment!=0xA000)throw new NotSupportedException($"Video segment {segment:X}");Frame[offset]=value;dirty=true;}
 public void WritePort(ushort port,byte value)
 {
  if(port==0x3c8){dacIndex=value;dacComponent=0;return;}if(port!=0x3c9)throw new NotSupportedException($"Video port {port:X}");byte c=(byte)((value&63)*255/63);if(dacComponent==0)red=c;else if(dacComponent==1)green=c;else{palette[dacIndex]=0xff000000u|(uint)(red<<16|green<<8|c);dacIndex=(dacIndex+1)&255;dirty=true;}dacComponent=(dacComponent+1)%3;
 }
 public void Interrupt(byte number,TInterruptRegisters r)
 {
  if(number==0x10){int fn=r.ax>>8;if(fn==0){mode=(byte)r.ax;Array.Clear(Frame);dirty=true;return;}if(fn==15){r.ax=(ushort)(40<<8|mode);return;}throw new NotSupportedException($"Video BIOS {r.ax:X4}");}
  if(number==0x1a){int ticks=BiosTicks();r.cx=(ushort)(ticks>>16);r.dx=(ushort)ticks;return;}
  throw new NotSupportedException($"Interrupt {number:X2}");
 }
 public void BiosWriteChar(byte page,byte col,byte row,byte attr,byte count,byte ch){if(page!=0)throw new NotSupportedException("Only primary VGA page supported.");if(ch==219){for(int y=row*8;y<row*8+8&&y<200;y++)for(int x=col*8;x<(col+count)*8&&x<320;x++)WriteByte(0xa000,(ushort)(y*320+x),(byte)(attr&15));}else TextRend.RenderChar(col,row,(byte)(attr&15),count,ch,1);}
 public void WriteChar(byte c,byte r,byte a,byte n,byte ch){BiosWriteChar(0,c,r,a,n,ch);}
 public void SoundCommand(ushort cmd){SoundDrv.SoundCommand(cmd);}public void SoundSuspend(){SoundDrv.SoundSuspend();}public void Halt(int code){Capture("exit");throw new GameExit(code);}public void WriteLine(string s){Console.WriteLine(s);}public TextWriter OpenDebugLog(string path){return new StreamWriter(Path.Combine(GDVars.dsSaveDirectory,Path.GetFileName(path)));}
 public byte ReadGameKey(){return GameIo.ReadGameKey();}public void ExitGame(){GameIo.ExitGame();}public void OverlayMessage(string s){Console.WriteLine(s);}public string CurrentDirectory=>Directory.GetCurrentDirectory();public string FullPath(string path)=>Path.GetFullPath(path);
 public TSpellProc SpellHandler(string name)=>MagicBindings.SpellHandler(name);public TEffectHandler EffectHandler(string name)=>EffHndlrBindings.EffectHandler(name);public TCastUI CastTargetMenu=>Magic.NoncombatCastTargetMenu;public void InstallLightningTracer(){MagicBindings.InstallLightningTracer();}public void SeedRandom(bool deterministic){var rng=deterministic?new Random(1):new Random();Dice.RandomBelow=n=>n<=0?0:rng.Next(n);}
 public int AvailableBytes=>(int)Math.Min(int.MaxValue,GC.GetGCMemoryInfo().TotalAvailableMemoryBytes-GC.GetTotalMemory(false));public int LargestBlock=>AvailableBytes;public string SecondArgument=>"";public string BuildCommit=>"7616920a managed startup";
 public void GetTime(out ushort h,out ushort m,out ushort s,out ushort c){Pump();var t=DateTime.Now;h=(ushort)t.Hour;m=(ushort)t.Minute;s=(ushort)t.Second;c=(ushort)(t.Millisecond/10);}
 public void Shutdown(byte[] text,byte code){WriteLine(System.Text.Encoding.ASCII.GetString(text));Halt(code);}
 public byte Read(ushort port){throw new NotSupportedException("Desktop audio is disabled in this startup pass.");}public void Write(ushort port,byte value){throw new NotSupportedException("Desktop audio is disabled in this startup pass.");}
 public void Dispose(){if(texture!=IntPtr.Zero)Sdl.SDL_DestroyTexture(texture);if(renderer!=IntPtr.Zero)Sdl.SDL_DestroyRenderer(renderer);if(window!=IntPtr.Zero)Sdl.SDL_DestroyWindow(window);Sdl.SDL_Quit();}
}
