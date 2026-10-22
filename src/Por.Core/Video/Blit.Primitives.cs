// FILE: src/Por.Core/Video/Blit.Primitives.cs
// Partial translation of unit Blit: only the pixel-level primitives (ComputeMask, MirrorBits, MaskBytes)
// used by PicVga are implemented here. The remainder of unit Blit (span blitting, picture remap
// orchestration, MirrorPairs, MirrorNibbles, etc.) is NOT translated in this file.
namespace Por.Core
{
    public static partial class Blit
    {
        public static byte ComputeMask(byte transpColor, byte[] chunk)
        {
            byte mask = 0;
            byte transpHigh = unchecked((byte)(transpColor << 4));
            byte bit = 0x80;

            for (int i = 0; i < 4; i++)
            {
                if ((chunk[i] & 0xF0) == transpHigh)
                    mask = unchecked((byte)(mask + bit));
                bit = unchecked((byte)(bit >> 1));

                if ((chunk[i] & 0x0F) == transpColor)
                    mask = unchecked((byte)(mask + bit));
                bit = unchecked((byte)(bit >> 1));
            }

            return mask;
        }

        public static byte MirrorBits(byte value)
        {
            int mirrored = value << 7;
            mirrored += (value & 0x02) << 5;
            mirrored += (value & 0x04) << 3;
            mirrored += (value & 0x08) << 1;
            mirrored += (value & 0x10) >> 1;
            mirrored += (value & 0x20) >> 3;
            mirrored += (value & 0x40) >> 5;
            mirrored += (value & 0x80) >> 7;
            return unchecked((byte)mirrored);
        }

        public static ushort MaskBytes(ushort imageBytes)
        {
            return unchecked((ushort)(imageBytes >> GDVars.video.maskShift));
        }
    }
}

