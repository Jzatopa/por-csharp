using System;
using static Por.Core.Types;

namespace Por.Core
{
    public static class View3D
    {
        public static void SetView3DColors(byte cSky, byte cHorz1, byte cHorz2, byte cFloor)
        {
            GDVars.dsColorSkyCeiling = cSky;
            GDVars.dsWallColorA = cHorz1;
            GDVars.dsWallColorB = cHorz2;
            GDVars.dsWallColorC = cFloor;
        }

        public static byte GetWallType(sbyte x, sbyte y, byte facing)
        {
            if (!Valid3DCoords(x, y) && ((GDVars.eclScriptId == 0) || (GDVars.eclScriptId == 10)))
                return 0;
            else
            {
                WrapCoords(ref x, ref y);
                byte result = 0;
                switch (facing)
                {
                    case C_Dir.NORTH:
                        result = (byte)((GDVars.map3d.wallsNE[y, x] & 0xF0) >> 4);
                        break;
                    case C_Dir.EAST:
                        result = (byte)(GDVars.map3d.wallsNE[y, x] & 0x0F);
                        break;
                    case C_Dir.SOUTH:
                        result = (byte)((GDVars.map3d.wallsSW[y, x] & 0xF0) >> 4);
                        break;
                    case C_Dir.WEST:
                        result = (byte)(GDVars.map3d.wallsSW[y, x] & 0x0F);
                        break;
                }
                return result;
            }
        }

        public static byte GetBarrierType(sbyte x, sbyte y, byte facing)
        {
            if (!Valid3DCoords(x, y) && ((GDVars.eclScriptId == 0) || (GDVars.eclScriptId == 10)))
                return 0;
            else
            {
                WrapCoords(ref x, ref y);
                byte b = 1;
                if (GetWallType(x, y, facing) > 0)
                {
                    switch (facing)
                    {
                        case C_Dir.WEST:
                            b = (byte)((GDVars.map3d.barriers[y, x] & 0xC0) >> 6);
                            break;
                        case C_Dir.SOUTH:
                            b = (byte)((GDVars.map3d.barriers[y, x] & 0x30) >> 4);
                            break;
                        case C_Dir.EAST:
                            b = (byte)((GDVars.map3d.barriers[y, x] & 0x0C) >> 2);
                            break;
                        case C_Dir.NORTH:
                            b = (byte)(GDVars.map3d.barriers[y, x] & 0x03);
                            break;
                    }
                }
                return b;
            }
        }

        public static byte GetMapEvent(sbyte x, sbyte y)
        {
            if (!Valid3DCoords(x, y) && ((GDVars.eclScriptId == 0) || (GDVars.eclScriptId == 10)))
                return 0;
            else
            {
                WrapCoords(ref x, ref y);
                return GDVars.map3d.events[y, x];
            }
        }

        public static void Display3DMapView(sbyte x, sbyte y, byte facing)
        {
            Explored.Mark(x, y);
            if (GDVars.automapOn)
            {
                DrawAutoMap(x, y, facing);
            }
            else
            {
                DrawBackdrop();
                byte fL = (byte)((facing + 6) % 8);
                byte fB = (byte)((facing + 4) % 8);
                byte fR = (byte)((facing + 2) % 8);
                int cxPos = x;
                int cyPos = y;
                ushort dxF = unchecked((ushort)GDConsts.dirDX[facing]);
                ushort dyF = unchecked((ushort)GDConsts.dirDY[facing]);
                int dist = 2;
                cxPos = unchecked((short)(cxPos + dist * dxF));
                cyPos = unchecked((short)(cyPos + dist * dyF));
                while (true)
                {
                    switch (dist)
                    {
                        case 2:
                            FarFronts(fL, fR, 1, -2, ref cxPos, ref cyPos, facing);
                            FarFronts(fR, fL, -1, 2, ref cxPos, ref cyPos, facing);
                            FarSides(fL, 1, -2, -1, ref cxPos, ref cyPos);
                            FarSides(fR, 2, 2, 1, ref cxPos, ref cyPos);
                            break;
                        case 1:
                            FrontsAndSides(fL, 2, 3, -6, 3, 3, fL, 4, fR, ref cxPos, ref cyPos, facing);
                            FrontsAndSides(fR, 2, 3, 6, -3, 3, fR, 5, fL, ref cxPos, ref cyPos, facing);
                            break;
                        case 0:
                            FrontsAndSides(fL, 1, 2, -7, 7, 6, fL, 7, fR, ref cxPos, ref cyPos, facing);
                            FrontsAndSides(fR, 1, 2, 7, -7, 6, fR, 8, fL, ref cxPos, ref cyPos, facing);
                            break;
                    }
                    cxPos += GDConsts.dirDX[fB];
                    cyPos += GDConsts.dirDY[fB];
                    dist--;
                    if (dist == -1) break;
                }
                DisplayViewBuf();
            }
        }

