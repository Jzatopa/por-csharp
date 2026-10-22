using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Por.Core;
using static Asm;

static partial class Tests
{
    // ---- TREASURE ----

    // The random-item table, written out from eclvm.pas's case statements, with the overlapping 86..92 / 91..98
    // labels made contiguous (86..90 cleric scroll, 91..98 magic items) as the user directed on 2026-10-21.
    static int ExpectedItem(Queue<int> r)
    {
        int roll = r.Dequeue();
        if (roll <= 60)
        {
            int roll2;
            do roll2 = r.Dequeue(); while (roll2 == 48 || roll2 == 49);
            if (roll2 <= 59) return roll2 == 45 ? 59 : roll2;
            if (roll2 <= 90)
            {
                int k = r.Dequeue();
                return k <= 4 ? 36 : k <= 7 ? 35 : k == 8 ? 34 : k == 9 ? 37 : 38;
            }
            return roll2 <= 94 ? 73 : roll2 <= 97 ? 93 : 77;
        }
        if (roll <= 85) return 61;
        if (roll <= 90) return 62;
        if (roll <= 98)
        {
            int k = r.Dequeue();
            int[] t = { 0, 71, 71, 71, 71, 71, 71, 71, 70, 70, 84, 78, 78, 79, 79, 92, 67 };
            return t[k];
        }
        return 69;
    }

    static void CheckTreasurePath(params int[] seq)
    {
        var expectQ = new Queue<int>(seq);
        int want = ExpectedItem(expectQ);
        Yes(expectQ.Count == 0, "model consumed the sequence");
        var q = new Queue<int>(seq);
        var sides = new List<int>();
        byte got = EclVm.TreasureItemId(s => { sides.Add(s); return (byte)q.Dequeue(); });
        Yes(got == want && q.Count == 0, "treasure item for " + string.Join(",", seq));
    }

    static void RunTreasureTable()
    {
        for (int roll = 61; roll <= 100; roll++)
        {
            if (roll >= 91 && roll <= 98) for (int k = 1; k <= 16; k++) CheckTreasurePath(roll, k);
            else CheckTreasurePath(roll);
        }
        // the fixed boundary: 90 is the last cleric scroll, 91 and 92 reach the magic-item table (1d16 asked)
        foreach (int edge in new[] { 90, 91, 92 })
        {
            var asked16 = new List<byte>();
            byte id = EclVm.TreasureItemId(s => { asked16.Add(s); return s == 100 ? (byte)edge : (byte)15; });
            if (edge == 90) Yes(id == 62 && asked16.SequenceEqual(new byte[] { 100 }), "90 is a cleric scroll");
            else Yes(id == 92 && asked16.SequenceEqual(new byte[] { 100, 16 }), edge + " is the magic-item table");
        }
        // every first roll lands in exactly one band: 60 / 25 / 5 / 8 / 2
        var bands = new Dictionary<int, int>();
        for (int roll = 1; roll <= 100; roll++)
        {
            int calls = 0;
            byte id = EclVm.TreasureItemId(s => calls++ == 0 ? (byte)roll : (byte)1);
            int band = roll <= 60 ? 0 : id == 61 ? 1 : id == 62 ? 2 : id == 69 ? 4 : 3;
            bands[band] = bands.ContainsKey(band) ? bands[band] + 1 : 1;
        }
        Yes(bands[0] == 60 && bands[1] == 25 && bands[2] == 5 && bands[3] == 8 && bands[4] == 2, "band sizes 60/25/5/8/2");
        for (int roll = 1; roll <= 60; roll += 7)
            for (int roll2 = 1; roll2 <= 100; roll2++)
            {
                if (roll2 == 48 || roll2 == 49) { CheckTreasurePath(roll, roll2, 49, 48, 5); continue; }
                if (roll2 >= 60 && roll2 <= 90) for (int k = 1; k <= 10; k++) CheckTreasurePath(roll, roll2, k);
                else CheckTreasurePath(roll, roll2);
            }
        // the dice asked for, in order: 1d100, then 1d100 / 1d10 / 1d16
        var asked = new List<byte>();
        var seqQ = new Queue<byte>(new byte[] { 10, 48, 70, 3 });
        EclVm.TreasureItemId(s => { asked.Add(s); return seqQ.Dequeue(); });
        Yes(asked.SequenceEqual(new byte[] { 100, 100, 100, 10 }), "dice order");

        // TREASURE with $80+n: money from vars 1..7, n generated items prepended, then every item labelled
        for (int n = 0; n <= 6; n++)
        {
            Setup();
            var seed = new Random(n);
            Dice.RandomBelow = s => seed.Next(s);
            var a = new Asm();
            a.Op(C_Ecl.TREASURE, Wd(65535), Wd(1), Wd(2), Wd(300), Wd(4), Wd(5), Wd(6), I(0x80 + n));
            Exec(a);
            Yes(GDVars.treasureMoney[C_Money.COPPER] == 65535 && GDVars.treasureMoney[3] == 300 && GDVars.treasureMoney[C_Money.JEWELRY] == 6, "treasure money");
            Yes(fake.Log.Count(l => l.StartsWith("PrependItem:")) == n, "n random items");
            int count = 0; for (var p = GDVars.treasureItems; p != null; p = p.next) count++;
            Yes(count == n && fake.Log.Count(l => l.StartsWith("ItemLabel:")) == n, "every item labelled");
            int lastPrepend = fake.Log.FindLastIndex(l => l.StartsWith("PrependItem:"));
            Yes(n == 0 || fake.Log.FindIndex(l => l.StartsWith("ItemLabel:")) > lastPrepend, "labels after generation");
        }
        Setup();
        var none = new Asm(); none.Op(C_Ecl.TREASURE, I(1), I(1), I(1), I(1), I(1), I(1), I(1), I(255)); Exec(none);
        Yes(fake.Log.All(l => !l.StartsWith("PrependItem") && !l.StartsWith("ItemLabel")) && GDVars.treasureMoney[0] == 1, "255: money only");
    }

