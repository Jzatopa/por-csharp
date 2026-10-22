using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Por.Core;
using static Asm;

static partial class Tests
{
    static Types.TItemRec Items(params string[] names)
    {
        Types.TItemRec head = null;
        for (int i = names.Length - 1; i >= 0; i--) head = new Types.TItemRec { name = names[i], itemType = (byte)(i + 1), next = head };
        return head;
    }

    static List<string> ItemNames(Types.TItemRec p) { var l = new List<string>(); for (; p != null; p = p.next) l.Add(p.name); return l; }

    // LOAD MONSTER
    static void RunLoadMon()
    {
        for (int t = 0; t < 400; t++)
        {
            Setup();
            var lead = Member("lead");
            Party(lead, Member("second"));
            GDVars.curChar = lead;
            int already = rng.Next(70);
            EclVm.creatureCnt = (byte)already;
            GDVars.gIconAnim = (byte)(8 + rng.Next(20));
            byte slot = GDVars.gIconAnim;
            var tmpl = Member("orc");
            tmpl.hpCur = 7; tmpl.thiefSkills[3] = 44; tmpl.memorizedSpells[4] = 9;
            fake.CreatureTemplate = tmpl;
            fake.CreatureItems = Items("sword", "shield");
            fake.CreatureEffects = new Types.TEffectRec { id = 5, next = new Types.TEffectRec { id = 6 } };
            // InitCreature returns the record with its own list heads in it, as well as the heads themselves
            tmpl.itemsAddr = fake.CreatureItems; tmpl.effectsAddr = fake.CreatureEffects;
            int count = rng.Next(5);
            var a = new Asm(); a.Op(C_Ecl.LOAD_MON, I(12), I(count), I(3));
            Exec(a);
            if (already >= 63 || slot > 25)
            {
                Yes(!fake.Log.Any(l => l.StartsWith("InitCreature")) && GDVars.gIconAnim == slot && EclVm.creatureCnt == already, "LOAD MONSTER limits");
                continue;
            }
            int effective = count <= 0 ? 1 : count;
            int added = Math.Min(effective, 63 - already);
            var list = new List<Types.TCharRecord>();
            for (var p = GDVars.creatureList; p != null; p = p.nextCharAddr) list.Add(p);
            Yes(list.Count == 2 + added, "monsters appended " + added);
            Yes(ReferenceEquals(list[2], tmpl), "the loaded creature itself is first");
            Yes(list.Skip(2).All(c => c.orderNumber == slot), "icon slot");
            Yes(EclVm.creatureCnt == already + added && GDVars.gIconAnim == slot + 1 && EclVm.monstersOn == 1, "counters");
            Yes(ReferenceEquals(GDVars.curChar, lead), "current character restored");
            foreach (var c in list.Skip(3))
            {
                Yes(c.name == "orc" && c.hpCur == 7 && c.thiefSkills[3] == 44 && c.memorizedSpells[4] == 9, "copy fields");
                Yes(!ReferenceEquals(c.thiefSkills, tmpl.thiefSkills) && !ReferenceEquals(c.memorizedSpells, tmpl.memorizedSpells), "copy arrays are its own");
                Yes(ItemNames(c.itemsAddr).SequenceEqual(new[] { "shield", "sword" }), "items prepended (reversed)");
                Yes(c.effectsAddr != null && c.effectsAddr.id == 6 && c.effectsAddr.next.id == 5 && c.effectsAddr.next.next == null, "effects prepended (reversed)");
            }
            if (list.Count > 3) { list[3].thiefSkills[3] = 1; Yes(tmpl.thiefSkills[3] == 44, "template independent of copies"); }
        }
    }

    // TCharRecord assignment
    static void RunCopyChar()
    {
        var r = new Random(5);
        for (int t = 0; t < 300; t++)
        {
            var src = new Types.TCharRecord();
            foreach (var f in typeof(Types.TCharRecord).GetFields())
            {
                var ft = f.FieldType;
                if (ft == typeof(byte)) f.SetValue(src, (byte)r.Next(256));
                else if (ft == typeof(sbyte)) f.SetValue(src, (sbyte)r.Next(-128, 128));
                else if (ft == typeof(ushort)) f.SetValue(src, (ushort)r.Next(65536));
                else if (ft == typeof(int)) f.SetValue(src, r.Next());
                else if (ft == typeof(string)) f.SetValue(src, "N" + r.Next());
                else if (ft == typeof(byte[])) { var arr = (byte[])f.GetValue(src); r.NextBytes(arr); }
                else if (ft == typeof(sbyte[])) { var arr = (sbyte[])f.GetValue(src); for (int i = 0; i < arr.Length; i++) arr[i] = (sbyte)r.Next(-128, 128); }
                else if (ft == typeof(ushort[])) { var arr = (ushort[])f.GetValue(src); for (int i = 0; i < arr.Length; i++) arr[i] = (ushort)r.Next(65536); }
                else if (ft == typeof(Types.TSpellSlots)) r.NextBytes(((Types.TSpellSlots)f.GetValue(src)).Data);
            }
            src.itemsAddr = new Types.TItemRec(); src.effectsAddr = new Types.TEffectRec(); src.nextCharAddr = new Types.TCharRecord();
            src.equipped[3] = new Types.TItemRec();
            var dst = new Types.TCharRecord();
            EclVm.CopyCharRecord(src, dst);
            foreach (var f in typeof(Types.TCharRecord).GetFields())
            {
                object a = f.GetValue(src), b = f.GetValue(dst);
                if (f.FieldType == typeof(Types.ByteView) || f.FieldType == typeof(Types.ByteGridView)) { Yes(!ReferenceEquals(a, b), "views stay per instance"); continue; }
                if (f.FieldType.IsArray)
                {
                    var x = (Array)a; var y = (Array)b;
                    Yes(!ReferenceEquals(x, y) && x.Length == y.Length, "array copied " + f.Name);
                    for (int i = 0; i < x.Length; i++) Yes(Equals(x.GetValue(i), y.GetValue(i)), "array element " + f.Name);
                }
                else if (f.FieldType == typeof(Types.TSpellSlots))
                    Yes(!ReferenceEquals(a, b) && ((Types.TSpellSlots)a).Data.SequenceEqual(((Types.TSpellSlots)b).Data), "spell slots copied");
                else Yes(Equals(a, b), "field " + f.Name);
            }
            Yes(ReferenceEquals(dst.equipped[3], src.equipped[3]), "equipped pointers copied, not targets");
            dst.strCur = 77; Yes(dst.stats[0] == 77 && src.stats[0] != 77 || src.strCur == 77, "dest views read dest fields");
        }
    }

