// SoundDrv (sounddrv.pas at 7616920): the Pascal-side control logic over a recording host.
using System;
using System.Collections.Generic;
using System.Linq;
using Por.Core;

class FakeSound : ISoundDriverHost
{
    public List<string> Log = new List<string>();
    public bool Hooked; public ushort Voice0; public int TickReads; public int TimeReads;
    public int TickStep = 1; public ushort Ticks;
    public Func<int, int> Clock = n => 100000 + n;   // centisecond value for the n-th GetTime call (0-based)
    public void InstallSoundTimer() { Log.Add("install"); }
    public void UninstallSoundTimer() { Log.Add("uninstall"); }
    public void ProgramSpeakerVoices(ushort t) { Log.Add("spk " + t); }
    public void ProgramTandyVoices(ushort t) { Log.Add("tandy " + t); }
    public void OplWrite(ushort r, ushort v) { Log.Add("opl " + r + "=" + v); }
    public void OplReset() { Log.Add("oplreset"); }
    public void SetSoundOutputEnabled(ushort e) { Log.Add("out " + e); }
    public ushort SoundTimerTicks() { TickReads++; ushort v = Ticks; Ticks = unchecked((ushort)(Ticks + TickStep)); return v; }
    public bool SoundTimerHooked() { Log.Add("hooked?"); return Hooked; }
    public void SetSoundDevice(ushort m) { Log.Add("dev " + m); }
    public ushort SoundVoiceTune(ushort v) { Log.Add("voice " + v); return Voice0; }
    public void GetTime(out ushort h, out ushort m, out ushort s, out ushort hs)
    {
        int c = Clock(TimeReads++);
        h = (ushort)(c / 360000); m = (ushort)(c / 6000 % 60); s = (ushort)(c / 100 % 60); hs = (ushort)(c % 100);
    }
}

static partial class Tests
{
    static FakeSound sfx;

    static void SoundSetup(int mode = 1, ushort autoStop = 0, byte musicOn = 1)
    {
        sfx = new FakeSound();
        SoundDrv.Host = sfx;
        GDVars.soundMode = B(mode); GDVars.soundSaved = 99; GDVars.soundAutoStop = autoStop; GDVars.musicOn = musicOn;
        GDVars.sound = new Types.TSoundDriver { mode = B(mode == 2 ? 1 : mode) };
        GDVars.sound.Play = t => sfx.Log.Add("play " + t);
        // musicTune is the unit's private state: StopMusic clears it whatever the mode, so each case starts with no music started
        int savedMode = GDVars.soundMode; GDVars.soundMode = 1; SoundDrv.StopMusic(); GDVars.soundMode = B(savedMode);
        sfx.Log.Clear();
    }

    static void SoundTime()
    {
        foreach (int h in new[] { 0, 1, 12, 23 }) foreach (int m in new[] { 0, 1, 30, 59 }) foreach (int s in new[] { 0, 1, 45, 59 }) foreach (int hs in new[] { 0, 7, 99 })
        {
            SoundSetup();
            int c = h * 360000 + m * 6000 + s * 100 + hs;
            sfx.Clock = n => c;
            Eq(c, SoundDrv.SoundTime(), "SoundTime " + h + ":" + m + ":" + s + "." + hs);
        }
        // the largest values a clock can carry fit a LongInt
        SoundSetup(); sfx.Clock = n => 23 * 360000 + 59 * 6000 + 59 * 100 + 99;
        Eq(8639999, SoundDrv.SoundTime(), "last centisecond of the day");
    }

