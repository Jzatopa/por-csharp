// Magic (src/magic.pas) checks, part 2: the groups. See MagicChecks1.cs for the method.
using System;
using System.Collections.Generic;
using System.Linq;
using Por.Core;
using static Por.Core.Types;

static partial class Tests
{
    const int MgSeeds = 1500;

    static TItemRec MgPickItem(Random r, TCharRecord c, bool readableOnly)
    {
        var items = new List<TItemRec>();
        for (var it = c.itemsAddr; it != null; it = it.next) if (!readableOnly || CharHlp.ItemReadable(it)) items.Add(it);
        return items.Count == 0 ? null : items[r.Next(items.Count)];
    }

    static void MgCoverage(string what)
    {
        // most scenarios must run to completion, not end in the same exception on both sides
        Console.WriteLine("  " + what + ": " + mgRuns + " scenario pairs, " + mgExceptions + " end in the same exception on both sides, " + mgLogLines + " logged calls compared");
        Yes(mgExceptions * 4 < mgRuns, what + ": too many scenarios end in an exception (" + mgExceptions + "/" + mgRuns + ")");
        mgExceptions = mgRuns = 0; mgLogLines = 0;
    }

    static void MgLists()
    {
        // SpellMark over every creature, id and mark
        for (int seed = 1; seed <= 60; seed++)
        {
            var w = MgBuild(seed);
            foreach (var c in w.All)
                for (int sp = 0; sp <= 255; sp++)
                    for (int mk = 0; mk <= 3; mk++)
                        Eq(MgOracle.Mark(c, sp, mk), Magic.SpellMark(c, (byte)sp, (byte)mk), "SpellMark " + sp + " " + mk);
        }
        for (int seed = 1; seed <= MgSeeds * 2; seed++)
        {
            int mt = 0; TCharRecord who = null;
            Action<MgWorld> setup = w =>
            {
                var r = new Random(seed + 101);
                mt = r.Next(0, 9);
                who = w.All[r.Next(w.All.Count)];
                if (r.Next(3) == 0) { GDVars.ifcState = C_Ifc.COMBAT; }
                var sc = MgPickItem(r, who, true);
                if (mt == 2 && sc == null) mt = 1;
                Magic.dsCurrentScrollPtr = sc; MgOracle.curScroll = sc;
            };
            MgPair(seed, setup, () => MgOracle.Generate(who, mt).ToString(), () => Magic.GenerateSpellList(who, (byte)mt).ToString(), () => "GenerateSpellList " + mt);
        }
        MgCoverage("MgLists");
    }

    static void MgFillList(Random r, MgWorld w)
    {
        TItemRec head = null, tail = null;
        int n = r.Next(0, 12);
        for (int i = 0; i < n; i++)
        {
            var node = new TItemRec { name = "line" + i, inUse = (byte)(r.Next(3) == 0 ? 1 : 0) };
            if (head == null) head = node; else tail.next = node;
            tail = node;
        }
        var allItems = new List<TItemRec>();
        foreach (var c in w.All) for (var it = c.itemsAddr; it != null; it = it.next) allItems.Add(it);
        var ids = new byte[69]; var scr = new TItemRec[69]; var ptr = new TItemRec[49];
        for (int i = 0; i <= 68; i++) { ids[i] = (byte)r.Next(0, 68); scr[i] = r.Next(2) == 0 || allItems.Count == 0 ? null : allItems[r.Next(allItems.Count)]; }
        for (int i = 0; i <= 48; i++) ptr[i] = r.Next(2) == 0 || allItems.Count == 0 ? null : allItems[r.Next(allItems.Count)];
        Magic.spellList = head; MgOracle.spellList = head;
        Array.Copy(ids, Magic.spellIds, 69); MgOracle.spellIds = (byte[])ids.Clone();
        Array.Copy(scr, Magic.spellScrolls, 69); MgOracle.spellScrolls = (TItemRec[])scr.Clone();
        Array.Copy(ptr, Magic.scrollPtrs, 49); MgOracle.scrollPtrs = (TItemRec[])ptr.Clone();
        var cur = allItems.Count == 0 ? null : allItems[r.Next(allItems.Count)];
        Magic.dsCurrentScrollPtr = cur; MgOracle.curScroll = cur;
    }

