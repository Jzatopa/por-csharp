using System;
using static Por.Core.Types;

namespace Por.Core
{
    public static class EclVars
    {
        public static Action<byte> diskHook;
        public static Action<byte, byte> wallSetHook;
        public static Action<TCharRecord> RecalcStats;

        public static ushort GetEclVar(sbyte idx)
        {
            unchecked
            {
                byte lo = GDVars.dsGbvmLowerByte[idx];
                byte hi = GDVars.dsGbvmUpperByte[idx];
                byte t = GDVars.dsGbvmType[idx];
    
                switch (t)
            {
                    case 0:
                        return lo;
                    case 1:
                    case 3:
                        return ReadGbvm((ushort)(hi * 256 + lo));
                    case 0x80:
                        return 0;
                    case 2:
                    case 0x81:
                        return CombineBytesToWord(lo, hi);
                    default:
                        throw new InvalidOperationException("Unknown ECL variable type");
                }
                        }
            }
    
            public static void LoadEclVars(byte count)
            {
            unchecked
            {
                byte strIdx = 0;
                for (int i = 1; i <= count; i++)
            {
                    byte t = GDVars.zone3[GDVars.eclIp + 1];
                    byte lo = GDVars.zone3[GDVars.eclIp + 2];
                    GDVars.eclIp = (ushort)(GDVars.eclIp + 2);
    
                    byte hi = 0;
                    switch (t)
                    {
                        case 1:
                        case 2:
                        case 3:
                            GDVars.eclIp = (ushort)(GDVars.eclIp + 1);
                            hi = GDVars.zone3[GDVars.eclIp];
                            break;
                        case 0x80:
                            if (strIdx < GB_STRING_LAST) strIdx++;
                            ushort len = lo;
                            if (len > 0)
                                ConvGbString(len, strIdx);
                            else
                                GDVars.gbStrings[strIdx] = "";
                            break;
                        case 0x81:
                            if (strIdx < GB_STRING_LAST) strIdx++;
                            GDVars.eclIp = (ushort)(GDVars.eclIp + 1);
                            hi = GDVars.zone3[GDVars.eclIp];
                            ushort v = CombineBytesToWord(lo, hi);
                            DerefGbString(strIdx, v);
                            break;
                    }
    
                    if (i <= 64)
                    {
                        GDVars.dsGbvmType[i] = t;
                        GDVars.dsGbvmLowerByte[i] = lo;
                        if (t == 1 || t == 2 || t == 3 || t == 0x81)
                            GDVars.dsGbvmUpperByte[i] = hi;
                    }
                }
                GDVars.eclIp = (ushort)(GDVars.eclIp + 1);
                        }
            }
    
            public static ushort CombineBytesToWord(byte lowByte, byte highByte)
            {
            unchecked
            {
                return (ushort)(highByte * 256 + lowByte);
                        }
            }
    
            private static char DecodeGameCharacter(byte c)
            {
            unchecked
            {
                if (c > 31)
                    return (char)c;
                else
                    return (char)(c + 64);
                        }
            }
    
            public static byte GbvmZone(ushort address)
            {
            unchecked
            {
                if (address >= C_Gbvm.ZONE0_FIRST && address <= C_Gbvm.ZONE0_LAST) return 0;
                if (address >= C_Gbvm.ZONE1_FIRST && address <= C_Gbvm.ZONE1_LAST) return 1;
                if (address >= C_Gbvm.ZONE2_FIRST && address <= C_Gbvm.ZONE2_LAST) return 2;
                if (address >= C_Gbvm.SCRIPT_FIRST && address <= C_Gbvm.SCRIPT_LAST) return 3;
                return 4;
                        }
            }
    
            private static byte FindCurCharIdx(TCharRecord creature)
            {
            unchecked
            {
                TCharRecord p = GDVars.creatureList;
                bool found = false;
                byte idx = 0;
                while (p != null && !found)
            {
                    if (ReferenceEquals(p, creature))
                        found = true;
                    else
                    {
                        idx++;
                        p = p.nextCharAddr;
                    }
                }
                return idx;
                        }
            }
    
