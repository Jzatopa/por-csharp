using System;

namespace Por.Core
{
    public static partial class Types
    {
        public class WordView
        {
            private readonly byte[] _backing;
            private readonly int _offset;
            private readonly int _firstIndex;
            private readonly int _count;

            public WordView(byte[] backing, int offset, int firstIndex, int count)
            {
                if (backing == null) throw new ArgumentNullException(nameof(backing));
                if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
                if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
                if (offset > backing.Length) throw new ArgumentOutOfRangeException(nameof(offset));
                if (count > (backing.Length - offset) / 2) throw new ArgumentOutOfRangeException(nameof(count));
                _backing = backing;
                _offset = offset;
                _firstIndex = firstIndex;
                _count = count;
            }

            public ushort this[int index]
            {
                get
                {
                    if (index < _firstIndex || index >= _firstIndex + _count)
                        throw new ArgumentOutOfRangeException(nameof(index));
                    int byteOffset = _offset + (index - _firstIndex) * 2;
                    return (ushort)(_backing[byteOffset] | (_backing[byteOffset + 1] << 8));
                }
                set
                {
                    if (index < _firstIndex || index >= _firstIndex + _count)
                        throw new ArgumentOutOfRangeException(nameof(index));
                    int byteOffset = _offset + (index - _firstIndex) * 2;
                    _backing[byteOffset] = (byte)(value & 0xFF);
                    _backing[byteOffset + 1] = (byte)((value >> 8) & 0xFF);
                }
            }
        }

        public class TGbvmWord
        {
            private readonly byte[] _backing;
            private readonly int _offset;

            public TGbvmWord()
            {
                _backing = new byte[2];
                _offset = 0;
            }

            public TGbvmWord(byte[] backing, int offset)
            {
                if (backing == null) throw new ArgumentNullException(nameof(backing));
                if (offset < 0 || offset > backing.Length - 2) throw new ArgumentOutOfRangeException(nameof(offset));
                _backing = backing;
                _offset = offset;
            }

            public ushort value
            {
                get { return (ushort)(_backing[_offset] | (_backing[_offset + 1] << 8)); }
                set
                {
                    _backing[_offset] = (byte)(value & 0xFF);
                    _backing[_offset + 1] = (byte)((value >> 8) & 0xFF);
                }
            }

            public byte lo
            {
                get { return _backing[_offset]; }
                set { _backing[_offset] = value; }
            }

            public byte hi
            {
                get { return _backing[_offset + 1]; }
                set { _backing[_offset + 1] = value; }
            }
        }

        public class TGbvmClock
        {
            private readonly WordView _values;

            public TGbvmClock() : this(new byte[14], 0) { }

            public TGbvmClock(byte[] backing, int offset)
            {
                _values = new WordView(backing, offset, 0, 7);
            }

            public WordView values => _values;

            public ushort segment { get => _values[0]; set => _values[0] = value; }
            public ushort minute { get => _values[1]; set => _values[1] = value; }
            public ushort turn { get => _values[2]; set => _values[2] = value; }
            public ushort hour { get => _values[3]; set => _values[3] = value; }
            public ushort day { get => _values[4]; set => _values[4] = value; }
            public ushort month { get => _values[5]; set => _values[5] = value; }
            public ushort year { get => _values[6]; set => _values[6] = value; }
        }

        public class TGbvmSavedWalls
        {
            private readonly WordView _wallSetIds;
            private readonly WordView _symbolSlots;
            private readonly WordView _menuPrefix;
            private readonly WordView _menuAnimation;

            public TGbvmSavedWalls() : this(new byte[12], 0) { }

            public TGbvmSavedWalls(byte[] backing, int offset)
            {
                _wallSetIds = new WordView(backing, offset, 1, 3);
                _symbolSlots = new WordView(backing, offset + 6, 1, 3);
                _menuPrefix = new WordView(backing, offset, 1, 5);
                _menuAnimation = new WordView(backing, offset + 10, 1, 1);
            }

            public WordView wallSetIds => _wallSetIds;
            public WordView symbolSlots => _symbolSlots;
            public WordView menuPrefix => _menuPrefix;
            public ushort menuAnimation { get => _menuAnimation[1]; set => _menuAnimation[1] = value; }
        }

        public class TGbvmZone0State
        {
            private readonly byte[] _bytes;

            public TGbvmZone0State() : this(new byte[2048]) { }

            public TGbvmZone0State(byte[] bytes)
            {
                if (bytes == null) throw new ArgumentNullException(nameof(bytes));
                if (bytes.Length != 2048) throw new ArgumentException("Zone0 must be exactly 2048 bytes", nameof(bytes));
                _bytes = bytes;
            }

            public byte[] bytes => _bytes;

            public WordView words => new WordView(_bytes, 0, 0x4900, 1024);

