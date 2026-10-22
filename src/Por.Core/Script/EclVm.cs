// FILE: src/Por.Core/Script/EclVm.cs
// Main game loop and ECL script interpreter: C# conversion of eclvm.pas (pinned source 7616920).
// Authored by Claude as a supervised exception to the Qwen-first route (user request: "take the next pass yourself on
// the hardest file"). Routines keep the Pascal names, order and integer widths; Word/Byte wrap is explicit (unchecked).
// Calls into units that are not ported yet go through EclVmUnits (see EclVmUnits.cs) and fail clearly when unbound.
// Deviations from the source are marked DEVIATION and listed in docs/ECLVM-2026-10-21.md.
using System;
using System.Reflection;

namespace Por.Core
{
    public static class EclVm
    {
        // ---- unit bindings (unported dependencies) ----

        /// <summary>Bindings for the units eclvm.pas uses that are not ported yet.</summary>
        public static EclVmUnits Units = new EclVmUnits();

        private static Exception Missing(string unit)
        {
            return new NotSupportedException("EclVm needs unit " + unit + ", which is not ported or not bound (EclVm.Units)");
        }

        private static IEclVarsUnit EclVars { get { return Units.EclVars ?? throw Missing("EclVars"); } }
        private static IEclMenuUnit EclMenu { get { return Units.EclMenu ?? throw Missing("EclMenu"); } }
        private static ISaveLoadUnit SaveLoad { get { return Units.SaveLoad ?? throw Missing("SaveLoad"); } }
        private static ICharDispUnit CharDisp { get { return Units.CharDisp ?? throw Missing("CharDisp"); } }
        private static ICharHlpUnit CharHlp { get { return Units.CharHlp ?? throw Missing("CharHlp"); } }
        private static IFxEngineUnit FxEngine { get { return Units.FxEngine ?? throw Missing("FxEngine"); } }
        private static IExploreUnit Explore { get { return Units.Explore ?? throw Missing("Explore"); } }
        private static IEclVmGameUnits Game
        {
            get { return Units.Game ?? throw Missing("Shop/VaultMnu/Temple/PostCbt/Combat/CharGen/Ending/Camp/MapView/SoundDrv"); }
        }

        // Crt colours
        private const byte LightGreen = 10;
        private const byte LightMagenta = 13;
        private const byte White = 15;

        // ---- unit variables ----

        // stop the VM and return control to the player
        internal static byte eclStop;
        // NEWECL ran; the game loop then calls NewEclDone
        internal static byte newEclFlag;
        // LOAD CHARACTER ran; restore curChar from savedChar
        internal static byte restoreChar;
        // LOADFILES or LOADPIECES ran (not that loading succeeded)
        internal static byte filesLoaded;
        // first LOADFILES/LOADPIECES pair still to draw the interface
        internal static byte enteringLoop;
        // LOADPIECES ran (not that loading succeeded)
        internal static byte piecesLoaded;
        // LOADFILES ran (not that loading succeeded)
        internal static byte mapLoaded;
        // monsters added by LOAD MONSTER since CLEAR MONSTERS (max 63)
        internal static byte creatureCnt;
        // curChar backup
        internal static Types.TCharRecord savedChar;
        // Current monster ID
        internal static byte monId;
        // Current monster picture ID
        internal static byte monPicId;
        // Creature list contains hostile monsters flag
        internal static byte monstersOn;

        // LOAD MONSTER adds no monsters beyond this count
        public const byte MAX_LOADED_MONSTERS = 63;
        // the last combat-icon slot (GDVars.dsIconAnimation is [0..25]); LOAD MONSTER adds no monsters past it
        public const byte LAST_ICON_SLOT = 25;

        // The WHO prompts that choose who duels, by script and the WHO's address, and where Escape on them sends the
        // script: back to the PRINTCLEAR that asks the duel's question again (#661). SSI's WHO has no way out.
        public const byte DUEL_WHOS = 3;
        private struct DuelWho { public byte script; public ushort at; public ushort back; }
        // [1..DUEL_WHOS]; index 0 unused
        private static readonly DuelWho[] DUEL_WHO =
        {
            default(DuelWho),
            // the New Phlan training hall's arena: "Do you duel?"
            new DuelWho { script = 11, at = 0x9C72, back = 0x9C05 },
            // the Pitt's tavern buccaneer: the Challenge Attack Flee Bribe menu
            new DuelWho { script = 18, at = 0xA985, back = 0xA90D },
            // the Lizardman Keep champion: "Will you champion me?"
            new DuelWho { script = 16, at = 0xA300, back = 0xA287 },
        };

        // training hall (class bitmask in C_Gbvm.TRAIN_CLASS_MASK)
        public const byte PROG_TRAINING = 0;
        public const byte PROG_WIN_GAME = 8;
        public const byte PROG_CAMP = 9;
        // The ids of the three wilderness scripts (script ids, not opcodes)
        public const byte WILD_WEST_SCRIPT = 25;
        public const byte WILD_CENTRAL_SCRIPT = 26;
        public const byte WILD_EAST_SCRIPT = 27;

        // ---- helpers for Pascal semantics ----

        private static byte B(int x) { return unchecked((byte)x); }
        private static ushort W(int x) { return unchecked((ushort)x); }
        private static sbyte SB(int x) { return unchecked((sbyte)x); }

        // Assignment to a String[cap] keeps the first cap characters.
        private static string Short(string s, int cap) { return s.Length > cap ? s.Substring(0, cap) : s; }

        // System.UpCase: only 'a'..'z' change.
        private static char UpCase(char c) { return c >= 'a' && c <= 'z' ? (char)(c - 32) : c; }

        // System.Random(n) for n >= 1: 0..n-1, from the same generator Dice uses.
        private static int Random(int n)
        {
            if (Dice.RandomBelow == null) throw new InvalidOperationException("RandomBelow is not set.");
            int r = Dice.RandomBelow(n);
            if (r < 0 || r >= n) throw new InvalidOperationException("RandomBelow returned an out-of-bounds value.");
            return r;
        }

        // Crt.Delay
        private static void Delay(ushort ms)
        {
            if (TextRend.DelayMilliseconds == null) throw new NotSupportedException("Delay host missing.");
            TextRend.DelayMilliseconds(ms);
        }

        private static ushort VarAddress(int i)
        {
            return EclVars.CombineBytesToWord(GDVars.dsGbvmLowerByte[i], GDVars.dsGbvmUpperByte[i]);
        }

        private static void ClearMonsterFlags()
        {
            GDVars.dsMonsterFlags[0] = false;
            GDVars.dsMonsterFlags[1] = false;
        }

        private static void ClearCmpFlags()
        {
            for (int i = 0; i < GDVars.cmpFlags.Length; i++) GDVars.cmpFlags[i] = false;
        }

        /// <summary>
        /// Pascal record assignment (dest^ := src^) for TCharRecord: every field by value. Arrays are copied element by
        /// element; pointer fields (items, effects, next, combat, equipped slots) copy the pointer, not the target.
        /// The variant-record views are per-instance accessors over the copied fields and are not copied.
        /// </summary>
        internal static void CopyCharRecord(Types.TCharRecord src, Types.TCharRecord dest)
        {
            foreach (FieldInfo f in typeof(Types.TCharRecord).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                Type t = f.FieldType;
                object v = f.GetValue(src);
                if (t == typeof(Types.ByteView) || t == typeof(Types.ByteGridView)) continue;
                if (t.IsArray) f.SetValue(dest, v == null ? null : ((Array)v).Clone());
                else if (t == typeof(Types.TSpellSlots))
                    f.SetValue(dest, new Types.TSpellSlots((byte[])((Types.TSpellSlots)v).Data.Clone()));
                else if (t.IsPrimitive || t == typeof(string) || t == typeof(Types.TCharRecord) ||
                         t == typeof(Types.TItemRec) || t == typeof(Types.TEffectRec) || t == typeof(Types.TCombatRec))
                    f.SetValue(dest, v);
                else
                    throw new NotSupportedException("CopyCharRecord: no copy rule for field " + f.Name + " of " + t);
            }
        }

        // SizeOf(Types.TItemRec): TP packs record fields. name String[40] 0..40, inUse 41, next 42..45, itemType 46,
        // nameIds 47..49, plus 50, saveBonus 51, equipped 52, unidMask 53, cursed 54, weight 55..56, quantity 57,
        // value 58..59, variant 60..62.
        public const int ITEM_REC_SIZE = 63;

        /// <summary>Move(data^[offset], it, SizeOf(TItemRec)): decode one packed item record (CP437 bytes as chars).</summary>
        internal static Types.TItemRec ItemFromBytes(byte[] data, int offset)
        {
            var it = new Types.TItemRec();
            int len = data[offset];
            var name = new char[len];
            for (int k = 0; k < len; k++) name[k] = (char)data[offset + 1 + k];
            // a stored length above 40 cannot occur in a String[40]; the bytes are taken as they are, as Move does
            it.name = new string(name);
            it.inUse = data[offset + 41];
            // next (42..45) is a stale DOS pointer; PrependItemCopy sets the copy's own next
            it.next = null;
            it.itemType = data[offset + 46];
            for (int k = 1; k <= 3; k++) it.nameIds[k] = data[offset + 46 + k];
            it.plus = SB(data[offset + 50]);
            it.saveBonus = SB(data[offset + 51]);
            it.equipped = data[offset + 52];
            it.unidMask = data[offset + 53];
            it.cursed = data[offset + 54];
            it.weight = W(data[offset + 55] | (data[offset + 56] << 8));
            it.quantity = data[offset + 57];
            it.value = W(data[offset + 58] | (data[offset + 59] << 8));
            for (int k = 0; k < 3; k++) it.variantData[k] = data[offset + 60 + k];
            return it;
        }

        // ---- the commands ----

        // Free the whole GOSUB return stack.
        private static void FreeEclStack()
        {
            while (GDVars.dsEclStackTopPtr != null)
            {
                GDVars.dsEclStackSecondPtr = GDVars.dsEclStackTopPtr.next;
                GDVars.dsEclStackTopPtr = GDVars.dsEclStackSecondPtr;
            }
        }