    static byte[] ItemBytes(Random r, out object[] fields)
    {
        var b = new byte[EclVm.ITEM_REC_SIZE];
        r.NextBytes(b);
        int len = r.Next(41);
        b[0] = (byte)len;
        string name = new string(Enumerable.Range(1, len).Select(i => (char)b[i]).ToArray());
        fields = new object[] { name, b[41], b[46], b[47], b[48], b[49], unchecked((sbyte)b[50]), unchecked((sbyte)b[51]), b[52], b[53], b[54],
            (ushort)(b[55] + 256 * b[56]), b[57], (ushort)(b[58] + 256 * b[59]), b[60], b[61], b[62] };
        return b;
    }

    static void WriteDax(string name, Dictionary<byte, byte[]> blocks)
    {
        var packs = new List<byte[]>();
        foreach (var raw in blocks.Values)
        {
            var p = new List<byte>();
            for (int a = 0; a < raw.Length; a += 126) { int n = Math.Min(126, raw.Length - a); p.Add((byte)(n - 1)); p.AddRange(raw.Skip(a).Take(n)); }
            packs.Add(p.ToArray());
        }
        using (var w = new BinaryWriter(File.Create(Path.Combine(Path.GetDirectoryName(GDVars.gameDir), name.ToLowerInvariant() + ".dax"))))
        {
            w.Write((ushort)(blocks.Count * 9));
            int off = 0, k = 0;
            foreach (var row in blocks) { w.Write(row.Key); w.Write(off); w.Write((ushort)row.Value.Length); w.Write((ushort)packs[k].Length); off += packs[k++].Length; }
            foreach (var p in packs) w.Write(p);
        }
        DaxIo.ResetArchiveCache();
    }

