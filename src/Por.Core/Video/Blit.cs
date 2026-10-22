using System;
using static Por.Core.Types;

namespace Por.Core
{
    public static partial class Blit
    {
        // Explicit BIOS boundary; a desktop host must install an adapter before changing modes.
        public static Action<byte, TInterruptRegisters> Interrupt;
        public static byte SrcNibble(byte color, TColorMap map)
        {
            if ((color & 1) != 0)
                return unchecked((byte)(map[color >> 1] & 0x0F));
            else
                return unchecked((byte)((map[color >> 1] & 0xF0) >> 4));
        }

        public static void SetVideoMode(byte mode)
        {
            GDVars.savedVideoMode = GDVars.curVideoMode;
            GDVars.dsInterruptRegisters.ax = mode;
            if (Interrupt == null)
                throw new NotSupportedException("No video adapter available");
            Interrupt(0x10, GDVars.dsInterruptRegisters);
            GDVars.curVideoMode = mode;
        }

        public static void RestoreVideoMode()
        {
            SetVideoMode(GDVars.savedVideoMode);
        }

        public static void LoadPic(string name, byte fileId, bool hasMask, byte transpColor, ref TImgBuf pic)
        {
            if (pic != null)
                FreePic(ref pic);

            ushort dataSize;
            byte[] data;
            DaxIo.LoadDax(name, ".dax", fileId, out dataSize, out data);

            if (dataSize == 0)
                return;

            ushort picHeight = unchecked((ushort)(data[0] | (data[1] << 8)));
            ushort picWidth = unchecked((ushort)(data[2] | (data[3] << 8)));
            ushort picOriginX = unchecked((ushort)(data[4] | (data[5] << 8)));
            ushort picOriginY = unchecked((ushort)(data[6] | (data[7] << 8)));
            byte picCount = data[8];
            TColorMap colorMap = new TColorMap();
            for (int i = 0; i < 8; i++)
                colorMap.Data[i] = data[9 + i];

            AllocPic(picHeight, picWidth, picCount, hasMask ? (byte)1 : (byte)0, ref pic);

            pic.originX = picOriginX;
            pic.originY = picOriginY;
            pic.map = colorMap;

            ushort readOfs = 17;
            ExpandPicFrames(data, readOfs, hasMask, transpColor, pic);
        }

        public static void ExpandPicFrames(byte[] imgData, ushort srcOff, bool hasMask, byte transpColor, TImgBuf pic)
        {
            while (pic != null)
            {
                sbyte[] sbyteData = new sbyte[65535];
                for (int i = 0; i < imgData.Length; i++)
                    sbyteData[i] = unchecked((sbyte)imgData[i]);

                TByteArr tba = new TByteArr(sbyteData);
                GDVars.video.ExpandFrames(tba, srcOff, hasMask, transpColor, pic);

                srcOff = unchecked((ushort)(srcOff + pic.count * pic.height * pic.width * 4));
                pic = pic.more;
            }
        }

