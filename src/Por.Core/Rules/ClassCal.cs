// Source: Talonius pool-of-radiance pinned 7616920a
using System;

namespace Por.Core
{
    public static class ClassCal
    {
        public const short PRIME_STAT_THRESHOLD = 15;

        public static void RecalcBaseSkills(Types.TCharRecord creature)
        {
            sbyte i;
            sbyte k;
            sbyte lvl;
            sbyte slotLvl;
            sbyte tl;
            byte m;

            creature.thac0Base = 0;
            for (i = C_Level.CLERIC; i <= C_Level.MONK; i++)
            {
                lvl = creature.classLevels[i];
                slotLvl = lvl;
                if (slotLvl > 6)
                    slotLvl = 6;
                tl = lvl;
                if (tl < 0)
                    tl = 0;
                else if (tl > 10)
                    tl = 10;

                if (GDConsts.thac0Tbl[i, tl] > creature.thac0Base)
                    creature.thac0Base = GDConsts.thac0Tbl[i, tl];

                if (creature.levelHighest < lvl)
                    unchecked { creature.levelHighest = (sbyte)lvl; }

                switch (i)
                {
                    case C_Level.FIGHTER:
                        if (lvl > 6) creature.attacks = 3;
                        else creature.attacks = 2;
                        break;
                    case C_Level.PALADIN:
                        if (lvl > 6) creature.attacks = 3;
                        break;
                    case C_Level.RANGER:
                        if (lvl > 7) creature.attacks = 3;
                        break;
                    case C_Level.CLERIC:
                        if (lvl > 1)
                        {
                            for (k = 1; k <= 3; k++)
                                creature.clericSpells[k] = GDConsts.dsExperienceSpellsClass[C_Level.CLERIC].slots[slotLvl, k];
                            ClassCal.AwardWisdomBonus(creature);
                            for (m = 1; m <= 56; m++)
                            {
                                if (GDConsts.spellTable[m].castClass == 0 &&
                                    GDConsts.spellTable[m].levelSp >= 1 &&
                                    GDConsts.spellTable[m].levelSp <= 3 &&
                                    creature.clericSpells[GDConsts.spellTable[m].levelSp] > 0)
                                {
                                    creature.spellsKnown[m] = 1;
                                }
                            }
                        }
                        break;
                    case C_Level.MAGE:
                        if (lvl > 1)
                        {
                            for (k = 1; k <= 3; k++)
                                creature.mageSpells[k] = GDConsts.dsExperienceSpellsClass[C_Level.MAGE].slots[slotLvl, k];
                        }
                        break;
                }
            }
            ClassCal.RecalcSaves(creature);
            if (creature.classLevels[C_Level.THIEF] > 0)
                ClassCal.RecalcThiefSkills(creature);
        }

        public static void AwardWisdomBonus(Types.TCharRecord creature)
        {
            byte w;
            byte wis;
            byte lvl;

            wis = creature.wisCur;
            if (wis > 25)
                wis = 25;
            for (w = 13; w <= wis; w++)
            {
                for (lvl = 1; lvl <= Types.CASTER_LEVEL_LAST; lvl++)
                {
                    if (creature.clericSpells[lvl] > 0)
                    {
                        unchecked
                        {
                            creature.clericSpells[lvl] = (byte)(creature.clericSpells[lvl] + GDConsts.wisBonusSpells[w, lvl]);
                        }
                    }
                }
            }
        }

        public static void RecalcSaves(Types.TCharRecord creature)
        {
            sbyte i;
            sbyte k;
            sbyte lvl;

            for (i = 0; i <= 4; i++)
            {
                creature.saves[i] = 20;
                for (k = C_Level.CLERIC; k <= C_Level.MONK; k++)
                {
                    if (creature.classLevels[k] > 0)
                    {
                        lvl = creature.classLevels[k];
                        if (lvl > 9)
                            lvl = 9;
                        if (creature.saves[i] > GDConsts.saveTbl[k, lvl].values[i])
                            creature.saves[i] = GDConsts.saveTbl[k, lvl].values[i];
                    }
                }
            }
        }

        public static void RecalcThiefSkills(Types.TCharRecord creature)
        {
            byte j;
            int total;
            byte dex;
            sbyte r;
            sbyte tl;

            dex = creature.dexCur;
            if (dex < 9)
                dex = 9;
            else if (dex > 25)
                dex = 25;

            r = creature.race;
            if (r < C_Race.DWARF || r > C_Race.HUMAN)
                r = C_Race.HUMAN;

            tl = creature.classLevels[C_Level.THIEF];
            if (tl < 1)
                return;
            if (tl > 9)
                tl = 9;

            for (j = 1; j <= 8; j++)
            {
                if (GDConsts.raceAdj[r].values[j] < 0 &&
                    Math.Abs((int)GDConsts.raceAdj[r].values[j]) > GDConsts.thiefBase[tl].values[j])
                {
                    creature.thiefSkills[j] = 0;
                }
                else
                {
                    total = GDConsts.thiefBase[tl].values[j] + GDConsts.raceAdj[r].values[j];
                    if (j < 6)
                        total = total + GDConsts.dexAdj[dex].values[j];
                    if (total < 0)
                        total = 0;
                    unchecked { creature.thiefSkills[j] = (byte)total; }
                }
            }
        }

