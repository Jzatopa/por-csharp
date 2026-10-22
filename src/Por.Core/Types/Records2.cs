// FILE: src/Por.Core/Types/Records2.cs
using System;

namespace Por.Core
{
    public static partial class Types
    {
        public class TTerrainRec
        {
            public byte cost;
            public byte clearance;
            public byte blocking;
            public byte tileId;
        }

        public class TViewSlotGeom
        {
            public byte[] start = new byte[10];
            public byte[] cols = new byte[10];
            public byte[] rows = new byte[10];
            public short[] xCol = new short[10];
            public short[] yRow = new short[10];
        }

        public class TWallSetCacheSlot
        {
            public short wallDefId;
            public short symbolSlot;
        }

        public class TAimTarget
        {
            public object cr;
            public sbyte x;
            public sbyte y;
            public byte pad;
        }

        public class TCellList
        {
            public byte count;
            public sbyte[] x = new sbyte[65];
            public sbyte[] y = new sbyte[65];
        }

        public class TAttackPhaseView
        {
            public byte[] fill = new byte[273];
            public byte[] acCurArr = new byte[2];
            public byte[] curAtt = new byte[3];
        }

        public class TArena
        {
            public byte pad0;
            public byte pad1;
            public sbyte ox;
            public sbyte oy;
            public bool cursorOn;
            public byte cursorSize;
            public byte distMode;
            public byte[,] cells = new byte[25, 50];
        }

        public class TAgeRec
        {
            public ushort @base;
            public byte num;
            public byte sides;
        }

        public class TGoldDice
        {
            public byte num;
            public byte sides;
        }

        public class TRaceLim
        {
            public byte[] strMin = new byte[2];
            public byte[] strMax = new byte[2];
            public byte[] excMax = new byte[2];
            public byte intMin;
            public byte intMax;
            public byte wisMin;
            public byte wisMax;
            public byte dexMin;
            public byte dexMax;
            public byte conMin;
            public byte conMax;
            public byte chaMin;
            public byte chaMax;
        }

        public class TPortSlot
        {
            public byte pictureId;
            public TImgBuf pic;
            public byte disk;
        }

        public class TSoundDriver
        {
            public string name = string.Empty;
            public byte mode;
            public TSoundPlay Play;
            public bool singleStep;
        }

        public class TVideoDriver
        {
            public string name = string.Empty;
            public byte colours;
            public bool hasPalette;
            public bool keyed;
            public byte cellShift;
            public byte screenShift;
            public byte maskShift;
            public TCellFillProc CellFill;
            public byte biosMode;
            public ushort ramVideoParas;
            public TVideoProc Start;
            public TVideoProc FlushSpans;
            public TShowPicProc ShowPic;
            public TDrawGlyphProc DrawGlyph;
            public TClearRectProc ClearRect;
            public TVideoProc ClearScreen;
            public TWriteCharProc WriteChar;
            public TExpandFramesProc ExpandFrames;
            public TMirrorFrameProc MirrorFrame;
            public TRemapColourProc RemapColour;
            public TRecolorFrameProc RecolorFrame;
            public TPutPixelRowProc PutPixelRow;
            public TVideoProc InitRowTables;
            public TSetPaletteProc SetPalette;
        }

        public class TEclRet
        {
            public ushort addr;
            public TEclRet next;
        }

        public class THeapEndW
        {
            public ushort o;
            public ushort s;
        }

        public class TMenuAuto
        {
            public int timeout;
            public char key;
        }

        public class TOptRange
        {
            public byte first;
            public byte last;
        }

        public class TLoW
        {
            public ushort lo;
            public ushort hi;
        }

        public class TPathReq
        {
            public short sx;
            public short sy;
            public short ex;
            public short ey;
            public short err;
            public short adx;
            public short ady;
            public short incD;
            public short incS;
            public sbyte stepX;
            public sbyte stepY;
            public short cx;
            public short cy;
            public byte dist;
            public byte step;
        }

        public class TAnimFrame
        {
            public int time;
            public TImgBuf pic;
        }

        public class TAnimRec
        {
            public byte count;
            public byte current;
            public TAnimFrame[] frames = new TAnimFrame[9];

            public TAnimRec()
            {
                for (int i = 0; i < 9; i++)
                {
                    frames[i] = new TAnimFrame();
                }
            }
        }

        public class TStinkRec
        {
            public TCharRecord caster;
            public TStinkRec next;
            public byte[] under = new byte[5];
            public byte[] exists = new byte[5];
            public sbyte ulX;
            public sbyte ulY;
            public byte idx;
        }

        public class TGeoMap
        {
            public TGeoPlane wallsNE = new TGeoPlane();
            public TGeoPlane wallsSW = new TGeoPlane();
            public TGeoPlane events = new TGeoPlane();
            public TGeoPlane barriers = new TGeoPlane();
        }
    }
}
