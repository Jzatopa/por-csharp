// FILE: src/Por.Core/Types/Aliases.cs
using System;
using System.Collections.Generic;

namespace Por.Core
{
    public static partial class Types
    {
        // Constants
        public const int NAME_LEN = 15;
        public const int MAX_PARTY = 8;
        public const int ARENA_SLOTS = 72;
        public const int LAST_DISK = 8;
        public const int MAP_LAST = 15;
        public const int SPELL_SLOT_LAST = 20;
        public const int CASTER_LEVEL_LAST = 3;
        public const int BASE_ITEM_LAST = 127;
        public const int BASE_ITEM_OVERFLOW = 128;
        public const int GB_STRING_LAST = 20;

        public const int AC_DEX = 0;
        public const int AC_SHIELD = 1;
        public const int AC_PLUSES = 2;
        public const int AC_RING = 3;
        public const int AC_ARMOR = 4;

        public const byte PIC_KEY = 0xFF;

        // Enums
        public enum TAimShape
        {
            AimBlast,
            AimBolt,
            AimCloud
        }

        // Delegates
        public delegate void TIdleProc();
        public delegate void TMenuAction();
        public delegate void TCellTracer(sbyte x, sbyte y, ref TCellList cells);
        public delegate void TCellFillProc(byte color, ref TColorMap colorMap, ref TCellBytes cell);
        public delegate void TVideoProc();
        public delegate void TSetPaletteProc(byte registerIndex, byte color);
        public delegate void TShowPicProc(short screenX, short screenY, TImgBuf pic);
        public delegate void TDrawGlyphProc(byte col, byte row, byte color, byte glyph);
        public delegate void TClearRectProc(byte ulCol, byte ulRow, byte lrCol, byte lrRow);
        public delegate void TWriteCharProc(byte col, byte row, byte attr, byte count, byte ch);
        public delegate void TExpandFramesProc(TByteArr imgData, ushort srcOff, bool hasMask, byte transpColor, TImgBuf pic);
        public delegate void TMirrorFrameProc(TImgBuf src, TImgBuf dst);
        public delegate void TRemapColourProc(TImgBuf pic, TImgBuf scratch, ushort srcOff, byte nFrames, byte fromC, byte toC, bool isDithered);
        public delegate void TRecolorFrameProc(ref TColorTab toT, TImgBuf pic);
        public delegate void TPutPixelRowProc(TImgBuf pic, ushort at, byte y, ref TPixelRow px);
        public delegate void TSoundPlay(ushort tune);
        public delegate void TEffectHandler(TCharRecord creature, object effectData, bool removed);
        public delegate void TCastUI(byte spell, bool b, ref bool sel);
        public delegate void TSpellProc();

        // Array Aliases
        public class TIconCols
        {
            public byte[] Data;
            public TIconCols() { Data = new byte[7]; }
            public TIconCols(byte[] values)
            {
                if (values == null || values.Length != 7) throw new ArgumentException("Invalid length");
                Data = values;
            }
            public byte this[int index]
            {
                get { if (index < 1 || index > 6) throw new IndexOutOfRangeException(); return Data[index]; }
                set { if (index < 1 || index > 6) throw new IndexOutOfRangeException(); Data[index] = value; }
            }
        }

        public class TSpellSlots
        {
            public byte[] Data;
            public TSpellSlots() { Data = new byte[21]; }
            public TSpellSlots(byte[] values)
            {
                if (values == null || values.Length != 21) throw new ArgumentException("Invalid length");
                Data = values;
            }
            public byte this[int index]
            {
                get { if (index < 0 || index > 20) throw new IndexOutOfRangeException(); return Data[index]; }
                set { if (index < 0 || index > 20) throw new IndexOutOfRangeException(); Data[index] = value; }
            }
        }

        public class TBrkSet : HashSet<char>
        {
            public TBrkSet() : base() { }
            public TBrkSet(HashSet<char> values) : base(values) { }
        }

