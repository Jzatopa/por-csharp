using System;using System.IO;using System.Linq;using System.Collections.Generic;using Por.Core;
class Fatal:Exception{}
class Io:IGameIoHost{
 public Queue<byte[]> Batches=new Queue<byte[]>();public Queue<byte> Active=new Queue<byte>();public List<string> Log;public Action BeforeKey;
 public bool KeyPressed{get{return Active.Count>0;}}
 public void Inject(){if(Active.Count==0){if(Batches.Count==0)throw new Exception("Input exhausted in menu");foreach(var b in Batches.Dequeue())Active.Enqueue(b);}}
 public byte ReadKey(){if(BeforeKey!=null)BeforeKey();Inject();if(Active.Count==0)throw new Exception("Input exhausted");var k=Active.Dequeue();Log.Add("key:"+k);return k;}
 public void Halt(int c){Log.Add("halt");throw new Fatal();}public void WriteLine(string s){}public void SoundCommand(ushort c){}public void SoundSuspend(){}public void Interrupt(byte n,Types.TInterruptRegisters r){}public TextWriter OpenDebugLog(string s){throw new NotSupportedException();}
}
static partial class Tests{
 static long checks;static List<string> log=new List<string>();static Io io;static string dir;static int bios;
 static byte B(int n){return unchecked((byte)n);}static ushort W(int n){return unchecked((ushort)n);}static void Yes(bool b,string s){checks++;if(!b)throw new Exception(s);}
 static void Setup(){
  log.Clear();GDVars.zone0=new Types.TGbvmZone0State();GDVars.zone1=new Types.TGbvmZone1();GDVars.curChar=new Types.TCharRecord{name="TEST",strCur=10};GDVars.creatureList=GDVars.curChar;GDVars.treasureItems=null;Array.Clear(GDVars.treasureMoney.Data,0,7);GDVars.gameDelay=0;GDVars.gameFont=new Types.TFontArr();GDVars.animData.current=0;GDVars.stingArg=false;GDVars.dsSaveDirectory=dir+Path.DirectorySeparatorChar;GDVars.gameDir=dir+Path.DirectorySeparatorChar;GDVars.virtualDisk=1;
  GDVars.video.DrawGlyph=(x,y,c,g)=>{};GDVars.video.WriteChar=(x,y,a,n,c)=>{};GDVars.video.ShowPic=(x,y,p)=>{};GDVars.video.ClearScreen=()=>{};GDVars.video.ClearRect=(a,b,c,d)=>log.Add("clear:"+a+":"+b+":"+c+":"+d);GDVars.video.FlushSpans=()=>{};for(int i=0;i<5;i++)GDVars.dsSymbolSetPtrs[i]=null;
  bios=0;TextRend.ReadBiosTicks=()=>bios++/2*10;TextRend.DelayMilliseconds=ms=>log.Add("pause:"+ms);io=new Io{Log=log};GameIo.Host=io;MenuCore.idleHook=()=>io.Inject();MenuCore.arrowsToCaller=false;MenuCore.quickKeysToCaller=false;MenuCore.extraKeys="";MenuCore.unmarked="";MenuCore.enterToCaller=false;
  Shop.DisplayInterface=()=>log.Add("interface");Shop.DisplayParty=c=>log.Add("party");Shop.BottomLine=s=>log.Add("bottom:"+s);Shop.ItemLabel=CharHlp.ItemLabel;VaultMnu.DisplayInterface=()=>log.Add("interface");VaultMnu.DisplayParty=c=>log.Add("party");CommerceServices.CannotCarryItem=null;CommerceServices.ViewCharacter=null;CommerceServices.LootTake=null;Dice.RandomBelow=n=>0;
 }
 static byte[] K(char c){return new[]{B(c)};}static byte[] Ext(int c){return new byte[]{0,B(c)};}static void Keys(params byte[][] keys){foreach(var k in keys)io.Batches.Enqueue(k);}
 static int Main(string[] args){dir=Path.Combine(Path.GetTempPath(),"por-direct-ten-"+Guid.NewGuid());Directory.CreateDirectory(dir);try{var cases=new Dictionary<string,Action>{{"HeapErr",HeapChecks},{"Ending",EndingChecks},{"Shop",ShopChecks},{"VaultMnu",VaultMenuChecks},{"VaultExtra",VaultExtra},{"Bindings",Bindings}};foreach(var row in cases){if(args.Length>0&&!args.Contains(row.Key))continue;Setup();long n=checks;row.Value();Console.WriteLine(row.Key+" source-derived assertions: "+(checks-n));}Console.WriteLine("DirectTen assertions: "+checks);return 0;}catch(Exception e){Console.WriteLine(e);return 1;}finally{DaxIo.ResetArchiveCache();Directory.Delete(dir,true);}}
 static void HeapChecks(){
  HeapErr.ResetForTests();HeapErr.KeepReserve();HeapErr.KeepReserve();Yes(HeapErr.HasReserve,"reserve allocated/idempotent");Yes(HeapErr.HeapErrorHandler(0)==0&&HeapErr.HasReserve&&log.Count==0,"zero request untouched");Keys(Ext(75),K((char)13),K((char)13));int saves=0;HeapErr.SaveGame=()=>{Yes(!HeapErr.HasReserve,"reserve released before save");int n=log.Count;Yes(HeapErr.HeapErrorHandler(65535)==0&&log.Count==n,"reentry no UI/save");Yes(GDVars.recentSaved==0,"save flag cleared before each attempt");if(++saves==2)GDVars.recentSaved=1;};bool halted=false;try{HeapErr.HeapErrorHandler(65535);}catch(Fatal){halted=true;}Yes(halted&&saves==2&&log.Last()=="halt","cancelled save retries then fatal exit");int count=log.Count;Yes(HeapErr.HeapErrorHandler(1)==0&&log.Count==count,"handler guard survives exit");
  Setup();HeapErr.ResetForTests();Keys(K((char)27));halted=false;try{HeapErr.HeapErrorHandler(1);}catch(Fatal){halted=true;}Yes(halted,"escape exits without requiring saver");
  Setup();HeapErr.ResetForTests();Keys(K((char)13));bool missing=false;try{HeapErr.HeapErrorHandler(1);}catch(NotSupportedException){missing=true;}Yes(missing,"missing real saver fails clearly");
 }
 static void ShopChecks(){
  bool failed=false;var item=new Types.TItemRec{value=20,weight=10,name="TEST"};CommerceServices.CannotCarryItem=(c,i)=>true;Shop.GiveItemCopyToCreature(item,ref failed);Yes(failed&&GDVars.curChar.itemsAddr==null&&log.Last()=="bottom:OverLoaded","overload no give or charge");CommerceServices.CannotCarryItem=(c,i)=>false;Shop.GiveItemCopyToCreature(item,ref failed);Yes(!failed&&GDVars.curChar.itemsAddr!=item&&GDVars.curChar.itemsAddr.value==20&&GDVars.curChar.itemsAddr.next==null,"copy before stats not alias");
  Setup();var a=new Types.TItemRec{name=new string('Z',40),value=65535,next=new Types.TItemRec{name="B",value=0}};GDVars.treasureItems=a;List<string> shown=null;io.BeforeKey=()=>{shown=new List<string>{a.name,a.next.name};};Shop.ItemLabel=(i,inv)=>i.name="RESTORED";Keys(K('E'));byte key=0;Types.TItemRec selected=null,cursor=a;short idx=0;Shop.ShopBuyItemSelection(ref key,ref selected,ref cursor,ref idx);Yes(key==0&&selected==null&&a.next.value==1,"selection exit and zero price floor");Yes(shown[0]==new string('Z',24)+" "+"65535"&&shown[1]=="B"+new string(' ',28)+"1","30-column prices and clipped names");Yes(a.name=="RESTORED"&&a.next.name=="RESTORED","all real labels restored after menu");
  foreach(bool temple in new[]{false,true}){
   Setup();int commands=0;Keys(K(temple?'H':'B'),K('E'));Shop.CounterMenu(temple,()=>commands++);Yes(commands==1&&GDVars.ifcState==C_Ifc.SHOP_TEMPLE,"correct counter command");Yes(log.First()=="interface"&&log.Last()=="party","counter panel lifecycle");
   Setup();commands=0;MenuCore.quickKeysToCaller=true;Keys(Ext(temple?C_Keys.UP:C_Keys.F8),K('E'));Shop.CounterMenu(temple,()=>commands++);Yes(commands==0,"scan H/B never triggers Heal/Buy");
  }
  Setup();Keys(K((char)27),K('E'));Shop.CounterMenu(false,()=>{});Yes(log.Count(s=>s=="party")==3,"source Escape does not leave counter");
  Setup();Keys(K('B'),K('B'),K('E'),K('E'));int bought=0;CommerceServices.CannotCarryItem=(c,i)=>{bought++;return false;};GDVars.curChar.coins[C_Money.GOLD]=100;GDVars.treasureItems=new Types.TItemRec{value=25,itemType=1};Shop.ShopMainMenu();Yes(bought==1&&MoneyCal.CreatureGold(GDVars.curChar)==75&&GDVars.curChar.itemsAddr!=null,"actual purchase charges only after copy");
  foreach(bool noFunds in new[]{false,true}){
   Setup();Keys(K('B'),K('E'));GDVars.treasureItems=new Types.TItemRec{value=25,itemType=1};GDVars.curChar.coins[C_Money.GOLD]=B(noFunds?0:100);int calls=0;CommerceServices.CannotCarryItem=(c,i)=>{calls++;return true;};Shop.ShopBuyMenu();Yes(GDVars.curChar.itemsAddr==null&&MoneyCal.CreatureGold(GDVars.curChar)==(noFunds?0:100)&&calls==(noFunds?0:1),"not enough or overload never charges/copies");
  }
  Setup();Keys(K('B'),K('E'));GDVars.treasureItems=new Types.TItemRec{value=25,itemType=1};GDVars.treasureMoney[C_Money.GOLD]=40;CommerceServices.CannotCarryItem=(c,i)=>false;Shop.ShopBuyMenu();Yes(MoneyCal.CoinValueInGold(GDVars.treasureMoney)==15&&GDVars.curChar.itemsAddr!=null,"pool pays when character cannot");
  Setup();Keys(K('B'),K('E'),K('N'));Shop.CounterMenu(false,()=>GDVars.treasureMoney[C_Money.GOLD]=10);Yes(GDVars.treasureMoney[C_Money.GOLD]==10,"leaving pool prompts No and keeps remaining money");
  Setup();Keys(K('V'));bool missing=false;try{Shop.CounterMenu(false,()=>{});}catch(NotSupportedException){missing=true;}Yes(missing,"shop missing viewer fails");

 }
 static void VaultMenuChecks(){
  FileUtil.DeleteFile(GDVars.dsSaveDirectory+"tmpvault.dat");Keys(K('E'));Dice.RandomBelow=n=>throw new Exception("first visit must not draw RNG");VaultMnu.VaultMenu();Yes(log.First()=="interface"&&log.Last()=="party"&&File.Exists(GDVars.dsSaveDirectory+"tmpvault.dat"),"first visit no random and persists on exit");Yes(GDVars.ifcState==C_Ifc.VAULT,"vault interface state");
  Setup();int rolls=0;Dice.RandomBelow=n=>{Yes(n==20,"welcome d20");rolls++;return 0;};Keys(K('V'),K('E'));CommerceServices.ViewCharacter=(ref bool ext)=>log.Add("view");VaultMnu.VaultMenu();Yes(rolls==1&&log.Contains("view"),"visited random welcome and view route");
  Setup();GDVars.treasureMoney[C_Money.GOLD]=12;Vault.WriteVault();Setup();Keys(K('T'),K('E'));CommerceServices.LootTake=(money,items)=>{Yes(money&&!items,"take flags");log.Add("take");GDVars.treasureMoney[C_Money.GOLD]=0;};VaultMnu.VaultMenu();Yes(log.Contains("take")&&log.Count(s=>s=="interface")==2,"loot then refresh");
  Setup();Keys(K('V'));CommerceServices.ViewCharacter=null;bool missing=false;try{VaultMnu.VaultMenu();}catch(NotSupportedException){missing=true;}Yes(missing,"missing real ViewChar boundary");
 }
 static void VaultExtra(){
  Types.TItemRec tail=null;for(int i=0;i<1100;i++){var item=new Types.TItemRec{name="N"+i,value=W(i),weight=1};if(tail==null)GDVars.treasureItems=item;else tail.next=item;tail=item;}Vault.WriteVault();Yes(new FileInfo(Path.Combine(dir,"tmpvault.dat")).Length==35+1100*63,"stream vault beyond65535bytes no Word length clipping");Yes(GDVars.treasureItems==null,"clear after streaming");Vault.ReadVault();int count=0;for(var p=GDVars.treasureItems;p!=null;p=p.next)count++;Yes(count==100,"load capacity100 despite oversized source");
 }
 static void Bindings(){
  EclVm.Units=new EclVmUnits();DirectTenBindings.BindGameUnits();var adapter=EclVm.Units.Game;DirectTenBindings.BindGameUnits();Yes(ReferenceEquals(adapter,EclVm.Units.Game),"binding is explicit/idempotent");bool missing=false;try{adapter.CombatMain();}catch(NotSupportedException){missing=true;}Yes(missing,"missing unconverted VM game service explicit");
  var previous=new GameHost();EclVm.Units.Game=previous;DirectTenBindings.BindGameUnits();EclVm.Units.Game.PostCombat();bool flag=false;EclVm.Units.Game.CampMainMenu(ref flag);Yes(previous.Calls==2&&flag,"preserve previous unrelated bindings and refs");
  var old=MapView.Display3DMapView;try{int draws=0;MapView.Display3DMapView=(x,y,f)=>draws++;GDVars.zone0.view3dFlag=1;GDVars.map3d=new Types.TGeoMap();EclVm.Units.Game.DisplayMapView();Yes(draws==1,"VM game adapter reaches converted MapView");}finally{MapView.Display3DMapView=old;}
 }

}

class GameHost:IEclVmGameUnits{
 public int Calls;public void ShopMainMenu(){throw new Exception("old shop used");}public void VaultMenu(){throw new Exception("old vault used");}public void TempleMainMenu(){Calls++;}public void PostCombat(){Calls++;}public void CombatMain(){Calls++;}public void CharGenMainMenu(){Calls++;}public void WinGame(){throw new Exception("old ending used");}public void YarashComplete(bool a){throw new Exception("old Yarash used");}public void CampMainMenu(ref bool f){Calls++;f=true;}public void DisplayMapView(){throw new Exception("old map used");}public void SoundCommand(ushort c){Calls++;}
}
