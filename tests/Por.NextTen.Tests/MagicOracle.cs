// Oracle model of src/magic.pas, written from the Pascal text for the Magic checks. It does not call Magic. Fakes for
// FxEngine, CbtMap and every side-effecting converted unit are reached through the recording world W, the same world the
// port's seams and call points are bound to, so both sides produce comparable call logs.
using System;
using System.Collections.Generic;
using System.Linq;
using Por.Core;
using static Por.Core.Types;

static class MgOracle
{
    public static MgWorld W;
    // unit variables
    public static TItemRec spellList;
    public static byte[] spellIds = new byte[69];
    public static TItemRec[] spellScrolls = new TItemRec[69];
    public static TItemRec[] scrollPtrs = new TItemRec[49];
    public static byte scrollCount;
    public static TItemRec curScroll;

    public static void Reset()
    {
        spellList = null; spellIds = new byte[69]; spellScrolls = new TItemRec[69]; scrollPtrs = new TItemRec[49]; scrollCount = 0; curScroll = null;
    }
    public static string State()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("list=").Append(MgWorld.ListSig(spellList));
        sb.Append(" ids=").Append(string.Join(",", spellIds));
        sb.Append(" scrolls=").Append(string.Join(",", spellScrolls.Select(MgWorld.F)));
        sb.Append(" ptrs=").Append(string.Join(",", scrollPtrs.Select(MgWorld.F)));
        sb.Append(" count=").Append(scrollCount).Append(" cur=").Append(MgWorld.F(curScroll));
        return sb.ToString();
    }

    static byte u8(int v) { return (byte)(v & 0xFF); }
    static sbyte s8(int v) { v &= 0xFF; return (sbyte)(v > 127 ? v - 256 : v); }
    static short s16(int v) { v &= 0xFFFF; return (short)(v > 32767 ? v - 65536 : v); }
    static string Cut(string s, int n) { return s.Length <= n ? s : s.Substring(0, n); }
    static TSpellRec T(int id) { return GDConsts.spellTable[id]; }
    static TCharRecord Tgt(int i) { return GDVars.dsTargetCreaturePtr[i]; }
    static byte Being { get { return GDVars.dsCurrentSpellBeing; } }
    static bool Combat { get { return GDVars.ifcState == 5; } }
    static byte CL(int spell) { return CharHlp.GetCasterLevel((byte)spell); }

    // ---------------- spell lists ----------------
    public static char Mark(TCharRecord cr, int spell, int mark)
    {
        int id = spell & 0x7F;
        if (id < 1 || id > 56) return ' ';
        if (mark == 1) return cr.spellsKnown[id] != 0 ? '-' : ' ';
        if (mark == 2)
        {
            if (cr.spellsKnown[id] != 0) return ' ';
            var s = T(id);
            bool learn = false;
            if (s.levelSp >= 1 && s.levelSp <= 3)
            {
                if (s.castClassSp == 0) learn = cr.classLevels[0] > 0;
                else if (s.castClassSp == 1) learn = cr.classLevels[5] > 0;
            }
            return learn ? '+' : ' ';
        }
        return ' ';
    }

    public static void AddSpell(TCharRecord cr, int spell, int mark)
    {
        TItemRec node;
        int count, lastLevel;
        if (spellList == null)
        {
            node = new TItemRec(); spellList = node; lastLevel = 0; count = 0;
        }
        else
        {
            // number of selectable entries before the tail, plus the tail itself
            var all = new List<TItemRec>();
            for (var q = spellList; q != null; q = q.next) all.Add(q);
            count = 1 + all.Take(all.Count - 1).Count(q => q.inUse == 0);
            if (count > 68) return;
            lastLevel = T(spellIds[count - 1]).levelSp;
            node = new TItemRec();
            all[all.Count - 1].next = node;
        }
        int lvl = T(spell & 0x7F).levelSp;
        if (lvl != lastLevel)
        {
            node.name = Cut(GDConsts.spellLevelNames[lvl], 40); node.inUse = 1;
            var n2 = new TItemRec(); node.next = n2; node = n2;
        }
        node.name = spell > 0x7F ? " *" : " " + Mark(cr, spell, mark);
        node.name = Cut(node.name + GDConsts.spellNames[spell & 0x7F], 40);
        node.inUse = 0;
        spellIds[count] = u8(spell & 0x7F);
    }

    static void ScrollSpells(TCharRecord cr, bool forScribe, int mark)
    {
        if (!CharHlp.ScrollLegible(cr, curScroll)) return;
        for (int i = 1; i <= 3; i++)
        {
            int v = curScroll.spells[i];
            bool take = forScribe ? v > 0x80 : v > 0;
            if (!take) continue;
            AddSpell(cr, v, mark);
            if (scrollCount <= 48) { scrollPtrs[scrollCount] = curScroll; scrollCount = u8(scrollCount + 1); }
        }
    }

    static void AllScrollSpells(TCharRecord cr, bool forScribe, int mark)
    {
        for (curScroll = cr.itemsAddr; curScroll != null; curScroll = curScroll.next)
            if (CharHlp.ItemReadable(curScroll)) ScrollSpells(cr, forScribe, mark);
    }

    static void ScrollLine(TCharRecord cr, TItemRec item, int slot, ref int lines)
    {
        if (lines > 68) return;
        AddSpell(cr, item.spells[slot], 0);
        spellScrolls[lines] = item;
        lines = u8(lines + 1);
        var tail = spellList; while (tail.next != null) tail = tail.next;
        if (tail.name.Length >= 2) { var ch = tail.name.ToCharArray(); ch[1] = '-'; tail.name = new string(ch); }
    }

    static void Available(TCharRecord cr)
    {
        bool memOk = !Combat || cr.combatAddr.castAllowed != 0;
        bool scrollOk = CharHlp.CanUseItems(cr);
        int lines = 0;
        for (int lvl = 1; lvl <= 7; lvl++)
        {
            if (memOk)
                for (int i = 0; i <= 20; i++)
                {
                    int id = cr.memorizedSpells[i];
                    if (id >= 1 && id <= 56 && T(id).levelSp == lvl && lines <= 68) { AddSpell(cr, id, 0); lines = u8(lines + 1); }
                }
            if (scrollOk)
                for (var it = cr.itemsAddr; it != null; it = it.next)
                {
                    if (!CharHlp.CastableScroll(cr, it)) continue;
                    for (int i = 1; i <= 3; i++)
                    {
                        int id = CharHlp.ScrollSpellAt(it, (byte)i);
                        if (id != 0 && T(id).levelSp == lvl) ScrollLine(cr, it, i, ref lines);
                    }
                }
        }
    }

    public static bool Generate(TCharRecord cr, int menuType)
    {
        spellList = null;
        for (int i = 0; i <= 68; i++) { spellIds[i] = 0; spellScrolls[i] = null; }
        for (int i = 0; i <= 48; i++) scrollPtrs[i] = null;
        scrollCount = 0;
        if (menuType == 7) Available(cr);
        else if (menuType == 0) { for (int i = 0; i <= 20; i++) { int v = cr.memorizedSpells[i]; if (v > 0 && v < 0x80) AddSpell(cr, v, 0); } }
        else if (menuType == 5) { for (int i = 0; i <= 20; i++) { int v = cr.memorizedSpells[i]; if (v > 0x7F) AddSpell(cr, v, 0); } }
        else if (menuType == 1) { for (int i = 1; i <= 56; i++) if (cr.spellsKnown[i] != 0) AddSpell(cr, i, 0); }
        else if (menuType == 2) ScrollSpells(cr, false, 2);
        else if (menuType == 3) AllScrollSpells(cr, false, 1);
        else if (menuType == 6) AllScrollSpells(cr, true, 0);
        else if (menuType == 4)
        {
            for (int i = 1; i <= 56; i++)
            {
                var s = T(i);
                if (s.castClassSp < 0 || s.castClassSp > 1 || s.levelSp < 1 || s.levelSp > 3) continue;
                if (cr.slots[s.castClassSp, s.levelSp] > 0 && cr.spellsKnown[i] == 0) AddSpell(cr, i, 0);
            }
        }
        var p = spellList;
        while (p != null && p.inUse != 0) p = p.next;
        if (p != null) return true;
        if (spellList != null) { W.L("FreeList", MgWorld.ListSig(spellList)); spellList = null; }
        return false;
    }

    public static byte Selection(int selType, ref short idx, ref TItemRec scroll)
    {
        string[] titles = { "", "Cast", "Memorize", "Scribe", "Learn" };
        string title = selType >= 1 && selType <= 4 ? titles[selType] : "";
        string prompt = selType >= 1 && selType <= 4 ? "Choose Spell: " : "";
        int bottom = selType == 2 ? 15 : 22;
        bool addExit = selType != 4;
        bool redraw = false;
        if (idx < 0)
        {
            W.L("DrawWindow", (sbyte)1, (sbyte)3, (sbyte)38, (sbyte)bottom, (byte)0, (byte)0, "");
            redraw = true;
            GDVars.vertTop = 0;
            int k = 0;
            var p = spellList;
            while (p != null && p.inUse != 0) { p = p.next; k++; }
            idx = p == null ? (short)0 : s16(k);
        }
        if (selType == 4 || selType == 1) redraw = true;
        TItemRec sel = null;
        byte ch;
        var stop = new byte[] { 0, 13, (byte)'C', (byte)'E', (byte)'L', (byte)'M', (byte)'S' };
        do
        {
            MenuCore.listOnly = selType == 5;
            ch = W.MenuV(prompt, title, 13, 10, 15, 1, 5, 38, (sbyte)bottom, spellList, addExit, ref redraw, ref idx, ref sel);
        } while (Array.IndexOf(stop, ch) < 0);
        int count = 0;
        for (var p = spellList; p != sel; p = p.next) if (p.inUse == 0) count = u8(count + 1);
        scroll = null;
        byte res;
        if (ch == 0 || ch == 'E' || count > 68) res = 0;
        else { res = spellIds[count]; scroll = spellScrolls[count]; }
        if (selType == 3) curScroll = count <= 48 ? scrollPtrs[count] : null;
        W.L("FreeList", MgWorld.ListSig(spellList));
        return res;
    }

    public static byte Choose(int menuType, int selType, ref short idx, ref bool notEmpty, ref TItemRec scroll)
    {
        string[] words = { "in Memory", "in Spell Book", "on Scroll", "on Scrolls", "to choose from", "to be memorized", "to be scribed", "" };
        string s = menuType >= 0 && menuType <= 7 ? words[menuType] : "";
        notEmpty = Generate(GDVars.curChar, menuType);
        if (!notEmpty) { scroll = null; return 0; }
        if (idx < 0)
        {
            W.L("DrawWindow", (sbyte)1, (sbyte)1, (sbyte)38, (sbyte)1, (byte)0, (byte)0, "");
            W.L("DCN", GDVars.curChar, (byte)1, (byte)1, true);
            s = menuType == 7 ? "Available Spells" : Cut("Spells " + s, 40);
            W.L("DS", u8(GDVars.curChar.name.Length + 4), (byte)1, (byte)10, s);
        }
        return Selection(selType, ref idx, ref scroll);
    }

    // ---------------- durations and resolution ----------------
    public static ushort Duration(int spell)
    {
        int d;
        if (spell == 40) d = Dice.RollDice(1, 6) * 10;
        else if (spell == 57 || spell == 61) d = Dice.RollDice(5, 4);
        else if (spell == 59) d = Dice.RollDice(1, 4) * 10 + 40;
        else if (spell == 63) d = Combat ? Dice.RollDice(2, 10) * 10 : (Dice.RollDice(1, 10) + 10) * 10;
        else if (spell == 67) d = 1440;
        else d = T(spell).durFixed + T(spell).durVar * CL(spell);
        return (ushort)(d & 0xFFFF);
    }

    public static void Resolve(int spell, int eff, bool onRemove, int damage, int elem, string msg)
    {
        msg = Cut(msg, 40);
        GDVars.elementMask = damage == 0 ? (byte)0 : u8(elem);
        if (GDVars.spellTargetCount == 0) return;
        byte effLevel = eff == 0 ? CL(spell) : u8(eff);
        int n = GDVars.spellTargetCount;
        for (int i = 1; i <= n; i++)
        {
            var t = Tgt(i);
            if (t == null) continue;
            bool saved = T(spell).saveResult == 0 ? false : W.RollSavingThrow(t, s8(T(spell).saveType), 0);
            if (T(Being).rangeFixed == 255)
            {
                W.L("RecalcStats", t);
                W.TriggerEffectsWithCondition(11, t);
                if (!W.AttackRoll(GDVars.curChar, t, t.acCur)) { damage = 0; saved = true; }
            }
            if (damage > 0) W.InflictNonWeaponDamage(t, u8(damage), T(spell).saveResult, saved);
            if (T(spell).effect > 0)
                W.AddEffectWithMessage(t, T(spell).effect, Duration(spell), effLevel, onRemove, T(spell).saveResult, saved, msg);
        }
        GDVars.elementMask = 0;
    }

    // ---------------- casting ----------------
    public static void NoncombatTargets(int spell, bool b, ref bool cast)
    {
        if (GDVars.castTarget == null) GDVars.castTarget = GDVars.curChar;
        GDVars.dsTargetCreaturePtr[1] = GDVars.curChar;
        GDVars.spellTargetCount = 1;
        cast = true;
        int mode = T(spell).target;
        if (mode == 1) return;
        if (mode == 2)
        {
            W.L("NoncombatIfc");
            W.SelectChar("Cast Spell on whom", true, ref GDVars.castTarget);
            if (GDVars.castTarget == null) { GDVars.dsTargetCreaturePtr[1] = null; GDVars.spellTargetCount = 0; cast = false; }
            else GDVars.dsTargetCreaturePtr[1] = GDVars.castTarget;
            return;
        }
        if (mode == 4)
        {
            GDVars.dsTargetCreaturePtr[1] = GDVars.creatureList;
            while (GDVars.dsTargetCreaturePtr[GDVars.spellTargetCount].nextCharAddr != null)
            {
                GDVars.dsTargetCreaturePtr[GDVars.spellTargetCount + 1] = GDVars.dsTargetCreaturePtr[GDVars.spellTargetCount].nextCharAddr;
                GDVars.spellTargetCount = u8(GDVars.spellTargetCount + 1);
            }
            return;
        }
        cast = false;
    }

    public static void ShowCombatOnly(int spell)
    {
        W.L("DS", (byte)1, (byte)19, (byte)10, GDConsts.spellNames[spell]);
        W.L("DS", (byte)1, (byte)20, (byte)10, "is a combat-only spell...");
    }

    public static void CastMsg(TCharRecord cr, string msg, int spell)
    {
        msg = Cut(msg, 40);
        if (Combat)
        {
            W.L("PNS", cr, "Casts a Spell", (byte)10, true);
            W.L("ClearBox", (sbyte)0, (sbyte)23, (sbyte)39, (sbyte)23);
            W.L("DS", (byte)0, (byte)23, (byte)10, "Spell:" + GDConsts.spellNames[spell]);
            return;
        }
        W.L("ClearBox", (sbyte)1, (sbyte)18, (sbyte)38, (sbyte)22);
        W.L("DCN", cr, (byte)1, (byte)19, false);
        W.L("DS", u8(cr.name.Length + 2), (byte)19, (byte)10, msg);
        W.L("DS", (byte)1, (byte)20, (byte)10, GDConsts.spellNames[spell]);
        W.L("Pause");
        W.L("CMW");
    }

    public static void Cast(int spell, bool b, bool ai, ref bool sel)
    {
        var me = GDVars.curChar;
        bool ok = true;
        if (!Combat && T(spell).target == 0)
        {
            if (!GDVars.usingItem)
            {
                ShowCombatOnly(spell);
                if (W.YesNo("Lose it? ", 13, 15, 10) == 'Y') W.L("RemoveSpell", GDVars.curChar, (byte)spell);
            }
            else
            {
                W.L("DS", (byte)1, (byte)19, (byte)10, "That Item");
                W.L("DS", (byte)1, (byte)20, (byte)10, "is a combat-only item...");
                if (W.YesNo("Use it? ", 13, 15, 10) == 'Y') sel = true;
            }
            ai = false; ok = false;
        }
        if (ai && !GDVars.usingItem) CastMsg(me, "casts", spell);
        while (ok)
        {
            GDVars.castTargetMenu((byte)spell, b, ref sel);
            if (!sel)
            {
                if (!Combat) ok = false;
                else if (b || W.YesNo("Abort Spell? ", 14, 10, 15) == 'Y')
                {
                    W.L("Bottom", "Spell Aborted");
                    if (!GDVars.usingItem) W.L("RemoveSpell", GDVars.curChar, (byte)spell);
                    ok = false;
                }
                continue;
            }
            ok = false;
            if (Combat)
            {
                W.L("BuildIcon", (byte)18);
                sbyte x = W.CombatCellX(me), y = W.CombatCellY(me);
                byte k = 0;
                while (!Pathing.IsDirToward(x, y, GDVars.gTargetX, GDVars.gTargetY, k)) k = u8(k + 1);
                W.SetCreatureFacing(me, k, 1, 0);
                W.L("AutoStop", (ushort)0);
                ushort snd = spell == 47 ? GDConsts.soundIds.combat[7] : spell == 51 ? GDConsts.soundIds.combat[4] : GDConsts.soundIds.commands[3];
                W.L("Snd", snd);
                W.L("Fly", x, y, GDVars.gTargetX, GDVars.gTargetY, (byte)4, (byte)30);
                W.L("AutoStop", (ushort)10);
                if (W.CombatIconVisible(me, 0))
                {
                    W.SetCreatureFacing(me, me.combatAddr.direction, 1, 1);
                    W.SetCreatureFacing(me, me.combatAddr.direction, 0, 0);
                }
            }
            W.RemoveInvisibility(me);
            if (!GDVars.usingItem) { W.L("RemoveSpell", GDVars.curChar, (byte)spell); W.L("StatsCount", me, (byte)10, 1); }
            GDVars.dsCurrentSpellBeing = (byte)spell;
            GDVars.spellFns[GDVars.dsCurrentSpellBeing]();
            GDVars.dsCurrentSpellBeing = 0;
            GDVars.gMultiTarget = 0;
        }
        W.L("CMW");
        if (Combat) W.L("ClearBox", (sbyte)0, (sbyte)23, (sbyte)39, (sbyte)23);
    }

    // ---------------- handlers ----------------
    static void OneSide(int team, string msg)
    {
        GDVars.gMultiTarget = 1;
        int n = GDVars.spellTargetCount;
        for (int i = 1; i <= n; i++)
        {
            var t = Tgt(i);
            bool drop = t.hostile != team;
            if (!drop && Being == 1 && Combat) drop = W.CountTargets(t, 1) > 0;
            if (drop) GDVars.dsTargetCreaturePtr[i] = null;
        }
        Resolve(Being, 0, false, 0, 0, msg);
    }

    static void Simple(string msg) { Resolve(Being, 0, false, 0, 0, msg); }

    static void Charm()
    {
        var t = Tgt(1);
        if (t.charType > 1 || t.iconDimensions > 1) { W.L("PNS", t, "is unaffected", (byte)10, true); return; }
        Resolve(Being, u8(GDVars.curChar.hostile * 128 + CL(Being)), true, 0, 0, "is charmed");
        TEffectRec e;
        if (CharHlp.HasEffect(t, 11, out e)) W.CallEffectHandler(11, Tgt(1), e, false);
    }

    static void Enlarge()
    {
        int lvl = CL(Being);
        int[] table = { 0, 0, 1, 51, 76, 91, 100 };
        byte exc = (byte)(lvl > 6 ? 100 : table[lvl]);
        byte enc = 0;
        if (W.AdjustForNewStrength(Tgt(1), 18, exc, ref enc)) W.L("PNS", Tgt(1), "is stronger", (byte)10, true);
        W.AddEffect(Tgt(1), 12, Duration(Being), enc, true);
    }

    static void Reduce()
    {
        if (GDVars.spellTargetCount == 0) return;
        if (W.RollSavingThrow(Tgt(1), 4, 0)) return;
        if (W.RemoveEffectWithMessage(Tgt(1), 12)) W.L("PNS", Tgt(1), "has been reduced", (byte)10, true);
    }

    static void Friends()
    {
        byte old = Tgt(1).chaCur;
        int v = old + Dice.RollDice(2, 4);
        Tgt(1).chaCur = AbilAdj.ClampScore((short)v);
        Resolve(Being, old, true, 0, 0, "is friendly");
    }

    static void Sleep()
    {
        GDVars.gMultiTarget = 1;
        int pool = Dice.RollDice(4, 4);
        int n = GDVars.spellTargetCount;
        for (int i = 1; i <= n; i++)
        {
            var t = Tgt(i);
            int lh = t.levelHighest, cost;
            if (lh == 0 || lh == 1) cost = 1;
            else if (lh == 2) cost = 2;
            else if (lh == 3) cost = 4;
            else if (lh == 4) cost = 6;
            else if (lh == 5) cost = t.race == 0 ? 10 : 20;
            else cost = 20;
            TEffectRec e;
            if (t.status != 1 && !CharHlp.HasEffect(t, 53, out e) && pool >= cost) pool -= cost;
            else GDVars.dsTargetCreaturePtr[i] = null;
        }
        Resolve(Being, 0, false, 0, 0, "falls asleep");
    }

    static void Hold()
    {
        int n = GDVars.spellTargetCount;
        sbyte smod = (sbyte)(n == 1 ? (Being == 23 ? -2 : -3) : n == 2 ? -1 : 0);
        for (int i = n; i >= 1; i--)
        {
            var t = Tgt(i);
            if (t == null) continue;
            if (i < GDVars.spellTargetCount)
            {
                W.L("AutoStop", (ushort)0);
                W.L("Snd", GDConsts.soundIds.commands[3]);
                W.L("BuildIcon", (byte)18);
                sbyte a = W.CombatCellX(GDVars.curChar), bb = W.CombatCellY(GDVars.curChar), c = W.CombatCellX(t), d = W.CombatCellY(t);
                W.L("Fly", a, bb, c, d, (byte)4, (byte)30);
                W.L("AutoStop", (ushort)10);
            }
            bool saved = W.RollSavingThrow(t, s8(T(Being).saveType), smod);
            if (t.charType > 1 || t.iconDimensions > 1) saved = true;
            W.AddEffectWithMessage(t, T(Being).effect, Duration(Being), CL(Being), false, T(Being).saveResult, saved, "is held");
        }
    }

    static void SlowPoison()
    {
        var t = Tgt(1);
        TEffectRec e;
        if (t.status == 1) { GDVars.dsTargetCreaturePtr[1] = null; return; }
        if (!CharHlp.HasEffect(t, 55, out e)) return;
        if (t.hpCur == 0) t.hpCur = 1;
        Resolve(Being, 255, true, 0, 0, "is affected");
        if (!W.ReviveDeadCreature(t, t.hpCur)) W.AddEffect(t, 78, 1, 0, true);
        W.AddEffect(t, 15, 10, 255, true);
    }

    static void SnakeCharm()
    {
        int pool = GDVars.curChar.hpCur;
        GDVars.spellTargetCount = 0;
        for (var p = GDVars.creatureList; p != null; p = p.nextCharAddr)
        {
            if (p.charType != 14 || pool < p.hpCur) continue;
            pool -= p.hpCur;
            GDVars.spellTargetCount = u8(GDVars.spellTargetCount + 1);
            GDVars.dsTargetCreaturePtr[GDVars.spellTargetCount] = p;
        }
        Resolve(Being, 0, false, 0, 0, "is charmed");
    }

    static void Cloud()
    {
        GDVars.gMultiTarget = 1;
        int cl = CL(Being);
        int mine = 0;
        TStinkRec node;
        if (GDVars.stinkHead == null) { GDVars.stinkHead = new TStinkRec(); node = GDVars.stinkHead; }
        else
        {
            node = GDVars.stinkHead;
            while (node.next != null) { if (node.caster == GDVars.curChar) mine++; node = node.next; }
            node.next = new TStinkRec(); node = node.next;
        }
        W.AddEffect(GDVars.curChar, 40, (ushort)cl, u8(cl + mine * 16), true);
        node.caster = GDVars.curChar; node.idx = u8(mine); node.ulX = GDVars.gTargetX; node.ulY = GDVars.gTargetY;
        var occ = new byte[5];
        for (int k = 1; k <= 4; k++)
        {
            int d = GDConsts.cloudDir[k];
            int cx = GDVars.gTargetX + GDConsts.dirDX[d], cy = GDVars.gTargetY + GDConsts.dirDY[d];
            byte terr = 0;
            W.GetArenaContents(s8(cx), s8(cy), ref occ[k], ref terr);
            node.exists[k] = (byte)(terr > 0 && GDConsts.terrainInfo[terr].cost < 255 ? 1 : 0);
            if (terr == 30)
            {
                for (var p2 = GDVars.stinkHead; p2 != null; p2 = p2.next)
                {
                    if (p2 == node) continue;
                    for (int j = 1; j <= 4; j++)
                    {
                        int dj = GDConsts.cloudDir[j];
                        if (p2.exists[j] != 0 && p2.ulX + GDConsts.dirDX[dj] == cx && p2.ulY + GDConsts.dirDY[dj] == cy && p2.under[j] != 30)
                            terr = p2.under[j];
                    }
                }
            }
            else if (terr == 31)
            {
                for (int j = 1; j <= GDVars.downedCount; j++)
                    if (GDVars.downed[j].x == cx && GDVars.downed[j].y == cy) terr = GDVars.downed[j].terrain;
            }
            node.under[k] = terr;
            if (node.exists[k] != 0) GDVars.arenaPtr.cells[cy, cx] = 30;
        }
        W.L("PNS", GDVars.curChar, "Creates a noxious cloud", (byte)10, false);
        W.ScrollView(GDVars.gTargetX, GDVars.gTargetY, 255, 8);
        W.L("Pause");
        W.L("CMW");
        for (int k = 1; k <= 4; k++) if (occ[k] > 0) W.ConsiderStinkingCloud(GDVars.creatureArr[occ[k]]);
    }

    static void Strength()
    {
        var t = Tgt(1);
        int roll = 0, exc = 0;
        if (t.classLevels[5] > 0) roll = Dice.RollDice(1, 4);
        if (t.classLevels[0] > 0 || t.classLevels[6] > 0) roll = Dice.RollDice(1, 6);
        if (CharHlp.WarriorLevel(t) > 0) roll = Dice.RollDice(1, 8);
        int str = u8(t.strCur + roll);
        if (str > 18)
        {
            if (CharHlp.WarriorLevel(t) > 0) { exc = u8(t.strExc + (str - 18) * 10); if (exc > 100) exc = 100; }
            str = 18;
        }
        byte enc = 0;
        if (W.AdjustForNewStrength(t, (byte)str, (byte)exc, ref enc)) W.L("PNS", t, "is stronger", (byte)10, true);
        W.AddEffect(t, 38, Duration(Being), enc, true);
    }

    static void Animate()
    {
        GDVars.gMultiTarget = 1;
        int n = CL(Being);
        GDVars.spellTargetCount = 0;
        for (var p = GDVars.creatureList; p != null && n > 0; p = p.nextCharAddr)
        {
            if (p.status != 6 || p.charType != 0) continue;
            sbyte x = W.CombatCellX(p), y = W.CombatCellY(p);
            if (!W.IsValidCreatureLoc(p, u8(x), u8(y), true)) continue;
            byte strength = u8(p.hostile * 16 + CL(Being));
            p.hostile = GDVars.curChar.hostile; p.quickfight = 1; p.levelUndead = 2; p.attackLevel = 0; p.movementBase = 6;
            for (int i = 0; i <= 20; i++) p.memorizedSpells[i] = 0;
            p.npc = (byte)(p.npc >= 0x80 ? 0xB2 : 0xB3);
            p.charType = 4;
            if (Combat) p.combatAddr.other = null;
            n--;
            if (W.ReviveDeadCreature(p, p.hpMax))
            {
                W.AddEffectWithMessage(p, 32, 0, strength, true, 0, false, "is animated");
                p.status = 1;
            }
        }
    }

    public static bool CureDisease(TCharRecord t)
    {
        bool any = false;
        GDVars.effectLock = 1;
        if (W.RemoveEffectWithMessage(t, 34)) any = true;
        if (W.RemoveEffectWithMessage(t, 43)) { any = true; W.RemoveEffect(t, 44, null); W.RemoveEffect(t, 31, null); }
        if (W.RemoveEffectWithMessage(t, 50)) { any = true; W.RemoveEffect(t, 57, null); }
        GDVars.effectLock = 0;
        return any;
    }

    static void Dispel()
    {
        GDVars.gMultiTarget = 1;
        int cl = CL(Being);
        int n = GDVars.spellTargetCount;
        for (int i = 1; i <= n; i++)
        {
            bool removed = false;
            TCharRecord t = i <= 72 ? Tgt(i) : null;
            var e = t == null ? null : t.effectsAddr;
            while (e != null)
            {
                var next = e.next;
                if (e.strength < 255 && e.id != 140 && e.id != 141)
                {
                    int lv = e.strength & 15;
                    int chance = cl > lv ? u8((cl - lv) * 5 + 50) : cl < lv ? u8(50 - (lv - cl) * 2) : 50;
                    if (Dice.RollDice(1, 100) <= chance) { W.RemoveEffect(t, e.id, e); removed = true; }
                }
                e = next;
            }
            if (removed) W.L("PNSA", t, true, "is affected");
        }
    }

    public static void RemoveCurse()
    {
        if (W.RemoveEffectWithMessage(Tgt(1), 36)) { W.L("PNSA", Tgt(1), true, "is un-cursed"); return; }
        bool found = false;
        for (var it = Tgt(1).itemsAddr; it != null && !found; it = it.next)
            if (it.cursed != 0) { found = true; it.cursed = 0; }
        if (found) W.L("PNSA", Tgt(1), true, "'s item is un-cursed");
    }

    static void Fireball()
    {
        GDVars.gMultiTarget = 1;
        int cl = Being == 64 ? Dice.RollDice(1, 3) * 2 + 1 : CL(Being);
        if (GDVars.zone0.view3dFlag == 0) W.L("TargetArea", GDVars.gTargetX, GDVars.gTargetY, (byte)2);
        W.ScrollView(GDVars.gTargetX, GDVars.gTargetY, 0, 8);
        Resolve(Being, 0, false, Dice.RollDamageDice(u8(cl), 6), 9, "");
    }

    static void HasteSlow(int removeId, int team, string msg)
    {
        GDVars.gMultiTarget = 1;
        int cl = CL(Being);
        int n = GDVars.spellTargetCount;
        for (int i = 1; i <= n; i++)
        {
            if (Tgt(i).hostile == team && cl > 0)
            {
                cl--;
                if (W.RemoveEffectWithMessage(Tgt(i), (byte)removeId)) GDVars.dsTargetCreaturePtr[i] = null;
            }
            else GDVars.dsTargetCreaturePtr[i] = null;
        }
        Resolve(Being, 0, false, 0, 0, msg);
        n = GDVars.spellTargetCount;
        for (int i = 1; i <= n; i++) if (Tgt(i) != null) W.TriggerEffectsWithCondition(18, Tgt(i));
    }

    public static bool BoltHit(int x, int y, int damage, int saveType)
    {
        byte cr = 0, terr = 0;
        W.GetArenaContents(s8(x), s8(y), ref cr, ref terr);
        bool blocked = terr > 0 && GDConsts.terrainInfo[terr].cost == 255;
        if (cr > 0)
        {
            var t = GDVars.creatureArr[cr];
            GDVars.elementMask = 12;
            bool sv = W.RollSavingThrow(t, s8(saveType), 0);
            W.InflictNonWeaponDamage(t, u8(damage), 2, sv);
            W.L("BuildIcon", (byte)19);
            GDVars.elementMask = 0;
        }
        return blocked;
    }

    public static void Walk(int boltLen, int damage, int saveType, bool bounce, bool trace, TCellList cells)
    {
        cells.count = 0;
        sbyte keepX = GDVars.gTargetX, keepY = GDVars.gTargetY; byte keepMulti = GDVars.gMultiTarget;
        if (!trace) W.L("BuildIcon", (byte)19);
        byte prevCr = 0, cr = 0, terr = 0;
        W.GetArenaContents(GDVars.gTargetX, GDVars.gTargetY, ref prevCr, ref terr);
        int sign = 1;
        sbyte x0 = W.CombatCellX(GDVars.curChar), y0 = W.CombatCellY(GDVars.curChar);
        bool doBounce = bounce;
        if (x0 == GDVars.gTargetX && y0 == GDVars.gTargetY) return;
        int budget = u8(boltLen * 2);
        GDVars.gMultiTarget = 1;
        bool blocked = false;
        while (budget > 0)
        {
            var req = new TPathReq
            {
                sx = GDVars.gTargetX, sy = GDVars.gTargetY,
                ex = s16(GDVars.gTargetX + (GDVars.gTargetX - x0) * sign * budget),
                ey = s16(GDVars.gTargetY + (GDVars.gTargetY - y0) * sign * budget)
            };
            Pathing.PathFind(ref req);
            do
            {
                sbyte px = s8(req.cx), py = s8(req.cy);
                if (req.sx != req.ex || req.sy != req.ey)
                {
                    while (true)
                    {
                        bool valid = Pathing.NextPathStep(ref req);
                        W.GetArenaContents(s8(req.cx), s8(req.cy), ref cr, ref terr);
                        if (trace && valid && terr != 0 && cells.count < 64)
                        {
                            cells.count++;
                            cells.x[cells.count] = s8(req.cx); cells.y[cells.count] = s8(req.cy);
                        }
                        if (!valid) break;
                        if (cr > 0 && cr != prevCr) break;
                        if (terr == 0) break;
                        if (GDConsts.terrainInfo[terr].cost > 1) break;
                        if (req.dist >= budget) break;
                    }
                }
                prevCr = cr;
                if (terr == 0) budget = 0;
                if (trace) blocked = terr > 0 && GDConsts.terrainInfo[terr].cost == 255;
                else
                {
                    W.L("Fly", px, py, s8(req.cx), s8(req.cy), (byte)4, (byte)50);
                    blocked = BoltHit(req.cx, req.cy, damage, saveType);
                }
                if (blocked)
                {
                    GDVars.gTargetX = s8(req.cx); GDVars.gTargetY = s8(req.cy);
                    var back = new TPathReq { sx = GDVars.gTargetX, sy = GDVars.gTargetY, ex = x0, ey = y0 };
                    Pathing.PathFind(ref back);
                    while (Pathing.NextPathStep(ref back)) { }
                    if (doBounce && back.dist <= 8) req.dist = u8(req.dist + 8);
                    sign = -sign;
                    doBounce = false;
                    prevCr = 0;
                }
                budget = req.dist < budget ? budget - req.dist : 0;
            } while (!blocked && budget != 0);
        }
        GDVars.gMultiTarget = 0;
        if (trace) { GDVars.gTargetX = keepX; GDVars.gTargetY = keepY; GDVars.gMultiTarget = keepMulti; }
    }

    public static void Trace(sbyte x, sbyte y, TCellList cells)
    {
        sbyte kx = GDVars.gTargetX, ky = GDVars.gTargetY;
        GDVars.gTargetX = x; GDVars.gTargetY = y;
        Walk(8, 0, 4, true, true, cells);
        GDVars.gTargetX = kx; GDVars.gTargetY = ky;
    }

    static void Restoration()
    {
        var t = Tgt(1);
        if (t.drainedLevels == 0) return;
        int share = t.drainedHps / t.drainedLevels;
        t.hpMax = u8(t.hpMax + share); t.hpCur = u8(t.hpCur + share); t.hpRolled = u8(t.hpRolled + share);
        t.drainedHps = u8(t.drainedHps - share); t.drainedLevels = u8(t.drainedLevels - 1);
        int bestLvl = 13, bestCls = -1; int bestXp = 10000000;
        for (int c = 0; c <= 7; c++)
        {
            int lvl = t.classLevels[c];
            if (lvl <= 0 || lvl >= 9 || lvl > bestLvl) continue;
            int xp = GDConsts.dsExperienceSpellsClass[c].xp[lvl + 1];
            if (xp <= 0 || xp >= bestXp) continue;
            if (CharHlp.AtLevelCap(t, (sbyte)c)) continue;
            bestLvl = lvl; bestCls = c; bestXp = xp;
        }
        if (bestCls >= 0)
        {
            t.classLevels[bestCls] = s8(t.classLevels[bestCls] + 1);
            if (t.experience < bestXp) t.experience = bestXp;
        }
        W.L("RecalcBaseSkills", t);
        W.L("PNS", t, "is restored", (byte)10, true);
    }

    static void Keoghtom()
    {
        var t = Tgt(1);
        TEffectRec e;
        bool slowed = CharHlp.HasEffect(t, 22, out e) || CharHlp.HasEffect(t, 15, out e);
        if (W.RemoveEffectWithMessage(t, 55))
        {
            GDVars.effectLock = 1; W.RemoveEffect(t, 22, null); GDVars.effectLock = 0;
            if (t.status == 6 && !slowed)
            {
                if (t.hpCur == 0) t.hpCur = 1;
                if (!W.ReviveDeadCreature(t, t.hpCur)) W.AddEffect(t, 78, 1, 0, true);
            }
            return;
        }
        if (CureDisease(t)) return;
        if (W.RestoreHitPoints(Tgt(1), u8(Dice.RollDice(1, 4) + 8), false)) W.L("PNSA", Tgt(1), true, "is Healed");
    }

    public static void Dragon(TCharRecord cr, object p)
    {
        if (GDVars.roundCounter != 0 && Dice.RollDice(1, 100) <= 50) return;
        sbyte x = W.CombatCellX(cr), y = W.CombatCellY(cr);
        W.L("PNS", cr, "Breathes!", (byte)10, true);
        GDVars.castTargetMenu(51, true, ref GDVars.breathAim);
        int dx = GDVars.gTargetX - x, dy = GDVars.gTargetY - y;
        GDVars.gTargetX = s8(x + Math.Sign(dx)); GDVars.gTargetY = s8(y + Math.Sign(dy));
        if (GDVars.gTargetX == x + 1) GDVars.gTargetX = s8(GDVars.gTargetX + 1);
        if (GDVars.gTargetY == y + 1) GDVars.gTargetY = s8(GDVars.gTargetY + 1);
        W.RemoveInvisibility(cr);
        W.L("BuildIcon", (byte)19);
        W.L("Fly", x, y, GDVars.gTargetX, GDVars.gTargetY, (byte)4, (byte)50);
        BoltHit(GDVars.gTargetX, GDVars.gTargetY, cr.hpMax, 3);
        Walk(10, cr.hpMax, 3, false, false, new TCellList());
        var e = (TEffectRec)p;
        if (e.strength > 253) e.strength--; else W.RemoveEffect(cr, 88, p);
        bool r = W.Coin(); W.L("EndActions", cr, r);
    }

    // Run the handler magic.pas installs for spell id (GameInit.InitSpellFns' mapping), by handler name.
    public static void Handler(string name)
    {
        var me = GDVars.curChar;
        switch (name)
        {
            case "BlessHandler": OneSide(me.hostile, "is Blessed"); break;
            case "CurseHandler": OneSide(me.hostile == 0 ? 1 : 0, "is Cursed"); break;
            case "CureLightWoundsHandler":
                if (GDVars.spellTargetCount == 0) break;
                if (W.RestoreHitPoints(Tgt(1), Dice.RollDice(1, 8), false)) W.L("HpRestoreMsg", Tgt(1));
                break;
            case "CauseLightWoundsHandler": Resolve(Being, 0, false, Dice.RollDamageDice(1, 8), 8, ""); break;
            case "DetectionHandler": Simple("is affected"); break;
            case "ProtectionHandler": Simple("is protected"); break;
            case "ResistColdHandler": Simple("is cold-resistant"); break;
            case "BurningHandsHandler": Resolve(Being, 0, false, CL(Being), 9, ""); break;
            case "CharmPersonHandler": Charm(); break;
            case "EnlargeHandler": Enlarge(); break;
            case "ReduceHandler": Reduce(); break;
            case "FriendsHandler": Friends(); break;
            case "MagicMissileHandler": { int cl = CL(Being); Resolve(Being, 0, false, u8(cl / 2 + Dice.RollDamageDice(u8(cl / 2), 4)), 8, ""); break; }
            case "ShieldHandler": Simple("is shielded"); break;
            case "ShockingGraspHandler": Resolve(Being, 0, false, u8(Dice.RollDamageDice(1, 8) + CL(Being)), 12, ""); break;
            case "SleepHandler": Sleep(); break;
            case "HoldPersonHandler": Hold(); break;
            case "ResistFireHandler": Simple("is fire resistant"); break;
            case "SilenceHandler": Simple("is silenced"); break;
            case "SlowPoisonHandler": SlowPoison(); break;
            case "SnakeCharmHandler": SnakeCharm(); break;
            case "SpiritualHammerHandler": Resolve(Being, 0, true, 0, 0, ""); W.CallEffectHandler(23, Tgt(1), null, false); break;
            case "InvisibilityHandler": Simple("is invisible"); break;
            case "KnockHandler": Simple("Knock-Knock"); break;
            case "MirrorImageHandler": Resolve(Being, Dice.RollDice(1, 4), false, 0, 0, "is duplicated"); break;
            case "RayOfEnfeeblementHandler": Simple("is weakened"); break;
            case "StinkingCloudHandler": Cloud(); break;
            case "StrengthHandler": Strength(); break;
            case "AnimateDeadHandler": Animate(); break;
            case "CureBlindnessHandler": if (W.RemoveEffectWithMessage(Tgt(1), 33)) W.L("PNSA", Tgt(1), true, "can see"); break;
            case "CauseBlindnessHandler": Simple("is blind"); break;
            case "CureDiseaseHandler": CureDisease(Tgt(1)); break;
            case "CauseDiseaseHandler": Resolve(Being, 0, true, 0, 0, "is diseased"); break;
            case "DispelMagicHandler": Dispel(); break;
            case "PrayerHandler": Resolve(Being, u8(me.hostile * 16 + CL(Being)), false, 0, 0, "is praying"); break;
            case "RemoveCurseSpell": RemoveCurse(); break;
            case "BestowCurseHandler": Simple("has been cursed!"); break;
            case "BlinkHandler": Simple("is blinking"); break;
            case "FireballHandler": Fireball(); break;
            case "HasteHandler": HasteSlow(42, me.hostile, "is Hasted"); break;
            case "LightningBoltHandler":
                {
                    byte dmg = Dice.RollDice(CL(Being), 6);
                    BoltHit(GDVars.gTargetX, GDVars.gTargetY, dmg, 4);
                    Walk(8, dmg, 4, true, false, new TCellList());
                    break;
                }
            case "SlowHandler": HasteSlow(39, me.hostile == 0 ? 1 : 0, "is Slowed"); break;
            case "RestorationHandler": Restoration(); break;
            case "PotionOfSpeedHandler": if (!W.RemoveEffectWithMessage(Tgt(1), 42)) Simple("is Speedy"); break;
            case "KeoghtomsOintmentHandler": Keoghtom(); break;
            case "GiantStrengthHandler":
                {
                    byte enc = 0;
                    if (W.AdjustForNewStrength(Tgt(1), 21, 0, ref enc)) W.L("PNS", Tgt(1), "is stronger", (byte)10, true);
                    W.AddEffect(Tgt(1), 38, Duration(Being), enc, true);
                    break;
                }
            case "JavelinOfLightningHandler":
                BoltHit(GDVars.gTargetX, GDVars.gTargetY, Dice.RollDice(1, 6) + 20, 4);
                Walk(3, 20, 4, false, false, new TCellList());
                break;
            case "WandParalyzationHandler": Simple("is paralyzed"); break;
            case "PotionOfHealingHandler": if (W.RestoreHitPoints(Tgt(1), u8(Dice.RollDice(2, 4) + 2), false)) W.L("PNSA", Tgt(1), true, "is Healed"); break;
            case "DustDisappearanceHandler": Simple("is invisible"); break;
            case "WandMagicMissilesHandler": Resolve(Being, 0, false, u8(Dice.RollDamageDice(2, 4) + 2), 8, ""); break;
            case "UnusedSpellHandler": break;
            case "ManualBodilyHealthHandler": Resolve(Being, 255, true, 0, 0, "is Reading"); break;
            default: throw new ArgumentException(name);
        }
    }

    // ---------------- items ----------------
    public static void ScrollRemove(TCharRecord cr, TItemRec it, int spell)
    {
        int sp = spell & 0x7F, found = 0;
        for (int i = 1; i <= 3; i++) if ((it.spells[i] & 0x7F) == sp) found = i;
        if (found == 0) return;
        it.spells[found] = 0;
        it.nameIds[2] = u8(it.nameIds[2] - 1);
        if (it.nameIds[2] < 210) W.RemoveItem(cr, it);
    }

    public static void UseSpell(TItemRec item, int spell, bool announce, ref bool done)
    {
        bool readable = CharHlp.ItemReadable(item);
        int id = !readable && spell > 56 ? spell - 23 : spell;
        if (id < 1 || id > 67) { done = false; return; }
        if (readable && !Combat && T(spell).target == 0)
        {
            ShowCombatOnly(spell);
            done = W.YesNo("Use it? ", 13, 15, 10) == 'Y';
            W.L("CMW");
            if (done) ScrollRemove(GDVars.curChar, item, spell);
            return;
        }
        if (Combat && GDVars.curChar.quickfight == 0) W.L("CombatIfc");
        if (!CharHlp.ItemReadable(item))
        {
            if (spell > 56) spell -= 23;
            W.L("PNS", GDVars.curChar, "uses an item", (byte)10, false);
            if (Combat) { W.L("DS", (byte)0, (byte)23, (byte)10, "Item:"); W.L("ItemLabel", item, (byte)5, (byte)23); }
            else W.L("ItemLabel", item, (byte)1, (byte)22);
            W.L("Pause");
            W.L("CMW");
        }
        GDVars.usingItem = true;
        if (announce) CastMsg(GDVars.curChar, "casts", spell);
        Cast(spell, GDVars.curChar.quickfight != 0, false, ref done);
        GDVars.usingItem = false;
        if (Combat && T(spell).whenCast != 0) { bool r = W.Coin(); W.L("EndActions", GDVars.curChar, r); done = r; }
        if (!done) return;
        if (CharHlp.ItemReadable(item)) ScrollRemove(GDVars.curChar, item, spell);
        else if (item.charges > 0)
        {
            if (item.quantity > 1) item.quantity--;
            else
            {
                item.charges--;
                if (item.charges == 0) W.RemoveItem(GDVars.curChar, item);
            }
        }
    }

    public static void Use(TItemRec item, ref bool done)
    {
        short idx = -1;
        GDVars.usingItem = false;
        int spell = 0;
        if (CharHlp.ItemReadable(item))
        {
            curScroll = item;
            bool ne = false; TItemRec sc = null;
            spell = Choose(2, 1, ref idx, ref ne, ref sc);
        }
        else if (item.effect > 0 && item.special < 0x80) spell = item.effect;
        if (spell == 0) done = false;
        else UseSpell(item, spell, false, ref done);
    }
}