        public class TBytes
        {
            public byte[] Data;
            public TBytes() { Data = new byte[65001]; }
            public TBytes(byte[] values)
            {
                if (values == null || values.Length != 65001) throw new ArgumentException("Invalid length");
                Data = values;
            }
            // Blit allocates open-ended mask blocks; bounds follow allocated backing storage.
            public byte this[int index]
            {
                get { return Data[index]; }
                set { Data[index] = value; }
            }
        }

        public class TColorMap
        {
            public byte[] Data;
            public TColorMap() { Data = new byte[8]; }
            public TColorMap(byte[] values)
            {
                if (values == null || values.Length != 8) throw new ArgumentException("Invalid length");
                Data = values;
            }
            public byte this[int index]
            {
                get { if (index < 0 || index > 7) throw new IndexOutOfRangeException(); return Data[index]; }
                set { if (index < 0 || index > 7) throw new IndexOutOfRangeException(); Data[index] = value; }
            }
        }

        public class TPixelRow
        {
            public byte[] Data;
            public TPixelRow() { Data = new byte[8]; }
            public TPixelRow(byte[] values)
            {
                if (values == null || values.Length != 8) throw new ArgumentException("Invalid length");
                Data = values;
            }
            public byte this[int index]
            {
                get { if (index < 0 || index > 7) throw new IndexOutOfRangeException(); return Data[index]; }
                set { if (index < 0 || index > 7) throw new IndexOutOfRangeException(); Data[index] = value; }
            }
        }

        public class TColorTab
        {
            public byte[] Data;
            public TColorTab() { Data = new byte[16]; }
            public TColorTab(byte[] values)
            {
                if (values == null || values.Length != 16) throw new ArgumentException("Invalid length");
                Data = values;
            }
            public byte this[int index]
            {
                get { if (index < 0 || index > 15) throw new IndexOutOfRangeException(); return Data[index]; }
                set { if (index < 0 || index > 15) throw new IndexOutOfRangeException(); Data[index] = value; }
            }
        }

        public class TTargetList
        {
            public TCombatTarget[] Data;
            public TTargetList()
            {
                Data = new TCombatTarget[73];
                for (int i = 0; i < 73; i++) Data[i] = new TCombatTarget();
            }
            public TTargetList(TCombatTarget[] values)
            {
                if (values == null || values.Length != 73) throw new ArgumentException("Invalid length");
                Data = values;
            }
            public TCombatTarget this[int index]
            {
                get { if (index < 1 || index > 72) throw new IndexOutOfRangeException(); return Data[index]; }
                set { if (index < 1 || index > 72) throw new IndexOutOfRangeException(); Data[index] = value; }
            }
        }

        public class TTurnUndeadTable
        {
            public sbyte[,] Data;
            public TTurnUndeadTable() { Data = new sbyte[11, 11]; }
            public TTurnUndeadTable(sbyte[,] values)
            {
                if (values == null || values.GetLength(0) != 11 || values.GetLength(1) != 11) throw new ArgumentException("Invalid dimensions");
                Data = values;
            }
            public sbyte this[int i, int j]
            {
                get { if (i < 1 || i > 10 || j < 1 || j > 10) throw new IndexOutOfRangeException(); return Data[i, j]; }
                set { if (i < 1 || i > 10 || j < 1 || j > 10) throw new IndexOutOfRangeException(); Data[i, j] = value; }
            }
        }

        public class TGbvmZone0
        {
            public ushort[] Data;
            public TGbvmZone0() { Data = new ushort[0x4D00]; }
            public TGbvmZone0(ushort[] values)
            {
                if (values == null || values.Length != 0x4D00) throw new ArgumentException("Invalid length");
                Data = values;
            }
            public ushort this[int index]
            {
                get { if (index < 0x4900 || index > 0x4CFF) throw new IndexOutOfRangeException(); return Data[index]; }
                set { if (index < 0x4900 || index > 0x4CFF) throw new IndexOutOfRangeException(); Data[index] = value; }
            }
        }