        // EXIT: redraw the borders if PROTECTION text was shown, restore the backed-up current character, clear the
        // graphic flags, stop the VM, step past the opcode, free the GOSUB stack and park the message cursor
        internal static void EclExit()
        {
            if (GDVars.protTextShown != 0)
            {
                Border.DrawWindow(1, 1, 15, 15, 0, White, "");
                if (GDVars.zone0.view3dFlag != 0)
                    Border.DrawWindow(3, 3, 13, 13, 0, White, "");
            }
            if (restoreChar != 0)
            {
                GDVars.curChar = savedChar;
                restoreChar = 0;
            }
            ClearMonsterFlags();
            GDVars.eclGraphics = false;
            GDVars.protTextShown = 0;
            eclStop = 1;
            GDVars.eclIp = W(GDVars.eclIp + 1);
            FreeEclStack();
            GDVars.curRow = C_Msg.MESSAGE_TOP;
            GDVars.curCol = C_Msg.TEXT_LEFT;
            GDVars.dsEcl3dClearFlag = 0;
        }

        // GOTO: jump to var 1's address
        internal static void EclGoto()
        {
            EclVars.LoadEclVars(1);
            GDVars.eclIp = VarAddress(1);
        }

        // GOSUB: push the return address and jump to var 1's address
        internal static void EclGosubCmd()
        {
            EclVars.LoadEclVars(1);
            EclMenu.EclGosub(1);
        }

        // COMPARE: set the comparison flags from vars 1 and 2, as strings when either is a string (type bit 7 set)
        internal static void EclCompare()
        {
            EclVars.LoadEclVars(2);
            if (GDVars.dsGbvmType[1] >= 0x80 || GDVars.dsGbvmType[2] >= 0x80)
            {
                string s1 = GDVars.gbStrings[1];
                string s2 = GDVars.gbStrings[2];
                EclMenu.EclCompareStr(s1, s2);
            }
            else
            {
                ushort v1 = EclVars.GetEclVar(1);
                ushort v2 = EclVars.GetEclVar(2);
                EclMenu.EclCompareInt(v1, v2);
            }
        }

        // ADD, SUBTRACT, DIVIDE, MULTIPLY: combine vars 1 and 2 into var 3's address. SUBTRACT is var 2 - var 1.
        // DIVIDE tests var 2 for zero (storing 0) but divides var 1 by var 2.
        internal static void EclArith()
        {
            var v = new ushort[3];
            EclVars.LoadEclVars(3);
            for (sbyte i = 1; i <= 2; i++)
                v[i] = EclVars.GetEclVar(i);
            ushort address = VarAddress(3);
            ushort r = 0;
            switch (GDVars.curOpcode)
            {
                case C_Ecl.ADD: r = W(v[1] + v[2]); break;
                case C_Ecl.SUBTRACT: r = W(v[2] - v[1]); break;
                case C_Ecl.DIVIDE: r = v[2] == 0 ? (ushort)0 : W(v[1] / v[2]); break;
                // the C# int product of two Words can pass Int32.MaxValue: keep the whole expression unchecked
                case C_Ecl.MULTIPLY: r = unchecked((ushort)(v[1] * v[2])); break;
            }
            EclVars.WriteGbvm(address, r);
        }

        // RANDOM: store a random number from 0 to var 1 (0 to 254 when var 1 is 255) at var 2's address
        internal static void EclRandom()
        {
            EclVars.LoadEclVars(2);
            byte n = B(EclVars.GetEclVar(1));
            if (n < 255) n = B(n + 1);
            ushort address = VarAddress(2);
            byte roll = B(Random(n));
            EclVars.WriteGbvm(address, roll);
        }

        // SAVE: store var 1, a number or a string (type bit 7 set), at var 2's address
        internal static void EclSave()
        {
            EclVars.LoadEclVars(2);
            ushort address = VarAddress(2);
            if (GDVars.dsGbvmType[1] < 0x80)
            {
                ushort v = EclVars.GetEclVar(1);
                EclVars.WriteGbvm(address, v);
            }
            else
                EclVars.WriteGbvmString(address, GDVars.gbStrings[1]);
        }

        // LOAD CHARACTER: make creature number var 1 (low seven bits, 0 = the first) current; EXIT and PROGRAM restore
        // the previous one. With bit 7 set, and dumpCr6C00 and dumpCr6B00 both set, the creature leaves the party.
        internal static void EclLoadChar()
        {
            restoreChar = 1;
            EclVars.LoadEclVars(1);
            byte i = 0;
            byte idx = B(EclVars.GetEclVar(1));
            Types.TCharRecord p = GDVars.creatureList;
            byte flag80 = B(idx & 0x80);
            idx = B(idx & 0x7F);
            if (idx > 0)
                while (i < idx && p != null)
                {
                    p = p.nextCharAddr;
                    i++;
                }
            if (p != null)
                GDVars.curChar = p;
            if (flag80 != 0 && GDVars.dumpCr6C00 && GDVars.dumpCr6B00)
            {
                if (ReferenceEquals(savedChar, GDVars.curChar))
                    restoreChar = 0;
                SaveLoad.RemoveCreature(false, true);
                CharDisp.DisplayParty(GDVars.curChar);
                GDVars.dumpCr6C00 = false;
                GDVars.dumpCr6B00 = false;
            }
        }

        // Set MONSTER_DIST to MaxMonsterDist capped by MONSTER_DIST_REQ (shared by SETUP MONSTER and ENCOUNTER)
        private static void SetMonsterDist()
        {
            GDVars.zone1[C_Gbvm.MONSTER_DIST] = EclMenu.MaxMonsterDist(B(GDVars.gMapX), B(GDVars.gMapY), GDVars.gFacing);
            if (GDVars.zone1[C_Gbvm.MONSTER_DIST_REQ] < GDVars.zone1[C_Gbvm.MONSTER_DIST])
                GDVars.zone1[C_Gbvm.MONSTER_DIST] = GDVars.zone1[C_Gbvm.MONSTER_DIST_REQ];
        }

        private static void ShowMonster()
        {
            EclMenu.DisplayMonster(monId, monPicId, B(GDVars.zone1[C_Gbvm.MONSTER_DIST]), GDVars.dsMonsterFlags);
        }

        // SETUP MONSTER: monster id and picture, requested and actual distance, then draw the monster
        internal static void EclSetupMon()
        {
            EclVars.LoadEclVars(3);
            monId = B(EclVars.GetEclVar(1));
            GDVars.zone1[C_Gbvm.MONSTER_DIST_REQ] = EclVars.GetEclVar(2);
            monPicId = B(EclVars.GetEclVar(3));
            SetMonsterDist();
            ShowMonster();
        }

        // LOAD MONSTER: load monster var 1 and append it to the creature list, then var 2 - 1 copies, each with its own
        // copy of the item and effect lists (prepended, so reversed). All use combat icon var 3 in the next icon slot.
        // Stops at 63 monsters, and loads none when the icon slots are used up.
        internal static void EclLoadMon()
        {
            Types.TCharRecord savedCur = GDVars.curChar;
            byte i = 1;
            EclVars.LoadEclVars(3);
            if (creatureCnt >= MAX_LOADED_MONSTERS) return;
            if (GDVars.gIconAnim > LAST_ICON_SLOT) return;
            Types.TCharRecord p;
            Types.TCharRecord q;
            byte id = B(EclVars.GetEclVar(1));
            Types.TCharRecord creature = null;
            Types.TItemRec item = null;
            Types.TEffectRec ef = null;
            EclMenu.InitCreature(id, ref creature, ref item, ref ef);
            var tmpl = new Types.TCharRecord();
            CopyCharRecord(creature, tmpl);
            Types.TItemRec invHead = item;
            Types.TEffectRec effHead = ef;
            byte count = B(EclVars.GetEclVar(2));
            if (count <= 0) count = 1;
            byte picId = B(EclVars.GetEclVar(3));
            string nm = "CPIC";
            MapTiles.LoadIconAnim(nm, picId, GDVars.gIconAnim);
            p = Party.LastCreature();
            p.nextCharAddr = creature;
            p = p.nextCharAddr;
            p.orderNumber = GDVars.gIconAnim;
            p.nextCharAddr = null;
            creatureCnt = B(creatureCnt + 1);
            i = B(i + 1);
            while (i <= count && creatureCnt < MAX_LOADED_MONSTERS)
            {
                q = new Types.TCharRecord();
                p.nextCharAddr = q;
                p = p.nextCharAddr;
                CopyCharRecord(tmpl, p);
                p.orderNumber = GDVars.gIconAnim;
                p.nextCharAddr = null;
                p.effectsAddr = null;
                p.itemsAddr = null;
                i = B(i + 1);
                creatureCnt = B(creatureCnt + 1);
                while (item != null)
                {
                    CharHlp.PrependItemCopy(ref p.itemsAddr, item);
                    item = item.next;
                }
                item = invHead;
                while (ef != null)
                {
                    CharHlp.PrependEffectCopy(ref p.effectsAddr, ef);
                    ef = ef.next;
                }
                ef = effHead;
            }
            GDVars.gIconAnim = B(GDVars.gIconAnim + 1);
            monstersOn = 1;
            GDVars.curChar = savedCur;
        }

        // APPROACH: bring the monster one step closer and redraw it
        internal static void EclApproach()
        {
            if (GDVars.zone1[C_Gbvm.MONSTER_DIST] > 0)
            {
                GDVars.zone1[C_Gbvm.MONSTER_DIST] = W(GDVars.zone1[C_Gbvm.MONSTER_DIST] - 1);
                ShowMonster();
            }
            GDVars.eclIp = W(GDVars.eclIp + 1);
        }

