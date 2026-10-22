// Direct conversion of pinned src/charedit.pas.
using System;
using System.Globalization;
using static Por.Core.Types;
namespace Por.Core
{
    public interface ICharEditServices{void DisplayChar(TCharRecord c);void ModifyCharacter(bool hp);void GainLevels(byte classes,bool training);}
    public static class CharEdit
    {
        public static ICharEditServices Services;
        private static ICharEditServices S{get{if(Services==null)throw new NotSupportedException("CharEdit: CharGen/ViewChar services missing.");return Services;}}
        private static readonly byte[] Kinds={0,71,84,71,71,0,69,78,79,78,63,71,70,92,21,78,69,69,70,67};
        internal static int HeldClasses(sbyte[] held){int n=0;for(sbyte i=C_Level.CLERIC;i<=C_Level.MONK;i++)if(GDVars.curChar.classLevels[i]>0)held[n++]=i;return n;}
        internal static void LowerLevel(sbyte cls,byte count)
        {
            var c=GDVars.curChar;if(c.classLevels[cls]<=1)return;
            short avg=(short)(GDConsts.hpSides[cls]/2+1);int loss=(avg+ClassCal.ConHpPerDie(cls,c.stats[C_Abil.CONSTITUTION]))/count;if(loss<1)loss=1;
            c.classLevels[cls]--;int damage=c.hpMax-c.hpCur;int hp=Math.Max(1,Math.Max(c.hpMax-loss,ClassCal.HpMinimum(c)));c.hpMax=unchecked((byte)hp);c.hpCur=unchecked((byte)Math.Max(0,hp-damage));
            if(c.hpRolled>avg/count)c.hpRolled=unchecked((byte)(c.hpRolled-avg/count));
            if(c.classLevels[cls]==1){if(cls==C_Level.CLERIC){Array.Clear(c.clericSpells,0,c.clericSpells.Length);c.clericSpells[1]=1;ClassCal.AwardWisdomBonus(c);}else if(cls==C_Level.MAGE){Array.Clear(c.mageSpells,0,c.mageSpells.Length);c.mageSpells[1]=1;}}
            c.levelHighest=0;ClassCal.RecalcBaseSkills(c);
        }
        internal static int LevelXp(sbyte cls,byte level){return level<2?0:GDConsts.dsExperienceSpellsClass[cls].xp[level];}
        internal static void DrawLevels(byte sel)
        {
            sbyte[] held=new sbyte[8];int count=HeldClasses(held);Border.ClearBox(1,1,38,22);CharDisp.DisplayCreatureName(GDVars.curChar,3,2,false);
            for(int i=0;i<count;i++){TextRend.DisplayString(3,(byte)(4+i),(byte)(i==sel?13:10),GDConsts.classNames[held[i]]);TextRend.DisplayString(16,(byte)(4+i),15,"level "+StrUtil.NumStr(GDVars.curChar.classLevels[held[i]])+" ");}
            TextRend.DisplayString(3,(byte)(5+count),10,"Experience");TextRend.DisplayString(16,(byte)(5+count),15,StrUtil.NumStr(GDVars.curChar.experience)+"   ");TextRend.DisplayString(3,(byte)(6+count),10,"Hit points");TextRend.DisplayString(16,(byte)(6+count),15,StrUtil.NumStr(GDVars.curChar.hpCur)+"/"+StrUtil.NumStr(GDVars.curChar.hpMax)+"   ");
        }
        internal static void LevelsTool()
        {
            sbyte[] held=new sbyte[8];int count=HeldClasses(held);if(count==0)return;byte sel=0,key;bool ext=false;
            do
            {
                DrawLevels(sel);MenuCore.arrowsToCaller=true;key=MenuCore.MenuHorizontal("Levels: ","Experience Done",13,10,15,true,false,ref ext);sbyte cls=held[sel];var c=GDVars.curChar;
                if(ext)switch(key)
                {
                    case C_Keys.DOWN:sel=(byte)((sel+1)%count);break;case C_Keys.UP:sel=(byte)((sel+count-1)%count);break;
                    case C_Keys.LEFT:if(c.classLevels[cls]>1){LowerLevel(cls,(byte)count);int upper=LevelXp(cls,(byte)(c.classLevels[cls]+1));if(c.experience>=upper)c.experience=upper-1;}break;
                    case C_Keys.RIGHT:if(c.classLevels[cls]<C_Level.LEVEL_LAST){S.GainLevels(GDConsts.classBit[cls],false);int xp=LevelXp(cls,unchecked((byte)c.classLevels[cls]));if(c.experience<xp)c.experience=xp;}break;
                }
                else if(key=='E'){int xp;if(Parse(TextRend.ReadString("Experience: ",10,7),out xp)&&xp>=0)c.experience=xp;}
            }while(ext||(key!='D'&&key!=0));
        }
        internal static bool Parse(string s,out int n){return s.StartsWith("$",StringComparison.Ordinal) ? int.TryParse(s.Substring(1),NumberStyles.AllowHexSpecifier,CultureInfo.InvariantCulture,out n) : int.TryParse(s,NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out n);}
        internal static void HealthAction(byte key)
        {
            var c=GDVars.curChar;if(key=='H'){c.hpCur=c.hpMax;if(c.status==C_Status.UNCONSCIOUS||c.status==C_Status.DYING)c.status=C_Status.OKAY;}
            if(key=='S'){c.status=(byte)(c.status==C_Status.OKAY?C_Status.UNCONSCIOUS:c.status==C_Status.UNCONSCIOUS?C_Status.DYING:c.status==C_Status.DYING?C_Status.DEAD:c.status==C_Status.DEAD?C_Status.STONED:c.status==C_Status.STONED?C_Status.GONE:C_Status.OKAY);if(c.status==C_Status.OKAY){if(c.hpCur==0)c.hpCur=1;}else c.hpCur=0;}
        }
        internal static void HealthTool(){byte key;bool ext=false;do{S.DisplayChar(GDVars.curChar);key=MenuCore.MenuHorizontal("Health: ","Heal Status Exit",13,10,15,false,false,ref ext);HealthAction(key);}while(key!='E'&&key!=0);}
        private static void AddLine(ref TItemRec head,ref TItemRec tail,ushort[] codes,ref byte n,string name,ushort code){if(n>100)return;MenuCore.AppendMenuLine(ref head,ref tail,name,code==0);codes[n]=code;n++;}
        internal static string Named(TItemRec item){CharHlp.ItemLabel(item,false);item.name=item.name.TrimEnd(' ');return item.name;}
        internal static void Make(ushort code,TItemRec item){if(code>256){RandItem.GenerateRandomItem(Kinds[code-256],item);RandItem.ApplyTemplate(item,(byte)(code-256));}else RandItem.GenerateRandomItem(unchecked((byte)code),item);}
        internal static bool ChoosePlus(TItemRec item)
        {
            byte pos=0;for(byte i=1;i<=3;i++)if(item.nameIds[i]>C_Item.NAME_PLUS_BASE&&item.nameIds[i]<=C_Item.NAME_PLUS_BASE+5)pos=i;
            ushort per=item.plus>0?(ushort)(item.value/(ushort)item.plus):(ushort)0;if(pos!=0){item.plus=1;item.nameIds[pos]=C_Item.NAME_PLUS_BASE+1;item.value=per;}
            byte key;bool ext=false;
            do{Border.ClearBox(1,1,38,22);TextRend.DisplayString(3,4,15,Named(item));if(pos!=0)TextRend.DisplayString(3,6,10,"Left and Right change its plus.");MenuCore.arrowsToCaller=true;key=MenuCore.MenuHorizontal("Add: ","Take Exit",13,10,15,true,false,ref ext);
                if(ext&&pos!=0){if(key==C_Keys.LEFT&&item.plus>0)item.plus--;if(key==C_Keys.RIGHT&&item.plus<5)item.plus++;item.nameIds[pos]=(byte)(item.plus==0?0:C_Item.NAME_PLUS_BASE+item.plus);item.value=unchecked((ushort)(per*(ushort)item.plus));}
            }while(ext||(key!='T'&&key!='E'&&key!=0));return key=='T';
        }
        internal static void AddItem()
        {
            TItemRec item=new TItemRec(),head=null,tail=null,sel=null;ushort[] codes=new ushort[101];byte n=0;
            AddLine(ref head,ref tail,codes,ref n,"Weapons and armor",0);
            for(byte k=1;k<=93;k++)if(k<=59||k==73||k==77||k==93){RandItem.GenerateRandomItem(k,item);if(Named(item)!="")AddLine(ref head,ref tail,codes,ref n,Named(item),k);}
            AddLine(ref head,ref tail,codes,ref n,"Magic items",0);for(byte k=1;k<=19;k++)if(Kinds[k]!=0){Make((ushort)(256+k),item);item.unidMask=0;AddLine(ref head,ref tail,codes,ref n,Named(item),(ushort)(256+k));}
            for(byte k=61;k<=62;k++){RandItem.GenerateRandomItem(k,item);AddLine(ref head,ref tail,codes,ref n,Named(item),k);}
            short idx=0;bool redraw=true;Border.ClearBox(1,1,38,22);byte key=MenuCore.MenuVertical("Add: ","Take",13,10,15,1,1,38,22,head,true,ref redraw,ref idx,ref sel);ushort code=0;
            if(key!=0&&sel!=null){int k=0;var p=head;while(p!=null&&p!=sel){p=p.next;k++;}if(p!=null)code=codes[k];}MenuCore.FreeStringList(ref head);
            if(code!=0){Make(code,item);item.unidMask=0;if(code>256||code==61||code==62||ChoosePlus(item)){bool failed=false;Shop.GiveItemCopyToCreature(item,ref failed);if(failed)TextRend.PauseOnly();}}
        }
        internal static void MoneyTool()
        {
            short idx=0;int m;
            do{TItemRec head=null,tail=null,sel=null;for(int i=0;i<=6;i++)MenuCore.AppendMenuLine(ref head,ref tail,StrUtil.PadLeft(GDConsts.moneyNames[i],8)+" "+StrUtil.NumStr(GDVars.curChar.coins[i]),false);
                bool redraw=true;byte key=MenuCore.MenuVertical("Money: ","Set",13,10,15,12,7,27,13,head,true,ref redraw,ref idx,ref sel);m=-1;
                if(key!=0&&sel!=null){m=0;var p=head;while(p!=null&&p!=sel){p=p.next;m++;}}MenuCore.FreeStringList(ref head);
                if(m>=0){int count;if(Parse(TextRend.ReadString(GDConsts.moneyNames[m]+": ",10,5),out count)&&count>=0&&count<=65535)GDVars.curChar.coins[m]=(ushort)count;CharHlp.RecalcStats(GDVars.curChar);}
            }while(m>=0);
        }
        internal static void GearTool(){byte key;bool ext=false;do{S.DisplayChar(GDVars.curChar);key=MenuCore.MenuHorizontal("Gear: ","Add Money Exit",13,10,15,false,false,ref ext);if(key=='A')AddItem();if(key=='M')MoneyTool();}while(key!='E'&&key!=0);}
        internal static void SpellList(byte cls)
        {
            short idx=0;int k;
            do{TItemRec head=null,tail=null,sel=null;byte[] spells=new byte[57];int n=0;for(byte m=1;m<=56;m++){var sp=GDConsts.spellTable[m];if(sp.castClass==cls&&sp.levelSp>=1&&sp.levelSp<=CASTER_LEVEL_LAST){MenuCore.AppendMenuLine(ref head,ref tail,(GDVars.curChar.spellsKnown[m]!=0?"* ":"  ")+StrUtil.NumStr(sp.levelSp)+" "+GDConsts.spellNames[m],false);spells[++n]=m;}}
                bool redraw=true;Border.ClearBox(1,1,38,22);byte key=MenuCore.MenuVertical("Known: ","Toggle",13,10,15,1,1,38,22,head,true,ref redraw,ref idx,ref sel);k=0;
                if(key!=0&&sel!=null){var p=head;k=1;while(p!=null&&p!=sel){p=p.next;k++;}if(p==null)k=0;}MenuCore.FreeStringList(ref head);if(k!=0)GDVars.curChar.spellsKnown[spells[k]]=(byte)(GDVars.curChar.spellsKnown[spells[k]]==0?1:0);
            }while(k!=0);
        }
        internal static void MagicTool(){byte key;bool ext=false;do{S.DisplayChar(GDVars.curChar);key=MenuCore.MenuHorizontal("Spells known: ","Mage Cleric Exit",13,10,15,false,false,ref ext);if(key=='M')SpellList(1);if(key=='C')SpellList(0);}while(key!='E'&&key!=0);}
        public static void EditMenu()
        {
            if(GDVars.curChar==null)return;byte key;bool ext=false;
            do{S.DisplayChar(GDVars.curChar);key=MenuCore.MenuHorizontal("","Abilities Levels Gear Magic Health Exit",13,10,15,false,false,ref ext);
                if(ext){if(key==C_Keys.HOME||key==C_Keys.END_KEY||key==C_Keys.UP||key==C_Keys.DOWN)Party.SelectPrevNextChar((char)key);}
                else switch(key){case (byte)'A':S.ModifyCharacter(true);break;case (byte)'L':LevelsTool();break;case (byte)'G':GearTool();break;case (byte)'M':MagicTool();break;case (byte)'H':HealthTool();break;}
            }while(ext||(key!='E'&&key!=0));
        }
    }
}
