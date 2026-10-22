// FILE: src/Por.Core/Script/EvtKind.cs
// What happens on an event square (the Companion Tools' map), read from the loaded area script without running it.
// C# conversion of evtkind.pas (pinned source 7616920). Authored by Claude on the user's request, as for EclVm.
// Its one unported dependency, EclVars (CombineBytesToWord, SkipEclCmd), is bound through EvtKind.EclVarsUnit and
// fails clearly when unbound. Widths, wraps, short-circuit order and the DOS-heap visited bitmap follow the source.
using System;

namespace Por.Core
{
    public static class EvtKind
    {
        public const byte KIND_NONE = 0;
        // an event whose kind is not known
        public const byte KIND_EVENT = 1;
        public const byte KIND_MESSAGE = 2;
        public const byte KIND_TRAP = 3;
        public const byte KIND_TREASURE = 4;
        public const byte KIND_SERVICE = 5;
        public const byte KIND_FIGHT = 6;
        public const byte KIND_EXIT = 7;

        /// <summary>The EclVars unit (not ported yet). Bind the same instance EclVm.Units.EclVars uses.</summary>
        public static IEclVarsUnit EclVarsUnit;

        private static IEclVarsUnit EclVars
        {
            get { return EclVarsUnit ?? throw new NotSupportedException("EvtKind needs unit EclVars, which is not ported or not bound (EvtKind.EclVarsUnit)"); }
        }

        // the variables a scan follows the event byte into
        private const byte MAX_VARS = 8;
        private const byte MAX_DISPATCH = 96;
        // instructions the walk of the whole script queues, and a scan from one read of the event byte
        private const ushort MAX_QUEUE = 512;
        private const ushort MAX_SCAN = 256;
        // instructions followed from an event's address
        private const byte MAX_FOLLOW = 160;

        // TVisited = array[0..(SCRIPT_LAST - SCRIPT_FIRST) div 8] of Byte
        private const int VISITED_BYTES = (C_Gbvm.SCRIPT_LAST - C_Gbvm.SCRIPT_FIRST) / 8 + 1;

        internal struct TDispatch
        {
            // the event byte masked with mask equals value
            public byte mask;
            public byte value;
            public ushort target;
            // a COMPARE's IF = runs one instruction at target: only it is the event's (unless it is a GOTO or GOSUB)
            public bool single;
        }

        internal static byte dispatchCount;
        // [1..MAX_DISPATCH]
        internal static readonly TDispatch[] dispatch = new TDispatch[MAX_DISPATCH + 1];
        // [1..MAX_QUEUE]
        private static readonly ushort[] queue = new ushort[MAX_QUEUE + 1];
        private static ushort queueLen;
        private static byte[] visited;
        // a scan's variables holding the event byte masked with varMasks less varOffsets: bit k of a path's live set
        // is vars[k + 1], and one variable can be there twice with two offsets
        private static byte varCount;
        private static readonly ushort[] vars = new ushort[MAX_VARS + 1];
        private static readonly byte[] varMasks = new byte[MAX_VARS + 1];
        private static readonly byte[] varOffsets = new byte[MAX_VARS + 1];

        private static byte B(int x) { return unchecked((byte)x); }
        private static ushort W(int x) { return unchecked((ushort)x); }
        private static bool IsIf(byte op) { return op >= C_Ecl.IF_EQ && op <= C_Ecl.IF_GE; }

        // DEVIATION: the source reads Operand(i) for every i up to an ON GOTO list's count n (up to 255), but
        // LoadEclVars keeps only the first 64 operands (dsGbvm*[1..64]); past them the DOS build reads whatever
        // globals follow those arrays. The port takes at most the 64 kept addresses, as EclVm's ON GOTO does.
        private static byte Kept(byte n) { return n > 64 ? (byte)64 : n; }

