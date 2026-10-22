// Source: Talonius pool-of-radiance pinned 7616920a (src/charhlp.pas); Qwen3.8:27b initial draft, supervisor repaired.
using System;


namespace Por.Core
{
    public static class CharHlp
    {
        public delegate void TFaultHook(string msg);
        public static TFaultHook faultHook;

        private const int LIGHT_ARMOR = 150;
        private const int MEDIUM_ARMOR = 399;
        private const int MAGIC_ARMOR_MOVE = 3;
        private const int MOVE_LOADED = 9;
        private const int MOVE_HEAVY = 6;
        private const int MOVE_OVERLOADED = 3;
        private const int OVER_LIGHT = 512;
        private const int OVER_LOADED = 768;
        private const int OVER_HEAVY = 1024;
        private const int BAG_OF_HOLDING_COINS = 5000;
        private const int MAX_ENCUMBRANCE = 65535;
        private const int HEAVY_LOOT = 255;
        private const int HEAVY_LOOT_PENALTY = 90;
        private const int MEDIUM_LOOT = 24;
        private const int MEDIUM_LOOT_PENALTY = 50;
        private const int STR_BAND_LOW = 0;
        private const int STR_BAND_17 = 1;
        private const int STR_BAND_18 = 2;
        private static readonly sbyte[,] FIGHTER_CAP = new sbyte[5, 3] {
            { 7, 8, 0 },
            { 5, 6, 7 },
            { 5, 5, 6 },
            { 6, 7, 8 },
            { 4, 5, 6 }
        };
        private const int HALFELF_CLERIC_CAP = 5;

        private static ushort gEquipWeight;

        public static void WeaponAdjust(Types.TCharRecord creature)
        {
            Types.TItemRec w = creature.equipped[C_Equip.WEAPON];
            if (w == null) return;
            byte wtype = w.itemType;
            creature.thac0Cur = creature.thac0Base;
            if ((BaseItem(wtype).props & C_Item.PROP_DEX_MISSILE) != 0)
                creature.thac0Cur = unchecked((byte)(AbilAdj.DexMissileAdjust(creature) + creature.thac0Cur));
            creature.curModA[1] = BaseItem(wtype).dmgSmMod;
            if ((BaseItem(wtype).props & C_Item.PROP_STR_BONUS) != 0)
            {
                creature.thac0Cur = unchecked((byte)(AbilAdj.StrToHitAdjust(creature) + creature.thac0Cur));
                creature.curModA[1] = unchecked((sbyte)(AbilAdj.StrDamageAdjust(creature) + creature.curModA[1]));
            }
            sbyte plus = w.plus;
            if (BaseItem(wtype).props > 127)
                if (creature.equipped[C_Equip.BOLT] != null)
                    plus = unchecked((sbyte)(plus + creature.equipped[C_Equip.BOLT].plus));
            if ((BaseItem(wtype).props & C_Item.PROP_ARROWS) > 0)
                if (creature.equipped[C_Equip.ARROW] != null)
                    plus = unchecked((sbyte)(plus + creature.equipped[C_Equip.ARROW].plus));
            creature.curModA[1] = unchecked((sbyte)(creature.curModA[1] + plus));
            if (creature.race == C_Race.ELF &&
                ((w.itemType >= C_Item.COMPOSITE_LONG_BOW && w.itemType <= C_Item.SHORT_BOW) || w.itemType == C_Item.LONG_SWORD || w.itemType == C_Item.SHORT_SWORD ||
                 (w.itemType == C_Item.HEAVY_CROSSBOW && w.nameIds[2] >= C_Item.NAME_COMPOSITE_LONG_BOW && w.nameIds[2] <= C_Item.NAME_SHORT_BOW)))
                plus = unchecked((sbyte)(plus + 1));
            creature.thac0Cur = unchecked((byte)(creature.thac0Cur + plus));
            creature.curRollsA[1] = BaseItem(wtype).dmgSmRolls;
            creature.curDiceA[1] = BaseItem(wtype).dmgSmDice;
        }