    // LOAD CHARACTER
    static void RunLoadChar()
    {
        for (int idx = 0; idx < 256; idx++)
            for (int dump = 0; dump < 4; dump++)
            {
                Setup();
                var ms = Enumerable.Range(0, 5).Select(i => Member("p" + i)).ToArray();
                Party(ms);
                GDVars.curChar = ms[2];
                EclVm.savedChar = (idx & 1) == 0 ? ms[2] : ms[0];
                GDVars.dumpCr6C00 = (dump & 1) != 0; GDVars.dumpCr6B00 = (dump & 2) != 0;
                var a = new Asm(); a.Op(C_Ecl.LOAD_CHAR, I(idx)); Exec(a);
                int n = idx & 0x7F;
                var want = n < 5 ? ms[n] : ms[2];
                Yes(ReferenceEquals(GDVars.curChar, want), "LOAD CHARACTER " + idx);
                bool remove = (idx & 0x80) != 0 && dump == 3;
                Yes(fake.Log.Contains("RemoveCreature:False,True") == remove, "remove from party");
                Yes(EclVm.restoreChar == (remove && ReferenceEquals(EclVm.savedChar, want) ? 0 : 1), "restoreChar");
                if (remove) Yes(!GDVars.dumpCr6C00 && !GDVars.dumpCr6B00 && fake.Log.Last() == "DisplayParty:" + want.name, "flags cleared, party redrawn");
            }
        // EXIT restores the backed-up character
        Setup();
        var m = Enumerable.Range(0, 3).Select(i => Member("q" + i)).ToArray();
        Party(m);
        GDVars.curChar = m[0]; EclVm.savedChar = m[0];
        var s = new Asm(); s.Op(C_Ecl.LOAD_CHAR, I(2)); s.Op(C_Ecl.EXIT_SCRIPT); s.Load();
        EclVm.CallEclScript(Asm.Base);
        Yes(ReferenceEquals(GDVars.curChar, m[0]) && EclVm.restoreChar == 0, "EXIT restores curChar");
    }

    // DAMAGE
    static void RunDamage()
    {
        for (int t = 0; t < 4000; t++)
        {
            Setup();
            int size = 1 + rng.Next(6);
            var ms = Enumerable.Range(0, size).Select(i => Member("d" + i)).ToArray();
            foreach (var x in ms) x.enabled = (byte)(rng.Next(4) == 0 ? 0 : 1);
            Party(ms);
            GDVars.curChar = ms[rng.Next(size)];
            var cur = GDVars.curChar;
            int mode = t % 5;
            int attack = mode == 0 ? 0xC0 | 0x20 | rng.Next(32) : mode == 1 ? 0xC0 | rng.Next(32) : mode == 2 || mode == 3 ? 0x80 | rng.Next(32) : rng.Next(4);
            int value = mode == 2 ? 0x80 | rng.Next(256) : rng.Next(128);
            if (mode == 4) value = rng.Next(256);
            int dice = rng.Next(4), sides = 1 + rng.Next(8), bonus = rng.Next(256);
            // rolls: dice for the damage, then 1dSIZE when not all-party, then per attack 1dSIZE and a damage reroll
            var plannedRolls = new List<int>();
            Func<int, int> roll = s => { int v = rng.Next(s); plannedRolls.Add(v); return v + 1; };
            var wantDmg = new List<Tuple<string, ushort>>();
            var saves = new Queue<bool>(); var hits = new Queue<bool>();
            int dmg = bonus; int rolled = 0; for (int i = 0; i < dice; i++) rolled += roll(sides); dmg += (byte)rolled;
            bool allParty = (attack & 0x40) != 0;
            int count = 0;
            if (!allParty) count = roll(size);
            if ((attack & 0x80) != 0)
            {
                int saveType = value & 7;
                if (allParty)
                    foreach (var x in ms)
                    {
                        if ((attack & 0x20) != 0) wantDmg.Add(Tuple.Create(x.name, (ushort)dmg));
                        else { bool ok = rng.Next(2) == 0; saves.Enqueue(ok); if (!ok) wantDmg.Add(Tuple.Create(x.name, (ushort)dmg)); }
                    }
                else if ((value & 0x80) != 0)
                {
                    if (saveType == 0) wantDmg.Add(Tuple.Create(cur.name, (ushort)dmg));
                    else { bool ok = rng.Next(2) == 0; saves.Enqueue(ok); if (!ok) wantDmg.Add(Tuple.Create(cur.name, (ushort)dmg)); }
                }
                else
                {
                    var victim = ms[Math.Max(1, count) - 1];
                    bool ok = rng.Next(2) == 0; saves.Enqueue(ok); if (!ok) wantDmg.Add(Tuple.Create(victim.name, (ushort)dmg));
                }
            }
            else
                for (int i = 1; i <= attack; i++)
                {
                    int c = roll(size);
                    bool hit = rng.Next(2) == 0; hits.Enqueue(hit);
                    if (hit) wantDmg.Add(Tuple.Create(ms[Math.Max(1, c) - 1].name, (ushort)dmg));
                    rolled = 0; for (int k = 0; k < dice; k++) rolled += roll(sides); dmg = bonus + (byte)rolled;
                }
            foreach (var v in plannedRolls) rolls.Enqueue(v);
            fake.Saves = saves; fake.Hits = hits;
            var a = new Asm(); a.Op(C_Ecl.DAMAGE, I(attack), I(dice), I(sides), I(bonus), I(value)); Exec(a);
            Yes(fake.Damage.SequenceEqual(wantDmg), "damage targets mode " + mode);
            Yes(rolls.Count == 0 && saves.Count == 0 && hits.Count == 0, "all rolls and saves used");
            var saveLines = fake.Log.Where(l => l.StartsWith("Save:")).ToList();
            int st = value & 7, sm = attack & 0x1F;
            Yes(saveLines.All(l => l.EndsWith(":" + (((attack & 0x40) == 0 && (value & 0x80) != 0) ? st - 1 : st) + ":" + sm)), "save type and modifier");
            Yes(fake.Log.Where(l => l.StartsWith("Attack:")).All(l => l.EndsWith(":" + value)), "attack value");
            bool defeated = ms.All(x => x.enabled == 0);
            Yes(GDVars.partyDefeated == defeated && draw.Contains("delay:3000") == defeated, "party defeat");
            Yes(ReferenceEquals(GDVars.curChar, cur), "current character restored");
        }
    }

