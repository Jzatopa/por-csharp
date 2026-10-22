// CharHlp part 4: ItemLabel against a model written from the Pascal text.
using System;
using System.Collections.Generic;
using System.Linq;
using Por.Core;

static partial class Tests
{
    static string Fit40(string s) { return s.Length > 40 ? s.Substring(0, 40) : s; }

    static string ModelLabel(Types.TItemRec item, bool isInv, bool detect)
    {
        string name = "";
        if (isInv) name = item.equipped != 0 ? " Yes  " : " No   ";
        if (detect && (item.plus > 0 || item.saveBonus > 0 || item.cursed != 0)) name = Fit40(name + "* ");
        if (item.quantity > 0) name = Fit40(name + item.quantity + " ");
        int mask = 0;
        for (int i = 1; i <= 3; i++)
            if (item.nameIds[i] != 0 && ((item.unidMask >> (3 - i)) & 1) == 0) mask += 1 << (i - 1);
        bool appended = false;
        for (int i = 3; i >= 1; i--)
        {
            if (((mask >> (i - 1)) & 1) == 0) continue;
            name = Fit40(name + GDConsts.nameParts[item.nameIds[i]]);
            if (item.quantity < 2 || appended) name = Fit40(name + " ");
            else
            {
                bool plural = (1 << (i - 1)) == mask || (i == 1 && mask > 4 && item.itemType != C_Item.FLASK_OF_OIL) || (i == 2 && (mask % 2 == 0)) ||
                              (i == 3 && item.itemType == C_Item.FLASK_OF_OIL) ||
                              ((item.itemType == C_Item.ARROW || item.itemType == C_Item.QUARREL) && item.nameIds[3] != C_Item.NAME_SILVER);
                if (plural) { name = Fit40(name + "s "); appended = true; }
                else name = Fit40(name + " ");
            }
        }
        return name;
    }

    static void CharHlpLabel()
    {
        ResetWorld();
        var saved = (string[])GDConsts.nameParts.Clone();
        try
        {
            foreach (bool detect in new[] { false, true })
            {
                var holder = Ch();
                if (detect) holder.effectsAddr = new Types.TEffectRec { id = 1, next = new Types.TEffectRec { id = C_Effect.DETECT_MAGIC } };
                GDVars.creatureList = Ch(); GDVars.creatureList.nextCharAddr = holder;
                foreach (bool isInv in new[] { false, true })
                    foreach (int equipped in new[] { 0, 1, 7 })
                        foreach (int mag in Enumerable.Range(0, 8))
                            foreach (int qty in new[] { 0, 1, 2, 5, 255 })
                                foreach (int type in new[] { 1, C_Item.FLASK_OF_OIL, C_Item.ARROW, C_Item.QUARREL })
                                    for (int ids = 0; ids < 8; ids++)
                                        for (int um = 0; um < 8; um++)
                                            foreach (int silver in new[] { 0, 1 })
                                            {
                                                var it = It(type, (mag & 1) != 0 ? 1 : 0, (mag & 2) != 0 ? 1 : 0, equipped, 10, qty);
                                                it.cursed = (mag & 4) != 0 ? B(1) : B(0);
                                                it.unidMask = B(um);
                                                it.nameIds[1] = (ids & 1) != 0 ? B(5) : B(0); it.nameIds[2] = (ids & 2) != 0 ? B(41) : B(0);
                                                it.nameIds[3] = (ids & 4) != 0 ? B(silver != 0 ? C_Item.NAME_SILVER : 44) : B(0);
                                                it.name = "stale name";
                                                CharHlp.ItemLabel(it, isInv);
                                                EqS(ModelLabel(it, isInv, detect), it.name, "ItemLabel " + detect + isInv + equipped + mag + qty + type + ids + um + silver);
                                            }
            }
            // hand-checked examples (rules in charhlp.pas): " No   " marker, a quantity, parts 3..1, 's' on the part that pluralizes
            for (int i = 0; i < 256; i++) GDConsts.nameParts[i] = "p" + i;
            GDVars.creatureList = null;
            var a = It(C_Item.ARROW, 0, 0, 0, 1, 12); a.nameIds[1] = 7; a.nameIds[2] = 8; a.nameIds[3] = 9; a.unidMask = 0;
            CharHlp.ItemLabel(a, true);
            EqS(" No   12 p9s p8 p7 ", a.name, "arrows: the first part printed (name 3) takes the s");
            var sw = It(C_Item.LONG_SWORD, 1, 0, 1, 1, 0); sw.nameIds[1] = 7; sw.nameIds[2] = 8; sw.unidMask = 0;
            GDVars.creatureList = Ch(); GDVars.creatureList.effectsAddr = new Types.TEffectRec { id = C_Effect.DETECT_MAGIC };
            CharHlp.ItemLabel(sw, true);
            EqS(" Yes  * p8 p7 ", sw.name, "sword: equipped marker, detect-magic star, parts 2 then 1");
            CharHlp.ItemLabel(sw, false);
            EqS("* p8 p7 ", sw.name, "no marker outside inventory listings");
            sw.unidMask = 7; CharHlp.ItemLabel(sw, false);
            EqS("* ", sw.name, "unidentified parts are left out");
            // names are String[40]: every assignment truncates
            for (int i = 0; i < 256; i++) GDConsts.nameParts[i] = new string((char)('a' + i % 26), 18);
            var big = It(1, 0, 0, 1, 1, 200); big.nameIds[1] = 1; big.nameIds[2] = 2; big.nameIds[3] = 3; big.unidMask = 0;
            CharHlp.ItemLabel(big, true);
            Eq(40, big.name.Length, "label truncated to 40");
            EqS(ModelLabel(big, true, true), big.name, "truncating model agrees");
        }
        finally { for (int i = 0; i < 256; i++) GDConsts.nameParts[i] = saved[i]; GDVars.creatureList = null; }
    }
}
