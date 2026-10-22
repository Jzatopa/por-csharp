// CharHlp (charhlp.pas at 7616920): pure helpers, list operations, damage, combat gates.
using System;
using System.Collections.Generic;
using System.Linq;
using Por.Core;

static partial class Tests
{
    static Types.TCharRecord Ch()
    {
        var c = new Types.TCharRecord();
        c.status = C_Status.OKAY; c.enabled = 1;
        return c;
    }

    static Types.TItemRec It(int type, int plus = 0, int save = 0, int equipped = 0, int weight = 0, int qty = 0)
    {
        var i = new Types.TItemRec();
        i.itemType = B(type); i.plus = SB(plus); i.saveBonus = SB(save); i.equipped = B(equipped); i.weight = W(weight); i.quantity = B(qty);
        return i;
    }

    static void ResetWorld()
    {
        GDVars.baseItems = new Types.TBaseItems();
        for (int i = 0; i <= 128; i++) GDVars.baseItems[i] = new Types.TBaseItemRec();
        GDVars.creatureList = null; GDVars.curChar = Ch(); GDVars.ifcState = 0; GDVars.usingItem = false;
        GDVars.zone0 = new Types.TGbvmZone0State(); GDVars.zone1 = new Types.TGbvmZone1();
        GDVars.treasureItems = null; GDVars.treasureMoney = new Types.TMoney();
        GDVars.teamCount = new byte[2];
        CharHlp.faultHook = null;
    }

    static List<Types.TItemRec> Chain(Types.TItemRec head)
    {
        var l = new List<Types.TItemRec>();
        for (var p = head; p != null; p = p.next) { l.Add(p); if (l.Count > 1000) throw new Exception("cycle"); }
        return l;
    }

    static void Link(Types.TCharRecord c, params Types.TItemRec[] items)
    {
        c.itemsAddr = items.Length > 0 ? items[0] : null;
        for (int i = 0; i < items.Length; i++) items[i].next = i + 1 < items.Length ? items[i + 1] : null;
    }