    // SPELL
    static void RunSpell()
    {
        for (int t = 0; t < 3000; t++)
        {
            Setup();
            int size = rng.Next(7);
            var ms = Enumerable.Range(0, size).Select(i => Member("s" + i)).ToArray();
            foreach (var x in ms) { x.status = (byte)(rng.Next(3) == 0 ? 1 : 0); for (int k = 0; k <= 20; k++) x.memorizedSpells[k] = (byte)rng.Next(12); }
            Party(ms);
            int spell = rng.Next(12);
            var a = new Asm(); a.Op(C_Ecl.SPELL, I(spell), A(0x4E00), A(0x4E02)); Exec(a);
            int slot = 255, idx = 0; bool found = false;
            for (int i = 0; i < size && !found; i++)
            {
                if (ms[i].status == 0) for (int k = 1; k <= 20; k++) if (!found && ms[i].memorizedSpells[k] == spell) { found = true; slot = k - 1; }
                if (!found) idx++;
            }
            Yes(fake.Mem[0x4E00] == slot && fake.Mem[0x4E02] == idx, "spell slot and member");
        }
    }

    static void RunMisc()
    {
        // CLOCK in steps of at most 255
        foreach (int n in new[] { 0, 1, 254, 255, 256, 510, 511, 1000, 65535 })
        {
            Setup();
            var a = new Asm(); a.Op(C_Ecl.CLOCK, Wd(n)); Exec(a);
            var steps = fake.Log.Where(l => l.StartsWith("Time:")).ToList();
            var want = new List<string>(); int m = n; while (m > 255) { want.Add("Time:255:1"); m -= 255; } want.Add("Time:" + m + ":1");
            Yes(steps.SequenceEqual(want), "clock " + n);
        }
        // PRINT RETURN / CLEAR BOX / DELAY / CLEAR MONSTERS
        Setup();
        GDVars.curRow = 7; GDVars.curCol = 20; GDVars.protNewLine = 1;
        var b = new Asm(); b.Op(C_Ecl.PRINT_RET); Exec(b);
        Yes(GDVars.curRow == 8 && GDVars.curCol == C_Msg.TEXT_LEFT && GDVars.protNewLine == 0 && GDVars.eclIp == Asm.Base + 1, "PRINTRETURN");
        b = new Asm(); b.Op(C_Ecl.CLEAR_BOX); Exec(b);
        Yes(GDVars.dsEcl3dClearFlag == 1 && draw.Contains("c:1:1:15:15"), "CLEAR BOX");
        GDVars.gameDelay = 3; b = new Asm(); b.Op(C_Ecl.PAUSE); Exec(b);
        Yes(draw.Last() == "delay:675" && GDVars.eclIp == Asm.Base + 1, "DELAY uses the text delay");
        EclVm.creatureCnt = 9; EclVm.monstersOn = 1; GDVars.gIconAnim = 20;
        b = new Asm(); b.Op(C_Ecl.CLEAR_MON); Exec(b);
        Yes(EclVm.creatureCnt == 0 && EclVm.monstersOn == 0 && GDVars.gIconAnim == 8 && fake.Log.Last() == "ClearTreasure", "CLEAR MONSTERS");
        // APPROACH
        foreach (int d in new[] { 0, 1, 2 })
        {
            Setup(); GDVars.zone1[C_Gbvm.MONSTER_DIST] = (ushort)d; EclVm.monId = 3; EclVm.monPicId = 4;
            b = new Asm(); b.Op(C_Ecl.APPROACH); Exec(b);
            Yes(GDVars.zone1[C_Gbvm.MONSTER_DIST] == Math.Max(0, d - 1) && fake.Log.Contains("Monster:3,4," + (d - 1)) == (d > 0), "APPROACH");
        }
        // SETUP MONSTER
        for (int req = 0; req < 4; req++) for (int max = 0; max < 3; max++)
        {
            Setup(); fake.MaxDist = (byte)max; GDVars.gMapX = -3; GDVars.gMapY = 5; GDVars.gFacing = 6;
            b = new Asm(); b.Op(C_Ecl.SETUP_MON, Wd(0x1FF), I(req), I(77)); Exec(b);
            Yes(EclVm.monId == 0xFF && EclVm.monPicId == 77 && GDVars.zone1[C_Gbvm.MONSTER_DIST] == Math.Min(req, max), "SETUP MONSTER");
            Yes(fake.Log.Contains("MaxDist:253,5,6") && fake.Log.Last() == "Monster:255,77," + Math.Min(req, max), "SETUP MONSTER draw");
        }
        // SPRITE OFF
        Setup(); GDVars.sprShown = true; GDVars.eclGraphics = true;
        b = new Asm(); b.Op(C_Ecl.SPRITE_OFF); Exec(b);
        Yes(fake.Log.Contains("MapView") && !GDVars.sprShown && !GDVars.eclGraphics, "SPRITE OFF");
        Setup(); b = new Asm(); b.Op(C_Ecl.SPRITE_OFF); Exec(b);
        Yes(!fake.Log.Contains("MapView"), "SPRITE OFF without a sprite");
        // FIND ITEM
        for (int t = 0; t < 500; t++)
        {
            Setup();
            var ms = Enumerable.Range(0, 1 + rng.Next(4)).Select(i => Member("f" + i)).ToArray();
            foreach (var x in ms) x.itemsAddr = Items(Enumerable.Range(0, rng.Next(4)).Select(k => "i").ToArray());
            foreach (var x in ms) for (var it = x.itemsAddr; it != null; it = it.next) it.itemType = (byte)rng.Next(6);
            Party(ms);
            int type = rng.Next(6);
            for (int k = 0; k < 6; k++) GDVars.cmpFlags[k] = true;
            b = new Asm(); b.Op(C_Ecl.FIND_ITEM, I(type)); Exec(b);
            bool has = ms.Any(x => { for (var it = x.itemsAddr; it != null; it = it.next) if (it.itemType == type) return true; return false; });
            Yes(GDVars.cmpFlags[0] == has && GDVars.cmpFlags[1] == !has && !GDVars.cmpFlags[2] && !GDVars.cmpFlags[5], "FIND ITEM");
        }
        // PARTY SURPRISE
        foreach (int cls in new[] { C_Class.RANGER, C_Class.CLERIC_RANGER, C_Class.MAGE, C_Class.CLERIC })
        {
            Setup(); var x = Member("r"); x.charClass = (sbyte)cls; Party(Member("a"), x);
            b = new Asm(); b.Op(C_Ecl.PARTY_SURPR, A(0x4E10), A(0x4E12)); Exec(b);
            Yes(fake.Mem[0x4E10] == ((cls == C_Class.RANGER || cls == C_Class.CLERIC_RANGER) ? 1 : 0) && fake.Mem[0x4E12] == 0, "PARTY SURPRISE");
        }
        // ROB
        foreach (int pct in new[] { 0, 1, 33, 50, 99, 100, 101, 255 })
            foreach (int whole in new[] { 0, 1 })
            {
                Setup(); var ms = new[] { Member("x0"), Member("x1"), Member("x2") }; Party(ms); GDVars.curChar = ms[1];
                b = new Asm(); b.Op(C_Ecl.ROB, I(whole), I(pct), I(42)); Exec(b);
                int p = Math.Min(pct, 100);
                string frac = ((100 - p) / 100.0).ToString("R");
                var who = whole == 0 ? new[] { "x1" } : new[] { "x0", "x1", "x2" };
                var want = new List<string>();
                foreach (var w in who) { want.Add("RobMoney:" + frac + ":" + w); want.Add("RobItems:42:" + w); want.Add("Recalc:" + w); }
                want.Add("DisplayParty:x1");
                Yes(fake.Log.Skip(1).SequenceEqual(want), "ROB " + pct + " " + whole);
            }
        // ADD NPC
        foreach (int id in new[] { 3, 24 })
            foreach (bool full in new[] { false, true })
                foreach (int morale in new[] { 0, 1, 100, 255 })
                {
                    Setup(); var lead = Member("l"); Party(lead); GDVars.curChar = lead;
                    var npc = Member("npc");
                    fake.OnAddCreature = () => { if (!full) { lead.nextCharAddr = npc; GDVars.zone1[C_Gbvm.PARTY_SIZE] = 2; GDVars.curChar = npc; } };
                    b = new Asm(); b.Op(C_Ecl.ADD_NPC, I(id), I(morale)); Exec(b);
                    if (full) Yes(lead.npc == 0 && !fake.Log.Any(l => l.StartsWith("Recalc")), "full party takes nobody");
                    else Yes(npc.npc == ((morale / 2) | 0x80) && npc.hostile == (id == 24 ? 1 : 0) && fake.Log.Last() == "DisplayParty:npc", "ADD NPC");
                }
        // WHO, with and without the duel table
        foreach (var c in new[] { new[] { 11, 0x9C72, 0x9C05 }, new[] { 18, 0xA985, 0xA90D }, new[] { 16, 0xA300, 0xA287 }, new[] { 11, 0x9900, 0 } })
            foreach (bool escape in new[] { false, true })
            {
                Setup(); var p0 = Member("w0"); var p1 = Member("w1"); Party(p0, p1); GDVars.curChar = p0;
                fake.Picked = escape ? null : p1; fake.Strings[2] = new string('Q', 50);
                var a = new Asm();
                while (Asm.Base + a.Bytes.Count < c[1]) a.Bytes.Add(0);
                a.Op(C_Ecl.WHO, S(2));
                a.Load();
                GDVars.eclScriptId = (byte)c[0];
                GDVars.eclIp = (ushort)c[1]; GDVars.curOpcode = C_Ecl.WHO;
                EclVm.ExecNextEclCmd();
                Yes(fake.Log.Contains("SelectChar:" + new string('Q', 40) + ":" + (c[2] != 0)), "WHO prompt and exit flag");
                if (escape) Yes(GDVars.eclIp == c[2] && ReferenceEquals(GDVars.curChar, p0), "WHO escape goes back");
                else Yes(ReferenceEquals(GDVars.curChar, p1) && GDVars.eclIp == c[1] + 3, "WHO picks");
            }
        // INPUT STRING / NUMBER with Enter alone
        Setup(); io.Keys.Clear(); io.Keys.Enqueue(13);
        b = new Asm(); b.Op(C_Ecl.INPUT_STR, I(0), A(0x4E20)); Exec(b);
        Yes(fake.MemStr[0x4E20] == " ", "empty INPUT STRING stores a space");
        // PRINT a number
        Setup(); b = new Asm(); b.Op(C_Ecl.PRINT, Wd(4321)); Exec(b);
        Yes(GDVars.gbStrings[1] == "4321" && GDVars.msgDelayFlag == 0 && GDVars.bottomMsgCleared == 0, "PRINT number");
        Setup(); GDVars.curRow = 2; b = new Asm(); b.Op(C_Ecl.PRINTCLEAR, Wd(7)); Exec(b);
        Yes(draw.Contains("c:" + C_Msg.TEXT_LEFT + ":" + C_Msg.MESSAGE_TOP + ":" + C_Msg.TEXT_RIGHT + ":" + C_Msg.TEXT_BOTTOM), "PRINTCLEAR clears");
        // PICTURE: removal and a portrait
        Setup(); GDVars.eclGraphics = true; GDVars.gLocType = C_Area.LOC_3D_MAP; GDVars.dsMonsterFlags[0] = true; GDVars.dsMonsterFlags[1] = true;
        b = new Asm(); b.Op(C_Ecl.PICTURE, I(255)); Exec(b);
        Yes(fake.Log.Contains("MapView") && !GDVars.eclGraphics && GDVars.picDrawn && !GDVars.dsMonsterFlags[0] && !GDVars.dsMonsterFlags[1], "PICTURE 255");
        Setup(); GDVars.zone1[C_Gbvm.PIC_LOADING] = 12; GDVars.zone0.view3dFlag = 1;
        b = new Asm(); b.Op(C_Ecl.PICTURE, I(40)); Exec(b);
        Yes(fake.Log.Last() == "Portrait:12,40" && GDVars.eclGraphics && GDVars.dsMonsterFlags[1], "PICTURE portrait");
        // LOADPIECES with all slots 255 and LOADFILES that loads nothing: interface drawn once both ran
        Setup(); EclVm.enteringLoop = 1; var lp = Member("lp"); Party(lp); GDVars.curChar = lp;
        for (int i = 1; i <= 3; i++) { GDVars.dsWallSetCache[i].wallDefId = 5; GDVars.dsWallSetCache[i].symbolSlot = 5; }
        b = new Asm(); b.Op(C_Ecl.LOADPIECES, I(255), I(255), I(255)); Exec(b);
        Yes(Enumerable.Range(1, 3).All(i => GDVars.dsWallSetCache[i].wallDefId == -1 && GDVars.dsWallSetCache[i].symbolSlot == -1), "255 clears wall slots");
        Yes(!fake.Log.Contains("Noncombat") && EclVm.piecesLoaded == 1 && EclVm.filesLoaded == 1, "pieces only");
        GDVars.zone0.view3dFlag = 1;
        b = new Asm(); b.Op(C_Ecl.LOADFILES, I(255), I(0), I(255)); Exec(b);
        Yes(fake.Log.Skip(fake.Log.Count - 2).SequenceEqual(new[] { "Noncombat", "DisplayParty:lp" }) && EclVm.enteringLoop == 0, "interface drawn after both");
    }

