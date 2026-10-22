using System;
using System.Collections.Generic;
using System.Linq;
using Por.Core;
using static Asm;

static partial class Tests
{
    static readonly byte[] AllKinds = { EvtKind.KIND_NONE, EvtKind.KIND_MESSAGE, EvtKind.KIND_TRAP, EvtKind.KIND_TREASURE,
        EvtKind.KIND_SERVICE, EvtKind.KIND_FIGHT, EvtKind.KIND_EXIT };

    // AND [event] #mask [v]; ON GOTO / ON GOSUB [v]: address k for (event & mask) = k - 1
    static void RunOnGotoMask()
    {
        foreach (int op in new[] { C_Ecl.ONGOTO, C_Ecl.ONGOSUB })
            foreach (bool reversed in new[] { false, true })
                foreach (int mask in new[] { 0x0F, 0x1F, 0x07, 0xFF, 0x3C })
                {
                    Setup();
                    int n = 1 + rng.Next(12);
                    var kinds = Enumerable.Range(0, n).Select(i => AllKinds[rng.Next(AllKinds.Length)]).ToArray();
                    var a = new Asm();
                    a.Mark("stop"); a.Op(C_Ecl.EXIT_SCRIPT);
                    a.Mark("main");
                    if (reversed) a.Op(C_Ecl.BIT_AND, I(mask), A(EV), A(0x4B10));
                    else a.Op(C_Ecl.BIT_AND, A(EV), I(mask), A(0x4B10));
                    a.Op(op, A(0x4B10), I(n)); a.Labels("t", n);
                    a.Op(C_Ecl.EXIT_SCRIPT);
                    for (int i = 0; i < n; i++) Body(a, "t" + i, kinds[i]);
                    Prepare(a, "main");
                    // entry i is for (event & mask) = i & mask: entries past the mask's range wrap onto earlier values,
                    // and an event matching several takes the largest kind
                    for (int e = 0; e < 256; e++)
                    {
                        int best = -1;
                        for (int i = 0; i < n; i++) if ((e & mask) == (i & mask)) best = Math.Max(best, kinds[i]);
                        byte want = e == 0 ? EvtKind.KIND_NONE : best < 0 ? EvtKind.KIND_EVENT : (byte)best;
                        Yes(EvtKind.Kind((byte)e) == want, "event " + e + " mask " + mask);
                    }
                    Yes(EvtKind.dispatchCount == n, "one dispatch per list entry (distinct targets)");
                }
    }

