using System;using Por.Core;
public static class NumericChecks
{
 static void Yes(bool b,string s){IntegrationVerification.Yes(b,s);}
 public static void Run()
 {
  for(int sum=0;sum<256;sum++)for(int con=-128;con<=127;con++)for(int n=1;n<=8;n++){
   int expected=con<0&&sum<=Math.Abs(con)+n?1:((sum+con)/n)&255;Yes(ClassCal.DivideAmongClasses((byte)sum,(sbyte)con,(byte)n)==expected,"Byte hp quotient truncation");
  }
  int[] xs={int.MinValue,-int.MaxValue,-100,-1,0,1,100,int.MaxValue};var c=new Types.TCharRecord(){strCur=16,intCur=16,wisCur=16,dexCur=16,conCur=16,chaCur=16};
  foreach(int cls in new[]{C_Class.CLERIC,C_Class.FIGHTER,C_Class.PALADIN,C_Class.RANGER,C_Class.MAGE,C_Class.THIEF}){c.charClass=(sbyte)cls;foreach(int xp in xs)Yes(ClassCal.ExperienceShare(c,xp)==unchecked(xp+xp/10),"LongInt experience wrapping");}
  var money=new Types.TMoney();foreach(int n in xs){for(int i=0;i<5;i++)money[i]=n;long wide=0;for(int i=0;i<5;i++)wide+=(long)GDConsts.dsCoinValuationCopper[i]*n;Yes(MoneyCal.CopperValue(money)==unchecked((int)wide),"LongInt coin multiply/add wrap");}
  for(int i=0;i<5;i++)money[i]=0;money[0]=int.MaxValue;money[1]=1;Yes(MoneyCal.CopperValue(money)==unchecked(int.MaxValue+10),"coin addition wraps independently of multiply");
  foreach(int rem in xs)foreach(int cap in xs){int r=Math.Max(rem,0);var expected=new int[7];for(int i=4;i>=0;i--){int n=r/GDConsts.dsCoinValuationCopper[i];n=Math.Min(n,cap);expected[i]=n;r=unchecked(r-n*GDConsts.dsCoinValuationCopper[i]);}MoneyCal.MakeChange(rem,cap,money);for(int i=0;i<5;i++)Yes(money[i]==expected[i],"LongInt make change");}
 }
}