    static void MgSelect()
    {
        for (int seed = 1; seed <= MgSeeds * 2; seed++)
        {
            int st = 0; short idx0 = 0;
            Action<MgWorld> setup = w =>
            {
                var r = new Random(seed + 202);
                MgFillList(r, w);
                st = r.Next(0, 7);
                idx0 = (short)(r.Next(3) == 0 ? r.Next(-3, 0) : r.Next(0, 12));
            };
            MgPair(seed, setup,
                () => { short i = idx0; TItemRec s = MgItemMarker; byte v = MgOracle.Selection(st, ref i, ref s); return v + "/" + i + "/" + MgWorld.F(s); },
                () => { short i = idx0; TItemRec s = MgItemMarker; byte v = Magic.SpellSelection((byte)st, ref i, ref s); return v + "/" + i + "/" + MgWorld.F(s); },
                () => "SpellSelection " + st);
        }
        for (int seed = 1; seed <= MgSeeds * 2; seed++)
        {
            int mt = 0, st = 0, form = 0; short idx0 = 0;
            Action<MgWorld> setup = w =>
            {
                var r = new Random(seed + 303);
                mt = r.Next(0, 9); st = r.Next(0, 7); form = r.Next(3);
                idx0 = (short)(r.Next(2) == 0 ? -1 : r.Next(0, 10));
                var sc = MgPickItem(r, GDVars.curChar, true);
                if (mt == 2 && sc == null) mt = 1;
                Magic.dsCurrentScrollPtr = sc; MgOracle.curScroll = sc;
            };
            MgPair(seed, setup,
                () =>
                {
                    short i = idx0; bool ne = false; TItemRec s = MgItemMarker; byte v;
                    if (form == 2) v = MgOracle.Choose(7, 1, ref i, ref ne, ref s); else v = MgOracle.Choose(mt, st, ref i, ref ne, ref s);
                    return v + "/" + i + "/" + ne + "/" + (form == 1 ? "-" : MgWorld.F(s));
                },
                () =>
                {
                    short i = idx0; bool ne = false; TItemRec s = MgItemMarker; byte v;
                    if (form == 0) v = Magic.ChooseSpell((byte)mt, (byte)st, ref i, ref ne, ref s);
                    else if (form == 1) v = Magic.SpellMenu((byte)mt, (byte)st, ref i, ref ne);
                    else v = Magic.CastSpellMenu(ref i, ref ne, ref s);
                    return v + "/" + i + "/" + ne + "/" + (form == 1 ? "-" : MgWorld.F(s));
                },
                () => "ChooseSpell " + mt + " " + st + " form " + form);
        }
        MgCoverage("MgSelect");
    }
    static readonly TItemRec MgItemMarker = new TItemRec { name = "marker" };

    static void MgCast()
    {
        for (int seed = 1; seed <= MgSeeds * 2; seed++)
        {
            int spell = 0, form = 0; bool b = false, ai = false, sel0 = false;
            Action<MgWorld> setup = w =>
            {
                var r = new Random(seed + 404);
                spell = r.Next(1, 68); form = r.Next(5); b = r.Next(2) == 0; ai = r.Next(2) == 0; sel0 = r.Next(3) == 0;
                if (form == 3 || form == 4) spell = r.Next(1, 57);
            };
            Func<bool, string> run = oracle =>
            {
                bool sel = sel0;
                switch (form)
                {
                    case 0: case 1:
                        if (oracle) MgOracle.Cast(spell, b, ai, ref sel); else Magic.CastSpellNC((byte)spell, b, ai, ref sel);
                        return sel.ToString();
                    case 2:
                        if (oracle) MgOracle.NoncombatTargets(spell, b, ref sel); else Magic.NoncombatCastTargetMenu((byte)spell, b, ref sel);
                        return sel.ToString();
                    case 3:
                        if (oracle) MgOracle.CastMsg(GDVars.curChar, "casts a longer message that is cut at forty chars", spell);
                        else Magic.PrintCastSpellMsg(GDVars.curChar, "casts a longer message that is cut at forty chars", (byte)spell);
                        return "";
                    default:
                        if (oracle) MgOracle.ShowCombatOnly(spell); else Magic.ShowCombatOnlySpell((byte)spell);
                        return "";
                }
            };
            MgPair(seed, setup, () => run(true), () => run(false), () => "Cast form " + form + " spell " + spell);
        }
        MgCoverage("MgCast");
    }

