using System;

namespace Por.Core
{
    public static partial class UsualSpl
    {
        private const int USUAL_ENTRIES = Types.MAX_PARTY;
        private const int NAME_LEN = 15;
        private const int RecSize = 44; // sig(5) version(1) name(16) randByte(1) spells TSpellSlots[0..20](21)

        private static readonly Types.TCharRecord[] usualOwner = new Types.TCharRecord[USUAL_ENTRIES + 1];
        private static readonly string[] usualName = new string[USUAL_ENTRIES + 1];
        private static readonly Types.TSpellSlots[] usualSpells = new Types.TSpellSlots[USUAL_ENTRIES + 1];

        static UsualSpl()
        {
            for (int i = 1; i <= USUAL_ENTRIES; i++)
            {
                usualOwner[i] = null;
                usualName[i] = string.Empty;
                usualSpells[i] = new Types.TSpellSlots();
            }
        }

        private static byte FindUsualEntry(Types.TCharRecord creature)
        {
            if (creature == null) return 0;
            byte result = 0;
            for (int i = 1; i <= USUAL_ENTRIES; i++)
            {
                if (ReferenceEquals(usualOwner[i], creature) && usualName[i] == creature.name)
                {
                    result = unchecked((byte)i);
                }
            }
            return result;
        }

        public static bool GetUsualSpells(Types.TCharRecord creature, ref Types.TSpellSlots spells)
        {
            byte i = FindUsualEntry(creature);
            if (i > 0)
            {
                for (int j = 0; j <= 20; j++)
                {
                    spells.Data[j] = usualSpells[i].Data[j];
                }
            }
            return i > 0;
        }

        public static void SetUsualSpells(Types.TCharRecord creature, ref Types.TSpellSlots spells)
        {
            if (creature == null) return;

            ForgetUsualSpells(creature);

            byte i = 0;
            for (int j = USUAL_ENTRIES; j >= 1; j--)
            {
                if (usualOwner[j] == null)
                {
                    i = unchecked((byte)j);
                }
            }

            if (i == 0)
            {
                for (int j = USUAL_ENTRIES; j >= 1; j--)
                {
                    if (!Party.IsPartyMember(usualOwner[j]))
                    {
                        i = unchecked((byte)j);
                    }
                }
            }

            if (i == 0)
            {
                i = 1;
            }

            usualOwner[i] = creature;
            usualName[i] = creature.name;
            for (int j = 0; j <= 20; j++)
            {
                usualSpells[i].Data[j] = spells.Data[j];
            }
        }

        public static void ForgetUsualSpells(Types.TCharRecord creature)
        {
            for (int i = 1; i <= USUAL_ENTRIES; i++)
            {
                if (ReferenceEquals(usualOwner[i], creature))
                {
                    usualOwner[i] = null;
                }
            }
        }

        public static void WriteUsualSpells(Types.TCharRecord creature, string fn)
        {
            string path = fn + ".mem";
            FileUtil.DeleteFile(path);

            Types.TSpellSlots recSpells = new Types.TSpellSlots();
            if (GetUsualSpells(creature, ref recSpells))
            {
                byte[] buffer = new byte[RecSize];
                Array.Clear(buffer, 0, RecSize);

                buffer[0] = 4;
                buffer[1] = (byte)'U';
                buffer[2] = (byte)'S';
                buffer[3] = (byte)'P';
                buffer[4] = (byte)'L';
                buffer[5] = 1; // Version

                string name = creature.name;
                int nameLen = name.Length;
                if (nameLen > NAME_LEN) nameLen = NAME_LEN;
                
                // Validate name chars
                for (int i = 0; i < nameLen; i++)
                {
                    if (name[i] > 255) return; // Or throw, but contract implies validation
                }

                buffer[6] = (byte)nameLen;
                for (int i = 0; i < nameLen; i++)
                {
                    buffer[7 + i] = (byte)name[i];
                }

                buffer[22] = creature.randByte;

                for (int i = 0; i <= 20; i++)
                {
                    buffer[23 + i] = recSpells.Data[i];
                }

                FileUtil.WriteRecord(path, buffer, (ushort)RecSize);
            }
        }

        public static void ReadUsualSpells(Types.TCharRecord creature, string fn)
        {
            ForgetUsualSpells(creature);

            string path = fn + ".mem";
            byte[] buffer = new byte[RecSize];
            if (!FileUtil.ReadRecord(path, buffer, (ushort)RecSize))
                return;

            if (buffer[0] != 4 ||
                buffer[1] != (byte)'U' || buffer[2] != (byte)'S' || buffer[3] != (byte)'P' || buffer[4] != (byte)'L' ||
                buffer[5] != 1)
                return;

            int nameLen = buffer[6];
            if (nameLen > NAME_LEN) return;

            string name = string.Empty;
            for (int i = 0; i < nameLen; i++)
            {
                name += (char)buffer[7 + i];
            }

            if (name != creature.name) return;
            if (buffer[22] != creature.randByte) return;

            Types.TSpellSlots recSpells = new Types.TSpellSlots();
            for (int i = 0; i <= 20; i++)
            {
                recSpells.Data[i] = buffer[23 + i];
            }

            SetUsualSpells(creature, ref recSpells);
        }
    }
}
