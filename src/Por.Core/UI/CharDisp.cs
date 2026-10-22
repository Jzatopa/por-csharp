// Source: Talonius pool-of-radiance pinned 7616920a (src/chardisp.pas); Qwen3.8:27b initial draft, supervisor repaired.
using System;


namespace Por.Core
{
    // Platform boundary: explicit combat map binding (CbtMapBindings.Bind). Supervisor-written, as specified in the contract.
    public interface ICombatMapHost
    {
        void SetCombatPalette();
        bool ViewCoordsValid(sbyte x, sbyte y);
        bool CombatIconVisible(Types.TCharRecord creature, byte whole);
        void ScrollView(sbyte x, sbyte y, byte delta, byte dir);
        sbyte CombatCellX(Types.TCharRecord creature);
        sbyte CombatCellY(Types.TCharRecord creature);
        byte CombatSlot(Types.TCharRecord creature);
    }

    public static class CharDisp
    {
        private static byte messageTop;

        public static ICombatMapHost CombatMap;

        private static ICombatMapHost Cbt()
        {
            if (CombatMap == null)
                throw new NotSupportedException("CbtMap is not bound; call CbtMapBindings.Bind");
            return CombatMap;
        }

        internal static Action<ushort> DelayMs = ms => { if (TextRend.DelayMilliseconds == null) throw new NotSupportedException("no delay host"); TextRend.DelayMilliseconds(ms); };

        // Supervisor-added seams: each defaults to the converted routine the Pascal unit calls, so tests can log the calls.
        internal delegate void LoadAnimationProc(string name, byte picId, byte transp, ref Types.TAnimRec a);
        internal static Action<short, short, byte, byte, Types.TImgBuf, Types.TImgBuf> BlitClipped = Blit.BlitTileClipped;
        internal static Action FlushViewCall = Blit.FlushView;
        internal static Action<ushort> SoundSetAutoStopCall = SoundDrv.SoundSetAutoStop;
        internal static Action<ushort> SoundCommandCall = SoundDrv.SoundCommand;
        internal static Action DisplayMapViewCall = MapView.DisplayMapView;
        internal static Action DisplayWildMapViewCall = MapView.DisplayWildMapView;
        internal static Action DrawWildWindowsCall = Wilds.DrawWildWindows;
        internal static Action<byte, byte> LoadPortraitBufsCall = Portrait.LoadPortraitBufs;
        internal static Action<byte, byte, bool> DisplayPortraitBufsCall = Portrait.DisplayPortraitBufs;
        internal static LoadAnimationProc LoadAnimationCall = Portrait.LoadAnimation;
        internal static Action<short, short, bool, Types.TImgBuf> DisplayImageBufCall = Portrait.DisplayImageBuf;

        private const byte Black = 0;
        private const byte White = 15;
        private const byte LightGreen = 10;
        private const byte Yellow = 14;
        private const byte LightMagenta = 13;
        private const byte LightRed = 12;
        private const byte LightCyan = 11;

        public static void DisplayParty(Types.TCharRecord hl)
        {
            byte col;
            if (GDVars.ifcState == C_Ifc.MAIN_MENU)
                col = 1;
            else
                col = 17;

            byte row = 2;
            TextRend.DisplayString(col, row, White, "Name");
            TextRend.DisplayString(33, row, White, "AC  HP");
            row = unchecked((byte)(row + 2));

            Types.TCharRecord p = GDVars.creatureList;
            while (p != null)
            {
                Border.ClearBox(unchecked((sbyte)col), unchecked((sbyte)row), unchecked((sbyte)38), unchecked((sbyte)row));
                if (p == hl)
                    TextRend.DisplayString(col, row, White, p.name);
                else
                    DisplayCreatureName(p, col, row, false);

                byte band;
                switch (p.acCur)
                {
                    case byte b when b >= 0 && b <= 50:
                        band = 1;
                        break;
                    case byte b when b >= 51 && b <= 60:
                        band = 2;
                        break;
                    case byte b when b >= 61 && b <= 69:
                        band = 1;
                        break;
                    default:
                        band = 0;
                        break;
                }
                PrintArmorClass(p, unchecked((byte)(band + 32)), row);

                switch (p.hpCur)
                {
                    case byte b when b >= 0 && b <= 9:
                        band = 2;
                        break;
                    case byte b when b >= 10 && b <= 99:
                        band = 1;
                        break;
                    default:
                        band = 0;
                        break;
                }
                PrintHitPoints(p, unchecked((byte)(band + 36)), row, false);

                row = unchecked((byte)(row + 1));
                p = p.nextCharAddr;
            }
            Border.ClearBox(unchecked((sbyte)col), unchecked((sbyte)row), unchecked((sbyte)38), unchecked((sbyte)row));
        }

