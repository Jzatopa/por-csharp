namespace Por.Core
{
    public static class Clock
    {
        public static void ValidateClock(Types.TClock clock, bool ageOnYear)
        {
            for (int i = 0; i <= C_Time.MONTHS; i++)
            {
                if (clock[i] >= GDConsts.unitScale[i])
                {
                    clock[i + 1] = unchecked((ushort)(clock[i + 1] + 1));
                    clock[i] = (ushort)(clock[i] - GDConsts.unitScale[i]);
                    if (i == C_Time.MONTHS && ageOnYear)
                    {
                        Types.TCharRecord cur = GDVars.creatureList;
                        while (cur != null)
                        {
                            cur.age = unchecked((ushort)(cur.age + 1));
                            cur = cur.nextCharAddr;
                        }
                    }
                }
            }
        }

        public static void SetFromMinutes(Types.TClock clock, ushort minutes)
        {
            for (int i = 0; i < 7; i++)
            {
                clock[i] = 0;
            }
            clock[C_Time.DAYS] = (ushort)(minutes / 1440);
            clock[C_Time.HOURS] = (ushort)((minutes % 1440) / 60);
            clock[C_Time.TURNS] = (ushort)((minutes % 60) / 10);
            clock[C_Time.MINUTES] = (ushort)(minutes % 10);
        }
    }
}