        public static void ArmorMoveAdjust(Types.TCharRecord creature, Types.TItemRec item)
        {
            if (BaseItem(item.itemType).slot == C_Equip.ARMOR)
            {
                if (item.weight <= LIGHT_ARMOR)
                    creature.movementCur = creature.movementBase;
                else if (item.weight <= MEDIUM_ARMOR)
                    creature.movementCur = MOVE_LOADED;
                else
                    creature.movementCur = MOVE_HEAVY;
                if (item.plus != 0 && creature.movementCur <= MOVE_LOADED)
                    creature.movementCur = unchecked((byte)(creature.movementCur + MAGIC_ARMOR_MOVE));
            }
        }

        public static void ArmorAdjustHelper(Types.TCharRecord creature, Types.TItemRec item, Types.TAcAdjust adj, ref bool isMagicBody, ref sbyte ringSave)
        {
            byte descr = BaseItem(item.itemType).acDescr;
            if (descr > 127)
            {
                descr = unchecked((byte)(descr & 127));
                byte slot = BaseItem(item.itemType).slot;
                if (slot == C_Equip.SHIELD)
                    adj[Types.AC_SHIELD] = unchecked((sbyte)(descr + item.plus));
                else if (descr == 0)
                {
                    if (slot == C_Equip.RING1)
                    {
                        if (item.plus > adj[Types.AC_RING])
                            adj[Types.AC_RING] = item.plus;
                        if (item.plus > ringSave)
                            ringSave = item.plus;
                        if (item.saveBonus > ringSave)
                            ringSave = item.saveBonus;
                    }
                    else
                    {
                        adj[Types.AC_PLUSES] = unchecked((sbyte)(adj[Types.AC_PLUSES] + item.plus));
                        creature.saveBonus = unchecked((sbyte)(creature.saveBonus + item.saveBonus));
                    }
                }
                else
                {
                    if (descr + item.plus > adj[Types.AC_ARMOR])
                    {
                        adj[Types.AC_ARMOR] = unchecked((sbyte)(descr + item.plus));
                        if (item.plus > 0 && slot == C_Equip.ARMOR)
                            isMagicBody = true;
                    }
                }
            }
        }

        public static void EncumbranceAdjust(Types.TCharRecord creature)
        {
            int over = unchecked((int)creature.encumbrance - AbilAdj.WeightAllowance(creature));
            if (over < 0) over = 0;
            byte movement;
            if (over <= OVER_LIGHT)
                movement = creature.movementCur;
            else if (over <= OVER_LOADED)
                movement = MOVE_LOADED;
            else if (over <= OVER_HEAVY)
                movement = MOVE_HEAVY;
            else
                movement = MOVE_OVERLOADED;
            if (movement < creature.movementCur)
                creature.movementCur = movement;
        }

        public static void ItemLabel(Types.TItemRec item, bool isInv)
        {
            item.name = "";
            if (isInv)
            {
                if (item.equipped != 0)
                    item.name = " Yes  ";
                else
                    item.name = " No   ";
            }
            bool found = false;
            Types.TCharRecord p = GDVars.creatureList;
            while (p != null && !found)
            {
                Types.TEffectRec effect;
                if (HasEffect(p, C_Effect.DETECT_MAGIC, out effect))
                    found = true;
                p = p.nextCharAddr;
            }
            if (found)
                if (item.plus > 0 || item.saveBonus > 0 || item.cursed != 0)
                    item.name = (item.name + "* ").Substring(0, Math.Min(40, (item.name + "* ").Length));
            if (item.quantity > 0)
                item.name = (item.name + StrUtil.NumStr(item.quantity) + " ").Substring(0, Math.Min(40, (item.name + StrUtil.NumStr(item.quantity) + " ").Length));
            byte mask = 0;
            for (int i = 1; i <= 3; i++)
                if (item.nameIds[i] != 0)
                    if (((item.unidMask >> (3 - i)) & 1) == 0)
                        mask = unchecked((byte)(mask + (1 << (i - 1))));
            bool appended = false;
            for (int i = 3; i >= 1; i--)
                if (((mask >> (i - 1)) & 1) > 0)
                {
                    item.name = (item.name + GDConsts.nameParts[item.nameIds[i]]).Substring(0, Math.Min(40, (item.name + GDConsts.nameParts[item.nameIds[i]]).Length));
                    if (item.quantity < 2 || appended)
                        item.name = (item.name + " ").Substring(0, Math.Min(40, (item.name + " ").Length));
                    else if ((1 << (i - 1)) == mask || (i == 1 && mask > 4 && item.itemType != C_Item.FLASK_OF_OIL) ||
                             (i == 2 && (mask & 1) == 0) || (i == 3 && item.itemType == C_Item.FLASK_OF_OIL) ||
                             ((item.itemType == C_Item.ARROW || item.itemType == C_Item.QUARREL) && item.nameIds[3] != C_Item.NAME_SILVER))
                    {
                        item.name = (item.name + "s ").Substring(0, Math.Min(40, (item.name + "s ").Length));
                        appended = true;
                    }
                    else
                        item.name = (item.name + " ").Substring(0, Math.Min(40, (item.name + " ").Length));
                }
        }

