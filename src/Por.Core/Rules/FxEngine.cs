// Direct conversion of Talonius PoR src/fxengine.pas at 7616920a9178fa32efd45939b9360e95933bbf49.
using System;
using static Por.Core.Types;
namespace Por.Core
{
    public static partial class FxEngine
    {
        // External UI/stat recalculation boundaries; every default is the converted source dependency.
        internal static Action<TCharRecord,string,byte,bool> PrintName=CharDisp.PrintNameString;
        internal static Action<TCharRecord,bool,string> AnimateName=CharDisp.PrintNameStringAnim;
        internal static Action<string,byte,bool> Message=CharDisp.MessageText;
        internal static Action MessageWait=CharDisp.MessageWait, ClearMessage=CharDisp.ClearMessageWindow;
        internal static Action<string> BottomLine=CharDisp.PrintBottomLine;
        internal static Action Pause=TextRend.PauseOnly, Flush=Blit.FlushView;
        internal static Action<TCharRecord> RecalcStats=CharHlp.RecalcStats;
        internal static Action<TCharRecord,byte> InflictDamage=CharHlp.InflictDamage;
        internal static Action<TItemRec,bool> ItemLabel=CharHlp.ItemLabel;
        private static byte B(int n){return unchecked((byte)n);}
        private static sbyte S(int n){return unchecked((sbyte)n);}
        private static string ShortString(string s,int cap){return s.Length>cap?s.Substring(0,cap):s;}
        public static void CallEffectHandler(byte effId,TCharRecord creature,object p,bool removed)
        {
            if(effId<C_Effect.BLESS||effId>C_Effect.RECENT_CURE)return;
            var handler=GDVars.effectFns[effId];
            if(handler==null)throw new NotSupportedException("FxEngine: effect handler "+effId+" is not bound (EffHndlr).");
            handler(creature,p,removed);
        }
        private static bool InList(TCharRecord c,TEffectRec effect)
        {
            for(var p=c.effectsAddr;p!=null;p=p.next)if(ReferenceEquals(p,effect))return true;
            return false;
        }
        public static void RemoveEffect(TCharRecord c,byte effId,object eff)
        {
            TEffectRec node=eff==null?null:(TEffectRec)eff;
            if(node==null){for(var p=c.effectsAddr;p!=null;p=p.next)if(p.id==effId){node=p;break;}}
            else if(!InList(c,node))return;
            if(node==null)return;
            if(node.onRemove)
            {
                CallEffectHandler(effId,c,node,true);
                // A handler may already have unlinked this node, including via another callback.
                if(!InList(c,node))return;
            }
            if(ReferenceEquals(c.effectsAddr,node))c.effectsAddr=node.next;
            else
            {
                var prev=c.effectsAddr;
                while(prev!=null&&!ReferenceEquals(prev.next,node))prev=prev.next;
                if(prev==null)return;
                prev.next=node.next;
            }
            // Managed identity replaces FreeMem; do not mutate node.next, which a callback may retain.
        }
        private static void CopyTargets(TTargetList src,TTargetList dst)
        {
            for(int i=1;i<=72;i++){dst[i].idx=src[i].idx;dst[i].dist=src[i].dist;dst[i].dir=src[i].dir;}
        }
        internal static void TriggerEffect(TCharRecord c,byte effId)
        {
            TEffectRec effect;
            bool found=CharHlp.HasEffect(c,effId,out effect);
            if(!found&&(effId==C_Effect.SILENCE||effId==C_Effect.PROT_EVIL_10FT||effId==C_Effect.PROT_GOOD_10FT||effId==C_Effect.PRAYER))
            {
                var saved=new TTargetList();CopyTargets(GDVars.targetList,saved);
                for(var p=GDVars.creatureList;p!=null&&!found;p=p.nextCharAddr)
                    if(CharHlp.HasEffect(p,effId,out effect))
                    {
                        if(GDVars.ifcState==C_Ifc.COMBAT)
                        {
                            byte range=(byte)(effId==C_Effect.PRAYER?6:1);
                            Pathing.BuildTargetList(CbtMap.CombatCellX(p),CbtMap.CombatCellY(p),range,255,CbtMap.GetIconSize(p),GDVars.arenaPtr);
                            for(int i=1;i<=GDVars.targetCount;i++)if(GDVars.targetList[i].idx==CbtMap.CombatSlot(c))found=true;
                        }
                        else found=true;
                    }
                // Source restores the records, but NOT targetCount. Keep that observable quirk.
                CopyTargets(saved,GDVars.targetList);
            }
            if(found)CallEffectHandler(effId,c,effect,false);
        }
        public static void ConsiderStinkingCloud(TCharRecord c)
        {
            if(c.enabled==0)return;
            byte cr,t;CbtMap.ProbeDirection(c,C_Dir.CENTER,out cr,out t);if(t!=C_Terr.STINK_CLOUD)return;
            TEffectRec effect;
            foreach(byte id in new byte[]{C_Effect.STINK_COUGH,C_Effect.HELPLESS,C_Effect.ANIMATE_DEAD,C_Effect.NO_PARA_POISON,C_Effect.NO_CHARM_POISON})if(CharHlp.HasEffect(c,id,out effect))return;
            bool saved=RollSavingThrow(c,C_Save.PARALYZE,S(AbilAdj.ConPoisonSave(c.conCur)));
            byte effectId=B(saved?C_Effect.STINK_COUGH:C_Effect.HELPLESS);
            ushort minutes=saved?(ushort)1:unchecked((ushort)(Dice.RollDice(1,4)+1));
            AddEffectWithMessage(c,effectId,minutes,255,false,false,saved,saved?"starts to cough":"chokes and gags from nausea");
            // All five failed HasEffect calls leave effect nil in the Pascal implementation.
            CallEffectHandler(effectId,c,null,false);
        }
        public static bool AttackRollNoAttacker(TCharRecord defender,byte sixtyMinusThac0)
        {
            GDVars.gAttackRoll=S(Dice.RollDice(1,20));if(GDVars.gAttackRoll<=1)return false;
            if(GDVars.gAttackRoll==20)GDVars.gAttackRoll=100;
            TriggerEffectsWithCondition(C_Cond.ATTACK_ROLL_DEF,defender);
            return GDVars.gAttackRoll>=0&&GDVars.gAttackRoll+sixtyMinusThac0>defender.acCur;
        }
        public static bool AttackRoll(TCharRecord attacker,TCharRecord defender,byte sixtyMinusAc)
        {
            RemoveInvisibility(attacker);GDVars.gAttackRoll=S(Dice.RollDice(1,20));if(GDVars.gAttackRoll<=1)return false;
            if(GDVars.gAttackRoll==20)GDVars.gAttackRoll=100;
            TriggerEffectsWithCondition(C_Cond.ATTACK_ROLL_ATT,attacker);TriggerEffectsWithCondition(C_Cond.ATTACK_ROLL_DEF,defender);
            sbyte zmod=S(GDVars.zone1[attacker.hostile==0?C_Gbvm.PARTY_HIT_MOD:C_Gbvm.MONSTER_HIT_MOD]&255);
            return GDVars.gAttackRoll>=0&&GDVars.gAttackRoll+attacker.thac0Cur+zmod>=sixtyMinusAc;
        }
        public static bool RollSavingThrow(TCharRecord c,sbyte saveType,sbyte modifier)
        {
            if(saveType<C_Save.PARALYZE||saveType>C_Save.SPELL)saveType=C_Save.SPELL;
            GDVars.gSaveRoll=Dice.RollDice(1,20);if(GDVars.gSaveRoll==1)return false;if(GDVars.gSaveRoll==20)return true;
            GDVars.gSaveRoll=unchecked((short)(GDVars.gSaveRoll+c.saveBonus+modifier));GDVars.gSaveType=B(saveType);
            TriggerEffectsWithCondition(C_Cond.SAVING_THROW,c);return c.saves[saveType]<=GDVars.gSaveRoll;
        }
        public static void AddEffect(TCharRecord c,byte effId,ushort minutes,byte strength,bool onRemove)
        {
            if(effId==C_Effect.POISONED)Stats.Count(c,Stats.POISONED,1);
            var node=new TEffectRec{id=effId,duration=minutes,strength=strength,onRemove=onRemove};
            if(c.effectsAddr==null)c.effectsAddr=node;else{var tail=c.effectsAddr;while(tail.next!=null)tail=tail.next;tail.next=node;}
        }
        public static void PutOutOfPlayWithStatus(TCharRecord c,byte status,string msg)
        {
            if(c.enabled==0)return;byte slot=CbtMap.CombatSlot(c);
            CbtMap.DisplayMapViewCentered(c,3,false);PrintName(c,ShortString(msg,40),10,true);c.enabled=0;c.status=status;
            if(c.status!=C_Status.RUNNING)c.hpCur=0;
            if(slot>0){CbtMap.UpdateCombatMapTile(0,0,slot);Flush();GDVars.arenaLoc[slot].size=0;}
            CbtMap.BuildArenaArrays();if(CharHlp.EndActions(c))RemoveEffectsHp0(c);
        }
        public static void RemoveInvisibility(TCharRecord c){TEffectRec e;while(CharHlp.HasEffect(c,C_Effect.INVISIBLE,out e))RemoveEffect(c,C_Effect.INVISIBLE,e);}
        public static void RemoveEffectsHp0(TCharRecord c){for(int i=1;i<=16;i++)RemoveEffect(c,GDConsts.hpZeroEffects[i],null);}
        public static void RemoveAiEffects(TCharRecord c){for(int i=1;i<=2;i++)RemoveEffect(c,GDConsts.aiRoundRemoveEffects[i],null);}
        public static bool RemoveEffectWithMessage(TCharRecord c,byte effId)
        {
            TEffectRec e;if(!CharHlp.HasEffect(c,effId,out e))return false;PrintName(c,"is Cured",10,true);RemoveEffect(c,effId,e);return true;
        }
        public static byte StrengthModifier(byte baseStr,byte excStr){return B(baseStr==18?excStr+1:baseStr+100);}
        public static void StrengthEffectToStrength(TEffectRec effect,ref byte baseStr,ref byte excStr)
        {
            int value=effect.strength&127;excStr=0;if(value<=101){excStr=B(value-1);baseStr=18;}else baseStr=B(value-100);
        }
        public static bool AdjustForNewStrength(TCharRecord c,byte baseStr,byte excStr,ref byte effStrength)
        {
            byte oldBase=c.strCur,oldExc=c.strExc;
            if(baseStr<oldBase||(baseStr==18&&excStr<oldExc)){effStrength=B(StrengthModifier(baseStr,excStr)+128);return false;}
            TEffectRec found=null;
            for(var p=c.effectsAddr;p!=null&&found==null;p=p.next)if((p.id==C_Effect.ENLARGE||p.id==C_Effect.STRENGTH)&&p.strength<128)found=p;
            if(found!=null){byte enc=StrengthModifier(oldBase,oldExc);StrengthEffectToStrength(found,ref oldBase,ref oldExc);found.strength=B(enc+128);}
            c.strCur=AbilAdj.ClampScore(baseStr);c.strExc=excStr;RecalcStats(c);effStrength=StrengthModifier(oldBase,oldExc);return true;
        }
        internal static void NonWeaponDamage(TCharRecord c,byte damage,byte saveReduction,bool saved,TItemRec shockItem)
        {
            if(c.status==C_Status.DEAD||c.status==C_Status.STONED||c.status==C_Status.GONE)return;
            GDVars.pendingDamage=damage;TriggerEffectsWithCondition(C_Cond.NONWEAPON_DAMAGE,c);
            if(saved){if(saveReduction==1)GDVars.pendingDamage=0;else if(saveReduction==2)GDVars.pendingDamage=B(GDVars.pendingDamage/2);}
            if(GDVars.pendingDamage==0)return;
            string text=GDVars.pendingDamage==1?"takes 1 point of damage ":"takes "+StrUtil.NumStr(GDVars.pendingDamage)+" points of damage ";
            switch(GDVars.elementMask&~C_Effect.ELEM_MAGIC){case C_Effect.ELEM_FIRE:text+="from Fire";break;case C_Effect.ELEM_COLD:text+="from Cold";break;case C_Effect.ELEM_ELEC:text+="from Electricity";break;case C_Effect.ELEM_ACID:text+="from Acid";break;}
            if((GDVars.elementMask&C_Effect.ELEM_MAGIC)==GDVars.elementMask)text+="from Magic";
            text=ShortString(text,43);
            if(shockItem==null)AnimateName(c,false,text);
            else{ItemLabel(shockItem,false);Message(shockItem.name,10,true);Message("shocks ",15,false);Message(c.name,10,false);Message(" for "+StrUtil.NumStr(GDVars.pendingDamage)+" damage, and has been unequipped.",15,false);MessageWait();}
            InflictDamage(c,GDVars.pendingDamage);
            if(GDVars.ifcState==C_Ifc.COMBAT){c.combatAddr.castAllowed=0;if(c.combatAddr.spellCast>0){PrintName(c,"lost a spell",12,true);CharHlp.RemoveSpell(c,c.combatAddr.spellCast);c.combatAddr.spellCast=0;}}
            if(c.enabled==0)
            {
                text="Goes Down";if(c.status==C_Status.DYING)text+=", and is Dying";if(c.status==C_Status.DEAD||c.status==C_Status.STONED||c.status==C_Status.GONE)text="is killed";
                PrintName(c,text,B(GDVars.curRow+1),false);
                if(GDVars.ifcState!=C_Ifc.COMBAT)Pause();
                else{RemoveEffectsHp0(c);TriggerEffectsWithCondition(C_Cond.HP_ZERO,c);if(c.enabled==0)CbtMap.CreatureDowned(c);else Pause();}
            }
            ClearMessage();
        }
        public static void InflictNonWeaponDamage(TCharRecord c,byte damage,byte saveReduction,bool saved){NonWeaponDamage(c,damage,saveReduction,saved,null);}
        public static void InflictItemShock(TCharRecord c,TItemRec item,byte damage){NonWeaponDamage(c,damage,0,false,item);}
        public static bool DiseaseImmune(TCharRecord c,byte effId)
        {
            return c!=null&&c.classLevels[C_Level.PALADIN]>0&&(effId==C_Effect.DISEASED||effId==C_Effect.DISEASE_STR||effId==C_Effect.DISEASE_HP||effId==C_Effect.ROT_NO_HEAL||effId==C_Effect.MUMMY_ROT);
        }
        public static void AddEffectWithMessage(TCharRecord c,byte effId,ushort minutes,byte strength,bool onRemove,bool saveNegates,bool saved,string msg)
        {AddEffectWithMessage(c,effId,minutes,strength,onRemove,(byte)(saveNegates?1:0),saved,msg);}
        // Magic deliberately passes the raw Pascal Boolean byte (0..3); only exactly 1 is True here.
        public static void AddEffectWithMessage(TCharRecord c,byte effId,ushort minutes,byte strength,bool onRemove,byte saveNegates,bool saved,string msg)
        {
            msg=ShortString(msg,40);GDVars.pendingEffectId=effId;TriggerEffectsWithCondition(C_Cond.EFFECT_IMMUNITY,c);
            if(DiseaseImmune(c,effId))GDVars.pendingEffectId=0;
            if(GDVars.pendingEffectId==0||(saved&&saveNegates==1)){PrintName(c,"is Unaffected",10,true);return;}
            TEffectRec effect;if(CharHlp.HasEffect(c,effId,out effect)&&((effect.duration>0&&minutes>effect.duration)||minutes==0))RemoveEffect(c,effId,effect);
            AddEffect(c,effId,minutes,strength,onRemove);
            if(msg.Length>0){AnimateName(c,true,msg);ClearMessage();}
        }
        public static bool RestoreHitPoints(TCharRecord c,byte hp,bool bypassRot)
        {
            if(c.status!=C_Status.OKAY&&c.status!=C_Status.ANIMATED&&c.status!=C_Status.UNCONSCIOUS&&c.status!=C_Status.DYING)return false;
            TEffectRec e;if(c.hpCur>=c.hpMax||!bypassRot){if(CharHlp.HasEffect(c,C_Effect.ROT_NO_HEAL,out e))return false;if(bypassRot)return false;}
            byte before=c.hpCur;c.hpCur=B(Math.Min(c.hpMax,c.hpCur+hp));Stats.Healed(GDVars.curChar,c,B(c.hpCur-before),false);
            if(c.enabled==0){if(c.status==C_Status.DYING)c.status=C_Status.UNCONSCIOUS;if(c.status==C_Status.UNCONSCIOUS&&GDVars.ifcState!=C_Ifc.COMBAT)CallEffectHandler(C_Effect.HEAL_WAKE,c,null,true);}
            return true;
        }
        public static bool ReviveDeadCreature(TCharRecord c,byte hp)
        {
            if(!CbtMap.IsValidCreatureLoc(c,unchecked((byte)CbtMap.CombatCellX(c)),unchecked((byte)CbtMap.CombatCellY(c)),true))return false;
            c.status=C_Status.OKAY;c.enabled=1;c.hpCur=hp;if(GDVars.ifcState==C_Ifc.COMBAT)CbtMap.DisplayMapViewCentered(c,3,false);
            PrintName(c,c.hostile==1?"stands up and grins":"gets back up",10,true);CharHlp.CountTeams();return true;
        }
        public static bool HasDisease(TCharRecord c){bool found=false;TEffectRec e;for(int i=1;i<GDConsts.cureDiseaseEffects.Length;i++)if(CharHlp.HasEffect(c,GDConsts.cureDiseaseEffects[i],out e))found=true;return found;}
        public static void RemoveDiseases(TCharRecord c){GDVars.effectLock=1;for(int i=1;i<GDConsts.cureDiseaseEffects.Length;i++)RemoveEffect(c,GDConsts.cureDiseaseEffects[i],null);GDVars.effectLock=0;}
        public static void ToggleReadyItem(TItemRec item)
        {
            bool special=item.special>127;var c=GDVars.curChar;
            if(item.equipped!=0){if(item.cursed!=0)BottomLine("It's Cursed");else{item.equipped=0;if(special)CallEffectHandler(item.special,c,item,true);}return;}
            byte err=0;if(c.handsEquipped+CharHlp.BaseItem(item.itemType).hands>2)err=3;
            sbyte slot=unchecked((sbyte)CharHlp.BaseItem(item.itemType).slot);
            if(unchecked((byte)slot)>=C_Equip.WEAPON&&unchecked((byte)slot)<=C_Equip.BOOTS){if(c.equipped[slot]!=null)err=2;}
            else if(unchecked((byte)slot)==C_Equip.RING1){if(c.equipped[C_Equip.RING2]!=null)err=2;}
            if(item.itemType==C_Item.ARROW&&c.equipped[C_Equip.ARROW]!=null){err=2;slot=C_Equip.ARROW;}
            if(item.itemType==C_Item.QUARREL&&c.equipped[C_Equip.BOLT]!=null){err=2;slot=C_Equip.BOLT;}
            if((CharHlp.BaseItem(item.itemType).classMask&c.itemLimits)==0)err=1;
            switch(err){case 0:item.equipped=1;if(special)CallEffectHandler(item.special,c,item,false);break;case 1:BottomLine("Wrong Class");break;case 2:ItemLabel(c.equipped[slot],false);BottomLine("already using "+c.equipped[slot].name);break;case 3:if(GDVars.ifcState!=C_Ifc.COMBAT||c.quickfight==0)BottomLine("Your hands are full!");break;}
        }
        internal static void TimeOutEffects(byte count,sbyte tUnit)
        {
            const int maxExpiring=32;
            // Only source entries 1..ARENA_SLOTS; managed index zero is padding.
            int flagCount=GDVars.effFlags.Length-1;
            if(GDVars.ifcState!=C_Ifc.CAMPING){for(int k=1;k<=flagCount;k++)GDVars.effFlags[k]=1;}
            else
            {
                bool found=false;int i=1;
                do{if(GDVars.effFlags[i]!=0)found=true;i++;}while(!found&&i<=GDVars.zone1[C_Gbvm.PARTY_SIZE]);
                if(!found)return;
            }
            int total=count;int u=tUnit;if(u==C_Time.SEGMENTS)total/=GDConsts.unitScale[C_Time.SEGMENTS];
            while(u>1){total=unchecked(total*GDConsts.unitScale[u-1]);u--;}
            while(total>0)
            {
                int step=Math.Min(10,total),i=1;
                for(var c=GDVars.creatureList;c!=null;c=c.nextCharAddr,i++)
                {
                    if(i<=flagCount&&GDVars.effFlags[i]==0)continue;
                    bool again=false;var expiring=new TEffectRec[maxExpiring];var ids=new byte[maxExpiring];var durations=new ushort[maxExpiring];int n=0;
                    for(var e=c.effectsAddr;e!=null;e=e.next)if(e.duration>0){if(step<e.duration){e.duration=unchecked((ushort)(e.duration-step));again=true;}else if(n<maxExpiring){expiring[n]=e;ids[n]=e.id;durations[n]=e.duration;n++;}else again=true;}
                    for(int k=0;k<n;k++)if(InList(c,expiring[k])&&expiring[k].id==ids[k]&&expiring[k].duration==durations[k])RemoveEffect(c,expiring[k].id,expiring[k]);
                    for(var e=c.effectsAddr;e!=null&&!again;e=e.next)if(e.duration>0)again=true;
                    if(i<=flagCount)GDVars.effFlags[i]=(byte)(again?1:0);
                }
                total=total>10?total-10:0;
            }
        }
        public static void AdvanceTime(byte amount,sbyte tUnit)
        {
            var time=new TClock();for(int i=0;i<=6;i++)time[i]=GDVars.zone0.words[C_Gbvm.CLOCK+i];
            for(int j=1;j<=amount;j++){time[tUnit]=unchecked((ushort)(time[tUnit]+1));Clock.ValidateClock(time,true);}
            for(int i=0;i<=6;i++)GDVars.zone0.words[C_Gbvm.CLOCK+i]=time[i];TimeOutEffects(amount,tUnit);
        }
        public static TCharRecord HoldTarget(TCharRecord c,object p,bool removed,byte effId,ref bool released)
        {
            var e=(TEffectRec)p;released=false;
            if(e.strength==0||e.strength>GDVars.arenaCount||e.strength>ARENA_SLOTS){if(!removed){e.onRemove=false;RemoveEffect(c,effId,e);}return null;}
            GDVars.dsOtherCreatureCurrentPtr=GDVars.creatureArr[e.strength];
            if(removed||c.enabled==0||GDVars.dsOtherCreatureCurrentPtr.enabled==0){RemoveEffect(GDVars.dsOtherCreatureCurrentPtr,C_Effect.IMMOBILE,null);if(!removed){e.onRemove=false;RemoveEffect(c,effId,e);}released=true;return null;}
            return GDVars.dsOtherCreatureCurrentPtr;
        }
    }
}
