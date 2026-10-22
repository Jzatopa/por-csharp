using System;
using static Por.Core.Types;

namespace Por.Core
{
    public static class VidEga
    {
        private static ushort egaCurPage;
        private static ushort egaOtherPage;

        private const int GC_WRITE_MODE_1 = 1;
        private const ushort EGA_STAGE_SEG = 0xA200;

        private static void ResetEgaGC()
        {
            VideoHardware.Port(C_Ega.GC_ADDR_PORT, 0);
            VideoHardware.Port(C_Ega.GC_DATA_PORT, 0);
            VideoHardware.Port(C_Ega.GC_ADDR_PORT, 1);
            VideoHardware.Port(C_Ega.GC_DATA_PORT, 0);
            VideoHardware.Port(C_Ega.GC_ADDR_PORT, 2);
            VideoHardware.Port(C_Ega.GC_DATA_PORT, 0);
            VideoHardware.Port(C_Ega.GC_ADDR_PORT, 3);
            VideoHardware.Port(C_Ega.GC_DATA_PORT, 0);
            VideoHardware.Port(C_Ega.GC_ADDR_PORT, 4);
            VideoHardware.Port(C_Ega.GC_DATA_PORT, 0);
            VideoHardware.Port(C_Ega.GC_ADDR_PORT, 5);
            VideoHardware.Port(C_Ega.GC_DATA_PORT, 0);
            VideoHardware.Port(C_Ega.GC_ADDR_PORT, 7);
            VideoHardware.Port(C_Ega.GC_DATA_PORT, 0x0F);
            VideoHardware.Port(C_Ega.GC_ADDR_PORT, 8);
            VideoHardware.Port(C_Ega.GC_DATA_PORT, 0xFF);
            VideoHardware.Port(C_Ega.SEQ_ADDR_PORT, C_Ega.SEQ_MAP_MASK_REG);
            VideoHardware.Port(C_Ega.SEQ_DATA_PORT, C_Ega.MAP_MASK_ALL_PLANES);
        }

        private static void SetupEgaCurrentPage(byte page)
        {
            GDVars.dsInterruptRegisters.ax = (ushort)(0x500 | page);
            VideoHardware.Interrupt(0x10, GDVars.dsInterruptRegisters);
            egaCurPage = unchecked((ushort)(C_Video.EGA_VIDEO_SEG + (page << 9)));
        }

        private static void SetupEgaOtherPage(byte page)
        {
            egaOtherPage = unchecked((ushort)(C_Video.EGA_VIDEO_SEG + (page << 9)));
        }

        private static void FillRectEga(byte ulCol, byte ulRow, byte lrCol, byte lrRow, byte page, byte color)
        {
            byte savedPage = GDVars.pageNum;
            ushort dstOff = unchecked((ushort)(ulRow * 40 * 8 + ulCol));
            ushort rowCount = unchecked((ushort)((lrRow - ulRow + 1) * 8 - 1));

            SetupEgaOtherPage(page);
            ResetEgaGC();
            VideoHardware.Port(C_Ega.GC_ADDR_PORT, C_Ega.GC_MODE_REG);
            VideoHardware.Port(C_Ega.GC_DATA_PORT, 2);

            for (int i = 0; i <= rowCount; i++)
            {
                VideoHardware.Fill(egaOtherPage, dstOff, lrCol - ulCol + 1, color);
                dstOff = unchecked((ushort)(dstOff + 40));
            }

            VideoHardware.Port(C_Ega.GC_ADDR_PORT, C_Ega.GC_MODE_REG);
            VideoHardware.Port(C_Ega.GC_DATA_PORT, C_Ega.GC_WRITE_MODE_0);
            GDVars.pageNum = savedPage;
            SetupEgaOtherPage(GDVars.pageNum);
        }

        private static void Start()
        {
            GDVars.pageMain = 0;
            GDVars.pageNum = 0;
            SetupEgaCurrentPage(GDVars.pageMain);
            SetupEgaOtherPage(GDVars.pageNum);
        }

