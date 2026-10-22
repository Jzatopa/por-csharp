// CharDisp checks part 4: icon facings, moving-icon animation, name flash, heal message, SelectChar.
// Oracles are written from chardisp.pas (lines 288-530 and 648-690), using separate int arithmetic with explicit wraps.
using System;
using System.Collections.Generic;
using System.Linq;
using Por.Core;

static partial class Tests
{
    // test video mirror: a fixed byte function, so the copy/offset logic is what is under test
    static void IconMirror(Types.TImgBuf s, Types.TImgBuf d)
    {
        for (int i = 0; i < d.data.Length && i < s.data.Length; i++) d.data[i] = (byte)(s.data[i] ^ 0x5A);
        if (s.transp != null && d.transp != null)
            for (int i = 0; i < d.transp.Data.Length && i < s.transp.Data.Length; i++) d.transp.Data[i] = (byte)(s.transp.Data[i] ^ 0xA5);
    }

    static void IconRig(Random r, out byte[] sheetData, out byte[] sheetMask)
    {
        Types.TImgBuf sheet = null; Blit.AllocPic(24, 3, 4, 1, ref sheet);
        for (int i = 0; i < sheet.data.Length; i++) sheet.data[i] = (byte)r.Next(256);
        for (int i = 0; i < sheet.transp.Data.Length; i++) sheet.transp.Data[i] = (byte)r.Next(256);
        GDVars.dsMoveIconSheet = sheet;
        for (int c = 0; c < 26; c++) for (int h = 0; h < 2; h++)
        {
            Types.TImgBuf p = null; Blit.AllocPic(24, 3, 1, 1, ref p);
            for (int i = 0; i < p.data.Length; i++) p.data[i] = (byte)r.Next(256);
            for (int i = 0; i < p.transp.Data.Length; i++) p.transp.Data[i] = (byte)r.Next(256);
            GDVars.dsIconAnimation[c, h] = p;
        }
        sheetData = (byte[])sheet.data.Clone(); sheetMask = (byte[])sheet.transp.Data.Clone();
    }

    // expected sheet after CopyIconFacing, computed from the Pascal Move calls
    static void OCopyFacing(byte[] data, byte[] mask, int imgSize, int maskBytes, Types.TImgBuf src, int row, bool mirror)
    {
        byte[] sd = src.data, sm = src.transp.Data;
        if (mirror)
        {
            var t = new Types.TImgBuf { data = new byte[src.data.Length], transp = new Types.TBytes { Data = new byte[src.transp.Data.Length] } };
            IconMirror(src, t); sd = t.data; sm = t.transp.Data;
        }
        for (int i = 0; i < imgSize; i++) data[row * imgSize + i] = sd[i];
        for (int i = 0; i < maskBytes; i++) mask[row * maskBytes + i] = sm[i];
    }

    static void CharDispIcons()
    {
        var oldMir = GDVars.video.MirrorFrame; byte oldShift = GDVars.video.maskShift;
        GDVars.video.MirrorFrame = IconMirror; GDVars.video.maskShift = 2;
        try
        {
            for (int n = 0; n < 200; n++)
            {
                var r = new Random(n * 77 + 1);
                GDVars.video.maskShift = (byte)(n % 4);
                byte[] data, mask; IconRig(r, out data, out mask);
                int cell = r.Next(26), half = r.Next(2), row = r.Next(4); bool mir = r.Next(2) == 0;
                int stride = GDVars.dsMoveIconSheet.imgSize, s4 = Blit.MaskBytes((ushort)stride);
                Yes(stride > 0 && s4 > 0, "icon strides positive");
                OCopyFacing(data, mask, stride, s4, GDVars.dsIconAnimation[cell, half], row, mir);
                CharDisp.CopyIconFacing((byte)cell, (byte)half, (byte)row, mir);
                Yes(data.SequenceEqual(GDVars.dsMoveIconSheet.data), "CopyIconFacing data " + cell + "/" + half + "/" + row + "/" + mir);
                Yes(mask.SequenceEqual(GDVars.dsMoveIconSheet.transp.Data), "CopyIconFacing mask " + cell + "/" + half + "/" + row + "/" + mir);
                // the source pictures are never altered
                // whole facing sheet
                var r2 = new Random(n * 31 + 5); IconRig(r2, out data, out mask);
                stride = GDVars.dsMoveIconSheet.imgSize; s4 = Blit.MaskBytes((ushort)stride);
                OCopyFacing(data, mask, stride, s4, GDVars.dsIconAnimation[cell, 0], 0, false);
                OCopyFacing(data, mask, stride, s4, GDVars.dsIconAnimation[cell, 0], 1, true);
                OCopyFacing(data, mask, stride, s4, GDVars.dsIconAnimation[cell, 1], 2, true);
                OCopyFacing(data, mask, stride, s4, GDVars.dsIconAnimation[cell, 1], 3, false);
                CharDisp.BuildIconFacings((byte)cell);
                Yes(data.SequenceEqual(GDVars.dsMoveIconSheet.data), "BuildIconFacings data " + cell);
                Yes(mask.SequenceEqual(GDVars.dsMoveIconSheet.transp.Data), "BuildIconFacings mask " + cell);
            }
        }
        finally { GDVars.video.MirrorFrame = oldMir; GDVars.video.maskShift = oldShift; GDVars.dsMoveIconSheet = null; }
    }

