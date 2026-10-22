using System;
namespace Por.Core
{
    // The source VOICE is 24 words (48 bytes). Word-indexed storage preserves SSET's byte offsets.
    public sealed class SoundVoice
    {
        public readonly ushort[] Words=new ushort[24];
        public ushort this[int byteOffset]{get{return Words[byteOffset/2];}set{Words[byteOffset/2]=value;}}
        public ushort Duration{get{return this[0];}set{this[0]=value;}}
        public ushort Stream{get{return this[2];}set{this[2]=value;}}
        public ushort OutPeriod{get{return this[8];}set{this[8]=value;}}
        public ushort Level{get{return this[10];}set{this[10]=value;}}
        public void Clear(){Array.Clear(Words,0,Words.Length);}
    }
    public sealed class SoundSequenceState
    {
        public ushort CurrentTune;
        public byte OutputOn=1,SequenceBusy;
        public readonly byte[] VoiceTune=new byte[4];
    }
    public interface ISoundPortBus
    {
        byte Read(ushort port);
        void Write(ushort port,byte value);
    }
}
