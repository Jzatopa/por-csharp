// FILE: src/Por.Core/Types/RuleOverlays.cs
using System;

namespace Por.Core
{
    public static partial class Types
    {
    public sealed class ByteView
    {
        private readonly Func<int, byte> _getter;
        private readonly Action<int, byte> _setter;

        public ByteView(Func<int, byte> getter, Action<int, byte> setter)
        {
            _getter = getter ?? throw new ArgumentNullException(nameof(getter));
            _setter = setter ?? throw new ArgumentNullException(nameof(setter));
        }

        public byte this[int index]
        {
            get => _getter(index);
            set => _setter(index, value);
        }
    }

    public sealed class TRuleThief
    {
        public sbyte[] values = new sbyte[9];

        public sbyte pickPockets
        {
            get => values[1];
            set => values[1] = value;
        }

        public sbyte openLocks
        {
            get => values[2];
            set => values[2] = value;
        }

        public sbyte findTraps
        {
            get => values[3];
            set => values[3] = value;
        }

        public sbyte moveSilently
        {
            get => values[4];
            set => values[4] = value;
        }

        public sbyte hideShadows
        {
            get => values[5];
            set => values[5] = value;
        }

        public sbyte hearNoise
        {
            get => values[6];
            set => values[6] = value;
        }

        public sbyte climbWalls
        {
            get => values[7];
            set => values[7] = value;
        }

        public sbyte readLanguages
        {
            get => values[8];
            set => values[8] = value;
        }
    }

    public sealed class TRuleThiefDex
    {
        public sbyte[] values = new sbyte[6];

        public sbyte pickPockets
        {
            get => values[1];
            set => values[1] = value;
        }

        public sbyte openLocks
        {
            get => values[2];
            set => values[2] = value;
        }

        public sbyte findTraps
        {
            get => values[3];
            set => values[3] = value;
        }

        public sbyte moveSilently
        {
            get => values[4];
            set => values[4] = value;
        }

        public sbyte hideShadows
        {
            get => values[5];
            set => values[5] = value;
        }
    }

    public sealed class TRuleClassParts
    {
        public sbyte[] values = new sbyte[4];

        public sbyte first
        {
            get => values[1];
            set => values[1] = value;
        }

        public sbyte second
        {
            get => values[2];
            set => values[2] = value;
        }

        public sbyte third
        {
            get => values[3];
            set => values[3] = value;
        }
    }

    public sealed class TRuleAbilities
    {
        public sbyte[] values = new sbyte[6];

        public sbyte strength
        {
            get => values[0];
            set => values[0] = value;
        }

        public sbyte intelligence
        {
            get => values[1];
            set => values[1] = value;
        }

        public sbyte wisdom
        {
            get => values[2];
            set => values[2] = value;
        }

        public sbyte dexterity
        {
            get => values[3];
            set => values[3] = value;
        }

        public sbyte constitution
        {
            get => values[4];
            set => values[4] = value;
        }

        public sbyte charisma
        {
            get => values[5];
            set => values[5] = value;
        }
    }

    public sealed class TRuleClassLevels
    {
        public sbyte[] values = new sbyte[8];

        public sbyte cleric
        {
            get => values[0];
            set => values[0] = value;
        }

        public sbyte druid
        {
            get => values[1];
            set => values[1] = value;
        }

        public sbyte fighter
        {
            get => values[2];
            set => values[2] = value;
        }

        public sbyte paladin
        {
            get => values[3];
            set => values[3] = value;
        }

        public sbyte ranger
        {
            get => values[4];
            set => values[4] = value;
        }

        public sbyte mage
        {
            get => values[5];
            set => values[5] = value;
        }

        public sbyte thief
        {
            get => values[6];
            set => values[6] = value;
        }

        public sbyte monk
        {
            get => values[7];
            set => values[7] = value;
        }
    }

    public sealed class TRuleAgeCategories
    {
        public ushort[] values = new ushort[6];

        public ushort youngAdult
        {
            get => values[1];
            set => values[1] = value;
        }

        public ushort mature
        {
            get => values[2];
            set => values[2] = value;
        }

        public ushort middleAged
        {
            get => values[3];
            set => values[3] = value;
        }

        public ushort old
        {
            get => values[4];
            set => values[4] = value;
        }

        public ushort venerable
        {
            get => values[5];
            set => values[5] = value;
        }
    }

    public sealed class TRuleSaves
    {
        public byte[] values = new byte[5];

        public byte paralyze
        {
            get => values[0];
            set => values[0] = value;
        }

        public byte petrify
        {
            get => values[1];
            set => values[1] = value;
        }

        public byte wand
        {
            get => values[2];
            set => values[2] = value;
        }

        public byte breath
        {
            get => values[3];
            set => values[3] = value;
        }

        public byte spell
        {
            get => values[4];
            set => values[4] = value;
        }
    }

    public sealed class TSpellRec
    {
        private byte[] _raw = new byte[16];

        public byte castClass
        {
            get => _raw[0];
            set => _raw[0] = value;
        }

        public byte level
        {
            get => _raw[1];
            set => _raw[1] = value;
        }

        public byte rangeFixed
        {
            get => _raw[2];
            set => _raw[2] = value;
        }

        public byte rangeVar
        {
            get => _raw[3];
            set => _raw[3] = value;
        }

        public byte durFixed
        {
            get => _raw[4];
            set => _raw[4] = value;
        }

        public byte durVar
        {
            get => _raw[5];
            set => _raw[5] = value;
        }

        public byte area
        {
            get => _raw[6];
            set => _raw[6] = value;
        }

        public byte target
        {
            get => _raw[7];
            set => _raw[7] = value;
        }

        public byte saveResult
        {
            get => _raw[8];
            set => _raw[8] = value;
        }

        public byte saveType
        {
            get => _raw[9];
            set => _raw[9] = value;
        }

        public byte effect
        {
            get => _raw[10];
            set => _raw[10] = value;
        }

        public byte whenCast
        {
            get => _raw[11];
            set => _raw[11] = value;
        }

        public byte castTime
        {
            get => _raw[12];
            set => _raw[12] = value;
        }

        public byte aiPriority
        {
            get => _raw[13];
            set => _raw[13] = value;
        }

        public byte offensive
        {
            get => _raw[14];
            set => _raw[14] = value;
        }

        public byte aiRadius
        {
            get => _raw[15];
            set => _raw[15] = value;
        }

        public sbyte castClassSp
        {
            get => unchecked((sbyte)castClass);
            set => castClass = unchecked((byte)value);
        }

        public sbyte levelSp
        {
            get => unchecked((sbyte)level);
            set => level = unchecked((byte)value);
        }

        public ByteView savePrefix
        {
            get
            {
                return new ByteView(
                    i =>
                    {
                        if (i < 0 || i > 7) throw new ArgumentOutOfRangeException(nameof(i));
                        return _raw[i];
                    },
                    (i, v) =>
                    {
                        if (i < 0 || i > 7) throw new ArgumentOutOfRangeException(nameof(i));
                        _raw[i] = v;
                    }
                );
            }
        }

        public bool saveEffect
        {
            get => saveResult != 0;
            set => saveResult = value ? (byte)1 : (byte)0;
        }
    }
    }
}
