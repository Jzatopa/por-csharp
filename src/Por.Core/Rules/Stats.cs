using System;

namespace Por.Core
{
    public static class Stats
    {
        public const byte KILLS = 1;
        public const byte DAMAGE_DONE = 2;
        public const byte DAMAGE_TAKEN = 3;
        public const byte HEALED_GIVEN = 4;
        public const byte HEALED_TAKEN = 5;
        public const byte RAISED = 6;
        public const byte POISONED = 7;
        public const byte ITEMS_LOOTED = 8;
        public const byte COIN_LOOTED = 9;
        public const byte SPELLS_CAST = 10;
        public const byte KNOCKED_DOWN = 11;
        public const byte DIED = 12;
        public const byte STAT_COUNT = 12;
        public const byte KINDS = 48;

        public static readonly string[] NAMES = new string[STAT_COUNT + 1];

        private const int ENTRIES = Types.MAX_PARTY;
        private const string SIG = "STAT";
        private const byte VERSION = 1;

        private static Types.TCharRecord[] owner = new Types.TCharRecord[ENTRIES + 1];
        private static string[] ownerName = new string[ENTRIES + 1];
        private static TStatBlock[] blocks = new TStatBlock[ENTRIES + 1];
        private static Types.TCharRecord[] marked = new Types.TCharRecord[ENTRIES + 1];
        private static int[] markedWorth = new int[ENTRIES + 1];
        public static TStatBlock game = new TStatBlock();

        private class TStatFile
        {
            public string sig;
            public byte version;
            public string name;
            public byte randByte;
            public TStatBlock block;

            public TStatFile()
            {
                sig = "";
                version = 0;
                name = "";
                randByte = 0;
                block = new TStatBlock();
            }
        }

        private static TStatFile rec = new TStatFile();

        public class TKill
        {
            public string name;
            public int count;

            public TKill()
            {
                name = "";
                count = 0;
            }
        }

        public class TStatBlock
        {
            public int[] counts;
            public byte kindCount;
            public TKill[] kills;

            public TStatBlock()
            {
                counts = new int[STAT_COUNT + 1];
                for (int i = 1; i <= STAT_COUNT; i++)
                    counts[i] = 0;
                kindCount = 0;
                kills = new TKill[KINDS + 1];
                for (int i = 1; i <= KINDS; i++)
                    kills[i] = new TKill();
            }
        }

        static Stats()
        {
            NAMES[1] = "Kills";
            NAMES[2] = "Damage done";
            NAMES[3] = "Damage taken";
            NAMES[4] = "Healing given";
            NAMES[5] = "Healing taken";
            NAMES[6] = "Raised";
            NAMES[7] = "Poisoned";
            NAMES[8] = "Items looted";
            NAMES[9] = "Coin looted (net)";
            NAMES[10] = "Spells cast";
            NAMES[11] = "Knocked down";
            NAMES[12] = "Died";

            for (int i = 1; i <= ENTRIES; i++)
            {
                owner[i] = null;
                ownerName[i] = "";
                blocks[i] = null;
                marked[i] = null;
                markedWorth[i] = 0;
            }
        }

        private static void AddTo(ref int counter, int n)
        {
            if (n <= 0) return;
            if (counter > int.MaxValue - n)
                counter = int.MaxValue;
            else
                counter = unchecked((int)(counter + n));
        }

        private static void AddSigned(ref int counter, int n)
        {
            if (n > 0 && counter > int.MaxValue - n)
                counter = int.MaxValue;
            else if (n < 0 && counter < -int.MaxValue - n)
                counter = -int.MaxValue;
            else
                counter = unchecked((int)(counter + n));
        }

        private static void ClearBlock(TStatBlock b)
        {
            for (int i = 1; i <= STAT_COUNT; i++)
                b.counts[i] = 0;
            b.kindCount = 0;
            for (int i = 1; i <= KINDS; i++)
            {
                b.kills[i].name = "";
                b.kills[i].count = 0;
            }
        }

