// Direct conversion of sndtdy.inc tick_tandy and ProgramTandyVoices; stream data generated separately.
namespace Por.Core
{
    public sealed class TandySequencer:FourVoiceSequencer
    {
        public TandySequencer(ISoundPortBus bus,SoundSequenceState state):base(bus,state,TandyArrangement.Create()){}
        public override void Tick()
        {
            if(State.CurrentTune!=0)
            {
                for(int i=0;i<4;i++)if(Voices[i].Duration!=0)Step(i);
                if(State.OutputOn!=0)
                {
                    for(int i=0;i<4;i++)
                    {
                        int latch=0x80+i*0x20,period=Voices[i].OutPeriod;
                        Bus.Write(0xC0,unchecked((byte)(((period>>6)&15)^latch)));
                        if(i!=3)Bus.Write(0xC0,unchecked((byte)(period>>10)));
                        Bus.Write(0xC0,unchecked((byte)((((65535-Voices[i].Level)>>12)^latch)+16)));
                    }
                    return;
                }
            }
            Bus.Write(0xC0,0x9F);Bus.Write(0xC0,0xBF);Bus.Write(0xC0,0xDF);Bus.Write(0xC0,0xFF);
        }
        public void TickOpl(OplSequencer opl){opl.Tick(State,Voices,i=>Step(i));}
    }
}