    static void SoundDelayChecks()
    {
        // hooked: counts the driver's own ticks, (centiseconds * 2367 + 500) div 1000 of them
        foreach (int cs in new[] { 0, 1, 2, 10, 100, 1000, 65535 })
            foreach (int step in new[] { 1, 3, 100 })
                foreach (int start in new[] { 0, 65530, 65535 })
                {
                    SoundSetup(); sfx.Hooked = true; sfx.TickStep = step; sfx.Ticks = (ushort)start;
                    SoundDrv.SoundDelay((ushort)cs);
                    long ticks = ((long)cs * 2367 + 500) / 1000;
                    long iterations = ticks == 0 ? 0 : (ticks + step - 1) / step;
                    Eq(1 + iterations, sfx.TickReads, "tick reads " + cs + "/" + step + "/" + start);
                    Eq(0, sfx.TimeReads, "the clock is not used while the timer is hooked");
                }
        // every centisecond count up to 2000, which covers the rounding edge ((cs * 2367 + 500) div 1000 rounds halves up)
        for (int cs = 0; cs < 2000; cs++)
        {
            SoundSetup(); sfx.Hooked = true; sfx.Ticks = 40000;
            SoundDrv.SoundDelay((ushort)cs);
            Eq(1 + ((long)cs * 2367 + 500) / 1000, sfx.TickReads, "tick reads (sweep) " + cs);
        }
        // unhooked: busy-waits against the clock
        foreach (int cs in new[] { 0, 1, 5, 10, 250 })
        {
            SoundSetup(); sfx.Hooked = false;
            SoundDrv.SoundDelay((ushort)cs);
            Eq(1 + Math.Max(cs, 1), sfx.TimeReads, "clock reads for " + cs);   // target read, then one read per centisecond (at least one)
        }
        // a clock that jumps back before the start ends the wait at once (target - now > centiseconds)
        foreach (int cs in new[] { 0, 1, 10 })
        {
            SoundSetup(); sfx.Clock = n => n == 0 ? 500000 : 500000 - 100000 - n;
            SoundDrv.SoundDelay((ushort)cs);
            Eq(2, sfx.TimeReads, "backward jump ends the wait " + cs);
        }
        // a stopped clock gives up after MAX_CLOCK_PASSES (60000) samples
        SoundSetup(); sfx.Clock = n => 777;
        SoundDrv.SoundDelay(10);
        Eq(1 + 60000, sfx.TimeReads, "stopped clock: 60000 passes");
        // midnight wrap: the target is past the end of the day, the clock restarts at 0 and the jump back ends the wait
        SoundSetup(); sfx.Clock = n => n == 0 ? 8639995 : 3 + n;
        SoundDrv.SoundDelay(10);
        Eq(2, sfx.TimeReads, "midnight ends the wait on the first sample");
    }

    static List<string> Expected(params string[] parts) { return parts.ToList(); }

    static void SoundCommandChecks()
    {
        // disabled: nothing is touched at all, not even the host
        foreach (int cmd in new[] { 0, 1, 2, 255 })
        {
            SoundSetup(C_Sound.MODE_DISABLED, 5);
            SoundDrv.Host = null;
            SoundDrv.SoundCommand((ushort)cmd);
            Eq(5, GDVars.soundAutoStop, "disabled leaves autostop");
        }
        foreach (int mode in new[] { 0, 1, 3 })
            foreach (int cmd in new[] { 0, 1, 2, 5, 254, 255 })
                foreach (int auto in new[] { 0, 3 })
                    foreach (bool musicPlaying in new[] { false, true })
                        foreach (bool hooked in new[] { false, true })
                        {
                            SoundSetup(mode, (ushort)auto);
                            if (musicPlaying) { SoundDrv.PlayMusic(7); }
                            sfx.Hooked = hooked; sfx.Voice0 = 7; sfx.Log.Clear(); sfx.TickReads = 0; sfx.TimeReads = 0;
                            SoundDrv.SoundCommand((ushort)cmd);
                            var e = new List<string> { "dev " + mode };
                            bool musicLeft = musicPlaying && cmd == 1;       // only CMD_ENABLE leaves the music playing
                            if (auto > 0) e.Add("install");
                            if (cmd == 0) { e.Add("tandy 0"); e.Add("spk 0"); e.Add("out 0"); }
                            else if (cmd == 1) e.Add("out 1");
                            else if (cmd == 255) { e.Add("tandy 0"); e.Add("spk 0"); }
                            else e.Add("play " + (cmd - 1));
                            int ticksExpected = 0, timeExpected = 0;
                            if (auto > 0)
                            {
                                // MusicPlaying: short-circuit on musicTune, then hooked, then voice 0
                                bool playing = false;
                                if (musicLeft) { e.Add("hooked?"); if (hooked) { e.Add("voice 0"); playing = true; } }
                                if (!playing)
                                {
                                    e.Add("hooked?");                       // SoundDelay(10): which clock
                                    if (hooked) ticksExpected = 1 + 24; else timeExpected = 1 + 10;
                                    e.Add("tandy 0"); e.Add("spk 0"); e.Add("uninstall");
                                }
                            }
                            EqS(string.Join("|", e), string.Join("|", sfx.Log), "SoundCommand " + mode + "/" + cmd + "/" + auto + "/" + musicPlaying + "/" + hooked);
                            Eq(ticksExpected, sfx.TickReads, "tick reads"); Eq(timeExpected, sfx.TimeReads, "time reads");
                        }
    }