        // PICTURE: show picture var 1 in the view window, as an animation or a portrait depending on PIC_LOADING.
        // 255 removes the picture, redrawing the map view when ECL graphics or a sprite were showing.
        internal static void EclPicture()
        {
            EclVars.LoadEclVars(1);
            byte picId = B(EclVars.GetEclVar(1));
            if (GDVars.zone0.view3dFlag == 0 && picId != 255)
                Border.DrawWindow(3, 3, 13, 13, 0, White, "");
            if (picId != 255)
            {
                GDVars.dsMonsterFlags[1] = true;
                GDVars.eclGraphics = true;
                if (GDVars.zone1[C_Gbvm.PIC_LOADING] == 255)
                {
                    Portrait.LoadAnimation("PIC", picId, 0, ref GDVars.animData);
                    Portrait.DisplayImageBuf(3, 3, true, GDVars.animData.frames[1].pic);
                }
                else
                    EclMenu.DisplayPortrait(B(GDVars.zone1[C_Gbvm.PIC_LOADING]), picId);
            }
            else
            {
                if ((GDVars.locTypePrev > C_Area.LOC_3D_MAP || GDVars.gLocType == C_Area.LOC_3D_MAP) &&
                    (GDVars.eclGraphics || GDVars.sprShown))
                {
                    Game.DisplayMapView();
                    GDVars.eclGraphics = false;
                    GDVars.sprShown = false;
                    GDVars.picDrawn = true;
                }
                GDVars.dsMonsterFlags[0] = false;
                GDVars.dsMonsterFlags[1] = false;
            }
        }

        // INPUT NUMBER: read a number from the player into var 2's address
        internal static void EclInputNum()
        {
            EclVars.LoadEclVars(2);
            string q = "";
            ushort address = VarAddress(2);
            ushort v = TextRend.InputUint16(q, LightGreen);
            EclVars.WriteGbvm(address, v);
        }

        // INPUT STRING: read up to 40 characters into var 2's address; an empty answer is stored as one space
        internal static void EclInputStr()
        {
            EclVars.LoadEclVars(2);
            string q = "";
            ushort address = VarAddress(2);
            string s = TextRend.ReadString(q, LightGreen, 40);
            if (s == "") s = " ";
            EclVars.WriteGbvmString(address, s);
        }

        // PRINT / PRINTCLEAR: show var 1 (a number is converted to text) in the message window. PRINTCLEAR clears the
        // window and starts at its top.
        internal static void EclPrint()
        {
            EclVars.LoadEclVars(1);
            GDVars.bottomMsgCleared = 0;
            GDVars.msgDelayFlag = 1;
            if (GDVars.dsGbvmType[1] < 0x80)
                GDVars.gbStrings[1] = StrUtil.NumStr(EclVars.GetEclVar(1));
            if (GDVars.curOpcode == C_Ecl.PRINT)
                TextRend.ShowMessage(LightGreen, false, GDVars.gbStrings[1]);
            else
            {
                GDVars.curRow = C_Msg.MESSAGE_TOP;
                GDVars.curCol = C_Msg.TEXT_LEFT;
                TextRend.DisplayMessageWindow(C_Msg.TEXT_LEFT, C_Msg.MESSAGE_TOP, C_Msg.TEXT_RIGHT, C_Msg.TEXT_BOTTOM,
                                              LightGreen, true, GDVars.gbStrings[1]);
            }
            GDVars.msgDelayFlag = 0;
        }

        // RETURN: pop the GOSUB return stack into the IP. With an empty stack it only steps past the opcode.
        internal static void EclReturn()
        {
            GDVars.eclIp = W(GDVars.eclIp + 1);
            if (GDVars.dsEclStackTopPtr != null)
            {
                GDVars.eclIp = GDVars.dsEclStackTopPtr.addr;
                GDVars.dsEclStackSecondPtr = GDVars.dsEclStackTopPtr.next;
                GDVars.dsEclStackTopPtr = GDVars.dsEclStackSecondPtr;
            }
        }

        // COMPARE AND: set the eq flag when var 1 = var 2 and var 3 = var 4, else the ne flag
        internal static void EclCompareAnd()
        {
            var v = new ushort[5];
            ClearCmpFlags();
            EclVars.LoadEclVars(4);
            for (sbyte i = 1; i <= 4; i++)
                v[i] = EclVars.GetEclVar(i);
            if (v[1] == v[2] && v[3] == v[4])
                GDVars.cmpFlags[0] = true;
            else
                GDVars.cmpFlags[1] = true;
        }

        // IF: skip the next command unless the comparison flag for this opcode is set
        internal static void EclIf()
        {
            GDVars.eclIp = W(GDVars.eclIp + 1);
            switch (GDVars.curOpcode)
            {
                case C_Ecl.IF_EQ: if (!GDVars.cmpFlags[0]) EclVars.SkipEclCmd(); break;
                case C_Ecl.IF_NE: if (!GDVars.cmpFlags[1]) EclVars.SkipEclCmd(); break;
                case C_Ecl.IF_LT: if (!GDVars.cmpFlags[2]) EclVars.SkipEclCmd(); break;
                case C_Ecl.IF_GT: if (!GDVars.cmpFlags[3]) EclVars.SkipEclCmd(); break;
                case C_Ecl.IF_LE: if (!GDVars.cmpFlags[4]) EclVars.SkipEclCmd(); break;
                case C_Ecl.IF_GE: if (!GDVars.cmpFlags[5]) EclVars.SkipEclCmd(); break;
            }
        }

        // NEWECL: remember the current script id, check the script archive, load and initialize script var 1, then
        // stop the VM and set newEclFlag so the game loop runs its on-load entry (NewEclDone)
        internal static void EclNewEcl()
        {
            EclVars.LoadEclVars(1);
            GDVars.zone0.previousScriptId = GDVars.eclScriptId;
            byte id = B(EclVars.GetEclVar(1));
            GDVars.eclScriptId = id;
            // name: TPathStr (String[79])
            SaveLoad.CheckFileElseQuit(Short(GDVars.gameDir + DaxIo.ArchiveName("ECL" + StrUtil.NumStr(GDVars.virtualDisk)) +
                                             ".dax", 79));
            EclMenu.LoadEclScript(id);
            // a NEWECL inside a subroutine leaves return addresses on the stack, and EclInit only clears the pointer
            FreeEclStack();
            EclMenu.EclInit();
            eclStop = 1;
            newEclFlag = 1;
            filesLoaded = 0;
        }

        // LOADFILES: load GEO map var 1 when view3dFlag <> 0, or the BACPAC tile set when view3dFlag = 0. LOADPIECES:
        // load the wall sets in vars 1..3. Once both have run on entering the main loop, draw the interface and party.
        internal static void EclLoadFiles()
        {
            var f = new byte[4];
            EclVars.LoadEclVars(3);
            filesLoaded = 1;
            for (sbyte i = 1; i <= 3; i++)
                f[i] = B(EclVars.GetEclVar(i));
            if (GDVars.curOpcode == C_Ecl.LOADFILES)
            {
                mapLoaded = 1;
                if (f[1] != 255 && f[1] != 127 && GDVars.zone0.view3dFlag != 0)
                {
                    GDVars.zone0.mapId.value = f[1];
                    View3D.Load3DMap(f[1]);
                    GDVars.zone1[C_Gbvm.MOVE_LOCKED] = 0;
                }
                if (f[3] != 255 && GDVars.zone0.view3dFlag == 0)
                    MapTiles.Load24x24Set("BACPAC", 0, 40);
            }
            else
            {
                piecesLoaded = 1;
                if (f[1] == 127)
                    View3D.LoadWallSet(0, 1);
                else if (GDVars.zone0.mapGraphics1 != 0 && GDVars.zone0.mapGraphics2 != 0)
                {
                    View3D.LoadWallSet(f[1], 1);
                    View3D.LoadWallSet(f[3], 3);
                }
                else
                    for (byte i = 1; i <= 3; i++)
                        if (f[i] != 255)
                            View3D.LoadWallSet(f[i], i);
                        else
                        {
                            GDVars.dsWallSetCache[i].wallDefId = -1;
                            GDVars.dsWallSetCache[i].symbolSlot = -1;
                        }
            }
            if (enteringLoop != 0 && piecesLoaded != 0 && mapLoaded != 0)
            {
                CharDisp.DisplayNoncombatInterface();
                CharDisp.DisplayParty(GDVars.curChar);
                enteringLoop = 0;
            }
        }

        // AND / OR: combine vars 1 and 2, truncate to a byte, set the flags as COMPARE 0, result; store at var 3
        internal static void EclAndOr()
        {
            EclVars.LoadEclVars(3);
            ushort v1 = EclVars.GetEclVar(1);
            ushort v2 = EclVars.GetEclVar(2);
            ushort address = VarAddress(3);
            byte r;
            if (GDVars.curOpcode == C_Ecl.BIT_AND)
                r = B(v1 & v2);
            else
                r = B(v1 | v2);
            EclMenu.EclCompareInt(0, r);
            EclVars.WriteGbvm(address, r);
        }

        // GETTABLE: read the value at var 1's address + var 2 into var 3's address
        internal static void EclGetTable()
        {
            EclVars.LoadEclVars(3);
            ushort addr1 = VarAddress(1);
            byte idx = B(EclVars.GetEclVar(2));
            ushort addr2 = VarAddress(3);
            ushort sum = W(addr1 + idx);
            ushort v = EclVars.ReadGbvm(sum);
            EclVars.WriteGbvm(addr2, v);
        }

        // SAVETABLE: store var 1 at var 2's address + var 3
        internal static void EclSaveTable()
        {
            EclVars.LoadEclVars(3);
            ushort v = EclVars.GetEclVar(1);
            ushort address = VarAddress(2);
            ushort idx = EclVars.GetEclVar(3);
            ushort sum = W(address + idx);
            EclVars.WriteGbvm(sum, v);
        }