        public static bool IsHelpless(Types.TCharRecord creature)
        {
            bool result = false;
            for (int i = 1; i <= GDConsts.HELPLESS_FX_COUNT; i++)
            {
                Types.TEffectRec effect;
                if (HasEffect(creature, GDConsts.helplessFx[i], out effect))
                    result = true;
            }
            return result;
        }

        public static void LinkEquipped(Types.TCharRecord creature)
        {
            for (int i = C_Equip.WEAPON; i <= C_Equip.BOLT; i++)
                creature.equipped[i] = null;
            Types.TItemRec item = creature.itemsAddr;
            while (item != null)
            {
                if (item.equipped != 0)
                {
                    int slot = BaseItem(item.itemType).slot;
                    if (slot >= C_Equip.WEAPON && slot <= C_Equip.BOOTS)
                        creature.equipped[slot] = item;
                    else if (slot == C_Equip.RING1)
                    {
                        if (creature.equipped[C_Equip.RING1] != null)
                        {
                            if (creature.equipped[C_Equip.RING2] == null)
                                creature.equipped[C_Equip.RING2] = item;
                        }
                        else
                            creature.equipped[C_Equip.RING1] = item;
                    }
                    if (item.itemType == C_Item.ARROW)
                        creature.equipped[C_Equip.ARROW] = item;
                    if (item.itemType == C_Item.QUARREL)
                        creature.equipped[C_Equip.BOLT] = item;
                }
                item = item.next;
            }
        }

        internal static void CopyItem(Types.TItemRec src, Types.TItemRec dst)
        {
            dst.name = src.name;
            dst.inUse = src.inUse;
            dst.next = src.next;
            dst.itemType = src.itemType;
            for (int i = 0; i < 4; i++) dst.nameIds[i] = src.nameIds[i];
            dst.plus = src.plus;
            dst.saveBonus = src.saveBonus;
            dst.equipped = src.equipped;
            dst.unidMask = src.unidMask;
            dst.cursed = src.cursed;
            dst.weight = src.weight;
            dst.quantity = src.quantity;
            dst.value = src.value;
            for (int i = 0; i < 3; i++) dst.variantData[i] = src.variantData[i];
        }

