// Direct conversion of pinned src/cbtmap.pas. Coordinates narrow as Pascal ShortInt.
using System;
using static Por.Core.Types;
namespace Por.Core
{
    public static class CbtMap
    {
        public static TCellTracer boltTracer;
        private static readonly byte[,] occupants = new byte[25,50];
        private static bool previewOn, aimSet;
        private static byte previewRadius, areaArt, aimSize, areaCount, ringCount, ringsShown, frameNo;
        private static TAimShape previewShape;
        private static sbyte aimX, aimY;
        private static readonly sbyte[] areaX=new sbyte[65], areaY=new sbyte[65];
        private static readonly byte[] areaRing=new byte[65];
        private static int lastTick;
        // External effects only; defaults exercise the real renderer, sound and clock.
        internal delegate void TileCopy(sbyte x,sbyte y,byte tile,sbyte mode);
        internal delegate void SpriteDraw(sbyte x,sbyte y,byte facing,byte frame,byte art);
        internal static TileCopy CopyTile=MapTiles.Copy2DTile;
        internal static SpriteDraw DrawSprite=MapTiles.DrawIconAnimSprite;
        internal static Action Flush=Blit.FlushView;
        internal static Func<int> ReadTicks=TextRend.GetTicks;
        internal static Action<ushort> AutoStop=SoundDrv.SoundSetAutoStop;
        internal static Action<ushort> Sound=SoundDrv.SoundCommand;
        internal static Action Pause=TextRend.PauseOnly;
        private static sbyte S(int value){return unchecked((sbyte)value);}
        private static byte B(int value){return unchecked((byte)value);}
        private static bool Delta(byte size,int i,out sbyte dx,out sbyte dy){dx=dy=0;return Pathing.GetIconTileDelta(size,B(i),ref dx,ref dy);}
        public static void SetCombatPalette(){Blit.SetPaletteRegister(0,8);Blit.SetPaletteRegister(8,0);}
        public static void SetNoncombatPalette(){Blit.SetPaletteRegister(0,0);Blit.SetPaletteRegister(8,8);}
        public static void BuildArenaArrays()
        {
            Array.Clear(occupants,0,occupants.Length);
            for(int i=1;i<=GDVars.arenaCount;i++) if(GDVars.arenaLoc[i].size>0)
            {
                var loc=GDVars.arenaLoc[i];
                for(int j=0;j<4;j++){sbyte dx,dy;if(Delta(loc.size,j,out dx,out dy))occupants[loc.y+dy,loc.x+dx]=B(i);}
                GDVars.gridX[i]=S(loc.x-GDVars.arenaPtr.ox);GDVars.gridY[i]=S(loc.y-GDVars.arenaPtr.oy);
            }
        }
        private static void CalcGridDeltas(){for(int i=1;i<=GDVars.arenaCount;i++){GDVars.gridX[i]=S(GDVars.arenaLoc[i].x-GDVars.arenaPtr.ox);GDVars.gridY[i]=S(GDVars.arenaLoc[i].y-GDVars.arenaPtr.oy);}}
        public static void GetArenaContents(sbyte x,sbyte y,out byte crIdx,out byte terrIdx)
        {
            crIdx=terrIdx=0;
            if(x>=0&&x<=49&&y>=0&&y<=24){crIdx=occupants[y,x];terrIdx=GDVars.arenaPtr.cells[y,x];}
        }
        public static byte OccupantAt(sbyte x,sbyte y){byte c,t;GetArenaContents(x,y,out c,out t);return c;}
        public static bool ViewCoordsValid(sbyte x,sbyte y){return x>=0&&x<=6&&y>=0&&y<=6;}
        public static byte CombatSlot(TCharRecord c){for(int i=1;i<=GDVars.arenaCount;i++)if(i<=ARENA_SLOTS&&ReferenceEquals(GDVars.creatureArr[i],c))return B(i);return 0;}
        public static sbyte CombatCellX(TCharRecord c){byte i=CombatSlot(c);return i==0?(sbyte)0:GDVars.arenaLoc[i].x;}
        public static sbyte CombatCellY(TCharRecord c){byte i=CombatSlot(c);return i==0?(sbyte)0:GDVars.arenaLoc[i].y;}
        public static byte GetIconSize(TCharRecord c){byte i=CombatSlot(c);return i==0?(byte)0:GDVars.arenaLoc[i].size;}
        public static bool CombatIconVisible(TCharRecord c,byte whole)
        {
            byte slot=CombatSlot(c);if(slot==0||GDVars.arenaLoc[slot].size==0)return false;
            bool result=true;
            for(int j=0;j<4;j++){sbyte dx,dy;if(Delta(GDVars.arenaLoc[slot].size,j,out dx,out dy)){result=ViewCoordsValid(S(GDVars.gridX[slot]+dx),S(GDVars.gridY[slot]+dy));if(result==(whole==0))return result;}}
            return result;
        }
        public static void UpdateCombatMapTile(sbyte x,sbyte y,byte crIdx)
        {
            sbyte ax=0,ay=0;byte t;
            if(crIdx==0){ax=S(x+GDVars.arenaPtr.ox);ay=S(y+GDVars.arenaPtr.oy);GetArenaContents(ax,ay,out crIdx,out t);}
            if(crIdx>0)
            {
                x=GDVars.gridX[crIdx];y=GDVars.gridY[crIdx];ax=S(x+GDVars.arenaPtr.ox);ay=S(y+GDVars.arenaPtr.oy);
                for(int i=0;i<4;i++){sbyte dx,dy;if(Delta(GDVars.arenaLoc[crIdx].size,i,out dx,out dy)&&ViewCoordsValid(S(x+dx),S(y+dy)))CopyTile(S((x+dx)*3),S((y+dy)*3),GDConsts.terrainInfo[GDVars.arenaPtr.cells[ay+dy,ax+dx]].tileId,0);}
            }
            else if(ViewCoordsValid(x,y))CopyTile(S(x*3),S(y*3),GDConsts.terrainInfo[GDVars.arenaPtr.cells[ay,ax]].tileId,0);
        }
        private static void DrawCellCreature(sbyte x,sbyte y)
        {
            byte c,t;GetArenaContents(x,y,out c,out t);
            if(c>0&&CombatIconVisible(GDVars.creatureArr[c],0))DrawSprite(GDVars.gridX[c],GDVars.gridY[c],GDVars.creatureArr[c].combatAddr.direction,0,GDVars.creatureArr[c].orderNumber);
        }
        public static void RedrawArenaCell(sbyte x,sbyte y){UpdateCombatMapTile(S(x-GDVars.arenaPtr.ox),S(y-GDVars.arenaPtr.oy),0);DrawCellCreature(x,y);}
        private static bool BuildViewBuffer(sbyte x,sbyte y,byte delta)
        {
            sbyte cx=S(GDVars.arenaPtr.ox+3),cy=S(GDVars.arenaPtr.oy+3);int d=delta==255?0:delta;
            sbyte minX=S(cx-d),maxX=S(cx+d),minY=S(cy-d),maxY=S(cy+d);
            if(delta!=255&&x>=minX&&x<=maxX&&y>=minY&&y<=maxY)return false;
            if(x<minX){while(x<cx&&cx>3)cx=S(cx-1);}else if(x>maxX){while(x>cx&&cx<46)cx=S(cx+1);}
            if(y<minY){while(y<cy&&cy>3)cy=S(cy-1);}else if(y>maxY){while(y>cy&&cy<21)cy=S(cy+1);}
            GDVars.arenaPtr.ox=S(cx-3);GDVars.arenaPtr.oy=S(cy-3);
            for(int j=0;j<7;j++)for(int i=0;i<7;i++)CopyTile(S(i*3),S(j*3),GDConsts.terrainInfo[GDVars.arenaPtr.cells[GDVars.arenaPtr.oy+j,GDVars.arenaPtr.ox+i]].tileId,1);
            CalcGridDeltas();return true;
        }
        public static void ScrollView(sbyte x,sbyte y,byte delta,byte dir)
        {
            sbyte tx=S(x+GDConsts.dirDX[dir]),ty=S(y+GDConsts.dirDY[dir]);
            if(BuildViewBuffer(tx,ty,delta))for(int i=1;i<=GDVars.arenaCount;i++){var p=GDVars.creatureArr[i];if(p.enabled!=0&&GDVars.arenaLoc[i].size>0&&CombatIconVisible(p,0))DrawSprite(GDVars.gridX[i],GDVars.gridY[i],p.combatAddr.direction,0,p.orderNumber);}
            RedrawArenaCell(x,y);
            if(!ViewCoordsValid(S(tx-GDVars.arenaPtr.ox),S(ty-GDVars.arenaPtr.oy))){if(tx>49)tx=49;if(tx<0)tx=0;if(ty>24)ty=24;if(ty<0)ty=0;}
            DrawCursorCell(tx,ty);Flush();
        }
        private static void DrawCursorCell(sbyte x,sbyte y)
        {
            if(previewOn){aimSet=false;EraseSpellPreview();}
            sbyte rx=S(x-GDVars.arenaPtr.ox),ry=S(y-GDVars.arenaPtr.oy);
            for(int i=0;i<4;i++){sbyte dx,dy;if(Delta(GDVars.arenaPtr.cursorSize,i,out dx,out dy)&&ViewCoordsValid(S(rx+dx),S(ry+dy))){CopyTile(S((rx+dx)*3),S((ry+dy)*3),GDConsts.terrainInfo[GDVars.arenaPtr.cells[y+dy,x+dx]].tileId,0);if(GDVars.arenaPtr.cursorOn)DrawSprite(S(rx+dx),S(ry+dy),0,0,25);}}
            DrawCellCreature(x,y);if(previewOn&&GDVars.arenaPtr.cursorOn)RestartSpellPreview(x,y);
        }
        public static void SetCreatureFacing(TCharRecord c,byte dir,byte frame,byte eraseOnly)
        {
            byte slot=CombatSlot(c);if(slot==0){c.combatAddr.direction=dir;return;}
            if(!CombatIconVisible(c,1))ScrollView(CombatCellX(c),CombatCellY(c),3,C_Dir.CENTER);
            if(c.combatAddr.direction/4!=dir/4||frame!=0||eraseOnly!=0)UpdateCombatMapTile(0,0,slot);
            if(dir!=c.combatAddr.direction)c.combatAddr.direction=dir;
            if(eraseOnly==0&&CombatIconVisible(c,1)){DrawSprite(GDVars.gridX[slot],GDVars.gridY[slot],dir,frame,c.orderNumber);Flush();}
        }
        public static void ProbeDirection(TCharRecord c,byte dir,out byte crIdx,out byte terrIdx)
        {
            crIdx=0;terrIdx=23;byte best=1,slot=CombatSlot(c);if(slot==0){terrIdx=0;return;}
            var loc=GDVars.arenaLoc[slot];
            for(int i=0;i<4;i++){sbyte dx,dy;if(!Delta(loc.size,i,out dx,out dy))continue;byte cr,t;GetArenaContents(S(loc.x+dx+GDConsts.dirDX[dir]),S(loc.y+dy+GDConsts.dirDY[dir]),out cr,out t);if(cr==slot)cr=0;if(cr>0)crIdx=cr;
                if(t==0||terrIdx==0)terrIdx=0;else if(t==C_Terr.STINK_CLOUD||terrIdx==C_Terr.STINK_CLOUD)terrIdx=C_Terr.STINK_CLOUD;else if(GDConsts.terrainInfo[t].cost>=best){best=GDConsts.terrainInfo[t].cost;terrIdx=t;}}
        }
        public static void RegisterDowned(TCharRecord c,byte x,byte y)
        {
            int free=0;for(int k=1;k<=GDVars.downedCount;k++)if(ReferenceEquals(GDVars.downed[k].cr,c))return;else if(GDVars.downed[k].cr==null&&free==0)free=k;
            if(free==0){if(GDVars.downedCount>=8)return;free=++GDVars.downedCount;}
            var rec=GDVars.downed[free];rec.terrain=GDVars.arenaPtr.cells[y,x];if(rec.terrain!=C_Terr.STINK_CLOUD)GDVars.arenaPtr.cells[y,x]=C_Terr.CORPSE;
            rec.cr=c;rec.x=S(x);rec.y=S(y);
        }
        public static void CreatureDowned(TCharRecord c)
        {
            AutoStop(0);
            if(GDVars.ifcState!=C_Ifc.COMBAT){Sound(GDConsts.soundIds.combat[C_Sound.DOWNED]);Pause();}
            else
            {
                for(int k=1;k<=GDVars.downedCount;k++)if(ReferenceEquals(GDVars.downed[k].cr,c)){AutoStop(10);return;}
                byte slot=CombatSlot(c);if(slot==0){AutoStop(10);return;}
                sbyte x=CombatCellX(c),y=CombatCellY(c);
                if(!CombatIconVisible(c,1))ScrollView(x,y,3,C_Dir.CENTER);
                UpdateCombatMapTile(0,0,slot);
                for(int k=0;k<4;k++){sbyte dx,dy;if(Delta(GDVars.arenaLoc[slot].size,k,out dx,out dy))Blit.BlitTileClipped((short)((GDVars.gridX[slot]+dx)*3),(short)((GDVars.gridY[slot]+dy)*3),0,5,GDVars.dsIconAnimation[24,0],GDVars.dsViewImageBufferPtr);}
                Flush();if(c.combatAddr.notPcNpc==0)RegisterDowned(c,unchecked((byte)x),unchecked((byte)y));
                Sound(GDConsts.soundIds.combat[C_Sound.DOWNED]);Pause();UpdateCombatMapTile(0,0,slot);GDVars.arenaLoc[slot].size=0;BuildArenaArrays();
                ScrollView(S(GDVars.arenaPtr.ox+3),S(GDVars.arenaPtr.oy+3),3,C_Dir.CENTER);
                c.combatAddr.initiative=0;c.combatAddr.movement=0;c.combatAddr.spellCast=0;c.combatAddr.guarding=0;
            }
            AutoStop(10);
        }
        public static bool IsValidCreatureLoc(TCharRecord c,byte x,byte y,bool moving)
        {
            if(GDVars.ifcState!=C_Ifc.COMBAT)return true;
            byte slot=CombatSlot(c);if(slot==0)return false;
            var loc=GDVars.arenaLoc[slot];loc.size=B(c.iconDimensions&127);loc.x=S(x);loc.y=S(y);
            byte cr,t;ProbeDirection(c,C_Dir.CENTER,out cr,out t);
            if(cr!=0||t==0||GDConsts.terrainInfo[t].cost==255){loc.size=0;return false;}
            if(moving&&c.combatAddr.notPcNpc==0)
            {
                for(int i=1;i<=GDVars.downedCount;i++)if(ReferenceEquals(GDVars.downed[i].cr,c)){var rec=GDVars.downed[i];if(rec.terrain!=C_Terr.CORPSE)t=rec.terrain;rec.cr=null;rec.x=rec.y=0;rec.terrain=0;}
                bool found=false;for(int i=1;i<=GDVars.downedCount;i++)if(GDVars.downed[i].cr!=null&&GDVars.downed[i].x==x&&GDVars.downed[i].y==y)found=true;
                if(!found)GDVars.arenaPtr.cells[y,x]=t;
            }
            BuildArenaArrays();return true;
        }
        public static void DisplayMapViewCentered(TCharRecord c,byte delta,bool cursor)
        {
            GDVars.arenaPtr.cursorOn=cursor;GDVars.arenaPtr.cursorSize=GetIconSize(c);ScrollView(CombatCellX(c),CombatCellY(c),delta,C_Dir.CENTER);GDVars.arenaPtr.cursorOn=false;GDVars.arenaPtr.cursorSize=1;
        }
        private static void CopyTarget(TCombatTarget src,TCombatTarget dst){dst.idx=src.idx;dst.dist=src.dist;dst.dir=src.dir;}
        public static byte CountTargets(TCharRecord c,byte range)
        {
            Pathing.BuildTargetList(CombatCellX(c),CombatCellY(c),range,255,GetIconSize(c),GDVars.arenaPtr);
            byte n=GDVars.targetCount,cnt=0;
            if(n>0){for(int i=1;i<=n;i++)if(CharHlp.EnemyTeam(c)==GDVars.creatureArr[GDVars.targetList[i].idx].hostile){cnt++;CopyTarget(GDVars.targetList[i],GDVars.targetList[cnt]);}n=cnt;GDVars.targetCount=cnt;}
            for(int i=1;i<=n;i++)GDVars.dsPotentialTargetCreature[i]=GDVars.targetList[i].idx;return n;
        }
        public static byte CalcDistance(TCharRecord c,TCharRecord other)
        {
            var saved=new TTargetList();for(int j=1;j<saved.Data.Length;j++)CopyTarget(GDVars.targetList[j],saved[j]);byte savedCount=GDVars.targetCount;
            GDVars.arenaPtr.distMode=1;Pathing.BuildTargetList(CombatCellX(c),CombatCellY(c),255,255,GetIconSize(c),GDVars.arenaPtr);GDVars.arenaPtr.distMode=0;
            int i=1;while(!ReferenceEquals(GDVars.creatureArr[GDVars.targetList[i].idx],other)&&i<GDVars.targetCount)i++;
            // Preserve the source's first-entry comparison even when targetCount is zero.
            byte result=ReferenceEquals(GDVars.creatureArr[GDVars.targetList[i].idx],other)?B(GDVars.targetList[i].dist>>1):(byte)255;
            for(int j=1;j<saved.Data.Length;j++)CopyTarget(saved[j],GDVars.targetList[j]);GDVars.targetCount=savedCount;return result;
        }
        private static bool InAimFootprint(sbyte x,sbyte y)
        {
            if(aimSet)for(int i=0;i<4;i++){sbyte dx,dy;if(Delta(aimSize,i,out dx,out dy)&&aimX+dx==x&&aimY+dy==y)return true;}return false;
        }
        private static void DrawCellTop(sbyte x,sbyte y,sbyte rx,sbyte ry){if(InAimFootprint(x,y))DrawSprite(rx,ry,0,0,25);DrawCellCreature(x,y);}
        private static void RepaintCell(sbyte x,sbyte y){sbyte rx=S(x-GDVars.arenaPtr.ox),ry=S(y-GDVars.arenaPtr.oy);if(ViewCoordsValid(rx,ry)){CopyTile(S(rx*3),S(ry*3),GDConsts.terrainInfo[GDVars.arenaPtr.cells[y,x]].tileId,0);DrawCellTop(x,y,rx,ry);}}
        private static void DrawPreviewArt(sbyte x,sbyte y,byte art,byte facing,byte half){sbyte rx=S(x-GDVars.arenaPtr.ox),ry=S(y-GDVars.arenaPtr.oy);if(ViewCoordsValid(rx,ry)){DrawSprite(rx,ry,facing,half,art);DrawCellCreature(x,y);}}
        private static void DrawAreaCell(int i,byte half)
        {
            if(previewShape==TAimShape.AimCloud){sbyte rx=S(areaX[i]-GDVars.arenaPtr.ox),ry=S(areaY[i]-GDVars.arenaPtr.oy);if(ViewCoordsValid(rx,ry)){CopyTile(S(rx*3),S(ry*3),GDConsts.terrainInfo[C_Terr.STINK_CLOUD].tileId,0);DrawCellTop(areaX[i],areaY[i],rx,ry);}}
            else DrawPreviewArt(areaX[i],areaY[i],areaArt,0,half);
        }
        private static void EraseSpellPreview(){for(int i=1;i<=areaCount;i++)if(areaRing[i]<ringsShown)RepaintCell(areaX[i],areaY[i]);ringsShown=0;}
        private static void AddAreaCell(sbyte x,sbyte y,byte ring){if(areaCount<64){areaCount++;areaX[areaCount]=x;areaY[areaCount]=y;areaRing[areaCount]=ring;if(ring>=ringCount)ringCount=B(ring+1);}}
        private static void RestartSpellPreview(sbyte x,sbyte y)
        {
            aimSet=true;aimX=x;aimY=y;aimSize=GDVars.arenaPtr.cursorSize;areaCount=ringCount=0;
            switch(previewShape)
            {
                case TAimShape.AimBolt:
                    AddAreaCell(x,y,0);var bolt=new TCellList();if(boltTracer==null)throw new NotSupportedException("CbtMap: Magic.TraceLightningBolt callback is not bound.");boltTracer(x,y,ref bolt);
                    for(int i=1;i<=bolt.count;i++)AddAreaCell(bolt.x[i],bolt.y[i],B((i+1)/2));break;
                case TAimShape.AimCloud:
                    for(int i=1;i<=4;i++){sbyte tx=S(x+GDConsts.dirDX[GDConsts.cloudDir[i]]),ty=S(y+GDConsts.dirDY[GDConsts.cloudDir[i]]);byte cr,t;GetArenaContents(tx,ty,out cr,out t);if(t>0&&GDConsts.terrainInfo[t].cost<255)AddAreaCell(tx,ty,(byte)(i>1?1:0));}break;
                default:
                    for(int ry=0;ry<7;ry++)for(int rx=0;rx<7;rx++){sbyte tx=S(GDVars.arenaPtr.ox+rx),ty=S(GDVars.arenaPtr.oy+ry);ushort dist=previewRadius;if(Pathing.WalkLine(x,y,ref tx,ref ty,ref dist,GDVars.arenaPtr))AddAreaCell(S(GDVars.arenaPtr.ox+rx),S(GDVars.arenaPtr.oy+ry),B(dist/2));}break;
            }
            frameNo=0;lastTick=unchecked(ReadTicks()-12);
        }
        public static void StartSpellPreview(TAimShape shape,byte radius,byte areaCell){StopSpellPreview();previewShape=shape;previewRadius=radius;areaArt=areaCell;aimSet=false;ringsShown=areaCount=0;previewOn=true;MenuCore.idleHook=TickSpellPreview;}
        public static void TickSpellPreview()
        {
            if(!previewOn||!aimSet)return;int now=ReadTicks();if(now>=lastTick&&unchecked(now-lastTick)<12)return;lastTick=now;byte ring=frameNo;
            if(ring<=ringCount){for(int i=1;i<=areaCount;i++)if(areaRing[i]+1==ring){RepaintCell(areaX[i],areaY[i]);DrawAreaCell(i,1);}else if(areaRing[i]==ring)DrawAreaCell(i,0);if(ring<ringCount)ringsShown=B(ring+1);}
            else if(ring==ringCount+3)EraseSpellPreview();else if(ring>=ringCount+5){frameNo=0;return;}
            frameNo=B(frameNo+1);Flush();
        }
        public static void StopSpellPreview(){if(previewOn){MenuCore.idleHook=null;aimSet=false;EraseSpellPreview();Flush();previewOn=false;}}
    }
}
