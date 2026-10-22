using System;using System.IO;using System.Linq;using Por.Core;
class Stopped:Exception{}
class Host:IGameIoHost{
 public string Message;public bool KeyPressed{get{return false;}}public byte ReadKey(){return 65;}public void SoundCommand(ushort c){}public void SoundSuspend(){}public void Interrupt(byte n,Types.TInterruptRegisters r){}public void Halt(int code){throw new Stopped();}public void WriteLine(string s){Message=s;}public TextWriter OpenDebugLog(string p){throw new NotSupportedException();}
}
class Tests{
 static long checks;static void Yes(bool b,string m){checks++;if(!b)throw new Exception(m);}static Host host=new Host();
 static void Draw(){
  int[] first={1,46,116,186,256},last={45,115,185,255,286};int shown=0;GDVars.video.cellShift=0;GDVars.video.maskShift=0;GDVars.video.screenShift=0;GDVars.video.keyed=false;
  GDVars.dsCurrent8x8SymbolPtr2=new Types.TImgBuf{data=new byte[16]};
  for(int set=0;set<5;set++){var p=new Types.TImgBuf{width=1,height=8,count=(byte)(last[set]-first[set]+1),imgSize=8};for(int i=0;i<p.count*8;i++)p.data[i]=unchecked((byte)(i*29+set*41));GDVars.dsSymbolSetPtrs[set]=p;}
  for(int symbol=1;symbol<=286;symbol++){
   int set=Array.FindIndex(last,x=>symbol<=x),local=symbol-first[set];var p=GDVars.dsSymbolSetPtrs[set];sbyte col=unchecked((sbyte)(symbol-150)),row=unchecked((sbyte)(symbol*3));
   GDVars.video.ShowPic=(x,y,img)=>{shown++;Yes(x==col&&y==row&&Object.ReferenceEquals(img,GDVars.dsCurrent8x8SymbolPtr2),"screen delegate coordinates/reference");};
   for(int i=0;i<16;i++)GDVars.dsCurrent8x8SymbolPtr2.data[i]=199;
   Sym8x8.Put8x8Sym(col,row,(ushort)symbol,false,true);
   Yes(GDVars.dsCurrent8x8SymbolPtr2.data.Take(8).SequenceEqual(p.data.Skip(local*8).Take(8)),"all286 symbol offsets");Yes(GDVars.dsCurrent8x8SymbolPtr2.data.Skip(8).All(x=>x==199),"copy only imgSize");
   for(int masked=0;masked<2;masked++){
    var dest=new Types.TImgBuf{width=4,height=24,imgSize=96,data=Enumerable.Repeat((byte)77,96).ToArray()};GDVars.dsViewImageBufferPtr=dest;p.transp=new Types.TBytes();for(int i=0;i<p.count*8;i++)p.transp[i]=(byte)(i%2==0?255:0);
    Sym8x8.Put8x8Sym(2,1,(ushort)symbol,true,masked!=0);
    for(int i=0;i<96;i++){int yy=i/4,xx=i%4;byte expected=77;if(xx==2&&yy>=8&&yy<16){int at=local*8+yy-8;expected=masked==0?p.data[at]:unchecked((byte)((77&p.transp[at])|p.data[at]));}Yes(dest.data[i]==expected,"combat coordinates/transparency");}
   }
  }
  Yes(shown==286,"one screen draw per glyph");
  GDVars.video.ShowPic=(x,y,p)=>{throw new Exception("guard drew");};
  for(int set=0;set<5;set++){var p=GDVars.dsSymbolSetPtrs[set];p.height=7;Sym8x8.Put8x8Sym(0,0,(ushort)first[set],false,false);p.height=8;p.width=2;Sym8x8.Put8x8Sym(0,0,(ushort)first[set],false,false);p.width=1;p.count=0;Sym8x8.Put8x8Sym(0,0,(ushort)first[set],false,false);GDVars.dsSymbolSetPtrs[set]=null;Sym8x8.Put8x8Sym(0,0,(ushort)first[set],false,false);Yes(true,"shape/count/unloaded guards");}
  for(int n=0;n<65536;n++)if(n==0||n>286){host.Message=null;bool stopped=false;try{Sym8x8.Put8x8Sym(0,0,(ushort)n,false,false);}catch(Stopped){stopped=true;}Yes(stopped&&host.Message=="Bad symbol number in Put8x8Symbol.","all invalid symbols fatal");}
 }
 static void Load(){string dir=Path.Combine(Path.GetTempPath(),"por-symbol-"+Guid.NewGuid());Directory.CreateDirectory(dir);try{
  GDVars.gameDir=dir+Path.DirectorySeparatorChar;GDVars.virtualDisk=1;GDVars.dsSuppressingInsertDiskFlag=true;DaxIo.ResetArchiveCache();GDVars.picBytes=0;GDVars.video.cellShift=0;GDVars.video.maskShift=0;
  byte[] raw=new byte[49];raw[0]=8;raw[2]=1;raw[8]=1;for(int i=17;i<49;i++)raw[i]=(byte)i;
  using(var b=new BinaryWriter(File.Create(Path.Combine(dir,"8x8d1.dax")))){b.Write((ushort)9);b.Write((byte)7);b.Write(0);b.Write((ushort)raw.Length);b.Write((ushort)(raw.Length+1));b.Write((byte)(raw.Length-1));b.Write(raw);}
  int calls=0;GDVars.video.ExpandFrames=(data,off,mask,tc,p)=>{calls++;Yes(off==17&&mask&&tc==13,"symbol load transparency/header");for(int i=0;i<8;i++)p.data[i]=unchecked((byte)data[off+i]);};
  for(int set=0;set<5;set++){Sym8x8.Load8x8Set(7,(byte)set);Yes(GDVars.dsSymbolSetPtrs[set]!=null&&GDVars.dsSymbolSetPtrs[set].data.Take(8).SequenceEqual(raw.Skip(17).Take(8)),"loads each slot through real DAX/Blit");}
  Yes(calls==5,"five load calls");for(int set=5;set<256;set++)Sym8x8.Load8x8Set(7,(byte)set);Yes(calls==5,"out of range set no load");
  bool stopped=false;try{Sym8x8.Load8x8Set(99,0);}catch(Stopped){stopped=true;}Yes(stopped&&GDVars.dsSymbolSetPtrs[0]==null&&host.Message=="Unable to load 8x8D1-99","missing block fatal and freed old slot");
 }finally{DaxIo.ResetArchiveCache();Directory.Delete(dir,true);}}
 static void Main(){GameIo.Host=host;GDVars.debugTraceEnabled=false;Draw();Load();Console.WriteLine("Sym8x8 source-derived assertions: "+checks);}
}
