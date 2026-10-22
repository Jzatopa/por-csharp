using System;using System.Collections.Generic;using System.Linq;using Por.Core;
static partial class Tests
{
 static void Tandy()
 {
  var bus=new Bus();var state=new SoundSequenceState();var seq=new TandySequencer(bus,state);
  var raw=System.IO.File.ReadAllBytes("evidence/ordered-four-2026-10-22/tandy-data.bin");Eq(raw.Length,seq.Arrangement.Data.Length,"Tandy compiled data length");for(int k=0;k<raw.Length;k++)Eq(raw[k],seq.Arrangement.Data[k],"Tandy compiled source data byte");for(int r=0;r<29;r++)for(int v=0;v<4;v++)Eq(seq.Arrangement.ReadWord(seq.Arrangement.Labels["songTableB"]+8*r+2*v),seq.Arrangement.Songs[r,v],"Tandy song pointer metadata");
  foreach(int tune in Enumerable.Range(0,30).Concat(new[]{65535}))
  {
   for(int i=0;i<8;i++)for(int j=0;j<24;j++)seq.Voices[i].Words[j]=W(100+i*24+j);for(int i=0;i<4;i++)state.VoiceTune[i]=99;state.SequenceBusy=77;seq.Program(W(tune));int row=tune>=29?0:tune;Eq(row,state.CurrentTune,"Tandy invalid tune becomes silence");Eq(0,state.SequenceBusy,"Tandy loader releases busy flag");for(int i=0;i<4;i++){Eq(1,seq.Voices[i].Duration,"Tandy allrows arm supplied streams");Eq(seq.Arrangement.Songs[row,i],seq.Voices[i].Stream,"Tandy stream pointer");for(int j=2;j<24;j++)Eq(0,seq.Voices[i].Words[j],"Tandy cleared whole voice");Eq(row,state.VoiceTune[i],"Tandy tune tracker");}for(int i=4;i<8;i++)for(int j=0;j<24;j++)Eq(100+i*24+j,seq.Voices[i].Words[j],"Tandy spare voices retained");
  }
  state.CurrentTune=1;var steps=new List<int>();seq.StepVoice=i=>steps.Add(i);for(int p=0;p<65536;p+=7)
  {
   bus.Writes.Clear();steps.Clear();state.OutputOn=B(p%5==0?0:1);for(int i=0;i<4;i++){seq.Voices[i].Duration=W(i%2);seq.Voices[i].OutPeriod=W(p+i*71);seq.Voices[i].Level=W(p*17+i);}
   seq.Tick();Yes(steps.SequenceEqual(new[]{1,3}),"Tandy steps active voices before output gate");if(state.OutputOn==0){Yes(bus.Writes.Select(w=>w[1]).SequenceEqual(new[]{159,191,223,255}),"Tandy mute all attenuators");continue;}
   int n=0;for(int i=0;i<4;i++){int period=seq.Voices[i].OutPeriod,level=seq.Voices[i].Level,latch=128+32*i;Eq(192,bus.Writes[n][0],"Tandy port");Eq(latch^(period/64%16),bus.Writes[n++][1],"Tandy period latch low nibble");if(i<3)Eq(period/1024,bus.Writes[n++][1],"Tandy high six period bits");Eq((latch^((65535-level)/4096))+16,bus.Writes[n++][1],"Tandy inverted attenuation");}Eq(11,n,"Tandy tone/noise bytecount");
  }
  seq.StepVoice=null;state.CurrentTune=0;bus.Writes.Clear();seq.Tick();Eq(4,bus.Writes.Count,"zero tune no stepping");state.CurrentTune=1;bool missing=false;try{seq.Tick();}catch(NotSupportedException){missing=true;}Yes(missing,"Tandy missing voice_step explicit");
  seq.StepVoice=i=>{seq.Voices[i].OutPeriod=6400;seq.Voices[i].Level=65535;};var opl=new OplSequencer(bus);bus.Writes.Clear();seq.TickOpl(opl);Yes(bus.Writes.Any(w=>w[0]==904&&w[1]==189),"Tandy voices drive real OPL rhythm module");
 }
}
