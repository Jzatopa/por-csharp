using System;using System.Collections.Generic;using System.Linq;using Por.Core;
static partial class Tests
{
 static long count;static void Eq(long e,long a,string m){count++;if(e!=a)throw new Exception(m+": expected "+e+" got "+a);}static void Yes(bool v,string m){count++;if(!v)throw new Exception(m);}static byte B(int n){return unchecked((byte)n);}static ushort W(int n){return unchecked((ushort)n);}
 sealed class Bus:ISoundPortBus{public List<string> Log=new List<string>();public List<int[]> Writes=new List<int[]>();public byte ReadValue;public byte Read(ushort p){Log.Add("R"+p);return ReadValue;}public void Write(ushort p,byte v){Log.Add("W"+p+":"+v);Writes.Add(new[]{(int)p,(int)v});}}
 static ushort OPitch(int p){double f=9*262144.0/(p==0?1024:p);int block=0;while(Math.Floor(f)>1023&&block<7){block++;f/=2;}return W(8192+block*1024+Math.Min(1023,(int)Math.Floor(f)));}
 static void Opl()
 {
  for(int p=0;p<65536;p++)Eq(OPitch(p),OplSequencer.Pitch(W(p)),"pitch all word periods");var bus=new Bus();var opl=new OplSequencer(bus);opl.Write(0x123,0x456);Yes(bus.Log.SequenceEqual(new[]{"W904:35"}.Concat(Enumerable.Repeat("R904",6)).Concat(new[]{"W905:86"}).Concat(Enumerable.Repeat("R904",35))),"OPL waits/address/data low bytes");
  bus.Log.Clear();bus.Writes.Clear();opl.Reset();Eq(290,bus.Writes.Count/2,"reset register count");var registers=new List<int[]>();for(int r=1;r<246;r++)registers.Add(new[]{r,0});registers.Add(new[]{1,32});int[] chan={0,1,2,7},carrier={3,4,5,20};int[] pr={32,35,64,67,96,99,128,131,224,227},pv={33,33,26,63,240,240,15,15,0,0};for(int i=0;i<4;i++){for(int j=0;j<10;j++)registers.Add(new[]{pr[j]+carrier[i]-3,pv[j]});registers.Add(new[]{192+chan[i],10});}
  for(int i=0;i<registers.Count;i++){Eq(904,bus.Writes[i*2][0],"reset address port");Eq(registers[i][0],bus.Writes[i*2][1],"reset register order");Eq(905,bus.Writes[i*2+1][0],"reset value port");Eq(registers[i][1],bus.Writes[i*2+1][1],"reset patch value");}Eq(290*43,bus.Log.Count,"reset all wait reads");
  var state=new SoundSequenceState{CurrentTune=1};var voices=Enumerable.Range(0,4).Select(i=>new SoundVoice{Duration=1}).ToArray();var rng=new Random(727);int[] prevF={65535,65535,65535,65535},prevL={63,63,63,63};int rhythm=0;int[] att={0,3,5,8,11,13,16,19,21,24,27,29,32,35,37,63};
  for(int n=0;n<1200;n++)
  {
   for(int i=0;i<4;i++){voices[i].OutPeriod=W(rng.Next(65536));voices[i].Level=W(rng.Next(65536));voices[i].Duration=W(rng.Next(2));}state.CurrentTune=W(n%9==0?0:1);state.OutputOn=B(n%7==0?0:2);bus.Writes.Clear();bus.Log.Clear();var steps=new List<int>();opl.Tick(state,voices,i=>steps.Add(i));Yes(steps.SequenceEqual(Enumerable.Range(0,4).Where(i=>state.CurrentTune!=0&&voices[i].Duration!=0)),"step voices even while muted");
   var expected=new List<int[]>();Action<int,int> write=(r,v)=>{expected.Add(new[]{904,r});expected.Add(new[]{905,v});};
   bool active=state.CurrentTune!=0&&state.OutputOn!=0;if(active&&rhythm!=40){write(189,40);rhythm=40;}
   for(int i=0;i<4;i++){int level=active?att[(65535-voices[i].Level)/4096]:63;if(prevL[i]!=level){write(64+carrier[i],level);prevL[i]=level;}if(active){int f;if(i<3)f=OPitch(voices[i].OutPeriod/64);else{int rate=voices[i].OutPeriod/64%4;f=rate==3?prevF[2]&~8192:new[]{5720,4696,3672}[rate];}if(f!=prevF[i]){write(160+chan[i],f%256);write(176+chan[i],f/256);prevF[i]=f;}}}
   Eq(expected.Count,bus.Writes.Count,"cached register write count");for(int i=0;i<expected.Count;i++){Eq(expected[i][0],bus.Writes[i][0],"tick port order");Eq(expected[i][1],bus.Writes[i][1],"tone/noise/level output");}
  }
  bus.Writes.Clear();opl.Silence();Eq(18,bus.Writes.Count,"silence unconditional writes");Eq(189,bus.Writes[16][1],"silence rhythm register");Eq(32,bus.Writes[17][1],"silence keys snare off");bool missing=false;voices[0].Duration=1;try{opl.Tick(new SoundSequenceState{CurrentTune=1},voices,null);}catch(NotSupportedException){missing=true;}Yes(missing,"missing shared stepper explicit");
 }
 static int Main(string[] args){try{Action<string,Action> run=(n,a)=>{if(args.Length==0||args.Contains(n)){long old=count;a();Console.WriteLine(n+": "+(count-old)+" assertions PASS");}};run("Opl",Opl);run("CharGenModify",CharGenModify);run("CharGenPickers",CharGenPickers);run("CharGenCreateTrain",CharGenCreateTrain);run("CharGenMenus",CharGenMenus);run("CharGenPixels",CharGenPixels);run("Tandy",Tandy);run("Speaker",Speaker);Console.WriteLine("TOTAL "+count+" assertions PASS");return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
