using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Por.Core;
using static Asm;

static partial class Tests
{
    static void Rerun(ushort at)
    {
        GDVars.eclIp = at;
        GDVars.curOpcode = GDVars.zone3[at];
        EclVm.ExecNextEclCmd();
    }

    static ushort[] Edge = { 0, 1, 2, 3, 127, 128, 255, 256, 32767, 32768, 65534, 65535 };

    static IEnumerable<ushort[]> Pairs(int randomCount)
    {
        foreach (var a in Edge) foreach (var b in Edge) yield return new[] { a, b };
        for (int i = 0; i < randomCount; i++) yield return new[] { W(rng.Next(65536)), W(rng.Next(65536)) };
    }

    // ADD SUBTRACT DIVIDE MULTIPLY: Word results, SUBTRACT = var2 - var1, DIVIDE by zero stores 0
    static void RunArith()
    {
        int[] ops = { C_Ecl.ADD, C_Ecl.SUBTRACT, C_Ecl.DIVIDE, C_Ecl.MULTIPLY };
        foreach (var pr in Pairs(5000))
            foreach (int op in ops)
            {
                fake.Mem.Clear();
                var a = new Asm();
                bool viaMemory = (pr[0] & 1) == 0;
                if (viaMemory) { fake.Mem[0x4B40] = pr[0]; fake.Mem[0x4B42] = pr[1]; a.Op(op, A(0x4B40), A(0x4B42), A(0x4B10)); }
                else a.Op(op, Wd(pr[0]), Wd(pr[1]), A(0x4B10));
                Exec(a);
                long v1 = pr[0], v2 = pr[1];
                long want = op == C_Ecl.ADD ? v1 + v2 : op == C_Ecl.SUBTRACT ? v2 - v1 : op == C_Ecl.MULTIPLY ? v1 * v2 : (v2 == 0 ? 0 : v1 / v2);
                Yes(fake.Mem[0x4B10] == (ushort)(want & 0xFFFF), "arith " + op + " " + v1 + "," + v2);
                Yes(GDVars.eclIp == Asm.Base + 10, "arith ip");
            }
    }

    // AND / OR: byte result, flags as COMPARE 0, result
    static void RunAndOr()
    {
        foreach (var pr in Pairs(3000))
            foreach (int op in new[] { C_Ecl.BIT_AND, C_Ecl.BIT_OR })
            {
                fake.Log.Clear();
                var a = new Asm();
                a.Op(op, Wd(pr[0]), Wd(pr[1]), A(0x4B12));
                Exec(a);
                int r = (op == C_Ecl.BIT_AND ? pr[0] & pr[1] : pr[0] | pr[1]) & 0xFF;
                Yes(fake.Mem[0x4B12] == r, "and/or value");
                Yes(fake.Log.Contains("CmpInt:0," + r), "and/or compares 0 with result");
                Yes(fake.Log.IndexOf("CmpInt:0," + r) < fake.Log.IndexOf("W:4B12=" + r), "flags set before store");
                Yes(GDVars.cmpFlags[0] == (r == 0) && GDVars.cmpFlags[2] == (r != 0), "and/or flags");
            }
    }

    // RANDOM: Random(n+1) for n < 255, Random(255) for 255, n taken as a Byte
    static void RunRandom()
    {
        for (int n = 0; n <= 600; n++)
        {
            var a = new Asm();
            a.Op(C_Ecl.RANDOM_NUMBER, Wd(n), A(0x4B14));
            int b = n & 0xFF;
            int arg = b < 255 ? b + 1 : 255;
            foreach (int roll in new[] { 0, arg - 1, (n * 7) % arg })
            {
                randomCalls.Clear();
                rolls.Enqueue(roll);
                Exec(a);
                Yes(randomCalls.Count == 1 && randomCalls[0] == arg, "random bound for " + n);
                Yes(fake.Mem[0x4B14] == roll, "random stored");
            }
        }
    }

