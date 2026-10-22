// Self-authored source-contract checks. No original DOS runtime equivalence is claimed.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Por.Core;
class BootHost : IGameBootHost
{
    public List<string> Log = new List<string>();
    public short Result; public int Buffer = 32000, Available = 200000, Probes;
    public short OverlayResult { get { return Result; } }
    public Func<ushort,short> OverlayReader { get; set; }
    public int OverlayBufferBytes { get { return Buffer; } }
    public int AvailableBytes { get { return Available; } }
    public string Config = "V\nP\nDATA\n\\SAVE\nF\n";
    public bool FailOpen, ErrorAfterBuffer;
    public BootHost() { OverlayReader = seg => unchecked((short)(seg+1)); }
    public void InitializeOverlays(string file) { Log.Add("overlay:"+file); }
    public void InitializeEms() { Log.Add("ems"); }
    public void SetOverlayBuffer(int bytes) { Log.Add("buffer:"+bytes); Buffer=bytes; if (ErrorAfterBuffer) Result=1; }
    public void SetOverlayRetry(int bytes) { Log.Add("retry:"+bytes); }
    public void ReserveGraphicsParagraphs(ushort p) { Log.Add("reserve:"+p); }
    public byte ProbeConfig(string f) { Log.Add("probe:"+f); return (byte)(Probes++ == 0 ? 2 : 0); }
    public TextReader OpenConfig(string f) { Log.Add("open:"+f); if(FailOpen) throw new IOException(); return new StringReader(Config); }
    public string GetDirectory(byte drive) { Log.Add("dir:"+drive); return drive==0 ? "C:\\GAMES" : ((char)('A'+drive-1))+":\\DRIVECWD"; }
    public void SetBreakChecking(bool b) { Log.Add("break:"+b); }
    public void WriteLine(string s) { Log.Add("say:"+s); }
    public void Halt(int code) { Log.Add("halt"); throw new Halted(); }
}
class TempleFx : ITempleEffects
{
    public List<string> Log = new List<string>(); public bool Diseased;
    public bool HasDisease(Types.TCharRecord c) { Log.Add("disease?"); return Diseased; }
    public void RemoveDiseases(Types.TCharRecord c) { Log.Add("diseases"); }
    public void RemoveEffect(Types.TCharRecord c, byte id, Types.TEffectRec e) { Log.Add("remove:"+id+":"+GDVars.effectLock); }
    public bool RestoreHitPoints(Types.TCharRecord c, byte hp,bool message) { Log.Add("hp:"+hp+":"+message); return false; }
    public void RemoveCurseSpell() { Log.Add("curse:"+ReferenceEquals(GDVars.dsTargetCreaturePtr[1],GDVars.curChar)); }
}
class MainServices : IFixMainServices
{
    public List<string> Log; public int Menus, Loops; public bool QuickLoadAsked { get; set; }
    public void GameInit2() { Log.Add("init"); }
    public void MainMenu() { Log.Add("menu"); if(++Menus==2) throw new Halted(); }
    public void QuickLoad() { Log.Add("load:"+QuickLoadAsked); }
    public void FreeParty() { Log.Add("free"); }
    public void ResetGameState() { Log.Add("reset"); }
}
static partial class Tests
{
    static void SoundWrappers()
    {
        SoundSetup(); SndSpk.Install();
        EqS("PC Speaker",GDVars.sound.name,"speaker name"); Eq(C_Sound.MODE_SPEAKER,GDVars.sound.mode,"speaker mode"); Yes(!GDVars.sound.singleStep,"speaker timer");
        sfx.Log.Clear(); GDVars.sound.Play(65535); EqS("spk 65535",string.Join("|",sfx.Log),"speaker tune forwarded unchanged");
        SndTdy.Install(); EqS("Tandy",GDVars.sound.name,"Tandy name"); Eq(C_Sound.MODE_TANDY,GDVars.sound.mode,"Tandy mode"); Yes(!GDVars.sound.singleStep,"Tandy timer");
        sfx.Log.Clear(); GDVars.sound.Play(0); EqS("tandy 0",string.Join("|",sfx.Log),"Tandy silence forwarded");
        // Exhaust all status-flag patterns, including ignored low bits; exact writes and 202 reads.
        foreach(int before in new[]{0,1,31,32,64,128,192,224,255})
        for(int after=0;after<256;after++)
        {
            int reads=0; SoundSetup();
            SndOpl.ReadPort=p=>{Eq(0x388,p,"status port"); return (byte)(reads++==0 ? before : after);};
            bool found=SndOpl.Detect(); Yes(found==((before&224)==0&&(after&224)==192),"OPL flag predicate");
            Eq(202,reads,"probe read count"); EqS("opl 4=96|opl 4=128|opl 2=255|opl 4=33|opl 4=96|opl 4=128",string.Join("|",sfx.Log),"probe write order");
        }
        foreach(bool found in new[]{false,true})
        {
            SoundSetup();int reads=0;SndOpl.ReadPort=p=>(byte)(reads++==0 ? 0 : found ? 192 : 0);
            GDVars.sound.singleStep=true; SndOpl.Install();
            EqS(found?"AdLib":"Speaker (no AdLib)",GDVars.sound.name,"detected driver/fallback");
            Eq(found?C_Sound.MODE_ADLIB:C_Sound.MODE_SPEAKER,GDVars.sound.mode,"detected mode"); Yes(!GDVars.sound.singleStep,"installed timer");
            Yes(sfx.Log.Contains("oplreset")==found,"reset only on detected card");
            foreach(int tune in new[]{0,1,C_Sound.MUSIC_TITLE-1,C_Sound.MUSIC_TITLE,65535})
            {
                sfx.Log.Clear();GDVars.sound.Play((ushort)tune);
                string expected=found?(tune>=C_Sound.MUSIC_TITLE?"dev "+C_Sound.MODE_ADLIB+"|tandy "+tune:"dev "+C_Sound.MODE_SPEAKER+"|spk "+tune):"spk "+tune;
                EqS(expected,string.Join("|",sfx.Log),"effect/music routing");
            }
        }
        SoundSetup();SndOpl.ReadPort=null;bool missing=false;try{SndOpl.Detect();}catch(NotSupportedException){missing=true;}Yes(missing,"required OPL port read");
        SoundDrv.Host=null;SndSpk.Install();missing=false;try{GDVars.sound.Play(1);}catch(NotSupportedException){missing=true;}Yes(missing,"wrapper does not fake playback");
    }
    static BootHost SetupBoot()
    {
        SmSetup();SmDone();smio.Keys.Enqueue(13);var h=new BootHost();GameBoot.Host=h;
        GameBoot.OverlayPrompt=()=>h.Log.Add("prompt");
        GameBoot.InstallVideo=c=>{h.Log.Add("video:"+c);GDVars.video.ramVideoParas=(ushort)(c=='T'?1024:0);GDVars.video.biosMode=19;};
        GameBoot.InstallSound=c=>{h.Log.Add("sound:"+c);if(c=='T')SndTdy.Install();else SndSpk.Install();};
        GameBoot.ReadVideoMode=r=>{Eq(15,r.ax>>8,"BIOS mode query");r.ax=3;h.Log.Add("bios");};
        GameBoot.SetVideoMode=m=>h.Log.Add("mode:"+m);GameBoot.StartVideo=()=>h.Log.Add("start");
        GameBoot.RestoreVideoMode=()=>h.Log.Add("restore");GameBoot.SuspendSound=()=>h.Log.Add("suspend");return h;
    }
    static void BootChecks()
    {
        var h=SetupBoot();
        foreach(var pair in new[]{new[]{"","C:\\GAMES\\"},new[]{"  DATA  ","C:\\GAMES\\DATA\\"},new[]{"\\DATA","C:\\DATA\\"},new[]{"d:","D:\\DRIVECWD\\"},new[]{"E:\\X","E:\\X\\"},new[]{"C:rel","C:rel\\"}})
            EqS(pair[1],GameBoot.NormalizePath(pair[0]),"DOS normalize");
        Eq(67,GameBoot.NormalizePath("C:\\"+new string('X',63)).Length,"exact DOS path cap");
        bool halted=false;try{GameBoot.NormalizePath("C:\\"+new string('X',64));}catch(Halted){halted=true;}Yes(halted&&h.Log.Contains("restore"),"overlong restores and halts");
        foreach(char video in new[]{'E','T','V','x'}) foreach(bool ems in new[]{false,true})
        {
            h=SetupBoot();h.Result=(short)(ems?0:1);h.Config=video+"\nT\nDATA\n\\SAVE\nF\n";
            // Ems result can fail, but buffer configuration restores normal overlay status.
            GameBoot.InstallVideo=c=>{h.Result=0;h.Log.Add("video:"+c);GDVars.video.ramVideoParas=(ushort)(c=='T'?1024:0);GDVars.video.biosMode=19;};
            GDVars.ovrLoads=99;GDVars.picBytes=99;GDVars.picPeak=99;GameBoot.Initialize("STING");
            Yes(GDVars.stingArg&&GDVars.fullIntro&&GDVars.ovrInEms==ems,"boot flags");Eq(0,GDVars.picBytes,"pic bytes reset");Eq(0,GDVars.picPeak,"pic peak reset");Eq(0,GDVars.ovrLoads,"loads reset");
            EqS("C:\\GAMES\\DATA\\",GDVars.gameDir,"game path");EqS("C:\\SAVE\\",GDVars.dsSaveDirectory,"save path");Eq(C_Sound.MODE_TANDY,GDVars.soundMode,"configured sound mode");
            Eq(42,h.OverlayReader(41),"saved reader return");Eq(1,GDVars.ovrLoads,"counting overlay wrapper");
            var expected=new List<string>{"prompt","overlay:GAME.OVR","ems","probe:pool.cfg","say:Please insert startup disk.","probe:pool.cfg","break:False","open:POOL.CFG","bios","video:"+video};
            if(video=='T')expected.Add("reserve:1024");expected.AddRange(new[]{"mode:19","start","buffer:100352","retry:33450","sound:T","reserve:1024","suspend","dir:0","dir:0"});
            EqS(string.Join("|",expected),string.Join("|",h.Log),"whole boot call sequence");
        }
        foreach(string failure in new[]{"memory","overlay","open"})
        {
            h=SetupBoot();if(failure=="memory")h.Available=68351;if(failure=="overlay")h.ErrorAfterBuffer=true;if(failure=="open")h.FailOpen=true;
            halted=false;try{GameBoot.Initialize("sting");}catch(Halted){halted=true;}
            Yes(halted,"boot halts on "+failure);Yes(!GDVars.stingArg,"STING case sensitive");
            Yes(h.Log.Contains("restore")== (failure!="open"),"restoration failure branch");Yes(!h.Log.Any(x=>x.StartsWith("sound:")),"failure never proceeds to sound/config paths");
        }
        h=SetupBoot();h.Available=68352;h.Config="V\nP\nDATA\nSAVE\nX\n";GameBoot.Initialize("");Yes(!GDVars.fullIntro,"intro F exact");
        GameBoot.Host=null;bool missing=false;try{GameBoot.Initialize("");}catch(NotSupportedException){missing=true;}Yes(missing,"required startup host");
    }
    static void MainChecks()
    {
        foreach(bool sting in new[]{false,true})
        {
            SmSetup();SmDone();GDVars.stingArg=sting;var log=new List<string>();var s=new MainServices{Log=log};FixMain.Services=s;
            FixMain.BuildDataFiles=()=>log.Add("build");FixMain.Introduction=()=>{log.Add("intro");smio.Keys.Enqueue(0);smio.Keys.Enqueue(72);};
            FixMain.ForgetVault=()=>log.Add("vault");FixMain.SetViewColors=()=>log.Add("colors");
            FixMain.GameLoop=()=>{log.Add("loop");s.Loops++;s.QuickLoadAsked=s.Loops<3;};
            bool halted=false;try{FixMain.RunBody();}catch(Halted){halted=true;}
            Yes(halted,"main menu exits program");Eq(3,s.Loops,"repeated quick loads restart VM");
            EqS("init|build|"+(sting?"":"intro|")+"vault|menu|colors|loop|load:False|loop|load:False|loop|free|vault|reset|menu",string.Join("|",log),"main lifecycle");
            Eq(0,smio.Keys.Count,"intro skip key fully discarded, including scan code");
        }
        FixMain.Services=null;bool missing=false;try{FixMain.RunBody();}catch(NotSupportedException){missing=true;}Yes(missing,"required game services");
        // The public entry must unhook audio even if startup fails.
        SoundSetup();var fake=sfx;GameBoot.Host=null;
        try{FixMain.Run("");}catch(NotSupportedException){}
        EqS("uninstall",string.Join("|",fake.Log),"entry finally cleanup");SoundDrv.Host=null;
    }
}
static partial class Tests
{
    static TempleFx TempleSetup(int status, byte[] effects, byte anyway, byte pay, List<string> calls)
    {
        CdSetup();GDVars.stingArg=false;GDVars.ifcState=C_Ifc.INDOORS;GDVars.effectLock=0;GDVars.zone1=new Types.TGbvmZone1();GDVars.zone1[C_Gbvm.PARTY_SIZE]=1;
        var c=new Types.TCharRecord{name="TEST",status=(byte)status,hpCur=9,hpMax=20,enabled=0,conCur=15};
        c.classLevels[C_Level.FIGHTER]=4;GDVars.creatureList=c;GDVars.curChar=c;
        foreach(byte id in effects)c.effectsAddr=new Types.TEffectRec{id=id,next=c.effectsAddr};
        var fx=new TempleFx();Temple.Effects=fx;
        Temple.ConfirmAnywayCall=s=>{calls.Add("ask:"+s);return anyway;};
        Temple.ConfirmCostCall=(s,cost)=>{calls.Add("pay:"+s+":"+cost);return pay;};return fx;
    }
    static void TempleChecks()
    {
        foreach(bool found in new[]{false,true})foreach(byte anyway in new[]{(byte)'Y',(byte)'N'})foreach(byte paid in new[]{(byte)'Y',(byte)'N'})
        {
            var calls=new List<string>();var fx=TempleSetup(0,found?new[]{(byte)C_Effect.BLIND}:new byte[0],anyway,paid,calls);
            Temple.CureBlindness();bool asked=!found, charged=found||anyway=='Y';
            Eq((asked?1:0)+(charged?1:0),calls.Count,"blind confirmation count");if(charged)EqS("pay:Cure Blindness:1000",calls.Last(),"blind cost");
            EqS(charged&&paid=='Y'?"remove:"+C_Effect.BLIND+":0":"",string.Join("|",fx.Log),"blind effect only after paid");
            calls.Clear();fx.Log.Clear();fx.Diseased=found;Temple.CureDisease();
            Eq((asked?1:0)+(charged?1:0),calls.Count,"disease confirmation count");if(charged)EqS("pay:Cure Disease:1000",calls.Last(),"disease cost");EqS("disease?"+(charged&&paid=='Y'?"|diseases":""),string.Join("|",fx.Log),"disease control flow");
        }
        foreach(byte spell in new byte[]{0,1,2,3,4,255})foreach(byte paid in new[]{(byte)'Y',(byte)'N'})
        {
            var calls=new List<string>();var fx=TempleSetup(0,new byte[0],(byte)'Y',paid,calls);int rolls=0;Dice.RandomBelow=n=>{Eq(8,n,"wound die size");rolls++;return 0;};
            Temple.CureWounds(spell);bool valid=spell>=1&&spell<=3;
            Eq(valid?1:0,calls.Count,"valid cure selection");Eq(valid&&paid=='Y'?spell:0,rolls,"wound dice count");
            int hp=spell+(spell==1?0:spell==2?1:3);
            EqS(valid&&paid=='Y'?"hp:"+hp+":False":"",string.Join("|",fx.Log),"wounds heal result ignored");
            if(valid)Yes(calls[0].EndsWith(":"+(spell==1?100:spell==2?350:600)),"wound prices");
        }
        for(int status=0;status<=8;status++)for(int poison=0;poison<8;poison++)foreach(byte paid in new[]{(byte)'Y',(byte)'N'})
        {
            var ids=new List<byte>();if((poison&1)!=0)ids.Add(C_Effect.POISONED);if((poison&2)!=0)ids.Add(C_Effect.SLOW_POISON_END);if((poison&4)!=0)ids.Add(C_Effect.SLOW_POISON_HIT);
            var calls=new List<string>();var fx=TempleSetup(status,ids.ToArray(),(byte)'Y',paid,calls);Temple.NeutralizePoison();
            bool resurrect=paid=='Y'&&status==C_Status.DEAD&&(poison&1)!=0&&(poison&6)==0;
            Eq(resurrect?C_Status.OKAY:status,GDVars.curChar.status,"poison versus slow poison resurrection");Eq(resurrect?1:9,GDVars.curChar.hpCur,"poison hp");
            EqS(paid=='Y'?"remove:"+C_Effect.POISONED+":1|remove:"+C_Effect.SLOW_POISON_END+":1|remove:"+C_Effect.SLOW_POISON_HIT+":1":"",string.Join("|",fx.Log),"locked removal order");Eq(0,GDVars.effectLock,"effect lock released");
            Eq((poison&1)!=0?1:2,calls.Count,"poison optional ask");EqS("pay:Neutralize Poison:1000",calls.Last(),"poison cost");
        }
        for(int status=0;status<=8;status++)foreach(int con in new[]{0,3,4,15,16,17,18,25})foreach(byte paid in new[]{(byte)'Y',(byte)'N'})
        {
            var calls=new List<string>();var fx=TempleSetup(status,new byte[0],(byte)'Y',paid,calls);GDVars.curChar.conCur=(byte)con;
            bool dead=status==C_Status.DEAD||status==C_Status.ANIMATED;bool raised=dead&&paid=='Y';
            // Separate constitution table evaluation, per source rule: 4 fighter hit dice.
            int afterCon=raised&&con>3?con-1:con;
            int oldBonus=ClassCal.ConHpPerDie(C_Level.FIGHTER,(byte)con),newBonus=ClassCal.ConHpPerDie(C_Level.FIGHTER,(byte)afterCon);
            int loss=raised?Math.Min(19,Math.Max(0,4*(oldBonus-newBonus))):0;
            Temple.RaiseDead();EqS("pay:Raise Dead:5500",calls.Last(),"raise price");Eq(afterCon,GDVars.curChar.conCur,"raise constitution floor");Eq(20-loss,GDVars.curChar.hpMax,"constitution hp loss");
            Eq(raised?C_Status.OKAY:status,GDVars.curChar.status,"raise status");Eq(raised?1:9,GDVars.curChar.hpCur,"raise hp");Eq(raised?1:0,GDVars.curChar.enabled,"raise enable");
            EqS(raised?"remove:"+C_Effect.ANIMATE_DEAD+":1|remove:"+C_Effect.POISONED+":1":"",string.Join("|",fx.Log),"raise effect order");
        }
        foreach(bool curseItem in new[]{false,true})foreach(bool curseFx in new[]{false,true})foreach(byte answer in new[]{(byte)'Y',(byte)'N'})
        {
            var calls=new List<string>();var fx=TempleSetup(0,curseFx?new[]{(byte)C_Effect.BESTOW_CURSE}:new byte[0],answer,(byte)'Y',calls);
            GDVars.curChar.itemsAddr=new Types.TItemRec{next=new Types.TItemRec{cursed=(byte)(curseItem?1:0)}};Temple.RemoveCurse();
            bool cast=curseItem||curseFx||answer=='Y';if(cast)EqS("pay:Remove Curse:3500",calls.Last(),"curse price");EqS(cast?"curse:True":"",string.Join("|",fx.Log),"remove curse targeting");
        }
        for(int status=0;status<=8;status++)foreach(byte anyway in new[]{(byte)'Y',(byte)'N'})foreach(byte paid in new[]{(byte)'Y',(byte)'N'})
        {
            var calls=new List<string>();TempleSetup(status,new byte[0],anyway,paid,calls);Temple.StoneToFlesh();
            bool cured=status==C_Status.STONED&&paid=='Y';if(status==C_Status.STONED||anyway=='Y')EqS("pay:Stone to Flesh:2000",calls.Last(),"stone price");Eq(cured?C_Status.OKAY:status,GDVars.curChar.status,"stone status");Eq(cured?1:9,GDVars.curChar.hpCur,"stone hp");
        }
        // Real menu/pay integration (rather than seam confirmations).
        foreach(bool funds in new[]{false,true})
        {
            CdSetup();GDVars.stingArg=false;GDVars.ifcState=C_Ifc.INDOORS;GDVars.curChar=new Types.TCharRecord{name="Patient",strCur=18};GDVars.creatureList=GDVars.curChar;GDVars.curChar.coins[C_Money.GOLD]=(ushort)(funds?1500:0);Array.Clear(GDVars.treasureMoney.Data,0,7);
            smio.Keys.Enqueue((byte)'Y');smio.Keys.Enqueue(13);byte c=Temple.ConfirmCureCost("Cure Blindness",1000);
            Eq(funds?'Y':'N',c,"real payment confirmation");Eq(funds?500:0,MoneyCal.CreatureGold(GDVars.curChar),"real cure charge");
        }
        CdSetup();GDVars.curChar=new Types.TCharRecord{name=new string('A',30)};GDVars.creatureList=GDVars.curChar;smio.Keys.Enqueue(27);
        int restored=0;Temple.DisplayInterface=()=>restored++;Temple.DisplayParty=c=>restored++;
        Temple.HealMenu();Eq(2,restored,"heal menu escape restores interface and party");
        var l=new List<string>();TempleSetup(0,new byte[0],(byte)'Y',(byte)'Y',l);Temple.Effects=null;bool missing=false;try{Temple.CureDisease();}catch(NotSupportedException){missing=true;}Yes(missing,"effects required");
        Temple.ConfirmAnywayCall=Temple.ConfirmCureAnyway;Temple.ConfirmCostCall=Temple.ConfirmCureCost;Temple.DisplayInterface=CharDisp.DisplayNoncombatInterface;Temple.DisplayParty=CharDisp.DisplayParty;CdRestore();
    }
    static void MoneyFifteen()
    {
        try
        {
            CdSame("GoBack clears message pane only",()=>{MSetup(1,1,false)();TextRend.DisplayString(2,2,10,"KEEP");},()=>{Money.GoBackForIt("LEFT","BACK?");},()=>{OGoBack("LEFT","BACK?");},new byte[]{(byte)'N'});
            CdSetup();MSetup(1,1,false)();smio.Keys.Enqueue((byte)'Y');smio.OnRead=()=>TextRend.DisplayString(2,2,10,"KEEP");
            Yes(Money.GoBackForIt("LEFT","BACK?"),"Yes returns go-back decision");smio.OnRead=null;EqS("11/10",smscr.Cell(2,2),"GoBack clear preserves upper pane written while menu waits");
            foreach(int itemCount in new[]{14,15,16})
            {
                Func<Random,Types.TCharRecord,int> extra=(r,head)=>
                {
                    var c=GDVars.curChar;Array.Clear(c.coins,0,c.coins.Length);c.useStrength=1;c.strCur=18;c.coins[C_Money.GEMS]=1;c.coins[C_Money.JEWELRY]=0;c.itemsAddr=null;
                    for(int i=0;i<itemCount;i++)c.itemsAddr=new Types.TItemRec{itemType=2,weight=1,next=c.itemsAddr};
                    CharHlp.RecalcStats(c);return 0;
                };
                bool a=false,b=false;
                CdSame("Appraise capacity boundary "+itemCount,MSetup(19,1,false,extra),()=>{Money.AppraiseMenu(ref a);MEnd();},()=>{OAppraise(ref b);MEnd();},new byte[]{(byte)'G',(byte)'K',(byte)'S',(byte)'E'});
                // Prove the 15-item case actually offers Keep, not merely that two paths exit equally.
                CdSetup();MSetup(19,1,false,extra)();smio.Keys.Enqueue((byte)'G');smio.Keys.Enqueue((byte)'K');smio.Keys.Enqueue((byte)'S');smio.Keys.Enqueue((byte)'E');
                Money.AppraiseMenu(ref a);Eq(itemCount<16?itemCount+1:16,GDVars.curChar.numberOfItems,"appraised gem reaches inventory boundary");
            }
        }
        finally{MClean();}
    }
}
static partial class Tests
{
    static void BindingChecks()
    {
        ResetWorld();EclVm.Units=new EclVmUnits();EasiestTenBindings.BindUnits();
        var game=EclVm.Units.Game;EasiestTenBindings.BindUnits();Yes(ReferenceEquals(game,EclVm.Units.Game),"game decorator idempotent");
        var c=Ch();c.effectsAddr=new Types.TEffectRec{id=33};Types.TEffectRec e=null;
        Yes(EclVm.Units.CharHlp.HasEffect(c,33,ref e)&&ReferenceEquals(e,c.effectsAddr),"effect lookup pointer ref adapter");
        Types.TItemRec list=null;var original=new Types.TItemRec{weight=17};EclVm.Units.CharHlp.PrependItemCopy(ref list,original);Yes(!ReferenceEquals(list,original)&&list.weight==17,"item deep copy adapter");
        GDVars.treasureItems=list;GDVars.treasureMoney[0]=100;EclVm.Units.CharHlp.ClearTreasure();Yes(GDVars.treasureItems==null&&GDVars.treasureMoney[0]==0,"treasure adapter");
        CdSetup();GDVars.ifcState=C_Ifc.MAIN_MENU;GDVars.creatureList=null;EclVm.Units.CharDisp.DisplayParty(null);EqS(new Scr().Cell(0,0),smscr.Cell(0,0),"display adapter leaves unrelated cell");EqS("14/15",smscr.Cell(1,2),"display adapter draws name heading");
        GDVars.soundMode=C_Sound.MODE_DISABLED;SoundDrv.Host=null;game.SoundCommand(2);Yes(true,"VM disabled sound real no-op route");
        // VM Temple routes to the converted counter and Heal menu, not a fallback.
        CdSetup();GDVars.curChar=new Types.TCharRecord{name="Priest visitor"};GDVars.creatureList=GDVars.curChar;
        var oldInterface=Shop.DisplayInterface;var oldParty=Shop.DisplayParty;int panels=0;Shop.DisplayInterface=()=>panels++;Shop.DisplayParty=p=>panels++;
        smio.Keys.Enqueue((byte)'E');game.TempleMainMenu();Yes(panels>=2&&GDVars.ifcState==C_Ifc.SHOP_TEMPLE,"VM temple real counter route");Shop.DisplayInterface=oldInterface;Shop.DisplayParty=oldParty;CdRestore();
        foreach(char v in new[]{'E','T','V','e','X'})
        {
            GameBoot.SelectVideo(v);EqS(v=='E'?"EGA":v=='T'?"Tandy":v=='V'?"VGA":"CGA",GDVars.video.name,"real boot video selection");
            Eq(v=='E'?13:v=='T'?9:v=='V'?19:4,GDVars.video.biosMode,"real BIOS mode");
        }
        SoundSetup();GameBoot.SelectSound('P');EqS("PC Speaker",GDVars.sound.name,"boot speaker");
        GameBoot.SelectSound('T');EqS("Tandy",GDVars.sound.name,"boot Tandy");GameBoot.SelectSound('p');EqS("None",GDVars.sound.name,"boot sound case sensitive");
        int reads=0;SndOpl.ReadPort=p=>(byte)(reads++==0?0:192);GameBoot.SelectSound('A');EqS("AdLib",GDVars.sound.name,"boot OPL");
        GameBoot.InstallSoundBlaster=null;bool missing=false;try{GameBoot.SelectSound('S');}catch(NotSupportedException){missing=true;}Yes(missing,"boot missing SB explicit");int called=0;GameBoot.InstallSoundBlaster=()=>called++;GameBoot.SelectSound('S');Eq(1,called,"SB callback route");GameBoot.InstallSoundBlaster=null;SoundDrv.Host=null;
    }
}