        public static void RecalcStats(Types.TCharRecord creature)
        {
            LinkEquipped(creature);
            bool bag = false;
            creature.numberOfItems = 0;
            Types.TItemRec item = creature.itemsAddr;
            creature.handsEquipped = 0;
            int raw = 0;
            gEquipWeight = 0;
            while (item != null)
            {
                creature.numberOfItems = unchecked((byte)(creature.numberOfItems + 1));
                int w = ItemWeight(item);
                raw += w;
                if (item.equipped != 0)
                {
                    gEquipWeight = unchecked((ushort)(gEquipWeight + w));
                    if (item.nameIds[1] == C_Item.NAME_HOLDING)
                        bag = true;
                    creature.handsEquipped = unchecked((byte)(creature.handsEquipped + BaseItem(item.itemType).hands));
                }
                item = item.next;
            }
            for (int c = C_Money.COPPER; c <= C_Money.JEWELRY; c++)
                raw += creature.coins[c];
            for (int j = 1; j <= 2; j++)
            {
                creature.curRollsA[j] = creature.unarmedRollsA[j];
                creature.curDiceA[j] = creature.unarmedDiceA[j];
                creature.curModA[j] = creature.unarmedModA[j];
            }
            Types.TAcAdjust adj = new Types.TAcAdjust();
            for (int k = Types.AC_DEX; k <= Types.AC_ARMOR; k++)
                adj[k] = 0;
            bool magicBody = false;
            creature.saveBonus = 0;
            sbyte ringSave = 0;
            creature.acCur = creature.acBase;
            creature.movementCur = creature.movementBase;
            creature.thac0Cur = creature.thac0Base;
            adj[Types.AC_DEX] = AbilAdj.DexterityArmorAdjustment(creature);
            if (creature.equipped[C_Equip.WEAPON] == null)
            {
                creature.thac0Cur = unchecked((byte)(AbilAdj.StrToHitAdjust(creature) + creature.thac0Cur));
                creature.curModA[1] = unchecked((sbyte)(AbilAdj.StrDamageAdjust(creature) + creature.curModA[1]));
            }
            WeaponAdjust(creature);
            item = creature.itemsAddr;
            while (item != null)
            {
                if (item.equipped != 0)
                {
                    ArmorMoveAdjust(creature, item);
                    ArmorAdjustHelper(creature, item, adj, ref magicBody, ref ringSave);
                }
                item = item.next;
            }
            creature.saveBonus = unchecked((sbyte)(creature.saveBonus + ringSave));
            if (magicBody)
                adj[Types.AC_RING] = 0;
            if (bag)
            {
                if (raw < BAG_OF_HOLDING_COINS)
                    raw = 0;
                else
                    raw -= BAG_OF_HOLDING_COINS;
                if (raw < gEquipWeight)
                    raw = gEquipWeight;
            }
            if (raw > MAX_ENCUMBRANCE)
                raw = MAX_ENCUMBRANCE;
            creature.encumbrance = unchecked((ushort)raw);
            EncumbranceAdjust(creature);
            if (adj[Types.AC_ARMOR] < creature.acCur)
                adj[Types.AC_ARMOR] = unchecked((sbyte)creature.acCur);
            creature.acCur = 0;
            for (int k = Types.AC_DEX; k <= Types.AC_ARMOR; k++)
                creature.acCur = unchecked((byte)(creature.acCur + adj[k]));
            creature.acBehind = unchecked((byte)(adj[Types.AC_ARMOR] + adj[Types.AC_PLUSES] + adj[Types.AC_RING] - 1 - 1));
            if ((WarriorLevel(creature) > 0) && (creature.race > C_Race.MONSTER))
                creature.attackLevel = unchecked((byte)WarriorLevel(creature));
            else
                creature.attackLevel = 1;
        }

        public static bool HasBagOfHolding(Types.TCharRecord creature)
        {
            bool result = false;
            Types.TItemRec item = creature.itemsAddr;
            while (item != null)
            {
                if ((item.equipped != 0) && (item.nameIds[1] == C_Item.NAME_HOLDING))
                    result = true;
                item = item.next;
            }
            return result;
        }

        public static int RawWeight(Types.TCharRecord creature)
        {
            int w = 0;
            Types.TItemRec item = creature.itemsAddr;
            while (item != null)
            {
                w += ItemWeight(item);
                item = item.next;
            }
            for (int c = C_Money.COPPER; c <= C_Money.JEWELRY; c++)
                w += creature.coins[c];
            return w;
        }

        public static int ItemWeight(Types.TItemRec item)
        {
            if (item.quantity > 0)
                return item.quantity * item.weight;
            else
                return item.weight;
        }

        public static void RemoveSpell(Types.TCharRecord creature, byte spell)
        {
            int i = 0;
            while (i <= Types.SPELL_SLOT_LAST)
            {
                if (creature.memorizedSpells[i] == spell)
                {
                    creature.memorizedSpells[i] = 0;
                    i = Types.SPELL_SLOT_LAST + 1;
                }
                i++;
            }
        }

        public static void FreeItemList(ref Types.TItemRec list)
        {
            while (list != null)
            {
                Types.TItemRec next = list.next;
                list = next;
            }
        }

        public static void PrependItemCopy(ref Types.TItemRec list, Types.TItemRec item)
        {
            Types.TItemRec n = new Types.TItemRec();
            CopyItem(item, n);
            n.next = list;
            list = n;
        }

        public static void PrependEffectCopy(ref Types.TEffectRec list, Types.TEffectRec effect)
        {
            Types.TEffectRec n = new Types.TEffectRec();
            n.id = effect.id;
            n.duration = effect.duration;
            n.strength = effect.strength;
            n.onRemove = effect.onRemove;
            n.next = list;
            list = n;
        }

        public static void ClearTreasure()
        {
            FreeItemList(ref GDVars.treasureItems);
            for (int i = 0; i < 7; i++)
                GDVars.treasureMoney.Data[i] = 0;
        }