    static void CharHlpSimple()
    {
        ResetWorld();
        // TheftChance: 90 less for over 255 coins, 50 less for over 24, never below 0
        foreach (int w in new[] { 0, 1, 24, 25, 254, 255, 256, 1000, 65535 })
            for (int chance = 0; chance <= 255; chance++)
            {
                int pen = w > 255 ? 90 : (w > 24 ? 50 : 0);
                Eq(chance > pen ? chance - pen : 0, CharHlp.TheftChance(B(chance), W(w)), "TheftChance " + chance + "/" + w);
            }
        // ItemWeight: a stack weighs weight * quantity, quantity 0 is a single item
        foreach (int wt in new[] { 0, 1, 7, 100, 65535 })
            for (int q = 0; q <= 255; q++)
                Eq(q > 0 ? (long)q * wt : wt, CharHlp.ItemWeight(It(1, 0, 0, 0, wt, q)), "ItemWeight " + wt + "x" + q);
        // WarriorLevel: highest of fighter, paladin, ranger; 0 when none or negative; other classes ignored
        for (int f = -3; f <= 12; f++) for (int p = -3; p <= 12; p++) for (int r = -3; r <= 12; r++)
        {
            var c = Ch();
            for (int k = 0; k < 8; k++) c.classLevels[k] = SB(rng.Next(0, 20));
            c.classLevels[C_Level.FIGHTER] = SB(f); c.classLevels[C_Level.PALADIN] = SB(p); c.classLevels[C_Level.RANGER] = SB(r);
            Eq(Math.Max(0, Math.Max(f, Math.Max(p, r))), CharHlp.WarriorLevel(c), "WarriorLevel " + f + "," + p + "," + r);
        }
        for (int h = -2; h <= 3; h++) { var c = Ch(); c.hostile = SB(h); Eq(h == 0 ? 1 : 0, CharHlp.EnemyTeam(c), "EnemyTeam " + h); }
        for (int npc = 0; npc <= 255; npc++) { var c = Ch(); c.npc = B(npc); Yes(CharHlp.IsNpc(c) == (npc >= 128), "IsNpc " + npc); }
        foreach (int npc in new[] { 0, 1, 127, 128, 129, 255 })
            foreach (int en in new[] { 0, 1 })
                for (int st = 0; st <= 8; st++)
                {
                    var c = Ch(); c.npc = B(npc); c.enabled = B(en); c.status = B(st);
                    bool isNpc = npc >= 128;
                    Yes(CharHlp.MayTrade(c) == (!isNpc || en == 0 || st == C_Status.ANIMATED), "MayTrade " + npc + "/" + en + "/" + st);
                    Yes(CharHlp.ComesUnderPlayer(c) == (!isNpc && st != C_Status.ANIMATED), "ComesUnderPlayer " + npc + "/" + st);
                }
        // BaseItem: the table entry, or the overflow entry past the table
        var entries = new Types.TBaseItemRec[129];
        for (int i = 0; i <= 128; i++) { entries[i] = new Types.TBaseItemRec(); GDVars.baseItems[i] = entries[i]; }
        for (int t = 0; t <= 255; t++)
            Yes(ReferenceEquals(CharHlp.BaseItem(B(t)), t > 127 ? entries[128] : entries[t]), "BaseItem " + t);
        // ItemReadable: non-null, base slot 11..13
        Yes(!CharHlp.ItemReadable(null), "ItemReadable null");
        for (int s = 0; s <= 30; s++)
        {
            entries[5].slot = B(s);
            Yes(CharHlp.ItemReadable(It(5)) == (s >= 11 && s <= 13), "ItemReadable slot " + s);
        }
        // ScrollSpellAt: slot 1..3, bit 7 ignored, 1..56 only
        var sc = It(5);
        for (int s = 0; s <= 5; s++)
            for (int v = 0; v <= 255; v++)
            {
                sc.spells[1] = B(v); sc.spells[2] = B(255 - v); sc.spells[3] = B(v ^ 0x55);
                int exp = 0;
                if (s >= 1 && s <= 3)
                {
                    int raw = s == 1 ? v : (s == 2 ? 255 - v : (v ^ 0x55));
                    int id = raw & 0x7F;
                    exp = (id >= 1 && id <= 56) ? id : 0;
                }
                Eq(exp, CharHlp.ScrollSpellAt(sc, B(s)), "ScrollSpellAt " + s + "/" + v);
            }
        // ScrollLegible: Read Magic, or any cleric level with a cleric's scroll (slot 12), clears the unidentified mask
        foreach (int mask in new[] { 0, 1, 5, 255 })
            foreach (bool rm in new[] { false, true })
                foreach (int cl in new[] { 0, 1, 3 })
                    foreach (int slot in new[] { 12, 11, 5 })
                    {
                        ResetWorld();
                        GDVars.baseItems[9].slot = B(slot);
                        var c = Ch(); c.classLevels[C_Level.CLERIC] = SB(cl);
                        if (rm) c.effectsAddr = new Types.TEffectRec { id = C_Effect.READ_MAGIC };
                        var item = It(9); item.unidMask = B(mask);
                        bool clears = rm || (cl > 0 && slot == 12);
                        bool legible = CharHlp.ScrollLegible(c, item);
                        Eq(clears ? 0 : mask, item.unidMask, "ScrollLegible mask " + mask + rm + cl + slot);
                        Yes(legible == (item.unidMask == 0), "ScrollLegible result " + mask + rm + cl + slot);
                    }
        // CastableScroll: readable and (readied or class accepted) and legible
        foreach (int slot in new[] { 10, 11, 12, 13, 14 })
            foreach (int eq in new[] { 0, 1 })
                foreach (int cm in new[] { 0, 2, 4 })
                    foreach (int lim in new[] { 0, 2, 6 })
                        foreach (int um in new[] { 0, 3 })
                        {
                            ResetWorld();
                            GDVars.baseItems[9].slot = B(slot); GDVars.baseItems[9].classMask = B(cm);
                            var c = Ch(); c.itemLimits = B(lim);
                            var item = It(9, 0, 0, eq); item.unidMask = B(um);
                            bool readable = slot >= 11 && slot <= 13;
                            bool exp = false;
                            if (readable && (eq != 0 || (cm & lim) != 0))
                            {
                                bool clears = (slot == 12 && c.classLevels[C_Level.CLERIC] > 0);
                                exp = um == 0 || clears;
                            }
                            Yes(CharHlp.CastableScroll(c, item) == exp, "CastableScroll " + slot + eq + cm + lim + um);
                        }
    }