        private static void BlitTile(short screenX, short screenY, byte tileIdx, byte flag, TImgBuf gfx, TImgBuf dest, bool clip)
        {
            while (gfx != null && gfx.more != null && tileIdx >= gfx.count)
            {
                tileIdx = unchecked((byte)(tileIdx - gfx.count));
                gfx = gfx.more;
            }

            if (gfx == null || dest == null)
                return;

            bool useMask = ((flag & 1) > 0) && (gfx.transp != null);
            bool saveRequested = (flag & 4) > 0;

            ushort clipLeft = 0;
            ushort clipRight = 0;
            ushort leftSkip = 0;
            ushort rightSkip = 0;
            ushort srcOff = 0;
            ushort destOff = 0;
            ushort saveOff = 0;
            ushort lastRow = 0;
            ushort scanline = 0;

            if (clip)
            {
                if (screenX < 0)
                {
                    clipLeft = unchecked((ushort)(-screenX));
                    if (clipLeft >= gfx.width)
                        return;
                    leftSkip = 0;
                }
                else
                {
                    clipLeft = 0;
                    leftSkip = unchecked((ushort)screenX);
                    if (leftSkip >= dest.width)
                        return;
                }

                if (dest.width < unchecked((ushort)(screenX + gfx.width)))
                {
                    clipRight = unchecked((ushort)(screenX + gfx.width - dest.width));
                    if (clipRight >= gfx.width)
                        return;
                    rightSkip = 0;
                }
                else
                {
                    clipRight = 0;
                    rightSkip = unchecked((ushort)(dest.width - (screenX + gfx.width)));
                    if (rightSkip > dest.width)
                        return;
                }

                if (screenY < 0)
                {
                    srcOff = unchecked((ushort)(-screenY * gfx.width * 8));
                    destOff = 0;
                    scanline = 0;
                    lastRow = unchecked((ushort)(gfx.height - 1 - (-screenY) * 8));
                    if (gfx.imgSize - 1 < srcOff || gfx.height - 1 < lastRow)
                        return;
                }
                else
                {
                    srcOff = 0;
                    scanline = unchecked((ushort)(screenY * 8));
                    destOff = unchecked((ushort)(scanline * dest.width));
                    lastRow = unchecked((ushort)(gfx.height - 1));
                    if (dest.height < unchecked((ushort)(screenY * 8 + gfx.height)))
                        lastRow = unchecked((ushort)(dest.height - 1 - screenY * 8));
                    else
                        lastRow = unchecked((ushort)(gfx.height - 1));

                    if (gfx.imgSize - 1 < srcOff || dest.imgSize - 1 < destOff || gfx.height - 1 < lastRow)
                        return;
                }
            }
            else
            {
                clipLeft = 0;
                clipRight = 0;
                srcOff = 0;
                scanline = unchecked((ushort)(screenY * 8));
                destOff = unchecked((ushort)(scanline * dest.width));
                lastRow = unchecked((ushort)(gfx.height - 1));
                leftSkip = unchecked((ushort)screenX);
                rightSkip = unchecked((ushort)(dest.width - (screenX + gfx.width)));
            }

            byte cs = GDVars.video.cellShift;
            byte ms = GDVars.video.maskShift;
            ushort screenOff = unchecked((ushort)(leftSkip << GDVars.video.screenShift));
            ushort rowBytes = unchecked((ushort)((gfx.width - clipLeft - clipRight) << cs));
            clipLeft = unchecked((ushort)(clipLeft << cs));
            clipRight = unchecked((ushort)(clipRight << cs));
            leftSkip = unchecked((ushort)(leftSkip << cs));
            rightSkip = unchecked((ushort)(rightSkip << cs));
            srcOff = unchecked((ushort)(srcOff << cs));
            saveOff = srcOff;
            srcOff = unchecked((ushort)(tileIdx * gfx.imgSize + srcOff));
            ushort maskOff = unchecked((ushort)(srcOff >> ms));
            ushort maskClipLeft = unchecked((ushort)(clipLeft >> ms));
            ushort maskClipRight = unchecked((ushort)(clipRight >> ms));
            byte planesPerMask = unchecked((byte)(1 << ms));
            byte planesLeft = planesPerMask;
            bool maskEach = useMask && (ms == 0);
            destOff = unchecked((ushort)(destOff << cs));

            for (int y = 0; y <= lastRow; y++)
            {
                GDVars.spanActive[scanline] = 1;
                srcOff = unchecked((ushort)(srcOff + clipLeft));
                saveOff = unchecked((ushort)(saveOff + clipLeft));
                destOff = unchecked((ushort)(destOff + leftSkip));

                if (GDVars.spanFirstOfs[scanline] > destOff)
                {
                    GDVars.spanScreenOfs[scanline] = screenOff;
                    GDVars.spanFirstOfs[scanline] = destOff;
                }

                if (saveRequested)
                    Array.Copy(dest.data, destOff, GDVars.dsIconImageBufferPtr.data, saveOff, rowBytes);

                saveOff = unchecked((ushort)(saveOff + rowBytes));

                if (useMask && GDVars.video.keyed)
                {
                    for (int i = 0; i < rowBytes; i++)
                    {
                        if (gfx.data[srcOff] != Types.PIC_KEY)
                            dest.data[destOff] = gfx.data[srcOff];
                        srcOff = unchecked((ushort)(srcOff + 1));
                        destOff = unchecked((ushort)(destOff + 1));
                    }
                }
                else if (maskEach)
                {
                    for (int i = 0; i < rowBytes; i++)
                    {
                        dest.data[destOff] = unchecked((byte)((dest.data[destOff] & gfx.transp.Data[srcOff]) | gfx.data[srcOff]));
                        srcOff = unchecked((ushort)(srcOff + 1));
                        destOff = unchecked((ushort)(destOff + 1));
                    }
                }
                else if (useMask)
                {
                    maskOff = unchecked((ushort)(maskOff + maskClipLeft));
                    for (int i = 0; i < rowBytes; i++)
                    {
                        dest.data[destOff] = unchecked((byte)((dest.data[destOff] & gfx.transp.Data[maskOff]) | gfx.data[srcOff]));
                        srcOff = unchecked((ushort)(srcOff + 1));
                        planesLeft = unchecked((byte)(planesLeft - 1));
                        if (planesLeft == 0)
                        {
                            planesLeft = planesPerMask;
                            maskOff = unchecked((ushort)(maskOff + 1));
                        }
                        destOff = unchecked((ushort)(destOff + 1));
                    }
                    maskOff = unchecked((ushort)(maskOff + maskClipRight));
                }
                else
                {
                    Array.Copy(gfx.data, srcOff, dest.data, destOff, rowBytes);
                    srcOff = unchecked((ushort)(srcOff + rowBytes));
                    destOff = unchecked((ushort)(destOff + rowBytes));
                }

                if (destOff - 1 > GDVars.spanLastOfs[scanline])
                    GDVars.spanLastOfs[scanline] = unchecked((ushort)(destOff - 1));

                srcOff = unchecked((ushort)(srcOff + clipRight));
                saveOff = unchecked((ushort)(saveOff + clipRight));
                destOff = unchecked((ushort)(destOff + rightSkip));
                scanline = unchecked((ushort)(scanline + 1));
            }
        }

