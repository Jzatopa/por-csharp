namespace Por.Desktop;
// TI SN76496 variant: 3.579545 MHz input, /16 counter clock, 10-bit tone
// registers, 2 dB attenuators and 17-bit noise shift register (taps 2/3).
// Register behavior cross-checked against MAME sn76496.cpp; no source copied.
public sealed class TandyPcm
{
 public const int Clock=3579545;
 readonly int[] period={1024,1024,1024},volume={15,15,15,15},remaining={1,1,1,1};
 readonly bool[] output=new bool[4];
 static readonly int[] gain=Enumerable.Range(0,16).Select(i=>i==15?0:(int)Math.Round(2200*Math.Pow(10,-i/10.0))).ToArray();
 int latch,noise,lfsr=0x10000;long phase;
 public void Write(byte value){if((value&128)!=0){latch=(value>>4)&7;Set(value&15,true);}else Set(value&63,false);}
 void Set(int data,bool low){int ch=latch/2;if((latch&1)!=0)volume[ch]=data&15;else if(ch==3){noise=data&7;lfsr=0x10000;}else{int n=period[ch]==1024?0:period[ch];n=low?(n&0x3F0)|(data&15):(n&15)|((data&63)<<4);period[ch]=n==0?1024:n;}}
 void Tick(){for(int ch=0;ch<3;ch++)if(--remaining[ch]<=0){remaining[ch]=period[ch];output[ch]=!output[ch];}
  if(--remaining[3]<=0){remaining[3]=(noise&3)==3?2*period[2]:32<<(noise&3);int feedback=(noise&4)!=0?((lfsr>>2)^(lfsr>>3))&1:(lfsr>>2)&1;lfsr=(lfsr>>1)|(feedback<<16);output[3]=(lfsr&1)!=0;}}
 public int Sample(){phase+=Clock;while(phase>=16L*SpeakerPcm.SampleRate){phase-=16L*SpeakerPcm.SampleRate;Tick();}int sum=0;for(int ch=0;ch<4;ch++)sum+=output[ch]?gain[volume[ch]]:-gain[volume[ch]];return sum;}
 public void Silence(){for(int ch=0;ch<4;ch++)volume[ch]=15;}
}