        // Operand i of the instruction last decoded, as an address (a GBVM read) when isAddr.
        private static ushort Operand(byte i, out bool isAddr)
        {
            byte t = GDVars.dsGbvmType[i];
            isAddr = t == 1 || t == 3;
            if (t == 1 || t == 2 || t == 3)
                return EclVars.CombineBytesToWord(GDVars.dsGbvmLowerByte[i], GDVars.dsGbvmUpperByte[i]);
            return GDVars.dsGbvmLowerByte[i];
        }

        // The first operand and the count of the ON GOTO, ON GOSUB or menu at a, read from the script itself: decoding
        // loads the list after them over them. A count read from a variable is not known, so taken as 0.
        private static void ListHead(ushort a, out ushort v, out bool isAddr, out byte n)
        {
            byte t = GDVars.zone3[W(a + 1)];
            isAddr = t == 1 || t == 3;
            v = GDVars.zone3[W(a + 2)];
            a = W(a + 3);
            if (t == 1 || t == 2 || t == 3)
            {
                v = EclVars.CombineBytesToWord(B(v), GDVars.zone3[a]);
                a = W(a + 1);
            }
            n = 0;
            byte ct = GDVars.zone3[a];
            if (ct == 0 || ct == 2)
                n = GDVars.zone3[W(a + 1)];
            if (n > 64) listsCut++;
        }

        // how often a list longer than the 64 kept operands was read (deviation 1); for checks over real scripts
        internal static int listsCut;

        // True when a is inside the script.
        private static bool InScript(ushort a)
        {
            return a >= C_Gbvm.SCRIPT_FIRST && a <= C_Gbvm.SCRIPT_LAST;
        }

        // Decode the instruction at a: its opcode, and the address after it. Leaves its operands in dsGbvm*.
        private static byte Decode(ushort a, out ushort next)
        {
            ushort savedIp = GDVars.eclIp;
            byte savedOp = GDVars.curOpcode;
            GDVars.eclIp = a;
            EclVars.SkipEclCmd();
            byte op = GDVars.curOpcode;
            next = GDVars.eclIp;
            GDVars.eclIp = savedIp;
            GDVars.curOpcode = savedOp;
            return op;
        }

        // The variable the instruction at a, op, writes (decoded last); 0 for none.
        private static ushort WrittenVar(ushort a, byte op)
        {
            bool isAddr;
            switch (op)
            {
                case C_Ecl.SAVE: case C_Ecl.RANDOM_NUMBER: case C_Ecl.INPUT_NUM: case C_Ecl.INPUT_STR:
                    return Operand(2, out isAddr);
                case C_Ecl.DIVIDE: case C_Ecl.MULTIPLY: case C_Ecl.GETTABLE: case C_Ecl.BIT_AND: case C_Ecl.BIT_OR:
                    return Operand(3, out isAddr);
                case C_Ecl.VMENU: case C_Ecl.HMENU:
                    ushort v; byte n;
                    ListHead(a, out v, out isAddr, out n);
                    return v;
            }
            return 0;
        }

        // Record that the event byte masked with mask equal to value runs target, once.
        private static void AddDispatch(byte mask, byte value, ushort target, bool single)
        {
            if (!InScript(target)) return;
            value = B(value & mask);
            for (int i = 1; i <= dispatchCount; i++)
                if (dispatch[i].mask == mask && dispatch[i].value == value && dispatch[i].target == target) return;
            if (dispatchCount < MAX_DISPATCH)
            {
                dispatchCount++;
                dispatch[dispatchCount] = new TDispatch { mask = mask, value = value, target = target, single = single };
            }
        }

        // The bit of variable v less offset among the scan's, adding it with mask when new; 0 when it is not one and
        // none is added (mask 0, or the table full).
        private static byte VarBit(ushort v, byte mask, byte offset)
        {
            for (int k = 1; k <= varCount; k++)
                if (vars[k] == v && (mask == 0 || varOffsets[k] == offset))
                    return B(1 << (k - 1));
            if (mask != 0 && varCount < MAX_VARS)
            {
                varCount++;
                vars[varCount] = v;
                varMasks[varCount] = mask;
                varOffsets[varCount] = offset;
                return B(1 << (varCount - 1));
            }
            return 0;
        }

