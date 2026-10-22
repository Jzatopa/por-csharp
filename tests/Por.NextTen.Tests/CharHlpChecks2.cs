// CharHlp part 2: spells, weapons, level caps, equipment arithmetic, RecalcStats and labels.
using System;
using System.Collections.Generic;
using System.Linq;
using Por.Core;

static partial class Tests
{
    static void CharHlpSpells()
    {
        // caster level: cleric / mage level of the current character, 12 for the special class, 6 from an item (not for special)
        for (int usingItem = 0; usingItem <= 1; usingItem++)
            for (int cl = 0; cl <= 9; cl += 3)
                for (int ml = 0; ml <= 9; ml += 3)
                    for (int spell = 1; spell <= 67; spell++)
                    {
                        ResetWorld();
                        GDVars.usingItem = usingItem != 0;
                        GDVars.curChar = Ch(); GDVars.curChar.classLevels[C_Level.CLERIC] = SB(cl); GDVars.curChar.classLevels[C_Level.MAGE] = SB(ml);
                        var sp = GDConsts.spellTable[spell];
                        int cls = sp.castClass;
                        int lvl = cls == 0 ? cl : (cls == 1 ? ml : 12);
                        if (usingItem == 1 && cls != 2) lvl = 6;
                        Eq(lvl, CharHlp.GetCasterLevel(B(spell)), "GetCasterLevel " + spell + "/" + usingItem + "/" + cl + "/" + ml);
                        int r = usingItem == 0 ? (sp.rangeFixed + sp.rangeVar * lvl) & 0xFF : (sp.rangeVar * 6 + sp.rangeFixed) & 0xFF;
                        if (r == 0 && sp.area != 0) r = 1;
                        if (r == 255) r = 1;
                        Eq(r, CharHlp.CalcSpellRange(B(spell)), "CalcSpellRange " + spell + "/" + usingItem + "/" + cl + "/" + ml);
                    }
        // HasScrollSpells: the creature may use items and holds a castable scroll with a spell on it
        foreach (int ifc in new[] { C_Ifc.CAMPING, C_Ifc.COMBAT, 0 })
            foreach (int spellA in new[] { 0, 4, 0x80, 0x81, 57 })
                foreach (int pos in new[] { 0, 1, 2, 3 })
                    foreach (bool readable in new[] { false, true })
                    {
                        ResetWorld();
                        GDVars.ifcState = B(ifc);
                        GDVars.baseItems[20].slot = B(readable ? 11 : 4);
                        GDVars.baseItems[21].slot = 4;
                        var c = Ch(); c.combatAddr = new Types.TCombatRec { itemUseAllowed = 1 };
                        var a = It(21); var b = It(20, 0, 0, 1); var d = It(21);
                        var scrolls = new[] { a, b, d };
                        Link(c, scrolls);
                        b.unidMask = 0;
                        b.spells[1] = 0; b.spells[2] = 0; b.spells[3] = 0;
                        if (pos >= 1 && pos <= 3) b.spells[pos] = B(spellA);
                        bool usable = ifc != 0;
                        int id = spellA & 0x7F;
                        bool hasSpell = pos >= 1 && pos <= 3 && id >= 1 && id <= 56;
                        Yes(CharHlp.HasScrollSpells(c) == (usable && readable && hasSpell), "HasScrollSpells " + ifc + "/" + spellA + "/" + pos + "/" + readable);
                    }
    }