    static void CharHlpLists()
    {
        ResetWorld();
        // HasEffect finds the first node with the id and leaves the effect on it; otherwise nil
        var e1 = new Types.TEffectRec { id = 3 }; var e2 = new Types.TEffectRec { id = 9, duration = 1 }; var e3 = new Types.TEffectRec { id = 9, duration = 2 }; var e4 = new Types.TEffectRec { id = 5 };
        e1.next = e2; e2.next = e3; e3.next = e4;
        var c = Ch(); c.effectsAddr = e1;
        Types.TEffectRec found;
        Yes(CharHlp.HasEffect(c, 9, out found) && ReferenceEquals(found, e2), "HasEffect first match");
        Yes(CharHlp.HasEffect(c, 3, out found) && ReferenceEquals(found, e1), "HasEffect head");
        Yes(CharHlp.HasEffect(c, 5, out found) && ReferenceEquals(found, e4), "HasEffect tail");
        Yes(!CharHlp.HasEffect(c, 7, out found) && found == null, "HasEffect none leaves nil");
        c.effectsAddr = null;
        Yes(!CharHlp.HasEffect(c, 7, out found) && found == null, "HasEffect empty list");
        // IsHelpless: any of the four effects in GDConsts.helplessFx
        var helpless = new HashSet<int>();
        for (int i = 1; i <= GDConsts.HELPLESS_FX_COUNT; i++) helpless.Add(GDConsts.helplessFx[i]);
        Eq(4, GDConsts.HELPLESS_FX_COUNT, "four helplessness effects");
        for (int id = 0; id <= 255; id++)
        {
            c.effectsAddr = new Types.TEffectRec { id = B(id), next = new Types.TEffectRec { id = 200 } };
            Yes(CharHlp.IsHelpless(c) == helpless.Contains(id), "IsHelpless " + id);
        }
        // PrependItemCopy: a copy at the front; copying a list one by one reverses it; the copy is independent
        var src = new List<Types.TItemRec>();
        for (int i = 1; i <= 4; i++)
        {
            var it = It(i, i, -i, i & 1, 100 + i, i);
            it.name = "item" + i; it.inUse = B(i + 10); it.value = W(1000 + i); it.cursed = B(i);
            it.nameIds[1] = B(i); it.nameIds[2] = B(i + 1); it.nameIds[3] = B(i + 2); it.unidMask = B(i); it.variantData[0] = B(i); it.variantData[1] = B(i + 1); it.variantData[2] = B(i + 2);
            src.Add(it);
        }
        Types.TItemRec list = null;
        foreach (var it in src) { it.next = src[0]; CharHlp.PrependItemCopy(ref list, it); }
        var ch = Chain(list);
        Eq(4, ch.Count, "Prepend count");
        for (int i = 0; i < 4; i++)
        {
            var orig = src[3 - i]; var cp = ch[i];
            Yes(!ReferenceEquals(orig, cp), "Prepend makes a new node");
            EqS(orig.name, cp.name, "name"); Eq(orig.inUse, cp.inUse, "inUse"); Eq(orig.itemType, cp.itemType, "type"); Eq(orig.plus, cp.plus, "plus");
            Eq(orig.saveBonus, cp.saveBonus, "save"); Eq(orig.equipped, cp.equipped, "equipped"); Eq(orig.unidMask, cp.unidMask, "unid"); Eq(orig.cursed, cp.cursed, "cursed");
            Eq(orig.weight, cp.weight, "weight"); Eq(orig.quantity, cp.quantity, "qty"); Eq(orig.value, cp.value, "value");
            for (int k = 1; k <= 3; k++) { Eq(orig.nameIds[k], cp.nameIds[k], "nameIds"); Eq(orig.spells[k], cp.spells[k], "spells"); }
            Eq(orig.charges, cp.charges, "charges"); Eq(orig.effect, cp.effect, "effect"); Eq(orig.special, cp.special, "special");
            Yes(!ReferenceEquals(orig.nameIds, cp.nameIds) && !ReferenceEquals(orig.variantData, cp.variantData), "arrays copied by value");
            if (i + 1 < 4) Yes(ReferenceEquals(cp.next, ch[i + 1]), "links to the old front"); else Yes(cp.next == null, "first copy ends the list");
        }
        ch[0].nameIds[1] = 99; ch[0].variantData[0] = 99; ch[0].name = "changed";
        Yes(src[3].nameIds[1] != 99 && src[3].variantData[0] != 99 && src[3].name == "item4", "changing a copy leaves the original");
        // AppendItemCopy: at the end, next nil, whatever the source's next was
        Types.TItemRec alist = null;
        foreach (var it in src) { it.next = src[1]; CharHlp.AppendItemCopy(ref alist, it); }
        ch = Chain(alist);
        Eq(4, ch.Count, "Append count");
        for (int i = 0; i < 4; i++) { EqS(src[i].name, ch[i].name, "Append order " + i); Eq(src[i].value, ch[i].value, "Append value"); Yes(!ReferenceEquals(src[i], ch[i]), "Append new node"); }
        Yes(ch[3].next == null, "Append ends with nil");
        Eq(1, ch[0].nameIds[1], "Append copies nameIds");
        // PrependEffectCopy
        Types.TEffectRec elist = null;
        var ea = new Types.TEffectRec { id = 1, duration = 500, strength = 7, onRemove = true, next = e1 };
        var eb = new Types.TEffectRec { id = 2, duration = 60000, strength = 255, onRemove = false };
        CharHlp.PrependEffectCopy(ref elist, ea); CharHlp.PrependEffectCopy(ref elist, eb);
        Yes(elist != eb && elist.id == 2 && elist.duration == 60000 && elist.strength == 255 && !elist.onRemove, "effect copy 2");
        Yes(elist.next != ea && elist.next.id == 1 && elist.next.duration == 500 && elist.next.strength == 7 && elist.next.onRemove && elist.next.next == null, "effect copy 1 (next replaced by the old front)");
        // FreeItemList and ClearTreasure
        list = ch[0];
        CharHlp.FreeItemList(ref list);
        Yes(list == null, "FreeItemList leaves nil");
        CharHlp.FreeItemList(ref list);
        Yes(list == null, "FreeItemList on nil");
        GDVars.treasureItems = alist;
        for (int i = C_Money.COPPER; i <= C_Money.JEWELRY; i++) GDVars.treasureMoney[i] = 100 + i;
        CharHlp.ClearTreasure();
        Yes(GDVars.treasureItems == null, "ClearTreasure items");
        for (int i = C_Money.COPPER; i <= C_Money.JEWELRY; i++) Eq(0, GDVars.treasureMoney[i], "ClearTreasure money " + i);
        // RemoveItem
        var msgs = new List<string>();
        CharHlp.faultHook = m => msgs.Add(m);
        foreach (int pos in new[] { 0, 1, 2, 3 })
        {
            var cc = Ch(); var items = new[] { It(1), It(2), It(3), It(4) };
            Link(cc, items);
            CharHlp.RemoveItem(cc, items[pos]);
            var rest = items.Where((x, k) => k != pos).ToList();
            var got = Chain(cc.itemsAddr);
            Eq(3, got.Count, "RemoveItem count " + pos);
            for (int k = 0; k < 3; k++) Yes(ReferenceEquals(rest[k], got[k]), "RemoveItem order " + pos + "/" + k);
            Yes(items[pos].next == null, "removed item's link cleared " + pos);
        }
        Eq(0, msgs.Count, "no fault for removals");
        var one = Ch(); var only = It(1); Link(one, only); CharHlp.RemoveItem(one, only);
        Yes(one.itemsAddr == null && only.next == null, "RemoveItem only item");
        var cx = Ch(); var ix = new[] { It(1), It(2) }; Link(cx, ix);
        CharHlp.RemoveItem(cx, null);
        Eq(1, msgs.Count, "fault for nil"); EqS("Nil Item pointer...", msgs[0], "nil message");
        CharHlp.RemoveItem(cx, It(3));
        Eq(2, msgs.Count, "fault for not found"); EqS("Tried to Lose item & couldn't find it!", msgs[1], "not found message");
        Yes(Chain(cx.itemsAddr).Count == 2 && ReferenceEquals(cx.itemsAddr, ix[0]) && ReferenceEquals(ix[0].next, ix[1]), "list unchanged after a failed removal");
        var empty = Ch(); CharHlp.RemoveItem(empty, It(1));
        Eq(3, msgs.Count, "fault on an empty list");
        CharHlp.faultHook = null;
        CharHlp.RemoveItem(empty, It(1)); CharHlp.RemoveItem(empty, null);
        Eq(3, msgs.Count, "no hook, no message (and no exception)");
    }

