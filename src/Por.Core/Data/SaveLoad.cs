// Direct conversion of pinned src/saveload.pas; explicit packed source windows, never managed-array padding.
using System;
using System.IO;
using System.Text;
using static Por.Core.Types;
namespace Por.Core
{
    public static class SaveLoad
    {
        public const char QUICK_SLOT='Q';
        // A restored party needs a final panel paint after the area's entry script has finished drawing.
        // Keep identity, not a global bool: stale requests cannot redraw a different/new party.
        private static TCharRecord pendingPartyRedraw;
        internal static void MarkPartyForRedraw(){pendingPartyRedraw=GDVars.creatureList;}
        internal static bool TakePartyRedraw(){var party=pendingPartyRedraw;pendingPartyRedraw=null;return party!=null&&ReferenceEquals(party,GDVars.creatureList);}

        internal static Func<string,string,bool,Stream> OpenRead=(name,msg,noPrompt)=>{FileStream f;return DaxIo.OpenWithRetry(name,msg,noPrompt,out f)?f:null;};
        internal static Action<bool> LoadIcons=LoadCharIconAnim;
        internal static Action<byte> LoadMap=View3D.Load3DMap;
        internal static Action<byte,byte> LoadWalls=View3D.LoadWallSet;
        internal static Action<string,byte,byte> LoadIcon=MapTiles.LoadIconAnim;
        internal static Action<bool> CompleteYarash=Ending.YarashComplete;
        private static bool Io(Exception e){return e is IOException||e is UnauthorizedAccessException||e is System.Security.SecurityException;}
        private static Encoding Cp437{get{Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);return Encoding.GetEncoding(437);}}
        private static Stream RewriteSaveFile(string name){while(true){try{return new FileStream(name,FileMode.Create,FileAccess.Write,FileShare.None);}catch(Exception e)when(Io(e)){TextRend.DisplayAndPause(14,SaveMenu.SaveDiskMsg());}}}
        internal static string CharFileBase(string name){return GDVars.dsSaveDirectory+DaxIo.SanitizeFileName(DaxIo.SanitizeString(name,"."));}
        internal static void EraseCharFiles(string name){foreach(string ext in new[]{".cha",".itm",".spc",".mem",".sts"})FileUtil.DeleteFile(name+ext);}
        public static void LoadCharlist(ref TItemRec list)
        {
            list=null;SaveMenu.CheckSaveDir();string fn=GDVars.dsSaveDirectory+"charlist.txt";if(!DaxIo.FileExists(fn))return;TItemRec tail=null;
            try{using(var f=new StreamReader(fn,Cp437,false)){string line;while((line=f.ReadLine())!=null){if(line.Length>40)line=line.Substring(0,40);for(var p=GDVars.creatureList;p!=null;p=p.nextCharAddr)if(p.name==line){line="* "+line;break;}MenuCore.AppendMenuLine(ref list,ref tail,line,false);}}}catch(Exception e)when(Io(e)){}
        }
        public static void SaveCharlist(TItemRec list){SaveMenu.CheckSaveDir();using(var f=new StreamWriter(RewriteSaveFile(GDVars.dsSaveDirectory+"charlist.txt"),Cp437)){f.NewLine="\r\n";for(var p=list;p!=null;p=p.next)if(!p.name.StartsWith("*",StringComparison.Ordinal))f.WriteLine(p.name);}}
        public static void LoadCharIconAnim(bool loading)
        {
            DaxIo.PushState(3);var c=GDVars.curChar;int height=c.iconSize==2?2:1;MapTiles.LoadIconAnim("CHEAD"+GDConsts.iconHeightSuffix[height],c.iconHead,11);MapTiles.LoadIconAnim("CBODY"+GDConsts.iconHeightSuffix[height],c.iconBody,c.orderNumber);
            VidOps.MergePic(GDVars.dsIconAnimation[11,0],GDVars.dsIconAnimation[c.orderNumber,0]);VidOps.MergePic(GDVars.dsIconAnimation[11,1],GDVars.dsIconAnimation[c.orderNumber,1]);
            if(loading){var from=new TColorTab();var to=new TColorTab();for(int i=0;i<16;i++)from[i]=to[i]=(byte)i;for(int i=1;i<=6;i++){to[GDConsts.defIconCol[i]]=(byte)(c.iconColors1[i]&15);to[GDConsts.defIconCol[i]+8]=(byte)((c.iconColors1[i]&240)>>4);}Blit.RemapPic(from,to,0,false,GDVars.dsIconAnimation[c.orderNumber,0]);Blit.RemapPic(from,to,0,false,GDVars.dsIconAnimation[c.orderNumber,1]);}
            MapTiles.FreeIconAnim(11);DaxIo.PopState();
        }
        private static bool WriteFile(string name,Action<Stream> write,bool retry)
        {
            try{using(var f=retry?RewriteSaveFile(name):new FileStream(name,FileMode.Create,FileAccess.Write,FileShare.None))write(f);return true;}catch(Exception e)when(Io(e)){return false;}
        }
        internal static byte[] EffectBytes(TEffectRec e){byte[] b=new byte[9];b[0]=e.id;b[1]=(byte)(e.duration&255);b[2]=(byte)(e.duration>>8);b[3]=e.strength;b[4]=(byte)(e.onRemove?1:0);return b;}
        internal static TEffectRec EffectFrom(byte[] b){return new TEffectRec{id=b[0],duration=(ushort)(b[1]|b[2]<<8),strength=b[3],onRemove=b[4]!=0};}
        public static void SaveChar(TCharRecord c,string fname)
        {
            if(fname.Length>8)fname=fname.Substring(0,8);string fn,ext;
            if(fname.Length==0){TItemRec list=null;LoadCharlist(ref list);var p=list;if(p!=null)while(p.next!=null)p=p.next;MenuCore.AppendMenuLine(ref list,ref p,c.name,false);SaveCharlist(list);MenuCore.FreeStringList(ref list);fn=CharFileBase(c.name);ext=".cha";}
            else{SaveMenu.CheckSaveDir();fn=GDVars.dsSaveDirectory+fname;ext=".sav";}
            bool ok=WriteFile(fn+ext,f=>{byte[] b=SaveRecordCodec.Encode(c);f.Write(b,0,b.Length);},true);
            FileUtil.DeleteFile(fn+".itm");if(c.itemsAddr!=null)ok=WriteFile(fn+".itm",f=>{for(var it=c.itemsAddr;it!=null;it=it.next){byte[] b=Vault.ItemToBytes(it);f.Write(b,0,b.Length);}},false)&&ok;
            FileUtil.DeleteFile(fn+".spc");if(c.effectsAddr!=null)ok=WriteFile(fn+".spc",f=>{for(var e=c.effectsAddr;e!=null;e=e.next){byte[] b=EffectBytes(e);f.Write(b,0,b.Length);}},false)&&ok;
            if(!ok)TextRend.DisplayAndPause(14,"Could not save "+c.name.Substring(0,Math.Min(NAME_LEN,c.name.Length))+" in full.");UsualSpl.WriteUsualSpells(c,fn);Stats.WriteChar(c,fn);
        }
        private static byte[] Whole(Stream f,int size)
        {
            byte[] b=new byte[size];int n=0;while(n<size){int got=f.Read(b,n,size-n);if(got==0)return null;n+=got;}return b;
        }
        public static bool LoadChar(string name,TCharRecord c,bool savedGame,bool askDisk)
        {
            string msg;if(askDisk){name=GDVars.dsSaveDirectory+DaxIo.SanitizeFileName(name);msg=SaveMenu.SaveDiskMsg();}else msg=GDConsts.insertDiskMsg[GDVars.virtualDisk]+DaxIo.DataDrive()+":";
            byte[] bytes=null;try{using(var f=OpenRead(name+(savedGame?".sav":".cha"),msg,false))if(f!=null)bytes=Whole(f,285);}catch(Exception e)when(Io(e)){}
            c.nextCharAddr=null;c.itemsAddr=null;c.effectsAddr=null;c.combatAddr=null;if(bytes==null)return false;
            SaveRecordCodec.Decode(bytes,c);if(GDVars.quickEnds!=0&&CharHlp.ComesUnderPlayer(c))c.quickfight=0;
            if(DaxIo.FileExists(name+".itm"))try{using(var f=OpenRead(name+".itm",msg,false)){TItemRec tail=null;byte[] b;while(f!=null&&(b=Whole(f,63))!=null){var it=EclVm.ItemFromBytes(b,0);it.next=null;if(tail==null)c.itemsAddr=it;else tail.next=it;tail=it;}}}catch(Exception e)when(Io(e)){}
            if(DaxIo.FileExists(name+".spc"))try{using(var f=OpenRead(name+".spc",msg,false)){TEffectRec tail=null;byte[] b;while(f!=null&&(b=Whole(f,9))!=null){var effect=EffectFrom(b);if(tail==null)c.effectsAddr=effect;else tail.next=effect;tail=effect;}}}catch(Exception e)when(Io(e)){}
            CharHlp.LinkEquipped(c);UsualSpl.ReadUsualSpells(c,name);Stats.ReadChar(c,name);return true;
        }
        public static void AddCharToParty(TCharRecord c)
        {
            c.orderNumber=255;if(GDVars.creatureList==null){GDVars.creatureList=c;GDVars.curChar=c;}else Party.LastCreature().nextCharAddr=c;
            bool[] used=new bool[MAX_PARTY];for(var p=GDVars.creatureList;p!=null;p=p.nextCharAddr)if(p.orderNumber<MAX_PARTY)used[p.orderNumber]=true;c.orderNumber=0;while(c.orderNumber<MAX_PARTY&&used[c.orderNumber])c.orderNumber++;
            GDVars.curChar=c;GDVars.zone1[C_Gbvm.PARTY_SIZE]=unchecked((ushort)(GDVars.zone1[C_Gbvm.PARTY_SIZE]+1));ClassCal.RecalcBaseSkills(c);
        }
        public static int SaveMinSize(){return 1+2048+2048+1024+C_Gbvm.SAVED_SCRIPT_BYTES+5+3+MAX_PARTY*41;}
        private static void WriteWords(BinaryWriter w,ushort[] data,int low,int high){for(int i=low;i<=high;i++)w.Write(data[i]);}
        private static void ReadWords(BinaryReader r,ushort[] data,int low,int high){for(int i=low;i<=high;i++)data[i]=r.ReadUInt16();}
        private static void ShortString(BinaryWriter w,string s,int capacity){int n=Math.Min(capacity,s.Length);w.Write((byte)n);for(int i=0;i<capacity;i++)w.Write(i<n?unchecked((byte)s[i]):(byte)0);}
        private static string ReadString(BinaryReader r,int capacity){int n=r.ReadByte();byte[] b=r.ReadBytes(capacity);if(n>capacity||b.Length!=capacity)throw new InvalidDataException("Invalid saved short string.");char[] a=new char[n];for(int i=0;i<n;i++)a[i]=(char)b[i];return new string(a);}
        internal static void LoadSlot(char letter)
        {
            string file=GDVars.dsSaveDirectory+"savgam"+letter;Stream f=OpenRead(file+".dat",SaveMenu.SaveDiskMsg(),true);TextRend.ClearBottomLine();
            if(f==null||f.Length<SaveMinSize()){if(f!=null)f.Dispose();TextRend.DisplayAndPause(14,"Save game "+letter+" is damaged.");TextRend.ClearBottomLine();return;}
            TextRend.DisplayString(0,24,10,"Loading...Please Wait");string[] names=new string[MAX_PARTY];byte count;
            using(var r=new BinaryReader(f))
            {
                GDVars.virtualDisk=r.ReadByte();if(GDVars.virtualDisk<1||GDVars.virtualDisk>LAST_DISK)GDVars.virtualDisk=1;
                byte[] z=r.ReadBytes(2048);Array.Copy(z,GDVars.zone0.bytes,2048);ReadWords(r,GDVars.zone1.Data,0x6B00,0x6EFF);ReadWords(r,GDVars.zone2.Data,0x9700,0x98FF);r.BaseStream.Seek(C_Gbvm.SAVED_SCRIPT_BYTES,SeekOrigin.Current);
                GDVars.gMapX=r.ReadSByte();GDVars.gMapY=r.ReadSByte();GDVars.gFacing=(byte)(r.ReadByte()&6);GDVars.wallAhead=r.ReadByte();GDVars.eventAtCell=r.ReadByte();GDVars.gLocType=r.ReadByte();GDVars.ifcState=r.ReadByte();count=r.ReadByte();for(int i=0;i<MAX_PARTY;i++)names[i]=ReadString(r,40);
            }
            Vault.RestoreFromSlot((byte)letter);Stats.ReadGame(file+".sts");Explored.ReadSlot(file+".map");MapNotes.ReadSlot(file+".nts");GDVars.zone1[C_Gbvm.PARTY_SIZE]=0;
            for(int i=0;i<Math.Min(8,(int)count);i++){string fn=DaxIo.SanitizeFileName(DaxIo.SanitizeString(names[i],"."));if(DaxIo.FileExists(GDVars.dsSaveDirectory+fn+".sav")){var c=new TCharRecord();if(LoadChar(fn,c,true,true))AddCharToParty(c);SaveMenu.CheckSaveDir();}}
            for(var c=GDVars.creatureList;c!=null;c=c.nextCharAddr){string fn=CharFileBase(c.name);if(DaxIo.FileExists(fn+".cha"))EraseCharFiles(fn);}
            for(GDVars.curChar=GDVars.creatureList;GDVars.curChar!=null;GDVars.curChar=GDVars.curChar.nextCharAddr)LoadIcons(true);GDVars.curChar=GDVars.creatureList;
            GDVars.virtualDisk=unchecked((byte)GDVars.zone1[C_Gbvm.DISK_NUM]);if(GDVars.virtualDisk<1||GDVars.virtualDisk>LAST_DISK)GDVars.virtualDisk=1;
            if(GDVars.gLocType<C_Area.LOC_WILD_WEST)LoadMap(GDVars.zone0.mapId.lo);
            if(GDVars.gLocType>C_Area.LOC_3D_MAP)LoadIcon("ICON",0,11);else for(byte i=1;i<=3;i++){GDVars.dsWallSetCache[i].wallDefId=unchecked((short)GDVars.zone0.words[C_Gbvm.WALLSLOT_ID+i]);GDVars.dsWallSetCache[i].symbolSlot=unchecked((short)GDVars.zone0.words[C_Gbvm.WALLSLOT_SLOT+i]);if(GDVars.dsWallSetCache[i].wallDefId>=0)LoadWalls(unchecked((byte)GDVars.dsWallSetCache[i].wallDefId),unchecked((byte)GDVars.dsWallSetCache[i].symbolSlot));}
            GDVars.zone0.savedWalls.menuAnimation=0;if(GDVars.zone0.yarashPyramidLeft!=0)CompleteYarash(false);TextRend.ClearBottomLine();MarkPartyForRedraw();
        }
        public static void LoadGame(){SaveMenu.CheckSaveDir();byte letter=SaveMenu.ChooseSlot(false,0);if(letter!=0)LoadSlot((char)letter);}
        public static bool QuickLoadable(){FileStream f;if(!FileUtil.OpenReadOnly(out f,GDVars.dsSaveDirectory+"savgamQ.dat"))return false;try{using(f)return f.Length>=SaveMinSize();}catch(IOException){return false;}}
        public static void QuickLoad(){if(!QuickLoadable()){TextRend.DisplayAndPause(14,"There is no quick save.");TextRend.ClearBottomLine();GDVars.loadedSave=1;return;}FreeParty();LoadSlot(QUICK_SLOT);GDVars.loadedSave=1;GDVars.locTypePrev=C_Area.LOC_3D_MAP;}
        internal static void SaveToSlot(char letter,string desc,string area)
        {
            GDVars.zone0.previousScriptId=GDVars.eclScriptId;SaveMenu.CheckSaveDir();string file=GDVars.dsSaveDirectory+"savgam"+letter;string[] names=new string[MAX_PARTY];int count=0;
            for(var p=GDVars.creatureList;p!=null&&count<MAX_PARTY;p=p.nextCharAddr){count++;names[count-1]="CHRDAT"+letter+count;}
            TextRend.ClearBottomLine();TextRend.DisplayString(0,24,10,"Saving...Please Wait");ushort[] ids=new ushort[3],slots=new ushort[3];for(int i=1;i<=3;i++){ids[i-1]=GDVars.zone0.savedWalls.wallSetIds[i];slots[i-1]=GDVars.zone0.savedWalls.symbolSlots[i];GDVars.zone0.words[C_Gbvm.WALLSLOT_ID+i]=unchecked((ushort)GDVars.dsWallSetCache[i].wallDefId);GDVars.zone0.words[C_Gbvm.WALLSLOT_SLOT+i]=unchecked((ushort)GDVars.dsWallSetCache[i].symbolSlot);}
            GDVars.zone0.gameSpeed.value=GDVars.gameDelay;GDVars.zone0.settings=unchecked((ushort)((1-GDVars.musicOn)*4+GDVars.monsterPics*2+GDVars.portraitsOn));GDVars.zone1[C_Gbvm.DISK_NUM]=GDVars.virtualDisk;
            try{using(var w=new BinaryWriter(RewriteSaveFile(file+".dat"))){w.Write(GDVars.virtualDisk);w.Write(GDVars.zone0.bytes);for(int i=1;i<=3;i++){GDVars.zone0.savedWalls.wallSetIds[i]=ids[i-1];GDVars.zone0.savedWalls.symbolSlots[i]=slots[i-1];}WriteWords(w,GDVars.zone1.Data,0x6B00,0x6EFF);WriteWords(w,GDVars.zone2.Data,0x9700,0x98FF);w.Write(GDVars.zone3.Data,C_Gbvm.SCRIPT_FIRST,C_Gbvm.SAVED_SCRIPT_BYTES);w.Write(GDVars.gMapX);w.Write(GDVars.gMapY);w.Write(GDVars.gFacing);w.Write(GDVars.wallAhead);w.Write(GDVars.eventAtCell);w.Write(GDVars.gLocType);w.Write(GDVars.ifcState);w.Write((byte)count);for(int i=0;i<MAX_PARTY;i++)ShortString(w,names[i]??"",40);}}
            finally{for(int i=1;i<=3;i++){GDVars.zone0.savedWalls.wallSetIds[i]=ids[i-1];GDVars.zone0.savedWalls.symbolSlots[i]=slots[i-1];}}
            SaveMenu.WriteSlotInfo((byte)letter,desc,area);Vault.CommitToSlot((byte)letter);Stats.WriteGame(file+".sts");Explored.WriteSlot(file+".map");MapNotes.WriteSlot(file+".nts");
            int n=0;for(var p=GDVars.creatureList;p!=null&&n<MAX_PARTY;p=p.nextCharAddr){n++;SaveChar(p,"CHRDAT"+letter+n);EraseCharFiles(CharFileBase(p.name));}GDVars.recentSaved=1;TextRend.ClearBottomLine();
        }
        public static void SaveGame(){SaveMenu.CheckSaveDir();string area=AreaName.CurrentArea(),desc,oldArea;byte letter=0;bool ok;do{letter=SaveMenu.ChooseSlot(true,letter);if(letter==0)return;SaveMenu.ReadSlotInfo(letter,out desc,out oldArea);ok=SaveMenu.AskDescription(letter,area,ref desc);}while(!ok);SaveToSlot((char)letter,desc,area);}
        public static void QuickSave(){SaveToSlot(QUICK_SLOT,"QUICK SAVE",AreaName.CurrentArea());TextRend.DisplayAndPause(10,"Quick save done.");TextRend.ClearBottomLine();}
        public static void CheckFileElseQuit(string name){if(DaxIo.FileExists(name))return;bool ext=false;byte key=MenuCore.MenuHorizontal("","Save and exit  Insert disk #"+StrUtil.NumStr(GDVars.virtualDisk),13,10,15,false,false,ref ext);if(!ext&&key=='S'){GDVars.gMapX=unchecked((sbyte)(GDVars.gMapX-GDConsts.dirDX[GDVars.gFacing]));GDVars.gMapY=unchecked((sbyte)(GDVars.gMapY-GDConsts.dirDY[GDVars.gFacing]));View3D.WrapCoords(ref GDVars.gMapX,ref GDVars.gMapY);DaxIo.PopState();SaveGame();GameIo.ExitGame();}}
        public static void FreeCreature(TCharRecord c){c.combatAddr=null;CharHlp.FreeItemList(ref c.itemsAddr);c.effectsAddr=null;UsualSpl.ForgetUsualSpells(c);Stats.Forget(c);}
        public static void RemoveCreature(bool suppress,bool freeAnim)
        {
            if(GDVars.creatureList==null)return;var cur=GDVars.curChar;var prev=GDVars.creatureList;
            if(prev!=cur){while(prev.nextCharAddr!=cur&&prev.nextCharAddr!=null)prev=prev.nextCharAddr;if(prev.nextCharAddr==null)return;if(freeAnim)MapTiles.FreeIconAnim(cur.orderNumber);prev.nextCharAddr=cur.nextCharAddr;FreeCreature(cur);GDVars.curChar=prev;}
            else{if(freeAnim)MapTiles.FreeIconAnim(cur.orderNumber);GDVars.creatureList=cur.nextCharAddr;var next=cur.nextCharAddr;FreeCreature(cur);GDVars.curChar=next;}
            if(!suppress)GDVars.zone1[C_Gbvm.PARTY_SIZE]=unchecked((ushort)(GDVars.zone1[C_Gbvm.PARTY_SIZE]-1));
        }
        public static void FreeParty(){while(GDVars.creatureList!=null){GDVars.curChar=GDVars.creatureList;RemoveCreature(false,true);}GDVars.curChar=null;}
    }
}
