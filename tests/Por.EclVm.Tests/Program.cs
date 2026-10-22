// Source-derived checks for the EclVm conversion (eclvm.pas at 7616920). Expected values are computed by separate
// models written from the Pascal source in this file, not by calling the production routines' helpers.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Por.Core;

class Halted : Exception { }

class TestIo : IGameIoHost
{
    public Queue<byte> Keys = new Queue<byte>();
    public bool KeyPressed { get { return Keys.Count > 0; } }
    public byte ReadKey() { return Keys.Count > 0 ? Keys.Dequeue() : (byte)13; }
    public void SoundCommand(ushort c) { }
    public void SoundSuspend() { }
    public void Interrupt(byte n, Types.TInterruptRegisters r) { }
    public void Halt(int c) { throw new Halted(); }
    public void WriteLine(string m) { }
    public TextWriter OpenDebugLog(string p) { throw new NotSupportedException(); }
}

class DaxHost : DaxIo.IDaxPlatform
{
    public string Dir;
    public List<string> Messages = new List<string>();
    public void WriteChar(byte col, byte row, byte attr, byte count, byte ch) { }
    public void BiosWriteChar(byte page, byte col, byte row, byte attr, byte count, byte ch) { }
    public byte ReadGameKey() { return 13; }
    public void ExitGame() { throw new Halted(); }
    public void OverlayMessage(string text) { Messages.Add(text); }
    public string CurrentDirectory { get { return Dir; } }
    public string FullPath(string path) { return Path.GetFullPath(path); }
}

class Asm
{
    public readonly List<byte> Bytes = new List<byte>();
    public const ushort Base = 0x9900;
    public ushort Here { get { return (ushort)(Base + Bytes.Count); } }
    public static byte[] I(int v) { return new byte[] { 0, (byte)v }; }
    public static byte[] Wd(int w) { return new byte[] { 2, (byte)(w & 0xFF), (byte)(w >> 8) }; }
    public static byte[] A(int addr) { return new byte[] { 1, (byte)(addr & 0xFF), (byte)(addr >> 8) }; }
    public static byte[] S(int idx) { return new byte[] { 0x80, (byte)idx }; }
    public int Op(int op, params byte[][] operands)
    {
        int at = Bytes.Count;
        Bytes.Add((byte)op);
        foreach (var o in operands) Bytes.AddRange(o);
        return at;
    }
    public void PatchWord(int at, ushort w) { Bytes[at] = (byte)(w & 0xFF); Bytes[at + 1] = (byte)(w >> 8); }
    public void Load()
    {
        GDVars.zone3 = new Types.TZone3();
        for (int i = 0; i < Bytes.Count; i++) GDVars.zone3[Base + i] = Bytes[i];
    }
}

static partial class Tests
{
    static long checks;
    static Fake fake;
    static List<string> draw;
    static Queue<int> rolls = new Queue<int>();
    static List<int> randomCalls = new List<int>();
    static TestIo io = new TestIo();
    static Random rng = new Random(20261021);

    static void Yes(bool v, string what) { checks++; if (!v) throw new Exception("FAILED: " + what); }
    static byte B(int x) { return unchecked((byte)x); }
    static ushort W(int x) { return unchecked((ushort)x); }

    static void Setup()
    {
        fake = new Fake();
        fake.Bind();
        EclVm.ResetUnitState();
        GDVars.zone0 = new Types.TGbvmZone0State();
        GDVars.zone1 = new Types.TGbvmZone1();
        GDVars.zone3 = new Types.TZone3();
        GDVars.gbStrings = new Types.TGbStrings();
        GDVars.gameFont = new Types.TFontArr();
        GDVars.creatureList = null;
        GDVars.curChar = null;
        GDVars.treasureItems = null;
        GDVars.dsEclStackTopPtr = null;
        GDVars.dsEclStackSecondPtr = null;
        GDVars.partyDefeated = false;
        GDVars.debugTraceEnabled = false;
        GDVars.curRow = 0; GDVars.curCol = 0;
        GDVars.dsEcl3dClearFlag = 0; GDVars.protTextShown = 0; GDVars.protNewLine = 0;
        GDVars.eclGraphics = false; GDVars.sprShown = false; GDVars.picDrawn = false; GDVars.duelFlag = false;
        GDVars.gIconAnim = 8;
        GDVars.picRegionW = false; GDVars.movedParty = false; GDVars.redrawSky = false;
        GDVars.gLocType = 0; GDVars.locTypePrev = 0; GDVars.recentSaved = 7;
        GDVars.inEncMenu = 0; GDVars.msgDelayFlag = 0;
        if (GDVars.map3d == null) GDVars.map3d = new Types.TGeoMap();
        io.Keys.Clear();
        GDVars.gameDelay = 0;
        for (int i = 0; i < 6; i++) GDVars.cmpFlags[i] = false;
        draw = new List<string>();
        GDVars.video.DrawGlyph = (x, y, c, g) => draw.Add("g:" + x + ":" + y + ":" + c + ":" + g);
        GDVars.video.ClearRect = (l, t, r, b) => draw.Add("c:" + l + ":" + t + ":" + r + ":" + b);
        TextRend.DelayMilliseconds = ms => draw.Add("delay:" + ms);
        TextRend.ReadBiosTicks = () => 0;
        GameIo.Host = io;
        rolls.Clear();
        randomCalls.Clear();
        Dice.RandomBelow = n => { randomCalls.Add(n); return rolls.Count > 0 ? rolls.Dequeue() : 0; };
    }

