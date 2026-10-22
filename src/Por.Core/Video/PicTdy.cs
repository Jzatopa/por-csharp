// FILE: src/Por.Core/Video/PicTdy.cs
// Translated from unit PicTdy (Turbo Pascal 7, Tandy video driver cold routines).
namespace Por.Core
{
    public static class PicTdy
    {
        // DAX packed nibbles are copied unchanged; a mask clears transparent nibbles.
        // hasMask is unused: mask generation
        // follows pic.transp being non-null, matching the original source exactly.
        public static void ExpandFrames(byte[] imgData, ushort srcOff, bool hasMask, byte transpColor, Types.TImgBuf pic)
        {
            ushort dstOff = 0;
            int count = pic.count;
            int imgSize = pic.imgSize;

            for (int frame = 1; frame <= count; frame++)
            {
                for (int i = 0; i < imgSize; i++)
                {
                    pic.data[dstOff + i] = imgData[srcOff + i];
                }

                if (pic.transp != null)
                {
                    for (int col = 0; col < imgSize; col++)
                    {
                        int idx = dstOff + col;
                        if ((pic.data[idx] >> 4) == transpColor)
                            pic.transp[idx] = 0xF0;
                        if ((pic.data[idx] & 0x0F) == transpColor)
                            pic.transp[idx] = unchecked((byte)(pic.transp[idx] + 0x0F));
                        pic.data[idx] = unchecked((byte)(pic.data[idx] & ~pic.transp[idx]));
                    }
                }

                dstOff = unchecked((ushort)(dstOff + imgSize));
                srcOff = unchecked((ushort)(srcOff + imgSize));
            }
        }

        // Reverse each row's byte order and swap each byte's nibbles, including the mask.
        // Only the first frame is mirrored, matching the original source's single height-bounded loop.
        public static void MirrorFrame(Types.TImgBuf src, Types.TImgBuf dst)
        {
            int width = src.width;
            int height = src.height;
            ushort rowBytes = unchecked((ushort)(width * 4));
            ushort srcOff = unchecked((ushort)(rowBytes - 1));
            ushort dstOff = 0;

            for (int row = 1; row <= height; row++)
            {
                ushort rowStart = dstOff;
                for (int col = 1; col <= rowBytes; col++)
                {
                    int srcIdx = unchecked((ushort)(srcOff - dstOff + rowStart));
                    dst.data[dstOff] = unchecked((byte)((src.data[srcIdx] << 4) | (src.data[srcIdx] >> 4)));
                    if (src.transp != null)
                        dst.transp[dstOff] = unchecked((byte)((src.transp[srcIdx] << 4) | (src.transp[srcIdx] >> 4)));
                    dstOff = unchecked((ushort)(dstOff + 1));
                }
                srcOff = unchecked((ushort)(srcOff + rowBytes));
            }
        }

        // Replace fromC with toC in nFrames frames of pic from srcOff, matching the original pixels and writing
        // scratch, which holds a copy of the frames; with isDithered, a random part of the matches.
        public static void RemapColour(Types.TImgBuf pic, Types.TImgBuf scratch, ushort srcOff, byte nFrames, byte fromC, byte toC, bool isDithered)
        {
            ushort dstOff = 0;
            int width = scratch.width;
            int height = scratch.height;
            ushort rowBytes = unchecked((ushort)(width * 4));
            byte fromHigh = unchecked((byte)(fromC << 4));
            byte toHigh = unchecked((byte)(toC << 4));

            for (int frameNo = 1; frameNo <= nFrames; frameNo++)
            {
                for (int row = 1; row <= height; row++)
                {
                    for (int col = 1; col <= rowBytes; col++)
                    {
                        if ((pic.data[srcOff] & 0xF0) == fromHigh)
                        {
                            if (!isDithered || (isDithered && Dice.RandomBelow(4) == 0))
                                scratch.data[dstOff] = unchecked((byte)((scratch.data[dstOff] & 0x0F) | toHigh));
                        }
                        if ((pic.data[srcOff] & 0x0F) == fromC)
                        {
                            if (!isDithered || (isDithered && Dice.RandomBelow(4) == 0))
                                scratch.data[dstOff] = unchecked((byte)((scratch.data[dstOff] & 0xF0) | toC));
                        }
                        srcOff = unchecked((ushort)(srcOff + 1));
                        dstOff = unchecked((ushort)(dstOff + 1));
                    }
                }
            }
        }

        // Recolor both packed nibbles in each byte of the first frame.
        public static void RecolorFrame(ref Types.TColorTab toT, Types.TImgBuf pic)
        {
            int imgSize = pic.imgSize;
            for (int n = 0; n < imgSize; n++)
            {
                pic.data[n] = unchecked((byte)((toT[pic.data[n] >> 4] << 4) | toT[pic.data[n] & 0x0F]));
            }
        }

        public static void PutPixelRow(Types.TImgBuf pic, ushort at, byte y, ref Types.TPixelRow px)
        {
            for (int i = 0; i < 4; i++)
            {
                pic.data[at + y * 4 + i] = unchecked((byte)(((px[i * 2] & 0x0F) << 4) | (px[i * 2 + 1] & 0x0F)));
            }
        }

        public static void InitRowTables()
        {
            for (int i = 0; i < 200; i++)
            {
                switch (i % 4)
                {
                    case 0:
                        GDVars.tandyRowSeg[i] = unchecked((ushort)((i / 4) * 10 + C_Video.GFX_BANK0_SEG));
                        break;
                    case 1:
                        GDVars.tandyRowSeg[i] = unchecked((ushort)((i / 4) * 10 + C_Video.GFX_BANK1_SEG));
                        break;
                    case 2:
                        GDVars.tandyRowSeg[i] = unchecked((ushort)((i / 4) * 10 + C_Video.GFX_BANK2_SEG));
                        break;
                    case 3:
                        GDVars.tandyRowSeg[i] = unchecked((ushort)((i / 4) * 10 + C_Video.GFX_BANK3_SEG));
                        break;
                }
            }
        }
    }
}