    static void CharHlpWeapons()
    {
        ResetWorld();
        var c = Ch();
        Yes(!CharHlp.HasRangedWeapon(c), "no weapon");
        Yes(!CharHlp.HasThrowableWeapon(c), "no weapon: not throwable");
        for (int range = 0; range <= 4; range++)
            for (int props = 0; props < 256; props++)
            {
                GDVars.baseItems[30].range = B(range); GDVars.baseItems[30].props = B(props);
                c.equipped[C_Equip.WEAPON] = It(30);
                Yes(CharHlp.HasRangedWeapon(c) == (range > 1), "ranged " + range);
                Yes(CharHlp.HasThrowableWeapon(c) == (range > 1 && (props & 0x14) == 0x14), "throwable " + range + "/" + props);
            }
        // FindDamagingRangedWeapon
        var arrow = It(C_Item.ARROW); var bolt = It(C_Item.QUARREL);
        for (int props = 0; props < 256; props++)
            foreach (bool hasArrow in new[] { false, true })
                foreach (bool hasBolt in new[] { false, true })
                    foreach (bool hasWeapon in new[] { false, true })
                    {
                        GDVars.baseItems[30].props = B(props);
                        var cc = Ch();
                        var w = It(30);
                        cc.equipped[C_Equip.WEAPON] = hasWeapon ? w : null;
                        cc.equipped[C_Equip.ARROW] = hasArrow ? arrow : null; cc.equipped[C_Equip.BOLT] = hasBolt ? bolt : null;
                        Types.TItemRec sel = bolt; // must be overwritten
                        bool r = CharHlp.FindDamagingRangedWeapon(cc, ref sel);
                        Types.TItemRec exp = null; int p = 0;
                        if (hasWeapon)
                        {
                            p = props;
                            if ((p & 0x10) != 0) exp = w;
                            if ((p & 0x08) != 0) { if ((p & 0x01) != 0) exp = hasArrow ? arrow : null; if ((p & 0x80) != 0) exp = hasBolt ? bolt : null; }
                        }
                        Yes(ReferenceEquals(exp, sel), "sel " + props + hasArrow + hasBolt + hasWeapon);
                        Yes(r == (exp != null || p == 0x0A), "found " + props + hasArrow + hasBolt + hasWeapon);
                    }
    }

    // level caps, from the table in charhlp.pas
    static readonly int[,] CapTable = { { 7, 8, 0 }, { 5, 6, 7 }, { 5, 5, 6 }, { 6, 7, 8 }, { 4, 5, 6 } };

    static bool ModelAtCap(int race, int cls, int str, int lvl)
    {
        if (lvl <= 0) return false;
        int band = str < 17 ? 0 : (str == 17 ? 1 : 2);
        if (race == C_Race.HALFELF && cls == C_Level.CLERIC) return lvl >= 5;
        if (race >= 1 && race <= 5 && (cls == C_Level.FIGHTER || (cls == C_Level.RANGER && race == C_Race.HALFELF)))
        { int cap = CapTable[race - 1, band]; return cap != 0 && lvl >= cap; }
        return false;
    }

    static void CharHlpCaps()
    {
        ResetWorld();
        for (int race = 0; race <= 7; race++)
            for (int cls = 0; cls <= 7; cls++)
                foreach (int str in new[] { 3, 16, 17, 18, 25 })
                    for (int lvl = -1; lvl <= 10; lvl++)
                    {
                        var c = Ch(); c.race = SB(race); c.strCur = B(str); c.classLevels[cls] = SB(lvl);
                        Yes(CharHlp.AtLevelCap(c, SB(cls)) == ModelAtCap(race, cls, str, lvl), "AtLevelCap " + race + "/" + cls + "/" + str + "/" + lvl);
                    }
        // CanTrain: some class below the top level with the experience for the next level, not at the racial cap
        for (int trial = 0; trial < 30000; trial++)
        {
            var c = Ch(); c.race = SB(rng.Next(0, 8)); c.strCur = B(rng.Next(3, 26));
            for (int k = 0; k < 8; k++) c.classLevels[k] = SB(rng.Next(0, 4) == 0 ? rng.Next(1, 11) : 0);
            int[] pts = { 0, 1500, 1501, 2000, 2001, 2750, 2751, 4000, 8000, 20000, 125000, 125001, 200000 };
            c.experience = pts[rng.Next(pts.Length)] + rng.Next(0, 3) - 1;
            bool exp = false;
            for (int k = 0; k <= 7; k++)
            {
                int lvl = c.classLevels[k];
                if (lvl > 0 && lvl < C_Level.LEVEL_LAST && !ModelAtCap(c.race, k, c.strCur, lvl))
                {
                    int need = GDConsts.dsExperienceSpellsClass[k].xp[lvl + 1];
                    if (need > 0 && need <= c.experience) exp = true;
                }
            }
            Yes(CharHlp.CanTrain(c) == exp, "CanTrain trial " + trial);
        }
        // a half-elf Fighter 5 with XP for 6 can train only up to the cap; check one named case through the real table
        var f = Ch(); f.race = C_Race.ELF; f.strCur = 16; f.classLevels[C_Level.FIGHTER] = 5; f.experience = 1000000;
        Yes(CharHlp.AtLevelCap(f, C_Level.FIGHTER), "elf fighter 5 with Str 16 is at the cap (5)");
        Yes(!CharHlp.CanTrain(f), "so no training");
        f.strCur = 17; Yes(!CharHlp.AtLevelCap(f, C_Level.FIGHTER) && CharHlp.CanTrain(f), "Str 17 raises the elf's cap to 6");
    }

