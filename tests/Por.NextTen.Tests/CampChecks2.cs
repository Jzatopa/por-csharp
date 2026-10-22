// Camp (src/camp.pas) checks, part 2: the groups. Each runs oracle/port pairs (CampChecks1.cs) over seeded scenarios,
// plus hand-worked cases whose expected values were worked out from the Pascal text.
using System;
using System.Collections.Generic;
using System.Linq;
using Por.Core;
using static Por.Core.Types;

static partial class Tests
{
    static void CpPrep()
    {
        CpMany("CampSpellPrepTime", 1500, (w, o) => { foreach (var c in w.All) w.L("result", o.PrepTime(c)); },
               w => { foreach (var c in w.All) w.L("result", (int)Camp.CampSpellPrepTime(c)); });
        CpMany("ResetMemorizeScribe", 600, (w, o) => { var c = w.Any(); o.ResetMemorize(c); o.ResetScribe(w.Any()); },
               w => { var c = w.Any(); Camp.ResetMemorize(c); Camp.ResetScribe(w.Any()); });
        CpMany("ResetPartySpellActivities", 400, (w, o) => o.ResetParty(), w => Camp.ResetPartySpellActivities());
        CpMany("SlotsAndPending", 2000,
               (w, o) =>
               {
                   int l = w.R.Next(1, 4), t = w.R.Next(0, 2), fl = w.R.Next(-1, 6), ft = w.R.Next(-1, 3), id = w.R.Next(1, 57);
                   w.L("result", o.SlotsUsed(l, t), o.Remaining(l, t), o.SlotFree(fl, ft), o.PutPending(id));
               },
               w =>
               {
                   int l = w.R.Next(1, 4), t = w.R.Next(0, 2), fl = w.R.Next(-1, 6), ft = w.R.Next(-1, 3), id = w.R.Next(1, 57);
                   bool placed = false;
                   byte used = Camp.SlotsUsed((sbyte)l, (sbyte)t), rem = Camp.SpellSlotsRemaining((sbyte)l, (sbyte)t);
                   bool free = Camp.SlotFree((sbyte)fl, (sbyte)ft);
                   Camp.PutPending((byte)id, ref placed);
                   w.L("result", (int)used, (int)rem, free, placed);
               },
               w => { if (w.Coin()) for (int s = 0; s <= 20; s++) if (GDVars.curChar.memorizedSpells[s] == 0) GDVars.curChar.memorizedSpells[s] = (byte)w.R.Next(1, 57); });
        CpMany("SortRememberQueue", 1500, (w, o) => { o.Sort(); o.Remember(); o.QueueUsual(); },
               w => { Camp.SortMemorizedSpells(); Camp.RememberUsualSpells(); Camp.QueueUsualSpells(); });
        CpMany("QueueUsualSpells", 2000, (w, o) => o.QueueUsual(), w => Camp.QueueUsualSpells(),
               w =>
               {
                   // usual lists of known spells with slots, some already memorized
                   var c = GDVars.curChar;
                   var u = new byte[21];
                   for (int s = 0; s <= 20; s++) u[s] = (byte)(w.R.Next(3) == 0 ? 0 : w.R.Next(1, 60));
                   w.Usual[c] = u;
                   for (int s = 1; s <= 56; s++) if (w.R.Next(2) == 0) c.spellsKnown[s] = 1;
               });
    }

    static void CpActivity()
    {
        CpMany("CanDoSpellActivity", 1500, (w, o) => w.L("result", o.CanDo(w.R.Next(0, 5))), w => w.L("result", Camp.CanDoSpellActivity((byte)w.R.Next(0, 5))),
               w => { if (w.Coin()) GDVars.curChar.status = C_Status.ANIMATED; });
        CpMany("DisplaySpellSlots", 1500, (w, o) => w.L("result", o.SpellSlots()), w => w.L("result", Camp.DisplaySpellSlots()),
               w => { if (w.R.Next(5) == 0) GDVars.curChar.clericSpells[w.R.Next(1, 4)] = (byte)w.R.Next(100, 256); });
        CpMany("NoncombatCastMenu", 1500, (w, o) => o.CastMenu(), w => Camp.NoncombatCastMenu());
    }