        private static void Fault(string msg)
        {
            if (faultHook != null)
                faultHook(msg);
        }

        public static void RemoveItem(Types.TCharRecord creature, Types.TItemRec item)
        {
            if (item == null)
                Fault("Nil Item pointer...");
            else
            {
                Types.TItemRec p = creature.itemsAddr;
                if (p == item)
                    creature.itemsAddr = item.next;
                else
                    while ((p != null) && (p.next != item))
                        p = p.next;
                if (p == null)
                    Fault("Tried to Lose item & couldn't find it!");
                else
                {
                    p.next = item.next;
                    item.next = null;
                }
            }
        }

        public static void AppendItemCopy(ref Types.TItemRec list, Types.TItemRec item)
        {
            Types.TItemRec n = new Types.TItemRec();
            CopyItem(item, n);
            n.next = null;
            Types.TItemRec p = list;
            if (p == null)
                list = n;
            else
            {
                while (p.next != null)
                    p = p.next;
                p.next = n;
            }
        }

        public static bool HasEffect(Types.TCharRecord creature, byte id, out Types.TEffectRec effect)
        {
            bool found = false;
            effect = creature.effectsAddr;
            while ((effect != null) && !found)
            {
                if (effect.id == id)
                    found = true;
                else
                    effect = effect.next;
            }
            return found;
        }

        public static void InflictDamage(Types.TCharRecord creature, byte dmg)
        {
            if (creature.status == C_Status.DEAD || creature.status == C_Status.STONED || creature.status == C_Status.GONE)
                return;
            bool wasUp = (creature.status == C_Status.OKAY || creature.status == C_Status.ANIMATED);
            byte hpBefore = creature.hpCur;
            byte over = 0;
            byte remain = 0;
            if (creature.hpCur >= dmg)
                remain = unchecked((byte)(creature.hpCur - dmg));
            else
                over = unchecked((byte)(dmg - creature.hpCur));
            if ((over > 9) || ((remain == 0) && (creature.status == C_Status.ANIMATED)))
                creature.status = C_Status.DEAD;
            else if (over > 0)
            {
                creature.status = C_Status.DYING;
                if (GDVars.ifcState == C_Ifc.COMBAT)
                    creature.combatAddr.overkill = over;
            }
            else if (remain == 0)
                creature.status = C_Status.UNCONSCIOUS;
            if (!(creature.status == C_Status.OKAY || creature.status == C_Status.ANIMATED))
            {
                creature.enabled = 0;
                creature.hpCur = 0;
                if (GDVars.ifcState == C_Ifc.COMBAT)
                {
                    if (wasUp)
                        GDVars.teamCount[creature.hostile] = unchecked((byte)(GDVars.teamCount[creature.hostile] - 1));
                    creature.combatAddr.initiative = 0;
                }
            }
            else
                creature.hpCur = remain;
            Stats.Damaged(GDVars.curChar, creature, unchecked((byte)(hpBefore - creature.hpCur)), wasUp);
        }

        public static sbyte EnemyTeam(Types.TCharRecord creature)
        {
            if (creature.hostile == 0)
                return 1;
            else
                return 0;
        }

        public static void CountTeams()
        {
            GDVars.teamCount[0] = 0;
            GDVars.teamCount[1] = 0;
            Types.TCharRecord p = GDVars.creatureList;
            while (p != null)
            {
                if (p.enabled != 0)
                    GDVars.teamCount[p.hostile] = unchecked((byte)(GDVars.teamCount[p.hostile] + 1));
                p = p.nextCharAddr;
            }
        }

        public static bool EndActions(Types.TCharRecord creature)
        {
            creature.combatAddr.initiative = 0;
            creature.combatAddr.spellCast = 0;
            creature.combatAddr.guarding = 0;
            creature.combatAddr.movement = 0;
            return true;
        }

        public static bool CanUseItems(Types.TCharRecord creature)
        {
            return (creature.enabled != 0) && (GDVars.zone0.antimagic == 0) &&
                   ((GDVars.ifcState == C_Ifc.CAMPING) || (GDVars.ifcState == C_Ifc.OUTDOORS) ||
                    (GDVars.ifcState == C_Ifc.INDOORS) ||
                    ((GDVars.ifcState == C_Ifc.COMBAT) && (creature.combatAddr.itemUseAllowed != 0)));
        }