        public static void PrintArmorClass(Types.TCharRecord creature, byte col, byte row)
        {
            string s = StrUtil.NumStr(Math.Abs(creature.acCur - 60));
            if (creature.acCur > 60)
                s = "-" + s;
            TextRend.DisplayString(col, row, LightGreen, s);
        }

        public static void PrintHitPoints(Types.TCharRecord creature, byte col, byte row, bool hotPink)
        {
            byte color;
            if (creature.hpCur < creature.hpMax)
                color = Yellow;
            else
                color = LightGreen;
            if (hotPink)
                color = LightMagenta;
            TextRend.DisplayString(col, row, color, StrUtil.NumStr(creature.hpCur));
        }

        public static void DisplayCreatureName(Types.TCharRecord creature, byte col, byte row, bool possessive)
        {
            byte color;
            if (creature.enabled == 0)
                color = LightRed;
            else if (creature.hostile == 1)
                color = Yellow;
            else if (CharHlp.CanTrain(creature))
                color = LightMagenta;
            else
                color = LightCyan;

            TextRend.DisplayString(col, row, color, creature.name);
            if (possessive)
                TextRend.DisplayString(unchecked((byte)(col + creature.name.Length)), row, color, "'s");
        }

        public static void CombatPrintCreatureInfo(Types.TCharRecord creature)
        {
            Border.ClearBox(unchecked((sbyte)23), unchecked((sbyte)1), unchecked((sbyte)38), unchecked((sbyte)21));
            byte row = 1;
            PrintNameString(creature, " ", row, false);
            row = unchecked((byte)(row + 1));
            TextRend.DisplayString(23, unchecked((byte)(row + 1)), LightGreen, "Hitpoints");
            PrintHitPoints(creature, 33, unchecked((byte)(row + 1)), false);
            row = unchecked((byte)(row + 2));
            TextRend.DisplayString(23, unchecked((byte)(row + 1)), LightGreen, "AC");
            PrintArmorClass(creature, 26, unchecked((byte)(row + 1)));
            GDVars.curRow = unchecked((byte)(row + 1));
            if (creature.equipped[C_Equip.WEAPON] != null)
            {
                row = unchecked((byte)(row + 2));
                CharHlp.ItemLabel(creature.equipped[C_Equip.WEAPON], false);
                TextRend.DisplayMessageWindow(unchecked((byte)23), unchecked((byte)(row + 1)), unchecked((byte)38), unchecked((byte)(row + 1 + 1 + 1)), LightGreen, true, creature.equipped[C_Equip.WEAPON].name);
            }
            row = unchecked((byte)(GDVars.curRow + 1));
            if (creature.enabled == 0)
                TextRend.DisplayString(23, unchecked((byte)(row + 1)), White, GDConsts.statusStrs[creature.status]);
            else if (CharHlp.IsHelpless(creature))
                TextRend.DisplayString(23, unchecked((byte)(row + 1)), White, "(Helpless)");
        }

        public static void PrintBottomLine(string s)
        {
            Border.ClearBox(unchecked((sbyte)0), unchecked((sbyte)24), unchecked((sbyte)39), unchecked((sbyte)24));
            TextRend.DisplayString(0, 24, LightGreen, s);
            TextRend.PauseOnly();
            Border.ClearBox(unchecked((sbyte)0), unchecked((sbyte)24), unchecked((sbyte)39), unchecked((sbyte)24));
        }

        private static byte OpenMessageWindow()
        {
            byte top;
            if (GDVars.dsSuppressDrawingBorderFlag != 0)
                top = unchecked((byte)(C_Msg.MESSAGE_TOP + 1));
            else
            {
                top = unchecked((byte)C_Msg.MESSAGE_TOP);
                Border.DrawBorder(unchecked((sbyte)C_Msg.TEXT_LEFT), unchecked((sbyte)top), unchecked((sbyte)C_Msg.TEXT_RIGHT), unchecked((sbyte)C_Msg.TEXT_BOTTOM), 0);
            }
            Border.ClearBox(unchecked((sbyte)C_Msg.TEXT_LEFT), unchecked((sbyte)top), unchecked((sbyte)C_Msg.TEXT_RIGHT), unchecked((sbyte)C_Msg.TEXT_BOTTOM));
            return top;
        }

