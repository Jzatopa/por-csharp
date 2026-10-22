using System;
using Por.Core;
namespace PortVerification {
internal static class RulesChecks {
 static int count;
 static void Eq(int expected,int actual,string context) {
  count++; if(expected!=actual)throw new Exception(context+": expected "+expected+", got "+actual);
 }
 public static int Run() {
  count=0;
  int[] hit={0,-3,-3,-3,-2,-2,-1,-1,0,0,0,0,0,0,0,0,0,1,1,1,2,2,2,3,3,3,4,4,5,6,7};
  int[] damage={0,-2,-2,-1,-1,-1,0,0,0,0,0,0,0,0,0,0,1,1,2,3,3,4,5,6,7,8,9,10,11,12,14};
  int[] weight={0,-350,-350,-350,-250,-250,-150,-150,0,0,0,0,100,100,200,200,350,500,750,1000,1250,1500,2000,3000,4500,5000,6000,7500,9000,12000,15000};
  var creature=new Types.TCharRecord();
  for(int s=0;s<256;s++)for(int exceptional=0;exceptional<256;exceptional++) {
   creature.strCur=(byte)s;creature.strExc=(byte)exceptional;
   int translated=s<18?s:s>25?30:s>18?s+5:exceptional==0?18:exceptional<=50?19:exceptional<=75?20:exceptional<=90?21:exceptional<=99?22:23;
   Eq(translated,AbilAdj.TranslatedStrength(creature),"strength scale");
   Eq(weight[translated],AbilAdj.WeightAllowance(creature),"weight");
   foreach(byte enabled in new byte[]{0,1,255}) {
    creature.useStrength=enabled;
    Eq(enabled==0?0:hit[translated],AbilAdj.StrToHitAdjust(creature),"strength hit");
    Eq(enabled==0?0:damage[translated],AbilAdj.StrDamageAdjust(creature),"strength damage");
   }
  }
  for(int con=0;con<256;con++) {
   Eq(con*2/7,AbilAdj.ConSaveBonus((byte)con),"constitution save");
   Eq(con<19?0:Math.Min(4,(con-17)/2),AbilAdj.ConPoisonSave((byte)con),"constitution poison");
   Eq(con<20?0:con>25?10:(26-con)*10,AbilAdj.ConRegenMinutes((byte)con),"constitution regeneration");
  }
  for(int score=short.MinValue;score<=short.MaxValue;score++)
   Eq(Math.Max(3,Math.Min(25,score)),AbilAdj.ClampScore((short)score),"score clamp");
  for(int d=0;d<256;d++) {
   creature.dexCur=(byte)d;
   int armor=d>=1&&d<=3?-4:d>=4&&d<=6?d-7:d>=15&&d<=18?d-14:d>=19&&d<=20?4:d>=21&&d<=23?5:d>=24&&d<=25?6:0;
   int missile=d<=2?-4:d<=5?d-6:d>=16&&d<=18?d-15:d>=19&&d<=20?3:d>=21&&d<=23?4:d>=24&&d<=25?5:0;
   Eq(armor,AbilAdj.DexterityArmorAdjustment(creature),"dexterity armor");
   Eq(missile,AbilAdj.DexMissileAdjust(creature),"dexterity missile");
  }
  return count;
 }
}
}
