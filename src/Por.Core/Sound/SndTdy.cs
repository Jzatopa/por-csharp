// Direct conversion of pinned src/sndtdy.pas. Sequencer remains in the required SoundDrv host.
namespace Por.Core
{
    public static class SndTdy
    {
        private static void Play(ushort tune) { SoundDrv.ProgramTandyVoices(tune); }
        public static void Install()
        {
            GDVars.sound.name = "Tandy";
            GDVars.sound.mode = C_Sound.MODE_TANDY;
            GDVars.sound.Play = Play;
            GDVars.sound.singleStep = false;
        }
    }
}
