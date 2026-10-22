using System;
using System.IO;
using System.Security;

namespace Por.Core
{
    public interface IDataBuildPlatform
    {
        long MaxAvailable { get; }
        int DiskFree(byte drive);
        void ClearBottomLine();
        void DisplayString(byte col, byte row, byte color, string text);
        void Open(bool[] stale, bool writable);
        void Advance(ushort count);
        void Reading(byte fam, byte disk);
        void Writing(int bytes);
        void Fixing(byte fam, byte disk);
        void Checking();
        void StartFamily(byte fam);
        void EndFamily(byte fam, bool success);
        void Finish(bool failed);
    }

    public static partial class DataBld
    {
        public const int MAX_BLOCKS = 512;
        public const int MAX_REMAPS = C_Data.FAMILY_REMAPS;
        public const int CHUNK_SIZE = 4096;
        public const int SOURCE_DISKS = 8;
        public const int RUN_SIZE = 127;
        public const int MAX_ENCODED = 64000;
        public const int MAX_LOADABLE = 35718;
        public const int ROOM_MARGIN = 65536;
        public const int SOURCES_SIZE = SOURCE_DISKS * C_Data.SOURCE_RECORD;
        public const int VERSION_RAW = 5;
        public const int VERSION_PACKED = 6;
        public const string LOG_NAME = "DATABLD.LOG";

        public struct TBlock
        {
            public byte id;
            public ushort rawSize;
            public ushort packedSize;
            public byte disk;
            public int start;
            public byte[] mem;
        }

        public struct TCrc
        {
            private int _value;
            public int value
            {
                get { return _value; }
                set { _value = value; }
            }
            public ushort lo
            {
                get { return unchecked((ushort)(_value & 0xFFFF)); }
                set { _value = (_value & unchecked((int)0xFFFF0000)) | unchecked((int)(value & 0xFFFF)); }
            }
            public ushort hi
            {
                get { return unchecked((ushort)((_value >> 16) & 0xFFFF)); }
                set { _value = (_value & 0x0000FFFF) | (unchecked((int)(value & 0xFFFF)) << 16); }
            }
        }

        public class TCrcTable
        {
            public ushort[] lo = new ushort[256];
            public ushort[] hi = new ushort[256];
        }

        public static IDataBuildPlatform Platform { get; set; }

        private static void RequirePlatform()
        {
            if (Platform == null)
                throw new InvalidOperationException("Platform not set");
        }

        private static FileStream logFile;
        private static bool logOpen;
        private static TCrcTable crcTable;
        private static byte[] bufA;
        private static byte[] bufB;
        private static TBlock[] sources;
        private static ushort sourceCount;
        private static TBlock[] blocks;
        private static ushort blockCount;
        private static byte[] sourceList;
        private static ushort sourceListLen;
        private static byte remapCount;
        private static byte[] remapDisk;
        private static byte[] remapFrom;
        private static byte[] remapTo;
        private static ushort[] remapBlock;
        private static string archiveName;
        private static string failNote;
        private static short lastError;
        private static string writeNote;
        private static string memNote;
        private static byte builtCount;
        private static byte keptCount;
        private static byte failedCount;
        private static short n;
        private static bool ioSuccess = true;

        internal static void SetIoError(Exception ex)
        {
            ioSuccess = false;
            if (ex is IOException) lastError = 1;
            else if (ex is UnauthorizedAccessException) lastError = 5;
            else if (ex is SecurityException) lastError = 5;
            else if (ex is ArgumentException) lastError = 18;
            else if (ex is NotSupportedException) lastError = 18;
            else lastError = 1;
        }

        internal static bool IoOk()
        {
            bool ok = ioSuccess;
            ioSuccess = true;
            return ok;
        }

        internal static bool OpenInput(string path, out FileStream stream)
        {
            stream = null;
            bool ok = FileUtil.OpenReadOnly(out stream, path);
            if (ok)
            {
                ioSuccess = true;
                return true;
            }
            else
            {
                ioSuccess = false;
                lastError = 2;
                return false;
            }
        }

        internal static bool CreateOutput(string path, out FileStream stream)
        {
            stream = null;
            try
            {
                stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
                ioSuccess = true;
                return true;
            }
            catch (Exception ex)
            {
                if (ex is IOException || ex is UnauthorizedAccessException || ex is SecurityException || ex is ArgumentException || ex is NotSupportedException)
                {
                    SetIoError(ex);
                }
                else
                {
                    throw;
                }
            }
            ioSuccess = false;
            return false;
        }