    // COMPARE [event] #k; IF =; one instruction (a GOTO or GOSUB is followed)
    static void RunCompareIf()
    {
        Setup();
        var a = new Asm();
        a.Mark("stop"); a.Op(C_Ecl.EXIT_SCRIPT);
        a.Mark("main");
        foreach (int c in new[] { 5, 6, 7, 8, 9, 10 }) a.Op(C_Ecl.GOSUB, a.L("c" + c));
        a.Op(C_Ecl.EXIT_SCRIPT);
        // 5: the PRINT alone is the event's; the DAMAGE after it is not
        a.Mark("c5"); a.Op(C_Ecl.COMPARE, A(EV), I(5)); a.Op(C_Ecl.IF_EQ); a.Op(C_Ecl.PRINT, Str("HI"));
        a.Op(C_Ecl.DAMAGE, I(1), I(1), I(4), I(0), I(9)); a.Op(C_Ecl.RETURN);
        // 6: a GOTO is followed
        a.Mark("c6"); a.Op(C_Ecl.COMPARE, A(EV), I(6)); a.Op(C_Ecl.IF_EQ); a.Op(C_Ecl.JUMP, a.L("trap")); a.Op(C_Ecl.RETURN);
        // 7: the number first; a GOSUB is followed, and so is what comes after it
        a.Mark("c7"); a.Op(C_Ecl.COMPARE, I(7), A(EV)); a.Op(C_Ecl.IF_EQ); a.Op(C_Ecl.GOSUB, a.L("fight"));
        a.Op(C_Ecl.TREASURE, I(0), I(0), I(0), I(0), I(0), I(0), I(0), I(255)); a.Op(C_Ecl.RETURN);
        // 8: IF <> is not a dispatch
        a.Mark("c8"); a.Op(C_Ecl.COMPARE, A(EV), I(8)); a.Op(C_Ecl.IF_NE); a.Op(C_Ecl.NEWECL, I(2)); a.Op(C_Ecl.RETURN);
        // 9: a number word operand (type 2) compared: its low byte is the event
        a.Mark("c9"); a.Op(C_Ecl.COMPARE, A(EV), Wd(0x0109)); a.Op(C_Ecl.IF_EQ); a.Op(C_Ecl.RUN_PROGRAM, I(0)); a.Op(C_Ecl.RETURN);
        // 10: an IF = whose next instruction is an IF: op becomes the instruction after it (the source's quirk), a GOTO
        // here, so the single cut does not apply and both the GOTO and the code after it are followed
        a.Mark("c10"); a.Op(C_Ecl.COMPARE, A(EV), I(10)); a.Op(C_Ecl.IF_EQ); a.Op(C_Ecl.IF_GT); a.Op(C_Ecl.JUMP, a.L("leave"));
        a.Op(C_Ecl.RETURN);
        Body(a, "trap", EvtKind.KIND_TRAP);
        a.Mark("fight"); a.Op(C_Ecl.LOAD_MON, I(1), I(1), I(1)); a.Op(C_Ecl.RETURN);
        Body(a, "leave", EvtKind.KIND_EXIT);
        Prepare(a, "main");
        var want = new Dictionary<int, byte> { { 5, EvtKind.KIND_MESSAGE }, { 6, EvtKind.KIND_TRAP }, { 7, EvtKind.KIND_FIGHT },
            { 9, EvtKind.KIND_SERVICE }, { 10, EvtKind.KIND_EXIT } };
        for (int e = 0; e < 256; e++)
        {
            byte w = e == 0 ? EvtKind.KIND_NONE : want.ContainsKey(e) ? want[e] : EvtKind.KIND_EVENT;
            Yes(EvtKind.Kind((byte)e) == w, "compare event " + e + " got " + EvtKind.Kind((byte)e));
        }
        Yes(Enumerable.Range(1, EvtKind.dispatchCount).All(i => EvtKind.dispatch[i].single && EvtKind.dispatch[i].mask == 0xFF), "COMPARE dispatches are single, full mask");
    }

    // New Phlan: count a variable up from 0 until it equals the event, then ON GOTO on the count
    static void RunCountUp()
    {
        for (int n = 1; n <= 20; n++)
        {
            Setup();
            var kinds = Enumerable.Range(0, n).Select(i => AllKinds[(i * 3 + n) % AllKinds.Length]).ToArray();
            var a = new Asm();
            a.Mark("stop"); a.Op(C_Ecl.EXIT_SCRIPT);
            a.Mark("main");
            a.Op(C_Ecl.SAVE, I(0), A(0x4C00));
            a.Mark("loop");
            a.Op(C_Ecl.COMPARE, A(EV), A(0x4C00)); a.Op(C_Ecl.IF_EQ); a.Op(C_Ecl.JUMP, a.L("disp"));
            a.Op(C_Ecl.ADD, I(1), A(0x4C00), A(0x4C00));
            a.Op(C_Ecl.COMPARE, A(0x4C00), I(n)); a.Op(C_Ecl.IF_LT); a.Op(C_Ecl.JUMP, a.L("loop"));
            a.Op(C_Ecl.EXIT_SCRIPT);
            a.Mark("disp");
            a.Op(C_Ecl.ONGOTO, A(0x4C00), I(n)); a.Labels("t", n);
            for (int i = 0; i < n; i++) Body(a, "t" + i, kinds[i]);
            Prepare(a, "main");
            for (int e = 0; e < 256; e++)
            {
                byte w = e == 0 ? EvtKind.KIND_NONE : e < n ? kinds[e] : EvtKind.KIND_EVENT;
                Yes(EvtKind.Kind((byte)e) == w, "count-up event " + e + " n " + n);
            }
        }
    }