    // GOTO, GOSUB, RETURN, COMPARE, IF, SAVE, EXIT through CallEclScript, with the STING trace log
    static void RunScriptFlow()
    {
        fake.SkipOperands[C_Ecl.JUMP] = 1;
        var a = new Asm();
        a.Op(C_Ecl.SAVE, I(0), A(0x4B00));
        ushort loop = a.Here;
        a.Op(C_Ecl.ADD, A(0x4B00), I(3), A(0x4B00));
        a.Op(C_Ecl.COMPARE, A(0x4B00), Wd(30));
        a.Op(C_Ecl.IF_LT);
        a.Op(C_Ecl.JUMP, Wd(loop));
        int gosub = a.Op(C_Ecl.GOSUB, Wd(0));
        ushort exitAt = a.Here;
        a.Op(C_Ecl.EXIT_SCRIPT);
        ushort sub = a.Here;
        a.Op(C_Ecl.MULTIPLY, A(0x4B00), Wd(2), A(0x4B02));
        a.Op(C_Ecl.RETURN);
        a.PatchWord(gosub + 2, sub);
        a.Load();
        var trace = new StringWriter();
        GDVars.debugFile = trace;
        GDVars.debugTraceEnabled = true;
        GDVars.curRow = 3; GDVars.curCol = 9; GDVars.dsEcl3dClearFlag = 1;
        EclVm.CallEclScript(Asm.Base);
        GDVars.debugTraceEnabled = false;
        Yes(fake.Mem[0x4B00] == 30 && fake.Mem[0x4B02] == 60, "loop and subroutine results");
        var want = new List<string> { "SAVE" };
        for (int i = 0; i < 9; i++) want.AddRange(new[] { "ADD", "COMPARE", EclTrace.OpName(C_Ecl.IF_LT), "GOTO" });
        want.AddRange(new[] { "ADD", "COMPARE", EclTrace.OpName(C_Ecl.IF_LT), "GOSUB", "MULTIPLY", EclTrace.OpName(C_Ecl.RETURN), "EXIT" });
        var got = trace.ToString().Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        Yes(got.SequenceEqual(want), "trace order: " + string.Join(",", got));
        Yes(fake.Log.Count(l => l == "Skip:01") == 1, "the false IF skipped the GOTO once");
        Yes(GDVars.eclIp == exitAt + 1 && EclVm.eclStop == 0, "EXIT steps past itself; eclStop cleared on return");
        Yes(GDVars.dsEclStackTopPtr == null, "stack empty");
        Yes(GDVars.curRow == C_Msg.MESSAGE_TOP && GDVars.curCol == C_Msg.TEXT_LEFT && GDVars.dsEcl3dClearFlag == 0, "EXIT parks cursor");

        // RETURN with an empty stack steps over the opcode only
        var r = new Asm(); r.Op(C_Ecl.RETURN); Exec(r);
        Yes(GDVars.eclIp == Asm.Base + 1, "RETURN empty stack");

        // nested GOSUBs then NEWECL inside the subroutine: stack freed, VM stopped, script switched
        Setup();
        var n = new Asm();
        int g1 = n.Op(C_Ecl.GOSUB, Wd(0));
        n.Op(C_Ecl.EXIT_SCRIPT);
        ushort s1 = n.Here; int g2 = n.Op(C_Ecl.GOSUB, Wd(0));
        ushort s2 = n.Here; n.Op(C_Ecl.NEWECL, I(42));
        n.PatchWord(g1 + 2, s1); n.PatchWord(g2 + 2, s2);
        n.Load();
        GDVars.eclScriptId = 7;
        GDVars.virtualDisk = 3;
        string originalGameDir = GDVars.gameDir;
        GDVars.gameDir = Path.Combine(Path.GetTempPath(), new string('d', 100)) + Path.DirectorySeparatorChar;
        EclVm.filesLoaded = 1;
        EclVm.CallEclScript(Asm.Base);
        Yes(GDVars.dsEclStackTopPtr == null, "NEWECL frees the return stack");
        Yes(GDVars.zone0.previousScriptId == 7 && GDVars.eclScriptId == 42, "NEWECL script ids");
        Yes(EclVm.newEclFlag == 1 && EclVm.filesLoaded == 0, "NEWECL flags");
        string file = GDVars.gameDir + DaxIo.ArchiveName("ECL3") + ".dax";
        Yes(file.Length > 79, "NEWECL regression exercises a long managed host path");
        int cf = fake.Log.IndexOf("CheckFile:" + file), ls = fake.Log.IndexOf("LoadEclScript:42"), ei = fake.Log.IndexOf("EclInit");
        GDVars.gameDir = originalGameDir;
        GDVars.virtualDisk = 1;
        Yes(cf >= 0 && cf < ls && ls < ei, "NEWECL checks full host path before load and init");

        // the VM stops when the party is defeated before running anything
        Setup();
        var d = new Asm(); d.Op(C_Ecl.SAVE, I(5), A(0x4B00)); d.Op(C_Ecl.EXIT_SCRIPT); d.Load();
        GDVars.partyDefeated = true;
        EclVm.CallEclScript(Asm.Base);
        Yes(!fake.Mem.ContainsKey(0x4B00), "defeated party runs nothing");

        // an opcode with no handler is stepped over by SkipEclCmd
        Setup();
        var u = new Asm(); u.Op(0x1F); u.Op(0x3E); u.Op(0xFF); u.Op(C_Ecl.EXIT_SCRIPT); u.Load();
        EclVm.CallEclScript(Asm.Base);
        Yes(fake.Log.Where(l => l.StartsWith("Skip:")).SequenceEqual(new[] { "Skip:1F", "Skip:3E", "Skip:FF" }), "unknown opcodes skipped");

        // SAVE number and string; COMPARE numbers and strings; COMPARE AND
        Setup();
        fake.Strings[5] = "HELLO"; fake.Strings[6] = "WORLD";
        var s = new Asm(); s.Op(C_Ecl.SAVE, Wd(54321), A(0x4B20)); Exec(s);
        Yes(fake.Mem[0x4B20] == 54321, "SAVE number");
        s = new Asm(); s.Op(C_Ecl.SAVE, S(5), A(0x4B22)); Exec(s);
        Yes(fake.MemStr[0x4B22] == "HELLO", "SAVE string");
        s = new Asm(); s.Op(C_Ecl.COMPARE, S(5), I(3)); Exec(s);
        Yes(fake.Log.Last() == "CmpStr:HELLO|" + GDVars.gbStrings[2], "COMPARE string when var 1 is a string");
        s = new Asm(); s.Op(C_Ecl.COMPARE, I(3), S(6)); Exec(s);
        Yes(fake.Log.Last().StartsWith("CmpStr:"), "COMPARE string when var 2 is a string");
        for (int i = 0; i < 2000; i++)
        {
            int[] v = { rng.Next(4), rng.Next(4), rng.Next(4), rng.Next(4) };
            for (int k = 0; k < 6; k++) GDVars.cmpFlags[k] = true;
            s = new Asm(); s.Op(C_Ecl.COMPARE_AND, Wd(v[0]), Wd(v[1]), Wd(v[2]), Wd(v[3])); Exec(s);
            bool eq = v[0] == v[1] && v[2] == v[3];
            Yes(GDVars.cmpFlags[0] == eq && GDVars.cmpFlags[1] == !eq && !GDVars.cmpFlags[2] && !GDVars.cmpFlags[3] &&
                !GDVars.cmpFlags[4] && !GDVars.cmpFlags[5], "COMPARE AND flags");
        }
    }

