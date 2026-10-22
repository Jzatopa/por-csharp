// Direct conversion of pinned src/sndopl.pas; port reads require an explicit hardware adapter.
using System;
namespace Por.Core
{
    public static class SndOpl
    {
        public static Func<ushort, byte> ReadPort;
        private static byte Status()
        {
            if (ReadPort == null) throw new NotSupportedException("SndOpl: OPL status port reader not installed.");
            return ReadPort(0x388);
        }
        public static bool Detect()
        {
            SoundDrv.OplWrite(4, 0x60); SoundDrv.OplWrite(4, 0x80);
            byte before = Status();
            SoundDrv.OplWrite(2, 255); SoundDrv.OplWrite(4, 0x21);
            for (int i = 1; i <= 200; i++) Status();
            byte after = Status();
            SoundDrv.OplWrite(4, 0x60); SoundDrv.OplWrite(4, 0x80);
            return (before & 0xE0) == 0 && (after & 0xE0) == 0xC0;
        }
        public static void Play(ushort tune)
        {
            if (tune >= C_Sound.MUSIC_TITLE)
            {
                SoundDrv.SetSoundDevice(C_Sound.MODE_ADLIB);
                SoundDrv.ProgramTandyVoices(tune);
            }
            else
            {
                SoundDrv.SetSoundDevice(C_Sound.MODE_SPEAKER);
                SoundDrv.ProgramSpeakerVoices(tune);
            }
        }
        public static void Install()
        {
            if (Detect())
            {
                SoundDrv.OplReset();
                GDVars.sound.name = "AdLib"; GDVars.sound.mode = C_Sound.MODE_ADLIB;
                GDVars.sound.Play = Play; GDVars.sound.singleStep = false;
            }
            else { SndSpk.Install(); GDVars.sound.name = "Speaker (no AdLib)"; }
        }
    }
}
