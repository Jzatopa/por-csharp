using System;
using System.IO;
using Por.Core;
public static class IntegrationVerification
{
 internal static int count,failed;
 internal static void Yes(bool ok,string label){count++;if(!ok)throw new Exception(label);}
 static void Run(string label,Action action){try{action();Console.WriteLine("PASS "+label);}catch(Exception e){failed++;Console.WriteLine("FAIL "+label+": "+e);}}
 static void DiceChecks()
 {
  Dice.RandomBelow=null;Yes(Dice.RollDice(0,0)==0,"zero dice no adapter");
  try{Dice.RollDamageDice(7,6);throw new Exception("missing RNG accepted");}catch(InvalidOperationException){Yes(GDVars.gDamageDice==7,"damage dice count before failure");}
  GDVars.pendingDamage=42;
  for(int n=0;n<=255;n++)for(int sides=0;sides<=255;sides++)
  {
   int calls=0,nn=n,ss=sides;Dice.RandomBelow=b=>{calls++;if(calls>nn)throw new Exception("wrapped byte loop at n="+nn);Yes(b==ss,"RNG bound passed");return b==0?0:b-1;};
   byte total=Dice.RollDice((byte)n,(byte)sides);
   Yes(calls==n,"exact count");Yes(total==((n*(sides==0?1:sides))&255),"byte sum wrap");
  }
  Dice.RandomBelow=b=>0;Yes(Dice.RollDamageDice(255,8)==255&&GDVars.gDamageDice==255&&GDVars.pendingDamage==42,"damage count no pending mutation");Dice.RandomBelow=null;
 }
 static void PartyChecks()
 {
  GDVars.zone1=new Types.TGbvmZone1();var a=new Types.TCharRecord();var b=new Types.TCharRecord();var c=new Types.TCharRecord();a.nextCharAddr=b;b.nextCharAddr=c;GDVars.creatureList=a;
  foreach(ushort n in new ushort[]{0,1,2,3,8,32767,32768,65535})
  {GDVars.zone1[C_Gbvm.PARTY_SIZE]=n;short size=unchecked((short)n);Yes(Party.IsPartyMember(a)==(size>=1),"signed party count a");Yes(Party.IsPartyMember(b)==(size>=2),"signed party count b");Yes(Party.IsPartyMember(c)==(size>=3),"signed party count c");}
  Yes(!Party.IsPartyMember(null),"nil membership");Yes(ReferenceEquals(Party.PrevCreature(a),c),"prev wraps");Yes(ReferenceEquals(Party.NextCreature(c),a),"next wraps");Yes(Party.PrevCreature(new Types.TCharRecord())==null,"unknown previous");
  GDVars.curChar=a;Party.SelectPrevNextChar((char)C_Keys.UP);Yes(ReferenceEquals(GDVars.curChar,c),"navigation previous callback");
  Party.SelectPrevNextChar((char)C_Keys.DOWN);Yes(ReferenceEquals(GDVars.curChar,a),"navigation next callback");
  Party.SelectPrevNextChar('x');Yes(ReferenceEquals(GDVars.curChar,a),"other key selects head");
 }
 public static void Main(){Run("source public API signatures",SourceApiChecks.Run);Run("dice width/boundary corpus",DiceChecks);Run("party identity and original signatures",PartyChecks);Run("numeric boundary source contracts",NumericChecks.Run);Run("rules/stats source checks",()=>{count+=RulesVerification.Run();});Run("archive loader smoke",DaxRuntimeTests.Main);Run("archive metadata/platform fixtures",ArchiveDeepChecks.Run);Run("stats packed records and snapshots",StatsDeepChecks.Run);Console.WriteLine("Integration counted assertions="+count+" failed groups="+failed);if(failed!=0)Environment.Exit(1);}
}