            private static ushort SpecialReadZ1(ushort address, out bool found)
            {
            unchecked
            {
                found = true;
                ushort result = 0;
                switch (address)
            {
                    case C_Gbvm.CHAR_CON:
                        result = GDVars.curChar.conCur;
                        break;
                    case C_Gbvm.CHAR_SAVE_PETRIFY:
                        result = GDVars.curChar.saves[C_Save.PETRIFY];
                        break;
                    case C_Gbvm.CHAR_LEVEL:
                        result = unchecked((ushort)(short)GDVars.curChar.levelHighest);
                        break;
                    case C_Gbvm.CHAR_THIEF_SKILLS:
                    case C_Gbvm.CHAR_THIEF_SKILLS + 1:
                    case C_Gbvm.CHAR_THIEF_SKILLS + 2:
                    case C_Gbvm.CHAR_THIEF_SKILLS + 3:
                    case C_Gbvm.CHAR_THIEF_SKILLS + 4:
                    case C_Gbvm.CHAR_THIEF_SKILLS + 5:
                    case C_Gbvm.CHAR_THIEF_SKILLS + 6:
                    case C_Gbvm.CHAR_THIEF_SKILLS + 7:
                        {
                            byte t = (byte)(address - C_Gbvm.CHAR_THIEF_SKILLS + 1);
                            result = GDVars.curChar.thiefSkills[t];
                            break;
                        }
                    case C_Gbvm.CHAR_NPC:
                        result = GDVars.curChar.npc;
                        break;
                    case C_Gbvm.CHAR_COPPER:
                        result = GDVars.curChar.coins[C_Money.COPPER];
                        break;
                    case C_Gbvm.CHAR_SILVER:
                        result = GDVars.curChar.coins[C_Money.SILVER];
                        break;
                    case C_Gbvm.CHAR_ELECTRUM:
                        result = GDVars.curChar.coins[C_Money.ELECTRUM];
                        break;
                    case C_Gbvm.CHAR_GOLD:
                        result = GDVars.curChar.coins[C_Money.GOLD];
                        break;
                    case C_Gbvm.CHAR_PLATINUM:
                        result = GDVars.curChar.coins[C_Money.PLATINUM];
                        break;
                    case C_Gbvm.CHAR_ALIGNMENT:
                        result = (ushort)(sbyte)GDVars.curChar.alignment;
                        break;
                    case C_Gbvm.CHAR_XP_AWARD:
                        result = GDVars.curChar.xpAward;
                        break;
                    case C_Gbvm.CHAR_XP_PER_HP:
                        result = GDVars.curChar.xpBonusPerHp;
                        break;
                    case C_Gbvm.CHAR_ENABLED:
                        result = GDVars.curChar.enabled != 0 ? (ushort)1 : (ushort)0x80;
                        break;
                    case C_Gbvm.CHAR_CONTROL:
                        if (GDVars.curChar.hostile == 0 && GDVars.curChar.quickfight != 0)
                            result = 0x80;
                        else if (GDVars.curChar.hostile == 1)
                            result = 0x81;
                        else
                            result = 0;
                        break;
                    case C_Gbvm.CHAR_CONTROL_HI:
                        throw new InvalidOperationException("CHAR_CONTROL_HI read is undefined");
                    case C_Gbvm.CHAR_MOVEMENT:
                        result = GDVars.curChar.movementCur;
                        break;
                    case C_Gbvm.CHAR_PARTY_INDEX:
                    case C_Gbvm.CHAR_PARTY_INDEX2:
                        result = FindCurCharIdx(GDVars.curChar);
                        break;
                    case C_Gbvm.CHAR_LOYALTY:
                        {
                            byte cha = GDVars.curChar.chaCur;
                            if (cha < 3 || cha > 25)
                                throw new InvalidOperationException("Loyalty read undefined for charisma outside 3..25");
                            switch (cha)
                            {
                                case 3: result = 0; break;
                                case 4: result = 5; break;
                                case 5: result = 10; break;
                                case 6: result = 15; break;
                                case 7: result = 20; break;
                                case 8: case 9: case 10: case 11: case 12: result = 25; break;
                                case 13: result = 30; break;
                                case 14: result = 35; break;
                                case 15: result = 40; break;
                                case 16: result = 50; break;
                                case 17: result = 55; break;
                                default: result = 60; break;
                            }
                            break;
                        }
                    case C_Gbvm.DISK_NUM:
                        result = GDVars.virtualDisk;
                        break;
                    case C_Gbvm.PARTY_SIZE:
                        result = GDVars.zone1[C_Gbvm.PARTY_SIZE];
                        break;
                    default:
                        found = false;
                        break;
                }
                return result;
                        }
            }
    
