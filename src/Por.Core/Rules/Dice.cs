using System;

namespace Por.Core
{
    public static class Dice
    {
        public static Func<int, int> RandomBelow = null;

        public static byte RollDice(byte n, byte sides)
        {
            byte total = 0;
            if (n > 0)
            {
                if (RandomBelow == null)
                {
                    throw new InvalidOperationException("RandomBelow is not set.");
                }
                for (int i = 1; i <= n; i++)
                {
                    int roll = RandomBelow(sides);
                    if (sides > 0 && (roll < 0 || roll >= sides))
                    {
                        throw new InvalidOperationException("RandomBelow returned an out-of-bounds value.");
                    }
                    total = unchecked((byte)(total + (byte)(roll + 1)));
                }
            }
            return total;
        }

        public static byte RollDamageDice(byte n, byte sides)
        {
            GDVars.gDamageDice = n;
            return RollDice(n, sides);
        }
    }
}
