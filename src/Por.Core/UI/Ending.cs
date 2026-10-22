// Direct conversion of pinned src/ending.pas (7616920).
using System;
using static Por.Core.Types;
namespace Por.Core
{
    public static class Ending
    {
        private static TImgBuf finalPic;
        private static byte savedIfc;
        private static string finalSfx;
        internal delegate void LoadPicProc(string n,byte id,bool mask,byte key,ref TImgBuf pic);
        internal delegate void LoadAnimProc(string n,byte id,byte key,ref TAnimRec anim);
        internal delegate void FreePicProc(ref TImgBuf pic);
        internal delegate void FreeAnimProc(ref TAnimRec anim);
        // Real defaults; recording adapters can check ordering without game media or a desktop.
        internal static LoadPicProc LoadPic = Blit.LoadPic;
        internal static LoadAnimProc LoadAnim = Portrait.LoadAnimation;
        internal static FreePicProc FreePic = Blit.FreePic;
        internal static FreeAnimProc FreeAnim = Portrait.FreeAnimation;
        internal static Action<short,short,byte,byte,TImgBuf,TImgBuf> BlitClipped = Blit.BlitTileClipped;
        internal static Action<short,short,bool,TImgBuf> DisplayImage = Portrait.DisplayImageBuf;
        internal static Action Flush = Blit.FlushView;
        internal static Action DisplayInterface = CharDisp.DisplayNoncombatInterface;
        internal static Action<short,short> BuildWild = Wilds.BuildWildernessMap;
        internal static Action<TColorTab,TColorTab,short,bool,TImgBuf> Remap = Blit.RemapPic;
        internal static Func<TImgBuf> SecondSheet = MapTiles.SecondSheet;
        internal static void PlayFinalAnim(short col,short row,byte picId,byte nCycles)
        {
            unchecked
            {
                byte cycles = 0; int last = TextRend.GetTicks(); var a = new TAnimRec();
                LoadAnim("FINAL",picId,2,ref a);
                if (a.count == 0) { FreeAnim(ref a); return; }
                BlitClipped((short)(col-1),(short)(row-1),0,0,a.frames[1].pic,GDVars.dsViewImageBufferPtr); Flush();
                do
                {
                    DisplayImage(col,row,true,a.frames[a.current].pic); int now = TextRend.GetTicks();
                    if (now-last > a.frames[a.current].time*(GDVars.gameDelay+3) || now < last)
                    {
                        a.current++; if (a.current > a.count) { a.current = 1; cycles++; } last = now;
                    }
                } while (cycles != nCycles);
                FreeAnim(ref a);
            }
        }
        private static byte Cycles() { return unchecked((byte)((ushort)((ushort)(10-GDVars.gameDelay)*5)/2)); }
        public static void WinGame()
        {
            finalSfx = GDVars.virtualDisk.ToString(System.Globalization.CultureInfo.InvariantCulture).Substring(0,1);
            savedIfc = GDVars.ifcState; GDVars.ifcState = C_Ifc.WON_GAME; finalPic = null;
            TextRend.ClearRect(1,17,38,22);
            TextRend.DisplayString(1,17,10,"Mortally wounded, the dragon roars!");
            TextRend.DisplayString(1,18,10,"The spirit of Tyranthraxus flares up");
            TextRend.DisplayString(1,19,10,"from the dragon's body.");
            DisplayInterface();
            LoadPic("FINAL"+finalSfx,1,false,0,ref finalPic); BlitClipped(0,0,0,0,finalPic,GDVars.dsViewImageBufferPtr);
            PlayFinalAnim(3,5,2,Cycles());
            LoadPic("FINAL"+finalSfx,3,false,0,ref finalPic); BlitClipped(0,0,0,0,finalPic,GDVars.dsViewImageBufferPtr);
            if (GDVars.zone1[C_Gbvm.PARTY_SIZE] > 1) { LoadPic("FINAL"+finalSfx,4,false,0,ref finalPic); BlitClipped(0,11,0,0,finalPic,GDVars.dsViewImageBufferPtr); }
            if (GDVars.zone1[C_Gbvm.PARTY_SIZE] > 2) { LoadPic("FINAL"+finalSfx,5,false,0,ref finalPic); BlitClipped(13,9,0,0,finalPic,GDVars.dsViewImageBufferPtr); }
            if (GDVars.zone1[C_Gbvm.PARTY_SIZE] > 3) { LoadPic("FINAL"+finalSfx,6,false,0,ref finalPic); BlitClipped(0,9,0,0,finalPic,GDVars.dsViewImageBufferPtr); }
            TextRend.ClearRect(1,17,38,22);
            TextRend.DisplayString(1,17,10,"\"Fools, you have but slain the body");
            TextRend.DisplayString(1,18,10,"I possessed.  I cannot be defeated!\"");
            TextRend.DisplayString(1,19,10,"With the power of the pool of radiance");
            TextRend.DisplayString(1,20,10,"which I moved here, to my lair,");
            TextRend.DisplayString(1,21,10,"I will still rule, by possessing one");
            TextRend.DisplayString(1,22,10,"of you!\"");
            PlayFinalAnim(3,1,7,Cycles());
            TextRend.ClearRect(1,17,38,22);
            TextRend.DisplayString(1,17,10,"\"No Lord Bane!  I can still rule here!");
            TextRend.DisplayString(1,18,10,"I have not failed.  Do not call me back");
            TextRend.DisplayString(1,19,10,"through the pool!\"");
            TextRend.DisplayString(1,20,10,"Noooo..."); PlayFinalAnim(3,1,8,1);
            TextRend.PauseOnly(); TextRend.PauseOnly(); TextRend.PauseOnly();
            FreePic(ref finalPic); GDVars.ifcState = savedIfc; DisplayInterface();
        }
        private static void RemapTiles(bool dither)
        {
            for (int t=0;t<11;t++) RemapTile(GDConsts.yarBlueGreenTiles[t],GDConsts.yarBlueGreenTo,dither);
            for (int t=0;t<18;t++) RemapTile(GDConsts.yarGreenTiles[t],GDConsts.yarGreenTo,dither);
        }
        private static void RemapTile(byte tile,TColorTab to,bool dither)
        {
            Remap(GDConsts.yarRemapFrom,to,(short)(tile>C_Tile.ID_MASK?tile&C_Tile.ID_MASK:tile),dither,
                tile>C_Tile.ID_MASK?SecondSheet():GDVars.ds2dMapTilePtr);
        }
        public static void YarashComplete(bool animate)
        {
            if (animate)
            {
                BuildWild(17,15); Flush();
                for (int step=0;step<9;step++) { RemapTiles(true); BuildWild((short)GDConsts.yarViewX[step],(short)GDConsts.yarViewY[step]); Flush(); }
            }
            RemapTiles(false);
            if (animate) { BuildWild(23,25); Flush(); TextRend.PauseOnly(); TextRend.PauseOnly(); }
        }
    }
}
