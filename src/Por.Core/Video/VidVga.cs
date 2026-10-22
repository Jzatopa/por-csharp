using System;
using static Por.Core.Types;

namespace Por.Core
{
    public static class VidVga
    {
        private static readonly byte[][] EGA_RGB = new byte[16][]
        {
            new byte[] { 0, 0, 0 },
            new byte[] { 0, 0, 42 },
            new byte[] { 0, 42, 0 },
            new byte[] { 0, 42, 42 },
            new byte[] { 42, 0, 0 },
            new byte[] { 42, 0, 42 },
            new byte[] { 42, 21, 0 },
            new byte[] { 42, 42, 42 },
            new byte[] { 21, 21, 21 },
            new byte[] { 21, 21, 63 },
            new byte[] { 21, 63, 21 },
            new byte[] { 21, 63, 63 },
            new byte[] { 63, 21, 21 },
            new byte[] { 63, 21, 63 },
            new byte[] { 63, 63, 21 },
            new byte[] { 63, 63, 63 }
        };

        private const ushort DAC_WRITE_INDEX = 0x3C8;
        private const ushort DAC_DATA = 0x3C9;

        private static void Start()
        {
        }

        private static void FlushSpans()
        {

            int spanCount = GDVars.spanActive.Length;
            for (int row = 0; row < spanCount; row++)
            {
                if (GDVars.spanActive[row] != 0)
                {
                    int count = GDVars.spanLastOfs[row] - GDVars.spanFirstOfs[row] + 1;
                    int dstOff = unchecked((ushort)(GDVars.fbRowOfs[row + 8] + GDVars.spanScreenOfs[row] + 8));
                    VideoHardware.CopyTo(GDVars.dsViewImageBufferPtr.data, GDVars.spanFirstOfs[row], C_Video.VGA_VIDEO_SEG, (ushort)dstOff, count);
                }
            }
        }

        private static void ShowPic(short screenX, short screenY, TImgBuf pic)
        {

            int rowBytes = unchecked((ushort)(pic.width * 8));
            int srcOff = 0;
            int startRow = unchecked((ushort)(screenY * 8));
            int endRow = unchecked((ushort)(startRow + pic.height - 1));

            for (int row = startRow; row <= endRow; row++)
            {
                int dstOff = unchecked((ushort)(GDVars.fbRowOfs[row] + screenX * 8));
                VideoHardware.CopyTo(pic.data, srcOff, C_Video.VGA_VIDEO_SEG, (ushort)dstOff, rowBytes);
                srcOff = unchecked((ushort)(srcOff + rowBytes));
            }
        }

        private static void DrawGlyph(byte col, byte row, byte color, byte glyph)
        {

            for (int r = 0; r < 8; r++)
            {
                int dstOff = unchecked((ushort)(GDVars.fbRowOfs[row * 8 + r] + col * 8));
                byte bits = GDVars.gameFont[glyph,r];
                for (int k = 0; k < 8; k++)
                {
                    if ((bits & (byte)(0x80 >> k)) != 0)
                        VideoHardware.Write(C_Video.VGA_VIDEO_SEG, unchecked((ushort)(dstOff + k)), color);
                    else
                        VideoHardware.Write(C_Video.VGA_VIDEO_SEG, unchecked((ushort)(dstOff + k)), 0);
                }
            }
        }

        private static void ClearRect(byte ulCol, byte ulRow, byte lrCol, byte lrRow)
        {

            int startRow = ulRow * 8;
            int endRow = lrRow * 8 + 7;
            int width = (lrCol - ulCol + 1) * 8;

            for (int y = startRow; y <= endRow; y++)
            {
                int dstOff = unchecked((ushort)(GDVars.fbRowOfs[y] + ulCol * 8));
                VideoHardware.Fill(C_Video.VGA_VIDEO_SEG, (ushort)dstOff, width, 0);
            }
        }

        private static void ClearScreen()
        {
            VideoHardware.Fill(C_Video.VGA_VIDEO_SEG, 0, C_Video.VGA_ROW_BYTES * 200, 0);
        }

        private static void WriteChar(byte col, byte row, byte attr, byte count, byte ch)
        {
            DaxIo.BiosWriteChar(GDVars.pageMain, col, row, attr, count, ch);
        }

        private static void SetPalette(byte registerIndex, byte color)
        {

            int idx = color & 0x0F;
            VideoHardware.Port(DAC_WRITE_INDEX, registerIndex);
            VideoHardware.Port(DAC_DATA, EGA_RGB[idx][0]);
            VideoHardware.Port(DAC_DATA, EGA_RGB[idx][1]);
            VideoHardware.Port(DAC_DATA, EGA_RGB[idx][2]);
        }

        private static void CellFill(byte color, ref TColorMap colorMap, ref TCellBytes cell)
        {
            for (int i = 0; i < 8; i++)
                cell.Data[i] = color;
        }

        private static void ExpandFramesAdapter(TByteArr raw, ushort width, bool flag, byte color, TImgBuf buf)
        {
            sbyte[] data = raw.Data;
            byte[] byteData = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
                byteData[i] = unchecked((byte)data[i]);
            PicVga.ExpandFrames(byteData, width, flag, color, buf);
        }

        public static void Install()
        {
            var v = GDVars.video;
            v.name = "VGA";
            v.colours = 16;
            v.hasPalette = true;
            v.keyed = true;
            v.cellShift = 3;
            v.screenShift = 3;
            v.maskShift = 3;
            v.CellFill = CellFill;
            v.biosMode = 0x13;
            v.ramVideoParas = 0;
            v.Start = Start;
            v.FlushSpans = FlushSpans;
            v.ShowPic = ShowPic;
            v.DrawGlyph = DrawGlyph;
            v.ClearRect = ClearRect;
            v.ClearScreen = ClearScreen;
            v.WriteChar = WriteChar;
            v.ExpandFrames = ExpandFramesAdapter;
            v.MirrorFrame = PicVga.MirrorFrame;
            v.RemapColour = PicVga.RemapColour;
            v.RecolorFrame = PicVga.RecolorFrame;
            v.PutPixelRow = PicVga.PutPixelRow;
            v.InitRowTables = PicVga.InitRowTables;
            v.SetPalette = SetPalette;
        }
    }
}
