// Wilds (wilds.pas at 7616920): the 5x5 wilderness view against a model written from the Pascal text.
using System;
using System.Collections.Generic;
using System.Linq;
using Por.Core;

static partial class Tests
{
    static void WildsView()
    {
        var calls = new List<int[]>();
        var winCalls = new List<string>();
        var oldTile = Wilds.Copy2DTile; var oldWin = Wilds.DrawWindow;
        Wilds.Copy2DTile = (c, r, t, u) => calls.Add(new int[] { c, r, t, u });
        Wilds.DrawWindow = (a, b, c, d, e, f, g) => winCalls.Add(string.Join(",", a, b, c, d, e, f) + ",[" + g + "]");
        try
        {
            var special = GDConsts.wildernessSpecialLocations;
            Func<int, int, int, int[], int> expectedIcon = null;
            // flags: gate, present, campFlags, campGate, east, yarash
            Action<int, int, int[]> check = (ulX, ulY, f) =>
            {
                GDVars.zone0 = new Types.TGbvmZone0State();
                GDVars.zone0.buccaneersBaseGate = W(f[0]); GDVars.zone0.nomadCampPresent = W(f[1]); GDVars.zone0.nomadCampFlags = W(f[2]);
                GDVars.zone0.nomadCampGate = W(f[3]); GDVars.zone0.easternSitesGate = W(f[4]); GDVars.zone0.yarashMissionDone = W(f[5]);
                calls.Clear();
                Wilds.BuildWildernessMap((short)ulX, (short)ulY);
                int cx = Math.Max(0, Math.Min(39, ulX)), cy = Math.Max(0, Math.Min(31, ulY));
                string tag = "ul " + ulX + "," + ulY + " flags " + string.Join("/", f);
                Eq(25, calls.Count, "25 tiles " + tag);
                int n = 0;
                for (int row = 0; row <= 4; row++)
                    for (int col = 0; col <= 4; col++)
                    {
                        int mapX = col + cx, mapY = row + cy;
                        int icon = (GDConsts.wildMap[mapY, mapX] - 1) & 0xFF;
                        for (int k = 1; k <= 9; k++)
                        {
                            if (mapX != special[k].x || mapY != special[k].y) continue;
                            int sp = (special[k].icon - 1) & 0xFF;
                            switch (k)
                            {
                                case 3: if (f[0] != 255) icon = sp; break;
                                case 4: if (f[1] != 0 || (f[2] & 8) != 0 || f[3] == 0) icon = sp; break;
                                case 5: if ((f[4] & 2) == 0) icon = sp; break;
                                case 6: icon = sp; break;
                                case 7: if ((f[4] & 1) == 0) icon = sp; break;
                                case 8: case 9: if (f[5] > 253) icon = sp; break;
                            }
                        }
                        var c = calls[n++];
                        Eq(col * 3, c[0], "col " + tag); Eq(row * 3, c[1], "row " + tag); Eq(icon, c[2], "icon at " + mapX + "," + mapY + " " + tag); Eq(1, c[3], "last argument 1");
                    }
            };
            var rnd = new Random(606);
            // every clamped corner with a few flag sets; corners that show a special location with every flag set
            var flagSets = new List<int[]>();
            foreach (int g in new[] { 0, 254, 255 }) foreach (int p in new[] { 0, 1 }) foreach (int fl in new[] { 0, 8, 7 }) foreach (int cg in new[] { 0, 1 })
                foreach (int e in new[] { 0, 1, 2, 3 }) foreach (int y in new[] { 0, 253, 254, 255 }) flagSets.Add(new[] { g, p, fl, cg, e, y });
            for (int ulY = -2; ulY <= 34; ulY++)
                for (int ulX = -2; ulX <= 42; ulX++)
                {
                    bool nearSpecial = false;
                    for (int k = 1; k <= 9; k++) if (special[k].x >= Math.Max(0, Math.Min(39, ulX)) && special[k].x <= Math.Max(0, Math.Min(39, ulX)) + 4 &&
                                                    special[k].y >= Math.Max(0, Math.Min(31, ulY)) && special[k].y <= Math.Max(0, Math.Min(31, ulY)) + 4) nearSpecial = true;
                    if (nearSpecial) foreach (var f in flagSets) check(ulX, ulY, f);
                    else for (int i = 0; i < 3; i++) check(ulX, ulY, flagSets[rnd.Next(flagSets.Count)]);
                }
            foreach (int ul in new[] { -32768, -1000, 1000, 32767 }) { check(ul, ul, flagSets[5]); check(ul, 0, flagSets[100]); check(0, ul, flagSets[200]); }
            // DrawWildWindows
            Wilds.DrawWildWindows();
            Eq(3, winCalls.Count, "three windows");
            EqS("1,1,15,15,0,0,[]", winCalls[0], "left window"); EqS("17,1,38,15,0,0,[]", winCalls[1], "right window");
            EqS("1,17,38,22,0,0,[]", winCalls[2], "message window");
            // the 9 special locations are the ones the unit's comments list
            int[][] expected = { null, new[] { 10, 9 }, new[] { 3, 32 }, new[] { 12, 31 }, new[] { 25, 11 }, new[] { 32, 15 }, null, new[] { 37, 8 }, new[] { 19, 16 }, new[] { 25, 27 } };
            for (int k = 1; k <= 9; k++) if (expected[k] != null) { Eq(expected[k][0], special[k].x, "special " + k + " x"); Eq(expected[k][1], special[k].y, "special " + k + " y"); }
        }
        finally { Wilds.Copy2DTile = oldTile; Wilds.DrawWindow = oldWin; }
    }
}
