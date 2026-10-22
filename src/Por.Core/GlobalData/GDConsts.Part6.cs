namespace Por.Core
{
    public static partial class GDConsts
    {
private static Types.TRuleXp CreateXpSource(int[] compactXP, byte[,] compactSlots) { Types.TRuleXp result = new Types.TRuleXp(); for (int i = 0; i < 8; i++) { result.xp[i + 2] = compactXP[i]; for (int j = 0; j < 3; j++) { result.slots[i + 2, j + 1] = compactSlots[i, j]; } } return result; }

        public static Types.TRuleClassAlignments[] alignByClass = new Types.TRuleClassAlignments[17];

        public static sbyte[] conBonusTbl = new sbyte[26];

        public static Types.TRuleXp[] dsExperienceSpellsClass = new Types.TRuleXp[8];

        public static Types.TRuleGold[] dsStartingGoldClass = new Types.TRuleGold[8];

        public static Types.TRuleSaves[,] saveTbl = new Types.TRuleSaves[8, 10];

        private static readonly bool _part6Initialized = InitializePart6();
        private static bool InitializePart6()
        {
alignByClass[0] = new Types.TRuleClassAlignments { count = (sbyte)9, alignments = new sbyte[10] { 0, 0, 1, 2, 3, 4, 5, 6, 7, 8 } };
alignByClass[1] = new Types.TRuleClassAlignments { count = (sbyte)5, alignments = new sbyte[10] { 0, 1, 3, 4, 5, 7, 0, 0, 0, 0 } };
alignByClass[2] = new Types.TRuleClassAlignments { count = (sbyte)9, alignments = new sbyte[10] { 0, 0, 1, 2, 3, 4, 5, 6, 7, 8 } };
alignByClass[3] = new Types.TRuleClassAlignments { count = (sbyte)1, alignments = new sbyte[10] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } };
alignByClass[4] = new Types.TRuleClassAlignments { count = (sbyte)3, alignments = new sbyte[10] { 0, 0, 3, 6, 0, 0, 0, 0, 0, 0 } };
alignByClass[5] = new Types.TRuleClassAlignments { count = (sbyte)9, alignments = new sbyte[10] { 0, 0, 1, 2, 3, 4, 5, 6, 7, 8 } };
alignByClass[6] = new Types.TRuleClassAlignments { count = (sbyte)7, alignments = new sbyte[10] { 0, 1, 2, 3, 4, 5, 7, 8, 0, 0 } };
alignByClass[7] = new Types.TRuleClassAlignments { count = (sbyte)9, alignments = new sbyte[10] { 0, 0, 1, 2, 3, 4, 5, 6, 7, 8 } };
alignByClass[8] = new Types.TRuleClassAlignments { count = (sbyte)9, alignments = new sbyte[10] { 0, 0, 1, 2, 3, 4, 5, 6, 7, 8 } };
alignByClass[9] = new Types.TRuleClassAlignments { count = (sbyte)9, alignments = new sbyte[10] { 0, 0, 1, 2, 3, 4, 5, 6, 7, 8 } };
alignByClass[10] = new Types.TRuleClassAlignments { count = (sbyte)3, alignments = new sbyte[10] { 0, 0, 3, 6, 0, 0, 0, 0, 0, 0 } };
alignByClass[11] = new Types.TRuleClassAlignments { count = (sbyte)9, alignments = new sbyte[10] { 0, 0, 1, 2, 3, 4, 5, 6, 7, 8 } };
alignByClass[12] = new Types.TRuleClassAlignments { count = (sbyte)9, alignments = new sbyte[10] { 0, 0, 1, 2, 3, 4, 5, 6, 7, 8 } };
alignByClass[13] = new Types.TRuleClassAlignments { count = (sbyte)9, alignments = new sbyte[10] { 0, 0, 1, 2, 3, 4, 5, 6, 7, 8 } };
alignByClass[14] = new Types.TRuleClassAlignments { count = (sbyte)7, alignments = new sbyte[10] { 0, 1, 2, 3, 4, 5, 7, 8, 0, 0 } };
alignByClass[15] = new Types.TRuleClassAlignments { count = (sbyte)7, alignments = new sbyte[10] { 0, 1, 2, 3, 4, 5, 7, 8, 0, 0 } };
alignByClass[16] = new Types.TRuleClassAlignments { count = (sbyte)7, alignments = new sbyte[10] { 0, 1, 2, 3, 4, 5, 7, 8, 0, 0 } };
conBonusTbl[3] = -2; conBonusTbl[4] = -1; conBonusTbl[5] = -1; conBonusTbl[6] = -1; conBonusTbl[7] = 0; conBonusTbl[8] = 0; conBonusTbl[9] = 0; conBonusTbl[10] = 0; conBonusTbl[11] = 0; conBonusTbl[12] = 0; conBonusTbl[13] = 0; conBonusTbl[14] = 0; conBonusTbl[15] = 1; conBonusTbl[16] = 2; conBonusTbl[17] = 2; conBonusTbl[18] = 2; conBonusTbl[19] = 2; conBonusTbl[20] = 2; conBonusTbl[21] = 2; conBonusTbl[22] = 2; conBonusTbl[23] = 2; conBonusTbl[24] = 2; conBonusTbl[25] = 2;
dsExperienceSpellsClass[0] = CreateXpSource(new int[] { 1501, 3001, 6001, 13001, 27501, -1, -1, -1 }, new byte[,] { { 2, 0, 0 }, { 2, 1, 0 }, { 3, 2, 0 }, { 3, 3, 1 }, { 3, 3, 2 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 } }); dsExperienceSpellsClass[1] = CreateXpSource(new int[] { -1, -1, -1, -1, -1, -1, -1, -1 }, new byte[,] { { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 } }); dsExperienceSpellsClass[2] = CreateXpSource(new int[] { 2001, 4001, 8001, 18001, 35001, 70001, 125001, -1 }, new byte[,] { { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 } }); dsExperienceSpellsClass[3] = CreateXpSource(new int[] { 2751, 5501, 12001, 24001, 45001, 95001, -1, -1 }, new byte[,] { { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 } }); dsExperienceSpellsClass[4] = CreateXpSource(new int[] { 2251, 4501, 10001, 20001, 40001, 90001, -1, -1 }, new byte[,] { { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 } }); dsExperienceSpellsClass[5] = CreateXpSource(new int[] { 2501, 5001, 10001, 22501, 40001, -1, -1, -1 }, new byte[,] { { 2, 0, 0 }, { 2, 1, 0 }, { 3, 2, 0 }, { 4, 2, 1 }, { 4, 2, 2 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 } }); dsExperienceSpellsClass[6] = CreateXpSource(new int[] { 1251, 2501, 5001, 10001, 20001, 42501, 70001, 110001 }, new byte[,] { { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 } }); dsExperienceSpellsClass[7] = CreateXpSource(new int[] { -1, -1, -1, -1, -1, -1, -1, -1 }, new byte[,] { { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 } });
            // dsStartingGoldClass: array[C_Level.CLERIC..C_Level.MONK] (0..7)
            dsStartingGoldClass[0] = new Types.TRuleGold { num = 3, sides = 6 };
            dsStartingGoldClass[1] = new Types.TRuleGold { num = 3, sides = 6 };
            dsStartingGoldClass[2] = new Types.TRuleGold { num = 5, sides = 4 };
            dsStartingGoldClass[3] = new Types.TRuleGold { num = 5, sides = 4 };
            dsStartingGoldClass[4] = new Types.TRuleGold { num = 5, sides = 4 };
            dsStartingGoldClass[5] = new Types.TRuleGold { num = 2, sides = 4 };
            dsStartingGoldClass[6] = new Types.TRuleGold { num = 2, sides = 6 };
            dsStartingGoldClass[7] = new Types.TRuleGold { num = 5, sides = 4 };

            // saveTbl: array[C_Level.CLERIC..C_Level.MONK, 1..9]
            // Dimensions: [8, 10] (Class 0..7, Level 0..9)
            
            // 0: Cleric
            saveTbl[0, 0] = new Types.TRuleSaves();
            saveTbl[0, 1] = new Types.TRuleSaves { values = new byte[5] { 10, 13, 14, 16, 15 } };
            saveTbl[0, 2] = new Types.TRuleSaves { values = new byte[5] { 10, 13, 14, 16, 15 } };
            saveTbl[0, 3] = new Types.TRuleSaves { values = new byte[5] { 10, 13, 14, 16, 15 } };
            saveTbl[0, 4] = new Types.TRuleSaves { values = new byte[5] { 9, 12, 13, 15, 14 } };
            saveTbl[0, 5] = new Types.TRuleSaves { values = new byte[5] { 9, 12, 13, 15, 14 } };
            saveTbl[0, 6] = new Types.TRuleSaves { values = new byte[5] { 9, 12, 13, 15, 14 } };
            saveTbl[0, 7] = new Types.TRuleSaves { values = new byte[5] { 7, 10, 11, 13, 12 } };
            saveTbl[0, 8] = new Types.TRuleSaves { values = new byte[5] { 7, 10, 11, 13, 12 } };
            saveTbl[0, 9] = new Types.TRuleSaves { values = new byte[5] { 7, 10, 11, 13, 12 } };

            // 1: Druid
            saveTbl[1, 0] = new Types.TRuleSaves();
            saveTbl[1, 1] = new Types.TRuleSaves { values = new byte[5] { 10, 13, 14, 16, 15 } };
            saveTbl[1, 2] = new Types.TRuleSaves { values = new byte[5] { 10, 13, 14, 16, 15 } };
            saveTbl[1, 3] = new Types.TRuleSaves { values = new byte[5] { 10, 13, 14, 16, 15 } };
            saveTbl[1, 4] = new Types.TRuleSaves { values = new byte[5] { 9, 12, 13, 15, 14 } };
            saveTbl[1, 5] = new Types.TRuleSaves { values = new byte[5] { 9, 12, 13, 15, 14 } };
            saveTbl[1, 6] = new Types.TRuleSaves { values = new byte[5] { 9, 12, 13, 15, 14 } };
            saveTbl[1, 7] = new Types.TRuleSaves { values = new byte[5] { 7, 10, 11, 13, 12 } };
            saveTbl[1, 8] = new Types.TRuleSaves { values = new byte[5] { 7, 10, 11, 13, 12 } };
            saveTbl[1, 9] = new Types.TRuleSaves { values = new byte[5] { 7, 10, 11, 13, 12 } };

            // 2: Fighter
            saveTbl[2, 0] = new Types.TRuleSaves();
            saveTbl[2, 1] = new Types.TRuleSaves { values = new byte[5] { 14, 15, 16, 17, 17 } };
            saveTbl[2, 2] = new Types.TRuleSaves { values = new byte[5] { 14, 15, 16, 17, 17 } };
            saveTbl[2, 3] = new Types.TRuleSaves { values = new byte[5] { 13, 14, 15, 16, 16 } };
            saveTbl[2, 4] = new Types.TRuleSaves { values = new byte[5] { 13, 14, 15, 16, 16 } };
            saveTbl[2, 5] = new Types.TRuleSaves { values = new byte[5] { 11, 12, 13, 13, 14 } };
            saveTbl[2, 6] = new Types.TRuleSaves { values = new byte[5] { 11, 12, 13, 13, 14 } };
            saveTbl[2, 7] = new Types.TRuleSaves { values = new byte[5] { 10, 11, 12, 12, 13 } };
            saveTbl[2, 8] = new Types.TRuleSaves { values = new byte[5] { 10, 11, 12, 12, 13 } };
            saveTbl[2, 9] = new Types.TRuleSaves { values = new byte[5] { 8, 9, 10, 9, 11 } };

            // 3: Paladin
            saveTbl[3, 0] = new Types.TRuleSaves();
            saveTbl[3, 1] = new Types.TRuleSaves { values = new byte[5] { 12, 13, 14, 15, 15 } };
            saveTbl[3, 2] = new Types.TRuleSaves { values = new byte[5] { 12, 13, 14, 15, 15 } };
            saveTbl[3, 3] = new Types.TRuleSaves { values = new byte[5] { 11, 12, 13, 14, 14 } };
            saveTbl[3, 4] = new Types.TRuleSaves { values = new byte[5] { 11, 12, 13, 14, 14 } };
            saveTbl[3, 5] = new Types.TRuleSaves { values = new byte[5] { 9, 10, 11, 11, 12 } };
            saveTbl[3, 6] = new Types.TRuleSaves { values = new byte[5] { 9, 10, 11, 11, 12 } };
            saveTbl[3, 7] = new Types.TRuleSaves { values = new byte[5] { 8, 9, 10, 10, 11 } };
            saveTbl[3, 8] = new Types.TRuleSaves { values = new byte[5] { 8, 9, 10, 10, 11 } };
            saveTbl[3, 9] = new Types.TRuleSaves { values = new byte[5] { 6, 7, 8, 7, 9 } };

            // 4: Ranger
            saveTbl[4, 0] = new Types.TRuleSaves();
            saveTbl[4, 1] = new Types.TRuleSaves { values = new byte[5] { 14, 15, 16, 17, 17 } };
            saveTbl[4, 2] = new Types.TRuleSaves { values = new byte[5] { 14, 15, 16, 17, 17 } };
            saveTbl[4, 3] = new Types.TRuleSaves { values = new byte[5] { 13, 14, 15, 16, 16 } };
            saveTbl[4, 4] = new Types.TRuleSaves { values = new byte[5] { 13, 14, 15, 16, 16 } };
            saveTbl[4, 5] = new Types.TRuleSaves { values = new byte[5] { 11, 12, 13, 13, 14 } };
            saveTbl[4, 6] = new Types.TRuleSaves { values = new byte[5] { 11, 12, 13, 13, 14 } };
            saveTbl[4, 7] = new Types.TRuleSaves { values = new byte[5] { 10, 11, 12, 12, 13 } };
            saveTbl[4, 8] = new Types.TRuleSaves { values = new byte[5] { 10, 11, 12, 12, 13 } };
            saveTbl[4, 9] = new Types.TRuleSaves { values = new byte[5] { 8, 9, 10, 9, 11 } };

            // 5: Magic-User
            saveTbl[5, 0] = new Types.TRuleSaves();
            saveTbl[5, 1] = new Types.TRuleSaves { values = new byte[5] { 14, 13, 11, 15, 12 } };
            saveTbl[5, 2] = new Types.TRuleSaves { values = new byte[5] { 14, 13, 11, 15, 12 } };
            saveTbl[5, 3] = new Types.TRuleSaves { values = new byte[5] { 14, 13, 11, 15, 12 } };
            saveTbl[5, 4] = new Types.TRuleSaves { values = new byte[5] { 14, 13, 11, 15, 12 } };
            saveTbl[5, 5] = new Types.TRuleSaves { values = new byte[5] { 14, 13, 11, 15, 12 } };
            saveTbl[5, 6] = new Types.TRuleSaves { values = new byte[5] { 13, 11, 9, 13, 10 } };
            saveTbl[5, 7] = new Types.TRuleSaves { values = new byte[5] { 13, 11, 9, 13, 10 } };
            saveTbl[5, 8] = new Types.TRuleSaves { values = new byte[5] { 13, 11, 9, 13, 10 } };
            saveTbl[5, 9] = new Types.TRuleSaves { values = new byte[5] { 13, 11, 9, 13, 10 } };

            // 6: Thief
            saveTbl[6, 0] = new Types.TRuleSaves();
            saveTbl[6, 1] = new Types.TRuleSaves { values = new byte[5] { 13, 12, 14, 16, 15 } };
            saveTbl[6, 2] = new Types.TRuleSaves { values = new byte[5] { 13, 12, 14, 16, 15 } };
            saveTbl[6, 3] = new Types.TRuleSaves { values = new byte[5] { 13, 12, 14, 16, 15 } };
            saveTbl[6, 4] = new Types.TRuleSaves { values = new byte[5] { 13, 12, 14, 16, 15 } };
            saveTbl[6, 5] = new Types.TRuleSaves { values = new byte[5] { 12, 11, 12, 15, 13 } };
            saveTbl[6, 6] = new Types.TRuleSaves { values = new byte[5] { 12, 11, 12, 15, 13 } };
            saveTbl[6, 7] = new Types.TRuleSaves { values = new byte[5] { 12, 11, 12, 15, 13 } };
            saveTbl[6, 8] = new Types.TRuleSaves { values = new byte[5] { 12, 11, 12, 15, 13 } };
            saveTbl[6, 9] = new Types.TRuleSaves { values = new byte[5] { 11, 10, 10, 14, 11 } };

            // 7: Monk
            saveTbl[7, 0] = new Types.TRuleSaves();
            saveTbl[7, 1] = new Types.TRuleSaves { values = new byte[5] { 13, 12, 14, 16, 15 } };
            saveTbl[7, 2] = new Types.TRuleSaves { values = new byte[5] { 13, 12, 14, 16, 15 } };
            saveTbl[7, 3] = new Types.TRuleSaves { values = new byte[5] { 13, 12, 14, 16, 15 } };
            saveTbl[7, 4] = new Types.TRuleSaves { values = new byte[5] { 13, 12, 14, 16, 15 } };
            saveTbl[7, 5] = new Types.TRuleSaves { values = new byte[5] { 12, 11, 12, 15, 13 } };
            saveTbl[7, 6] = new Types.TRuleSaves { values = new byte[5] { 12, 11, 12, 15, 13 } };
            saveTbl[7, 7] = new Types.TRuleSaves { values = new byte[5] { 12, 11, 12, 15, 13 } };
            saveTbl[7, 8] = new Types.TRuleSaves { values = new byte[5] { 12, 11, 12, 15, 13 } };
            saveTbl[7, 9] = new Types.TRuleSaves { values = new byte[5] { 11, 10, 10, 14, 11 } };
            return true;
        }
    }
}
