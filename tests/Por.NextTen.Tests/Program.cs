// Source-derived checks for the units converted in the "next ten" pass (October 21, 2026). Expected values come from separate models written
// from the Pascal source text in these files, not from the production routines under test.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Por.Core;

class Halted : Exception { }

static partial class Tests
{
    static long checks;
    static Random rng = new Random(20261022);

    static void Yes(bool v, string what) { checks++; if (!v) throw new Exception("FAILED: " + what); }
    static void Eq(long expected, long actual, string what) { checks++; if (expected != actual) throw new Exception("FAILED: " + what + " expected " + expected + " got " + actual); }
    static void EqS(string expected, string actual, string what) { checks++; if (expected != actual) throw new Exception("FAILED: " + what + " expected [" + expected + "] got [" + actual + "]"); }
    static byte B(int x) { return unchecked((byte)x); }
    static sbyte SB(int x) { return unchecked((sbyte)x); }
    static ushort W(int x) { return unchecked((ushort)x); }

    static int Main(string[] args)
    {
        PristineStatics = SnapStatics();
        var all = new List<KeyValuePair<string, Action>>();
        Action<string, Action> add = (n, a) => all.Add(new KeyValuePair<string, Action>(n, a));
        add("CharHlpSimple", CharHlpSimple);
        add("CharHlpLists", CharHlpLists);
        add("CharHlpDamage", CharHlpDamage);
        add("CharHlpCombat", CharHlpCombat);
        add("CharHlpSpells", CharHlpSpells);
        add("CharHlpWeapons", CharHlpWeapons);
        add("CharHlpCaps", CharHlpCaps);
        add("CharHlpEquip", CharHlpEquip);
        add("CharHlpRecalc", CharHlpRecalc);
        add("CharHlpLabel", CharHlpLabel);
        add("WildsView", WildsView);
        add("SoundTime", SoundTime);
        add("SoundDelay", SoundDelayChecks);
        add("SoundCommand", SoundCommandChecks);
        add("SoundMusic", SoundMusicChecks);
        add("SaveMenuInfo", SaveMenuInfo);
        add("SaveMenuTime", SaveMenuTime);
        add("SaveMenuChoose", SaveMenuChoose);
        add("SaveMenuAsk", SaveMenuAsk);
        add("SaveMenuDir", SaveMenuDir);
        add("VaultFiles", VaultFiles);
        add("VaultItems", VaultItems);
        add("VaultRead", VaultRead);
        add("VaultWrite", VaultWrite);
        add("MapViewWild", MapViewWild);
        add("MapViewMain", MapViewMain);
        add("CharDispNumbers", CharDispNumbers);
        add("CharDispName", CharDispName);
        add("CharDispParty", CharDispParty);
        add("CharDispMessages", CharDispMessages);
        add("CharDispInfo", CharDispInfo);
        add("CharDispScreens", CharDispScreens);
        add("CharDispDelayHost", CharDispDelayHost);
        add("CharDispIcons", CharDispIcons);
        add("CharDispAnim", CharDispAnim);
        add("CharDispFlash", CharDispFlash);
        add("CharDispSelect", CharDispSelect);
        add("IntroDelayOrKey", IntroDelayOrKey);
        add("IntroCredits", IntroCredits);
        add("IntroIntro", IntroIntro);
        add("JrnlViewSessions", JrnlViewSessions);
        add("JrnlViewPreviewEdge", JrnlViewPreviewEdge);
        add("MoneyCapacity", MoneyCapacity);
        add("MoneyPay", MoneyPay);
        add("MoneyMove", MoneyMove);
        add("MoneyInput", MoneyInput);
        add("MoneyMenus", MoneyMenus);
        add("MoneyAppraise", MoneyAppraise);
        add("MoneyShareEdge", MoneyShareEdge);
        add("TempleBindingInstalled", TempleBindingInstalled);
        add("SoundWrappers", SoundWrappers);
        add("BootChecks", BootChecks);
        add("MainChecks", MainChecks);
        add("TempleChecks", TempleChecks);
        add("MoneyFifteen", MoneyFifteen);
        add("BindingChecks", BindingChecks);
        add("DRest",DRest);
        add("DSound",DSound);
        add("DOptions",DOptions);
        add("DInit",DInit);
        add("DCharEdit",DCharEdit);
        add("DSave",DSave);
        add("CbtRoundStart", () => CbGuarded(CbtRoundStart));
        add("CbtDamage", () => CbGuarded(CbtDamage));
        add("CbtResult", () => CbGuarded(CbtResult));
        add("CbtDoAttack", () => CbGuarded(CbtDoAttack));
        add("CbtAttack", () => CbGuarded(CbtAttack));
        add("CbtMove", () => CbGuarded(CbtMove));
        add("CbtFlee", () => CbGuarded(CbtFlee));
        add("CbtSweep", () => CbGuarded(CbtSweep));
        add("CbtTurn", () => CbGuarded(CbtTurn));
        add("CbtTurnMany", () => CbGuarded(CbtTurnMany));
        add("CbtSpells", () => CbGuarded(CbtSpells));
        add("CbtAim", () => CbGuarded(CbtAim));
        add("CbtAcquire", () => CbGuarded(CbtAcquire));
        add("CbtHandlers", () => CbGuarded(CbtHandlers));
        add("CbtSeams", () => CbGuarded(CbtSeams));
        add("MgLists", () => MgGuarded(MgLists));
        add("MgSelect", () => MgGuarded(MgSelect));
        add("MgCast", () => MgGuarded(MgCast));
        add("MgItems", () => MgGuarded(MgItems));
        add("MgResolve", () => MgGuarded(MgResolve));
        add("MgHandlers", () => MgGuarded(MgHandlers));
        add("MgCloud", () => MgGuarded(MgCloud));
        add("MgBolt", () => MgGuarded(MgBolt));
        add("MgSeams", () => MgGuarded(MgSeams));
        add("MgBindings", () => MgGuarded(MgBindings));
        add("EhRolls", () => EhGuarded(EhRolls));
        add("EhFoes", () => EhGuarded(EhFoes));
        add("EhWeapons", () => EhGuarded(EhWeapons));
        add("EhTimed", () => EhGuarded(EhTimed));
        add("EhTouch", () => EhGuarded(EhTouch));
        add("EhGaze", () => EhGuarded(EhGaze));
        add("EhDrain", () => EhGuarded(EhDrain));
        add("EhItems", () => EhGuarded(EhItems));
        add("EhDisplacedReadyNil", EhDisplacedReadyNil);
        add("EhCloud", () => EhGuarded(EhCloud));
        add("EhResist", () => EhGuarded(EhResist));
        add("EhExamples", () => EhGuarded(EhExamples));
        add("EhSeams", () => EhGuarded(EhSeams));
        add("EhBindings", () => EhGuarded(EhBindings));
        add("VcSheet", () => VcGuarded(VcSheet));
        add("VcPaladin", () => VcGuarded(VcPaladin));
        add("VcStatsPages", () => VcGuarded(VcStatsPages));
        add("VcMenu", () => VcGuarded(VcMenu));
        add("VcItems", () => VcGuarded(VcItems));
        add("VcItemOps", () => VcGuarded(VcItemOps));
        add("VcCoins", () => VcGuarded(VcCoins));
        add("VcExamples", () => VcGuarded(VcExamples));
        add("VcSeamsAndBindings", () => VcGuarded(VcSeamsAndBindings));
        add("CpPrep", () => CpGuarded(CpPrep));
        add("CpActivity", () => CpGuarded(CpActivity));
        add("CpMemorize", () => CpGuarded(CpMemorize));
        add("CpScribe", () => CpGuarded(CpScribe));
        add("CpDisplay", () => CpGuarded(CpDisplay));
        add("CpRest", () => CpGuarded(CpRest));
        add("CpOrder", () => CpGuarded(CpOrder));
        add("CpFix", () => CpGuarded(CpFix));
        add("CpMain", () => CpGuarded(CpMain));
        add("CpExamples", () => CpGuarded(CpExamples));
        add("CpSeams", () => CpGuarded(CpSeams));
        add("DExplore",DExplore);
        add("DPost",DPost);
        add("DCombat",DCombat);
        add("DMenusAndBindings",DMenusAndBindings);
        add("StartupCheckRealPath", StartupCheckRealPath);
        add("StartupCheckNamesMissing", StartupCheckNamesMissing);
        add("StaticSeamInventory", StaticSeamInventory);
        add("DesktopSeamsRealBindings", DesktopSeamsRealBindings);
        int failed = 0;
        foreach (var t in all)
        {
            if (args.Length > 0 && !args.Contains(t.Key)) continue;
            long before = checks;
            try { t.Value(); Console.WriteLine("PASS " + t.Key + " (" + (checks - before) + " assertions)"); }
            catch (Exception e) { failed++; Console.WriteLine("FAIL " + t.Key + ": " + e); }
        }
        Console.WriteLine("NextTen source-derived assertions: " + checks + ", failed groups: " + failed);
        return failed == 0 ? 0 : 1;
    }
}