        public static void BlitTileCell(short screenX, short screenY, byte tileIdx, byte flag, TImgBuf gfx, TImgBuf dest)
        {
            BlitTile(screenX, screenY, tileIdx, unchecked((byte)(flag & ~4)), gfx, dest, false);
        }

        public static void BlitTileClipped(short screenX, short screenY, byte tileIdx, byte flag, TImgBuf gfx, TImgBuf dest)
        {
            BlitTile(screenX, screenY, tileIdx, flag, gfx, dest, GDVars.clipFlag != 0);
        }

        public static ushort MirrorPairs(ushort value)
        {
            ushort mirrored = unchecked((ushort)((value & GDConsts.cgaPixelMask[7]) << 14));
            mirrored = unchecked((ushort)(mirrored + ((value & GDConsts.cgaPixelMask[6]) << 10)));
            mirrored = unchecked((ushort)(mirrored + ((value & GDConsts.cgaPixelMask[5]) << 6)));
            mirrored = unchecked((ushort)(mirrored + ((value & GDConsts.cgaPixelMask[4]) << 2)));
            mirrored = unchecked((ushort)(mirrored + ((value & GDConsts.cgaPixelMask[3]) >> 2)));
            mirrored = unchecked((ushort)(mirrored + ((value & GDConsts.cgaPixelMask[2]) >> 6)));
            mirrored = unchecked((ushort)(mirrored + ((value & GDConsts.cgaPixelMask[1]) >> 10)));
            mirrored = unchecked((ushort)(mirrored + ((value & GDConsts.cgaPixelMask[0]) >> 14)));
            return mirrored;
        }

        public static byte MirrorNibbles(byte value)
        {
            byte mirrored = unchecked((byte)(value << 4));
            mirrored = unchecked((byte)(mirrored + ((value & 0xF0) >> 4)));
            return mirrored;
        }

        public static void MirrorPic(TImgBuf src, TImgBuf dst)
        {
            if (src == null || dst == null)
                return;

            Array.Copy(src.map.Data, dst.map.Data, 8);
            GDVars.video.MirrorFrame(src, dst);
        }

        public static void ShowPic(short screenX, short screenY, TImgBuf pic)
        {
            if (pic == null)
                return;
            GDVars.video.ShowPic(screenX, screenY, pic);
        }

        public static void FlushView()
        {
            GDVars.video.FlushSpans();
            Array.Clear(GDVars.spanActive, 0, GDVars.spanActive.Length);
            for (int i = 0; i < GDVars.spanFirstOfs.Length; i++)
                GDVars.spanFirstOfs[i] = 0xFFFF;
            Array.Clear(GDVars.spanLastOfs, 0, GDVars.spanLastOfs.Length);
        }

