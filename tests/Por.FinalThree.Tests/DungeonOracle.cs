using System;using Por.Core;using static Por.Core.Types;
static partial class Tests
{
 static void DungeonOracle()
 {
  var rng=new Random(865);int[] kinds={0,1,3};
  for(int iteration=0;iteration<40;iteration++){
   var edges=new byte[16,16,8];for(int y=0;y<16;y++)for(int x=0;x<16;x++)for(int d=0;d<8;d+=2)edges[y,x,d]=(byte)kinds[rng.Next(3)];
   GDVars.gMapX=unchecked((sbyte)rng.Next(16));GDVars.gMapY=unchecked((sbyte)rng.Next(16));
   CombatSt.View3DGetBarrierTypeCall=(x,y,d)=>edges[y,x,d]==1?(byte)0:(byte)1;CombatSt.View3DGetWallTypeCall=(x,y,d)=>edges[y,x,d]==0?(byte)0:(byte)1;
   Func<int,int,int,int> side=(x,y,d)=>x>=0&&x<16&&y>=0&&y<16?edges[y,x,d]:y==GDVars.gMapY&&(d==2||d==6)?0:1;
   Func<int,int,int,int> wall=(x,y,d)=>side(x,y,d)|side(x+GDConsts.dirDX[d],y+GDConsts.dirDY[d],(d+4)%8);
   byte[,] expected=new byte[25,50];Action<int,int,int,int,int> plot=(dx,dy,x,y,t)=>{int ax=dx*6+dy*5+21+x,ay=dy*5+10+y;if(ax>=0&&ax<50&&ay>=0&&ay<25)expected[ay,ax]=(byte)(t+1);};
   for(int dy=-2;dy<=2;dy++)for(int dx=-6;dx<=6;dx++){
    int cx=GDVars.gMapX+dx,cy=GDVars.gMapY+dy,n=wall(cx,cy,0),w=wall(cx,cy,6),e=wall(cx,cy,2);Action<int,int,int> put=(x,y,t)=>plot(dx,dy,x,y,t);
    for(int y=2;y<=4;y++)for(int x=0;x<6;x++)put(x,y,22);
    if(w==1)for(int y=2;y<=4;y++){put(y-1,y,4);put(y,y,3);put(y+1,y,13);}else if(w==3){put(1,2,8);put(5,4,0);}
    put(3,0,n==1?5:22);put(4,0,n==1?5:22);put(3,1,n==1?10:22);put(4,1,n==1?10:22);
    bool open=wall(cx,cy-1,6)==0&&wall(cx-1,cy,0)==0;
    // NW table indexed north [open,solid,door], west [open,solid,door], openness.
    int c1=n==0?(w==0?22:w==3?13:open?0:13):w==0?(open?15:5):(open?18:2);
    int c2=n==0?22:n==3?17:5;
    int c3=w==0?(n==0?22:open?16:10):w==3?(open?20:7):(open?1:3);
    int c4=w==1?(n==0?13:n==3?21:6):(n==0?22:n==3?23:10);
    put(1,0,c1);put(2,0,c2);put(1,1,c3);put(2,1,c4);
    int kn=wall(cx,cy-1,2),ke=wall(cx+1,cy,0);open=kn==0&&ke==0;
    c1=n==0?(kn==1?4:22):n==3?15:5;
    c2=n==0?(kn==0?22:kn==3?(e==0&&ke!=0?24:1):(e==0&&ke!=0?11:3)):(e!=0?9:ke!=0?5:open?17:19);
    c3=n==0?22:n==3?16:10;
    c4=n==0?(kn==0?22:e!=0?4:ke==0?8:12):(e!=0?14:ke==0?23:10);
    put(5,0,c1);put(6,0,c2);put(5,1,c3);put(6,1,c4);
   }
   GDVars.arenaPtr=new TArena();CombatSt.DiceRollDiceCall=(n,s)=>1;CombatSt.BuildDungeonArena();
   for(int y=0;y<25;y++)for(int x=0;x<50;x++)Eq(expected[y,x],GDVars.arenaPtr.cells[y,x],"dungeon skew/corner clipping and overwrite");
  }
 }
 static void StOrchestration()
 {
  // Full CombatStart through real geometry/init/placement. Only asset/UI/time calls are recorded fakes.
  var log=new System.Collections.Generic.List<string>();CombatSt.PortraitFreeAnimationCall=(ref TAnimRec anim)=>{log.Add("anim");anim=null;};CombatSt.BlitFreePicCall=(ref TImgBuf pic)=>{log.Add("pic");pic=null;};CombatSt.TextRendClearBottomLineCall=()=>log.Add("clear");CombatSt.TextRendPauseOnlyCall=()=>log.Add("pause");CombatSt.TextRendDisplayStringCall=(x,y,col,s)=>log.Add(s);CombatSt.SoundDrvPlayMusicCall=id=>log.Add("music"+id);CombatSt.MapTilesFreeSecondSheetCall=()=>log.Add("sheetfree");CombatSt.MapTilesLoad24x24SetCall=(n,id,s)=>log.Add(n+":"+id+":"+s);CombatSt.GameIoDiscardPendingKeysCall=()=>log.Add("keys");CombatSt.CharHlpRecalcStatsCall=c=>{};CombatSt.BlitAllocPicCall=(ushort h,ushort w,byte p,byte n,ref TImgBuf pic)=>{Eq(24,h,"move icon height");Eq(3,w,"move icon width");log.Add("alloc");pic=new TImgBuf();};CombatSt.CharDispDisplayCombatInterfaceCall=()=>log.Add("interface");CombatSt.FxEngineTriggerEffectsWithConditionCall=(cond,c)=>{Eq(C_Cond.COMBAT_START,cond,"start condition");log.Add("effect"+c.name);};CombatSt.CbtActsCalcHostileHpPercentCall=()=>log.Add("hp");CombatSt.CbtMapCombatCellXCall=CbtMap.CombatCellX;CombatSt.CbtMapCombatCellYCall=CbtMap.CombatCellY;CombatSt.CbtMapBuildArenaArraysCall=CbtMap.BuildArenaArrays;CombatSt.CbtMapProbeDirectionCall=CbtMap.ProbeDirection;CombatSt.CbtMapRegisterDownedCall=CbtMap.RegisterDowned;CombatSt.CharHlpCountTeamsCall=CharHlp.CountTeams;
  GDVars.gMapX=8;GDVars.gMapY=8;GDVars.gFacing=2;GDVars.gLocType=C_Area.LOC_3D_MAP;GDVars.quickMagicOn=1;GDVars.zone1[C_Gbvm.MONSTER_DIST]=2;GDVars.zone1[C_Gbvm.MORALE_THRESHOLD]=255;GDVars.zone1[C_Gbvm.PARTY_SIZE]=2;GDVars.duelFlag=false;
  CombatSt.View3DGetBarrierTypeCall=(x,y,d)=>1;CombatSt.View3DGetWallTypeCall=(x,y,d)=>0;CombatSt.DiceRollDiceCall=(n,s)=>1;GDConsts.terrainInfo[23].tileId=22;GDConsts.terrainInfo[23].cost=1;
  var a=Creature();a.name="A";a.hostile=0;a.iconDimensions=1;var b=Creature();b.name="B";b.enabled=0;b.hostile=0;b.iconDimensions=1;a.nextCharAddr=b;var enemy=Creature();enemy.name="C";enemy.iconDimensions=1;b.nextCharAddr=enemy;GDVars.creatureList=a;GDVars.downedCount=9;GDVars.downed[1].cr=enemy;
  CombatSt.CombatStart();Eq(100,GDVars.zone1[C_Gbvm.MORALE_THRESHOLD],"threshold cap");Eq(1,GDVars.downedCount,"disabled party member gets fresh corpse record");Yes(GDVars.downed[1].cr==b,"downed inline record remains allocated");Eq(255,GDVars.dsPortraitCache[1].pictureId,"portrait invalidation");Yes(GDVars.quickMagic,"perbattle quick magic");Eq(C_Ifc.COMBAT,GDVars.ifcState,"combat interface");Yes(log.IndexOf("music"+C_Sound.MUSIC_BATTLE)<log.IndexOf("DungCom:0:24"),"music before map");Yes(log.IndexOf("interface")<log.IndexOf("effectA"),"interface before start effects");Yes(log[log.Count-1]=="hp","hp after effects");Eq(CbtMap.CombatCellX(a)-3,GDVars.arenaPtr.ox,"initial view offsetX");
 }
}
