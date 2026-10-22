// FILE: src/Por.Core/Rules/Pathing.cs
using System;

namespace Por.Core
{
    public static class Pathing
    {
        public delegate byte TOccupant(sbyte x, sbyte y);

        private const int ARENA_LAST_X = 49;
        private const int ARENA_LAST_Y = 24;
        private const byte WALL_COST = 255;
        private const byte UNREACHED = 255;
        private const byte START = 8;

        private static readonly byte[] TURNS = { 0, 1, 7, 2, 6, 3, 5, 4 };

        public static short SignOf(short v)
        {
            if (v < 0)
                return -1;
            if (v > 0)
                return 1;
            return 0;
        }

        private static void SortTargets()
        {
            if (GDVars.targetCount <= 1)
                return;
            for (int i = 1; i <= GDVars.targetCount - 1; i++)
            {
                for (int j = i + 1; j <= GDVars.targetCount; j++)
                {
                    byte auxI = GDVars.targetList[i].dir;
                    byte auxJ = GDVars.targetList[j].dir;
                    if ((GDVars.targetList[j].dist < GDVars.targetList[i].dist) ||
                        ((GDVars.targetList[j].dist == GDVars.targetList[i].dist) && (auxJ < auxI) &&
                         (auxJ % 2 <= auxI % 2)))
                    {
                        var ti = GDVars.targetList[i];
                        var tj = GDVars.targetList[j];
                        byte tmpIdx = ti.idx;
                        byte tmpDist = ti.dist;
                        byte tmpDir = ti.dir;
                        ti.idx = tj.idx;
                        ti.dist = tj.dist;
                        ti.dir = tj.dir;
                        tj.idx = tmpIdx;
                        tj.dist = tmpDist;
                        tj.dir = tmpDir;
                    }
                }
            }
        }

        public static void PathFind(ref Types.TPathReq req)
        {
            req.dist = 0;
            req.adx = (short)Math.Abs(req.ex - req.sx);
            req.ady = (short)Math.Abs(req.ey - req.sy);
            req.stepX = unchecked((sbyte)SignOf((short)(req.ex - req.sx)));
            req.stepY = unchecked((sbyte)SignOf((short)(req.ey - req.sy)));
            req.cx = req.sx;
            req.cy = req.sy;
            if (req.adx > req.ady)
            {
                req.err = (short)(req.ady * 2 - req.adx);
                req.incD = (short)((req.ady - req.adx) * 2);
                req.incS = (short)(req.ady * 2);
            }
            else
            {
                req.err = (short)(req.adx * 2 - req.ady);
                req.incD = (short)((req.adx - req.ady) * 2);
                req.incS = (short)(req.adx * 2);
            }
        }

        public static bool NextPathStep(ref Types.TPathReq req)
        {
            sbyte dx8 = 0;
            sbyte dy8 = 0;
            bool result = false;
            if (req.adx > req.ady)
            {
                if (req.cx != req.ex)
                {
                    dx8 = req.stepX;
                    if (req.err >= 0)
                    {
                        dy8 = req.stepY;
                        req.cy = (short)(req.cy + req.stepY);
                        req.err = (short)(req.err + req.incD);
                        req.dist = unchecked((byte)(req.dist + 3));
                    }
                    else
                    {
                        req.err = (short)(req.err + req.incS);
                        req.dist = unchecked((byte)(req.dist + 2));
                    }
                    req.cx = (short)(req.cx + req.stepX);
                    result = true;
                }
            }
            else
            {
                if (req.cy != req.ey)
                {
                    dy8 = req.stepY;
                    if (req.err >= 0)
                    {
                        dx8 = req.stepX;
                        req.cx = (short)(req.cx + req.stepX);
                        req.err = (short)(req.err + req.incD);
                        req.dist = unchecked((byte)(req.dist + 3));
                    }
                    else
                    {
                        req.err = (short)(req.err + req.incS);
                        req.dist = unchecked((byte)(req.dist + 2));
                    }
                    req.cy = (short)(req.cy + req.stepY);
                    result = true;
                }
            }
            req.step = GDConsts.dirByDelta[dy8 + 1, dx8 + 1];
            return result;
        }

