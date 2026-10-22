using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Por.Core;

static partial class Tests
{
    const int WH = 15, LG = 10, LM = 13, YE = 14;

    // model of the list drawing in savemenu.pas
    static Scr ModelList(List<SM> L, int sel, int top)
    {
        var e = new Scr();
        e.Put(10, 1, WH, "Load/Save Game Menu");
        int count = L.Count;
        if (top > 1) e.Put(17, 3, WH, "(more)");
        for (int k = 0; k < 6; k++)
        {
            int i = top + k;
            if (i > count) continue;
            var s = L[i - 1];
            int row = 4 + 3 * k, color = i == sel ? WH : LG;
            e.Put(2, row, WH, s.L.ToString());
            e.Put(3, row, color, ":");
            if (s.Used)
            {
                e.Put(5, row, color, Fit(s.Desc, 27));
                e.Put(38 - s.Clock.Length, row, color, s.Clock);
                e.Put(5, row + 1, color, s.Area);
                e.Put(38 - s.Date.Length, row + 1, color, s.Date);
            }
            else e.Put(5, row, color, "EMPTY");
        }
        if (top + 5 < count) e.Put(17, 22, WH, "(more)");
        return e;
    }

    class NavModel
    {
        public int sel, top, count, choice = 3; public string listed; public char action; public int result = -2;
        public void Move(int i)
        {
            if (i < 1) i = count; else if (i > count) i = 1;
            sel = i; if (sel < top) top = sel; if (sel > top + 5) top = sel - 5;
        }
        public void Ext(int scan)
        {
            if (scan == 72 || scan == 71) Move(sel - 1);
            else if (scan == 80 || scan == 79) Move(sel + 1);
            else if (scan == 73) Move(sel > 6 ? sel - 6 : 1);
            else if (scan == 81) Move(sel + 6 < count ? sel + 6 : count);
        }
        void Step(bool next) { if (next) choice = choice == 4 ? 1 : choice + 1; else choice = choice == 1 ? 4 : choice - 1; }
        public void Take() { result = listed[sel - 1]; }
        public void Apply(byte[] b)
        {
            if (b.Length == 2 && b[0] == 0) { if (b[1] == 75) Step(false); else if (b[1] == 77) Step(true); else Ext(b[1]); return; }
            int k = b[0];
            if (k == 27) { result = 0; return; }
            if (k == 13)
            {
                if (choice == 1) Move(sel + 1); else if (choice == 2) Move(sel - 1); else if (choice == 3) Take(); else result = 0;
                return;
            }
            if (k == ',') { Step(false); return; }
            if (k == '.') { Step(true); return; }
            char up = char.ToUpper((char)k);
            if (up == ' ') return;
            if (up >= '1' && up <= '9') { Ext(new[] { 79, 80, 81, 75, 32, 77, 71, 72, 73 }[up - '1']); return; }
            if (up == 'N') { choice = 1; Move(sel + 1); return; }
            if (up == 'P') { choice = 2; Move(sel - 1); return; }
            if (up == action) { choice = 3; Take(); return; }
            if (listed.IndexOf(up) >= 0) result = up;
        }
    }

    static byte[] K(int k) { return new byte[] { (byte)k }; }
    static byte[] X(int s) { return new byte[] { 0, (byte)s }; }