    // IF: step past the opcode, then SkipEclCmd unless the opcode's flag is set
    static void RunIfFlags()
    {
        fake.SkipOperands[C_Ecl.JUMP] = 1;
        int[] ifs = { C_Ecl.IF_EQ, C_Ecl.IF_NE, C_Ecl.IF_LT, C_Ecl.IF_GT, C_Ecl.IF_LE, C_Ecl.IF_GE };
        for (int k = 0; k < 6; k++)
            for (int mask = 0; mask < 64; mask++)
            {
                for (int j = 0; j < 6; j++) GDVars.cmpFlags[j] = (mask >> j & 1) != 0;
                var a = new Asm(); a.Op(ifs[k]); a.Op(C_Ecl.JUMP, Wd(0x1234));
                fake.Log.Clear();
                Exec(a);
                bool set = (mask >> k & 1) != 0;
                Yes(GDVars.eclIp == (set ? Asm.Base + 1 : Asm.Base + 5), "IF " + k + " mask " + mask);
                Yes(fake.Log.Contains("Skip:01") == !set, "IF skip call");
            }
    }

    // ONGOTO / ONGOSUB: selector below min(count,64) jumps or calls; otherwise falls through past the list
    static void RunOnGoto()
    {
        for (int count = 0; count <= 70; count++)
            for (int sel = 0; sel <= 72; sel += (sel < 3 || sel >= 59 ? 1 : 7))
                foreach (int op in new[] { C_Ecl.ONGOTO, C_Ecl.ONGOSUB })
                {
                    GDVars.dsEclStackTopPtr = null;
                    var a = new Asm();
                    a.Op(op, I(sel), I(count));
                    for (int i = 0; i < count; i++) a.Bytes.AddRange(Wd(0x9A00 + i * 3));
                    Exec(a);
                    ushort end = (ushort)(Asm.Base + 5 + count * 3);
                    bool hit = sel < Math.Min(count, 64);
                    ushort want = hit ? (ushort)(0x9A00 + sel * 3) : end;
                    Yes(GDVars.eclIp == want, "ON" + op + " sel " + sel + " count " + count);
                    if (op == C_Ecl.ONGOSUB && hit) Yes(GDVars.dsEclStackTopPtr != null && GDVars.dsEclStackTopPtr.addr == end, "ONGOSUB return address");
                    else Yes(GDVars.dsEclStackTopPtr == null, "no push");
                }
    }

