// CpOracle: a separate model of camp.pas, written from the Pascal text (not from Camp.cs). It calls the world's fakes
// directly in the order the Pascal makes the calls. It keeps its own HasEffect, its own clock decomposition (as
// Clock.SetFromMinutes, minutes into days, hours, turns and minutes) and its own effect-label table; it shares
// CharHlp.ItemReadable and Party.LastCreature with the port (accepted with their own units).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using Por.Core;
using static Por.Core.Types;

sealed class CpOracle
{
    readonly CpWorld w;
    public CpOracle(CpWorld w) { this.w = w; }

    const byte LC = 11, LG = 10, LM = 13, YEL = 14, WH = 15;
    static TCharRecord Cur { get { return GDVars.curChar; } }
    static byte b(int v) { return unchecked((byte)v); }
    static string N(long v) { return v.ToString(CultureInfo.InvariantCulture); }
    static bool ExitKey(byte c) { return c == 0 || c == 'E'; }
    static bool Has(TCharRecord c, int id) { for (var e = c.effectsAddr; e != null; e = e.next) if (e.id == id) return true; return false; }
    static TSpellRec Spell(int id) { return GDConsts.spellTable[id]; }
    static IEnumerable<TCharRecord> Party() { for (var p = GDVars.creatureList; p != null; p = p.nextCharAddr) yield return p; }
    static IEnumerable<TItemRec> Items(TCharRecord c) { for (var p = c.itemsAddr; p != null; p = p.next) yield return p; }

    static void SetClock(int minutes)
    {
        var k = GDVars.dsRestingTimeGbvm;
        for (int i = 0; i < 7; i++) k[i] = 0;
        k[C_Time.MINUTES] = (ushort)(minutes % 10);
        k[C_Time.TURNS] = (ushort)(minutes % 60 / 10);
        k[C_Time.HOURS] = (ushort)(minutes % 1440 / 60);
        k[C_Time.DAYS] = (ushort)(minutes / 1440);
    }
    static void ZeroClock() { for (int i = 0; i < 7; i++) GDVars.dsRestingTimeGbvm[i] = 0; }

    // ---- preparation ----
    public int PrepTime(TCharRecord c)
    {
        var mem = c.memorizedSpells.Data.Where(x => x >= 0x80).Select(x => (int)b(Spell(x & 0x7F).levelSp)).ToList();
        var scr = new List<int>();
        foreach (var it in Items(c))
            if (CharHlp.ItemReadable(it))
                for (int s = 1; s <= 3; s++)
                    if (it.spells[s] >= 0x80) scr.Add(b(Spell(it.spells[s] & 0x7F).levelSp));
        int sumMem = mem.Sum() & 0xFF, sumScr = scr.Sum() & 0xFF;
        int hours = 0;
        if (sumMem != 0 || sumScr != 0) hours = 4;
        if (mem.Concat(scr).Any(l => l >= 3)) hours = 6;
        c.spellPrepTime = b(hours);
        return hours * 60 + 15 * (sumMem + sumScr);
    }

    public void ResetMemorize(TCharRecord c)
    {
        for (int i = 0; i < 21; i++) if (c.memorizedSpells[i] >= 0x80) c.memorizedSpells[i] = 0;
        c.spellPrepTime = 0;
    }

    public void ResetScribe(TCharRecord c)
    {
        foreach (var it in Items(c))
            if (CharHlp.ItemReadable(it))
                for (int s = 1; s <= 3; s++) it.spells[s] = b(it.spells[s] & 0x7F);
    }

    public void ResetParty() { foreach (var c in Party().ToList()) { ResetMemorize(c); ResetScribe(c); } }

    public int SlotsUsed(int lvl, int typ)
    {
        return Cur.memorizedSpells.Data.Count(x =>
        {
            int id = x & 0x7F;
            return id >= 1 && id <= 67 && Spell(id).levelSp == lvl && Spell(id).castClassSp == typ;
        });
    }

    public bool PutPending(int id)
    {
        int slot = Array.IndexOf(Cur.memorizedSpells.Data, (byte)0);
        if (slot < 0) return false;
        Cur.memorizedSpells[slot] = b(id + 0x80);
        return true;
    }

    public int Remaining(int lvl, int typ)
    {
        int allowed = Cur.slots[typ, lvl];
        return Math.Max(0, allowed - SlotsUsed(lvl, typ));
    }