    static void SaveMenuChoose()
    {
        SmSetup();
        try
        {
            // nothing to load
            {
                var all = new Dictionary<char, SM>();
                foreach (var f in Directory.GetFiles(smDir)) File.Delete(f);
                byte r = SaveMenu.ChooseSlot(false, (byte)'A');
                Eq(0, r, "no saves returns NONE"); Eq(0, smClears, "no clear"); Eq(0, smio.Reads, "no input");
                for (int x = 0; x < 40; x++) for (int y = 0; y < 25; y++) Yes(smscr.idx[x, y] == -1, "nothing drawn");
            }
            // expected border pictures
            smpics.Clear(); Border.DrawBorder(1, 3, 38, 22, 0); Border.DrawBorder(1, 1, 38, 1, 0);
            var borderPics = smpics.ToList();
            Yes(borderPics.Count > 0, "border produces pictures");
            var keypool = new List<byte[]>();
            foreach (int s in new[] { 72, 80, 71, 79, 73, 81, 75, 77, 63 + 100 }) keypool.Add(X(s));
            keypool.RemoveAll(b => b[1] == 163);
            foreach (char c in ",.NnPpSsLlEeAaBbCcDdFfGgHhIiJjQqKkMmZz 123456789") keypool.Add(K(c));
            for (int round = 0; round < 1500; round++)
            {
                foreach (var f in Directory.GetFiles(smDir)) File.Delete(f);
                bool saving = rng.Next(2) == 0;
                var all = new Dictionary<char, SM>();
                var L = MakeSlots(saving, all, round % 10 == 0 ? 2100 : 2040);
                if (L.Count == 0) { Eq(0, SaveMenu.ChooseSlot(saving, (byte)'A'), "empty"); continue; }
                string listed = new string(L.Select(s => s.L).ToArray());
                var m = new NavModel { count = L.Count, listed = listed, action = saving ? 'S' : 'L' };
                m.sel = 1; int newest = 0;
                for (int i = 1; i <= L.Count; i++) if (L[i - 1].Used && (newest == 0 || L[i - 1].Time > newest)) { newest = L[i - 1].Time; m.sel = i; }
                char start = rng.Next(3) == 0 ? "Z0Q" [rng.Next(3)] : listed[rng.Next(listed.Length)];
                if (listed.IndexOf(start) >= 0) m.sel = listed.IndexOf(start) + 1;
                m.top = 1; m.Move(m.sel);
                var batches = new List<byte[]>();
                if (rng.Next(3) == 0) { batches.Add(K(13)); m.Apply(K(13)); }
                while (m.result == -2 && batches.Count < 60)
                {
                    var b = keypool[rng.Next(keypool.Count)];
                    batches.Add(b); m.Apply(b);
                }
                if (m.result == -2) { batches.Add(K(27)); m.Apply(K(27)); }
                smscr.Clear(); smpics.Clear(); smClears = 0; smio.Keys.Clear();
                int pass = 0;
                MenuCore.idleHook = () => { if (pass >= batches.Count) throw new Exception("menu wants more keys than scripted"); foreach (byte b in batches[pass++]) smio.Keys.Enqueue(b); };
                byte got;
                try { got = SaveMenu.ChooseSlot(saving, (byte)start); } finally { MenuCore.idleHook = null; }
                Eq(m.result, got, "result round " + round + " saving=" + saving + " keys=" + string.Join(" ", batches.Select(b => string.Join(",", b))));
                Eq(batches.Count, pass, "all scripted keys used");
                Eq(1, smClears, "one screen clear");
                Yes(smpics.SequenceEqual(borderPics), "border pictures");
                string diff = smscr.Diff(ModelList(L, m.sel, m.top), 0, 23);
                Yes(diff == null, "screen round " + round + ": " + diff);
                for (int x = 0; x < 40; x++) EqS("", smscr.Cell(x, 24), "bottom line cleared");
            }
        }
        finally { MenuCore.idleHook = null; }
        try
        {
            // equal save times: the first listed slot of the newest time is selected
            foreach (var f in Directory.GetFiles(smDir)) File.Delete(f);
            var dt = new DateTime(2001, 2, 3, 4, 5, 6);
            foreach (char c in "BDE") { File.WriteAllBytes(Fp("savgam" + c + ".dat"), new byte[3]); File.SetLastWriteTime(Fp("savgam" + c + ".dat"), dt); }
            smio.Keys.Clear(); smio.Keys.Enqueue(13);
            int pass2 = 0;
            MenuCore.idleHook = () => { if (pass2++ > 0) throw new Exception("one pass expected"); };
            Eq('B', SaveMenu.ChooseSlot(false, (byte)'Z'), "first of equal times");
            MenuCore.idleHook = null;
        }
        finally { MenuCore.idleHook = null; SmDone(); }
    }

