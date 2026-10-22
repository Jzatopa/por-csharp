// Direct conversion of pinned src/postcbt.pas.
using System;
using static Por.Core.Types;
namespace Por.Core
{
    public interface IPostCombatServices{void ViewCharacter(ref bool exit);void CastSpellNC(byte spell,bool a,bool b,ref bool exit);}
    public static class PostCbt
    {
        public static IPostCombatServices Services;
        private static IPostCombatServices S{get{if(Services==null)throw new NotSupportedException("PostCbt: ViewChar/Magic services missing.");return Services;}}
        internal static bool nonEscapedHostileFound,partyFled,combatWon;internal static byte nonAnimCount;internal static int expShare;
        internal static Action DisplayInterface=CharDisp.DisplayNoncombatInterface;
        internal static void DetermineLootExp(ref int share)
        {
            unchecked
            {
                if(GDVars.duelFlag&&GDVars.zone1[C_Gbvm.MIRROR_DUEL]!=0){share=GDVars.curChar.levelHighest*100;return;}
                int total=0;byte count=0;
                for(var p=GDVars.creatureList;p!=null;p=p.nextCharAddr)if(p.hostile==1&&p.status!=C_Status.RUNNING)
                {
                    nonEscapedHostileFound=true;for(int i=0;i<7;i++)GDVars.treasureMoney[i]+=p.coins[i];total+=p.xpAward+p.xpBonusPerHp*p.hpRolled;
                    if(GDVars.zone1[C_Gbvm.NO_COMBAT_ITEMS]!=1)for(var item=p.itemsAddr;item!=null;item=item.next)if(item.value>0||(count<8&&Dice.RollDice(1,10)<=3)){count++;CharHlp.ItemLabel(item,false);CharHlp.PrependItemCopy(ref GDVars.treasureItems,item);GDVars.treasureItems.equipped=0;}
                }
                total+=GDVars.treasureMoney[0]/200+GDVars.treasureMoney[1]/20+GDVars.treasureMoney[2]/2+GDVars.treasureMoney[3]+GDVars.treasureMoney[4]*5+GDVars.treasureMoney[5]*250+GDVars.treasureMoney[6]*2200;
                for(var q=GDVars.treasureItems;q!=null&&q!=GDVars.lastMissileItem;q=q.next)if(q.plus>0)total+=q.plus*400;
                short sharers=(short)(GDVars.zone1[C_Gbvm.PARTY_SIZE]-nonAnimCount);share=sharers>0?total/sharers:0;
            }
        }
        internal static void AwardExperience(int xp){for(var p=GDVars.creatureList;p!=null;p=p.nextCharAddr)if(p.enabled!=0&&p.status!=C_Status.ANIMATED)p.experience=unchecked(p.experience+ClassCal.ExperienceShare(p,xp));}
        internal static bool IsMonster(TCharRecord c){return c.combatAddr!=null&&c.combatAddr.notPcNpc==1;}
        internal static void DetermineCombatResults()
        {
            nonAnimCount=0;GDVars.partyDefeated=true;partyFled=false;
            for(var p=GDVars.creatureList;p!=null&&!IsMonster(p);p=p.nextCharAddr)if(p.status==C_Status.RUNNING)partyFled=true;
            if(GDVars.duelFlag)GDVars.partyDefeated=false;combatWon=false;
            if(!GDVars.duelFlag)
            {
                for(var p=GDVars.creatureList;p!=null&&!IsMonster(p);p=p.nextCharAddr)
                {
                    if((p.status==C_Status.RUNNING||p.status==C_Status.ANIMATED||p.status==C_Status.OKAY||GDVars.zone1[C_Gbvm.MIRROR_DUEL]==1)&&p.hostile==0&&!CharHlp.IsNpc(p))GDVars.partyDefeated=false;
                    if(p.status==C_Status.ANIMATED||p.status==C_Status.OKAY){combatWon=true;partyFled=false;}
                    if(p.enabled==0||p.status==C_Status.ANIMATED)nonAnimCount=unchecked((byte)(nonAnimCount+1));
                }
                if(combatWon){DetermineLootExp(ref expShare);AwardExperience(expShare);}
                var p2=GDVars.creatureList;
                while(p2!=null&&!IsMonster(p2))
                {
                    var next=p2.nextCharAddr;
                    if(GDVars.partyDefeated){GDVars.curChar=p2;SaveLoad.RemoveCreature(false,true);}
                    else if(partyFled){GDVars.zone1[C_Gbvm.COMBAT_RESULT]=0x81;if(p2.status==C_Status.RUNNING){p2.status=C_Status.OKAY;p2.enabled=1;}else{GDVars.curChar=p2;SaveLoad.RemoveCreature(false,true);}}
                    else if(p2.status==C_Status.RUNNING||p2.status==C_Status.UNCONSCIOUS&&p2.hpCur>0){p2.status=C_Status.OKAY;p2.enabled=1;}
                    else if(p2.status==C_Status.DYING)p2.status=C_Status.UNCONSCIOUS;
                    p2=next;
                }
                if(GDVars.partyDefeated)GDVars.zone1[C_Gbvm.PARTY_SIZE]=0;
            }
            else
            {
                for(var p=GDVars.creatureList;p!=null;p=p.nextCharAddr)if(p.enabled==1&&p.status==C_Status.OKAY&&p.hostile!=1){combatWon=true;DetermineLootExp(ref expShare);AwardExperience(expShare);}
                for(var p=GDVars.creatureList;p!=null;p=p.nextCharAddr){if(p.status==C_Status.OKAY||p.status==C_Status.ANIMATED)p.enabled=1;if(p.status==C_Status.DYING)p.status=C_Status.UNCONSCIOUS;}
            }
        }
        internal static void PostCombatSummary(int xp)
        {
            Border.DrawWindow(1,1,38,22,0,15,"");string msg;
            if(nonEscapedHostileFound||GDVars.duelFlag){if(GDVars.duelFlag&&!combatWon){msg="You have lost the duel.";xp=0;}else msg=GDVars.duelFlag?"You have won the duel.":"The party has won.";}
            else{msg=partyFled?"The party has fled.":"The party has found Treasure!";if(partyFled)xp=0;}
            TextRend.DisplayString(1,3,10,msg);string num=xp.ToString(System.Globalization.CultureInfo.InvariantCulture);if(num.Length>10)num=num.Substring(0,10);TextRend.DisplayString(1,5,10,(GDVars.duelFlag?"The duelist receives ":"Each character receives ")+num);TextRend.DisplayString(1,7,10,"experience points.");bool ext=false;MenuCore.MenuHorizontal("","press <enter>/<return> to continue",15,15,15,true,false,ref ext);
        }
        internal static void LootTakeItemSelection(ref byte c,ref TItemRec selected,ref TItemRec cursor,ref short idx)
        {
            bool redraw=true;Border.DrawWindow(1,1,38,22,0,15,"Loot");for(var p=GDVars.treasureItems;p!=null;p=p.next)CharHlp.ItemLabel(p,false);c=MenuCore.MenuVertical("Items: ","Take",13,10,15,1,4,38,22,GDVars.treasureItems,true,ref redraw,ref idx,ref cursor);selected=cursor;
        }
        internal static void LootTakeItemMenu()
        {
            TItemRec item=null,cursor=GDVars.treasureItems;short idx=0;byte key=0;bool done;
            do{LootTakeItemSelection(ref key,ref item,ref cursor,ref idx);done=key!='T'&&key!=C_Keys.ENTER;if(!done){bool failed=false;Shop.GiveItemCopyToCreature(item,ref failed);if(!failed&&GDVars.ifcState==C_Ifc.LOOT)Stats.Count(GDVars.curChar,Stats.ITEMS_LOOTED,1);if(!failed){var p=GDVars.treasureItems;if(p==item)GDVars.treasureItems=item.next;else{while(p.next!=item)p=p.next;p.next=item.next;}item.next=null;if(GDVars.treasureItems==null)done=true;}}}while(!done);DisplayInterface();
        }
        public static void LootTakeMenu(ref bool treasure,ref bool items)
        {
            bool ext=false,done=false;
            if(treasure){if(items)do{byte c=MenuCore.MenuHorizontal("Take: ","Money Items Exit",13,10,15,true,true,ref ext);switch(c){case (byte)'M':Money.TakeMoneyMenu();DisplayInterface();break;case (byte)'I':LootTakeItemMenu();break;case (byte)'E':case 0:done=true;break;case (byte)'G':case (byte)'O':Party.SelectPrevNextChar((char)c);break;}CharDisp.DisplayParty(GDVars.curChar);Money.DetermineTreasure(ref treasure,ref items);if(!treasure||!items)done=true;}while(!done);else{Money.TakeMoneyMenu();DisplayInterface();}}
            else LootTakeItemMenu();
        }
        internal static void LootMenu()
        {
            bool treasure=false,items=false,done=false,ext=false;DisplayInterface();
            do{Money.DetermineTreasure(ref treasure,ref items);string tail=" Exit";byte detect=0;
                if(items)for(int i=0;i<=SPELL_SLOT_LAST;i++)if(GDVars.curChar.memorizedSpells[i]==5||GDVars.curChar.memorizedSpells[i]==11){detect=GDVars.curChar.memorizedSpells[i];tail=" Detect Exit";break;}
                string menu=treasure?"View Take Pool Share"+tail:items?"View Take Pool"+tail:"View Pool Exit";byte c=MenuCore.MenuHorizontal("",menu,13,10,15,true,true,ref ext);Stats.MarkCoins();
                switch(c){case (byte)'V':S.ViewCharacter(ref ext);break;case (byte)'T':LootTakeMenu(ref treasure,ref items);break;case (byte)'P':if(!ext)Money.PoolMoney();break;case (byte)'S':Money.ShareMoney();break;case (byte)'D':S.CastSpellNC(detect,false,false,ref ext);break;case (byte)'E':case 0:Money.DetermineTreasure(ref treasure,ref items);done=!(treasure||items)||!Money.GoBackForIt("There is still treasure left.  ","Do you want to go back and claim your treasure?");break;case (byte)'G':case (byte)'O':Party.SelectPrevNextChar((char)c);CharDisp.DisplayParty(GDVars.curChar);break;}Stats.SettleCoins();
            }while(!done);
        }
        internal static void PostCombatCreatureCleanup()
        {
            GDVars.zone1[C_Gbvm.MONSTERS_DEFEATED]=0;var p=GDVars.creatureList;
            while(p!=null){if(IsMonster(p)||p.hostile==1){if(p.enabled!=1)GDVars.zone1[C_Gbvm.MONSTERS_DEFEATED]=unchecked((ushort)(GDVars.zone1[C_Gbvm.MONSTERS_DEFEATED]+1));var next=p.nextCharAddr;GDVars.curChar=p;SaveLoad.RemoveCreature(IsMonster(p),true);p=next;}else{p.combatAddr=null;if(GDVars.quickEnds!=0&&CharHlp.ComesUnderPlayer(p))p.quickfight=0;p=p.nextCharAddr;}}
            GDVars.curChar=GDVars.creatureList;
        }
        internal static void NpcTreasureShare()
        {
            byte npc=0,total=0;for(var p=GDVars.creatureList;p!=null;p=p.nextCharAddr){if(CharHlp.IsNpc(p)&&p.status==C_Status.OKAY){npc=unchecked((byte)(npc+(p.npcTreasureShare&7)));total=unchecked((byte)(total+(p.npcTreasureShare&7)));}else total=unchecked((byte)(total+1));}
            bool took=false;if(npc>0)for(int i=0;i<7;i++)if(GDVars.treasureMoney[i]>0){GDVars.treasureMoney[i]=unchecked(GDVars.treasureMoney[i]-npc*(GDVars.treasureMoney[i]/total));took=true;}
            if(took){Border.DrawWindow(1,1,38,22,0,15,"");byte row=0;for(var p=GDVars.creatureList;p!=null;p=p.nextCharAddr)if(CharHlp.IsNpc(p)&&p.status==C_Status.OKAY&&p.npcTreasureShare>0){TextRend.DisplayMessageWindow(5,unchecked((byte)(row+5)),34,22,10,true,p.name+" takes and hides his share.");row=unchecked((byte)(row+2));}bool ext=false;MenuCore.MenuHorizontal("","press <enter>/<return> to continue",15,15,15,true,false,ref ext);}
        }
        public static void PostCombat()
        {
            GDVars.zone1[C_Gbvm.COMBAT_RESULT]=0;nonEscapedHostileFound=false;GDVars.ifcState=C_Ifc.LOOT;DetermineCombatResults();PostCombatCreatureCleanup();
            if(!GDVars.partyDefeated||GDVars.duelFlag){NpcTreasureShare();PostCombatSummary(expShare);LootMenu();CharHlp.FreeItemList(ref GDVars.treasureItems);}
            else{GDVars.zone1[C_Gbvm.COMBAT_RESULT]=0x80;Border.DrawWindow(1,1,38,22,0,15,"The END!");GDVars.curCol=2;GDVars.curRow=6;TextRend.DisplayMessageWindow(2,5,37,22,10,true,"The monsters rejoice for the party has been destroyed");TextRend.DisplayAndPause(13,"Press any key to continue");}
            GDVars.msgDelayFlag=1;foreach(int a in new[]{C_Gbvm.MOD_MONSTER_THAC0,C_Gbvm.MOD_PARTY_THAC0,C_Gbvm.MOD_PARTY_MOVE,C_Gbvm.NO_COMBAT_ITEMS,C_Gbvm.MIRROR_DUEL})GDVars.zone1[a]=0;
        }
    }
}
