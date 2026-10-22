using System;
using static Por.Core.Types;

namespace Por.Core
{
    public static class VidCga
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
                    VideoHardware.CopyTo(GDVars.dsViewImageBufferPtr.data, GDVars.spanFirstOfs[row], GDVars.cgaRowSeg[row + 8], unchecked((ushort)(GDVars.spanScreenOfs[row] + 2)), count);
                }
            }
        }

        private static void ShowPic(short screenX, short screenY, TImgBuf pic)
        {
            int rowBytes = unchecked((ushort)(pic.width * 2));
            int srcOff = 0;
            int firstRow = unchecked((ushort)(screenY * 8));
            int lastRow = unchecked((ushort)(firstRow + pic.height - 1));
            int colOff = unchecked((ushort)(screenX * 2));
            for (int row = firstRow; row <= lastRow; row++)
            {
                VideoHardware.CopyTo(pic.data, srcOff, GDVars.cgaRowSeg[row], (ushort)colOff, rowBytes);
                srcOff = unchecked((ushort)(srcOff + rowBytes));
            }
        }

        private static void DrawGlyph(byte col, byte row, byte color, byte glyph)
        {
            color = GDConsts.attrTable[color];
            int dstOff = row * 4;
            int cnt = 0;
            for (int outerRow = 0; outerRow < 4; outerRow++)
            {
                ushort cgaWord = 0;
                byte mask = 0x80;
                for (int k = 0; k < 8; k++)
                {
                    if ((GDVars.gameFont[glyph, cnt] & mask) > 0)
                        cgaWord |= (ushort)(GDConsts.cgaPixelMask[k] & GDConsts.cgaColorPat[color]);
                    mask >>= 1;
                }
                VideoHardware.Write(0xB800, unchecked((ushort)(dstOff * 80 + col * 2)), (byte)(cgaWord >> 8));
                VideoHardware.Write(0xB800, unchecked((ushort)(dstOff * 80 + col * 2 + 1)), unchecked((byte)cgaWord));
                cnt++;
                cgaWord = 0;
                mask = 0x80;
                for (int k = 0; k < 8; k++)
                {
                    if ((GDVars.gameFont[glyph, cnt] & mask) > 0)
                        cgaWord |= (ushort)(GDConsts.cgaPixelMask[k] & GDConsts.cgaColorPat[color]);
                    mask >>= 1;
                }
                VideoHardware.Write(0xBA00, unchecked((ushort)(dstOff * 80 + col * 2)), (byte)(cgaWord >> 8));
                VideoHardware.Write(0xBA00, unchecked((ushort)(dstOff * 80 + col * 2 + 1)), unchecked((byte)cgaWord));
                cnt++;
                dstOff++;
            }
        }

        private static void ClearRect(byte ulCol, byte ulRow, byte lrCol, byte lrRow)
        {
            int byteCount = (lrCol - ulCol + 1) * 2;
            for (int row = ulRow * 4; row <= lrRow * 4 + 3; row++)
            {
                VideoHardware.Fill(0xB800, unchecked((ushort)(row * 80 + ulCol * 2)), byteCount, 0);
                VideoHardware.Fill(0xBA00, unchecked((ushort)(row * 80 + ulCol * 2)), byteCount, 0);
            }
        }

        private static void ClearScreen()
        {
            VideoHardware.Fill(0xB800, 0, 8000, 0);
            VideoHardware.Fill(0xBA00, 0, 8000, 0);
        }

        private static void WriteChar(byte col, byte row, byte attr, byte count, byte ch)
        {
            DaxIo.BiosWriteChar(GDVars.pageMain, col, row, GDConsts.attrTable[attr & 0x0F], count, ch);
        }

        private static void CellFill(byte color, ref TColorMap colorMap, ref TCellBytes cell)
        {
            byte fillVal = GDConsts.cgaColorFill[Blit.SrcNibble(color, colorMap)];
            for (int i = 0; i < 8; i++)
                cell.Data[i] = fillVal;
        }

        private static void SetPalette(byte registerIndex, byte color)
        {
        }

        private static void ExpandFramesAdapter(TByteArr data, ushort width, bool flag, byte color, TImgBuf img)
        {
            byte[] byteData = new byte[data.Data.Length];
            for (int i = 0; i < data.Data.Length; i++)
                byteData[i] = unchecked((byte)data.Data[i]);
            PicCga.ExpandFrames(byteData, width, flag, color, img);
        }

        public static void Install()
        {
            GDVars.video.name = "CGA";
            GDVars.video.colours = 4;
            GDVars.video.hasPalette = false;
            GDVars.video.keyed = false;
            GDVars.video.cellShift = 1;
            GDVars.video.screenShift = 1;
            GDVars.video.maskShift = 0;
            GDVars.video.CellFill = (byte c, ref TColorMap cm, ref TCellBytes cb) => CellFill(c, ref cm, ref cb);
            GDVars.video.biosMode = 4;
            GDVars.video.ramVideoParas = 0;
            GDVars.video.Start = Start;
            GDVars.video.FlushSpans = FlushSpans;
            GDVars.video.ShowPic = ShowPic;
            GDVars.video.DrawGlyph = DrawGlyph;
            GDVars.video.ClearRect = ClearRect;
            GDVars.video.ClearScreen = ClearScreen;
            GDVars.video.WriteChar = WriteChar;
            GDVars.video.ExpandFrames = ExpandFramesAdapter;
            GDVars.video.MirrorFrame = PicCga.MirrorFrame;
            GDVars.video.RemapColour = PicCga.RemapColour;
            GDVars.video.RecolorFrame = PicCga.RecolorFrame;
            GDVars.video.PutPixelRow = PicCga.PutPixelRow;
            GDVars.video.InitRowTables = PicCga.InitRowTables;
            GDVars.video.SetPalette = SetPalette;
        }
    }
}