        public static void PrintNameString(Types.TCharRecord creature, string s, byte row, bool pause)
        {
            if (GDVars.ifcState == C_Ifc.COMBAT)
            {
                Border.ClearBox(unchecked((sbyte)C_Msg.COMBAT_LEFT), unchecked((sbyte)row), unchecked((sbyte)C_Msg.TEXT_RIGHT), unchecked((sbyte)C_Msg.COMBAT_BOTTOM));
                DisplayCreatureName(creature, unchecked((byte)C_Msg.COMBAT_LEFT), row, false);
                TextRend.DisplayMessageWindow(unchecked((byte)C_Msg.COMBAT_LEFT), unchecked((byte)(row + 1)), unchecked((byte)C_Msg.TEXT_RIGHT), unchecked((byte)C_Msg.COMBAT_BOTTOM), LightGreen, true, s);
            }
            else
            {
                byte top = OpenMessageWindow();
                DisplayCreatureName(creature, 1, unchecked((byte)(top + 1)), false);
                TextRend.DisplayMessageWindow(unchecked((byte)C_Msg.TEXT_LEFT), unchecked((byte)(top + 1 + 1)), unchecked((byte)C_Msg.TEXT_RIGHT), unchecked((byte)C_Msg.TEXT_BOTTOM), LightGreen, true, s);
            }
            if (pause)
            {
                TextRend.PauseOnly();
                ClearMessageWindow();
            }
        }

        public static void PrintMessageWait(string s)
        {
            MessageText(s, LightGreen, true);
            MessageWait();
        }

        public static void MessageText(string s, byte color, bool first)
        {
            if (GDVars.ifcState == C_Ifc.COMBAT)
            {
                if (first)
                    Border.ClearBox(unchecked((sbyte)C_Msg.COMBAT_LEFT), unchecked((sbyte)C_Msg.COMBAT_MESSAGE_TOP), unchecked((sbyte)C_Msg.TEXT_RIGHT), unchecked((sbyte)C_Msg.COMBAT_BOTTOM));
                TextRend.DisplayMessageWindow(unchecked((byte)C_Msg.COMBAT_LEFT), unchecked((byte)C_Msg.COMBAT_MESSAGE_TOP), unchecked((byte)C_Msg.TEXT_RIGHT), unchecked((byte)C_Msg.COMBAT_BOTTOM), color, first, s);
            }
            else
            {
                if (first)
                    messageTop = OpenMessageWindow();
                TextRend.DisplayMessageWindow(unchecked((byte)C_Msg.TEXT_LEFT), unchecked((byte)(messageTop + 1)), unchecked((byte)C_Msg.TEXT_RIGHT), unchecked((byte)C_Msg.TEXT_BOTTOM), color, first, s);
            }
        }

        public static void MessageWait()
        {
            TextRend.DisplayAndPause(LightMagenta, "Press any key to continue");
            Border.ClearBox(unchecked((sbyte)0), unchecked((sbyte)24), unchecked((sbyte)39), unchecked((sbyte)24));
        }

        public static void ClearMessageWindow()
        {
            if (GDVars.ifcState == C_Ifc.COMBAT)
                Border.ClearBox(unchecked((sbyte)C_Msg.COMBAT_LEFT), unchecked((sbyte)C_Msg.COMBAT_MESSAGE_TOP), unchecked((sbyte)C_Msg.TEXT_RIGHT), unchecked((sbyte)C_Msg.COMBAT_BOTTOM));
            else
                Border.ClearBox(unchecked((sbyte)C_Msg.TEXT_LEFT), unchecked((sbyte)(C_Msg.MESSAGE_TOP + 1)), unchecked((sbyte)C_Msg.TEXT_RIGHT), unchecked((sbyte)C_Msg.TEXT_BOTTOM));
        }

