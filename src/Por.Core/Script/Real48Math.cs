using System;
namespace Por.Core
{
    // Real48 format: sign, 39 fraction bits, 8-bit exponent (bias129); no subnormals.
    // Pinned src/TPC.CFG selects N- software Real. See report for validation scope.
    public static class Real48Math
    {
        public static double Quantize(double value)
        {
            ulong original = unchecked((ulong)BitConverter.DoubleToInt64Bits(value));
            ulong sign = original & (1UL << 63), bits = original & 0x7FFFFFFFFFFFFFFFUL;
            int rawExponent = (int)(bits >> 52);
            if (rawExponent == 2047) throw new OverflowException("Real48 cannot represent NaN or infinity.");
            if (bits == 0 || rawExponent - 1023 < -128) return 0;
            if (rawExponent - 1023 > 126) throw new OverflowException("Real48 exponent overflow.");
            // Keep40significand bits, nearest/ties-to-even at the storage boundary.
            bits = (bits + 4095UL + ((bits >> 13) & 1UL)) & ~8191UL;
            if ((int)(bits >> 52) - 1023 > 126) throw new OverflowException("Real48 exponent overflow.");
            return BitConverter.Int64BitsToDouble(unchecked((long)(sign | bits)));
        }
        public static ushort ScaleCoins(ushort coin, double fraction)
        {
            double stored = Quantize(fraction);
            if (stored == 0 || coin == 0) return 0;
            ulong bits = unchecked((ulong)BitConverter.DoubleToInt64Bits(stored));
            bool negative = (bits >> 63) != 0;
            int power = (int)((bits >> 52) & 2047) - 1023 - 39;
            ulong significand = (((bits & 0xFFFFFFFFFFFFFUL) | (1UL << 52)) >> 13) * coin;
            // This product has at most56bits: round it once, without intermediate Double rounding.
            int length = 0;
            for (ulong n = significand; n != 0; n >>= 1) length++;
            if (length > 40)
            {
                int shift = length - 40;
                ulong low = significand & ((1UL << shift) - 1), high = significand >> shift;
                ulong half = 1UL << (shift - 1);
                if (low > half || (low == half && (high & 1) != 0)) high++;
                significand = high; power += shift;
            }
            ulong integral;
            ulong limit = negative ? 2147483648UL : 2147483647UL;
            if (power >= 0)
            {
                if (power >= 32 || significand > (limit >> power)) throw new OverflowException("Pascal Trunc LongInt overflow.");
                integral = significand << power;
            }
            else integral = -power >= 64 ? 0 : significand >> -power;
            if (integral > limit) throw new OverflowException("Pascal Trunc LongInt overflow.");
            return unchecked((ushort)(negative ? -(long)integral : (long)integral));
        }
    }
}
