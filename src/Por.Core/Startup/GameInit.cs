// Direct conversion of pinned src/gameinit.pas; handler assignments extracted reproducibly.
using System;
using System.IO;
using static Por.Core.Types;
namespace Por.Core
{
    public interface IGameInitServices
    {
        TSpellProc SpellHandler(string name);TEffectHandler EffectHandler(string qualifiedName);
        TCastUI CastTargetMenu {get;}
        void InstallLightningTracer();void SeedRandom(bool deterministic);
    }
    public static partial class GameInit
    {
        public static IGameInitServices Services;
        internal static IGameInitServices Required{get{if(Services==null)throw new NotSupportedException("GameInit: Magic/EffHndlr/CbtActs services missing.");return Services;}}
        private static TSpellProc RequireSpell(string name){return Required.SpellHandler(name)??throw new NotSupportedException("Missing Magic."+name);}
        private static TEffectHandler RequireEffect(string name){return Required.EffectHandler(name)??throw new NotSupportedException("Missing "+name);}
        internal static Action LoadOptionsCall=Options.LoadOptions;
        internal static Action LoadFont=TextRend.LoadGameFont;
        internal static Action<byte,byte> LoadSymbols=Sym8x8.Load8x8Set;
        internal delegate void LoadPicProc(string n,byte id,bool mask,byte trans,ref TImgBuf pic);
        internal static LoadPicProc LoadPic=Blit.LoadPic;
        internal static Action<string,byte,byte> LoadIcon=MapTiles.LoadIconAnim;
        internal static Func<Stream> OpenItems=()=>{FileStream f;DaxIo.OpenWithRetry("ITEMS",GDConsts.insertDiskMsg[GDVars.virtualDisk]+DaxIo.DataDrive()+":",false,out f);return f;};
        public static void ResetGameState()
        {
            Explored.Clear();MapNotes.Clear();Array.Clear(GDVars.zone0.bytes,0,GDVars.zone0.bytes.Length);
            Array.Clear(GDVars.zone1.Data,0x6B00,0x400);Array.Clear(GDVars.zone2.Data,0x9700,0x200);Array.Clear(GDVars.zone3.Data,C_Gbvm.SCRIPT_FIRST,C_Gbvm.SCRIPT_LAST-C_Gbvm.SCRIPT_FIRST+1);
            GDVars.wallAhead=0;GDVars.eventAtCell=0;GDVars.gMapX=15;GDVars.gMapY=1;GDVars.gFacing=C_Dir.WEST;
            for(int i=0;i<GDVars.doorOpen.Length;i++)GDVars.doorOpen[i]=1;
            GDVars.debugTraceEnabled=false;GDVars.bottomLineTaken=false;GDVars.loadedSave=0;GDVars.restTicks=0;GDVars.creatureList=null;GDVars.curChar=null;GDVars.eclIp=0x9900;
            GDVars.suppressContinuePrompt=0;GDVars.virtualDisk=1;GDVars.prevVirtualDisk=1;GDVars.automapOn=false;GDVars.locTypePrev=C_Area.LOC_3D_MAP;GDVars.gLocType=C_Area.LOC_3D_MAP;
            GDVars.zone1[C_Gbvm.PARTY_SIZE]=0;GDVars.dsWildAnimFrame=0;GDVars.vertTop=1;GDVars.duelFlag=false;GDVars.dsSuppressDrawingBorderFlag=0;GDVars.dsSearchModeFlag=0;GDVars.dsEcl3dClearFlag=0;
            GDVars.protNewLine=0;GDVars.eclGraphics=false;GDVars.partyDefeated=false;GDVars.clipFlag=1;GDVars.sprShown=false;GDVars.dsSuppressingInsertDiskFlag=false;GDVars.picArcName="";GDVars.picArcId=255;
            GDVars.recentSaved=0;GDVars.ceilFloorColorSel=232;GDVars.wallColorSel=219;GDVars.inEncMenu=0;Stats.ResetGame();
        }
        internal static void ReadItems(Stream f)
        {
            byte[] data=new byte[129*16];int got=0;try{using(f){f.Seek(2,SeekOrigin.Begin);int n;while(got<data.Length&&(n=f.Read(data,got,data.Length-got))>0)got+=n;}}catch(IOException){GameIo.FatalError("Unable to read ITEMS.");return;}
            // The source requires the regular 128 records; a missing overflow record remains zero-initialized.
            if(got<(BASE_ITEM_LAST+1)*16){GameIo.FatalError("Unable to read ITEMS.");return;}
            for(int i=0;i<129;i++){int p=i*16;GDVars.baseItems[i]=new TBaseItemRec{slot=data[p],hands=data[p+1],dmgLgRolls=data[p+2],dmgLgDice=data[p+3],dmgLgMod=unchecked((sbyte)data[p+4]),rateOfFire=data[p+5],acDescr=data[p+6],wpnClass=data[p+7],unk8=data[p+8],dmgSmRolls=data[p+9],dmgSmDice=data[p+10],dmgSmMod=unchecked((sbyte)data[p+11]),range=data[p+12],classMask=data[p+13],props=data[p+14],unkF=data[p+15]};}
        }
        public static void GameInit2(){GameInit2("");}
        public static void GameInit2(string secondArgument)
        {
            GDVars.zone0=new TGbvmZone0State();GDVars.zone1=new TGbvmZone1();GDVars.zone2=new TZone2();GDVars.zone3=new TZone3();GDVars.gbStrings=new TGbStrings();GDVars.baseItems=new TBaseItems();HeapErr.KeepReserve();
            GDVars.gameFont=new TFontArr();GDVars.wallGfx=new TWallSlices();GDVars.map3d=new TGeoMap();
            Blit.AllocPic(8,1,1,0,ref GDVars.dsCurrent8x8SymbolPtr2);Blit.AllocPic(168,21,1,0,ref GDVars.dsViewImageBufferPtr);Blit.AllocPic(24,3,1,0,ref GDVars.dsIconImageBufferPtr);
            Array.Clear(GDVars.spanActive,0,GDVars.spanActive.Length);Array.Clear(GDVars.spanScreenOfs,0,GDVars.spanScreenOfs.Length);for(int i=0;i<GDVars.spanFirstOfs.Length;i++)GDVars.spanFirstOfs[i]=65535;Array.Clear(GDVars.spanLastOfs,0,GDVars.spanLastOfs.Length);VidOps.InitRowTables();
            Array.Clear(GDVars.dsSymbolSetPtrs,0,GDVars.dsSymbolSetPtrs.Length);GDVars.ds2dMapTilePtr=null;GDVars.ds2dMapTilePtr2=null;Array.Clear(GDVars.dsIconAnimation,0,GDVars.dsIconAnimation.Length);
            for(int i=1;i<=2;i++){GDVars.dsPortraitCache[i].pictureId=255;GDVars.dsPortraitCache[i].pic=null;}GDVars.animData=new TAnimRec();GDVars.treasureItems=null;
            GDVars.dsWallSetCache[1].wallDefId=0;GDVars.dsWallSetCache[1].symbolSlot=1;for(int i=2;i<=3;i++){GDVars.dsWallSetCache[i].wallDefId=-1;GDVars.dsWallSetCache[i].symbolSlot=-1;}
            GDVars.portraitsOn=1;GDVars.monsterPics=1;GDVars.musicOn=1;GDVars.soundOn=1;GDVars.quickEnds=1;GDVars.quickMagicOn=0;GDVars.companionOn=0;GDVars.stingHpFlag=0;GDVars.msgDelayFlag=1;GDVars.gameDelay=1;
            ResetGameState();GDVars.soundAutoStop=0;SoundDrv.SoundSetAutoStop(100);
            string savedDir=GDVars.gameDir;GDVars.gameDir="";LoadFont();TextRend.ClearBottomLine();TextRend.DisplayString(0,24,10,"Loading...Please Wait");LoadSymbols(202,4);LoadSymbols(203,0);LoadPic("sqrpaci",1,false,0,ref GDVars.ds2dMapTilePtr);
            for(byte i=0;i<=11;i++)LoadIcon("COMSPR",i,(byte)(i+13));LoadIcon("COMSPR",25,25);ReadItems(OpenItems());GDVars.gameDir=savedDir;TextRend.ClearBottomLine();LoadOptionsCall();Required.SeedRandom(secondArgument=="NORAND");
            InitSpellFns();InitEffectHandlers();Required.InstallLightningTracer();GameIo.showMemory=MemShow.ShowMemory;CharHlp.faultHook=CharDisp.ShowFault;EclVars.diskHook=DaxIo.PushState;EclVars.wallSetHook=View3D.LoadWallSet;
        }
    }
}