            private static void SpecialWriteZ1(ushort address, ushort value)
            {
            unchecked
            {
                if (address >= C_Gbvm.CHAR_MEMORIZED_FIRST && address <= C_Gbvm.CHAR_MEMORIZED_LAST)
            {
                    int t = address - C_Gbvm.CHAR_MEMORIZED_FIRST + 1;
                    byte v = unchecked((byte)value); var c = GDVars.curChar;
                    if (t <= 20) c.memorizedSpells[t] = v;
                    else switch (t)
                    {
                        case 21: c.spellPrepTime = v; break;
                        case 22: c.thac0Base = v; break;
                        case 23: c.race = unchecked((sbyte)v); break;
                        case 24: c.charClass = unchecked((sbyte)v); break;
                        case 25: c.age = (ushort)((c.age & 0xFF00) | v); break;
                        case 26: c.age = (ushort)((c.age & 0xFF) | (v << 8)); break;
                        case 27: c.hpMax = v; break;
                        default: c.spellsKnown[t - 27] = v; break;
                    }
                    return;
                }
    
                switch (address)
            {
                    case C_Gbvm.CHAR_DUMP:
                        if (value == 0) GDVars.dumpCr6B00 = true;
                        break;
                    case C_Gbvm.CHAR_NPC:
                        {
                            ushort v = value;
                            if (v > C_Npc.ANIMATED_NPC) v = (ushort)(v - 0x32);
                            GDVars.curChar.npc = (byte)v;
                            break;
                        }
                    case C_Gbvm.CHAR_COPPER:
                        GDVars.curChar.coins[C_Money.COPPER] = (ushort)value;
                        break;
                    case C_Gbvm.CHAR_SILVER:
                        GDVars.curChar.coins[C_Money.SILVER] = (ushort)value;
                        break;
                    case C_Gbvm.CHAR_ELECTRUM:
                        GDVars.curChar.coins[C_Money.ELECTRUM] = (ushort)value;
                        break;
                    case C_Gbvm.CHAR_GOLD:
                        GDVars.curChar.coins[C_Money.GOLD] = (ushort)value;
                        break;
                    case C_Gbvm.CHAR_PLATINUM:
                        GDVars.curChar.coins[C_Money.PLATINUM] = (ushort)value;
                        break;
                    case C_Gbvm.CHAR_XP_AWARD:
                        GDVars.curChar.xpAward = value;
                        break;
                    case C_Gbvm.CHAR_XP_PER_HP:
                        GDVars.curChar.xpBonusPerHp = (byte)value;
                        break;
                    case C_Gbvm.CHAR_ENABLED:
                        if (value >= 0x80)
                        {
                            GDVars.curChar.enabled = 0;
                            if (value == 0x87) GDVars.curChar.status = C_Status.STONED;
                        }
                        if (value == 0) GDVars.dumpCr6C00 = true;
                        break;
                    case C_Gbvm.CHAR_CONTROL:
                        switch (value)
                        {
                            case 0:
                                GDVars.curChar.hostile = 0;
                                GDVars.curChar.quickfight = 0;
                                break;
                            case 0x80:
                                GDVars.curChar.hostile = 0;
                                GDVars.curChar.quickfight = 1;
                                break;
                            case 0x81:
                                GDVars.curChar.hostile = 1;
                                GDVars.curChar.quickfight = 1;
                                break;
                        }
                        break;
                    case C_Gbvm.DISK_NUM:
                        if (value >= 1 && value <= 8)
                        {
                            if (diskHook == null) throw new NotSupportedException("diskHook not set");
                            diskHook((byte)value);
                        }
                        break;
                    case C_Gbvm.WALLSET_LOAD1:
                    case C_Gbvm.WALLSET_LOAD2:
                    case C_Gbvm.WALLSET_LOAD3:
                        if (value > 0x80)
                        {
                            if (wallSetHook == null) throw new NotSupportedException("wallSetHook not set");
                            wallSetHook((byte)(value & 0x7F), (byte)((address - C_Gbvm.WALLSET_LOAD1) / 2 + 1));
                        }
                        break;
                }
    
                if (address >= C_Gbvm.CHAR_COPPER && address <= C_Gbvm.CHAR_PLATINUM && (address & 1) != 0)
            {
                    if (RecalcStats == null) throw new NotSupportedException("RecalcStats not set");
                    RecalcStats(GDVars.curChar);
                }
                        }
            }
    