        public static void CopyIconFacing(byte cell, byte half, byte row, bool mirror)
        {
            ushort stride = GDVars.dsMoveIconSheet.imgSize;
            ushort stride4 = Blit.MaskBytes(stride);
            if (mirror)
            {
                Types.TImgBuf tmp = default;
                Blit.AllocPic(24, 3, 1, 1, ref tmp);
                Blit.MirrorPic(GDVars.dsIconAnimation[cell, half], tmp);
                Array.Copy(tmp.data, 0, GDVars.dsMoveIconSheet.data, unchecked((ushort)(row * stride)), stride);
                Array.Copy(tmp.transp.Data, 0, GDVars.dsMoveIconSheet.transp.Data, unchecked((ushort)(row * stride4)), stride4);
                Blit.FreePic(ref tmp);
            }
            else
            {
                Array.Copy(GDVars.dsIconAnimation[cell, half].data, 0, GDVars.dsMoveIconSheet.data, unchecked((ushort)(row * stride)), stride);
                Array.Copy(GDVars.dsIconAnimation[cell, half].transp.Data, 0, GDVars.dsMoveIconSheet.transp.Data, unchecked((ushort)(row * stride4)), stride4);
            }
        }

        public static void BuildIconFacings(byte cell)
        {
            CharDisp.CopyIconFacing(cell, 0, 0, false);
            CharDisp.CopyIconFacing(cell, 0, 1, true);
            CharDisp.CopyIconFacing(cell, 1, 2, true);
            CharDisp.CopyIconFacing(cell, 1, 3, false);
        }