        // VMENU: show the prompt, then a vertical menu of the var 3 strings that follow (at most 20), and store the
        // chosen index at var 1's address
        internal static void EclVMenu()
        {
            GDVars.bottomMsgCleared = 0;
            bool redraw = true;
            string q = "";
            byte i = 1;
            EclVars.LoadEclVars(3);
            ushort address = VarAddress(1);
            string msg = GDVars.gbStrings[1];
            byte count = B(EclVars.GetEclVar(3));
            GDVars.eclIp = W(GDVars.eclIp - 1);
            EclVars.LoadEclVars(count);
            if (count > Types.GB_STRING_LAST)
                count = Types.GB_STRING_LAST;
            Types.TItemRec list = null;
            MenuCore.AllocStringList(count, ref list);
            Types.TItemRec head = list;
            GDVars.curCol = C_Msg.TEXT_LEFT;
            GDVars.curRow = C_Msg.MESSAGE_TOP;
            TextRend.ShowMessage(LightGreen, true, msg);
            while (list != null)
            {
                // DEVIATION (bounds): list members past gbStrings^[20] cannot occur: AllocStringList allocates
                // max(count,1) <= 20 members.
                list.name = GDVars.gbStrings[i];
                list.inUse = 0;
                list = list.next;
                i = B(i + 1);
            }
            list = head;
            short idx = 0;
            EclMenu.EclVerticalMenu(q, q, LightMagenta, LightGreen, White, C_Msg.TEXT_LEFT, SB(GDVars.curRow + 1),
                                    C_Msg.TEXT_RIGHT, C_Msg.TEXT_BOTTOM, list, false, ref redraw, ref idx, ref list);
            EclVars.WriteGbvm(address, W(idx));
            // the menu leaves list on the chosen line (nil on Escape), so free from the head
            MenuCore.FreeStringList(ref head);
            TextRend.ClearRect(C_Msg.TEXT_LEFT, C_Msg.MESSAGE_TOP, C_Msg.TEXT_RIGHT, C_Msg.TEXT_BOTTOM);
        }

        // HMENU: build a '~A ~B ~C' horizontal menu from the var 2 strings that follow (1 to 20) and store the chosen
        // index at var 1's address. A single choice also accepts Enter, and the joystick wording becomes the keyboard's.
        internal static void EclHMenu()
        {
            byte count;
            byte ch;
            string q = "";
            // menu: String[50]
            string menu = "";
            bool enterOk;
            bool anim;
            byte hlCol;
            byte mCol;
            EclVars.LoadEclVars(2);
            ushort address = VarAddress(1);
            count = B(EclVars.GetEclVar(2));
            GDVars.eclIp = W(GDVars.eclIp - 1);
            EclVars.LoadEclVars(count);
            // a count of 0 shows gbStrings^[1] alone
            if (count > Types.GB_STRING_LAST)
                count = Types.GB_STRING_LAST;
            else if (count == 0)
                count = 1;
            menu = "";
            if (count == 1)
            {
                enterOk = true;
                hlCol = White;
                mCol = White;
                if (GDVars.gbStrings[1] == "PRESS <RETURN> OR BUTTON TO CONTINUE")
                    GDVars.gbStrings[1] = "PRESS <ENTER>/<RETURN> TO CONTINUE";
            }
            else
            {
                enterOk = false;
                hlCol = White;
                mCol = LightGreen;
            }
            anim = GDVars.eclGraphics && GDVars.picDrawn;
            for (byte i = 1; i <= count - 1; i++)
                menu = Short(menu + "~" + GDVars.gbStrings[i] + " ", 50);
            menu = Short(menu + "~" + GDVars.gbStrings[count], 50);
            ch = EclMenu.MenuHorizontalCS(q, menu, LightMagenta, mCol, hlCol, enterOk, anim);
            EclVars.WriteGbvm(address, ch);
            TextRend.ClearRect(0, 24, 39, 24);
        }

        // CLEAR MONSTERS: forget the loaded monsters, reset the monster icon slot, clear the treasure
        internal static void EclClearMon()
        {
            GDVars.eclIp = W(GDVars.eclIp + 1);
            creatureCnt = 0;
            monstersOn = 0;
            GDVars.gIconAnim = 8;
            CharHlp.ClearTreasure();
        }

        // PARTY STRENGTH: (4*cleric + (hp + 5*(AC below 0) + 5*(THAC0 below 21) + 8*mage)) div 10 per member, from the
        // 60-biased AC and THAC0, summed and capped at 255
        internal static void EclPartyStr()
        {
            EclVars.LoadEclVars(1);
            ushort strength = 0;
            Types.TCharRecord p = GDVars.creatureList;
            while (p != null)
            {
                byte hp = p.hpCur;
                byte ac = p.acCur;
                byte th = p.thac0Cur;
                byte mage = B(p.classLevels[C_Level.MAGE]);
                byte cleric = B(p.classLevels[C_Level.CLERIC]);
                if (ac > 60) ac = B(ac - 60); else ac = 0;
                if (th > 39) th = B(th - 39); else th = 0;
                // TP evaluates this in Integer (at most 5370, no overflow)
                strength = W(strength + (cleric * 4 + (hp + ac * 5 + th * 5 + mage * 8)) / 10);
                p = p.nextCharAddr;
            }
            ushort address = VarAddress(1);
            if (strength > 255) strength = 255;
            EclVars.WriteGbvm(address, strength);
        }

        // CHECK PARTY: var 1 = 0 reports whether any member has effect var 2; $6BA7 (thiefSkills[3]) or $6C1B
        // (movement) the party's lowest, highest and average. Results to vars 3..6 (lowest, highest, average, found).
        internal static void EclCheckParty()
        {
            ushort attr;
            ushort sum;
            byte effId;
            Types.TCharRecord p;
            bool found;
            Types.TEffectRec effect = null;
            var outv = new byte[4];
            ushort addr1, addr2, addr3, addr4;
            byte count;

            EclVars.LoadEclVars(6);
            p = GDVars.creatureList;
            found = false;
            if (GDVars.dsGbvmType[1] == 1)
                attr = VarAddress(1);
            else
                attr = EclVars.GetEclVar(1);
            effId = B(EclVars.GetEclVar(2));
            addr1 = VarAddress(3);
            addr2 = VarAddress(4);
            addr3 = VarAddress(5);
            addr4 = VarAddress(6);

            // Store the three results and the found flag at the addresses of vars 3..6
            void Write4Gbvm()
            {
                EclVars.WriteGbvm(addr1, outv[1]);
                EclVars.WriteGbvm(addr2, outv[2]);
                EclVars.WriteGbvm(addr3, outv[3]);
                // ShortInt(found)
                EclVars.WriteGbvm(addr4, (ushort)(found ? 1 : 0));
            }

            sum = 0;
            count = 0;
            outv[1] = 255;
            outv[2] = 0;
            switch (attr)
            {
                case 0:
                    while (p != null && !found)
                    {
                        found = CharHlp.HasEffect(p, effId, ref effect);
                        p = p.nextCharAddr;
                    }
                    for (count = 1; count <= 3; count++)
                        outv[count] = 0;
                    Write4Gbvm();
                    break;
                case 0x6BA7:
                    while (p != null)
                    {
                        count = B(count + 1);
                        if (p.thiefSkills[3] < outv[1]) outv[1] = p.thiefSkills[3];
                        if (p.thiefSkills[3] > outv[2]) outv[2] = p.thiefSkills[3];
                        sum = W(sum + p.thiefSkills[3]);
                        p = p.nextCharAddr;
                    }
                    // an empty party divides by zero, as v1.3's runtime error 200 did
                    outv[3] = B(sum / count);
                    Write4Gbvm();
                    break;
                case 0x6C1B:
                    while (p != null)
                    {
                        count = B(count + 1);
                        if (p.movementCur < outv[1]) outv[1] = p.movementCur;
                        if (p.movementCur > outv[2]) outv[2] = p.movementCur;
                        sum = W(sum + p.movementCur);
                        p = p.nextCharAddr;
                    }
                    outv[3] = B(sum / count);
                    Write4Gbvm();
                    break;
            }
        }

        // PARTY SURPRISE: store 1 at var 1's address when the party has a ranger, else 0, and 0 at var 2's address
        internal static void EclPartySurprise()
        {
            EclVars.LoadEclVars(2);
            Types.TCharRecord p = GDVars.creatureList;
            byte mod1 = 0;
            byte mod2 = 0;
            while (p != null)
            {
                if (p.charClass == C_Class.RANGER || p.charClass == C_Class.CLERIC_RANGER)
                    mod1 = 1;
                p = p.nextCharAddr;
            }
            ushort addr1 = VarAddress(1);
            ushort addr2 = VarAddress(2);
            EclVars.WriteGbvm(addr1, mod1);
            EclVars.WriteGbvm(addr2, mod2);
        }

        // SURPRISE: roll 1d6 per side against targets from vars 1..4; store the bitmask at C_Gbvm.SURPRISE:
        // 1 first roll succeeds, 2 the second, 3 both, 0 neither.
        internal static void EclSurprise()
        {
            var v = new byte[5];
            EclVars.LoadEclVars(4);
            byte result = 0;
            for (sbyte i = 1; i <= 4; i++)
                v[i] = B(EclVars.GetEclVar(i));
            sbyte t1 = SB(v[4] + 2 - v[1]);
            sbyte t2 = SB(v[2] + 2 - v[3]);
            byte r1 = Dice.RollDice(1, 6);
            byte r2 = Dice.RollDice(1, 6);
            if (r1 <= t1) result = 1;
            if (r2 <= t2) result = B(result | 2);
            EclVars.WriteGbvm(C_Gbvm.SURPRISE, result);
        }

        // COMBAT: with no monsters and no duel, the shop, vault, temple or post-combat; otherwise fight, then
        // post-combat. Afterwards restore the interface state.
        internal static void EclCombat()
        {
            GDVars.eclIp = W(GDVars.eclIp + 1);
            if (monstersOn == 0 && !GDVars.duelFlag)
            {
                if (GDVars.zone1[C_Gbvm.ENTER_SHOP] == 1)
                {
                    GDVars.zone1[C_Gbvm.ENTER_SHOP] = 0;
                    Game.ShopMainMenu();
                }
                else if (GDVars.zone1[C_Gbvm.ENTER_SHOP] == 2)
                {
                    // New Phlan's vault, The Hoarder's Respite: the data patch for #606 stores 2
                    GDVars.zone1[C_Gbvm.ENTER_SHOP] = 0;
                    Game.VaultMenu();
                }
                else if (GDVars.zone1[C_Gbvm.ENTER_TEMPLE] == 1)
                {
                    GDVars.zone1[C_Gbvm.ENTER_TEMPLE] = 0;
                    Game.TempleMainMenu();
                }
                else
                    Game.PostCombat();
            }
            else
            {
                ushort dist = EclMenu.MaxMonsterDist(B(GDVars.gMapX), B(GDVars.gMapY), GDVars.gFacing);
                if (dist < GDVars.zone1[C_Gbvm.MONSTER_DIST])
                    GDVars.zone1[C_Gbvm.MONSTER_DIST] = dist;
                Game.CombatMain();
                Game.PostCombat();
                if (GDVars.zone0.view3dFlag == 0)
                {
                    MapTiles.Load24x24Set("BACPAC", 0, 40);
                    MapTiles.LoadIconAnim("ICON", 0, 9);
                    MapTiles.LoadIconAnim("ICON", 1, 10);
                    MapTiles.LoadIconAnim("ICON", 2, 11);
                }
                if (GDVars.duelFlag)
                    GDVars.duelFlag = false;
            }
            if (GDVars.gLocType == C_Area.LOC_3D_MAP)
                GDVars.ifcState = C_Ifc.INDOORS;
            else
                GDVars.ifcState = C_Ifc.OUTDOORS;
            GDVars.zone1[C_Gbvm.SEARCH_FLAGS] = W(GDVars.zone1[C_Gbvm.SEARCH_FLAGS] & 1);
            ClearMonsterFlags();
            GDVars.eclGraphics = false;
            CharDisp.DisplayNoncombatInterface();
        }

