namespace Por.Core
{
    using System;
    using System.IO;
    using System.Security;

    /// <summary>
    /// Faithful port of the Pascal DataArc unit.
    /// Adaptations:
    /// - Strings are treated as byte-valued characters (0..255) to match Pascal's AnsiString behavior for DOS ASCII.
    /// - UpCase is implemented explicitly for DOS ASCII only (A-Z).
    /// - File I/O uses FileUtil.OpenReadOnly(out FileStream, string) as specified.
    /// - TDirEntry is serialized manually to ensure exactly 9 bytes (no struct padding).
    /// - IO exceptions are captured and result in false/-1 returns.
    /// </summary>
    public static partial class DataArc
    {
        // Nested struct for DAX directory entry
        public struct TDirEntry
        {
            public byte id;
            public int offset;
            public ushort rawSize;
            public ushort packedSize;
        }

        // Family definition
        private struct TFamily
        {
            public string prefix;
            public string suffix;
            public string merged;
        }

        // Source family table (1-based index in Pascal, 0-based in C#)
        private static readonly TFamily[] families = new TFamily[]
        {
            new TFamily { prefix = "ECL", suffix = "", merged = "ECL" },
            new TFamily { prefix = "GEO", suffix = "", merged = "GEO" },
            new TFamily { prefix = "WALLDEF", suffix = "", merged = "WALLDEF" },
            new TFamily { prefix = "PIC", suffix = "", merged = "PIC" },
            new TFamily { prefix = "SPRIT", suffix = "", merged = "SPRIT" },
            new TFamily { prefix = "CPIC", suffix = "", merged = "CPIC" },
            new TFamily { prefix = "FINAL", suffix = "", merged = "FINAL" },
            new TFamily { prefix = "8X8D", suffix = "", merged = "8X8D" },
            new TFamily { prefix = "ITEM", suffix = "", merged = "ITEM" },
            new TFamily { prefix = "MON", suffix = "CHA", merged = "MONCHA" },
            new TFamily { prefix = "MON", suffix = "ITM", merged = "MONITM" },
            new TFamily { prefix = "MON", suffix = "SPC", merged = "MONSPC" },
            new TFamily { prefix = "HEAD", suffix = "", merged = "HEAD" },
            new TFamily { prefix = "BODY", suffix = "", merged = "BODY" }
        };

        /// <summary>
        /// The per-disk archive name of a data family's disk, without extension.
        /// </summary>
        public static string SourceName(byte family, byte disk)
        {
            if (family >= 1 && family <= C_Data.FAMILIES && disk >= 1 && disk <= 8)
            {
                TFamily f = families[family - 1];
                char digit = (char)('0' + disk);
                return f.prefix + digit + f.suffix;
            }
            return "";
        }

        /// <summary>
        /// The merged archive name of a data family, without extension.
        /// </summary>
        public static string MergedName(byte family)
        {
            if (family >= 1 && family <= C_Data.FAMILIES)
            {
                return families[family - 1].merged;
            }
            return "";
        }