    public bool CanDo(int activity)
    {
        string why = null;
        if (activity == 1 && GDVars.zone0.antimagic != 0) why = "cannot cast spells in this area";
        else if (Cur.status == C_Status.ANIMATED || Cur.enabled == 0)
            why = "is in no condition to " + (activity == 1 ? "cast any spells" : activity == 2 ? "memorize spells" : activity == 3 ? "scribe any scrolls" : "");
        if (why == null) return true;
        w.PrintNameString(Cur, why, 10, true);
        return false;
    }

    public void CastMenu()
    {
        bool redraw = false;
        GDVars.castTarget = null;
        if (CanDo(1))
        {
            short idx = -1;
            bool notEmpty = false;
            TItemRec scroll = null;
            while (true)
            {
                byte id = w.CastSpellMenu(ref idx, ref notEmpty, ref scroll);
                if (id == 0)
                {
                    if (notEmpty) redraw = true;
                    else w.PrintNameString(Cur, "has no spells memorized", 10, true);
                    break;
                }
                redraw = true;
                w.DrawBorder(1, 17, 38, 22, 0);
                w.ClearBox(1, 17, 38, 22);
                bool unused = false;
                if (scroll != null)
                {
                    var who = Cur;
                    w.UseItemSpell(scroll, id, true, ref unused);
                    w.RecalcStats(who);
                }
                else w.CastSpellNC(id, false, true, ref unused);
                idx = -1;
            }
        }
        if (redraw) w.DisplayNoncombatInterface();
    }

    public bool SpellSlots()
    {
        var text = new string[2, 4];
        var shows = new bool[2];
        for (int typ = 0; typ < 2; typ++)
            for (int lvl = 1; lvl <= 3; lvl++)
            {
                string v = N(Remaining(lvl, typ));
                if (v.Length > 2) v = v.Remove(2);
                if (Cur.slots[typ, lvl] != 0) { shows[typ] = true; text[typ, lvl] = v; }
                else text[typ, lvl] = " ";
            }
        if (!shows[0] && !shows[1]) return false;
        w.PrintNameString(Cur, "can memorize:", 10, false);
        int row = 20;
        for (int typ = 0; typ < 2; typ++)
        {
            if (!shows[typ]) continue;
            w.DisplayString(1, b(row), LG, typ == 0 ? "    Cleric Spells:" : "Magic-User Spells:");
            for (int lvl = 1; lvl <= 3; lvl++) w.DisplayString(b(17 + 3 * lvl), b(row), LG, text[typ, lvl]);
            row++;
        }
        return true;
    }

    public void RestMenu(ref bool interrupted)
    {
        int longest = 0;
        foreach (var c in Party().ToList()) longest = Math.Max(longest, PrepTime(c));
        SetClock(longest);
        interrupted = w.CampRest(true);
        ZeroClock();
        w.PrintMiscInfo();
    }

    public void Sort()
    {
        // the source's exchange sort on the masked ids (the pending flag travels with its spell)
        var m = Cur.memorizedSpells;
        for (int i = 0; i < 20; i++)
            for (int j = i + 1; j < 21; j++)
                if ((m[i] & 0x7F) > (m[j] & 0x7F)) { byte t = m[i]; m[i] = m[j]; m[j] = t; }
    }

    public void Remember()
    {
        var s = new TSpellSlots();
        for (int i = 0; i < 21; i++) s[i] = b(Cur.memorizedSpells[i] & 0x7F);
        w.SetUsual(Cur, ref s);
    }

    public bool SlotFree(int lvl, int typ)
    {
        if (lvl < 1 || lvl > 3 || typ < 0 || typ > 1) return false;
        return SlotsUsed(lvl, typ) < Cur.slots[typ, lvl];
    }

    public void QueueUsual()
    {
        var usual = new TSpellSlots();
        if (!w.GetUsual(Cur, ref usual)) return;
        var have = Cur.memorizedSpells.Data.Select(x => x & 0x7F).ToList();
        for (int i = 0; i < 21; i++)
        {
            int id = usual[i];
            if (id < 1 || id > 56) continue;
            int at = have.IndexOf(id);
            if (at >= 0) { have[at] = 0; continue; }
            if (Cur.spellsKnown[id] != 0 && SlotFree(Spell(id).levelSp, Spell(id).castClassSp)) PutPending(id);
        }
        Sort();
    }

