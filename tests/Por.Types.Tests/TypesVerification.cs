using System;
using System.Collections.Generic;
using System.Reflection;
using Por.Core;
public static class TypesVerification
{
    static int count, failures;
    static void Check(bool ok,string label) { count++; if(!ok)throw new Exception(label); }
    static void Case(string name,Action action) { try {action(); Console.WriteLine("PASS "+name);} catch(Exception ex){failures++;Console.WriteLine("FAIL "+name+": "+ex.Message);} }
    static void Reject(Action action) { try {action();} catch(IndexOutOfRangeException) {count++;return;} catch(ArgumentOutOfRangeException) {count++;return;} throw new Exception("Out-of-domain access accepted"); }
    static void RunAliases()
    {
        var icon=new Types.TIconCols();Check(icon.Data.Length==7,"TIconCols padding");icon[1]=18;icon[6]=255;Check(icon.Data[1]==18 && icon.Data[6]==255,"TIconCols source indices");Reject(()=>{icon[0]=0;});
        var money=new Types.TMoney();money[6]=int.MinValue;Check(money.Data.Length==7&&money[6]==int.MinValue,"LongInt jewelry");
        var targets=new Types.TTargetList();Check(targets.Data.Length==73,"TargetList padding");for(int i=1;i<=72;i++)Check(targets[i]!=null,"target value record initialized");Reject(()=>{var x=targets[0];});
        var spells=new Types.TSpellSlots();spells[20]=255;Check(spells.Data[20]==255,"last spell slot");Reject(()=>{var x=spells[21];});
        var table=new Types.TTurnUndeadTable();Check(table.Data.GetLength(0)==11&&table.Data.GetLength(1)==11,"turn table padding");table[10,10]=-128;Check(table.Data[10,10]==-128,"turn table signedness");
        var names=new Types.TGbStrings();Check(names.Data.Length==21,"string indices");for(int i=1;i<=20;i++)Check(names[i]==string.Empty,"empty DOS string");
        var z0=new Types.TGbvmZone0();z0[0x4cff]=65535;Check(z0[0x4cff]==65535,"zone0 highest label");Reject(()=>{var x=z0[0x48ff];});
        var b=new Types.TBytes();b[65000]=255;Check(b[65000]==255,"bytes bound");Reject(()=>{var x=b[65001];});
        var sb=new Types.TByteArr();sb[65534]=-128;Check(sb[65534]==-128,"signed byte buffer");
        var plane=new Types.TGeoPlane();plane[15,15]=255;Check(plane[15,15]==255,"geo plane extent");
        var wall=new Types.TView3dWallDef();wall[5,155]=18;Check(wall.Data.GetLength(0)==6&&wall.Data[5,155]==18,"wall source indices");
        var slices=new Types.TWallSlices();slices[15,155]=11;Check(slices.Data.GetLength(0)==16&&slices.Data[15,155]==11,"slice source indices");
    }
    static void RunItems()
    {
        var item=new Types.TItemRec();
        for(int value=0;value<256;value++)
        {
            item.charges=(byte)value;item.effect=(byte)(255-value);item.special=(byte)(value^85);
            Check(item.spells[1]==value&&item.spells[2]==255-value&&item.spells[3]==(value^85),"item fields->slots");
            item.spells[1]=(byte)(255-value);Check(item.charges==255-value,"item slots->fields");
        }
        Reject(()=>{var x=item.spells[0];});Reject(()=>{item.spells[4]=0;});
        var a=new Types.TAnimRec();for(int i=1;i<=8;i++)Check(a.frames[i]!=null,"frame initialized");
        Check(!ReferenceEquals(a.frames[1],a.frames[2]),"value records must be distinct");
        var geo=new Types.TGeoMap();geo.wallsNE[0,0]=9;Check(geo.wallsSW[0,0]==0,"distinct by-value geo planes");
        var ch=new Types.TCharRecord();item.next=item;ch.itemsAddr=item;Check(ReferenceEquals(ch.itemsAddr.next,item),"pointer identity/cycle");
    }
    static void RunCharacter()
    {
        var ch=new Types.TCharRecord();
        for(int value=0;value<256;value++)
        {
            ch.stats[0]=(byte)value;ch.stats[5]=(byte)(255-value);
            Check(ch.strCur==value&&ch.chaCur==255-value,"ability union write");
            ch.dexCur=(byte)value;Check(ch.stats[3]==value,"ability union read");
            ch.gender=(byte)value;Check(ch.genderSp==unchecked((sbyte)value)&&ch.genderIdx==value,"gender aliases");
            ch.genderSp=unchecked((sbyte)(255-value));Check(ch.gender==255-value,"signed gender write");
            ch.alignment=(byte)value;Check(ch.alignmentIdx==value,"alignment read");ch.alignmentIdx=(byte)(255-value);Check(ch.alignment==255-value,"alignment write");
            for(int i=1;i<=6;i++){ch.iconColors1[i]=(byte)value;Check(ch.iconColors[i-1]==value,"icon alias");}
            for(int i=1;i<=3;i++){ch.slots[0,i]=(byte)value;ch.mageSpells[i]=(byte)(255-value);Check(ch.clericSpells[i]==value&&ch.slots[1,i]==255-value,"slot aliases");}
        }
        Reject(()=>{var x=ch.stats[6];});Reject(()=>{ch.iconColors1[0]=0;});Reject(()=>{var x=ch.slots[2,1];});Reject(()=>{ch.slots[0,0]=0;});
    }
    static void RunRules()
    {
        var thief=new Types.TRuleThief();thief.pickPockets=-128;Check(thief.values[1]==-128,"thief overlay");thief.values[8]=127;Check(thief.readLanguages==127,"thief reverse");
        var dex=new Types.TRuleThiefDex();dex.values[5]=-9;Check(dex.hideShadows==-9,"dex overlay");
        var parts=new Types.TRuleClassParts();parts.first=-1;Check(parts.values[1]==-1,"class parts");
        var ab=new Types.TRuleAbilities();ab.charisma=-3;Check(ab.values[C_Abil.CHARISMA]==-3,"ability signed overlay");
        var levels=new Types.TRuleClassLevels();levels.values[C_Level.MONK]=-1;Check(levels.monk==-1,"class levels");
        var ages=new Types.TRuleAgeCategories();ages.venerable=65535;Check(ages.values[5]==65535,"age overlay");
        var saves=new Types.TRuleSaves();saves.spell=255;Check(saves.values[C_Save.SPELL]==255,"save overlay");
        var spell=new Types.TSpellRec();for(int value=0;value<256;value++)
        {spell.castClass=(byte)value;Check(spell.castClassSp==unchecked((sbyte)value),"spell signed read");spell.levelSp=unchecked((sbyte)value);Check(spell.level==value,"spell signed write");
         spell.savePrefix[7]=(byte)value;Check(spell.target==value,"savePrefix aliases target");spell.rangeVar=(byte)value;Check(spell.savePrefix[3]==value,"savePrefix reverse");
         spell.saveResult=(byte)value;Check(spell.saveEffect==(value!=0),"save boolean read");spell.saveEffect=true;Check(spell.saveResult==1,"save boolean true");spell.saveEffect=false;Check(spell.saveResult==0,"save boolean false");}
        Reject(()=>{var x=spell.savePrefix[8];});
    }
    static void RunWords()
    {
        var word=new Types.TGbvmWord();var clock=new Types.TGbvmClock();var walls=new Types.TGbvmSavedWalls();
        for(int value=0;value<65536;value++)
        {
            word.value=(ushort)value;Check(word.lo==(value&255)&&word.hi==(value>>8),"word split");word.lo=18;Check(word.value==((value&65280)|18),"lo union write");word.hi=52;Check(word.value==0x3412,"hi union write");
        }
        clock.values[6]=65535;Check(clock.year==65535,"clock array->field");clock.minute=0x1234;Check(clock.values[1]==0x1234,"clock field->array");
        walls.wallSetIds[1]=0x1234;Check(walls.menuPrefix[1]==0x1234,"walls prefix alias");walls.symbolSlots[3]=65535;Check(walls.menuAnimation==65535,"menu animation alias");walls.menuAnimation=123;Check(walls.symbolSlots[3]==123,"walls reverse alias");
        var zone=new Types.TGbvmZone0State();Check(zone.bytes.Length==2048,"zone byte size");
        for(int addr=0x4900;addr<=0x4cff;addr++){ushort v=(ushort)((addr*37)&65535);zone.words[addr]=v;int at=(addr-0x4900)*2;Check(zone.bytes[at]==(v&255)&&zone.bytes[at+1]==(v>>8),"label->byte offset");}
        zone.partyX.lo=0x81;Check(zone.words[0x49c3]==zone.partyX.value,"partyX alias");zone.bytes[392]=0xaa;Check(zone.partyY.lo==0xaa,"partyY byte alias");
        zone.clock.year=0x1234;Check(zone.words[0x49cc]==0x1234,"clock source word address");
        zone.savedWalls.menuAnimation=0x5678;Check(zone.words[0x4aff]==0x5678,"savedwalls source address");
        zone.entryWords[0x4a00]=0x1298;Check(zone.words[0x4a00]==0x1298,"entryWords alias");
        zone.scriptWords4B00[0x4cff]=0xabcd;Check(zone.bytes[2046]==0xcd&&zone.bytes[2047]==0xab,"last word alias");
        Reject(()=>{var x=zone.words[0x48ff];});Reject(()=>{zone.words[0x4d00]=0;});
    }
    public static void Main()
    {
        Case("Source record field inventory",SourceFieldChecks.Run);
        Case("Ability rules using translated character",()=>{count+=PortVerification.RulesChecks.Run();});
        Case("Source scalar widths",()=>{count+=SourceWidths.Run();});
        Case("Source inline arrays",()=>{count+=SourceArrays.Run();});
        Case("Source pointer representations",()=>{count+=SourcePointers.Run();});
        Case("All source alias bounds",()=>{count+=SourceAliasBounds.Run();});Case("Original-index aliases",RunAliases);Case("Item and by-value records",RunItems);Case("Character unions",RunCharacter);Case("Rules/spell unions",RunRules);Case("GBVM byte/word unions",RunWords);Case("Source-derived GBVM layout",()=>{count+=SourceZoneLayout.Run();});
        Console.WriteLine("Types assertions passed before group failures: "+count+"; failed groups: "+failures);
        if(failures!=0)Environment.Exit(1);
    }
}