    static int S8(int v) { return unchecked((sbyte)v); }
    static int Sgn(int v) { return v > 0 ? 1 : v < 0 ? -1 : 0; }

    // oracle for CombatAnimateMovingIcon, written from the Pascal text
    static void OAnim(int startX, int startY, int endX, int endY, int frames, int pace)
    {
        var steps = new int[149]; for (int i = 0; i < 149; i++) steps[i] = 8;
        var req = new Types.TPathReq();
        int n = 0, count, frame = 0;
        req.sx = (short)(startX * 3); req.sy = (short)(startY * 3); req.ex = (short)(endX * 3); req.ey = (short)(endY * 3);
        Pathing.PathFind(ref req);
        bool done;
        do { done = !Pathing.NextPathStep(ref req); steps[n] = req.step; n = (n + 1) & 255; if (n >= 148) done = true; } while (!done);
        count = n; Yes(count != 2, "path step count 2 is unreachable (documents the equivalent count<3 mutant)");
        if (count < 2) return;
        int dirX = S8(endX - startX), dirY = S8(endY - startY);
        var ar = GDVars.arenaPtr;
        int camX, camY; bool lastPass;
        if (!cdCbt.ViewCoordsValid((sbyte)S8(startX - ar.ox), (sbyte)S8(startY - ar.oy)) || !cdCbt.ViewCoordsValid((sbyte)S8(endX - ar.ox), (sbyte)S8(endY - ar.oy)))
        {
            if (Math.Abs(dirX) <= 6 && Math.Abs(dirY) <= 6) { lastPass = true; camX = S8(startX + dirX / 2); camY = S8(startY + dirY / 2); }
            else { lastPass = false; camX = S8(ar.ox + 3); camY = S8(ar.oy + 3); }
        }
        else { lastPass = true; camX = S8(ar.ox + 3); camY = S8(ar.oy + 3); }
        cdCbt.ScrollView((sbyte)camX, (sbyte)camY, 255, 8);
        n = 0; int accX = 0, accY = 0, pixX, pixY, prevX, prevY;
        do
        {
            pixX = S8(S8(startX - ar.ox) * 3 + accX); pixY = S8(S8(startY - ar.oy) * 3 + accY);
            prevX = startX; prevY = startY; done = false;
            do
            {
                if (pace > 0 || pixX % 3 == 0 || pixY % 3 == 0)
                {
                    CharDisp.BlitClipped((short)pixX, (short)pixY, (byte)frame, 5, GDVars.dsMoveIconSheet, GDVars.dsViewImageBufferPtr);
                    CharDisp.FlushViewCall();
                    CharDisp.DelayMs((ushort)pace);
                    CharDisp.BlitClipped((short)pixX, (short)pixY, 0, 0, GDVars.dsIconImageBufferPtr, GDVars.dsViewImageBufferPtr);
                    frame = (frame + 1) & 255; if (frame >= frames) frame = 0;
                }
                n = (n + 1) & 255;
                int stepX = GDConsts.dirDX[steps[n]], stepY = GDConsts.dirDY[steps[n]];
                pixX = S8(pixX + stepX); pixY = S8(pixY + stepY);
                if (pixX < 0 || pixX > 18 || pixY < 0 || pixY > 18) done = true;
                if (!done && n < count)
                {
                    accX = S8(accX + stepX); accY = S8(accY + stepY);
                    if (Math.Abs(accX) == 3) { startX = S8(startX + Sgn(accX)); camX = S8(camX + Sgn(accX)); accX = 0; }
                    if (Math.Abs(accY) == 3) { startY = S8(startY + Sgn(accY)); camY = S8(camY + Sgn(accY)); accY = 0; }
                }
            } while (!(n >= count || done));
            if (n < count)
            {
                dirX = Sgn(S8(prevX - startX)); dirY = Sgn(S8(prevY - startY));
                if (dirX != 0) { if (startX >= ar.ox + 6) camX = S8(startX + 3); else if (startX <= ar.ox) camX = S8(startX - 3); }
                if (dirY != 0) { if (startY >= ar.oy + 6) camY = S8(startY + 3); else if (startY <= ar.oy) camY = S8(startY - 3); }
                cdCbt.ScrollView((sbyte)camX, (sbyte)camY, 255, 8);
            }
            else
            {
                lastPass = true;
                if (!cdCbt.ViewCoordsValid((sbyte)S8(endX - ar.ox), (sbyte)S8(endY - ar.oy))) cdCbt.ScrollView((sbyte)endX, (sbyte)endY, 3, 8);
                pixX = S8(S8(endX - ar.ox) * 3); pixY = S8(S8(endY - ar.oy) * 3);
                CharDisp.BlitClipped((short)pixX, (short)pixY, (byte)frame, 5, GDVars.dsMoveIconSheet, GDVars.dsViewImageBufferPtr);
                if (pace > 0)
                {
                    CharDisp.FlushViewCall(); CharDisp.DelayMs((ushort)pace);
                    CharDisp.BlitClipped((short)pixX, (short)pixY, 0, 0, GDVars.dsIconImageBufferPtr, GDVars.dsViewImageBufferPtr);
                }
            }
        } while (!lastPass);
        CharDisp.FlushViewCall();
    }