    // the training hall: a number added to or taken from the variable in place moves its offset
    static void RunOffsets()
    {
        foreach (int op in new[] { C_Ecl.SUBTRACT, C_Ecl.ADD })
            foreach (int s in new[] { 0, 1, 3, 10, 200, 255 })
                foreach (bool twice in new[] { false, true })
                {
                    Setup();
                    int n = 6;
                    var kinds = new[] { EvtKind.KIND_EXIT, EvtKind.KIND_TRAP, EvtKind.KIND_MESSAGE, EvtKind.KIND_FIGHT, EvtKind.KIND_TREASURE, EvtKind.KIND_SERVICE };
                    var a = new Asm();
                    a.Mark("stop"); a.Op(C_Ecl.EXIT_SCRIPT);
                    a.Mark("main");
                    a.Op(C_Ecl.BIT_AND, A(EV), I(0xFF), A(0x4C10));
                    a.Op(op, I(s), A(0x4C10), A(0x4C10));
                    if (twice) a.Op(op, I(s), A(0x4C10), A(0x4C10));
                    a.Op(C_Ecl.ONGOTO, A(0x4C10), I(n)); a.Labels("t", n);
                    for (int i = 0; i < n; i++) Body(a, "t" + i, kinds[i]);
                    Prepare(a, "main");
                    // SUBTRACT computes v - s, so list entry k is for event k + s; ADD for event k - s
                    int shift = (op == C_Ecl.SUBTRACT ? s : -s) * (twice ? 2 : 1);
                    for (int e = 0; e < 256; e++)
                    {
                        int k = ((e - shift) % 256 + 256) % 256;
                        byte w = e == 0 ? EvtKind.KIND_NONE : k < n ? kinds[k] : EvtKind.KIND_EVENT;
                        Yes(EvtKind.Kind((byte)e) == w, "offset " + op + " " + s + " event " + e);
                    }
                }
        // adding a variable (not a number) ends the holding
        Setup();
        var b = new Asm();
        b.Mark("stop"); b.Op(C_Ecl.EXIT_SCRIPT);
        b.Mark("main");
        b.Op(C_Ecl.BIT_AND, A(EV), I(0xFF), A(0x4C10));
        b.Op(C_Ecl.ADD, A(0x4C20), A(0x4C10), A(0x4C10));
        b.Op(C_Ecl.ONGOTO, A(0x4C10), I(1), b.L("t0")); b.Op(C_Ecl.EXIT_SCRIPT);
        Body(b, "t0", EvtKind.KIND_EXIT);
        Prepare(b, "main");
        Yes(EvtKind.dispatchCount == 0 && EvtKind.Kind(1) == EvtKind.KIND_EVENT, "ADD of a variable ends it");
    }

