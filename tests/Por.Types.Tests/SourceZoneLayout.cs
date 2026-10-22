using System; using System.Reflection; using Por.Core;
public static class SourceZoneLayout {
 public static int Run() {int n=0; var z=new Types.TGbvmZone0State();
 for(int i=0;i<195;i++){z.scriptWords4900[0x4900+i]=(ushort)(i+1);Check(z.words[0x4900+i]==i+1,"scriptWords4900 read/write");n++;}
 z.partyX.value=0xa581;Check(z.words[0x49c3]==0xa581,"partyX word");n++;
 z.partyY.value=0xa581;Check(z.words[0x49c4]==0xa581,"partyY word");n++;
 z.mapId.value=0xa581;Check(z.words[0x49c5]==0xa581,"mapId word");n++;
 for(int i=0;i<7;i++){z.clock.values[i]=(ushort)(i+97);Check(z.words[0x49c6+i]==i+97,"clock values");n++;}
 for(int i=0;i<24;i++){z.scriptWords49CD[0x49cd+i]=(ushort)(i+1);Check(z.words[0x49cd+i]==i+1,"scriptWords49CD read/write");n++;}
 z.antimagic=0xa581;Check(z.words[0x49e5]==0xa581,"antimagic scalar");n++;
 z.view3dFlag=0xa581;Check(z.words[0x49e6]==0xa581,"view3dFlag scalar");n++;
 z.mapGraphics1=0xa581;Check(z.words[0x49e7]==0xa581,"mapGraphics1 scalar");n++;
 z.mapGraphics2=0xa581;Check(z.words[0x49e8]==0xa581,"mapGraphics2 scalar");n++;
 for(int i=0;i<7;i++){z.scriptWords49E9[0x49e9+i]=(ushort)(i+1);Check(z.words[0x49e9+i]==i+1,"scriptWords49E9 read/write");n++;}
 z.previousX=0xa581;Check(z.words[0x49f0]==0xa581,"previousX scalar");n++;
 z.previousY=0xa581;Check(z.words[0x49f1]==0xa581,"previousY scalar");n++;
 z.previousScriptId=0xa581;Check(z.words[0x49f2]==0xa581,"previousScriptId scalar");n++;
 for(int i=0;i<8;i++){z.scriptWords49F3[0x49f3+i]=(ushort)(i+1);Check(z.words[0x49f3+i]==i+1,"scriptWords49F3 read/write");n++;}
 z.noAutomap=0xa581;Check(z.words[0x49fb]==0xa581,"noAutomap scalar");n++;
 z.gameSpeed.value=0xa581;Check(z.words[0x49fc]==0xa581,"gameSpeed word");n++;
 z.skyCeilingColor=0xa581;Check(z.words[0x49fd]==0xa581,"skyCeilingColor scalar");n++;
 z.alternateSkyColor=0xa581;Check(z.words[0x49fe]==0xa581,"alternateSkyColor scalar");n++;
 z.settings=0xa581;Check(z.words[0x49ff]==0xa581,"settings scalar");n++;
 for(int i=0;i<32;i++){z.entryWords[0x4a00+i]=(ushort)(i+1);Check(z.words[0x4a00+i]==i+1,"entryWords read/write");n++;}
 for(int i=0;i<92;i++){z.scriptWords4A20[0x4a20+i]=(ushort)(i+1);Check(z.words[0x4a20+i]==i+1,"scriptWords4A20 read/write");n++;}
 z.nomadCampFlags=0xa581;Check(z.words[0x4a7c]==0xa581,"nomadCampFlags scalar");n++;
 for(int i=0;i<15;i++){z.scriptWords4A7D[0x4a7d+i]=(ushort)(i+1);Check(z.words[0x4a7d+i]==i+1,"scriptWords4A7D read/write");n++;}
 z.buccaneersBaseGate=0xa581;Check(z.words[0x4a8c]==0xa581,"buccaneersBaseGate scalar");n++;
 for(int i=0;i<18;i++){z.scriptWords4A8D[0x4a8d+i]=(ushort)(i+1);Check(z.words[0x4a8d+i]==i+1,"scriptWords4A8D read/write");n++;}
 z.nomadCampGate=0xa581;Check(z.words[0x4a9f]==0xa581,"nomadCampGate scalar");n++;
 z.easternSitesGate=0xa581;Check(z.words[0x4aa0]==0xa581,"easternSitesGate scalar");n++;
 z.scriptWord4AA1=0xa581;Check(z.words[0x4aa1]==0xa581,"scriptWord4AA1 scalar");n++;
 z.yarashPyramidLeft=0xa581;Check(z.words[0x4aa2]==0xa581,"yarashPyramidLeft scalar");n++;
 for(int i=0;i<16;i++){z.scriptWords4AA3[0x4aa3+i]=(ushort)(i+1);Check(z.words[0x4aa3+i]==i+1,"scriptWords4AA3 read/write");n++;}
 z.yarashMissionDone=0xa581;Check(z.words[0x4ab3]==0xa581,"yarashMissionDone scalar");n++;
 for(int i=0;i<30;i++){z.scriptWords4AB4[0x4ab4+i]=(ushort)(i+1);Check(z.words[0x4ab4+i]==i+1,"scriptWords4AB4 read/write");n++;}
 z.nomadCampPresent=0xa581;Check(z.words[0x4ad2]==0xa581,"nomadCampPresent scalar");n++;
 for(int i=0;i<39;i++){z.scriptWords4AD3[0x4ad3+i]=(ushort)(i+1);Check(z.words[0x4ad3+i]==i+1,"scriptWords4AD3 read/write");n++;}
 for(int i=1;i<=3;i++){z.savedWalls.wallSetIds[i]=(ushort)(i+27);z.savedWalls.symbolSlots[i]=(ushort)(i+57);Check(z.words[0x4afa+i-1]==i+27 && z.words[0x4afd+i-1]==i+57,"savedWalls partitions");n++;}
 for(int i=0;i<512;i++){z.scriptWords4B00[0x4b00+i]=(ushort)(i+1);Check(z.words[0x4b00+i]==i+1,"scriptWords4B00 read/write");n++;}
return n; }static void Check(bool b,string l){if(!b)throw new Exception(l);}}
