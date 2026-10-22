using System;using Por.Core;
static partial class Tests
{
 static void AdlibChecks()
 {
  int[] atten={0,3,5,8,11,13,16,19,21,24,27,29,32,35,37,63};
  for(int tune=1;tune<=28;tune++){
   var sbus=new Bus();var obus=new Bus();var sn=new SoundDriver(sbus,new Platform());var opl=new SoundDriver(obus,new Platform());sn.SetSoundDevice(0);opl.SetSoundDevice(3);sn.ProgramTandyVoices(W(tune));opl.ProgramTandyVoices(W(tune));opl.OplReset();int[] regs=new int[256],period=new int[4],level={15,15,15,15};int reg=0,latch=0;
   Action apply=()=>{foreach(var w in obus.Writes){if(w[0]==0x388)reg=w[1];else if(w[0]==0x389)regs[reg]=w[1];}obus.Writes.Clear();};apply();
   for(int tick=0;tick<400;tick++){
    sbus.Writes.Clear();sn.TraceTick();opl.TraceTick();apply();foreach(var w in sbus.Writes){int v=w[1];if((v&128)!=0){latch=v;int ch=v>>5&3;if((v&16)!=0)level[ch]=v&15;else period[ch]=(period[ch]&0x3F0)|(v&15);}else{int ch=latch>>5&3;period[ch]=(period[ch]&15)|(v&63)<<4;}}
    for(int ch=0;ch<3;ch++){Eq(atten[level[ch]],regs[0x43+ch]&63,"AdLib carrier matches Tandy attenuation");if(level[ch]!=15){int fnum=regs[0xA0+ch]|(regs[0xB0+ch]&3)<<8,block=regs[0xB0+ch]>>2&7;double wanted=111860.78/(period[ch]==0?1024:period[ch]),got=fnum*49716.0/Math.Pow(2,20-block);if(wanted<6000)Yes(Math.Abs(got-wanted)<=wanted*.01,"AdLib pitch within 1percent");Yes((regs[0xB0+ch]&32)!=0,"active tone keyed");}}
    Eq(atten[level[3]],regs[0x54]&63,"AdLib snare matches Tandy noise level");if(level[3]!=15)Eq(40,regs[0xBD],"noise snare key");
   }
  }
 }
}
