// Intro checks, written from intro.pas. The credits table below was generated from the Pascal text by a script (not from Intro.cs).
// Introduction and DelayOrKey run against a scripted clock/key/random environment and an oracle written from the Pascal control flow;
// every external call (pictures, palette, delays, music, key polls, tick reads) is logged and the logs must match exactly.
using System;
using System.Collections.Generic;
using System.Linq;
using Por.Core;

class IntroEnv : IGameIoHost
{
    public int Freeze = int.MaxValue; public int Cur; public Random R; public int Polls, KeyAfter; public int MaxDelta; public List<string> Log; public int BiosReads;
    int Delta() { if (Polls >= Freeze) return 0; return R.Next(MaxDelta + 1) * (R.Next(40) == 0 ? -1000 : 1); }
    public bool KeyPressed { get { Polls++; Log.Add("key"); Cur = unchecked(Cur + Delta()); return Polls >= KeyAfter; } }
    public byte ReadKey() { throw new NotSupportedException(); }
    public void SoundCommand(ushort c) { }
    public void SoundSuspend() { }
    public void Interrupt(byte n, Types.TInterruptRegisters r) { }
    public void Halt(int c) { throw new Halted(); }
    public void WriteLine(string m) { }
    public System.IO.TextWriter OpenDebugLog(string p) { throw new System.NotSupportedException(); }
    public void Advance() { Cur = unchecked(Cur + Delta()); }
}

static partial class Tests
{
    static readonly object[][] CREDITS = new object[][] {
        new object[] { 5, 1, 10, "scenario created by:" },
        new object[] { 26, 1, 14, "tsr, inc." },
        new object[] { 15, 2, 10, "jim ward" },
        new object[] { 2, 3, 10, "david cook,steve winter,mike breault" },
        new object[] { 1, 5, 10, "game created by:" },
        new object[] { 18, 5, 14, "ssi special projects" },
        new object[] { 0, 6, 11, "@version" },
        new object[] { 2, 7, 14, "programming:" },
        new object[] { 20, 7, 14, "original" },
        new object[] { 2, 8, 11, "scot bayless" },
        new object[] { 20, 8, 14, "programming:" },
        new object[] { 2, 9, 11, "brad myers" },
        new object[] { 20, 9, 11, "keith brors" },
        new object[] { 2, 10, 11, "russ brown" },
        new object[] { 20, 10, 11, "brad myers" },
        new object[] { 2, 11, 11, "ted greer" },
        new object[] { 2, 13, 14, "graphic arts:" },
        new object[] { 20, 13, 14, "encounter coding:" },
        new object[] { 2, 14, 11, "tom wahl" },
        new object[] { 20, 14, 11, "paul murray" },
        new object[] { 2, 15, 11, "fred butts" },
        new object[] { 20, 15, 11, "russ brown" },
        new object[] { 2, 16, 11, "darla marasco" },
        new object[] { 20, 16, 11, "victor penman" },
        new object[] { 2, 17, 11, "susan halbleib" },
        new object[] { 20, 17, 11, "dave shelley" },
        new object[] { 2, 19, 14, "project manager" },
        new object[] { 20, 19, 14, "developer:" },
        new object[] { 2, 20, 11, "victor penman" },
        new object[] { 20, 20, 11, "george mac donald" },
        new object[] { 2, 22, 14, "testing:" },
        new object[] { 11, 22, 11, "joel billings,steve salyer" },
        new object[] { 2, 23, 11, "james kucera,robert daly,rick white" },
    };

    static int introFlash, introEarly, introCredits, introRav;
    static IntroEnv ie;
    static List<string> intLog;

