// MapView checks, written from mapview.pas: the callees are replaced through MapView's seams and every call is logged.
using System;
using System.Collections.Generic;
using System.Linq;
using Por.Core;

static partial class Tests
{
    static List<string> mvLog;
    static byte mvEvent;

    static void MvSetup()
    {
        mvLog = new List<string>();
        GDVars.zone0 = new Types.TGbvmZone0State();
        MapView.BuildWildernessMap = (x, y) => mvLog.Add("build:" + x + ":" + y);
        MapView.GetMapEvent = (x, y) => { mvLog.Add("event:" + x + ":" + y); return mvEvent; };
        MapView.SetView3DColors = (a, b, c, d) => mvLog.Add("colors:" + a + ":" + b + ":" + c + ":" + d);
        MapView.Display3DMapView = (x, y, f) => mvLog.Add("3d:" + x + ":" + y + ":" + f);
        MapView.DrawIconAnimSprite = (c, r, f, fr, ix) => mvLog.Add("icon:" + c + ":" + r + ":" + f + ":" + fr + ":" + ix);
        MapView.FlushView = () => mvLog.Add("flush");
    }
    static void MvRestore()
    {
        MapView.BuildWildernessMap = Wilds.BuildWildernessMap; MapView.GetMapEvent = View3D.GetMapEvent;
        MapView.SetView3DColors = View3D.SetView3DColors; MapView.Display3DMapView = View3D.Display3DMapView;
        MapView.DrawIconAnimSprite = MapTiles.DrawIconAnimSprite; MapView.FlushView = Blit.FlushView;
    }
    static readonly int[] MvColOfs = { 0, 0, 0, 13, 26 };
    static readonly int[] MvSky = { 0, 15, 12, 11, 13, 10, 9, 14, 0, 15, 12, 11, 13, 10, 9, 14 };

    // the icon call the Pascal makes, as a string
    static string MvIcon(int loc, int px, int py, int facing, int anim)
    {
        int wxr = unchecked((sbyte)(MvColOfs[loc] + px)); int wx = wxr;
        if (wx < 2) { } else if (wx < 42) wx = 2; else if (wx < 43) wx = 3; else wx = 4;
        int wy = py < 2 ? (py & 255) : py < 34 ? 2 : py < 35 ? 3 : 4;
        return "icon:" + wx + ":" + wy + ":" + facing + ":" + (anim % 2) + ":" + (byte)(anim / 2 + 9);
    }

    static void MapViewWild()
    {
        MvSetup();
        try
        {
            var xs = Enumerable.Range(0, 60).Concat(new[] { 100, 127, 128, 129, 170, 200, 255, 256, 300, 1000, 32767, 32768, 65535 }).ToArray();
            foreach (int loc in Enumerable.Range(0, 256))
                for (int n = 0; n < 40; n++)
                {
                    int px = loc >= 2 && loc <= 4 && n < xs.Length ? xs[n] : rng.Next(3) == 0 ? rng.Next(65536) : rng.Next(60);
                    int py = n % 5 == 0 ? rng.Next(65536) : rng.Next(50);
                    int facing = rng.Next(256), anim = rng.Next(256);
                    MvSetup();
                    GDVars.gLocType = (byte)loc; GDVars.zone0.partyX.value = (ushort)px; GDVars.zone0.partyY.value = (ushort)py;
                    GDVars.gFacing = (byte)facing; GDVars.dsWildAnimFrame = (byte)anim;
                    MapView.DisplayWildMapView();
                    if (loc < 2 || loc > 4)
                    {
                        Eq(0, mvLog.Count, "no drawing outside the wilderness"); Eq(anim, GDVars.dsWildAnimFrame, "frame untouched");
                    }
                    else
                    {
                        EqS(MvIcon(loc, px, py, facing, anim) + "|flush", string.Join("|", mvLog), "icon loc " + loc + " px " + px + " py " + py);
                        Eq((anim + 1) % 6, GDVars.dsWildAnimFrame, "frame advances mod 6");
                    }
                }
            // anim frame cycle
            MvSetup(); GDVars.gLocType = 3; GDVars.dsWildAnimFrame = 0;
            var seen = new List<int>(); for (int i = 0; i < 14; i++) { MapView.DisplayWildMapView(); seen.Add(GDVars.dsWildAnimFrame); }
            Yes(seen.SequenceEqual(new[] { 1, 2, 3, 4, 5, 0, 1, 2, 3, 4, 5, 0, 1, 2 }), "cycle");
        }
        finally { MvRestore(); }
    }

