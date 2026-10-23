// Runs EvtKind over every area script in the original game data, read from copies of ECL<d>.DAX and GEO<d>.DAX in a
// temporary folder (the originals are only read, to copy them). For each script it loads the block as
// EclMenu.LoadEclScript does, reads the five entry addresses as EclMenu.EclInit does, runs EvtKind.Prepare, finds the
// GEO maps the script loads (LOADFILES), and classifies every event square on them. Output: a Markdown report.
// Usage: Por.EvtKind.Media <original game folder> <report.md>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Por.Core;

class DaxHost : DaxIo.IDaxPlatform
{
    public string Dir;
    public void WriteChar(byte col, byte row, byte attr, byte count, byte ch) { }
    public void BiosWriteChar(byte page, byte col, byte row, byte attr, byte count, byte ch) { }
    public byte ReadGameKey() { throw new InvalidOperationException("no key input in a batch run"); }
    public void ExitGame() { throw new InvalidOperationException("game exit requested"); }
    public void OverlayMessage(string text) { }
    public string CurrentDirectory { get { return Dir; } }
    public string FullPath(string path) { return Path.GetFullPath(path); }
}

static class Program
{
    static readonly string[] KindName = { "none", "event?", "message", "trap", "treasure", "service", "fight", "exit" };

    // DAX block ids: header word = entries * 9; each entry id byte, offset int32, raw word, packed word
    static List<byte> BlockIds(string path)
    {
        var ids = new List<byte>();
        using (var r = new BinaryReader(File.OpenRead(path)))
        {
            int hdr = r.ReadUInt16();
            for (int i = 0; i < hdr / 9; i++) { ids.Add(r.ReadByte()); r.ReadInt32(); r.ReadUInt16(); r.ReadUInt16(); }
        }
        return ids;
    }

    static byte[] Load(string name, byte id)
    {
        ushort size; byte[] data;
        DaxIo.LoadDax(name, ".dax", id, out size, out data);
        return size == 0 ? null : data.Take(size).ToArray();
    }

    // GEO maps the script loads: LOADFILES with a literal map id, on any path from the entry points
    static SortedSet<int> MapsLoaded()
    {
        var maps = new SortedSet<int>();
        var seen = new HashSet<ushort>();
        var q = new Queue<ushort>();
        Action<ushort> push = a => { if (a >= C_Gbvm.SCRIPT_FIRST && a <= C_Gbvm.SCRIPT_LAST && seen.Add(a)) q.Enqueue(a); };
        for (int i = 1; i <= 5; i++) push(GDVars.scriptEntryAddrs[i]);
        while (q.Count > 0)
        {
            ushort a = q.Dequeue();
            GDVars.eclIp = a;
            EclVars.SkipEclCmd();
            byte op = GDVars.curOpcode;
            ushort next = GDVars.eclIp;
            Func<int, ushort> w = i => EclVars.CombineBytesToWord(GDVars.dsGbvmLowerByte[i], GDVars.dsGbvmUpperByte[i]);
            switch (op)
            {
                case C_Ecl.EXIT_SCRIPT: case C_Ecl.RETURN: case C_Ecl.NEWECL: break;
                case C_Ecl.JUMP: push(w(1)); break;
                case C_Ecl.GOSUB: push(w(1)); push(next); break;
                case C_Ecl.ONGOTO: case C_Ecl.ONGOSUB:
                    {
                        // the list head read from the script, as EvtKind's ListHead does
                        byte t0 = GDVars.zone3[a + 1];
                        int p = a + 3 + ((t0 >= 1 && t0 <= 3) ? 1 : 0);
                        byte n = (GDVars.zone3[p] == 0 || GDVars.zone3[p] == 2) ? GDVars.zone3[p + 1] : (byte)0;
                        for (int i = 1; i <= Math.Min((int)n, 64); i++) push(w(i));
                        if (op == C_Ecl.ONGOSUB) push(next);
                        break;
                    }
                default:
                    if (op >= C_Ecl.IF_EQ && op <= C_Ecl.IF_GE)
                    {
                        push(next);
                        GDVars.eclIp = next; EclVars.SkipEclCmd(); push(GDVars.eclIp);
                        break;
                    }
                    if (op == C_Ecl.LOADFILES)
                    {
                        byte t = GDVars.dsGbvmType[1];
                        int f1 = GDVars.dsGbvmLowerByte[1];
                        if ((t == 0 || t == 2) && f1 != 255 && f1 != 127) maps.Add(f1);
                    }
                    push(next);
                    break;
            }
        }
        return maps;
    }