    static void SaveMenuAsk()
    {
        SmSetup();
        try
        {
            for (int round = 0; round < 3000; round++)
            {
                char letter = SL[rng.Next(SL.Length)];
                string area = rng.Next(4) == 0 ? "" : RandTxt(rng.Next(19));
                string initial = rng.Next(3) == 0 ? "" : RandTxt(rng.Next(31));
                // keys
                var keys = new List<byte>(); var exp = new List<string>();
                int n = rng.Next(0, 45);
                for (int i = 0; i < n; i++)
                {
                    int t = rng.Next(10);
                    if (t < 6) keys.Add((byte)(32 + rng.Next(91)));
                    else if (t == 6) keys.Add(8);
                    else if (t == 7) { keys.Add(0); keys.Add((byte)new[] { 72, 75, 65, 100, 32, 8, 77 }[rng.Next(7)]); }
                    else if (t == 8) keys.Add((byte)new[] { 1, 9, 123, 126, 200, 255 }[rng.Next(6)]);
                    else keys.Add((byte)(97 + rng.Next(26)));
                }
                int endKind = rng.Next(5);
                if (endKind == 0) { keys.Add(0); keys.Add(13); }
                else if (endKind == 1) { keys.Add(0); keys.Add(27); }
                else keys.Add((byte)(endKind == 2 ? 13 : 27));
                // model
                string s = Fit(initial, 27); string def = "SAVE GAME " + letter;
                var states = new List<string>(); int pos = 0; int key = 0;
                Func<int> read = () => { states.Add(s); return keys[pos++]; };
                do
                {
                    key = read();
                    if (key == 0) key = read();
                    else if (key >= 32 && key <= 122) { if (s.Length < 27) s += char.ToUpper((char)key); }
                    else if (key == 8) { if (s.Length > 0) s = s.Substring(0, s.Length - 1); }
                } while (key != 13 && key != 27);
                bool modelOk = key == 13; string modelDesc = s == "" ? def : s;
                Eq(keys.Count, pos, "model consumed all keys");
                // run
                var garbage = new Scr();
                for (int x = 0; x < 40; x++) for (int y = 2; y <= 24; y++) if (rng.Next(2) == 0) garbage.Glyph(x, y, 1 + rng.Next(15), 33 + rng.Next(30));
                smscr = garbage;
                GDVars.video.DrawGlyph = (x, y, c, g) => smscr.Glyph(x, y, c, g);
                GDVars.video.ClearRect = (l, t, r, b) => smscr.Rect(l, t, r, b);
                smio.Keys.Clear(); foreach (byte k in keys) smio.Keys.Enqueue(k);
                int read_i = 0; var fails = new List<string>();
                var baseScr = new Scr();
                for (int x = 0; x < 40; x++) for (int y = 0; y < 25; y++) { baseScr.idx[x, y] = garbage.idx[x, y]; baseScr.col[x, y] = garbage.col[x, y]; }
                smio.OnRead = () =>
                {
                    var e = new Scr();
                    for (int x = 0; x < 40; x++) for (int y = 0; y < 25; y++) { e.idx[x, y] = baseScr.idx[x, y]; e.col[x, y] = baseScr.col[x, y]; }
                    e.Rect(1, 3, 38, 22); e.Rect(0, 24, 39, 24);
                    e.Put(2, 4, LG, def); e.Put(2, 5, LG, "Location: " + area); e.Put(2, 7, LG, "Description:");
                    string st = states[read_i++];
                    string shown = st == "" ? def : st;
                    e.Put(2, 8, WH, shown);
                    for (int i = shown.Length + 1; i <= 27; i++) e.Put(2 + i - 1, 8, LG, ".");
                    e.Put(0, 24, LM, "Save: "); e.Put(6, 24, LG, "Exit");
                    string d = smscr.Diff(e, 2, 24);
                    if (d != null && fails.Count == 0) fails.Add("read " + read_i + ": " + d);
                };
                string desc = initial;
                bool ok = SaveMenu.AskDescription((byte)letter, area, ref desc);
                smio.OnRead = null;
                Yes(fails.Count == 0, "round " + round + " " + (fails.Count > 0 ? fails[0] : ""));
                Eq(states.Count, read_i, "reads");
                Eq(modelOk ? 1 : 0, ok ? 1 : 0, "result");
                EqS(modelOk ? modelDesc : initial, desc, "desc out round " + round);
                for (int x = 0; x < 40; x++) EqS("", smscr.Cell(x, 24), "bottom cleared");
                Eq(0, smpics.Count, "no pictures");
            }
        }
        finally { SmDone(); }
    }

