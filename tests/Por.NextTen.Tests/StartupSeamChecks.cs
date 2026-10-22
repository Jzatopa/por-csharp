// Wiring review (October 22, 2026): startup seam check, platform gaps, and the production bindings behind seams that
// other groups only drive through fakes. The hosts here are inert stand-ins for the SDL application (Por.Desktop.Tests runs
// the real DesktopHost composition); every binding under test is the production one.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Por.Core;

public class InertHost : DispatchProxy
{
    internal static T Of<T>() { return DispatchProxy.Create<T, InertHost>(); }
    protected override object Invoke(MethodInfo m, object[] a)
    {
        Type t = m.ReturnType;
        if (t == typeof(void)) return null;
        if (t == typeof(string)) return "";
        return t.IsValueType ? Activator.CreateInstance(t) : null;
    }
}

class SeamIo : IGameIoHost
{
    public readonly Queue<byte> Keys = new Queue<byte>();
    public readonly List<string> Events = new List<string>();
    public bool KeyPressed { get { return false; } }
    public byte ReadKey() { Events.Add("key"); return Keys.Count > 0 ? Keys.Dequeue() : (byte)13; }
    public void SoundCommand(ushort c) { }
    public void SoundSuspend() { }
    public void Interrupt(byte n, Types.TInterruptRegisters r) { }
    public void Halt(int code) { throw new Halted(); }
    public void WriteLine(string m) { }
    public TextWriter OpenDebugLog(string p) { throw new IOException("no debug log in tests"); }
}

static partial class Tests
{
    static Dictionary<FieldInfo, object> PristineStatics;

