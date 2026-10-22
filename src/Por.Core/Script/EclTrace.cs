namespace Por.Core
{
    public static class EclTrace
    {
        public static string OpName(byte op)
        {
            switch (op)
            {
                case C_Ecl.EXIT_SCRIPT: return "EXIT";
                case C_Ecl.JUMP: return "GOTO";
                case C_Ecl.GOSUB: return "GOSUB";
                case C_Ecl.COMPARE: return "COMPARE";
                case C_Ecl.ADD: return "ADD";
                case C_Ecl.SUBTRACT: return "SUBTRAT";
                case C_Ecl.DIVIDE: return "DIVIDE";
                case C_Ecl.MULTIPLY: return "MULTIPLY";
                case C_Ecl.RANDOM_NUMBER: return "RANDOM";
                case C_Ecl.SAVE: return "SAVE";
                case C_Ecl.LOAD_CHAR: return "LOAD CHARACTER";
                case C_Ecl.LOAD_MON: return "LOAD MONSTER";
                case C_Ecl.SETUP_MON: return "SETUP MONSTER";
                case C_Ecl.APPROACH: return "APPROACH";
                case C_Ecl.PICTURE: return "PICTURE";
                case C_Ecl.INPUT_NUM: return "INPUT NUMBER";
                case C_Ecl.INPUT_STR: return "INPUT STRING";
                case C_Ecl.PRINT: return "PRINT";
                case C_Ecl.PRINTCLEAR: return "PRINTCLEAR";
                case C_Ecl.RETURN: return "RETURN";
                case C_Ecl.COMPARE_AND: return "COMPARE AND";
                case C_Ecl.VMENU: return "VERTICAL MENU";
                case C_Ecl.IF_EQ: return "IF = ";
                case C_Ecl.IF_NE: return "IF <>";
                case C_Ecl.IF_LT: return "IF <";
                case C_Ecl.IF_GT: return "IF >";
                case C_Ecl.IF_LE: return "IF <=";
                case C_Ecl.IF_GE: return "IF >=";
                case C_Ecl.CLEAR_MON: return "CLEARMONSTERS";
                case C_Ecl.PARTY_STR: return "PARTYSTRENGTH";
                case C_Ecl.CHECK_PARTY: return "CHECKPARTY";
                case C_Ecl.NEWECL: return "NEWECL";
                case C_Ecl.LOADFILES: return "LOAD FILES";
                case C_Ecl.LOADPIECES: return "LOAD PIECES";
                case C_Ecl.PARTY_SURPR: return "PARTY SURPRISE";
                case C_Ecl.SURPRISE: return "SURPRISE";
                case C_Ecl.START_COMBAT: return "COMBAT";
                case C_Ecl.ONGOTO: return "ON GOTO";
                case C_Ecl.ONGOSUB: return "ON GOSUB";
                case C_Ecl.TREASURE: return "TREASURE";
                case C_Ecl.ROB: return "ROB";
                case C_Ecl.ENCOUNTER: return "ENCOUNTER MENU";
                case C_Ecl.GETTABLE: return "GETTABLE";
                case C_Ecl.HMENU: return "HORIZONTAL MENU";
                case C_Ecl.PARLAY: return "PARLAY";
                case C_Ecl.CALL: return "CALL";
                case C_Ecl.DAMAGE: return "DAMAGE";
                case C_Ecl.BIT_AND: return "AND";
                case C_Ecl.BIT_OR: return "OR";
                case C_Ecl.SPRITE_OFF: return "SPRITE OFF";
                case C_Ecl.FIND_ITEM: return "FIND ITEM";
                case C_Ecl.PRINT_RET: return "PRINT RETURN";
                case C_Ecl.CLOCK: return "ECL CLOCK";
                case C_Ecl.SAVE_TABLE: return "SAVE TABLE";
                case C_Ecl.ADD_NPC: return "ADD NPC";
                case C_Ecl.RUN_PROGRAM: return "PROGRAM";
                case C_Ecl.WHO: return "WHO";
                case C_Ecl.PAUSE: return "DELAY";
                case C_Ecl.SPELL: return "SPELL";
                case C_Ecl.PROTECTION: return "PROTECTION";
                case C_Ecl.CLEAR_BOX: return "CLEAR BOX";
                default: return "?";
            }
        }
    }
}
