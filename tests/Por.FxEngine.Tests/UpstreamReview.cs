// Bounded post-pin amendments, reviewed against Talonius d6874f1 and 8149706 (2026-10-28).
using System;
using System.Linq;
using Por.Core;
using static Por.Core.Types;
static partial class Checks
{
 static void UpstreamReady()
 {
  foreach(bool inCombat in new[]{false,true})
   foreach(byte id in new byte[]{C_Effect.FLAME_TONGUE,C_Effect.UNDEAD_SLAYER,C_Effect.RING_FIRE_RES,C_Effect.DISPLACED})
   {
    Rig();FxEngineBindings.Bind();var c=Char();c.combatAddr=inCombat?new TCombatRec():null;
    GDVars.ifcState=(byte)(inCombat?C_Ifc.COMBAT:C_Ifc.INDOORS);
    GDVars.effectFns[C_Effect.FLAME_TONGUE]=EffHndlr.EffFlameTongue;
    GDVars.effectFns[C_Effect.UNDEAD_SLAYER]=EffHndlr.EffUndeadSlayer;
    GDVars.effectFns[C_Effect.DISPLACED]=EffHndlr.EffDisplaced;
    GDVars.gAttackRoll=7;GDVars.pendingDamage=5;GDVars.elementMask=0;
    var item=new TItemRec{effect=id};EffHndlr.EffReadyItem(c,item,false);
    Yes(Nodes(c).Count==1&&Nodes(c)[0].id==id,"ready adds actual effect without attack");
    Eq(7,GDVars.gAttackRoll,"ready does not alter an attack roll");Eq(5,GDVars.pendingDamage,"ready does not alter damage");Eq(0,GDVars.elementMask,"ready does not set attack element");
    Eq(12,Nodes(c)[0].strength,"ready retains fresh effect strength, including displacement");
    EffHndlr.EffReadyItem(c,item,true);Yes(c.effectsAddr==null,"unready removes added effect");
    EffHndlr.EffFlameTongue(c,null,false);EffHndlr.EffUndeadSlayer(c,null,false);
    Eq(7,GDVars.gAttackRoll,"direct attack handler with no target is safe");Eq(5,GDVars.pendingDamage,"targetless direct handler preserves damage");
   }
  foreach(byte id in new byte[]{C_Effect.RING_INVIS,C_Effect.CAMOUFLAGE})
  {
   Rig();FxEngineBindings.Bind();var c=Char();GDVars.effectFns[id]=id==C_Effect.RING_INVIS?(TEffectHandler)EffHndlr.EffRingInvis:EffHndlr.EffCamouflage;
   EffHndlr.EffReadyItem(c,new TItemRec{effect=id},false);
   Yes(Nodes(c).Any(e=>e.id==id)&&Nodes(c).Any(e=>e.id==C_Effect.INVISIBLE),"immediate invisibility still acts on ready");
  }
  Rig();var attacker=Char();attacker.combatAddr.other=new TCharRecord{charType=4};GDVars.gAttackRoll=7;GDVars.pendingDamage=5;
  EffHndlr.EffUndeadSlayer(attacker,null,false);Eq(9,GDVars.gAttackRoll,"undead attack bonus retained");Eq(7,GDVars.pendingDamage,"undead damage bonus retained");
  GDVars.gAttackRoll=7;GDVars.pendingDamage=5;EffHndlr.EffFlameTongue(attacker,null,false);
  Eq(10,GDVars.gAttackRoll,"flame tongue attack bonus retained");Eq(8,GDVars.pendingDamage,"flame tongue damage retained");
 }
 sealed class EndingBoundary : Exception {}
 static void UpstreamEnding()
 {
  Rig();GDVars.gameFont=new TFontArr();GDVars.virtualDisk=1;GDVars.ifcState=C_Ifc.INDOORS;
  var oldDraw=GDVars.video.DrawGlyph;var oldClear=GDVars.video.ClearRect;var oldInterface=Ending.DisplayInterface;var oldLoad=Ending.LoadPic;
  var bottom=Enumerable.Repeat(true,40).ToArray();bool clearBeforeText=false,reached=false;
  try
  {
   GDVars.video.DrawGlyph=(x,y,c,g)=>{if(y==24&&c==0&&g==32)bottom[x]=false;};
   GDVars.video.ClearRect=(a,b,c,d)=>{if(!reached){clearBeforeText=bottom.All(v=>!v);reached=true;}};
   Ending.DisplayInterface=()=>{};
   Ending.LoadPic=(string n,byte id,bool mask,byte key,ref TImgBuf pic)=>{throw new EndingBoundary();};
   try{Ending.WinGame();}catch(EndingBoundary){}
   Yes(reached&&clearBeforeText,"treasure menu erased before ending message rectangle");
   Yes(bottom.All(v=>!v),"all forty bottom-row cells erased before first FINAL load");
  }
  finally{GDVars.video.DrawGlyph=oldDraw;GDVars.video.ClearRect=oldClear;Ending.DisplayInterface=oldInterface;Ending.LoadPic=oldLoad;GDVars.ifcState=C_Ifc.INDOORS;}
 }
}