    static void MgItems()
    {
        for (int seed = 1; seed <= MgSeeds * 3; seed++)
        {
            int spell = 0, form = 0; bool announce = false, done0 = false; TItemRec item = null;
            Action<MgWorld> setup = w =>
            {
                var r = new Random(seed + 505);
                form = r.Next(3);
                item = MgPickItem(r, GDVars.curChar, r.Next(2) == 0);
                if (item == null)
                {
                    item = new TItemRec { name = "loose", itemType = (byte)r.Next(1, 6), quantity = (byte)r.Next(0, 3) };
                    for (int s = 1; s <= 3; s++) item.spells[s] = (byte)r.Next(0, 70);
                    item.nameIds[2] = (byte)r.Next(208, 213);
                    item.next = GDVars.curChar.itemsAddr; GDVars.curChar.itemsAddr = item;
                }
                int[] sp = { 0, 3, 10, 21, 34, 47, 51, 56, 57, 60, 64, 67, 70, 80, 90, 91, 120, 0x80 | 15 };
                spell = r.Next(3) == 0 ? r.Next(0, 256) : sp[r.Next(sp.Length)];
                announce = r.Next(2) == 0; done0 = r.Next(2) == 0;
                if (r.Next(4) == 0) item.special = (byte)(r.Next(2) == 0 ? 0x7F : 0x80); // the special < $80 edge
            };
            Func<bool, string> run = oracle =>
            {
                bool done = done0;
                switch (form)
                {
                    case 0:
                        if (oracle) MgOracle.UseSpell(item, spell, announce, ref done); else Magic.UseItemSpell(item, (byte)spell, announce, ref done);
                        return done.ToString();
                    case 1:
                        if (oracle) MgOracle.Use(item, ref done); else Magic.UseItem(item, ref done);
                        return done.ToString();
                    default:
                        if (oracle) MgOracle.ScrollRemove(GDVars.curChar, item, spell); else Magic.RemoveSpellFromScroll(GDVars.curChar, item, (byte)spell);
                        return "";
                }
            };
            MgPair(seed, setup, () => run(true), () => run(false), () => "Items form " + form + " spell " + spell);
        }
        MgCoverage("MgItems");
    }

    static void MgResolve()
    {
        for (int seed = 1; seed <= MgSeeds * 2; seed++)
        {
            int spell = 0, eff = 0, dmg = 0, elem = 0, form = 0; bool onRem = false; string msg = "";
            Action<MgWorld> setup = w =>
            {
                var r = new Random(seed + 606);
                spell = r.Next(1, 68); form = r.Next(4);
                eff = r.Next(3) == 0 ? 0 : r.Next(0, 256); dmg = r.Next(2) == 0 ? 0 : r.Next(0, 256); elem = r.Next(0, 16); onRem = r.Next(2) == 0;
                msg = r.Next(4) == 0 ? "a message well over forty characters long, to be cut" : "is affected";
                if (r.Next(3) == 0) GDVars.dsCurrentSpellBeing = 4; // rangeFixed 255: attack roll
            };
            MgPair(seed, setup,
                () => { if (form == 0) return MgOracle.Duration(spell).ToString(); MgOracle.Resolve(spell, eff, onRem, dmg, elem, msg); return ""; },
                () => { if (form == 0) return Magic.CalcSpellDuration((byte)spell).ToString(); Magic.ResolveSpell((byte)spell, (byte)eff, onRem, (byte)dmg, (byte)elem, msg); return ""; },
                () => "Resolve form " + form + " spell " + spell);
        }
        MgCoverage("MgResolve");
    }