    // an instruction writing the variable ends its holding the event; a write elsewhere does not
    static void RunOverwrite()
    {
        var writers = new List<Action<Asm, int>>
        {
            (a, v) => a.Op(C_Ecl.SAVE, I(5), A(v)),
            (a, v) => a.Op(C_Ecl.RANDOM_NUMBER, I(5), A(v)),
            (a, v) => a.Op(C_Ecl.INPUT_NUM, I(0), A(v)),
            (a, v) => a.Op(C_Ecl.INPUT_STR, I(0), A(v)),
            (a, v) => a.Op(C_Ecl.DIVIDE, I(5), I(2), A(v)),
            (a, v) => a.Op(C_Ecl.MULTIPLY, I(5), I(2), A(v)),
            (a, v) => a.Op(C_Ecl.GETTABLE, A(0x4D00), I(2), A(v)),
            (a, v) => a.Op(C_Ecl.BIT_AND, I(5), I(2), A(v)),
            (a, v) => a.Op(C_Ecl.BIT_OR, I(5), I(2), A(v)),
            (a, v) => a.Op(C_Ecl.HMENU, A(v), I(1), Str("OK")),
            (a, v) => a.Op(C_Ecl.VMENU, A(v), Str("PICK"), I(2), Str("A"), Str("B")),
            (a, v) => a.Op(C_Ecl.SUBTRACT, A(0x4D02), A(v), A(v)),
        };
        for (int wi = 0; wi < writers.Count; wi++)
            foreach (bool same in new[] { true, false })
            {
                Setup();
                var a = new Asm();
                a.Mark("stop"); a.Op(C_Ecl.EXIT_SCRIPT);
                a.Mark("main");
                a.Op(C_Ecl.BIT_AND, A(EV), I(0x0F), A(0x4C30));
                writers[wi](a, same ? 0x4C30 : 0x4C40);
                a.Op(C_Ecl.ONGOTO, A(0x4C30), I(2)); a.Labels("t", 2);
                Body(a, "t0", EvtKind.KIND_TRAP); Body(a, "t1", EvtKind.KIND_MESSAGE);
                Prepare(a, "main");
                Yes(EvtKind.Kind(1) == (same ? EvtKind.KIND_EVENT : EvtKind.KIND_MESSAGE), "writer " + wi + " same " + same);
                Yes(EvtKind.Kind(16) == (same ? EvtKind.KIND_EVENT : EvtKind.KIND_TRAP), "writer " + wi + " masked");
            }
    }

