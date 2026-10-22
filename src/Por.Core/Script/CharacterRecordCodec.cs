// Generated from the pinned Pascal primary TCharRecord layout; see scripts/generate_character_codec.py.
using System;
namespace Por.Core
{
    public static class CharacterRecordCodec
    {
        public const int Size = 285;
        public static Func<uint, Types.TItemRec> ResolveEquippedItem;
        private static ushort Word(byte[] d, int at) { return (ushort)(d[at] | d[at + 1] << 8); }
        private static uint Dword(byte[] d, int at) { return unchecked((uint)(d[at] | d[at + 1] << 8 | d[at + 2] << 16 | d[at + 3] << 24)); }
        public static void DecodeMonster(byte[] d, int start, Types.TCharRecord c)
        {
            if (d == null || start < 0 || d.Length - start < Size) throw new ArgumentException("Short packed character record.");
            if (d[start] > 15) throw new FormatException("Invalid packed character name length.");
            var chars = new char[d[start]];
            for (int i = 0; i < chars.Length; i++) chars[i] = (char)d[start + i + 1];
            c.name = new string(chars);
            c.strCur = d[start + 16];
            c.intCur = d[start + 17];
            c.wisCur = d[start + 18];
            c.dexCur = d[start + 19];
            c.conCur = d[start + 20];
            c.chaCur = d[start + 21];
            c.strExc = d[start + 22];
            for (int i = 0; i <= 20; i++)
            {
                c.memorizedSpells[i] = d[start + 23 + (i - 0) * 1];
            }
            c.spellPrepTime = d[start + 44];
            c.thac0Base = d[start + 45];
            c.race = unchecked((sbyte)d[start + 46]);
            c.charClass = unchecked((sbyte)d[start + 47]);
            c.age = Word(d, start + 48);
            c.hpMax = d[start + 50];
            for (int i = 1; i <= 56; i++)
            {
                c.spellsKnown[i] = d[start + 51 + (i - 1) * 1];
            }
            c.attackLevel = d[start + 107];
            c.iconDimensions = d[start + 108];
            for (int i = 0; i <= 4; i++)
            {
                c.saves[i] = d[start + 109 + (i - 0) * 1];
            }
            c.movementBase = d[start + 114];
            c.levelHighest = unchecked((sbyte)d[start + 115]);
            c.drainedLevels = d[start + 116];
            c.drainedHps = d[start + 117];
            c.levelUndead = unchecked((sbyte)d[start + 118]);
            for (int i = 1; i <= 8; i++)
            {
                c.thiefSkills[i] = d[start + 119 + (i - 1) * 1];
            }
            c.effectsAddr = null; // Source LoadCreatureData discards this stale DOS pointer.
            c.unknown083 = d[start + 131];
            c.npc = d[start + 132];
            c.npcTreasureShare = d[start + 133];
            c.unknown086 = d[start + 134];
            c.unknown087 = d[start + 135];
            for (int i = 0; i <= 6; i++)
            {
                c.coins[i] = Word(d, start + 136 + (i - 0) * 2);
            }
            for (int i = 0; i <= 7; i++)
            {
                c.classLevels[i] = unchecked((sbyte)d[start + 150 + (i - 0) * 1]);
            }
            c.gender = d[start + 158];
            c.charType = d[start + 159];
            c.alignment = d[start + 160];
            c.attacks = d[start + 161];
            c.attacks2 = d[start + 162];
            for (int i = 1; i <= 2; i++)
            {
                c.unarmedRollsA[i] = d[start + 163 + (i - 1) * 1];
            }
            for (int i = 1; i <= 2; i++)
            {
                c.unarmedDiceA[i] = d[start + 165 + (i - 1) * 1];
            }
            for (int i = 1; i <= 2; i++)
            {
                c.unarmedModA[i] = unchecked((sbyte)d[start + 167 + (i - 1) * 1]);
            }
            c.acBase = d[start + 169];
            c.useStrength = d[start + 170];
            c.randByte = d[start + 171];
            c.experience = unchecked((int)Dword(d, start + 172));
            c.itemLimits = d[start + 176];
            c.hpRolled = d[start + 177];
            for (int i = 1; i <= 3; i++)
            {
                c.clericSpells[i] = d[start + 178 + (i - 1) * 1];
            }
            for (int i = 1; i <= 3; i++)
            {
                c.mageSpells[i] = d[start + 181 + (i - 1) * 1];
            }
            c.xpAward = Word(d, start + 184);
            c.xpBonusPerHp = d[start + 186];
            c.portraitHead = d[start + 187];
            c.portraitBody = d[start + 188];
            c.iconHead = d[start + 189];
            c.iconBody = d[start + 190];
            c.orderNumber = d[start + 191];
            c.iconSize = d[start + 192];
            for (int i = 0; i <= 5; i++)
            {
                c.iconColors[i] = d[start + 193 + (i - 0) * 1];
            }
            c.numberOfItems = d[start + 199];
            c.itemsAddr = null; // Source LoadCreatureData discards this stale DOS pointer.
            for (int i = 0; i <= 12; i++)
            {
                uint pointer = Dword(d, start + 204 + (i - 0) * 4);
                if (pointer != 0 && ResolveEquippedItem == null) throw new NotSupportedException("Packed equipped DOS pointer needs a resolver.");
                c.equipped[i] = pointer == 0 ? null : ResolveEquippedItem(pointer);
            }
            c.handsEquipped = d[start + 256];
            c.saveBonus = unchecked((sbyte)d[start + 257]);
            c.encumbrance = Word(d, start + 258);
            c.nextCharAddr = null; // Source LoadCreatureData discards this stale DOS pointer.
            c.combatAddr = null; // Source LoadCreatureData discards this stale DOS pointer.
            c.status = d[start + 268];
            c.enabled = d[start + 269];
            c.hostile = unchecked((sbyte)d[start + 270]);
            c.quickfight = d[start + 271];
            c.thac0Cur = d[start + 272];
            c.acCur = d[start + 273];
            c.acBehind = d[start + 274];
            c.curAttacks = d[start + 275];
            c.curAttacks2 = d[start + 276];
            for (int i = 1; i <= 2; i++)
            {
                c.curRollsA[i] = d[start + 277 + (i - 1) * 1];
            }
            for (int i = 1; i <= 2; i++)
            {
                c.curDiceA[i] = d[start + 279 + (i - 1) * 1];
            }
            for (int i = 1; i <= 2; i++)
            {
                c.curModA[i] = unchecked((sbyte)d[start + 281 + (i - 1) * 1]);
            }
            c.hpCur = d[start + 283];
            c.movementCur = d[start + 284];
        }
    }
}