    static void RunTreasureFile()
    {
        var r = new Random(63);
        // field-by-field decode of the packed 63-byte record
        for (int t = 0; t < 3000; t++)
        {
            object[] f;
            var b = ItemBytes(r, out f);
            var pad = new byte[b.Length + 10];
            Array.Copy(b, 0, pad, 5, b.Length);
            var it = EclVm.ItemFromBytes(pad, 5);
            object[] got = { it.name, it.inUse, it.itemType, it.nameIds[1], it.nameIds[2], it.nameIds[3], it.plus, it.saveBonus,
                it.equipped, it.unidMask, it.cursed, it.weight, it.quantity, it.value, it.charges, it.effect, it.special };
            for (int i = 0; i < f.Length; i++) Yes(Equals(f[i], got[i]), "item field " + i);
            Yes(it.next == null, "stale pointer dropped");
        }
        // TREASURE id < $80 loads ITEM<disk>.dax: whole records prepended in file order, a trailing part record ignored
        var blocks = new Dictionary<byte, byte[]>();
        var names = new List<string>();
        var data = new List<byte>();
        for (int i = 0; i < 3; i++) { object[] f; data.AddRange(ItemBytes(r, out f)); names.Add((string)f[0] + ":" + f[2]); }
        data.AddRange(new byte[] { 1, 2, 3, 4, 5, 6, 7 });
        blocks[17] = data.ToArray();
        blocks[18] = new byte[40];
        WriteDax("ITEM1", blocks);
        var a = new Asm(); a.Op(C_Ecl.TREASURE, I(0), I(0), I(0), I(0), I(0), I(0), I(0), I(17)); Exec(a);
        var pre = fake.Log.Where(l => l.StartsWith("PrependItem:")).Select(l => l.Substring(12)).ToList();
        Yes(pre.SequenceEqual(names), "file records in order");
        Yes(fake.Log.All(l => !l.StartsWith("ItemLabel")), "archive items are not relabelled");
        Setup();
        a = new Asm(); a.Op(C_Ecl.TREASURE, I(0), I(0), I(0), I(0), I(0), I(0), I(0), I(18)); Exec(a);
        Yes(fake.Log.All(l => !l.StartsWith("PrependItem")), "a block shorter than one record adds nothing");
        // a missing block shows the message and exits the game
        Setup();
        bool halted = false;
        try { a = new Asm(); a.Op(C_Ecl.TREASURE, I(0), I(0), I(0), I(0), I(0), I(0), I(0), I(19)); Exec(a); }
        catch (Halted) { halted = true; }
        Yes(halted && fake.Log.All(l => !l.StartsWith("PrependItem")), "missing item file exits");
    }

    // ---- ENCOUNTER ----

    class EncModel
    {
        public List<int> Writes = new List<int>();
        public List<string> Menus = new List<string>();
        public int Dist, Displays, Waits, Flees;
    }

    // eclvm.pas EclEncounter, restated as a model over the menu answers (255 re-asks: see the DEVIATION note)
    static EncModel ModelEncounter(int[] resp, Queue<int> answers, int dist, bool view3d, int fleePar, int fleeMon, int hi, int lo)
    {
        var m = new EncModel { Dist = dist };
        bool waited;
        do
        {
            waited = false;
            string menu = (m.Dist == 0 || !view3d) ? "~COMBAT ~WAIT ~FLEE ~PARLAY" : "~COMBAT ~WAIT ~FLEE ~ADVANCE";
            int sel;
            do { m.Menus.Add(menu); sel = answers.Dequeue(); } while (sel > 3);
            if ((m.Dist == 0 || !view3d) && sel == 3) sel = 4;
            int c = resp[sel];
            Action approachOrWait = () => { if (m.Dist != 0) { m.Dist--; m.Displays++; } else m.Waits++; waited = true; };
            Action approachOrFight = () => { if (m.Dist == 0) m.Writes.Add(3); else { m.Dist--; m.Displays++; waited = true; } };
            Action flee = () => { m.Writes.Add(0); m.Flees++; };
            switch (c)
            {
                case 0: m.Writes.Add(sel != 2 ? 1 : (lo >= fleePar ? 2 : 1)); break;
                case 1:
                    if (sel == 0) m.Writes.Add(1); else if (sel == 1) { waited = true; m.Waits++; } else if (sel == 2) m.Writes.Add(2);
                    else if (sel == 3) approachOrWait(); else approachOrFight();
                    break;
                case 2: if (sel == 0) { if (fleeMon > hi) flee(); else m.Writes.Add(1); } else flee(); break;
                case 3:
                    if (sel == 0) m.Writes.Add(1); else if (sel == 1 || sel == 3) approachOrWait(); else if (sel == 2) m.Writes.Add(2); else approachOrFight();
                    break;
                case 4:
                    if (sel == 0) m.Writes.Add(1); else if (sel == 2) m.Writes.Add(2); else approachOrFight();
                    break;
            }
        } while (waited);
        return m;
    }

