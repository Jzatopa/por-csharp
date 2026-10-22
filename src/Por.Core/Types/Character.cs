// FILE: src/Por.Core/Types/Character.cs
using System;

namespace Por.Core
{
    public static partial class Types
    {
        public class ByteGridView
        {
            private readonly Func<int, int, byte> _getter;
            private readonly Action<int, int, byte> _setter;

            public ByteGridView(Func<int, int, byte> getter, Action<int, int, byte> setter)
            {
                _getter = getter;
                _setter = setter;
            }

            public byte this[int row, int col]
            {
                get => _getter(row, col);
                set => _setter(row, col, value);
            }
        }

        public class TCharRecord
        {
            // { character name }
            public string name = string.Empty;

            // Case 0 fields
            public byte strCur;
            public byte intCur;
            public byte wisCur;
            public byte dexCur;
            public byte conCur;
            public byte chaCur;
            public byte strExc;
            public TSpellSlots memorizedSpells = new TSpellSlots();
            public byte spellPrepTime;
            public byte thac0Base;
            public sbyte race;
            public sbyte charClass;
            public ushort age;
            public byte hpMax;
            public byte[] spellsKnown = new byte[57]; // 1..56
            public byte attackLevel;
            public byte iconDimensions;
            public byte[] saves = new byte[5]; // C_Save.PARALYZE..C_Save.SPELL (0..4)
            public byte movementBase;
            public sbyte levelHighest;
            public byte drainedLevels;
            public byte drainedHps;
            public sbyte levelUndead;
            public byte[] thiefSkills = new byte[9]; // 1..8
            public TEffectRec effectsAddr;
            public byte unknown083;
            public byte npc;
            public byte npcTreasureShare;
            public byte unknown086;
            public byte unknown087;
            public ushort[] coins = new ushort[7]; // C_Money.COPPER..C_Money.JEWELRY (0..6)
            public sbyte[] classLevels = new sbyte[8]; // C_Level.CLERIC..C_Level.MONK (0..7)
            public byte gender;
            public byte charType;
            public byte alignment;
            public byte attacks;
            public byte attacks2;
            public byte[] unarmedRollsA = new byte[3]; // 1..2
            public byte[] unarmedDiceA = new byte[3]; // 1..2
            public sbyte[] unarmedModA = new sbyte[3]; // 1..2
            public byte acBase;
            public byte useStrength;
            public byte randByte;
            public int experience;
            public byte itemLimits;
            public byte hpRolled;
            public byte[] clericSpells = new byte[4]; // 1..CASTER_LEVEL_LAST (3)
            public byte[] mageSpells = new byte[4]; // 1..CASTER_LEVEL_LAST (3)
            public ushort xpAward;
            public byte xpBonusPerHp;
            public byte portraitHead;
            public byte portraitBody;
            public byte iconHead;
            public byte iconBody;
            public byte orderNumber;
            public byte iconSize;
            public byte[] iconColors = new byte[6]; // 0..5
            public byte numberOfItems;
            public TItemRec itemsAddr;
            public TItemRec[] equipped = new TItemRec[13]; // C_Equip.WEAPON..C_Equip.BOLT (0..12)
            public byte handsEquipped;
            public sbyte saveBonus;
            public ushort encumbrance;
            public TCharRecord nextCharAddr;
            public TCombatRec combatAddr;
            public byte status;
            public byte enabled;
            public sbyte hostile;
            public byte quickfight;
            public byte thac0Cur;
            public byte acCur;
            public byte acBehind;
            public byte curAttacks;
            public byte curAttacks2;
            public byte[] curRollsA = new byte[3]; // 1..2
            public byte[] curDiceA = new byte[3]; // 1..2
            public sbyte[] curModA = new sbyte[3]; // 1..2
            public byte hpCur;
            public byte movementCur;

            // Case 1 fields
            public ByteView stats;

            // Case 2 fields
            public sbyte genderSp
            {
                get => unchecked((sbyte)gender);
                set => gender = unchecked((byte)value);
            }

            // Case 3 fields
            public ByteView iconColors1;

            // Case 4 fields
            public ByteGridView slots;

            // Case 5 fields
            public byte genderIdx
            {
                get => gender;
                set => gender = value;
            }

            public byte alignmentIdx
            {
                get => alignment;
                set => alignment = value;
            }

            public TCharRecord()
            {
                stats = new ByteView(
                    i =>
                    {
                        switch (i)
                        {
                            case 0: return strCur;
                            case 1: return intCur;
                            case 2: return wisCur;
                            case 3: return dexCur;
                            case 4: return conCur;
                            case 5: return chaCur;
                            default: throw new ArgumentOutOfRangeException(nameof(i), "Invalid stats index");
                        }
                    },
                    (i, v) =>
                    {
                        switch (i)
                        {
                            case 0: strCur = v; break;
                            case 1: intCur = v; break;
                            case 2: wisCur = v; break;
                            case 3: dexCur = v; break;
                            case 4: conCur = v; break;
                            case 5: chaCur = v; break;
                            default: throw new ArgumentOutOfRangeException(nameof(i), "Invalid stats index");
                        }
                    }
                );

                iconColors1 = new ByteView(
                    i =>
                    {
                        if (i < 1 || i > 6) throw new ArgumentOutOfRangeException(nameof(i), "Invalid iconColors1 index");
                        return iconColors[i - 1];
                    },
                    (i, v) =>
                    {
                        if (i < 1 || i > 6) throw new ArgumentOutOfRangeException(nameof(i), "Invalid iconColors1 index");
                        iconColors[i - 1] = v;
                    }
                );

                slots = new ByteGridView(
                    (row, col) =>
                    {
                        if (row < 0 || row > 1) throw new ArgumentOutOfRangeException(nameof(row), "Invalid slots row");
                        if (col < 1 || col > 3) throw new ArgumentOutOfRangeException(nameof(col), "Invalid slots col");
                        if (row == 0) return clericSpells[col];
                        return mageSpells[col];
                    },
                    (row, col, v) =>
                    {
                        if (row < 0 || row > 1) throw new ArgumentOutOfRangeException(nameof(row), "Invalid slots row");
                        if (col < 1 || col > 3) throw new ArgumentOutOfRangeException(nameof(col), "Invalid slots col");
                        if (row == 0) clericSpells[col] = v;
                        else mageSpells[col] = v;
                    }
                );
            }

        }
    }
}
