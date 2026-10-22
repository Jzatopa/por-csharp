// FILE: src/Por.Core/Video/PicEga.cs
// Translated from unit PicEga (Turbo Pascal 7, EGA video driver cold routines).
namespace Por.Core
{
    public static class PicEga
    {
        // Each DAX cell row (4 bytes, two pixels a byte, high nibble first) becomes 4 plane bytes; with a mask, a pixel of
        // transpColor clears its plane bits. hasMask is unused: mask generation follows pic.transp being non-null, matching
        // the original source exactly.
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

                        if (pic.transp != null)
                        {
                            pic.transp[maskOff] = Blit.ComputeMask(transpColor, chunk);
                            maskOff = unchecked((ushort)(maskOff + 1));
                        }

                        byte bit = 0x80;
                        for (int k = 0; k < 8; k++)
                        {
                            byte color;
                            if ((k & 1) != 0)
                                color = unchecked((byte)(chunk[k >> 1] & 0x0F));
                            else
                                color = unchecked((byte)(chunk[k >> 1] >> 4));

                            for (int plane = 0; plane < 4; plane++)
                            {
                                if ((GDConsts.egaPlaneBit[plane] & color) > 0)
                                    pic.data[plane + dstOff] = unchecked((byte)(pic.data[plane + dstOff] | bit));
                            }
                            bit = unchecked((byte)(bit >> 1));
                        }

                        if (pic.transp != null)
                        {
                            for (int plane = 0; plane < 4; plane++)
                            {
                                pic.data[plane + dstOff] = unchecked((byte)(pic.data[plane + dstOff] & ~pic.transp[maskOff - 1]));
                            }
                        }

                        dstOff = unchecked((ushort)(dstOff + 4));
                        srcOff = unchecked((ushort)(srcOff + 4));
                    }
                }
            }
        }

        // Each row's plane bytes in reverse order; each mask byte (a cell row) to the mirrored cell, its bits reversed.
        // Only the first frame is mirrored, matching the original source's single height-bounded loop.
        public static void MirrorFrame(Types.TImgBuf src, Types.TImgBuf dst)
        {
            int width = src.width;
            int height = src.height;
            ushort rowBytes = unchecked((ushort)(width * 4));
            ushort srcOff = unchecked((ushort)(rowBytes - 4));
            ushort dstOff = 0;

            for (int row = 1; row <= height; row++)
            {
                ushort rowStart = dstOff;
                for (int col = 1; col <= width; col++)
                {
                    for (int plane = 0; plane < 4; plane++)
                    {
                        dst.data[plane + dstOff] = Blit.MirrorBits(src.data[srcOff - dstOff + rowStart + plane]);
                    }
                    dstOff = unchecked((ushort)(dstOff + 4));
                }
                srcOff = unchecked((ushort)(srcOff + rowBytes));
            }

            if (src.transp != null)
            {
                srcOff = 0;
                for (int row = 1; row <= height; row++)
                {
                    for (int col = 0; col < width; col++)
                    {
                        dst.transp[srcOff + width - col - 1] = Blit.MirrorBits(src.transp[srcOff + col]);
                    }
                    srcOff = unchecked((ushort)(srcOff + width));
                }
            }
        }

        // Replace fromC with toC in nFrames frames of pic from srcOff, matching the original pixels and writing
        // scratch, which holds a copy of the frames; with isDithered, a random part of the matches.
        public static void RemapColour(Types.TImgBuf pic, Types.TImgBuf scratch, ushort srcOff, byte nFrames, byte fromC, byte toC, bool isDithered)
        {
            ushort dstOff = 0;
            byte[] planeMask = new byte[4];

            for (int plane = 0; plane < 4; plane++)
            {
                if ((fromC & GDConsts.egaPlaneBit[plane]) > 0)
                    planeMask[plane] = 0;
                else
                    planeMask[plane] = 0xFF;
            }

            int height = scratch.height;
            int width = scratch.width;

            for (int frameNo = 1; frameNo <= nFrames; frameNo++)
            {
                for (int row = 1; row <= height; row++)
                {
                    for (int col = 1; col <= width; col++)
                    {
                        byte match;
                        if (isDithered)
                            match = unchecked((byte)(Dice.RandomBelow(256) & Dice.RandomBelow(256)));
                        else
                            match = 0xFF;

                        for (int plane = 0; plane < 4; plane++)
                        {
                            match = unchecked((byte)((pic.data[plane + srcOff] ^ planeMask[plane]) & match));
                        }

                        for (int plane = 0; plane < 4; plane++)
                        {
                            if ((toC & GDConsts.egaPlaneBit[plane]) > 0)
                                scratch.data[plane + dstOff] = unchecked((byte)(scratch.data[plane + dstOff] | match));
                            else
                                scratch.data[plane + dstOff] = unchecked((byte)(scratch.data[plane + dstOff] & ~match));
                        }

                        srcOff = unchecked((ushort)(srcOff + 4));
                        dstOff = unchecked((ushort)(dstOff + 4));
                    }
                }
            }
        }

        // Recolor each pixel of the first frame through toT, retaining only four plane bits.
        public static void RecolorFrame(ref Types.TColorTab toT, Types.TImgBuf pic)
        {
            int imgSize = pic.imgSize;
            int n = 0;
            while (n + 3 < imgSize)
            {
                byte[] q = new byte[4];
                for (int k = 0; k < 4; k++)
                    q[k] = 0;

                for (int bit = 0; bit < 8; bit++)
                {
                    byte c = unchecked((byte)(
                        ((pic.data[n] >> bit) & 1) |
                        (((pic.data[n + 1] >> bit) & 1) << 1) |
                        (((pic.data[n + 2] >> bit) & 1) << 2) |
                        (((pic.data[n + 3] >> bit) & 1) << 3)
                    ));
                    c = toT[c];
                    for (int k = 0; k < 4; k++)
                    {
                        if ((c & (1 << k)) != 0)
                            q[k] = unchecked((byte)(q[k] | (1 << bit)));
                    }
                }

                for (int k = 0; k < 4; k++)
                    pic.data[n + k] = q[k];

                n += 4;
            }
        }

        public static void PutPixelRow(Types.TImgBuf pic, ushort at, byte y, ref Types.TPixelRow px)
        {
            for (int plane = 0; plane < 4; plane++)
            {
                byte b = 0;
                for (int k = 0; k < 8; k++)
                {
                    if ((px[k] & GDConsts.egaPlaneBit[plane]) != 0)
                        b = unchecked((byte)(b | (0x80 >> k)));
                }
                pic.data[at + y * 4 + plane] = b;
            }
        }

        public static void InitRowTables()
        {
            for (int i = 0; i < 200; i++)
            {
                GDVars.fbRowOfs[i] = unchecked((ushort)(i * 40));
            }
        }
    }
}