        public static void RemapPic(TColorTab fromColors, TColorTab toColors, short frame, bool isDithered, TImgBuf pic)
        {
            if (pic == null)
                return;

            bool allFrames = frame < 0;

            if (!allFrames)
            {
                while (pic.more != null && frame >= pic.count)
                {
                    frame = unchecked((short)(frame - pic.count));
                    pic = pic.more;
                }
            }

            if (frame < 0)
            {
                frame = 0;
            }

            byte nFrames = allFrames ? pic.count : (byte)1;
            ushort srcOff = unchecked((ushort)(frame * pic.imgSize));
            ushort dstOff = 0;

            TImgBuf scratchPic = null;
            AllocPic(pic.height, pic.width, nFrames, 0, ref scratchPic);
            Array.Copy(pic.map.Data, scratchPic.map.Data, 8);

            Array.Copy(pic.data, srcOff, scratchPic.data, dstOff, nFrames * scratchPic.imgSize);

            for (int pairIdx = 0; pairIdx < 16; pairIdx++)
            {
                srcOff = unchecked((ushort)(frame * pic.imgSize));
                dstOff = 0;
                if (fromColors[pairIdx] != toColors[pairIdx])
                    GDVars.video.RemapColour(pic, scratchPic, srcOff, nFrames, fromColors[pairIdx], toColors[pairIdx], isDithered);
            }

            Array.Copy(scratchPic.data, 0, pic.data, frame * pic.imgSize, nFrames * pic.imgSize);

            FreePic(ref scratchPic);

            if (allFrames && pic.more != null)
                RemapPic(fromColors, toColors, -1, isDithered, pic.more);
        }

        public static void AllocPic(ushort height, ushort width, byte count, byte flag, ref TImgBuf pic)
        {
            ushort frameSize = FrameBytes(height, width);

            if (frameSize == 0)
                throw new DivideByZeroException();

            long fit = (65520 - 27) / frameSize;

            if (fit == 0)
                throw new InvalidOperationException("A picture of " + height + " rows and " + width + " cells is too large for the " + GDVars.video.name + " video mode.");

            byte rest = 0;
            if (count > fit)
            {
                rest = unchecked((byte)(count - fit));
                count = unchecked((byte)fit);
            }

            ushort totalSize = unchecked((ushort)(count * frameSize));

            pic = new TImgBuf();
            pic.data = new byte[totalSize];
            pic.height = height;
            pic.width = width;
            pic.imgSize = frameSize;
            pic.count = count;

            if ((flag & 1) > 0)
            {
                ushort maskSize = MaskBytes(totalSize);
                pic.transp = new TBytes();
                pic.transp.Data = new byte[maskSize];
            }

            GDVars.picBytes += totalSize + 27;
            if ((flag & 1) > 0)
                GDVars.picBytes += MaskBytes(totalSize);

            if (GDVars.picBytes > GDVars.picPeak)
                GDVars.picPeak = GDVars.picBytes;

            if (rest > 0)
                AllocPic(height, width, rest, flag, ref pic.more);
        }

        public static void FreePic(ref TImgBuf pic)
        {
            if (pic == null)
                return;

            FreePic(ref pic.more);

            ushort nBytes = unchecked((ushort)(pic.count * pic.imgSize));

            if (pic.transp != null)
            {
                ushort maskSize = MaskBytes(nBytes);
                GDVars.picBytes -= maskSize;
                pic.transp = null;
            }

            GDVars.picBytes -= nBytes + 27;
            pic = null;
        }