    static void CpMemorize()
    {
        CpMany("CampMemorizeMenu", 2500, (w, o) => o.MemorizeMenu(), w => Camp.CampMemorizeMenu());
    }

    static void CpScribeSetup(CpWorld w)
    {
        var c = GDVars.curChar;
        var r = w.R;
        c.classLevels[C_Level.MAGE] = (sbyte)r.Next(1, 10);
        c.status = 0; c.enabled = 1;
        GDVars.zone0.antimagic = 0;
        var ids = Enumerable.Range(1, 56).Where(k => GDConsts.spellTable[k].levelSp >= 1 && GDConsts.spellTable[k].levelSp <= 3).ToList();
        byte id = (byte)ids[r.Next(ids.Count)];
        c.spellsKnown[id] = (byte)(r.Next(5) == 0 ? 1 : 0);
        for (int s = 1; s <= 3; s++) { c.clericSpells[s] = (byte)r.Next(0, 3); c.mageSpells[s] = (byte)r.Next(0, 3); }
        TItemRec head = null, tail = null;
        for (int k = r.Next(1, 6); k > 0; k--)
        {
            // item types 0..7 have random slots; pick readable and unreadable ones
            var it = new TItemRec { itemType = (byte)r.Next(0, 8), name = "t" + k };
            for (int s = 1; s <= 3; s++)
            {
                int pick = r.Next(5);
                it.spells[s] = pick == 0 ? id : pick == 1 ? (byte)(id | 0x80) : pick == 2 ? (byte)r.Next(1, 57) : (byte)0;
            }
            if (head == null) head = it; else tail.next = it;
            tail = it;
        }
        c.itemsAddr = head;
        w.SpellScript = new Queue<byte>();
        if (r.Next(2) == 0) w.SpellScript.Enqueue(0);
        for (int k = r.Next(1, 4); k > 0; k--) w.SpellScript.Enqueue(id);
        w.SpellScript.Enqueue(0);
    }

    static void CpScribe()
    {
        CpMany("CampScribeMenu", 2500, (w, o) => o.ScribeMenu(), w => Camp.CampScribeMenu(),
               w => { if (w.R.Next(4) != 0) GDVars.curChar.classLevels[C_Level.MAGE] = (sbyte)w.R.Next(1, 10); w.Budget = w.R.Next(2, 40); });
        // a mage picking a spell it lacks, with plain, pending and look-alike copies spread over readable and other items
        CpMany("CampScribeTargeted", 2000, (w, o) => o.ScribeMenu(), w => Camp.CampScribeMenu(), CpScribeSetup);
    }

    static void CpDisplay()
    {
        CpMany("CampDisplayMenu", 1200, (w, o) => o.DisplayMenu(), w => Camp.CampDisplayMenu(),
               w => { foreach (var c in w.All) for (int j = w.R.Next(0, 4); j > 0; j--) c.effectsAddr = new TEffectRec { id = (byte)w.R.Next(0, 100), next = c.effectsAddr }; });
    }

    static void CpRest()
    {
        CpMany("CampRestMenu", 1500, (w, o) => { bool i = w.Coin(); o.RestMenu(ref i); w.L("int", i); },
               w => { bool i = w.Coin(); Camp.CampRestMenu(ref i); w.L("int", i); });
        CpMany("CampMagicMenu", 2000, (w, o) => { bool i = w.R.Next(8) == 0; o.MagicMenu(ref i); w.L("int", i); },
               w => { bool i = w.R.Next(8) == 0; Camp.CampMagicMenu(ref i); w.L("int", i); });
    }

    static void CpOrder()
    {
        CpMany("OrderMove", 1500, (w, o) => { if (w.Coin()) o.MoveUp(); else o.MoveDown(); }, w => { if (w.Coin()) Camp.OrderMoveUp(); else Camp.OrderMoveDown(); },
               w => { if (w.R.Next(20) == 0) GDVars.curChar = new TCharRecord { name = "stray" }; });
        CpMany("CampOrderMenu", 1500, (w, o) => o.OrderMenu(), w => Camp.CampOrderMenu(),
               w => { w.Budget = w.R.Next(5, 40); });
        CpMany("CampDropMenu", 1500, (w, o) => o.DropMenu(), w => Camp.CampDropMenu());
        CpMany("CampAlterMenu", 1500, (w, o) => o.AlterMenu(), w => Camp.CampAlterMenu());
    }