    static void IntroInstall(int seed, int start, int keyAfter, int maxDelta, bool exists, bool palette, bool full, bool bigRandom, int freeze = int.MaxValue)
    {
        intLog = new List<string>();
        ie = new IntroEnv { Cur = start, R = new Random(seed), KeyAfter = keyAfter, MaxDelta = maxDelta, Log = intLog, Freeze = int.MaxValue };
        var rr = new Random(seed + 1);
        GameIo.Host = ie;
        TextRend.ReadBiosTicks = () => { ie.BiosReads++; return ie.Cur; };
        TextRend.DelayMilliseconds = ms => { intLog.Add("delay:" + ms); ie.Advance(); };
        Dice.RandomBelow = n => { int v = bigRandom ? n - 1 : rr.Next(n); intLog.Add("rnd:" + n + ":" + v); return v; };
        Intro.FileExistsCall = name => { intLog.Add("exists:" + name); return exists; };
        Intro.LoadPicCall = (string name, byte id, bool mask, byte tc, ref Types.TImgBuf pic) => { pic = new Types.TImgBuf { width = (ushort)((intLog.Count + 1) & 0xFFFF) }; intLog.Add("load:" + name + ":" + id + ":" + mask + ":" + tc + ":w" + pic.width + ":dir[" + GDVars.gameDir + "]"); };
        Intro.ShowPicCall = (x, y, pic) => intLog.Add("show:" + x + ":" + y + ":" + (pic == null ? "null" : "w" + pic.width) + ":dir[" + GDVars.gameDir + "]");
        Intro.FreePicCall = (ref Types.TImgBuf pic) => intLog.Add("free:" + (pic == null ? "null" : "w" + pic.width));
        Intro.HasPaletteCall = () => { intLog.Add("haspal"); if (freeze != int.MaxValue && ie.Freeze == int.MaxValue) ie.Freeze = ie.Polls; return palette; };
        Intro.SetPaletteRegisterCall = (r, c) => intLog.Add("pal:" + r + ":" + c);
        Intro.PlayMusicCall = t => intLog.Add("music:" + t);
        GDVars.fullIntro = full;
    }
    static void IntroRestore()
    {
        GameIo.Host = null; TextRend.ReadBiosTicks = null; TextRend.DelayMilliseconds = null; Dice.RandomBelow = null;
        Intro.FileExistsCall = DaxIo.FileExists; Intro.LoadPicCall = Blit.LoadPic; Intro.ShowPicCall = Blit.ShowPic; Intro.FreePicCall = Blit.FreePic;
        Intro.HasPaletteCall = Blit.HasPalette; Intro.SetPaletteRegisterCall = Blit.SetPaletteRegister; Intro.PlayMusicCall = SoundDrv.PlayMusic;
    }
    static bool IKey() { return GameIo.Host.KeyPressed; }
    static void IDelayOrKey(int seconds)
    {
        int start = TextRend.GetTicks(), deadline = unchecked(start + 100 * seconds), now;
        do { now = TextRend.GetTicks(); } while (!(now >= deadline || now < start || IKey()));
    }
    static void ICredits()
    {
        Border.DrawWindow(1, 1, 38, 3, 0, 0, ""); Border.DrawWindow(1, 5, 38, 23, 0, 0, "");
        foreach (var e in CREDITS)
        {
            if ((string)e[3] == "@version") TextRend.DisplayCentered(0, 39, 6, 11, GDConsts.GAME_VERSION);
            else TextRend.DisplayString((byte)(int)e[0], (byte)(int)e[1], (byte)(int)e[2], (string)e[3]);
        }
        IDelayOrKey(10);
    }
    static void IIntro(Random random3, bool bigRandom)
    {
        Types.TImgBuf pic = null; int rndAcc = 0; string saved = GDVars.gameDir; GDVars.gameDir = "";
        if (Intro.FileExistsCall(GDVars.gameDir + "RAVFULL.DAX")) { Intro.LoadPicCall("RAVFULL", 1, false, 0, ref pic); Intro.ShowPicCall(0, 0, pic); IDelayOrKey(2); }
        Intro.LoadPicCall("title", 1, false, 0, ref pic); Intro.ShowPicCall(0, 0, pic); Intro.PlayMusicCall(21); IDelayOrKey(5);
        if (!GDVars.fullIntro) { GDVars.gameDir = saved; Intro.FreePicCall(ref pic); return; }
        Intro.LoadPicCall("title", 2, false, 0, ref pic); GDVars.gameDir = saved; Intro.ShowPicCall(0, 0, pic); Intro.FreePicCall(ref pic);
        int deadline = unchecked(TextRend.GetTicks() + 500), now;
        do
        {
            if (Intro.HasPaletteCall())
            {
                rndAcc = unchecked(rndAcc + Dice.RandomBelow(4));
                if (rndAcc > 50000)
                {
                    rndAcc = 0; int bound = Dice.RandomBelow(3) + 1;
                    for (int i = 0; i <= bound; i++)
                    {
                        int[] cols = { 9, 11, 15, 11, 9, 1 }; int[] dl = { 150, 100, 50, 100, 150 };
                        for (int k = 0; k < 6; k++) { Intro.SetPaletteRegisterCall(1, (byte)cols[k]); if (k < 5) TextRend.DelayMilliseconds((ushort)dl[k]); }
                    }
                }
            }
            now = TextRend.GetTicks();
        } while (!(IKey() || now >= deadline || now < unchecked(deadline - 500)));
        ICredits();
    }

