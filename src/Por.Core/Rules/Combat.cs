// Direct conversion of pinned src/combat.pas; CbtMapBindings installs map calls; other gameplay services require explicit binding.
using System;
using static Por.Core.Types;
namespace Por.Core
{
    public interface ICombatServices
    {
        TCastUI CombatTargetMenu{get;}TCastUI NoncombatTargetMenu{get;}
        void CombatStart();void RoundStart(TCharRecord c);void SetNoncombatPalette();
        void TriggerEffects(byte condition,TCharRecord c);void ConsiderStinkingCloud(TCharRecord c);void AdvanceTime(byte amount,sbyte unit);void CalcHostileHpPercent();
        void DisplayMapCentered(TCharRecord c,byte margin,bool cursor);void RedrawCell(sbyte x,sbyte y);sbyte CellX(TCharRecord c);sbyte CellY(TCharRecord c);byte Slot(TCharRecord c);
        void ScrollView(sbyte x,sbyte y,byte margin,byte direction);void UpdateTile(sbyte x,sbyte y,byte slot);bool IsValidCreatureLoc(TCharRecord c,sbyte x,sbyte y,bool check);
        void SetFacing(TCharRecord c,byte dir,byte a,byte b);void ProbeDirection(TCharRecord c,byte dir,out byte creature,out byte terrain);
        void AiControl(TCharRecord c);void ToggleQuickMagic();void AllToManual();
        void CastSpellNC(byte spell,bool a,bool b,ref bool done);void ViewCharacter(ref bool done);void UseItem(ref bool done);void SetCurAttacksX2(TCharRecord c);
        void AimMenu(TCharRecord c,byte spell,byte mode,byte range,ref bool done,ref object target);void CastSpellCombat(byte spell,byte item,ref bool done);
        void TurnUndead(TCharRecord c);byte TurnLevel(TCharRecord c);bool StingVictoryCheat();
        bool FleeAttempt(TCharRecord c);void MoveOpportunityAttacks(TCharRecord c,byte direction);void MoveCreature(TCharRecord c,byte direction);
        bool CheckAttackAlly(TCharRecord a,TCharRecord d);bool SweepAttempt(TCharRecord a,TCharRecord d);void NoteAttackDirection(TCharRecord a,TCharRecord d);void CombatAttack(TCharRecord a,TCharRecord d,bool free,object missile,ref bool done);
    }
    public static class Combat
    {
        public static ICombatServices Services;
        private static ICombatServices S{get{if(Services==null)throw new NotSupportedException("Combat: combat map/actions/AI/effect services missing.");return Services;}}
        internal static Action<TCharRecord> PrintInfo=CharDisp.CombatPrintCreatureInfo;
        internal static Action DisplayInterface=CharDisp.DisplayCombatInterface;
        internal static Action<TCharRecord,string,byte,bool> NameMessage=CharDisp.PrintNameString;
        internal static void Delay(ushort ms){if(TextRend.DelayMilliseconds==null)throw new NotSupportedException("Combat: delay host missing.");TextRend.DelayMilliseconds(ms);}
        internal static void CombatDoneCleanup(){while(GDVars.stinkHead!=null)GDVars.stinkHead=GDVars.stinkHead.next;GDVars.arenaPtr=null;Blit.FreePic(ref GDVars.dsMoveIconSheet);S.SetNoncombatPalette();GDVars.castTargetMenu=S.NoncombatTargetMenu;}
        public static void CombatMain()
        {
            GDVars.ifcState=C_Ifc.COMBAT;GDVars.castTargetMenu=S.CombatTargetMenu;S.CombatStart();bool done=GDVars.teamCount[0]==0||GDVars.teamCount[1]==0;
            while(!done){CharHlp.CountTeams();for(var c=GDVars.creatureList;c!=null;c=c.nextCharAddr)S.RoundStart(c);GDVars.zone1[C_Gbvm.SURPRISE]=0;TCharRecord next=null;NextCreature(ref next);while(next!=null){CreatureAction(next);NextCreature(ref next);}EndOfRound(ref done);}CombatDoneCleanup();
        }
        internal static void NextCreature(ref TCharRecord next)
        {
            byte best=0,bestRoll=0;for(var p=GDVars.creatureList;p!=null;p=p.nextCharAddr){byte roll=Dice.RollDice(1,100);if(p.combatAddr.initiative>best)bestRoll=roll;if(p.combatAddr.initiative>=best&&roll>=bestRoll){bestRoll=roll;best=unchecked((byte)p.combatAddr.initiative);next=p;}}if(best==0)next=null;
        }
        internal static void CreatureAction(TCharRecord c)
        {
            c.combatAddr.flankCount=0;c.combatAddr.flankSpread=0;c.combatAddr.guarding=0;S.TriggerEffects(C_Cond.TURN_START,c);if(c.combatAddr.initiative<=0)return;if(c.combatAddr.initiative==20)c.combatAddr.initiative=19;
            GDVars.curChar=c;S.DisplayMapCentered(c,2,true);CharHlp.RecalcStats(c);PrintInfo(c);S.TriggerEffects(C_Cond.ACTION_START,c);if(c.combatAddr.initiative>0){if(c.quickfight!=0)S.AiControl(c);else ManualControl(c);}S.RedrawCell(S.CellX(c),S.CellY(c));
        }
        internal static void ManualControl(TCharRecord c)
        {
            if(c.enabled==0){CharHlp.EndActions(c);return;}bool done=false;
            if(c.combatAddr.spellCast>0){byte spell=c.combatAddr.spellCast;c.combatAddr.spellCast=0;S.CastSpellNC(spell,false,true,ref done);CharHlp.EndActions(c);return;}
            while(!done)
            {
                byte ch=0;CombatMenu(c,ref ch);object target=null;
                switch(ch)
                {
                    case (byte)'M':MoveMenu(c,ref done);break;
                    case (byte)'V':S.ViewCharacter(ref done);S.SetCurAttacksX2(c);if(!done)DisplayInterface();break;
                    case (byte)'A':S.AimMenu(c,255,1,0,ref done,ref target);break;
                    case (byte)'U':S.UseItem(ref done);S.SetCurAttacksX2(c);if(!done)DisplayInterface();break;
                    case (byte)'C':S.CastSpellCombat(0,0,ref done);break;
                    case (byte)'T':S.TurnUndead(c);done=CharHlp.EndActions(c);break;
                    case (byte)'Q':TurnOnAuto(c);TextRend.ClearBottomLine();Delay(200);done=true;S.AiControl(c);break;
                    case (byte)'D':DoneMenu(c,ref done);break;
                    case C_Keys.ALT_M:S.ToggleQuickMagic();break;case (byte)' ':S.AllToManual();break;
                    case C_Keys.ALT_Q:c.combatAddr.initiative=20;for(var p=GDVars.creatureList;p!=null;p=p.nextCharAddr)TurnOnAuto(p);TextRend.ClearBottomLine();Delay(200);done=true;break;
                    case C_Keys.ALT_X:if(S.StingVictoryCheat()){S.DisplayMapCentered(c,3,false);done=true;}else CharDisp.PrintBottomLine("That doesn't work");break;
                }
                if(!done){S.DisplayMapCentered(c,2,true);PrintInfo(c);}
            }
        }
        internal static void CombatMenu(TCharRecord c,ref byte key)
        {
            S.TriggerEffects(C_Cond.ACTION_START,c);string menu=c.combatAddr.movement>0?"Move ":"";menu+="View Aim ";if(c.numberOfItems>0)menu+="Use ";bool spell=false;for(int i=0;i<=SPELL_SLOT_LAST;i++)if(c.memorizedSpells[i]>0)spell=true;
            if((spell&&c.combatAddr.castAllowed!=0||CharHlp.HasScrollSpells(c))&&GDVars.zone0.antimagic==0)menu+="Cast ";if(S.TurnLevel(c)>0&&c.combatAddr.usedTurnUndead==0)menu+="Turn ";menu+="Quick Done";if(menu.Length>40)menu=menu.Substring(0,40);
            do{key=MenuCore.MenuHorizontal("",menu,13,10,15,true,false,ref spell);if(spell&&key!=C_Keys.ALT_Q&&key!=C_Keys.ALT_R&&key!=C_Keys.ALT_X&&key!=C_Keys.ALT_M)key=0;}while(key!=C_Keys.ALT_Q&&key!=C_Keys.ALT_R&&key!=' '&&key!=C_Keys.ALT_X&&key!=C_Keys.ALT_M&&key!='A'&&key!='C'&&key!='D'&&key!='M'&&key!='Q'&&key!='T'&&key!='U'&&key!='V');
        }
        internal static void EndOfRound(ref bool done)
        {
            S.AdvanceTime(1,C_Time.MINUTES);GDVars.roundCounter=unchecked((byte)(GDVars.roundCounter+1));S.CalcHostileHpPercent();
            for(var p=GDVars.creatureList;p!=null;p=p.nextCharAddr){S.TriggerEffects(C_Cond.ROUND_END,p);S.ConsiderStinkingCloud(p);if(p.status==C_Status.DYING){p.combatAddr.overkill=unchecked((byte)(p.combatAddr.overkill+1));if(p.combatAddr.overkill>9)p.status=C_Status.DEAD;Stats.BledOut(p);}}
            if(BandageDying(false))CharDisp.PrintBottomLine("Your Teammate is Dying");CharHlp.CountTeams();S.ScrollView(unchecked((sbyte)(GDVars.arenaPtr.ox+3)),unchecked((sbyte)(GDVars.arenaPtr.oy+3)),255,C_Dir.CENTER);
            if(GDVars.teamCount[0]==0||GDVars.teamCount[1]==0)done=true;if(GDVars.teamCount[0]>0&&GDVars.teamCount[1]==0&&GDVars.suppressContinuePrompt==0&&MenuCore.YesNoMenu("Continue Battle:",13,10,15)=='Y')done=false;
        }
        internal static void MoveMenu(TCharRecord c,ref bool done)
        {
            byte savedMove=c.combatAddr.movement,savedDir=c.combatAddr.direction; sbyte startX=S.CellX(c),startY=S.CellY(c);byte key=32,dir=C_Dir.CENTER;done=false;
            while(c.combatAddr.movement>1&&key!=0&&key!=C_Keys.ENTER)
            {
                Border.ClearBox(24,24,39,24);MenuCore.arrowsToCaller=true;key=MenuCore.MenuHorizontal("Move/Attack, Move Left = "+StrUtil.NumStr(c.combatAddr.movement/2)+" ","",10,10,15,true,false,ref GDVars.doneRoundFlag);
                switch(key)
                {
                    case 0:c.combatAddr.movement=savedMove;if(S.Slot(c)>0)S.UpdateTile(0,0,S.Slot(c));done=!S.IsValidCreatureLoc(c,startX,startY,false);S.ScrollView(S.CellX(c),S.CellY(c),0,C_Dir.CENTER);c.combatAddr.direction=savedDir;dir=C_Dir.CENTER;break;
                    case C_Keys.UP:dir=C_Dir.NORTH;break;case C_Keys.PGUP:dir=C_Dir.NORTHEAST;break;case C_Keys.RIGHT:dir=C_Dir.EAST;break;case C_Keys.PGDN:dir=C_Dir.SOUTHEAST;break;case C_Keys.DOWN:dir=C_Dir.SOUTH;break;case C_Keys.END_KEY:dir=C_Dir.SOUTHWEST;break;case C_Keys.LEFT:dir=C_Dir.WEST;break;case C_Keys.HOME:dir=C_Dir.NORTHWEST;break;default:dir=C_Dir.CENTER;break;
                }
                if(dir>=C_Dir.CENTER)continue;S.SetFacing(c,dir,0,0);byte cr,terrain;S.ProbeDirection(c,dir,out cr,out terrain);
                if(cr>0)MeleeAttack(c,GDVars.creatureArr[cr],ref done);
                else if(terrain==0){byte answer=MenuCore.YesNoMenu("Flee:",13,10,15);if(answer=='Y')done=S.FleeAttempt(c);if(answer=='N')done=false;}
                else if(GDConsts.terrainInfo[terrain].cost>c.combatAddr.movement)CharDisp.PrintBottomLine("Blocked");
                else{S.MoveOpportunityAttacks(c,dir);if(c.enabled==0)done=CharHlp.EndActions(c);else{S.MoveCreature(c,dir);if(c.enabled==0)done=CharHlp.EndActions(c);S.ConsiderStinkingCloud(c);if(CharHlp.IsHelpless(c))done=CharHlp.EndActions(c);}}
            }if(c.combatAddr.movement<2)c.combatAddr.movement=0;
        }
        internal static void MeleeAttack(TCharRecord a,TCharRecord d,ref bool done)
        {
            if(CharHlp.HasRangedWeapon(a)&&!CharHlp.HasThrowableWeapon(a))CharDisp.PrintBottomLine("Not with that weapon");else if(S.CheckAttackAlly(a,d)){a.combatAddr.other=d;if(S.SweepAttempt(a,d))done=true;else{S.NoteAttackDirection(a,d);S.CombatAttack(a,d,false,null,ref done);}}
        }
        internal static void DoneMenu(TCharRecord c,ref bool done)
        {
            done=false;string menu=!CharHlp.HasRangedWeapon(c)||CharHlp.HasThrowableWeapon(c)?"Guard ":"";menu+="Delay Quit ";if(BandageDying(false))menu+="Bandage ";menu+="Speed Exit";if(menu.Length>40)menu=menu.Substring(0,40);byte key=32;
            while(key!=0&&key!='E'&&!done){key=MenuCore.MenuHorizontal("",menu,13,10,15,false,false,ref GDVars.doneRoundFlag);switch(key){case (byte)'G':done=CharHlp.Guard(c);CharDisp.PrintBottomLine("Guarding");break;case (byte)'D':c.combatAddr.initiative=1;done=true;break;case (byte)'Q':done=CharHlp.EndActions(c);break;case (byte)'B':BandageDying(true);done=CharHlp.EndActions(c);break;case (byte)'S':SpeedMenu();break;}}
        }
        public static bool BandageDying(bool bandage)
        {
            bool found=false;for(var p=GDVars.creatureList;p!=null;p=p.nextCharAddr)if(p.combatAddr.notPcNpc==0&&p.hostile==0&&p.status==C_Status.DYING){found=true;if(bandage){p.status=C_Status.UNCONSCIOUS;p.combatAddr.overkill=0;NameMessage(p,"is bandaged",10,true);bandage=false;}}return found;
        }
        internal static void SpeedMenu()
        {
            byte speed=GDVars.gameDelay,key=32;while(key!=0&&key!='E'){string menu=" ";if(GDVars.gameDelay<9)menu+="Slower ";if(GDVars.gameDelay>0)menu+="Faster ";menu+="Exit";key=MenuCore.MenuHorizontal("GameSpeed ("+StrUtil.NumStr(GDVars.gameDelay)+") :",menu,13,10,15,false,false,ref GDVars.doneRoundFlag);if(key=='S')GDVars.gameDelay=unchecked((byte)(GDVars.gameDelay+1));if(key=='F')GDVars.gameDelay=unchecked((byte)(GDVars.gameDelay-1));}if(speed!=GDVars.gameDelay)Options.SaveOptions();
        }
        internal static void TurnOnAuto(TCharRecord c){c.quickfight=1;if(c.combatAddr.other!=null&&c.combatAddr.other.hostile==c.hostile)c.combatAddr.other=null;}
    }
}