    // parties with clerics of every kind: able and not, knowing the cures or not, with wounds and conditions
    static void CpClerics(CpWorld w)
    {
        int[] conds = { C_Effect.DISEASED, C_Effect.DISEASE_STR, C_Effect.ROT_NO_HEAL, C_Effect.BLIND, C_Effect.BESTOW_CURSE };
        foreach (var c in w.All)
        {
            if (w.R.Next(3) != 0) c.classLevels[C_Level.CLERIC] = (sbyte)w.R.Next(1, 10);
            c.spellsKnown[C_Spell.CURE_LIGHT_WOUNDS] = (byte)(w.R.Next(4) == 0 ? 0 : 1);
            c.spellsKnown[C_Spell.CURE_DISEASE] = (byte)w.R.Next(0, 2);
            c.spellsKnown[C_Spell.CURE_BLINDNESS] = (byte)w.R.Next(0, 2);
            c.spellsKnown[C_Spell.REMOVE_CURSE] = (byte)w.R.Next(0, 2);
            c.clericSpells[1] = (byte)w.R.Next(0, 6); c.clericSpells[3] = (byte)w.R.Next(0, 3);
            if (w.R.Next(3) == 0) c.effectsAddr = new TEffectRec { id = (byte)conds[w.R.Next(conds.Length)], next = c.effectsAddr };
            if (w.R.Next(2) == 0) c.hpCur = (byte)w.R.Next(0, c.hpMax);
        }
    }

    static void CpFix()
    {
        CpMany("CampFixMenu", 1500, (w, o) => { bool i = w.Coin(); o.FixMenu(ref i); w.L("int", i); },
               w => { bool i = w.Coin(); Camp.CampFixMenu(ref i); w.L("int", i); });
        CpMany("CampFixClerics", 3000, (w, o) => { bool i = w.Coin(); o.FixMenu(ref i); w.L("int", i); },
               w => { bool i = w.Coin(); Camp.CampFixMenu(ref i); w.L("int", i); }, CpClerics);
    }

    static void CpMain()
    {
        CpMany("CampMainMenu", 2500, (w, o) => { bool i = w.Coin(); o.MainMenu(ref i); w.L("int", i); },
               w => { bool i = w.Coin(); Camp.CampMainMenu(ref i); w.L("int", i); });
        CpMany("CampMainMenuLong", 800, (w, o) => { bool i = false; o.MainMenu(ref i); w.L("int", i); },
               w => { bool i = false; Camp.CampMainMenu(ref i); w.L("int", i); }, w => { w.Budget = 80; CpClerics(w); });
    }