    static void RunKindAt()
    {
        // the strongest kind wins within one event's code: exit, service, fight, treasure, trap, message
        byte[] order = { EvtKind.KIND_EXIT, EvtKind.KIND_SERVICE, EvtKind.KIND_FIGHT, EvtKind.KIND_TREASURE, EvtKind.KIND_TRAP, EvtKind.KIND_MESSAGE };
        for (int subset = 0; subset < 64; subset++)
        {
            Setup();
            var a = new Asm();
            a.Mark("t");
            for (int k = 0; k < 6; k++) if ((subset >> k & 1) != 0) a.Op(C_Ecl.GOSUB, a.L("b" + k));
            a.Op(C_Ecl.EXIT_SCRIPT);
            for (int k = 0; k < 6; k++) Body(a, "b" + k, order[k]);
            a.Load();
            byte want = EvtKind.KIND_NONE;
            for (int k = 5; k >= 0; k--) if ((subset >> k & 1) != 0) want = order[k];
            Yes(EvtKind.KindAt(a["t"], false) == want, "strongest of subset " + subset);
            Yes(EvtKind.KindAt(a["t"], true) == want, "a GOSUB is followed even when single");
        }
        // service outranks fight within one event's code, but across two matching dispatches Kind takes the larger
        // constant, so a fight (6) outranks a service (5) there, as in the source
        Setup();
        var c = new Asm();
        c.Mark("stop"); c.Op(C_Ecl.EXIT_SCRIPT);
        c.Mark("main");
        c.Op(C_Ecl.COMPARE, A(EV), I(3)); c.Op(C_Ecl.IF_EQ); c.Op(C_Ecl.JUMP, c.L("both"));
        c.Op(C_Ecl.BIT_AND, A(EV), I(0x01), A(0x4C50)); c.Op(C_Ecl.ONGOTO, A(0x4C50), I(2)); c.Labels("t", 2);
        c.Mark("both"); c.Op(C_Ecl.SAVE, I(1), A(C_Gbvm.ENTER_TEMPLE)); c.Op(C_Ecl.START_COMBAT); c.Op(C_Ecl.EXIT_SCRIPT);
        Body(c, "t0", EvtKind.KIND_MESSAGE); Body(c, "t1", EvtKind.KIND_FIGHT);
        Prepare(c, "main");
        Yes(EvtKind.KindAt(c["both"], false) == EvtKind.KIND_SERVICE, "service before fight in one code path");
        Yes(EvtKind.Kind(3) == EvtKind.KIND_FIGHT, "fight (6) beats service (5) across dispatches");
        Yes(EvtKind.Kind(5) == EvtKind.KIND_FIGHT && EvtKind.Kind(2) == EvtKind.KIND_MESSAGE, "mask-1 dispatch");
        // SAVE flags: only an address operand naming the shop or temple flag counts
        foreach (var t in new[] { Tuple.Create(A(C_Gbvm.ENTER_SHOP), true), Tuple.Create(A(C_Gbvm.ENTER_TEMPLE), true),
                                  Tuple.Create(Wd(C_Gbvm.ENTER_SHOP), false), Tuple.Create(A(0x6E6D), false) })
        {
            Setup(); var s = new Asm(); s.Mark("t"); s.Op(C_Ecl.SAVE, I(1), t.Item1); s.Op(C_Ecl.EXIT_SCRIPT); s.Load();
            Yes(EvtKind.KindAt(s["t"], false) == (t.Item2 ? EvtKind.KIND_SERVICE : EvtKind.KIND_NONE), "SAVE flag");
        }
        // single: only the first instruction, unless it is a GOTO or GOSUB
        foreach (int first in new[] { C_Ecl.PRINT_RET, C_Ecl.CLEAR_BOX })
        {
            Setup(); var s = new Asm(); s.Mark("t"); s.Op(first); s.Op(C_Ecl.NEWECL, I(1)); s.Load();
            Yes(EvtKind.KindAt(s["t"], true) == (first == C_Ecl.PRINT_RET ? EvtKind.KIND_MESSAGE : EvtKind.KIND_NONE), "single cut");
            Yes(EvtKind.KindAt(s["t"], false) == EvtKind.KIND_EXIT, "not single follows on");
        }
        // both ways past an IF, every ON GOTO address, ENCOUNTER, PRINT RETURN
        Setup();
        var d = new Asm(); d.Mark("t");
        d.Op(C_Ecl.COMPARE, A(0x4B00), I(1)); d.Op(C_Ecl.IF_EQ); d.Op(C_Ecl.NEWECL, I(2)); d.Op(C_Ecl.EXIT_SCRIPT);
        d.Mark("u"); d.Op(C_Ecl.ONGOTO, A(0x4B00), I(3)); d.Labels("t", 3);
        Body(d, "t0", EvtKind.KIND_MESSAGE); Body(d, "t1", EvtKind.KIND_NONE);
        d.Mark("t2"); d.Op(C_Ecl.ENCOUNTER, I(1), I(1), I(1), A(0x4B00), I(0), I(0), I(0), I(0), I(0), Str(""), Str(""), Str(""), I(0), I(0)); d.Op(C_Ecl.EXIT_SCRIPT);
        d.Load();
        Yes(EvtKind.KindAt(d["t"], false) == EvtKind.KIND_EXIT, "IF taken both ways");
        Yes(EvtKind.KindAt(d["u"], false) == EvtKind.KIND_FIGHT, "every ON GOTO address, ENCOUNTER is a fight");
        // at most 160 instructions are followed
        foreach (int fill in new[] { 158, 159, 160, 200 })
        {
            Setup(); var f = new Asm(); f.Mark("t");
            for (int i = 0; i < fill; i++) f.Op(C_Ecl.CLEAR_BOX);
            f.Op(C_Ecl.NEWECL, I(1)); f.Load();
            Yes(EvtKind.KindAt(f["t"], false) == (fill + 1 <= 160 ? EvtKind.KIND_EXIT : EvtKind.KIND_NONE), "MAX_FOLLOW " + fill);
        }
        // a loop is followed once
        Setup(); var g = new Asm(); g.Mark("t"); g.Op(C_Ecl.PRINT_RET); g.Op(C_Ecl.JUMP, g.L("t")); g.Load();
        Yes(EvtKind.KindAt(g["t"], false) == EvtKind.KIND_MESSAGE, "loops terminate");
    }