        public static bool IsDirToward(sbyte x, sbyte y, sbyte tx, sbyte ty, byte dir)
        {
            if (x < 0 || x > 49 || y < 0 || y > 24 || tx < 0 || tx > 49 || ty < 0 || ty > 24)
                return false;
            if (dir > C_Dir.CENTER)
                dir = (byte)C_Dir.CENTER;
            sbyte nx = unchecked((sbyte)(x + GDConsts.dirDX[dir]));
            sbyte ny = unchecked((sbyte)(y + GDConsts.dirDY[dir]));
            if ((x == tx && y == ty) || (nx == tx && ny == ty))
                return true;
            switch (dir)
            {
                case C_Dir.NORTH:
                    return ((tx >= nx) && (ty <= (nx - tx) + ny)) || ((tx <= nx) && (ty <= (tx - nx) + ny));
                case C_Dir.NORTHEAST:
                    return ((tx >= nx) && (ty <= (nx - tx) + ny)) || ((tx >= (nx + ny) - ty) && (ty <= ny));
                case C_Dir.EAST:
                    return ((tx >= (nx + ny) - ty) && (ty <= ny)) || ((tx >= (nx + ty) - ny) && (ty >= ny));
                case C_Dir.SOUTHEAST:
                    return ((tx >= (nx + ty) - ny) && (ty >= ny)) || ((tx >= nx) && (ty >= (tx - nx) + ny));
                case C_Dir.SOUTH:
                    return ((tx >= nx) && (ty >= (tx - nx) + ny)) || ((tx <= nx) && (ty >= (nx - tx) + ny));
                case C_Dir.SOUTHWEST:
                    return ((tx <= nx) && (ty >= (nx - tx) + ny)) || ((tx <= (nx + ny) - ty) && (ty >= ny));
                case C_Dir.WEST:
                    return ((tx <= (nx + ny) - ty) && (ty >= ny)) || ((tx <= (nx + ty) - ny) && (ty <= ny));
                case C_Dir.NORTHWEST:
                    return ((tx <= (nx + ty) - ny) && (ty <= ny)) || ((tx <= nx) && (ty <= (tx - nx) + ny));
                case C_Dir.CENTER:
                    return true;
                default:
                    return false;
            }
        }

        public static bool WalkLine(sbyte x, sbyte y, ref sbyte tx, ref sbyte ty, ref ushort dist, Types.TArena view)
        {
            Types.TPathReq walk = new Types.TPathReq();
            Types.TPathReq meter = new Types.TPathReq();
            bool done;
            ushort maxDist;

            if (x < 0 || x > 49 || y < 0 || y > 24 || tx < 0 || tx > 49 || ty < 0 || ty > 24)
            {
                dist = 0;
                return false;
            }

            maxDist = unchecked((ushort)(dist * 2 + 1));
            walk.sx = x;
            walk.sy = y;
            walk.ex = tx;
            walk.ey = ty;
            PathFind(ref walk);

            meter.sx = 0;
            meter.sy = GDConsts.terrainInfo[view.cells[y, x]].clearance;
            if (walk.adx > walk.ady)
                meter.ex = walk.adx;
            else
                meter.ex = walk.ady;
            meter.ey = GDConsts.terrainInfo[view.cells[y, x]].clearance;
            PathFind(ref meter);

            done = false;
            do
            {
                if (((view.distMode == 0) &&
                     (GDConsts.terrainInfo[view.cells[walk.cy, walk.cx]].blocking > meter.cy)) ||
                    (walk.dist > maxDist))
                {
                    tx = unchecked((sbyte)walk.cx);
                    ty = unchecked((sbyte)walk.cy);
                    dist = walk.dist;
                    return false;
                }
                NextPathStep(ref meter);
                done = !NextPathStep(ref walk);
            } while (!done);

            dist = walk.dist;
            return true;
        }