    static void RunEncounter()
    {
        for (int t = 0; t < 6000; t++)
        {
            Setup();
            var resp = Enumerable.Range(0, 5).Select(i => rng.Next(5)).ToArray();
            int maxDist = rng.Next(3), req = rng.Next(4);
            bool view3d = rng.Next(2) == 1;
            int fleePar = rng.Next(15), fleeMon = rng.Next(15);
            fake.MaxDist = (byte)maxDist; fake.HiMove = (byte)rng.Next(15); fake.LoMove = (byte)rng.Next(15);
            var answers = new List<int>();
            for (int i = 0; i < 8; i++) answers.Add(rng.Next(10) == 0 ? 255 : rng.Next(4));
            answers.Add(0);
            foreach (var x in answers) fake.HMenu.Enqueue((byte)x);
            fake.Strings[1] = t % 3 == 0 ? "" : "FAR"; fake.Strings[2] = t % 4 == 0 ? "" : "NEAR"; fake.Strings[3] = "HERE";
            GDVars.zone0.view3dFlag = (ushort)(view3d ? 1 : 0);
            var a = new Asm();
            a.Op(C_Ecl.ENCOUNTER, I(9), I(req), I(4), A(0x4D00), I(resp[0]), I(resp[1]), I(resp[2]), I(resp[3]), I(resp[4]),
                S(1), S(2), S(3), I(fleePar), I(fleeMon));
            Exec(a);
            var m = ModelEncounter(resp, new Queue<int>(answers), Math.Min(maxDist, req), view3d, fleePar, fleeMon, fake.HiMove, fake.LoMove);
            var writes = fake.Log.Where(l => l.StartsWith("W:4D00=")).Select(l => int.Parse(l.Substring(7))).ToList();
            Yes(writes.SequenceEqual(m.Writes), "encounter outcome writes " + string.Join(",", resp));
            Yes(fake.HMenuTexts.Select(s => s.Split('|')[0]).SequenceEqual(m.Menus), "encounter menus");
            Yes(GDVars.zone1[C_Gbvm.MONSTER_DIST] == m.Dist, "monster distance");
            Yes(fake.Log.Count(l => l.StartsWith("Monster:9,4,")) == 1 + m.Displays, "monster redraws");
            Yes(GDVars.zone1[C_Gbvm.MONSTER_DIST_REQ] == req, "requested distance kept");
            Yes(GDVars.inEncMenu == 0 && GDVars.msgDelayFlag == 0, "encounter flags reset");
            Yes(fake.Log.IndexOf("MoveExtremes") < fake.Log.IndexOf("LoadEclVars:14"), "move extremes read first");
            Yes(fake.HMenuTexts.All(s => s.EndsWith("|False|10|15|" + (GDVars.eclGraphics && GDVars.picDrawn) + "|")), "menu colours");
        }
    }

    static void RunParlay()
    {
        for (int t = 0; t < 500; t++)
        {
            Setup();
            var v = Enumerable.Range(0, 5).Select(i => rng.Next(256)).ToArray();
            int sel = rng.Next(5);
            if (t % 4 == 0) fake.HMenu.Enqueue(255);
            fake.HMenu.Enqueue((byte)sel);
            var a = new Asm(); a.Op(C_Ecl.PARLAY, I(v[0]), I(v[1]), I(v[2]), I(v[3]), I(v[4]), A(0x4D10)); Exec(a);
            Yes(fake.Mem[0x4D10] == v[sel], "parlay stores the chosen attitude's value");
            Yes(fake.HMenuTexts.All(s => s == "~HAUGHTY ~SLY ~NICE ~MEEK ~ABUSIVE|False|10|15|False| "), "parlay menu");
            Yes(fake.HMenuTexts.Count == (t % 4 == 0 ? 2 : 1), "255 asks again");
        }
    }