    // GETTABLE and SAVETABLE: Word address arithmetic
    static void RunTables()
    {
        for (int i = 0; i < 3000; i++)
        {
            ushort baseAddr = W(rng.Next(65536)), dest = 0x4B30;
            int idx = rng.Next(i < 1500 ? 256 : 65536);
            ushort sum = W(baseAddr + (idx & 0xFF));
            fake.Mem.Clear();
            fake.Mem[sum] = W(i * 37 + 1);
            var a = new Asm(); a.Op(C_Ecl.GETTABLE, A(baseAddr), Wd(idx), A(dest)); Exec(a);
            Yes(fake.Mem[dest] == W(i * 37 + 1), "GETTABLE reads base + byte index");
            fake.Mem.Clear();
            a = new Asm(); a.Op(C_Ecl.SAVE_TABLE, Wd(i), A(baseAddr), Wd(idx)); Exec(a);
            Yes(fake.Mem.Count == 1 && fake.Mem[W(baseAddr + idx)] == i, "SAVETABLE writes base + word index");
        }
    }

    // PARTY STRENGTH
    static void RunPartyStr()
    {
        for (int t = 0; t < 3000; t++)
        {
            int size = 1 + rng.Next(8);
            var ms = new Types.TCharRecord[size];
            long want = 0;
            for (int i = 0; i < size; i++)
            {
                var m = ms[i] = Member("m" + i);
                m.hpCur = B(rng.Next(256)); m.acCur = B(rng.Next(256)); m.thac0Cur = B(rng.Next(256));
                m.classLevels[C_Level.MAGE] = unchecked((sbyte)rng.Next(256));
                m.classLevels[C_Level.CLERIC] = unchecked((sbyte)rng.Next(256));
                if (t < 100) { m.classLevels[C_Level.MAGE] = 0; m.classLevels[C_Level.CLERIC] = 0; m.hpCur = B(rng.Next(20)); }
                int ac = m.acCur > 60 ? m.acCur - 60 : 0, th = m.thac0Cur > 39 ? m.thac0Cur - 39 : 0;
                int cl = B(m.classLevels[C_Level.CLERIC]), mg = B(m.classLevels[C_Level.MAGE]);
                want += (cl * 4 + (m.hpCur + ac * 5 + th * 5 + mg * 8)) / 10;
            }
            Party(ms);
            var a = new Asm(); a.Op(C_Ecl.PARTY_STR, A(0x4B50)); Exec(a);
            Yes(fake.Mem[0x4B50] == Math.Min(255, want), "party strength " + want);
        }
    }

