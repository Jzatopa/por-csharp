using System;

namespace Por.Core
{
    public static partial class DaxIo
    {
        private static char UpCase(char c)
        {
            if (c >= 'a' && c <= 'z')
            {
                return (char)(c - 'a' + 'A');
            }
            return c;
        }

        public static string SanitizeString(string src, string del)
        {
            if (src == null) return null;

            if (src.Length > 255)
            {
                src = src.Substring(0, 255);
            }

            if (del == null || del.Length == 0)
            {
                return src;
            }

            if (del.Length > 255)
            {
                del = del.Substring(0, 255);
            }

            int delLen = del.Length;
            int limit = unchecked((byte)(src.Length - delLen + 1));

            string current = src;
            for (int i = 1; i <= limit; i++)
            {
                int start = i - 1;
                if (start < current.Length && current.Length - start >= delLen)
                {
                    if (current.Substring(start, delLen) == del)
                    {
                        current = current.Substring(0, start) + current.Substring(start + delLen);
                    }
                }
            }

            return current;
        }

        public static string SanitizeFileName(string name)
        {
            if (name == null) return null;

            if (name.Length > 255)
            {
                name = name.Substring(0, 255);
            }

            for (int i = 1; i <= 10; i++)
            {
                name = SanitizeString(name, GDConsts.invalidFileChars[i].ToString());
            }

            if (name.Length > 8)
            {
                name = name.Substring(0, 8);
            }

            char[] chars = name.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                chars[i] = UpCase(chars[i]);
            }

            return new string(chars);
        }

        public static byte[] DecompressDax(byte[] src, int outputLength)
        {
            if (outputLength < 0 || outputLength > 65535)
            {
                throw new ArgumentOutOfRangeException(nameof(outputLength), "outputLength must be between 0 and 65535.");
            }

            if (src == null)
            {
                throw new ArgumentNullException(nameof(src));
            }

            if (src.Length < 0 || src.Length > 65535)
            {
                throw new ArgumentOutOfRangeException(nameof(src), "src.Length must be between 0 and 65535.");
            }

            byte[] dst = new byte[outputLength];
            int srcIdx = 0;
            int dstIdx = 0;
            int lenCompressed = src.Length;
            int lenDecompressed = outputLength;

            while (srcIdx < lenCompressed && dstIdx < lenDecompressed)
            {
                byte b = src[srcIdx];
                srcIdx++;

                if (b <= 0x7E)
                {
                    int n = b + 1;
                    if (n > lenCompressed - srcIdx) n = lenCompressed - srcIdx;
                    if (n > lenDecompressed - dstIdx) n = lenDecompressed - dstIdx;
                    Array.Copy(src, srcIdx, dst, dstIdx, n);
                    srcIdx += n;
                    dstIdx += n;
                }
                else if (b >= 0x81)
                {
                    sbyte sb = unchecked((sbyte)b);
                    if (srcIdx < lenCompressed)
                    {
                        int n = -sb;
                        if (n > lenDecompressed - dstIdx) n = lenDecompressed - dstIdx;
                        byte fillByte = src[srcIdx];
                        for (int i = 0; i < n; i++)
                        {
                            dst[dstIdx + i] = fillByte;
                        }
                        srcIdx++;
                        dstIdx += n;
                    }
                }
            }

            return dst;
        }
    }
}