        public static void CombatAnimateMovingIcon(sbyte startX, sbyte startY, sbyte endX, sbyte endY, byte frames, byte pace)
        {
            byte[] steps = new byte[149];
            for (int i = 0; i < 149; i++) steps[i] = 8;
            Types.TPathReq req = new Types.TPathReq();
            byte n = 0;
            byte count;
            byte frame = 0;
            bool lastPass;
            bool done;
            sbyte camX;
            sbyte camY;
            sbyte prevX;
            sbyte prevY;
            sbyte accX;
            sbyte accY;
            sbyte dirX;
            sbyte dirY;
            sbyte stepX;
            sbyte stepY;
            sbyte pixX;
            sbyte pixY;

            prevX = startX;
            prevY = startY;
            n = 0;
            frame = 0;
            req.sx = unchecked((short)(startX * 3));
            req.sy = unchecked((short)(startY * 3));
            req.ex = unchecked((short)(endX * 3));
            req.ey = unchecked((short)(endY * 3));
            Pathing.PathFind(ref req);
            do
            {
                done = !Pathing.NextPathStep(ref req);
                steps[n] = req.step;
                n = unchecked((byte)(n + 1));
                if (n >= 148)
                    done = true;
            } while (!done);
            count = n;
            if (count < 2) return;
            dirX = unchecked((sbyte)(endX - startX));
            dirY = unchecked((sbyte)(endY - startY));
            if (!Cbt().ViewCoordsValid(unchecked((sbyte)(startX - GDVars.arenaPtr.ox)), unchecked((sbyte)(startY - GDVars.arenaPtr.oy))) ||
                !Cbt().ViewCoordsValid(unchecked((sbyte)(endX - GDVars.arenaPtr.ox)), unchecked((sbyte)(endY - GDVars.arenaPtr.oy))))
            {
                if (Math.Abs(dirX) <= 6 && Math.Abs(dirY) <= 6)
                {
                    lastPass = true;
                    camX = unchecked((sbyte)(startX + (sbyte)(dirX / 2)));
                    camY = unchecked((sbyte)(startY + (sbyte)(dirY / 2)));
                }
                else
                {
                    lastPass = false;
                    camX = unchecked((sbyte)(GDVars.arenaPtr.ox + 3));
                    camY = unchecked((sbyte)(GDVars.arenaPtr.oy + 3));
                }
            }
            else
            {
                lastPass = true;
                camX = unchecked((sbyte)(GDVars.arenaPtr.ox + 3));
                camY = unchecked((sbyte)(GDVars.arenaPtr.oy + 3));
            }
            Cbt().ScrollView(camX, camY, 255, 8);
            n = 0;
            accX = 0;
            accY = 0;
            do
            {
                pixX = unchecked((sbyte)((unchecked((sbyte)(startX - GDVars.arenaPtr.ox)) * 3 + accX)));
                pixY = unchecked((sbyte)((unchecked((sbyte)(startY - GDVars.arenaPtr.oy)) * 3 + accY)));
                prevX = startX;
                prevY = startY;
                done = false;
                do
                {
                    if (pace > 0 || (pixX % 3 == 0) || (pixY % 3 == 0))
                    {
                        BlitClipped(unchecked((short)pixX), unchecked((short)pixY), frame, 5, GDVars.dsMoveIconSheet, GDVars.dsViewImageBufferPtr);
                        FlushViewCall();
                        DelayMs(pace);
                        BlitClipped(unchecked((short)pixX), unchecked((short)pixY), 0, 0, GDVars.dsIconImageBufferPtr, GDVars.dsViewImageBufferPtr);
                        frame = unchecked((byte)(frame + 1));
                        if (frame >= frames)
                            frame = 0;
                    }
                    n = unchecked((byte)(n + 1));
                    stepX = GDConsts.dirDX[steps[n]];
                    stepY = GDConsts.dirDY[steps[n]];
                    pixX = unchecked((sbyte)(pixX + stepX));
                    pixY = unchecked((sbyte)(pixY + stepY));
                    if (pixX < 0 || pixX > 18 || pixY < 0 || pixY > 18)
                        done = true;
                    if (!done && n < count)
                    {
                        accX = unchecked((sbyte)(accX + stepX));
                        accY = unchecked((sbyte)(accY + stepY));
                        if (Math.Abs(accX) == 3)
                        {
                            startX = unchecked((sbyte)(startX + Pathing.SignOf(accX)));
                            camX = unchecked((sbyte)(camX + Pathing.SignOf(accX)));
                            accX = 0;
                        }
                        if (Math.Abs(accY) == 3)
                        {
                            startY = unchecked((sbyte)(startY + Pathing.SignOf(accY)));
                            camY = unchecked((sbyte)(camY + Pathing.SignOf(accY)));
                            accY = 0;
                        }
                    }
                } while (n < count && !done);
                if (n < count)
                {
                    dirX = unchecked((sbyte)Pathing.SignOf(unchecked((short)(prevX - startX))));
                    dirY = unchecked((sbyte)Pathing.SignOf(unchecked((short)(prevY - startY))));
                    if (dirX != 0)
                    {
                        if (startX >= unchecked((sbyte)(GDVars.arenaPtr.ox + 6)))
                            camX = unchecked((sbyte)(startX + 3));
                        else if (startX <= GDVars.arenaPtr.ox)
                            camX = unchecked((sbyte)(startX - 3));
                    }
                    if (dirY != 0)
                    {
                        if (startY >= unchecked((sbyte)(GDVars.arenaPtr.oy + 6)))
                            camY = unchecked((sbyte)(startY + 3));
                        else if (startY <= GDVars.arenaPtr.oy)
                            camY = unchecked((sbyte)(startY - 3));
                    }
                    Cbt().ScrollView(camX, camY, 255, 8);
                }
                else
                {
                    lastPass = true;
                    if (!Cbt().ViewCoordsValid(unchecked((sbyte)(endX - GDVars.arenaPtr.ox)), unchecked((sbyte)(endY - GDVars.arenaPtr.oy))))
                        Cbt().ScrollView(endX, endY, 3, 8);
                    pixX = unchecked((sbyte)((unchecked((sbyte)(endX - GDVars.arenaPtr.ox)) * 3)));
                    pixY = unchecked((sbyte)((unchecked((sbyte)(endY - GDVars.arenaPtr.oy)) * 3)));
                    BlitClipped(unchecked((short)pixX), unchecked((short)pixY), frame, 5, GDVars.dsMoveIconSheet, GDVars.dsViewImageBufferPtr);
                    if (pace > 0)
                    {
                        FlushViewCall();
                        DelayMs(pace);
                        BlitClipped(unchecked((short)pixX), unchecked((short)pixY), 0, 0, GDVars.dsIconImageBufferPtr, GDVars.dsViewImageBufferPtr);
                    }
                }
            } while (!lastPass);
            FlushViewCall();
        }

