using System;
using static Por.Core.Types;

namespace Por.Core
{
    public static class Debug
    {
        public static void PrintHpDebug(string msg, byte maximumHitPoints, byte currentHitPoints, byte drainedHitPoints)
        {
            if (msg != null && msg.Length > 40)
            {
                msg = msg.Substring(0, 40);
            }

            if (GDVars.stingHpFlag != 0)
            {
                TextRend.ClearRect(0, 0, 39, 0);
                TextRend.DisplayString(0, 0, 14, StrUtil.NumStr(maximumHitPoints) + " , " + StrUtil.NumStr(currentHitPoints) + " , " + StrUtil.NumStr(drainedHitPoints));
                TextRend.DisplayAndPause(14, msg);
                TextRend.ClearRect(0, 0, 39, 0);
            }
        }
    }
}
