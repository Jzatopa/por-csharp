// Connect accepted units; hardware, filesystem and presentation hosts are still supplied by the application.
using System;using static Por.Core.Types;
namespace Por.Core
{
 public static class FinalGameplayBindings
 {
  public static void Bind()
  {
   FxEngineBindings.Bind();CbtMapBindings.BindGameplay();MagicBindings.BindCbtActs();EffHndlrBindings.BindCombatMap();
   ViewCharBindings.BindCommerce();ViewCharBindings.BindCharGen();
   Explore.Services=new Exploration();PostCbt.Services=new AfterCombat();
   EclMenu.Host=new ScriptActions();GameTime.Services=new RestActions();TempleBindings.Bind();
  }
  private sealed class ScriptActions:IEclMenuHost
  {
   public void DisplayMapView(){MapView.DisplayMapView();}public void DisplayParty(TCharRecord c){CharDisp.DisplayParty(c);}
   public void PrependItemCopy(ref TItemRec list,TItemRec item){CharHlp.PrependItemCopy(ref list,item);}public byte TheftChance(byte chance,ushort weight){return CharHlp.TheftChance(chance,weight);}
   public void RemoveItem(TCharRecord c,TItemRec item){CharHlp.RemoveItem(c,item);}public bool HasEffect(TCharRecord c,byte id,ref TEffectRec effect){return CharHlp.HasEffect(c,id,out effect);}
   public void CallEffectHandler(byte id,TCharRecord c,object arg,bool removed){FxEngine.CallEffectHandler(id,c,arg,removed);}
   // Pascal InflictDamage's byte parameter narrows EclInflictDamage's word at the call boundary.
   public void InflictDamage(TCharRecord c,ushort dmg){CharHlp.InflictDamage(c,unchecked((byte)dmg));}
   public void AddCharToParty(TCharRecord c){SaveLoad.AddCharToParty(c);}public void LoadCharIconAnim(bool flag){SaveLoad.LoadCharIconAnim(flag);}
  }
  private sealed class RestActions:IRestServices
  {
   public bool RestoreHitPoints(TCharRecord c,byte hp,bool bypass){return FxEngine.RestoreHitPoints(c,hp,bypass);}public void AdvanceTime(byte amount,sbyte unit){FxEngine.AdvanceTime(amount,unit);}
   public void PrintCastSpellMsg(TCharRecord c,string msg,byte spell){Magic.PrintCastSpellMsg(c,msg,spell);}public void RemoveSpellFromScroll(TCharRecord c,TItemRec item,byte spell){Magic.RemoveSpellFromScroll(c,item,spell);}
  }
  private sealed class Exploration:IExploreServices
  {
   public void AdvanceTime(byte a,sbyte u){FxEngine.AdvanceTime(a,u);}public void NoncombatCastMenu(){Camp.NoncombatCastMenu();}public void ViewCharacter(ref bool done){ViewChar.ViewCharMenu(ref done);}
  }
  private sealed class AfterCombat:IPostCombatServices
  {
   public void ViewCharacter(ref bool done){ViewChar.ViewCharMenu(ref done);}public void CastSpellNC(byte spell,bool a,bool b,ref bool done){Magic.CastSpellNC(spell,a,b,ref done);}
  }
 }
}