    static void MapViewMain()
    {
        MvSetup();
        try
        {
            // 3D view: every selector, both sky choices
            foreach (int ev in Enumerable.Range(0, 52).Select(i => i * 5).Concat(new[] { 126, 127, 128, 129, 255 }))
                foreach (bool pic in new[] { false, true })
                    foreach (int sel in Enumerable.Range(0, 256))
                    {
                        MvSetup(); mvEvent = (byte)ev;
                        GDVars.gLocType = (byte)(rng.Next(2) == 0 ? 1 : rng.Next(256));
                        GDVars.zone0.view3dFlag = (ushort)(GDVars.gLocType == 1 && rng.Next(2) == 0 ? 0 : 1 + rng.Next(65535));
                        if (GDVars.gLocType != 1 && GDVars.zone0.view3dFlag == 0) GDVars.zone0.view3dFlag = 1;
                        GDVars.zone0.skyCeilingColor = (ushort)rng.Next(65536); GDVars.zone0.alternateSkyColor = (ushort)rng.Next(65536);
                        GDVars.picRegionW = pic; GDVars.ceilFloorColorSel = (byte)sel; GDVars.wallColorSel = (byte)(255 - sel);
                        GDVars.dsWallColorA = 41; GDVars.dsWallColorB = 42; GDVars.dsWallColorC = 43;
                        GDVars.gMapX = (sbyte)rng.Next(-128, 128); GDVars.gMapY = (sbyte)rng.Next(-128, 128); GDVars.gFacing = (byte)rng.Next(4);
                        GDVars.automapOn = true; GDVars.zone0.noAutomap = (ushort)(sel % 3 == 0 ? 0 : 1);
                        MapView.DisplayMapView();
                        int sky = MvSky[(ev > 0x7F ? GDVars.zone0.alternateSkyColor : GDVars.zone0.skyCeilingColor) & 15];
                        int a = 41, b = 42, c = 43;
                        if (pic)
                        {
                            a = sel == 8 || sel == 65 ? 0 : 15;
                            b = sel == 8 ? 8 : 7;
                            int w = 255 - sel; c = w == 11 ? 8 : w == 6 ? 1 : 6;
                        }
                        string exp = "event:" + GDVars.gMapX + ":" + GDVars.gMapY + "|colors:" + sky + ":" + a + ":" + b + ":" + c + "|3d:" + GDVars.gMapX + ":" + GDVars.gMapY + ":" + GDVars.gFacing;
                        EqS(exp, string.Join("|", mvLog), "3d view ev " + ev + " pic " + pic + " sel " + sel);
                        Eq(ev, GDVars.eventAtCell, "event stored"); Eq(sky, GDVars.dsColorSkyCeiling, "sky stored");
                        Eq(a, GDVars.dsWallColorA, "A"); Eq(b, GDVars.dsWallColorB, "B"); Eq(c, GDVars.dsWallColorC, "C");
                        Eq(GDVars.zone0.noAutomap != 0 ? 0 : 1, GDVars.automapOn ? 1 : 0, "automap");
                    }
            // wilderness
            foreach (int loc in Enumerable.Range(0, 8))
                for (int n = 0; n < 400; n++)
                {
                    MvSetup();
                    int px = n < 120 ? n : rng.Next(65536), py = n < 120 ? (n * 7) % 100 : rng.Next(65536);
                    GDVars.gLocType = (byte)loc; GDVars.zone0.view3dFlag = 0; GDVars.zone0.partyX.value = (ushort)px; GDVars.zone0.partyY.value = (ushort)py;
                    GDVars.gFacing = (byte)rng.Next(256); GDVars.dsWildAnimFrame = (byte)rng.Next(6);
                    int anim = GDVars.dsWildAnimFrame, facing = GDVars.gFacing;
                    MapView.DisplayMapView();
                    if (loc >= 2 && loc <= 4)
                    {
                        int wx = unchecked((sbyte)(MvColOfs[loc] + px - 2)); if (wx < 0) wx = 0; if (wx > 39) wx = 39;
                        int wy = unchecked((sbyte)(py - 2)); if (wy < 0) wy = 0; if (wy > 31) wy = 31;
                        EqS("build:" + wx + ":" + wy + "|" + MvIcon(loc, px, py, facing, anim) + "|flush", string.Join("|", mvLog), "wild loc " + loc + " " + px + "," + py);
                    }
                    else Eq(loc == 1 ? 3 : 0, mvLog.Count, "calls for location " + loc);
                }
            // the 3D flag overrides a wilderness location
            MvSetup(); GDVars.gLocType = 3; GDVars.zone0.view3dFlag = 1; MapView.DisplayMapView();
            Yes(mvLog.Count == 3 && mvLog[0].StartsWith("event") && mvLog[2].StartsWith("3d"), "flag forces 3D");
        }
        finally { MvRestore(); }
    }
}