    static void CharHlpDamage()
    {
        foreach (int st in new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 })
            foreach (int ifc in new[] { C_Ifc.COMBAT, C_Ifc.CAMPING })
                foreach (int hostile in new[] { 0, 1 })
                    foreach (bool partyMember in new[] { false, true })
                        for (int hp = 0; hp <= 14; hp++)
                            foreach (int dmg in new[] { 0, 1, 2, 5, 9, 10, 14, 15, 16, 20, 23, 24, 25, 30, 255 })
                            {
                                ResetWorld();
                                Stats.ResetGame();
                                GDVars.ifcState = B(ifc);
                                var v = Ch(); v.status = B(st); v.hpCur = B(hp); v.hostile = SB(hostile); v.enabled = 1;
                                v.combatAddr = new Types.TCombatRec { initiative = 7, overkill = 0 };
                                GDVars.teamCount[0] = 5; GDVars.teamCount[1] = 6;
                                if (partyMember) { GDVars.creatureList = v; GDVars.zone1[C_Gbvm.PARTY_SIZE] = 1; }
                                long takenBefore = Stats.game.counts[Stats.DAMAGE_TAKEN];
                                CharHlp.InflictDamage(v, B(dmg));
                                string tag = "st" + st + " ifc" + ifc + " host" + hostile + " hp" + hp + " dmg" + dmg + (partyMember ? " party" : "");
                                if (st == C_Status.DEAD || st == C_Status.STONED || st == C_Status.GONE)
                                {
                                    Eq(st, v.status, "untouched status " + tag); Eq(hp, v.hpCur, "untouched hp " + tag); Eq(7, v.combatAddr.initiative, "untouched initiative " + tag);
                                    Eq(5, GDVars.teamCount[0], "teams " + tag); Eq(6, GDVars.teamCount[1], "teams " + tag);
                                    Eq(takenBefore, Stats.game.counts[Stats.DAMAGE_TAKEN], "no statistics " + tag);
                                    continue;
                                }
                                bool wasUp = st == C_Status.OKAY || st == C_Status.ANIMATED;
                                int remain = 0, over = 0;
                                if (hp >= dmg) remain = hp - dmg; else over = dmg - hp;
                                int status = st;
                                int overkill = 0;
                                if (over > 9 || (remain == 0 && st == C_Status.ANIMATED)) status = C_Status.DEAD;
                                else if (over > 0) { status = C_Status.DYING; if (ifc == C_Ifc.COMBAT) overkill = over; }
                                else if (remain == 0) status = C_Status.UNCONSCIOUS;
                                bool up = status == C_Status.OKAY || status == C_Status.ANIMATED;
                                int expHp = up ? remain : 0;
                                int exp0 = 5, exp1 = 6, expInit = 7;
                                if (!up && ifc == C_Ifc.COMBAT)
                                {
                                    if (wasUp) { if (hostile == 0) exp0--; else exp1--; }
                                    expInit = 0;
                                }
                                Eq(status, v.status, "status " + tag); Eq(expHp, v.hpCur, "hp " + tag); Eq(up ? 1 : 0, v.enabled, "enabled " + tag);
                                Eq(overkill, v.combatAddr.overkill, "overkill " + tag); Eq(expInit, v.combatAddr.initiative, "initiative " + tag);
                                Eq(exp0, GDVars.teamCount[0], "team0 " + tag); Eq(exp1, GDVars.teamCount[1], "team1 " + tag);
                                if (partyMember)
                                {
                                    // Stats.Damaged(curChar, creature, hpBefore - hpCur, wasUp): the victim in the party is credited the damage it took
                                    long took = Stats.game.counts[Stats.DAMAGE_TAKEN] - takenBefore;
                                    Eq(Math.Min(hp - expHp & 0xFF, 255), took, "damage credited " + tag);
                                }
                            }
    }

    static void CharHlpCombat()
    {
        // CanUseItems: able, not in an anti-magic area, camping/outdoors/indoors, or in combat with item use allowed
        for (int ifc = 0; ifc <= 8; ifc++)
            foreach (int en in new[] { 0, 1 })
                foreach (int am in new[] { 0, 1, 256 })
                    foreach (int allowed in new[] { 0, 1, 2 })
                    {
                        ResetWorld();
                        GDVars.ifcState = B(ifc); GDVars.zone0.antimagic = W(am);
                        var c = Ch(); c.enabled = B(en); c.combatAddr = new Types.TCombatRec { itemUseAllowed = B(allowed) };
                        bool exp = en != 0 && am == 0 && (ifc == C_Ifc.CAMPING || ifc == C_Ifc.OUTDOORS || ifc == C_Ifc.INDOORS || (ifc == C_Ifc.COMBAT && allowed != 0));
                        Yes(CharHlp.CanUseItems(c) == exp, "CanUseItems " + ifc + en + am + allowed);
                    }
        // CanUseItems on a creature with no combat record outside combat does not need one
        ResetWorld(); GDVars.ifcState = C_Ifc.CAMPING; Yes(CharHlp.CanUseItems(Ch()), "no combat record needed in camp");
        // EndActions / Guard
        var g = Ch(); g.combatAddr = new Types.TCombatRec { initiative = 9, spellCast = 4, guarding = 0, movement = 12, attackPhase = 3 };
        Yes(CharHlp.EndActions(g), "EndActions returns true");
        Eq(0, g.combatAddr.initiative, "ini"); Eq(0, g.combatAddr.spellCast, "spell"); Eq(0, g.combatAddr.guarding, "guard"); Eq(0, g.combatAddr.movement, "move"); Eq(3, g.combatAddr.attackPhase, "attackPhase untouched");
        g.combatAddr = new Types.TCombatRec { initiative = 9, spellCast = 4, guarding = 0, movement = 12 };
        Yes(CharHlp.Guard(g), "Guard returns true");
        Eq(0, g.combatAddr.initiative, "ini"); Eq(0, g.combatAddr.spellCast, "spell"); Eq(1, g.combatAddr.guarding, "guarding raised"); Eq(0, g.combatAddr.movement, "move");
        // CountTeams: creatures still in action on each team
        for (int trial = 0; trial < 300; trial++)
        {
            ResetWorld();
            GDVars.teamCount[0] = 200; GDVars.teamCount[1] = 201;
            int n = rng.Next(0, 12), e0 = 0, e1 = 0;
            Types.TCharRecord head = null;
            for (int i = 0; i < n; i++)
            {
                var c = Ch(); c.enabled = B(rng.Next(0, 3)); c.hostile = SB(rng.Next(0, 2));
                if (c.enabled != 0) { if (c.hostile == 0) e0++; else e1++; }
                c.nextCharAddr = head; head = c;
            }
            GDVars.creatureList = head;
            CharHlp.CountTeams();
            Eq(e0, GDVars.teamCount[0], "CountTeams 0"); Eq(e1, GDVars.teamCount[1], "CountTeams 1");
        }
    }
}