        public static bool Guard(Types.TCharRecord creature)
        {
            bool result = EndActions(creature);
            creature.combatAddr.guarding = 1;
            return result;
        }

        public static byte GetCasterLevel(byte spell)
        {
            byte result;
            switch (GDConsts.spellTable[spell].castClass)
            {
                case C_Spell.CLASS_CLERIC:
                    result = unchecked((byte)GDVars.curChar.classLevels[C_Level.CLERIC]);
                    break;
                case C_Spell.CLASS_MAGE:
                    result = unchecked((byte)GDVars.curChar.classLevels[C_Level.MAGE]);
                    break;
                case C_Spell.CLASS_SPECIAL:
                    result = C_Spell.SPECIAL_CASTER_LEVEL;
                    break;
                default:
                    result = 0;
                    break;
            }
            if (GDVars.usingItem && (GDConsts.spellTable[spell].castClass != C_Spell.CLASS_SPECIAL))
                result = C_Spell.ITEM_CASTER_LEVEL;
            return result;
        }

        public static bool HasRangedWeapon(Types.TCharRecord creature)
        {
            if ((creature.equipped[C_Equip.WEAPON] != null) &&
                (BaseItem(creature.equipped[C_Equip.WEAPON].itemType).range > 1))
                return true;
            else
                return false;
        }

        public static bool HasThrowableWeapon(Types.TCharRecord creature)
        {
            if (HasRangedWeapon(creature) &&
                ((BaseItem(creature.equipped[C_Equip.WEAPON].itemType).props & C_Item.PROPS_THROWABLE) == C_Item.PROPS_THROWABLE))
                return true;
            else
                return false;
        }

        public static bool FindDamagingRangedWeapon(Types.TCharRecord creature, ref Types.TItemRec sel)
        {
            bool result = false;
            Types.TItemRec w;
            byte props;
            w = creature.equipped[C_Equip.WEAPON];
            sel = null;
            props = 0;
            if (w != null)
            {
                props = BaseItem(w.itemType).props;
                if ((props & C_Item.PROP_THROWN) != 0)
                    sel = w;
                if ((props & C_Item.PROP_FIRES_AMMO) != 0)
                {
                    if ((props & C_Item.PROP_ARROWS) != 0)
                        sel = creature.equipped[C_Equip.ARROW];
                    if ((props & C_Item.PROP_QUARRELS) != 0)
                        sel = creature.equipped[C_Equip.BOLT];
                }
            }
            if ((sel != null) || (props == C_Item.PROP_DEX_MISSILE + C_Item.PROP_FIRES_AMMO))
                result = true;
            return result;
        }

        public static bool CanTrain(Types.TCharRecord creature)
        {
            bool result = false;
            sbyte cls;
            sbyte lvl;
            for (cls = C_Level.CLERIC; cls <= C_Level.MONK; cls++)
            {
                lvl = creature.classLevels[cls];
                if ((lvl > 0) && (lvl < C_Level.LEVEL_LAST))
                    if (!AtLevelCap(creature, cls))
                        if (GDConsts.dsExperienceSpellsClass[cls].xp[lvl + 1] > 0)
                            if (GDConsts.dsExperienceSpellsClass[cls].xp[lvl + 1] <= creature.experience)
                                result = true;
            }
            return result;
        }

        public static bool AtLevelCap(Types.TCharRecord creature, sbyte cls)
        {
            bool result = false;
            sbyte lvl;
            byte band;
            lvl = creature.classLevels[cls];
            if (lvl <= 0)
                return result;
            if (creature.strCur < 17)
                band = STR_BAND_LOW;
            else if (creature.strCur == 17)
                band = STR_BAND_17;
            else
                band = STR_BAND_18;
            if ((creature.race == C_Race.HALFELF) && (cls == C_Level.CLERIC))
                result = lvl >= HALFELF_CLERIC_CAP;
            else if ((creature.race >= C_Race.DWARF && creature.race <= C_Race.HALFLING) &&
                    ((cls == C_Level.FIGHTER) || ((cls == C_Level.RANGER) && (creature.race == C_Race.HALFELF))))
                result = (FIGHTER_CAP[creature.race - C_Race.DWARF, band] != 0) && (lvl >= FIGHTER_CAP[creature.race - C_Race.DWARF, band]);
            return result;
        }