    // spell id -> handler name, as GameInit.InitSpellFns installs them
    static readonly string[] MgHandlerOf = {
        "", "BlessHandler", "CurseHandler", "CureLightWoundsHandler", "CauseLightWoundsHandler", "DetectionHandler", "ProtectionHandler",
        "ProtectionHandler", "ResistColdHandler", "BurningHandsHandler", "CharmPersonHandler", "DetectionHandler", "EnlargeHandler", "ReduceHandler",
        "FriendsHandler", "MagicMissileHandler", "ProtectionHandler", "ProtectionHandler", "DetectionHandler", "ShieldHandler", "ShockingGraspHandler",
        "SleepHandler", "DetectionHandler", "HoldPersonHandler", "ResistFireHandler", "SilenceHandler", "SlowPoisonHandler", "SnakeCharmHandler",
        "SpiritualHammerHandler", "DetectionHandler", "InvisibilityHandler", "KnockHandler", "MirrorImageHandler", "RayOfEnfeeblementHandler",
        "StinkingCloudHandler", "StrengthHandler", "AnimateDeadHandler", "CureBlindnessHandler", "CauseBlindnessHandler", "CureDiseaseHandler",
        "CauseDiseaseHandler", "DispelMagicHandler", "PrayerHandler", "RemoveCurseSpell", "BestowCurseHandler", "BlinkHandler", "DispelMagicHandler",
        "FireballHandler", "HasteHandler", "HoldPersonHandler", "InvisibilityHandler", "LightningBoltHandler", "ProtectionHandler", "ProtectionHandler",
        "ProtectionHandler", "SlowHandler", "RestorationHandler", "PotionOfSpeedHandler", "KeoghtomsOintmentHandler", "GiantStrengthHandler",
        "JavelinOfLightningHandler", "WandParalyzationHandler", "PotionOfHealingHandler", "DustDisappearanceHandler", "FireballHandler",
        "WandMagicMissilesHandler", "UnusedSpellHandler", "ManualBodilyHealthHandler" };

    static void MgHandlerRuns(IEnumerable<string> names, int seeds, Action<Random, MgWorld> extra)
    {
        foreach (var name in names)
        {
            int natural = Array.IndexOf(MgHandlerOf, name);
            for (int seed = 1; seed <= seeds; seed++)
            {
                Action<MgWorld> setup = w =>
                {
                    var r = new Random(seed * 7 + natural);
                    var ids = Enumerable.Range(1, 67).Where(i => MgHandlerOf[i] == name).ToArray();
                    GDVars.dsCurrentSpellBeing = (byte)(r.Next(10) < 8 ? ids[r.Next(ids.Length)] : r.Next(1, 68));
                    if (r.Next(5) != 0 && GDVars.spellTargetCount > 0 && GDVars.dsTargetCreaturePtr[1] == null) GDVars.dsTargetCreaturePtr[1] = w.All[0];
                    if (r.Next(4) == 0 && GDVars.spellTargetCount == 0) { GDVars.spellTargetCount = 1; GDVars.dsTargetCreaturePtr[1] = w.All[r.Next(w.All.Count)]; }
                    if (extra != null) extra(r, w);
                };
                string n = name;
                MgPair(seed, setup, () => { MgOracle.Handler(n); return ""; }, () => { MagicBindings.SpellHandler(n)(); return ""; }, () => "Handler " + name + " spell " + GDVars.dsCurrentSpellBeing);
            }
        }
    }