    static int Main(string[] args)
    {
        string src = args[0], report = args[1];
        string dir = Path.Combine(Path.GetTempPath(), "por-evtkind-media-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var sb = new StringBuilder();
        int failures = 0;
        var inventory = new List<object>();
        var geometryIssues = new List<object>();
        try
        {
            foreach (var f in Directory.GetFiles(src))
            {
                string n = Path.GetFileName(f).ToLowerInvariant();
                if ((n.StartsWith("ecl") || n.StartsWith("geo")) && n.EndsWith(".dax")) File.Copy(f, Path.Combine(dir, n));
            }
            GDVars.gameDir = dir + Path.DirectorySeparatorChar;
            GDVars.dsSuppressingInsertDiskFlag = true;
            DaxIo.Platform = new DaxHost { Dir = dir };
            DaxIo.ResetArchiveCache();
            GDVars.zone0 = new Types.TGbvmZone0State();
            GDVars.zone1 = new Types.TGbvmZone1();
            GDVars.zone2 = new Types.TZone2();
            GDVars.zone3 = new Types.TZone3();
            GDVars.gbStrings = new Types.TGbStrings();
            GDVars.map3d = new Types.TGeoMap();
            // scripts quote the current character's name ($81 string references to $6B00); the game always has one
            GDVars.curChar = new Types.TCharRecord { name = "TESTER" };
            EclBindings.BindConvertedUnits();

            var totals = new int[8];
            int scripts = 0, squares = 0;
            var table = new StringBuilder();
            table.AppendLine("| Disk | Script | Bytes | Dispatches | Maps | Event squares | exit | service | fight | treasure | trap | message | none | event? |");
            table.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            var unknown = new StringBuilder();
            var edgeExit = new int[2];
            for (int disk = 1; disk <= 8; disk++)
            {
                string ecl = Path.Combine(dir, "ecl" + disk + ".dax");
                if (!File.Exists(ecl)) continue;
                foreach (byte id in BlockIds(ecl))
                {
                    try
                    {
                        var data = Load("ECL" + disk, id);
                        if (data == null || data.Length < 2) { sb.AppendLine("- ECL" + disk + " block " + id + ": empty"); continue; }
                        for (int i = 0; i < 0x2700; i++) GDVars.zone3.Data[0x9900 + i] = 0;
                        int len = Math.Min(data.Length - 2, 0x2700);
                        for (int i = 0; i < len; i++) GDVars.zone3.Data[0x9900 + i] = data[2 + i];
                        GDVars.eclIp = 0x9900;
                        for (int e = 1; e <= 5; e++)
                        {
                            EclVars.LoadEclVars(1);
                            GDVars.scriptEntryAddrs[e] = EclVars.CombineBytesToWord(GDVars.dsGbvmLowerByte[1], GDVars.dsGbvmUpperByte[1]);
                        }
                        EvtKind.Prepare();
                        scripts++;
                        int dispatches = EvtKind.dispatchCount;
                        var maps = MapsLoaded();
                        var counts = new int[8];
                        int cells = 0;
                        var mapInventory = new List<object>();
                        foreach (int m in maps)
                        {
                            var geo = Load("GEO" + disk, (byte)m);
                            if (geo == null || geo.Length != 1026) { geometryIssues.Add(new { disk, scriptId=(int)id, mapId=m, observedBytes=geo==null?0:geo.Length }); sb.AppendLine("- GEO" + disk + " map " + m + " (from script " + id + "): missing or not 1026 bytes"); continue; }
                            var cellsByKind = new List<string>();
                            var candidates = new List<object>();
                            for (int y = 0; y < 16; y++)
                                for (int x = 0; x < 16; x++)
                                {
                                    byte ev = geo[2 + 512 + y * 16 + x];
                                    if (ev == 0) continue;
                                    byte k = EvtKind.Kind(ev);
                                    counts[k]++; cells++;
                                    if (k == EvtKind.KIND_FIGHT) candidates.Add(new { x, y, eventId=(int)ev, status="static_candidate_not_runtime_validated" });
                                    if (k == EvtKind.KIND_EVENT) cellsByKind.Add("(" + x + "," + y + ")=" + ev);
                                    if (k == EvtKind.KIND_EXIT) edgeExit[(x == 0 || y == 0 || x == 15 || y == 15) ? 0 : 1]++;
                                }
                            mapInventory.Add(new { mapId=m, area=AreaName.AreaFor(id,(byte)m), fightCandidates=candidates });
                            if (cellsByKind.Count > 0)
                                unknown.AppendLine("- Disk " + disk + ", script " + id + ", map " + m + ": " + string.Join(" ", cellsByKind));
                        }
                        inventory.Add(new { disk, scriptId=(int)id, scriptSha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(data)).ToLowerInvariant(), maps=mapInventory, dispatches, status="static_scan_only", scanLimits="bounded classifier; literal LOADFILES only; dynamic/random/rest encounters and state-dependent alternatives are not enumerated exhaustively" });
                        for (int k = 0; k < 8; k++) totals[k] += counts[k];
                        squares += cells;
                        table.AppendLine("| " + disk + " | " + id + " | " + len + " | " + dispatches + " | " + (maps.Count == 0 ? "–" : string.Join(",", maps)) +
                            " | " + cells + " | " + counts[7] + " | " + counts[5] + " | " + counts[6] + " | " + counts[4] + " | " + counts[3] + " | " + counts[2] + " | " + counts[0] + " | " + counts[1] + " |");
                    }
                    catch (Exception e)
                    {
                        failures++;
                        sb.AppendLine("- **ECL" + disk + " block " + id + " failed:** " + e.GetType().Name + ": " + e.Message + " at " +
                            string.Join(" < ", e.StackTrace.Split('\n').Take(4).Select(l => l.Trim().Split(new[] { " in " }, StringSplitOptions.None)[0].Replace("at Por.Core.", ""))));
                    }
                }
            }
            var o = new StringBuilder();
            o.AppendLine("# EvtKind over the original area scripts");
            o.AppendLine();
            o.AppendLine("Generated by `tests/Por.EvtKind.Media` from copies of the original `ecl1..8.dax` and `geo1..8.dax` (unpatched shipped data; the DataBld fixes are not applied). Each script was loaded as `EclMenu.LoadEclScript` does, and its event squares were classified on the GEO maps it loads with a literal LOADFILES.");
            o.AppendLine();
            o.AppendLine("- Scripts analysed: " + scripts + "; failures: " + failures + ".");
            o.AppendLine("- Event squares classified: " + squares + ". By kind: " + string.Join(", ", new[] { 7, 5, 6, 4, 3, 2, 0, 1 }.Select(k => KindName[k] + " " + totals[k])) + ".");
            o.AppendLine("- Exit squares on a map edge: " + edgeExit[0] + "; inside the map: " + edgeExit[1] + ".");
            o.AppendLine("- ON GOTO lists longer than 64 (the port's deviation 1): " + EvtKind.listsCut + ".");
            o.AppendLine();
            if (sb.Length > 0) { o.AppendLine("## Problems"); o.AppendLine(); o.Append(sb); o.AppendLine(); }
            o.AppendLine("## Per script"); o.AppendLine(); o.Append(table); o.AppendLine();
            o.AppendLine("## Squares whose dispatch was not found (event?)"); o.AppendLine();
            o.Append(unknown.Length > 0 ? unknown.ToString() : "None.\n");
            File.WriteAllText(report, o.ToString());
            if(args.Length>2)File.WriteAllText(args[2],JsonSerializer.Serialize(new { schema=1, scope="unpatched original area script static candidates, NOT campaign runtime coverage", failures, scanTotals=new { eventSquares=squares, unresolvedEventSquares=totals[1], fightClassifiedSquares=totals[6] }, geometryIssues, scripts=inventory },new JsonSerializerOptions { WriteIndented=true }));
            Console.WriteLine("scripts=" + scripts + " failures=" + failures + " squares=" + squares + " listsCut=" + EvtKind.listsCut);
            Console.WriteLine(string.Join(" ", Enumerable.Range(0, 8).Select(k => KindName[k] + "=" + totals[k])));
        }
        finally { DaxIo.ResetArchiveCache(); Directory.Delete(dir, true); }
        return failures == 0 ? 0 : 1;
    }
}