    static void RunCall()
    {
        GDVars.map3d = new Types.TGeoMap();
        for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++) { GDVars.map3d.events[y, x] = B(y * 16 + x); GDVars.map3d.wallsNE[y, x] = B(x * 3 + y); GDVars.map3d.wallsSW[y, x] = B(x + y * 5); }
        foreach (int addr in new[] { 0x2C90, 0x8000, 0x8001, 0xBA03, 0xC01E, 0xC018 })
            for (int variant = 0; variant < 4; variant++)
            {
                Setup();
                GDVars.gMapX = (sbyte)(variant * 3); GDVars.gMapY = (sbyte)(variant + 4); GDVars.gFacing = (byte)(variant * 2);
                EclVm.filesLoaded = (byte)(variant & 1); GDVars.sprShown = variant >= 2; GDVars.sndEffect = (ushort)(variant == 1 ? 8 : variant == 2 ? 10 : 9);
                GDVars.gLocType = (byte)(variant == 3 ? 2 : 1); GDVars.wallAhead = 99;
                var a = new Asm(); a.Op(C_Ecl.CALL, Wd(addr)); Exec(a);
                var calls = fake.Log.Skip(1).ToList();
                switch (addr)
                {
                    case 0x2C90:
                        Yes(GDVars.eventAtCell == View3D.GetMapEvent(GDVars.gMapX, GDVars.gMapY), "event refreshed");
                        bool redraw = (variant & 1) == 1 && variant >= 2;
                        Yes(calls.SequenceEqual(redraw ? new[] { "MapView", "MiscInfo" } : new string[0]), "view refresh " + variant);
                        if (redraw) Yes(!GDVars.sprShown && !GDVars.picRegionW, "flags cleared");
                        break;
                    case 0x8000: Yes(calls.SequenceEqual(new[] { "FightsAlone:1" }), "solo with mirror"); break;
                    case 0x8001: Yes(calls.SequenceEqual(new[] { "FightsAlone:0" }), "solo"); break;
                    case 0xBA03:
                        var want = variant == 1 ? new[] { "Sound:" + GDConsts.soundIds.combat[C_Sound.STEP] } : variant == 2 ? new[] { "Sound:" + GDConsts.soundIds.combat[C_Sound.ECL_CUE] } : new string[0];
                        Yes(calls.SequenceEqual(want), "sound cue"); break;
                    case 0xC01E: Yes(calls.SequenceEqual(new[] { "MoveForward" }), "move forward"); break;
                    case 0xC018:
                        Yes(GDVars.wallAhead == (variant == 3 ? 99 : View3D.GetWallType(GDVars.gMapX, GDVars.gMapY, GDVars.gFacing)), "wall ahead"); break;
                }
            }
        for (int i = 0; i < 3000; i++)
        {
            int addr = rng.Next(65536);
            if (new[] { 0x2C90, 0x8000, 0x8001, 0xBA03, 0xC01E, 0xC018 }.Contains(addr)) continue;
            Setup(); GDVars.wallAhead = 7; GDVars.eventAtCell = 7;
            var a = new Asm(); a.Op(C_Ecl.CALL, Wd(addr)); Exec(a);
            Yes(fake.Log.Count == 1 && GDVars.wallAhead == 7 && GDVars.eventAtCell == 7, "other CALL addresses do nothing");
        }
    }

    static void RunProtection()
    {
        for (int t = 0; t < 2000; t++)
        {
            Setup();
            int row = rng.Next(20), col = 1 + rng.Next(38);
            GDVars.curRow = (byte)row; GDVars.curCol = (byte)col;
            GDVars.dsEcl3dClearFlag = (byte)rng.Next(2);
            ushort baseAddr = W(rng.Next(65536));
            int len = rng.Next(45);
            for (int i = 0; i < len; i++) fake.Mem[W(baseAddr + i)] = W(rng.Next(1, 250) + (rng.Next(4) == 0 ? 0x100 : 0));
            int end = rng.Next(3);
            fake.Mem[W(baseAddr + len)] = end == 0 ? (ushort)0 : end == 1 ? (ushort)0xFF : (ushort)0x1FF;
            var a = new Asm(); a.Op(C_Ecl.PROTECTION, A(baseAddr)); Exec(a);
            int r = row, c = col; bool moved = false;
            if (GDVars.dsEcl3dClearFlag != 0 && r > 13) { r = 5; c = 2; moved = true; }
            var want = new List<string>();
            int k = 0;
            while (true)
            {
                int ch = (fake.ReadGbvm(W(baseAddr + k)) + 1) & 0xFF;
                if (ch > 1) { if (c <= 39 && r <= 24 && ch < 177) want.Add("g:" + c + ":" + r + ":15:" + ch); c++; k++; } else break;
                if (c > C_Msg.TEXT_RIGHT) break;
            }
            Yes(draw.SequenceEqual(want), "protection glyphs");
            Yes(GDVars.curRow == r + 1 && GDVars.curCol == c && GDVars.protNewLine == 1 && GDVars.protTextShown == (moved ? 1 : 0), "protection cursor");
        }
    }

    static void RunCombat()
    {
        foreach (int shop in new[] { 0, 1, 2 })
            foreach (int temple in new[] { 0, 1 })
                foreach (int mons in new[] { 0, 1 })
                    foreach (bool duel in new[] { false, true })
                    {
                        Setup();
                        GDVars.zone1[C_Gbvm.ENTER_SHOP] = (ushort)shop; GDVars.zone1[C_Gbvm.ENTER_TEMPLE] = (ushort)temple;
                        EclVm.monstersOn = (byte)mons; GDVars.duelFlag = duel; GDVars.zone0.view3dFlag = 1;
                        GDVars.zone1[C_Gbvm.MONSTER_DIST] = 2; fake.MaxDist = 1; GDVars.zone1[C_Gbvm.SEARCH_FLAGS] = 7;
                        GDVars.gLocType = (byte)(mons == 1 ? 1 : 3); GDVars.eclGraphics = true;
                        var a = new Asm(); a.Op(C_Ecl.START_COMBAT); Exec(a);
                        var calls = fake.Log.Where(l => l != "Noncombat" && !l.StartsWith("MaxDist")).ToList();
                        string[] want;
                        if (mons == 0 && !duel)
                            want = shop == 1 ? new[] { "Shop" } : shop == 2 ? new[] { "Vault" } : temple == 1 ? new[] { "Temple" } : new[] { "PostCombat" };
                        else want = new[] { "Combat", "PostCombat" };
                        Yes(calls.SequenceEqual(want), "COMBAT dispatch");
                        if (mons == 0 && !duel) Yes(GDVars.zone1[C_Gbvm.ENTER_SHOP] == (shop == 0 ? 0 : 0) && (shop != 0 || GDVars.zone1[C_Gbvm.ENTER_TEMPLE] == 0), "entry flag consumed");
                        else Yes(GDVars.zone1[C_Gbvm.MONSTER_DIST] == 1 && !GDVars.duelFlag, "distance capped, duel over");
                        Yes(GDVars.ifcState == (GDVars.gLocType == 1 ? C_Ifc.INDOORS : C_Ifc.OUTDOORS) && GDVars.zone1[C_Gbvm.SEARCH_FLAGS] == 1, "interface state");
                        Yes(!GDVars.eclGraphics && fake.Log.Last() == "Noncombat" && GDVars.eclIp == Asm.Base + 1, "after combat");
                    }
    }

    static void RunProgram()
    {
        foreach (int prog in new[] { 0, 8, 9, 3, 255 })
            foreach (bool interrupted in new[] { false, true })
            {
                Setup();
                var lead = Member("pl"); var other = Member("po"); Party(lead, other);
                GDVars.curChar = other; EclVm.savedChar = lead; EclVm.restoreChar = 1;
                var a = new Asm();
                a.Op(C_Ecl.RUN_PROGRAM, I(prog));
                ushort e3 = a.Here; a.Op(C_Ecl.SAVE, I(3), A(0x4F03)); a.Op(C_Ecl.EXIT_SCRIPT);
                ushort e4 = a.Here; a.Op(C_Ecl.SAVE, I(4), A(0x4F04)); a.Op(C_Ecl.EXIT_SCRIPT);
                GDVars.scriptEntryAddrs[3] = e3; GDVars.scriptEntryAddrs[4] = e4;
                fake.CampInterrupted = interrupted;
                Exec(a);
                Yes(ReferenceEquals(GDVars.curChar, lead) && EclVm.restoreChar == 0, "PROGRAM restores the character");
                var calls = fake.Log.Where(l => !l.StartsWith("LoadEclVars") && !l.StartsWith("W:")).ToList();
                if (prog == 0) Yes(calls.SequenceEqual(new[] { "Training", "Noncombat" }), "training");
                else if (prog == 8) Yes(calls.SequenceEqual(new[] { "WinGame" }), "win");
                else if (prog == 9)
                {
                    var want = new List<string> { "Camp" };
                    if (interrupted) want.Add("Noncombat");
                    want.Add("MapView");
                    Yes(calls.SequenceEqual(want), "camp sequence");
                    Yes(fake.Mem.ContainsKey(0x4F03) && fake.Mem.ContainsKey(0x4F04) == interrupted, "camp entries run");
                    Yes(GDVars.eclIp == Asm.Base + 4 && EclVm.eclStop == 1 && GDVars.recentSaved == 0, "camp then EXIT from the PROGRAM's IP");
                }
                else Yes(calls.Count == 0, "other programs do nothing");
            }
    }

    static void RunGameLoop()
    {
        Setup();
        var a = new Asm();
        ushort onLoad = a.Here; a.Op(C_Ecl.SAVE, I(1), A(0x4F10)); a.Op(C_Ecl.EXIT_SCRIPT);
        // entry 1 runs NEWECL on its first two runs
        ushort entry1 = a.Here;
        a.Op(C_Ecl.ADD, A(0x4F20), I(1), A(0x4F20));
        a.Op(C_Ecl.COMPARE, A(0x4F20), I(3));
        a.Op(C_Ecl.IF_LT);
        int jump = a.Op(C_Ecl.JUMP, Wd(0));
        a.Op(C_Ecl.EXIT_SCRIPT);
        ushort doNew = a.Here; a.Op(C_Ecl.NEWECL, I(26));
        ushort search = a.Here; a.Op(C_Ecl.ADD, A(0x4F30), I(1), A(0x4F30)); a.Op(C_Ecl.EXIT_SCRIPT);
        ushort camp3 = a.Here; a.Op(C_Ecl.EXIT_SCRIPT);
        a.PatchWord(jump + 2, doNew);
        a.Load();
        fake.SkipOperands[C_Ecl.JUMP] = 1;
        GDVars.scriptEntryAddrs[1] = entry1; GDVars.scriptEntryAddrs[2] = search; GDVars.scriptEntryAddrs[3] = camp3;
        GDVars.scriptEntryAddrs[4] = camp3; GDVars.scriptEntryAddrs[5] = onLoad;
        GDVars.zone0.previousScriptId = 26; GDVars.zone0.view3dFlag = 0;
        GDVars.zone0.yarashMissionDone = 254; GDVars.zone0.yarashPyramidLeft = 0;
        var lead = Member("g"); Party(lead); GDVars.curChar = lead;
        GDVars.gMapX = 4; GDVars.gMapY = 4;
        foreach (char c in "eMQ") fake.MapCommands.Enqueue(c);
        // the third map menu asks for a quick load
        int menus = 0;
        var explore = new QuickExplore(fake, () => ++menus == 3);
        EclVm.Units.Explore = explore;
        EclVm.GameLoop();
        var big = fake.Log.Where(l => !l.StartsWith("LoadEclVars") && !l.StartsWith("W:") && !l.StartsWith("CmpInt") && !l.StartsWith("Skip")).ToList();
        var want = new List<string> {
            "LoadEclScript:26", "EclInit", "MapMenu", "Camp", "MapView", "MapMenu",
            // entry 1 runs NEWECL; NewEclDone: on-load, Yarash (central wilderness, view3d 0), entry 1 runs NEWECL again
            // so the search entry waits and the loop repeats; the third entry 1 ends, then search and party
            "CheckFile:" + Short79(GDVars.gameDir + DaxIo.ArchiveName("ECL1") + ".dax"), "LoadEclScript:26", "EclInit",
            "Yarash:True",
            "CheckFile:" + Short79(GDVars.gameDir + DaxIo.ArchiveName("ECL1") + ".dax"), "LoadEclScript:26", "EclInit",
            "Yarash:True", "DisplayParty:g",
            "MapMenu" };
        Yes(big.SequenceEqual(want), "game loop sequence: " + string.Join(",", big));
        Yes(GDVars.gLocType == C_Area.LOC_WILD_CENTRAL && GDVars.ifcState == C_Ifc.OUTDOORS, "wilderness state");
        Yes(fake.Mem[0x4F30] == 1, "search entry ran once");
        Yes(!GDVars.partyDefeated && GDVars.zone0.previousScriptId == 26, "loop exit state");

        // movement path: a plain command moves the party, plays the step and runs the search entry
        Setup();
        a.Load();
        fake.SkipOperands[C_Ecl.JUMP] = 1;
        fake.Mem[0x4F20] = 5;
        GDVars.scriptEntryAddrs[1] = entry1; GDVars.scriptEntryAddrs[2] = search; GDVars.scriptEntryAddrs[5] = onLoad;
        GDVars.zone0.previousScriptId = 5; GDVars.zone0.view3dFlag = 1;
        lead = Member("h"); Party(lead); GDVars.curChar = lead; GDVars.gMapX = 2; GDVars.gMapY = 9;
        foreach (char c in "MQ") fake.MapCommands.Enqueue(c);
        menus = 0;
        EclVm.Units.Explore = new QuickExplore(fake, () => ++menus == 2);
        EclVm.GameLoop();
        big = fake.Log.Where(l => !l.StartsWith("LoadEclVars") && !l.StartsWith("W:") && !l.StartsWith("CmpInt") && !l.StartsWith("Skip")).ToList();
        want = new List<string> { "LoadEclScript:5", "EclInit", "Noncombat", "MapView", "MapMenu", "Move", "MapView",
            "Sound:" + GDConsts.soundIds.combat[C_Sound.STEP], "MapMenu" };
        Yes(big.SequenceEqual(want), "movement sequence: " + string.Join(",", big));
        Yes(GDVars.zone0.previousX == 2 && GDVars.zone0.previousY == 9 && GDVars.gMapX == 3 && GDVars.picDrawn, "movement state");
    }

    // NewEclDone's location types by script id and view, and the Yarash completion test
    static void RunNewEclDone()
    {
        foreach (int script in new[] { 24, 25, 26, 27, 28 })
            foreach (int view in new[] { 0, 1 })
                foreach (int done in new[] { 253, 254 })
                    foreach (int left in new[] { 0, 1 })
                        foreach (int prevLoc in new[] { 1, 3 })
                        {
                            Setup();
                            var a = new Asm(); a.Op(C_Ecl.EXIT_SCRIPT); a.Load();
                            for (int i = 1; i <= 5; i++) GDVars.scriptEntryAddrs[i] = Asm.Base;
                            GDVars.eclScriptId = (byte)script; GDVars.zone0.view3dFlag = (ushort)view;
                            GDVars.zone0.yarashMissionDone = (ushort)done; GDVars.zone0.yarashPyramidLeft = (ushort)left;
                            GDVars.gLocType = (byte)prevLoc; EclVm.filesLoaded = 1;
                            var g = Member("n"); Party(g); GDVars.curChar = g;
                            EclVm.NewEclDone();
                            int loc = view != 0 || script < 25 || script > 27 ? 1 : script - 23;
                            Yes(GDVars.gLocType == loc && GDVars.locTypePrev == prevLoc, "location type");
                            Yes(GDVars.ifcState == (loc == 1 ? C_Ifc.INDOORS : C_Ifc.OUTDOORS), "interface state");
                            Yes(fake.Log.Contains("Yarash:True") == (loc == 3 && done == 254 && left == 0), "Yarash completion");
                            Yes(fake.Log.Contains("MapView") == (prevLoc > 1 || loc == 1), "map view redraw");
                            Yes(draw.Count > 0 == (prevLoc > 1 && loc == 1), "3D border drawn on entering the 3D map");
                            Yes(GDVars.zone1[C_Gbvm.LEAVE_MAP] == 0 && fake.Log.Last() == "DisplayParty:n", "leave flag and party");
                        }
    }

    static string Short79(string s) { return s.Length > 79 ? s.Substring(0, 79) : s; }

    class QuickExplore : IExploreUnit
    {
        readonly Fake f; readonly Func<bool> quick; bool asked;
        public QuickExplore(Fake f, Func<bool> quick) { this.f = f; this.quick = quick; }
        public char MapMainMenu() { char c = f.MapMainMenu(); if (quick()) asked = true; return c; }
        public void ResolveMovement() { f.ResolveMovement(); }
        public bool QuickLoadAsked { get { return asked; } }
    }

    static void RunUnbound()
    {
        Setup();
        EclVm.Units = new EclVmUnits();
        var a = new Asm(); a.Op(C_Ecl.EXIT_SCRIPT); a.Load();
        EclVm.CallEclScript(Asm.Base);
        Yes(EclVm.eclStop == 0 && GDVars.eclIp == Asm.Base + 1, "EXIT needs no unbound unit");
        string msg = null;
        try { a = new Asm(); a.Op(C_Ecl.JUMP, Wd(0x9905)); Exec(a); } catch (NotSupportedException e) { msg = e.Message; }
        Yes(msg != null && msg.Contains("EclVars"), "unbound EclVars fails clearly");
        EclVm.Units.EclVars = fake;
        msg = null;
        try { a = new Asm(); a.Op(C_Ecl.START_COMBAT); Exec(a); } catch (NotSupportedException e) { msg = e.Message; }
        Yes(msg != null && msg.Contains("PostCbt"), "unbound game units fail clearly");
        msg = null;
        try { EclVm.GameLoop(); } catch (NotSupportedException e) { msg = e.Message; }
        Yes(msg != null && msg.Contains("EclMenu"), "game loop without EclMenu fails clearly");
    }
}
