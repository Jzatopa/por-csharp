namespace Por.Core
{
    /// <summary>
    /// Number and padding helpers for text the game shows.
    /// Adaptation notes:
    /// - Pascal 'String' (short string) is mapped to C# 'string' with byte-valued characters (0..255).
    /// - Input strings are truncated to 255 characters to match Pascal short string limits.
    /// - 'Str' function is emulated using invariant decimal formatting.
    /// - 'Word' is mapped to 'ushort', 'LongInt' to 'int', 'Byte' to 'byte'.
    /// - Arithmetic operations use unchecked context to ensure explicit wrapping behavior where applicable.
    /// </summary>
    public static partial class StrUtil
    {
        /// <summary>
        /// Converts an integer to its decimal string representation.
        /// Equivalent to Pascal's Str(n, s) for LongInt.
        /// </summary>
        /// <param name="n">The integer value to convert.</param>
        /// <returns>A string containing the decimal representation of n.</returns>
        public static string NumStr(int n)
        {
            return n.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts a word (ushort) to a two-digit string, padding with a leading zero if necessary.
        /// Only the last two digits are considered (n mod 100).
        /// </summary>
        /// <param name="n">The unsigned short value to convert.</param>
        /// <returns>A two-character string representing the last two digits of n.</returns>
        public static string ZeroPad2(ushort n)
        {
            unchecked
            {
                int val = (int)(n % 100);
                string s = val.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (s.Length < 2)
                {
                    s = "0" + s;
                }
                return s;
            }
        }

        /// <summary>
        /// Truncates or pads a string with spaces on the right to the specified width.
        /// Input string is truncated to 255 characters first.
        /// </summary>
        /// <param name="s">The input string.</param>
        /// <param name="width">The desired width (1-255).</param>
        /// <returns>The truncated or padded string.</returns>
        public static string PadRight(string s, byte width)
        {
            if (s == null) s = "";
            
            // Truncate to 255 chars to match Pascal short string behavior
            if (s.Length > 255)
            {
                s = s.Substring(0, 255);
            }

            // Truncate to width
            if (s.Length > width)
            {
                s = s.Substring(0, width);
            }

            // Pad with spaces on the right
            while (s.Length < width)
            {
                s = s + " ";
            }

            return s;
        }

        /// <summary>
        /// Truncates or pads a string with spaces on the left to the specified width.
        /// Input string is truncated to 255 characters first.
        /// </summary>
        /// <param name="s">The input string.</param>
        /// <param name="width">The desired width (1-255).</param>
        /// <returns>The truncated or padded string.</returns>
        public static string PadLeft(string s, byte width)
        {
            if (s == null) s = "";
            
            // Truncate to 255 chars to match Pascal short string behavior
            if (s.Length > 255)
            {
                s = s.Substring(0, 255);
            }

            // Truncate to width
            if (s.Length > width)
            {
                s = s.Substring(0, width);
            }

            // Pad with spaces on the left
            while (s.Length < width)
            {
                s = " " + s;
            }

            return s;
        }
    }
}
