// CharHlp part 3: WeaponAdjust/RecalcStats against a separately written model, and ItemLabel.
using System;
using System.Collections.Generic;
using System.Linq;
using Por.Core;

static partial class Tests
{
    static Types.TItemRec[] ModelLink(List<Types.TItemRec> items)
    {
        var exp = new Types.TItemRec[13];
        foreach (var it in items)
        {
            if (it.equipped == 0) continue;
            int slot = GDVars.baseItems[it.itemType].slot;
            if (slot >= C_Equip.WEAPON && slot <= C_Equip.BOOTS) exp[slot] = it;
            else if (slot == C_Equip.RING1) { if (exp[C_Equip.RING1] != null) { if (exp[C_Equip.RING2] == null) exp[C_Equip.RING2] = it; } else exp[C_Equip.RING1] = it; }
            if (it.itemType == C_Item.ARROW) exp[C_Equip.ARROW] = it;
            if (it.itemType == C_Item.QUARREL) exp[C_Equip.BOLT] = it;
        }
        return exp;
    }

    // WeaponAdjust, from the Pascal text, on plain ints: returns thac0Cur, curModA[1], rolls, dice
    static int[] ModelWeapon(Types.TCharRecord c, Types.TItemRec[] eq)
    {
        var w = eq[C_Equip.WEAPON];
        int thac0 = c.thac0Cur, mod = c.curModA[1], rolls = c.curRollsA[1], dice = c.curDiceA[1];
        if (w == null) return new[] { thac0, mod, rolls, dice };
        var bi = GDVars.baseItems[w.itemType];
        thac0 = c.thac0Base;
        if ((bi.props & 0x02) != 0) thac0 = (thac0 + AbilAdj.DexMissileAdjust(c)) & 0xFF;
        mod = bi.dmgSmMod;
        if ((bi.props & 0x04) != 0) { thac0 = (thac0 + AbilAdj.StrToHitAdjust(c)) & 0xFF; mod = SB(mod + AbilAdj.StrDamageAdjust(c)); }
        int plus = w.plus;
        if (bi.props > 0x7F && eq[C_Equip.BOLT] != null) plus = SB(plus + eq[C_Equip.BOLT].plus);
        if ((bi.props & 0x01) > 0 && eq[C_Equip.ARROW] != null) plus = SB(plus + eq[C_Equip.ARROW].plus);
        mod = SB(mod + plus);
        bool elfBonus = c.race == C_Race.ELF && ((w.itemType >= 41 && w.itemType <= 44) || w.itemType == 36 || w.itemType == 37 ||
                        (w.itemType == 45 && w.nameIds[2] >= 41 && w.nameIds[2] <= 44));
        if (elfBonus) plus = SB(plus + 1);
        thac0 = (thac0 + plus) & 0xFF;
        return new[] { thac0, mod, bi.dmgSmRolls, bi.dmgSmDice };
    }

    static void RandomBase(int lo, int hi)
    {
        for (int t = lo; t <= hi; t++)
        {
            var b = GDVars.baseItems[t];
            int k = rng.Next(0, 6);
            b.slot = B(k == 0 ? C_Equip.WEAPON : k == 1 ? C_Equip.ARMOR : k == 2 ? C_Equip.SHIELD : k == 3 ? C_Equip.RING1 : rng.Next(0, 16));
            b.hands = B(rng.Next(0, 3)); b.acDescr = B(rng.Next(0, 4) == 0 ? rng.Next(0, 128) : rng.Next(128, 256) & (rng.Next(0, 3) == 0 ? 0x80 : 0xFF));
            b.props = B(rng.Next(0, 256)); b.dmgSmMod = SB(rng.Next(-3, 4)); b.dmgSmRolls = B(rng.Next(0, 4)); b.dmgSmDice = B(rng.Next(0, 13));
        }
    }