        // ONGOTO / ONGOSUB: jump to or call address number var 1 of the var 2 addresses that follow (at most 64); a
        // selector of var 2 or more, or 64 or more, falls through past the list
        internal static void EclOnGoto()
        {
            bool done = false;
            EclVars.LoadEclVars(2);
            byte sel = B(EclVars.GetEclVar(1));
            byte count = B(EclVars.GetEclVar(2));
            GDVars.eclIp = W(GDVars.eclIp - 1);
            EclVars.LoadEclVars(count);
            if (count > 64) count = 64;
            byte i = 0;
            while (i < count && !done)
            {
                if (sel == i)
                {
                    if (GDVars.curOpcode == C_Ecl.ONGOTO)
                        GDVars.eclIp = VarAddress(i + 1);
                    else
                        EclMenu.EclGosub(B(i + 1));
                    done = true;
                }
                else
                    i = B(i + 1);
            }
        }

        /// <summary>
        /// The item id TREASURE generates for one random item, from the rolls the source makes. rollDice is
        /// Dice.RollDice(1, sides). Exposed for source-table tests.
        /// </summary>
        internal static byte TreasureItemId(Func<byte, byte> rollDice)
        {
            byte itemId = 0;
            byte roll = rollDice(100);
            byte roll2;
            // DEVIATION (fix, user-directed 2026-10-21): the source labels overlap, 86..92 then 91..98. Pascal tests
            // case labels in order, so there 91 and 92 gave a cleric scroll and the magic-item table only saw 93..98.
            // The ranges are made contiguous as intended: 86..90 cleric scroll, 91..98 magic items.
            if (roll >= 1 && roll <= 60)
            {
                do { roll2 = rollDice(100); } while (roll2 == 48 || roll2 == 49);
                if ((roll2 >= 1 && roll2 <= 47) || (roll2 >= 50 && roll2 <= 59))
                    itemId = roll2 == 45 ? (byte)59 : roll2;
                else if (roll2 >= 60 && roll2 <= 90)
                {
                    roll2 = rollDice(10);
                    if (roll2 >= 1 && roll2 <= 4) itemId = 36;
                    else if (roll2 >= 5 && roll2 <= 7) itemId = 35;
                    else if (roll2 == 8) itemId = 34;
                    else if (roll2 == 9) itemId = 37;
                    else if (roll2 == 10) itemId = 38;
                }
                else if (roll2 >= 91 && roll2 <= 94) itemId = 73;
                else if (roll2 >= 95 && roll2 <= 97) itemId = 93;
                else if (roll2 >= 98 && roll2 <= 100) itemId = 77;
            }
            else if (roll >= 61 && roll <= 85) itemId = 61;
            else if (roll >= 86 && roll <= 90) itemId = 62;
            else if (roll >= 91 && roll <= 98)
            {
                byte roll3 = rollDice(16);
                if (roll3 >= 1 && roll3 <= 7) itemId = 71;
                else if (roll3 >= 8 && roll3 <= 9) itemId = 70;
                else if (roll3 == 10) itemId = 84;
                else if (roll3 == 11 || roll3 == 12) itemId = 78;
                else if (roll3 == 13 || roll3 == 14) itemId = 79;
                else if (roll3 == 15) itemId = 92;
                else if (roll3 == 16) itemId = 67;
            }
            else if (roll == 99 || roll == 100) itemId = 69;
            return itemId;
        }

        // TREASURE: set the treasure money from vars 1..7 and add items by var 8: below $80 an ITEM archive list,
        // $80+n n random items, 255 none. Item nodes are prepended; a trailing part record in the list is ignored.
        internal static void EclTreasure()
        {
            EclVars.LoadEclVars(8);
            for (sbyte i = C_Money.COPPER; i <= C_Money.JEWELRY; i++)
                GDVars.treasureMoney[i] = EclVars.GetEclVar(SB(i + 1));
            byte tId = B(EclVars.GetEclVar(8));
            if (tId < 0x80)
            {
                ushort sizeRead;
                byte[] data;
                DaxIo.LoadDax("ITEM" + StrUtil.NumStr(GDVars.virtualDisk), ".dax", tId, out sizeRead, out data);
                if (sizeRead == 0)
                {
                    TextRend.DisplayAndPause(White, "Unable to find item file");
                    GameIo.ExitGame();
                    // ExitGame halts; a host whose Halt returns must not continue into the missing data
                    return;
                }
                ushort offset = 0;
                while (sizeRead - offset >= ITEM_REC_SIZE)
                {
                    Types.TItemRec it = ItemFromBytes(data, offset);
                    offset = W(offset + ITEM_REC_SIZE);
                    CharHlp.PrependItemCopy(ref GDVars.treasureItems, it);
                }
            }
            else
            {
                if (tId == 255) return;
                sbyte n = SB(tId - 0x80);
                // `it` is one record reused for every item, as in the source
                var it = new Types.TItemRec();
                for (int j = 1; j <= n; j++)
                {
                    byte itemId = TreasureItemId(sides => Dice.RollDice(1, sides));
                    RandItem.GenerateRandomItem(itemId, it);
                    CharHlp.PrependItemCopy(ref GDVars.treasureItems, it);
                }
                Types.TItemRec walk = GDVars.treasureItems;
                while (walk != null)
                {
                    CharHlp.ItemLabel(walk, false);
                    walk = walk.next;
                }
            }
        }

        // ROB: take var 2 percent of the money (at most 100) and steal each item with a var 3 percent chance, from
        // the current character or, when var 1 is nonzero, from every member; robbed characters are recalculated.
        internal static void EclRob()
        {
            EclVars.LoadEclVars(3);
            byte whole = B(EclVars.GetEclVar(1));
            byte pct = B(EclVars.GetEclVar(2));
            if (pct > 100) pct = 100;
            // Real (6-byte) in the source; see EclVmUnits.RobMoney
            double fraction = (100 - pct) / 100.0;
            byte chance = B(EclVars.GetEclVar(3));
            if (whole == 0)
            {
                EclMenu.RobMoney(fraction, GDVars.curChar);
                EclMenu.RobItems(chance, GDVars.curChar);
                CharHlp.RecalcStats(GDVars.curChar);
            }
            else
            {
                Types.TCharRecord p = GDVars.creatureList;
                while (p != null)
                {
                    EclMenu.RobMoney(fraction, p);
                    EclMenu.RobItems(chance, p);
                    CharHlp.RecalcStats(p);
                    p = p.nextCharAddr;
                }
            }
            // the party list shows the new armor class at once
            CharDisp.DisplayParty(GDVars.curChar);
        }

        // DEVIATION: MenuHorizontalCS returns 255 for a letter or digit of the menu text that is not a hotkey (for
        // example 'O' on COMBAT). ENCOUNTER and PARLAY then index their five-entry tables with 255, which in the DOS
        // build reads whatever lies on the stack past the array (undefined). The port asks again instead.
        private static byte MenuChoice(string query, string menu, byte qCol, byte mCol, byte hlCol, bool enterOk,
                                       bool anim, byte last)
        {
            byte sel;
            do
                sel = EclMenu.MenuHorizontalCS(query, menu, qCol, mCol, hlCol, enterOk, anim);
            while (sel > last);
            return sel;
        }