    public void MemorizeMenu()
    {
        if (!CanDo(2)) return;
        if (!Cur.memorizedSpells.Data.Any(x => x >= 0x80)) QueueUsual();
        short idx = -1;
        bool notEmpty = false;
        w.SpellMenu(5, 5, ref idx, ref notEmpty);
        bool redraw = notEmpty;
        bool done = false;
        if (notEmpty)
        {
            if (w.YesNo("Memorize These Spells? ", YEL, LG, WH) == 'N') ResetMemorize(Cur);
            else { Remember(); done = true; }
        }
        idx = -1;
        while (!done)
        {
            if (!SpellSlots())
            {
                w.PrintNameString(Cur, "cannot memorize any spells", 10, true);
                break;
            }
            byte id = w.SpellMenu(1, 2, ref idx, ref notEmpty);
            redraw = true;
            if (id == 0) break;
            if (Remaining(Spell(id).levelSp, Spell(id).castClassSp) != 0)
                if (PutPending(id)) Sort();
        }
        if (idx != -1)
        {
            idx = -1;
            w.SpellMenu(5, 5, ref idx, ref notEmpty);
            if (notEmpty)
            {
                if (w.YesNo("Memorize these spells? ", YEL, LG, WH) == 'N') ResetMemorize(Cur);
                else Remember();
            }
        }
        if (redraw) w.DisplayNoncombatInterface();
    }

    public void ScribeMenu()
    {
        if (!CanDo(3)) return;
        if (Cur.classLevels[C_Level.MAGE] < 1)
        {
            w.ShowMessage(LG, true, "Scribing a scroll requires arcane training.");
            w.MessageWait();
            w.ClearMessageWindow();
            return;
        }
        short idx = -1;
        bool notEmpty = false;
        w.SpellMenu(6, 0, ref idx, ref notEmpty);
        bool redraw = notEmpty;
        bool done = false;
        if (notEmpty)
        {
            if (w.YesNo("Scribe These Spells? ", YEL, LG, WH) == 'N') ResetScribe(Cur);
            else done = true;
        }
        idx = -1;
        while (!done)
        {
            byte id = w.SpellMenu(3, 3, ref idx, ref notEmpty);
            if (id == 0)
            {
                if (notEmpty) redraw = true;
                else w.PrintNameString(Cur, "has no copyable scrolls.", 10, true);
                break;
            }
            redraw = true;
            bool slotless = id > 56 || Spell(id).levelSp < 1 || Spell(id).levelSp > 3 || Spell(id).castClassSp < 0 || Spell(id).castClassSp > 1;
            if (slotless) { w.PrintBottomLine("You can not scribe that spell."); continue; }
            if (Cur.spellsKnown[id] != 0) { w.PrintBottomLine("You already know that spell."); continue; }
            // the first readable item holding the spell pending stops the walk (every pending copy on it is reported)
            bool pendingAlready = false;
            foreach (var it in Items(Cur))
            {
                if (CharHlp.ItemReadable(it))
                    for (int s = 1; s <= 3; s++)
                        if (it.spells[s] >= 0x80 && (it.spells[s] & 0x7F) == id)
                        {
                            w.PrintBottomLine("You are already scribing that spell.");
                            pendingAlready = true;
                        }
                if (pendingAlready) break;
            }
            if (pendingAlready) continue;
            if (Cur.slots[Spell(id).castClassSp, Spell(id).levelSp] == 0) { w.PrintBottomLine("You can not scribe that spell."); continue; }
            // mark the first byte equal to the id, on any item
            foreach (var it in Items(Cur))
            {
                int s = Enumerable.Range(1, 3).FirstOrDefault(k => it.spells[k] == id);
                if (s != 0) { it.spells[s] = b(it.spells[s] | 0x80); break; }
            }
        }
        if (idx != -1)
        {
            idx = -1;
            w.SpellMenu(6, 0, ref idx, ref notEmpty);
            if (notEmpty && w.YesNo("Scribe these spells? ", YEL, LG, WH) == 'N') ResetScribe(Cur);
        }
        if (redraw) w.DisplayNoncombatInterface();
    }

