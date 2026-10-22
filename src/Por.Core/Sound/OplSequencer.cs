// Direct conversion of pinned sndopl.inc. The device module, not the DOS timer owner.
using System;
namespace Por.Core
{
    public sealed class OplSequencer
    {
        private readonly ISoundPortBus bus;
        private readonly ushort[] frequencies={65535,65535,65535,65535};
        private readonly byte[] levels={255,255,255,255};
        private byte rhythm=255;
        private static readonly byte[] channels={0,1,2,7},carriers={3,4,5,20};
        private static readonly byte[] attenuation={0,3,5,8,11,13,16,19,21,24,27,29,32,35,37,63};
        private static readonly uint[] numerators={2359296,1179648,589824,294912,147456,73728,36864,18432};
        private static readonly ushort[] noise={0x1658,0x1258,0x0E58};
        private static readonly byte[,] patch={{0x20,0x21},{0x23,0x21},{0x40,0x1A},{0x43,63},{0x60,0xF0},{0x63,0xF0},{0x80,15},{0x83,15},{0xE0,0},{0xE3,0}};
        public OplSequencer(ISoundPortBus portBus){bus=portBus??throw new ArgumentNullException(nameof(portBus));}
        public void Write(ushort register,ushort value)
        {
            bus.Write(0x388,unchecked((byte)register));for(int i=0;i<6;i++)bus.Read(0x388);
            bus.Write(0x389,unchecked((byte)value));for(int i=0;i<35;i++)bus.Read(0x388);
        }
        private void Level(int voice,byte value){if(levels[voice]==value)return;levels[voice]=value;Write(unchecked((byte)(0x40+carriers[voice])),value);}
        private void Frequency(int voice,ushort value){if(frequencies[voice]==value)return;frequencies[voice]=value;Write(unchecked((byte)(0xA0+channels[voice])),unchecked((byte)value));Write(unchecked((byte)(0xB0+channels[voice])),unchecked((byte)(value>>8)));}
        public static ushort Pitch(ushort period)
        {
            uint divisor=period==0?1024u:period;
            for(int block=0;block<8;block++){uint q=numerators[block]/divisor;if(q<=1023)return unchecked((ushort)(q|(uint)(block<<10)|0x2000));}
            return 0x3FFF;
        }
        public void Tick(SoundSequenceState state,SoundVoice[] voices,Action<int> stepVoice)
        {
            if(state.CurrentTune!=0)
            {
                for(int i=0;i<4;i++)if(voices[i].Duration!=0){if(stepVoice==null)throw new NotSupportedException("OPL: shared voice sequencer is not bound.");stepVoice(i);}
                if(state.OutputOn!=0)
                {
                    if(rhythm!=0x28){rhythm=0x28;Write(0xBD,rhythm);}
                    for(int i=0;i<4;i++)
                    {
                        Level(i,attenuation[(65535-voices[i].Level)>>12]);ushort period=voices[i].OutPeriod,pair;
                        if(i!=3)pair=Pitch(unchecked((ushort)(period>>6)));
                        else{int rate=(period>>6)&3;pair=rate==3?unchecked((ushort)(frequencies[2]&~0x2000)):noise[rate];}
                        Frequency(i,pair);
                    }
                    return;
                }
            }
            for(int i=0;i<4;i++)Level(i,63);
        }
        public void Reset()
        {
            for(int reg=1;reg<0xF6;reg++)Write(unchecked((ushort)reg),0);Write(1,0x20);
            for(int i=0;i<4;i++)
            {
                int offset=carriers[i]-3;for(int j=0;j<10;j++)Write(unchecked((byte)(patch[j,0]+offset)),patch[j,1]);
                Write(unchecked((byte)(0xC0+channels[i])),10);levels[i]=63;frequencies[i]=65535;
            }
            rhythm=0;
        }
        public void Silence()
        {
            for(int i=0;i<4;i++){Write(unchecked((byte)(0x40+carriers[i])),63);levels[i]=63;Write(unchecked((byte)(0xB0+channels[i])),0);frequencies[i]=65535;}
            Write(0xBD,0x20);rhythm=0x20;
        }
    }
}