        public class TGbvmZone1
        {
            public ushort[] Data;
            public TGbvmZone1() { Data = new ushort[0x6F00]; }
            public TGbvmZone1(ushort[] values)
            {
                if (values == null || values.Length != 0x6F00) throw new ArgumentException("Invalid length");
                Data = values;
            }
            public ushort this[int index]
            {
                get { if (index < 0x6B00 || index > 0x6EFF) throw new IndexOutOfRangeException(); return Data[index]; }
                set { if (index < 0x6B00 || index > 0x6EFF) throw new IndexOutOfRangeException(); Data[index] = value; }
            }
        }

        public class TGbStrings
        {
            public string[] Data;
            public TGbStrings()
            {
                Data = new string[21];
                for (int i = 0; i < 21; i++) Data[i] = string.Empty;
            }
            public TGbStrings(string[] values)
            {
                if (values == null || values.Length != 21) throw new ArgumentException("Invalid length");
                Data = values;
            }
            public string this[int index]
            {
                get { if (index < 1 || index > 20) throw new IndexOutOfRangeException(); return Data[index]; }
                set { if (index < 1 || index > 20) throw new IndexOutOfRangeException(); Data[index] = value; }
            }
        }

        public class TBaseItems
        {
            public TBaseItemRec[] Data;
            public TBaseItems()
            {
                Data = new TBaseItemRec[129];
                for (int i = 0; i < 129; i++) Data[i] = new TBaseItemRec();
            }
            public TBaseItems(TBaseItemRec[] values)
            {
                if (values == null || values.Length != 129) throw new ArgumentException("Invalid length");
                Data = values;
            }
            public TBaseItemRec this[int index]
            {
                get { if (index < 0 || index > 128) throw new IndexOutOfRangeException(); return Data[index]; }
                set { if (index < 0 || index > 128) throw new IndexOutOfRangeException(); Data[index] = value; }
            }
        }

        public class TCgaBank
        {
            public byte[][] Data;
            public TCgaBank()
            {
                Data = new byte[100][];
                for (int i = 0; i < 100; i++) Data[i] = new byte[80];
            }
            public TCgaBank(byte[][] values)
            {
                if (values == null || values.Length != 100) throw new ArgumentException("Invalid outer length");
                for (int i = 0; i < 100; i++)
                {
                    if (values[i] == null || values[i].Length != 80) throw new ArgumentException("Invalid inner length");
                }
                Data = values;
            }
            public byte this[int i, int j]
            {
                get { if (i < 0 || i > 99 || j < 0 || j > 79) throw new IndexOutOfRangeException(); return Data[i][j]; }
                set { if (i < 0 || i > 99 || j < 0 || j > 79) throw new IndexOutOfRangeException(); Data[i][j] = value; }
            }
        }

        public class TTandyBank
        {
            public byte[][] Data;
            public TTandyBank()
            {
                Data = new byte[50][];
                for (int i = 0; i < 50; i++) Data[i] = new byte[160];
            }
            public TTandyBank(byte[][] values)
            {
                if (values == null || values.Length != 50) throw new ArgumentException("Invalid outer length");
                for (int i = 0; i < 50; i++)
                {
                    if (values[i] == null || values[i].Length != 160) throw new ArgumentException("Invalid inner length");
                }
                Data = values;
            }
            public byte this[int i, int j]
            {
                get { if (i < 0 || i > 49 || j < 0 || j > 159) throw new IndexOutOfRangeException(); return Data[i][j]; }
                set { if (i < 0 || i > 49 || j < 0 || j > 159) throw new IndexOutOfRangeException(); Data[i][j] = value; }
            }
        }

        public class TClock
        {
            public ushort[] Data;
            public TClock() { Data = new ushort[7]; }
            public TClock(ushort[] values)
            {
                if (values == null || values.Length != 7) throw new ArgumentException("Invalid length");
                Data = values;
            }
            public ushort this[int index]
            {
                get { if (index < 0 || index > 6) throw new IndexOutOfRangeException(); return Data[index]; }
                set { if (index < 0 || index > 6) throw new IndexOutOfRangeException(); Data[index] = value; }
            }
        }

