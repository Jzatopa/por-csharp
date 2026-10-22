// Source-derived checks for the EvtKind conversion (evtkind.pas at 7616920). Scripts are assembled as ECL bytecode
// and read through a test EclVars whose LoadEclVars/SkipEclCmd restate eclvars.pas (string operands consume their
// packed bytes; their text is not decoded, which EvtKind never reads). Expected kinds are written by hand from the
// patterns evtkind.pas documents, or computed by a separate model of the dispatch.
using System;
using System.Collections.Generic;
using System.Linq;
using Por.Core;

class TestVars : IEclVarsUnit
{
    public int Skips;
    public void LoadEclVars(byte count)
    {
        for (int i = 1; i <= count; i++)
        {
            byte t = GDVars.zone3[GDVars.eclIp + 1];
            byte lo = GDVars.zone3[GDVars.eclIp + 2];
            byte hi = 0;
            GDVars.eclIp = (ushort)(GDVars.eclIp + 2);
            if (t == 1 || t == 2 || t == 3 || t == 0x81) { GDVars.eclIp++; hi = GDVars.zone3[GDVars.eclIp]; }
            else if (t == 0x80) GDVars.eclIp = (ushort)(GDVars.eclIp + lo);
            if (i <= 64)
            {
                GDVars.dsGbvmType[i] = t;
                GDVars.dsGbvmLowerByte[i] = lo;
                if (t == 1 || t == 2 || t == 3 || t == 0x81) GDVars.dsGbvmUpperByte[i] = hi;
            }
        }
        GDVars.eclIp++;
    }
    public ushort CombineBytesToWord(byte lowByte, byte highByte) { return (ushort)(lowByte + highByte * 256); }
    public ushort GetEclVar(sbyte idx)
    {
        byte lo = GDVars.dsGbvmLowerByte[idx], hi = GDVars.dsGbvmUpperByte[idx];
        switch (GDVars.dsGbvmType[idx])
        {
            case 0: return lo;
            case 1: case 3: return 0; // a GBVM read; never a menu or ON GOTO count in these scripts
            case 0x80: return 0;
            default: return CombineBytesToWord(lo, hi);
        }
    }
    public void WriteGbvm(ushort address, ushort value) { throw new InvalidOperationException("EvtKind must not write"); }
    public void WriteGbvmString(ushort address, string s) { throw new InvalidOperationException("EvtKind must not write"); }
    public ushort ReadGbvm(ushort address) { return 0; }
    public void SkipEclCmd()
    {
        Skips++;
        byte op = GDVars.curOpcode = GDVars.zone3[GDVars.eclIp];
        switch (op)
        {
            case C_Ecl.JUMP: case C_Ecl.GOSUB: case C_Ecl.LOAD_CHAR: case C_Ecl.PICTURE: case C_Ecl.PRINT:
            case C_Ecl.PRINTCLEAR: case C_Ecl.PARTY_STR: case C_Ecl.NEWECL: case C_Ecl.CALL: case C_Ecl.FIND_ITEM:
            case C_Ecl.CLOCK: case C_Ecl.RUN_PROGRAM: case C_Ecl.WHO: case C_Ecl.PROTECTION:
                LoadEclVars(1); break;
            case C_Ecl.COMPARE: case C_Ecl.RANDOM_NUMBER: case C_Ecl.SAVE: case C_Ecl.INPUT_NUM: case C_Ecl.INPUT_STR:
            case 0x1F: case C_Ecl.PARTY_SURPR: case C_Ecl.ADD_NPC:
                LoadEclVars(2); break;
            case C_Ecl.ADD: case C_Ecl.SUBTRACT: case C_Ecl.DIVIDE: case C_Ecl.MULTIPLY: case C_Ecl.LOAD_MON:
            case C_Ecl.LOADFILES: case C_Ecl.ROB: case C_Ecl.GETTABLE: case C_Ecl.BIT_AND: case C_Ecl.BIT_OR:
            case C_Ecl.SAVE_TABLE: case C_Ecl.LOADPIECES: case C_Ecl.SPELL: case C_Ecl.SETUP_MON:
                LoadEclVars(3); break;
            case C_Ecl.VMENU:
                { LoadEclVars(3); byte c = (byte)GetEclVar(3); GDVars.eclIp--; LoadEclVars(c); break; }
            case C_Ecl.HMENU: case C_Ecl.ONGOTO: case C_Ecl.ONGOSUB:
                { LoadEclVars(2); byte c = (byte)GetEclVar(2); GDVars.eclIp--; LoadEclVars(c); break; }
            case C_Ecl.COMPARE_AND: case C_Ecl.SURPRISE: LoadEclVars(4); break;
            case C_Ecl.DAMAGE: LoadEclVars(5); break;
            case C_Ecl.CHECK_PARTY: case C_Ecl.PARLAY: LoadEclVars(6); break;
            case C_Ecl.TREASURE: LoadEclVars(8); break;
            case C_Ecl.ENCOUNTER: LoadEclVars(14); break;
            default: GDVars.eclIp++; break;
        }
    }
}

