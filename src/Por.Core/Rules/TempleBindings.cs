// Binds Temple's ITempleEffects to the converted FxEngine and Magic routines. Temple.Effects was never assigned in the
// desktop startup path, so healing or curing at a temple threw NotSupportedException.
// Pascal temple.pas calls FxEngine.RestoreHitPoints(c, hp, False); the interface parameter named "message" carries that same flag.
using System;using static Por.Core.Types;
namespace Por.Core
{
 public static class TempleBindings
 {
  public static void Bind()
  {
   if (Temple.Effects == null) Temple.Effects = new TempleActions();
  }
  private sealed class TempleActions : ITempleEffects
  {
   public bool HasDisease(TCharRecord c) { return FxEngine.HasDisease(c); }
   public void RemoveDiseases(TCharRecord c) { FxEngine.RemoveDiseases(c); }
   public void RemoveEffect(TCharRecord c, byte id, TEffectRec effect) { FxEngine.RemoveEffect(c, id, effect); }
   public bool RestoreHitPoints(TCharRecord c, byte hp, bool message) { return FxEngine.RestoreHitPoints(c, hp, message); }
   public void RemoveCurseSpell() { Magic.RemoveCurseSpell(); }
  }
 }
}
