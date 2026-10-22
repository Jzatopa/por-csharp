// Source: Talonius pool-of-radiance pinned 7616920a (src/intro.pas); Qwen3.8:27b initial draft, supervisor repaired.
using System;


namespace Por.Core
{
    public static class Intro
    {
        // Supervisor-added test seams: the picture, palette, file and music calls are installable so the introduction's control flow can be
        // checked without game media or a video device. Defaults are the real routines.
        internal delegate void LoadPicProc(string name, byte fileId, bool hasMask, byte transpColor, ref Types.TImgBuf pic);
        internal delegate void FreePicProc(ref Types.TImgBuf pic);
        internal static Func<string, bool> FileExistsCall = DaxIo.FileExists;
        internal static LoadPicProc LoadPicCall = Blit.LoadPic;
        internal static Action<short, short, Types.TImgBuf> ShowPicCall = Blit.ShowPic;
        internal static FreePicProc FreePicCall = Blit.FreePic;
        internal static Func<bool> HasPaletteCall = Blit.HasPalette;
        internal static Action<byte, byte> SetPaletteRegisterCall = Blit.SetPaletteRegister;
        internal static Action<ushort> PlayMusicCall = SoundDrv.PlayMusic;


        private const byte Black = 0;
        private const byte Blue = 1;
        private const byte LightGreen = 10;
        private const byte LightBlue = 9;
        private const byte LightCyan = 11;
        private const byte Yellow = 14;
        private const byte White = 15;

        private static bool KeyPressed()
        {
            if (GameIo.Host == null)
                throw new NotSupportedException();
            return GameIo.Host.KeyPressed;
        }

        private static void Delay(ushort ms)
        {
            if (TextRend.DelayMilliseconds == null)
                throw new NotSupportedException();
            TextRend.DelayMilliseconds(ms);
        }

        private static int Random(int n)
        {
            if (Dice.RandomBelow == null)
                throw new InvalidOperationException();
            return Dice.RandomBelow(n);
        }

        internal static void DelayOrKey(ushort seconds)
        {
            int start = TextRend.GetTicks();
            int deadline = unchecked(start + 100 * seconds);
            int now;
            do
            {
                now = TextRend.GetTicks();
            } while (!(now >= deadline) && !(now < start) && !KeyPressed());
        }

        internal static void Credits()
        {
            Border.DrawWindow(1, 1, 38, 3, 0, Black, "");
            Border.DrawWindow(1, 5, 38, 23, 0, Black, "");
            TextRend.DisplayString(5, 1, LightGreen, "scenario created by:");
            TextRend.DisplayString(26, 1, Yellow, "tsr, inc.");
            TextRend.DisplayString(15, 2, LightGreen, "jim ward");
            TextRend.DisplayString(2, 3, LightGreen, "david cook,steve winter,mike breault");
            TextRend.DisplayString(1, 5, LightGreen, "game created by:");
            TextRend.DisplayString(18, 5, Yellow, "ssi special projects");

            TextRend.DisplayCentered(0, 39, 6, LightCyan, GDConsts.GAME_VERSION);
            TextRend.DisplayString(2, 7, Yellow, "programming:");
            TextRend.DisplayString(20, 7, Yellow, "original");
            TextRend.DisplayString(2, 8, LightCyan, "scot bayless");
            TextRend.DisplayString(20, 8, Yellow, "programming:");
            TextRend.DisplayString(2, 9, LightCyan, "brad myers");
            TextRend.DisplayString(20, 9, LightCyan, "keith brors");
            TextRend.DisplayString(2, 10, LightCyan, "russ brown");
            TextRend.DisplayString(20, 10, LightCyan, "brad myers");
            TextRend.DisplayString(2, 11, LightCyan, "ted greer");
            TextRend.DisplayString(2, 13, Yellow, "graphic arts:");
            TextRend.DisplayString(20, 13, Yellow, "encounter coding:");
            TextRend.DisplayString(2, 14, LightCyan, "tom wahl");
            TextRend.DisplayString(20, 14, LightCyan, "paul murray");
            TextRend.DisplayString(2, 15, LightCyan, "fred butts");
            TextRend.DisplayString(20, 15, LightCyan, "russ brown");
            TextRend.DisplayString(2, 16, LightCyan, "darla marasco");
            TextRend.DisplayString(20, 16, LightCyan, "victor penman");
            TextRend.DisplayString(2, 17, LightCyan, "susan halbleib");
            TextRend.DisplayString(20, 17, LightCyan, "dave shelley");
            TextRend.DisplayString(2, 19, Yellow, "project manager");
            TextRend.DisplayString(20, 19, Yellow, "developer:");
            TextRend.DisplayString(2, 20, LightCyan, "victor penman");
            TextRend.DisplayString(20, 20, LightCyan, "george mac donald");
            TextRend.DisplayString(2, 22, Yellow, "testing:");
            TextRend.DisplayString(11, 22, LightCyan, "joel billings,steve salyer");
            TextRend.DisplayString(2, 23, LightCyan, "james kucera,robert daly,rick white");
            DelayOrKey(10);
        }

        public static void Introduction()
        {
            Types.TImgBuf pic = null;
            int rndAcc = 0;
            string savedDir = GDVars.gameDir;
            GDVars.gameDir = "";

            if (FileExistsCall(GDVars.gameDir + "RAVFULL.DAX"))
            {
                LoadPicCall("RAVFULL", 1, false, 0, ref pic);
                ShowPicCall(0, 0, pic);
                DelayOrKey(2);
            }

            LoadPicCall("title", 1, false, 0, ref pic);
            ShowPicCall(0, 0, pic);
            PlayMusicCall((ushort)C_Sound.MUSIC_TITLE);
            DelayOrKey(5);

            if (!GDVars.fullIntro)
            {
                GDVars.gameDir = savedDir;
                FreePicCall(ref pic);
                return;
            }

            LoadPicCall("title", 2, false, 0, ref pic);
            GDVars.gameDir = savedDir;
            ShowPicCall(0, 0, pic);
            FreePicCall(ref pic);

            int deadline = unchecked(TextRend.GetTicks() + 500);
            int now;
            do
            {
                if (HasPaletteCall())
                {
                    rndAcc = unchecked(rndAcc + Random(4));
                    if (rndAcc > 50000)
                    {
                        rndAcc = 0;
                        int bound = Random(3) + 1;
                        for (int i = 0; i <= bound; i++)
                        {
                            SetPaletteRegisterCall(1, LightBlue);
                            Delay(150);
                            SetPaletteRegisterCall(1, LightCyan);
                            Delay(100);
                            SetPaletteRegisterCall(1, White);
                            Delay(50);
                            SetPaletteRegisterCall(1, LightCyan);
                            Delay(100);
                            SetPaletteRegisterCall(1, LightBlue);
                            Delay(150);
                            SetPaletteRegisterCall(1, Blue);
                        }
                    }
                }
                now = TextRend.GetTicks();
            } while (!KeyPressed() && !(now >= deadline) && !(now < unchecked(deadline - 500)));

            Credits();
        }
    }
}
