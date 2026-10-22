using System;using System.Collections.Generic;using System.Linq;using System.IO;using System.Text.Json;using Por.Core;
class SixTests
{
 static long checks;static Random random=new Random(32161);static void Yes(bool b,string s){checks++;if(!b)throw new Exception(s);}
 static Types.TImgBuf Pic(int width,int height,int frames,bool mask){return new Types.TImgBuf{width=(ushort)width,height=(ushort)height,count=(byte)frames,imgSize=(ushort)(width*height*8),transp=mask?new Types.TBytes():null};}
 static byte Reverse(int x){int v=0;for(int i=0;i<8;i++)v=v*2+((x>>i)&1);return(byte)v;}
 static int Mask(int colour,byte[] c){int m=0;for(int p=0;p<8;p++){int pixel=p%2==0?c[p/2]>>4:c[p/2]&15;bool equal=p%2==0?pixel==(colour&15):pixel==colour;if(equal)m|=128>>p;}return m;}
 static void Leaves(){
  var expected=JsonDocument.Parse(File.ReadAllText("evidence/six-files-2026-10-21/ecltrace-oracle.json"));for(int i=0;i<256;i++)Yes(EclTrace.OpName((byte)i)==expected.RootElement[i].GetString(),"opcode "+i);
  var sound=GDVars.sound;GDVars.sound.name="previous";GDVars.sound.mode=111;GDVars.sound.singleStep=true;GDVars.sound.Play=t=>{throw new Exception("oldplay");};SndNone.Install();Yes(Object.ReferenceEquals(sound,GDVars.sound),"sound record identity");Yes(sound.name=="None"&&sound.mode==C_Sound.MODE_DISABLED&&!sound.singleStep,"install silent fields");for(int t=0;t<=65535;t++)sound.Play((ushort)t);SndNone.Install();Yes(sound.Play!=null,"reinstall delegate");
 }
 static void Main(){Leaves();Console.WriteLine("Qwen leaf assertions="+checks+" failures=0; all65536silenttunes invoked");}
}
