using System;using Por.Core;using static Por.Core.Types;
static partial class Tests
{
 static void WildernessChecks()
 {
  foreach(var line in System.IO.File.ReadAllLines("evidence/final-three-2026-10-22/wilderness-golden.tsv")){
   var cols=line.Split('\t');int glyph=int.Parse(cols[0]);uint state=uint.Parse(cols[1]);int calls=0;
   Func<int,int,byte> roll=null;roll=(n,s)=>{calls++;state=unchecked(state*1664525u+1013904223u);if(n==1)return B((int)(state%(uint)s)+1);int sum=0;for(int i=0;i<n;i++)sum+=roll(1,s);return B(sum);};CombatSt.DiceRollDiceCall=(n,s)=>roll(n,s);
   GDVars.arenaPtr=new TArena();GDVars.gLocType=C_Area.LOC_WILD_WEST;GDVars.zone0.yarashMissionDone=0;GDVars.zone0.partyX.lo=0;GDVars.zone0.partyY.lo=0;GDVars.eclScriptId=9;GDConsts.wildMap[0,0]=B(glyph);GDConsts.terrainInfo[23].tileId=22;
   CombatSt.BuildWildernessArena();Eq(long.Parse(cols[2]),calls,"wilderness exact roll order");Eq(long.Parse(cols[3]),state,"wilderness consumes same random inputs");var hex=cols[4];for(int i=0;i<1250;i++)Eq(Convert.ToByte(hex.Substring(i*2,2),16),GDVars.arenaPtr.cells[i/50,i%50],"wilderness river/ford/tree/scatter/rare grid");
  }
 }
}