        internal static void CloseFile(FileStream stream)
        {
            if (stream != null)
            {
                try
                {
                    stream.Close();
                    ioSuccess = true;
                }
                catch (Exception ex)
                {
                    if (ex is IOException || ex is UnauthorizedAccessException || ex is SecurityException || ex is ArgumentException || ex is NotSupportedException)
                    {
                        SetIoError(ex);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        internal static bool WriteBytes(FileStream stream, byte[] data, int count)
        {
            try
            {
                stream.Write(data, 0, count);
                ioSuccess = true;
                return true;
            }
            catch (Exception ex)
            {
                if (ex is IOException || ex is UnauthorizedAccessException || ex is SecurityException || ex is ArgumentException || ex is NotSupportedException)
                {
                    SetIoError(ex);
                }
                else
                {
                    throw;
                }
            }
            ioSuccess = false;
            return false;
        }

        internal static bool ReadExact(FileStream stream, byte[] data, int count)
        {
            try
            {
                int read = 0;
                while (read < count)
                {
                    int r = stream.Read(data, read, count - read);
                    if (r == 0) break;
                    read += r;
                }
                if (read == count)
                {
                    ioSuccess = true;
                    return true;
                }
            }
            catch (Exception ex)
            {
                if (ex is IOException || ex is UnauthorizedAccessException || ex is SecurityException || ex is ArgumentException || ex is NotSupportedException)
                {
                    SetIoError(ex);
                }
                else
                {
                    throw;
                }
            }
            ioSuccess = false;
            return false;
        }

        internal static void WriteLE16(byte[] buf, int offset, ushort value)
        {
            buf[offset] = unchecked((byte)(value & 0xFF));
            buf[offset + 1] = unchecked((byte)((value >> 8) & 0xFF));
        }

        internal static void WriteLE32(byte[] buf, int offset, int value)
        {
            buf[offset] = unchecked((byte)(value & 0xFF));
            buf[offset + 1] = unchecked((byte)((value >> 8) & 0xFF));
            buf[offset + 2] = unchecked((byte)((value >> 16) & 0xFF));
            buf[offset + 3] = unchecked((byte)((value >> 24) & 0xFF));
        }

        internal static ushort ReadLE16(byte[] buf, int offset)
        {
            return unchecked((ushort)(buf[offset] | (buf[offset + 1] << 8)));
        }

        internal static int ReadLE32(byte[] buf, int offset)
        {
            return unchecked((int)(buf[offset] | (buf[offset + 1] << 8) | (buf[offset + 2] << 16) | (buf[offset + 3] << 24)));
        }

        internal static void LogLine(string s)
        {
            if (logOpen)
            {
                try
                {
                    byte[] bytes = new byte[s.Length + 2];
                    for (int i = 0; i < s.Length; i++)
                        bytes[i] = unchecked((byte)s[i]);
                    bytes[s.Length] = 13;
                    bytes[s.Length + 1] = 10;
                    logFile.Write(bytes, 0, bytes.Length);
                    logFile.Flush();
                    ioSuccess = true;
                }
                catch (Exception ex)
                {
                    if (ex is IOException || ex is UnauthorizedAccessException || ex is SecurityException || ex is ArgumentException || ex is NotSupportedException)
                    {
                        SetIoError(ex);
                        logOpen = false;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        internal static void Progress(string s)
        {
            RequirePlatform();
            Platform.ClearBottomLine();
            string text = s.Length > 40 ? s.Substring(0, 40) : s;
            Platform.DisplayString(0, 24, 10, text);
            LogLine(s);
        }

        internal static void Fail(string why)
        {
            if (failNote == null || failNote.Length == 0)
                failNote = why.Length > 80 ? why.Substring(0, 80) : why;
        }

        internal static void FailWriting(string why)
        {
            Fail(why);
            if (writeNote == null || writeNote.Length == 0)
                writeNote = why.Length > 80 ? why.Substring(0, 80) : why;
        }

        internal static bool Reserve(int size)
        {
            RequirePlatform();
            bool ok = Platform.MaxAvailable >= unchecked(size + 8);
            if (!ok)
                Fail("not enough memory");
            return ok;
        }

        internal static void InitCrcTable()
        {
            for (int i = 0; i < 256; i++)
            {
                TCrc c = new TCrc();
                c.value = i;
                for (int k = 0; k < 8; k++)
                {
                    if ((c.value & 1) != 0)
                        c.value = unchecked((int)((uint)c.value >> 1)) ^ unchecked((int)0xEDB88320);
                    else
                        c.value = unchecked((int)((uint)c.value >> 1));
                }
                crcTable.lo[i] = c.lo;
                crcTable.hi[i] = c.hi;
            }
        }

        internal static void AddCrc(ref TCrc crc, byte[] buf, ushort count)
        {
            ushort low16 = crc.lo;
            ushort high16 = crc.hi;
            for (int i = 0; i < count; i++)
            {
                byte idx = unchecked((byte)(low16 ^ buf[i]));
                low16 = unchecked((ushort)(((low16 >> 8) | (high16 << 8)) ^ crcTable.lo[idx]));
                high16 = unchecked((ushort)((high16 >> 8) ^ crcTable.hi[idx]));
            }
            crc.lo = low16;
            crc.hi = high16;
        }

        internal static bool ReadAt(FileStream f, int pos, byte[] buf, ushort count)
        {
            try
            {
                f.Seek(pos, SeekOrigin.Begin);
                return ReadExact(f, buf, count);
            }
            catch (Exception ex)
            {
                if (ex is IOException || ex is UnauthorizedAccessException || ex is SecurityException || ex is ArgumentException || ex is NotSupportedException)
                {
                    SetIoError(ex);
                }
                else
                {
                    throw;
                }
            }
            ioSuccess = false;
            return false;
        }
    }
}
