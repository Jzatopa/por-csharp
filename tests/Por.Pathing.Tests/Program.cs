using System;using System.Collections.Generic;using System.Linq;using System.IO;using System.Text.Json;using Por.Core;
class SixTests
{
 static long checks;static Random random=new Random(32161);static void Yes(bool b,string s){checks++;if(!b)throw new Exception(s);}
 static List<(int x,int y,int dist)> Points(int x,int y,int tx,int ty){var list=new List<(int,int,int)>();int dx=Math.Abs(tx-x),dy=Math.Abs(ty-y),sx=Math.Sign(tx-x),sy=Math.Sign(ty-y),major=Math.Max(dx,dy);list.Add((x,y,0));for(int k=1;k<=major;k++){int m=dx>dy?(2*dy*k+dx)/(2*dx):(2*dx*k+dy)/(2*dy);list.Add((x+sx*(dx>dy?k:m),y+sy*(dx>dy?m:k),2*k+m));}return list;}
 static bool Toward(int x,int y,int tx,int ty,int d){if(x<0||x>49||tx<0||tx>49||y<0||y>24||ty<0||ty>24)return false;if(d>=8||x==tx&&y==ty)return true;int nx=x+GDConsts.dirDX[d],ny=y+GDConsts.dirDY[d],a=tx-nx,b=ty-ny;if(a==0&&b==0)return true;switch(d){case 0:return b<=-Math.Abs(a);case 1:return a>=0&&b<=0;case 2:return a>=Math.Abs(b);case 3:return a>=0&&b>=0;case 4:return b>=Math.Abs(a);case 5:return a<=0&&b>=0;case 6:return a<=-Math.Abs(b);default:return a<=0&&b<=0;}}
 static void Paths(){
  for(int i=-32768;i<=32767;i++)Yes(Pathing.SignOf((short)i)==Math.Sign(i),"sign");
  for(int t=0;t<5000;t++){int x=random.Next(50),y=random.Next(25),tx=random.Next(50),ty=random.Next(25);var points=Points(x,y,tx,ty);var q=new Types.TPathReq{sx=(short)x,sy=(short)y,ex=(short)tx,ey=(short)ty};Pathing.PathFind(ref q);Yes(q.cx==x&&q.cy==y&&q.dist==0,"pathinit");for(int i=1;i<points.Count;i++){Yes(Pathing.NextPathStep(ref q),"steptrue");var p=points[i];var prev=points[i-1];Yes(q.cx==p.x&&q.cy==p.y&&q.dist==p.dist,"analyticline");Yes(q.step==GDConsts.dirByDelta[p.y-prev.y+1,p.x-prev.x+1],"stepdir");}Yes(!Pathing.NextPathStep(ref q)&&q.step==8,"endpointfalse");for(int d=0;d<10;d++)Yes(Pathing.IsDirToward((sbyte)x,(sbyte)y,(sbyte)tx,(sbyte)ty,(byte)d)==Toward(x,y,tx,ty,d),"cone");}
  for(int size=0;size<256;size++)for(byte i=0;i<4;i++){sbyte dx=99,dy=99;bool ok=Pathing.GetIconTileDelta((byte)size,i,ref dx,ref dy);Yes(ok==(size>=1&&size<=4&&GDConsts.iconDelta[size,i].dx>=0),"icontile");if(size==0||size>4)Yes(dx==99&&dy==99,"invalidsizeoutputs");}
  var arena=new Types.TArena();GDVars.arenaPtr=arena;for(int t=0;t<1000;t++){int x=random.Next(50),y=random.Next(25),tx=random.Next(50),ty=random.Next(25),budget=random.Next(80);for(int yy=0;yy<25;yy++)for(int xx=0;xx<50;xx++)arena.cells[yy,xx]=(byte)(random.Next(6)==0?2:1);arena.distMode=(byte)(t%2);GDConsts.terrainInfo[1].clearance=0;GDConsts.terrainInfo[1].blocking=0;GDConsts.terrainInfo[2].clearance=0;GDConsts.terrainInfo[2].blocking=1;var points=Points(x,y,tx,ty);var last=points.Last();bool success=true;foreach(var p in points)if((arena.distMode==0&&arena.cells[p.y,p.x]==2)||p.dist>budget*2+1){success=false;last=p;break;}sbyte ax=(sbyte)tx,ay=(sbyte)ty;ushort dist=(ushort)budget;bool got=Pathing.WalkLine((sbyte)x,(sbyte)y,ref ax,ref ay,ref dist,arena);Yes(got==success&&dist==last.dist&&ax==(success?tx:last.x)&&ay==(success?ty:last.y),"walkline");}
  Routes(arena);Targets(arena);
 }
 static IEnumerable<(int x,int y)> Body(int x,int y,int size){int w=size==3||size==4?2:1,h=size==2||size==4?2:1;for(int yy=0;yy<h;yy++)for(int xx=0;xx<w;xx++)yield return(x+xx,y+yy);}
 static bool Adjacent(int x,int y,int size,int tx,int ty,int tsize){return Body(x,y,size).Any(a=>Body(tx,ty,tsize).Any(b=>Math.Max(Math.Abs(a.x-b.x),Math.Abs(a.y-b.y))<=1));}
 static void Routes(Types.TArena arena){
  GDConsts.terrainInfo[1].cost=1;GDConsts.terrainInfo[2].cost=255;
  for(int t=0;t<120;t++){
   int size=1+t%4,tsize=1+(t/4)%4,sx=5,sy=5,tx=t%12==0?6:10+random.Next(35),ty=t%12==0?5:3+random.Next(18);
   GDVars.arenaLoc[1].x=(sbyte)sx;GDVars.arenaLoc[1].y=(sbyte)sy;GDVars.arenaLoc[1].size=(byte)size;GDVars.arenaLoc[2].x=(sbyte)tx;GDVars.arenaLoc[2].y=(sbyte)ty;GDVars.arenaLoc[2].size=(byte)tsize;
   for(int y=0;y<25;y++)for(int x=0;x<50;x++)arena.cells[y,x]=(byte)(random.Next(5)==0?2:1);
   foreach(var b in Body(sx,sy,size))arena.cells[b.y,b.x]=1;
   Func<int,int,byte> occupant=(x,y)=>Body(sx,sy,size).Any(b=>b.x==x&&b.y==y)?(byte)1:Body(tx,ty,tsize).Any(b=>b.x==x&&b.y==y)?(byte)2:(byte)0;
   Func<int,int,bool> fits=(x,y)=>Body(x,y,size).All(b=>b.x>=0&&b.x<50&&b.y>=0&&b.y<25&&arena.cells[b.y,b.x]==1&&occupant(b.x,b.y)!=2);
   int[,] distances=new int[25,50];var queue=new Queue<(int x,int y)>();for(int y=0;y<25;y++)for(int x=0;x<50;x++){distances[y,x]=-1;if(fits(x,y)&&Adjacent(x,y,size,tx,ty,tsize)){distances[y,x]=0;queue.Enqueue((x,y));}}
   while(queue.Count>0){var v=queue.Dequeue();for(int yy=-1;yy<=1;yy++)for(int xx=-1;xx<=1;xx++){int nx=v.x+xx,ny=v.y+yy;if(nx>=0&&nx<50&&ny>=0&&ny<25&&distances[ny,nx]<0&&fits(nx,ny)){distances[ny,nx]=distances[v.y,v.x]+1;queue.Enqueue((nx,ny));}}}
   int heading=0;for(int d=0;d<8;d++)if(GDConsts.dirDX[d]==Math.Sign(tx-sx)&&GDConsts.dirDY[d]==Math.Sign(ty-sy))heading=d;
   int best=Int32.MaxValue,expected=-1;foreach(int turn in new[]{0,1,7,2,6,3,5,4}){int d=(heading+turn)%8,nx=sx+GDConsts.dirDX[d],ny=sy+GDConsts.dirDY[d];if(fits(nx,ny)&&distances[ny,nx]>=0&&distances[ny,nx]<best){best=distances[ny,nx];expected=d;}}
   if(Adjacent(sx,sy,size,tx,ty,tsize))expected=-1;byte result=222;bool got=Pathing.RouteStep(1,2,(sbyte x,sbyte y)=>occupant(x,y),ref result);Yes(got==(expected>=0)&&result==(expected<0?222:expected),"reverseBFSroute "+t);
  }
  GDVars.arenaLoc[1].size=0;byte direction=199;Yes(!Pathing.RouteStep(1,2,(x,y)=>0,ref direction)&&direction==199,"inactive route");
 }
 static void Targets(Types.TArena arena){
  arena.distMode=1;for(int y=0;y<25;y++)for(int x=0;x<50;x++)arena.cells[y,x]=1;
  for(int trial=0;trial<80;trial++){
   GDVars.arenaCount=(byte)(trial%8==0?80:24);int last=Math.Min((int)GDVars.arenaCount,Types.ARENA_SLOTS);for(int i=1;i<=Types.ARENA_SLOTS;i++){var a=GDVars.arenaLoc[i];a.size=(byte)random.Next(5);a.x=(sbyte)random.Next(48);a.y=(sbyte)random.Next(23);GDVars.creatureArr[i]=new Types.TCharRecord();}
   int x=random.Next(45),y=random.Next(20),size=1+random.Next(4),budget=random.Next(35),filter=trial%9;var expected=new List<(byte idx,int dist,int dir)>();
   for(int i=1;i<=last;i++){var a=GDVars.arenaLoc[i];if(a.size==0)continue;int best=255;var origin=(x:0,y:0);var target=(x:0,y:0);bool found=false;
    foreach(var b in Body(a.x,a.y,a.size))foreach(var mine in Body(x,y,size))if(Toward(mine.x,mine.y,b.x,b.y,filter)){int distance=Points(mine.x,mine.y,b.x,b.y).Last().dist;if(distance<=budget*2+1){found=true;if(distance<best){best=distance;origin=mine;target=b;}}}
    if(found){int d=filter<8?filter:Enumerable.Range(0,9).First(n=>Toward(origin.x,origin.y,target.x,target.y,n));expected.Add(((byte)i,best,d));}
   }
   for(int i=0;i<expected.Count;i++)for(int j=i+1;j<expected.Count;j++){var a=expected[i];var b=expected[j];if(b.dist<a.dist||b.dist==a.dist&&b.dir<a.dir&&b.dir%2<=a.dir%2){expected[i]=b;expected[j]=a;}}
   var identities=Enumerable.Range(1,Types.ARENA_SLOTS).Select(i=>GDVars.targetList[i]).ToArray();Pathing.BuildTargetList((sbyte)x,(sbyte)y,(ushort)budget,(byte)filter,(byte)size,arena);Yes(GDVars.targetCount==expected.Count,"targetcount");for(int i=0;i<expected.Count;i++){var got=GDVars.targetList[i+1];Yes(got.idx==expected[i].idx&&got.dist==expected[i].dist&&got.dir==expected[i].dir,"targetsorted");}for(int i=0;i<identities.Length;i++)Yes(Object.ReferenceEquals(identities[i],GDVars.targetList[i+1]),"targetrecordidentity");
   Pathing.TargetArea((sbyte)x,(sbyte)y,(byte)budget);Yes(GDVars.spellTargetCount==GDVars.targetCount,"areasize");for(int i=1;i<=GDVars.targetCount;i++)Yes(Object.ReferenceEquals(GDVars.dsTargetCreaturePtr[i],GDVars.creatureArr[GDVars.targetList[i].idx]),"areapointeridentity");
  }
  var longPath=new Types.TPathReq{sx=0,sy=0,ex=120,ey=120};Pathing.PathFind(ref longPath);for(int i=1;i<=120;i++){Yes(Pathing.NextPathStep(ref longPath)&&longPath.dist==unchecked((byte)(3*i)),"bytepathdistancewrap");}
  sbyte badX=-1,badY=1;ushort distanceOut=42;Yes(!Pathing.WalkLine(0,0,ref badX,ref badY,ref distanceOut,arena)&&badX==-1&&badY==1&&distanceOut==0,"invalidline");
 }
 static void Main(){Paths();Console.WriteLine("Pathing assertions="+checks+" failures=0");}
}
