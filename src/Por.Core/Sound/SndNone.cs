namespace Por.Core
{
    public static class SndNone
    {
        public static void Install()
        {
            GDVars.sound.name = "None";
            GDVars.sound.mode = (byte)C_Sound.MODE_DISABLED;
            GDVars.sound.Play = Play;
            GDVars.sound.singleStep = false;
        }

        private static void Play(ushort tune)
        {
        }
    }
}
