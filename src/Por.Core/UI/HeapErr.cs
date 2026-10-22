// Direct conversion of pinned src/heaperr.pas; managed allocation failures require explicit host routing.
using System;
namespace Por.Core
{
    public static class HeapErr
    {
        public const int RESERVE_BYTES = 4096;
        private static byte[] reserve;
        private static bool handling;
        public static Action SaveGame;
        // .NET has no TP global HeapError hook. The application must call this handler at its allocation boundary.
        public static void KeepReserve() { if (reserve == null) reserve = new byte[RESERVE_BYTES]; }
        public static short HeapErrorHandler(ushort size)
        {
            if (size == 0 || handling) return 0;
            handling = true; reserve = null;
            bool done;
            do
            {
                TextRend.ClearBottomLine();
                TextRend.DisplayString(0, 24, 14, "OUT OF MEMORY: ENTER SAVES, ESC QUITS");
                GameIo.DiscardPendingKeys();
                byte ch;
                do { ch = GameIo.ReadGameKey(); if (ch == C_Keys.NONE) ch = GameIo.ReadGameKey(); }
                while (ch != C_Keys.ENTER && ch != C_Keys.ESC);
                if (ch == C_Keys.ESC) done = true;
                else
                {
                    GDVars.recentSaved = 0;
                    if (SaveGame == null) throw new NotSupportedException("HeapErr requires SaveLoad.SaveGame (HeapErr.SaveGame).");
                    SaveGame(); done = GDVars.recentSaved != 0;
                }
            } while (!done);
            GameIo.ExitGame();
            throw new InvalidOperationException("ExitGame returned from the fatal allocation handler.");
        }
        internal static void ResetForTests() { reserve = null; handling = false; SaveGame = null; }
        internal static bool HasReserve { get { return reserve != null; } }
    }
}
