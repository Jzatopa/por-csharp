using System;
using System.IO;
using Por.Core;

// Independently authored checks, not model-generated acceptance tests.
public static class RulesVerification
{
    static int checks;
    static void Eq(long expected,long actual,string label) { checks++; if(expected!=actual) throw new Exception(label+": expected "+expected+", got "+actual); }
    static void Yes(bool value,string label) { checks++; if(!value) throw new Exception(label); }
    public static int Run()
    {
        checks=0;
        GDVars.zone1 = new Types.TGbvmZone1();
        GDVars.treasureMoney = new Types.TMoney();
        var m=new Types.TMoney();
        m[C_Money.COPPER]=1; m[C_Money.SILVER]=2; m[C_Money.ELECTRUM]=3; m[C_Money.GOLD]=4; m[C_Money.PLATINUM]=5;
        // Original table: copper1 silver10 electrum100 gold200 platinum1000.
        Eq(6121,MoneyCal.CopperValue(m),"mixed coin copper");
        Eq(30,MoneyCal.CoinValueInGold(m),"whole gold truncation");
        m[C_Money.GEMS]=9; m[C_Money.JEWELRY]=7;
        MoneyCal.MakeChange(12345,65535,m);
        Eq(12,m[C_Money.PLATINUM],"change platinum"); Eq(1,m[C_Money.GOLD],"change gold"); Eq(1,m[C_Money.ELECTRUM],"change electrum"); Eq(4,m[C_Money.SILVER],"change silver"); Eq(5,m[C_Money.COPPER],"change copper"); Eq(9,m[C_Money.GEMS],"change keeps gems"); Eq(7,m[C_Money.JEWELRY],"change keeps jewelry");
        MoneyCal.MakeChange(-1,65535,m); Eq(0,MoneyCal.CopperValue(m),"negative change");
        MoneyCal.MakeChange(999999,1,m); Eq(1311,MoneyCal.CopperValue(m),"coin caps lose excess");
        var item=new Types.TItemRec(); item.itemType=9; item.quantity=20; item.value=100; Eq(100,MoneyCal.ItemValue(item),"arrows bundle"); item.quantity=0; Eq(5,MoneyCal.ItemValue(item),"zero bundle quantity means one"); item.itemType=1; item.quantity=3; item.value=400; Eq(1200,MoneyCal.ItemValue(item),"unbundled stack"); Eq(600,MoneyCal.ItemSalePrice(item),"sale halves value"); item.quantity=255; item.value=65535; Eq(65535,MoneyCal.ItemSalePrice(item),"sale price Word cap");
        string label="previous"; Eq(C_Money.GEMS,MoneyCal.ParseMoneyType("   Gems",ref label),"parse Gems"); Yes(label=="Gems ","Gems display space"); Eq(C_Money.GOLD,MoneyCal.ParseMoneyType("Gold",ref label),"parse Gold");
        Eq(1,ClassCal.DivideAmongClasses(3,-5,1),"negative hp floor"); Eq(4,ClassCal.DivideAmongClasses(10,-2,2),"divide hp penalty"); Eq(6,ClassCal.DivideAmongClasses(10,2,2),"divide hp bonus");
        for(int con=0;con<=255;con++) Eq(con<19?1:con<21?2:con<23?3:4,ClassCal.HitDieFloor((byte)con),"hit die floor");
        Eq(-2,ClassCal.ConHpPerDie(C_Level.FIGHTER,0),"constitution low clamp"); Eq(7,ClassCal.ConHpPerDie(C_Level.FIGHTER,30),"constitution high clamp"); Eq(2,ClassCal.ConHpPerDie(C_Level.MAGE,25),"mage no warrior bonus");
        var c=new Types.TCharRecord(); c.race=C_Race.HUMAN; c.classLevels[C_Level.FIGHTER]=1; ClassCal.RecalcBaseSkills(c); Eq(40,c.thac0Base,"fighter1 thac0"); Eq(2,c.attacks,"fighter1 attacks"); c.classLevels[C_Level.FIGHTER]=12; ClassCal.RecalcBaseSkills(c); Eq(49,c.thac0Base,"thac0 clamp10"); Eq(3,c.attacks,"fighter12 attacks"); Eq(8,c.saves[0],"saves clamp9");
        c=new Types.TCharRecord(); c.race=C_Race.HUMAN; c.classLevels[C_Level.CLERIC]=4; c.wisCur=17; ClassCal.RecalcBaseSkills(c); Eq(5,c.clericSpells[1],"cleric wisdom slots1"); Eq(4,c.clericSpells[2],"cleric wisdom slots2"); Eq(0,c.clericSpells[3],"no nonexistent slots"); Eq(1,c.spellsKnown[22],"Find Traps known"); Eq(0,c.spellsKnown[36],"Animate Dead unknown");
        c=new Types.TCharRecord(); c.race=C_Race.DWARF; c.classLevels[C_Level.THIEF]=1; c.dexCur=3; ClassCal.RecalcThiefSkills(c); int[] skill={0,15,25,25,0,0,10,75,0}; for(int i=1;i<=8;i++) Eq(skill[i],c.thiefSkills[i],"thief skill"+i);
        c=new Types.TCharRecord(); c.charClass=C_Class.CLERIC_FIGHTER; c.classLevels[C_Level.CLERIC]=2; c.classLevels[C_Level.FIGHTER]=2; c.conCur=10; Eq(18,ClassCal.HpMaximum(c),"multiclass maximum"); Eq(5,ClassCal.ConHpTotal(c,17),"multiclass Constitution total"); Eq(50,ClassCal.ExperienceShare(c,101),"multiclass share truncation");
        c.charClass=C_Class.FIGHTER; c.strCur=16; Eq(110,ClassCal.ExperienceShare(c,100),"fighter prime bonus");
        // Fresh party graph; monsters follow members but must never acquire member stats.
        var monster=new Types.TCharRecord(); monster.name="KOBOLD"; monster.status=C_Status.DYING;
        var b=new Types.TCharRecord(); b.name="ANNA"; b.nextCharAddr=monster;
        var a=new Types.TCharRecord(); a.name="TALONIA"; a.nextCharAddr=b; a.randByte=91;
        GDVars.creatureList=a; GDVars.zone1[C_Gbvm.PARTY_SIZE]=2; GDVars.ifcState=C_Ifc.COMBAT;
        Yes(Party.IsPartyMember(a)&&Party.IsPartyMember(b)&&!Party.IsPartyMember(monster)&&!Party.IsPartyMember(null),"bounded membership");
        Stats.ResetGame(); Stats.Count(a,Stats.SPELLS_CAST,int.MaxValue-1); Stats.Count(a,Stats.SPELLS_CAST,5); Eq(int.MaxValue,Stats.BlockOf(a).counts[Stats.SPELLS_CAST],"saturate stat"); Stats.Count(a,Stats.RAISED,-3); Eq(0,Stats.BlockOf(a).counts[Stats.RAISED],"ignore negative Count"); Yes(Stats.BlockOf(monster)==null,"no monster stats");
        Stats.Damaged(a,monster,7,true); Eq(7,Stats.BlockOf(a).counts[Stats.DAMAGE_DONE],"damage done"); Eq(1,Stats.BlockOf(a).counts[Stats.KILLS],"kill"); Yes(Stats.BlockOf(a).kills[1].name=="KOBOLD","kind name");
        for(int i=1;i<=50;i++){monster.name="BEAST"+i; Stats.Damaged(a,monster,1,true);} Eq(48,Stats.BlockOf(a).kindCount,"kind cap"); Eq(4,Stats.BlockOf(a).kills[48].count,"OTHER aggregation");
        a.coins[C_Money.GOLD]=10; b.coins[C_Money.SILVER]=50; Stats.MarkCoins(); a.coins[C_Money.GOLD]=15; b.coins[C_Money.SILVER]=0; Stats.SettleCoins(); Eq(1000,Stats.BlockOf(a).counts[Stats.COIN_LOOTED],"net coin gain"); Eq(-500,Stats.BlockOf(b).counts[Stats.COIN_LOOTED],"net coin loss");
        string path=Path.Combine(Path.GetTempPath(),"por-stats-"+Guid.NewGuid().ToString("N"));
        try {
            Stats.WriteChar(a,path); byte[] raw=File.ReadAllBytes(path+".sts"); Eq(1032,raw.Length,"TP packed file size"); Eq(4,raw[0],"signature length"); Yes(raw[1]==83&&raw[2]==84&&raw[3]==65&&raw[4]==84,"STAT signature"); Eq(1,raw[5],"version offset"); Eq(7,raw[6],"owner name length"); Eq(91,raw[22],"randByte offset"); Eq(51,ReadInt(raw,23),"kill counter offset23"); Eq(48,raw[71],"kind count offset71"); Eq(6,raw[72],"kill name length offset72"); Eq(1,ReadInt(raw,88),"kill count offset88");
            Stats.Forget(a); Stats.ReadChar(a,path); Eq(51,Stats.BlockOf(a).counts[Stats.KILLS],"character file reload"); Stats.Count(a,Stats.KILLS,1); Eq(52,Stats.BlockOf(a).counts[Stats.KILLS],"post-reload increment");
            Stats.WriteGame(path+".game"); Stats.ResetGame(); Stats.ReadGame(path+".game"); Eq(52,Stats.game.counts[Stats.KILLS],"game file reload");
            raw[5]=2; File.WriteAllBytes(path+".sts",raw); Stats.ReadChar(a,path); Eq(0,Stats.BlockOf(a).counts[Stats.KILLS],"wrong version rejected");
            File.WriteAllBytes(path+".game",new byte[3]); Stats.ReadGame(path+".game"); Eq(0,Stats.game.counts[Stats.KILLS],"short game file clears totals");
        } finally { File.Delete(path+".sts"); File.Delete(path+".game"); }
        return checks;
    }
    static int ReadInt(byte[] bytes,int offset) { return bytes[offset]|bytes[offset+1]<<8|bytes[offset+2]<<16|bytes[offset+3]<<24; }
}