        // ENCOUNTER: run the encounter menu until combat, flight, parley or monster retreat ends it. Write the outcome
        // to var 4's address: 0 monsters flee, 1 combat, 2 party flees, 3 parley.
        internal static void EclEncounter()
        {
            byte sel;
            var resp = new byte[5];
            var msgs = new string[4];
            // uninitialised in the source; MONSTER_DIST is always <= 2 when the loop starts, so msg is set before use
            string msg = "";
            byte fleePar;
            byte fleeMon;
            byte hiMove = 0;
            byte loMove = 0;
            bool waited = false;
            bool clearW;
            bool anim;
            string menu;
            string q;
            byte choice;
            byte k;
            byte tried;
            ushort address;

            // The party closes in, or with the monsters at hand both sides wait; the menu comes round again.
            void ApproachOrWait()
            {
                if (GDVars.zone1[C_Gbvm.MONSTER_DIST] != 0)
                {
                    GDVars.zone1[C_Gbvm.MONSTER_DIST] = W(GDVars.zone1[C_Gbvm.MONSTER_DIST] - 1);
                    ShowMonster();
                }
                else
                    TextRend.ShowMessage(LightGreen, true, "Both sides wait.");
                waited = true;
            }

            // The party closes in and the menu comes round again, or with the monsters at hand it is a fight (3).
            void ApproachOrFight()
            {
                if (GDVars.zone1[C_Gbvm.MONSTER_DIST] <= 0)
                    EclVars.WriteGbvm(address, 3);
                else
                {
                    GDVars.zone1[C_Gbvm.MONSTER_DIST] = W(GDVars.zone1[C_Gbvm.MONSTER_DIST] - 1);
                    ShowMonster();
                    waited = true;
                }
            }

            // The monsters run (0).
            void MonstersFlee()
            {
                EclVars.WriteGbvm(address, 0);
                GDVars.curCol = C_Msg.TEXT_LEFT;
                GDVars.curRow = C_Msg.MESSAGE_TOP;
                TextRend.ShowMessage(LightGreen, true, "The monsters flee.");
            }

            GDVars.inEncMenu = 1;
            GDVars.bottomMsgCleared = 0;
            GDVars.msgDelayFlag = 1;
            EclMenu.MoveExtremes(ref hiMove, ref loMove);
            q = "";
            EclVars.LoadEclVars(14);
            monId = B(EclVars.GetEclVar(1));
            GDVars.zone1[C_Gbvm.MONSTER_DIST_REQ] = EclVars.GetEclVar(2);
            monPicId = B(EclVars.GetEclVar(3));
            address = VarAddress(4);
            for (byte i = 0; i <= 4; i++)
                resp[i] = B(EclVars.GetEclVar(SB(i + 5)));
            for (byte i = 1; i <= 3; i++)
                msgs[i] = GDVars.gbStrings[i];
            fleePar = B(EclVars.GetEclVar(13));
            fleeMon = B(EclVars.GetEclVar(14));
            SetMonsterDist();
            ShowMonster();
            do
            {
                anim = GDVars.eclGraphics && GDVars.picDrawn;
                clearW = true;
                waited = false;
                GDVars.curCol = C_Msg.TEXT_LEFT;
                GDVars.curRow = C_Msg.MESSAGE_TOP;
                // the message for the distance, else the next one after it that is set, wrapping round the three
                if (GDVars.zone1[C_Gbvm.MONSTER_DIST] <= 2)
                {
                    k = B(GDVars.zone1[C_Gbvm.MONSTER_DIST] + 1);
                    tried = 0;
                    do
                    {
                        msg = msgs[k];
                        k = B(k + 1);
                        if (k > 3) k = 1;
                        tried = B(tried + 1);
                    } while (!(msg != "" || tried == 3));
                }
                if (msg == "")
                    clearW = false;
                TextRend.ShowMessage(LightGreen, clearW, msg);
                if (GDVars.zone1[C_Gbvm.MONSTER_DIST] == 0 || GDVars.zone0.view3dFlag == 0)
                    menu = "~COMBAT ~WAIT ~FLEE ~PARLAY";
                else
                    menu = "~COMBAT ~WAIT ~FLEE ~ADVANCE";
                sel = MenuChoice(q, menu, LightMagenta, LightGreen, White, false, anim, 3);
                if ((GDVars.zone1[C_Gbvm.MONSTER_DIST] == 0 || GDVars.zone0.view3dFlag == 0) && sel == 3)
                    sel = 4;
                choice = resp[sel];
                switch (choice)
                {
                    case 0:
                        if (sel != 2)
                            EclVars.WriteGbvm(address, 1);
                        else
                        {
                            if (loMove >= fleePar)
                                EclVars.WriteGbvm(address, 2);
                            else
                                EclVars.WriteGbvm(address, 1);
                        }
                        break;
                    case 1:
                        switch (sel)
                        {
                            case 0: EclVars.WriteGbvm(address, 1); break;
                            case 1:
                                waited = true;
                                TextRend.ShowMessage(LightGreen, true, "Both sides wait.");
                                break;
                            case 2: EclVars.WriteGbvm(address, 2); break;
                            case 3: ApproachOrWait(); break;
                            case 4: ApproachOrFight(); break;
                        }
                        break;
                    case 2:
                        switch (sel)
                        {
                            case 0:
                                if (fleeMon > hiMove)
                                    MonstersFlee();
                                else
                                    EclVars.WriteGbvm(address, 1);
                                break;
                            case 1: case 2: case 3: case 4: MonstersFlee(); break;
                        }
                        break;
                    case 3:
                        switch (sel)
                        {
                            case 0: EclVars.WriteGbvm(address, 1); break;
                            case 1: case 3: ApproachOrWait(); break;
                            case 2: EclVars.WriteGbvm(address, 2); break;
                            case 4: ApproachOrFight(); break;
                        }
                        break;
                    case 4:
                        switch (sel)
                        {
                            case 0: EclVars.WriteGbvm(address, 1); break;
                            case 1: case 3: case 4: ApproachOrFight(); break;
                            case 2: EclVars.WriteGbvm(address, 2); break;
                        }
                        break;
                }
            } while (waited);
            TextRend.ClearRect(0, 24, 39, 24);
            GDVars.msgDelayFlag = 0;
            GDVars.inEncMenu = 0;
        }

        // PARLAY: offer the five attitudes, with a single space as the prompt, and store var (choice + 1) at var 6
        internal static void EclParlay()
        {
            var v = new byte[5];
            EclVars.LoadEclVars(6);
            string space = " ";
            for (byte i = 0; i <= 4; i++)
                v[i] = B(EclVars.GetEclVar(SB(i + 1)));
            byte sel = MenuChoice(space, "~HAUGHTY ~SLY ~NICE ~MEEK ~ABUSIVE", LightMagenta, LightGreen, White, false,
                                  false, 4);
            ushort address = VarAddress(6);
            byte v5 = v[sel];
            EclVars.WriteGbvm(address, v5);
        }

        // FIND ITEM: set the eq comparison flag (else ne) if any party member carries an item of type var 1
        internal static void EclFindItem()
        {
            EclVars.LoadEclVars(1);
            bool found = false;
            byte itemType = B(EclVars.GetEclVar(1));
            ClearCmpFlags();
            GDVars.cmpFlags[1] = true;
            Types.TCharRecord p = GDVars.creatureList;
            while (p != null && !found)
            {
                Types.TItemRec item = p.itemsAddr;
                while (item != null && !found)
                {
                    if (itemType == item.itemType)
                    {
                        GDVars.cmpFlags[0] = true;
                        GDVars.cmpFlags[1] = false;
                        found = true;
                    }
                    item = item.next;
                }
                p = p.nextCharAddr;
            }
        }

        // DELAY: pause for the text delay
        internal static void EclDelay()
        {
            GDVars.eclIp = W(GDVars.eclIp + 1);
            TextRend.PauseOnly();
        }

        // DAMAGE: apply scripted damage, then check for total party defeat. See eclvm.pas for the operand encoding.
        internal static void EclDamage()
        {
            byte count = 0;
            Types.TCharRecord savedCur = GDVars.curChar;
            byte allParty = 0;
            Types.TCharRecord p = GDVars.creatureList;
            EclVars.LoadEclVars(5);
            byte attack = B(EclVars.GetEclVar(1));
            byte dieCount = B(EclVars.GetEclVar(2));
            byte sides = B(EclVars.GetEclVar(3));
            byte bonus = B(EclVars.GetEclVar(4));
            byte value = B(EclVars.GetEclVar(5));
            ushort dmg = W(bonus + Dice.RollDice(dieCount, sides));
            if ((attack & 0x40) != 0)
                allParty = 1;
            else
                count = Dice.RollDice(1, B(GDVars.zone1[C_Gbvm.PARTY_SIZE]));
            if ((attack & 0x80) != 0)
            {
                byte saveMod = B(attack & 0x1F);
                byte saveType = B(value & 7);
                if (allParty != 0)
                {
                    while (p != null)
                    {
                        if ((attack & 0x20) != 0)
                            EclMenu.EclInflictDamage(dmg, p);
                        else if (!FxEngine.RollSavingThrow(p, SB(saveType), SB(saveMod)))
                            EclMenu.EclInflictDamage(dmg, p);
                        p = p.nextCharAddr;
                    }
                }
                else if ((value & 0x80) != 0)
                {
                    if (saveType == 0 || !FxEngine.RollSavingThrow(GDVars.curChar, SB(saveType - 1), SB(saveMod)))
                        EclMenu.EclInflictDamage(dmg, GDVars.curChar);
                }
                else
                {
                    for (int i = 2; i <= count; i++)
                        p = p.nextCharAddr;
                    if (!FxEngine.RollSavingThrow(p, SB(saveType), SB(saveMod)))
                        EclMenu.EclInflictDamage(dmg, p);
                }
            }
            else
            {
                for (int i = 1; i <= attack; i++)
                {
                    p = GDVars.creatureList;
                    count = Dice.RollDice(1, B(GDVars.zone1[C_Gbvm.PARTY_SIZE]));
                    byte k = 1;
                    while (k < count)
                    {
                        p = p.nextCharAddr;
                        k = B(k + 1);
                    }
                    if (FxEngine.AttackRollNoAttacker(p, value))
                        EclMenu.EclInflictDamage(dmg, p);
                    dmg = W(bonus + Dice.RollDice(dieCount, sides));
                }
            }
            Types.TCharRecord q = GDVars.creatureList;
            GDVars.partyDefeated = true;
            while (q != null)
            {
                if (q.enabled != 0)
                    GDVars.partyDefeated = false;
                q = q.nextCharAddr;
            }
            if (GDVars.partyDefeated)
            {
                Border.DrawWindow(C_Msg.TEXT_LEFT, C_Msg.TEXT_TOP, C_Msg.TEXT_RIGHT, C_Msg.TEXT_BOTTOM, 0, White, "The END!");
                GDVars.curCol = 2;
                GDVars.curRow = 2;
                TextRend.DisplayMessageWindow(C_Msg.TEXT_LEFT, C_Msg.TEXT_TOP, C_Msg.TEXT_RIGHT, C_Msg.TEXT_BOTTOM,
                                              LightGreen, true, "The entire party is killed!");
                Delay(3000);
            }
            GDVars.curChar = savedCur;
            TextRend.DisplayAndPause(White, "press <enter>/<return> to continue");
        }

        // SPRITE OFF: if a sprite is showing, redraw the map view over it
        internal static void EclSpriteOff()
        {
            GDVars.eclIp = W(GDVars.eclIp + 1);
            if (GDVars.sprShown)
            {
                Game.DisplayMapView();
                GDVars.sprShown = false;
                GDVars.eclGraphics = false;
            }
        }

        // CLOCK: advance the clock by var 1 minutes, in steps of at most 255
        internal static void EclClock()
        {
            EclVars.LoadEclVars(1);
            ushort n = EclVars.GetEclVar(1);
            while (n > 255)
            {
                FxEngine.AdvanceTime(255, 1);
                n = W(n - 255);
            }
            FxEngine.AdvanceTime(B(n), 1);
        }

