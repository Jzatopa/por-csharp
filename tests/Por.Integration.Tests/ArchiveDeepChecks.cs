using System;using System.IO;using System.Collections.Generic;using Por.Core;
public static class ArchiveDeepChecks
{
 static void Yes(bool b,string s){IntegrationVerification.Yes(b,s);}
 sealed class Block {public byte id;public byte[] raw,packed;public int declared=-1;public Block(byte i,byte[] r){id=i;raw=r;packed=Pack(r);}}
 static byte[] Pack(byte[] raw){using(var m=new MemoryStream()){for(int i=0;i<raw.Length;){int n=Math.Min(127,raw.Length-i);m.WriteByte((byte)(n-1));m.Write(raw,i,n);i+=n;}return m.ToArray();}}
 static void Archive(string path,params Block[] b){using(var w=new BinaryWriter(File.Create(path))){w.Write((ushort)(9*b.Length));int at=0;foreach(var x in b){w.Write(x.id);w.Write(at);w.Write((ushort)x.raw.Length);w.Write((ushort)(x.declared<0?x.packed.Length:x.declared));at+=x.packed.Length;}foreach(var x in b)w.Write(x.packed);}}
 static byte[] Source(byte disk,int length){using(var m=new MemoryStream())using(var w=new BinaryWriter(m)){w.Write(disk);w.Write(length);w.Write(0xbadf00dU);return m.ToArray();}}
 static void Merged(string dir,string name,int sourceSize,byte[] remap,byte version=33){Archive(Path.Combine(dir,name+".DAX"),new Block(20,new byte[]{11}),new Block(21,new byte[]{22}),new Block(253,Source(1,sourceSize)),new Block(254,remap),new Block(255,new byte[]{version,0xaa,0xbb,0xcc,0xdd}));}
 sealed class ExitSignal:Exception{}
 sealed class Host:DaxIo.IDaxPlatform {
  public List<byte[]> writes=new List<byte[]>();public int keys,exits,bios,overlay;public Action keyAction;public string CurrentDirectory{get{return "c:\\WORK";}}public string FullPath(string path){return new string('x',50)+"\\"+Path.GetFileName(path);}
  public void WriteChar(byte col,byte row,byte attr,byte count,byte ch){writes.Add(new byte[]{col,row,attr,count,ch});}
  public void BiosWriteChar(byte page,byte col,byte row,byte attr,byte count,byte ch){bios++;}
  public byte ReadGameKey(){keys++;if(keyAction!=null)keyAction();return 13;}
  public void ExitGame(){exits++;throw new ExitSignal();}public void OverlayMessage(string text){overlay++;Yes(text=="Please insert overlay disk.","source overlay prompt");}
 }
 public static void Run()
 {
  string dir=Path.Combine(Path.GetTempPath(),"por-dax-deep-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);string old=GDVars.gameDir,oldCwd=Directory.GetCurrentDirectory();var oldVideo=GDVars.video;var oldPlatform=DaxIo.Platform;GDVars.gameDir=dir+Path.DirectorySeparatorChar;GDVars.dsSuppressingInsertDiskFlag=true;var host=new Host();DaxIo.Platform=host;GDVars.video=new Types.TVideoDriver(){WriteChar=host.WriteChar};DaxIo.ResetArchiveCache();
  try{
   string disk=Path.Combine(dir,"eCl1.DaX");Archive(disk,new Block(7,new byte[]{99}));int sourceSize=(int)new FileInfo(disk).Length;
   Merged(dir,"ECL",sourceSize,new byte[]{1,7,20,1,7,21,0,7,20,1,253,20,1,7,254,0x11,0x22});
   Yes(DaxIo.FamilyUsable(1),"source version+size accepts ignored CRC bytes");Yes(DaxIo.ArchiveName("ecl1")=="ECL","family redirect case-insensitive DOS file");Yes(DaxIo.DiskHasBlock("ECL1",7),"per-disk block ignores merged redirect");
   ushort size;byte[] data;DaxIo.LoadDax("ECL1",".DAX",7,out size,out data);Yes(size==1&&data[0]==22,"last remap match wins; invalid/trailing skipped");
   DaxIo.LoadDax("ECL1",".DAX",253,out size,out data);Yes(size==0&&data==null,"reserved ids never redirect");
   using(var w=new FileStream(disk,FileMode.Append))w.WriteByte(0);Yes(DaxIo.FamilyUsable(1),"positive family cache sticky");DaxIo.ResetArchiveCache();Yes(!DaxIo.FamilyUsable(1),"source size change rejected after reset");Archive(disk,new Block(7,new byte[]{98}));Yes(!DaxIo.FamilyUsable(1),"negative cache sticky");DaxIo.ResetArchiveCache();Yes(DaxIo.FamilyUsable(1),"same-size content changes accepted per original source");
   Archive(Path.Combine(dir,"ECL2.DAX"),new Block(8,new byte[]{1}));DaxIo.ResetArchiveCache();Yes(!DaxIo.FamilyUsable(1),"unlisted source file rejected");File.Delete(Path.Combine(dir,"ECL2.DAX"));
   Merged(dir,"ECL",sourceSize,new byte[]{1,7,20},34);DaxIo.ResetArchiveCache();Yes(!DaxIo.FamilyUsable(1),"wrong version rejected");
   byte[] full=new byte[224*3];for(int i=0;i<224;i++){full[i*3]=1;full[i*3+1]=7;full[i*3+2]=21;}Merged(dir,"ECL",sourceSize,full);DaxIo.ResetArchiveCache();Yes(DaxIo.FamilyUsable(1),"224 total remaps fit, not per-family16");
   byte[] overflow=new byte[225*3];for(int i=0;i<225;i++){overflow[i*3]=1;overflow[i*3+1]=7;overflow[i*3+2]=21;}Merged(dir,"ECL",sourceSize,overflow);DaxIo.ResetArchiveCache();Yes(!DaxIo.FamilyUsable(1),"225th remap drops family");DaxIo.LoadDax("ECL1",".DAX",7,out size,out data);Yes(size==1&&data[0]==98,"overflow restores per-disk fallback");
   var cut=new Block(9,new byte[5]){packed=new byte[]{2,77},declared=4};Archive(Path.Combine(dir,"CUT.DAX"),cut);DaxIo.LoadDax("CUT",".DAX",9,out size,out data);Yes(size==5&&data[0]==77&&data[4]==0,"truncated payload zero padded");
   File.WriteAllBytes(Path.Combine(dir,"BAD.DAX"),new byte[]{9,0,9});System.IO.FileStream f1,f2;ushort hdr;Yes(!DaxIo.OpenDax(Path.Combine(dir,"BAD"),".DAX",out hdr,out f1,out f2)&&f1==null&&f2==null,"bad header closes both");
   using(var w=new BinaryWriter(File.Create(Path.Combine(dir,"WRAP.DAX")))){w.Write((ushort)65535);w.Write(new byte[65535]);}Yes(DaxIo.OpenDax(Path.Combine(dir,"WRAP"),".DAX",out hdr,out f1,out f2)&&hdr==1&&f1.Position==1&&f2.Position==2,"header Word wrap preserved");DaxIo.CloseDax(f1,f2);
   foreach(ushort n in new ushort[]{0,1,7,8,9,36000,65528,65529,65535}){byte[] p=null;DaxIo.AllocRounded(ref p,n);Yes(p.Length==((n+7)&65528),"8-byte allocation formula");DaxIo.FreeRounded(ref p,n);Yes(p==null,"free clears ref");}
   GDVars.virtualDisk=2;DaxIo.PushState(3);DaxIo.PushState(5);DaxIo.PopState();Yes(GDVars.virtualDisk==3,"disk previous is single slot");DaxIo.PopState();Yes(GDVars.virtualDisk==3,"disk pop not invented stack");
   char savedChar=GDConsts.invalidFileChars[1];try{GDConsts.invalidFileChars[1]='Z';Yes(DaxIo.SanitizeFileName("aZ")=="A","sanitizer observes mutable source table");}finally{GDConsts.invalidFileChars[1]=savedChar;}
   host.writes.Clear();DaxIo.WriteString(254,1,2,"abc");Yes(host.writes.Count==3&&host.writes[0][0]==254&&host.writes[1][0]==255&&host.writes[2][0]==0,"byte column wrap");
   host.writes.Clear();int k=host.keys;DaxIo.PromptBox(14,new string('Z',60));Yes(host.keys==k+1&&host.writes.Count==42&&host.writes[0][3]==40&&host.writes[41][2]==0,"40-byte prompt and clears");DaxIo.BiosWriteChar(2,3,4,5,6,7);Yes(host.bios==1,"BIOS adapter boundary");
   string retry=Path.Combine(dir,"RETRY.DAT");int before=host.keys;host.keyAction=()=>{if(host.keys==before+2)File.WriteAllBytes(retry,new byte[]{1});};Yes(DaxIo.OpenWithRetry(retry,"disk prompt",false,out f1),"two-prompt retry");f1.Dispose();host.keyAction=null;
   Directory.SetCurrentDirectory(dir);host.keyAction=()=>File.WriteAllBytes(Path.Combine(dir,"GAME.OVR"),new byte[]{1});DaxIo.OverlayPrompt();Yes(host.overlay==1,"overlay retry case-insensitive");Directory.SetCurrentDirectory(oldCwd);host.keyAction=null;
   var huge=new Block(1,new byte[1]){declared=36001,packed=new byte[0]};Archive(Path.Combine(dir,"TEMPSIZE.DAX"),huge);try{DaxIo.LoadDax("TEMPSIZE",".DAX",9,out size,out data);throw new Exception("oversized compressed block not rejected");}catch(ExitSignal){Yes(host.exits==1,"tempsize checked even for unmatched id");}
   using(var f=new FileStream(Path.Combine(dir,"TEMPSIZE.DAX"),FileMode.Open,FileAccess.ReadWrite,FileShare.None))Yes(f.Length==11,"exit path closed handles");
   DaxIo.Platform=null;GDVars.video.WriteChar=null;try{DaxIo.WriteChar(0,0,0,1,65);throw new Exception("missing renderer faked");}catch(InvalidOperationException){Yes(true,"missing renderer fails clearly");}
  }finally{Directory.SetCurrentDirectory(oldCwd);GDVars.gameDir=old;GDVars.video=oldVideo;DaxIo.Platform=oldPlatform;DaxIo.ResetArchiveCache();Directory.Delete(dir,true);}
 }
}
