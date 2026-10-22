// Direct source conversion: pinned src/gametime.pas (7616920a).
using System;
using static Por.Core.Types;
namespace Por.Core
{
    public interface IRestServices
    {
        bool RestoreHitPoints(TCharRecord c,byte hp,bool message);
        void AdvanceTime(byte amount,sbyte unit);
        void PrintCastSpellMsg(TCharRecord c,string message,byte spell);
        void RemoveSpellFromScroll(TCharRecord c,TItemRec item,byte spell);
    }
    public static class GameTime
    {
        public static IRestServices Services;
        private static IRestServices S { get { if(Services==null)throw new NotSupportedException("GameTime: FxEngine/Magic not bound.");return Services;} }
        internal static readonly byte[] prepTimers=new byte[9];
        internal static void ValidateRestTime()
        {
            var t=GDVars.dsRestingTimeGbvm;Clock.ValidateClock(t,false);
            if(t[C_Time.MONTHS]>0){t[C_Time.DAYS]=unchecked((ushort)(t[C_Time.DAYS]+GDConsts.unitScale[C_Time.DAYS]*t[C_Time.MONTHS]));t[C_Time.MONTHS]=0;if(t[C_Time.DAYS]>99)t[C_Time.DAYS]=99;}
        }
        internal static void DecrementRestTime(byte count,sbyte unit)
        {
            var t=GDVars.dsRestingTimeGbvm;
            if(t[4]==0&&t[3]==0&&t[2]==0&&t[1]==0)return;
            while(count>t[unit])
            {
                int j=unit+1;while(t[j]==0&&j<C_Time.MONTHS)j++;
                if(j==C_Time.MONTHS){Array.Clear(t.Data,0,t.Data.Length);count=0;}
                else for(int k=j;k>unit;k--){t[k]=unchecked((ushort)(t[k]-1));t[k-1]=unchecked((ushort)(t[k-1]+GDConsts.unitScale[k-1]));}
            }
            t[unit]=unchecked((ushort)(t[unit]-count));ValidateRestTime();
        }
        internal static void DisplayRestTime(sbyte hl)
        {
            byte[] colors={10,10,10,10,10,10,10};colors[hl]=15;
            TextRend.DisplayString(1,17,10,"Rest Time:");byte x=11;var t=GDVars.dsRestingTimeGbvm;
            for(int i=C_Time.DAYS;i>=C_Time.HOURS;i--){TextRend.DisplayString((byte)(x+1),17,colors[i],StrUtil.ZeroPad2(t[i]));TextRend.DisplayString((byte)(x+3),17,10,":");x+=3;}
            TextRend.DisplayString((byte)(x+1),17,colors[C_Time.TURNS],StrUtil.ZeroPad2(unchecked((ushort)(10*t[C_Time.TURNS]+t[C_Time.MINUTES]))));
        }
        internal static bool CampRestMenu()
        {
            bool result=false,ext=false;byte key;sbyte sel=C_Time.TURNS;
            do
            {
                DisplayRestTime(sel);MenuCore.arrowsToCaller=true;
                key=MenuCore.MenuHorizontal("","Rest   daYs Hours Mins   Inc Dec   Exit",13,10,15,true,false,ref ext);
                if(ext)switch(key){case C_Keys.UP:key=(byte)'I';break;case C_Keys.DOWN:key=(byte)'D';break;case C_Keys.LEFT:sel++;if(sel>C_Time.DAYS)sel=C_Time.TURNS;key=88;break;case C_Keys.RIGHT:sel--;if(sel<C_Time.TURNS)sel=C_Time.DAYS;key=88;break;default:key=88;break;}
                switch(key)
                {
                    case (byte)'R':result=true;break;case (byte)'Y':sel=C_Time.DAYS;break;case (byte)'H':sel=C_Time.HOURS;break;case (byte)'M':sel=C_Time.TURNS;break;
                    case (byte)'I':int u=sel==C_Time.TURNS?C_Time.MINUTES:sel;GDVars.dsRestingTimeGbvm[u]=unchecked((ushort)(GDVars.dsRestingTimeGbvm[u]+(sel==C_Time.TURNS?5:1)));ValidateRestTime();break;
                    case (byte)'D':DecrementRestTime((byte)(sel==C_Time.TURNS?5:1),(sbyte)(sel==C_Time.TURNS?C_Time.MINUTES:sel));ValidateRestTime();break;
                }
            }while(key!=0&&key!='E'&&key!='R');return result;
        }
        internal static void CheckRestingHeal()
        {
            GDVars.healTicks=unchecked((ushort)(GDVars.healTicks+1));if(GDVars.healTicks<288)return;
            bool healed=false;for(var c=GDVars.creatureList;c!=null;c=c.nextCharAddr)if(S.RestoreHitPoints(c,1,false))healed=true;
            DisplayRestTime(0);TextRend.DisplayString(1,19,10,"The Whole Party Is Healed");if(healed)CharDisp.DisplayParty(GDVars.curChar);
            TextRend.PauseOnly();CharDisp.ClearMessageWindow();GDVars.healTicks=0;
        }
        internal static byte CheckMemorizeDone(TCharRecord c,ref bool done)
        {
            byte level=0;
            for(int i=0;i<=SPELL_SLOT_LAST&&level==0;i++)
            {
                byte sp=c.memorizedSpells[i];int id=sp&C_Spell.ID_MASK;
                if(sp>127&&id>=1&&id<=C_Spell.LAST)
                {if(done)level=GDConsts.spellTable[id].level;else{c.memorizedSpells[i]=unchecked((byte)(sp-C_Spell.PENDING_FLAG));DisplayRestTime(0);S.PrintCastSpellMsg(c,"has memorized",c.memorizedSpells[i]);done=true;}}
            }return level;
        }
        internal static byte CheckScribeDone(TCharRecord c,ref bool done)
        {
            byte level=0;var item=c.itemsAddr;
            while(item!=null&&level==0)
            {
                var next=item.next;
                if(CharHlp.ItemReadable(item))for(int j=1;j<=3&&level==0;j++)
                {
                    byte sp=item.spells[j];byte id=(byte)(sp&C_Spell.ID_MASK);
                    if(sp>128&&id<=C_Spell.LAST)
                    {
                        if(done)level=GDConsts.spellTable[id].level;
                        else{c.spellsKnown[id]=1;S.RemoveSpellFromScroll(c,item,id);DisplayRestTime(0);S.PrintCastSpellMsg(c,"has scribed",id);done=true;var held=c.itemsAddr;while(held!=null&&held!=item)held=held.next;if(held==null)j=3;}
                    }
                }item=next;
            }return level;
        }
        internal static void AdvanceSpellPrep()
        {
            int i=1;for(var c=GDVars.creatureList;c!=null&&i<=8;c=c.nextCharAddr,i++)
            {
                prepTimers[i]=unchecked((byte)(prepTimers[i]-1));if(prepTimers[i]!=0||c.spellPrepTime!=0)continue;
                bool done=false;byte level=CheckScribeDone(c,ref done);if(level==0)level=CheckMemorizeDone(c,ref done);prepTimers[i]=unchecked((byte)(level*3));
            }
        }
        internal static void AdvanceSpellPrepBase(ref byte tick)
        {
            tick=unchecked((byte)(tick+1));if(tick<12)return;tick=0;byte i=1;
            for(var c=GDVars.creatureList;c!=null;c=c.nextCharAddr,i=unchecked((byte)(i+1)))
                if(unchecked((sbyte)c.spellPrepTime)>0)
                {c.spellPrepTime--;if(c.spellPrepTime==0){bool done=true;byte level=CheckScribeDone(c,ref done);if(level==0)level=CheckMemorizeDone(c,ref done);if(i<=8)prepTimers[i]=unchecked((byte)(level*3));}}
        }
        public static bool CampRest(bool interactive)
        {
            int i=1;for(var c=GDVars.creatureList;c!=null&&i<=8;c=c.nextCharAddr,i++)prepTimers[i]=0;
            for(i=0;i<GDVars.effFlags.Length;i++)GDVars.effFlags[i]=1;
            byte tick=0,cnt=0;bool interrupted=false;TextRend.ClearRect(1,17,38,22);DisplayRestTime(0);GDVars.dsSuppressDrawingBorderFlag=1;
            bool stop=interactive&&!CampRestMenu();var t=GDVars.dsRestingTimeGbvm;
            while(!stop&&(t[4]>0||t[3]>0||t[2]>0||t[1]>0))
            {
                if(GameIo.Host==null)throw new NotSupportedException("GameTime: input host missing.");
                if(GameIo.Host.KeyPressed){DisplayRestTime(0);if(MenuCore.YesNoMenu("Stop Resting? ",13,10,15)=='Y')stop=true;else TextRend.ClearBottomLine();}
                if(stop)continue;
                DecrementRestTime(5,C_Time.MINUTES);cnt++;if(cnt>=5){DisplayRestTime(0);cnt=0;}
                S.AdvanceTime(5,C_Time.MINUTES);CheckRestingHeal();AdvanceSpellPrep();AdvanceSpellPrepBase(ref tick);
                if(GDVars.zone1[C_Gbvm.REST_SAFE_TIME]>0)
                {
                    GDVars.restTicks=unchecked((ushort)(GDVars.restTicks+1));
                    if(GDVars.restTicks>=GDVars.zone1[C_Gbvm.REST_SAFE_TIME]){GDVars.restTicks=0;if(Dice.RollDice(1,100)<=GDVars.zone1[C_Gbvm.REST_INTERRUPT_PCT]){CharDisp.ClearMessageWindow();DisplayRestTime(0);TextRend.DisplayString(1,19,15,"The Party is rudely interrupted!");stop=true;interrupted=true;TextRend.PauseOnly();}}
                }
            }TextRend.ClearRect(1,17,38,22);GDVars.dsSuppressDrawingBorderFlag=0;return interrupted;
        }
    }
}