        public static void LoadWallSet(byte fileId, byte wsSlot)
        {
            if (wsSlot < 1 || wsSlot > 3) return;
            ushort sizeRead;
            byte[] data;
            DaxIo.LoadDax("WALLDEF" + StrUtil.NumStr(GDVars.virtualDisk), ".dax", fileId, out sizeRead, out data);
            int blockSize = 780;
            if (sizeRead == 0 || (sizeRead % blockSize != 0) || ((sizeRead / blockSize) + wsSlot > 4) || ((sizeRead > blockSize) && (fileId > 25)))
            {
                GameIo.FatalError("Unable to load wallset in LoadWallSet.");
            }
            ushort symOfs = (ushort)(GDConsts.symSetFirst[wsSlot] - GDConsts.symSetFirst[1]);
            int offset = 0;
            int i = 1;
            while (offset < sizeRead)
            {
                int baseWall = 5 * (wsSlot + i - 2) + 1;
                for (int r = 0; r < 5; r++)
                {
                    for (int c = 0; c < 156; c++)
                    {
                        GDVars.wallGfx[baseWall + r, c] = data[offset + r * 156 + c];
                    }
                }
                offset += blockSize;
                i++;
            }
            int count = i - 1;
            for (i = 1; i <= count; i++)
            {
                int slot = wsSlot + i - 1;
                if (slot >= 1 && slot <= 3)
                {
                    GDVars.dsWallSetCache[slot].wallDefId = -1;
                    GDVars.dsWallSetCache[slot].symbolSlot = -1;
                    int baseWall = 5 * (wsSlot + i - 2) + 1;
                    for (int r = 0; r < 5; r++)
                    {
                        for (int c = 0; c < 156; c++)
                        {
                            if (GDVars.wallGfx[baseWall + r, c] >= GDConsts.symSetLast[0])
                            {
                                GDVars.wallGfx[baseWall + r, c] = unchecked((byte)(GDVars.wallGfx[baseWall + r, c] + symOfs));
                            }
                        }
                    }
                    if (count > 1)
                    {
                        byte setFile = (fileId == 0) ? (byte)10 : fileId;
                        Sym8x8.Load8x8Set((byte)(setFile * 10 + i), (byte)slot);
                    }
                    else
                    {
                        Sym8x8.Load8x8Set(fileId, (byte)slot);
                    }
                }
            }
            GDVars.dsWallSetCache[wsSlot].wallDefId = fileId;
            GDVars.dsWallSetCache[wsSlot].symbolSlot = wsSlot;
        }

        public static void Load3DMap(byte mapId)
        {
            ushort sizeRead;
            byte[] data;
            DaxIo.LoadDax("GEO" + StrUtil.NumStr(GDVars.virtualDisk), ".dax", mapId, out sizeRead, out data);
            if (sizeRead == 0 || sizeRead != 1026)
            {
                GameIo.FatalError("Unable to load geo in Load3DMap.");
            }
            int offset = 2;
            for (int r = 0; r < 16; r++)
            {
                for (int c = 0; c < 16; c++)
                {
                    GDVars.map3d.wallsNE[r, c] = data[offset + r * 16 + c];
                }
            }
            offset += 256;
            for (int r = 0; r < 16; r++)
            {
                for (int c = 0; c < 16; c++)
                {
                    GDVars.map3d.wallsSW[r, c] = data[offset + r * 16 + c];
                }
            }
            offset += 256;
            for (int r = 0; r < 16; r++)
            {
                for (int c = 0; c < 16; c++)
                {
                    GDVars.map3d.events[r, c] = data[offset + r * 16 + c];
                }
            }
            offset += 256;
            for (int r = 0; r < 16; r++)
            {
                for (int c = 0; c < 16; c++)
                {
                    GDVars.map3d.barriers[r, c] = data[offset + r * 16 + c];
                }
            }
            GDVars.zone0.mapId.value = mapId;
        }

