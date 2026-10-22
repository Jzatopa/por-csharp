using System;
using System.Text;
using static Por.Core.Types;

namespace Por.Core
{
    public interface IEclMenuHost
    {
        void DisplayMapView();
        void DisplayParty(TCharRecord curChar);
        void PrependItemCopy(ref TItemRec list, TItemRec item);
        byte TheftChance(byte chance, ushort weight);
        void RemoveItem(TCharRecord creature, TItemRec item);
        bool HasEffect(TCharRecord creature, byte id, ref TEffectRec effect);
        void CallEffectHandler(byte id, TCharRecord creature, object arg, bool removed);
        void InflictDamage(TCharRecord creature, ushort dmg);
        void AddCharToParty(TCharRecord creature);
        void LoadCharIconAnim(bool flag);
    }

    public static class EclMenu
    {
        public static IEclMenuHost Host;

        private static byte picIdShown;

        private static IEclMenuHost Required()
        {
            unchecked
            {
                if (Host == null) throw new NotSupportedException("EclMenu gameplay host not bound; call FinalGameplayBindings.Bind().");
                return Host;
            }
        }

        public static void EclInit()
        {
            unchecked
            {
                GDVars.eclGraphics = false;
                GDVars.protTextShown = 0;
                GDVars.dumpCr6C00 = false;
                GDVars.dumpCr6B00 = false;
                GDVars.wallColorSel = 65;
                GDVars.ceilFloorColorSel = 9;
                GDVars.picRegionW = true;
                for (int i = 0; i < GDVars.dsMonsterFlags.Data.Length; i++) GDVars.dsMonsterFlags[i] = false;
                GDVars.gIconAnim = 8;
                GDVars.eclIp = 0x9900;
                GDVars.breathAim = false;
                GDVars.dsEclStackTopPtr = null;
                GDVars.dsEclStackSecondPtr = null;
                for (int i = 0; i < GDVars.cmpFlags.Length; i++) GDVars.cmpFlags[i] = false;
                GDVars.zone1[C_Gbvm.PIC_LOADING] = 255;
                GDVars.zone1[C_Gbvm.REST_SAFE_TIME] = 0;
                GDVars.zone1[C_Gbvm.REST_INTERRUPT_PCT] = 0;
                GDVars.zone0.antimagic = 0;
                EclVars.LoadEclVars(1);
                GDVars.scriptEntryAddrs[1] = EclVars.CombineBytesToWord(GDVars.dsGbvmLowerByte[1], GDVars.dsGbvmUpperByte[1]);
                EclVars.LoadEclVars(1);
                GDVars.scriptEntryAddrs[C_Ecl.ENTRY_SEARCH] = EclVars.CombineBytesToWord(GDVars.dsGbvmLowerByte[1], GDVars.dsGbvmUpperByte[1]);
                EclVars.LoadEclVars(1);
                GDVars.scriptEntryAddrs[3] = EclVars.CombineBytesToWord(GDVars.dsGbvmLowerByte[1], GDVars.dsGbvmUpperByte[1]);
                EclVars.LoadEclVars(1);
                GDVars.scriptEntryAddrs[4] = EclVars.CombineBytesToWord(GDVars.dsGbvmLowerByte[1], GDVars.dsGbvmUpperByte[1]);
                EclVars.LoadEclVars(1);
                GDVars.scriptEntryAddrs[5] = EclVars.CombineBytesToWord(GDVars.dsGbvmLowerByte[1], GDVars.dsGbvmUpperByte[1]);
                GDVars.zone0.view3dFlag = 1;
                if (GDVars.loadedSave == 0)
            {
                    for (int i = 1; i <= 32; i++)
                    {
                        GDVars.zone0.words[i + 0x49FF] = 0;
                    }
                    for (int i = 0; i <= 9; i++)
                    {
                        GDVars.zone1[i + 0x6E79] = 0;
                    }
                }
                else
            {
                    GDVars.loadedSave = 0;
                }
            }
        }