    // Hand-worked cases: expected values computed by hand from the Pascal text.
    static void CpExamples()
    {
        var w = CpBuild(5);
        CpInstall(w);
        var c = w.All[0];
        GDVars.curChar = c;
        GDVars.creatureList = c; c.nextCharAddr = null;
        // prep time: Cure Light Wounds (cleric level 1) and a level-3 spell pending, nothing on scrolls: 6 hours + (1+3)*15
        int l3 = Enumerable.Range(1, 56).First(k => GDConsts.spellTable[k].levelSp == 3);
        for (int s = 0; s <= 20; s++) c.memorizedSpells[s] = 0;
        c.itemsAddr = null;
        c.memorizedSpells[0] = (byte)(C_Spell.CURE_LIGHT_WOUNDS | 0x80);
        c.memorizedSpells[5] = (byte)(l3 | 0x80);
        c.memorizedSpells[6] = (byte)l3;
        Eq(GDConsts.spellTable[C_Spell.CURE_LIGHT_WOUNDS].levelSp, 1, "Cure Light Wounds is level 1");
        Eq(6 * 60 + 4 * 15, Camp.CampSpellPrepTime(c), "prep: 6 hours and 4 spell levels");
        Eq(6, c.spellPrepTime, "prep hours stored");
        c.memorizedSpells[5] = 0;
        Eq(4 * 60 + 15, Camp.CampSpellPrepTime(c), "prep: 4 hours and 1 level");
        c.memorizedSpells[0] = 0;
        Eq(0, Camp.CampSpellPrepTime(c), "nothing pending: no time");
        // sort keeps the pending bit with its spell
        for (int s = 0; s <= 20; s++) c.memorizedSpells[s] = 0;
        c.memorizedSpells[0] = 0x80 | 9; c.memorizedSpells[1] = 3; c.memorizedSpells[2] = 0x80 | 1;
        Camp.SortMemorizedSpells();
        EqS(string.Join(".", Enumerable.Repeat("0", 18)) + ".129.3.137", string.Join(".", c.memorizedSpells.Data), "sorted with flags kept");
        // party order: A B C D with C current: Up gives A C B D; Down from D wraps it to the front
        var a = new TCharRecord { name = "A" }; var bb = new TCharRecord { name = "B" }; var cc = new TCharRecord { name = "C" }; var d = new TCharRecord { name = "D" };
        a.nextCharAddr = bb; bb.nextCharAddr = cc; cc.nextCharAddr = d;
        Func<string> order = () => { var s = ""; for (var p = GDVars.creatureList; p != null; p = p.nextCharAddr) s += p.name; return s; };
        GDVars.creatureList = a; GDVars.curChar = cc; Camp.OrderMoveUp(); EqS("ACBD", order(), "C moves up");
        GDVars.curChar = a; Camp.OrderMoveUp(); EqS("CBDA", order(), "the first wraps to the end");
        GDVars.curChar = a; Camp.OrderMoveDown(); EqS("ACBD", order(), "the last wraps to the front");
        GDVars.curChar = bb; Camp.OrderMoveDown(); EqS("ACDB", order(), "B moves down");
        // DisplaySpellSlots: 2 level-1 cleric slots, one used; magic-user none: one line, columns 20, 23, 26
        GDVars.curChar = c; GDVars.creatureList = c;
        for (int s = 0; s <= 20; s++) c.memorizedSpells[s] = 0;
        c.memorizedSpells[0] = C_Spell.CURE_LIGHT_WOUNDS;
        c.clericSpells[1] = 2; c.clericSpells[2] = 0; c.clericSpells[3] = 1; c.mageSpells[1] = c.mageSpells[2] = c.mageSpells[3] = 0;
        w.Log.Clear();
        Yes(Camp.DisplaySpellSlots(), "has an allowance");
        Yes(w.Log.Contains("Str(1,20,10,\"    Cleric Spells:\")") && w.Log.Contains("Str(20,20,10,\"1\")") && w.Log.Contains("Str(23,20,10,\" \")") &&
            w.Log.Contains("Str(26,20,10,\"1\")") && !w.Log.Any(x => x.Contains("Magic-User")), "cleric line only");
        // Fix: one able cleric with 3 level-1 slots, a member 10 hp down: (2*10+8) div 9 = 3 casts, 4 h + 45 min of rest
        var cl = new TCharRecord { name = "Cl", status = 0, hpMax = 10, hpCur = 10 };
        cl.classLevels[C_Level.CLERIC] = 3; cl.spellsKnown[C_Spell.CURE_LIGHT_WOUNDS] = 1; cl.clericSpells[1] = 3;
        var hurt = new TCharRecord { name = "H", status = C_Status.UNCONSCIOUS, hpMax = 30, hpCur = 20 };
        cl.nextCharAddr = hurt; GDVars.creatureList = cl; GDVars.curChar = cl;
        TClock seen = null;
        Camp.CampRestCall = x => { seen = new TClock((ushort[])GDVars.dsRestingTimeGbvm.Data.Clone()); for (int i = 0; i < 7; i++) GDVars.dsRestingTimeGbvm[i] = 0; return false; };
        int rolls = 0;
        Camp.RollDiceCall = (n, s) => { rolls++; return 4; };
        Camp.RestoreHitPointsCall = (cr, hp, by) => { cr.hpCur = (byte)(cr.hpCur + hp); return true; };
        bool intr = true;
        Camp.CampFixMenu(ref intr);
        Yes(!intr, "rest not interrupted");
        Eq(4, seen[C_Time.HOURS], "rest hours"); Eq(4, seen[C_Time.TURNS], "rest turns (45 minutes)"); Eq(5, seen[C_Time.MINUTES], "rest minutes");
        Eq(3, rolls, "three Cure Light Wounds"); Eq(30, hurt.hpCur, "12 hp pool heals the 10 missing");
    }