    static void MgHandlers()
    {
        var names = MgHandlerOf.Skip(1).Distinct().Where(n => n != "StinkingCloudHandler" && n != "LightningBoltHandler" && n != "JavelinOfLightningHandler").ToArray();
        MgHandlerRuns(names, 400, (r, w) =>
        {
            // make the rarer branches reachable: poisoned/dead targets, drained levels, warriors, sleeping and snake targets
            var t = GDVars.dsTargetCreaturePtr[1];
            if (t != null && r.Next(3) == 0) MgWorld.AddFx(t, (byte)new[] { 55, 22, 15, 12, 11, 42, 39, 33, 36, 34, 43, 50 }[r.Next(12)], 5, (byte)r.Next(0, 30), false);
            if (t != null && r.Next(4) == 0) { t.status = C_Status.DEAD; t.hpCur = 0; }
            if (t != null && r.Next(4) == 0) { t.drainedLevels = (byte)r.Next(1, 3); t.classLevels[r.Next(8)] = (sbyte)r.Next(1, 9); }
            if (t != null && r.Next(4) == 0) t.classLevels[C_Level.FIGHTER] = (sbyte)r.Next(1, 9);
            if (r.Next(3) == 0) foreach (var c in w.All) if (r.Next(2) == 0) { c.status = C_Status.DEAD; c.charType = 0; }
            if (r.Next(4) == 0) foreach (var c in w.All) if (r.Next(2) == 0) c.charType = 14;
        });
        MgCoverage("MgHandlers");
    }

    static void MgCloud()
    {
        MgHandlerRuns(new[] { "StinkingCloudHandler" }, 4000, (r, w) =>
        {
            GDVars.gTargetX = (sbyte)r.Next(1, 48); GDVars.gTargetY = (sbyte)r.Next(1, 23);
            // clouds and corpses around the aim cell
            for (var s = GDVars.stinkHead; s != null; s = s.next)
                if (r.Next(2) == 0) { s.ulX = (sbyte)(GDVars.gTargetX + r.Next(-1, 2)); s.ulY = (sbyte)(GDVars.gTargetY + r.Next(-1, 2)); }
            for (int j = 1; j <= 8; j++)
                if (r.Next(2) == 0) { GDVars.downed[j].x = (sbyte)(GDVars.gTargetX + r.Next(0, 2)); GDVars.downed[j].y = (sbyte)(GDVars.gTargetY + r.Next(0, 2)); }
            for (int dy = 0; dy <= 1; dy++) for (int dx = 0; dx <= 1; dx++)
                    if (r.Next(2) == 0) GDVars.arenaPtr.cells[GDVars.gTargetY + dy, GDVars.gTargetX + dx] = (byte)(r.Next(2) == 0 ? 30 : 31);
            foreach (var c in w.Slot.Keys.ToList())
                if (r.Next(3) == 0) { w.X[c] = (sbyte)(GDVars.gTargetX + r.Next(0, 2)); w.Y[c] = (sbyte)(GDVars.gTargetY + r.Next(0, 2)); }
        });
        MgCoverage("MgCloud");
    }

    static void MgBoltSetup(Random r, MgWorld w)
    {
        // a lane of open terrain with walls, off-map edges and creatures on it
        for (int y = 0; y < 25; y++) for (int x = 0; x < 50; x++)
            {
                int k = r.Next(20);
                GDVars.arenaPtr.cells[y, x] = (byte)(k == 0 ? 1 : k == 1 ? 0 : k == 2 ? 10 : k == 3 ? 20 : k == 4 ? 40 : 5);
            }
        // creatures stand on open ground (a caster on impassable terrain can make the source's bolt bounce on its own cell forever)
        foreach (var c in w.Slot.Keys)
            for (int dy = 0; dy <= (c.iconDimensions > 1 ? 1 : 0); dy++)
                for (int dx = 0; dx <= (c.iconDimensions > 1 ? 1 : 0); dx++)
                    if (w.Y[c] + dy < 25 && w.X[c] + dx < 50) GDVars.arenaPtr.cells[w.Y[c] + dy, w.X[c] + dx] = 5;
        GDVars.arenaPtr.cells[0, 0] = 5; // where CombatCellX/Y put a creature that is not in the arena
        if (r.Next(4) == 0 && w.Slot.ContainsKey(GDVars.curChar)) { GDVars.gTargetX = w.X[GDVars.curChar]; GDVars.gTargetY = w.Y[GDVars.curChar]; }
    }