        private static void CopyBlock(TStatBlock dst, TStatBlock src)
        {
            for (int i = 1; i <= STAT_COUNT; i++)
                dst.counts[i] = src.counts[i];
            dst.kindCount = src.kindCount;
            for (int i = 1; i <= KINDS; i++)
            {
                dst.kills[i].name = src.kills[i].name;
                dst.kills[i].count = src.kills[i].count;
            }
        }

        private static void AddKill(ref TStatBlock b, string name)
        {
            if (name.Length > Types.NAME_LEN)
                name = name.Substring(0, Types.NAME_LEN);

            int i = 1;
            while (i <= b.kindCount && b.kills[i].name != name)
                i++;

            if (i > b.kindCount)
            {
                if (b.kindCount < KINDS - 1)
                {
                    b.kindCount++;
                    i = b.kindCount;
                }
                else
                {
                    i = KINDS;
                    b.kindCount = KINDS;
                    name = "OTHER";
                }
                b.kills[i].name = name;
                if (i < KINDS)
                    b.kills[i].count = 0;
            }
            AddTo(ref b.kills[i].count, 1);
        }

        private static byte FindEntry(Types.TCharRecord creature)
        {
            byte result = 0;
            for (int i = 1; i <= ENTRIES; i++)
            {
                if (owner[i] == creature && owner[i] != null && ownerName[i] == creature.name)
                    result = (byte)i;
            }
            return result;
        }

        private static byte NewEntry(Types.TCharRecord creature)
        {
            for (int i = 1; i <= ENTRIES; i++)
            {
                if (owner[i] == null || ownerName[i] != owner[i].name)
                {
                    if (blocks[i] == null)
                        blocks[i] = new TStatBlock();
                    ClearBlock(blocks[i]);
                    owner[i] = creature;
                    ownerName[i] = creature.name;
                    return (byte)i;
                }
            }
            return 0;
        }

        public static TStatBlock BlockOf(Types.TCharRecord creature)
        {
            if (!Party.IsPartyMember(creature))
                return null;
            byte i = FindEntry(creature);
            if (i == 0)
                i = NewEntry(creature);
            if (i != 0)
                return blocks[i];
            return null;
        }

        public static void Count(Types.TCharRecord creature, byte stat, int n)
        {
            if (!Party.IsPartyMember(creature))
                return;
            TStatBlock b = BlockOf(creature);
            if (b != null)
                AddTo(ref b.counts[stat], n);
            AddTo(ref game.counts[stat], n);
        }

        public static void Damaged(Types.TCharRecord actor, Types.TCharRecord victim, byte hp, bool wasUp)
        {
            bool down = wasUp && !(victim.status == C_Status.OKAY || victim.status == C_Status.ANIMATED);
            if (Party.IsPartyMember(victim))
            {
                Count(victim, DAMAGE_TAKEN, hp);
                if (down)
                    Count(victim, KNOCKED_DOWN, 1);
                if (victim.status == C_Status.DEAD)
                    Count(victim, DIED, 1);
            }
            else if (GDVars.ifcState == C_Ifc.COMBAT && actor != victim && Party.IsPartyMember(actor))
            {
                Count(actor, DAMAGE_DONE, hp);
                if (down)
                {
                    Count(actor, KILLS, 1);
                    TStatBlock b = BlockOf(actor);
                    if (b != null)
                        AddKill(ref b, victim.name);
                    AddKill(ref game, victim.name);
                }
            }
        }

        public static void BledOut(Types.TCharRecord creature)
        {
            Count(creature, DIED, 1);
        }

        public static void Healed(Types.TCharRecord actor, Types.TCharRecord target, byte hp, bool byPower)
        {
            Count(target, HEALED_TAKEN, hp);
            if (byPower || GDVars.dsCurrentSpellBeing != 0)
                Count(actor, HEALED_GIVEN, hp);
        }

