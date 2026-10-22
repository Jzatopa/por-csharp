using System;
using static Por.Core.Types;

namespace Por.Core
{
    public static class VidTdy
    {
        private static void Start()
        {
        }

        private static void FlushSpans()
        {
            for (int row = 0; row < GDVars.spanActive.Length; row++)
            {
                if (GDVars.spanActive[row] != 0)
                {
                    int count = GDVars.spanLastOfs[row] - GDVars.spanFirstOfs[row] + 1;
                    VideoHardware.CopyTo(
                        GDVars.dsViewImageBufferPtr.data,
                        GDVars.spanFirstOfs[row],
                        GDVars.tandyRowSeg[row + 8],
                        unchecked((ushort)(GDVars.spanScreenOfs[row] + 4)),
                        count);
                }
            }
        }

        private static void ShowPic(short screenX, short screenY, TImgBuf pic)
        {
            int rowBytes = unchecked((ushort)(pic.width * 4));
            int srcOff = 0;
            int firstRow = unchecked((ushort)(screenY * 8));
            int lastRow = unchecked((ushort)(firstRow + pic.height - 1));
            int colOff = unchecked((ushort)(screenX * 4));

            for (int row = firstRow; row <= lastRow; row++)
            {
                VideoHardware.CopyTo(
                    pic.data,
                    unchecked((ushort)srcOff),
                    GDVars.tandyRowSeg[row],
                    unchecked((ushort)colOff),
                    rowBytes);
                srcOff = unchecked((ushort)(srcOff + rowBytes));
            }
        }

        private static void DrawGlyph(byte col, byte row, byte color, byte glyph)
        {
            int dstOff = 0;
            int cnt = unchecked((ushort)(row * 2));
            int colBase = unchecked((ushort)(col * 4));

            for (int outerRow = 0; outerRow <= 1; outerRow++)
            {
                for (int bank = 0; bank <= 3; bank++)
                {
                    int mask = 0x80;
                    for (int k = 0; k <= 3; k++)
                    {
                        byte tandyPx;
                        if ((GDVars.gameFont[glyph, dstOff] & mask) > 0)
                            tandyPx = unchecked((byte)(color << 4));
                        else
                            tandyPx = 0;

                        mask = unchecked((byte)(mask >> 1));
                        if ((GDVars.gameFont[glyph, dstOff] & mask) > 0)
                            tandyPx = unchecked((byte)(tandyPx | color));
                        mask = unchecked((byte)(mask >> 1));

                        ushort seg;
                        switch (bank)
                        {
                            case 0: seg = C_Video.GFX_BANK0_SEG; break;
                            case 1: seg = C_Video.GFX_BANK1_SEG; break;
                            case 2: seg = C_Video.GFX_BANK2_SEG; break;
                            case 3: seg = C_Video.GFX_BANK3_SEG; break;
                            default: seg = 0; break;
                        }
                        VideoHardware.Write(seg, unchecked((ushort)(cnt * 160 + k + colBase)), tandyPx);
                    }
                    dstOff++;
                }
                cnt++;
            }
        }

        private static void ClearRect(byte ulCol, byte ulRow, byte lrCol, byte lrRow)
        {
            int firstByteColumn = unchecked((ushort)(ulCol * 4));
            int byteCount = unchecked((ushort)((lrCol - ulCol + 1) * 4));

            for (int row = unchecked((ushort)(ulRow * 2)); row <= unchecked((ushort)(lrRow * 2 + 1)); row++)
            {
                VideoHardware.Fill(C_Video.GFX_BANK0_SEG, unchecked((ushort)(row * 160 + firstByteColumn)), byteCount, 0);
                VideoHardware.Fill(C_Video.GFX_BANK1_SEG, unchecked((ushort)(row * 160 + firstByteColumn)), byteCount, 0);
                VideoHardware.Fill(C_Video.GFX_BANK2_SEG, unchecked((ushort)(row * 160 + firstByteColumn)), byteCount, 0);
                VideoHardware.Fill(C_Video.GFX_BANK3_SEG, unchecked((ushort)(row * 160 + firstByteColumn)), byteCount, 0);
            }
        }

        private static void ClearScreen()
        {
            VideoHardware.Fill(C_Video.GFX_BANK0_SEG, 0, 8000, 0);
            VideoHardware.Fill(C_Video.GFX_BANK1_SEG, 0, 8000, 0);
            VideoHardware.Fill(C_Video.GFX_BANK2_SEG, 0, 8000, 0);
            VideoHardware.Fill(C_Video.GFX_BANK3_SEG, 0, 8000, 0);
        }

        private static void WriteChar(byte col, byte row, byte attr, byte count, byte ch)
        {
            DaxIo.BiosWriteChar(GDVars.pageMain, col, row, attr, count, ch);
        }

        private static void CellFill(byte color, ref TColorMap colorMap, ref TCellBytes cell)
        {
            byte fillVal = GDConsts.tandyColorFill[color];
            for (int i = 0; i < 8; i++)
                cell.Data[i] = fillVal;
        }

        private static void SetPalette(byte registerIndex, byte color)
        {
        }

        private static void ExpandFramesAdapter(TByteArr packed, ushort width, bool flag, byte color, TImgBuf buf)
        {
            sbyte[] raw = packed.Data;
            byte[] data = new byte[raw.Length];
            for (int i = 0; i < raw.Length; i++)
                data[i] = unchecked((byte)raw[i]);
            PicTdy.ExpandFrames(data, width, flag, color, buf);
        }

        public static void Install()
        {
            GDVars.video.name = "Tandy";
            GDVars.video.colours = 16;
            GDVars.video.hasPalette = false;
            GDVars.video.keyed = false;
            GDVars.video.cellShift = 2;
            GDVars.video.screenShift = 2;
            GDVars.video.maskShift = 0;
            GDVars.video.CellFill = CellFill;
            GDVars.video.biosMode = 9;
            GDVars.video.ramVideoParas = 1024;
            GDVars.video.Start = Start;
            GDVars.video.FlushSpans = FlushSpans;
            GDVars.video.ShowPic = ShowPic;
            GDVars.video.DrawGlyph = DrawGlyph;
            GDVars.video.ClearRect = ClearRect;
            GDVars.video.ClearScreen = ClearScreen;
            GDVars.video.WriteChar = WriteChar;
            GDVars.video.ExpandFrames = ExpandFramesAdapter;
            GDVars.video.MirrorFrame = PicTdy.MirrorFrame;
            GDVars.video.RemapColour = PicTdy.RemapColour;
            GDVars.video.RecolorFrame = PicTdy.RecolorFrame;
            GDVars.video.PutPixelRow = PicTdy.PutPixelRow;
            GDVars.video.InitRowTables = PicTdy.InitRowTables;
            GDVars.video.SetPalette = SetPalette;
        }
    }
}
