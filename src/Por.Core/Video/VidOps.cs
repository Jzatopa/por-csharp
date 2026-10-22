// FILE: src/Por.Core/Video/VidOps.cs
namespace Por.Core
{
    public static class VidOps
    {
        public static string ModeName()
        {
            return GDVars.video.name;
        }

        public static void InitRowTables()
        {
            GDVars.video.InitRowTables();
        }

        public static void MergePic(Types.TImgBuf src, Types.TImgBuf dst)
        {
            if (src == null || dst == null) return;
            if (src.transp == null || dst.transp == null) return;
            // an empty picture would make the loops below run 65536 times
            if (src.imgSize == 0) return;

            if (GDVars.video.keyed)
            {
                // a colour key (VGA): src's opaque pixels replace dst's
                for (int n = 0; n <= src.imgSize - 1; n++)
                {
                    if (src.data[n] != Types.PIC_KEY)
                        dst.data[n] = src.data[n];
                }
            }
            else
            {
                for (int n = 0; n <= src.imgSize - 1; n++)
                {
                    dst.data[n] = unchecked((byte)(dst.data[n] | src.data[n]));
                }
            }

            int maskBytes = Blit.MaskBytes(src.imgSize);
            for (int n = 0; n <= maskBytes - 1; n++)
            {
                dst.transp[n] = unchecked((byte)(dst.transp[n] & src.transp[n]));
            }
        }

        public static void OverlayPic(Types.TImgBuf src, Types.TImgBuf dst)
        {
            if (src == null || dst == null) return;
            if (src.transp == null || src.imgSize != dst.imgSize || dst.imgSize == 0) return;

            if (GDVars.video.keyed)
            {
                // a colour key (VGA)
                for (int n = 0; n <= dst.imgSize - 1; n++)
                {
                    if (src.data[n] != Types.PIC_KEY)
                        dst.data[n] = src.data[n];
                }
                return;
            }

            // one mask byte covers 1 shl maskShift picture bytes (EGA's cell row of four planes)
            for (int n = 0; n <= dst.imgSize - 1; n++)
            {
                int maskByte = src.transp[n >> GDVars.video.maskShift];
                dst.data[n] = unchecked((byte)((dst.data[n] & maskByte) | src.data[n]));
            }
        }

        public static void RecolorPic(ref Types.TColorTab toT, Types.TImgBuf pic)
        {
            if (pic == null) return;
            GDVars.video.RecolorFrame(ref toT, pic);
        }

        public static void PutPixelRow(Types.TImgBuf pic, byte frame, byte y, ref Types.TPixelRow px)
        {
            if (pic == null) return;
            ushort at = unchecked((ushort)(frame * pic.imgSize));
            GDVars.video.PutPixelRow(pic, at, y, ref px);
        }
    }
}