    static void RunHMenu()
    {
        for (int t = 0; t < 1500; t++)
        {
            Setup();
            int count = t < 30 ? t : rng.Next(26);
            GDVars.gbStrings[1] = "PREVIOUS";
            var a = new Asm();
            a.Op(C_Ecl.HMENU, A(0x4D20), I(count));
            var strs = new List<string>();
            for (int i = 0; i < count; i++)
            {
                string s = (t % 7 == 0 && i == 0) ? "PRESS <RETURN> OR BUTTON TO CONTINUE" : new string((char)('A' + i % 26), 1 + rng.Next(9));
                fake.Strings[10 + i] = s; strs.Add(s);
                a.Bytes.AddRange(S(10 + i));
            }
            byte ans = (byte)rng.Next(256);
            fake.HMenu.Enqueue(ans);
            Exec(a);
            // model: strings 1..20 kept, later ones overwrite the 20th; count 0 shows the previous gbStrings[1]
            var g = new string[21];
            g[1] = "PREVIOUS";
            for (int i = 0; i < count; i++) g[Math.Min(i + 1, 20)] = strs[i];
            int c = count > 20 ? 20 : count == 0 ? 1 : count;
            if (c == 1 && g[1] == "PRESS <RETURN> OR BUTTON TO CONTINUE") g[1] = "PRESS <ENTER>/<RETURN> TO CONTINUE";
            string menu = "";
            for (int i = 1; i <= c - 1; i++) { menu = menu + "~" + g[i] + " "; if (menu.Length > 50) menu = menu.Substring(0, 50); }
            menu = menu + "~" + g[c]; if (menu.Length > 50) menu = menu.Substring(0, 50);
            string want = menu + "|" + (c == 1) + "|" + (c == 1 ? 15 : 10) + "|15|False|";
            Yes(fake.HMenuTexts.Single() == want, "hmenu text " + count);
            Yes(fake.Mem[0x4D20] == ans, "hmenu stores the index");
            Yes(draw.Contains("c:0:24:39:24"), "bottom line cleared");
            Yes(GDVars.eclIp == Asm.Base + 6 + count * 2, "hmenu ip past the strings");
        }
    }

    static void RunVMenu()
    {
        for (int t = 0; t < 600; t++)
        {
            Setup();
            int count = t < 25 ? t : rng.Next(24);
            var a = new Asm();
            fake.Strings[1] = "PICK ONE";
            a.Op(C_Ecl.VMENU, A(0x4D30), S(1), I(count));
            var strs = new List<string>();
            for (int i = 0; i < count; i++) { string s = "ITEM" + i; fake.Strings[40 + i] = s; strs.Add(s); a.Bytes.AddRange(S(40 + i)); }
            short idx = (short)(t % 5 == 0 ? -1 : rng.Next(Math.Max(1, Math.Min(count, 20))));
            fake.VMenu.Enqueue(idx);
            Exec(a);
            var g = new string[21];
            g[1] = "PICK ONE";
            for (int i = 0; i < count; i++) g[Math.Min(i + 1, 20)] = strs[i];
            int members = Math.Max(1, Math.Min(count, 20));
            string names = string.Join("/", Enumerable.Range(1, members).Select(i => g[i]));
            string line = fake.Log.Single(l => l.StartsWith("VMenu:"));
            Yes(line.EndsWith(":" + names), "vmenu list " + count + " " + line);
            Yes(fake.Mem[0x4D30] == W(idx), "vmenu index stored as a Word");
            Yes(draw.Last() == "c:" + C_Msg.TEXT_LEFT + ":" + C_Msg.MESSAGE_TOP + ":" + C_Msg.TEXT_RIGHT + ":" + C_Msg.TEXT_BOTTOM, "message area cleared last");
        }
    }
}