    static IEnumerable<FieldInfo> CoreStatics()
    {
        foreach (Type t in typeof(FixMain).Assembly.GetTypes())
        {
            if (t.Namespace != "Por.Core" || t.ContainsGenericParameters) continue;
            foreach (FieldInfo f in t.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                if (!f.IsLiteral && !f.IsInitOnly) yield return f;
        }
    }
    static Dictionary<FieldInfo, object> SnapStatics()
    {
        var d = new Dictionary<FieldInfo, object>();
        foreach (FieldInfo f in CoreStatics()) d[f] = f.GetValue(null);
        return d;
    }
    static void RestoreStatics(Dictionary<FieldInfo, object> s) { foreach (var kv in s) kv.Key.SetValue(null, kv.Value); }

    static bool IsSeamType(Type t) { return typeof(Delegate).IsAssignableFrom(t) || t.IsInterface || (t.IsClass && t.IsAbstract && !t.IsSealed); }
    static string SeamName(FieldInfo f)
    {
        string n = f.Name;
        if (n.StartsWith("<") && n.Contains(">k__BackingField")) n = n.Substring(1, n.IndexOf('>') - 1);
        return f.DeclaringType.Name + "." + n;
    }

    // Set one seam named as in StartupSeamCheck.Required to null.
    static void ClearSeam(string name)
    {
        if (name.StartsWith("MemShow"))
        {
            MemShow.UnavailableMessage = null; MemShow.GetOverlayBuffer = null; MemShow.GetOverlayRetry = null; MemShow.GetFreeMemory = null; MemShow.GetLargestBlock = null;
            return;
        }
        string[] parts = name.Split('.');
        Type t = typeof(FixMain).Assembly.GetType("Por.Core." + parts[0], true);
        object target = null; Type owner = t; bool isStatic = true;
        for (int i = 1; i < parts.Length; i++)
        {
            BindingFlags flags = (isStatic ? BindingFlags.Static : BindingFlags.Instance) | BindingFlags.Public | BindingFlags.NonPublic;
            FieldInfo f = owner.GetField(parts[i], flags); PropertyInfo p = f == null ? owner.GetProperty(parts[i], flags) : null;
            if (f == null && p == null) throw new Exception("no member " + parts[i] + " on " + owner.Name);
            if (i == parts.Length - 1) { if (f != null) f.SetValue(target, null); else p.SetValue(target, null); return; }
            target = f != null ? f.GetValue(target) : p.GetValue(target);
            if (target == null) return;
            owner = target.GetType(); isStatic = false;
        }
    }

    // Fresh-process state: every static back to its initializer value, with fresh copies of the shared driver records.
    static void PristineState()
    {
        RestoreStatics(PristineStatics);
        GDVars.video = new Types.TVideoDriver(); GDVars.sound = new Types.TSoundDriver(); EclVm.Units = new EclVmUnits();
    }

    // The application hosts Por.Desktop installs (DesktopComposition), as inert stand-ins, plus the real gap bindings.
    static SeamIo InstallStandInHosts()
    {
        var io = new SeamIo();
        GameIo.Host = io; VideoHardware.Host = InertHost.Of<IVideoHardware>(); Blit.Interrupt = (n, r) => { };
        DaxIo.Platform = InertHost.Of<DaxIo.IDaxPlatform>(); GameInit.Services = InertHost.Of<IGameInitServices>(); Options.Info = InertHost.Of<IOptionsInfo>();
        DataBld.Platform = InertHost.Of<IDataBuildPlatform>(); TextRend.ReadBiosTicks = () => 0; TextRend.DelayMilliseconds = ms => { };
        BldShow.MaxAvailable = () => 0L; Portrait.MaxAvailable = () => 0L; SoundDrv.Host = InertHost.Of<ISoundDriverHost>();
        SndNone.Install(); VidVga.Install();
        PlatformGapBindings.Bind();
        return io;
    }

    // Desktop order: hosts, gap bindings, then FixMain.RunManaged binds the converted units and calls the check before RunBody.
    static List<string> RunDesktopBindingPath(out bool bodyEntered)
    {
        List<string> seen = null; bool entered = false;
        FixMain.RuntimeStage = s => entered = true;
        try { FixMain.RunManaged(() => { seen = StartupSeamCheck.Missing(); StartupSeamCheck.Verify(); throw new Halted(); }); }
        catch (Halted) { }
        bodyEntered = entered;
        return seen;
    }

    static void StartupCheckRealPath()
    {
        var saved = SnapStatics();
        try
        {
            PristineState();
            List<string> before = StartupSeamCheck.Missing();
            Eq(StartupSeamCheck.Required.Length - 1, before.Count, "fresh process: every required seam but EclVm.Units is unbound (" + string.Join(", ", before) + ")");
            InstallStandInHosts();
            bool entered;
            List<string> missing = RunDesktopBindingPath(out entered);
            Yes(missing != null, "RunManaged calls the verifier after binding");
            EqS("", string.Join(", ", missing), "startup check passes after the real binding path");
            Yes(!entered, "verifier runs before the game body");
            foreach (StartupSeamCheck.Seam s in StartupSeamCheck.Required) Yes(s.Name.Length > 0 && s.Owner.Length > 0, "seam " + s.Name + " names its owner");
        }
        finally { RestoreStatics(saved); }
    }

    static void StartupCheckNamesMissing()
    {
        var saved = SnapStatics();
        try
        {
            foreach (StartupSeamCheck.Seam s in StartupSeamCheck.Required)
            {
                PristineState(); InstallStandInHosts(); bool entered; RunDesktopBindingPath(out entered);
                ClearSeam(s.Name);
                List<string> missing = StartupSeamCheck.Missing();
                Yes(missing.Any(m => m.StartsWith(s.Name + " (")), "check names removed seam " + s.Name);
                bool threw = false;
                try { StartupSeamCheck.Verify(); } catch (InvalidOperationException e) { threw = e.Message.Contains(s.Name); }
                Yes(threw, "Verify fails naming " + s.Name);
            }
            // End to end: a host seam the application forgot stops RunManaged before the game body, with the seam's name.
            PristineState(); InstallStandInHosts(); Options.Info = null;
            bool bodyEntered = false; string message = null; FixMain.RuntimeStage = st => bodyEntered = true;
            try { FixMain.RunManaged(StartupSeamCheck.Verify); } catch (InvalidOperationException e) { message = e.Message; }
            Yes(message != null && message.Contains("Options.Info (host)"), "RunManaged(Verify) fails naming Options.Info");
            Yes(!bodyEntered, "game body not entered with an unbound seam");
        }
        finally { RestoreStatics(saved); }
    }

    // Seams that may still be null after the desktop startup path, and why. Anything else null fails.
    static readonly Dictionary<string, string> AllowedNullAfterStartup = new Dictionary<string, string>
    {
        { "CharHlp.faultHook", "GameInit2 installs CharDisp.ShowFault" },
        { "Dice.RandomBelow", "GameInit2 calls the host's SeedRandom" },
        { "GameIo.saveSettings", "Options.LoadOptions (GameInit2) installs SaveOptions" },
        { "GameIo.showMemory", "GameInit2 installs MemShow.ShowMemory" },
        { "GDVars.castTargetMenu", "GameInit2 tables and Combat set it" },
        { "GDVars.debugFile", "STING Ctrl-L debug log, opened on demand" },
        { "MenuCore.idleHook", "set only while a preview runs; null-checked" },
        { "FixMain.RuntimeStage", "optional observer; null-checked" },
        { "GameBoot.Host", "DOS command-line entry (FixMain.Run) only; desktop uses RunManaged" },
        { "SndOpl.ReadPort", "AdLib is installed only by the DOS GameBoot/SndSb path" },
        { "SndSb.Host", "Sound Blaster is installed only by the DOS GameBoot path" },
        { "MemShow.GetOverlayBuffer", "platform gap: MemShow.UnavailableMessage" },
        { "MemShow.GetOverlayRetry", "platform gap: MemShow.UnavailableMessage" },
        { "MemShow.GetFreeMemory", "platform gap: MemShow.UnavailableMessage" },
        { "MemShow.GetLargestBlock", "platform gap: MemShow.UnavailableMessage" },
    };

    static void StaticSeamInventory()
    {
        var saved = SnapStatics();
        try
        {
            PristineState(); InstallStandInHosts(); bool entered; RunDesktopBindingPath(out entered);
            FixMain.RuntimeStage = null;
            var unexpected = new List<string>(); int seams = 0;
            foreach (FieldInfo f in CoreStatics())
            {
                if (!IsSeamType(f.FieldType) || f.DeclaringType.Name.Contains("<") || f.Name.StartsWith("<>")) continue; // skip compiler lambda caches
                seams++;
                if (f.GetValue(null) == null && !AllowedNullAfterStartup.ContainsKey(SeamName(f))) unexpected.Add(SeamName(f));
            }
            Yes(seams > 100, "inventory found the static seams (" + seams + ")");
            EqS("", string.Join(", ", unexpected), "no unlisted static seam is null after the desktop binding path");
            // Instance seams inside the bound adapters.
            foreach (FieldInfo f in typeof(EclVmUnits).GetFields()) Yes(f.GetValue(EclVm.Units) != null, "EclVm.Units." + f.Name + " bound");
            foreach (string key in AllowedNullAfterStartup.Keys) Yes(AllowedNullAfterStartup[key].Length > 0, "reason recorded for " + key);
        }
        finally { RestoreStatics(saved); }
    }

    static Type Outermost(Type t) { while (t.DeclaringType != null) t = t.DeclaringType; return t; }
    static string ProductionOwner(object seam)
    {
        var d = seam as Delegate;
        Type t = d != null ? d.Method.DeclaringType : seam.GetType();
        Type o = Outermost(t);
        return o.Namespace == "Por.Core" ? o.Name : "TEST:" + o.FullName;
    }

    static List<byte> BottomLine(List<string> ev, int color)
    {
        var glyphs = new List<byte>();
        foreach (string e in ev) { string[] p = e.Split(':'); if (p[0] == "g" && p[2] == "24" && p[3] == color.ToString()) glyphs.Add(byte.Parse(p[4])); }
        return glyphs;
    }
    static List<byte> Glyphs(string s)
    {
        var g = new List<byte>();
        foreach (char ch in s) { int c = ch; if (c >= 97 && c <= 122) c -= 32; g.Add((byte)(c % 64)); }
        return g;
    }

    static void DesktopSeamsRealBindings()
    {
        var saved = SnapStatics();
        try
        {
            PristineState(); SeamIo io = InstallStandInHosts(); bool entered; RunDesktopBindingPath(out entered);
            // Seams other groups drive only through fakes: the production binding is installed.
            var owners = new Dictionary<string, object>
            {
                { "Explore.Services", Explore.Services }, { "PostCbt.Services", PostCbt.Services }, { "EclMenu.Host", EclMenu.Host },
                { "GameTime.Services", GameTime.Services }, { "Temple.Effects", Temple.Effects }, { "CharEdit.Services", CharEdit.Services },
                { "Combat.Services", Combat.Services }, { "CharDisp.CombatMap", CharDisp.CombatMap }, { "CbtActs.CombatMap", CbtActs.CombatMap },
                { "CbtActs.Effects", CbtActs.Effects }, { "CbtActs.MagicUnit", CbtActs.MagicUnit }, { "Magic.Effects", Magic.Effects },
                { "Magic.CombatMap", Magic.CombatMap }, { "EffHndlr.Effects", EffHndlr.Effects }, { "EffHndlr.CombatMap", EffHndlr.CombatMap },
                { "CbtMap.boltTracer", CbtMap.boltTracer }, { "CommerceServices.ViewCharacter", CommerceServices.ViewCharacter },
                { "CommerceServices.CannotCarryItem", CommerceServices.CannotCarryItem }, { "CommerceServices.LootTake", CommerceServices.LootTake },
                { "HeapErr.SaveGame", HeapErr.SaveGame }, { "CompTool.EditCharacter", CompTool.EditCharacter }, { "FixMain.Services", FixMain.Services },
                { "EclVm.Units.Game", EclVm.Units.Game }, { "EvtKind.EclVarsUnit", EvtKind.EclVarsUnit }, { "CharGenView.Host", CharGenView.Host },
                { "CharacterRecordCodec.ResolveEquippedItem", CharacterRecordCodec.ResolveEquippedItem },
            };
            foreach (var kv in owners)
            {
                Yes(kv.Value != null, kv.Key + " bound");
                string owner = ProductionOwner(kv.Value);
                Yes(!owner.StartsWith("TEST:"), kv.Key + " is a production binding, not a test double (" + owner + ")");
            }
            EqS("FinalGameplayBindings", ProductionOwner(Explore.Services), "exploration services owner");
            EqS("FinalGameplayBindings", ProductionOwner(PostCbt.Services), "post-combat services owner");
            EqS("TempleBindings", ProductionOwner(Temple.Effects), "temple effects owner");
            EqS("CharGenBindings", ProductionOwner(CharEdit.Services), "character editor services owner");
            EqS("CbtMapBindings", ProductionOwner(Combat.Services), "combat services owner");
            EqS("ViewChar", ProductionOwner(CommerceServices.CannotCarryItem), "commerce carry check is ViewChar's");
            EqS("SaveLoad", ProductionOwner(HeapErr.SaveGame), "heap error save is SaveLoad's");
            foreach (string flag in new[] { "EffectsBound", "GameplayBound" })
            {
                FieldInfo fl = Combat.Services.GetType().GetField(flag, BindingFlags.Instance | BindingFlags.NonPublic);
                Yes(fl != null && (bool)fl.GetValue(Combat.Services), "combat adapter " + flag);
            }
            // Explore.Services drives the real clock.
            GDVars.zone0 = new Types.TGbvmZone0State(); GDVars.zone1 = new Types.TGbvmZone1(); GDVars.creatureList = null; GDVars.curChar = null;
            Explore.Services.AdvanceTime(5, C_Time.MINUTES);
            Eq(5, GDVars.zone0.words[C_Gbvm.CLOCK + C_Time.MINUTES], "exploration AdvanceTime reaches FxEngine's clock");

            // Memory display gap: the STING Ctrl-Y key path shows the platform message and returns normally.
            var ev = new List<string>(); GDVars.gameFont = new Types.TFontArr(); GDVars.video.DrawGlyph = (gx, gy, gc, gg) => ev.Add("g:" + gx + ":" + gy + ":" + gc + ":" + gg);
            GameIo.showMemory = MemShow.ShowMemory; GDVars.stingArg = true; GDVars.bottomLineTaken = false; io.Keys.Enqueue(25); io.Events.Clear();
            byte key = GameIo.ReadGameKey();
            Eq(25, key, "Ctrl-Y returned to the caller");
            Yes(BottomLine(ev, 10).Take(PlatformGapBindings.Message.Length).SequenceEqual(Glyphs(PlatformGapBindings.Message)), "memory display shows the platform message");
            Eq(2, io.Events.Count, "message waits for one key after the Ctrl-Y read");
            Yes(GDVars.bottomLineTaken, "bottom line marked taken as the source does");
            GDVars.stingArg = false;

            // Equipped DOS pointer gap: a record with a stale pointer loads, the slot stays empty, the message is shown.
            byte[] raw = new byte[CharacterRecordCodec.Size]; raw[0] = 3; raw[1] = (byte)'O'; raw[2] = (byte)'R'; raw[3] = (byte)'C'; raw[16] = 17; raw[50] = 9;
            raw[204 + 3 * 4] = 0x34; raw[204 + 3 * 4 + 2] = 0x12;
            ev.Clear(); io.Events.Clear();
            var c = new Types.TCharRecord();
            CharacterRecordCodec.DecodeMonster(raw, 0, c);
            Yes(c.equipped[3] == null, "stale equipped pointer leaves the slot empty");
            EqS("ORC", c.name, "rest of the record decoded"); Eq(17, c.strCur, "strength decoded"); Eq(9, c.hpMax, "hp decoded");
            Yes(BottomLine(ev, 15).Take(PlatformGapBindings.Message.Length).SequenceEqual(Glyphs(PlatformGapBindings.Message)), "equipped pointer gap shows the platform message");
            Eq(1, io.Events.Count, "one key wait for one stale pointer");
            raw[204 + 3 * 4] = 0; raw[204 + 3 * 4 + 2] = 0; ev.Clear(); io.Events.Clear();
            CharacterRecordCodec.DecodeMonster(raw, 0, c);
            Eq(0, io.Events.Count, "zero pointers (all shipped records) show nothing");
        }
        finally { RestoreStatics(saved); }
    }
}