        private static void FlushSpans()
        {
            VideoHardware.Port(C_Ega.SEQ_ADDR_PORT, C_Ega.SEQ_MAP_MASK_REG);

            for (int row = 0; row < GDVars.spanActive.Length; row++)
            {
                if (GDVars.spanActive[row] != 0)
                {
                    ushort dstBase = unchecked((ushort)(GDVars.fbRowOfs[row + 8] + GDVars.spanScreenOfs[row] + 1));

                    for (int plane = 0; plane < 4; plane++)
                    {
                        VideoHardware.Port(C_Ega.SEQ_DATA_PORT, GDConsts.egaPlaneBit[plane]);
                        int srcOff = GDVars.spanFirstOfs[row] + plane;
                        int stageOffset = dstBase;
                        int lastOfs = GDVars.spanLastOfs[row];
                        if (lastOfs >= 65532) throw new InvalidOperationException("Word wrapping EGA span cannot terminate.");

                        while (true)
                        {
                            VideoHardware.Write(EGA_STAGE_SEG, unchecked((ushort)stageOffset), GDVars.dsViewImageBufferPtr.data[srcOff]);
                            srcOff += 4;
                            stageOffset++;
                            if (srcOff > lastOfs) break;
                            if (srcOff < GDVars.spanFirstOfs[row] + plane) throw new InvalidOperationException("Word wrap detected in FlushSpans");
                        }
                    }
                }
            }

            VideoHardware.Port(C_Ega.SEQ_DATA_PORT, C_Ega.MAP_MASK_ALL_PLANES);
            int dstOff = GDVars.fbRowOfs[8] + 1;
            VideoHardware.Port(C_Ega.GC_ADDR_PORT, C_Ega.GC_MODE_REG);
            VideoHardware.Port(C_Ega.GC_DATA_PORT, GC_WRITE_MODE_1);

            for (int row = 0; row < GDVars.spanActive.Length; row++)
            {
                if (GDVars.spanActive[row] != 0)
                {
                    int count = (GDVars.spanLastOfs[row] - GDVars.spanFirstOfs[row]) / 4 + 1;
                    VideoHardware.Move(EGA_STAGE_SEG, unchecked((ushort)(dstOff + GDVars.spanScreenOfs[row])),
                                       C_Video.EGA_VIDEO_SEG, unchecked((ushort)(dstOff + GDVars.spanScreenOfs[row])), count);
                }
                dstOff += 40;
            }

            VideoHardware.Port(C_Ega.GC_DATA_PORT, C_Ega.GC_WRITE_MODE_0);
        }

        private static void ShowPic(short screenX, short screenY, TImgBuf pic)
        {
            int firstRow = unchecked((ushort)(screenY * 8));
            int lastRow = unchecked((ushort)(firstRow + pic.height - 1));
            int colOff = unchecked((ushort)screenX);
            int colEnd = unchecked((ushort)(colOff + pic.width - 1));

            VideoHardware.Port(C_Ega.SEQ_ADDR_PORT, C_Ega.SEQ_MAP_MASK_REG);

            int srcOff = 0;
            for (int row = firstRow; row <= lastRow; row++)
            {
                for (int col = colOff; col <= colEnd; col++)
                {
                    for (int plane = 0; plane < 4; plane++)
                    {
                        VideoHardware.Port(C_Ega.SEQ_DATA_PORT, GDConsts.egaPlaneBit[plane]);
                        VideoHardware.Write(C_Video.EGA_VIDEO_SEG, unchecked((ushort)(GDVars.fbRowOfs[row] + col)), pic.data[srcOff]);
                        srcOff = unchecked((ushort)(srcOff + 1));
                    }
                }
            }

            VideoHardware.Port(C_Ega.SEQ_DATA_PORT, C_Ega.MAP_MASK_ALL_PLANES);
        }

