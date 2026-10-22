using static Por.Core.Types;
namespace Por.Core
{
 public static class CharGenBindings
 {
  public static void Bind(){if(TenDBindings.MainMenu==null)TenDBindings.MainMenu=CharGen.MainMenu;CharEdit.Services=new EditorAdapter();}
  private sealed class EditorAdapter:ICharEditServices
  {
   public void DisplayChar(TCharRecord c){CharGenView.DisplayChar(c);}public void ModifyCharacter(bool levelled){CharGen.ModifyCharacter(levelled);}public void GainLevels(byte classes,bool restoreDrain){CharGen.GainLevels(classes,restoreDrain);}
  }
 }
}
