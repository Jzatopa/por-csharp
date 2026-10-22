using System;
using static Por.Core.Types;

namespace Por.Core
{
    public static class Sym8x8
    {
        public static void Load8x8Set(byte fileID, byte setID)
        {
            if (setID <= 4)
            {
                string s1 = GDVars.virtualDisk.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (s1.Length > 1) s1 = s1.Substring(0, 1);

                Blit.LoadPic("8x8d" + s1, fileID, true, 13, ref GDVars.dsSymbolSetPtrs[setID]);

                if (GDVars.dsSymbolSetPtrs[setID] == null)
                {
                    GameIo.FatalError("Unable to load 8x8D" + s1 + "-" + StrUtil.NumStr(fileID));
                }
            }
        }

        public static void Put8x8Sym(sbyte col, sbyte row, ushort symbol, bool inCombat, bool other)
        {
            byte setIdx;
            if (symbol >= 1 && symbol <= 45) setIdx = 0;
            else if (symbol >= 46 && symbol <= 115) setIdx = 1;
            else if (symbol >= 116 && symbol <= 185) setIdx = 2;
            else if (symbol >= 186 && symbol <= 255) setIdx = 3;
            else if (symbol >= 256 && symbol <= 286) setIdx = 4;
            else
            {
                GameIo.FatalError("Bad symbol number in Put8x8Symbol.");
                return;
            }

            TImgBuf buf = GDVars.dsSymbolSetPtrs[setIdx];
            if (buf != null)
            {
                symbol = unchecked((ushort)(symbol - GDConsts.symSetFirst[setIdx]));

                if (buf.height != 8 || buf.width != 1 || symbol >= buf.count)
                    return;

                if (inCombat)
                {
                    byte localIdx = unchecked((byte)symbol);
                    Blit.BlitTileCell(col, row, localIdx, (byte)(other ? 1 : 0), buf, GDVars.dsViewImageBufferPtr);
                }
                else
                {
                    int imgSize = buf.imgSize;
                    int srcOffset = symbol * imgSize;
                    Array.Copy(buf.data, srcOffset, GDVars.dsCurrent8x8SymbolPtr2.data, 0, imgSize);
                    Blit.ShowPic(col, row, GDVars.dsCurrent8x8SymbolPtr2);
                }
            }
        }
    }
}