    // CHECK PARTY
    static void RunCheckParty()
    {
        for (int t = 0; t < 1500; t++)
        {
            int size = 1 + rng.Next(8);
            var ms = new Types.TCharRecord[size];
            for (int i = 0; i < size; i++)
            {
                ms[i] = Member("c" + i);
                ms[i].thiefSkills[3] = B(rng.Next(256));
                ms[i].movementCur = B(rng.Next(256));
            }
            Party(ms);
            int mode = t % 5;
            ushort attr = mode == 0 || mode == 1 ? (ushort)0x6BA7 : mode == 2 || mode == 3 ? (ushort)0x6C1B : (ushort)0;
            fake.Mem.Clear(); fake.Log.Clear(); fake.EffectHolders.Clear();
            int holder = rng.Next(size + 2);
            if (holder < size) fake.EffectHolders.Add("c" + holder);
            var a = new Asm();
            a.Op(C_Ecl.CHECK_PARTY, (mode % 2 == 0) ? A(attr) : Wd(attr), I(33), A(0x4C00), A(0x4C02), A(0x4C04), A(0x4C06));
            Exec(a);
            if (attr == 0)
            {
                Yes(fake.Mem[0x4C00] == 0 && fake.Mem[0x4C02] == 0 && fake.Mem[0x4C04] == 0, "effect check zeroes the stats");
                Yes(fake.Mem[0x4C06] == (holder < size ? 1 : 0), "effect found flag");
                Yes(fake.Log.Count(l => l.StartsWith("HasEffect:")) == (holder < size ? holder + 1 : size), "stops at first holder");
                Yes(fake.Log.All(l => !l.StartsWith("HasEffect:") || l.EndsWith(":33")), "effect id");
            }
            else
            {
                Func<Types.TCharRecord, int> f = c => attr == 0x6BA7 ? c.thiefSkills[3] : c.movementCur;
                Yes(fake.Mem[0x4C00] == ms.Min(f) && fake.Mem[0x4C02] == ms.Max(f), "min/max");
                Yes(fake.Mem[0x4C04] == (byte)(ms.Sum(f) / size) && fake.Mem[0x4C06] == 0, "average and found");
            }
        }
        // a type-1 operand of 0 with a nonzero word at address 0 still selects the effect check (address, not value)
        Party(Member("z"));
        fake.Mem.Clear(); fake.Mem[0] = 0x6BA7;
        var b = new Asm(); b.Op(C_Ecl.CHECK_PARTY, A(0), I(1), A(0x4C00), A(0x4C02), A(0x4C04), A(0x4C06)); Exec(b);
        Yes(fake.Log.Any(l => l.StartsWith("HasEffect:z")), "type 1 takes the address");
        // other attributes write nothing
        fake.Mem.Clear();
        b = new Asm(); b.Op(C_Ecl.CHECK_PARTY, Wd(0x1234), I(1), A(0x4C00), A(0x4C02), A(0x4C04), A(0x4C06)); Exec(b);
        Yes(fake.Mem.Count == 0, "unknown attribute writes nothing");
    }

    // SURPRISE
    static void RunSurprise()
    {
        var vals = new List<int[]>();
        for (int v1 = 0; v1 < 9; v1++) for (int v2 = 0; v2 < 9; v2++) for (int v3 = 0; v3 < 9; v3++) for (int v4 = 0; v4 < 9; v4++)
            vals.Add(new[] { v1, v2, v3, v4 });
        for (int i = 0; i < 3000; i++) vals.Add(new[] { rng.Next(256), rng.Next(256), rng.Next(256), rng.Next(256) });
        foreach (var v in vals)
        {
            var a = new Asm(); a.Op(C_Ecl.SURPRISE, I(v[0]), I(v[1]), I(v[2]), I(v[3])); a.Load();
            int t1 = unchecked((sbyte)(v[3] + 2 - v[0])), t2 = unchecked((sbyte)(v[1] + 2 - v[2]));
            for (int r1 = 1; r1 <= 6; r1 += (v[0] > 8 ? 5 : 1))
                for (int r2 = 1; r2 <= 6; r2 += (v[0] > 8 ? 5 : 1))
                {
                    rolls.Enqueue(r1 - 1); rolls.Enqueue(r2 - 1);
                    Rerun(Asm.Base);
                    int want = (r1 <= t1 ? 1 : 0) | (r2 <= t2 ? 2 : 0);
                    Yes(fake.Mem[C_Gbvm.SURPRISE] == want, "surprise");
                }
        }
    }
}