        public static void WrapCoords(ref sbyte x, ref sbyte y)
        {
            if (x > MAP_LAST) x = 0;
            if (x < 0) x = MAP_LAST;
            if (y > MAP_LAST) y = 0;
            if (y < 0) y = MAP_LAST;
        }

        private static bool Valid3DCoords(sbyte x, sbyte y)
        {
            if ((x > MAP_LAST) || (x < 0) || (y > MAP_LAST) || (y < 0))
                return false;
            else
                return true;
        }

        private static void DisplayViewBuf()
        {
            Blit.FlushView();
        }

        private static void DrawAutoMap(sbyte x, sbyte y, byte facing)
        {
            sbyte pwx;
            sbyte pwy;
            sbyte wx;
            sbyte wy;
            sbyte xLo;
            sbyte yLo;
            sbyte mx;
            sbyte my;
            ushort sym;

            xLo = (sbyte)(x - 5);
            if (xLo < 0) xLo = 0;
            if (xLo > 5) xLo = 5;
            pwx = (sbyte)(x - xLo);
            yLo = (sbyte)(y - 5);
            if (yLo < 0) yLo = 0;
            if (yLo > 5) yLo = 5;
            pwy = (sbyte)(y - yLo);
            for (wy = 0; wy <= 10; wy++)
            {
                my = (sbyte)(wy + yLo);
                for (wx = 0; wx <= 10; wx++)
                {
                    mx = (sbyte)(wx + xLo);
                    sym = 260;
                    if (GetWallType(mx, my, C_Dir.NORTH) > 0) sym++;
                    if (GetWallType(mx, my, C_Dir.EAST) > 0) sym += 2;
                    if (GetWallType(mx, my, C_Dir.SOUTH) > 0) sym += 4;
                    if (GetWallType(mx, my, C_Dir.WEST) > 0) sym += 8;
                    Sym8x8.Put8x8Sym((sbyte)(wx + 3 - 1), (sbyte)(wy + 3 - 1), sym, true, false);
                }
            }
            Sym8x8.Put8x8Sym((sbyte)(pwx + 3 - 1), (sbyte)(pwy + 3 - 1), (ushort)(facing / 2 + 256), true, false);
            DisplayViewBuf();
        }

        private static void DrawBackdrop()
        {
            Blit.FillPicRect(2, 16, 11, 43, GDVars.dsColorSkyCeiling, GDConsts.dsIconColorMap, GDVars.dsViewImageBufferPtr);
            Blit.FillPicRect(2, 59, 11, 1, GDVars.dsWallColorA, GDConsts.dsIconColorMap, GDVars.dsViewImageBufferPtr);
            Blit.FillPicRect(2, 60, 11, 2, GDVars.dsWallColorB, GDConsts.dsIconColorMap, GDVars.dsViewImageBufferPtr);
            Blit.FillPicRect(2, 62, 11, 42, GDVars.dsWallColorC, GDConsts.dsIconColorMap, GDVars.dsViewImageBufferPtr);
        }

        private static void DrawWallSlice(sbyte x, sbyte y, byte wallType, byte slot)
        {
            int colLim;
            int rowLim;
            int col;
            int row;
            byte cnt;

            cnt = GDConsts.viewSlotGeom.start[slot];
            colLim = GDConsts.viewSlotGeom.cols[slot] + x - 1;
            rowLim = GDConsts.viewSlotGeom.rows[slot] + y - 1;
            for (row = y; row <= rowLim; row++)
            {
                for (col = x; col <= colLim; col++)
                {
                    if ((row >= 0) && (row <= 10) && (col >= 0) && (col <= 10) && (GDVars.wallGfx[wallType, cnt] > 0))
                    {
                        Sym8x8.Put8x8Sym((sbyte)(col + 3 - 1), (sbyte)(row + 3 - 1), GDVars.wallGfx[wallType, cnt], true, true);
                    }
                    cnt++;
                }
            }
        }