        // The live set's bits for variable v, whatever its offset.
        private static byte Bits(ushort v, byte live)
        {
            byte b = 0;
            for (int k = 1; k <= varCount; k++)
                if (vars[k] == v)
                    b = B(b | (1 << (k - 1)));
            return B(b & live);
        }

        // True when v is live, with its mask and offset (the last live entry's; unchanged when none).
        private static bool LiveVar(ushort v, byte live, ref byte mask, ref byte offset)
        {
            bool result = false;
            for (int k = 1; k <= varCount; k++)
                if (vars[k] == v && (live & (1 << (k - 1))) != 0)
                {
                    mask = varMasks[k];
                    offset = varOffsets[k];
                    result = true;
                }
            return result;
        }

        // Follow the script from start, where the variables of live hold the event byte, recording its dispatches.
        // See evtkind.pas for the rules (ON GOTO/ON GOSUB lists, COMPARE + IF =, variables holding the event, ADD and
        // SUBTRACT moving an offset, writes ending a variable's holding).
        private static void Scan(ushort start, byte live)
        {
            var todo = new ushort[MAX_SCAN + 1];
            var todoLive = new byte[MAX_SCAN + 1];
            ushort todoLen = 0;
            ushort next, afterIf, v1, v2, w;
            byte op, n;
            bool isAddr, isAddr2, isAddr3;
            byte mask = 0, offset = 0;

            void Follow(ushort t, byte l)
            {
                if (!InScript(t) || l == 0 || todoLen >= MAX_SCAN) return;
                for (int k = 1; k <= todoLen; k++)
                    if (todo[k] == t) return;
                todoLen++;
                todo[todoLen] = t;
                todoLive[todoLen] = l;
            }

            Follow(start, live);
            ushort head = 1;
            while (head <= todoLen)
            {
                ushort a = todo[head];
                live = todoLive[head];
                head++;
                op = Decode(a, out next);
                switch (op)
                {
                    case C_Ecl.EXIT_SCRIPT: case C_Ecl.RETURN: case C_Ecl.NEWECL:
                        next = 0;
                        break;
                    case C_Ecl.JUMP:
                        Follow(Operand(1, out isAddr), live);
                        next = 0;
                        break;
                    case C_Ecl.ONGOTO: case C_Ecl.ONGOSUB:
                        ListHead(a, out v1, out isAddr, out n);
                        if (isAddr && LiveVar(v1, live, ref mask, ref offset))
                            for (int i = 1; i <= Kept(n); i++)
                                AddDispatch(mask, B(i - 1 + offset), Operand(B(i), out isAddr2), false);
                        if (op == C_Ecl.ONGOTO)
                            next = 0;
                        break;
                    case C_Ecl.COMPARE:
                        v1 = Operand(1, out isAddr);
                        v2 = Operand(2, out isAddr2);
                        mask = 0;
                        // short-circuit order matters: LiveVar sets mask and offset when it finds a live variable
                        if (!(isAddr && LiveVar(v1, live, ref mask, ref offset)) && isAddr2 &&
                            LiveVar(v2, live, ref mask, ref offset))
                        {
                            v2 = v1;
                            isAddr2 = isAddr;
                        }
                        if (mask != 0 && Decode(next, out afterIf) == C_Ecl.IF_EQ)
                        {
                            if (isAddr2)
                                // past the IF, the other variable holds it too
                                Follow(afterIf, B(live | VarBit(v2, mask, offset)));
                            else
                                AddDispatch(mask, B(B(v2) + offset), afterIf, true);
                        }
                        break;
                    case C_Ecl.ADD: case C_Ecl.SUBTRACT:
                        // a number added to or taken from a variable in place moves its offset; anything else written
                        // ends it
                        v1 = Operand(1, out isAddr);
                        v2 = Operand(2, out isAddr2);
                        w = Operand(3, out isAddr3);
                        if (!isAddr && isAddr2 && v2 == w && LiveVar(w, live, ref mask, ref offset))
                        {
                            if (op == C_Ecl.ADD)
                                offset = B(offset - B(v1));
                            else
                                offset = B(offset + B(v1));
                            live = B((live & ~Bits(w, live)) | VarBit(w, mask, offset));
                        }
                        else
                            live = B(live & ~Bits(w, live));
                        break;
                    default:
                        if (IsIf(op))
                        {
                            Decode(next, out afterIf);
                            Follow(afterIf, live);
                        }
                        else
                        {
                            w = WrittenVar(a, op);
                            if (w != 0)
                                live = B(live & ~Bits(w, live));
                        }
                        break;
                }
                if (next != 0)
                    Follow(next, live);
            }
        }

