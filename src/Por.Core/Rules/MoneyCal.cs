// Source: Talonius pool-of-radiance pinned 7616920a
using System;

namespace Por.Core
{
    public static class MoneyCal
    {
        public const byte PAY_NOBODY = 0;
        public const byte PAY_CHARACTER = 1;
        public const byte PAY_POOL = 2;

        public static int CopperValue(Types.TMoney m)
        {
            int total = 0;
            for (sbyte i = C_Money.COPPER; i <= C_Money.PLATINUM; i++)
            {
                total = unchecked(total + GDConsts.dsCoinValuationCopper[i] * m[i]);
            }
            return total;
        }

        public static int CoinValueInGold(Types.TMoney m)
        {
            return CopperValue(m) / GDConsts.dsCoinValuationCopper[C_Money.GOLD];
        }

        public static void CoinsOf(Types.TCharRecord creature, Types.TMoney m)
        {
            // FillChar equivalent: clear all money fields/indexes COPPER..JEWELRY
            for (int i = 0; i < 7; i++)
            {
                m[i] = 0;
            }
            for (sbyte i = C_Money.COPPER; i <= C_Money.PLATINUM; i++)
            {
                m[i] = creature.coins[i];
            }
        }

        public static int CreatureCopper(Types.TCharRecord creature)
        {
            Types.TMoney m = new Types.TMoney();
            CoinsOf(creature, m);
            return CopperValue(m);
        }

        public static int CreatureGold(Types.TCharRecord creature)
        {
            return CreatureCopper(creature) / GDConsts.dsCoinValuationCopper[C_Money.GOLD];
        }

        public static void MakeChange(int rem, int cap, Types.TMoney m)
        {
            if (rem < 0)
            {
                rem = 0;
            }
            for (sbyte i = C_Money.PLATINUM; i >= C_Money.COPPER; i--)
            {
                int n = rem / GDConsts.dsCoinValuationCopper[i];
                if (n > cap)
                {
                    n = cap;
                }
                m[i] = n;
                rem = unchecked(rem - n * GDConsts.dsCoinValuationCopper[i]);
            }
        }

        public static byte WhoPays(Types.TCharRecord buyer, int gp)
        {
            if (gp <= CreatureGold(buyer))
            {
                return PAY_CHARACTER;
            }
            else if (gp <= CoinValueInGold(GDVars.treasureMoney))
            {
                return PAY_POOL;
            }
            else
            {
                return PAY_NOBODY;
            }
        }

        public static byte ItemBundle(Types.TItemRec item)
        {
            switch (item.itemType)
            {
                case 9:
                case 28:
                case 73:
                    return 20;
                default:
                    return 1;
            }
        }

        public static int ItemValue(Types.TItemRec item)
        {
            int q = item.quantity;
            if (q == 0)
            {
                q = 1;
            }
            return unchecked((int)item.value * q / ItemBundle(item));
        }

        public static ushort ItemSalePrice(Types.TItemRec item)
        {
            int v = ItemValue(item) / 2;
            if (v > 65535)
            {
                v = 65535;
            }
            return (ushort)v;
        }

        public static sbyte ParseMoneyType(string s, ref string typeName)
        {
            if (string.IsNullOrEmpty(s))
            {
                throw new ArgumentException("Input string must not be null or empty", nameof(s));
            }

            int i = 0;
            while (i < s.Length && s[i] == ' ')
            {
                i++;
            }

            if (i >= s.Length)
            {
                throw new ArgumentException("Input string contains only whitespace", nameof(s));
            }

            char first = s[i];
            char second = (i + 1 < s.Length) ? s[i + 1] : '\0';

            switch (first)
            {
                case 'G':
                    if (char.ToUpper(second) == 'E')
                    {
                        typeName = "Gems ";
                        return C_Money.GEMS;
                    }
                    else
                    {
                        typeName = "Gold ";
                        return C_Money.GOLD;
                    }
                case 'P':
                    typeName = "Platinum ";
                    return C_Money.PLATINUM;
                case 'E':
                    typeName = "electrum ";
                    return C_Money.ELECTRUM;
                case 'S':
                    typeName = "silver ";
                    return C_Money.SILVER;
                case 'C':
                    typeName = "Copper ";
                    return C_Money.COPPER;
                case 'J':
                    typeName = "Jewelry ";
                    return C_Money.JEWELRY;
                default:
                    throw new ArgumentException("Unknown money type character", nameof(s));
            }
        }
    }
}