    static void SaveMenuDir()
    {
        SmSetup();
        var orig = SaveMenu.MakeDir;
        try
        {
            EqS("Put save disk in C:", SaveMenu.SaveDiskMsg() == null ? null : (GDVars.dsSaveDirectory = "C:\\GAMES\\") == null ? null : SaveMenu.SaveDiskMsg(), "disk msg");
            GDVars.dsSaveDirectory = "D:\\X\\"; EqS("Put save disk in D:", SaveMenu.SaveDiskMsg(), "disk msg D");
            Action<string, int[], string[], int> run = (dir, script, calls, pauses) =>
            {
                GDVars.dsSaveDirectory = dir;
                var got = new List<string>(); int ix = 0;
                SaveMenu.MakeDir = p => { got.Add(p); return ix < script.Length ? script[ix++] : 0; };
                smio.Keys.Clear(); for (int i = 0; i < pauses + 1; i++) smio.Keys.Enqueue(13);
                int promptsSeen = 0;
                smio.OnRead = () =>
                {
                    promptsSeen++;
                    var e = new Scr(); e.Put(0, 24, YE, "Put save disk in " + dir[0] + ":");
                    Yes(smscr.Diff(e, 24, 24) == null, "prompt text");
                };
                SaveMenu.CheckSaveDir();
                smio.OnRead = null;
                EqS(string.Join("|", calls), string.Join("|", got), "calls for " + dir);
                Eq(pauses, promptsSeen, "pauses for " + dir);
            };
            run("C:\\", new int[0], new string[0], 0);
            run("C:", new int[0], new string[0], 0);
            run("", new int[0], new string[0], 0);
            run("C:\\GAMES\\", new[] { 0 }, new[] { "C:\\GAMES" }, 0);
            run("C:\\GAMES\\", new[] { 5 }, new[] { "C:\\GAMES" }, 0);
            run("C:\\GAMES\\", new[] { 3, 0 }, new[] { "C:\\GAMES", "C:\\GAMES" }, 1);
            run("C:\\GAMES\\", new[] { 1, 2, 4, 0 }, new[] { "C:\\GAMES", "C:\\GAMES", "C:\\GAMES", "C:\\GAMES" }, 3);
            run("C:\\A\\", new[] { 0 }, new[] { "C:\\A" }, 0);
            run("C:\\A\\B\\C\\", new[] { 0, 0, 0 }, new[] { "C:\\A", "C:\\A\\B", "C:\\A\\B\\C" }, 0);
            run("C:\\A\\B\\C\\", new[] { 99, 98, 0 }, new[] { "C:\\A", "C:\\A\\B", "C:\\A\\B\\C" }, 0);
            run("C:\\A\\B\\C\\", new[] { 1, 1, 9, 0, 0, 0 }, new[] { "C:\\A", "C:\\A\\B", "C:\\A\\B\\C", "C:\\A", "C:\\A\\B", "C:\\A\\B\\C" }, 1);
            run("C:\\A\\B\\C\\", new[] { 0, 0, 5 }, new[] { "C:\\A", "C:\\A\\B", "C:\\A\\B\\C" }, 0);
            run("/tmp/x/y/", new[] { 0, 0, 0 }, new[] { "/tmp", "/tmp/x", "/tmp/x/y" }, 0);
            // the real MakeDir
            SaveMenu.MakeDir = orig;
            string baseDir = smDir + "a" + Path.DirectorySeparatorChar;
            Eq(3, orig(baseDir + "b"), "missing parent");
            Eq(0, orig(baseDir.TrimEnd(Path.DirectorySeparatorChar)), "created");
            Eq(5, orig(baseDir.TrimEnd(Path.DirectorySeparatorChar)), "exists");
            Eq(0, orig(baseDir + "b"), "child created");
            Eq(3, orig(smDir + "x\0y"), "creation failure is not success");
            string deep = smDir + "p" + Path.DirectorySeparatorChar + "q" + Path.DirectorySeparatorChar + "r" + Path.DirectorySeparatorChar;
            GDVars.dsSaveDirectory = deep;
            smio.Keys.Clear();
            SaveMenu.CheckSaveDir();
            int readsBefore = smio.Reads;
            Yes(Directory.Exists(deep), "nested directory created");
            SaveMenu.CheckSaveDir();
            Eq(readsBefore, smio.Reads, "no prompt when it exists");
        }
        finally { SaveMenu.MakeDir = orig; smio.OnRead = null; SmDone(); }
    }
}
