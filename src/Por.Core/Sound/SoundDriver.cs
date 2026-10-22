// Direct managed conversion of pinned sounddrv.asm. Port I/O and displaced INT8/clock/video/exit are explicit host services.
using System;
namespace Por.Core
{
    public interface ISoundTimerPlatform
    {
        Action TimerVector { get; set; }
        void GetTime(out ushort hour,out ushort minute,out ushort second,out ushort hundredthSecond);
        // Must reset the current video mode, print at (0,0), attribute7, advance cursor, then terminate with code255.
        void Shutdown(byte[] message,byte exitCode);
    }
    public sealed class SoundDriver:ISoundDriverHost
    {
        private readonly ISoundPortBus bus;
        private readonly ISoundTimerPlatform platform;
        private Action oldVector;
        private readonly Action interrupt;
        public readonly SoundSequenceState State=new SoundSequenceState();
        public readonly SpeakerSequencer Speaker;
        public readonly TandySequencer Tandy;
        public readonly OplSequencer Opl;
        public byte Device {get;private set;}=2;
        public ushort Ticks {get;private set;}
        public bool Hooked {get;private set;}
        public ushort ExitCount;
        public ushort ReturnAddress {get;private set;}
        private byte divisor=13;
        private readonly byte[] exitMessage={0xD2,0xF5,0xEE,0xAD,0xF4,0xE9,0xED,0xE5,0xA0,0xA0,0xC5,0xF2,0xF2,0xEF,0xF2,0xA0,0xB2,0xB0,0xB9,0xA0,0xE1,0xF4,0xA0,0xB0,0xC2,0xC1,0xC4,0xBA,0xB0,0xB0,0xB3,0xB8,0x8A,0x8D};
        public SoundDriver(ISoundPortBus ports,ISoundTimerPlatform timer)
        {
            bus=ports??throw new ArgumentNullException(nameof(ports));platform=timer??throw new ArgumentNullException(nameof(timer));
            Speaker=new SpeakerSequencer(bus,State);Tandy=new TandySequencer(bus,State);Opl=new OplSequencer(bus);
            Speaker.StepVoice=i=>StepVoice(Speaker,i);Tandy.StepVoice=i=>StepVoice(Tandy,i);interrupt=Interrupt;
        }
        public void InstallSoundTimer()
        {
            if(Hooked)return;
            if(platform.TimerVector!=interrupt){oldVector=platform.TimerVector;platform.TimerVector=interrupt;}
            Hooked=true;bus.Write(0x43,0xB6);bus.Write(0x43,0x36);bus.Write(0x40,0xB1);bus.Write(0x40,0x13);
        }
        public void UninstallSoundTimer()
        {
            if(!Hooked)return;Hooked=false;platform.TimerVector=oldVector;
            bus.Write(0x43,0x36);bus.Write(0x40,0);bus.Write(0x40,0);GateOff();
            if(Device==3)Opl.Silence();else if(Device==0)foreach(byte b in new byte[]{0x9F,0xBF,0xDF,0xFF})bus.Write(0xC0,b);
        }
        private void GateOff(){bus.Write(0x61,unchecked((byte)(bus.Read(0x61)&0xFC)));}
        public void SetSoundDevice(ushort mode)
        {
            byte next=unchecked((byte)mode);if(next>3)next=2;
            if(next!=Device){if(Device==3)Opl.Silence();else if(Device==1)GateOff();}
            Device=next;
        }
        public void ProgramSpeakerVoices(ushort tune){Speaker.Program(tune);}
        public void ProgramTandyVoices(ushort tune){Tandy.Program(tune);}
        public void SetSoundOutputEnabled(ushort enabled){State.OutputOn=unchecked((byte)enabled);}
        public ushort SoundTimerTicks(){return Ticks;}
        public bool SoundTimerHooked(){return Hooked;}
        public ushort SoundVoiceTune(ushort voice){return State.VoiceTune[voice&3];}
        public void OplWrite(ushort reg,ushort value){Opl.Write(reg,value);}
        public void OplReset(){Opl.Reset();}
        public void GetTime(out ushort h,out ushort m,out ushort s,out ushort cs){platform.GetTime(out h,out m,out s,out cs);}
        // TRACE's entry: sequence only, with neither drvTicks nor BIOS divider advanced.
        public void TraceTick()
        {
            if(State.SequenceBusy!=0)return;State.SequenceBusy=1;
            switch(Device){case 0:Tandy.Tick();break;case 1:Speaker.Tick();break;case 3:Tandy.TickOpl(Opl);break;}
            State.SequenceBusy=0;
        }
        public void Interrupt()
        {
            Ticks=unchecked((ushort)(Ticks+1));
            if(ExitCount!=0){ExitCount--;if(ExitCount==0){var text=new byte[33];for(int i=0;i<33;i++){exitMessage[i]&=0x7F;text[i]=exitMessage[i];}platform.Shutdown(text,255);throw new InvalidOperationException("Sound shutdown host returned after process termination.");}}
            TraceTick();divisor=unchecked((byte)(divisor-1));
            if(divisor==0){divisor=13;if(oldVector==null)throw new NotSupportedException("Sound: displaced INT8 handler missing.");oldVector();}else bus.Write(0x20,0x20);
        }
        private static ushort W(int n){return unchecked((ushort)n);}
        public void StepVoice(FourVoiceSequencer device,int voice)
        {
            var v=device.Voices[voice];v[10]=W(v[10]+v[12]);v[4]=W(v[4]+v[6]);int delta=0;
            ushort phase=W(v[30]+v[32]);
            if(phase!=0){if(phase>=v[36])phase=W(phase-v[36]);v[30]=phase;int sample=unchecked((sbyte)device.Arrangement.Data[W((unchecked((short)phase)>>4)+v[28])]);delta=(sample*unchecked((short)v[34]))>>8;}
            v[8]=W(v[4]+delta);
            if(v[20]!=0){v[20]--;if(v[20]==0){v[24]=16;v[26]=1;}}
            v[0]=W(v[0]-1);if(v[0]==0)RunStream(device,voice);
            if(v[26]!=0){v[26]--;if(v[26]==0){int budget=65536;while(true){if(--budget==0)throw new InvalidOperationException("Non-yielding sound envelope.");ushort p=W(v[22]+v[24]);ushort value=device.Arrangement.ReadWord(p),ticks=device.Arrangement.ReadWord(W(p+2));v[24]=W(v[24]+4);if(ticks==65535){v[10]=value;if(value==0)v[12]=0;}else{v[12]=value;v[26]=ticks;break;}}}}
        }
        private static void Reset(SoundVoice v)
        {
            foreach(int field in new[]{4,6,8,10,12,16,18,22,24,26,28,30,32,34,36})v[field]=0;
        }
        public void RunStream(FourVoiceSequencer device,int voice)
        {
            SoundVoice original=device.Voices[voice],current=original;ushort cursor=original.Stream;
            if(cursor==0)return;var a=device.Arrangement;int budget=1000000;
            while(true)
            {
                if(--budget==0)throw new InvalidOperationException("Non-yielding sound stream.");byte cmd=a.Data[cursor];cursor=W(cursor+1);
                if(cmd>=0xFA)
                {
                    switch(cmd)
                    {
                        case 0xFA:Reset(current);break;
                        case 0xFB:cursor=ReturnAddress;ReturnAddress=0;if(cursor==0){original.Duration=0;EndStream(original,voice,0);return;}break;
                        case 0xFC:ushort target=a.ReadWord(cursor);cursor=W(cursor+2);ReturnAddress=cursor;cursor=target;break;
                        case 0xFD:current=device.Voices[a.ReadWord(cursor)/48];cursor=W(cursor+2);Reset(current);break;
                        case 0xFE:byte f=a.Data[cursor];cursor=W(cursor+1);ushort jump=a.ReadWord(cursor);cursor=W(cursor+2);if(current[f]==0)cursor=jump;else{current[f]--;if(current[f]!=0)cursor=jump;}break;
                        case 0xFF:byte field=a.Data[cursor];cursor=W(cursor+1);current[field]=a.ReadWord(cursor);cursor=W(cursor+2);if(field==0){EndStream(original,voice,cursor);return;}break;
                    }
                }
                else
                {
                    // Back-to-back note records are not command-decoded after the first one.
                    while(true)
                    {
                        var noteVoice=device.Voices[cmd>>5];byte pitch=a.Data[cursor],length=a.Data[W(cursor+1)];int tempo=current[14]&255;
                        current.Duration=W((tempo<=1?1:tempo)*length);
                        if((pitch&127)!=127)
                        {
                            noteVoice.Duration=current.Duration;noteVoice[20]=current.Duration<noteVoice[16]?(ushort)0:W(current.Duration-noteVoice[16]);
                            ushort note=W((pitch&127)+noteVoice[18]);noteVoice[24]=0;noteVoice[26]=1;
                            // 8086 SHR uses the full 8-bit count, not the later x86 five-bit mask.
                            int octave=note/12,semi=note%12;int shift=octave&255;ushort period=shift>=16?(ushort)0:W(a.Notes[semi]>>shift);
                            noteVoice[4]=period;noteVoice[8]=period;
                        }
                        cursor=W(cursor+2);if((pitch&128)!=0){EndStream(original,voice,cursor);return;}cmd=a.Data[cursor];cursor=W(cursor+1);
                    }
                }
            }
        }
        private void EndStream(SoundVoice original,int voice,ushort cursor)
        {
            if(original.Duration==0){State.VoiceTune[voice]=0;cursor=0;}original.Stream=cursor;
        }
    }
}
