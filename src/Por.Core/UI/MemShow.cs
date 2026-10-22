using System;
using static Por.Core.Types;

namespace Por.Core
{
    public static class MemShow
    {
        public static Func<int> GetOverlayBuffer;
        public static Func<int> GetOverlayRetry;
        public static Func<int> GetFreeMemory;
        public static Func<int> GetLargestBlock;
        // Set by a platform with no overlay manager or TP heap (PlatformGapBindings): show this instead of the measurements.
        public static string UnavailableMessage;

        private static int Measure(Func<int> callback)
        {
            if (callback == null) throw new NotSupportedException("Memory display measurement adapter is not installed.");
            return callback();
        }

        public static void ShowMemory()
        {
            if (UnavailableMessage != null && (GetOverlayBuffer == null || GetOverlayRetry == null || GetFreeMemory == null || GetLargestBlock == null))
            {
                TextRend.DisplayAndPause((byte)10, UnavailableMessage);
                TextRend.ClearBottomLine();
                GDVars.bottomLineTaken = true;
                return;
            }
            string source = GDVars.ovrInEms ? "EMS" : "disk";

            TextRend.DisplayAndPause((byte)10, "Overlay loads " + StrUtil.NumStr(GDVars.ovrLoads));
            TextRend.DisplayAndPause((byte)10, "Ovr buffer " + StrUtil.NumStr(Measure(GetOverlayBuffer)) + " retry " + StrUtil.NumStr(Measure(GetOverlayRetry)) + " from " + source);
            TextRend.DisplayAndPause((byte)10, "Heap free " + StrUtil.NumStr(Measure(GetFreeMemory)) + " big " + StrUtil.NumStr(Measure(GetLargestBlock)));
            TextRend.DisplayAndPause((byte)10, "Pictures " + StrUtil.NumStr(GDVars.picBytes) + " peak " + StrUtil.NumStr(GDVars.picPeak));

            TextRend.ClearBottomLine();
            GDVars.bottomLineTaken = true;
        }
    }
}
