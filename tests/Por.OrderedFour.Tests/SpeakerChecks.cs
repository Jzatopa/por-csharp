using System;using System.Linq;using Por.Core;
static partial class Tests
{
 static void Speaker()
 {
  var bus=new Bus();var state=new SoundSequenceState();var seq=new SpeakerSequencer(bus,state);
  var raw=System.IO.File.ReadAllBytes("evidence/ordered-four-2026-10-22/speaker-data.bin");Eq(raw.Length,seq.Arrangement.Data.Length,"speaker compiled data length");for(int k=0;k<raw.Length;k++)Eq(raw[k],seq.Arrangement.Data[k],"speaker compiled source data byte");for(int r=0;r<29;r++)for(int v=0;v<4;v++)Eq(seq.Arrangement.ReadWord(seq.Arrangement.Labels["songTableA"]+8*r+2*v),seq.Arrangement.Songs[r,v],"speaker song pointer metadata");
  foreach(int tune in Enumerable.Range(0,30).Concat(new[]{255,65535}))
  {
   for(int i=0;i<8;i++)for(int j=0;j<24;j++)seq.Voices[i].Words[j]=W(13+i*24+j);for(int i=0;i<4;i++)state.VoiceTune[i]=99;seq.Program(W(tune));int row=tune>=29?0:tune;Eq(row,state.CurrentTune,"speaker invalid tune fallback");Eq(0,state.SequenceBusy,"speaker busy released");
   for(int i=0;i<8;i++)for(int j=0;j<24;j++){bool supplied=i<4&&seq.Arrangement.Songs[row,i]!=0;int expected=!supplied?13+i*24+j:j==0?1:j==1?seq.Arrangement.Songs[row,i]:0;Eq(expected,seq.Voices[i].Words[j],"speaker zero table pointers preserve complete voice");}
   for(int i=0;i<4;i++)Eq(i==0||row<20?row:99,state.VoiceTune[i],"speaker zero pointers preserve per-voice tune tracker");
  }
  for(int mask=0;mask<16;mask++)for(int stopped=0;stopped<16;stopped++)foreach(int output in new[]{0,1,7})foreach(int old in new[]{0,3,85,170,255})
  {
   state.CurrentTune=1;state.OutputOn=B(output);bus.ReadValue=B(old);bus.Writes.Clear();bus.Log.Clear();int chosen=-1,stepped=0;
   for(int i=0;i<4;i++){seq.Voices[i].Duration=W((mask>>i)&1);seq.Voices[i].Level=W(i+1);seq.Voices[i].OutPeriod=W(1234+i);if((mask&(1<<i))!=0&&(stopped&(1<<i))==0&&chosen<0)chosen=i;}
   seq.StepVoice=i=>{stepped++;if((stopped&(1<<i))!=0)seq.Voices[i].Duration=0;};seq.Tick();Eq(System.Numerics.BitOperations.PopCount((uint)mask),stepped,"speaker steps every active voice");Eq(chosen,seq.SelectedVoice,"speaker first surviving nonzero-level priority");bool audible=output!=0&&chosen>=0;Eq(audible?3:1,bus.Writes.Count,"speaker PIT write gating");if(audible){Eq((1234+chosen)%256,bus.Writes[0][1],"speaker divisor low byte first");Eq((1234+chosen)/256,bus.Writes[1][1],"speaker divisor high byte");}Eq((old&252)|(audible?(chosen+1)&3:0),bus.Writes.Last()[1],"speaker preserves non-gate port61 bits");Yes(bus.Log.Last().StartsWith("W97:"),"speaker gate write last");
  }
  state.CurrentTune=0;seq.StepVoice=null;bus.Writes.Clear();seq.Tick();Eq(1,bus.Writes.Count,"silent tune no stepping or PIT");state.CurrentTune=1;seq.Voices[0].Duration=1;bool missing=false;try{seq.Tick();}catch(NotSupportedException){missing=true;}Yes(missing,"speaker missing shared stepper explicit");
 }
}