            public static void WriteGbvm(ushort address, ushort value)
            {
            unchecked
            {
                byte zone = GbvmZone(address);
                switch (zone)
            {
                    case 0:
                        GDVars.zone0.words[address] = value;
                        if (address == C_Gbvm.SKY_CEILING_COLOR || address == C_Gbvm.ALT_SKY_COLOR)
                            GDVars.redrawSky = true;
                        break;
                    case 1:
                        GDVars.zone1[address] = value;
                        SpecialWriteZ1(address, value);
                        break;
                    case 2:
                        GDVars.zone2[address] = value;
                        break;
                    case 3:
                        GDVars.zone3[address] = unchecked((byte)value);
                        break;
                    case 4:
                        switch (address)
                        {
                            case C_Gbvm.READ_REG1:
                            case C_Gbvm.READ_REG2:
                            case C_Gbvm.READ_REG3:
                            case C_Gbvm.WRITE_REG1:
                            case C_Gbvm.WRITE_REG2:
                                break;
                            case C_Gbvm.SOUND_EFFECT:
                                GDVars.sndEffect = value;
                                break;
                            case C_Gbvm.CELL_PARTY_X:
                                GDVars.movedParty = true;
                                GDVars.gMapX = (sbyte)value;
                                break;
                            case C_Gbvm.CELL_PARTY_Y:
                                GDVars.gMapY = (sbyte)value;
                                GDVars.movedParty = true;
                                break;
                            case C_Gbvm.CELL_FACING:
                                {
                                    bool ok = false;
                                    ushort v = value;
                                    while (!ok)
                                    {
                                        switch (v)
                                        {
                                            case 0: GDVars.gFacing = C_Dir.NORTH; ok = true; break;
                                            case 1: GDVars.gFacing = C_Dir.EAST; ok = true; break;
                                            case 2: GDVars.gFacing = C_Dir.SOUTH; ok = true; break;
                                            case 3: GDVars.gFacing = C_Dir.WEST; ok = true; break;
                                            default: v = (ushort)(v - 4); break;
                                        }
                                    }
                                    GDVars.movedParty = true;
                                    break;
                                }
                            case C_Gbvm.CELL_WALL_COLOR:
                                GDVars.wallColorSel = (byte)value;
                                GDVars.picRegionW = true;
                                break;
                            case C_Gbvm.CELL_CEIL_FLOOR_COLOR:
                                GDVars.ceilFloorColorSel = (byte)value;
                                GDVars.picRegionW = true;
                                break;
                            case C_Gbvm.CELL_IGNORED:
                                break;
                        }
                        break;
                }
                        }
            }
    
            private static ushort PartyEffectsTable(ushort address)
            {
            unchecked
            {
                ushort result = 0;
                ushort k = (ushort)((address - C_Gbvm.PARTY_EFFECTS) % (C_Gbvm.PARTY_EFFECT_OWNERS - C_Gbvm.PARTY_EFFECTS));
                ushort e = 0;
                byte n = 0;
                TCharRecord p = GDVars.creatureList;
                while (p != null)
            {
                    TEffectRec effect = p.effectsAddr;
                    while (effect != null)
                    {
                        if (e == k)
                        {
                            if (address < C_Gbvm.PARTY_EFFECT_OWNERS)
                                result = (ushort)effect.id;
                            else
                                result = n;
                        }
                        e++;
                        effect = effect.next;
                    }
                    n++;
                    p = p.nextCharAddr;
                }
                return result;
                        }
            }
    