    static void IntroDelayOrKey()
    {
        try
        {
            for (int n = 0; n < 3000; n++)
            {
                int secs = rng.Next(4) == 0 ? 0 : rng.Next(1, 12), start = rng.Next(4) == 0 ? rng.Next(1500000) : rng.Next(5000000, 5001000);
                if (rng.Next(10) == 0) start = 1573040 - rng.Next(20);
                int keyAfter = rng.Next(3) == 0 ? 1 + rng.Next(5) : 100000, md = rng.Next(1, 40), seed = rng.Next();
                var logs = new List<string>[2]; var polls = new int[2]; var reads = new int[2]; var cur = new int[2];
                for (int run = 0; run < 2; run++)
                {
                    IntroInstall(seed, start, keyAfter, md, false, false, false, false);
                    if (run == 0) Intro.DelayOrKey((ushort)secs); else IDelayOrKey(secs);
                    logs[run] = intLog; polls[run] = ie.Polls; reads[run] = ie.BiosReads; cur[run] = ie.Cur;
                }
                string tag = "DelayOrKey " + n + " secs=" + secs + " start=" + start + " keyAfter=" + keyAfter;
                EqS(string.Join("|", logs[1]), string.Join("|", logs[0]), tag + ": calls");
                Eq(polls[1], polls[0], tag + ": key polls"); Eq(reads[1], reads[0], tag + ": clock reads"); Eq(cur[1], cur[0], tag + ": final clock");
            }
        }
        finally { IntroRestore(); }
    }

    static void IntroCredits()
    {
        try
        {
            for (int n = 0; n < 20; n++)
            {
                var snaps = new string[2]; var logs = new string[2]; var pics = new string[2];
                for (int run = 0; run < 2; run++)
                {
                    SmSetup(); SmDone();
                    IntroInstall(n, 100000, 100000, 10, false, false, false, false);
                    GDConsts.GAME_VERSION = "ver " + n;
                    if (run == 0) Intro.Credits(); else ICredits();
                    snaps[run] = SnapOf(smscr); logs[run] = string.Join("|", intLog); pics[run] = string.Join("|", smpics);
                }
                EqS(pics[1], pics[0], "Credits border symbols " + n); Yes(pics[0].Length > 200, "credits borders drawn"); EqS(snaps[1], snaps[0], "Credits screen " + n); EqS(logs[1], logs[0], "Credits calls " + n);
                Yes(snaps[0].Length > 800, "credits screen is populated");
            }
        }
        finally { IntroRestore(); }
    }

    static void IntroIntro()
    {
        try
        {
            for (int n = 0; n < 1500; n++)
            {
                bool exists = rng.Next(2) == 0, pal = rng.Next(3) != 0, full = rng.Next(4) != 0, big = rng.Next(2) == 0;
                int start = rng.Next(4) == 0 ? rng.Next(1500000) : rng.Next(5000000, 5001000), seed = rng.Next();
                int mode = rng.Next(3), keyAfter, md, freeze = int.MaxValue;
                if (mode == 0) { keyAfter = 1 + rng.Next(30); md = rng.Next(0, 4); }
                else if (mode == 1) { keyAfter = 30 + rng.Next(100000); md = rng.Next(15, 40); }
                else { keyAfter = 40000 + rng.Next(30000); md = rng.Next(15, 40); freeze = 0; }
                string dir = rng.Next(2) == 0 ? "C:\\POOL\\" : "";
                var pics = new string[2]; var logs = new string[2]; var snaps = new string[2]; var dirs = new string[2]; var cur = new int[2]; var polls = new int[2];
                for (int run = 0; run < 2; run++)
                {
                    SmSetup(); SmDone();
                    IntroInstall(seed, start, keyAfter, md, exists, pal, full, big, freeze);
                    GDVars.gameDir = dir; GDConsts.GAME_VERSION = "v" + n;
                    if (run == 0) Intro.Introduction(); else IIntro(null, big);
                    logs[run] = string.Join("|", intLog); snaps[run] = SnapOf(smscr); pics[run] = string.Join("|", smpics); dirs[run] = GDVars.gameDir; cur[run] = ie.Cur; polls[run] = ie.Polls;
                }
                string tag = "Introduction " + n + " exists=" + exists + " pal=" + pal + " full=" + full + " big=" + big + " keyAfter=" + keyAfter + " md=" + md;
                EqS(logs[1], logs[0], tag + ": calls"); EqS(snaps[1], snaps[0], tag + ": screen"); EqS(pics[1], pics[0], tag + ": border symbols"); EqS(dirs[1], dirs[0], tag + ": gameDir"); Eq(cur[1], cur[0], tag + ": clock"); Eq(polls[1], polls[0], tag + ": polls");
                Yes(dirs[0] == dir, tag + ": gameDir restored");
                if (logs[0].Contains("|pal:1:9|")) introFlash++; if (logs[0].Contains("|key|") && !full) introEarly++; if (snaps[0].Length > 800) introCredits++; if (logs[0].Contains("load:RAVFULL")) introRav++;
            }
        }
        finally { IntroRestore(); GDVars.gameDir = ""; }
        Yes(introFlash >= 20 && introCredits >= 100 && introEarly >= 50 && introRav >= 100, "intro coverage flash=" + introFlash + " credits=" + introCredits + " early=" + introEarly + " rav=" + introRav);
    }
}