        private static void FarFronts(byte walk, byte other, int corner, int step, ref int cxPos, ref int cyPos, byte facing)
        {
            int wx = cxPos;
            int wy = cyPos;
            byte side = 0;
            int symOfs = 0;
            byte wallFront;
            byte prevWall = 0;
            while (side < 4)
            {
                wallFront = GetWallType((sbyte)wx, (sbyte)wy, facing);
                if (!Valid3DCoords((sbyte)wx, (sbyte)wy) && (GetWallType((sbyte)wx, (sbyte)wy, other) == 0))
                    prevWall = 0;
                if (wallFront != 0)
                {
                    if (prevWall > 0)
                        DrawWallSlice((sbyte)(GDConsts.viewSlotGeom.xCol[9] + symOfs + corner), (sbyte)GDConsts.viewSlotGeom.yRow[9], prevWall, 9);
                    prevWall = wallFront;
                    DrawWallSlice((sbyte)(GDConsts.viewSlotGeom.xCol[0] + symOfs), (sbyte)GDConsts.viewSlotGeom.yRow[0], wallFront, 0);
                }
                else
                {
                    if ((prevWall > 0) &&
                       (GetWallType((sbyte)(wx - GDConsts.dirDX[walk]), (sbyte)(wy - GDConsts.dirDY[walk]), walk) != 0))
                        DrawWallSlice((sbyte)(GDConsts.viewSlotGeom.xCol[9] + symOfs + corner), (sbyte)GDConsts.viewSlotGeom.yRow[9], prevWall, 9);
                    prevWall = 0;
                }
                side++;
                symOfs += step;
                wx += GDConsts.dirDX[walk];
                wy += GDConsts.dirDY[walk];
            }
        }

        private static void FarSides(byte dir, byte slot, int step, int adjust, ref int cxPos, ref int cyPos)
        {
            int wx = cxPos;
            int wy = cyPos;
            byte side = 0;
            int symOfs = 0;
            byte wallSide;
            while (side < 3)
            {
                wallSide = GetWallType((sbyte)wx, (sbyte)wy, dir);
                if (wallSide != 0)
                {
                    if (side == 0)
                        DrawWallSlice((sbyte)(GDConsts.viewSlotGeom.xCol[slot] + symOfs), (sbyte)GDConsts.viewSlotGeom.yRow[slot], wallSide, slot);
                    else
                        DrawWallSlice((sbyte)(GDConsts.viewSlotGeom.xCol[slot] + symOfs + adjust), (sbyte)GDConsts.viewSlotGeom.yRow[slot], wallSide, slot);
                }
                side++;
                symOfs += step;
                wx += GDConsts.dirDX[dir];
                wy += GDConsts.dirDY[dir];
            }
        }

        private static void FrontsAndSides(byte start, byte cells, byte count, int symStart, int symStep, byte frontSlot, byte sideDir, byte sideSlot, byte scan, ref int cxPos, ref int cyPos, byte facing)
        {
            int wx = cxPos;
            int wy = cyPos;
            byte side;
            int symOfs;
            byte wallFront;
            byte wallSide;
            for (side = 1; side <= cells; side++)
            {
                wx += GDConsts.dirDX[start];
                wy += GDConsts.dirDY[start];
            }
            side = 0;
            symOfs = symStart;
            while (side < count)
            {
                wallFront = GetWallType((sbyte)wx, (sbyte)wy, facing);
                if (wallFront != 0)
                    DrawWallSlice((sbyte)(GDConsts.viewSlotGeom.xCol[frontSlot] + symOfs), (sbyte)GDConsts.viewSlotGeom.yRow[frontSlot], wallFront, frontSlot);
                wallSide = GetWallType((sbyte)wx, (sbyte)wy, sideDir);
                if (wallSide != 0)
                    DrawWallSlice((sbyte)(GDConsts.viewSlotGeom.xCol[sideSlot] + symOfs), (sbyte)GDConsts.viewSlotGeom.yRow[sideSlot], wallSide, sideSlot);
                side++;
                symOfs += symStep;
                wx += GDConsts.dirDX[scan];
                wy += GDConsts.dirDY[scan];
            }
        }
    }
}