    // ---- the Display list ----
    static readonly Dictionary<int, string> Fixed = new Dictionary<int, string>
    {
        {4, "Studying Manual of Bodily Health"}, {7, "Training with Manual of Bodily Health"}, {27, "Feather Fall"}, {31, "Helpless"},
        {35, "Prayer"}, {44, "Cause Disease"}, {50, "Dreaded Mummy Disease"}, {54, "Repulsed"}, {55, "Poisoned"}, {59, "Regenerating"},
        {61, "Fire Resistance"}, {71, "Invisible"}, {72, "Camouflaged"}, {89, "Displaced"},
    };
    // the spell effects listed by spell name: 1, 2, 5, 8..14, 16, 17, 19..25, 28, 29, 32..34, 36..39, 41, 42, 45, 46, 49, 51..53
    static readonly HashSet<int> Named = new HashSet<int>(new[] { 1, 2, 5, 16, 17, 28, 29, 41, 42, 45, 46, 49 }
        .Concat(Enumerable.Range(8, 7)).Concat(Enumerable.Range(19, 7)).Concat(Enumerable.Range(32, 3)).Concat(Enumerable.Range(36, 4)).Concat(Enumerable.Range(51, 3)));

    static string Label(int id)
    {
        if (Named.Contains(id))
        {
            for (int k = 1; k <= 56; k++) if (Spell(k).effect == id) return GDConsts.spellNames[k];
            return "Funky--" + N(id);
        }
        string s;
        return Fixed.TryGetValue(id, out s) ? s : null;
    }

    public void DisplayMenu()
    {
        TItemRec head = new TItemRec { name = " ", inUse = 0 };
        TItemRec tail = head;
        foreach (var c in Party().ToList())
        {
            w.AppendMenuLine(ref head, ref tail, c.name, true);
            int shown = 0;
            for (var e = c.effectsAddr; e != null; e = e.next)
            {
                string s = Label(e.id);
                if (s == null) continue;
                shown++;
                w.AppendMenuLine(ref head, ref tail, " " + s, false);
            }
            if ((shown & 0xFF) == 0) w.AppendMenuLine(ref head, ref tail, " <No Spell Effects>", false);
            w.AppendMenuLine(ref head, ref tail, " ", false);
        }
        bool redraw = true;
        short idx = 0;
        w.DrawWindow(1, 1, 38, 22, 0, WH, "Display");
        w.MenuV("", "", LC, LG, WH, 1, 4, 38, 22, head, true, ref redraw, ref idx, ref tail);
        w.FreeStringList(ref head);
        w.DisplayNoncombatInterface();
    }

    public void MagicMenu(ref bool interrupted)
    {
        while (!interrupted)
        {
            bool ext = false;
            byte c = w.MenuH("", GDConsts.magicMenu, LM, LG, WH, true, true, ref ext);
            if (ext) { w.SelectPrevNextChar((char)c); w.DisplayParty(Cur); }
            else if (c == 'C') CastMenu();
            else if (c == 'M') MemorizeMenu();
            else if (c == 'S') ScribeMenu();
            else if (c == 'D') DisplayMenu();
            else if (c == 'R') RestMenu(ref interrupted);
            if (ExitKey(c)) return;
        }
    }

    // ---- party order ----
    static List<TCharRecord> Order() { var l = new List<TCharRecord>(); for (var p = GDVars.creatureList; p != null && l.Count < 30; p = p.nextCharAddr) l.Add(p); return l; }
    static void Relink(List<TCharRecord> l)
    {
        GDVars.creatureList = l.Count == 0 ? null : l[0];
        for (int i = 0; i < l.Count; i++) l[i].nextCharAddr = i + 1 < l.Count ? l[i + 1] : null;
    }

    public void MoveUp()
    {
        var l = Order();
        int i = l.IndexOf(Cur);
        if (i < 0) throw new NullReferenceException("curChar not in the party");
        l.RemoveAt(i);
        if (i == 0) l.Add(Cur); else l.Insert(i - 1, Cur);
        Relink(l);
    }

    public void MoveDown()
    {
        var l = Order();
        int i = l.IndexOf(Cur);
        if (i < 0) throw new NullReferenceException("curChar not in the party");
        l.RemoveAt(i);
        if (i == l.Count) l.Insert(0, Cur); else l.Insert(i + 1, Cur);
        Relink(l);
    }