        public static byte RaceAbilityMin(sbyte race, sbyte gender, sbyte ability)
        {
            switch (ability)
            {
                case C_Abil.STRENGTH:
                    return GDConsts.dsMinimumMaximumStatistics[race].strMin[gender];
                case C_Abil.INTELLIGENCE:
                    return GDConsts.dsMinimumMaximumStatistics[race].intMin;
                case C_Abil.WISDOM:
                    return GDConsts.dsMinimumMaximumStatistics[race].wisMin;
                case C_Abil.DEXTERITY:
                    return GDConsts.dsMinimumMaximumStatistics[race].dexMin;
                case C_Abil.CONSTITUTION:
                    return GDConsts.dsMinimumMaximumStatistics[race].conMin;
                default:
                    return GDConsts.dsMinimumMaximumStatistics[race].chaMin;
            }
        }

        public static byte RaceAbilityMax(sbyte race, sbyte gender, sbyte ability)
        {
            switch (ability)
            {
                case C_Abil.STRENGTH:
                    return GDConsts.dsMinimumMaximumStatistics[race].strMax[gender];
                case C_Abil.INTELLIGENCE:
                    return GDConsts.dsMinimumMaximumStatistics[race].intMax;
                case C_Abil.WISDOM:
                    return GDConsts.dsMinimumMaximumStatistics[race].wisMax;
                case C_Abil.DEXTERITY:
                    return GDConsts.dsMinimumMaximumStatistics[race].dexMax;
                case C_Abil.CONSTITUTION:
                    return GDConsts.dsMinimumMaximumStatistics[race].conMax;
                default:
                    return GDConsts.dsMinimumMaximumStatistics[race].chaMax;
            }
        }

        public static sbyte ConHpPerDie(sbyte cls, byte con)
        {
            sbyte share;

            if (con < 3)
                con = 3;
            else if (con > 25)
                con = 25;

            share = (sbyte)GDConsts.conBonusTbl[con];

            if (cls == C_Level.FIGHTER || cls == C_Level.PALADIN || cls == C_Level.RANGER)
            {
                switch (con)
                {
                    case 17:
                        unchecked { share++; }
                        break;
                    case 18:
                        unchecked { share += 2; }
                        break;
                    case 19:
                    case 20:
                        unchecked { share += 3; }
                        break;
                    case 21:
                    case 22:
                    case 23:
                        unchecked { share += 4; }
                        break;
                    case 24:
                    case 25:
                        unchecked { share += 5; }
                        break;
                }
            }
            return share;
        }

        public static sbyte ConHpBonus(Types.TCharRecord creature, byte mask)
        {
            sbyte bonus;
            sbyte cls;
            sbyte share;

            bonus = 0;
            for (cls = C_Level.CLERIC; cls <= C_Level.MONK; cls++)
            {
                if (creature.classLevels[cls] > 0 && (GDConsts.classBit[cls] & mask) > 0)
                {
                    share = ClassCal.ConHpPerDie(cls, creature.stats[C_Abil.CONSTITUTION]);
                    if (cls == C_Level.RANGER && creature.classLevels[cls] == 1)
                        unchecked { share = (sbyte)(share * 2); }
                    unchecked { bonus += share; }
                }
            }
            return bonus;
        }

public static byte DivideAmongClasses(byte sum, sbyte conB, byte clsCount)
{
    if (conB < 0)
    {
        if (sum > Math.Abs((int)conB) + clsCount)
            return unchecked((byte)((sum + conB) / clsCount));
        else
            return 1;
    }
    else
        return unchecked((byte)((sum + conB) / clsCount));
}

        public static byte HpMinimum(Types.TCharRecord creature)
        {
            byte clsCount;
            byte sum;
            sbyte cls;

            clsCount = 0;
            sum = 0;
            for (cls = C_Level.CLERIC; cls <= C_Level.MONK; cls++)
            {
                if (creature.classLevels[cls] > 0)
                {
                    unchecked { sum += (byte)creature.classLevels[cls]; }
                    unchecked { clsCount++; }
                    switch (cls)
                    {
                        case C_Level.RANGER:
                            unchecked { sum++; }
                            break;
                        case C_Level.MONK:
                            unchecked { sum++; }
                            break;
                    }
                }
            }
            return ClassCal.DivideAmongClasses(sum, ClassCal.ConHpBonus(creature, 0xFF), clsCount);
        }