        public class TAcAdjust
        {
            public sbyte[] Data;
            public TAcAdjust() { Data = new sbyte[5]; }
            public TAcAdjust(sbyte[] values)
            {
                if (values == null || values.Length != 5) throw new ArgumentException("Invalid length");
                Data = values;
            }
            public sbyte this[int index]
            {
                get { if (index < 0 || index > 4) throw new IndexOutOfRangeException(); return Data[index]; }
                set { if (index < 0 || index > 4) throw new IndexOutOfRangeException(); Data[index] = value; }
            }
        }

        public class TByteArr
        {
            public sbyte[] Data;
            public TByteArr() { Data = new sbyte[65535]; }
            public TByteArr(sbyte[] values)
            {
                if (values == null || values.Length != 65535) throw new ArgumentException("Invalid length");
                Data = values;
            }
            public sbyte this[int index]
            {
                get { if (index < 0 || index > 65534) throw new IndexOutOfRangeException(); return Data[index]; }
                set { if (index < 0 || index > 65534) throw new IndexOutOfRangeException(); Data[index] = value; }
            }
        }

        public class TCellBytes
        {
            public byte[] Data;
            public TCellBytes() { Data = new byte[8]; }
            public TCellBytes(byte[] values)
            {
                if (values == null || values.Length != 8) throw new ArgumentException("Invalid length");
                Data = values;
            }
            public byte this[int index]
            {
                get { if (index < 0 || index > 7) throw new IndexOutOfRangeException(); return Data[index]; }
                set { if (index < 0 || index > 7) throw new IndexOutOfRangeException(); Data[index] = value; }
            }
        }

        public class TMonFlags
        {
            public bool[] Data;
            public TMonFlags() { Data = new bool[2]; }
            public TMonFlags(bool[] values)
            {
                if (values == null || values.Length != 2) throw new ArgumentException("Invalid length");
                Data = values;
            }
            public bool this[int index]
            {
                get { if (index < 0 || index > 1) throw new IndexOutOfRangeException(); return Data[index]; }
                set { if (index < 0 || index > 1) throw new IndexOutOfRangeException(); Data[index] = value; }
            }
        }

        public class TZone2
        {
            public ushort[] Data;
            public TZone2() { Data = new ushort[0x9900]; }
            public TZone2(ushort[] values)
            {
                if (values == null || values.Length != 0x9900) throw new ArgumentException("Invalid length");
                Data = values;
            }
            public ushort this[int index]
            {
                get { if (index < 0x9700 || index > 0x98FF) throw new IndexOutOfRangeException(); return Data[index]; }
                set { if (index < 0x9700 || index > 0x98FF) throw new IndexOutOfRangeException(); Data[index] = value; }
            }
        }

        public class TZone3
        {
            public byte[] Data;
            public TZone3() { Data = new byte[C_Gbvm.SCRIPT_LAST + 1]; }
            public TZone3(byte[] values)
            {
                if (values == null || values.Length != (C_Gbvm.SCRIPT_LAST + 1)) throw new ArgumentException("Invalid length");
                Data = values;
            }
            public byte this[int index]
            {
                get { if (index < C_Gbvm.SCRIPT_FIRST || index > C_Gbvm.SCRIPT_LAST) throw new IndexOutOfRangeException(); return Data[index]; }
                set { if (index < C_Gbvm.SCRIPT_FIRST || index > C_Gbvm.SCRIPT_LAST) throw new IndexOutOfRangeException(); Data[index] = value; }
            }
        }

        public class TMoney
        {
            public int[] Data;
            public TMoney() { Data = new int[7]; }
            public TMoney(int[] values)
            {
                if (values == null || values.Length != 7) throw new ArgumentException("Invalid length");
                Data = values;
            }
            public int this[int index]
            {
                get { if (index < C_Money.COPPER || index > C_Money.JEWELRY) throw new IndexOutOfRangeException(); return Data[index - C_Money.COPPER]; }
                set { if (index < C_Money.COPPER || index > C_Money.JEWELRY) throw new IndexOutOfRangeException(); Data[index - C_Money.COPPER] = value; }
            }
        }