    static void CpSeams()
    {
        var expect = new Dictionary<string, string>
        {
            {"DisplayStringCall","TextRend.DisplayString"},{"ShowMessageCall","TextRend.ShowMessage"},{"ClearBottomLineCall","TextRend.ClearBottomLine"},
            {"DrawBorderCall","Border.DrawBorder"},{"ClearBoxCall","Border.ClearBox"},{"DrawWindowCall","Border.DrawWindow"},
            {"PrintNameStringCall","CharDisp.PrintNameString"},{"DisplayNoncombatInterfaceCall","CharDisp.DisplayNoncombatInterface"},
            {"PrintMiscInfoCall","CharDisp.PrintMiscInfo"},{"DisplayPartyCall","CharDisp.DisplayParty"},{"MessageWaitCall","CharDisp.MessageWait"},
            {"ClearMessageWindowCall","CharDisp.ClearMessageWindow"},{"PrintBottomLineCall","CharDisp.PrintBottomLine"},
            {"PrintMessageWaitCall","CharDisp.PrintMessageWait"},{"MenuHorizontalCall","MenuCore.MenuHorizontal"},{"MenuVerticalCall","MenuCore.MenuVertical"},
            {"YesNoMenuCall","MenuCore.YesNoMenu"},{"AppendMenuLineCall","MenuCore.AppendMenuLine"},{"FreeStringListCall","MenuCore.FreeStringList"},
            {"PlayerPortraitsHereCall","Portrait.PlayerPortraitsHere"},{"SpellMenuCall","Magic.SpellMenu"},{"CastSpellMenuCall","Magic.CastSpellMenu"},
            {"CastSpellNCCall","Magic.CastSpellNC"},{"UseItemSpellCall","Magic.UseItemSpell"},{"CureDiseaseHandlerCall","Magic.CureDiseaseHandler"},
            {"CureBlindnessHandlerCall","Magic.CureBlindnessHandler"},{"RemoveCurseSpellCall","Magic.RemoveCurseSpell"},
            {"RecalcStatsCall","CharHlp.RecalcStats"},{"RestoreHitPointsCall","FxEngine.RestoreHitPoints"},{"CampRestCall","GameTime.CampRest"},
            {"GetUsualSpellsCall","UsualSpl.GetUsualSpells"},{"SetUsualSpellsCall","UsualSpl.SetUsualSpells"},
            {"SelectPrevNextCharCall","Party.SelectPrevNextChar"},{"RemoveCreatureCall","SaveLoad.RemoveCreature"},{"SaveGameCall","SaveLoad.SaveGame"},
            {"ExitGameCall","GameIo.ExitGame"},{"ToolsMenuCall","CompTool.ToolsMenu"},{"OptionsMenuCall","Options.OptionsMenu"},
            {"CustomizeIconCall","CharGen.CustomizeIcon"},{"CustomizePortraitCall","CharGen.CustomizePortrait"},{"ViewCharMenuCall","ViewChar.ViewCharMenu"},
            {"RollDiceCall","Dice.RollDice"},
        };
        int seen = 0;
        foreach (var f in typeof(Camp).GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic))
        {
            var dl = f.GetValue(null) as Delegate;
            if (dl == null || !f.Name.EndsWith("Call")) continue;
            seen++;
            Yes(expect.ContainsKey(f.Name), "call point " + f.Name + " expected");
            EqS(expect[f.Name], dl.Method.DeclaringType.Name + "." + dl.Method.Name, f.Name + " default");
        }
        Eq(expect.Count, seen, "call points");
    }
}