    // Prepare walks the control flow from the entry points
    static void RunWalk()
    {
        // dispatch code reached through GOSUB from entry 3, an ON GOSUB list, the skipped side of an IF and a GOTO;
        // code reached by nothing is not read
        Setup();
        var a = new Asm();
        a.Mark("stop"); a.Op(C_Ecl.EXIT_SCRIPT);
        a.Mark("e3"); a.Op(C_Ecl.GOSUB, a.L("sub")); a.Op(C_Ecl.EXIT_SCRIPT);
        a.Mark("e5"); a.Op(C_Ecl.ONGOSUB, A(0x4B00), I(1)); a.Labels("t", 1);
        a.Op(C_Ecl.COMPARE, A(0x4B00), I(1)); a.Op(C_Ecl.IF_EQ); a.Op(C_Ecl.JUMP, a.L("far")); a.Op(C_Ecl.RETURN);
        a.Mark("sub"); a.Op(C_Ecl.COMPARE, A(EV), I(11)); a.Op(C_Ecl.IF_EQ); a.Op(C_Ecl.JUMP, a.L("k11")); a.Op(C_Ecl.RETURN);
        a.Mark("t0"); a.Op(C_Ecl.COMPARE, A(EV), I(12)); a.Op(C_Ecl.IF_EQ); a.Op(C_Ecl.JUMP, a.L("k12")); a.Op(C_Ecl.RETURN);
        a.Mark("far"); a.Op(C_Ecl.COMPARE, A(EV), I(13)); a.Op(C_Ecl.IF_EQ); a.Op(C_Ecl.JUMP, a.L("k13")); a.Op(C_Ecl.EXIT_SCRIPT);
        a.Mark("dead"); a.Op(C_Ecl.COMPARE, A(EV), I(14)); a.Op(C_Ecl.IF_EQ); a.Op(C_Ecl.JUMP, a.L("k14")); a.Op(C_Ecl.EXIT_SCRIPT);
        a.Mark("afternew"); a.Op(C_Ecl.NEWECL, I(3)); a.Op(C_Ecl.COMPARE, A(EV), I(15)); a.Op(C_Ecl.IF_EQ); a.Op(C_Ecl.JUMP, a.L("k14")); a.Op(C_Ecl.EXIT_SCRIPT);
        Body(a, "k11", EvtKind.KIND_TREASURE); Body(a, "k12", EvtKind.KIND_TRAP); Body(a, "k13", EvtKind.KIND_EXIT); Body(a, "k14", EvtKind.KIND_FIGHT);
        a.Load();
        for (int i = 1; i <= 5; i++) GDVars.scriptEntryAddrs[i] = a["stop"];
        GDVars.scriptEntryAddrs[3] = a["e3"]; GDVars.scriptEntryAddrs[5] = a["e5"]; GDVars.scriptEntryAddrs[1] = a["afternew"];
        EvtKind.Prepare();
        Yes(EvtKind.Kind(11) == EvtKind.KIND_TREASURE, "through GOSUB from entry 3");
        Yes(EvtKind.Kind(12) == EvtKind.KIND_TRAP, "through ON GOSUB");
        Yes(EvtKind.Kind(13) == EvtKind.KIND_EXIT, "through an IF's GOTO");
        Yes(EvtKind.Kind(14) == EvtKind.KIND_EVENT, "unreached code is not read");
        Yes(EvtKind.Kind(15) == EvtKind.KIND_EVENT, "NEWECL ends a path");
        // Prepare starts afresh
        Setup();
        var b = new Asm(); b.Mark("stop"); b.Op(C_Ecl.EXIT_SCRIPT); b.Load();
        Entries(b);
        EvtKind.Prepare();
        Yes(EvtKind.dispatchCount == 0 && EvtKind.Kind(11) == EvtKind.KIND_EVENT && EvtKind.Kind(0) == EvtKind.KIND_NONE, "Prepare resets");
        // a dispatch found by two scans is kept once: the scan from the first COMPARE runs on into the second
        Setup();
        var c = new Asm(); c.Mark("stop"); c.Op(C_Ecl.EXIT_SCRIPT); c.Mark("main");
        c.Op(C_Ecl.COMPARE, A(EV), I(3)); c.Op(C_Ecl.IF_EQ); c.Op(C_Ecl.JUMP, c.L("k3"));
        c.Op(C_Ecl.COMPARE, A(EV), I(4)); c.Op(C_Ecl.IF_EQ); c.Op(C_Ecl.JUMP, c.L("k4")); c.Op(C_Ecl.EXIT_SCRIPT);
        Body(c, "k3", EvtKind.KIND_TRAP); Body(c, "k4", EvtKind.KIND_TREASURE);
        Prepare(c, "main");
        Yes(EvtKind.dispatchCount == 2 && EvtKind.Kind(3) == EvtKind.KIND_TRAP && EvtKind.Kind(4) == EvtKind.KIND_TREASURE, "dispatches kept once");
    }