        public static byte HpMaximum(Types.TCharRecord creature)
        {
            byte clsCount;
            byte sum;
            sbyte cls;

            clsCount = 0;
            sum = 0;
            for (cls = C_Level.CLERIC; cls <= C_Level.MONK; cls++)
            {
                if (creature.classLevels[cls] > 0)
                {
                    unchecked { clsCount++; }
                    unchecked { sum += (byte)((creature.classLevels[cls] + GDConsts.hpDice[cls] - 1) * GDConsts.hpSides[cls]); }
                }
            }
            return ClassCal.DivideAmongClasses(sum, ClassCal.ConHpBonus(creature, 0xFF), clsCount);
        }

        public static byte HitDieFloor(byte con)
        {
            switch (con)
            {
                case 19:
                case 20:
                    return 2;
                case 21:
                case 22:
                    return 3;
                default:
                    if (con >= 23)
                        return 4;
                    return 1;
            }
        }

        public static byte RollFirstLevelHp(Types.TCharRecord creature, byte mask)
        {
            byte roll;
            byte die;
            byte floor;
            byte sum;
            sbyte cls;
            byte dieCount;
            byte i;

            sum = 0;
            floor = ClassCal.HitDieFloor(creature.stats[C_Abil.CONSTITUTION]);
            for (cls = C_Level.CLERIC; cls <= C_Level.MONK; cls++)
            {
                if (creature.classLevels[cls] > 0 && (GDConsts.classBit[cls] & mask) > 0)
                {
                    dieCount = GDConsts.hpDice[cls];
                    if (creature.classLevels[cls] > 1)
                        dieCount = 1;
                    roll = 0;
                    for (i = 1; i <= dieCount; i++)
                    {
                        die = Dice.RollDice(1, GDConsts.hpSides[cls]);
                        if (die < floor)
                            die = floor;
                        unchecked { roll += die; }
                    }
                    if (creature.classLevels[cls] == 1)
                    {
                        if (roll < GDConsts.hpSides[cls] * 2 / 3)
                            roll = (byte)(GDConsts.hpSides[cls] * 2 / 3);
                    }
                    unchecked { sum += roll; }
                }
            }
            return sum;
        }

        public static short ConHpTotal(Types.TCharRecord creature, byte con)
        {
            short total;
            byte clsCount;
            sbyte cls;
            byte dice;

            total = 0;
            clsCount = 0;
            for (cls = C_Level.CLERIC; cls <= C_Level.MONK; cls++)
            {
                if (creature.classLevels[cls] > 0)
                {
                    unchecked { clsCount++; }
                    unchecked { dice = (byte)creature.classLevels[cls]; }
                    if (cls == C_Level.RANGER)
                        unchecked { dice++; }
                    unchecked { total += (short)(dice * ClassCal.ConHpPerDie(cls, con)); }
                }
            }
            if (clsCount > 0)
                unchecked { total = (short)(total / clsCount); }
            return total;
        }

        public static int ExperienceShare(Types.TCharRecord creature, int experience)
        {
            int share;

            unchecked
            {
                share = experience;
                switch (creature.charClass)
                {
                    case C_Class.CLERIC:
                        if (creature.wisCur > PRIME_STAT_THRESHOLD)
                            share = experience / 10 + experience;
                        break;
                    case C_Class.FIGHTER:
                        if (creature.strCur > PRIME_STAT_THRESHOLD)
                            share = experience / 10 + experience;
                        break;
                    case C_Class.PALADIN:
                        if (creature.strCur > PRIME_STAT_THRESHOLD && creature.wisCur > PRIME_STAT_THRESHOLD)
                            share = experience / 10 + experience;
                        break;
                    case C_Class.RANGER:
                        if (creature.strCur > PRIME_STAT_THRESHOLD && creature.intCur > PRIME_STAT_THRESHOLD && creature.wisCur > PRIME_STAT_THRESHOLD)
                            share = experience / 10 + experience;
                        break;
                    case C_Class.MAGE:
                        if (creature.intCur > PRIME_STAT_THRESHOLD)
                            share = experience / 10 + experience;
                        break;
                    case C_Class.THIEF:
                        if (creature.dexCur > PRIME_STAT_THRESHOLD)
                            share = experience / 10 + experience;
                        break;
                    case C_Class.CLERIC_FIGHTER:
                    case C_Class.CLERIC_RANGER:
                    case C_Class.FIGHTER_MAGE:
                    case C_Class.FIGHTER_THIEF:
                    case C_Class.MAGE_THIEF:
                        share = experience / 2;
                        break;
                    case C_Class.CLERIC_FIGHTER_MAGE:
                    case C_Class.FIGHTER_MAGE_THIEF:
                        share = experience / 3;
                        break;
                }
            }
            return share;
        }
    }
}
