// FILE: src/Por.Core/Video/PicCga.cs
// Translated from unit PicCga (Turbo Pascal 7, CGA video driver cold routines).
namespace Por.Core
{
    public static class PicCga
    {
        // Each DAX cell row (4 bytes, two pixels a byte, high nibble first) becomes 2 bytes; with a mask, a pixel of
        // transpColor sets a two-bit mask and clears those bits. hasMask is unused: mask generation
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

                        if (pic.transp != null)
                        {
                            byte mask = Blit.ComputeMask(transpColor, chunk);
                            byte bit = 0x80;
                            for (int j = 1; j <= 2; j++)
                            {
                                for (int k = 4; k <= 7; k++)
                                {
                                    if ((bit & mask) != 0)
                                        pic.transp[maskOff] = unchecked((byte)(pic.transp[maskOff] | GDConsts.cgaPixelMask[k]));
                                    bit = unchecked((byte)(bit >> 1));
                                }
                                maskOff = unchecked((ushort)(maskOff + 1));
                            }
                        }

                        for (int k = 0; k <= 3; k++)
                        {
                            if ((k & 1) != 0)
                                pic.data[dstOff] = unchecked((byte)(pic.data[dstOff] | GDConsts.cgaPixPos[Blit.SrcNibble(unchecked((byte)(chunk[k >> 1] & 0x0F)), pic.map),k]));
                            else
                                pic.data[dstOff] = unchecked((byte)(pic.data[dstOff] | GDConsts.cgaPixPos[Blit.SrcNibble(unchecked((byte)((chunk[k >> 1] & 0xF0) >> 4)), pic.map),k]));
                        }

                        if (pic.transp != null)
                            pic.data[dstOff] = unchecked((byte)(pic.data[dstOff] & ~pic.transp[unchecked((ushort)(maskOff - 2))]));

                        dstOff = unchecked((ushort)(dstOff + 1));

                        for (int k = 4; k <= 7; k++)
                        {
                            if ((k & 1) != 0)
                                pic.data[dstOff] = unchecked((byte)(pic.data[dstOff] | GDConsts.cgaPixPos[Blit.SrcNibble(unchecked((byte)(chunk[k >> 1] & 0x0F)), pic.map),k - 4]));
                            else
                                pic.data[dstOff] = unchecked((byte)(pic.data[dstOff] | GDConsts.cgaPixPos[Blit.SrcNibble(unchecked((byte)((chunk[k >> 1] & 0xF0) >> 4)), pic.map),k - 4]));
                        }

                        if (pic.transp != null)
                            pic.data[dstOff] = unchecked((byte)(pic.data[dstOff] & ~pic.transp[unchecked((ushort)(maskOff - 1))]));

                        dstOff = unchecked((ushort)(dstOff + 1));
                        srcOff = unchecked((ushort)(srcOff + 4));
                    }
                }
            }
        }

        // Reverse the order of eight two-bit pixels per cell, including their mask.
        // Only the first frame is mirrored, matching the original source's single height-bounded loop.
        public static void MirrorFrame(Types.TImgBuf src, Types.TImgBuf dst)
        {
            int width = src.width;
            int height = src.height;
            ushort rowBytes = unchecked((ushort)(width * 2));
            ushort srcOff = unchecked((ushort)(rowBytes - 2));
            ushort dstOff = 0;

            for (int row = 1; row <= height; row++)
            {
                ushort rowStart = dstOff;
                for (int col = 0; col < width; col++)
                {
                    ushort cell = unchecked((ushort)(src.data[unchecked((ushort)(srcOff - dstOff + rowStart))] | (ushort)(src.data[unchecked((ushort)(srcOff - dstOff + rowStart + 1))] << 8)));
                    cell = Blit.MirrorPairs(cell);
                    dst.data[dstOff] = unchecked((byte)(cell & 0xFF));
                    dst.data[unchecked((ushort)(dstOff + 1))] = unchecked((byte)(cell >> 8));

                    if (src.transp != null)
                    {
                        ushort tcell = unchecked((ushort)(src.transp[unchecked((ushort)(srcOff - dstOff + rowStart))] | (ushort)(src.transp[unchecked((ushort)(srcOff - dstOff + rowStart + 1))] << 8)));
                        tcell = Blit.MirrorPairs(tcell);
                        dst.transp[dstOff] = unchecked((byte)(tcell & 0xFF));
                        dst.transp[unchecked((ushort)(dstOff + 1))] = unchecked((byte)(tcell >> 8));
                    }

                    dstOff = unchecked((ushort)(dstOff + 2));
                }
                srcOff = unchecked((ushort)(srcOff + rowBytes));
            }
        }

        // Replace fromC with toC in nFrames frames of pic from srcOff, matching the original pixels and writing
        // scratch, which holds a copy of the frames; with isDithered, a random part of the matches.
        public static void RemapColour(Types.TImgBuf pic, Types.TImgBuf scratch, ushort srcOff, byte nFrames, byte fromC, byte toC, bool isDithered)
        {
            ushort dstOff = 0;
            int height = scratch.height;
            int rowBytes = scratch.width * 2;

            for (int frameNo = 1; frameNo <= nFrames; frameNo++)
            {
                for (int row = 1; row <= height; row++)
                {
                    for (int col = 1; col <= rowBytes; col++)
                    {
                        for (int pixPos = 0; pixPos <= 3; pixPos++)
                        {
                            if ((pic.data[srcOff] & GDConsts.cgaPixelMask[pixPos + 4]) == GDConsts.cgaPixPos[Blit.SrcNibble(fromC, scratch.map),pixPos])
                            {
                                if (!isDithered || (isDithered && Dice.RandomBelow(4) == 0))
                                {
                                    scratch.data[dstOff] = unchecked((byte)(
                                        (scratch.data[dstOff] & ~GDConsts.cgaPixelMask[pixPos + 4]) |
                                        GDConsts.cgaPixPos[Blit.SrcNibble(toC, scratch.map),pixPos]
                                    ));
                                }
                            }
                        }
                        srcOff = unchecked((ushort)(srcOff + 1));
                        dstOff = unchecked((ushort)(dstOff + 1));
                    }
                }
            }
        }

        // Recolor the first frame through a noncascading source-color remap.
        public static void RecolorFrame(ref Types.TColorTab toT, Types.TImgBuf pic)
        {
            Types.TColorTab fromT = new Types.TColorTab();
            for (int k = 0; k <= 15; k++)
                fromT[k] = unchecked((byte)k);
            Blit.RemapPic(fromT, toT, 0, false, pic);
        }

        public static void PutPixelRow(Types.TImgBuf pic, ushort at, byte y, ref Types.TPixelRow px)
        {
            for (int i = 0; i <= 1; i++)
            {
                byte b = 0;
                for (int k = 0; k <= 3; k++)
                {
                    b = unchecked((byte)(b | ((GDConsts.attrTable[unchecked((byte)(px[i * 4 + k] & 0x0F))] & 3) << (6 - 2 * k))));
                }
                pic.data[unchecked((ushort)(at + y * 2 + i))] = b;
            }
        }

        public static void InitRowTables()
        {
            for (int i = 0; i <= 199; i++)
            {
                if ((i & 1) != 0)
                    GDVars.cgaRowSeg[i] = unchecked((ushort)((i / 2) * 5 + C_Video.GFX_BANK1_SEG));
                else
                    GDVars.cgaRowSeg[i] = unchecked((ushort)((i / 2) * 5 + C_Video.GFX_BANK0_SEG));
            }
        }
    }
}
