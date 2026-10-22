using System;
using System.Collections.Generic;
using System.Linq;
using Por.Core;

static partial class Tests
{
    static void DMW(int l, int t, int r, int b, int color, bool clear, string s) { TextRend.DisplayMessageWindow(B(l), B(t), B(r), B(b), B(color), clear, s); }
    static void DW(int l, int t, int r, int b, int color) { Border.DrawWindow(SB(l), SB(t), SB(r), SB(b), 0, B(color), ""); }
    static void ODisplayCreatureName(Types.TCharRecord c, int col, int row) { DS(col, row, NameColor(c), c.name); }
    static int msgTopOracle; static byte[] MoreKeys() { return Enumerable.Repeat((byte)65, 300).ToArray(); }
    static int OOpen(bool suppress)
    {
        int top = suppress ? 18 : 17;
        if (!suppress) Border.DrawBorder(1, SB(top), 38, 22, 0);
        CB(1, top, 38, 22);
        return top;
    }
    static void OPNS(Types.TCharRecord c, string s, int row, bool pause)
    {
        if (GDVars.ifcState == C_Ifc.COMBAT)
        {
            CB(23, row, 38, 21); ODisplayCreatureName(c, 23, row); DMW(23, row + 1, 38, 21, 10, true, s);
        }
        else
        {
            int top = OOpen(GDVars.dsSuppressDrawingBorderFlag != 0); ODisplayCreatureName(c, 1, top + 1); DMW(1, top + 2, 38, 22, 10, true, s);
        }
        if (pause) { TextRend.PauseOnly(); OClear(); }
    }
    static void OClear() { if (GDVars.ifcState == C_Ifc.COMBAT) CB(23, 10, 38, 21); else CB(1, 18, 38, 22); }
    static string Wrapy(int n) { var w = new List<string>(); for (int i = 0; i < n; i++) w.Add(RandTxt(1 + rng.Next(9)).Trim() + "x"); return string.Join(" ", w); }

    static void CharDispParty()
    {
        try
        {
            var acs = new[] { 0, 1, 49, 50, 51, 59, 60, 61, 68, 69, 70, 100, 255 };
            var hps = new[] { 0, 1, 9, 10, 11, 98, 99, 100, 255 };
            for (int n = 0; n < 400; n++)
            {
                int cnt = rng.Next(9); bool main = rng.Next(3) == 0;
                var seeds = Enumerable.Range(0, cnt).Select(i => new int[] { acs[rng.Next(acs.Length)], hps[rng.Next(hps.Length)], rng.Next(2) == 0 ? 255 : hps[rng.Next(hps.Length)], rng.Next(3) == 0 ? 0 : 1, rng.Next(3) - 1, rng.Next(2) }).ToArray();
                var nms = Enumerable.Range(0, cnt).Select(i => "N" + i + RandTxt(3).Replace(" ", "Q")).ToArray(); int hlIdx = rng.Next(cnt + 1) - 1; var list = new List<Types.TCharRecord>();
                Action setup = () =>
                {
                    list.Clear();
                    for (int i = 0; i < cnt; i++) { var c = MkChar(nms[i], seeds[i][0], seeds[i][1], seeds[i][2], seeds[i][3], seeds[i][4]); if (seeds[i][5] == 1) { c.classLevels[C_Level.CLERIC] = 1; c.experience = 2000000000; } list.Add(c); }
                    for (int i = 0; i < cnt; i++) list[i].nextCharAddr = i + 1 < cnt ? list[i + 1] : null;
                    GDVars.creatureList = cnt > 0 ? list[0] : null; GDVars.ifcState = (byte)(main ? C_Ifc.MAIN_MENU : C_Ifc.INDOORS);
                };
                CdSame("party " + n, setup, () => CharDisp.DisplayParty(hlIdx >= 0 && hlIdx < list.Count ? list[hlIdx] : null), () =>
                {
                    int col = main ? 1 : 17, row = 2; DS(col, row, 15, "Name"); DS(33, row, 15, "AC  HP"); row += 2;
                    for (int i = 0; i < list.Count; i++)
                    {
                        var p = list[i]; CB(col, row, 38, row);
                        if (i == hlIdx) DS(col, row, 15, p.name); else ODisplayCreatureName(p, col, row);
                        int ac = p.acCur, band = ac <= 50 ? 1 : ac <= 60 ? 2 : ac <= 69 ? 1 : 0;
                        DS(band + 32, row, 10, (ac > 60 ? "-" : "") + Math.Abs(ac - 60));
                        int hp = p.hpCur; band = hp <= 9 ? 2 : hp <= 99 ? 1 : 0;
                        DS(band + 36, row, hp < p.hpMax ? 14 : 10, hp.ToString());
                        row++;
                    }
                    CB(col, row, 38, row);
                });
            }
        }
        finally { CdRestore(); GDVars.creatureList = null; }
    }