        private static void DrawGlyph(byte col, byte row, byte color, byte glyph)
        {
            int dstOff = (row * 8) * 40 + col;
            VideoHardware.Port(C_Ega.SEQ_ADDR_PORT, C_Ega.SEQ_MAP_MASK_REG);

            for (int outerRow = 0; outerRow < 8; outerRow++)
            {
                for (int k = 0; k < 4; k++)
                {
                    byte mask = GDConsts.egaPlaneMask[3 - k];
                    VideoHardware.Port(C_Ega.SEQ_DATA_PORT, mask);
                    byte val = ((mask & color) > 0) ? GDVars.gameFont[glyph, outerRow] : (byte)0;
                    VideoHardware.Write(egaCurPage, (ushort)dstOff, val);
                    VideoHardware.Write(egaOtherPage, (ushort)dstOff, val);
                }
                dstOff += 40;
            }

            VideoHardware.Port(C_Ega.SEQ_DATA_PORT, C_Ega.MAP_MASK_ALL_PLANES);
        }

        private static void ClearRect(byte ulCol, byte ulRow, byte lrCol, byte lrRow)
        {
            FillRectEga(ulCol, ulRow, lrCol, lrRow, 0, 0);
            FillRectEga(ulCol, ulRow, lrCol, lrRow, 1, 0);
        }

        private static void ClearScreen()
        {
            FillRectEga(0, 0, 39, 24, 0, 0);
            FillRectEga(0, 0, 39, 24, 1, 0);
        }

        private static void WriteChar(byte col, byte row, byte attr, byte count, byte ch)
        {
            DaxIo.BiosWriteChar(GDVars.pageMain, col, row, attr, count, ch);
            DaxIo.BiosWriteChar(GDVars.pageNum, col, row, attr, count, ch);
        }

        private static void CellFill(byte color, ref TColorMap colorMap, ref TCellBytes cell)
        {
            for (int plane = 0; plane < 4; plane++)
            {
                if ((GDConsts.egaPlaneBit[plane] & color) > 0)
                    cell.Data[plane] = 0xFF;
                else
                    cell.Data[plane] = 0;
            }
        }

        private static void SetPalette(byte registerIndex, byte color)
        {
            GDVars.dsInterruptRegisters.ax = 0x1000;
            byte bh = (color < 8) ? color : unchecked((byte)(color + 8));
            GDVars.dsInterruptRegisters.bx = (ushort)((bh << 8) | registerIndex);
            VideoHardware.Interrupt(0x10, GDVars.dsInterruptRegisters);
        }

        private static void ExpandFramesAdapter(TByteArr packed, ushort width, bool flag, byte color, TImgBuf buf)
        {
            sbyte[] raw = packed.Data;
            byte[] data = new byte[raw.Length];
            for (int i = 0; i < raw.Length; i++)
                data[i] = unchecked((byte)raw[i]);
            PicEga.ExpandFrames(data, width, flag, color, buf);
        }

        public static void Install()
        {
            GDVars.video.name = "EGA";
            GDVars.video.colours = 16;
            GDVars.video.hasPalette = true;
            GDVars.video.keyed = false;
            GDVars.video.cellShift = 2;
            GDVars.video.screenShift = 0;
            GDVars.video.maskShift = 2;
            GDVars.video.CellFill = CellFill;
            GDVars.video.biosMode = 13;
            GDVars.video.ramVideoParas = 0;
            GDVars.video.Start = Start;
            GDVars.video.FlushSpans = FlushSpans;
            GDVars.video.ShowPic = ShowPic;
            GDVars.video.DrawGlyph = DrawGlyph;
            GDVars.video.ClearRect = ClearRect;
            GDVars.video.ClearScreen = ClearScreen;
            GDVars.video.WriteChar = WriteChar;
            GDVars.video.ExpandFrames = ExpandFramesAdapter;
            GDVars.video.MirrorFrame = PicEga.MirrorFrame;
            GDVars.video.RemapColour = PicEga.RemapColour;
            GDVars.video.RecolorFrame = PicEga.RecolorFrame;
            GDVars.video.PutPixelRow = PicEga.PutPixelRow;
            GDVars.video.InitRowTables = PicEga.InitRowTables;
            GDVars.video.SetPalette = SetPalette;
        }
    }
}
