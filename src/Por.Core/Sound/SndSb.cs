// Direct conversion of pinned src/sndsb.pas. Ports/DMA allocation stay behind a required hardware adapter.
using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
namespace Por.Core
{
    public sealed class SoundDmaBlock
    {
        public byte[] Data; public int PhysicalAddress;
    }
    public interface ISoundBlasterHost
    {
        byte ReadPort(ushort port);void WritePort(ushort port,byte value);
        string BlasterEnvironment { get; } int MaxAvailable { get; }
        SoundDmaBlock Allocate(ushort size);void Free(SoundDmaBlock block);
    }
    public static class SndSb
    {
        public static ISoundBlasterHost Host;
        internal static Func<string,Stream> OpenEffects = name => { FileStream f;return FileUtil.OpenReadOnly(out f,name)?f:null; };
        private static ISoundBlasterHost H { get { if(Host==null)throw new NotSupportedException("SndSb: DSP/DMA host missing.");return Host;} }
        internal struct Effect { public byte Format,Time;public ushort Length,Start; }
        internal static ushort deviceBase=0x220;internal static byte dma=1;
        internal static bool exitInstalled;internal static SoundDmaBlock store,bounce;internal static bool loadTried;
        internal static readonly Effect[] effects=new Effect[21];
        private static readonly byte[] Addresses={0,2,4,6},Counts={1,3,5,7},Pages={0x87,0x83,0x81,0x82};
        private static byte Read(int p){return H.ReadPort(unchecked((ushort)p));}private static void Write(int p,int v){H.WritePort(unchecked((ushort)p),unchecked((byte)v));}
        internal static bool DspWrite(byte value){for(int i=1;i<=2000;i++)if((Read(deviceBase+12)&128)==0){Write(deviceBase+12,value);return true;}return false;}
        internal static bool DspReset(){Write(deviceBase+6,1);for(int i=1;i<=8;i++)Read(deviceBase+6);Write(deviceBase+6,0);for(int i=1;i<=2000;i++)if((Read(deviceBase+14)&128)!=0)return Read(deviceBase+10)==0xAA;return false;}
        internal static void ReadBlaster()
        {
            deviceBase=0x220;dma=1;string s=(H.BlasterEnvironment??"");if(s.Length>255)s=s.Substring(0,255);s=(s+" ");if(s.Length>255)s=s.Substring(0,255);
            for(int i=0;i<s.Length-1;)
            {
                int k=i+1;while(k<s.Length&&s[k]!=' ')k++;
                string value=s.Substring(i+1,k-i-1);ushort n;
                if(char.ToUpperInvariant(s[i])=='A'&&ushort.TryParse(value,NumberStyles.AllowHexSpecifier,CultureInfo.InvariantCulture,out n))deviceBase=n;
                if(char.ToUpperInvariant(s[i])=='D'&&ushort.TryParse(value,NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out n)&&n<=3)dma=(byte)n;
                i=k+1;
            }
        }
        internal static bool Crosses(SoundDmaBlock p,int offset,ushort length){return ((p.PhysicalAddress+offset)&65535)+(int)length>65536;}
        internal static SoundDmaBlock PlaceBlock(ushort size)
        {
            var held=new List<SoundDmaBlock>();SoundDmaBlock p=null;
            for(int i=0;i<4;i++)if(p==null&&H.MaxAvailable>=size){p=H.Allocate(size);if(p==null)throw new InvalidOperationException("DMA allocator returned null despite available memory.");if(Crosses(p,0,size)){held.Add(p);p=null;}}
            foreach(var b in held)H.Free(b);return p;
        }
        internal static void LoadEffects()
        {
            loadTried=true;Array.Clear(effects,0,effects.Length);Stream f;
            try{f=OpenEffects(GDVars.gameDir+"SBSFX.DAT");}catch(Exception e)when(e is IOException||e is UnauthorizedAccessException){return;}
            if(f==null)return;
            SoundDmaBlock p=null;ushort size=0;
            try
            {
                using(f)
                {
                    long len=f.Length;if(len<6||len>0xFFF0||len>H.MaxAvailable)return;size=(ushort)len;p=H.Allocate(size);
                    if(p==null)throw new InvalidOperationException("DMA allocator returned null.");
                    int got=0,n;while(got<size&&(n=f.Read(p.Data,got,size-got))>0)got+=n;
                    if(got!=size){H.Free(p);return;}
                }
            }
            catch(IOException){if(p!=null)H.Free(p);return;}
            byte[] data=p.Data;int count=data[5];if(data[0]!='S'||data[1]!='B'||data[2]!='F'||data[3]!='X'||data[4]!=2||6+7*count>size){H.Free(p);return;}
            ushort longest=0;
            for(int i=0;i<count;i++)
            {
                int at=6+7*i,tune=data[at];if(tune<1||tune>20)continue;
                Effect e=new Effect{Format=data[at+1],Time=data[at+2],Length=(ushort)(data[at+3]|data[at+4]<<8),Start=(ushort)(data[at+5]|data[at+6]<<8)};
                if(e.Length==0||e.Format>1||(int)e.Start+e.Length>size)e.Start=0;else if(e.Length>longest)longest=e.Length;
                effects[tune]=e;
            }
            store=p;if(longest>0)bounce=PlaceBlock(longest);
        }
        internal static bool PlayEffect(Effect e)
        {
            SoundDmaBlock p=store;int offset=e.Start;
            if(Crosses(p,offset,e.Length)){if(bounce==null)return false;Array.Copy(p.Data,offset,bounce.Data,0,e.Length);p=bounce;offset=0;}
            int phys=p.PhysicalAddress+offset;Read(deviceBase+14);DspWrite(0xD0);
            Write(10,4|dma);Write(12,0);Write(11,0x48|dma);Write(Addresses[dma],phys);Write(Addresses[dma],phys>>8);Write(Pages[dma],phys>>16);
            Write(Counts[dma],e.Length-1);Write(Counts[dma],(e.Length-1)>>8);Write(10,dma);
            // The source deliberately returns True even when one of the DSP writes times out.
            bool ok=DspWrite(0x40)&&DspWrite(e.Time);ok=ok&&DspWrite(e.Format==1?(byte)0x75:(byte)0x14);ok=ok&&DspWrite(unchecked((byte)(e.Length-1)))&&DspWrite(unchecked((byte)((e.Length-1)>>8)));
            return true;
        }
        internal static void Play(ushort tune)
        {
            if(tune>=1&&tune<=20)
            {
                if(!loadTried)LoadEffects();
                if(store!=null&&effects[tune].Start!=0){SoundDrv.SetSoundDevice(C_Sound.MODE_ADLIB);SoundDrv.ProgramTandyVoices(0);if(PlayEffect(effects[tune]))return;}
            }
            if(tune==0)DspWrite(0xD0);SndOpl.Play(tune);
        }
        public static void SbExit(){DspWrite(0xD0);}
        public static void ExitIfInstalled(){if(exitInstalled)SbExit();}
        public static void Install()
        {
            ReadBlaster();if(DspReset())
            {
                DspWrite(0xD1);SndOpl.Install();if(GDVars.sound.mode==C_Sound.MODE_ADLIB)GDVars.sound.name="Sound Blaster";else{GDVars.sound.mode=C_Sound.MODE_ADLIB;GDVars.sound.name="Blaster (no AdLib)";}
                GDVars.sound.Play=Play;GDVars.sound.singleStep=true;exitInstalled=true;
            }
            else{SndOpl.Install();if(GDVars.sound.mode==C_Sound.MODE_ADLIB)GDVars.sound.name="AdLib (no Blaster)";}
        }
        internal static void ResetForTests(){store=null;bounce=null;loadTried=false;exitInstalled=false;Array.Clear(effects,0,effects.Length);}
    }
}
