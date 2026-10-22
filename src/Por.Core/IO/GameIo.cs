using System;
using System.IO;
using static Por.Core.Types;

namespace Por.Core
{
    public interface IGameIoHost
    {
        bool KeyPressed { get; }
        byte ReadKey();
        void SoundCommand(ushort command);
        void SoundSuspend();
        void Interrupt(byte number, TInterruptRegisters regs);
        void Halt(int code);
        void WriteLine(string message);
        TextWriter OpenDebugLog(string path);
    }

    public static class GameIo
    {
        public static IGameIoHost Host;

        public static Action saveSettings;
        public static Action showMemory;

        private static bool memShown = false;

        public static byte ReadGameKey()
        {
            if (Host == null) throw new NotSupportedException();

            byte key = Host.ReadKey();

            if (key == 19)
            {
                SetSound(GDVars.soundMode == C_Sound.MODE_DISABLED);
                if (saveSettings != null)
                {
                    saveSettings();
                }
            }

            if (GDVars.stingArg)
            {
                if (key == 4)
                {
                    GDVars.stingHpFlag = (byte)(GDVars.stingHpFlag == 0 ? 1 : 0);
                }
                if (key == 3)
                {
                    ExitGame();
                }
                if (key == 12)
                {
                    GDVars.debugTraceEnabled = !GDVars.debugTraceEnabled;
                    if (GDVars.debugTraceEnabled)
                    {
                        try
                        {
                            GDVars.debugFile = Host.OpenDebugLog("debug.txt");
                        }
                        catch (IOException)
                        {
                            GDVars.debugTraceEnabled = false;
                        }
                        catch (UnauthorizedAccessException)
                        {
                            GDVars.debugTraceEnabled = false;
                        }
                    }
                    else
                    {
                        if (GDVars.debugFile != null)
                        {
                            GDVars.debugFile.Close();
                            GDVars.debugFile = null;
                        }
                    }
                }
                if (key == 26 && GDVars.debugTraceEnabled)
                {
                    DumpEffects();
                }
                if (key == 25 && !memShown && showMemory != null)
                {
                    memShown = true;
                    try
                    {
                        showMemory();
                    }
                    finally
                    {
                        memShown = false;
                    }
                }
            }

            if (key != 0)
            {
                while (Host.KeyPressed)
                {
                    key = Host.ReadKey();
                }
            }

            return key;
        }

        public static void SetSound(bool turnOn)
        {
            if (GDVars.sound.mode == C_Sound.MODE_DISABLED)
            {
                return;
            }

            if (Host == null) throw new NotSupportedException();

            if (!turnOn)
            {
                if (GDVars.soundMode != C_Sound.MODE_DISABLED)
                {
                    Host.SoundCommand(GDConsts.soundIds.commands[0]);
                    GDVars.soundMode = C_Sound.MODE_DISABLED;
                }
            }
            else if (GDVars.soundMode == C_Sound.MODE_DISABLED)
            {
                GDVars.soundMode = GDVars.soundSaved;
                Host.SoundSuspend();
            }

            GDVars.soundOn = (byte)(turnOn ? 1 : 0);
        }

        public static void DiscardPendingKeys()
        {
            if (Host == null) throw new NotSupportedException();

            if (Host.KeyPressed)
            {
                byte key = ReadGameKey();
                if (key == 0 && Host.KeyPressed)
                {
                    key = Host.ReadKey();
                }
            }
        }

        public static void ExitGame()
        {
            if (Host == null) throw new NotSupportedException();

            if (GDVars.debugTraceEnabled)
            {
                if (GDVars.debugFile != null)
                {
                    GDVars.debugFile.Close();
                    GDVars.debugFile = null;
                }
            }

            GDVars.soundMode = GDVars.soundSaved;
            Host.SoundCommand(GDConsts.soundIds.commands[0]);

            GDVars.dsInterruptRegisters.ax = GDVars.savedVideoMode;
            Host.Interrupt(0x10, GDVars.dsInterruptRegisters);

            Host.Halt(0);
            throw new InvalidOperationException("Game host Halt unexpectedly returned.");
        }

        public static void FatalError(string msg)
        {
            if (Host == null) throw new NotSupportedException();

            GDVars.dsInterruptRegisters.ax = GDVars.savedVideoMode;
            Host.Interrupt(0x10, GDVars.dsInterruptRegisters);

            Host.WriteLine(msg);

            byte key = ReadGameKey();

            ExitGame();
        }

        private static void DumpEffects()
        {
            if (Host == null) throw new NotSupportedException();

            TCharRecord p = GDVars.creatureList;
            while (p != null)
            {
                TEffectRec e = p.effectsAddr;
                while (e != null)
                {
                    if (GDVars.debugFile != null)
                    {
                        GDVars.debugFile.WriteLine("who: {0}  sp#: {1}", p.name, e.id);
                    }
                    e = e.next;
                }
                p = p.nextCharAddr;
            }
        }

        static GameIo()
        {
            memShown = false;
            saveSettings = null;
            showMemory = null;
        }
    }
}
