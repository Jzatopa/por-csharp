using System;
using System.IO;
using Por.Core;

// Supervisor-authored tests. Run under checked arithmetic to expose implicit narrowing.
public static class DataVerification
{
    static int assertions, vectors;
    static void Check(bool condition, string message) { assertions++; if (!condition) throw new Exception(message); }
    static void Throws<T>(Action action) where T : Exception
    { try { action(); } catch (T) { assertions++; return; } throw new Exception("Missing " + typeof(T).Name); }
    static int BigInt(BinaryReader r)
    { return (r.ReadByte()<<24)|(r.ReadByte()<<16)|(r.ReadByte()<<8)|r.ReadByte(); }
    public static void Main(string[] args)
    {
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("ar-SA");
        StrUtilTests.Main();
        DataLaneTests.Main();
        ArchiveTests.Main();
        using (var r = new BinaryReader(File.OpenRead(args[0])))
        {
            while (r.BaseStream.Position < r.BaseStream.Length)
            {
                int n = BigInt(r), size = BigInt(r);
                byte[] input = r.ReadBytes(n), expected = r.ReadBytes(size), copy = (byte[])input.Clone();
                Check(input.Length == n && expected.Length == size, "truncated fixture");
                byte[] actual = DaxIo.DecompressDax(input, size);
                Check(actual.Length == size, "output length");
                for (int i=0; i<size; i++) Check(expected[i] == actual[i], "DAX output");
                for (int i=0; i<n; i++) Check(copy[i] == input[i], "input mutated");
                vectors++;
            }
        }
        Throws<ArgumentNullException>(() => DaxIo.DecompressDax(null, 0));
        Throws<ArgumentOutOfRangeException>(() => DaxIo.DecompressDax(new byte[0], -1));
        Throws<ArgumentOutOfRangeException>(() => DaxIo.DecompressDax(new byte[0], 65536));
        Throws<ArgumentOutOfRangeException>(() => DaxIo.DecompressDax(new byte[65536], 1));
        Throws<ArgumentException>(() => FileUtil.ReadRecord("missing", new byte[1], 2));
        Throws<ArgumentException>(() => FileUtil.WriteRecord("missing", new byte[1], 2));
        Throws<ArgumentNullException>(() => FileUtil.ReadRecord("missing", null, 2));
        Throws<ArgumentNullException>(() => FileUtil.WriteRecord("missing", null, 2));
        Check(DaxIo.SanitizeString(new string('a', 300), "") == new string('a', 255), "empty-delimiter truncation");
        Check(DaxIo.SanitizeString("abc", new string('a', 300)) == "abc", "delimiter truncation");
        string scratch = Path.Combine(Path.GetTempPath(), "por-data-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(scratch);
        try
        {
            string dir = Path.Combine(scratch, new string('x', 100)); Directory.CreateDirectory(dir);
            string file = Path.Combine(dir, "record.dat"); byte[] b = {0,128,255};
            Check(FileUtil.WriteRecord(file, b, 3), "host path >79 write");
            byte[] dst = new byte[3]; Check(FileUtil.ReadRecord(file, dst, 3), "host path >79 read");
            Check(dst[2] == 255, "binary high byte");
            Check(!FileUtil.ReadRecord(dir, dst, 3), "directory read failure");
            Check(!FileUtil.WriteRecord(dir, b, 3), "directory write failure");
            FileUtil.DeleteFile(file); Check(!File.Exists(file), "delete");
        }
        finally { Directory.Delete(scratch, true); }
        Console.WriteLine("DAX source-reference vectors passed: " + vectors);
        Console.WriteLine("Additional independent assertions passed: " + assertions);
    }
}
