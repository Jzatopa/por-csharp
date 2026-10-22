using System;
using static Por.Core.Types;

namespace Por.Core
{
    public static class Portrait
    {
        public static Func<long> MaxAvailable;
        private static long HeapAvailable()
        {
            if (MaxAvailable == null) throw new NotSupportedException("Portrait heap measurement adapter missing.");
            return MaxAvailable();
        }
        private static ushort ReadWord(byte[] data, int at) { return (ushort)(data[at] | data[at + 1] << 8); }
        private static int ReadLong(byte[] data, int at) { return unchecked(data[at] | data[at + 1] << 8 | data[at + 2] << 16 | data[at + 3] << 24); }

        public static void DisplayImageBuf(short col, short row, bool anim, TImgBuf pic)
        {
            if (pic != null)
            {
                if (anim)
                {
                    Blit.BlitTileClipped(unchecked((short)(col - 1)), unchecked((short)(row - 1)), 0, 1, pic, GDVars.dsViewImageBufferPtr);
                    Blit.FlushView();
                }
                else
                {
                    Blit.ShowPic(col, row, pic);
                }
            }
        }

        private static void DisplayPortraitHead(byte col, byte row, TImgBuf pic)
        {
            DisplayImageBuf((short)col, (short)row, false, pic);
        }

        private static void DisplayPortraitBody(byte col, byte row, TImgBuf pic)
        {
            DisplayImageBuf((short)col, (short)(row + 5), false, pic);
        }

        public static void LoadAnimation(string name, byte picId, byte transp, ref TAnimRec a)
        {
            if (name.Length > 8) name = name.Substring(0, 8);
            ushort sz;
            byte[] buf;
            ushort offset;
            byte f;
            ushort h = 0;
            ushort w = 0;
            string diskCh;
            byte[] xorBuf = null;
            ushort planeSz;
            bool isPic;
            bool gotXor;
            ushort xorSz = 0;
            ushort xorLast;
            byte loaded;
            bool stop;

            diskCh = GDVars.virtualDisk.ToString(System.Globalization.CultureInfo.InvariantCulture).Substring(0, 1);
            if ((name + diskCh == GDVars.picArcName) && (picId == GDVars.picArcId))
                return;

            FreeAnimation(ref a);
            isPic = (name == "PIC") || (name == "FINAL");
            DaxIo.LoadDax(name + diskCh, ".dax", picId, out sz, out buf);
            if (sz == 0)
                return;

            GDVars.picArcName = name + diskCh;
            GDVars.picArcId = picId;
            offset = 0;
            gotXor = false;

            a.count = buf[offset];
            if ((name == "PIC") && (picId == 1))
            {
                if (a.count > 4) a.count = 4;
            }
            if (a.count > 8) a.count = 8;
            offset++;
            a.current = 1;
            loaded = 0;
            stop = false;
            f = 1;

            while ((f <= a.count) && !stop)
            {
                if ((long)offset + 4 + 2 + 2 > sz)
                {
                    stop = true;
                }
                else
                {
                    a.frames[loaded + 1].time = ReadLong(buf, offset);
                    offset += 4;
                    h = ReadWord(buf, offset);
                    offset += 2;
                    w = ReadWord(buf, offset);
                    offset += 2;

                    if ((h == 0) || (w == 0) || ((long)offset + 2 + 2 + 1 + 8 + (long)h * w * 4 > sz))
                    {
                        stop = true;
                    }
                }

                if (stop)
                {
                    // frames before it stay loaded
                }
                else if ((HeapAvailable() >= 3900) && (HeapAvailable() >= FrameNeed(h, w, transp, isPic, f)))
                {
                    loaded++;
                    Blit.AllocPic(h, w, 1, transp, ref a.frames[loaded].pic);
                    TImgBuf pic = a.frames[loaded].pic;

                    pic.originX = ReadWord(buf, offset);
                    offset += 2;
                    pic.originY = ReadWord(buf, offset);
                    offset += 2;
                    offset += 1; // count
                    for (int i = 0; i < 8; i++)
                    {
                        pic.map.Data[i] = buf[offset + i];
                    }
                    offset += 8;

                    planeSz = (ushort)(pic.height * pic.width * 4 - 1);

                    if (isPic)
                    {
                        if (f == 1)
                        {
                            xorSz = (ushort)(planeSz + 1);
                            xorBuf = new byte[xorSz];
                            Array.Copy(buf, offset, xorBuf, 0, xorSz);
                            gotXor = true;
                        }
                        else
                        {
                            xorLast = planeSz;
                            if (xorLast > xorSz - 1)
                                xorLast = (ushort)(xorSz - 1);

                            for (int j = offset; j <= offset + xorLast; j++)
                            {
                                buf[j] = (byte)(buf[j] ^ xorBuf[j - offset]);
                            }
                        }
                    }

                    Blit.ExpandPicFrames(buf, offset, transp != 0, 0, pic);
                    if (((transp & 1) > 0) && (Blit.ColourCount() == 16))
                    {
                        Blit.RemapPic(GDConsts.animRemapFrom, GDConsts.animRemapTo, 0, false, pic);
                    }

                    offset = (ushort)(offset + planeSz + 1);
                }
                else if (f == 1)
                {
                    stop = true;
                }
                else
                {
                    offset = (ushort)(offset + 2 + 2 + 1 + 8 + (ushort)(h * w * 4));
                }

                f++;
            }

            a.count = loaded;

            if (isPic && gotXor)
            {
                // FreeMem equivalent - in C# GC handles this, but we can null it
                xorBuf = null;
            }
            buf = null;
        }