    public void OrderMenu()
    {
        bool place = false;
        while (true)
        {
            bool ext = false;
            byte c = w.MenuH("Party Order: ", GDConsts.orderMenus[place ? 1 : 0], LM, LG, WH, true, true, ref ext);
            if (ext)
            {
                if (!place) w.SelectPrevNextChar((char)c);
                else if (c == C_Keys.HOME || c == C_Keys.UP) MoveUp();
                else if (c == C_Keys.END_KEY || c == C_Keys.DOWN) MoveDown();
                w.DisplayParty(Cur);
            }
            else if (c == 'P' || c == 'S')
            {
                place = !place;
                if (place) w.PrintNameString(Cur, "has been selected", 10, false);
                else w.ClearMessageWindow();
            }
            if (ExitKey(c)) return;
        }
    }

    public void DropMenu()
    {
        if (GDVars.creatureList == Cur && Cur.nextCharAddr == null)
        {
            if (w.YesNo("quit TO DOS: ", YEL, LG, WH) == 'Y') { w.RemoveCreature(false, true); w.ExitGame(); }
            return;
        }
        w.PrintNameString(Cur, "will be gone", 10, false);
        if (w.YesNo("Drop from party? ", YEL, LG, WH) != 'Y') { w.PrintNameString(Cur, "Breathes A sigh of relief.", 10, true); return; }
        w.PrintNameString(Cur, Cur.enabled != 0 ? "bids you farewell." : "is dumped in a ditch.", 10, true);
        w.RemoveCreature(false, true);
        w.ClearBox(17, 1, 38, 11);
        w.DisplayParty(Cur);
    }

    public void AlterMenu()
    {
        while (true)
        {
            bool ext = false;
            byte c = w.MenuH("Alter: ", GDConsts.alterMenu, LM, LG, WH, true, true, ref ext);
            if (ext) { w.SelectPrevNextChar((char)c); w.DisplayParty(Cur); }
            else if (c == 'O') OrderMenu();
            else if (c == 'D') DropMenu();
            else if (c == 'C') CharacterMenu();
            else if (c == 'S') { w.OptionsMenu(); w.DisplayNoncombatInterface(); }
            if (ExitKey(c)) return;
        }
    }

    void CharacterMenu()
    {
        while (true)
        {
            string menu = w.PlayerPortraitsHere() ? "Icon Portrait Exit" : "Icon Exit";
            bool ext = false;
            byte c = w.MenuH("Character: ", menu, LM, LG, WH, true, true, ref ext);
            if (ext) { w.SelectPrevNextChar((char)c); w.DisplayParty(Cur); continue; }
            if (c == 'I') { w.CustomizeIcon(); w.DisplayNoncombatInterface(); }
            else if (c == 'P' && w.PlayerPortraitsHere()) { w.CustomizePortrait(); w.DisplayNoncombatInterface(); }
            if (ExitKey(c)) return;
        }
    }

    // ---- Fix ----
    sealed class Member
    {
        public TCharRecord C;
        public int L1Free, L3Free, L1Casts, L3Casts;
        public bool[] Cure = new bool[4];
        public int Time { get { return (L3Casts > 0 ? 360 : L1Casts > 0 ? 240 : 0) + 15 * L1Casts + 45 * L3Casts; } }
    }
    static bool Treatable(TCharRecord c) { return c.status == 0 || c.status == 1 || c.status == 4 || c.status == 5; }

