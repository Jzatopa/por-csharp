// Direct conversion of pinned src/explore.pas.
using System;
using static Por.Core.Types;
namespace Por.Core
{
    public interface IExploreServices{void AdvanceTime(byte amount,sbyte unit);void NoncombatCastMenu();void ViewCharacter(ref bool done);}
    public static class Explore
    {
        public static IExploreServices Services;public static bool quickLoadAsked;
        private static IExploreServices S{get{if(Services==null)throw new NotSupportedException("Explore: FxEngine/Camp/ViewChar services missing.");return Services;}}
        internal static Action<ushort> Sound=SoundDrv.SoundCommand;
        internal static Action PrintInfo=CharDisp.PrintMiscInfo;
        internal static Action<sbyte,sbyte,byte> Draw3D=View3D.Display3DMapView;
        internal static Action DisplayInterface=CharDisp.DisplayNoncombatInterface;
        internal static Action DisplayWilderness=CharDisp.DisplayWildernessInterface;
        internal static void ChangeBarrier(sbyte x,sbyte y,byte facing,bool passable)
        {
            if(x<0||x>MAP_LAST||y<0||y>MAP_LAST)return;
            int shift=facing==C_Dir.WEST?6:facing==C_Dir.SOUTH?4:facing==C_Dir.EAST?2:facing==C_Dir.NORTH?0:-1;if(shift<0)return;
            GDVars.map3d.barriers[y,x]=(byte)((GDVars.map3d.barriers[y,x]&~(3<<shift))|(passable?1<<shift:0));
        }
        internal static void ChangeBarrierImpassable(sbyte x,sbyte y,byte f){ChangeBarrier(x,y,f,false);}internal static void ChangeBarrierPassable(sbyte x,sbyte y,byte f){ChangeBarrier(x,y,f,true);}
        internal static void OpenBarrierAhead(){ChangeBarrierPassable(GDVars.gMapX,GDVars.gMapY,GDVars.gFacing);ChangeBarrierPassable(unchecked((sbyte)(GDVars.gMapX+GDConsts.dirDX[GDVars.gFacing])),unchecked((sbyte)(GDVars.gMapY+GDConsts.dirDY[GDVars.gFacing])),(byte)((GDVars.gFacing+4)%8));}
        internal static bool PartyHasClass(sbyte cls){for(var p=GDVars.creatureList;p!=null;p=p.nextCharAddr)if(p.classLevels[cls]>0)return true;return false;}
        internal static bool BashDoor()
        {
            bool success=false;
            for(var c=GDVars.creatureList;c!=null&&!success;c=c.nextCharAddr)
            {
                if(c.status!=C_Status.OKAY)continue;int strength=c.strCur,exc=c.strExc,sides=0,limit=0;
                if(View3D.GetBarrierType(GDVars.gMapX,GDVars.gMapY,GDVars.gFacing)==3)
                {
                    if(strength==18&&exc>=91){sides=6;limit=exc<100?1:2;}
                    else if(strength>=19){sides=strength<=23?6:strength==24?8:10;limit=strength<=20?3:strength<=22?4:strength==23?5:strength==24?7:9;}
                    else GDVars.doorOpen[0]=0;
                    if(strength==18&&exc<91)GDVars.doorOpen[0]=0;
                }
                else if(strength>=3)
                {
                    sides=strength<=18?6:strength<=20?8:strength==21?10:strength<=23?12:strength==24?20:24;
                    limit=strength<=7?1:strength<=15?2:strength<=17?3:strength==18?(exc<=50?3:exc<100?4:5):sides-1;
                }
                if(sides>0)success=Dice.RollDice(1,(byte)sides)<=limit;
            }if(success)OpenBarrierAhead();return success;
        }
        internal static bool PickLock(){bool success=false;for(var c=GDVars.creatureList;c!=null&&!success;c=c.nextCharAddr)success=Dice.RollDice(1,100)<=c.thiefSkills[2]&&c.status==C_Status.OKAY;GDVars.doorOpen[1]=0;if(success)OpenBarrierAhead();return success;}
        internal static byte FindMemorizedSpell(byte spell,TCharRecord c){for(byte i=0;i<=SPELL_SLOT_LAST;i++)if(c.memorizedSpells[i]==spell)return i;return 255;}
        internal static TCharRecord FindCharWithSpell(byte spell){for(var c=GDVars.creatureList;c!=null;c=c.nextCharAddr)if(c.status==C_Status.OKAY&&FindMemorizedSpell(spell,c)!=255)return c;return null;}
        internal static bool PartyCastsKnock(){var p=FindCharWithSpell(C_Spell.KNOCK);if(p==null)return false;CharHlp.RemoveSpell(p,C_Spell.KNOCK);OpenBarrierAhead();return true;}
        internal static void MoveForward()
        {
            int x=GDVars.gMapX,y=GDVars.gMapY,f=GDVars.gFacing;GDVars.zone1[C_Gbvm.LEAVE_MAP]=0;
            if(View3D.GetBarrierType((sbyte)x,(sbyte)y,(byte)f)==0)return;x+=GDConsts.dirDX[f];y+=GDConsts.dirDY[f];
            if(x>15){GDVars.gMapX=15;GDVars.zone1[C_Gbvm.LEAVE_MAP]=1;}if(x<0){GDVars.gMapX=0;GDVars.zone1[C_Gbvm.LEAVE_MAP]=1;}if(y>15){GDVars.gMapY=15;GDVars.zone1[C_Gbvm.LEAVE_MAP]=1;}if(y<0){GDVars.gMapY=0;GDVars.zone1[C_Gbvm.LEAVE_MAP]=1;}
        }
        internal static void MovePartyForward3D()
        {
            if(!GDVars.sound.singleStep)Sound(GDConsts.soundIds.combat[C_Sound.STEP]);GDVars.gMapX=unchecked((sbyte)(GDVars.gMapX+GDConsts.dirDX[GDVars.gFacing]));GDVars.gMapY=unchecked((sbyte)(GDVars.gMapY+GDConsts.dirDY[GDVars.gFacing]));View3D.WrapCoords(ref GDVars.gMapX,ref GDVars.gMapY);
            GDVars.wallAhead=View3D.GetWallType(GDVars.gMapX,GDVars.gMapY,GDVars.gFacing);for(int i=0;i<GDVars.doorOpen.Length;i++)GDVars.doorOpen[i]=1;GDVars.eventAtCell=View3D.GetMapEvent(GDVars.gMapX,GDVars.gMapY);S.AdvanceTime(1,(sbyte)((GDVars.zone1[C_Gbvm.SEARCH_FLAGS]&1)!=0?C_Time.TURNS:C_Time.MINUTES));
        }
        internal static bool Impassable(byte glyph){return glyph==78||(glyph>=126&&glyph<=128)||(glyph>=130&&glyph<=132)||(glyph>=168&&glyph<=172)||(glyph>=174&&glyph<=179)||glyph==188||(glyph>=221&&glyph<=222)||(glyph>=225&&glyph<=233)||glyph==245;}
        internal static void MovePartyWilderness()
        {
            sbyte x=unchecked((sbyte)(GDVars.zone0.partyX.value+GDConsts.dirDX[GDVars.gFacing])),y=unchecked((sbyte)(GDVars.zone0.partyY.value+GDConsts.dirDY[GDVars.gFacing]));x=(sbyte)Math.Max(0,Math.Min(15,(int)x));y=(sbyte)Math.Max(0,Math.Min(35,(int)y));sbyte col=unchecked((sbyte)(x+GDConsts.wildColOfs[GDVars.gLocType]));byte tile=GDConsts.wildMap[y,col];
            if(Impassable(tile))return;if((tile>=102&&tile<=108||tile==114)&&GDVars.zone0.yarashPyramidLeft==0){TextRend.PrintAndPause("The river's stench drives you back.",14);return;}
            GDVars.zone0.partyX.value=unchecked((ushort)x);GDVars.zone0.partyY.value=unchecked((ushort)y);S.AdvanceTime(12,C_Time.HOURS);
        }
        internal static bool AskQuickLoad(){bool result=SaveLoad.QuickLoadable();if(SaveLoad.QuickLoadable())quickLoadAsked=true;else{TextRend.DisplayAndPause(14,"There is no quick save.");TextRend.ClearBottomLine();}return result;}
        public static char MapMainMenu()
        {
            if(SaveLoad.TakePartyRedraw())CharDisp.DisplayParty(GDVars.curChar);
            FixMain.RuntimeStage?.Invoke("explore");
            GDVars.zone1[C_Gbvm.MOVE_LOCKED]=0;bool wild=GDVars.gLocType>=C_Area.LOC_WILD_WEST&&GDVars.gLocType<=C_Area.LOC_WILD_EAST;byte key=0;bool ext=false,done=false,viewDone=false;
            if(wild&&GDVars.locTypePrev<C_Area.LOC_WILD_WEST){MapTiles.LoadIconAnim("ICON",0,9);MapTiles.LoadIconAnim("ICON",1,10);MapTiles.LoadIconAnim("ICON",2,11);DisplayWilderness();}
            if(wild||GDVars.gLocType==C_Area.LOC_3D_MAP)do
            {
                MenuCore.arrowsToCaller=true;MenuCore.quickKeysToCaller=true;key=MenuCore.MenuHorizontal("",wild?GDConsts.menuWild:GDConsts.menu3D,13,10,15,true,false,ref ext);
                if(!ext)switch(key)
                {
                    case (byte)'A':if(!wild){if(GDVars.zone0.noAutomap==0||GDVars.stingArg){GDVars.automapOn=!GDVars.automapOn;Draw3D(GDVars.gMapX,GDVars.gMapY,GDVars.gFacing);}else TextRend.PrintAndPause("Not Here",14);}break;
                    case (byte)'C':if(wild||GDVars.curChar.status==C_Status.OKAY)S.NoncombatCastMenu();break;case (byte)'V':S.ViewCharacter(ref viewDone);break;case (byte)'J':JrnlView.JournalMenu();break;case (byte)'E':done=true;break;
                    case (byte)'S':GDVars.zone1[C_Gbvm.SEARCH_FLAGS]^=1;break;
                    case (byte)'L':GDVars.zone1[C_Gbvm.SEARCH_FLAGS]|=2;S.AdvanceTime(1,C_Time.TURNS);GDVars.eclIp=GDVars.scriptEntryAddrs[C_Ecl.ENTRY_SEARCH];done=true;break;
                }
                else
                {
                    done=wild;
                    switch(key)
                    {
                        case C_Keys.UP:if(wild)GDVars.gFacing=C_Dir.NORTH;else{MoveForward();done=true;}break;
                        case C_Keys.DOWN:case C_Keys.LEFT:case C_Keys.RIGHT:
                            if(wild)GDVars.gFacing=(byte)(key==C_Keys.DOWN?C_Dir.SOUTH:key==C_Keys.LEFT?C_Dir.WEST:C_Dir.EAST);
                            else{GDVars.gFacing=(byte)((GDVars.gFacing+(key==C_Keys.DOWN?4:key==C_Keys.LEFT?6:2))%8);if(key!=C_Keys.DOWN)Sound(GDConsts.soundIds.combat[C_Sound.STEP]);GDVars.wallAhead=View3D.GetWallType(GDVars.gMapX,GDVars.gMapY,GDVars.gFacing);Draw3D(GDVars.gMapX,GDVars.gMapY,GDVars.gFacing);}break;
                        case C_Keys.F5:SaveLoad.QuickSave();done=false;break;case C_Keys.F8:done=AskQuickLoad();break;
                        case C_Keys.F6:if(GDVars.companionOn!=0){CompTool.ToolsMenu();if(wild)DisplayWilderness();else DisplayInterface();}done=false;break;
                        default:Party.SelectPrevNextChar((char)key);CharDisp.DisplayParty(GDVars.curChar);done=false;break;
                    }
                }PrintInfo();
            }while(!done);
            GDVars.locTypePrev=GDVars.gLocType;if(GDVars.bottomMsgCleared==0){Border.ClearBox(1,17,38,22);GDVars.bottomMsgCleared=1;}return (char)key;
        }
        public static void ResolveMovement()
        {
            bool moved=false;byte loc=GDVars.gLocType;
            if(loc==C_Area.LOC_3D_MAP)
            {
                if(GDVars.zone1[C_Gbvm.MOVE_LOCKED]<255)
                {
                    byte type=View3D.GetBarrierType(GDVars.gMapX,GDVars.gMapY,GDVars.gFacing);if(type==1)moved=true;
                    if(type==2||type==3)
                    {
                        string menu=GDVars.doorOpen[0]!=0?"Bash":"";if(GDVars.doorOpen[1]!=0&&PartyHasClass(C_Level.THIEF))menu+=" Pick";
                        if(GDVars.doorOpen[2]!=0&&(type==2?FindCharWithSpell(C_Spell.KNOCK)!=null:PartyHasClass(C_Level.MAGE)))menu+=" Knock";menu+=" Exit";
                        if(menu==" Exit")ChangeBarrierImpassable(GDVars.gMapX,GDVars.gMapY,GDVars.gFacing);
                        else{bool ext=false;byte key=MenuCore.MenuHorizontal("Locked. ",menu,13,10,15,false,false,ref ext);if(key=='B')moved=BashDoor();if(key=='P'){if(type==2)moved=PickLock();else GDVars.doorOpen[1]=0;}if(key=='K')moved=PartyCastsKnock();}
                    }
                    if(moved)MovePartyForward3D();else Sound(GDConsts.soundIds.combat[C_Sound.IMPACT]);PrintInfo();
                }else GDVars.zone1[C_Gbvm.MOVE_LOCKED]=0;
            }
            else if(loc>=C_Area.LOC_WILD_WEST&&loc<=C_Area.LOC_WILD_EAST){if(GDVars.zone1[C_Gbvm.MOVE_LOCKED]<255){MovePartyWilderness();PrintInfo();}else GDVars.zone1[C_Gbvm.MOVE_LOCKED]=0;}
            Portrait.FreeAnimation(ref GDVars.animData);Blit.FreePic(ref GDVars.dsPortraitCache[1].pic);Blit.FreePic(ref GDVars.dsPortraitCache[2].pic);GDVars.dsPortraitCache[1].pictureId=255;GDVars.dsPortraitCache[2].pictureId=255;
        }
    }
}