        public static void LoadEclScript(byte scriptId)
        {
            unchecked
            {
                for (int i = 0; i < 0x2700; i++) GDVars.zone3.Data[0x9900 + i] = 0;
                GDVars.dsSuppressingInsertDiskFlag = false;
                TextRend.ClearBottomLine();
                TextRend.DisplayString(0, 24, 15, "Loading...Please Wait");
                if (GDVars.virtualDisk < 1 || GDVars.virtualDisk > Types.LAST_DISK)
            {
                    DaxIo.PushState(1);
                }
                string s = GDVars.virtualDisk.ToString();
                if (DaxIo.ArchiveName("ECL" + s) != "ECL" + s)
            {
                    if (!DaxIo.DiskHasBlock("ECL" + s, scriptId))
                    {
                        byte disk = 1;
                        while (disk <= 8)
                        {
                            s = disk.ToString();
                            if (DaxIo.DiskHasBlock("ECL" + s, scriptId))
                            {
                                DaxIo.PushState(disk);
                                disk = 8;
                            }
                            disk++;
                        }
                        s = GDVars.virtualDisk.ToString();
                    }
                }
                ushort sizeRead;
                byte[] data;
                DaxIo.LoadDax("ECL" + s, ".dax", scriptId, out sizeRead, out data);
                if (sizeRead < 2)
            {
                    GDVars.dsSuppressingInsertDiskFlag = true;
                    byte disk = 1;
                    while (sizeRead < 2 && disk <= 8)
                    {
                        if (disk != GDVars.virtualDisk)
                        {
                            s = disk.ToString();
                            DaxIo.LoadDax("ECL" + s, ".dax", scriptId, out sizeRead, out data);
                            if (sizeRead >= 2)
                            {
                                DaxIo.PushState(disk);
                            }
                        }
                        disk++;
                    }
                    GDVars.dsSuppressingInsertDiskFlag = false;
                    if (sizeRead < 2)
                    {
                        s = scriptId.ToString();
                        DaxIo.PromptBox(14, "Script area " + s + " is missing");
                        GameIo.ExitGame();
                    }
                }
                int n = sizeRead - 2;
                if (n > 0x2700) n = 0x2700;
                for (int i = 0; i < n; i++)
            {
                    GDVars.zone3.Data[0x9900 + i] = data[2 + i];
                }
                TextRend.ClearBottomLine();
            }
        }

        public static void InitCreature(byte id, ref TCharRecord creature, ref TItemRec inv, ref TEffectRec effect)
        {
            unchecked
            {
                creature = new TCharRecord();
                LoadCreatureData(id, creature);
                inv = creature.itemsAddr;
                effect = creature.effectsAddr;
            }
        }

        public static byte MaxMonsterDist(byte x, byte y, byte facing)
        {
            unchecked
            {
                bool blocked = false;
                byte dist = 0;
                byte result = 0;
                if (GDVars.zone0.view3dFlag == 0)
            {
                    result = 2;
                    GDVars.zone1[C_Gbvm.MONSTER_DIST] = 2;
                }
                else
            {
                    while (dist < 2 && !blocked)
                    {
                        if (View3D.GetWallType((sbyte)x, (sbyte)y, facing) == 0)
                        {
                            dist++;
                            result = dist;
                            switch (facing)
                            {
                                case C_Dir.NORTH: y--; break;
                                case C_Dir.EAST: x++; break;
                                case C_Dir.SOUTH: y++; break;
                                case C_Dir.WEST: x--; break;
                            }
                        }
                        else
                        {
                            blocked = true;
                        }
                    }
                }
                return result;
            }
        }

        public static void DisplayPortrait(byte headId, byte bodyId)
        {
            unchecked
            {
                GDVars.picDrawn = false;
                GDVars.dsCurrentPortraitHead = headId;
                GDVars.dsCurrentPortraitHead2 = bodyId;
                Portrait.LoadPortraitBufs(headId, bodyId);
                Portrait.DisplayPortraitBufs(3, 3, true);
            }
        }