        // Queue a for the walk, once.
        private static void Push(ushort a)
        {
            if (!InScript(a)) return;
            int k = a - C_Gbvm.SCRIPT_FIRST;
            if ((visited[k / 8] & (1 << (k % 8))) != 0) return;
            visited[k / 8] = B(visited[k / 8] | (1 << (k % 8)));
            if (queueLen < MAX_QUEUE)
            {
                queueLen++;
                queue[queueLen] = a;
            }
        }

        /// <summary>
        /// Read the loaded area script's event dispatch: walk its control flow from the five entry points, scanning on
        /// from each instruction that reads the event byte (an AND of it into a variable, or a COMPARE of it).
        /// Call it once the area's script is loaded, before Kind.
        /// </summary>
        public static void Prepare()
        {
            ushort next, afterIf, v1, v2;
            byte op, n, eventBit;
            bool isAddr, isAddr2;
            dispatchCount = 0;
            queueLen = 0;
            visited = new byte[VISITED_BYTES];
            for (int i = 1; i <= 5; i++)
                Push(GDVars.scriptEntryAddrs[i]);
            ushort head = 1;
            while (head <= queueLen)
            {
                ushort a = queue[head];
                head++;
                op = Decode(a, out next);
                switch (op)
                {
                    case C_Ecl.EXIT_SCRIPT: case C_Ecl.RETURN: case C_Ecl.NEWECL:
                        // the path ends
                        break;
                    case C_Ecl.JUMP:
                        Push(Operand(1, out isAddr));
                        break;
                    case C_Ecl.GOSUB:
                        Push(Operand(1, out isAddr));
                        Push(next);
                        break;
                    case C_Ecl.ONGOTO: case C_Ecl.ONGOSUB:
                        ListHead(a, out v1, out isAddr, out n);
                        for (int i = 1; i <= Kept(n); i++)
                            Push(Operand(B(i), out isAddr2));
                        if (op == C_Ecl.ONGOSUB)
                            Push(next);
                        break;
                    default:
                        if (IsIf(op))
                        {
                            // the next instruction runs or is skipped
                            Push(next);
                            Decode(next, out afterIf);
                            Push(afterIf);
                            break;
                        }
                        if (op == C_Ecl.BIT_AND || op == C_Ecl.COMPARE)
                        {
                            v1 = Operand(1, out isAddr);
                            v2 = Operand(2, out isAddr2);
                            if ((isAddr && v1 == C_Gbvm.CELL_EVENT) || (isAddr2 && v2 == C_Gbvm.CELL_EVENT))
                            {
                                // a fresh set of variables for each scan
                                varCount = 0;
                                eventBit = VarBit(C_Gbvm.CELL_EVENT, 0xFF, 0);
                                if (op == C_Ecl.COMPARE)
                                    Scan(a, eventBit);
                                else
                                {
                                    // AND a, b, destination: the event byte and a mask, either way round
                                    if (v1 == C_Gbvm.CELL_EVENT)
                                        v1 = v2;
                                    Scan(next, B(eventBit | VarBit(Operand(3, out isAddr), B(v1), 0)));
                                }
                            }
                        }
                        Push(next);
                        break;
                }
            }
            visited = null;
        }