    static void RunLimits()
    {
        // MAX_DISPATCH: two 64-entry lists, mask $7F then $FF; only the first 96 dispatches are kept
        Setup();
        var a = new Asm();
        a.Mark("stop"); a.Op(C_Ecl.EXIT_SCRIPT);
        a.Mark("main");
        a.Op(C_Ecl.BIT_AND, A(EV), I(0x7F), A(0x4C60));
        a.Op(C_Ecl.ONGOSUB, A(0x4C60), I(64)); a.Labels("t", 64);
        a.Op(C_Ecl.BIT_AND, A(EV), I(0xFF), A(0x4C62));
        a.Op(C_Ecl.ONGOTO, A(0x4C62), I(64));for (int i = 0; i < 64; i++) a.Bytes.AddRange(new byte[] { 2, 0, 0 });
        int listAt = a.Bytes.Count - 64 * 3;
        for (int i = 0; i < 64; i++) Body(a, "t" + i, i % 2 == 0 ? EvtKind.KIND_MESSAGE : EvtKind.KIND_TRAP);
        var uAddr = new ushort[64];
        for (int i = 0; i < 64; i++) { uAddr[i] = a.Here; Body(a, "u" + i, EvtKind.KIND_EXIT); }
        for (int i = 0; i < 64; i++) { a.Bytes[listAt + i * 3 + 1] = (byte)(uAddr[i] & 0xFF); a.Bytes[listAt + i * 3 + 2] = (byte)(uAddr[i] >> 8); }
        Prepare(a, "main");
        Yes(EvtKind.dispatchCount == 96, "96 dispatches kept");
        for (int e = 1; e < 256; e++)
        {
            int k = e & 0x7F;
            byte want = k >= 64 ? EvtKind.KIND_EVENT : (e < 32 ? EvtKind.KIND_EXIT : (k % 2 == 0 ? EvtKind.KIND_MESSAGE : EvtKind.KIND_TRAP));
            Yes(EvtKind.Kind((byte)e) == want, "dispatch cap event " + e);
        }
        // an ON GOTO count above 64: only the 64 kept addresses (DEVIATION from reading past dsGbvm*)
        Setup();
        var b = new Asm(); b.Mark("stop"); b.Op(C_Ecl.EXIT_SCRIPT); b.Mark("main");
        b.Op(C_Ecl.BIT_AND, A(EV), I(0xFF), A(0x4C70));
        b.Op(C_Ecl.ONGOTO, A(0x4C70), I(70)); b.Labels("t", 70);
        for (int i = 0; i < 70; i++) Body(b, "t" + i, EvtKind.KIND_TRAP);
        Prepare(b, "main");
        Yes(EvtKind.Kind(63) == EvtKind.KIND_TRAP && EvtKind.Kind(64) == EvtKind.KIND_EVENT && EvtKind.dispatchCount == 64, "list cut at 64");
        // MAX_SCAN: the scan from one read of the event byte follows at most 256 instructions
        foreach (int fill in new[] { 254, 255, 256 })
        {
            Setup();
            var c = new Asm(); c.Mark("stop"); c.Op(C_Ecl.EXIT_SCRIPT); c.Mark("main");
            c.Op(C_Ecl.BIT_AND, A(EV), I(0xFF), A(0x4C80));
            for (int i = 0; i < fill; i++) c.Op(C_Ecl.CLEAR_BOX);
            c.Op(C_Ecl.ONGOTO, A(0x4C80), I(2)); c.Labels("t", 2);
            Body(c, "t0", EvtKind.KIND_TRAP); Body(c, "t1", EvtKind.KIND_TRAP);
            Prepare(c, "main");
            Yes(EvtKind.Kind(1) == (fill + 1 <= 256 ? EvtKind.KIND_TRAP : EvtKind.KIND_EVENT), "MAX_SCAN " + fill);
        }
    }