        /// <summary>
        /// The family whose per-disk archive fileName (any case, no extension) names, with that archive's disk, 1..8, in disk;
        /// 0 when it names none.
        /// </summary>
        public static byte MatchFamily(string fileName, ref byte disk)
        {
            byte result = 0;
            if (fileName == null) return result;

            // Convert to uppercase (DOS ASCII only)
            char[] chars = new char[fileName.Length];
            for (int i = 0; i < fileName.Length; i++)
            {
                char c = fileName[i];
                if (c >= 'a' && c <= 'z')
                {
                    c = (char)(c - 'a' + 'A');
                }
                chars[i] = c;
            }
            string u = new string(chars);

            for (int f = 1; f <= C_Data.FAMILIES; f++)
            {
                TFamily fam = families[f - 1];
                int digitPos = fam.prefix.Length; // 0-based index of the digit
                int expectedLen = fam.prefix.Length + 1 + fam.suffix.Length;

                if (u.Length == expectedLen)
                {
                    // Check prefix
                    if (u.Substring(0, fam.prefix.Length) == fam.prefix)
                    {
                        char digitChar = u[digitPos];
                        if (digitChar >= '1' && digitChar <= '8')
                        {
                            // Check suffix
                            if (fam.suffix.Length > 0)
                            {
                                if (u.Substring(digitPos + 1, fam.suffix.Length) == fam.suffix)
                                {
                                    disk = (byte)(digitChar - '0');
                                    result = (byte)f;
                                    return result;
                                }
                            }
                            else
                            {
                                // No suffix, already checked length
                                disk = (byte)(digitChar - '0');
                                result = (byte)f;
                                return result;
                            }
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// True when the file at path is a whole merged archive: its directory is non-empty, its last entry is the version block,
        /// and the file ends exactly where that block does.
        /// </summary>
        public static bool ArchiveIntact(string path)
        {
            bool ok = false;
            FileStream fs = null;
            try
            {
                if (FileUtil.OpenReadOnly(out fs, path))
                {
                    long size = fs.Length;
                    // Read dirSize (2 bytes)
                    byte[] dirSizeBytes = new byte[2];
                    if (!ReadExact(fs, dirSizeBytes, 0, 2))
                    {
                        return false;
                    }
                    ushort dirSize = (ushort)(dirSizeBytes[0] | (dirSizeBytes[1] << 8));
                    int entrySize = 9; // 1 + 4 + 2 + 2
                    if (dirSize > 0 && dirSize % entrySize == 0 && (2 + dirSize <= size))
                    {
                        // Seek to last entry
                        long lastEntryOffset = 2 + dirSize - entrySize;
                        fs.Seek(lastEntryOffset, SeekOrigin.Begin);
                        byte[] entryBytes = new byte[entrySize];
                        if (!ReadExact(fs, entryBytes, 0, entrySize))
                        {
                            return false;
                        }
                        TDirEntry e = DeserializeEntry(entryBytes);
                        ok = (e.id == C_Data.VERSION_BLOCK) && (e.offset >= 0) &&
                             (unchecked(2 + dirSize + e.offset + e.packedSize) == size);
                    }
                }
            }
            catch (IOException)
            {
                ok = false;
            }
            catch (UnauthorizedAccessException)
            {
                ok = false;
            }
            catch (SecurityException)
            {
                ok = false;
            }
            catch (ArgumentException)
            {
                ok = false;
            }
            catch (NotSupportedException)
            {
                ok = false;
            }
            finally
            {
                if (fs != null)
                {
                    try { fs.Close(); } catch (IOException) { } catch (UnauthorizedAccessException) { } catch (SecurityException) { } catch (ArgumentException) { } catch (NotSupportedException) { }
                }
            }
            return ok;
        }

        /// <summary>
        /// True when the DAX archive at path lists block id. False, with no prompt, when the file cannot be read.
        /// </summary>
        public static bool HasBlock(string path, byte id)
        {
            bool found = false;
            FileStream fs = null;
            try
            {
                if (FileUtil.OpenReadOnly(out fs, path))
                {
                    byte[] dirSizeBytes = new byte[2];
                    if (!ReadExact(fs, dirSizeBytes, 0, 2))
                    {
                        return false;
                    }
                    ushort dirSize = (ushort)(dirSizeBytes[0] | (dirSizeBytes[1] << 8));
                    int entrySize = 9;
                    int entryCount = dirSize / entrySize;
                    for (int i = 0; i < entryCount && !found; i++)
                    {
                        byte[] entryBytes = new byte[entrySize];
                        if (!ReadExact(fs, entryBytes, 0, entrySize))
                        {
                            break;
                        }
                        TDirEntry e = DeserializeEntry(entryBytes);
                        if (e.id == id)
                        {
                            found = true;
                        }
                    }
                }
            }
            catch (IOException)
            {
                found = false;
            }
            catch (UnauthorizedAccessException)
            {
                found = false;
            }
            catch (SecurityException)
            {
                found = false;
            }
            catch (ArgumentException)
            {
                found = false;
            }
            catch (NotSupportedException)
            {
                found = false;
            }
            finally
            {
                if (fs != null)
                {
                    try { fs.Close(); } catch (IOException) { } catch (UnauthorizedAccessException) { } catch (SecurityException) { } catch (ArgumentException) { } catch (NotSupportedException) { }
                }
            }
            return found;
        }

        /// <summary>
        /// The size in bytes of the file at path; -1 when it cannot be opened.
        /// </summary>
        public static int FileLength(string path)
        {
            int size = -1;
            FileStream fs = null;
            try
            {
                if (FileUtil.OpenReadOnly(out fs, path))
                {
                    size = unchecked((int)fs.Length);
                }
            }
            catch (IOException)
            {
                size = -1;
            }
            catch (UnauthorizedAccessException)
            {
                size = -1;
            }
            catch (SecurityException)
            {
                size = -1;
            }
            catch (ArgumentException)
            {
                size = -1;
            }
            catch (NotSupportedException)
            {
                size = -1;
            }
            finally
            {
                if (fs != null)
                {
                    try { fs.Close(); } catch (IOException) { size = -1; } catch (UnauthorizedAccessException) { size = -1; } catch (SecurityException) { size = -1; } catch (ArgumentException) { size = -1; } catch (NotSupportedException) { size = -1; }
                }
            }
            return size;
        }

        // Helper to read exactly count bytes from stream into buffer at offset
        private static bool ReadExact(Stream stream, byte[] buffer, int offset, int count)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int bytesRead = stream.Read(buffer, offset + totalRead, count - totalRead);
                if (bytesRead == 0)
                {
                    return false; // EOF
                }
                totalRead += bytesRead;
            }
            return true;
        }

        // Helper to deserialize 9 bytes to TDirEntry
        private static TDirEntry DeserializeEntry(byte[] buf)
        {
            TDirEntry e = new TDirEntry();
            e.id = buf[0];
            e.offset = buf[1] | (buf[2] << 8) | (buf[3] << 16) | (buf[4] << 24);
            e.rawSize = (ushort)(buf[5] | (buf[6] << 8));
            e.packedSize = (ushort)(buf[7] | (buf[8] << 8));
            return e;
        }
    }
}