    static void CharDispAnim()
    {
        try
        {
            for (int n = 0; n < 600; n++)
            {
                int range = n % 3 == 0 ? 3 : n % 3 == 1 ? 9 : 20;
                int ox = rng.Next(-4, 5), oy = rng.Next(-4, 5);
                int sx = ox + rng.Next(-1, 8), sy = oy + rng.Next(-1, 8);
                int ex = sx + rng.Next(-range, range + 1), ey = sy + rng.Next(-range, range + 1);
                if (n % 25 == 0) { sx = rng.Next(-128, 128); sy = rng.Next(-128, 128); ex = sx + rng.Next(-30, 31); ey = sy + rng.Next(-30, 31); }
                if (n % 17 == 0) { ex = sx; ey = sy; }
                int frames = 1 + rng.Next(4), pace = rng.Next(3) == 0 ? 0 : rng.Next(1, 4);
                sbyte a = (sbyte)S8(sx), b = (sbyte)S8(sy), c = (sbyte)S8(ex), d = (sbyte)S8(ey);
                CdSame("anim " + n + " " + a + "," + b + "->" + c + "," + d + " ox=" + ox + " oy=" + oy + " f=" + frames + " p=" + pace,
                    () => { GDVars.arenaPtr = new Types.TArena { ox = (sbyte)ox, oy = (sbyte)oy }; },
                    () => CharDisp.CombatAnimateMovingIcon(a, b, c, d, (byte)frames, (byte)pace),
                    () => OAnim(a, b, c, d, frames, pace));
            }
        }
        finally { CdRestore(); }
    }

    static string SheetHash(byte[] d, byte[] m)
    {
        long a = 0, b = 0; for (int i = 0; i < d.Length; i++) a += d[i] * (i % 251 + 1); for (int i = 0; i < m.Length; i++) b += m[i] * (i % 241 + 1);
        return "sheet:" + d.Length + ":" + a + ":" + m.Length + ":" + b;
    }