    // random ON GOTO dispatches with offsets, against a model: the largest kind of every matching entry
    static void RunRandom()
    {
        for (int t = 0; t < 400; t++)
        {
            Setup();
            int mask = new[] { 0xFF, 0x7F, 0x3F, 0x1F, 0x0F, 0xF0, 0x3C, 0x81 }[rng.Next(8)];
            int n = 1 + rng.Next(30), s = rng.Next(256);
            int op = rng.Next(3);
            var kinds = Enumerable.Range(0, n).Select(i => AllKinds[rng.Next(AllKinds.Length)]).ToArray();
            int distinct = 1 + rng.Next(n);
            var target = Enumerable.Range(0, n).Select(i => rng.Next(distinct)).ToArray();
            var a = new Asm();
            a.Mark("stop"); a.Op(C_Ecl.EXIT_SCRIPT); a.Mark("main");
            a.Op(C_Ecl.BIT_AND, A(EV), I(mask), A(0x4C90));
            if (op == 1) a.Op(C_Ecl.SUBTRACT, I(s), A(0x4C90), A(0x4C90));
            if (op == 2) a.Op(C_Ecl.ADD, I(s), A(0x4C90), A(0x4C90));
            a.Op(C_Ecl.ONGOTO, A(0x4C90), I(n));
            int listAt = a.Bytes.Count;
            for (int i = 0; i < n; i++) a.Bytes.AddRange(new byte[] { 2, 0, 0 });
            var addr = new ushort[distinct];
            for (int j = 0; j < distinct; j++) { addr[j] = a.Here; Body(a, "d" + j, kinds[j]); }
            for (int i = 0; i < n; i++) { a.Bytes[listAt + i * 3 + 1] = (byte)(addr[target[i]] & 0xFF); a.Bytes[listAt + i * 3 + 2] = (byte)(addr[target[i]] >> 8); }
            a.Mark("x");
            Prepare(a, "main");
            int offset = op == 0 ? 0 : op == 1 ? s : -s;
            for (int e = 0; e < 256; e++)
            {
                int best = -1;
                for (int i = 0; i < n; i++)
                    if ((e & mask) == (((i + offset) % 256 + 256) % 256 & mask)) best = Math.Max(best, kinds[target[i]]);
                byte want = e == 0 ? EvtKind.KIND_NONE : best < 0 ? EvtKind.KIND_EVENT : (byte)best;
                Yes(EvtKind.Kind((byte)e) == want, "random dispatch " + t + " event " + e);
            }
        }
    }

    static void RunUnbound()
    {
        var a = new Asm(); a.Mark("stop"); a.Op(C_Ecl.EXIT_SCRIPT); a.Load(); Entries(a);
        EvtKind.EclVarsUnit = null;
        Yes(EvtKind.Kind(0) == EvtKind.KIND_NONE, "event 0 needs nothing");
        string msg = null;
        try { EvtKind.Prepare(); } catch (NotSupportedException e) { msg = e.Message; }
        Yes(msg != null && msg.Contains("EclVars"), "unbound EclVars fails clearly");
    }
}