    static Types.TCharRecord RandomChar()
    {
        var c = Ch();
        c.strCur = B(rng.Next(3, 26)); c.strExc = B(rng.Next(0, 101)); c.dexCur = B(rng.Next(3, 26)); c.race = SB(rng.Next(0, 8));
        c.thac0Base = B(rng.Next(0, 22)); c.acBase = B(rng.Next(0, 12)); c.movementBase = B(rng.Next(3, 15));
        c.thac0Cur = 99; c.acCur = 99; c.movementCur = 99; c.saveBonus = 77; c.encumbrance = 9999; c.attackLevel = 50; c.acBehind = 50;
        for (int k = 0; k < 7; k++) c.coins[k] = W(rng.Next(0, 4) == 0 ? rng.Next(0, 65536) : rng.Next(0, 400));
        for (int k = 0; k < 8; k++) c.classLevels[k] = SB(rng.Next(0, 3) == 0 ? rng.Next(0, 10) : 0);
        for (int j = 1; j <= 2; j++) { c.unarmedRollsA[j] = B(rng.Next(0, 3)); c.unarmedDiceA[j] = B(rng.Next(0, 9)); c.unarmedModA[j] = SB(rng.Next(-2, 3)); c.curRollsA[j] = 88; c.curDiceA[j] = 88; c.curModA[j] = 88; }
        return c;
    }

    static List<Types.TItemRec> RandomItems(Types.TCharRecord c)
    {
        int n = rng.Next(0, 10);
        var items = new List<Types.TItemRec>();
        for (int i = 0; i < n; i++)
        {
            int type = rng.Next(0, 8) == 0 ? C_Item.ARROW : (rng.Next(0, 8) == 0 ? C_Item.QUARREL : rng.Next(40, 60));
            var it = It(type, rng.Next(-3, 6), rng.Next(-2, 4), rng.Next(0, 3) == 0 ? 0 : 1, rng.Next(0, 3) == 0 ? rng.Next(0, 3000) : rng.Next(0, 500), rng.Next(0, 3) == 0 ? rng.Next(0, 40) : 0);
            it.nameIds[1] = B(rng.Next(0, 6) == 0 ? C_Item.NAME_HOLDING : rng.Next(0, 5)); it.nameIds[2] = B(rng.Next(0, 8) == 0 ? rng.Next(41, 46) : rng.Next(0, 3));
            items.Add(it);
        }
        Link(c, items.ToArray());
        return items;
    }

