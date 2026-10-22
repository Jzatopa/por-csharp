using System;
using System.Collections.Generic;
using System.Linq;
using Por.Core;

static partial class Tests
{
    static string P2s(int n) { n %= 100; return n < 10 ? "0" + n : n.ToString(); }

    static void CharDispScreens()
    {
        try
        {
            // PrintMiscInfo
            string[] dirs = GDConsts.dirNames;
            for (int n = 0; n < 3000; n++)
            {
                int loc = rng.Next(7), mx = rng.Next(-128, 128), my = rng.Next(-128, 128), px = rng.Next(3) == 0 ? rng.Next(65536) : rng.Next(60), py = rng.Next(3) == 0 ? rng.Next(65536) : rng.Next(40);
                int hour = rng.Next(3) == 0 ? rng.Next(65536) : rng.Next(24), turn = rng.Next(3) == 0 ? rng.Next(65536) : rng.Next(6), minute = rng.Next(3) == 0 ? rng.Next(65536) : rng.Next(10);
                int noAuto = rng.Next(3) == 0 ? 0 : rng.Next(2) == 0 ? 1 : rng.Next(65536), face = rng.Next(8), ifc = rng.Next(2) == 0 ? C_Ifc.CAMPING : rng.Next(9);
                bool dbg = rng.Next(2) == 0; int flags = rng.Next(65536);
                Action setup = () =>
                {
                    GDVars.zone0 = new Types.TGbvmZone0State(); GDVars.zone1 = new Types.TGbvmZone1();
                    GDVars.gLocType = (byte)loc; GDVars.gMapX = (sbyte)mx; GDVars.gMapY = (sbyte)my; GDVars.zone0.partyX.value = (ushort)px; GDVars.zone0.partyY.value = (ushort)py;
                    GDVars.zone0.clock.hour = (ushort)hour; GDVars.zone0.clock.turn = (ushort)turn; GDVars.zone0.clock.minute = (ushort)minute; GDVars.zone0.noAutomap = (ushort)noAuto;
                    GDVars.gFacing = (byte)face; GDVars.ifcState = (byte)ifc; GDVars.debugTraceEnabled = dbg; GDVars.zone1[C_Gbvm.SEARCH_FLAGS] = (ushort)flags;
                };
                CdSame("misc " + n + " loc=" + loc + " noAuto=" + noAuto + " px=" + px + " mx=" + mx + " h=" + hour + " t=" + turn + " m=" + minute, setup, () => CharDisp.PrintMiscInfo(), () =>
                {
                    int x = mx & 255, y = my & 255;
                    if (loc > 1) { x = px & 255; y = py & 255; if (loc == 3) x = (x + 13) & 255; if (loc == 4) x = (x + 26) & 255; }
                    string info = noAuto == 0 ? x + "," + y + " " : "";
                    info += dirs[face] + " " + P2s(hour) + ":" + P2s((10 * turn + minute) & 0xFFFF);
                    if (dbg) info += "*";
                    if (ifc == C_Ifc.CAMPING) info += " camping"; else if ((flags & 1) > 0) info += " search";
                    CB(17, 15, 38, 15); DS(17, 15, 10, info);
                });
            }
            // interfaces
            for (int ifc = 0; ifc < 10; ifc++)
                for (int rep = 0; rep < 6; rep++)
                {
                    int head = rng.Next(256), head2 = rng.Next(256), cur = rng.Next(5); string nm = "P" + rep;
                    var cs = new List<Types.TCharRecord>();
                    Action setup = () =>
                    {
                        cs.Clear(); for (int i = 0; i < 3; i++) cs.Add(MkChar("C" + i, 55 + i, 5 + i, 9, 1, 0));
                        for (int i = 0; i < 3; i++) cs[i].nextCharAddr = i < 2 ? cs[i + 1] : null; GDVars.creatureList = cs[0]; GDVars.curChar = cs[1];
                        GDVars.ifcState = (byte)ifc; GDVars.dsCurrentPortraitHead = (byte)head; GDVars.dsCurrentPortraitHead2 = (byte)head2;
                        GDVars.zone0 = new Types.TGbvmZone0State(); GDVars.zone1 = new Types.TGbvmZone1(); GDVars.animData = new Types.TAnimRec { current = (byte)(rep % 2) };
                        GDVars.animData.frames[1] = new Types.TAnimFrame { pic = rep % 3 == 0 ? null : new Types.TImgBuf() };
                    };
                    bool inset = rep % 2 == 0;
                    CdSame("windows " + inset, setup, () => CharDisp.DrawNoncombatWindows(inset), () => { DW(1, 1, 38, 22, 15); DW(1, 17, 38, 22, 15); DW(1, 1, 15, 15, 15); if (inset) DW(3, 3, 13, 13, 15); });
                    CdSame("noncombat " + ifc + " " + rep, setup, () => CharDisp.DisplayNoncombatInterface(), () =>
                    {
                        Action<bool> wins = i2 => { DW(1, 1, 38, 22, 15); DW(1, 17, 38, 22, 15); DW(1, 1, 15, 15, 15); if (i2) DW(3, 3, 13, 13, 15); };
                        Action party = () => CharDisp.DisplayParty(GDVars.curChar);
                        Action misc = () => CharDisp.PrintMiscInfo();
                        switch (ifc)
                        {
                            case 0: DW(1, 1, 38, 22, 15); break;
                            case 1: case 8: wins(true); cdLog.Add("lpb:" + head + ":" + head2); cdLog.Add("dpb:3:3:True"); break;
                            case 2: wins(true); cdLog.Add("anim:PIC:29:0"); if (GDVars.animData.current > 0) cdLog.Add("dib:3:3:True:" + (GDVars.animData.frames[GDVars.animData.current].pic == null ? "null" : "pic")); party(); misc(); break;
                            case 4: wins(true); cdLog.Add("map"); party(); misc(); break;
                            case 3: wins(false); cdLog.Add("map"); party(); misc(); break;
                            case 6: wins(true); cdLog.Add("anim:PIC:1:0"); party(); break;
                        }
                    });
                }
            // wilderness and combat
            CdSame("wilderness", () => { GDVars.ifcState = 0; GDVars.zone0 = new Types.TGbvmZone0State(); GDVars.zone1 = new Types.TGbvmZone1(); GDVars.creatureList = MkChar("W", 50, 5, 9, 1, 0); GDVars.curChar = GDVars.creatureList; },
                () => CharDisp.DisplayWildernessInterface(), () => { GDVars.ifcState = C_Ifc.OUTDOORS; cdLog.Add("wildwin"); cdLog.Add("map"); cdLog.Add("wildmap"); CharDisp.PrintMiscInfo(); CharDisp.DisplayParty(GDVars.curChar); });
            for (int n = 0; n < 50; n++)
            {
                int ox = rng.Next(-128, 128), oy = rng.Next(-128, 128);
                CdSame("combat interface", () => { GDVars.arenaPtr = new Types.TArena { ox = (sbyte)ox, oy = (sbyte)oy }; },
                    () => CharDisp.DisplayCombatInterface(), () =>
                    {
                        cdLog.Add("palette"); DW(1, 1, 21, 21, 0); DW(23, 1, 38, 21, 0); CB(0, 23, 39, 24);
                        sbyte cx = unchecked((sbyte)(ox + 3)), cy = unchecked((sbyte)(oy + 3));
                        cdLog.Add("scroll:" + cx + ":" + cy + ":255:8");
                    });
            }
            // CbtMap not installed
            CdSetup(); CharDisp.CombatMap = null; bool threw = false;
            try { CharDisp.DisplayCombatInterface(); } catch (NotSupportedException) { threw = true; }
            Yes(threw, "combat interface without CbtMap host throws");
            // misc small routines
            for (int n = 0; n < 40; n++)
            {
                int col = rng.Next(40), row = rng.Next(25); int seed = rng.Next(); string msg = RandTxt(rng.Next(60)).Trim() + "m";
                CdSame("PrintItemLabel", () => { }, () => CharDisp.PrintItemLabel(MkWeapon(seed), (byte)col, (byte)row), () => { var w = MkWeapon(seed); CharHlp.ItemLabel(w, false); DS(col, row, 10, w.name); });
                CdSame("ShowFault", () => { }, () => CharDisp.ShowFault(msg), () => TextRend.DisplayAndPause(14, msg), new byte[] { 65 });
            }
        }
        finally { CdRestore(); GDVars.creatureList = null; GDVars.curChar = null; }
    }

    static void CharDispDelayHost()
    {
        try
        {
            CdRestore();
            bool threw = false; try { CharDisp.DelayMs(5); } catch (NotSupportedException) { threw = true; }
            Yes(threw, "default delay without host");
            var got = new List<int>(); TextRend.DelayMilliseconds = ms => got.Add(ms); CharDisp.DelayMs(70); CharDisp.DelayMs(3);
            Yes(got.SequenceEqual(new[] { 70, 3 }), "default delay forwards"); TextRend.DelayMilliseconds = null;
        }
        finally { CdRestore(); }
    }
}