    static void CharDispFlash()
    {
        var oldMir = GDVars.video.MirrorFrame; byte oldShift = GDVars.video.maskShift;
        GDVars.video.MirrorFrame = IconMirror; GDVars.video.maskShift = 2;
        try
        {
            for (int n = 0; n < 400; n++)
            {
                bool combat = rng.Next(4) != 0, effect = rng.Next(2) == 0, visible = rng.Next(2) == 0;
                int slot = rng.Next(3) == 0 ? 0 : 1 + rng.Next(60), cx = rng.Next(-20, 20), cy = rng.Next(-20, 20), gdelay = rng.Next(0, 4);
                int gxv = rng.Next(-128, 128), gyv = rng.Next(-128, 128);
                string s = "hit" + rng.Next(1000);
                var cr = MkChar("Cr" + rng.Next(100), 50, 5, 9, 1, rng.Next(2));
                Action setup = () =>
                {
                    GDVars.ifcState = (byte)(combat ? C_Ifc.COMBAT : C_Ifc.INDOORS); GDVars.dsSuppressDrawingBorderFlag = 0; GDVars.gameDelay = (byte)gdelay;
                    cdCbt.Visible = visible; cdCbt.CellX = cx; cdCbt.CellY = cy; cdCbt.Slot = (byte)slot;
                    GDVars.gridX[slot] = (sbyte)gxv; GDVars.gridY[slot] = (sbyte)gyv;
                    byte[] d0, m0; IconRig(new Random(n), out d0, out m0);
                };
                CdSame("flash " + n, setup, () => { CharDisp.PrintNameStringAnim(cr, effect, s); cdLog.Add(SheetHash(GDVars.dsMoveIconSheet.data, GDVars.dsMoveIconSheet.transp.Data)); }, () =>
                {
                    var ed = (byte[])GDVars.dsMoveIconSheet.data.Clone(); var em = (byte[])GDVars.dsMoveIconSheet.transp.Data.Clone();
                    if (!combat) { OPNS(cr, s, 10, true); cdLog.Add(SheetHash(ed, em)); return; }
                    { int c0 = effect ? 22 : 23, st = GDVars.dsMoveIconSheet.imgSize, s4 = Blit.MaskBytes((ushort)st); OCopyFacing(ed, em, st, s4, GDVars.dsIconAnimation[c0, 0], 0, false); OCopyFacing(ed, em, st, s4, GDVars.dsIconAnimation[c0, 0], 1, true); OCopyFacing(ed, em, st, s4, GDVars.dsIconAnimation[c0, 1], 2, true); OCopyFacing(ed, em, st, s4, GDVars.dsIconAnimation[c0, 1], 3, false); }
                    CharDisp.SoundSetAutoStopCall(0);
                    // facings are built from icon cell 22 (effect) or 23 (damage); the seam only logs, so the build itself is covered by CharDispIcons
                    if (!cdCbt.CombatIconVisible(cr, 1)) cdCbt.ScrollView((sbyte)cdCbt.CombatCellX(cr), (sbyte)cdCbt.CombatCellY(cr), 3, 8);
                    CharDisp.SoundCommandCall(effect ? GDConsts.soundIds.combat[0] : GDConsts.soundIds.damage);
                    OPNS(cr, s, 10, false);
                    int reps = effect ? GDVars.gameDelay : 0;
                    int sl = cdCbt.CombatSlot(cr);
                    if (sl > 0)
                    {
                        int gx = S8(GDVars.gridX[sl] * 3), gy = S8(GDVars.gridY[sl] * 3);
                        for (int i = 0; i <= reps; i++) for (int j = 0; j <= 3; j++)
                        {
                            CharDisp.BlitClipped((short)gx, (short)gy, (byte)j, 5, GDVars.dsMoveIconSheet, GDVars.dsViewImageBufferPtr);
                            CharDisp.FlushViewCall(); CharDisp.DelayMs(70);
                            CharDisp.BlitClipped((short)gx, (short)gy, 0, 0, GDVars.dsIconImageBufferPtr, GDVars.dsViewImageBufferPtr);
                        }
                    }
                    CharDisp.SoundSetAutoStopCall(10); CharDisp.FlushViewCall();
                    if (reps == 0) TextRend.PauseOnly();
                    cdLog.Add(SheetHash(ed, em));
                }, MoreKeys());
            }
            // heal message
            GDVars.creatureList = MkChar("Head", 50, 5, 9, 1, 0); GDVars.curChar = GDVars.creatureList;
            for (int n = 0; n < 200; n++)
            {
                bool combat = rng.Next(2) == 0; int hp = rng.Next(0, 4), mx = rng.Next(0, 4);
                var cr = MkChar("Hl" + rng.Next(100), 50, hp, mx, 1, 0);
                CdSame("heal " + n, () => { GDVars.ifcState = (byte)(combat ? C_Ifc.COMBAT : C_Ifc.INDOORS); GDVars.dsSuppressDrawingBorderFlag = 0; },
                    () => CharDisp.PrintHpRestoreMsg(cr), () =>
                    {
                        OPNS(cr, hp == mx ? "is fully healed" : "is partially healed", 10, true);
                        if (!combat) CharDisp.DisplayParty(GDVars.curChar);
                    }, MoreKeys());
            }
        }
        finally { GDVars.video.MirrorFrame = oldMir; GDVars.video.maskShift = oldShift; GDVars.dsMoveIconSheet = null; CdRestore(); GDVars.creatureList = null; GDVars.curChar = null; }
    }

