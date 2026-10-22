using System;

namespace Por.Core
{
    public static class Party
    {
        public static bool IsPartyMember(Types.TCharRecord creature)
        {
            bool result = false;
            if (creature == null) return result;

            Types.TCharRecord p = GDVars.creatureList;
            short n = unchecked((short)GDVars.zone1[C_Gbvm.PARTY_SIZE]);

            while (p != null && n > 0)
            {
                if (ReferenceEquals(p, creature))
                {
                    result = true;
                    break;
                }
                p = p.nextCharAddr;
                n = unchecked((short)(n - 1));
            }
            return result;
        }

        public static void SelectPrevNextChar(char c)
        {
            Types.TCharRecord p = GDVars.creatureList;
            if (p != null)
            {
                if (c == C_Keys.HOME || c == C_Keys.UP)
                {
                    p = Party.PrevCreature(GDVars.curChar);
                }
                else if (c == C_Keys.END_KEY || c == C_Keys.DOWN)
                {
                    p = Party.NextCreature(GDVars.curChar);
                }
            }
            GDVars.curChar = p;
        }

        public static Types.TCharRecord LastCreature()
        {
            Types.TCharRecord p = GDVars.creatureList;
            if (p != null)
            {
                while (p.nextCharAddr != null)
                {
                    p = p.nextCharAddr;
                }
            }
            return p;
        }

        public static Types.TCharRecord PrevCreature(Types.TCharRecord c)
        {
            Types.TCharRecord p = GDVars.creatureList;
            if (ReferenceEquals(p, c))
            {
                p = Party.LastCreature();
            }
            else
            {
                while (p != null && !ReferenceEquals(p.nextCharAddr, c))
                {
                    p = p.nextCharAddr;
                }
            }
            return p;
        }

        public static Types.TCharRecord NextCreature(Types.TCharRecord c)
        {
            if (c.nextCharAddr != null)
            {
                return c.nextCharAddr;
            }
            else
            {
                return GDVars.creatureList;
            }
        }
    }
}
