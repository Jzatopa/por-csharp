using System;using static Por.Core.Types;
namespace Por.Core
{
 public interface ICharGenView{void DisplayChar(TCharRecord c);void PrintStat(sbyte ability,bool highlighted);void PrintCombatStats();void PrintMoney();void ViewCharMenu(ref bool changed);}
 public static class CharGenView
 {
  public static ICharGenView Host;
  private static ICharGenView H{get{if(Host==null)throw new NotSupportedException("CharGen: ViewChar service not bound.");return Host;}}
  public static void DisplayChar(TCharRecord c){H.DisplayChar(c);}public static void PrintStat(sbyte a,bool h){H.PrintStat(a,h);}public static void PrintCombatStats(){H.PrintCombatStats();}public static void PrintMoney(){H.PrintMoney();}public static void ViewCharMenu(ref bool changed){H.ViewCharMenu(ref changed);}
 }
 public static partial class CharGen
 {
  internal delegate byte HorizontalProc(string query,string menu,byte q,byte m,byte h,bool reinterpret,bool animate,ref bool ext);
  internal delegate byte VerticalProc(string query,string menu,byte q,byte m,byte h,sbyte x,sbyte y,sbyte r,sbyte b,TItemRec list,bool exit,ref bool redraw,ref short index,ref TItemRec selected);
  internal delegate void LoadPicProc(string name,byte id,bool mask,byte trans,ref TImgBuf pic);
  internal delegate byte SpellMenuProc(byte mode,byte cls,ref short index,ref bool notEmpty);
  internal static HorizontalProc Horizontal=MenuCore.MenuHorizontal;
  internal static VerticalProc Vertical=MenuCore.MenuVertical;
  internal static Func<string,byte,byte,byte,byte> YesNo=MenuCore.YesNoMenu;
  internal static Action<TCharRecord> RecalcStats=CharHlp.RecalcStats;
  internal static Action<TCharRecord,byte,byte,bool> PrintHitPoints=CharDisp.PrintHitPoints;
  internal static Action<sbyte,sbyte,sbyte,sbyte> ClearBox=Border.ClearBox;
  internal static Action<sbyte,sbyte,sbyte,sbyte,byte> DrawBorder=Border.DrawBorder;
  internal static Action<sbyte,sbyte,sbyte,sbyte,byte,byte,string> DrawWindow=Border.DrawWindow;
  internal static Action<byte,byte,byte,string> DisplayString=TextRend.DisplayString;
  internal static Action<byte,byte,byte,byte,string> DisplayCentered=TextRend.DisplayCentered;
  internal static Action ClearScreen=TextRend.ClearScreen,ClearBottomLine=TextRend.ClearBottomLine;
  internal static Action<string,byte> PrintAndPause=TextRend.PrintAndPause;
  internal static LoadPicProc LoadPic=Blit.LoadPic;
  internal static Action<short,short,TImgBuf> ShowPic=Blit.ShowPic;
  internal static Func<bool,byte> PortraitCount=Portrait.PlayerPortraitCount;
  internal static Action<short,short,bool,TImgBuf> DisplayImage=Portrait.DisplayImageBuf;
  internal static Func<string,byte,byte,string> ReadName=TextRend.ReadString;
  internal static SpellMenuProc SpellMenu=Magic.SpellMenu;
  internal static Action<TCharRecord,string> SaveCharCall=SaveLoad.SaveChar;
  internal static Action<TCharRecord> FreeCreatureCall=SaveLoad.FreeCreature;
  internal delegate void LoadRosterProc(ref TItemRec list);
  internal static LoadRosterProc LoadRoster=SaveLoad.LoadCharlist;
  internal static Action<TItemRec> SaveRoster=SaveLoad.SaveCharlist;
  internal static Func<string,TCharRecord,bool,bool,bool> LoadCharacter=SaveLoad.LoadChar;
  internal static Action<bool> LoadCharacterIcon=SaveLoad.LoadCharIconAnim;
  internal static Action<byte,string> DisplayAndPause=TextRend.DisplayAndPause;
  internal static Action<bool,bool> RemoveCharacter=SaveLoad.RemoveCreature;
  internal static Action<TCharRecord> DisplayParty=CharDisp.DisplayParty;
  internal static Action<TCharRecord,byte,byte,bool> DisplayCreatureName=CharDisp.DisplayCreatureName;
  internal static Action<string> PrintBottomLine=CharDisp.PrintBottomLine;
  private static int RandomBelow(int n){if(Dice.RandomBelow==null)throw new NotSupportedException("CharGen: random source missing.");return Dice.RandomBelow(n);}
  private static string Copy(string s,int start,int length){if(start<1)start=1;if(length<=0||start>s.Length)return "";return s.Substring(start-1,Math.Min(length,s.Length-start+1));}
  private static byte GetByte(object a,int i){if(a is byte[])return ((byte[])a)[i];if(a is ByteView)return ((ByteView)a)[i];if(a is TIconCols)return ((TIconCols)a)[i];if(a is TColorTab)return ((TColorTab)a)[i];if(a is TColorMap)return ((TColorMap)a)[i];if(a is TBytes)return ((TBytes)a).Data[i];throw new NotSupportedException("Unknown source byte window.");}
  private static void PutByte(object a,int i,byte value){if(a is byte[])((byte[])a)[i]=value;else if(a is ByteView)((ByteView)a)[i]=value;else if(a is TIconCols)((TIconCols)a)[i]=value;else if(a is TColorTab)((TColorTab)a)[i]=value;else if(a is TColorMap)((TColorMap)a)[i]=value;else if(a is TBytes)((TBytes)a).Data[i]=value;else throw new NotSupportedException("Unknown source byte window.");}
  private static void CopyIcon(object src,object dst){int start=src is TColorTab||src is TColorMap?0:1,end=src is TColorTab?16:src is TColorMap?8:7;for(int i=start;i<end;i++)PutByte(dst,i,GetByte(src,i));}
  private static void MoveBytes(object src,object dst,int count){int start=src is ByteView?(count==6?0:1):dst is ByteView?(count==6?0:1):count==3?1:0;for(int i=0;i<count;i++)PutByte(dst,i+start,GetByte(src,i+start));}
 }
}