    static Types.TCharRecord Member(string name)
    {
        return new Types.TCharRecord { name = name };
    }

    static Types.TCharRecord Party(params Types.TCharRecord[] members)
    {
        for (int i = 0; i + 1 < members.Length; i++) members[i].nextCharAddr = members[i + 1];
        GDVars.creatureList = members.Length > 0 ? members[0] : null;
        GDVars.zone1[C_Gbvm.PARTY_SIZE] = (ushort)members.Length;
        return GDVars.creatureList;
    }

    // run the single command at Asm.Base
    static void Exec(Asm a)
    {
        a.Load();
        GDVars.eclIp = Asm.Base;
        GDVars.curOpcode = GDVars.zone3[Asm.Base];
        EclVm.ExecNextEclCmd();
    }

    static int Main(string[] args)
    {
        var all = new List<KeyValuePair<string, Action>>
        {
            new KeyValuePair<string, Action>("Arith", RunArith),
            new KeyValuePair<string, Action>("AndOr", RunAndOr),
            new KeyValuePair<string, Action>("Random", RunRandom),
            new KeyValuePair<string, Action>("ScriptFlow", RunScriptFlow),
            new KeyValuePair<string, Action>("IfFlags", RunIfFlags),
            new KeyValuePair<string, Action>("OnGoto", RunOnGoto),
            new KeyValuePair<string, Action>("Tables", RunTables),
            new KeyValuePair<string, Action>("PartyStr", RunPartyStr),
            new KeyValuePair<string, Action>("CheckParty", RunCheckParty),
            new KeyValuePair<string, Action>("Surprise", RunSurprise),
            new KeyValuePair<string, Action>("TreasureTable", RunTreasureTable),
            new KeyValuePair<string, Action>("TreasureFile", RunTreasureFile),
            new KeyValuePair<string, Action>("Encounter", RunEncounter),
            new KeyValuePair<string, Action>("Parlay", RunParlay),
            new KeyValuePair<string, Action>("HMenu", RunHMenu),
            new KeyValuePair<string, Action>("VMenu", RunVMenu),
            new KeyValuePair<string, Action>("LoadMon", RunLoadMon),
            new KeyValuePair<string, Action>("CopyChar", RunCopyChar),
            new KeyValuePair<string, Action>("LoadChar", RunLoadChar),
            new KeyValuePair<string, Action>("Damage", RunDamage),
            new KeyValuePair<string, Action>("Spell", RunSpell),
            new KeyValuePair<string, Action>("Misc", RunMisc),
            new KeyValuePair<string, Action>("Call", RunCall),
            new KeyValuePair<string, Action>("Protection", RunProtection),
            new KeyValuePair<string, Action>("Combat", RunCombat),
            new KeyValuePair<string, Action>("Program", RunProgram),
            new KeyValuePair<string, Action>("GameLoop", RunGameLoop),
            new KeyValuePair<string, Action>("NewEclDone", RunNewEclDone),
            new KeyValuePair<string, Action>("Unbound", RunUnbound),
        };
        string dir = Path.Combine(Path.GetTempPath(), "por-eclvm-" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        GDVars.gameDir = dir + Path.DirectorySeparatorChar;
        GDVars.virtualDisk = 1;
        GDVars.dsSuppressingInsertDiskFlag = true;
        DaxIo.Platform = new DaxHost { Dir = dir };
        int failed = 0;
        try
        {
            foreach (var t in all)
            {
                if (args.Length > 0 && !args.Contains(t.Key)) continue;
                long before = checks;
                try
                {
                    Setup();
                    t.Value();
                    Console.WriteLine("PASS " + t.Key + " (" + (checks - before) + " assertions)");
                }
                catch (Exception e)
                {
                    failed++;
                    Console.WriteLine("FAIL " + t.Key + ": " + e);
                }
            }
        }
        finally { DaxIo.ResetArchiveCache(); Directory.Delete(dir, true); }
        Console.WriteLine("EclVm source-derived assertions: " + checks + ", failed groups: " + failed);
        return failed == 0 ? 0 : 1;
    }
}