            public static ushort ReadGbvm(ushort address)
            {
            unchecked
            {
                byte zone = GbvmZone(address);
                bool found = false;
                switch (zone)
            {
                    case 0:
                        if (address <= C_Gbvm.PARTY_EFFECTS_LAST)
                            return PartyEffectsTable(address);
                        else
                            return GDVars.zone0.words[address];
                    case 1:
                        {
                            ushort r = SpecialReadZ1(address, out found);
                            if (!found)
                                return GDVars.zone1[address];
                            return r;
                        }
                    case 2:
                        return GDVars.zone2[address];
                    case 3:
                        return GDVars.zone3[address];
                    case 4:
                        switch (address)
                        {
                            case C_Gbvm.READ_REG1:
                            case C_Gbvm.READ_REG2:
                            case C_Gbvm.READ_REG3:
                                return 0;
                            case C_Gbvm.FACING_RAW:
                                return GDVars.gFacing;
                            case C_Gbvm.CELL_35F:
                                throw new InvalidOperationException("CELL_35F read is undefined");
                            case C_Gbvm.CELL_PARTY_X:
                                return (ushort)(sbyte)GDVars.gMapX;
                            case C_Gbvm.CELL_PARTY_Y:
                                return (ushort)(sbyte)GDVars.gMapY;
                            case C_Gbvm.CELL_FACING:
                                return (ushort)(GDVars.gFacing / 2);
                            case C_Gbvm.CELL_WALL_AHEAD:
                                return GDVars.wallAhead;
                            case C_Gbvm.CELL_EVENT:
                                return GDVars.eventAtCell;
                            case C_Gbvm.CELL_WALL_COLOR:
                                throw new InvalidOperationException("CELL_WALL_COLOR read is undefined");
                            default:
                                throw new InvalidOperationException("Unhandled zone 4 read address");
                        }
                }
                throw new InvalidOperationException("Unhandled zone read");
                        }
            }
    
            private static ushort ZoneLast(byte zone)
            {
            unchecked
            {
                switch (zone)
            {
                    case 0: return C_Gbvm.ZONE0_LAST;
                    case 1: return C_Gbvm.ZONE1_LAST;
                    case 2: return C_Gbvm.ZONE2_LAST;
                    case 3: return C_Gbvm.SCRIPT_LAST;
                    default: return 0;
                }
                        }
            }
    
            public static void WriteGbvmString(ushort address, string s)
            {
            unchecked
            {
                if (s.Length > 255) s = s.Substring(0, 255);
                byte zone = GbvmZone(address);
                int len = s.Length;
                ushort last = ZoneLast(zone);
                if (zone < 4)
            {
                    if (last - address < len)
                        len = last - address;
                }
    
                switch (zone)
            {
                    case 0:
                        for (int i = 0; i < len; i++)
                            GDVars.zone0.words[address + i] = (ushort)s[i];
                        GDVars.zone0.words[address + len] = 0;
                        break;
                    case 1:
                        if (address == C_Gbvm.CHAR_NAME)
                            GDVars.curChar.name = s.Length > 15 ? s.Substring(0, 15) : s;
                        else
                        {
                            for (int i = 0; i < len; i++)
                                GDVars.zone1[address + i] = (ushort)s[i];
                            GDVars.zone1[address + len] = 0;
                        }
                        break;
                    case 2:
                        for (int i = 0; i < len; i++)
                            GDVars.zone2[address + i] = (ushort)s[i];
                        GDVars.zone2[address + len] = 0;
                        break;
                    case 3:
                        for (int i = 0; i < len; i++)
                            GDVars.zone3[address + i] = unchecked((byte)s[i]);
                        GDVars.zone3[address + len] = 0;
                        break;
                }
                        }
            }
    
            private static void ConvGbString(ushort len, byte strIdx)
            {
            unchecked
            {
                GDVars.gbStrings[strIdx] = "";
                int i = 0;
                byte phase = 1;
                byte cur = 0;
                byte prev = 0;
    
                while (i < len)
            {
                    if (phase < 4)
                    {
                        GDVars.eclIp = (ushort)(GDVars.eclIp + 1);
                        prev = cur;
                        cur = GDVars.zone3[GDVars.eclIp];
                        i++;
                    }
    
                    byte c6;
                    switch (phase)
                    {
                        case 1:
                            c6 = (byte)(cur >> 2);
                            break;
                        case 2:
                            c6 = (byte)(unchecked((byte)((prev << 6) + (cur >> 2))) >> 2);
                            break;
                        case 3:
                            c6 = (byte)(unchecked((byte)((prev << 4) + (cur >> 4))) >> 2);
                            break;
                        case 4:
                            c6 = (byte)(cur & 0x3F);
                            break;
                        default:
                            c6 = 0;
                            break;
                    }
    
                    if (phase < 4) phase++;
                    else phase = 1;
    
                    if (c6 != 0)
                    {
                        char ch = DecodeGameCharacter(c6);
                        if (GDVars.gbStrings[strIdx].Length < 255) GDVars.gbStrings[strIdx] += ch;
                    }
                }
    
                if (phase == 4)
            {
                    byte c6 = (byte)(cur & 0x3F);
                    if (c6 != 0)
                    {
                        char ch = DecodeGameCharacter(c6);
                        if (GDVars.gbStrings[strIdx].Length < 255) GDVars.gbStrings[strIdx] += ch;
                    }
                }
                        }
            }
    