        public static void BuildTargetList(sbyte x, sbyte y, ushort range, byte filter, byte size, Types.TArena view)
        {
            byte bestDir;
            bool found;
            sbyte tx, ty;
            sbyte dx8 = 0, dy8 = 0;
            sbyte[] myX = new sbyte[4];
            sbyte[] myY = new sbyte[4];
            sbyte[] tgX = new sbyte[4];
            sbyte[] tgY = new sbyte[4];
            ushort dist;
            ushort bestDist;
            byte last;

            for (int k = 0; k <= 3; k++)
            {
                if (GetIconTileDelta(size, (byte)k, ref dx8, ref dy8))
                {
                    myX[k] = unchecked((sbyte)(x + dx8));
                    myY[k] = unchecked((sbyte)(y + dy8));
                }
                else
                {
                    myX[k] = -1;
                }
            }

            GDVars.targetCount = 0;
            last = GDVars.arenaCount;
            if (last > Types.ARENA_SLOTS)
                last = (byte)Types.ARENA_SLOTS;

            for (int i = 1; i <= last; i++)
            {
                if (GDVars.arenaLoc[i].size > 0)
                {
                    found = false;
                    bestDist = 255;
                    int bestK = 0;
                    int bestJ = 0;

                    for (int j = 0; j <= 3; j++)
                    {
                        if (GetIconTileDelta(GDVars.arenaLoc[i].size, (byte)j, ref dx8, ref dy8))
                        {
                            tgX[j] = unchecked((sbyte)(GDVars.arenaLoc[i].x + dx8));
                            tgY[j] = unchecked((sbyte)(GDVars.arenaLoc[i].y + dy8));
                        }
                        else
                        {
                            tgX[j] = -1;
                        }
                    }

                    for (int j = 0; j <= 3; j++)
                    {
                        if (tgX[j] >= 0)
                        {
                            for (int k = 0; k <= 3; k++)
                            {
                                if (myX[k] >= 0)
                                {
                                    if (IsDirToward(myX[k], myY[k], tgX[j], tgY[j], filter))
                                    {
                                        tx = tgX[j];
                                        ty = tgY[j];
                                        dist = range;
                                        if (WalkLine(myX[k], myY[k], ref tx, ref ty, ref dist, view))
                                        {
                                            found = true;
                                            if (dist < bestDist)
                                            {
                                                bestDist = dist;
                                                bestK = k;
                                                bestJ = j;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (found)
                    {
                        GDVars.targetCount = unchecked((byte)(GDVars.targetCount + 1));
                        GDVars.targetList[GDVars.targetCount].idx = (byte)i;
                        GDVars.targetList[GDVars.targetCount].dist = unchecked((byte)bestDist);
                        bestDir = 0;
                        if (filter < C_Dir.CENTER)
                        {
                            bestDir = filter;
                        }
                        else
                        {
                            while (!IsDirToward(myX[bestK], myY[bestK], tgX[bestJ], tgY[bestJ], bestDir))
                            {
                                bestDir++;
                            }
                        }
                        GDVars.targetList[GDVars.targetCount].dir = bestDir;
                    }
                }
            }

            SortTargets();
        }

        public static bool GetIconTileDelta(byte size, byte idx, ref sbyte dx, ref sbyte dy)
        {
            if (size == 0 || size > 4)
                return false;
            dx = GDConsts.iconDelta[size, idx].dx;
            dy = GDConsts.iconDelta[size, idx].dy;
            return dx >= 0;
        }

        public static void TargetArea(sbyte x, sbyte y, byte radius)
        {
            BuildTargetList(x, y, radius, 255, 1, GDVars.arenaPtr);
            for (int k = 1; k <= GDVars.targetCount; k++)
            {
                GDVars.dsTargetCreaturePtr[k] = GDVars.creatureArr[GDVars.targetList[k].idx];
            }
            GDVars.spellTargetCount = GDVars.targetCount;
        }

        private static bool CanStand(sbyte x, sbyte y, byte size, byte slot, TOccupant occupant)
        {
            for (int i = 0; i <= 3; i++)
            {
                sbyte dx = 0, dy = 0;
                if (GetIconTileDelta(size, (byte)i, ref dx, ref dy))
                {
                    sbyte cx = unchecked((sbyte)(x + dx));
                    sbyte cy = unchecked((sbyte)(y + dy));
                    if (cx < 0 || cx > ARENA_LAST_X || cy < 0 || cy > ARENA_LAST_Y)
                        return false;
                    byte t = GDVars.arenaPtr.cells[cy, cx];
                    if (t == 0 || GDConsts.terrainInfo[t].cost == WALL_COST)
                        return false;
                    byte c = occupant(cx, cy);
                    if (c != 0 && c != slot)
                        return false;
                }
            }
            return true;
        }

        private static bool Reaches(sbyte x, sbyte y, byte size, sbyte tx, sbyte ty, byte tsize)
        {
            for (int i = 0; i <= 3; i++)
            {
                sbyte dx = 0, dy = 0;
                if (GetIconTileDelta(size, (byte)i, ref dx, ref dy))
                {
                    for (int j = 0; j <= 3; j++)
                    {
                        sbyte ex = 0, ey = 0;
                        if (GetIconTileDelta(tsize, (byte)j, ref ex, ref ey))
                        {
                            if (Math.Abs((x + dx) - (tx + ex)) <= 1 && Math.Abs((y + dy) - (ty + ey)) <= 1)
                                return true;
                        }
                    }
                }
            }
            return false;
        }

        public static bool RouteStep(byte slot, byte targetSlot, TOccupant occupant, ref byte dir)
        {
            sbyte sx = GDVars.arenaLoc[slot].x;
            sbyte sy = GDVars.arenaLoc[slot].y;
            byte size = GDVars.arenaLoc[slot].size;
            sbyte tx = GDVars.arenaLoc[targetSlot].x;
            sbyte ty = GDVars.arenaLoc[targetSlot].y;
            byte tsize = GDVars.arenaLoc[targetSlot].size;

            if (size == 0 || tsize == 0 || Reaches(sx, sy, size, tx, ty, tsize))
                return false;

            byte heading = (byte)C_Dir.NORTH;
            for (int d = C_Dir.NORTH; d <= C_Dir.NORTHWEST; d++)
            {
                if (GDConsts.dirDX[d] == SignOf((short)(tx - sx)) && GDConsts.dirDY[d] == SignOf((short)(ty - sy)))
                    heading = (byte)d;
            }

            byte[,] marks = new byte[ARENA_LAST_Y + 1, ARENA_LAST_X + 1];
            ushort[] queue = new ushort[(ARENA_LAST_X + 1) * (ARENA_LAST_Y + 1)];

            for (int yy = 0; yy <= ARENA_LAST_Y; yy++)
                for (int xx = 0; xx <= ARENA_LAST_X; xx++)
                    marks[yy, xx] = UNREACHED;

            marks[sy, sx] = START;
            queue[0] = unchecked((ushort)(sx + (ARENA_LAST_X + 1) * sy));

            int head = 0;
            int tail = 1;
            bool found = false;

            while (head < tail && !found)
            {
                int cx = queue[head] % (ARENA_LAST_X + 1);
                int cy = queue[head] / (ARENA_LAST_X + 1);
                head++;

                int k = 0;
                while (k <= 7 && !found)
                {
                    int d = (heading + TURNS[k]) % 8;
                    int nx = cx + GDConsts.dirDX[d];
                    int ny = cy + GDConsts.dirDY[d];

                    if (nx >= 0 && nx <= ARENA_LAST_X && ny >= 0 && ny <= ARENA_LAST_Y)
                    {
                        if (marks[ny, nx] == UNREACHED && CanStand((sbyte)nx, (sbyte)ny, size, slot, occupant))
                        {
                            byte first = marks[cy, cx];
                            if (first == START)
                                first = (byte)d;
                            marks[ny, nx] = first;

                            if (Reaches((sbyte)nx, (sbyte)ny, size, tx, ty, tsize))
                            {
                                dir = first;
                                found = true;
                            }
                            else
                            {
                                queue[tail] = unchecked((ushort)(nx + (ARENA_LAST_X + 1) * ny));
                                tail++;
                            }
                        }
                    }
                    k++;
                }
            }

            return found;
        }
    }
}