    static void CharDispMessages()
    {
        try
        {
            for (int n = 0; n < 600; n++)
            {
                bool combat = rng.Next(2) == 0, suppress = rng.Next(2) == 0, pause = rng.Next(2) == 0;
                string s = rng.Next(4) == 0 ? "" : Wrapy(rng.Next(60)); int row = rng.Next(5) == 0 ? rng.Next(256) : 1 + rng.Next(20), color = 1 + rng.Next(15);
                var cr = MkChar("HERO", 50, 5, 9, 1, 0);
                Action setup = () => { GDVars.ifcState = (byte)(combat ? C_Ifc.COMBAT : C_Ifc.INDOORS); GDVars.dsSuppressDrawingBorderFlag = (byte)(suppress ? 7 : 0); };
                CdSame("PrintNameString " + n, setup, () => CharDisp.PrintNameString(cr, s, B(row), pause), () => OPNS(cr, s, row, pause), MoreKeys());
                CdSame("ClearMessageWindow " + n, setup, () => CharDisp.ClearMessageWindow(), () => OClear());
                CdSame("PrintBottomLine " + n, setup, () => CharDisp.PrintBottomLine(s.Length > 40 ? s.Substring(0, 40) : s), () => { CB(0, 24, 39, 24); DS(0, 24, 10, s.Length > 40 ? s.Substring(0, 40) : s); TextRend.PauseOnly(); CB(0, 24, 39, 24); });
                CdSame("MessageWait " + n, setup, () => CharDisp.MessageWait(), () => { TextRend.DisplayAndPause(13, "Press any key to continue"); CB(0, 24, 39, 24); }, new byte[] { 65 });
                string s2 = Wrapy(rng.Next(30)); int color2 = 1 + rng.Next(15);
                CdSame("MessageText " + n, setup, () => { CharDisp.MessageText(s, B(color), true); CharDisp.MessageText(s2, B(color2), false); }, () =>
                {
                    if (combat)
                    {
                        CB(23, 10, 38, 21); DMW(23, 10, 38, 21, color, true, s); DMW(23, 10, 38, 21, color2, false, s2);
                    }
                    else
                    {
                        int top = OOpen(suppress); DMW(1, top + 1, 38, 22, color, true, s); DMW(1, top + 1, 38, 22, color2, false, s2);
                    }
                }, MoreKeys());
                CdSame("PrintMessageWait " + n, setup, () => CharDisp.PrintMessageWait(s), () =>
                {
                    if (combat) { CB(23, 10, 38, 21); DMW(23, 10, 38, 21, 10, true, s); }
                    else { int top = OOpen(suppress); DMW(1, top + 1, 38, 22, 10, true, s); }
                    TextRend.DisplayAndPause(13, "Press any key to continue"); CB(0, 24, 39, 24);
                }, MoreKeys());
            }
        }
        finally { CdRestore(); }
    }

    static Types.TItemRec MkWeapon(int seed)
    {
        var r = new Random(seed); var it = new Types.TItemRec { name = "BLADE", inUse = 1, itemType = (byte)r.Next(1, 20), plus = (sbyte)r.Next(-3, 4), quantity = 1, cursed = 0 };
        it.nameIds[1] = (byte)r.Next(0, 3); it.nameIds[2] = 0; it.nameIds[3] = 0; it.unidMask = (byte)r.Next(2);
        return it;
    }

    static void CharDispInfo()
    {
        try
        {
            for (int n = 0; n < 500; n++)
            {
                bool weapon = rng.Next(2) == 0, enabled = rng.Next(3) != 0; int ac = rng.Next(256), hp = rng.Next(256), max = rng.Next(256), status = rng.Next(9), wseed = rng.Next(), helpless = rng.Next(3);
                Types.TCharRecord cr = null; Types.TItemRec w = null; string nm = RandTxt(1 + rng.Next(12)).Replace(" ", "Z"); int host = rng.Next(2);
                Action setup = () =>
                {
                    cr = MkChar(nm, ac, hp, max, enabled ? 1 : 0, host); cr.status = (byte)status;
                    if (helpless == 0) cr.effectsAddr = new Types.TEffectRec { id = GDConsts.helplessFx[1] };
                    if (weapon) { w = MkWeapon(wseed); cr.equipped[C_Equip.WEAPON] = w; }
                    GDVars.ifcState = C_Ifc.COMBAT; GDVars.curRow = 77;
                };
                string nameSave = null; byte statusSave = 0;
                CdSame("CombatPrintCreatureInfo " + n + " en=" + enabled + " st=" + status + " hl=" + helpless + " w=" + weapon, setup, () => CharDisp.CombatPrintCreatureInfo(cr), () =>
                {
                    CB(23, 1, 38, 21); OPNS(cr, " ", 1, false);
                    DS(23, 3, 10, "Hitpoints"); DS(33, 3, hp < max ? 14 : 10, hp.ToString());
                    DS(23, 5, 10, "AC"); DS(26, 5, 10, (ac > 60 ? "-" : "") + Math.Abs(ac - 60));
                    GDVars.curRow = 5;
                    if (cr.equipped[C_Equip.WEAPON] != null) { CharHlp.ItemLabel(cr.equipped[C_Equip.WEAPON], false); DMW(23, 7, 38, 9, 10, true, cr.equipped[C_Equip.WEAPON].name); }
                    int sr = GDVars.curRow + 2; /* the message window leaves curRow on its last line */ if (cr.enabled == 0) DS(23, sr, 15, GDConsts.statusStrs[cr.status]); else if (CharHlp.IsHelpless(cr)) DS(23, sr, 15, "(Helpless)");
                }, MoreKeys());
            }
            // the curRow side effect
            CdOnce(() => { GDVars.ifcState = C_Ifc.COMBAT; GDVars.curRow = 77; }, () => CharDisp.CombatPrintCreatureInfo(MkChar("A", 60, 1, 1, 1, 0)), null);
            Eq(5, GDVars.curRow, "curRow set");
        }
        finally { CdRestore(); }
    }
}