        // PRINTRETURN: move the message cursor to the start of the next line and clear protNewLine
        internal static void EclPrintRet()
        {
            GDVars.eclIp = W(GDVars.eclIp + 1);
            if (GDVars.protNewLine != 0)
            {
                GDVars.curCol = C_Msg.TEXT_LEFT;
                GDVars.curRow = B(GDVars.curRow + 1);
                GDVars.protNewLine = 0;
            }
            else
            {
                GDVars.curCol = C_Msg.TEXT_LEFT;
                GDVars.curRow = B(GDVars.curRow + 1);
            }
        }

        // CLEAR BOX: clear the 3D view box and note it for PROTECTION
        internal static void EclClearBox()
        {
            GDVars.eclIp = W(GDVars.eclIp + 1);
            TextRend.ClearRect(1, 1, 15, 15);
            GDVars.dsEcl3dClearFlag = 1;
        }

        // WHO: let the player pick a party member, prompting with var 1. A duel WHO adds Exit: Exit or Escape picks no
        // one and sends the script back to the duel's question.
        internal static void EclWho()
        {
            ushort at = GDVars.eclIp;
            ushort back = 0;
            for (int i = 1; i <= DUEL_WHOS; i++)
                if (DUEL_WHO[i].script == GDVars.eclScriptId && DUEL_WHO[i].at == at)
                    back = DUEL_WHO[i].back;
            EclVars.LoadEclVars(1);
            TextRend.ClearRect(C_Msg.TEXT_LEFT, C_Msg.MESSAGE_TOP, C_Msg.TEXT_RIGHT, C_Msg.TEXT_BOTTOM);
            string msg = GDVars.gbStrings[1];
            Types.TCharRecord picked = GDVars.curChar;
            // query: TStr40
            CharDisp.SelectChar(Short(msg, 40), back != 0, ref picked);
            if (picked == null)
                GDVars.eclIp = back;
            else
                GDVars.curChar = picked;
        }

        // ADD NPC: add creature var 1 as an NPC whose npc byte is half the morale var 2 with bit 7 set. Monster 24
        // joins hostile, every other friendly. A full party takes nobody.
        internal static void EclAddNpc()
        {
            EclVars.LoadEclVars(2);
            byte id = B(EclVars.GetEclVar(1));
            byte partySize = B(GDVars.zone1[C_Gbvm.PARTY_SIZE]);
            EclMenu.AddCreatureToParty(id);
            if (GDVars.zone1[C_Gbvm.PARTY_SIZE] == partySize) return;
            byte morale = B(EclVars.GetEclVar(2));
            morale = B(morale / 2);
            morale = B(morale | C_Npc.FLAG);
            GDVars.curChar.npc = morale;
            if (id != 24)
                GDVars.curChar.hostile = 0;
            else
                GDVars.curChar.hostile = 1;
            CharHlp.RecalcStats(GDVars.curChar);
            CharDisp.DisplayParty(GDVars.curChar);
        }

        // SPELL: find the first okay member with spell var 1 memorized in memorizedSpells[1..20]; write slot index - 1
        // to var 2 (255 when nobody) and the member's party position to var 3.
        internal static void EclSpell()
        {
            EclVars.LoadEclVars(3);
            byte spellId = B(EclVars.GetEclVar(1));
            ushort addr1 = VarAddress(2);
            ushort addr2 = VarAddress(3);
            byte slot = 255;
            byte idx = 0;
            bool found = false;
            Types.TCharRecord p = GDVars.creatureList;
            while (p != null && !found)
            {
                if (p.status == C_Status.OKAY)
                    // SizeOf(p^.memorizedSpells) - 1 = SPELL_SLOT_LAST
                    for (int k = 1; k <= p.memorizedSpells.Data.Length - 1; k++)
                        if (!found && p.memorizedSpells[k] == spellId)
                        {
                            found = true;
                            slot = B(k - 1);
                        }
                if (!found)
                {
                    p = p.nextCharAddr;
                    idx = B(idx + 1);
                }
            }
            EclVars.WriteGbvm(addr1, slot);
            EclVars.WriteGbvm(addr2, idx);
        }

        // CALL: dispatch ECL service calls by address
        internal static void EclCall()
        {
            EclVars.LoadEclVars(1);
            ushort address = VarAddress(1);
            ushort key = W(address - 0x7FFF);
            switch (key)
            {
                // address $2C90
                case 0xAC91:
                    GDVars.eventAtCell = View3D.GetMapEvent(GDVars.gMapX, GDVars.gMapY);
                    if (filesLoaded != 0)
                        if (GDVars.eclGraphics || GDVars.sprShown || GDVars.picRegionW || GDVars.movedParty ||
                            GDVars.redrawSky)
                        {
                            Game.DisplayMapView();
                            CharDisp.PrintMiscInfo();
                            GDVars.redrawSky = false;
                            GDVars.picRegionW = false;
                            GDVars.movedParty = false;
                            GDVars.eclGraphics = false;
                            GDVars.sprShown = false;
                        }
                    break;
                // address $8000
                case 1:
                    EclMenu.CharFightsAlone(1);
                    break;
                // address $8001
                case 2:
                    EclMenu.CharFightsAlone(0);
                    break;
                // address $BA03
                case 0x3A04:
                    switch (GDVars.sndEffect)
                    {
                        case 8: Game.SoundCommand(GDConsts.soundIds.combat[C_Sound.STEP]); break;
                        case 10: Game.SoundCommand(GDConsts.soundIds.combat[C_Sound.ECL_CUE]); break;
                    }
                    break;
                // address $C01E
                case 0x401F:
                    EclMenu.EclMoveForward();
                    break;
                // address $C018
                case 0x4019:
                    if (GDVars.gLocType == C_Area.LOC_3D_MAP)
                        GDVars.wallAhead = View3D.GetWallType(GDVars.gMapX, GDVars.gMapY, GDVars.gFacing);
                    break;
            }
        }

        // Run the pre-camp entry (scriptEntryAddrs[3]) and the camp menu, then the camp-interruption entry
        // (scriptEntryAddrs[4]) if camp was interrupted. Redraw the map view and clear recentSaved.
        internal static void EnterCamp()
        {
            bool interrupted = false;
            CallEclScript(GDVars.scriptEntryAddrs[3]);
            Game.CampMainMenu(ref interrupted);
            if (interrupted)
            {
                CharDisp.DisplayNoncombatInterface();
                CallEclScript(GDVars.scriptEntryAddrs[4]);
            }
            Game.DisplayMapView();
            GDVars.recentSaved = 0;
        }

        // PROGRAM: restore the backed-up current character, then run program var 1: training hall, win game, or camp
        // (IP preserved, then EXIT). Other values do nothing.
        internal static void EclProgram()
        {
            if (restoreChar != 0)
            {
                GDVars.curChar = savedChar;
                restoreChar = 0;
            }
            EclVars.LoadEclVars(1);
            byte v = B(EclVars.GetEclVar(1));
            switch (v)
            {
                case PROG_TRAINING:
                    Game.CharGenMainMenu();
                    CharDisp.DisplayNoncombatInterface();
                    break;
                case PROG_WIN_GAME:
                    Game.WinGame();
                    break;
                case PROG_CAMP:
                    ushort savedIp = GDVars.eclIp;
                    EnterCamp();
                    GDVars.eclIp = savedIp;
                    EclExit();
                    break;
            }
        }

        // PROTECTION: draw the text at var 1's address at the message cursor; each byte is one less than its glyph, a
        // $00 or $FF byte ends it, as does the right edge of the text area. After CLEAR BOX, text below row 13 moves
        // into the 3D box and EXIT redraws its border.
        internal static void EclProtection()
        {
            ClearMonsterFlags();
            GDVars.eclGraphics = false;
            EclVars.LoadEclVars(1);
            ushort address = VarAddress(1);
            ushort i = 0;
            bool done = false;
            if (GDVars.dsEcl3dClearFlag != 0 && GDVars.curRow > 13)
            {
                GDVars.curRow = 5;
                GDVars.curCol = 2;
                GDVars.protTextShown = 1;
            }
            GDVars.protNewLine = 1;
            do
            {
                ushort a = W(address + i);
                byte c = B(EclVars.ReadGbvm(a) + 1);
                if (c > 1)
                {
                    TextRend.RenderChar(GDVars.curCol, GDVars.curRow, White, 1, c, 0);
                    GDVars.curCol = B(GDVars.curCol + 1);
                    i = W(i + 1);
                }
                else
                    done = true;
            } while (!(done || GDVars.curCol > C_Msg.TEXT_RIGHT));
            GDVars.curRow = B(GDVars.curRow + 1);
        }