            public WordView scriptWords4900 => new WordView(_bytes, 0, 0x4900, 0x49C2 - 0x4900 + 1);
            public TGbvmWord partyX => new TGbvmWord(_bytes, (0x49C3 - 0x4900) * 2);
            public TGbvmWord partyY => new TGbvmWord(_bytes, (0x49C4 - 0x4900) * 2);
            public TGbvmWord mapId => new TGbvmWord(_bytes, (0x49C5 - 0x4900) * 2);
            public TGbvmClock clock => new TGbvmClock(_bytes, (0x49C6 - 0x4900) * 2);
            public WordView scriptWords49CD => new WordView(_bytes, (0x49CD - 0x4900) * 2, 0x49CD, 0x49E4 - 0x49CD + 1);
            public ushort antimagic { get => words[0x49E5]; set => words[0x49E5] = value; }
            public ushort view3dFlag { get => words[0x49E6]; set => words[0x49E6] = value; }
            public ushort mapGraphics1 { get => words[0x49E7]; set => words[0x49E7] = value; }
            public ushort mapGraphics2 { get => words[0x49E8]; set => words[0x49E8] = value; }
            public WordView scriptWords49E9 => new WordView(_bytes, (0x49E9 - 0x4900) * 2, 0x49E9, 0x49EF - 0x49E9 + 1);
            public ushort previousX { get => words[0x49F0]; set => words[0x49F0] = value; }
            public ushort previousY { get => words[0x49F1]; set => words[0x49F1] = value; }
            public ushort previousScriptId { get => words[0x49F2]; set => words[0x49F2] = value; }
            public WordView scriptWords49F3 => new WordView(_bytes, (0x49F3 - 0x4900) * 2, 0x49F3, 0x49FA - 0x49F3 + 1);
            public ushort noAutomap { get => words[0x49FB]; set => words[0x49FB] = value; }
            public TGbvmWord gameSpeed => new TGbvmWord(_bytes, (0x49FC - 0x4900) * 2);
            public ushort skyCeilingColor { get => words[0x49FD]; set => words[0x49FD] = value; }
            public ushort alternateSkyColor { get => words[0x49FE]; set => words[0x49FE] = value; }
            public ushort settings { get => words[0x49FF]; set => words[0x49FF] = value; }
            public WordView entryWords => new WordView(_bytes, (0x4A00 - 0x4900) * 2, 0x4A00, 0x4A1F - 0x4A00 + 1);
            public WordView scriptWords4A20 => new WordView(_bytes, (0x4A20 - 0x4900) * 2, 0x4A20, 0x4A7B - 0x4A20 + 1);
            public ushort nomadCampFlags { get => words[0x4A7C]; set => words[0x4A7C] = value; }
            public WordView scriptWords4A7D => new WordView(_bytes, (0x4A7D - 0x4900) * 2, 0x4A7D, 0x4A8B - 0x4A7D + 1);
            public ushort buccaneersBaseGate { get => words[0x4A8C]; set => words[0x4A8C] = value; }
            public WordView scriptWords4A8D => new WordView(_bytes, (0x4A8D - 0x4900) * 2, 0x4A8D, 0x4A9E - 0x4A8D + 1);
            public ushort nomadCampGate { get => words[0x4A9F]; set => words[0x4A9F] = value; }
            public ushort easternSitesGate { get => words[0x4AA0]; set => words[0x4AA0] = value; }
            public ushort scriptWord4AA1 { get => words[0x4AA1]; set => words[0x4AA1] = value; }
            public ushort yarashPyramidLeft { get => words[0x4AA2]; set => words[0x4AA2] = value; }
            public WordView scriptWords4AA3 => new WordView(_bytes, (0x4AA3 - 0x4900) * 2, 0x4AA3, 0x4AB2 - 0x4AA3 + 1);
            public ushort yarashMissionDone { get => words[0x4AB3]; set => words[0x4AB3] = value; }
            public WordView scriptWords4AB4 => new WordView(_bytes, (0x4AB4 - 0x4900) * 2, 0x4AB4, 0x4AD1 - 0x4AB4 + 1);
            public ushort nomadCampPresent { get => words[0x4AD2]; set => words[0x4AD2] = value; }
            public WordView scriptWords4AD3 => new WordView(_bytes, (0x4AD3 - 0x4900) * 2, 0x4AD3, 0x4AF9 - 0x4AD3 + 1);
            public TGbvmSavedWalls savedWalls => new TGbvmSavedWalls(_bytes, (0x4AFA - 0x4900) * 2);
            public WordView scriptWords4B00 => new WordView(_bytes, (0x4B00 - 0x4900) * 2, 0x4B00, 0x4CFF - 0x4B00 + 1);
        }
    }
}
