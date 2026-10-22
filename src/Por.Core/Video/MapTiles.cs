using System;
using static Por.Core.Types;

namespace Por.Core
{
    public static class MapTiles
    {
        public static void Load24x24Set(string name, byte mult, sbyte unused)
        {
            if (name.Length > 255) name = name.Substring(0, 255);
            if (mult > 127)
            {
                GameIo.FatalError("Start range error in Load24x24Set");
            }

            TImgBuf pic = null;
            Blit.LoadPic(name, 1, false, 0, ref pic);

            if (pic == null)
                return;

            if (GDVars.ds2dMapTilePtr != null)
            {
                int tiles = pic.count;
                ushort frameCount = Blit.FrameCount(GDVars.ds2dMapTilePtr);
                if (mult + tiles > frameCount)
                    tiles = frameCount - mult;

                for (int i = 0; i < tiles; i++)
                {
                    TImgBuf dst = GDVars.ds2dMapTilePtr;
                    ushort frame = unchecked((ushort)(mult + i));
                    Blit.FrameBlock(ref dst, ref frame);
                    Array.Copy(pic.data, i * pic.imgSize, dst.data, frame * dst.imgSize, pic.imgSize);
                }
            }

            Blit.FreePic(ref pic);
        }

        public static void Copy2DTile(sbyte col, sbyte row, byte tile, sbyte unused)
        {
            if (tile > C_Tile.ID_MASK)
            {
                if (SecondSheet() != null)
                {
                    Blit.BlitTileCell(col, row, unchecked((byte)(tile & C_Tile.ID_MASK)), 0, GDVars.ds2dMapTilePtr2, GDVars.dsViewImageBufferPtr);
                }
            }
            else if (GDVars.ds2dMapTilePtr != null)
            {
                Blit.BlitTileCell(col, row, tile, 0, GDVars.ds2dMapTilePtr, GDVars.dsViewImageBufferPtr);
            }
        }

        public static TImgBuf SecondSheet()
        {
            if (GDVars.ds2dMapTilePtr2 == null)
            {
                Blit.LoadPic("sqrpaci", 2, false, 0, ref GDVars.ds2dMapTilePtr2);
            }
            return GDVars.ds2dMapTilePtr2;
        }

        public static void FreeSecondSheet()
        {
            Blit.FreePic(ref GDVars.ds2dMapTilePtr2);
        }

        public static void FreeIconAnim(byte animIdx)
        {
            if (animIdx > 25)
                return;

            if (GDVars.dsIconAnimation[animIdx, 0] != null)
                Blit.FreePic(ref GDVars.dsIconAnimation[animIdx, 0]);

            if (GDVars.dsIconAnimation[animIdx, 1] != null)
                Blit.FreePic(ref GDVars.dsIconAnimation[animIdx, 1]);
        }

        public static void LoadIconAnim(string name, byte fileId, byte animIdx)
        {
            if (name.Length > 255) name = name.Substring(0, 255);
            if (animIdx > 25)
                return;

            string suffix;

            if (name.Length >= 5 && (name.Substring(0, 5) == "CHEAD" || name.Substring(0, 5) == "CBODY"))
            {
                if (name.Length > 0 && char.ToUpperInvariant(name[name.Length - 1]) == 'T')
                    fileId = unchecked((byte)(fileId + 64));

                name = name.Substring(0, name.Length - 1);
                suffix = "";

                Blit.LoadPic(name + suffix, fileId, true, 0, ref GDVars.dsIconAnimation[animIdx, 0]);
                Blit.LoadPic(name + suffix, unchecked((byte)(fileId + 128)), true, 0, ref GDVars.dsIconAnimation[animIdx, 1]);
            }
            else if (name == "COMSPR" || name == "ICON")
            {
                suffix = "";

                Blit.LoadPic(name + suffix, fileId, true, 0, ref GDVars.dsIconAnimation[animIdx, 0]);
                Blit.LoadPic(name + suffix, unchecked((byte)(fileId + 128)), true, 0, ref GDVars.dsIconAnimation[animIdx, 1]);

                if (name == "ICON")
                {
                    Blit.RemapPic(GDConsts.iconColorIdentity, GDConsts.iconColorPinkToBlack, 0, false, GDVars.dsIconAnimation[animIdx, 0]);
                    Blit.RemapPic(GDConsts.iconColorIdentity, GDConsts.iconColorPinkToBlack, 0, false, GDVars.dsIconAnimation[animIdx, 1]);
                }
            }
            else
            {
                suffix = GDVars.virtualDisk.ToString(System.Globalization.CultureInfo.InvariantCulture).Substring(0, 1);

                Blit.LoadPic(name + suffix, fileId, true, 0, ref GDVars.dsIconAnimation[animIdx, 0]);
                Blit.RemapPic(GDConsts.iconColorIdentity, GDConsts.iconColorPinkToDarkGray, 0, false, GDVars.dsIconAnimation[animIdx, 0]);

                Blit.LoadPic(name + suffix, unchecked((byte)(fileId + 128)), true, 0, ref GDVars.dsIconAnimation[animIdx, 1]);
                Blit.RemapPic(GDConsts.iconColorIdentity, GDConsts.iconColorPinkToDarkGray, 0, false, GDVars.dsIconAnimation[animIdx, 1]);
            }
        }

        public static void DrawIconAnimSprite(sbyte col, sbyte row, byte facing, byte frame, byte animIdx)
        {
            if (animIdx > 25 || frame > 1)
                return;

            TImgBuf src = GDVars.dsIconAnimation[animIdx, frame];
            if (src == null)
                return;

            TImgBuf work = null;
            Blit.AllocPic(src.height, src.width, 1, 1, ref work);

            if (facing > C_Dir.SOUTHEAST)
            {
                Blit.MirrorPic(src, work);
            }
            else
            {
                Array.Copy(src.data, 0, work.data, 0, src.imgSize);
                Array.Copy(src.transp.Data, 0, work.transp.Data, 0, Blit.MaskBytes(src.imgSize));
            }

            Blit.BlitTileClipped((short)(col * 3), (short)(row * 3), 0, 1, work, GDVars.dsViewImageBufferPtr);
            Blit.FreePic(ref work);
        }
    }
}