        public static void DisplayMonster(byte sprId, byte picId, byte dist, TMonFlags flags)
        {
            unchecked
            {
                if (!flags[1] && flags[0])
            {
                    Required().DisplayMapView();
                }
                byte d3 = (byte)(dist + 1);
                if (!flags[1])
            {
                    if (!flags[0])
                    {
                        if (GDVars.automapOn)
                        {
                            GDVars.automapOn = false;
                            Required().DisplayMapView();
                        }
                        Portrait.LoadAnimation("SPRIT", sprId, 1, ref GDVars.animData);
                        flags[0] = true;
                        GDVars.sprShown = true;
                    }
                    if (GDVars.gLocType == C_Area.LOC_3D_MAP)
                    {
                        Portrait.Show3DSprite(d3, GDVars.animData);
                    }
                    else
                    {
                        Portrait.Show3DSprite(2, GDVars.animData);
                    }
                }
                if (flags[1])
            {
                    if (picIdShown == GDVars.zone1[C_Gbvm.PIC_LOADING])
                    {
                        return;
                    }
                }
                if (dist != 0) return;
                if (GDVars.gLocType != C_Area.LOC_3D_MAP) return;
                if (GDVars.inEncMenu != 0 && GDVars.monsterPics == 0) return;
                picIdShown = (byte)GDVars.zone1[C_Gbvm.PIC_LOADING];
                GDVars.eclGraphics = true;
                if (GDVars.zone1[C_Gbvm.PIC_LOADING] == 255)
            {
                    Portrait.LoadAnimation("PIC", picId, 0, ref GDVars.animData);
                    flags[1] = true;
                    Portrait.DisplayImageBuf(3, 3, true, GDVars.animData.frames[1].pic);
                }
                else
            {
                    DisplayPortrait((byte)GDVars.zone1[C_Gbvm.PIC_LOADING], picId);
                    flags[1] = true;
                    GDVars.picDrawn = false;
                }
            }
        }

        // Pascal String retains its stale tail after Delete; the for-loop bound is fixed.
        private static void MenuToOptions(ref string menu, ref string opts)
        {
            unchecked
            {
                byte[] buffer = new byte[256];
                int length = Math.Min(menu.Length, 255), originalLength = length;
                for (int i = 1; i <= length; i++) buffer[i] = (byte)menu[i - 1];
                opts = "";
                for (int i = 1; i <= originalLength; i++)
            {
                    byte c = buffer[i];
                    if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z')) buffer[i] = (byte)(c + 32);
                    if (buffer[i] == '~')
                    {
                        if (i <= length)
                        {
                            for (int j = i; j < length; j++) buffer[j] = buffer[j + 1];
                            length--;
                        }
                        if (buffer[i] >= 'a' && buffer[i] <= 'z') buffer[i] -= 32;
                        if (opts.Length < 40) opts += (char)buffer[i];
                    }
                }
                var result = new StringBuilder(length);
                for (int i = 1; i <= length; i++) result.Append((char)buffer[i]);
                menu = result.ToString();
            }
        }

