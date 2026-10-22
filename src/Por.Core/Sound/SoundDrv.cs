// Source: Talonius pool-of-radiance pinned 7616920a (src/sounddrv.pas); Qwen3.8:27b initial draft, supervisor repaired.
using System;


namespace Por.Core
{
    // SoundDriver implements the sequencer; a platform supplies the timer vector, port bus and clock.
    public interface ISoundDriverHost
    {
        void InstallSoundTimer();
        void UninstallSoundTimer();
        void ProgramSpeakerVoices(ushort tuneId);
        void ProgramTandyVoices(ushort tuneId);
        void OplWrite(ushort reg, ushort value);
        void OplReset();
        void SetSoundOutputEnabled(ushort enabledValue);
        ushort SoundTimerTicks();
        bool SoundTimerHooked();
        void SetSoundDevice(ushort mode);
        ushort SoundVoiceTune(ushort voice);
        void GetTime(out ushort hour, out ushort minute, out ushort second, out ushort hundredthSecond);
    }

    public static class SoundDrv
    {
        public static ISoundDriverHost Host;

        private static ISoundDriverHost H
        {
            get
            {
                if (Host == null)
                    throw new NotSupportedException("SoundDrv: SOUNDDRV.OBJ host not installed");
                return Host;
            }
        }

        private const int CMD_DISABLE = 0;
        private const int CMD_ENABLE = 1;
        private const int CMD_STOP = 255;
        private const int SILENT_TUNE = 0;
        private const int OUTPUT_DISABLED = 0;
        private const int OUTPUT_ENABLED = 1;
        private const int TICKS_PER_1000_CS = 2367;
        private const int MAX_CLOCK_PASSES = 60000;

        private static ushort musicTune;

        internal static void InstallSoundTimer() => H.InstallSoundTimer();

        internal static void UninstallSoundTimer() => H.UninstallSoundTimer();

        public static void ProgramSpeakerVoices(ushort tuneId) => H.ProgramSpeakerVoices(tuneId);

        public static void ProgramTandyVoices(ushort tuneId) => H.ProgramTandyVoices(tuneId);

        public static void OplWrite(ushort reg, ushort value) => H.OplWrite(reg, value);

        public static void OplReset() => H.OplReset();

        internal static void SetSoundOutputEnabled(ushort enabledValue) => H.SetSoundOutputEnabled(enabledValue);

        internal static ushort SoundTimerTicks() => H.SoundTimerTicks();

        internal static bool SoundTimerHooked() => H.SoundTimerHooked();

        public static void SetSoundDevice(ushort mode) => H.SetSoundDevice(mode);

        internal static ushort SoundVoiceTune(ushort voice) => H.SoundVoiceTune(voice);

        internal static void SilenceAll()
        {
            ProgramTandyVoices((ushort)SILENT_TUNE);
            ProgramSpeakerVoices((ushort)SILENT_TUNE);
        }

        public static int SoundTime()
        {
            H.GetTime(out ushort hour, out ushort minute, out ushort second, out ushort hundredthSecond);
            return unchecked((int)(hour * 360000 + (int)minute * 6000 + second * 100 + hundredthSecond));
        }

        public static void SoundDelay(ushort centiseconds)
        {
            if (SoundTimerHooked())
            {
                int ticks = unchecked((int)((int)centiseconds * TICKS_PER_1000_CS + 500) / 1000);
                int elapsed = 0;
                ushort last = SoundTimerTicks();
                while (elapsed < ticks)
                {
                    ushort sample = SoundTimerTicks();
                    elapsed = unchecked((int)(elapsed + (ushort)(sample - last)));
                    last = sample;
                }
                return;
            }

            int target = SoundTime() + centiseconds;
            ushort passes = 0;
            while (true)
            {
                int now = SoundTime();
                if (target - now > centiseconds) now = target + 1;
                passes = unchecked((ushort)(passes + 1));
                if (passes == MAX_CLOCK_PASSES) now = target;
                if (now >= target) break;
            }
        }

        public static void SoundCommand(ushort cmd)
        {
            if (GDVars.soundMode == C_Sound.MODE_DISABLED) return;
            SetSoundDevice(GDVars.sound.mode);
            if (cmd != CMD_ENABLE) musicTune = 0;
            if (GDVars.soundAutoStop > 0) InstallSoundTimer();

            if (cmd == CMD_DISABLE)
            {
                SilenceAll();
                SetSoundOutputEnabled((ushort)OUTPUT_DISABLED);
            }
            else if (cmd == CMD_ENABLE)
            {
                SetSoundOutputEnabled((ushort)OUTPUT_ENABLED);
            }
            else if (cmd == CMD_STOP)
            {
                SilenceAll();
            }
            else
            {
                GDVars.sound.Play(unchecked((ushort)(cmd - 1)));
            }

            if (GDVars.soundAutoStop > 0 && !MusicPlaying())
            {
                SoundDelay(10);
                SilenceAll();
                UninstallSoundTimer();
            }
        }

        public static void SoundSuspend()
        {
            GDVars.soundSaved = GDVars.soundMode;
            if (GDVars.soundMode == C_Sound.MODE_DISABLED) return;
            musicTune = 0;
            SetSoundDevice(GDVars.sound.mode);
            InstallSoundTimer();
            ProgramSpeakerVoices((ushort)SILENT_TUNE);
            ProgramTandyVoices((ushort)SILENT_TUNE);
            UninstallSoundTimer();
        }

        public static void SoundSetAutoStop(ushort autoStop)
        {
            if (GDVars.soundMode == C_Sound.MODE_DISABLED) return;
            if (autoStop > 0)
            {
                if (GDVars.soundAutoStop == 0 && !MusicPlaying()) UninstallSoundTimer();
            }
            else
            {
                InstallSoundTimer();
            }
            GDVars.soundAutoStop = autoStop;
        }

        public static void PlayMusic(ushort tune)
        {
            if (GDVars.soundMode == C_Sound.MODE_DISABLED || GDVars.musicOn == 0) return;
            SetSoundDevice(GDVars.sound.mode);
            InstallSoundTimer();
            musicTune = tune;
            GDVars.sound.Play(tune);
        }

        public static void StopMusic()
        {
            if (musicTune == 0) return;
            musicTune = 0;
            if (GDVars.soundMode == C_Sound.MODE_DISABLED) return;
            GDVars.sound.Play((ushort)SILENT_TUNE);
            if (GDVars.soundAutoStop > 0) UninstallSoundTimer();
        }

        public static bool MusicPlaying()
        {
            return musicTune != 0 && SoundTimerHooked() && SoundVoiceTune(0) == musicTune;
        }

        // The host must call this on every way out of the program.
        public static void SoundExit()
        {
            UninstallSoundTimer();
        }
    }
}