        public static void MarkCoins()
        {
            for (int i = 1; i <= ENTRIES; i++)
            {
                marked[i] = null;
                markedWorth[i] = 0;
            }

            Types.TCharRecord p = GDVars.creatureList;
            int n = 0;
            while (p != null && n < GDVars.zone1[C_Gbvm.PARTY_SIZE] && n < ENTRIES)
            {
                n++;
                marked[n] = p;
                markedWorth[n] = MoneyCal.CreatureCopper(p);
                p = p.nextCharAddr;
            }
        }

        public static void SettleCoins()
        {
            for (int n = 1; n <= ENTRIES; n++)
            {
                if (marked[n] != null && Party.IsPartyMember(marked[n]))
                {
                    int delta = MoneyCal.CreatureCopper(marked[n]) - markedWorth[n];
                    if (delta != 0)
                    {
                        TStatBlock b = BlockOf(marked[n]);
                        if (b != null)
                            AddSigned(ref b.counts[COIN_LOOTED], delta);
                        AddSigned(ref game.counts[COIN_LOOTED], delta);
                    }
                }
            }
            for (int i = 1; i <= ENTRIES; i++)
            {
                marked[i] = null;
                markedWorth[i] = 0;
            }
        }

        public static void Forget(Types.TCharRecord creature)
        {
            byte i = FindEntry(creature);
            if (i != 0)
                owner[i] = null;
        }

        public static void ResetGame()
        {
            ClearBlock(game);
        }

        public static void WriteChar(Types.TCharRecord creature, string fn)
        {
            FileUtil.DeleteFile(fn + ".sts");
            byte i = FindEntry(creature);
            if (i == 0) return;

            rec.sig = SIG;
            rec.version = VERSION;
            rec.name = creature.name;
            rec.randByte = creature.randByte;
            CopyBlock(rec.block, blocks[i]);

            byte[] data = SerializeFile(rec);
            FileUtil.WriteRecord(fn + ".sts", data, (ushort)data.Length);
        }

        private static bool ReadFile(string name)
        {
            byte[] data = new byte[1032];
            if (!FileUtil.ReadRecord(name, data, (ushort)1032))
                return false;

            if (data.Length != 1032)
                return false;

            TStatFile file = DeserializeFile(data);
            if (file == null)
                return false;

            if (file.sig != SIG || file.version != VERSION || file.block.kindCount > KINDS)
                return false;

            rec.sig = file.sig;
            rec.version = file.version;
            rec.name = file.name;
            rec.randByte = file.randByte;
            CopyBlock(rec.block, file.block);
            return true;
        }

        public static void ReadChar(Types.TCharRecord creature, string fn)
        {
            Forget(creature);
            if (!ReadFile(fn + ".sts"))
                return;
            if (rec.name != creature.name || rec.randByte != creature.randByte)
                return;
            byte i = NewEntry(creature);
            if (i != 0)
                CopyBlock(blocks[i], rec.block);
        }

        public static void WriteGame(string fn)
        {
            rec.sig = SIG;
            rec.version = VERSION;
            rec.name = "";
            rec.randByte = 0;
            CopyBlock(rec.block, game);

            byte[] data = SerializeFile(rec);
            FileUtil.WriteRecord(fn, data, (ushort)data.Length);
        }

        public static void ReadGame(string fn)
        {
            if (ReadFile(fn))
                CopyBlock(game, rec.block);
            else
                ResetGame();
        }

