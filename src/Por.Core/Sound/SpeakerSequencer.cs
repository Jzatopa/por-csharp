// Direct conversion of sndspk.inc tick_spkr and ProgramSpeakerVoices; source arrangement is a relocated data image.
namespace Por.Core
{
    public sealed class SpeakerSequencer:FourVoiceSequencer
    {
        public int SelectedVoice{get;private set;}=-1;
        public SpeakerSequencer(ISoundPortBus bus,SoundSequenceState state):base(bus,state,SpeakerArrangement.Create()){}
        public override void Tick()
        {
            int gate=0;
            if(State.CurrentTune!=0)
            {
                SelectedVoice=-1;
                for(int i=0;i<4;i++)if(Voices[i].Duration!=0)
                {
                    Step(i);
                    if(SelectedVoice<0&&Voices[i].Level!=0&&Voices[i].Duration!=0)SelectedVoice=i;
                }
                if(State.OutputOn!=0&&SelectedVoice>=0)
                {
                    ushort period=Voices[SelectedVoice].OutPeriod;
                    Bus.Write(0x42,unchecked((byte)period));Bus.Write(0x42,unchecked((byte)(period>>8)));
                    gate=Voices[SelectedVoice].Level&3;
                }
            }
            byte old=Bus.Read(0x61);Bus.Write(0x61,unchecked((byte)((old&0xFC)|gate)));
        }
    }
}
