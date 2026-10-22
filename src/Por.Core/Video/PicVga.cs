// FILE: src/Por.Core/Video/PicVga.cs
// Translated from unit PicVga (Turbo Pascal 7, VGA video driver cold routines).
namespace Por.Core
{
    public static class PicVga
    {
        // Each DAX cell row (4 bytes, two pixels a byte, high nibble first) becomes 8 bytes; with a mask, a pixel of
        // transpColor becomes PIC_KEY and sets its mask bit (bit 7 the leftmost). hasMask is unused: mask generation
        // follows pic.transp being non-null, matching the original source exactly.
        public static void ExpandFrames(byte[] imgData, ushort srcOff, bool hasMask, byte transpColor, Types.TImgBuf pic)
        {
            byte[] chunk = new byte[4];
            ushort dstOff = 0;
            ushort maskOff = 0;
            int count = pic.count;
            int height = pic.height;
            int width = pic.width;

            for (int frame = 1; frame <= count; frame++)
            {
                for (int row = 0; row < height; row++)
                {
                    for (int col = 0; col < width; col++)
                    {
                        chunk[0] = imgData[srcOff];
                        chunk[1] = imgData[srcOff + 1];
                        chunk[2] = imgData[srcOff + 2];
                        chunk[3] = imgData[srcOff + 3];

                        byte mask = 0;
                        if (pic.transp != null)
                        {
                            mask = Blit.ComputeMask(transpColor, chunk);
                            pic.transp[maskOff] = mask;
                            maskOff = unchecked((ushort)(maskOff + 1));
                        }

                        for (int k = 0; k < 8; k++)
                        {
                            byte color;
                            if ((k & 1) != 0)
                                color = unchecked((byte)(chunk[k >> 1] & 0x0F));
                            else
                                color = unchecked((byte)(chunk[k >> 1] >> 4));

                            if ((mask & (0x80 >> k)) != 0)
                                color = Types.PIC_KEY;

                            pic.data[dstOff + k] = color;
                        }

                        dstOff = unchecked((ushort)(dstOff + 8));
                        srcOff = unchecked((ushort)(srcOff + 4));
                    }
                }
            }
        }

        // Each row's bytes in reverse order; each mask byte (a cell row) to the mirrored cell, its bits reversed.
        // Only the first frame is mirrored, matching the original source's single height-bounded loop.
        public static void MirrorFrame(Types.TImgBuf src, Types.TImgBuf dst)
        {
            int width = src.width;
            int height = src.height;
            ushort rowBytes = unchecked((ushort)(width * 8));
            ushort rowOff = 0;

            for (int row = 1; row <= height; row++)
            {
                for (int col = 0; col < rowBytes; col++)
                {
                    dst.data[rowOff + col] = src.data[rowOff + rowBytes - 1 - col];
                }
                rowOff = unchecked((ushort)(rowOff + rowBytes));
            }

            if (src.transp != null)
            {
                ushort maskOff = 0;
                for (int row = 1; row <= height; row++)
                {
                    for (int col = 0; col < width; col++)
                    {
                        dst.transp[maskOff + width - col - 1] = Blit.MirrorBits(src.transp[maskOff + col]);
                    }
                    maskOff = unchecked((ushort)(maskOff + width));
                }
            }
        }

        // Replace fromC with toC in nFrames frames of pic from srcOff, matching the original pixels and writing
        // scratch, which holds a copy of the frames; with isDithered, a quarter of the matches, at random.
        public static void RemapColour(Types.TImgBuf pic, Types.TImgBuf scratch, ushort srcOff, byte nFrames, byte fromC, byte toC, bool isDithered)
        {
            ushort dstOff = 0;
            int product = nFrames * (int)scratch.imgSize;
            ushort total = unchecked((ushort)product);

            for (int n = 1; n <= total; n++)
            {
                if (pic.data[srcOff] == fromC)
                {
                    bool doSet;
                    if (!isDithered)
                        doSet = true;
                    else
                        doSet = Dice.RandomBelow(4) == 0;

                    if (doSet)
                        scratch.data[dstOff] = toC;
                }

                srcOff = unchecked((ushort)(srcOff + 1));
                dstOff = unchecked((ushort)(dstOff + 1));
            }
        }

        // Every pixel but PIC_KEY to the colour toT gives it.
        public static void RecolorFrame(ref Types.TColorTab toT, Types.TImgBuf pic)
        {
            int imgSize = pic.imgSize;
            for (int n = 0; n < imgSize; n++)
            {
                if (pic.data[n] < 16)
                    pic.data[n] = toT[pic.data[n]];
            }
        }

        public static void PutPixelRow(Types.TImgBuf pic, ushort at, byte y, ref Types.TPixelRow px)
        {
            for (int k = 0; k < 8; k++)
            {
                pic.data[at + y * 8 + k] = unchecked((byte)(px[k] & 0x0F));
            }
        }

        public static void InitRowTables()
        {
            for (int i = 0; i < 200; i++)
            {
                GDVars.fbRowOfs[i] = unchecked((ushort)(i * C_Video.VGA_ROW_BYTES));
            }
        }
    }
}