        private static byte[] SerializeFile(TStatFile file)
        {
            byte[] data = new byte[1032];
            int offset = 0;

            int sigLen = Math.Min(4, file.sig.Length);
            data[offset++] = (byte)sigLen;
            for (int i = 0; i < 4; i++)
            {
                if (i < sigLen)
                {
                    char c = file.sig[i];
                    if (c > 255) throw new ArgumentException("Char > 255 in sig");
                    data[offset++] = (byte)c;
                }
                else
                    data[offset++] = 0;
            }

            data[offset++] = file.version;

            int nameLen = Math.Min(15, file.name.Length);
            data[offset++] = (byte)nameLen;
            for (int i = 0; i < 15; i++)
            {
                if (i < nameLen)
                {
                    char c = file.name[i];
                    if (c > 255) throw new ArgumentException("Char > 255 in name");
                    data[offset++] = (byte)c;
                }
                else
                    data[offset++] = 0;
            }

            data[offset++] = file.randByte;

            for (int i = 1; i <= STAT_COUNT; i++)
            {
                int val = file.block.counts[i];
                data[offset++] = (byte)(val & 0xFF);
                data[offset++] = (byte)((val >> 8) & 0xFF);
                data[offset++] = (byte)((val >> 16) & 0xFF);
                data[offset++] = (byte)((val >> 24) & 0xFF);
            }

            data[offset++] = file.block.kindCount;

            for (int i = 1; i <= KINDS; i++)
            {
                string name = file.block.kills[i].name;
                int killLen = Math.Min(15, name.Length);
                data[offset++] = (byte)killLen;
                for (int j = 0; j < 15; j++)
                {
                    if (j < killLen)
                    {
                        char c = name[j];
                        if (c > 255) throw new ArgumentException("Char > 255 in kill name");
                        data[offset++] = (byte)c;
                    }
                    else
                        data[offset++] = 0;
                }
                int count = file.block.kills[i].count;
                data[offset++] = (byte)(count & 0xFF);
                data[offset++] = (byte)((count >> 8) & 0xFF);
                data[offset++] = (byte)((count >> 16) & 0xFF);
                data[offset++] = (byte)((count >> 24) & 0xFF);
            }

            return data;
        }

        private static TStatFile DeserializeFile(byte[] data)
        {
            if (data.Length != 1032)
                return null;

            int offset = 0;
            TStatFile file = new TStatFile();

            byte sigLen = data[offset++];
            if (sigLen > 4) return null;
            char[] sigChars = new char[4];
            for (int i = 0; i < 4; i++)
            {
                if (i < sigLen)
                    sigChars[i] = (char)data[offset++];
                else
                    sigChars[i] = (char)data[offset++];
            }
            file.sig = new string(sigChars, 0, sigLen);

            file.version = data[offset++];

            byte nameLen = data[offset++];
            if (nameLen > 15) return null;
            char[] nameChars = new char[15];
            for (int i = 0; i < 15; i++)
            {
                if (i < nameLen)
                    nameChars[i] = (char)data[offset++];
                else
                    nameChars[i] = (char)data[offset++];
            }
            file.name = new string(nameChars, 0, nameLen);

            file.randByte = data[offset++];

            file.block = new TStatBlock();
            for (int i = 1; i <= STAT_COUNT; i++)
            {
                int val = (data[offset] & 0xFF) | ((data[offset + 1] & 0xFF) << 8) |
                          ((data[offset + 2] & 0xFF) << 16) | ((data[offset + 3] & 0xFF) << 24);
                file.block.counts[i] = val;
                offset += 4;
            }

            file.block.kindCount = data[offset++];
            if (file.block.kindCount > KINDS)
                return null;

            for (int i = 1; i <= KINDS; i++)
            {
                byte killNameLen = data[offset++];
                if (killNameLen > 15) return null;
                char[] killNameChars = new char[15];
                for (int j = 0; j < 15; j++)
                {
                    if (j < killNameLen)
                        killNameChars[j] = (char)data[offset++];
                    else
                        killNameChars[j] = (char)data[offset++];
                }
                file.block.kills[i].name = new string(killNameChars, 0, killNameLen);

                int count = (data[offset] & 0xFF) | ((data[offset + 1] & 0xFF) << 8) |
                            ((data[offset + 2] & 0xFF) << 16) | ((data[offset + 3] & 0xFF) << 24);
                file.block.kills[i].count = count;
                offset += 4;
            }

            return file;
        }
    }
}
