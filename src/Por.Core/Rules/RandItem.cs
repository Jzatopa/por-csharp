using System;

namespace Por.Core
{
    public static partial class RandItem
    {
        private static sbyte RandomBonus()
        {
            byte r = Dice.RollDice(1, 20);
            if (r >= 1 && r <= 14)
                return 1;
            if (r >= 15 && r <= 19)
                return 2;
            return 3;
        }

        public static void ApplyTemplate(Types.TItemRec item, byte template)
        {
            Types.TRandomItemTemplate tpl = GDConsts.randomItemTemplates[template];

            for (byte j = 1; j <= 3; j++)
            {
                item.nameIds[j] = unchecked((byte)(tpl.nameIds[j] & 0xFF));
            }

            item.plus = 1;
            item.saveBonus = 1;

            if (template == 13)
            {
                item.plus = 2;
                item.saveBonus = 2;
            }

            item.weight = tpl.weight;
            item.quantity = 0;
            item.value = tpl.value;

            for (byte j = 1; j <= 3; j++)
            {
                item.spells[j] = unchecked((byte)(tpl.properties[j] & 0xFF));
            }
        }

        public static void GenerateRandomItem(byte kind, Types.TItemRec item)
        {
            // Clear the whole item record
            item.name = string.Empty;
            item.inUse = 0;
            item.next = null;
            item.itemType = 0;
            for (int i = 0; i < 4; i++) item.nameIds[i] = 0;
            item.plus = 0;
            item.saveBonus = 0;
            item.equipped = 0;
            item.unidMask = 0;
            item.cursed = 0;
            item.weight = 0;
            item.quantity = 0;
            item.value = 0;
            for (int i = 0; i < 3; i++) item.variantData[i] = 0;

            byte template = 0;

            // with item^ do begin
            // FillChar(spells, SizeOf(spells), 0);
            for (int i = 1; i <= 3; i++) item.spells[i] = 0;

            item.next = null;
            item.equipped = 0;
            item.unidMask = 6;
            item.cursed = 0;
            item.itemType = kind;

            // FillChar(nameIds, SizeOf(nameIds), 0);
            for (int i = 0; i < 4; i++) item.nameIds[i] = 0;

            byte r;
            byte n;
            byte j;
            byte spell;

            switch (item.itemType)
            {
                case 1: case 2: case 3: case 4: case 5: case 6: case 7: case 8: case 9: case 10:
                case 11: case 12: case 13: case 14: case 15: case 16: case 17: case 18: case 19:
                case 20: case 21: case 22: case 23: case 24: case 25: case 26: case 27: case 28:
                case 29: case 30: case 31: case 32: case 33: case 34: case 35: case 36: case 37:
                case 38: case 39: case 40: case 41: case 42: case 43: case 44: case 45: case 46:
                case 47: case 48: case 49: case 50: case 51: case 52: case 53: case 54: case 55:
                case 56: case 57: case 58: case 59:
                case 73:
                case 77:
                case 93:
                    {
                        sbyte plus = RandomBonus();
                        item.plus = plus;

                        switch (item.itemType)
                        {
                            case 21:
                                {
                                    r = Dice.RollDice(1, 5);
                                    if (r == 5)
                                        template = 14;
                                    item.nameIds[3] = 21;
                                    item.nameIds[2] = unchecked((byte)(plus + C_Item.NAME_PLUS_BASE));
                                }
                                break;
                            case 28:
                                {
                                    item.nameIds[3] = 28;
                                    item.nameIds[2] = unchecked((byte)(plus + C_Item.NAME_PLUS_BASE));
                                }
                                break;
                            case 50:
                            case 51:
                                {
                                    item.nameIds[3] = item.itemType;
                                    item.nameIds[2] = 49;
                                    item.nameIds[1] = unchecked((byte)(plus + C_Item.NAME_PLUS_BASE));
                                    item.unidMask = 4;
                                }
                                break;
                            case 52:
                                {
                                    item.nameIds[3] = item.itemType;
                                    item.nameIds[2] = 50;
                                    item.nameIds[1] = unchecked((byte)(plus + C_Item.NAME_PLUS_BASE));
                                    item.unidMask = 4;
                                }
                                break;
                            case 53: case 54: case 55: case 56: case 57: case 58:
                                {
                                    item.nameIds[3] = item.itemType;
                                    item.nameIds[2] = 48;
                                    item.nameIds[1] = unchecked((byte)(plus + C_Item.NAME_PLUS_BASE));
                                    item.unidMask = 4;
                                }
                                break;
                            case 73:
                                {
                                    item.nameIds[3] = 61;
                                    item.nameIds[2] = unchecked((byte)(plus + C_Item.NAME_PLUS_BASE));
                                }
                                break;
                            case 77:
                                {
                                    item.nameIds[3] = 79;
                                    item.nameIds[2] = (byte)C_Item.NAME_OF;
                                    plus = unchecked((sbyte)(plus * 2 + 2));
                                    item.plus = plus;
                                    switch (plus)
                                    {
                                        case 4: item.nameIds[1] = 221; break;
                                        case 6: item.nameIds[1] = 222; break;
                                        case 8: item.nameIds[1] = 220; break;
                                    }
                                }
                                break;
                            case 93:
                                {
                                    item.nameIds[3] = 66;
                                    item.nameIds[2] = 224;
                                    item.nameIds[1] = unchecked((byte)(plus + C_Item.NAME_PLUS_BASE));
                                }
                                break;
                            default:
                                {
                                    item.nameIds[3] = item.itemType;
                                    item.nameIds[2] = unchecked((byte)(plus + C_Item.NAME_PLUS_BASE));
                                }
                                break;
                        }

                        item.saveBonus = 0;
                        item.quantity = 0;

                        // weight in coins
                        switch (item.itemType)
                        {
                            case 1: case 13: case 14: case 35:
                                item.weight = 75;
                                break;
                            case 2: case 20: case 29: case 31: case 32: case 33: case 39: case 42: case 44: case 46: case 59:
                                item.weight = 50;
                                break;
                            case 3: case 24: case 40:
                                item.weight = 125;
                                break;
                            case 4: case 15: case 23: case 34: case 43: case 45: case 51:
                                item.weight = 100;
                                break;
                            case 5: case 12: case 17: case 19: case 50:
                                item.weight = 150;
                                break;
                            case 6:
                                item.weight = 15;
                                break;
                            case 7:
                                item.weight = 30;
                                break;
                            case 8: case 77:
                                item.weight = 10;
                                break;
                            case 9:
                                item.weight = 5;
                                item.quantity = 5;
                                break;
                            case 28:
                                item.weight = 3;
                                item.quantity = 10;
                                break;
                            case 73:
                                item.weight = 4;
                                item.quantity = 10;
                                break;
                            case 10: case 26: case 36:
                                item.weight = 60;
                                break;
                            case 11: case 16: case 25: case 27: case 41: case 47:
                                item.weight = 80;
                                break;
                            case 18:
                                item.weight = 175;
                                break;
                            case 21:
                                item.weight = 20;
                                break;
                            case 22: case 30:
                                item.weight = 40;
                                break;
                            case 37:
                                item.weight = 35;
                                break;
                            case 38: case 53:
                                item.weight = 250;
                                break;
                            case 52:
                                item.weight = 200;
                                break;
                            case 54: case 56:
                                item.weight = 400;
                                break;
                            case 55:
                                item.weight = 300;
                                break;
                            case 57:
                                item.weight = 350;
                                break;
                            case 58:
                                item.weight = 450;
                                break;
                            // Only 93 reaches this arm; 47 is matched above
                            case 93:
                                item.weight = 1;
                                break;
                            default:
                                item.weight = 40;
                                item.quantity = 10;
                                break;
                        }

                        // value in gp = plus * K
                        switch (item.itemType)
                        {
                            case 59:
                                item.value = unchecked((ushort)(plus * 2500));
                                break;
                            case 9: case 28: case 73:
                                item.value = unchecked((ushort)(plus * 150));
                                break;
                            case 53: case 54:
                                item.value = unchecked((ushort)(plus * 3000));
                                break;
                            case 55: case 56:
                                item.value = unchecked((ushort)(plus * 3500));
                                break;
                            case 57:
                                item.value = unchecked((ushort)(plus * 4000));
                                break;
                            case 58:
                                item.value = unchecked((ushort)(plus * 5000));
                                break;
                            case 77:
                                item.value = unchecked((ushort)(plus * 3000));
                                break;
                            default:
                                item.value = unchecked((ushort)(plus * 2000));
                                break;
                        }
                        break;
                    }
                case 61:
                case 62:
                    {
                        n = Dice.RollDice(1, 3);
                        if (item.itemType == 61)
                            item.nameIds[3] = 209;
                        else
                            item.nameIds[3] = 208;
                        item.nameIds[2] = unchecked((byte)(n + 209));
                        item.nameIds[1] = 0;
                        item.plus = 1;
                        item.weight = 25;
                        item.quantity = 0;
                        item.value = 0;
                        for (j = 1; j <= n; j++)
                        {
                            r = Dice.RollDice(1, 3);
                            if (item.itemType == 61)
                            {
                                switch (r)
                                {
                                    case 1: spell = unchecked((byte)(Dice.RollDice(1, 13) + 8)); break;
                                    case 2: spell = unchecked((byte)(Dice.RollDice(1, 7) + 28)); break;
                                    case 3: spell = unchecked((byte)(Dice.RollDice(1, 11) + 44)); break;
                                    default: spell = 0; break;
                                }
                            }
                            else
                            {
                                switch (r)
                                {
                                    case 1: spell = Dice.RollDice(1, 8); break;
                                    case 2: spell = unchecked((byte)(Dice.RollDice(1, 7) + 21)); break;
                                    case 3: spell = unchecked((byte)(Dice.RollDice(1, 9) + 35)); break;
                                    default: spell = 0; break;
                                }
                            }
                            item.spells[j] = spell;
                            item.value = unchecked((ushort)(item.value + (ushort)(r * 300)));
                        }
                        break;
                    }
                case 63:
                    template = 10;
                    break;
                case 67:
                    template = 19;
                    break;
                case 69:
                    {
                        r = Dice.RollDice(1, 10);
                        switch (r)
                        {
                            case 1: template = 6; break;
                            case 2: case 3: case 4: case 5: case 6: template = 16; break;
                            case 7: case 8: case 9: case 10: template = 17; break;
                        }
                    }
                    break;
                case 78:
                    {
                        // r is 2..4 vs labels 1..3: 49 is never chosen and r = 4 leaves no template
                        r = unchecked((byte)(Dice.RollDice(1, 3) + 1));
                        switch (r)
                        {
                            case 1: template = 7; break;
                            case 2: template = 9; break;
                            case 3: template = 15; break;
                        }
                    }
                    break;
                case 79:
                    template = 8;
                    break;
                case 84:
                    template = 2;
                    break;
                case 92:
                    template = 13;
                    break;
                case 71:
                    {
                        // 11 makes nothing
                        r = Dice.RollDice(1, 11);
                        switch (r)
                        {
                            case 1: case 2: case 3: case 4: case 5: template = 3; break;
                            case 6: case 7: case 8: template = 1; break;
                            case 9: template = 4; break;
                            case 10: template = 11; break;
                        }
                    }
                    break;
                case 70:
                    {
                        r = Dice.RollDice(1, 10);
                        switch (r)
                        {
                            case 1: case 2: case 3: case 4: case 5: case 6: case 7: template = 12; break;
                            case 8: case 9: case 10: template = 18; break;
                        }
                    }
                    break;
            }

            if (template != 0)
                ApplyTemplate(item, template);
        }
    }
}