    public void FixMenu(ref bool interrupted)
    {
        interrupted = false;
        var ms = Party().Take(8).Select(c => new Member { C = c }).ToList();
        int clerics = ms.Count(m => m.C.classLevels[C_Level.CLERIC] > 0);
        int able = ms.Count(m => m.C.classLevels[C_Level.CLERIC] > 0 && m.C.status == 0);
        foreach (var m in ms)
            if (m.C.status == 0)
            {
                if (m.C.spellsKnown[3] != 0) m.L1Free = m.C.clericSpells[1];
                m.L3Free = m.C.clericSpells[3];
            }
        int need = ms.Where(m => Treatable(m.C) && m.C.hpCur < m.C.hpMax).Sum(m => m.C.hpMax - m.C.hpCur);
        if (clerics == 0) { w.PrintMessageWait("No clerics available to heal."); return; }
        if (able == 0) { w.PrintMessageWait("No cleric is in a condition to heal."); return; }
        Func<int, bool> assign = spell =>
        {
            Member best = null;
            foreach (var m in ms)
            {
                bool ok = spell == 0 ? m.L1Free > 0 : m.L3Free > 0 && m.C.spellsKnown[spell] != 0;
                if (ok && (best == null || m.Time < best.Time)) best = m;
            }
            if (best == null) return false;
            if (spell == 0) { best.L1Free--; best.L1Casts++; } else { best.L3Free--; best.L3Casts++; }
            return true;
        };
        bool anyCure = false, anyCondition = false;
        int[] spellFor = { 0, C_Spell.CURE_DISEASE, C_Spell.CURE_BLINDNESS, C_Spell.REMOVE_CURSE };
        for (int kind = 1; kind <= 3; kind++)
            foreach (var m in ms)
            {
                if (!Treatable(m.C)) continue;
                bool has = kind == 1 ? Has(m.C, C_Effect.DISEASED) || Has(m.C, C_Effect.DISEASE_STR) || Has(m.C, C_Effect.ROT_NO_HEAL)
                         : kind == 2 ? Has(m.C, C_Effect.BLIND) : Has(m.C, C_Effect.BESTOW_CURSE);
                if (!has) continue;
                anyCondition = true;
                if (assign(spellFor[kind])) { m.Cure[kind] = true; anyCure = true; }
            }
        // ceil(need / 4.5) Cure Light Wounds, as many as there are slots for
        int casts = 0;
        int wanted = (2 * need + 8) / 9;
        while (casts < wanted && assign(0)) casts++;
        if (casts == 0 && !anyCure)
        {
            w.PrintMessageWait(need == 0 && !anyCondition ? "Further healing would do no good." : "Further healing isn't possible.");
            return;
        }
        SetClock(ms.Max(m => m.Time));
        interrupted = w.CampRest(false);
        var k = GDVars.dsRestingTimeGbvm;
        bool complete = k[C_Time.DAYS] == 0 && k[C_Time.HOURS] == 0 && k[C_Time.TURNS] == 0 && k[C_Time.MINUTES] == 0;
        ZeroClock();
        if (complete && !interrupted)
        {
            foreach (var m in ms)
                for (int kind = 1; kind <= 3; kind++)
                    if (m.Cure[kind])
                    {
                        GDVars.dsTargetCreaturePtr[1] = m.C;
                        if (kind == 1) w.CureDisease(); else if (kind == 2) w.CureBlindness(); else w.RemoveCurse();
                    }
            int pool = 0;
            for (int i = 0; i < casts; i++) pool += w.RollDice(1, 8);
            foreach (var m in ms)
            {
                var c = m.C;
                if (!Treatable(c) || c.hpCur >= c.hpMax || pool <= 0) continue;
                int hp = Math.Min(c.hpMax - c.hpCur, pool);
                if (w.RestoreHitPoints(c, b(hp), false)) pool -= hp;
            }
        }
        w.DisplayParty(Cur);
        w.PrintMiscInfo();
    }

    // ---- the camp menu ----
    public void MainMenu(ref bool interrupted)
    {
        byte saved = GDVars.ifcState;
        GDVars.ifcState = C_Ifc.CAMPING;
        GDVars.healTicks = 0;
        ZeroClock();
        w.DisplayNoncombatInterface();
        w.ClearBox(1, 17, 38, 22);
        w.DisplayString(1, 18, LG, "The party makes camp...");
        ResetParty();
        interrupted = false;
        while (!interrupted)
        {
            bool ext = false;
            byte c = w.MenuH("Camp:", GDConsts.campMenu, LM, LG, WH, true, true, ref ext);
            if (ext)
            {
                if (c == C_Keys.F6) { if (GDVars.companionOn != 0) { w.ToolsMenu(); w.DisplayNoncombatInterface(); } }
                else { w.SelectPrevNextChar((char)c); w.DisplayParty(Cur); }
            }
            else if (c == 'S')
            {
                w.SaveGame();
                w.DisplayNoncombatInterface();
                if (w.YesNo("Quit TO DOS ", YEL, LG, WH) == 'Y') w.ExitGame();
            }
            else if (c == 'V') { bool d = false; w.ViewCharMenu(ref d); }
            else if (c == 'M') MagicMenu(ref interrupted);
            else if (c == 'R') RestMenu(ref interrupted);
            else if (c == 'A') AlterMenu();
            else if (c == 'F') FixMenu(ref interrupted);
            if (ExitKey(c)) break;
        }
        ResetParty();
        GDVars.castTarget = null;
        GDVars.ifcState = saved;
        w.PrintMiscInfo();
        w.ClearMessageWindow();
        w.ClearBottomLine();
    }
}