    static void CharHlpEquip()
    {
        // ArmorMoveAdjust
        foreach (int slot in new[] { C_Equip.ARMOR, C_Equip.SHIELD, 0 })
            foreach (int plus in new[] { -2, 0, 1, 5 })
                foreach (int mvBase in new[] { 3, 6, 9, 10, 12, 15 })
                    foreach (int w in Enumerable.Range(0, 520).Concat(new[] { 700, 1000, 65535 }))
                    {
                        ResetWorld();
                        GDVars.baseItems[40].slot = B(slot);
                        var c = Ch(); c.movementBase = B(mvBase); c.movementCur = 77;
                        var it = It(40, plus, 0, 1, w);
                        CharHlp.ArmorMoveAdjust(c, it);
                        int exp = 77;
                        if (slot == C_Equip.ARMOR)
                        {
                            exp = w <= 150 ? mvBase : (w <= 399 ? 9 : 6);
                            if (plus != 0 && exp <= 9) exp += 3;
                        }
                        Eq(exp, c.movementCur, "ArmorMoveAdjust " + slot + "/" + plus + "/" + mvBase + "/" + w);
                    }
        // ArmorAdjustHelper
        foreach (int acDescr in new[] { 0, 5, 127, 128, 129, 130, 133, 140, 255 })
            foreach (int slot in new[] { C_Equip.SHIELD, C_Equip.ARMOR, C_Equip.RING1, 0, 3 })
                foreach (int plus in new[] { -3, 0, 2, 5 })
                    foreach (int save in new[] { -1, 0, 3 })
                        foreach (int adjStart in new[] { 0, 3, 7 })
                            foreach (int ringStart in new[] { -2, 0, 4 })
                            {
                                ResetWorld();
                                GDVars.baseItems[41].acDescr = B(acDescr); GDVars.baseItems[41].slot = B(slot);
                                var c = Ch(); c.saveBonus = 2;
                                var it = It(41, plus, save, 1);
                                var adj = new Types.TAcAdjust();
                                for (int k = 0; k < 5; k++) adj[k] = SB(adjStart + k);
                                bool magic = false; sbyte ringSave = SB(ringStart);
                                CharHlp.ArmorAdjustHelper(c, it, adj, ref magic, ref ringSave);
                                var e = new int[5]; for (int k = 0; k < 5; k++) e[k] = adjStart + k;
                                int eSave = 2, eRing = ringStart; bool eMagic = false;
                                if (acDescr > 127)
                                {
                                    int d = acDescr & 127;
                                    if (slot == C_Equip.SHIELD) e[Types.AC_SHIELD] = SB(d + plus);
                                    else if (d == 0)
                                    {
                                        if (slot == C_Equip.RING1)
                                        {
                                            if (plus > e[Types.AC_RING]) e[Types.AC_RING] = plus;
                                            if (plus > eRing) eRing = plus;
                                            if (save > eRing) eRing = save;
                                        }
                                        else { e[Types.AC_PLUSES] = SB(e[Types.AC_PLUSES] + plus); eSave = SB(eSave + save); }
                                    }
                                    else if (d + plus > e[Types.AC_ARMOR])
                                    {
                                        e[Types.AC_ARMOR] = SB(d + plus);
                                        if (plus > 0 && slot == C_Equip.ARMOR) eMagic = true;
                                    }
                                }
                                string tag = acDescr + "/" + slot + "/" + plus + "/" + save + "/" + adjStart + "/" + ringStart;
                                for (int k = 0; k < 5; k++) Eq(e[k], adj[k], "adj" + k + " " + tag);
                                Eq(eSave, c.saveBonus, "saveBonus " + tag); Eq(eRing, ringSave, "ringSave " + tag); Yes(magic == eMagic, "magic " + tag);
                            }
        // EncumbranceAdjust: movement only ever lowers, by the coins carried over the weight allowance
        for (int str = 3; str <= 25; str += 2)
            for (int mv = 1; mv <= 12; mv++)
                foreach (int enc in Enumerable.Range(0, 120).Select(i => i * 97).Concat(new[] { 65535, 65534 }).Concat(Enumerable.Range(0, 40).Select(i => 400 + i * 40)))
                {
                    var c = Ch(); c.strCur = B(str); c.movementCur = B(mv); c.encumbrance = W(enc);
                    int over = enc - AbilAdj.WeightAllowance(c); if (over < 0) over = 0;
                    int m = over <= 512 ? mv : (over <= 768 ? 9 : (over <= 1024 ? 6 : 3));
                    CharHlp.EncumbranceAdjust(c);
                    Eq(m < mv ? m : mv, c.movementCur, "EncumbranceAdjust " + str + "/" + mv + "/" + enc);
                }
        // boundaries exactly: allowance 0 at strength 8..11
        foreach (var t in new[] { new[] { 512, 5 }, new[] { 513, 9 }, new[] { 768, 9 }, new[] { 769, 6 }, new[] { 1024, 6 }, new[] { 1025, 3 } })
        {
            var c = Ch(); c.strCur = 10; c.movementCur = 12; c.encumbrance = W(t[0]);
            CharHlp.EncumbranceAdjust(c);
            Eq(t[1] == 5 ? 12 : t[1], c.movementCur, "encumbrance band edge " + t[0]);
        }
        // LinkEquipped
        ResetWorld();
        for (int trial = 0; trial < 3000; trial++)
        {
            for (int t = 40; t < 60; t++) GDVars.baseItems[t].slot = B(rng.Next(0, 16));
            var c = Ch();
            for (int s = 0; s < 13; s++) c.equipped[s] = It(1);
            int n = rng.Next(0, 9);
            var items = new List<Types.TItemRec>();
            for (int i = 0; i < n; i++)
                items.Add(It(rng.Next(0, 4) == 0 ? C_Item.ARROW : (rng.Next(0, 4) == 0 ? C_Item.QUARREL : rng.Next(40, 60)), 0, 0, rng.Next(0, 3) == 0 ? 0 : 1));
            Link(c, items.ToArray());
            CharHlp.LinkEquipped(c);
            var exp = new Types.TItemRec[13];
            foreach (var it in items)
            {
                if (it.equipped == 0) continue;
                int slot = GDVars.baseItems[it.itemType].slot;
                if (slot >= 0 && slot <= 8) exp[slot] = it;
                else if (slot == 9) { if (exp[9] != null) { if (exp[10] == null) exp[10] = it; } else exp[9] = it; }
                if (it.itemType == C_Item.ARROW) exp[11] = it;
                if (it.itemType == C_Item.QUARREL) exp[12] = it;
            }
            for (int s = 0; s < 13; s++) Yes(ReferenceEquals(exp[s], c.equipped[s]), "LinkEquipped slot " + s + " trial " + trial);
        }
    }
}
