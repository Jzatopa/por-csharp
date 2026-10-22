// Pinned Pascal transcription; regenerate with scripts/translate-final-combat.py CombatSt
using System;using System.Linq;using static Por.Core.Types;
namespace Por.Core{public static partial class CombatSt{
private static byte terrainChar=default(byte);
private static Byte[,,,] quadFree=new byte[(1)+1,(3)+1,(5)+1,(10)+1];
private static sbyte[] placeX=new sbyte[(1)+1];
private static sbyte[] placeY=new sbyte[(1)+1];
private static sbyte[] teamRows=new sbyte[(1)+1];
private static sbyte[] teamDir=new sbyte[(1)+1];
public static void SprinkleRareFeatures(){
sbyte col=default(sbyte);
sbyte row=default(sbyte);
byte roll=default(byte);

{
{int __g2=49;for(int __g1=0;__g1<=__g2;__g1++){col=unchecked((sbyte)(__g1));{int __g4=24;for(int __g3=0;__g3<=__g4;__g3++){row=unchecked((sbyte)(__g3));if((GDConsts.terrainInfo[GDVars.arenaPtr.cells[row,col]].tileId==22)){
roll=DiceRollDiceCall(unchecked((byte)(1)),unchecked((byte)(100)));
{var __g5=roll;if((__g5>=1&&__g5<=97)){;}else if(__g5==98){GDVars.arenaPtr.cells[row,col]=unchecked((byte)(26));}else if(__g5==99){GDVars.arenaPtr.cells[row,col]=unchecked((byte)(27));}else if(__g5==100){if((GDVars.eclScriptId!=10)){if((DiceRollDiceCall(unchecked((byte)(1)),unchecked((byte)(100)))==1)){var __g6=DiceRollDiceCall(unchecked((byte)(1)),unchecked((byte)(10)));if((__g6>=1&&__g6<=6)){GDVars.arenaPtr.cells[row,col]=unchecked((byte)(28));}else if((__g6>=7&&__g6<=10)){GDVars.arenaPtr.cells[row,col]=unchecked((byte)(29));}}}}}
}}}}}
}
}
public static byte WallBetween(sbyte x,sbyte y,byte dir){
byte __result_WallBetween=default(byte);
byte k1=default(byte);
byte k2=default(byte);
byte opp=default(byte);
byte GetWallKind(sbyte __p_px,sbyte __p_py,byte __p_pdir){
byte __result_GetWallKind=default(byte);


{
if((((((__p_px>=0))&&((__p_px<=15)))&&((__p_py>=0)))&&((__p_py<=15)))){
if((View3DGetBarrierTypeCall(__p_px,__p_py,__p_pdir)==0)){__result_GetWallKind=unchecked((byte)(1));}else {if((View3DGetWallTypeCall(__p_px,__p_py,__p_pdir)==0)){__result_GetWallKind=unchecked((byte)(0));}else {__result_GetWallKind=unchecked((byte)(3));}}
}else {
if((((__p_py==GDVars.gMapY))&&((((__p_pdir==C_Dir.EAST))||((__p_pdir==C_Dir.WEST)))))){__result_GetWallKind=unchecked((byte)(0));}else {__result_GetWallKind=unchecked((byte)(1));}
}
return __result_GetWallKind;
}
}
{
opp=unchecked((byte)((((dir+4))%8)));
k1=GetWallKind(x,y,dir);
k2=GetWallKind(unchecked((sbyte)((x+GDConsts.dirDX[dir]))),unchecked((sbyte)((y+GDConsts.dirDY[dir]))),opp);
__result_WallBetween=unchecked((byte)((k1|k2)));
return __result_WallBetween;
}
}
public static void BuildDungeonArena(){
sbyte dx=default(sbyte);
sbyte dy=default(sbyte);
sbyte cellX=default(sbyte);
sbyte cellY=default(sbyte);
byte wallN=default(byte);
byte wallW=default(byte);
byte wallE=default(byte);
void PlotCell(byte __p_i,byte __p_j,byte __p_t){
sbyte x=default(sbyte);
sbyte y=default(sbyte);

{
x=unchecked((sbyte)(((((dx*6)+(dy*5))+21)+__p_i)));
y=unchecked((sbyte)((((dy*5)+10)+__p_j)));
if((((((x>=0))&&((x<=49)))&&((y>=0)))&&((y<=24)))){GDVars.arenaPtr.cells[y,x]=unchecked((byte)((__p_t+1)));}
}
}
void DrawFloorWestWall(){
byte c=default(byte);
byte r=default(byte);

{
{int __g8=4;for(int __g7=2;__g7<=__g8;__g7++){r=unchecked((byte)(__g7));{int __g10=5;for(int __g9=0;__g9<=__g10;__g9++){c=unchecked((byte)(__g9));PlotCell(c,r,unchecked((byte)(22)));}}}}
if((wallW==1)){
{int __g12=4;for(int __g11=2;__g11<=__g12;__g11++){r=unchecked((byte)(__g11));{
PlotCell(unchecked((byte)((r-1))),r,unchecked((byte)(4)));
PlotCell(r,r,unchecked((byte)(3)));
PlotCell(unchecked((byte)((r+1))),r,unchecked((byte)(13)));
}}}
}else {if((wallW==3)){
PlotCell(unchecked((byte)(1)),unchecked((byte)(2)),unchecked((byte)(8)));
PlotCell(unchecked((byte)(5)),unchecked((byte)(4)),unchecked((byte)(0)));
}}
}
}
void DrawNorthWall(){


{
if((wallN==1)){
PlotCell(unchecked((byte)(3)),unchecked((byte)(0)),unchecked((byte)(5)));
PlotCell(unchecked((byte)(4)),unchecked((byte)(0)),unchecked((byte)(5)));
PlotCell(unchecked((byte)(3)),unchecked((byte)(1)),unchecked((byte)(10)));
PlotCell(unchecked((byte)(4)),unchecked((byte)(1)),unchecked((byte)(10)));
}else {
PlotCell(unchecked((byte)(3)),unchecked((byte)(0)),unchecked((byte)(22)));
PlotCell(unchecked((byte)(4)),unchecked((byte)(0)),unchecked((byte)(22)));
PlotCell(unchecked((byte)(3)),unchecked((byte)(1)),unchecked((byte)(22)));
PlotCell(unchecked((byte)(4)),unchecked((byte)(1)),unchecked((byte)(22)));
}
}
}
void FillCornerNW(){
byte k=default(byte);
byte c1=default(byte);
byte c3=default(byte);
byte c2=default(byte);
byte c4=default(byte);
byte open=default(byte);

{
if((((CombatSt.WallBetween(cellX,unchecked((sbyte)((cellY-1))),unchecked((byte)(C_Dir.WEST)))==0))&&((CombatSt.WallBetween(unchecked((sbyte)((cellX-1))),cellY,unchecked((byte)(C_Dir.NORTH)))==0)))){open=unchecked((byte)(1));}else {open=unchecked((byte)(0));}
{int __g14=4;for(int __g13=1;__g13<=__g14;__g13++){k=unchecked((byte)(__g13));{var __g15=k;if(__g15==1){var __g16=wallN;if(__g16==0){var __g17=wallW;if(__g17==0){c1=unchecked((byte)(22));}else if(__g17==3){c1=unchecked((byte)(13));}else if(__g17==1){if((open!=0)){c1=unchecked((byte)(0));}else {c1=unchecked((byte)(13));}}}else if(__g16==3||__g16==1){if((wallW==0)){
if((open!=0)){c1=unchecked((byte)(15));}else {c1=unchecked((byte)(5));}
}else {
if((open!=0)){c1=unchecked((byte)(18));}else {c1=unchecked((byte)(2));}
}}}else if(__g15==2){var __g18=wallN;if(__g18==0){c2=unchecked((byte)(22));}else if(__g18==3){c2=unchecked((byte)(17));}else if(__g18==1){c2=unchecked((byte)(5));}}else if(__g15==3){var __g19=wallW;if(__g19==0){if((wallN==0)){c3=unchecked((byte)(22));}else {if((open!=0)){c3=unchecked((byte)(16));}else {c3=unchecked((byte)(10));}}}else if(__g19==3){if((open!=0)){c3=unchecked((byte)(20));}else {c3=unchecked((byte)(7));}}else if(__g19==1){if((open!=0)){c3=unchecked((byte)(1));}else {c3=unchecked((byte)(3));}}}else if(__g15==4){var __g20=wallW;if(__g20==0||__g20==3){var __g21=wallN;if(__g21==0){c4=unchecked((byte)(22));}else if(__g21==3){c4=unchecked((byte)(23));}else if(__g21==1){c4=unchecked((byte)(10));}}else if(__g20==1){var __g22=wallN;if(__g22==0){c4=unchecked((byte)(13));}else if(__g22==3){c4=unchecked((byte)(21));}else if(__g22==1){c4=unchecked((byte)(6));}}}}}}
PlotCell(unchecked((byte)(1)),unchecked((byte)(0)),c1);
PlotCell(unchecked((byte)(2)),unchecked((byte)(0)),c2);
PlotCell(unchecked((byte)(1)),unchecked((byte)(1)),c3);
PlotCell(unchecked((byte)(2)),unchecked((byte)(1)),c4);
}
}
void FillCornerNE(){
byte k=default(byte);
byte c1=default(byte);
byte c3=default(byte);
byte c2=default(byte);
byte c4=default(byte);
byte open=default(byte);
byte kN=default(byte);
byte kE=default(byte);

{
kN=CombatSt.WallBetween(cellX,unchecked((sbyte)((cellY-1))),unchecked((byte)(C_Dir.EAST)));
kE=CombatSt.WallBetween(unchecked((sbyte)((cellX+1))),cellY,unchecked((byte)(C_Dir.NORTH)));
if((((kN==0))&&((kE==0)))){open=unchecked((byte)(1));}else {open=unchecked((byte)(0));}
{int __g24=4;for(int __g23=1;__g23<=__g24;__g23++){k=unchecked((byte)(__g23));{var __g25=k;if(__g25==1){var __g26=wallN;if(__g26==0){if((kN==1)){c1=unchecked((byte)(4));}else {c1=unchecked((byte)(22));}}else if(__g26==3){c1=unchecked((byte)(15));}else if(__g26==1){c1=unchecked((byte)(5));}}else if(__g25==2){if((wallN==0)){
{var __g27=kN;if(__g27==0){c2=unchecked((byte)(22));}else if(__g27==3){if((((wallE==0))&&((kE!=0)))){c2=unchecked((byte)(24));}else {c2=unchecked((byte)(1));}}else if(__g27==1){if((((wallE==0))&&((kE!=0)))){c2=unchecked((byte)(11));}else {c2=unchecked((byte)(3));}}}
}else {
if((wallE!=0)){c2=unchecked((byte)(9));}else {if((kE!=0)){c2=unchecked((byte)(5));}else {if((open!=0)){c2=unchecked((byte)(17));}else {c2=unchecked((byte)(19));}}}
}}else if(__g25==3){var __g28=wallN;if(__g28==0){c3=unchecked((byte)(22));}else if(__g28==3){c3=unchecked((byte)(16));}else if(__g28==1){c3=unchecked((byte)(10));}}else if(__g25==4){if((wallN==0)){
if((kN==0)){c4=unchecked((byte)(22));}else {if((wallE!=0)){c4=unchecked((byte)(4));}else {if((kE==0)){c4=unchecked((byte)(8));}else {c4=unchecked((byte)(12));}}}
}else {
if((wallE!=0)){c4=unchecked((byte)(14));}else {if((kE==0)){c4=unchecked((byte)(23));}else {c4=unchecked((byte)(10));}}
}}}}}
PlotCell(unchecked((byte)(5)),unchecked((byte)(0)),c1);
PlotCell(unchecked((byte)(6)),unchecked((byte)(0)),c2);
PlotCell(unchecked((byte)(5)),unchecked((byte)(1)),c3);
PlotCell(unchecked((byte)(6)),unchecked((byte)(1)),c4);
}
}
{
{int __g30=2;for(int __g29=-(2);__g29<=__g30;__g29++){dy=unchecked((sbyte)(__g29));{int __g32=6;for(int __g31=-(6);__g31<=__g32;__g31++){dx=unchecked((sbyte)(__g31));{
cellX=unchecked((sbyte)((GDVars.gMapX+dx)));
cellY=unchecked((sbyte)((GDVars.gMapY+dy)));
wallW=CombatSt.WallBetween(cellX,cellY,unchecked((byte)(C_Dir.WEST)));
wallN=CombatSt.WallBetween(cellX,cellY,unchecked((byte)(C_Dir.NORTH)));
wallE=CombatSt.WallBetween(cellX,cellY,unchecked((byte)(C_Dir.EAST)));
DrawFloorWestWall();
DrawNorthWall();
FillCornerNW();
FillCornerNE();
}}}}}
CombatSt.SprinkleRareFeatures();
}
}
public static byte TerrainClassOf(){
byte __result_TerrainClassOf=default(byte);
byte cls=default(byte);

{
{var __g33=terrainChar;if((__g33>=1&&__g33<=2)||(__g33>=40&&__g33<=41)||(__g33>=79&&__g33<=86)||(__g33>=91&&__g33<=92)||__g33==43||__g33==241){cls=unchecked((byte)(1));}else if((__g33>=168&&__g33<=177)){cls=unchecked((byte)(33));}else if(__g33==185||__g33==187||(__g33>=188&&__g33<=192)){cls=unchecked((byte)(17));}else if((__g33>=3&&__g33<=4)||__g33==5||(__g33>=6&&__g33<=7)||(__g33>=8&&__g33<=9)||(__g33>=19&&__g33<=21)||(__g33>=23&&__g33<=25)){cls=unchecked((byte)(3));}else if((__g33>=10&&__g33<=18)||__g33==22||(__g33>=26&&__g33<=36)||(__g33>=201&&__g33<=208)){cls=unchecked((byte)(2));}else if((__g33>=209&&__g33<=213)){cls=unchecked((byte)(18));}else if(__g33==37||__g33==57||__g33==64||(__g33>=154&&__g33<=158)||(__g33>=164&&__g33<=165)||__g33==180||__g33==243||(__g33>=239&&__g33<=240)||__g33==250||__g33==252){cls=unchecked((byte)(4));}else if(__g33==39||__g33==42||__g33==44||__g33==153||__g33==159){cls=unchecked((byte)(5));}else if((__g33>=45&&__g33<=46)||(__g33>=51&&__g33<=56)||(__g33>=67&&__g33<=77)||(__g33>=93&&__g33<=97)){cls=unchecked((byte)(68));}else if((__g33>=181&&__g33<=184)||__g33==186||(__g33>=193&&__g33<=200)){cls=unchecked((byte)(20));}else if((__g33>=47&&__g33<=50)||(__g33>=98&&__g33<=101)||__g33==242){cls=unchecked((byte)(64));}else if(__g33==38){cls=unchecked((byte)(65));}else if((__g33>=178&&__g33<=179)){cls=unchecked((byte)(96));}else if((__g33>=58&&__g33<=60)||(__g33>=65&&__g33<=66)){cls=unchecked((byte)(72));}else if((__g33>=61&&__g33<=63)||(__g33>=121&&__g33<=124)||(__g33>=135&&__g33<=137)||__g33==139||__g33==141||(__g33>=144&&__g33<=147)||__g33==149||(__g33>=151&&__g33<=152)||(__g33>=160&&__g33<=162)||(__g33>=166&&__g33<=167)||(__g33>=214&&__g33<=216)||__g33==238){cls=unchecked((byte)(8));}else if((__g33>=87&&__g33<=90)||__g33==163||__g33==224){cls=unchecked((byte)(136));}else if(__g33==125||__g33==134||__g33==138||__g33==140||__g33==142||__g33==143||__g33==148||__g33==150){cls=unchecked((byte)(9));}else if((__g33>=126&&__g33<=133)||(__g33>=229&&__g33<=231)){cls=unchecked((byte)(40));}else if((__g33>=217&&__g33<=223)){cls=unchecked((byte)(24));}else if(__g33==78||__g33==245||(__g33>=232&&__g33<=233)){cls=unchecked((byte)(32));}else if(__g33==109||__g33==244||__g33==246){cls=unchecked((byte)(128));}else if((__g33>=110&&__g33<=113)||(__g33>=115&&__g33<=120)){cls=unchecked((byte)(129));}else if((__g33>=102&&__g33<=108)||__g33==114){cls=unchecked((byte)(144));}else if((__g33>=225&&__g33<=228)||(__g33>=234&&__g33<=237)){cls=unchecked((byte)(160));}else{cls=unchecked((byte)(1));}}
if(((cls&128)!=0)){if((GDVars.zone0.yarashMissionDone==255)){
cls=unchecked((byte)((cls-128)));
cls=unchecked((byte)((cls|1)));
}}
__result_TerrainClassOf=cls;
return __result_TerrainClassOf;
}
}
public static void PlaceRiver(){
byte thresh=default(byte);
byte fordCnt=default(byte);
sbyte startCol=default(sbyte);
sbyte col=default(sbyte);
sbyte row=default(sbyte);
void PlotFord(sbyte __p_r,sbyte __p_c){


{
GDVars.arenaPtr.cells[__p_r,__p_c]=unchecked((byte)(52));
GDVars.arenaPtr.cells[__p_r,(__p_c+1)]=unchecked((byte)(53));
fordCnt=unchecked((byte)((fordCnt+1)));
}
}
{
thresh=unchecked((byte)(0));
fordCnt=unchecked((byte)(0));
if(((CombatSt.TerrainClassOf()&32)!=0)){thresh=unchecked((byte)(35));}
if(((CombatSt.TerrainClassOf()&16)!=0)){thresh=unchecked((byte)(75));}
if((DiceRollDiceCall(unchecked((byte)(1)),unchecked((byte)(100)))>thresh)){return;}
col=unchecked((sbyte)((34-DiceRollDiceCall(unchecked((byte)(5)),unchecked((byte)(4))))));
while(((((col+2))%7)>0)){col=unchecked((sbyte)((col-1)));}
startCol=col;
{int __g35=24;for(int __g34=0;__g34<=__g35;__g34++){row=unchecked((sbyte)(__g34));{
if((col<=48)){
GDVars.arenaPtr.cells[row,col]=unchecked((byte)(50));
GDVars.arenaPtr.cells[row,(col+1)]=unchecked((byte)(51));
if((((DiceRollDiceCall(unchecked((byte)(1)),unchecked((byte)(20)))==1))||(fordCnt&1)!=0)){PlotFord(row,col);}
col=unchecked((sbyte)((col+1)));
}
}}}
if((fordCnt==0)){
row=unchecked((sbyte)(((12-DiceRollDiceCall(unchecked((byte)(1)),unchecked((byte)(9))))+4)));
col=unchecked((sbyte)((startCol+row)));
PlotFord(row,col);
PlotFord(unchecked((sbyte)((row+1))),unchecked((sbyte)((col+1))));
}
}
}
public static void PlaceTrees(){
byte dens=default(byte);
byte h=default(byte);
sbyte col=default(sbyte);
sbyte row=default(sbyte);

{
dens=unchecked((byte)(1));
if(((CombatSt.TerrainClassOf()&2)!=0)){dens=unchecked((byte)(1));}
if(((CombatSt.TerrainClassOf()&4)!=0)){dens=unchecked((byte)(3));}
if(((CombatSt.TerrainClassOf()&64)!=0)){dens=unchecked((byte)(4));}
if(((CombatSt.TerrainClassOf()&8)!=0)){dens=unchecked((byte)(7));}
if(((CombatSt.TerrainClassOf()&128)!=0)){dens=unchecked((byte)(0));}
{int __g37=49;for(int __g36=0;__g36<=__g37;__g36++){col=unchecked((sbyte)(__g36));{int __g39=24;for(int __g38=1;__g38<=__g39;__g38++){row=unchecked((sbyte)(__g38));if(((((GDConsts.terrainInfo[GDVars.arenaPtr.cells[row,col]].tileId==22))&&((GDConsts.terrainInfo[GDVars.arenaPtr.cells[(row-1),col]].tileId==22)))&&((DiceRollDiceCall(unchecked((byte)(1)),unchecked((byte)(100)))<=dens)))){
h=DiceRollDiceCall(unchecked((byte)(1)),unchecked((byte)(10)));
if((h>8)){h=unchecked((byte)((DiceRollDiceCall(unchecked((byte)(1)),unchecked((byte)(2)))+4)));}else {h=unchecked((byte)((((h+1))/2)));}
if(((CombatSt.TerrainClassOf()&64)!=0)){h=unchecked((byte)((DiceRollDiceCall(unchecked((byte)(1)),unchecked((byte)(3)))+3)));}
GDVars.arenaPtr.cells[row,col]=unchecked((byte)(((h+31)+4)));
if((h<5)){GDVars.arenaPtr.cells[(row-1),col]=unchecked((byte)((h+31)));}
}}}}}
}
}
public static void ScatterFeatures(){
byte tSwamp=default(byte);
byte tA=default(byte);
byte tB=default(byte);
byte tC=default(byte);
byte tD=default(byte);
byte r=default(byte);
sbyte col=default(sbyte);
sbyte row=default(sbyte);

{
tSwamp=unchecked((byte)(0));
tA=unchecked((byte)(6));
tB=unchecked((byte)(15));
tC=unchecked((byte)(40));
tD=unchecked((byte)(0));
if(((CombatSt.TerrainClassOf()&2)!=0)){
tB=unchecked((byte)(45));
tC=unchecked((byte)(10));
}
if(((CombatSt.TerrainClassOf()&4)!=0)){tB=unchecked((byte)(25));}
if(((CombatSt.TerrainClassOf()&64)!=0)){
tC=unchecked((byte)(5));
tD=unchecked((byte)(30));
}
if(((CombatSt.TerrainClassOf()&128)!=0)){
tSwamp=unchecked((byte)(15));
tC=unchecked((byte)(25));
tB=unchecked((byte)(10));
}
{int __g41=49;for(int __g40=0;__g40<=__g41;__g40++){col=unchecked((sbyte)(__g40));{int __g43=24;for(int __g42=0;__g42<=__g43;__g42++){row=unchecked((sbyte)(__g42));if((GDConsts.terrainInfo[GDVars.arenaPtr.cells[row,col]].tileId==22)){
r=DiceRollDiceCall(unchecked((byte)(1)),unchecked((byte)(255)));
if((r<=tSwamp)){
r=DiceRollDiceCall(unchecked((byte)(1)),unchecked((byte)(4)));
if(((((r==4))&&((row>0)))&&((GDConsts.terrainInfo[GDVars.arenaPtr.cells[(row-1),col]].tileId==22)))){
GDVars.arenaPtr.cells[(row-1),col]=unchecked((byte)(65));
GDVars.arenaPtr.cells[row,col]=unchecked((byte)(64));
}
if((r<4)){GDVars.arenaPtr.cells[row,col]=unchecked((byte)(((r+31)+29)));}
}else {if((r<=(tA+tSwamp))){GDVars.arenaPtr.cells[row,col]=unchecked((byte)(((DiceRollDiceCall(unchecked((byte)(1)),unchecked((byte)(3)))+31)+23)));}else {if((r<=((tB+tSwamp)+tA))){GDVars.arenaPtr.cells[row,col]=unchecked((byte)(((DiceRollDiceCall(unchecked((byte)(1)),unchecked((byte)(4)))+31)+10)));}else {if((r<=(((tD+tB)+tSwamp)+tA))){
r=unchecked((byte)(((((DiceRollDiceCall(unchecked((byte)(1)),unchecked((byte)(10)))-1))/3)+1)));
GDVars.arenaPtr.cells[row,col]=unchecked((byte)(((r+31)+25)));
}else {if((r<=(tA+((((tC+tD)+tB)+tSwamp))))){
r=DiceRollDiceCall(unchecked((byte)(1)),unchecked((byte)(4)));
if((((r==4))&&(((CombatSt.TerrainClassOf()&64)==0)))){r=DiceRollDiceCall(unchecked((byte)(1)),unchecked((byte)(3)));}
GDVars.arenaPtr.cells[row,col]=unchecked((byte)(((r+31)+14)));
}}}}}
}}}}}
}
}
public static void BuildWildernessArena(){
sbyte mapX=default(sbyte);
sbyte mapY=default(sbyte);

{
for(int y=0;y<25;y++)for(int x=0;x<50;x++)GDVars.arenaPtr.cells[y,x]=(byte)23;
mapX=unchecked((sbyte)(GDVars.zone0.partyX.lo));
mapY=unchecked((sbyte)(GDVars.zone0.partyY.lo));
if((GDVars.gLocType==C_Area.LOC_WILD_CENTRAL)){mapX=unchecked((sbyte)((mapX+13)));}
if((GDVars.gLocType==C_Area.LOC_WILD_EAST)){mapX=unchecked((sbyte)((mapX+26)));}
terrainChar=GDConsts.wildMap[mapY,mapX];
CombatSt.PlaceRiver();
CombatSt.PlaceTrees();
CombatSt.ScatterFeatures();
CombatSt.SprinkleRareFeatures();
}
}
public static void LoadCombatTileSet(){


{
MapTilesFreeSecondSheetCall();
if((GDVars.gLocType==C_Area.LOC_3D_MAP)){MapTilesLoad24x24SetCall("DungCom",unchecked((byte)(0)),unchecked((sbyte)(24)));}else {MapTilesLoad24x24SetCall("WildCom",unchecked((byte)(0)),unchecked((sbyte)(33)));}
MapTilesLoad24x24SetCall("RandCom",unchecked((byte)(34)),unchecked((sbyte)(5)));
GDVars.arenaPtr=new TArena();
GDVars.arenaPtr.cursorOn=false;
GDVars.arenaPtr.cursorSize=unchecked((byte)(1));
GDVars.arenaPtr.distMode=unchecked((byte)(0));
if((GDVars.gLocType==C_Area.LOC_3D_MAP)){CombatSt.BuildDungeonArena();}else {CombatSt.BuildWildernessArena();}
}
}
public static void InitCreatureCombatData(){
TCharRecord p=null;
byte count=default(byte);
byte m=default(byte);

{
p=GDVars.creatureList;
count=unchecked((byte)(0));
while((p!=null)){
CharHlpRecalcStatsCall(p);
count=unchecked((byte)((count+1)));
p.combatAddr=new TCombatRec();
p.combatAddr=new TCombatRec();
if((count>GDVars.zone1[C_Gbvm.PARTY_SIZE])){p.combatAddr.notPcNpc=unchecked((byte)(1));}
p.combatAddr.direction=GDConsts.quadDir[(GDVars.gFacing/2)];
if((p.hostile==1)){p.combatAddr.direction=unchecked((byte)((((p.combatAddr.direction+4))%8)));}
m=unchecked((byte)((p.npc&C_Npc.MORALE_MASK)));
if(((((p.hostile==0))&&((p.combatAddr.notPcNpc==1)))&&((((m==0))||((m>102)))))){p.npc=unchecked((byte)((GDVars.zone1[C_Gbvm.MORALE_THRESHOLD]+C_Npc.FLAG)));}
p=p.nextCharAddr;
}
}
}
public static bool QuadFullyOff(sbyte qx,sbyte qy){
bool __result_QuadFullyOff=default(bool);


{
if((((((qx>=0))&&((qx<11))))||((((qy>=0))&&((qy<6)))))){__result_QuadFullyOff=false;}else {__result_QuadFullyOff=true;}
return __result_QuadFullyOff;
}
}
public static byte TryPlaceAt(sbyte team,byte slot,sbyte qx,sbyte qy,sbyte bqx,sbyte bqy,byte ring){
byte __result_TryPlaceAt=default(byte);
byte crIdx=default(byte);
byte terrIdx=default(byte);

{
if(((((((qx<0))||((qy<0)))||((qx>10)))||((qy>5)))||((quadFree[team,ring,qy,qx]==0)))){__result_TryPlaceAt=unchecked((byte)(0));}else {
GDVars.arenaLoc[slot].x=unchecked((sbyte)((((qx+(bqx*6))+(bqy*5))+22)));
GDVars.arenaLoc[slot].y=unchecked((sbyte)(((qy+(bqy*5))+10)));
CbtMapProbeDirectionCall(GDVars.creatureArr[slot],unchecked((byte)(C_Dir.CENTER)),out crIdx,out terrIdx);
if(((((crIdx==0))&&((terrIdx>0)))&&((GDConsts.terrainInfo[terrIdx].cost<255)))){
__result_TryPlaceAt=unchecked((byte)(1));
quadFree[team,ring,qy,qx]=unchecked((byte)(0));
}else {__result_TryPlaceAt=unchecked((byte)(0));}
}
return __result_TryPlaceAt;
}
}
public static bool FindPlacement(sbyte team,byte slot){
bool __result_FindPlacement=default(bool);
byte placed=default(byte);
byte firstPass=default(byte);
byte giveUp=default(byte);
byte offGrid=default(byte);
byte open=default(byte);
byte state=default(byte);
byte dir=default(byte);
byte quad=default(byte);
byte k=default(byte);
sbyte stepX=default(sbyte);
sbyte stepY=default(sbyte);
sbyte wallX=default(sbyte);
sbyte wallY=default(sbyte);
byte legShift=default(byte);
byte legLen=default(byte);
sbyte baseQX=default(sbyte);
sbyte baseQY=default(sbyte);
byte tries=default(byte);
byte ring=default(byte);
sbyte legX=default(sbyte);
sbyte legY=default(sbyte);
sbyte qx=default(sbyte);
sbyte qy=default(sbyte);

{
firstPass=unchecked((byte)(1));
giveUp=unchecked((byte)(0));
state=unchecked((byte)(1));
legShift=unchecked((byte)(0));
ring=unchecked((byte)(0));
baseQX=placeX[team];
baseQY=placeY[team];
do{
quad=unchecked((byte)((GDConsts.ringStart[teamDir[team],ring]/2)));
{var __g44=state;if(__g44==1){
stepX=GDConsts.dirDX[GDConsts.quadDir[(((quad+2))%4)]];
stepY=GDConsts.dirDY[GDConsts.quadDir[(((quad+2))%4)]];
legX=unchecked((sbyte)((GDConsts.startQX[((ring>0)?1:0),quad]+(stepX*legShift))));
legY=unchecked((sbyte)((GDConsts.startQY[((ring>0)?1:0),quad]+(stepY*legShift))));
qx=legX;
qy=legY;
legLen=unchecked((byte)(1));
state=unchecked((byte)(2));
tries=unchecked((byte)(1));
}else if(__g44==2){
stepX=GDConsts.dirDX[GDConsts.quadDir[(((quad+1))%4)]];
stepY=GDConsts.dirDY[GDConsts.quadDir[(((quad+1))%4)]];
qx=unchecked((sbyte)((legX+(stepX*legLen))));
qy=unchecked((sbyte)((legY+(stepY*legLen))));
state=unchecked((byte)(3));
tries=unchecked((byte)((tries+1)));
}else if(__g44==3){
stepX=GDConsts.dirDX[GDConsts.quadDir[(((quad+3))%4)]];
stepY=GDConsts.dirDY[GDConsts.quadDir[(((quad+3))%4)]];
qx=unchecked((sbyte)((legX+(stepX*legLen))));
qy=unchecked((sbyte)((legY+(stepY*legLen))));
state=unchecked((byte)(2));
legLen=unchecked((byte)((legLen+1)));
tries=unchecked((byte)((tries+1)));
}}
if((((((qx<0))||((qy<0)))||((qx>10)))||((qy>5)))){offGrid=unchecked((byte)(1));}else {offGrid=unchecked((byte)(0));}
if((state>1)){
if(((((((offGrid!=0))&&(!(CombatSt.QuadFullyOff(qx,qy)))))||((((firstPass!=0))&&((tries>=teamRows[team])))))||((((firstPass==0))&&((tries>11)))))){
legShift=unchecked((byte)((legShift+1)));
if((((((team==0))&&(teamDir[0]&1)!=0)&&((ring==0)))&&((legShift==1)))){
wallX=unchecked((sbyte)((placeX[team]+GDVars.gMapX)));
wallY=unchecked((sbyte)((placeY[team]+GDVars.gMapY)));
open=unchecked((byte)(0));
{int __g46=3;for(int __g45=1;__g45<=__g46;__g45++){k=unchecked((byte)(__g45));{
dir=GDConsts.ringDirTbl[teamDir[team],k];
if((((GDVars.gLocType>C_Area.LOC_3D_MAP))||((CombatSt.WallBetween(wallX,wallY,dir)!=1)))){open=unchecked((byte)(1));}
}}}
if((open!=0)){legShift=unchecked((byte)((legShift+1)));}
}
state=unchecked((byte)(1));
firstPass=unchecked((byte)(0));
}
}
if((((offGrid!=0))&&CombatSt.QuadFullyOff(qx,qy))){
placed=unchecked((byte)(0));
state=unchecked((byte)(0));
while((((ring<3))&&((state!=1)))){
ring=unchecked((byte)((ring+1)));
wallX=unchecked((sbyte)((placeX[team]+GDVars.gMapX)));
wallY=unchecked((sbyte)((placeY[team]+GDVars.gMapY)));
dir=GDConsts.ringDirTbl[teamDir[team],ring];
if((((GDVars.gLocType>C_Area.LOC_3D_MAP))||((CombatSt.WallBetween(wallX,wallY,dir)!=1)))){
baseQX=unchecked((sbyte)((placeX[team]+GDConsts.dirDX[dir])));
baseQY=unchecked((sbyte)((placeY[team]+GDConsts.dirDY[dir])));
legShift=unchecked((byte)(0));
state=unchecked((byte)(1));
}
}
if((state!=1)){giveUp=unchecked((byte)(1));}
}
if((offGrid==0)){placed=CombatSt.TryPlaceAt(team,slot,qx,qy,baseQX,baseQY,ring);}
}while(!((((placed!=0))||((giveUp!=0)))));
if((giveUp!=0)){__result_FindPlacement=false;}else {__result_FindPlacement=true;}
return __result_FindPlacement;
}
}
public static void PlaceAllCreatures(){
sbyte team=default(sbyte);
sbyte x=default(sbyte);
sbyte y=default(sbyte);
TCharRecord prev=null;
TCharRecord p=null;
TCharRecord next=null;
byte slot=default(byte);
byte g=default(byte);
byte d=default(byte);
byte placedCnt=default(byte);
byte removedCnt=default(byte);

{
CharHlpCountTeamsCall();
removedCnt=unchecked((byte)(0));
placedCnt=unchecked((byte)(0));
{int __g48=Types.ARENA_SLOTS;for(int __g47=1;__g47<=__g48;__g47++){slot=unchecked((byte)(__g47));GDVars.arenaLoc[slot].size=unchecked((byte)(0));}}
CbtMapBuildArenaArraysCall();
placeX[0]=unchecked((sbyte)(0));
placeY[0]=unchecked((sbyte)(0));
teamDir[0]=unchecked((sbyte)((GDVars.gFacing/2)));
placeX[1]=unchecked((sbyte)(((GDConsts.dirDX[GDVars.gFacing]*GDVars.zone1[C_Gbvm.MONSTER_DIST])+placeX[0])));
placeY[1]=unchecked((sbyte)(((GDConsts.dirDY[GDVars.gFacing]*GDVars.zone1[C_Gbvm.MONSTER_DIST])+placeY[0])));
teamDir[1]=unchecked((sbyte)(((((GDVars.gFacing+4))%8)/2)));
{int __g50=1;for(int __g49=0;__g49<=__g50;__g49++){team=unchecked((sbyte)(__g49));teamRows[team]=unchecked((sbyte)((((GDVars.teamCount[team]+1))/2)));}}
{int __g52=1;for(int __g51=0;__g51<=__g52;__g51++){team=unchecked((sbyte)(__g51));{int __g54=3;for(int __g53=0;__g53<=__g54;__g53++){g=unchecked((byte)(__g53));{
if((g==1)){d=unchecked((byte)(4));}else {d=unchecked((byte)(teamDir[team]));}
{int __g56=5;for(int __g55=0;__g55<=__g56;__g55++){y=unchecked((sbyte)(__g55));{int __g58=10;for(int __g57=0;__g57<=__g58;__g57++){x=unchecked((sbyte)(__g57));if((((GDConsts.rowBand[d,y].lo>x))||((GDConsts.rowBand[d,y].hi<x)))){quadFree[team,g,y,x]=unchecked((byte)(0));}else {quadFree[team,g,y,x]=unchecked((byte)(1));}}}}}
}}}}}
slot=unchecked((byte)(1));
GDVars.arenaCount=unchecked((byte)(1));
prev=null;
p=GDVars.creatureList;
while((((p!=null))&&((slot<=Types.ARENA_SLOTS)))){
GameIoDiscardPendingKeysCall();
next=p.nextCharAddr;
GDVars.creatureArr[slot]=p;
team=unchecked((sbyte)((p.hostile&1)));
GDVars.arenaLoc[slot].slotIndex=slot;
GDVars.arenaLoc[slot].size=unchecked((byte)((p.iconDimensions&7)));
if(CombatSt.FindPlacement(team,slot)){
prev=p;
if((p.enabled==0)){
GDVars.arenaLoc[slot].size=unchecked((byte)(0));
if(((!(GDVars.duelFlag))&&((p.combatAddr.notPcNpc==0)))){
x=GDVars.arenaLoc[slot].x;
y=GDVars.arenaLoc[slot].y;
CbtMapRegisterDownedCall(p,unchecked((byte)(x)),unchecked((byte)(y)));
}
}
CbtMapBuildArenaArraysCall();
slot=unchecked((byte)((slot+1)));
GDVars.arenaCount=unchecked((byte)((GDVars.arenaCount+1)));
placedCnt=unchecked((byte)((placedCnt+1)));
}else {
GDVars.arenaLoc[slot].size=unchecked((byte)(0));
if((p.combatAddr.notPcNpc==1)){
removedCnt=unchecked((byte)((removedCnt+1)));
GDVars.creatureArr[slot]=null;
GDVars.curChar=p;
SaveLoadRemoveCreatureCall(true,false);
}else {
prev=p;
slot=unchecked((byte)((slot+1)));
GDVars.arenaCount=unchecked((byte)((GDVars.arenaCount+1)));
}
}
p=next;
}
if((prev!=null)){prev.nextCharAddr=null;}
}
}
public static void CombatStart(){
TCharRecord p=null;

{
PortraitFreeAnimationCall(ref GDVars.animData);
BlitFreePicCall(ref GDVars.dsPortraitCache[1].pic);
BlitFreePicCall(ref GDVars.dsPortraitCache[2].pic);
GDVars.dsPortraitCache[1].pictureId=unchecked((byte)(255));
GDVars.dsPortraitCache[2].pictureId=unchecked((byte)(255));
TextRendClearBottomLineCall();
TextRendPauseOnlyCall();
TextRendDisplayStringCall(unchecked((byte)(0)),unchecked((byte)(24)),unchecked((byte)(10)),"A battle begins...");
SoundDrvPlayMusicCall(unchecked((ushort)(C_Sound.MUSIC_BATTLE)));
GDVars.quickMagic=(GDVars.quickMagicOn!=0);
GDVars.roundCounter=unchecked((byte)(0));
GDVars.gAttackRoll=unchecked((sbyte)(0));
GDVars.downedCount=unchecked((byte)(0));
GDVars.stinkHead=null;
GDVars.lastMissileItem=null;
for(int i=1;i<GDVars.downed.Length;i++)GDVars.downed[i]=new TDownedRec();
GDVars.zone1[C_Gbvm.ATTACKED_ALLIES]=unchecked((ushort)(0));
if((GDVars.zone1[C_Gbvm.MORALE_THRESHOLD]>100)){GDVars.zone1[C_Gbvm.MORALE_THRESHOLD]=unchecked((ushort)(100));}
CombatSt.LoadCombatTileSet();
GameIoDiscardPendingKeysCall();
CombatSt.InitCreatureCombatData();
GameIoDiscardPendingKeysCall();
CombatSt.PlaceAllCreatures();
GameIoDiscardPendingKeysCall();
BlitAllocPicCall(unchecked((ushort)(24)),unchecked((ushort)(3)),unchecked((byte)(4)),unchecked((byte)(1)),ref GDVars.dsMoveIconSheet);
GDVars.arenaPtr.ox=unchecked((sbyte)((CbtMapCombatCellXCall(GDVars.creatureList)-3)));
GDVars.arenaPtr.oy=unchecked((sbyte)((CbtMapCombatCellYCall(GDVars.creatureList)-3)));
CharDispDisplayCombatInterfaceCall();
p=GDVars.creatureList;
while((p!=null)){
FxEngineTriggerEffectsWithConditionCall(unchecked((byte)(C_Cond.COMBAT_START)),p);
p=p.nextCharAddr;
}
CbtActsCalcHostileHpPercentCall();
GDVars.ifcState=unchecked((byte)(C_Ifc.COMBAT));
}
}
}}