    static void CharDispSelect()
    {
        try
        {
            for (int n = 0; n < 500; n++)
            {
                int size = 1 + rng.Next(5), start = rng.Next(size); bool addExit = rng.Next(2) == 0;
                int ifc = rng.Next(3) == 0 ? C_Ifc.CAMPING : rng.Next(3) == 0 ? C_Ifc.LOOT : C_Ifc.INDOORS;
                var keys = new List<byte>();
                int pre = rng.Next(6);
                byte[][] moves = { new byte[] { 0, C_Keys.END_KEY }, new byte[] { 0, C_Keys.HOME }, new byte[] { 0, C_Keys.LEFT }, new byte[] { 0, C_Keys.RIGHT }, new byte[] { (byte)'X' }, new byte[] { (byte)'.' }, new byte[] { (byte)',' } };
                for (int i = 0; i < pre; i++) { if (!addExit && rng.Next(8) == 0) keys.Add(rng.Next(2) == 0 ? (byte)C_Keys.ESC : (byte)'E'); else keys.AddRange(moves[rng.Next(moves.Length)]); }
                byte[][] ends = addExit ? new[] { new byte[] { C_Keys.ENTER }, new byte[] { C_Keys.ESC }, new byte[] { (byte)'E' }, new byte[] { (byte)'S' } } : new[] { new byte[] { C_Keys.ENTER }, new byte[] { (byte)'S' }, new byte[] { (byte)'s' } };
                keys.AddRange(ends[rng.Next(ends.Length)]);
                string q = "Who" + rng.Next(10); int hc = 1 + rng.Next(3);
                Types.TCharRecord[] party = null; Types.TCharRecord actualSel = null, oracleSel = null;
                Action setup = () =>
                {
                    party = Enumerable.Range(0, size).Select(i => MkChar("P" + i, 40 + i, 3 + i, 9, 1, 0)).ToArray();
                    for (int i = 0; i + 1 < size; i++) party[i].nextCharAddr = party[i + 1];
                    GDVars.creatureList = party[0]; GDVars.curChar = party[0]; GDVars.ifcState = (byte)ifc; GDVars.horizChoice = (byte)hc;
                    GDVars.animData = new Types.TAnimRec { count = 3, current = 1 }; for (int f = 0; f < 9; f++) { GDVars.animData.frames[f].time = 0; GDVars.animData.frames[f].pic = null; }
                };
                string tag = "select " + n + " size=" + size + " start=" + start + " exit=" + addExit + " keys=" + string.Join(",", keys);
                int runs = 0;
                Action act = () => { actualSel = party[start]; CharDisp.SelectChar(q, addExit, ref actualSel); runs++; cdLog.Add("animcur:" + GDVars.animData.current + " hc=" + GDVars.horizChoice); };
                Action orc = () =>
                {
                    var sel = party[start]; byte ch = 32; bool ext = false; GDVars.horizChoice = 1;
                    string ex = addExit ? " Exit" : "";
                    while (!((ch == 13 || ch == 27 || ch == 'E' || ch == 'S') || (ch == 0 && addExit)))
                    {
                        CharDisp.DisplayParty(sel);
                        bool anim = GDVars.ifcState == C_Ifc.CAMPING || GDVars.ifcState == C_Ifc.LOOT;
                        ch = MenuCore.MenuHorizontal(q + " ", "Select" + ex, 13, 10, 15, true, anim, ref ext);
                        var p = sel;
                        if (ext)
                        {
                            if (ch == 79) { p = sel.nextCharAddr != null ? sel.nextCharAddr : party[0]; }
                            else if (ch == 71) { p = ReferenceEquals(sel, party[0]) ? party[size - 1] : party[Array.IndexOf(party, sel) - 1]; }
                        }
                        else if ((ch == 'E' || ch == 0) && addExit) p = null;
                        sel = p;
                    }
                    oracleSel = sel; cdLog.Add("animcur:" + GDVars.animData.current + " hc=" + GDVars.horizChoice);
                };
                try { CdSame(tag, setup, act, orc, keys.ToArray()); } catch (Exception e) { throw new Exception(tag + " :: " + e.Message); }
                // result of the real routine, replayed against a clean run of the oracle
                CdOnce(setup, act, keys.ToArray()); var realSel = actualSel; var realIdx = realSel == null ? -1 : Array.IndexOf(party, realSel);
                CdOnce(setup, orc, keys.ToArray()); var orcIdx = oracleSel == null ? -1 : Array.IndexOf(party, oracleSel);
                Eq(orcIdx, realIdx, tag + ": selection");
            }
        }
        finally { CdRestore(); GDVars.creatureList = null; GDVars.curChar = null; }
    }
}
