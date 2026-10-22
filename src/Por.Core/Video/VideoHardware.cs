using System;
namespace Por.Core
{
    // Hardware operations, not rendering algorithms. A desktop host or emulator owns this bus.
    public interface IVideoHardware
    {
        byte ReadByte(ushort segment, ushort offset);
        void WriteByte(ushort segment, ushort offset, byte value);
        void WritePort(ushort port, byte value);
        void Interrupt(byte number, Types.TInterruptRegisters registers);
    }
    public static class VideoHardware
    {
        public static IVideoHardware Host;
        private static IVideoHardware Required()
        {
            if (Host == null) throw new NotSupportedException("No video hardware adapter installed.");
            return Host;
        }
        public static byte Read(ushort segment, ushort offset) { return Required().ReadByte(segment, offset); }
        public static void Write(ushort segment, ushort offset, byte value) { Required().WriteByte(segment, offset, value); }
        public static void Port(ushort port, byte value) { Required().WritePort(port, value); }
        public static void Interrupt(byte number, Types.TInterruptRegisters registers) { Required().Interrupt(number, registers); }
        public static void Fill(ushort segment, ushort offset, int count, byte value)
        {
            var host = Required();
            for (int i = 0; i < count; i++) host.WriteByte(segment, unchecked((ushort)(offset + i)), value);
        }
        public static void CopyTo(byte[] source, int sourceOffset, ushort segment, ushort offset, int count)
        {
            var host = Required();
            for (int i = 0; i < count; i++) host.WriteByte(segment, unchecked((ushort)(offset + i)), source[sourceOffset + i]);
        }
        public static void Move(ushort sourceSegment, ushort sourceOffset, ushort destSegment, ushort destOffset, int count)
        {
            // A read immediately followed by its write is essential to EGA write-mode-1 latches.
            var host = Required();
            for (int i = 0; i < count; i++)
            {
                byte value = host.ReadByte(sourceSegment, unchecked((ushort)(sourceOffset + i)));
                host.WriteByte(destSegment, unchecked((ushort)(destOffset + i)), value);
            }
        }
    }
}