class Asm
{
    public readonly List<byte> Bytes = new List<byte>();
    public const ushort Base = 0x9900;
    public ushort Here { get { return (ushort)(Base + Bytes.Count); } }
    readonly Dictionary<string, ushort> labels = new Dictionary<string, ushort>();
    readonly List<Tuple<int, string>> fixups = new List<Tuple<int, string>>();
    public static byte[] I(int v) { return new byte[] { 0, (byte)v }; }
    public static byte[] Wd(int w) { return new byte[] { 2, (byte)(w & 0xFF), (byte)(w >> 8) }; }
    public static byte[] A(int addr) { return new byte[] { 1, (byte)(addr & 0xFF), (byte)(addr >> 8) }; }
    public static byte[] Str(string s) { var b = new List<byte> { 0x80, (byte)s.Length }; b.AddRange(s.Select(c => (byte)c)); return b.ToArray(); }
    public byte[] L(string label) { return new byte[] { 2, 0xEE, 0xEE, (byte)label.Length }.Concat(label.Select(c => (byte)c)).ToArray(); }
    public void Mark(string label) { labels[label] = Here; }
    // an ON GOTO/ON GOSUB address list: n word operands prefix0..prefix(n-1)
    public void Labels(string prefix, int n)
    {
        for (int i = 0; i < n; i++) { fixups.Add(Tuple.Create(Bytes.Count + 1, prefix + i)); Bytes.AddRange(new byte[] { 2, 0, 0 }); }
    }
    public void Op(int op, params byte[][] operands)
    {
        Bytes.Add((byte)op);
        foreach (var o in operands)
        {
            if (o.Length > 3 && o[0] == 2 && o[1] == 0xEE && o[2] == 0xEE)
            {
                string name = new string(o.Skip(4).Select(c => (char)c).ToArray());
                fixups.Add(Tuple.Create(Bytes.Count + 1, name));
                Bytes.AddRange(new byte[] { 2, 0, 0 });
            }
            else Bytes.AddRange(o);
        }
    }
    public ushort this[string label] { get { return labels[label]; } }
    public void Load()
    {
        foreach (var f in fixups) { ushort a = labels[f.Item2]; Bytes[f.Item1] = (byte)(a & 0xFF); Bytes[f.Item1 + 1] = (byte)(a >> 8); }
        GDVars.zone3 = new Types.TZone3();
        for (int i = 0; i < Bytes.Count; i++) GDVars.zone3[Base + i] = Bytes[i];
    }
}

static partial class Tests
{
    static long checks;
    static TestVars vars;
    static Random rng = new Random(533);
    const int EV = C_Gbvm.CELL_EVENT;

    static void Yes(bool v, string what) { checks++; if (!v) throw new Exception("FAILED: " + what); }

    // --real: decode through the converted EclVars (EclBindings) instead of the restated test double
    static bool real;