    static void SoundMusicChecks()
    {
        // SoundSuspend
        foreach (int mode in new[] { 0, 1, 2, 3 })
        {
            SoundSetup(mode); GDVars.sound.mode = 1;
            SoundDrv.SoundSuspend();
            Eq(mode, GDVars.soundSaved, "saved mode " + mode);
            if (mode == C_Sound.MODE_DISABLED) EqS("", string.Join("|", sfx.Log), "disabled suspend does nothing");
            else EqS("dev 1|install|spk 0|tandy 0|uninstall", string.Join("|", sfx.Log), "suspend " + mode);
        }
        // SoundSetAutoStop
        foreach (int mode in new[] { 1, 2 })
            foreach (int old in new[] { 0, 4 })
                foreach (int nw in new[] { 0, 9 })
                    foreach (bool playing in new[] { false, true })
                        foreach (bool hooked in new[] { false, true })
                        {
                            SoundSetup(mode, (ushort)old);
                            if (playing && mode != 2) { SoundDrv.PlayMusic(5); }
                            sfx.Hooked = hooked; sfx.Voice0 = 5; sfx.Log.Clear();
                            SoundDrv.SoundSetAutoStop((ushort)nw);
                            var e = new List<string>();
                            if (mode == C_Sound.MODE_DISABLED) { Eq(old, GDVars.soundAutoStop, "disabled: unchanged"); EqS("", string.Join("|", sfx.Log), "disabled: nothing"); continue; }
                            if (nw > 0)
                            {
                                if (old == 0)
                                {
                                    bool mp = false;
                                    if (playing) { e.Add("hooked?"); if (hooked) { e.Add("voice 0"); mp = true; } }
                                    if (!mp) e.Add("uninstall");
                                }
                            }
                            else e.Add("install");
                            EqS(string.Join("|", e), string.Join("|", sfx.Log), "SoundSetAutoStop " + mode + old + nw + playing + hooked);
                            Eq(nw, GDVars.soundAutoStop, "autostop stored");
                        }
        // PlayMusic / StopMusic / MusicPlaying
        foreach (int mode in new[] { 1, 2 })
            foreach (int musicOn in new[] { 0, 1, 2 })
            {
                SoundSetup(mode, 0, (byte)musicOn);
                SoundDrv.PlayMusic(21);
                bool does = mode != 2 && musicOn != 0;
                EqS(does ? "dev 1|install|play 21" : "", string.Join("|", sfx.Log), "PlayMusic " + mode + musicOn);
            }
        foreach (int auto in new[] { 0, 2 })
            foreach (int mode in new[] { 1, 2 })
            {
                SoundSetup(1, (ushort)auto);
                SoundDrv.StopMusic();
                EqS("", string.Join("|", sfx.Log), "StopMusic with no music does nothing");
                SoundDrv.PlayMusic(22); sfx.Log.Clear();
                GDVars.soundMode = B(mode);
                SoundDrv.StopMusic();
                var e = new List<string>();
                if (mode != C_Sound.MODE_DISABLED) { e.Add("play 0"); if (auto > 0) e.Add("uninstall"); }
                EqS(string.Join("|", e), string.Join("|", sfx.Log), "StopMusic " + auto + mode);
                sfx.Log.Clear(); SoundDrv.StopMusic();
                EqS("", string.Join("|", sfx.Log), "a second StopMusic does nothing");
            }
        foreach (bool hooked in new[] { false, true })
            foreach (int voice in new[] { 0, 22, 23 })
            {
                SoundSetup(1); SoundDrv.PlayMusic(22); sfx.Hooked = hooked; sfx.Voice0 = (ushort)voice;
                Yes(SoundDrv.MusicPlaying() == (hooked && voice == 22), "MusicPlaying " + hooked + voice);
            }
        SoundSetup(1); sfx.Hooked = true; sfx.Voice0 = 0; sfx.Log.Clear();
        Yes(!SoundDrv.MusicPlaying(), "no music started"); EqS("", string.Join("|", sfx.Log), "short-circuits before asking the host when no tune was started");
        // the pass-throughs and SoundExit
        SoundSetup(1); sfx.Log.Clear();
        SoundDrv.ProgramSpeakerVoices(3); SoundDrv.ProgramTandyVoices(4); SoundDrv.OplWrite(0xF5, 9); SoundDrv.OplReset(); SoundDrv.SetSoundDevice(2); SoundDrv.SoundExit();
        EqS("spk 3|tandy 4|opl 245=9|oplreset|dev 2|uninstall", string.Join("|", sfx.Log), "pass-throughs");
        // no host: the failure names the missing piece
        SoundSetup(1); SoundDrv.Host = null;
        try { SoundDrv.SoundSuspend(); Yes(false, "unbound host must fail"); }
        catch (NotSupportedException e) { Yes(e.Message.Contains("SOUNDDRV.OBJ"), "message names SOUNDDRV.OBJ"); }
        SoundDrv.Host = null;
    }
}
