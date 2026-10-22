// Direct conversion of pinned src/sndspk.pas. Sequencer remains in the required SoundDrv host.
namespace Por.Core
{
    public static class SndSpk
    {
        private static void Play(ushort tune) { SoundDrv.ProgramSpeakerVoices(tune); }
        public static void Install()
        {
            GDVars.sound.name = "PC Speaker";
            GDVars.sound.mode = C_Sound.MODE_SPEAKER;
            GDVars.sound.Play = Play;
            GDVars.sound.singleStep = false;
        }
    }
}
