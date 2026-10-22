using System;
using Por.Core;
public static class GlobalVerification
{
 static int checks,failed;
 static void Yes(bool b,string label){checks++;if(!b)throw new Exception(label);}
 static void Run(string name,Action action){try{action();Console.WriteLine("PASS "+name);}catch(Exception e){failed++;Console.WriteLine("FAIL "+name+": "+e);}}
 static void ClockChecks()
 {
  var c=new Types.TClock();for(int mins=0;mins<=65535;mins++)
  {
   for(int i=0;i<=6;i++)c[i]=65535;
   Clock.SetFromMinutes(c,(ushort)mins);
   Yes(c[0]==0&&c[5]==0&&c[6]==0,"duration zero components");
   Yes(c[1]<10&&c[2]<6&&c[3]<24,"duration normalized");
   Yes(c[4]*1440+c[3]*60+c[2]*10+c[1]==mins,"duration reconstruction");
  }
  var a=new Types.TCharRecord(){age=65535};var b=new Types.TCharRecord(){age=12};var monster=new Types.TCharRecord(){age=100};a.nextCharAddr=b;b.nextCharAddr=monster;GDVars.creatureList=a;
  var rnd=new Random(71313);for(int k=0;k<10000;k++)
  {
   var initial=new int[7];for(int i=0;i<7;i++){initial[i]=k==0?65535:rnd.Next(65536);c[i]=(ushort)initial[i];}
   bool age=(k&1)==0;int carry=0;var expected=new int[7];
   for(int i=0;i<6;i++){int value=(initial[i]+carry)&65535;carry=value>=GDConsts.unitScale[i]?1:0;expected[i]=(value-carry*GDConsts.unitScale[i])&65535;}
   expected[6]=(initial[6]+carry)&65535;
   int oldA=a.age,oldB=b.age,oldM=monster.age;
   Clock.ValidateClock(c,age);
   for(int i=0;i<7;i++)Yes(c[i]==expected[i],"single carry step component"+i);
   int aging=age?carry:0;Yes(a.age==((oldA+aging)&65535)&&b.age==((oldB+aging)&65535)&&monster.age==((oldM+aging)&65535),"age all creatures once with Word wrap");
  }
  GDVars.creatureList=null;
 }
 public static void Main(){Run("source table values",TableChecks.Run);Run("source global state",GlobalStateChecks.Run);Run("clock durations/carries",ClockChecks);Console.WriteLine("Global runtime assertions="+checks+" failed groups="+failed);if(failed!=0)Environment.Exit(1);}
}