        public static void PrintNameStringAnim(Types.TCharRecord creature, bool effect, string s)
        {
            sbyte gx;
            sbyte gy;
            byte cell;
            byte j;
            byte reps;
            byte slot;
            if (GDVars.ifcState == C_Ifc.COMBAT)
            {
                SoundSetAutoStopCall(0);
                if (effect)
                    cell = 22;
                else
                    cell = 23;
                CharDisp.BuildIconFacings(cell);
                if (!Cbt().CombatIconVisible(creature, 1))
                    Cbt().ScrollView(Cbt().CombatCellX(creature), Cbt().CombatCellY(creature), 3, 8);
                if (effect)
                    SoundCommandCall(GDConsts.soundIds.combat[0]);
                else
                    SoundCommandCall(GDConsts.soundIds.damage);
                CharDisp.PrintNameString(creature, s, 10, false);
                if (effect)
                    reps = GDVars.gameDelay;
                else
                    reps = 0;
                slot = Cbt().CombatSlot(creature);
                if (slot > 0)
                {
                    gx = unchecked((sbyte)(GDVars.gridX[slot] * 3));
                    gy = unchecked((sbyte)(GDVars.gridY[slot] * 3));
                    for (int rep = 0; rep <= reps; rep++)
                        for (j = 0; j <= 3; j++)
                        {
                            BlitClipped(unchecked((short)gx), unchecked((short)gy), j, 5, GDVars.dsMoveIconSheet, GDVars.dsViewImageBufferPtr);
                            FlushViewCall();
                            DelayMs(70);
                            BlitClipped(unchecked((short)gx), unchecked((short)gy), 0, 0, GDVars.dsIconImageBufferPtr, GDVars.dsViewImageBufferPtr);
                        }
                }
                SoundSetAutoStopCall(10);
                FlushViewCall();
                if (reps == 0)
                    TextRend.PauseOnly();
            }
            else
                CharDisp.PrintNameString(creature, s, 10, true);
        }

        public static void PrintHpRestoreMsg(Types.TCharRecord creature)
        {
            string s;
            if (creature.hpCur == creature.hpMax)
                s = "is fully healed";
            else
                s = "is partially healed";
            CharDisp.PrintNameString(creature, s, 10, true);
            if (GDVars.ifcState != C_Ifc.COMBAT)
                CharDisp.DisplayParty(GDVars.curChar);
        }

        public static void DrawNoncombatWindows(bool inset)
        {
            Border.DrawWindow(unchecked((sbyte)(C_Msg.TEXT_LEFT)), unchecked((sbyte)(C_Msg.TEXT_TOP)), unchecked((sbyte)(C_Msg.TEXT_RIGHT)), unchecked((sbyte)(C_Msg.TEXT_BOTTOM)), 0, White, "");
            Border.DrawWindow(unchecked((sbyte)(C_Msg.TEXT_LEFT)), unchecked((sbyte)(C_Msg.MESSAGE_TOP)), unchecked((sbyte)(C_Msg.TEXT_RIGHT)), unchecked((sbyte)(C_Msg.TEXT_BOTTOM)), 0, White, "");
            Border.DrawWindow(1, 1, 15, 15, 0, White, "");
            if (inset)
                Border.DrawWindow(3, 3, 13, 13, 0, White, "");
        }

        public static void DisplayNoncombatInterface()
        {
            switch (GDVars.ifcState)
            {
                case C_Ifc.MAIN_MENU:
                    Border.DrawWindow(unchecked((sbyte)(C_Msg.TEXT_LEFT)), unchecked((sbyte)(C_Msg.TEXT_TOP)), unchecked((sbyte)(C_Msg.TEXT_RIGHT)), unchecked((sbyte)(C_Msg.TEXT_BOTTOM)), 0, White, "");
                    break;
                case C_Ifc.SHOP_TEMPLE:
                case C_Ifc.VAULT:
                    DrawNoncombatWindows(true);
                    LoadPortraitBufsCall(GDVars.dsCurrentPortraitHead, GDVars.dsCurrentPortraitHead2);
                    DisplayPortraitBufsCall(3, 3, true);
                    break;
                case C_Ifc.CAMPING:
                    DrawNoncombatWindows(true);
                    LoadAnimationCall("PIC", 29, 0, ref GDVars.animData);
                    if (GDVars.animData.current > 0)
                        DisplayImageBufCall(3, 3, true, GDVars.animData.frames[GDVars.animData.current].pic);
                    DisplayParty(GDVars.curChar);
                    PrintMiscInfo();
                    break;
                case C_Ifc.INDOORS:
                    DrawNoncombatWindows(true);
                    DisplayMapViewCall();
                    DisplayParty(GDVars.curChar);
                    PrintMiscInfo();
                    break;
                case C_Ifc.OUTDOORS:
                    DrawNoncombatWindows(false);
                    DisplayMapViewCall();
                    DisplayParty(GDVars.curChar);
                    PrintMiscInfo();
                    break;
                case C_Ifc.LOOT:
                    DrawNoncombatWindows(true);
                    LoadAnimationCall("PIC", 1, 0, ref GDVars.animData);
                    DisplayParty(GDVars.curChar);
                    break;
            }
        }

