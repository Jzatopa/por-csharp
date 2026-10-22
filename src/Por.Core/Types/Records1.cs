// FILE: src/Por.Core/Types/Records1.cs
namespace Por.Core
{
    public static partial class Types
    {
        public class TBaseItemRec
        {
            public byte slot;
            public byte hands;
            public byte dmgLgRolls;
            public byte dmgLgDice;
            public sbyte dmgLgMod;
            public byte rateOfFire;
            public byte acDescr;
            public byte wpnClass;
            public byte unk8;
            public byte dmgSmRolls;
            public byte dmgSmDice;
            public sbyte dmgSmMod;
            public byte range;
            public byte classMask;
            public byte props;
            public byte unkF;
        }

        public class TCombatRec
        {
            public byte spellCast;
            public byte castAllowed;
            public byte itemUseAllowed;
            public sbyte initiative;
            public byte attackPhase;
            public byte sweepBudget;
            public byte movement;
            public byte guarding;
            public byte madeAttack;
            public byte direction;
            public TCharRecord other;
            public byte overkill;
            public byte flankCount;
            public byte turned;
            public byte usedTurnUndead;
            public byte flankSpread;
            public byte notPcNpc;
            public byte fleeing;
            public byte aiStance;
        }

        public class TDownedRec
        {
            public TCharRecord cr;
            public sbyte x;
            public sbyte y;
            public byte terrain;
        }

        public class TEffectRec
        {
            public byte id;
            public ushort duration;
            public byte strength;
            public bool onRemove;
            public TEffectRec next;
        }

        public class TMenuNode
        {
            public string name = string.Empty;
            public byte inUse;
            public TItemRec next;
        }

        public class TItemRec
        {
            public string name = string.Empty;
            public byte inUse;
            public TItemRec next;
            public byte itemType;
            public byte[] nameIds = new byte[4]; // 1..3
            public sbyte plus;
            public sbyte saveBonus;
            public byte equipped;
            public byte unidMask;
            public byte cursed;
            public ushort weight;
            public byte quantity;
            public ushort value;
            
            // Variant record backing storage
            // 0: charges, effect, special
            // 1: spells[1..3]
            // Shared backing array of 3 bytes
            public byte[] variantData = new byte[3];

            public byte charges
            {
                get { return variantData[0]; }
                set { variantData[0] = value; }
            }
            public byte effect
            {
                get { return variantData[1]; }
                set { variantData[1] = value; }
            }
            public byte special
            {
                get { return variantData[2]; }
                set { variantData[2] = value; }
            }

public ByteView spells;

public TItemRec()
{
    spells = new ByteView(
        i =>
        {
            if (i < 1 || i > 3) throw new System.IndexOutOfRangeException();
            return variantData[i - 1];
        },
        (i, v) =>
        {
            if (i < 1 || i > 3) throw new System.IndexOutOfRangeException();
            variantData[i - 1] = v;
        }
    );
}
        }

        public class TArenaLoc
        {
            public sbyte x;
            public sbyte y;
            public byte slotIndex;
            public byte size;
        }

        public class TBandRec
        {
            public sbyte lo;
            public sbyte hi;
        }

        public class TCombatTarget
        {
            public byte idx;
            public byte dist;
            public byte dir;
        }

        public class TIconCoordinateDelta
        {
            public sbyte dx;
            public sbyte dy;
        }

        public class TImgBuf
        {
            public ushort height;
            public ushort width;
            public ushort originX;
            public ushort originY;
            public byte count;
            public TColorMap map = new TColorMap();
            public ushort imgSize;
            public TBytes transp;
            public TImgBuf more;
            public byte[] data = new byte[32001]; // 0..32000
        }

        public class TInterruptRegisters
        {
            public ushort ax;
            public ushort bx;
            public ushort cx;
            public ushort dx;
            public ushort bp;
            public ushort si;
            public ushort di;
            public ushort ds;
            public ushort es;
            public ushort flags;
        }

        public class TMenuItem
        {
            public string name = string.Empty;
            public byte enabled;
        }

        public class TPortraitCacheSlot
        {
            public byte pictureId;
            public object pic; // Pointer
        }

        public class TRandomItemTemplate
        {
            public ushort[] nameIds = new ushort[4]; // 1..3
            public ushort weight;
            public ushort value;
            public ushort[] properties = new ushort[4]; // 1..3
        }

        public class TRuleAge
        {
            public ushort @base;
            public byte num;
            public byte sides;
        }

        public class TRuleGold
        {
            public byte num;
            public byte sides;
        }

        public class TRuleRace
        {
            // Assuming C_Gender.MALE=0, FEMALE=1 based on typical enum ordering, size 2
            public byte[] strMin = new byte[2];
            public byte[] strMax = new byte[2];
            public byte[] excMax = new byte[2];
            public byte intMin;
            public byte intMax;
            public byte wisMin;
            public byte wisMax;
            public byte dexMin;
            public byte dexMax;
            public byte conMin;
            public byte conMax;
            public byte chaMin;
            public byte chaMax;
        }

        public class TRuleRaceClasses
        {
            public sbyte count;
            public sbyte[] classes = new sbyte[14]; // 1..13
        }

        public class TRuleClassAlignments
        {
            public sbyte count;
            public sbyte[] alignments = new sbyte[10]; // 1..9
        }

        public class TRuleXp
        {
            public int[] xp = new int[C_Level.LEVEL_LAST + 1];
            public byte[,] slots = new byte[10, 4];
        }

        public class TSpecLoc
        {
            public ushort x;
            public ushort y;
            public ushort icon;
        }
    }
}