        // The kind of the code at a: its control flow followed for at most MAX_FOLLOW instructions, each path ending at
        // EXIT, RETURN or NEWECL; with single, the instruction at a alone unless it is a GOTO or GOSUB. The strongest
        // found wins: area change, service, fight, treasure, trap, message.
        internal static byte KindAt(ushort a, bool single)
        {
            var todo = new ushort[MAX_FOLLOW + 1];
            byte todoLen = 0;
            ushort next, afterIf, v;
            byte op, n;
            bool isAddr, isAddr2;
            bool sawExit = false, sawService = false, sawFight = false, sawTreasure = false, sawTrap = false,
                sawMessage = false;

            void Follow(ushort t)
            {
                if (!InScript(t) || todoLen >= MAX_FOLLOW) return;
                for (int k = 1; k <= todoLen; k++)
                    if (todo[k] == t) return;
                todoLen++;
                todo[todoLen] = t;
            }

            Follow(a);
            byte head = 1;
            while (head <= todoLen)
            {
                a = todo[head];
                head++;
                op = Decode(a, out next);
                switch (op)
                {
                    case C_Ecl.EXIT_SCRIPT: case C_Ecl.RETURN:
                        next = 0;
                        break;
                    case C_Ecl.NEWECL:
                        sawExit = true;
                        next = 0;
                        break;
                    case C_Ecl.START_COMBAT: case C_Ecl.ENCOUNTER: case C_Ecl.LOAD_MON:
                        sawFight = true;
                        break;
                    case C_Ecl.RUN_PROGRAM:
                        sawService = true;
                        break;
                    case C_Ecl.SAVE:
                        // a shop's or temple's flag: the COMBAT after it opens the shop
                        v = Operand(2, out isAddr);
                        if (isAddr && (v == C_Gbvm.ENTER_SHOP || v == C_Gbvm.ENTER_TEMPLE))
                            sawService = true;
                        break;
                    case C_Ecl.TREASURE:
                        sawTreasure = true;
                        break;
                    case C_Ecl.DAMAGE:
                        sawTrap = true;
                        break;
                    case C_Ecl.PRINT: case C_Ecl.PRINTCLEAR: case C_Ecl.PRINT_RET:
                        sawMessage = true;
                        break;
                    case C_Ecl.JUMP:
                        Follow(Operand(1, out isAddr));
                        next = 0;
                        break;
                    case C_Ecl.GOSUB:
                        Follow(Operand(1, out isAddr));
                        break;
                    case C_Ecl.ONGOTO: case C_Ecl.ONGOSUB:
                        ListHead(a, out v, out isAddr, out n);
                        for (int i = 1; i <= Kept(n); i++)
                            Follow(Operand(B(i), out isAddr2));
                        if (op == C_Ecl.ONGOTO)
                            next = 0;
                        break;
                    default:
                        if (IsIf(op))
                        {
                            // as in the source, op becomes the opcode after the IF, which the single test below reads
                            op = Decode(next, out afterIf);
                            Follow(afterIf);
                        }
                        break;
                }
                if (single && head == 2 && !(op == C_Ecl.JUMP || op == C_Ecl.GOSUB))
                    next = 0;
                if (next != 0)
                    Follow(next);
            }
            if (sawExit) return KIND_EXIT;
            if (sawService) return KIND_SERVICE;
            if (sawFight) return KIND_FIGHT;
            if (sawTreasure) return KIND_TREASURE;
            if (sawTrap) return KIND_TRAP;
            if (sawMessage) return KIND_MESSAGE;
            return KIND_NONE;
        }

        /// <summary>The kind of a square whose event byte is event; KIND_NONE for 0, and for an event whose code shows
        /// nothing; KIND_EVENT when no dispatch is found for it.</summary>
        public static byte Kind(byte @event)
        {
            if (@event == 0) return KIND_NONE;
            // an event no dispatch is found for is one whose kind is not known
            byte k = KIND_EVENT;
            bool found = false;
            for (int i = 1; i <= dispatchCount; i++)
                if ((@event & dispatch[i].mask) == dispatch[i].value)
                {
                    byte t = KindAt(dispatch[i].target, dispatch[i].single);
                    if (!found || t > k)
                        k = t;
                    found = true;
                }
            return k;
        }
    }
}
