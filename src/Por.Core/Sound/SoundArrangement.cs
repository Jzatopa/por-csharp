using System;using System.Collections.Generic;
namespace Por.Core
{
    // Relocatable source data image. Offset0 is null, all stream/envelope/LFO references are resolved in this image.
    public sealed class SoundArrangement
    {
        public readonly byte[] Data;
        public readonly ushort[,] Songs;
        public readonly ushort[] Notes;
        public readonly Dictionary<string,ushort> Labels;
        public SoundArrangement(byte[] data,ushort[,] songs,ushort[] notes,Dictionary<string,ushort> labels){Data=data;Songs=songs;Notes=notes;Labels=labels;}
        public ushort ReadWord(int offset){return unchecked((ushort)(Data[offset]|Data[offset+1]<<8));}
    }
    public abstract class FourVoiceSequencer
    {
        protected readonly ISoundPortBus Bus;
        public readonly SoundSequenceState State;
        public readonly SoundArrangement Arrangement;
        public readonly SoundVoice[] Voices=new SoundVoice[8];
        // sounddrv.asm owns voice_step. A timer/stream host must bind it; never silently advance fake voices.
        public Action<int> StepVoice;
        protected FourVoiceSequencer(ISoundPortBus bus,SoundSequenceState state,SoundArrangement data){Bus=bus??throw new ArgumentNullException(nameof(bus));State=state??throw new ArgumentNullException(nameof(state));Arrangement=data;for(int i=0;i<8;i++)Voices[i]=new SoundVoice();}
        public void Program(ushort tune)
        {
            if(tune>=Arrangement.Songs.GetLength(0))tune=0;State.CurrentTune=tune;State.SequenceBusy=1;
            for(int i=0;i<4;i++)if(Arrangement.Songs[tune,i]!=0){Voices[i].Clear();Voices[i].Stream=Arrangement.Songs[tune,i];State.VoiceTune[i]=unchecked((byte)tune);Voices[i].Duration=1;}
            State.SequenceBusy=0;
        }
        protected void Step(int voice){if(StepVoice==null)throw new NotSupportedException("Sound: sounddrv.asm voice_step is not bound.");StepVoice(voice);}
        public abstract void Tick();
    }
}