    static void Setup()
    {
        vars = new TestVars();
        if (real)
        {
            EclBindings.BindConvertedUnits();
            EvtKind.EclVarsUnit = EclVm.Units.EclVars;
            GDVars.gbStrings = new Types.TGbStrings();
        }
        else EvtKind.EclVarsUnit = vars;
        GDVars.zone3 = new Types.TZone3();
        for (int i = 0; i < 6; i++) GDVars.scriptEntryAddrs[i] = 0;
        GDVars.eclIp = 0x1234;
        GDVars.curOpcode = 0x77;
    }

    // entry addresses: unused entries point at the first EXIT
    static void Entries(Asm a, params string[] labels)
    {
        for (int i = 1; i <= 5; i++) GDVars.scriptEntryAddrs[i] = a["stop"];
        for (int i = 0; i < labels.Length; i++) GDVars.scriptEntryAddrs[i + 1] = a[labels[i]];
    }

    static void Prepare(Asm a, params string[] entries)
    {
        a.Load();
        Entries(a, entries);
        EvtKind.Prepare();
        Yes(GDVars.eclIp == 0x1234 && GDVars.curOpcode == 0x77, "Prepare restores the IP and opcode");
    }

    // A block of code doing one kind of thing, ending in EXIT.
    static void Body(Asm a, string label, byte kind)
    {
        a.Mark(label);
        switch (kind)
        {
            case EvtKind.KIND_EXIT: a.Op(C_Ecl.PRINT, Asm.Str("BYE")); a.Op(C_Ecl.NEWECL, Asm.I(4)); break;
            case EvtKind.KIND_SERVICE: a.Op(C_Ecl.SAVE, Asm.I(1), Asm.A(C_Gbvm.ENTER_SHOP)); a.Op(C_Ecl.START_COMBAT); break;
            case EvtKind.KIND_FIGHT: a.Op(C_Ecl.LOAD_MON, Asm.I(3), Asm.I(2), Asm.I(5)); a.Op(C_Ecl.START_COMBAT); break;
            case EvtKind.KIND_TREASURE: a.Op(C_Ecl.TREASURE, Asm.I(0), Asm.I(0), Asm.I(0), Asm.I(5), Asm.I(0), Asm.I(0), Asm.I(0), Asm.I(255)); break;
            case EvtKind.KIND_TRAP: a.Op(C_Ecl.DAMAGE, Asm.I(1), Asm.I(2), Asm.I(6), Asm.I(0), Asm.I(10)); break;
            case EvtKind.KIND_MESSAGE: a.Op(C_Ecl.PRINTCLEAR, Asm.Str("A SIGN")); break;
            default: a.Op(C_Ecl.SAVE, Asm.I(0), Asm.A(0x4B00)); break;
        }
        a.Op(C_Ecl.EXIT_SCRIPT);
    }

    static int Main(string[] args)
    {
        var all = new List<KeyValuePair<string, Action>>
        {
            new KeyValuePair<string, Action>("OnGotoMask", RunOnGotoMask),
            new KeyValuePair<string, Action>("CompareIf", RunCompareIf),
            new KeyValuePair<string, Action>("CountUp", RunCountUp),
            new KeyValuePair<string, Action>("Offsets", RunOffsets),
            new KeyValuePair<string, Action>("Overwrite", RunOverwrite),
            new KeyValuePair<string, Action>("KindAt", RunKindAt),
            new KeyValuePair<string, Action>("Walk", RunWalk),
            new KeyValuePair<string, Action>("Limits", RunLimits),
            new KeyValuePair<string, Action>("Random", RunRandom),
            new KeyValuePair<string, Action>("Unbound", RunUnbound),
        };
        int failed = 0;
        real = args.Contains("--real");
        args = args.Where(x => x != "--real").ToArray();
        if (real) Console.WriteLine("decoding through the converted EclVars");
        foreach (var t in all)
        {
            if (args.Length > 0 && !args.Contains(t.Key)) continue;
            long before = checks;
            try { Setup(); t.Value(); Console.WriteLine("PASS " + t.Key + " (" + (checks - before) + " assertions)"); }
            catch (Exception e) { failed++; Console.WriteLine("FAIL " + t.Key + ": " + e); }
        }
        Console.WriteLine("EvtKind source-derived assertions: " + checks + ", failed groups: " + failed);
        return failed == 0 ? 0 : 1;
    }
}