            private static void DerefGbString(byte strIdx, ushort address)
            {
            unchecked
            {
                byte zone = GbvmZone(address);
                GDVars.gbStrings[strIdx] = "";
    
                if (zone == 1 && address == C_Gbvm.CHAR_NAME)
            {
                    GDVars.gbStrings[strIdx] = GDVars.curChar.name;
                }
                else if (zone < 4)
            {
                    ushort last = ZoneLast(zone);
                    ushort a = address;
                    while (true)
                    {
                        ushort c;
                        switch (zone)
                        {
                            case 0: c = GDVars.zone0.words[a]; break;
                            case 1: c = GDVars.zone1[a]; break;
                            case 2: c = GDVars.zone2[a]; break;
                            case 3: c = GDVars.zone3[a]; break;
                            default: c = 0; break;
                        }
    
                        if (c != 0)
                            GDVars.gbStrings[strIdx] += (char)unchecked((byte)c);
    
                        a = (ushort)(a + 1);
                        if (c == 0 || a > last || GDVars.gbStrings[strIdx].Length == 255)
                            break;
                    }
                }
                        }
            }
    
            public static void SkipEclCmd()
            {
            unchecked
            {
            unchecked
            {
                    GDVars.curOpcode = GDVars.zone3[GDVars.eclIp];
                    switch (GDVars.curOpcode)
                    {
                        case C_Ecl.JUMP: case C_Ecl.GOSUB: case C_Ecl.LOAD_CHAR: case C_Ecl.PICTURE:
                        case C_Ecl.PRINT: case C_Ecl.PRINTCLEAR: case C_Ecl.PARTY_STR: case C_Ecl.NEWECL:
                        case C_Ecl.CALL: case C_Ecl.FIND_ITEM: case C_Ecl.CLOCK: case C_Ecl.RUN_PROGRAM:
                        case C_Ecl.WHO: case C_Ecl.PROTECTION: LoadEclVars(1); break;
                        case C_Ecl.COMPARE: case C_Ecl.RANDOM_NUMBER: case C_Ecl.SAVE: case C_Ecl.INPUT_NUM:
                        case C_Ecl.INPUT_STR: case 0x1F: case C_Ecl.PARTY_SURPR: case C_Ecl.ADD_NPC: LoadEclVars(2); break;
                        case C_Ecl.ADD: case C_Ecl.SUBTRACT: case C_Ecl.DIVIDE: case C_Ecl.MULTIPLY:
                        case C_Ecl.LOAD_MON: case C_Ecl.LOADFILES: case C_Ecl.ROB: case C_Ecl.GETTABLE:
                        case C_Ecl.BIT_AND: case C_Ecl.BIT_OR: case C_Ecl.SAVE_TABLE: case C_Ecl.LOADPIECES:
                        case C_Ecl.SPELL: case C_Ecl.SETUP_MON: LoadEclVars(3); break;
                        case C_Ecl.VMENU:
                            LoadEclVars(3); byte n = (byte)GetEclVar(3); GDVars.eclIp--; LoadEclVars(n); break;
                        case C_Ecl.HMENU: case C_Ecl.ONGOTO: case C_Ecl.ONGOSUB:
                            LoadEclVars(2); byte m = (byte)GetEclVar(2); GDVars.eclIp--; LoadEclVars(m); break;
                        case C_Ecl.COMPARE_AND: case C_Ecl.SURPRISE: LoadEclVars(4); break;
                        case C_Ecl.DAMAGE: LoadEclVars(5); break;
                        case C_Ecl.CHECK_PARTY: case C_Ecl.PARLAY: LoadEclVars(6); break;
                        case C_Ecl.TREASURE: LoadEclVars(8); break;
                        case C_Ecl.ENCOUNTER: LoadEclVars(14); break;
                        default: GDVars.eclIp++; break;
                    }
                }
                        }
            }
        }
    }