    static void CharHlpRecalc()
    {
        ResetWorld();
        // WeaponAdjust alone, 20000 random weapons
        for (int trial = 0; trial < 20000; trial++)
        {
            RandomBase(40, 59);
            var c = RandomChar();
            var items = RandomItems(c);
            var weapon = It(rng.Next(0, 3) == 0 ? rng.Next(36, 38) : rng.Next(40, 60), rng.Next(-3, 6)); weapon.nameIds[2] = B(rng.Next(40, 46));
            if (rng.Next(0, 5) == 0) weapon.itemType = C_Item.HEAVY_CROSSBOW;
            var eq = new Types.TItemRec[13]; eq[C_Equip.WEAPON] = rng.Next(0, 6) == 0 ? null : weapon;
            if (rng.Next(0, 2) == 0) eq[C_Equip.BOLT] = It(C_Item.QUARREL, rng.Next(-2, 5));
            if (rng.Next(0, 2) == 0) eq[C_Equip.ARROW] = It(C_Item.ARROW, rng.Next(-2, 5));
            for (int s = 0; s < 13; s++) c.equipped[s] = eq[s];
            var e = ModelWeapon(c, eq);
            CharHlp.WeaponAdjust(c);
            Eq(e[0], c.thac0Cur, "WeaponAdjust thac0 " + trial); Eq(e[1], c.curModA[1], "WeaponAdjust mod " + trial);
            Eq(e[2], c.curRollsA[1], "WeaponAdjust rolls " + trial); Eq(e[3], c.curDiceA[1], "WeaponAdjust dice " + trial);
        }
        // RecalcStats, 20000 random creatures against the model
        for (int trial = 0; trial < 20000; trial++)
        {
            RandomBase(40, 59); GDVars.baseItems[C_Item.ARROW].slot = 0; GDVars.baseItems[C_Item.QUARREL].slot = 0;
            var c = RandomChar();
            var items = RandomItems(c);
            string tag = "RecalcStats " + trial;
            var eq = ModelLink(items);
            // counts, weights, hands
            int n = items.Count; long raw = 0; int equipWeight = 0; int hands = 0; bool bag = false;
            foreach (var it in items)
            {
                long w = it.quantity > 0 ? (long)it.quantity * it.weight : it.weight;
                raw += w;
                if (it.equipped != 0) { equipWeight = (equipWeight + (int)w) & 0xFFFF; if (it.nameIds[1] == C_Item.NAME_HOLDING) bag = true; hands = (hands + GDVars.baseItems[it.itemType].hands) & 0xFF; }
            }
            for (int k = 0; k < 7; k++) raw += c.coins[k];
            int saveBonus = 0, ringSave = 0, thac0 = c.thac0Base, ac = c.acBase, mv = c.movementBase;
            var cm = new int[3]; var cr = new int[3]; var cd = new int[3];
            for (int j = 1; j <= 2; j++) { cm[j] = c.unarmedModA[j]; cr[j] = c.unarmedRollsA[j]; cd[j] = c.unarmedDiceA[j]; }
            var adj = new int[5]; adj[Types.AC_DEX] = AbilAdj.DexterityArmorAdjustment(c);
            if (eq[C_Equip.WEAPON] == null)
            {
                thac0 = (thac0 + AbilAdj.StrToHitAdjust(c)) & 0xFF;
                cm[1] = SB(cm[1] + AbilAdj.StrDamageAdjust(c));
            }
            // the weapon step restarts from the base values; build a probe creature carrying the state so far
            var probe = RandomChar();
            probe.race = c.race; probe.strCur = c.strCur; probe.strExc = c.strExc; probe.dexCur = c.dexCur; probe.thac0Base = c.thac0Base;
            probe.thac0Cur = B(thac0); probe.curModA[1] = SB(cm[1]); probe.curRollsA[1] = B(cr[1]); probe.curDiceA[1] = B(cd[1]);
            var wv = ModelWeapon(probe, eq);
            thac0 = wv[0]; cm[1] = wv[1]; cr[1] = wv[2]; cd[1] = wv[3];
            bool magicBody = false;
            foreach (var it in items)
            {
                if (it.equipped == 0) continue;
                var bi = GDVars.baseItems[it.itemType];
                if (bi.slot == C_Equip.ARMOR)
                {
                    mv = it.weight <= 150 ? c.movementBase : (it.weight <= 399 ? 9 : 6);
                    if (it.plus != 0 && mv <= 9) mv += 3;
                }
                int descr = bi.acDescr;
                if (descr > 127)
                {
                    descr &= 127;
                    if (bi.slot == C_Equip.SHIELD) adj[Types.AC_SHIELD] = SB(descr + it.plus);
                    else if (descr == 0)
                    {
                        if (bi.slot == C_Equip.RING1) { if (it.plus > adj[Types.AC_RING]) adj[Types.AC_RING] = it.plus; if (it.plus > ringSave) ringSave = it.plus; if (it.saveBonus > ringSave) ringSave = it.saveBonus; }
                        else { adj[Types.AC_PLUSES] = SB(adj[Types.AC_PLUSES] + it.plus); saveBonus = SB(saveBonus + it.saveBonus); }
                    }
                    else if (descr + it.plus > adj[Types.AC_ARMOR]) { adj[Types.AC_ARMOR] = SB(descr + it.plus); if (it.plus > 0 && bi.slot == C_Equip.ARMOR) magicBody = true; }
                }
            }
            saveBonus = SB(saveBonus + ringSave);
            if (magicBody) adj[Types.AC_RING] = 0;
            if (bag) { raw = raw < 5000 ? 0 : raw - 5000; if (raw < equipWeight) raw = equipWeight; }
            if (raw > 65535) raw = 65535;
            int enc = (int)raw;
            var tmp = Ch(); tmp.strCur = c.strCur; tmp.strExc = c.strExc;
            int over = enc - AbilAdj.WeightAllowance(tmp); if (over < 0) over = 0;
            int emv = over <= 512 ? mv : (over <= 768 ? 9 : (over <= 1024 ? 6 : 3));
            if (emv < mv) mv = emv;
            if (adj[Types.AC_ARMOR] < ac) adj[Types.AC_ARMOR] = SB(ac);
            int acCur = 0; for (int k = 0; k < 5; k++) acCur = (acCur + adj[k]) & 0xFF;
            int behind = (adj[Types.AC_ARMOR] + adj[Types.AC_PLUSES] + adj[Types.AC_RING] - 2) & 0xFF;
            int wl = Math.Max(0, Math.Max((int)c.classLevels[C_Level.FIGHTER], Math.Max((int)c.classLevels[C_Level.PALADIN], (int)c.classLevels[C_Level.RANGER])));
            int attack = (wl > 0 && c.race > C_Race.MONSTER) ? wl : 1;

            CharHlp.RecalcStats(c);
            Eq(n, c.numberOfItems, tag + " numberOfItems"); Eq(hands, c.handsEquipped, tag + " hands"); Eq(enc, c.encumbrance, tag + " encumbrance");
            Eq(thac0, c.thac0Cur, tag + " thac0"); Eq(acCur, c.acCur, tag + " ac"); Eq(behind, c.acBehind, tag + " acBehind"); Eq(mv, c.movementCur, tag + " movement");
            Eq(saveBonus, c.saveBonus, tag + " saveBonus"); Eq(attack, c.attackLevel, tag + " attackLevel");
            for (int j = 1; j <= 2; j++) { Eq(cm[j], c.curModA[j], tag + " mod" + j); Eq(cr[j], c.curRollsA[j], tag + " rolls" + j); Eq(cd[j], c.curDiceA[j], tag + " dice" + j); }
            for (int s = 0; s < 13; s++) Yes(ReferenceEquals(eq[s], c.equipped[s]), tag + " equipped " + s);
            if (!bag) Eq(Math.Min(raw, 65535), Math.Min(CharHlp.RawWeight(c), 65535), tag + " RawWeight is the weight before a bag takes its share");
        }
        // named cases
        ResetWorld();
        GDVars.baseItems[50].slot = C_Equip.ARMOR; GDVars.baseItems[50].acDescr = 0x80 | 6;  // armor value 6
        var hero = Ch(); hero.strCur = 16; hero.dexCur = 10; hero.acBase = 2; hero.movementBase = 12; hero.race = C_Race.HUMAN; hero.classLevels[C_Level.FIGHTER] = 4;
        var plate = It(50, 1, 0, 1, 450);
        Link(hero, plate);
        CharHlp.RecalcStats(hero);
        Eq(1, hero.numberOfItems, "named: one item"); Eq(450, hero.encumbrance, "named: its weight"); Eq(4, hero.attackLevel, "named: a fighter fights at its level");
        Eq(9, hero.movementCur, "named: heavy armour gives 6, a magical plus gives 3 back");
        Eq(7, hero.acCur, "named: armour 6 + plus 1"); Eq(5, hero.acBehind, "named: acBehind = armour + pluses + ring - 2");
        Eq(1, hero.handsEquipped == 0 ? 1 : 0, "named: the armour takes no hands (base hands 0)");
        // a Bag of Holding removes up to 5000 coins but never goes below the equipped weight
        var bagItem = It(51, 0, 0, 1, 20); bagItem.nameIds[1] = C_Item.NAME_HOLDING;
        var loot = It(52, 0, 0, 0, 1, 40);
        var rich = Ch(); rich.strCur = 16; rich.coins[C_Money.COPPER] = 9000;
        Link(rich, bagItem, loot);
        CharHlp.RecalcStats(rich);
        Eq(9000 + 20 + 40 - 5000, rich.encumbrance, "named: bag takes 5000 of 9060");
        rich.coins[C_Money.COPPER] = 100;
        CharHlp.RecalcStats(rich);
        Eq(20, rich.encumbrance, "named: bag never leaves less than the equipped weight");
        rich.coins[C_Money.COPPER] = 65535; rich.coins[C_Money.PLATINUM] = 65535; rich.coins[C_Money.GEMS] = 65535; rich.coins[C_Money.JEWELRY] = 65535;
        CharHlp.RecalcStats(rich);
        Eq(65535, rich.encumbrance, "named: encumbrance is stored as at most 65535");
    }
}