        // Run the command at the IP (curOpcode). An opcode with no handler is stepped over as SkipEclCmd steps it.
        internal static void ExecNextEclCmd()
        {
            switch (GDVars.curOpcode)
            {
                case C_Ecl.EXIT_SCRIPT: EclExit(); break;
                case C_Ecl.JUMP: EclGoto(); break;
                case C_Ecl.GOSUB: EclGosubCmd(); break;
                case C_Ecl.COMPARE: EclCompare(); break;
                case C_Ecl.ADD: case C_Ecl.SUBTRACT: case C_Ecl.DIVIDE: case C_Ecl.MULTIPLY: EclArith(); break;
                case C_Ecl.RANDOM_NUMBER: EclRandom(); break;
                case C_Ecl.SAVE: EclSave(); break;
                case C_Ecl.LOAD_CHAR: EclLoadChar(); break;
                case C_Ecl.LOAD_MON: EclLoadMon(); break;
                case C_Ecl.SETUP_MON: EclSetupMon(); break;
                case C_Ecl.APPROACH: EclApproach(); break;
                case C_Ecl.PICTURE: EclPicture(); break;
                case C_Ecl.INPUT_NUM: EclInputNum(); break;
                case C_Ecl.INPUT_STR: EclInputStr(); break;
                case C_Ecl.PRINT: case C_Ecl.PRINTCLEAR: EclPrint(); break;
                case C_Ecl.RETURN: EclReturn(); break;
                case C_Ecl.COMPARE_AND: EclCompareAnd(); break;
                case C_Ecl.VMENU: EclVMenu(); break;
                case C_Ecl.IF_EQ: case C_Ecl.IF_NE: case C_Ecl.IF_LT: case C_Ecl.IF_GT: case C_Ecl.IF_LE:
                case C_Ecl.IF_GE: EclIf(); break;
                case C_Ecl.CLEAR_MON: EclClearMon(); break;
                case C_Ecl.PARTY_STR: EclPartyStr(); break;
                case C_Ecl.CHECK_PARTY: EclCheckParty(); break;
                case C_Ecl.NEWECL: EclNewEcl(); break;
                case C_Ecl.LOADFILES: case C_Ecl.LOADPIECES: EclLoadFiles(); break;
                case C_Ecl.PARTY_SURPR: EclPartySurprise(); break;
                case C_Ecl.SURPRISE: EclSurprise(); break;
                case C_Ecl.START_COMBAT: EclCombat(); break;
                case C_Ecl.ONGOTO: case C_Ecl.ONGOSUB: EclOnGoto(); break;
                case C_Ecl.TREASURE: EclTreasure(); break;
                case C_Ecl.ROB: EclRob(); break;
                case C_Ecl.ENCOUNTER: EclEncounter(); break;
                case C_Ecl.GETTABLE: EclGetTable(); break;
                case C_Ecl.HMENU: EclHMenu(); break;
                case C_Ecl.PARLAY: EclParlay(); break;
                case C_Ecl.CALL: EclCall(); break;
                case C_Ecl.DAMAGE: EclDamage(); break;
                case C_Ecl.BIT_AND: case C_Ecl.BIT_OR: EclAndOr(); break;
                case C_Ecl.SPRITE_OFF: EclSpriteOff(); break;
                case C_Ecl.FIND_ITEM: EclFindItem(); break;
                case C_Ecl.PRINT_RET: EclPrintRet(); break;
                case C_Ecl.CLOCK: EclClock(); break;
                case C_Ecl.SAVE_TABLE: EclSaveTable(); break;
                case C_Ecl.ADD_NPC: EclAddNpc(); break;
                case C_Ecl.RUN_PROGRAM: EclProgram(); break;
                case C_Ecl.WHO: EclWho(); break;
                case C_Ecl.PAUSE: EclDelay(); break;
                case C_Ecl.SPELL: EclSpell(); break;
                case C_Ecl.PROTECTION: EclProtection(); break;
                case C_Ecl.CLEAR_BOX: EclClearBox(); break;
                default: EclVars.SkipEclCmd(); break;
            }
        }

        /// <summary>Runs commands at address until EXIT/NEWECL stops the VM or the party is defeated.</summary>
        public static void CallEclScript(ushort address)
        {
            GDVars.eclIp = address;
            eclStop = 0;
            while (eclStop == 0 && !GDVars.partyDefeated)
            {
                GDVars.curOpcode = GDVars.zone3[GDVars.eclIp];
                // STING Ctrl-L's trace log (#612)
                if (GDVars.debugTraceEnabled)
                    GDVars.debugFile.WriteLine(EclTrace.OpName(GDVars.curOpcode));
                ExecNextEclCmd();
            }
            eclStop = 0;
        }

        // Refresh the event byte, run the new script's on-load entry, set the wilderness location type, redraw, then
        // run scriptEntryAddrs[1] and the search entry; repeats while that entry itself runs NEWECL.
        internal static void NewEclDone()
        {
            Types.TCharRecord savedCur;
            do
            {
                GDVars.eventAtCell = View3D.GetMapEvent(GDVars.gMapX, GDVars.gMapY);
                GDVars.zone1[C_Gbvm.LEAVE_MAP] = 0;
                savedCur = GDVars.curChar;
                GDVars.locTypePrev = GDVars.gLocType;
                GDVars.gLocType = C_Area.LOC_3D_MAP;
                CallEclScript(GDVars.scriptEntryAddrs[5]);
                GDVars.ifcState = C_Ifc.INDOORS;
                if (GDVars.zone0.view3dFlag == 0)
                    switch (GDVars.eclScriptId)
                    {
                        case WILD_WEST_SCRIPT:
                            GDVars.gLocType = C_Area.LOC_WILD_WEST;
                            GDVars.ifcState = C_Ifc.OUTDOORS;
                            break;
                        case WILD_CENTRAL_SCRIPT:
                            GDVars.gLocType = C_Area.LOC_WILD_CENTRAL;
                            GDVars.ifcState = C_Ifc.OUTDOORS;
                            if (GDVars.zone0.yarashMissionDone == 254 && GDVars.zone0.yarashPyramidLeft == 0)
                                Game.YarashComplete(true);
                            break;
                        case WILD_EAST_SCRIPT:
                            GDVars.gLocType = C_Area.LOC_WILD_EAST;
                            GDVars.ifcState = C_Ifc.OUTDOORS;
                            break;
                    }
                if (GDVars.locTypePrev > C_Area.LOC_3D_MAP && GDVars.gLocType == C_Area.LOC_3D_MAP)
                    Border.DrawWindow(3, 3, 13, 13, 0, LightGreen, "");
                if (((GDVars.locTypePrev > C_Area.LOC_3D_MAP || GDVars.gLocType == C_Area.LOC_3D_MAP) && filesLoaded != 0) ||
                    (GDVars.locTypePrev == C_Area.LOC_3D_MAP && GDVars.gLocType == C_Area.LOC_3D_MAP))
                    Game.DisplayMapView();
                newEclFlag = 0;
                CallEclScript(GDVars.scriptEntryAddrs[1]);
                if (newEclFlag == 0)
                {
                    CallEclScript(GDVars.scriptEntryAddrs[C_Ecl.ENTRY_SEARCH]);
                    GDVars.curChar = savedCur;
                    CharDisp.DisplayParty(GDVars.curChar);
                }
            } while (newEclFlag != 0);
        }

        /// <summary>
        /// Main game loop: load and initialize the area's script and run its on-load entry, then repeat the map menu,
        /// camp, search and movement until the party is defeated or a quick load is asked for.
        /// </summary>
        public static void GameLoop()
        {
            char cmd;
            piecesLoaded = 0;
            mapLoaded = 0;
            restoreChar = 0;
            filesLoaded = 0;
            enteringLoop = 1;
            GDVars.ifcState = C_Ifc.INDOORS;
            newEclFlag = 0;
            GDVars.eclScriptId = B(GDVars.zone0.previousScriptId);
            switch (GDVars.eclScriptId)
            {
                case WILD_WEST_SCRIPT: case WILD_CENTRAL_SCRIPT: case WILD_EAST_SCRIPT:
                    if (GDVars.zone0.view3dFlag == 0)
                        GDVars.ifcState = C_Ifc.OUTDOORS;
                    break;
            }
            // the area's script is loaded from the data even for a saved game (#609)
            EclMenu.LoadEclScript(GDVars.eclScriptId);
            EclMenu.EclInit();
            CallEclScript(GDVars.scriptEntryAddrs[5]);
            if (GDVars.ifcState != C_Ifc.OUTDOORS)
            {
                if (enteringLoop == 1)
                {
                    CharDisp.DisplayNoncombatInterface();
                    enteringLoop = 0;
                }
                Game.DisplayMapView();
            }
            GDVars.zone0.previousScriptId = GDVars.eclScriptId;
            do
            {
                cmd = Explore.MapMainMenu();
                savedChar = GDVars.curChar;
                GDVars.zone0.previousScriptId = GDVars.eclScriptId;
                while ((GDVars.zone1[C_Gbvm.SEARCH_FLAGS] > 1 || UpCase(cmd) == 'E') &&
                       !GDVars.partyDefeated && !Explore.QuickLoadAsked)
                {
                    if (UpCase(cmd) == 'E')
                        EnterCamp();
                    else
                    {
                        GDVars.dsSearchModeFlag = B(GDVars.zone1[C_Gbvm.SEARCH_FLAGS] & 1);
                        GDVars.zone1[C_Gbvm.SEARCH_FLAGS] = 1;
                        Game.DisplayMapView();
                        CallEclScript(GDVars.scriptEntryAddrs[C_Ecl.ENTRY_SEARCH]);
                        if (newEclFlag != 0)
                            NewEclDone();
                        GDVars.zone1[C_Gbvm.SEARCH_FLAGS] = GDVars.dsSearchModeFlag;
                    }
                    if (!GDVars.partyDefeated)
                    {
                        cmd = Explore.MapMainMenu();
                        savedChar = GDVars.curChar;
                    }
                }
                // a quick load asked for on the map menu ends the loop at once
                if (!Explore.QuickLoadAsked)
                {
                    if (!GDVars.partyDefeated)
                        CallEclScript(GDVars.scriptEntryAddrs[1]);
                    if (newEclFlag != 0)
                        NewEclDone();
                    else if (!GDVars.partyDefeated)
                    {
                        GDVars.zone0.previousX = W(GDVars.gMapX);
                        GDVars.zone0.previousY = W(GDVars.gMapY);
                        Explore.ResolveMovement();
                        Game.DisplayMapView();
                        // the step sound, for a singleStep driver (POR-0170)
                        if (GDVars.gMapX != GDVars.zone0.previousX || GDVars.gMapY != GDVars.zone0.previousY)
                            Game.SoundCommand(GDConsts.soundIds.combat[C_Sound.STEP]);
                        GDVars.eclGraphics = false;
                        GDVars.picDrawn = true;
                        CallEclScript(GDVars.scriptEntryAddrs[C_Ecl.ENTRY_SEARCH]);
                        if (newEclFlag != 0)
                            NewEclDone();
                    }
                }
            } while (!(GDVars.partyDefeated || Explore.QuickLoadAsked));
            GDVars.partyDefeated = false;
        }

        /// <summary>Reset the unit's variables (Pascal unit globals start zeroed); for hosts and tests.</summary>
        public static void ResetUnitState()
        {
            eclStop = 0; newEclFlag = 0; restoreChar = 0; filesLoaded = 0; enteringLoop = 0; piecesLoaded = 0;
            mapLoaded = 0; creatureCnt = 0; savedChar = null; monId = 0; monPicId = 0; monstersOn = 0;
        }
    }
}