        public static byte MenuHorizontalCS(string query, string menu, byte qCol, byte mCol, byte hlCol, bool enterOk, bool animate)
        {
            unchecked
            {
                query = query.Substring(0, Math.Min(40, query.Length));
                string opts = "";
                MenuToOptions(ref menu, ref opts);
                GDVars.horizChoice = 1;
                char ch;
                bool ext = false;
                do
            {
                    ch = (char)MenuCore.MenuHorizontal(query, menu, qCol, mCol, hlCol, true, animate, ref ext);
                    if (ext)
                    {
                        Party.SelectPrevNextChar(ch);
                        Required().DisplayParty(GDVars.curChar);
                        ch = (char)C_Keys.NONE;
                    }
                } while (!((ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z') || (ch == C_Keys.ENTER && enterOk)));
                if (ch == C_Keys.ENTER)
            {
                    return 0;
                }
                int idx = 1;
                while (idx <= opts.Length && opts[idx - 1] != ch)
            {
                    idx++;
                }
                if (idx <= opts.Length)
            {
                    return (byte)(idx - 1);
                }
                return 255;
            }
        }

        public static void EclVerticalMenu(string query, string menu, byte qCol, byte mCol, byte hlCol, sbyte ulCol, sbyte ulRow, sbyte lrCol, sbyte lrRow, TItemRec list, bool addExit, ref bool redraw, ref short idx, ref TItemRec sel)
        {
            unchecked
            {
                query = query.Substring(0, Math.Min(40, query.Length));
                string opts = "";
                MenuToOptions(ref menu, ref opts);
                MenuCore.MenuVertical(query, menu, qCol, mCol, hlCol, ulCol, ulRow, lrCol, lrRow, list, addExit, ref redraw, ref idx, ref sel);
            }
        }

        public static void EclCompareStr(string s1, string s2)
        {
            unchecked
            {
                s1 = s1.Substring(0, Math.Min(255, s1.Length));
                s2 = s2.Substring(0, Math.Min(255, s2.Length));
                for (int i = 0; i < GDVars.cmpFlags.Length; i++) GDVars.cmpFlags[i] = false;
                if (string.CompareOrdinal(s1, s2) == 0) GDVars.cmpFlags[0] = true;
                if (string.CompareOrdinal(s1, s2) != 0) GDVars.cmpFlags[1] = true;
                if (string.CompareOrdinal(s1, s2) < 0) GDVars.cmpFlags[2] = true;
                if (string.CompareOrdinal(s1, s2) > 0) GDVars.cmpFlags[3] = true;
                if (string.CompareOrdinal(s1, s2) <= 0) GDVars.cmpFlags[4] = true;
                if (string.CompareOrdinal(s1, s2) >= 0) GDVars.cmpFlags[5] = true;
            }
        }

        public static void EclCompareInt(ushort v1, ushort v2)
        {
            unchecked
            {
                for (int i = 0; i < GDVars.cmpFlags.Length; i++) GDVars.cmpFlags[i] = false;
                if (v1 == v2) GDVars.cmpFlags[0] = true;
                if (v1 != v2) GDVars.cmpFlags[1] = true;
                if (v1 < v2) GDVars.cmpFlags[2] = true;
                if (v1 > v2) GDVars.cmpFlags[3] = true;
                if (v1 <= v2) GDVars.cmpFlags[4] = true;
                if (v1 >= v2) GDVars.cmpFlags[5] = true;
            }
        }

        public static void EclGosub(byte varIdx)
        {
            unchecked
            {
                if (GDVars.dsEclStackTopPtr == null)
            {
                    GDVars.dsEclStackTopPtr = new TEclRet();
                    GDVars.dsEclStackTopPtr.next = null;
                }
                else
            {
                    GDVars.dsEclStackSecondPtr = GDVars.dsEclStackTopPtr;
                    GDVars.dsEclStackTopPtr = new TEclRet();
                    GDVars.dsEclStackTopPtr.next = GDVars.dsEclStackSecondPtr;
                }
                GDVars.dsEclStackTopPtr.addr = GDVars.eclIp;
                GDVars.eclIp = EclVars.CombineBytesToWord(GDVars.dsGbvmLowerByte[varIdx], GDVars.dsGbvmUpperByte[varIdx]);
            }
        }

        public static void EclMoveForward()
        {
            unchecked
            {
                switch (GDVars.gFacing)
            {
                    case C_Dir.NORTH:
                        if (GDVars.gMapY > 0) GDVars.gMapY--;
                        else GDVars.gMapY = 15;
                        break;
                    case C_Dir.EAST:
                        if (GDVars.gMapX < 15) GDVars.gMapX++;
                        else GDVars.gMapX = 0;
                        break;
                    case C_Dir.SOUTH:
                        if (GDVars.gMapY < 15) GDVars.gMapY++;
                        else GDVars.gMapY = 0;
                        break;
                    case C_Dir.WEST:
                        if (GDVars.gMapX > 0) GDVars.gMapX--;
                        else GDVars.gMapX = 15;
                        break;
                }
                GDVars.eventAtCell = View3D.GetMapEvent(GDVars.gMapX, GDVars.gMapY);
                GDVars.wallAhead = View3D.GetWallType(GDVars.gMapX, GDVars.gMapY, GDVars.gFacing);
            }
        }

        public static void CharFightsAlone(sbyte mirror)
        {
            unchecked
            {
                GDVars.duelFlag = true;
                GDVars.zone1[C_Gbvm.MIRROR_DUEL] = (ushort)(short)mirror;
                TCharRecord cur = GDVars.curChar;
                TCharRecord p = GDVars.creatureList;
                while (p != null)
            {
                    if (!string.Equals(p.name, cur.name, StringComparison.Ordinal))
                    {
                        p.enabled = 0;
                    }
                    p = p.nextCharAddr;
                }
                if (mirror == 0) return;
                MapTiles.LoadIconAnim("CPIC", 11, GDVars.gIconAnim);
                TCharRecord last = Party.LastCreature();
                TCharRecord clone = new TCharRecord();
                EclVm.CopyCharRecord(cur, clone);
                clone.enabled = 1;
                clone.nextCharAddr = null;
                clone.name = "ROLF";
                clone.quickfight = 1;
                clone.hostile = 1;
                clone.npc = C_Npc.ANIMATED_NPC;
                clone.orderNumber = GDVars.gIconAnim;
                clone.effectsAddr = null;
                clone.itemsAddr = null;
                last.nextCharAddr = clone;
                TItemRec srcIt = cur.itemsAddr;
                while (srcIt != null)
            {
                    Required().PrependItemCopy(ref clone.itemsAddr, srcIt);
                    srcIt = srcIt.next;
                }
            }
        }

        public static void RobMoney(double fraction, TCharRecord creature)
        {
            unchecked
            {
                creature.coins[C_Money.COPPER] = Real48Math.ScaleCoins(creature.coins[C_Money.COPPER], fraction);
                creature.coins[C_Money.SILVER] = Real48Math.ScaleCoins(creature.coins[C_Money.SILVER], fraction);
                creature.coins[C_Money.ELECTRUM] = Real48Math.ScaleCoins(creature.coins[C_Money.ELECTRUM], fraction);
                creature.coins[C_Money.GOLD] = Real48Math.ScaleCoins(creature.coins[C_Money.GOLD], fraction);
                creature.coins[C_Money.PLATINUM] = Real48Math.ScaleCoins(creature.coins[C_Money.PLATINUM], fraction);
                creature.coins[C_Money.GEMS] = Real48Math.ScaleCoins(creature.coins[C_Money.GEMS], fraction);
                creature.coins[C_Money.JEWELRY] = Real48Math.ScaleCoins(creature.coins[C_Money.JEWELRY], fraction);
            }
        }

        public static void RobItems(byte chance, TCharRecord creature)
        {
            unchecked
            {
                TItemRec item = creature.itemsAddr;
                while (item != null)
            {
                    byte roll = Dice.RollDice(1, 100);
                    TItemRec nxt = item.next;
                    if (roll <= Required().TheftChance(chance, item.weight))
                    {
                        if (item.equipped != 0)
                        {
                            item.equipped = 0;
                            if (item.special > 0x7F)
                            {
                                Required().CallEffectHandler(item.special, creature, item, true);
                            }
                        }
                        Required().RemoveItem(creature, item);
                    }
                    item = nxt;
                }
            }
        }

        public static void MoveExtremes(ref byte hiMove, ref byte loMove)
        {
            unchecked
            {
                TCharRecord p = GDVars.creatureList;
                hiMove = p.movementCur;
                loMove = p.movementCur;
                while (p != null)
            {
                    byte m = p.movementCur;
                    TEffectRec effect = null;
                    if (Required().HasEffect(p, C_Effect.HASTED, ref effect))
                    {
                        m = (byte)(m * 2);
                    }
                    else if (Required().HasEffect(p, C_Effect.SLOWED, ref effect))
                    {
                        m = (byte)(m / 2);
                    }
                    if (m > hiMove) hiMove = m;
                    if (m < loMove) loMove = m;
                    p = p.nextCharAddr;
                }
            }
        }

        public static void EclInflictDamage(ushort dmg, TCharRecord creature)
        {
            unchecked
            {
                if (creature.status == C_Status.DEAD || creature.status == C_Status.STONED || creature.status == C_Status.GONE)
            {
                    return;
                }
                bool pause = false;
                string msg;
                if (creature.hpCur + 10 < dmg)
            {
                    msg = "  " + creature.name + " dies. ";
                }
                else
            {
                    msg = "  " + creature.name + " is hit FOR " + StrUtil.NumStr(dmg) + " points of Damage.";
                }
                if (GDVars.curRow > C_Msg.TEXT_BOTTOM)
            {
                    GDVars.curRow = C_Msg.MESSAGE_TOP;
                    pause = true;
                    TextRend.DisplayAndPause(15, "press <enter>/<return> to continue");
                }
                else
            {
                    pause = false;
                }
                GDVars.curCol = C_Msg.TEXT_RIGHT;
                TextRend.ShowMessage(15, pause, msg);
                Required().InflictDamage(creature, dmg);
                TextRend.ClearRect(17, 1, 38, 15);
                Required().DisplayParty(GDVars.curChar);
            }
        }

        public static void LoadCreatureData(byte id, TCharRecord creature)
        {
            unchecked
            {
                string numStr = GDVars.virtualDisk.ToString();
                ushort size;
                byte[] data;
                DaxIo.LoadDax("MON" + numStr + "CHA", ".dax", id, out size, out data);
                if (size < 285)
            {
                    TextRend.DisplayAndPause(15, "Unable to load monster");
                    GameIo.ExitGame();
                }
                CharacterRecordCodec.DecodeMonster(data, 0, creature);
                switch (creature.race)
            {
                    case C_Race.DWARF:
                    case C_Race.GNOME:
                    case C_Race.HALFLING:
                        creature.iconSize = 1;
                        break;
                    case C_Race.ELF:
                    case C_Race.HALFELF:
                    case C_Race.HALFORC:
                    case C_Race.HUMAN:
                        creature.iconSize = 2;
                        break;
                }
                for (int i = 1; i <= 3; i++)
            {
                    byte roll = Dice.RollDice(1, 6);
                    creature.iconColors1[i] = (byte)(roll + ((roll + 8) << 4));
                }
                creature.iconColors1[4] = 0xC4;
                for (int i = 5; i <= 6; i++)
            {
                    byte roll = Dice.RollDice(1, 6);
                    creature.iconColors1[i] = (byte)(roll + ((roll + 8) << 4));
                }
                GDVars.dsSuppressingInsertDiskFlag = true;
                DaxIo.LoadDax("MON" + numStr + "SPC", ".dax", id, out size, out data);
                if (size != 0)
            {
                    if (size >= 9)
                    {
                        int offset = 0;
                        TEffectRec effect = new TEffectRec();
                        creature.effectsAddr = effect;
                        while (size - offset >= 9)
                        {
                            effect.id = data[offset];
                            effect.duration = (ushort)(data[offset + 1] | (data[offset + 2] << 8));
                            effect.strength = data[offset + 3];
                            effect.onRemove = data[offset + 4] != 0;
                            effect.next = null;
                            offset += 9;
                            if (size - offset >= 9)
                            {
                                TEffectRec next = new TEffectRec();
                                effect.next = next;
                                effect = next;
                            }
                        }
                    }
                }
                DaxIo.LoadDax("MON" + numStr + "ITM", ".dax", id, out size, out data);
                if (size != 0)
            {
                    if (size >= 63)
                    {
                        TItemRec previous = null;
                        for (int offset = 0; size - offset >= 63; offset += 63)
                        {
                            TItemRec item = EclVm.ItemFromBytes(data, offset);
                            if (previous == null) creature.itemsAddr = item;
                            else previous.next = item;
                            previous = item;
                        }
                    }
                }
                GDVars.dsSuppressingInsertDiskFlag = false;
            }
        }

        public static void AddCreatureToParty(byte id)
        {
            unchecked
            {
                if (GDVars.zone1[C_Gbvm.PARTY_SIZE] >= Types.MAX_PARTY) return;
                TCharRecord creature = new TCharRecord();
                LoadCreatureData(id, creature);
                creature.randByte = (byte)Dice.RandomBelow(256);
                byte cnt = 0;
                byte[] classes = new byte[5];
                for (int i = C_Level.CLERIC; i <= C_Level.THIEF; i++)
            {
                    if (creature.classLevels[i] > 0 && cnt < 4)
                    {
                        cnt++;
                        classes[cnt] = (byte)i;
                    }
                }
                if (cnt == 0)
            {
                    cnt = 1;
                    classes[cnt] = C_Level.FIGHTER;
                }
                byte picked = Dice.RollDice(1, cnt);
                switch (classes[picked])
            {
                    case C_Level.CLERIC:
                        creature.portraitBody = GDConsts.npcClericBodies[creature.genderSp, Dice.RollDice(1, 5)];
                        break;
                    case C_Level.MAGE:
                        creature.portraitBody = GDConsts.npcMageBodies[creature.genderSp, Dice.RollDice(1, 5)];
                        break;
                    case C_Level.THIEF:
                        creature.portraitBody = GDConsts.npcThiefBodies[creature.genderSp, Dice.RollDice(1, 5)];
                        break;
                    default:
                        creature.portraitBody = GDConsts.npcFighterBodies[creature.genderSp, Dice.RollDice(1, 5)];
                        break;
                }
                creature.portraitHead = GDConsts.npcPortraitHeads[creature.genderSp, Dice.RollDice(1, 8)];
                Required().AddCharToParty(creature);
                Required().LoadCharIconAnim(true);
            }
        }
    }
}
