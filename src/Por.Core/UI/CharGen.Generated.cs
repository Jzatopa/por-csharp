// Structurally transcribed from pinned chargen.pas by scripts/translate-chargen.py; reviewed and source-tested.
using System;using System.Linq;using static Por.Core.Types;
namespace Por.Core{public static partial class CharGen{
private static byte stingMode=default(byte);
private static int RACE_SPLIT_COL=17;
private static int RACE_TOP_BOTTOM=8;
private static int RACE_LIST_ROW=2;





public static void MainMenu(){
int MENU_CREATE=0;
int MENU_DROP=1;
int MENU_MODIFY=2;
int MENU_TRAIN=3;
int MENU_VIEW=4;
int MENU_ADD=5;
int MENU_REMOVE=6;
int MENU_LOAD=7;
int MENU_SAVE=8;
int MENU_BEGIN=9;
int MENU_SETTINGS=10;
int MENU_QUIT=11;
byte i=default(byte);
char ch=default(char);
bool ext=default(bool);
bool ext2=default(bool);
sbyte n=default(sbyte);
bool redraw=default(bool);
byte stateB=default(byte);
bool quit=default(bool);

{
GDVars.virtualDisk=unchecked((byte)(3));
stateB=unchecked((byte)(C_Ifc.INDOORS));
redraw=true;
stingMode=unchecked((byte)(0));
GDVars.ifcState=unchecked((byte)(C_Ifc.MAIN_MENU));
while(true){
if(redraw){
DrawWindow(unchecked((sbyte)(C_Msg.TEXT_LEFT)),unchecked((sbyte)(C_Msg.TEXT_TOP)),unchecked((sbyte)(C_Msg.TEXT_RIGHT)),unchecked((sbyte)(C_Msg.TEXT_BOTTOM)),unchecked((byte)(0)),unchecked((byte)(0)),"");
if((GDVars.curChar!=null)){
DisplayParty(GDVars.curChar);
GDConsts.menuItems[MENU_DROP].enabled=unchecked((byte)(1));
GDConsts.menuItems[MENU_MODIFY].enabled=unchecked((byte)(1));
if((((GDVars.zone1[C_Gbvm.TRAIN_CLASS_MASK]>0))||((stingMode!=0)))){GDConsts.menuItems[MENU_TRAIN].enabled=unchecked((byte)(1));}
GDConsts.menuItems[MENU_VIEW].enabled=unchecked((byte)(1));
GDConsts.menuItems[MENU_REMOVE].enabled=unchecked((byte)(1));
GDConsts.menuItems[MENU_LOAD].enabled=unchecked((byte)(0));
GDConsts.menuItems[MENU_SAVE].enabled=unchecked((byte)(1));
GDConsts.menuItems[MENU_BEGIN].enabled=unchecked((byte)(1));
}else {
GDConsts.menuItems[MENU_DROP].enabled=unchecked((byte)(0));
GDConsts.menuItems[MENU_MODIFY].enabled=unchecked((byte)(0));
GDConsts.menuItems[MENU_TRAIN].enabled=unchecked((byte)(0));
GDConsts.menuItems[MENU_VIEW].enabled=unchecked((byte)(0));
GDConsts.menuItems[MENU_REMOVE].enabled=unchecked((byte)(0));
GDConsts.menuItems[MENU_LOAD].enabled=unchecked((byte)(1));
GDConsts.menuItems[MENU_SAVE].enabled=unchecked((byte)(0));
GDConsts.menuItems[MENU_BEGIN].enabled=unchecked((byte)(0));
}
n=unchecked((sbyte)(0));
for(int __g1=MENU_CREATE;__g1<=MENU_QUIT;__g1++){i=unchecked((byte)(__g1));if((GDConsts.menuItems[i].enabled!=0)){
DisplayString(unchecked((byte)(2)),unchecked((byte)((n+12))),unchecked((byte)(15)),Convert.ToString(GDConsts.menuItems[i].name[1-1]));
DisplayString(unchecked((byte)(3)),unchecked((byte)((n+12))),unchecked((byte)(10)),Copy(GDConsts.menuItems[i].name,2,(GDConsts.menuItems[i].name.Length-1)));
n=unchecked((sbyte)((n+1)));
}}
redraw=false;
}
ch=unchecked((char)(Horizontal(("pool of radiance "+GDConsts.GAME_VERSION),GDConsts.menuKeys,unchecked((byte)(5)),unchecked((byte)(0)),unchecked((byte)(0)),true,false,ref ext)));
ClearBottomLine();
if(ext){
if(((ch==C_Keys.HOME)||(ch==C_Keys.END_KEY)||(ch==C_Keys.UP)||(ch==C_Keys.DOWN))){
Party.SelectPrevNextChar(ch);
DisplayParty(GDVars.curChar);
}
}else {
if(!((((ch=='E')||(ch=='S'))))){GDVars.recentSaved=unchecked((byte)(0));}
{var __g2=ch;if(__g2=='C'){if((GDConsts.menuItems[MENU_CREATE].enabled!=0)){CharGen.CreateCharacterMenu();}}else if(__g2=='D'){if((GDConsts.menuItems[MENU_DROP].enabled!=0)){CharGen.DropCharacterMenu();}}else if(__g2=='M'){if((GDConsts.menuItems[MENU_MODIFY].enabled!=0)){CharGen.ModifyCharacterMenu();}}else if(__g2=='T'){if((GDConsts.menuItems[MENU_TRAIN].enabled!=0)){CharGen.TrainingMenu();}}else if(__g2=='V'){if((GDConsts.menuItems[MENU_VIEW].enabled!=0)){CharGenView.ViewCharMenu(ref ext2);}}else if(__g2=='A'){if((GDConsts.menuItems[MENU_ADD].enabled!=0)){CharGen.AddCharacterMenu();}}else if(__g2=='R'){if((((GDConsts.menuItems[MENU_REMOVE].enabled!=0))&&((GDVars.curChar!=null)))){
SaveCharCall(GDVars.curChar,"");
RemoveCharacter(false,true);
}}else if(__g2=='L'){if((GDConsts.menuItems[MENU_LOAD].enabled!=0)){
SaveLoad.LoadGame();
if((GDVars.ifcState!=C_Ifc.MAIN_MENU)){
GDVars.loadedSave=unchecked((byte)(1));
return;
}
}}else if(__g2=='S'){if((((GDConsts.menuItems[MENU_SAVE].enabled!=0))&&((GDVars.creatureList!=null)))){SaveLoad.SaveGame();}}else if(__g2=='B'){if((((GDConsts.menuItems[MENU_BEGIN].enabled!=0))&&((GDVars.creatureList!=null)))){
GDVars.ifcState=stateB;
ClearBottomLine();
return;
}}else if(__g2=='G'){if((GDConsts.menuItems[MENU_SETTINGS].enabled!=0)){Options.OptionsMenu();}}else if(__g2=='E'){if((GDConsts.menuItems[MENU_QUIT].enabled!=0)){
ch=unchecked((char)(YesNo("Quit to DOS ",unchecked((byte)(14)),unchecked((byte)(10)),unchecked((byte)(15)))));
if((ch=='Y')){
quit=true;
if((((GDVars.recentSaved==0))&&((GDVars.creatureList!=null)))){if((YesNo("Save first? ",unchecked((byte)(14)),unchecked((byte)(10)),unchecked((byte)(15)))=='Y')){
SaveLoad.SaveGame();
quit=(GDVars.recentSaved!=0);
}}
if(quit){GameIo.ExitGame();}
}
}}else if(__g2=='J'){if(GDVars.stingArg){
stingMode=unchecked((byte)(((stingMode==0)?1:0)));
if((stingMode!=0)){PrintBottomLine("Free training on");}else {PrintBottomLine("Free training off");}
}}}
redraw=true;
}
}
}
}
public static void ShowRaceInfo(sbyte race){
string[] ABIL_LABELS=new string[]{"STR","INT","WIS","DEX","CON","CHA"};
string[] PART_ABBREVS=new string[]{"CL","DR","FI","PA","RA","MU","TH","MO"};
int ABIL_COL=19;
int ADJ_COL=23;
int HEADING_ROW=11;
int FIRST_ROW=13;
int LAST_ROW=19;
int NOTE_ROW=20;
int LAST_MULTI_ROW=21;
int MAX_COL=2;
int MAX_VALUE_COL=3;
int CLASS_COL=7;
int MULTI_COL=21;
int LAST_COL=38;
sbyte i=default(sbyte);
byte slot=default(byte);
byte cnt=default(byte);
sbyte cls=default(sbyte);
byte row=default(byte);
sbyte adj=default(sbyte);
string s="";
byte p=default(byte);
sbyte part=default(sbyte);
sbyte lvl=default(sbyte);
bool hasMulti=default(bool);
bool[] isSingle=new bool[(C_Level.MONK)+1];

{
ClearBox(unchecked((sbyte)(ADJ_COL)),unchecked((sbyte)(RACE_LIST_ROW)),unchecked((sbyte)(LAST_COL)),unchecked((sbyte)((RACE_LIST_ROW+C_Abil.CHARISMA))));
ClearBox(unchecked((sbyte)(1)),unchecked((sbyte)(HEADING_ROW)),unchecked((sbyte)(LAST_COL)),unchecked((sbyte)((NOTE_ROW+1))));
if((((race<C_Race.DWARF))||((race>C_Race.HUMAN)))){return;}
for(int __g3=C_Abil.STRENGTH;__g3<=C_Abil.CHARISMA;__g3++){i=unchecked((sbyte)(__g3));{
DisplayString(unchecked((byte)(ABIL_COL)),unchecked((byte)((RACE_LIST_ROW+i))),unchecked((byte)(10)),ABIL_LABELS[i]);
adj=GDConsts.raceAbilityAdj[race].values[i];
if((adj>0)){s=('+'+StrUtil.NumStr(unchecked((int)(adj))));}else {if((adj<0)){s=('-'+StrUtil.NumStr(unchecked((int)(-(adj)))));}else {s=" 0";}}
DisplayString(unchecked((byte)(ADJ_COL)),unchecked((byte)((RACE_LIST_ROW+i))),unchecked((byte)(10)),s);
}}
Array.Fill(isSingle,(0!=0),0,(C_Level.MONK-C_Level.CLERIC+1));
hasMulti=false;
cnt=unchecked((byte)(GDConsts.classesByRace[race].count));
if((cnt>13)){cnt=unchecked((byte)(13));}
row=unchecked((byte)(FIRST_ROW));
for(int __g4=1;__g4<=cnt;__g4++){slot=unchecked((byte)(__g4));{
cls=GDConsts.classesByRace[race].classes[slot];
if((((cls>=C_Class.CLERIC))&&((cls<=C_Class.MAGE_THIEF)))){
s="";
for(int __g5=1;__g5<=3;__g5++){p=unchecked((byte)(__g5));{
part=GDConsts.classParts[cls].values[p];
if((((part>=C_Level.CLERIC))&&((part<=C_Level.MONK)))){
isSingle[part]=true;
if((s!="")){s=(s+'/');}
s=(s+PART_ABBREVS[part]);
}
}}
if((((cls>C_Class.MONK))&&((row<=LAST_MULTI_ROW)))){
hasMulti=true;
DisplayString(unchecked((byte)(MULTI_COL)),row,unchecked((byte)(10)),Copy(s,1,((LAST_COL-MULTI_COL)+1)));
row=unchecked((byte)((row+1)));
}
}
}}
DisplayString(unchecked((byte)(MAX_COL)),unchecked((byte)(HEADING_ROW)),unchecked((byte)(13)),"Max");
DisplayString(unchecked((byte)(CLASS_COL)),unchecked((byte)(HEADING_ROW)),unchecked((byte)(13)),"Class");
if(hasMulti){DisplayString(unchecked((byte)(MULTI_COL)),unchecked((byte)(HEADING_ROW)),unchecked((byte)(13)),"Multiclasses");}
row=unchecked((byte)(FIRST_ROW));
for(int __g6=C_Level.CLERIC;__g6<=C_Level.MONK;__g6++){part=unchecked((sbyte)(__g6));if((isSingle[part]&&((row<=LAST_ROW)))){
lvl=GDConsts.raceMaxLevel[race].values[part];
if((lvl==0)){s=Convert.ToString('U');}else {if((lvl>0)){s=StrUtil.NumStr(unchecked((int)(lvl)));}else {s=Convert.ToString('-');}}
DisplayString(unchecked((byte)(MAX_VALUE_COL)),row,unchecked((byte)(10)),s);
DisplayString(unchecked((byte)(CLASS_COL)),row,unchecked((byte)(10)),Copy(GDConsts.classNames[part],1,((MULTI_COL-CLASS_COL)-1)));
row=unchecked((byte)((row+1)));
}}
if(hasMulti){
DisplayString(unchecked((byte)(MAX_COL)),unchecked((byte)(NOTE_ROW)),unchecked((byte)(10)),"Multiclass levels");
DisplayString(unchecked((byte)((MAX_COL+2))),unchecked((byte)((NOTE_ROW+1))),unchecked((byte)(10)),"are as above.");
}
}
}
public static void CreateCharacterMenu(){
TCharRecord creature=null;
TCharRecord savedCur=null;
TItemRec list=null;
TItemRec m=null;
short sel=default(short);
byte slot=default(byte);
sbyte j=default(sbyte);
byte roll=default(byte);
char ch=default(char);
sbyte k=default(sbyte);
sbyte g=default(sbyte);
sbyte cls=default(sbyte);
sbyte i=default(sbyte);
byte cnt=default(byte);
short conB=default(short);
byte clsCount=default(byte);
byte hpTotal=default(byte);
bool addExit=default(bool);
bool redraw=default(bool);
void ClampToLimits(sbyte __p_i){


{
{
if((creature.stats[__p_i]<ClassCal.RaceAbilityMin(creature.race,creature.genderSp,__p_i))){creature.stats[__p_i]=ClassCal.RaceAbilityMin(creature.race,creature.genderSp,__p_i);}
if((creature.stats[__p_i]>ClassCal.RaceAbilityMax(creature.race,creature.genderSp,__p_i))){creature.stats[__p_i]=ClassCal.RaceAbilityMax(creature.race,creature.genderSp,__p_i);}
if((creature.stats[__p_i]<GDConsts.classMin[creature.charClass].values[__p_i])){creature.stats[__p_i]=unchecked((byte)(GDConsts.classMin[creature.charClass].values[__p_i]));}
}
}
}
bool PickFromList(){
bool __result=default(bool);
TItemRec picked=null;
char key=default(char);
bool withExit=default(bool);
bool repaint=default(bool);

{
__result=true;
picked=list;
sel=unchecked((short)(1));
withExit=true;
repaint=true;
do{
key=unchecked((char)(Vertical("","",unchecked((byte)(13)),unchecked((byte)(10)),unchecked((byte)(15)),unchecked((sbyte)(1)),unchecked((sbyte)(2)),unchecked((sbyte)(38)),unchecked((sbyte)(22)),list,withExit,ref repaint,ref sel,ref picked)));
if((key==C_Keys.NONE)){
MenuCore.FreeStringList(ref list);
FreeCreatureCall(creature);
__result=false;
return __result;
}
}while(!((key==C_Keys.ENTER)));
return __result;
}
}
{
creature=new TCharRecord();
creature=new TCharRecord();
{
for(int __g7=1;__g7<=6;__g7++){slot=unchecked((byte)(__g7));creature.iconColors1[slot]=unchecked((byte)((GDConsts.defIconCol[slot]+(((GDConsts.defIconCol[slot]+8))*16))));}
creature.acBase=unchecked((byte)(50));
creature.thac0Base=unchecked((byte)(40));
creature.status=unchecked((byte)(C_Status.OKAY));
creature.enabled=unchecked((byte)(1));
creature.portraitHead=unchecked((byte)(1));
creature.portraitBody=unchecked((byte)(1));
creature.iconDimensions=unchecked((byte)(1));
creature.randByte=unchecked((byte)(RandomBelow(256)));
}
MenuCore.AllocStringList(unchecked((short)(7)),ref list);
list.inUse=unchecked((byte)(1));
list.name="Pick Race";
for(int __g8=1;__g8<=7;__g8++){k=unchecked((sbyte)(__g8));{var __g9=k;if((__g9>=C_Race.DWARF&&__g9<=C_Race.HALFLING)){
m=MenuCore.FindListMember(unchecked((short)(k)),list);
m.name=("  "+GDConsts.raceNames[k]);
}else if(__g9==C_Race.HUMAN){
m=MenuCore.FindListMember(unchecked((short)((k-1))),list);
m.name=("  "+GDConsts.raceNames[k]);
}}}
ClearBox(unchecked((sbyte)(C_Msg.TEXT_LEFT)),unchecked((sbyte)(C_Msg.TEXT_TOP)),unchecked((sbyte)(C_Msg.TEXT_RIGHT)),unchecked((sbyte)(C_Msg.TEXT_BOTTOM)));
DrawBorder(unchecked((sbyte)(1)),unchecked((sbyte)((RACE_TOP_BOTTOM+2))),unchecked((sbyte)(38)),unchecked((sbyte)(22)),unchecked((byte)(0)));
DrawBorder(unchecked((sbyte)(1)),unchecked((sbyte)(1)),unchecked((sbyte)((RACE_SPLIT_COL-1))),unchecked((sbyte)(RACE_TOP_BOTTOM)),unchecked((byte)(0)));
DrawBorder(unchecked((sbyte)((RACE_SPLIT_COL+1))),unchecked((sbyte)(1)),unchecked((sbyte)(38)),unchecked((sbyte)(RACE_TOP_BOTTOM)),unchecked((byte)(0)));
CharGen.ShowRaceInfo(unchecked((sbyte)(C_Race.DWARF)));
m=list;
sel=unchecked((short)(1));
GDVars.vertTop=unchecked((short)(1));
redraw=true;
addExit=true;
do{
MenuCore.movesToCaller=true;
ch=unchecked((char)(Vertical("","",unchecked((byte)(13)),unchecked((byte)(10)),unchecked((byte)(15)),unchecked((sbyte)(1)),unchecked((sbyte)(RACE_LIST_ROW)),unchecked((sbyte)((RACE_SPLIT_COL-1))),unchecked((sbyte)((RACE_LIST_ROW+5))),list,addExit,ref redraw,ref sel,ref m)));
if((ch==C_Keys.NONE)){
MenuCore.FreeStringList(ref list);
FreeCreatureCall(creature);
return;
}
if((ch!=C_Keys.ENTER)){if((sel==6)){CharGen.ShowRaceInfo(unchecked((sbyte)(C_Race.HUMAN)));}else {CharGen.ShowRaceInfo(unchecked((sbyte)(sel)));}}
}while(!((ch==C_Keys.ENTER)));
DrawWindow(unchecked((sbyte)(C_Msg.TEXT_LEFT)),unchecked((sbyte)(C_Msg.TEXT_TOP)),unchecked((sbyte)(C_Msg.TEXT_RIGHT)),unchecked((sbyte)(C_Msg.TEXT_BOTTOM)),unchecked((byte)(0)),unchecked((byte)(0)),"");
if((sel==6)){sel=unchecked((short)((sel+1)));}
{
creature.race=unchecked((sbyte)(sel));
{var __g10=creature.race;if(__g10==C_Race.HALFLING){
creature.iconSize=unchecked((byte)(1));
FxEngine.AddEffect(creature,unchecked((byte)(C_Effect.CON_PSN_SAVE)),unchecked((ushort)(0)),unchecked((byte)(255)),false);
FxEngine.AddEffect(creature,unchecked((byte)(C_Effect.CON_SPELL_SAVE)),unchecked((ushort)(0)),unchecked((byte)(255)),false);
}else if(__g10==C_Race.DWARF){
creature.iconSize=unchecked((byte)(1));
FxEngine.AddEffect(creature,unchecked((byte)(C_Effect.CON_PSN_SAVE)),unchecked((ushort)(0)),unchecked((byte)(255)),false);
FxEngine.AddEffect(creature,unchecked((byte)(C_Effect.CON_SPELL_SAVE)),unchecked((ushort)(0)),unchecked((byte)(255)),false);
FxEngine.AddEffect(creature,unchecked((byte)(C_Effect.DWARF_THAC0)),unchecked((ushort)(0)),unchecked((byte)(255)),false);
FxEngine.AddEffect(creature,unchecked((byte)(C_Effect.DWARF_AC_GIANT)),unchecked((ushort)(0)),unchecked((byte)(255)),false);
}else if(__g10==C_Race.GNOME){
creature.iconSize=unchecked((byte)(1));
FxEngine.AddEffect(creature,unchecked((byte)(C_Effect.CON_SPELL_SAVE)),unchecked((ushort)(0)),unchecked((byte)(255)),false);
FxEngine.AddEffect(creature,unchecked((byte)(C_Effect.GNOME_THAC0)),unchecked((ushort)(0)),unchecked((byte)(255)),false);
FxEngine.AddEffect(creature,unchecked((byte)(C_Effect.DWARF_AC_GIANT)),unchecked((ushort)(0)),unchecked((byte)(255)),false);
FxEngine.AddEffect(creature,unchecked((byte)(C_Effect.GNOME_AC_BUG)),unchecked((ushort)(0)),unchecked((byte)(255)),false);
}else if(__g10==C_Race.ELF){
creature.iconSize=unchecked((byte)(2));
FxEngine.AddEffect(creature,unchecked((byte)(C_Effect.ELF_RESIST)),unchecked((ushort)(0)),unchecked((byte)(255)),false);
}else if(__g10==C_Race.HALFELF){
creature.iconSize=unchecked((byte)(2));
FxEngine.AddEffect(creature,unchecked((byte)(C_Effect.HALF_ELF_RESIST)),unchecked((ushort)(0)),unchecked((byte)(255)),false);
}else{creature.iconSize=unchecked((byte)(2));}}
}
MenuCore.FreeStringList(ref list);
MenuCore.AllocStringList(unchecked((short)(3)),ref list);
list.inUse=unchecked((byte)(1));
list.name="Pick Gender";
for(int __g11=0;__g11<=1;__g11++){g=unchecked((sbyte)(__g11));{
m=MenuCore.FindListMember(unchecked((short)((g+1))),list);
m.name=("  "+GDConsts.genderNames[g]);
}}
if(!(PickFromList())){return;}
creature.genderSp=unchecked((sbyte)((sel-1)));
MenuCore.FreeStringList(ref list);
cnt=unchecked((byte)(GDConsts.classesByRace[creature.race].count));
if((cnt>13)){cnt=unchecked((byte)(13));}
MenuCore.AllocStringList(unchecked((short)((cnt+1))),ref list);
list.inUse=unchecked((byte)(1));
list.name="Pick Class";
for(int __g12=1;__g12<=cnt;__g12++){slot=unchecked((byte)(__g12));{
m=MenuCore.FindListMember(unchecked((short)(slot)),list);
m.name=("  "+GDConsts.classNames[GDConsts.classesByRace[creature.race].classes[slot]]);
}}
if(!(PickFromList())){return;}
if((((sel<1))||((sel>cnt)))){sel=unchecked((short)(1));}
{
creature.charClass=GDConsts.classesByRace[creature.race].classes[sel];
creature.levelHighest=unchecked((sbyte)(1));
if((((creature.charClass>=C_Class.CLERIC))&&((creature.charClass<=C_Class.MAGE_THIEF)))){for(int __g13=1;__g13<=3;__g13++){slot=unchecked((byte)(__g13));if((((GDConsts.classParts[creature.charClass].values[slot]>=C_Level.CLERIC))&&((GDConsts.classParts[creature.charClass].values[slot]<=C_Level.MONK)))){creature.classLevels[GDConsts.classParts[creature.charClass].values[slot]]=unchecked((sbyte)(1));}}}
if((creature.charClass==C_Class.PALADIN)){FxEngine.AddEffect(creature,unchecked((byte)(C_Effect.PROT_EVIL)),unchecked((ushort)(0)),unchecked((byte)(255)),false);}
creature.itemLimits=unchecked((byte)(0));
creature.thac0Base=unchecked((byte)(0));
for(int __g14=C_Level.CLERIC;__g14<=C_Level.MONK;__g14++){cls=unchecked((sbyte)(__g14));if((creature.classLevels[cls]>0)){
if((GDConsts.thac0Tbl[cls,creature.classLevels[cls]]>creature.thac0Base)){creature.thac0Base=GDConsts.thac0Tbl[cls,creature.classLevels[cls]];}
creature.itemLimits=unchecked((byte)((creature.itemLimits|GDConsts.usabilityBit[cls])));
}}
}
ClassCal.RecalcSaves(creature);
MenuCore.FreeStringList(ref list);
cnt=unchecked((byte)(GDConsts.alignByClass[creature.charClass].count));
MenuCore.AllocStringList(unchecked((short)((cnt+1))),ref list);
list.inUse=unchecked((byte)(1));
list.name="Pick Alignment";
for(int __g15=1;__g15<=cnt;__g15++){slot=unchecked((byte)(__g15));{
m=MenuCore.FindListMember(unchecked((short)(slot)),list);
m.name=("  "+GDConsts.alignNames[GDConsts.alignByClass[creature.charClass].alignments[slot]]);
}}
if(!(PickFromList())){return;}
creature.alignment=unchecked((byte)(GDConsts.alignByClass[creature.charClass].alignments[sel]));
MenuCore.FreeStringList(ref list);
if((creature.charClass<=C_Class.MONK)){creature.age=unchecked((ushort)((Dice.RollDice(GDConsts.dsStartingAge[creature.race,creature.charClass].num,GDConsts.dsStartingAge[creature.race,creature.charClass].sides)+GDConsts.dsStartingAge[creature.race,creature.charClass].@base)));}else {var __g16=creature.charClass;if(__g16==C_Class.CLERIC_FIGHTER){creature.age=unchecked((ushort)((GDConsts.dsStartingAge[creature.race,C_Class.CLERIC].@base+(GDConsts.dsStartingAge[creature.race,C_Class.CLERIC].sides*GDConsts.dsStartingAge[creature.race,C_Class.CLERIC].num))));}else if(__g16==C_Class.CLERIC_FIGHTER_MAGE){creature.age=unchecked((ushort)((GDConsts.dsStartingAge[creature.race,C_Class.CLERIC].@base+(GDConsts.dsStartingAge[creature.race,C_Class.CLERIC].sides*GDConsts.dsStartingAge[creature.race,C_Class.CLERIC].num))));}else if(__g16==C_Class.CLERIC_RANGER){creature.age=unchecked((ushort)((GDConsts.dsStartingAge[creature.race,C_Class.CLERIC].@base+(GDConsts.dsStartingAge[creature.race,C_Class.CLERIC].sides*GDConsts.dsStartingAge[creature.race,C_Class.CLERIC].num))));}else if(__g16==C_Class.CLERIC_MAGE){creature.age=unchecked((ushort)((GDConsts.dsStartingAge[creature.race,C_Class.CLERIC].@base+(GDConsts.dsStartingAge[creature.race,C_Class.CLERIC].sides*GDConsts.dsStartingAge[creature.race,C_Class.CLERIC].num))));}else if(__g16==C_Class.CLERIC_THIEF){creature.age=unchecked((ushort)((GDConsts.dsStartingAge[creature.race,C_Class.CLERIC].@base+(GDConsts.dsStartingAge[creature.race,C_Class.CLERIC].sides*GDConsts.dsStartingAge[creature.race,C_Class.CLERIC].num))));}else if(__g16==C_Class.FIGHTER_MAGE){creature.age=unchecked((ushort)((GDConsts.dsStartingAge[creature.race,C_Class.MAGE].@base+(GDConsts.dsStartingAge[creature.race,C_Class.MAGE].sides*GDConsts.dsStartingAge[creature.race,C_Class.MAGE].num))));}else if(__g16==C_Class.FIGHTER_THIEF){creature.age=unchecked((ushort)((GDConsts.dsStartingAge[creature.race,C_Class.FIGHTER].@base+(GDConsts.dsStartingAge[creature.race,C_Class.FIGHTER].sides*GDConsts.dsStartingAge[creature.race,C_Class.FIGHTER].num))));}else if(__g16==C_Class.FIGHTER_MAGE_THIEF){creature.age=unchecked((ushort)((GDConsts.dsStartingAge[creature.race,C_Class.MAGE].@base+(GDConsts.dsStartingAge[creature.race,C_Class.MAGE].sides*GDConsts.dsStartingAge[creature.race,C_Class.MAGE].num))));}else if(__g16==C_Class.MAGE_THIEF){creature.age=unchecked((ushort)((GDConsts.dsStartingAge[creature.race,C_Class.MAGE].@base+(GDConsts.dsStartingAge[creature.race,C_Class.MAGE].sides*GDConsts.dsStartingAge[creature.race,C_Class.MAGE].num))));}}
savedCur=GDVars.curChar;
GDVars.curChar=creature;
CharGenView.DisplayChar(GDVars.curChar);
do{
creature.strExc=unchecked((byte)(0));
for(int __g17=0;__g17<=5;__g17++){i=unchecked((sbyte)(__g17));{
creature.stats[i]=unchecked((byte)(0));
for(int __g18=1;__g18<=6;__g18++){j=unchecked((sbyte)(__g18));{
roll=Dice.RollDice(unchecked((byte)(3)),unchecked((byte)(6)));
if((((creature.race>=C_Race.DWARF))&&((creature.race<=C_Race.HUMAN)))){roll=unchecked((byte)((roll+GDConsts.raceAbilityAdj[creature.race].values[i])));}
if((creature.stats[i]<roll)){creature.stats[i]=roll;}
}}
{var __g19=i;if(__g19==C_Abil.STRENGTH){
if((((creature.stats[i]>0))&&((GDConsts.ageCat[creature.race].youngAdult<creature.age)))){creature.stats[i]=unchecked((byte)((creature.stats[i]+1)));}
if((((creature.stats[i]>0))&&((GDConsts.ageCat[creature.race].mature<creature.age)))){creature.stats[i]=unchecked((byte)((creature.stats[i]-1)));}
if((((creature.stats[i]>0))&&((GDConsts.ageCat[creature.race].middleAged<creature.age)))){creature.stats[i]=unchecked((byte)((creature.stats[i]-2)));}
if((((creature.stats[i]>0))&&((GDConsts.ageCat[creature.race].old<creature.age)))){creature.stats[i]=unchecked((byte)((creature.stats[i]-1)));}
ClampToLimits(i);
if((((creature.stats[C_Abil.STRENGTH]==18))&&((CharHlp.WarriorLevel(creature)>0)))){
creature.strExc=unchecked((byte)((RandomBelow(100)+1)));
if((GDConsts.dsMinimumMaximumStatistics[creature.race].excMax[creature.genderSp]<creature.strExc)){creature.strExc=GDConsts.dsMinimumMaximumStatistics[creature.race].excMax[creature.genderSp];}
}
}else if(__g19==C_Abil.INTELLIGENCE){
if((GDConsts.ageCat[creature.race].mature<creature.age)){creature.stats[i]=unchecked((byte)((creature.stats[i]+1)));}
if((GDConsts.ageCat[creature.race].old<creature.age)){creature.stats[i]=unchecked((byte)((creature.stats[i]+1)));}
ClampToLimits(i);
}else if(__g19==C_Abil.WISDOM){
if((GDConsts.ageCat[creature.race].youngAdult>=creature.age)){creature.stats[i]=unchecked((byte)((creature.stats[i]-1)));}
if((GDConsts.ageCat[creature.race].mature<creature.age)){creature.stats[i]=unchecked((byte)((creature.stats[i]+1)));}
if((GDConsts.ageCat[creature.race].middleAged<creature.age)){creature.stats[i]=unchecked((byte)((creature.stats[i]+1)));}
if((GDConsts.ageCat[creature.race].old<creature.age)){creature.stats[i]=unchecked((byte)((creature.stats[i]+1)));}
ClampToLimits(i);
if((creature.stats[i]<13)){if(((creature.charClass>=C_Class.CLERIC_FIGHTER&&creature.charClass<=C_Class.CLERIC_THIEF))){creature.stats[i]=unchecked((byte)(13));}}
}else if(__g19==C_Abil.DEXTERITY){
if((GDConsts.ageCat[creature.race].middleAged<creature.age)){creature.stats[i]=unchecked((byte)((creature.stats[i]-2)));}
if((GDConsts.ageCat[creature.race].old<creature.age)){creature.stats[i]=unchecked((byte)((creature.stats[i]-1)));}
ClampToLimits(i);
}else if(__g19==C_Abil.CONSTITUTION){
if((GDConsts.ageCat[creature.race].mature<creature.age)){creature.stats[i]=unchecked((byte)((creature.stats[i]-1)));}
if((GDConsts.ageCat[creature.race].middleAged<creature.age)){creature.stats[i]=unchecked((byte)((creature.stats[i]-1)));}
if((GDConsts.ageCat[creature.race].old<creature.age)){creature.stats[i]=unchecked((byte)((creature.stats[i]-1)));}
ClampToLimits(i);
}else if(__g19==C_Abil.CHARISMA){
ClampToLimits(i);
}}
CharGenView.PrintStat(i,false);
}}
if((creature.classLevels[C_Level.THIEF]>0)){ClassCal.RecalcThiefSkills(creature);}
{
creature.hpCur=creature.hpMax;
creature.attacks=unchecked((byte)(2));
creature.unarmedRollsA[1]=unchecked((byte)(1));
creature.unarmedDiceA[1]=unchecked((byte)(2));
creature.useStrength=unchecked((byte)(1));
creature.movementBase=unchecked((byte)(12));
hpTotal=unchecked((byte)(0));
clsCount=unchecked((byte)(0));
for(int __g20=C_Level.CLERIC;__g20<=C_Level.MONK;__g20++){cls=unchecked((sbyte)(__g20));if((creature.classLevels[cls]>0)){
{var __g21=cls;if(__g21==C_Level.CLERIC){creature.clericSpells[1]=unchecked((byte)(1));}else if(__g21==C_Level.MAGE){creature.mageSpells[1]=unchecked((byte)(1));}}
hpTotal=unchecked((byte)((hpTotal+Dice.RollDice(GDConsts.dsStartingGoldClass[cls].num,GDConsts.dsStartingGoldClass[cls].sides))));
clsCount=unchecked((byte)((clsCount+1)));
{var __g22=cls;if(__g22==C_Level.CLERIC){
ClassCal.AwardWisdomBonus(creature);
for(int __g23=1;__g23<=(56/1);__g23++){slot=unchecked((byte)(__g23));if((((GDConsts.spellTable[slot].castClass==0))&&((GDConsts.spellTable[slot].level==1)))){creature.spellsKnown[slot]=unchecked((byte)(1));}}
}else if(__g22==C_Level.MAGE){
creature.spellsKnown[11]=unchecked((byte)(1));
creature.spellsKnown[18]=unchecked((byte)(1));
creature.spellsKnown[19]=unchecked((byte)(1));
creature.spellsKnown[21]=unchecked((byte)(1));
}}
}}
creature.coins[C_Money.GOLD]=unchecked((ushort)(((hpTotal*10)/clsCount)));
creature.hpRolled=ClassCal.RollFirstLevelHp(creature,unchecked((byte)(255)));
creature.hpMax=creature.hpRolled;
conB=unchecked((short)(ClassCal.ConHpBonus(creature,unchecked((byte)(255)))));
if((conB<0)){
if((creature.hpMax>(Math.Abs(conB)+clsCount))){creature.hpMax=unchecked((byte)((((creature.hpMax+conB))/clsCount)));}else {creature.hpMax=unchecked((byte)(1));}
}else {creature.hpMax=unchecked((byte)((((creature.hpMax+conB))/clsCount)));}
creature.hpCur=creature.hpMax;
creature.hpRolled=unchecked((byte)((creature.hpRolled/clsCount)));
}
CharGenView.PrintCombatStats();
CharGenView.PrintMoney();
ch=unchecked((char)(YesNo("Keep this character? ",unchecked((byte)(13)),unchecked((byte)(10)),unchecked((byte)(15)))));
}while(!((ch=='Y')));
CharGenView.DisplayChar(GDVars.curChar);
GDVars.curChar=savedCur;
do{
creature.name=ReadName("Character name: ",unchecked((byte)(13)),unchecked((byte)(Types.NAME_LEN)));
}while(!((creature.name!="")));
savedCur=GDVars.curChar;
GDVars.curChar=creature;
CharGen.CustomizePortrait();
CharGenView.DisplayChar(GDVars.curChar);
CharGen.CustomizeIcon();
SaveCharCall(creature,"");
GDVars.curChar=savedCur;
FreeCreatureCall(creature);
}
}
public static void DropCharacterMenu(){


{
if((GDVars.curChar==null)){return;}
{
if((((YesNo((("Drop "+GDVars.curChar.name)+" forever? "),unchecked((byte)(14)),unchecked((byte)(10)),unchecked((byte)(15)))=='Y'))&&((YesNo("Are you sure? ",unchecked((byte)(14)),unchecked((byte)(10)),unchecked((byte)(15)))=='Y')))){
if((GDVars.curChar.enabled==0)){PrintBottomLine((("You dump "+GDVars.curChar.name)+" out back."));}else {PrintBottomLine((GDVars.curChar.name+" bids you farewell."));}
RemoveCharacter(false,true);
}else {PrintBottomLine((GDVars.curChar.name+" breathes a sigh of relief."));}
}
}
}
public static bool Modifiable(){
bool __result=default(bool);


{
__result=(((((GDVars.curChar.race>=C_Race.DWARF))&&((GDVars.curChar.race<=C_Race.HUMAN)))&&((GDVars.curChar.charClass>=0)))&&((GDVars.curChar.charClass<=16)));
return __result;
}
}
public static void ModifyCharacterMenu(){


{
if((((GDVars.curChar.experience>0))||!(CharGen.Modifiable()))){PrintAndPause((GDVars.curChar.name+" can't be modified."),unchecked((byte)(14)));}else {CharGen.ModifyCharacter(false);}
}
}
public static void ModifyCharacter(bool levelled){
byte[] savedStats=new byte[(5)+1];
byte savedExc=default(byte);
byte savedHp=default(byte);
byte savedHpCur=default(byte);
byte[] savedClericSpells=new byte[(3)+1];
sbyte hold=default(sbyte);
char key=default(char);
bool ext=default(bool);
byte statIdx=default(byte);
byte oldCon=default(byte);
short hpShift=default(short);
void RedrawStat(bool __p_hotPink){


{
if((statIdx<6)){CharGenView.PrintStat(unchecked((sbyte)(statIdx)),__p_hotPink);}else {PrintHitPoints(GDVars.curChar,unchecked((byte)(4)),unchecked((byte)(18)),__p_hotPink);}
}
}
byte StatMinimum(byte __p_racialMin){
byte __result=default(byte);


{
if((GDConsts.classMin[GDVars.curChar.charClass].values[statIdx]>__p_racialMin)){__result=unchecked((byte)(GDConsts.classMin[GDVars.curChar.charClass].values[statIdx]));}else {__result=__p_racialMin;}
return __result;
}
}
void RaiseToMinimum(sbyte __p_a){


{
if((GDVars.curChar.stats[__p_a]<StatMinimum(ClassCal.RaceAbilityMin(GDVars.curChar.race,GDVars.curChar.genderSp,__p_a)))){GDVars.curChar.stats[__p_a]=StatMinimum(ClassCal.RaceAbilityMin(GDVars.curChar.race,GDVars.curChar.genderSp,__p_a));}
}
}
void LowerToMaximum(sbyte __p_a){


{
if((GDVars.curChar.stats[__p_a]>ClassCal.RaceAbilityMax(GDVars.curChar.race,GDVars.curChar.genderSp,__p_a))){GDVars.curChar.stats[__p_a]=ClassCal.RaceAbilityMax(GDVars.curChar.race,GDVars.curChar.genderSp,__p_a);}
}
}
void ShiftHp(){


{
{
hpShift=unchecked((short)((ClassCal.ConHpTotal(GDVars.curChar,GDVars.curChar.stats[C_Abil.CONSTITUTION])-ClassCal.ConHpTotal(GDVars.curChar,oldCon))));
if(((GDVars.curChar.hpMax+hpShift)<1)){hpShift=unchecked((short)((1-GDVars.curChar.hpMax)));}
GDVars.curChar.hpMax=unchecked((byte)((GDVars.curChar.hpMax+hpShift)));
if(((GDVars.curChar.hpCur+hpShift)<0)){GDVars.curChar.hpCur=unchecked((byte)(0));}else {GDVars.curChar.hpCur=unchecked((byte)((GDVars.curChar.hpCur+hpShift)));}
}
}
}
void StepHp(short __p_k){
short dmg=default(short);
short hp=default(short);

{
{
dmg=unchecked((short)((GDVars.curChar.hpMax-GDVars.curChar.hpCur)));
hp=unchecked((short)((GDVars.curChar.hpMax+__p_k)));
if((hp>ClassCal.HpMaximum(GDVars.curChar))){hp=unchecked((short)(ClassCal.HpMaximum(GDVars.curChar)));}
if((hp<ClassCal.HpMinimum(GDVars.curChar))){hp=unchecked((short)(ClassCal.HpMinimum(GDVars.curChar)));}
if((hp<1)){hp=unchecked((short)(1));}
GDVars.curChar.hpMax=unchecked((byte)(hp));
if(((hp-dmg)<0)){GDVars.curChar.hpCur=unchecked((byte)(0));}else {GDVars.curChar.hpCur=unchecked((byte)((hp-dmg)));}
}
}
}
{
if(!(CharGen.Modifiable())){
PrintAndPause((GDVars.curChar.name+" can't be modified."),unchecked((byte)(14)));
return;
}
CharGenView.DisplayChar(GDVars.curChar);
MoveBytes(GDVars.curChar.stats,savedStats,6);
savedExc=GDVars.curChar.strExc;
savedHp=GDVars.curChar.hpMax;
savedHpCur=GDVars.curChar.hpCur;
MoveBytes(GDVars.curChar.clericSpells,savedClericSpells,(3-1+1));
statIdx=unchecked((byte)(6));
RedrawStat(false);
statIdx=unchecked((byte)(0));
RedrawStat(true);
{
do{
MenuCore.arrowsToCaller=true;
key=unchecked((char)(Horizontal("Modify character: ","Keep Exit",unchecked((byte)(13)),unchecked((byte)(10)),unchecked((byte)(15)),true,false,ref ext)));
RedrawStat(false);
oldCon=GDVars.curChar.stats[C_Abil.CONSTITUTION];
if(ext){var __g24=key;if(__g24==C_Keys.DOWN||__g24==C_Keys.ENTER){
statIdx=unchecked((byte)((statIdx+1)));
if((statIdx>6)){statIdx=unchecked((byte)(0));}
}else if(__g24==C_Keys.UP){
statIdx=unchecked((byte)((statIdx-1)));
if((statIdx==255)){statIdx=unchecked((byte)(6));}
}else if(__g24==C_Keys.LEFT){if((statIdx<6)){
hold=unchecked((sbyte)(statIdx));
if((GDVars.curChar.stats[hold]>0)){GDVars.curChar.stats[hold]=unchecked((byte)((GDVars.curChar.stats[hold]-1)));}
{var __g25=hold;if(__g25==C_Abil.STRENGTH){if((GDVars.curChar.strExc>0)){
GDVars.curChar.strExc=unchecked((byte)((GDVars.curChar.strExc-1)));
GDVars.curChar.stats[hold]=unchecked((byte)((GDVars.curChar.stats[hold]+1)));
}else {RaiseToMinimum(hold);}}else if(__g25==C_Abil.WISDOM){
RaiseToMinimum(hold);
if((GDVars.curChar.clericSpells[1]>0)){GDVars.curChar.clericSpells[1]=unchecked((byte)(1));}
ClassCal.AwardWisdomBonus(GDVars.curChar);
}else if(__g25==C_Abil.CONSTITUTION){
RaiseToMinimum(hold);
if(levelled){ShiftHp();}else {
if((ClassCal.HpMaximum(GDVars.curChar)<GDVars.curChar.hpMax)){GDVars.curChar.hpMax=ClassCal.HpMaximum(GDVars.curChar);}
GDVars.curChar.hpCur=GDVars.curChar.hpMax;
}
statIdx=unchecked((byte)(6));
RedrawStat(false);
statIdx=unchecked((byte)(C_Abil.CONSTITUTION));
}else{RaiseToMinimum(hold);}}
}else {if(levelled){StepHp(unchecked((short)(-(1))));}else {
GDVars.curChar.hpMax=unchecked((byte)((GDVars.curChar.hpMax-1)));
if((ClassCal.HpMinimum(GDVars.curChar)>GDVars.curChar.hpMax)){GDVars.curChar.hpMax=ClassCal.HpMinimum(GDVars.curChar);}
GDVars.curChar.hpCur=GDVars.curChar.hpMax;
}}}else if(__g24==C_Keys.RIGHT){if((statIdx<6)){
hold=unchecked((sbyte)(statIdx));
GDVars.curChar.stats[hold]=unchecked((byte)((GDVars.curChar.stats[hold]+1)));
{var __g26=hold;if(__g26==C_Abil.STRENGTH){if((GDVars.curChar.stats[hold]>ClassCal.RaceAbilityMax(GDVars.curChar.race,GDVars.curChar.genderSp,hold))){
if(((((GDVars.curChar.stats[hold]>18))&&((CharHlp.WarriorLevel(GDVars.curChar)>0)))&&((GDConsts.dsMinimumMaximumStatistics[GDVars.curChar.race].excMax[GDVars.curChar.genderSp]>GDVars.curChar.strExc)))){GDVars.curChar.strExc=unchecked((byte)((GDVars.curChar.strExc+1)));}
GDVars.curChar.stats[hold]=ClassCal.RaceAbilityMax(GDVars.curChar.race,GDVars.curChar.genderSp,hold);
}}else if(__g26==C_Abil.WISDOM){
LowerToMaximum(hold);
if((GDVars.curChar.clericSpells[1]>0)){GDVars.curChar.clericSpells[1]=unchecked((byte)(1));}
ClassCal.AwardWisdomBonus(GDVars.curChar);
}else if(__g26==C_Abil.CONSTITUTION){
LowerToMaximum(hold);
if(levelled){ShiftHp();}else {
if((ClassCal.HpMinimum(GDVars.curChar)>GDVars.curChar.hpMax)){GDVars.curChar.hpMax=ClassCal.HpMinimum(GDVars.curChar);}
GDVars.curChar.hpCur=GDVars.curChar.hpMax;
}
statIdx=unchecked((byte)(6));
RedrawStat(false);
statIdx=unchecked((byte)(C_Abil.CONSTITUTION));
}else{LowerToMaximum(hold);}}
}else {if(levelled){StepHp(unchecked((short)(1)));}else {
GDVars.curChar.hpMax=unchecked((byte)((GDVars.curChar.hpMax+1)));
if((ClassCal.HpMaximum(GDVars.curChar)<GDVars.curChar.hpMax)){GDVars.curChar.hpMax=ClassCal.HpMaximum(GDVars.curChar);}
GDVars.curChar.hpCur=GDVars.curChar.hpMax;
}}}}else {var __g27=key;if(__g27=='E'||__g27==C_Keys.NONE){
for(int __g28=0;__g28<=5;__g28++){hold=unchecked((sbyte)(__g28));GDVars.curChar.stats[hold]=savedStats[hold];}
GDVars.curChar.strExc=savedExc;
GDVars.curChar.hpMax=savedHp;
GDVars.curChar.hpCur=GDVars.curChar.hpMax;
if(levelled){GDVars.curChar.hpCur=savedHpCur;}
MoveBytes(savedClericSpells,GDVars.curChar.clericSpells,(3-1+1));
RecalcStats(GDVars.curChar);
return;
}}
RecalcStats(GDVars.curChar);
CharGenView.PrintCombatStats();
RedrawStat(true);
}while(!(((!(ext))&&((key=='K')))));
if((GDVars.curChar.classLevels[C_Level.THIEF]>0)){ClassCal.RecalcThiefSkills(GDVars.curChar);}
if(!(levelled)){GDVars.curChar.npcTreasureShare=unchecked((byte)(1));}
}
}
}
public static void GainLevels(byte trainable,bool restoreDrain){
byte clsCount=default(byte);
short rolled=default(short);
short gain=default(short);
byte dmg=default(byte);
sbyte cls=default(sbyte);
short conB=default(short);
sbyte oldMage=default(sbyte);
byte sel=default(byte);
short idx=default(short);
bool notEmpty=default(bool);

{
{
clsCount=unchecked((byte)(0));
oldMage=GDVars.curChar.classLevels[C_Level.MAGE];
for(int __g29=C_Level.CLERIC;__g29<=C_Level.MONK;__g29++){cls=unchecked((sbyte)(__g29));if((GDVars.curChar.classLevels[cls]>0)){
clsCount=unchecked((byte)((clsCount+1)));
if(((GDConsts.classBit[cls]&trainable)>0)){
GDVars.curChar.classLevels[cls]=unchecked((sbyte)((GDVars.curChar.classLevels[cls]+1)));
if((restoreDrain&&((GDVars.curChar.drainedLevels>0)))){
GDVars.curChar.drainedHps=unchecked((byte)((GDVars.curChar.drainedHps-(GDVars.curChar.drainedHps/GDVars.curChar.drainedLevels))));
GDVars.curChar.drainedLevels=unchecked((byte)((GDVars.curChar.drainedLevels-1)));
}
}
}}
ClassCal.RecalcBaseSkills(GDVars.curChar);
if((GDVars.curChar.classLevels[C_Level.MAGE]>oldMage)){
idx=unchecked((short)(-(1)));
do{
sel=SpellMenu(unchecked((byte)(4)),unchecked((byte)(4)),ref idx,ref notEmpty);
}while(!((((sel>0))||(!(notEmpty)))));
if((sel>0)){GDVars.curChar.spellsKnown[sel]=unchecked((byte)(1));}
}
rolled=unchecked((short)(ClassCal.RollFirstLevelHp(GDVars.curChar,trainable)));
gain=unchecked((short)((rolled/clsCount)));
if((gain==0)){gain=unchecked((short)(1));}
GDVars.curChar.hpRolled=unchecked((byte)((GDVars.curChar.hpRolled+gain)));
conB=unchecked((short)(ClassCal.ConHpBonus(GDVars.curChar,trainable)));
gain=unchecked((short)((((rolled+conB))/clsCount)));
if((gain<1)){gain=unchecked((short)(1));}
dmg=unchecked((byte)((GDVars.curChar.hpMax-GDVars.curChar.hpCur)));
GDVars.curChar.hpMax=unchecked((byte)((GDVars.curChar.hpMax+gain)));
GDVars.curChar.hpCur=unchecked((byte)((GDVars.curChar.hpMax-dmg)));
}
}
}
public static void TrainingMenu(){
bool atCap=default(bool);
int xpClamp=default(int);
int nextXp=default(int);
byte trainMask=default(byte);
byte classMask=default(byte);
byte trainable=default(byte);
sbyte cls=default(sbyte);
sbyte row=default(sbyte);
sbyte lvl=default(sbyte);

{
if((((GDVars.curChar.status!=C_Status.OKAY))&&((stingMode==0)))){
PrintAndPause("we only train conscious people",unchecked((byte)(14)));
return;
}
if((((MoneyCal.CreatureGold(GDVars.curChar)<1000))&&((stingMode==0)))){
PrintAndPause("Training costs 1000 gp.",unchecked((byte)(14)));
return;
}
trainMask=unchecked((byte)(0));
classMask=unchecked((byte)(0));
{
xpClamp=0;
for(int __g30=C_Level.CLERIC;__g30<=C_Level.MONK;__g30++){cls=unchecked((sbyte)(__g30));if((GDVars.curChar.classLevels[cls]>0)){
classMask=unchecked((byte)((classMask+GDConsts.classBit[cls])));
atCap=false;
lvl=GDVars.curChar.classLevels[cls];
{var __g31=GDVars.curChar.race;if(__g31==C_Race.DWARF){if((cls==C_Level.FIGHTER)){if((((((lvl==8))&&((GDVars.curChar.stats[C_Abil.STRENGTH]==17))))||((((lvl==7))&&((GDVars.curChar.stats[C_Abil.STRENGTH]<17)))))){atCap=true;}}}else if(__g31==C_Race.ELF){if((cls==C_Level.FIGHTER)){if(((((lvl==7))||((((lvl==6))&&((GDVars.curChar.stats[C_Abil.STRENGTH]==17)))))||((((lvl==5))&&((GDVars.curChar.stats[C_Abil.STRENGTH]<17)))))){atCap=true;}}}else if(__g31==C_Race.GNOME){if((cls==C_Level.FIGHTER)){if((((lvl==6))||((((lvl==5))&&((GDVars.curChar.stats[C_Abil.STRENGTH]<18)))))){atCap=true;}}}else if(__g31==C_Race.HALFELF){if((((cls==C_Level.CLERIC))&&((lvl==5)))){atCap=true;}else {if((((cls==C_Level.FIGHTER))||((cls==C_Level.RANGER)))){if(((((lvl==8))||((((lvl==7))&&((GDVars.curChar.stats[C_Abil.STRENGTH]==17)))))||((((lvl==6))&&((GDVars.curChar.stats[C_Abil.STRENGTH]<17)))))){atCap=true;}}}}else if(__g31==C_Race.HALFLING){if((cls==C_Level.FIGHTER)){if(((((lvl==6))||((((lvl==5))&&((GDVars.curChar.stats[C_Abil.STRENGTH]==17)))))||((((lvl==4))&&((GDVars.curChar.stats[C_Abil.STRENGTH]<17)))))){atCap=true;}}}}
if(((!(atCap))&&((lvl<9)))){if((((GDConsts.dsExperienceSpellsClass[cls].xp[(lvl+1)]<=GDVars.curChar.experience))||((stingMode!=0)))){if((GDConsts.dsExperienceSpellsClass[cls].xp[(lvl+1)]>0)){
trainMask=unchecked((byte)((trainMask+GDConsts.classBit[cls])));
if((lvl<8)){nextXp=GDConsts.dsExperienceSpellsClass[cls].xp[((lvl+1)+1)];}else {nextXp=0;}
if((nextXp>0)){if((GDVars.curChar.experience>=nextXp)){if((nextXp>xpClamp)){xpClamp=(nextXp-1);}}}
}}}
}}
if((xpClamp>0)){GDVars.curChar.experience=xpClamp;}
if((stingMode==0)){
if((unchecked((byte)((classMask&GDVars.zone1[C_Gbvm.TRAIN_CLASS_MASK])))==0)){
PrintAndPause("We don't train that class here",unchecked((byte)(14)));
return;
}else {if((unchecked((byte)((trainMask&GDVars.zone1[C_Gbvm.TRAIN_CLASS_MASK])))==0)){
PrintAndPause("Not Enough Experience",unchecked((byte)(14)));
return;
}}
}
if((stingMode==0)){trainable=unchecked((byte)((trainMask&GDVars.zone1[C_Gbvm.TRAIN_CLASS_MASK])));}else {trainable=trainMask;}
ClearBox(unchecked((sbyte)(C_Msg.TEXT_LEFT)),unchecked((sbyte)(C_Msg.TEXT_TOP)),unchecked((sbyte)(C_Msg.TEXT_RIGHT)),unchecked((sbyte)(C_Msg.TEXT_BOTTOM)));
row=unchecked((sbyte)(4));
DisplayCreatureName(GDVars.curChar,unchecked((byte)(4)),unchecked((byte)(row)),false);
DisplayString(unchecked((byte)((GDVars.curChar.name.Length+4))),unchecked((byte)(row)),unchecked((byte)(10))," will become:");
for(int __g32=C_Level.CLERIC;__g32<=C_Level.MONK;__g32++){cls=unchecked((sbyte)(__g32));if((GDVars.curChar.classLevels[cls]>0)){if(((GDConsts.classBit[cls]&trainable)>0)){
row=unchecked((sbyte)((row+1)));
if((row==5)){DisplayString(unchecked((byte)(6)),unchecked((byte)(row)),unchecked((byte)(10)),((("    a level "+StrUtil.NumStr((GDVars.curChar.classLevels[cls]+1)))+' ')+GDConsts.classNames[cls]));}else {DisplayString(unchecked((byte)(6)),unchecked((byte)(row)),unchecked((byte)(10)),((("and a level "+StrUtil.NumStr((GDVars.curChar.classLevels[cls]+1)))+' ')+GDConsts.classNames[cls]));}
}}}
if((YesNo("Do you wish to train? ",unchecked((byte)(13)),unchecked((byte)(10)),unchecked((byte)(15)))!='Y')){return;}
PrintBottomLine("Congratulations...");
if((stingMode==0)){Money.PayFromCreature(GDVars.curChar,1000);}
CharGen.GainLevels(trainable,true);
}
}
}
public static void AddCharacterMenu(){
TItemRec list=null;
TItemRec m=null;
TCharRecord creature=null;
TCharRecord p=null;
short idx=default(short);
char key=default(char);
bool redraw=default(bool);
byte nonNpc=default(byte);
byte rangers=default(byte);
bool hasEvil=default(bool);
TCharRecord paladin=null;
bool newEvil=default(bool);
bool newPaladin=default(bool);

{
ClearBox(unchecked((sbyte)(C_Msg.TEXT_LEFT)),unchecked((sbyte)(C_Msg.TEXT_TOP)),unchecked((sbyte)(C_Msg.TEXT_RIGHT)),unchecked((sbyte)(C_Msg.TEXT_BOTTOM)));
LoadRoster(ref list);
if((list==null)){return;}
idx=unchecked((short)(0));
m=list;
redraw=true;
nonNpc=unchecked((byte)(0));
do{
key=unchecked((char)(Vertical("Add a character: ","Add Exit",unchecked((byte)(13)),unchecked((byte)(10)),unchecked((byte)(15)),unchecked((sbyte)(1)),unchecked((sbyte)(2)),unchecked((sbyte)(38)),unchecked((sbyte)(22)),list,false,ref redraw,ref idx,ref m)));
if((((key==C_Keys.ENTER))||((key=='A')))){if((m.name[1-1]!='*')){
creature=new TCharRecord();
nonNpc=unchecked((byte)(0));
if(!(LoadCharacter(m.name,creature,false,true))){
creature=null;
DisplayAndPause(unchecked((byte)(14)),(Copy(m.name,1,21)+"'s file is damaged."));
ClearBox(unchecked((sbyte)(0)),unchecked((sbyte)(24)),unchecked((sbyte)(39)),unchecked((sbyte)(24)));
}else {
m.name=("* "+m.name);
if((GDVars.creatureList==null)){
GDVars.zone1[C_Gbvm.PARTY_SIZE]=unchecked((ushort)(0));
SaveLoad.AddCharToParty(creature);
LoadCharacterIcon(true);
}else {
rangers=unchecked((byte)(0));
hasEvil=false;
paladin=null;
p=GDVars.creatureList;
while((((p!=null))&&((((p.name!=creature.name))||((p.randByte!=creature.randByte)))))){
if(!(CharHlp.IsNpc(p))){nonNpc=unchecked((byte)((nonNpc+1)));}
if((p.classLevels[C_Level.RANGER]>0)){rangers=unchecked((byte)((rangers+1)));}
if(((p.alignment==C_Align.LAWFUL_EVIL)||(p.alignment==C_Align.NEUTRAL_EVIL)||(p.alignment==C_Align.CHAOTIC_EVIL))){hasEvil=true;}
if((p.classLevels[C_Level.PALADIN]>0)){paladin=p;}
p=p.nextCharAddr;
}
newPaladin=(creature.classLevels[C_Level.PALADIN]>0);
newEvil=((creature.alignment==C_Align.LAWFUL_EVIL)||(creature.alignment==C_Align.NEUTRAL_EVIL)||(creature.alignment==C_Align.CHAOTIC_EVIL));
if((((p==null))&&(((((newPaladin&&hasEvil))||((((creature.classLevels[C_Level.RANGER]>0))&&((rangers>=3)))))||((newEvil&&((paladin!=null)))))))){
m.name=m.name.Remove(1-1,2);
if((newPaladin&&hasEvil)){DisplayAndPause(unchecked((byte)(14)),"paladins do not join with evil scum");}else {if((((creature.classLevels[C_Level.RANGER]>0))&&((rangers>=3)))){DisplayAndPause(unchecked((byte)(14)),"too many rangers in party");}else {DisplayAndPause(unchecked((byte)(14)),(paladin.name+" will tolerate no evil!"));}}
ClearBox(unchecked((sbyte)(0)),unchecked((sbyte)(24)),unchecked((sbyte)(39)),unchecked((sbyte)(24)));
FreeCreatureCall(creature);
}else {if(((((p==null))&&((GDVars.zone1[C_Gbvm.PARTY_SIZE]<Types.MAX_PARTY)))&&((CharHlp.IsNpc(creature)||((nonNpc<6)))))){
SaveLoad.AddCharToParty(creature);
LoadCharacterIcon(true);
if(!(CharHlp.IsNpc(creature))){nonNpc=unchecked((byte)((nonNpc+1)));}
}else {
if((p==null)){m.name=Copy(m.name,3,(m.name.Length-2));}
FreeCreatureCall(creature);
}}
}
}
}}
}while(!((((((key=='E'))||((key==C_Keys.NONE)))||((nonNpc>5)))||((GDVars.zone1[C_Gbvm.PARTY_SIZE]>=Types.MAX_PARTY)))));
SaveRoster(list);
MenuCore.FreeStringList(ref list);
}
}
public static void CustomizePortrait(){
byte[] PIC_COL=new byte[]{unchecked((byte)(2)),unchecked((byte)(14)),unchecked((byte)(26))};
int PIC_ROW=4;
int HINT_ROW=20;
char key=default(char);
bool ext=default(bool);
bool onBody=default(bool);
bool done=default(bool);
TImgBuf[] heads=new TImgBuf[(2)+1];
TImgBuf[] bodies=new TImgBuf[(2)+1];
string diskCh="";
byte i=default(byte);
short step=default(short);
byte nHeads=default(byte);
byte nBodies=default(byte);
byte Wrap(byte __p_cur,short __p_k,byte __p_n){
byte __result=default(byte);


{
__result=unchecked((byte)((((((((__p_cur-1)+__p_k)+__p_n))%__p_n))+1)));
return __result;
}
}
void ShowRow(bool __p_body){
short k=default(short);

{
for(int __g33=-(1);__g33<=1;__g33++){k=unchecked((short)(__g33));if(__p_body){
LoadPic(("BODY"+diskCh),GDConsts.bodyTbl[Wrap(GDVars.curChar.portraitBody,k,nBodies)],false,unchecked((byte)(0)),ref bodies[(k+1)]);
DisplayImage(unchecked((short)(PIC_COL[(k+1)])),unchecked((short)((PIC_ROW+5))),false,bodies[(k+1)]);
}else {
LoadPic(("HEAD"+diskCh),GDConsts.headTbl[Wrap(GDVars.curChar.portraitHead,k,nHeads)],false,unchecked((byte)(0)),ref heads[(k+1)]);
DisplayImage(unchecked((short)(PIC_COL[(k+1)])),unchecked((short)(PIC_ROW)),false,heads[(k+1)]);
}}
}
}
void Centered(byte __p_row,byte __p_color,string __p_s){


{
ClearBox(unchecked((sbyte)(2)),unchecked((sbyte)(__p_row)),unchecked((sbyte)(37)),unchecked((sbyte)(__p_row)));
DisplayCentered(unchecked((byte)(2)),unchecked((byte)(37)),__p_row,__p_color,__p_s);
}
}
void ShowCounts(){
byte col=default(byte);

{
{
if(onBody){col=unchecked((byte)(10));}else {col=unchecked((byte)(15));}
Centered(unchecked((byte)(17)),col,((("Head "+StrUtil.PadLeft(StrUtil.NumStr(unchecked((int)(GDVars.curChar.portraitHead))),unchecked((byte)(2))))+" of ")+StrUtil.NumStr(unchecked((int)(nHeads)))));
if(onBody){col=unchecked((byte)(15));}else {col=unchecked((byte)(10));}
Centered(unchecked((byte)(18)),col,((("Body "+StrUtil.PadLeft(StrUtil.NumStr(unchecked((int)(GDVars.curChar.portraitBody))),unchecked((byte)(2))))+" of ")+StrUtil.NumStr(unchecked((int)(nBodies)))));
}
}
}
{
diskCh=Convert.ToString(GDVars.virtualDisk).Substring(0,1);
nHeads=PortraitCount(false);
nBodies=PortraitCount(true);
for(int __g34=0;__g34<=2;__g34++){i=unchecked((byte)(__g34));{
heads[i]=null;
bodies[i]=null;
}}
{
if((((GDVars.curChar.portraitHead==0))||((GDVars.curChar.portraitHead>nHeads)))){GDVars.curChar.portraitHead=unchecked((byte)(1));}
if((((GDVars.curChar.portraitBody==0))||((GDVars.curChar.portraitBody>nBodies)))){GDVars.curChar.portraitBody=unchecked((byte)(1));}
ClearScreen();
DrawBorder(unchecked((sbyte)(C_Msg.TEXT_LEFT)),unchecked((sbyte)(C_Msg.TEXT_TOP)),unchecked((sbyte)(C_Msg.TEXT_RIGHT)),unchecked((sbyte)(C_Msg.TEXT_BOTTOM)),unchecked((byte)(0)));
Centered(unchecked((byte)(2)),unchecked((byte)(15)),GDVars.curChar.name);
DrawBorder(unchecked((sbyte)(PIC_COL[1])),unchecked((sbyte)(PIC_ROW)),unchecked((sbyte)((PIC_COL[1]+10))),unchecked((sbyte)((PIC_ROW+10))),unchecked((byte)(0)));
Centered(unchecked((byte)(HINT_ROW)),unchecked((byte)(10)),"Arrow keys for selection");
ShowRow(false);
ShowRow(true);
onBody=false;
done=false;
GDVars.horizChoice=unchecked((byte)(5));
do{
ShowCounts();
MenuCore.arrowsToCaller=true;
key=unchecked((char)(Horizontal("Portrait: ","Head Body Next Prev Keep",unchecked((byte)(13)),unchecked((byte)(10)),unchecked((byte)(15)),true,false,ref ext)));
step=unchecked((short)(0));
if(ext){var __g35=key;if(__g35==C_Keys.UP||__g35==C_Keys.DOWN){onBody=!(onBody);}else if(__g35==C_Keys.LEFT){step=unchecked((short)(-(1)));}else if(__g35==C_Keys.RIGHT){step=unchecked((short)(1));}}else {var __g36=key;if(__g36=='N'){step=unchecked((short)(1));}else if(__g36=='P'){step=unchecked((short)(-(1)));}else if(__g36=='H'){onBody=false;}else if(__g36=='B'){onBody=true;}else if(__g36=='K'||__g36==' '||__g36==C_Keys.ENTER){
ClearBox(unchecked((sbyte)(PIC_COL[0])),unchecked((sbyte)(PIC_ROW)),unchecked((sbyte)((PIC_COL[0]+10))),unchecked((sbyte)((PIC_ROW+10))));
ClearBox(unchecked((sbyte)(PIC_COL[2])),unchecked((sbyte)(PIC_ROW)),unchecked((sbyte)((PIC_COL[2]+10))),unchecked((sbyte)((PIC_ROW+10))));
ClearBox(unchecked((sbyte)(2)),unchecked((sbyte)(17)),unchecked((sbyte)(37)),unchecked((sbyte)(HINT_ROW)));
if((YesNo("Is this portrait ok? ",unchecked((byte)(13)),unchecked((byte)(10)),unchecked((byte)(15)))=='Y')){done=true;}else {
ShowRow(false);
ShowRow(true);
Centered(unchecked((byte)(HINT_ROW)),unchecked((byte)(10)),"Arrow keys for selection");
GDVars.horizChoice=unchecked((byte)(5));
}
}}
if((step!=0)){
if(onBody){GDVars.curChar.portraitBody=Wrap(GDVars.curChar.portraitBody,step,nBodies);}else {GDVars.curChar.portraitHead=Wrap(GDVars.curChar.portraitHead,step,nHeads);}
ShowRow(onBody);
}
}while(!(done));
}
for(int __g37=0;__g37<=2;__g37++){i=unchecked((byte)(__g37));{
Blit.FreePic(ref heads[i]);
Blit.FreePic(ref bodies[i]);
}}
ClearBottomLine();
}
}
public static void CustomizeIcon(){
int NUM_HEADS=14;
int NUM_WEAPONS=32;
int NUM_COLORS=16;
int ITEM_HEAD=0;
int ITEM_WEAPON=1;
int ITEM_SIZE=2;
int FIRST_PART=3;
int LAST_PART=9;
int ITEM_MENU=10;
string[] ITEM_NAME=new string[]{"Head","Weapon","Size","Weapon","Body","Hair","Face","Shield","Arm","Leg"};
byte[] ITEM_COL=new byte[]{unchecked((byte)(2)),unchecked((byte)(2)),unchecked((byte)(2)),unchecked((byte)(3)),unchecked((byte)(3)),unchecked((byte)(3)),unchecked((byte)(3)),unchecked((byte)(3)),unchecked((byte)(3)),unchecked((byte)(3))};
byte[] ITEM_ROW=new byte[]{unchecked((byte)(8)),unchecked((byte)(9)),unchecked((byte)(10)),unchecked((byte)(13)),unchecked((byte)(14)),unchecked((byte)(15)),unchecked((byte)(16)),unchecked((byte)(17)),unchecked((byte)(18)),unchecked((byte)(19))};
byte[] PART_SLOT=new byte[]{0,0,0,unchecked((byte)(6)),unchecked((byte)(1)),unchecked((byte)(4)),unchecked((byte)(4)),unchecked((byte)(5)),unchecked((byte)(2)),unchecked((byte)(3))};
byte[] PART_NIBBLE=new byte[]{0,0,0,unchecked((byte)(2)),unchecked((byte)(2)),unchecked((byte)(0)),unchecked((byte)(1)),unchecked((byte)(2)),unchecked((byte)(2)),unchecked((byte)(2))};
int COLOR_ROW=12;
int VALUE_COL=10;
int DIGIT_COL=11;
int READY_COL=3;
int ACTION_COL=10;
int PREVIEW_ROW=3;
int LABEL_ROW=6;
byte[] GRID_COL=new byte[]{unchecked((byte)(19)),unchecked((byte)(24)),unchecked((byte)(29)),unchecked((byte)(34))};
byte[] GRID_ROW=new byte[]{unchecked((byte)(2)),unchecked((byte)(6)),unchecked((byte)(10)),unchecked((byte)(14))};
int MORE_COL=25;
int MSG_TOP=18;
int MSG_BOTTOM=22;
int ICON_H=24;
int ICON_W=3;
string[] PART_FILE=new string[]{"CHEAD","CBODY"};
byte[] PART_COUNT=new byte[]{unchecked((byte)(NUM_HEADS)),unchecked((byte)(NUM_WEAPONS))};
int NO_BLOCK=255;
char key=default(char);
bool ext=default(bool);
bool done=default(bool);
byte cur=default(byte);
byte i=default(byte);
byte j=default(byte);
byte savedChoice=default(byte);
byte savedHead=default(byte);
byte savedBody=default(byte);
byte savedSize=default(byte);
TIconCols savedCols=new TIconCols();
byte tHead=default(byte);
byte tBody=default(byte);
byte tSize=default(byte);
TIconCols tCols=new TIconCols();
bool inGrid=default(bool);
byte which=default(byte);
byte pick=default(byte);
TImgBuf[,] loaded=new TImgBuf[(1)+1,(1)+1];
Byte[,] loadedBlock=new byte[(1)+1,(1)+1];
TImgBuf work=null;
TImgBuf[] cells=new TImgBuf[(15)+1];
TImgBuf readyPic=null;
TImgBuf actionPic=null;
TImgBuf shownPic=null;
TCharRecord p=null;
TImgBuf PartPic(byte __p_kind,byte __p_id,byte __p_frame){
TImgBuf __result=default(TImgBuf);
byte block=default(byte);

{
__result=null;
if((((__p_kind>1))||((__p_frame>1)))){return __result;}
if((__p_id>=PART_COUNT[__p_kind])){return __result;}
block=unchecked((byte)((__p_id+(__p_frame*128))));
if((tSize==2)){block=unchecked((byte)((block+64)));}
if((loadedBlock[__p_kind,__p_frame]!=block)){
DaxIo.PushState(unchecked((byte)(3)));
LoadPic(PART_FILE[__p_kind],block,true,unchecked((byte)(0)),ref loaded[__p_kind,__p_frame]);
DaxIo.PopState();
loadedBlock[__p_kind,__p_frame]=block;
}
__result=loaded[__p_kind,__p_frame];
return __result;
}
}
void Merge(TImgBuf __p_src){


{
if((((__p_src==null))||((work==null)))){return;}
if((((__p_src.transp==null))||((work.transp==null)))){return;}
if(((((__p_src.width!=work.width))||((__p_src.imgSize==0)))||((__p_src.imgSize>work.imgSize)))){return;}
VidOps.MergePic(__p_src,work);
CopyIcon(__p_src.map,work.map);
}
}
void BuildIcon(byte __p_frame,byte __p_h,byte __p_b,ref TIconCols __p_c){
byte k=default(byte);
TColorTab toT=new TColorTab();

{
if((((work==null))||((__p_frame>1)))){return;}
if((work.transp==null)){return;}
Blit.ClearPic(work);
Merge(PartPic(unchecked((byte)(1)),__p_b,__p_frame));
Merge(PartPic(unchecked((byte)(0)),__p_h,__p_frame));
for(int __g38=0;__g38<=15;__g38++){k=unchecked((byte)(__g38));toT[k]=k;}
for(int __g39=1;__g39<=6;__g39++){k=unchecked((byte)(__g39));{
toT[(GDConsts.defIconCol[k]&15)]=unchecked((byte)((__p_c[k]&15)));
toT[(((GDConsts.defIconCol[k]+8))&15)]=unchecked((byte)((__p_c[k]>>4)));
}}
VidOps.RecolorPic(ref toT,work);
}
}
void Place(TImgBuf __p_src,byte __p_frame,bool __p_boxed,TImgBuf __p_dst){


{
if((__p_dst==null)){return;}
if((__p_boxed&&((Blit.ColourCount()<16)))){Array.Fill(__p_dst.data,GDConsts.cgaColorFill[Blit.SrcNibble(unchecked((byte)(3)),GDConsts.dsIconColorMap)],0,__p_dst.imgSize);}else {Array.Fill(__p_dst.data,unchecked((byte)(0)),0,__p_dst.imgSize);}
if((__p_boxed&&((Blit.ColourCount()==16)))){if((__p_frame==0)){VidOps.OverlayPic(GDVars.dsIconAnimation[25,0],__p_dst);}else {VidOps.OverlayPic(GDVars.dsIconAnimation[25,1],__p_dst);}}
VidOps.OverlayPic(__p_src,__p_dst);
}
}
void Compose(byte __p_frame,byte __p_h,byte __p_b,ref TIconCols __p_c,TImgBuf __p_dst){


{
BuildIcon(__p_frame,__p_h,__p_b,ref __p_c);
Place(work,__p_frame,true,__p_dst);
}
}
byte OptionCount(){
byte __result=default(byte);


{
{var __g40=cur;if(__g40==ITEM_HEAD){__result=unchecked((byte)(NUM_HEADS));}else if(__g40==ITEM_WEAPON){__result=unchecked((byte)(NUM_WEAPONS));}else{__result=unchecked((byte)(NUM_COLORS));}}
return __result;
}
}
byte PageSize(byte __p_first){
byte __result=default(byte);
byte n=default(byte);

{
n=OptionCount();
if((n<=16)){__result=n;}else {if((__p_first==0)){__result=unchecked((byte)(12));}else {if(((n-__p_first)<=12)){__result=unchecked((byte)((n-__p_first)));}else {__result=unchecked((byte)(8));}}}
return __result;
}
}
byte PageFirst(byte __p_v){
byte __result=default(byte);
byte first=default(byte);

{
first=unchecked((byte)(0));
while((((first<OptionCount()))&&((__p_v>=(first+PageSize(first))))))first=unchecked((byte)((first+PageSize(first))));
__result=first;
return __result;
}
}
byte CellOf(byte __p_v){
byte __result=default(byte);
byte first=default(byte);

{
first=PageFirst(__p_v);
if((first>0)){__result=unchecked((byte)(((__p_v-first)+4)));}else {__result=unchecked((byte)((__p_v-first)));}
return __result;
}
}
byte PartNibble(){
byte __result=default(byte);


{
__result=unchecked((byte)(0));
if((((cur>=FIRST_PART))&&((cur<=LAST_PART)))){if((PART_NIBBLE[cur]==2)){__result=which;}else {__result=PART_NIBBLE[cur];}}
return __result;
}
}
void OptionState(byte __p_opt,ref byte __p_h,ref byte __p_b,ref TIconCols __p_c){
byte slot=default(byte);

{
{var __g41=cur;if(__g41==ITEM_HEAD){__p_h=__p_opt;}else if(__g41==ITEM_WEAPON){__p_b=__p_opt;}else if((__g41>=FIRST_PART&&__g41<=LAST_PART)){
slot=PART_SLOT[cur];
if((PartNibble()==0)){__p_c[slot]=unchecked((byte)(((__p_c[slot]&240)|((__p_opt&15)))));}else {__p_c[slot]=unchecked((byte)(((__p_c[slot]&15)|((((__p_opt&15))<<4)))));}
}}
}
}
byte CurrentOption(){
byte __result=default(byte);
byte v=default(byte);

{
{var __g42=cur;if(__g42==ITEM_HEAD){v=tHead;}else if(__g42==ITEM_WEAPON){v=tBody;}else{if((PartNibble()==0)){v=unchecked((byte)((tCols[PART_SLOT[cur]]&15)));}else {v=unchecked((byte)((tCols[PART_SLOT[cur]]>>4)));}}}
if((v>=OptionCount())){v=unchecked((byte)(0));}
__result=v;
return __result;
}
}
string Counter(byte __p_v,byte __p_count){
string __result=default(string);
string s="";

{
if((__p_v<__p_count)){s=StrUtil.NumStr((__p_v+1));}else {s=Convert.ToString('?');}
s=((s+'/')+StrUtil.NumStr(unchecked((int)(__p_count))));
while((s.Length<5))s=(s+' ');
__result=s;
return __result;
}
}
void DrawItem(byte __p_k){
byte col=default(byte);
byte d1=default(byte);
byte d2=default(byte);

{
if((__p_k>LAST_PART)){return;}
if((__p_k==cur)){col=unchecked((byte)(15));}else {col=unchecked((byte)(10));}
DisplayString(ITEM_COL[__p_k],ITEM_ROW[__p_k],col,ITEM_NAME[__p_k]);
{var __g43=__p_k;if(__g43==ITEM_HEAD){DisplayString(unchecked((byte)(VALUE_COL)),ITEM_ROW[__p_k],col,Counter(tHead,unchecked((byte)(NUM_HEADS))));}else if(__g43==ITEM_WEAPON){DisplayString(unchecked((byte)(VALUE_COL)),ITEM_ROW[__p_k],col,Counter(tBody,unchecked((byte)(NUM_WEAPONS))));}else if(__g43==ITEM_SIZE){if((tSize==1)){DisplayString(unchecked((byte)(VALUE_COL)),ITEM_ROW[__p_k],col,"Small");}else {DisplayString(unchecked((byte)(VALUE_COL)),ITEM_ROW[__p_k],col,"Large");}}else if((__g43>=FIRST_PART&&__g43<=LAST_PART)){if((PART_NIBBLE[__p_k]==2)){
d1=col;
d2=col;
if((inGrid&&((__p_k==cur)))){if((which==0)){d2=unchecked((byte)(10));}else {d1=unchecked((byte)(10));}}
DisplayString(unchecked((byte)(DIGIT_COL)),ITEM_ROW[__p_k],d1,Convert.ToString('1'));
DisplayString(unchecked((byte)((DIGIT_COL+2))),ITEM_ROW[__p_k],d2,Convert.ToString('2'));
}}}
}
}
void ShowCell(byte __p_k,bool __p_boxed){


{
if((((__p_k>15))||((cells[__p_k]==null)))){return;}
Place(cells[__p_k],unchecked((byte)(0)),__p_boxed,shownPic);
ShowPic(unchecked((short)(GRID_COL[(__p_k%4)])),unchecked((short)(GRID_ROW[(__p_k/4)])),shownPic);
}
}
void DrawGrid(){
byte k=default(byte);
short opt=default(short);
byte h=default(byte);
byte b=default(byte);
TIconCols c=new TIconCols();
byte first=default(byte);
byte top=default(byte);
bool moreBelow=default(bool);

{
first=PageFirst(pick);
if((first>0)){top=unchecked((byte)(4));}else {top=unchecked((byte)(0));}
moreBelow=((first+PageSize(first))<OptionCount());
if((OptionCount()>16)){ClearBox(unchecked((sbyte)(18)),unchecked((sbyte)(GRID_ROW[0])),unchecked((sbyte)(38)),unchecked((sbyte)((GRID_ROW[3]+2))));}
for(int __g44=0;__g44<=15;__g44++){k=unchecked((byte)(__g44));{
opt=unchecked((short)(((first+k)-top)));
if((((((k<4))&&((top>0))))||((((k>=12))&&moreBelow)))){
if(((k%4)==0)){DisplayString(unchecked((byte)(MORE_COL)),unchecked((byte)((GRID_ROW[(k/4)]+1))),unchecked((byte)(10)),"(more)");}
}else {if((opt<(first+PageSize(first)))){
h=tHead;
b=tBody;
CopyIcon(tCols,c);
OptionState(unchecked((byte)(opt)),ref h,ref b,ref c);
BuildIcon(unchecked((byte)(0)),h,b,ref c);
if(((((((cells[k]!=null))&&((work!=null)))&&((cells[k].imgSize==work.imgSize)))&&((cells[k].transp!=null)))&&((work.transp!=null)))){
MoveBytes(work.data,cells[k].data,work.imgSize);
MoveBytes(work.transp,cells[k].transp,Blit.MaskBytes(work.imgSize));
}
ShowCell(k,(opt==pick));
}else {ClearBox(unchecked((sbyte)(GRID_COL[(k%4)])),unchecked((sbyte)(GRID_ROW[(k/4)])),unchecked((sbyte)((GRID_COL[(k%4)]+2))),unchecked((sbyte)((GRID_ROW[(k/4)]+2))));}}
}}
}
}
void ShowPreviews(){


{
if(inGrid){Place(cells[CellOf(pick)],unchecked((byte)(0)),true,readyPic);}else {Compose(unchecked((byte)(0)),tHead,tBody,ref tCols,readyPic);}
ShowPic(unchecked((short)(READY_COL)),unchecked((short)(PREVIEW_ROW)),readyPic);
Compose(unchecked((byte)(1)),tHead,tBody,ref tCols,actionPic);
ShowPic(unchecked((short)(ACTION_COL)),unchecked((short)(PREVIEW_ROW)),actionPic);
}
}
void MsgLine(byte __p_row,string __p_s){


{
DisplayCentered(unchecked((byte)(18)),unchecked((byte)(38)),__p_row,unchecked((byte)(10)),__p_s);
}
}
void ShowMessage(){
byte top=default(byte);

{
ClearBox(unchecked((sbyte)(18)),unchecked((sbyte)(MSG_TOP)),unchecked((sbyte)(38)),unchecked((sbyte)(MSG_BOTTOM)));
if(!(inGrid)){return;}
if(((((cur>=FIRST_PART))&&((cur<=LAST_PART)))&&((PART_NIBBLE[cur]==2)))){top=unchecked((byte)(MSG_TOP));}else {top=unchecked((byte)((MSG_TOP+1)));}
if((cur>=FIRST_PART)){MsgLine(top,(("Choosing "+ITEM_NAME[cur])+" color"));}else {MsgLine(top,("Choosing "+ITEM_NAME[cur]));}
MsgLine(unchecked((byte)((top+1))),"Arrows to select,");
MsgLine(unchecked((byte)((top+2))),"Enter to choose");
if((top==MSG_TOP)){
MsgLine(unchecked((byte)((top+3))),"Space toggles");
MsgLine(unchecked((byte)((top+4))),"color 1/2");
}
}
}
void Revert(){


{
{
tHead=GDVars.curChar.iconHead;
tBody=GDVars.curChar.iconBody;
if((GDVars.curChar.iconSize==1)){tSize=unchecked((byte)(1));}else {tSize=unchecked((byte)(2));}
CopyIcon(GDVars.curChar.iconColors1,tCols);
}
}
}
void MovePick(char __p_arrow){
short n=default(short);
short v=default(short);

{
n=unchecked((short)(OptionCount()));
v=unchecked((short)(pick));
{var __g45=__p_arrow;if(__g45==C_Keys.LEFT){if((v>0)){v=unchecked((short)((v-1)));}else {v=unchecked((short)((n-1)));}}else if(__g45==C_Keys.RIGHT){if((v<(n-1))){v=unchecked((short)((v+1)));}else {v=unchecked((short)(0));}}else if(__g45==C_Keys.DOWN){if(((v+4)<n)){v=unchecked((short)((v+4)));}else {if(((v/4)<(((n-1))/4))){v=unchecked((short)((n-1)));}else {v=unchecked((short)((v%4)));}}}else if(__g45==C_Keys.UP){if((v>=4)){v=unchecked((short)((v-4)));}else {
v=unchecked((short)(((((((n-1))/4))*4)+v)));
if((v>(n-1))){v=unchecked((short)((v-4)));}
}}}
if((((v>=0))&&((v<n)))){pick=unchecked((byte)(v));}
}
}
void ChooseInGrid(){
byte old=default(byte);
bool finished=default(bool);

{
inGrid=true;
which=unchecked((byte)(0));
pick=CurrentOption();
ClearBottomLine();
DrawItem(cur);
ShowMessage();
DrawGrid();
ShowPreviews();
finished=false;
do{
MenuCore.arrowsToCaller=true;
key=unchecked((char)(Horizontal("","",unchecked((byte)(13)),unchecked((byte)(10)),unchecked((byte)(15)),true,false,ref ext)));
if(ext){
if(((key==C_Keys.UP)||(key==C_Keys.DOWN)||(key==C_Keys.LEFT)||(key==C_Keys.RIGHT))){
old=pick;
MovePick(key);
if((pick!=old)){
OptionState(pick,ref tHead,ref tBody,ref tCols);
if((PageFirst(pick)!=PageFirst(old))){DrawGrid();}else {
ShowCell(CellOf(old),false);
ShowCell(CellOf(pick),true);
}
DrawItem(cur);
ShowPreviews();
}
}
}else {var __g46=key;if(__g46==C_Keys.ENTER){
{var __g47=cur;if(__g47==ITEM_HEAD){GDVars.curChar.iconHead=tHead;}else if(__g47==ITEM_WEAPON){GDVars.curChar.iconBody=tBody;}else{CopyIcon(tCols,GDVars.curChar.iconColors1);}}
finished=true;
}else if(__g46==C_Keys.NONE){
Revert();
finished=true;
}else if(__g46==' '){if(((((cur>=FIRST_PART))&&((cur<=LAST_PART)))&&((PART_NIBBLE[cur]==2)))){
which=unchecked((byte)((1-which)));
pick=CurrentOption();
DrawItem(cur);
DrawGrid();
}}}
}while(!(finished));
inGrid=false;
ClearBox(unchecked((sbyte)(18)),unchecked((sbyte)(1)),unchecked((sbyte)(38)),unchecked((sbyte)(16)));
ShowMessage();
DrawItem(cur);
ShowPreviews();
}
}
bool Confirmed(){
bool __result=default(bool);


{
if((YesNo("Is this icon ok? ",unchecked((byte)(13)),unchecked((byte)(10)),unchecked((byte)(15)))=='Y')){__result=true;}else {
GDVars.horizChoice=unchecked((byte)(1));
__result=false;
}
return __result;
}
}
{
savedChoice=GDVars.horizChoice;
{
savedHead=GDVars.curChar.iconHead;
savedBody=GDVars.curChar.iconBody;
savedSize=GDVars.curChar.iconSize;
CopyIcon(GDVars.curChar.iconColors1,savedCols);
}
for(int __g48=0;__g48<=1;__g48++){i=unchecked((byte)(__g48));for(int __g49=0;__g49<=1;__g49++){j=unchecked((byte)(__g49));{
loaded[i,j]=null;
loadedBlock[i,j]=unchecked((byte)(NO_BLOCK));
}}}
Blit.AllocPic(unchecked((ushort)(ICON_H)),unchecked((ushort)(ICON_W)),unchecked((byte)(1)),unchecked((byte)(1)),ref work);
for(int __g50=0;__g50<=15;__g50++){i=unchecked((byte)(__g50));Blit.AllocPic(unchecked((ushort)(ICON_H)),unchecked((ushort)(ICON_W)),unchecked((byte)(1)),unchecked((byte)(1)),ref cells[i]);}
Blit.AllocPic(unchecked((ushort)(ICON_H)),unchecked((ushort)(ICON_W)),unchecked((byte)(1)),unchecked((byte)(0)),ref readyPic);
Blit.AllocPic(unchecked((ushort)(ICON_H)),unchecked((ushort)(ICON_W)),unchecked((byte)(1)),unchecked((byte)(0)),ref actionPic);
Blit.AllocPic(unchecked((ushort)(ICON_H)),unchecked((ushort)(ICON_W)),unchecked((byte)(1)),unchecked((byte)(0)),ref shownPic);
inGrid=false;
which=unchecked((byte)(0));
pick=unchecked((byte)(0));
cur=unchecked((byte)(ITEM_HEAD));
Revert();
ClearScreen();
CbtMap.SetCombatPalette();
DrawBorder(unchecked((sbyte)(1)),unchecked((sbyte)(1)),unchecked((sbyte)(16)),unchecked((sbyte)(22)),unchecked((byte)(0)));
DrawBorder(unchecked((sbyte)(18)),unchecked((sbyte)(1)),unchecked((sbyte)(38)),unchecked((sbyte)(22)),unchecked((byte)(0)));
DrawBorder(unchecked((sbyte)(18)),unchecked((sbyte)(MSG_TOP)),unchecked((sbyte)(38)),unchecked((sbyte)(MSG_BOTTOM)),unchecked((byte)(0)));
DisplayCentered(unchecked((byte)(1)),unchecked((byte)(16)),unchecked((byte)(1)),unchecked((byte)(3)),GDVars.curChar.name);
DisplayString(unchecked((byte)((READY_COL-1))),unchecked((byte)(LABEL_ROW)),unchecked((byte)(10)),"Ready");
DisplayString(unchecked((byte)((ACTION_COL-1))),unchecked((byte)(LABEL_ROW)),unchecked((byte)(10)),"Action");
DisplayString(unchecked((byte)(2)),unchecked((byte)(COLOR_ROW)),unchecked((byte)(10)),"Color");
for(int __g51=ITEM_HEAD;__g51<=LAST_PART;__g51++){i=unchecked((byte)(__g51));DrawItem(i);}
ShowPreviews();
done=false;
GDVars.horizChoice=unchecked((byte)(1));
do{
MenuCore.arrowsToCaller=(cur!=ITEM_MENU);
MenuCore.enterToCaller=(cur!=ITEM_MENU);
key=unchecked((char)(Horizontal("","Keep Exit",unchecked((byte)(13)),unchecked((byte)(10)),unchecked((byte)(15)),true,false,ref ext)));
if(ext){
if(((key==C_Keys.UP)||(key==C_Keys.DOWN))){
i=cur;
if((key==C_Keys.UP)){
if((cur==ITEM_HEAD)){cur=unchecked((byte)(ITEM_MENU));}else {cur=unchecked((byte)((cur-1)));}
}else {if((cur==ITEM_MENU)){cur=unchecked((byte)(ITEM_HEAD));}else {cur=unchecked((byte)((cur+1)));}}
DrawItem(i);
DrawItem(cur);
}
}else {var __g52=key;if(__g52==C_Keys.ENTER){var __g53=cur;if(__g53==ITEM_SIZE){
tSize=unchecked((byte)((3-tSize)));
GDVars.curChar.iconSize=tSize;
DrawItem(cur);
ShowPreviews();
}else{ChooseInGrid();}}else if(__g52=='K'){done=Confirmed();}else if(__g52=='E'||__g52==C_Keys.NONE){
{
GDVars.curChar.iconHead=savedHead;
GDVars.curChar.iconBody=savedBody;
GDVars.curChar.iconSize=savedSize;
CopyIcon(savedCols,GDVars.curChar.iconColors1);
}
done=true;
}}
}while(!(done));
for(int __g54=0;__g54<=1;__g54++){i=unchecked((byte)(__g54));for(int __g55=0;__g55<=1;__g55++){j=unchecked((byte)(__g55));Blit.FreePic(ref loaded[i,j]);}}
Blit.FreePic(ref work);
for(int __g56=0;__g56<=15;__g56++){i=unchecked((byte)(__g56));Blit.FreePic(ref cells[i]);}
Blit.FreePic(ref readyPic);
Blit.FreePic(ref actionPic);
Blit.FreePic(ref shownPic);
p=GDVars.creatureList;
while((((p!=null))&&((p!=GDVars.curChar))))p=p.nextCharAddr;
if((((p!=null))&&((GDVars.curChar.orderNumber<Types.MAX_PARTY)))){
LoadCharacterIcon(true);
if((GDVars.gLocType>C_Area.LOC_3D_MAP)){MapTiles.LoadIconAnim("ICON",unchecked((byte)(0)),unchecked((byte)(11)));}
}
ClearBottomLine();
CbtMap.SetNoncombatPalette();
GDVars.horizChoice=savedChoice;
}
}
}}