        private static long FrameNeed(ushort h, ushort w, byte transp, bool isPic, byte f)
        {
            long frameSz = Blit.FrameBytes(h, w);
            long need = 27 + frameSz + 8; // Packed source header plus heap rounding.
            if ((transp & 1) > 0)
            {
                need += Blit.MaskBytes((ushort)frameSz) + 8;
                if (Blit.ColourCount() == 16)
                {
                    need += 27 + frameSz + 8;
                }
            }

            if (isPic && (f == 1))
            {
                need += (long)h * w * 4 + 8;
            }

            return need;
        }

        public static void FreeAnimation(ref TAnimRec a)
        {
            for (short k = 1; k <= a.count; k++)
            {
                Blit.FreePic(ref a.frames[k].pic);
            }

            // Clear all 9 frame time/pic fields and count/current
            for (int i = 0; i < 9; i++)
            {
                a.frames[i].time = 0;
                a.frames[i].pic = null;
            }
            a.count = 0;
            a.current = 0;

            GDVars.picArcName = "";
            GDVars.picArcId = 255;
        }

        public static void DisplayCharPortrait()
        {
            if ((GDVars.curChar.portraitHead == 0) || (GDVars.curChar.portraitBody == 0) ||
                (GDVars.curChar.portraitHead > GDConsts.headTbl.Length - 1) ||
                (GDVars.curChar.portraitBody > GDConsts.bodyTbl.Length - 1))
                return;

            Border.DrawWindow(28, 1, 38, 11, 0, 0, "");
            LoadPortraitBufs(GDConsts.headTbl[GDVars.curChar.portraitHead], GDConsts.bodyTbl[GDVars.curChar.portraitBody]);
            DisplayPortraitBufs(28, 1, true);
        }

        private static bool MergedPortraits()
        {
            return (DaxIo.ArchiveName("HEAD" + StrUtil.NumStr(GDVars.virtualDisk)) == "HEAD") &&
                   (DaxIo.ArchiveName("BODY" + StrUtil.NumStr(GDVars.virtualDisk)) == "BODY");
        }

        public static bool PlayerPortraitsHere()
        {
            return (GDVars.virtualDisk == 3) || MergedPortraits();
        }

        public static byte PlayerPortraitCount(bool body)
        {
            if (MergedPortraits())
            {
                if (body)
                    return (byte)(GDConsts.bodyTbl.Length - 1);
                else
                    return (byte)(GDConsts.headTbl.Length - 1);
            }
            else if (body)
                return unchecked((byte)GDConsts.DISK3_BODIES);
            else
                return unchecked((byte)GDConsts.DISK3_HEADS);
        }

        public static void LoadPortraitBufs(byte headId, byte bodyId)
        {
            string diskCh = GDVars.virtualDisk.ToString(System.Globalization.CultureInfo.InvariantCulture).Substring(0, 1);

            if (headId != 255)
            {
                if ((GDVars.dsPortraitCache[1].pictureId == 255) ||
                    (GDVars.dsPortraitCache[1].pictureId != headId) ||
                    (GDVars.dsPortraitCache[1].disk != GDVars.virtualDisk))
                {
                    Blit.LoadPic("HEAD" + diskCh, headId, false, 0, ref GDVars.dsPortraitCache[1].pic);
                    if (GDVars.dsPortraitCache[1].pic != null)
                    {
                        GDVars.dsPortraitCache[1].pictureId = headId;
                        GDVars.dsPortraitCache[1].disk = GDVars.virtualDisk;
                    }
                    else
                    {
                        GDVars.dsPortraitCache[1].pictureId = 255;
                    }
                }
            }

            if (bodyId != 255)
            {
                if ((GDVars.dsPortraitCache[2].pictureId == 255) ||
                    (GDVars.dsPortraitCache[2].pictureId != bodyId) ||
                    (GDVars.dsPortraitCache[2].disk != GDVars.virtualDisk))
                {
                    Blit.LoadPic("BODY" + diskCh, bodyId, false, 0, ref GDVars.dsPortraitCache[2].pic);
                    if (GDVars.dsPortraitCache[2].pic != null)
                    {
                        GDVars.dsPortraitCache[2].pictureId = bodyId;
                        GDVars.dsPortraitCache[2].disk = GDVars.virtualDisk;
                    }
                    else
                    {
                        GDVars.dsPortraitCache[2].pictureId = 255;
                    }
                }
            }
        }

        public static void DisplayPortraitBufs(byte col, byte row, bool both)
        {
            if (both)
            {
                DisplayPortraitHead(col, row, GDVars.dsPortraitCache[1].pic);
                DisplayPortraitBody(col, row, GDVars.dsPortraitCache[2].pic);
            }
            else
            {
                DisplayImageBuf((short)col, (short)row, false, GDVars.dsPortraitCache[1].pic);
            }
        }

        public static void Show3DSprite(byte dist, TAnimRec a)
        {
            if ((dist < 1) || (dist > 3))
            {
                GameIo.FatalError("Illegal range in Show3DSprite.");
            }

            if (a.frames[dist].pic != null)
            {
                TImgBuf pic = a.frames[dist].pic;
                Blit.BlitTileClipped(unchecked((short)(pic.originX + 2)), unchecked((short)(pic.originY + 2)), 0, 1, pic, GDVars.dsViewImageBufferPtr);
            }
            else
            {
                return;
            }

            Blit.FlushView();
        }
    }
}