    static void MgBolt()
    {
        MgHandlerRuns(new[] { "LightningBoltHandler", "JavelinOfLightningHandler" }, 2500, MgBoltSetup);
        for (int seed = 1; seed <= 5000; seed++)
        {
            sbyte tx = 0, ty = 0;
            Action<MgWorld> setup = w => { var r = new Random(seed + 707); MgBoltSetup(r, w); tx = (sbyte)r.Next(-2, 52); ty = (sbyte)r.Next(-2, 27); };
            Func<bool, string> run = oracle =>
            {
                var cells = new TCellList();
                if (oracle) MgOracle.Trace(tx, ty, cells); else Magic.TraceLightningBolt(tx, ty, ref cells);
                var s = cells.count + ":";
                for (int i = 1; i <= cells.count; i++) s += cells.x[i] + "," + cells.y[i] + ";";
                return s;
            };
            MgPair(seed, setup, () => run(true), () => run(false), () => "TraceLightningBolt");
        }
        for (int seed = 1; seed <= 4000; seed++)
        {
            TCharRecord dragon = null; TEffectRec eff = null;
            Action<MgWorld> setup = w =>
            {
                var r = new Random(seed + 808);
                MgBoltSetup(r, w);
                dragon = w.All[r.Next(w.All.Count)];
                if (r.Next(4) != 0) GDVars.curChar = dragon; // the bolt runs from curChar, normally the dragon itself
                MgWorld.AddFx(dragon, 88, 0, (byte)r.Next(250, 256), true);
                eff = dragon.effectsAddr;
            };
            MgPair(seed, setup, () => { MgOracle.Dragon(dragon, eff); return ""; }, () => { Magic.DragonBreathHandler(dragon, eff, false); return ""; }, () => "DragonBreath");
        }
        MgCoverage("MgBolt");
    }

    static void MgSeams()
    {
        MgBuild(5);
        Magic.Effects = null;
        GDVars.dsCurrentSpellBeing = 5; GDVars.spellTargetCount = 1; GDVars.dsTargetCreaturePtr[1] = GDVars.curChar;
        bool threw = false;
        try { Magic.ReduceHandler(); } catch (NotSupportedException) { threw = true; }
        Yes(threw, "unbound FxEngine seam throws");
        MgBuild(5);
        Magic.CombatMap = null;
        threw = false;
        try { var cells = new TCellList(); Magic.TraceLightningBolt(3, 3, ref cells); } catch (NotSupportedException) { threw = true; }
        Yes(threw, "unbound CbtMap seam throws");
    }

    static void MgBindings()
    {
        for (int i = 1; i <= 67; i++)
        {
            var h = MagicBindings.SpellHandler(MgHandlerOf[i]);
            Yes(h != null, "handler " + MgHandlerOf[i]);
            EqS(MgHandlerOf[i], h.Method.Name, "handler name " + i);
        }
        Yes(MagicBindings.SpellHandler("Nope") == null, "unknown handler");
        MagicBindings.InstallLightningTracer();
        EqS("TraceLightningBolt", CbtMap.boltTracer.Method.Name, "bolt tracer");
        MagicBindings.BindCombatMap();
        Yes(Magic.CombatMap != null && !(Magic.CombatMap is MgWorld), "map adapter");
        // CbtActs' adapter: Integer idx survives the int/short boundary
        var w = MgBuild(11);
        MagicBindings.BindCbtActs();
        GDVars.curChar.spellsKnown[1] = 1;
        int idx = -1; bool ne = false; TItemRec sc = null;
        MgOracle.Reset();
        byte v = CbtActs.MagicUnit.CastSpellMenu(ref idx, ref ne, ref sc);
        short i2 = -1; bool ne2 = false; TItemRec sc2 = null;
        var w2 = MgBuild(11); GDVars.curChar.spellsKnown[1] = 1;
        byte v2 = MgOracle.Choose(7, 1, ref i2, ref ne2, ref sc2);
        Eq(v2, v, "adapter spell"); Eq(i2, idx, "adapter idx"); Yes(ne == ne2, "adapter notEmpty");
    }
}
