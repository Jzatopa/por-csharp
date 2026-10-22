namespace Por.Core
{
    using System;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Whole-file helpers for the game's own small files (a character's usual spells and statistics, a save slot's
    /// description, the vault): erase one, open one read-only, read or write one record as the whole file.
    /// </summary>
    /// <remarks>
    /// Adaptations from Pascal:
    /// 1. Strings are treated as byte-valued characters (0..255). C# strings are UTF-16, so we validate that all characters
    ///    are in the 0..255 range to ensure they can be represented as single bytes (DOS ASCII/Latin-1 compatible).
    /// 2. Host path lengths >79 are accepted as a deliberate adaptation; byte-valued characters (0..255) are retained.
    /// 3. I/O errors are caught and returned as false (or ignored for DeleteFile), never thrown, except for invalid arguments.
    /// 4. OpenReadOnly uses FileShare.ReadWrite to allow other processes to read/write while we have it open read-only.
    /// 5. ReadRecord requires the file size to be exactly 'size' bytes and reads exactly 'size' bytes.
    /// 6. WriteRecord writes exactly 'size' bytes, replacing the file.
    /// 7. DOS basename case host adaptation: On case-sensitive filesystems (like Linux), the adapter preserves DOS case-insensitive BASENAME lookup for mixed-case source files.
    ///    we resolve the exact existing path by comparing basenames case-insensitively within the configured host directory.
    ///    This is NOT a game feature; it's a host adapter for DOS basename case sensitivity.
    /// </remarks>
    public static partial class FileUtil
    {
        /// <summary>
        /// Validates that the path string contains only characters in the 0..255 range.
        /// </summary>
        /// <param name="path">The path to validate.</param>
        /// <returns>True if valid, false otherwise.</returns>
        private static bool IsValidPath(string path)
        {
            if (path == null)
                return false;

            foreach (char c in path)
            {
                if (c > 255)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Determines if the exception is a file-related error that should be handled gracefully.
        /// </summary>
        /// <param name="ex">The exception to check.</param>
        /// <returns>True if the exception is a file error, false otherwise.</returns>
        private static bool IsFileError(Exception ex)
        {
            return ex is IOException
                || ex is UnauthorizedAccessException
                || ex is System.Security.SecurityException
                || ex is ArgumentException
                || ex is NotSupportedException;
        }

        /// <summary>
        /// Resolves the exact existing path for a file within its configured host directory by comparing basenames
        /// case-insensitively. This is a DOS basename case host adaptation, not a game feature.
        /// 
        /// Strategy:
        /// 1. If the exact path exists, return it.
        /// 2. Otherwise, enumerate files in the directory and compare basenames using OrdinalIgnoreCase.
        ///    - If exactly one match is found, return that path.
        ///    - If multiple matches are found, throw IOException (caught as file error by callers).
        ///    - If no match is found, return the original path (for normal failure).
        /// 
        /// This method does NOT normalize or truncate the configured host root, and does NOT rename files.
        /// </summary>
        /// <param name="originalPath">The original requested path.</param>
        /// <returns>The resolved existing path, or the original path if no match found.</returns>
        /// <exception cref="IOException">Thrown if multiple case collisions are found in the directory.</exception>
        private static string ResolveExistingPath(string originalPath)
        {
            if (string.IsNullOrEmpty(originalPath))
                return originalPath;

            // First, check if the exact path exists
            if (File.Exists(originalPath))
                return originalPath;

            // Get the directory and basename
            string directory;
            string basename;
            int lastSep = originalPath.LastIndexOfAny(new[] { '/', '\\' });
            if (lastSep <= 0)
            {
                // No directory separator, or separator at position 0 (e.g., "C:\file" or "/file")
                // For "C:\file", lastSep is 1, so we need to handle this
                if (lastSep == 1 && originalPath.Length > 2 && originalPath[1] == ':')
                {
                    directory = originalPath.Substring(0, 2);
                    basename = originalPath.Substring(2);
                }
                else if (lastSep == 0)
                {
                    directory = originalPath.Substring(0, 1);
                    basename = originalPath.Substring(1);
                }
                else
                {
                    // No directory, just a filename
                    directory = ".";
                    basename = originalPath;
                }
            }
            else
            {
                directory = originalPath.Substring(0, lastSep);
                basename = originalPath.Substring(lastSep + 1);
            }

            // If basename is empty, return original
            if (string.IsNullOrEmpty(basename))
                return originalPath;

            // Check if directory exists
            if (!Directory.Exists(directory))
                return originalPath;

            // Enumerate files in the directory and compare basenames case-insensitively
            string[] files;
            try
            {
                files = Directory.GetFiles(directory);
            }
            catch (Exception ex) when (IsFileError(ex))
            {
                // If we can't enumerate, return original path for normal failure
                return originalPath;
            }

            string match = null;
            int matchCount = 0;

            foreach (string file in files)
            {
                string fileBasename = Path.GetFileName(file);
                if (string.Equals(fileBasename, basename, StringComparison.OrdinalIgnoreCase))
                {
                    match = file;
                    matchCount++;
                    if (matchCount > 1)
                    {
                        // Multiple case collisions - throw IOException
                        throw new IOException($"Multiple case collisions found for basename '{basename}' in directory '{directory}'");
                    }
                }
            }

            if (matchCount == 1)
                return match;

            // No match found, return original path for normal failure
            return originalPath;
        }

        /// <summary>
        /// Erase file name; a missing file, or any other error, is ignored.
        /// </summary>
        /// <param name="name">The file name to delete.</param>
        public static void DeleteFile(string name)
        {
            if (!IsValidPath(name))
                return;

            try
            {
                // Resolve existing path for DOS basename case host adaptation
                string resolvedPath = ResolveExistingPath(name);
                File.Delete(resolvedPath);
            }
            catch (Exception ex) when (IsFileError(ex))
            {
                // Ignore file errors, as per Pascal behavior
            }
        }

        /// <summary>
        /// Open name for reading only, whatever FileMode is (a read-only file opens too), with record size 1.
        /// True when it opened; the caller closes it.
        /// </summary>
        /// <param name="f">The file stream to open.</param>
        /// <param name="name">The file name to open.</param>
        /// <returns>True if the file was opened successfully.</returns>
        public static bool OpenReadOnly(out FileStream f, string name)
        {
            f = null;

            if (!IsValidPath(name))
                return false;

            try
            {
                // Resolve existing path for DOS basename case host adaptation
                string resolvedPath = ResolveExistingPath(name);
                f = new FileStream(resolvedPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                return true;
            }
            catch (Exception ex) when (IsFileError(ex))
            {
                f = null;
                return false;
            }
        }

        /// <summary>
        /// Read file name into rec: True only when it is exactly size bytes and they read.
        /// The caller checks what they hold.
        /// </summary>
        /// <param name="name">The file name to read.</param>
        /// <param name="rec">The buffer to read into.</param>
        /// <param name="size">The expected size of the record in bytes.</param>
        /// <returns>True if the file was read successfully and contained exactly 'size' bytes.</returns>
        public static bool ReadRecord(string name, byte[] rec, ushort size)
        {
            // Caller-buffer guards OUTSIDE IO try
            if (rec == null)
                throw new ArgumentNullException(nameof(rec));

            if (rec.Length < size)
                throw new ArgumentException("Buffer is smaller than the specified size", nameof(rec));

            if (!IsValidPath(name))
                return false;

            FileStream f = null;
            if (!OpenReadOnly(out f, name))
                return false;

            try
            {
                // Check file size
                if (f.Length != size)
                    return false;

                // Read exactly 'size' bytes
                int totalRead = 0;
                while (totalRead < size)
                {
                    int bytesRead = f.Read(rec, totalRead, size - totalRead);
                    if (bytesRead == 0)
                        return false; // EOF before reading all bytes
                    totalRead += bytesRead;
                }

                return true;
            }
            catch (Exception ex) when (IsFileError(ex))
            {
                return false;
            }
            finally
            {
                try
                {
                    f.Close();
                }
                catch (Exception ex) when (IsFileError(ex))
                {
                    // Ignore close errors
                }
            }
        }

        /// <summary>
        /// Write rec's size bytes as file name, replacing it: True when they were written.
        /// </summary>
        /// <param name="name">The file name to write.</param>
        /// <param name="rec">The buffer to write from.</param>
        /// <param name="size">The number of bytes to write.</param>
        /// <returns>True if the file was written successfully.</returns>
        public static bool WriteRecord(string name, byte[] rec, ushort size)
        {
            // Caller-buffer guards OUTSIDE IO try
            if (rec == null)
                throw new ArgumentNullException(nameof(rec));

            if (rec.Length < size)
                throw new ArgumentException("Buffer is smaller than the specified size", nameof(rec));

            if (!IsValidPath(name))
                return false;

            try
            {
                // For WriteRecord:
                // - If file exists (overwrite), use resolved existing path
                // - If file is missing (new file), use original requested case
                string pathToUse;
                if (File.Exists(name))
                {
                    // File exists, resolve for overwrite
                    pathToUse = ResolveExistingPath(name);
                }
                else
                {
                    // File missing, use original requested case for new file
                    pathToUse = name;
                }

                using (FileStream f = new FileStream(pathToUse, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    f.Write(rec, 0, size);
                    f.Flush();
                    return true;
                }
            }
            catch (Exception ex) when (IsFileError(ex))
            {
                return false;
            }
        }
    }
}