        public static void PrintMiscInfo()
        {
            byte x = unchecked((byte)(GDVars.gMapX));
            byte y = unchecked((byte)(GDVars.gMapY));
            string hrs;
            string mins;
            string info = "";

            if (GDVars.gLocType > C_Area.LOC_3D_MAP)
            {
                x = GDVars.zone0.partyX.lo;
                y = GDVars.zone0.partyY.lo;
                if (GDVars.gLocType == C_Area.LOC_WILD_CENTRAL)
                    x = unchecked((byte)(x + 13));
                if (GDVars.gLocType == C_Area.LOC_WILD_EAST)
                    x = unchecked((byte)(x + 26));
            }

            hrs = StrUtil.ZeroPad2(GDVars.zone0.clock.hour);
            mins = StrUtil.ZeroPad2(unchecked((ushort)(10 * GDVars.zone0.clock.turn + GDVars.zone0.clock.minute)));

            if (GDVars.zone0.noAutomap == 0)
                info = StrUtil.NumStr(x) + "," + StrUtil.NumStr(y) + " ";

            info = info + GDConsts.dirNames[GDVars.gFacing] + " ";
            info = info + hrs + ":" + mins;

            if (GDVars.debugTraceEnabled)
                info = info + "*";

            if (GDVars.ifcState == C_Ifc.CAMPING)
                info = info + " camping";
            else if ((GDVars.zone1[C_Gbvm.SEARCH_FLAGS] & 1) > 0)
                info = info + " search";

            Border.ClearBox(17, 15, 38, 15);
            TextRend.DisplayString(17, 15, LightGreen, info);
        }

        public static void DisplayWildernessInterface()
        {
            GDVars.ifcState = unchecked((byte)(C_Ifc.OUTDOORS));
            DrawWildWindowsCall();
            DisplayMapViewCall();
            DisplayWildMapViewCall();
            PrintMiscInfo();
            DisplayParty(GDVars.curChar);
        }

        public static void DisplayCombatInterface()
        {
            Cbt().SetCombatPalette();
            Border.DrawWindow(1, 1, 21, 21, 0, Black, "");
            Border.DrawWindow(23, 1, 38, 21, 0, Black, "");
            Border.ClearBox(0, 23, 39, 24);
            Cbt().ScrollView(unchecked((sbyte)(GDVars.arenaPtr.ox + 3)), unchecked((sbyte)(GDVars.arenaPtr.oy + 3)), 255, 8);
        }

        public static void SelectChar(string query, bool addExit, ref Types.TCharRecord sel)
        {
            byte ch = 32;
            bool ext = false;
            bool anim;
            Types.TCharRecord p;
            string exitStr = "";

            if (addExit)
                exitStr = " Exit";

            GDVars.horizChoice = 1;

            while (!((ch == C_Keys.ENTER || ch == C_Keys.ESC || ch == 'E' || ch == 'S') || (ch == C_Keys.NONE && addExit)))
            {
                DisplayParty(sel);
                anim = (GDVars.ifcState == C_Ifc.CAMPING) || (GDVars.ifcState == C_Ifc.LOOT);
                ch = MenuCore.MenuHorizontal(query + " ", "Select" + exitStr, LightMagenta, LightGreen, White, true, anim, ref ext);
                p = sel;
                if (ext)
                {
                    if (ch == C_Keys.END_KEY)
                        p = Party.NextCreature(p);
                    else if (ch == C_Keys.HOME)
                        p = Party.PrevCreature(sel);
                }
                else if (((ch == 'E') || (ch == C_Keys.NONE)) && addExit)
                {
                    p = default(Types.TCharRecord);
                }
                sel = p;
            }
        }

        public static void PrintItemLabel(Types.TItemRec item, byte col, byte row)
        {
            CharHlp.ItemLabel(item, false);
            TextRend.DisplayString(col, row, LightGreen, item.name);
        }

        public static void ShowFault(string msg)
        {
            TextRend.DisplayAndPause(Yellow, msg);
        }
    }
}