        public static sbyte WarriorLevel(Types.TCharRecord creature)
        {
            sbyte best;
            best = creature.classLevels[C_Level.FIGHTER];
            if (creature.classLevels[C_Level.PALADIN] > best)
                best = creature.classLevels[C_Level.PALADIN];
            if (creature.classLevels[C_Level.RANGER] > best)
                best = creature.classLevels[C_Level.RANGER];
            if (best < 0)
                best = 0;
            return best;
        }

        public static Types.TBaseItemRec BaseItem(byte itemType)
        {
            if (itemType > Types.BASE_ITEM_LAST)
                return GDVars.baseItems[Types.BASE_ITEM_OVERFLOW];
            else
                return GDVars.baseItems[itemType];
        }

        public static bool IsNpc(Types.TCharRecord creature)
        {
            return creature.npc >= C_Npc.FLAG;
        }

        public static bool MayTrade(Types.TCharRecord creature)
        {
            return !IsNpc(creature) || (creature.enabled == 0) || (creature.status == C_Status.ANIMATED);
        }

        public static bool ComesUnderPlayer(Types.TCharRecord creature)
        {
            return !IsNpc(creature) && (creature.status != C_Status.ANIMATED);
        }

        public static bool ItemReadable(object it)
        {
            if ((it != null) &&
               (BaseItem(((Types.TItemRec)it).itemType).slot >= C_Item.SLOT_MAGE_SCROLL && BaseItem(((Types.TItemRec)it).itemType).slot <= C_Item.SLOT_LAST_READABLE))
                return true;
            else
                return false;
        }

        public static bool ScrollLegible(Types.TCharRecord creature, Types.TItemRec item)
        {
            Types.TEffectRec effect;
            if (HasEffect(creature, C_Effect.READ_MAGIC, out effect) ||
                ((creature.classLevels[C_Level.CLERIC] > 0) &&
                 (BaseItem(item.itemType).slot == C_Item.SLOT_CLERIC_SCROLL)))
                item.unidMask = 0;
            return item.unidMask == 0;
        }

        public static byte ScrollSpellAt(Types.TItemRec item, byte slot)
        {
            byte result = C_Spell.NONE;
            byte id;
            if ((slot >= 1) && (slot <= 3))
            {
                id = unchecked((byte)(item.spells[slot] & C_Spell.ID_MASK));
                if ((id >= 1) && (id <= C_Spell.LAST))
                    result = id;
            }
            return result;
        }

        public static bool CastableScroll(Types.TCharRecord creature, Types.TItemRec item)
        {
            bool result = false;
            if (ItemReadable(item))
                if ((item.equipped != 0) || (BaseItem(item.itemType).classMask & creature.itemLimits) != 0)
                    result = ScrollLegible(creature, item);
            return result;
        }

        public static bool HasScrollSpells(Types.TCharRecord creature)
        {
            bool found = false;
            Types.TItemRec item;
            byte i;
            if (CanUseItems(creature))
            {
                item = creature.itemsAddr;
                while ((item != null) && !found)
                {
                    if (CastableScroll(creature, item))
                        for (i = 1; i <= 3; i++)
                            if (ScrollSpellAt(item, i) != C_Spell.NONE)
                                found = true;
                    item = item.next;
                }
            }
            return found;
        }

        public static byte CalcSpellRange(byte spell)
        {
            byte r;
            if (GDVars.usingItem == false)
                r = unchecked((byte)(GDConsts.spellTable[spell].rangeFixed + GDConsts.spellTable[spell].rangeVar * GetCasterLevel(spell)));
            else
                r = unchecked((byte)(GDConsts.spellTable[spell].rangeVar * C_Spell.ITEM_CASTER_LEVEL + GDConsts.spellTable[spell].rangeFixed));
            if ((r == 0) && (GDConsts.spellTable[spell].area != 0))
                r = 1;
            if (r == C_Spell.ATTACK_ROLL_RANGE)
                r = 1;
            return r;
        }

        public static byte TheftChance(byte chance, ushort weight)
        {
            byte penalty;
            if (weight > HEAVY_LOOT)
                penalty = HEAVY_LOOT_PENALTY;
            else if (weight > MEDIUM_LOOT)
                penalty = MEDIUM_LOOT_PENALTY;
            else
                penalty = 0;
            if (chance > penalty)
                return unchecked((byte)(chance - penalty));
            else
                return 0;
        }
    }
}
