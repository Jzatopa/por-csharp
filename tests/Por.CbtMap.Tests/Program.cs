// Self-authored source-derived oracles; no original DOS execution is claimed.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Por.Core;
using static Por.Core.Types;
static class Checks
{
 static long checks;static int tick,flush;static List<string> calls=new List<string>();
 static void Eq(long expected,long actual,string m){checks++;if(expected!=actual)throw new Exception(m+": expected "+expected+", actual "+actual);}
 static void Yes(bool v,string m){checks++;if(!v)throw new Exception(m);}
 static sbyte S(int n){return unchecked((sbyte)n);}static byte B(int n){return unchecked((byte)n);}
 static object Field(string n){return typeof(CbtMap).GetField(n,BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);}
 static object Invoke(string n,params object[] a){return typeof(CbtMap).GetMethod(n,BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,a);}
 static void Rig()
 {
  CbtMap.Flush=()=>flush++;CbtMap.StopSpellPreview();tick=100;flush=0;calls.Clear();
  CbtMap.CopyTile=(x,y,t,m)=>calls.Add("T:"+x+":"+y+":"+t+":"+m);
  CbtMap.DrawSprite=(x,y,d,f,a)=>calls.Add("S:"+x+":"+y+":"+d+":"+f+":"+a);
  CbtMap.ReadTicks=()=>tick;CbtMap.AutoStop=n=>calls.Add("A:"+n);CbtMap.Sound=n=>calls.Add("N:"+n);CbtMap.Pause=()=>calls.Add("P");CbtMap.boltTracer=null;
  GDVars.arenaPtr=new TArena{cursorSize=1};GDVars.arenaCount=0;GDVars.downedCount=0;GDVars.targetCount=0;GDVars.targetList=new TTargetList();GDVars.ifcState=C_Ifc.COMBAT;
  for(int i=0;i<GDVars.arenaLoc.Length;i++){GDVars.arenaLoc[i]=new TArenaLoc();GDVars.creatureArr[i]=null;GDVars.gridX[i]=GDVars.gridY[i]=99;}
  for(int i=0;i<GDVars.downed.Length;i++)GDVars.downed[i]=new TDownedRec();
  for(int y=0;y<25;y++)for(int x=0;x<50;x++)GDVars.arenaPtr.cells[y,x]=23;
  CbtMap.BuildArenaArrays();
 }
 static TCharRecord Add(int slot,int x,int y,int size=1,int team=0)
 {
  var c=new TCharRecord{enabled=1,iconDimensions=B(size),hostile=S(team),orderNumber=B(slot),combatAddr=new TCombatRec()};
  GDVars.creatureArr[slot]=c;GDVars.arenaLoc[slot]=new TArenaLoc{x=S(x),y=S(y),size=B(size)};GDVars.arenaCount=B(Math.Max(slot,GDVars.arenaCount));return c;
 }
 // Independent explicit footprints; not the production GetIconTileDelta table.
 static int[][] Foot(int size){switch(size){case 1:return new[]{new[]{0,0}};case 2:return new[]{new[]{0,0},new[]{0,1}};case 3:return new[]{new[]{0,0},new[]{1,0}};case 4:return new[]{new[]{0,0},new[]{1,0},new[]{0,1},new[]{1,1}};default:return new int[0][];}}
 static void Geometry()
 {
  Rig();for(int y=-128;y<128;y++)for(int x=-128;x<128;x++)Eq(x>=0&&x<7&&y>=0&&y<7?1:0,CbtMap.ViewCoordsValid(S(x),S(y))?1:0,"viewport bounds");
  for(int size=1;size<=4;size++)for(int y=0;y<24;y++)for(int x=0;x<49;x++)
  {
   GDVars.arenaCount=0;var c=Add(1,x,y,size);GDVars.arenaPtr.ox=S(x/2);GDVars.arenaPtr.oy=S(y/2);CbtMap.BuildArenaArrays();
   foreach(var p in Foot(size))Eq(1,CbtMap.OccupantAt(S(x+p[0]),S(y+p[1])),"occupied footprint");
   Eq(0,CbtMap.OccupantAt(S(x-1),S(y)),"clear previous footprint");Eq(x-GDVars.arenaPtr.ox,GDVars.gridX[1],"gridx");Eq(y-GDVars.arenaPtr.oy,GDVars.gridY[1],"gridy");Eq(1,CbtMap.CombatSlot(c),"slot");
  }
  Rig();var a=Add(1,5,5,4);Add(2,6,6);CbtMap.BuildArenaArrays();Eq(2,CbtMap.OccupantAt(6,6),"later overlap wins");Eq(1,CbtMap.OccupantAt(5,6),"overlap leaves other cells");
  for(int y=-128;y<128;y++)for(int x=-128;x<128;x++){byte cr,t;CbtMap.GetArenaContents(S(x),S(y),out cr,out t);Eq(x>=0&&x<50&&y>=0&&y<25?23:0,t,"arena bounds");}
  for(int size=0;size<=5;size++){GDVars.arenaLoc[1].size=B(size);for(int y=-4;y<=8;y++)for(int x=-4;x<=8;x++){GDVars.gridX[1]=S(x);GDVars.gridY[1]=S(y);var f=Foot(size);bool any=f.Any(p=>x+p[0]>=0&&x+p[0]<7&&y+p[1]>=0&&y+p[1]<7),all=f.All(p=>x+p[0]>=0&&x+p[0]<7&&y+p[1]>=0&&y+p[1]<7);Eq(size==0?0:size>4?1:any?1:0,CbtMap.CombatIconVisible(a,0)?1:0,"partial visibility");Eq(size==0?0:all?1:0,CbtMap.CombatIconVisible(a,1)?1:0,"whole visibility");}}
  var absent=new TCharRecord{combatAddr=new TCombatRec()};Eq(0,CbtMap.CombatSlot(absent),"absent slot");Eq(0,CbtMap.GetIconSize(absent),"absent size");Eq(0,CbtMap.CombatCellX(absent),"absent x");Eq(0,CbtMap.CombatCellY(absent),"absent y");CbtMap.SetCreatureFacing(absent,7,0,0);Eq(7,absent.combatAddr.direction,"off-map facing");
 }
 static void CombatEntry()
 {
  // Exercise placement -> occupancy -> renderer, the seam missed by isolated source tests.
  foreach(int count in new[]{0,1,2,16,ARENA_SLOTS,1})
  {
   Rig();GDVars.zone1=new TGbvmZone1();GDVars.gFacing=2;GDVars.zone1[C_Gbvm.MONSTER_DIST]=2;
   GDVars.creatureList=null;TCharRecord previous=null;
   for(int i=0;i<count;i++)
   {
    var c=new TCharRecord{enabled=1,iconDimensions=1,orderNumber=B(i+1),hostile=S(i%2),combatAddr=new TCombatRec()};
    if(previous==null)GDVars.creatureList=c;else previous.nextCharAddr=c;previous=c;
   }
   CombatSt.View3DGetBarrierTypeCall=(x,y,d)=>1;CombatSt.View3DGetWallTypeCall=(x,y,d)=>0;
   CombatSt.CharHlpCountTeamsCall=CharHlp.CountTeams;
   CombatSt.CbtMapBuildArenaArraysCall=CbtMap.BuildArenaArrays;CombatSt.CbtMapProbeDirectionCall=CbtMap.ProbeDirection;
   CombatSt.GameIoDiscardPendingKeysCall=()=>{};
   CombatSt.PlaceAllCreatures();Eq(count,GDVars.arenaCount,"placement count excludes next free slot");
   for(int i=1;i<=count;i++){Yes(GDVars.creatureArr[i]!=null,"counted slot populated");Eq(i,CbtMap.OccupantAt(GDVars.arenaLoc[i].x,GDVars.arenaLoc[i].y),"last slot included in occupancy");}
   // Force a rebuild: this used to dereference creatureArr[count+1].
   CbtMap.ScrollView(25,12,255,C_Dir.CENTER);Eq(1,flush,"placed arena redraw completes");
   if(count>0){var last=GDVars.creatureArr[count];Eq(count,CbtMap.CombatSlot(last),"last creature remains addressable");CbtMap.DisplayMapViewCentered(last,255,false);Yes(calls.Any(t=>t.StartsWith("S:")&&t.EndsWith(":"+count)),"last creature rendered");}
  }
  // A non-placeable party member still occupies a roster slot, but has no icon.
  Rig();GDVars.zone1=new TGbvmZone1();GDVars.gFacing=2;GDVars.zone1[C_Gbvm.MONSTER_DIST]=2;
  var unplaced=new TCharRecord{enabled=1,iconDimensions=1,combatAddr=new TCombatRec()};GDVars.creatureList=unplaced;
  byte oldCost=GDConsts.terrainInfo[23].cost;GDConsts.terrainInfo[23].cost=255;
  CombatSt.PlaceAllCreatures();Eq(1,GDVars.arenaCount,"unplaced PC retained without extra slot");Eq(0,GDVars.arenaLoc[1].size,"unplaced PC has no footprint");Yes(ReferenceEquals(unplaced,GDVars.creatureArr[1]),"unplaced PC identity retained");
  CbtMap.ScrollView(25,12,255,C_Dir.CENTER);Eq(1,flush,"unplaced PC redraw completes");GDConsts.terrainInfo[23].cost=oldCost;
 }
 static void ScrollAndDraw()
 {
  Rig();foreach(int delta in new[]{0,1,3,127,128,254,255})for(int oy=0;oy<=18;oy+=3)for(int ox=0;ox<=43;ox+=4)for(int ty=0;ty<25;ty+=4)for(int tx=0;tx<50;tx+=5)
  {
   GDVars.arenaPtr.ox=S(ox);GDVars.arenaPtr.oy=S(oy);calls.Clear();int d=delta==255?0:delta;int minX=S(ox+3-d),maxX=S(ox+3+d),minY=S(oy+3-d),maxY=S(oy+3+d);
   bool rebuild=delta==255||tx<minX||tx>maxX||ty<minY||ty>maxY;
   int ex=ox,ey=oy;
   if(rebuild){if(tx<minX)ex=Math.Min(ox,Math.Max(0,tx-3));else if(tx>maxX)ex=Math.Max(ox,Math.Min(43,tx-3));if(ty<minY)ey=Math.Min(oy,Math.Max(0,ty-3));else if(ty>maxY)ey=Math.Max(oy,Math.Min(18,ty-3));}
   bool actual=(bool)Invoke("BuildViewBuffer",S(tx),S(ty),B(delta));Eq(rebuild?1:0,actual?1:0,"rebuild choice");Eq(ex,GDVars.arenaPtr.ox,"originx");Eq(ey,GDVars.arenaPtr.oy,"originy");Eq(rebuild?49:0,calls.Count,"tile count");
   if(rebuild)for(int i=0;i<49;i++)Yes(calls[i]=="T:"+(i%7*3)+":"+(i/7*3)+":"+GDConsts.terrainInfo[23].tileId+":1","row-major 7x7 copy");
  }
  Rig();var c=Add(1,5,5,4);CbtMap.BuildArenaArrays();GDVars.arenaPtr.cursorOn=true;CbtMap.DisplayMapViewCentered(c,255,true);Eq(2,GDVars.arenaPtr.ox,"centered x");Eq(2,GDVars.arenaPtr.oy,"centered y");Yes(!GDVars.arenaPtr.cursorOn&&GDVars.arenaPtr.cursorSize==1,"cursor reset");Eq(4,calls.Count(t=>t.StartsWith("S:")&&t.EndsWith(":25")),"footprint cursor");Eq(1,flush,"scroll flush");
  calls.Clear();CbtMap.UpdateCombatMapTile(0,0,1);Eq(4,calls.Count,"erase entire footprint");
  calls.Clear();CbtMap.SetCreatureFacing(c,3,0,0);Eq(1,calls.Count,"same mirror side no erase");calls.Clear();CbtMap.SetCreatureFacing(c,4,0,0);Eq(5,calls.Count,"mirror crossing erases");calls.Clear();CbtMap.SetCreatureFacing(c,4,0,1);Eq(4,calls.Count,"erase-only no sprite");
  Rig();GDVars.arenaPtr.cursorOn=true;CbtMap.ScrollView(0,0,3,C_Dir.NORTHWEST);Eq(0,GDVars.arenaPtr.ox,"edge clampx");Eq(0,GDVars.arenaPtr.oy,"edge clampy");Yes(calls.Any(t=>t=="S:0:0:0:0:25"),"cursor clamped arena edge");
 }
 static void ProbeAndPlacement()
 {
  var random=new Random(9817);
  for(int n=0;n<1800;n++)
  {
   Rig();int size=1+n%4,x=n%3==0?0:10,y=n%5==0?0:10;var c=Add(1,x,y,size);Add(2,x+2,y);Add(3,x+1,y+2);
   for(int yy=0;yy<25;yy++)for(int xx=0;xx<50;xx++)GDVars.arenaPtr.cells[yy,xx]=B(random.Next(GDConsts.terrainInfo.Length));CbtMap.BuildArenaArrays();
   for(byte dir=0;dir<=8;dir++)
   {
    int ec=0,et=23,best=1;
    foreach(var f in Foot(size)){int xx=x+f[0]+GDConsts.dirDX[dir],yy=y+f[1]+GDConsts.dirDY[dir],t=xx<0||yy<0||xx>=50||yy>=25?0:GDVars.arenaPtr.cells[yy,xx];int cr=xx==x+2&&yy==y?2:xx==x+1&&yy==y+2?3:0;if(cr>0)ec=cr;if(t==0||et==0)et=0;else if(t==C_Terr.STINK_CLOUD||et==C_Terr.STINK_CLOUD)et=C_Terr.STINK_CLOUD;else if(GDConsts.terrainInfo[t].cost>=best){best=GDConsts.terrainInfo[t].cost;et=t;}}
    byte ac,at;CbtMap.ProbeDirection(c,dir,out ac,out at);Eq(ec,ac,"probe occupant");Eq(et,at,"probe dominance");
   }
  }
  Rig();var p=Add(1,1,1);Add(2,3,3);CbtMap.BuildArenaArrays();Yes(!CbtMap.IsValidCreatureLoc(p,3,3,false),"occupied rejected");Eq(0,GDVars.arenaLoc[1].size,"rejection clears size");Eq(1,CbtMap.OccupantAt(1,1),"source rejected placement retains old occupancy until rebuild");Yes(CbtMap.IsValidCreatureLoc(p,4,4,false),"free accepted");Eq(0,CbtMap.OccupantAt(1,1),"success clears old occupancy");Eq(1,CbtMap.OccupantAt(4,4),"new occupancy");
  Yes(!CbtMap.IsValidCreatureLoc(p,255,0,false),"unsigned destination narrows negative");GDVars.ifcState=0;var old=GDVars.arenaLoc[1].x;Yes(CbtMap.IsValidCreatureLoc(p,45,20,true),"noncombat allowed");Eq(old,GDVars.arenaLoc[1].x,"noncombat no mutation");
 }
 static void Corpses()
 {
  Rig();var chars=new List<TCharRecord>();for(int i=0;i<9;i++){var c=new TCharRecord();chars.Add(c);CbtMap.RegisterDowned(c,B(i),0);}Eq(8,GDVars.downedCount,"eight corpse cap");Eq(23,GDVars.arenaPtr.cells[0,8],"ninth does not alter terrain");CbtMap.RegisterDowned(chars[0],20,20);Eq(23,GDVars.arenaPtr.cells[20,20],"duplicate no terrain mutation");GDVars.downed[3].cr=null;GDVars.arenaPtr.cells[5,5]=C_Terr.STINK_CLOUD;var cloud=new TCharRecord();CbtMap.RegisterDowned(cloud,5,5);Eq(8,GDVars.downedCount,"reuse no growth");Yes(ReferenceEquals(cloud,GDVars.downed[3].cr),"first cleared slot reused");Eq(C_Terr.STINK_CLOUD,GDVars.arenaPtr.cells[5,5],"cloud remains");
  Rig();var a=Add(1,2,2);CbtMap.BuildArenaArrays();CbtMap.RegisterDowned(a,2,2);GDVars.arenaLoc[1].size=0;CbtMap.BuildArenaArrays();Yes(CbtMap.IsValidCreatureLoc(a,4,4,true),"revive moved");Yes(GDVars.downed[1].cr==null,"revive clears record");Eq(0,GDVars.downed[1].terrain,"revive clears saved terrain");Eq(23,GDVars.arenaPtr.cells[4,4],"source restore at destination");Eq(C_Terr.CORPSE,GDVars.arenaPtr.cells[2,2],"source old corpse remains when moved");
  Rig();a=Add(1,2,2);var b=new TCharRecord();CbtMap.RegisterDowned(a,2,2);CbtMap.RegisterDowned(b,2,2);GDVars.arenaLoc[1].size=0;CbtMap.BuildArenaArrays();Yes(CbtMap.IsValidCreatureLoc(a,2,2,true),"shared corpse revive");Eq(C_Terr.CORPSE,GDVars.arenaPtr.cells[2,2],"other corpse preserves marker");
  Rig();a=Add(1,2,2);CbtMap.BuildArenaArrays();a.combatAddr.initiative=12;a.combatAddr.movement=10;a.combatAddr.guarding=1;a.combatAddr.spellCast=2;CbtMap.CreatureDowned(a);Eq(0,GDVars.arenaLoc[1].size,"downed removes icon");Eq(0,CbtMap.OccupantAt(2,2),"downed rebuilds occupancy");Eq(1,GDVars.downedCount,"party corpse registered");Eq(0,a.combatAddr.initiative+a.combatAddr.movement+a.combatAddr.guarding+a.combatAddr.spellCast,"actions cleared");Yes(calls.First()=="A:0"&&calls.Last()=="A:10","sound timer balanced");calls.Clear();a.combatAddr.movement=7;CbtMap.CreatureDowned(a);Yes(calls.SequenceEqual(new[]{"A:0","A:10"}),"duplicate early sound restoration");Eq(7,a.combatAddr.movement,"duplicate leaves actions");
  calls.Clear();CbtMap.CreatureDowned(new TCharRecord());Yes(calls.SequenceEqual(new[]{"A:0","A:10"}),"offmap sound restoration");GDVars.ifcState=0;calls.Clear();CbtMap.CreatureDowned(a);Yes(calls.First()=="A:0"&&calls.Last()=="A:10"&&calls.Contains("P"),"noncombat sound pause");
 }
 static void Targets()
 {
  Rig();var a=Add(1,3,3);Add(2,4,3,1,0);var enemy=Add(3,5,3,1,1);Add(4,3,6,1,1);CbtMap.BuildArenaArrays();
  Eq(2,CbtMap.CountTargets(a,10),"enemy filtering");Eq(2,GDVars.targetCount,"filtered count");for(int i=1;i<=2;i++){Yes(GDVars.creatureArr[GDVars.targetList[i].idx].hostile==1,"enemy only");Eq(GDVars.targetList[i].idx,GDVars.dsPotentialTargetCreature[i],"potential indices");}
  for(int i=1;i<=72;i++)for(int j=i+1;j<=72;j++)Yes(!ReferenceEquals(GDVars.targetList[i],GDVars.targetList[j]),"compaction value copies never alias");
  for(int i=1;i<=72;i++){GDVars.targetList[i].idx=B(i%5);GDVars.targetList[i].dist=B(i*3);GDVars.targetList[i].dir=B(i+60);}GDVars.targetCount=51;
  var values=GDVars.targetList.Data.Select(t=>new[]{t.idx,t.dist,t.dir}).ToArray();var identities=(TCombatTarget[])GDVars.targetList.Data.Clone();
  Eq(2,CbtMap.CalcDistance(a,enemy),"distance halves stored distance");Eq(51,GDVars.targetCount,"count restored");Eq(0,GDVars.arenaPtr.distMode,"distance mode reset");
  for(int i=1;i<=72;i++){Yes(ReferenceEquals(identities[i],GDVars.targetList[i]),"list identity retained");Eq(values[i][0],GDVars.targetList[i].idx,"idx restored");Eq(values[i][1],GDVars.targetList[i].dist,"dist restored");Eq(values[i][2],GDVars.targetList[i].dir,"dir restored");}
  Eq(255,CbtMap.CalcDistance(a,new TCharRecord()),"absent distance");
  Rig();a=Add(1,3,3,0);GDVars.targetList[1].idx=0;GDVars.targetList[1].dist=42;Eq(21,CbtMap.CalcDistance(a,null),"documented source empty-list stale first-entry quirk");
 }
 static void Preview()
 {
  Rig();GDVars.arenaPtr.cursorOn=true;CbtMap.StartSpellPreview(TAimShape.AimBlast,0,17);Yes(MenuCore.idleHook!=null,"idle hook installed");CbtMap.TickSpellPreview();Eq(0,flush,"no aim no tick");CbtMap.ScrollView(3,3,3,C_Dir.CENTER);Eq(1,(byte)Field("areaCount"),"radius0 cursor only");calls.Clear();flush=0;
  CbtMap.TickSpellPreview();Yes(calls.SequenceEqual(new[]{"S:3:3:0:0:17"}),"first preview frame");Eq(1,flush,"first flush");calls.Clear();tick+=11;CbtMap.TickSpellPreview();Eq(0,calls.Count,"11cs no frame");tick++;CbtMap.TickSpellPreview();Yes(calls.Last()=="S:3:3:0:1:17","second half settles");
  for(int i=0;i<3;i++){tick+=12;CbtMap.TickSpellPreview();}Eq(0,(byte)Field("ringsShown"),"hold then erase");CbtMap.StopSpellPreview();Yes(MenuCore.idleHook==null,"stop clears hook");Yes(!(bool)Field("previewOn"),"stop off");
  Rig();GDVars.arenaPtr.cursorOn=true;CbtMap.StartSpellPreview(TAimShape.AimBlast,1,17);CbtMap.ScrollView(3,3,3,C_Dir.CENTER);var ax=(sbyte[])Field("areaX");var ay=(sbyte[])Field("areaY");var ar=(byte[])Field("areaRing");int count=(byte)Field("areaCount");var cells=new HashSet<string>();for(int i=1;i<=count;i++)cells.Add(ax[i]+","+ay[i]);Yes(cells.SetEquals(new[]{"3,3","2,3","4,3","3,2","3,4","2,2","4,2","2,4","4,4"}),"blast radius1 with doubled diagonal distance");CbtMap.TickSpellPreview();int f=flush;tick=0;CbtMap.TickSpellPreview();Eq(f+1,flush,"midnight wrap advances");CbtMap.StopSpellPreview();
  Rig();GDVars.arenaPtr.cursorOn=true;CbtMap.StartSpellPreview(TAimShape.AimCloud,8,17);GDVars.arenaPtr.cells[3,4]=0;CbtMap.ScrollView(3,3,3,C_Dir.CENTER);Eq(3,(byte)Field("areaCount"),"cloud skips terrain0");CbtMap.TickSpellPreview();Yes(calls.Contains("T:9:9:"+GDConsts.terrainInfo[C_Terr.STINK_CLOUD].tileId+":0"),"cloud terrain art");CbtMap.StopSpellPreview();
  Rig();GDVars.arenaPtr.cursorOn=true;CbtMap.StartSpellPreview(TAimShape.AimBolt,8,17);bool missing=false;try{CbtMap.ScrollView(3,3,3,C_Dir.CENTER);}catch(NotSupportedException){missing=true;}Yes(missing,"unbound lightning fails explicitly");CbtMap.StopSpellPreview();
  CbtMap.boltTracer=(sbyte x,sbyte y,ref TCellList list)=>{list=new TCellList{count=64};for(int i=1;i<=64;i++){list.x[i]=S(3+i%4);list.y[i]=3;}};
  CbtMap.StartSpellPreview(TAimShape.AimBolt,8,17);CbtMap.ScrollView(3,3,3,C_Dir.CENTER);Eq(64,(byte)Field("areaCount"),"bolt table caps64 including origin");ar=(byte[])Field("areaRing");for(int i=2;i<=64;i++)Eq(i/2,ar[i],"two bolt cells per ring");CbtMap.StopSpellPreview();
 }
 sealed class Video:IVideoHardware
 {
  public byte[] Memory=new byte[65536];public byte ReadByte(ushort s,ushort o){return Memory[o];}public void WriteByte(ushort s,ushort o,byte v){Memory[o]=v;}public void WritePort(ushort p,byte v){}public void Interrupt(byte n,TInterruptRegisters r){}
 }
 static void Pixels()
 {
  Rig();var video=new Video();VideoHardware.Host=video;VidVga.Install();GDVars.clipFlag=1;for(int i=0;i<200;i++)GDVars.fbRowOfs[i]=(ushort)(i*320);
  TImgBuf atlas=null,view=null;Blit.AllocPic(24,3,64,0,ref atlas);Blit.AllocPic(168,21,1,0,ref view);GDVars.ds2dMapTilePtr=atlas;GDVars.ds2dMapTilePtr2=atlas;GDVars.dsViewImageBufferPtr=view;Blit.FlushView();
  for(int y=0;y<25;y++)for(int x=0;x<50;x++)GDVars.arenaPtr.cells[y,x]=B(1+(x+y*3)%20);
  for(int f=0;f<64;f++)for(int y=0;y<24;y++)for(int x=0;x<24;x++)atlas.data[f*576+y*24+x]=B((f*7+x*3+y*5)%16);
  for(int i=0;i<26;i++)for(int h=0;h<2;h++){TImgBuf sprite=null;Blit.AllocPic(24,3,1,1,ref sprite);for(int p=0;p<576;p++){sprite.data[p]=p%24>=4&&p%24<20&&p/24>=4&&p/24<20?B((i+h+p%24)%16):PIC_KEY;}GDVars.dsIconAnimation[i,h]=sprite;}
  CbtMap.CopyTile=MapTiles.Copy2DTile;CbtMap.DrawSprite=MapTiles.DrawIconAnimSprite;CbtMap.Flush=Blit.FlushView;
  var atlasBefore=(byte[])atlas.data.Clone();var iconBefore=(byte[])GDVars.dsIconAnimation[1,0].data.Clone();
  var c=Add(1,10,10);c.combatAddr.direction=4;CbtMap.BuildArenaArrays();CbtMap.DisplayMapViewCentered(c,255,true);
  var expected=new byte[168*168];for(int y=0;y<168;y++)for(int x=0;x<168;x++){int terrain=GDVars.arenaPtr.cells[7+y/24,7+x/24],tile=GDConsts.terrainInfo[terrain].tileId&127;expected[y*168+x]=B((tile*7+x%24*3+y%24*5)%16);}
  // Source ordering: cursor first, then mirrored creature over its center cell.
  for(int y=4;y<20;y++)for(int x=4;x<20;x++)expected[(72+y)*168+72+x]=B((1+23-x)%16);
  for(int y=0;y<168;y++)for(int x=0;x<168;x++){Eq(expected[y*168+x],view.data[y*168+x],"actual renderer pixel "+x+","+y);Eq(expected[y*168+x],video.Memory[(y+8)*320+x+8],"hardware flush pixel "+x+","+y);}
  Yes(atlasBefore.SequenceEqual(atlas.data),"terrain atlas immutable");Yes(iconBefore.SequenceEqual(GDVars.dsIconAnimation[1,0].data),"mirrored icon source immutable");
  string dir="evidence/cbtmap-2026-10-21";Directory.CreateDirectory(dir);File.WriteAllBytes(dir+"/view-indexed.bin",view.data);using(var writer=new BinaryWriter(File.Create(dir+"/view.ppm"))){writer.Write(System.Text.Encoding.ASCII.GetBytes("P6\n168 168\n255\n"));foreach(byte p in view.data){int bright=(p&8)!=0?85:0;writer.Write(B(((p&4)!=0?170:0)+bright));writer.Write(B(p==6?85:((p&2)!=0?170:0)+bright));writer.Write(B(((p&1)!=0?170:0)+bright));}}
  CbtMap.CopyTile=(x,y,t,m)=>{};CbtMap.DrawSprite=(x,y,d,h,a)=>{};CbtMap.Flush=()=>flush++;
 }
 static void RealMagic()
 {
  Rig();var caster=Add(1,3,3);GDVars.curChar=caster;CbtMap.BuildArenaArrays();CbtMapBindings.Bind();
  GDVars.gTargetX=41;GDVars.gTargetY=19;GDVars.gMultiTarget=7;
  var bolt=new TCellList();CbtMap.boltTracer(4,3,ref bolt);Eq(8,bolt.count,"real Magic straight bolt eight cells");
  for(int i=1;i<=8;i++){Eq(4+i,bolt.x[i],"real bolt x");Eq(3,bolt.y[i],"real bolt y");}
  Eq(41,GDVars.gTargetX,"trace restores targetx");Eq(19,GDVars.gTargetY,"trace restores targety");Eq(7,GDVars.gMultiTarget,"trace restores multi");
  GDVars.arenaPtr.cursorOn=true;CbtMap.StartSpellPreview(TAimShape.AimBolt,8,19);CbtMap.ScrollView(4,3,3,C_Dir.CENTER);Eq(9,(byte)Field("areaCount"),"real tracer drives preview");CbtMap.TickSpellPreview();CbtMap.StopSpellPreview();
  // A blocking vertical wall cuts the blast before and beyond it, through real WalkLine.
  Rig();GDVars.arenaPtr.cursorOn=true;int wall=Array.FindIndex(GDConsts.terrainInfo,t=>t!=null&&t.blocking>GDConsts.terrainInfo[23].clearance);
  Yes(wall>0,"source terrain has blocking wall");for(int y=0;y<25;y++)GDVars.arenaPtr.cells[y,4]=B(wall);
  CbtMap.StartSpellPreview(TAimShape.AimBlast,8,17);CbtMap.ScrollView(3,3,3,C_Dir.CENTER);var xs=(sbyte[])Field("areaX");for(int i=1;i<=(byte)Field("areaCount");i++)Yes(xs[i]<4,"wall excludes blast cells behind it");CbtMap.StopSpellPreview();
  // The cloud's east/southeast/south neighbours have ring 1, with walls excluded.
  Rig();GDVars.arenaPtr.cursorOn=true;GDVars.arenaPtr.cells[4,4]=B(wall);CbtMap.StartSpellPreview(TAimShape.AimCloud,8,17);CbtMap.ScrollView(3,3,3,C_Dir.CENTER);Eq(3,(byte)Field("areaCount"),"cloud skips impassable wall");CbtMap.StopSpellPreview();
 }
 public sealed class RecordingCombat:ICombatServices
 {
  public List<string> Log=new List<string>();
  public TCastUI CombatTargetMenu{get{Log.Add("CombatTargetMenu");return null;}}public TCastUI NoncombatTargetMenu{get{Log.Add("NoncombatTargetMenu");return null;}}
  public void CombatStart(){Log.Add("CombatStart");}
  public void RoundStart(TCharRecord c){Log.Add("RoundStart");}
  public void SetNoncombatPalette(){Log.Add("SetNoncombatPalette");}
  public void TriggerEffects(byte condition,TCharRecord c){Log.Add("TriggerEffects");}
  public void ConsiderStinkingCloud(TCharRecord c){Log.Add("ConsiderStinkingCloud");}
  public void AdvanceTime(byte amount,sbyte unit){Log.Add("AdvanceTime");}
  public void CalcHostileHpPercent(){Log.Add("CalcHostileHpPercent");}
  public void DisplayMapCentered(TCharRecord c,byte margin,bool cursor){Log.Add("DisplayMapCentered");}
  public void RedrawCell(sbyte x,sbyte y){Log.Add("RedrawCell");}
  public sbyte CellX(TCharRecord c){Log.Add("CellX");return 13;}
  public sbyte CellY(TCharRecord c){Log.Add("CellY");return 13;}
  public byte Slot(TCharRecord c){Log.Add("Slot");return 13;}
  public void ScrollView(sbyte x,sbyte y,byte margin,byte direction){Log.Add("ScrollView");}
  public void UpdateTile(sbyte x,sbyte y,byte slot){Log.Add("UpdateTile");}
  public bool IsValidCreatureLoc(TCharRecord c,sbyte x,sbyte y,bool check){Log.Add("IsValidCreatureLoc");return true;}
  public void SetFacing(TCharRecord c,byte dir,byte a,byte b){Log.Add("SetFacing");}
  public void ProbeDirection(TCharRecord c,byte dir,out byte creature,out byte terrain){Log.Add("ProbeDirection");creature=0;terrain=0;}
  public void AiControl(TCharRecord c){Log.Add("AiControl");}
  public void ToggleQuickMagic(){Log.Add("ToggleQuickMagic");}
  public void AllToManual(){Log.Add("AllToManual");}
  public void CastSpellNC(byte spell,bool a,bool b,ref bool done){Log.Add("CastSpellNC");done=true;}
  public void ViewCharacter(ref bool done){Log.Add("ViewCharacter");done=true;}
  public void UseItem(ref bool done){Log.Add("UseItem");done=true;}
  public void SetCurAttacksX2(TCharRecord c){Log.Add("SetCurAttacksX2");}
  public void AimMenu(TCharRecord c,byte spell,byte mode,byte range,ref bool done,ref object target){Log.Add("AimMenu");done=true;target=this;}
  public void CastSpellCombat(byte spell,byte item,ref bool done){Log.Add("CastSpellCombat");done=true;}
  public void TurnUndead(TCharRecord c){Log.Add("TurnUndead");}
  public byte TurnLevel(TCharRecord c){Log.Add("TurnLevel");return 13;}
  public bool StingVictoryCheat(){Log.Add("StingVictoryCheat");return true;}
  public bool FleeAttempt(TCharRecord c){Log.Add("FleeAttempt");return true;}
  public void MoveOpportunityAttacks(TCharRecord c,byte direction){Log.Add("MoveOpportunityAttacks");}
  public void MoveCreature(TCharRecord c,byte direction){Log.Add("MoveCreature");}
  public bool CheckAttackAlly(TCharRecord a,TCharRecord d){Log.Add("CheckAttackAlly");return true;}
  public bool SweepAttempt(TCharRecord a,TCharRecord d){Log.Add("SweepAttempt");return true;}
  public void NoteAttackDirection(TCharRecord a,TCharRecord d){Log.Add("NoteAttackDirection");}
  public void CombatAttack(TCharRecord a,TCharRecord d,bool free,object missile,ref bool done){Log.Add("CombatAttack");done=true;}
 }
 static void Bindings()
 {
  Rig();var c=Add(1,4,5);CbtMap.BuildArenaArrays();Combat.Services=null;CbtMapBindings.Bind();var first=Combat.Services;CbtMapBindings.Bind();Yes(ReferenceEquals(first,Combat.Services),"idempotent combat binding");Eq(4,CharDisp.CombatMap.CombatCellX(c),"CharDisp real map");Eq(5,CbtActs.CombatMap.CombatCellY(c),"CbtActs real map");Eq(1,Combat.Services.Slot(c),"Combat real map");byte cr=99,t=99;CbtActs.CombatMap.GetArenaContents(4,5,ref cr,ref t);Eq(1,cr,"ref/out adapter");Eq(23,t,"terrain adapter");bool missing=false;try{Combat.Services.CombatStart();}catch(NotSupportedException){missing=true;}Yes(missing,"non-map missing still explicit");
  var old=new RecordingCombat();Combat.Services=old;CbtMapBindings.Bind();bool done=false;object target=null;Combat.Services.CombatStart();Combat.Services.AimMenu(c,1,2,3,ref done,ref target);Yes(done&&ReferenceEquals(old,target),"nonmap ref arguments forwarded");Yes(old.Log.SequenceEqual(new[]{"CombatStart","AimMenu"}),"existing unrelated services retained");Eq(13,Combat.Services.TurnLevel(c),"nonmap return value forwarded");
  CbtMap.boltTracer=(sbyte x,sbyte y,ref TCellList list)=>{};var tracer=CbtMap.boltTracer;CbtMapBindings.Bind();Yes(ReferenceEquals(tracer,CbtMap.boltTracer),"explicit custom tracer retained");
 }
 static int Main(string[] args)
 {
  try{Action<string,Action> run=(n,a)=>{if(args.Length==0||args.Contains(n)){long start=checks;a();Console.WriteLine(n+": "+(checks-start)+" assertions PASS");}};run("CombatEntry",CombatEntry);run("Geometry",Geometry);run("Scroll",ScrollAndDraw);run("Probe",ProbeAndPlacement);run("Corpses",Corpses);run("Targets",Targets);run("Preview",Preview);run("Pixels",Pixels);run("Bindings",Bindings);run("RealMagic",RealMagic);Console.WriteLine("TOTAL "+checks+" assertions PASS");return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}
 }
}
