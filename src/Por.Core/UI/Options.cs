// Direct conversion of pinned src/options.pas. Native file replacement is atomic rather than erase-then-rename.
using System;
using System.IO;
using System.Text;
using System.Globalization;
namespace Por.Core
{
    public interface IOptionsInfo
    {
        int AvailableBytes{get;}int LargestBlock{get;}string SecondArgument{get;}string BuildCommit{get;}
    }
    public static class Options
    {
        public static IOptionsInfo Info;
        internal static byte speedChoice;
        private static readonly string[] Keys={"","SPEED","MONSTERS","PORTRAITS","MUSIC","SOUND","AUTOENDQUICK","QUICKMAGIC","COMPANION"};
        private static readonly string[] Labels={"","","Monster Pictures","Character Portraits","Music","Sound","Auto End Quick Mode","Quick Mode Magic","Companion Tools"};
        private static Encoding Cp437{get{Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);return Encoding.GetEncoding(437);}}
        private static bool Io(Exception e){return e is IOException||e is UnauthorizedAccessException||e is System.Security.SecurityException;}
        internal static string Short(string s){return s.Length<=255?s:s.Substring(0,255);}
        internal static string Tidy(string s){char[] a=Short(s).Trim(' ','\t').ToCharArray();for(int i=0;i<a.Length;i++)if(a[i]>='a'&&a[i]<='z')a[i]=(char)(a[i]-32);return new string(a);}
        internal static byte KeyOf(string line){int eq=line.IndexOf('=');if(eq<0)return 0;string key=Tidy(line.Substring(0,eq));for(byte i=1;i<=8;i++)if(key==Keys[i])return i;return 0;}
        private static byte Flag(byte opt){switch(opt){case 2:return GDVars.monsterPics;case 3:return GDVars.portraitsOn;case 4:return GDVars.musicOn;case 5:return GDVars.soundOn;case 6:return GDVars.quickEnds;case 7:return GDVars.quickMagicOn;case 8:return GDVars.companionOn;default:return 0;}}
        private static void SetFlag(byte opt,byte flag){switch(opt){case 2:GDVars.monsterPics=flag;break;case 3:GDVars.portraitsOn=flag;break;case 4:GDVars.musicOn=flag;break;case 5:GDVars.soundOn=flag;break;case 6:GDVars.quickEnds=flag;break;case 7:GDVars.quickMagicOn=flag;break;case 8:GDVars.companionOn=flag;break;}}
        internal static string StoredValue(byte opt){if(opt<1||opt>8)return "";return opt==1?(GDVars.gameDelay%10).ToString(CultureInfo.InvariantCulture):Flag(opt)!=0?"ON":"OFF";}
        internal static string ShownValue(byte opt){return opt==5?(GDVars.soundMode!=C_Sound.MODE_DISABLED?"ON":"OFF"):StoredValue(opt);}
        internal static string SettingLabel(byte opt){return opt==1?"Game Speed ("+(GDVars.gameDelay%10)+")":opt<=8?Labels[opt]:"";}
        internal static void ReadValue(byte opt,string v){if(opt==1){if(v.Length==1&&v[0]>='0'&&v[0]<='9')GDVars.gameDelay=(byte)(v[0]-'0');return;}if(v=="ON"||v=="1")SetFlag(opt,1);else if(v=="OFF"||v=="0")SetFlag(opt,0);}
        public static void LoadOptions()
        {
            try{using(var f=new StreamReader(GDVars.dsSaveDirectory+"POOLOPT.CFG",Cp437,false)){for(int i=0;i<100;i++){string s=f.ReadLine();if(s==null)break;s=Short(s);byte opt=KeyOf(s);if(opt!=0)ReadValue(opt,Tidy(s.Substring(s.IndexOf('=')+1)));}}}catch(Exception e)when(Io(e)){}
            GameIo.SetSound(GDVars.soundOn!=0);GameIo.saveSettings=SaveOptions;
        }
        public static void SaveOptions()
        {
            string target=GDVars.dsSaveDirectory+"POOLOPT.CFG",temp=GDVars.dsSaveDirectory+"POOLOPT.$$$";
            try
            {
                using(var f=new StreamWriter(temp,false,Cp437))
                {
                    f.NewLine="\r\n";for(byte i=1;i<=8;i++)f.WriteLine(Keys[i]+"="+StoredValue(i));
                    StreamReader old=null;try{old=new StreamReader(target,Cp437,false);}catch(Exception e)when(Io(e)){}
                    if(old!=null)using(old){for(int i=0;i<100;i++){string line=old.ReadLine();if(line==null)break;line=Short(line);if(KeyOf(line)==0&&Tidy(line)!="")f.WriteLine(line);}}
                }
                File.Move(temp,target,true);
            }
            catch(Exception e)when(Io(e)){try{File.Delete(temp);}catch(Exception cleanup)when(Io(cleanup)){} }
        }
        internal static string Grouped(int value)
        {
            string s=value.ToString(CultureInfo.InvariantCulture);for(int i=s.Length-3;i>0;i-=3)s=s.Insert(i,",");return s;
        }
        internal static string NoteOnRow(byte row){int n=0;string[] notes=new string[2];if(GDVars.stingArg)notes[n++]="STING ";if(Info==null)throw new NotSupportedException("Options: build/memory info host missing.");if(Info.SecondArgument=="NORAND")notes[n++]="NORAND ";return row<=9&&9-row<n?notes[9-row]:"";}
        internal static void DrawSetting(byte opt,byte color)
        {
            if(opt<1||opt>8)return;byte row=(byte)(opt+1);TextRend.PrintSpaces(2,row,0,37);TextRend.DisplayString(2,row,color,SettingLabel(opt));
            if(opt==1){byte slow=color,fast=color;if(color==15){if(speedChoice==1)slow=10;else fast=10;}TextRend.DisplayString(24,row,slow,"Slower");TextRend.DisplayString(31,row,fast,"Faster");}else TextRend.DisplayString(24,row,color,ShownValue(opt));
            string note=NoteOnRow(row);TextRend.DisplayString((byte)(39-note.Length),row,14,note);
        }
        internal static void DrawInfo(byte row,string label,string value)
        {
            value=Short(value).Replace('\\','/');if(value.Length>23)value="..."+value.Substring(value.Length-20);
            TextRend.DisplayString(2,row,3,label.Substring(0,Math.Min(13,label.Length)));TextRend.PrintSpaces(16,row,0,23);TextRend.DisplayString(16,row,3,value);
        }
        internal static string DataLine()
        {
            int merged=0,present=0;for(byte f=1;f<=C_Data.FAMILIES;f++)if(DaxIo.FamilyUsable(f)){merged++;present++;}else{bool found=false;for(byte disk=1;disk<=8&&!found;disk++)found=DataArc.FileLength(GDVars.gameDir+DataArc.SourceName(f,disk)+".DAX")>=0;if(found)present++;}
            return "V"+Grouped(C_Data.VERSION)+"  Merged "+Grouped(merged)+"/"+Grouped(present);
        }
        internal static void DrawInfoLines()
        {
            if(Info==null)throw new NotSupportedException("Options: build/memory info host missing.");
            DrawInfo(12,"Video",VidOps.ModeName());DrawInfo(13,"Sound Device",GDVars.sound.name);DrawInfo(14,"Data Path",GDVars.gameDir);DrawInfo(15,"Save Path",GDVars.dsSaveDirectory);
            DrawInfo(17,"Version",GDConsts.GAME_VERSION);DrawInfo(18,"Commit",Info.BuildCommit);DrawInfo(19,"Data",DataLine());DrawInfo(20,"Free Memory",Grouped(Info.AvailableBytes));DrawInfo(21,"Largest Block",Grouped(Info.LargestBlock));
        }
        internal static void ChangeSetting(byte opt)
        {
            if(opt==1){if(speedChoice==0){if(GDVars.gameDelay<9)GDVars.gameDelay++;}else if(GDVars.gameDelay>0)GDVars.gameDelay--;}
            else if(opt==5)GameIo.SetSound(GDVars.soundMode==C_Sound.MODE_DISABLED);
            else{SetFlag(opt,(byte)(Flag(opt)==0?1:0));if(opt==4&&GDVars.musicOn==0)SoundDrv.StopMusic();}
        }
        public static void OptionsMenu()
        {
            byte saved=GDVars.horizChoice,sel=1;speedChoice=0;bool changed=false,ext=false;byte ch;
            Border.ClearBox(0,0,39,23);Border.DrawBorder(1,1,38,9,0);Border.DrawBorder(1,11,38,22,0);for(byte i=1;i<=8;i++)DrawSetting(i,(byte)(i==sel?15:10));DrawInfoLines();
            do
            {
                byte sound=GDVars.soundMode;bool touched=false;GDVars.horizChoice=1;MenuCore.arrowsToCaller=true;MenuCore.enterToCaller=true;
                ch=MenuCore.MenuHorizontal("Settings: ","Exit",13,10,15,true,false,ref ext);
                if(ext)
                {
                    byte next=sel;
                    if(ch==C_Keys.UP||ch==C_Keys.HOME)next=(byte)(sel>1?sel-1:8);
                    if(ch==C_Keys.DOWN||ch==C_Keys.END_KEY)next=(byte)(sel<8?sel+1:1);
                    if(next!=sel){DrawSetting(sel,10);sel=next;DrawSetting(sel,15);}
                    if(ch==C_Keys.LEFT||ch==C_Keys.RIGHT){if(sel==1){speedChoice=(byte)(ch==C_Keys.LEFT?0:1);DrawSetting(sel,15);}else{ChangeSetting(sel);touched=true;}}
                }
                else if(ch==C_Keys.ENTER){ChangeSetting(sel);touched=true;}
                if(touched)changed=true;if(touched||GDVars.soundMode!=sound){if(sel!=5)DrawSetting(5,10);DrawSetting(sel,15);}
            }while(ext||(ch!=0&&ch!='E'));
            if(changed)SaveOptions();GDVars.horizChoice=saved;
        }
    }
}
