// Source: Talonius pool-of-radiance pinned 7616920a
using System;

namespace Por.Core
{
    public static class AbilAdj
    {
        public const byte SCORE_MIN = 3;
        public const byte SCORE_MAX = 25;

        public static byte TranslatedStrength(Types.TCharRecord creature)
        {
            byte strCur = creature.strCur;
            if (strCur <= 17)
            {
                return strCur;
            }
            else if (strCur == 18)
            {
                byte strExc = creature.strExc;
                if (strExc == 0) return 18;
                if (strExc <= 50) return 19;
                if (strExc <= 75) return 20;
                if (strExc <= 90) return 21;
                if (strExc <= 99) return 22;
                return 23;
            }
            else if (strCur <= 25)
            {
                return unchecked((byte)(strCur + 5));
            }
            else
            {
                return 30;
            }
        }

        public static sbyte StrToHitAdjust(Types.TCharRecord creature)
        {
            sbyte result = 0;
            byte ts = TranslatedStrength(creature);
            if (creature.useStrength != 0)
            {
                if (ts >= 1 && ts <= 3) result = -3;
                else if (ts >= 4 && ts <= 5) result = -2;
                else if (ts >= 6 && ts <= 7) result = -1;
                else if (ts >= 17 && ts <= 19) result = 1;
                else if (ts >= 20 && ts <= 22) result = 2;
                else if (ts >= 23 && ts <= 25) result = 3;
                else if (ts >= 26 && ts <= 27) result = 4;
                else if (ts >= 28 && ts <= 30) result = unchecked((sbyte)(ts - 23));
            }
            return result;
        }

        public static sbyte StrDamageAdjust(Types.TCharRecord creature)
        {
            sbyte result = 0;
            byte ts = TranslatedStrength(creature);
            if (creature.useStrength != 0)
            {
                if (ts >= 1 && ts <= 2) result = -2;
                else if (ts >= 3 && ts <= 5) result = -1;
                else if (ts == 16) result = 1;
                else if (ts >= 17 && ts <= 19) result = unchecked((sbyte)(ts - 16));
                else if (ts >= 20 && ts <= 29) result = unchecked((sbyte)(ts - 17));
                else if (ts == 30) result = 14;
            }
            return result;
        }

        public static short WeightAllowance(Types.TCharRecord creature)
        {
            byte ts = TranslatedStrength(creature);
            short result = 0;
            if (ts >= 1 && ts <= 3) result = -350;
            else if (ts >= 4 && ts <= 5) result = -250;
            else if (ts >= 6 && ts <= 7) result = -150;
            else if (ts >= 12 && ts <= 13) result = 100;
            else if (ts >= 14 && ts <= 15) result = 200;
            else if (ts == 16) result = 350;
            else if (ts >= 17 && ts <= 21) result = unchecked((short)((ts - 17) * 250 + 500));
            else if (ts == 22 || ts == 23 || ts == 25 || ts == 26) result = unchecked((short)((ts - 22) * 1000 + 2000));
            else if (ts == 24) result = 4500;
            else if (ts == 27) result = 7500;
            else if (ts >= 28 && ts <= 30) result = unchecked((short)((ts - 28) * 3000 + 9000));
            return result;
        }

        public static sbyte DexMissileAdjust(Types.TCharRecord creature)
        {
            byte d = creature.dexCur;
            sbyte result = 0;
            if (d <= 2) result = -4;
            else if (d >= 3 && d <= 5) result = unchecked((sbyte)(d - 6));
            else if (d >= 16 && d <= 18) result = unchecked((sbyte)(d - 15));
            else if (d >= 19 && d <= 20) result = 3;
            else if (d >= 21 && d <= 23) result = 4;
            else if (d >= 24 && d <= 25) result = 5;
            return result;
        }

        public static sbyte DexterityArmorAdjustment(Types.TCharRecord creature)
        {
            byte d = creature.dexCur;
            sbyte result = 0;
            if (d >= 1 && d <= 3) result = -4;
            else if (d >= 4 && d <= 6) result = unchecked((sbyte)(d - 7));
            else if (d >= 15 && d <= 18) result = unchecked((sbyte)(d - 14));
            else if (d >= 19 && d <= 20) result = 4;
            else if (d >= 21 && d <= 23) result = 5;
            else if (d >= 24 && d <= 25) result = 6;
            return result;
        }

        public static byte ConSaveBonus(byte con)
        {
            return unchecked((byte)((ushort)con * 2 / 7));
        }

        public static byte ConPoisonSave(byte con)
        {
            if (con == 19 || con == 20) return 1;
            if (con == 21 || con == 22) return 2;
            if (con == 23 || con == 24) return 3;
            if (con >= 25) return 4;
            return 0;
        }

        public static ushort ConRegenMinutes(byte con)
        {
            if (con < 20) return 0;
            if (con > 25) return 10;
            return unchecked((ushort)((26 - con) * 10));
        }

        public static byte ClampScore(short score)
        {
            if (score < SCORE_MIN) return SCORE_MIN;
            if (score > SCORE_MAX) return SCORE_MAX;
            return unchecked((byte)score);
        }
    }
}