        public static void FillPicRect(short x, short y, ushort width, ushort height, byte color, TColorMap colorMap, TImgBuf pic)
        {
            if (pic == null)
                return;

            if (x >= pic.width || x + width <= 0 || y >= pic.height || y + height <= 0)
                return;

            ushort clipLeft = 0;
            ushort clipRight = 0;
            ushort clampedX = 0;
            ushort rightMargin = 0;
            ushort dstOff = 0;
            ushort lastRow = 0;
            ushort absRow = 0;

            if (x < 0)
            {
                clipLeft = unchecked((ushort)(-x));
                clampedX = 0;
            }
            else
            {
                clipLeft = 0;
                clampedX = unchecked((ushort)x);
            }

            if (pic.width < x + width)
            {
                clipRight = unchecked((ushort)(x + width - pic.width));
                rightMargin = 0;
            }
            else
            {
                clipRight = 0;
                rightMargin = unchecked((ushort)(pic.width - (x + width)));
            }

            if (y < 0)
            {
                dstOff = 0;
                absRow = 0;
                lastRow = unchecked((ushort)(height - (-y) - 1));
                if (lastRow > pic.height - 1)
                    lastRow = unchecked((ushort)(pic.height - 1));
            }
            else
            {
                absRow = unchecked((ushort)y);
                dstOff = unchecked((ushort)(absRow * pic.width));
                lastRow = unchecked((ushort)(height - 1));
                if (pic.height < y + height)
                    lastRow = unchecked((ushort)(pic.height - 1 - y));
                else
                    lastRow = unchecked((ushort)(height - 1));
            }

            ushort screenOfs = unchecked((ushort)(clampedX << GDVars.video.screenShift));
            ushort cells = unchecked((ushort)(width - clipLeft - clipRight));
            ushort runLen = unchecked((ushort)(cells << GDVars.video.cellShift));
            clampedX = unchecked((ushort)(clampedX << GDVars.video.cellShift));
            rightMargin = unchecked((ushort)(rightMargin << GDVars.video.cellShift));
            dstOff = unchecked((ushort)(dstOff << GDVars.video.cellShift));

            TCellBytes cell = new TCellBytes();
            var mapCopy = new TColorMap((byte[])colorMap.Data.Clone());
            GDVars.video.CellFill(color, ref mapCopy, ref cell);

            byte cellLen = unchecked((byte)(1 << GDVars.video.cellShift));
            bool uniform = true;
            for (int i = 1; i < cellLen; i++)
            {
                if (cell.Data[i] != cell.Data[0])
                {
                    uniform = false;
                    break;
                }
            }

            for (int row = 0; row <= lastRow; row++)
            {
                GDVars.spanActive[absRow] = 1;
                dstOff = unchecked((ushort)(dstOff + clampedX));

                if (GDVars.spanFirstOfs[absRow] > dstOff)
                {
                    GDVars.spanScreenOfs[absRow] = screenOfs;
                    GDVars.spanFirstOfs[absRow] = dstOff;
                }

                if (uniform)
                {
                    for (int i = 0; i < runLen; i++)
                        pic.data[dstOff + i] = cell.Data[0];
                    dstOff = unchecked((ushort)(dstOff + runLen));
                }
                else if (cellLen == 4)
                {
                    for (int i = 0; i < cells; i++)
                    {
                        pic.data[dstOff] = cell.Data[0];
                        pic.data[dstOff + 1] = cell.Data[1];
                        pic.data[dstOff + 2] = cell.Data[2];
                        pic.data[dstOff + 3] = cell.Data[3];
                        dstOff = unchecked((ushort)(dstOff + 4));
                    }
                }
                else
                {
                    for (int i = 0; i < cells; i++)
                    {
                        for (int j = 0; j < cellLen; j++)
                            pic.data[dstOff + j] = cell.Data[j];
                        dstOff = unchecked((ushort)(dstOff + cellLen));
                    }
                }

                if (dstOff - 1 > GDVars.spanLastOfs[absRow])
                    GDVars.spanLastOfs[absRow] = unchecked((ushort)(dstOff - 1));

                dstOff = unchecked((ushort)(dstOff + rightMargin));
                absRow = unchecked((ushort)(absRow + 1));
            }
        }

        public static void SetPaletteRegister(byte registerIndex, byte color)
        {
            if (HasPalette())
                GDVars.video.SetPalette(registerIndex, color);
        }

        public static void FrameBlock(ref TImgBuf pic, ref ushort frame)
        {
            while (pic != null && pic.more != null && frame >= pic.count)
            {
                frame = unchecked((ushort)(frame - pic.count));
                pic = pic.more;
            }
        }

        public static ushort FrameCount(TImgBuf pic)
        {
            ushort n = 0;
            while (pic != null)
            {
                n = unchecked((ushort)(n + pic.count));
                pic = pic.more;
            }
            return n;
        }

        public static void ClearPic(TImgBuf pic)
        {
            if (pic == null)
                return;

            byte clear = GDVars.video.keyed ? Types.PIC_KEY : (byte)0;
            for (int i = 0; i < pic.imgSize; i++)
                pic.data[i] = clear;

            if (pic.transp != null)
            {
                ushort maskSize = MaskBytes(pic.imgSize);
                for (int i = 0; i < maskSize; i++)
                    pic.transp.Data[i] = 0xFF;
            }
        }

        public static ushort FrameBytes(ushort height, ushort width)
        {
            return unchecked((ushort)((height * width) << GDVars.video.cellShift));
        }

        public static byte ColourCount()
        {
            return GDVars.video.colours;
        }

        public static bool HasPalette()
        {
            return GDVars.video.hasPalette;
        }
    }
}