        public class TCgaRow
        {
            public ushort[] Data;
            public TCgaRow() { Data = new ushort[40]; }
            public TCgaRow(ushort[] values)
            {
                if (values == null || values.Length != 40) throw new ArgumentException("Invalid length");
                Data = values;
            }
            public ushort this[int index]
            {
                get { if (index < 0 || index > 39) throw new IndexOutOfRangeException(); return Data[index]; }
                set { if (index < 0 || index > 39) throw new IndexOutOfRangeException(); Data[index] = value; }
            }
        }

        public class TFontArr
        {
            public byte[][] Data;
            public TFontArr()
            {
                Data = new byte[177][];
                for (int i = 0; i < 177; i++) Data[i] = new byte[8];
            }
            public TFontArr(byte[][] values)
            {
                if (values == null || values.Length != 177) throw new ArgumentException("Invalid outer length");
                for (int i = 0; i < 177; i++)
                {
                    if (values[i] == null || values[i].Length != 8) throw new ArgumentException("Invalid inner length");
                }
                Data = values;
            }
            public byte this[int i, int j]
            {
                get { if (i < 0 || i > 176 || j < 0 || j > 7) throw new IndexOutOfRangeException(); return Data[i][j]; }
                set { if (i < 0 || i > 176 || j < 0 || j > 7) throw new IndexOutOfRangeException(); Data[i][j] = value; }
            }
        }

        public class TTdyRow
        {
            public byte[] Data;
            public TTdyRow() { Data = new byte[160]; }
            public TTdyRow(byte[] values)
            {
                if (values == null || values.Length != 160) throw new ArgumentException("Invalid length");
                Data = values;
            }
            public byte this[int index]
            {
                get { if (index < 0 || index > 159) throw new IndexOutOfRangeException(); return Data[index]; }
                set { if (index < 0 || index > 159) throw new IndexOutOfRangeException(); Data[index] = value; }
            }
        }

        public class TGeoPlane
        {
            public byte[,] Data;
            public TGeoPlane() { Data = new byte[16, 16]; }
            public TGeoPlane(byte[,] values)
            {
                if (values == null || values.GetLength(0) != 16 || values.GetLength(1) != 16) throw new ArgumentException("Invalid dimensions");
                Data = values;
            }
            public byte this[int i, int j]
            {
                get { if (i < 0 || i > 15 || j < 0 || j > 15) throw new IndexOutOfRangeException(); return Data[i, j]; }
                set { if (i < 0 || i > 15 || j < 0 || j > 15) throw new IndexOutOfRangeException(); Data[i, j] = value; }
            }
        }

        public class TView3dWallDef
        {
            public byte[,] Data;
            public TView3dWallDef() { Data = new byte[6, 156]; }
            public TView3dWallDef(byte[,] values)
            {
                if (values == null || values.GetLength(0) != 6 || values.GetLength(1) != 156) throw new ArgumentException("Invalid dimensions");
                Data = values;
            }
            public byte this[int i, int j]
            {
                get { if (i < 1 || i > 5 || j < 0 || j > 155) throw new IndexOutOfRangeException(); return Data[i, j]; }
                set { if (i < 1 || i > 5 || j < 0 || j > 155) throw new IndexOutOfRangeException(); Data[i, j] = value; }
            }
        }

        public class TWallSlices
        {
            public byte[,] Data;
            public TWallSlices() { Data = new byte[16, 156]; }
            public TWallSlices(byte[,] values)
            {
                if (values == null || values.GetLength(0) != 16 || values.GetLength(1) != 156) throw new ArgumentException("Invalid dimensions");
                Data = values;
            }
            public byte this[int i, int j]
            {
                get { if (i < 1 || i > 15 || j < 0 || j > 155) throw new IndexOutOfRangeException(); return Data[i, j]; }
                set { if (i < 1 || i > 15 || j < 0 || j > 155) throw new IndexOutOfRangeException(); Data[i, j] = value; }
            }
        }
    }
}
