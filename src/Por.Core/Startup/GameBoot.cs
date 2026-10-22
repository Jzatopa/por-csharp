// Direct conversion of pinned src/gameboot.pas. Initialization is explicit, never a static constructor.
using System;
using System.IO;
namespace Por.Core
{
    // DOS overlay/heap operations are platform contracts, not simulated successful managed operations.
    public interface IGameBootHost
    {
        void InitializeOverlays(string file);
        void InitializeEms();
        short OverlayResult { get; }
        Func<ushort,short> OverlayReader { get; set; }
        int OverlayBufferBytes { get; }
        int AvailableBytes { get; }
        void SetOverlayBuffer(int bytes);
        void SetOverlayRetry(int bytes);
        void ReserveGraphicsParagraphs(ushort paragraphs);
        byte ProbeConfig(string file);
        TextReader OpenConfig(string file);
        string GetDirectory(byte drive);
        void SetBreakChecking(bool enabled);
        void WriteLine(string text);
        void Halt(int code);
    }
    public static class GameBoot
    {
        public static IGameBootHost Host;
        // SndSb is not converted. It must provide its detection and fallback logic, not silently use another driver.
        public static Action InstallSoundBlaster = SndSb.Install;
        internal static Action OverlayPrompt = DaxIo.OverlayPrompt;
        internal static Action<char> InstallVideo = SelectVideo;
        internal static Action<char> InstallSound = SelectSound;
        internal static Action<byte> SetVideoMode = Blit.SetVideoMode;
        internal static Action RestoreVideoMode = Blit.RestoreVideoMode;
        internal static Action StartVideo = () => GDVars.video.Start();
        internal static Action SuspendSound = SoundDrv.SoundSuspend;
        internal static Action<Types.TInterruptRegisters> ReadVideoMode = regs =>
        {
            if (Blit.Interrupt == null) throw new NotSupportedException("GameBoot: video BIOS adapter not installed.");
            Blit.Interrupt(0x10,regs);
        };
        private static IGameBootHost H { get { if (Host == null) throw new NotSupportedException("GameBoot: startup host not installed."); return Host; } }
        private static void Halt()
        {
            H.Halt(0); throw new InvalidOperationException("GameBoot host Halt unexpectedly returned.");
        }
        internal static void SelectVideo(char c)
        {
            switch (c) { case 'E': VidEga.Install(); break; case 'T': VidTdy.Install(); break; case 'V': VidVga.Install(); break; default: VidCga.Install(); break; }
        }
        internal static void SelectSound(char c)
        {
            switch (c)
            {
                case 'T': SndTdy.Install(); break; case 'P': SndSpk.Install(); break; case 'A': SndOpl.Install(); break;
                case 'S': if (InstallSoundBlaster == null) throw new NotSupportedException("GameBoot: SndSb.Install not bound."); InstallSoundBlaster(); break;
                default: SndNone.Install(); break;
            }
        }
        public static string NormalizePath(string path)
        {
            // Original String parameter is at most 255 bytes. Adapter returns DOS-shaped drive paths.
            if (path.Length > 255) path = path.Substring(0,255);
            path = path.Trim(' ');
            if (path.Length == 2 && path[1] == ':') path = H.GetDirectory(unchecked((byte)(char.ToUpperInvariant(path[0])-'A'+1)));
            else
            {
                string cur = H.GetDirectory(0);
                if (!cur.EndsWith("\\",StringComparison.Ordinal)) cur += "\\";
                if (path.Length < 2 || path[1] != ':')
                    path = path.StartsWith("\\",StringComparison.Ordinal) ? cur.Substring(0,2)+path : cur+path;
            }
            if (!path.EndsWith("\\",StringComparison.Ordinal)) path += "\\";
            if (path.Length > 67)
            {
                RestoreVideoMode(); H.WriteLine("POOL.CFG: this path is too long for DOS:"); H.WriteLine(path); Halt();
            }
            return path;
        }
        private static char ReadChar(TextReader cfg) { string line = cfg.ReadLine(); if (line == null) throw new EndOfStreamException("POOL.CFG is incomplete."); return line.Length == 0 ? '\0' : line[0]; }
        private static string ReadPath(TextReader cfg) { string line = cfg.ReadLine(); if (line == null) throw new EndOfStreamException("POOL.CFG is incomplete."); if (line.Length > 79) line = line.Substring(0,79); return NormalizePath(line); }
        public static void Initialize(string firstArgument)
        {
            IGameBootHost h = H;
            GDVars.stingArg = firstArgument == "STING";
            OverlayPrompt(); h.InitializeOverlays("GAME.OVR"); h.InitializeEms();
            GDVars.ovrInEms = h.OverlayResult == 0; GDVars.ovrLoads = 0; GDVars.picBytes = 0; GDVars.picPeak = 0;
            Func<ushort,short> previous = h.OverlayReader;
            if (previous == null) throw new NotSupportedException("GameBoot: overlay reader not supplied.");
            h.OverlayReader = seg => { GDVars.ovrLoads = unchecked(GDVars.ovrLoads+1); return previous(seg); };
            while (h.ProbeConfig("pool.cfg") != 0) { h.WriteLine("Please insert startup disk."); GameIo.ReadGameKey(); }
            h.SetBreakChecking(false);
            TextReader cfg;
            try { cfg = h.OpenConfig("POOL.CFG"); }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is System.Security.SecurityException) { h.WriteLine("Unable to find POOL.CFG"); Halt(); throw; }
            if (cfg == null) { h.WriteLine("Unable to find POOL.CFG"); Halt(); }
            using (cfg)
            {
                char c = ReadChar(cfg);
                GDVars.dsInterruptRegisters.ax = (ushort)((GDVars.dsInterruptRegisters.ax & 255) | 0x0F00); ReadVideoMode(GDVars.dsInterruptRegisters);
                GDVars.savedVideoMode = unchecked((byte)GDVars.dsInterruptRegisters.ax); GDVars.curVideoMode = GDVars.savedVideoMode;
                InstallVideo(c);
                if (GDVars.video.ramVideoParas > 0) h.ReserveGraphicsParagraphs(GDVars.video.ramVideoParas);
                SetVideoMode(GDVars.video.biosMode); StartVideo();
                int needed = 100352-h.OverlayBufferBytes;
                if (needed <= h.AvailableBytes) { h.SetOverlayBuffer(100352); h.SetOverlayRetry(h.OverlayBufferBytes/3); }
                else { RestoreVideoMode(); h.WriteLine("Not enough memory to run game.  You need " + needed + " more bytes."); Halt(); }
                if (h.OverlayResult != 0) { RestoreVideoMode(); h.WriteLine("Overlay error, program abort!"); Halt(); }
                InstallSound(ReadChar(cfg)); GDVars.soundMode = GDVars.sound.mode;
                h.ReserveGraphicsParagraphs(1024); SuspendSound();
                GDVars.gameDir = ReadPath(cfg); GDVars.dsSaveDirectory = ReadPath(cfg);
                GDVars.fullIntro = ReadChar(cfg) == 'F';
            }
        }
    }
}
