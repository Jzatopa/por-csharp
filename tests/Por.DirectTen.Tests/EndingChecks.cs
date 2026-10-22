using System;using System.Linq;using System.Collections.Generic;using System.IO;using Por.Core;
static partial class Tests{
 static void EndingRecording(){
  Ending.LoadPic=(string n,byte id,bool m,byte k,ref Types.TImgBuf p)=>{log.Add("load:"+n+":"+id);p=new Types.TImgBuf{originX=id};};
  Ending.FreePic=(ref Types.TImgBuf p)=>{log.Add("freepic");p=null;};
  Ending.LoadAnim=(string n,byte id,byte key,ref Types.TAnimRec a)=>{log.Add("anim:"+id+":"+key);a.count=1;a.current=1;a.frames[1].time=0;a.frames[1].pic=new Types.TImgBuf{originX=id};};
  Ending.FreeAnim=(ref Types.TAnimRec a)=>{log.Add("freeanim");a.count=0;};
  Ending.BlitClipped=(x,y,t,k,p,d)=>log.Add("blit:"+x+":"+y+":"+p.originX);Ending.DisplayImage=(x,y,b,p)=>log.Add("image:"+x+":"+y+":"+p.originX);Ending.Flush=()=>log.Add("flush");Ending.DisplayInterface=()=>log.Add("ifc:"+GDVars.ifcState);
 }
 static void EndingChecks(){
  EndingRecording();foreach(byte frames in new byte[]{0,1,2,8})foreach(byte cycles in new byte[]{1,2,25}){
   log.Clear();Ending.LoadAnim=(string n,byte id,byte key,ref Types.TAnimRec a)=>{a.count=frames;a.current=1;for(int i=1;i<=frames;i++){a.frames[i].time=0;a.frames[i].pic=new Types.TImgBuf{originX=B(i)};}};Ending.PlayFinalAnim(-3,4,7,cycles);
   if(frames==0)Yes(log.SequenceEqual(new[]{"freeanim"}),"zero frames frees without draw");else{Yes(log[0]=="blit:-4:3:1"&&log[1]=="flush"&&log.Last()=="freeanim","initial copy flush and release");var images=log.Where(s=>s.StartsWith("image:")).ToArray();Yes(images.Length==frames*cycles,"exact frame cycles");for(int i=0;i<images.Length;i++)Yes(images[i]=="image:-3:4:"+(i%frames+1),"source 1-based frame order");}}
  foreach(byte frameCount in new byte[]{1,2}){Setup();EndingRecording();Ending.LoadAnim=(string n,byte id,byte key,ref Types.TAnimRec a)=>{a.count=frameCount;a.current=1;for(int i=1;i<=frameCount;i++){a.frames[i].time=frameCount==1?int.MaxValue:0;a.frames[i].pic=new Types.TImgBuf();}};Ending.PlayFinalAnim(1,1,2,0);Yes(log.Count(v=>v.StartsWith("image:"))==1,"zero cycles stops after first draw if first iteration does not finish a cycle");}
  Setup();EndingRecording();Ending.LoadAnim=(string n,byte id,byte key,ref Types.TAnimRec a)=>{a.count=1;a.current=1;a.frames[1].time=20;a.frames[1].pic=new Types.TImgBuf();};var threshold=new Queue<int>(new[]{0,0,11,11,12,12});TextRend.ReadBiosTicks=()=>threshold.Dequeue();Ending.PlayFinalAnim(1,1,2,1);Yes(log.Count(v=>v.StartsWith("image:"))==2,"strict greater-than frame deadline holds on equality");
  Setup();EndingRecording();var times=new Queue<int>(new[]{100,100,0,0});TextRend.ReadBiosTicks=()=>times.Dequeue();Ending.PlayFinalAnim(1,1,2,1);Yes(log.Count(s=>s.StartsWith("image:"))==1,"midnight causes frame advance");
  Setup();EndingRecording();foreach(bool animate in new[]{false,true}){
   log.Clear();var first=new Types.TImgBuf();var second=new Types.TImgBuf();GDVars.ds2dMapTilePtr=first;Ending.SecondSheet=()=>second;Ending.BuildWild=(x,y)=>log.Add("wild:"+x+":"+y);Ending.Remap=(from,to,frame,dither,p)=>log.Add("remap:"+frame+":"+(to==GDConsts.yarBlueGreenTo?"BG":"G")+":"+dither+":"+(p==first?1:2));
   Ending.YarashComplete(animate);var want=new List<string>();Action<bool> remaps=d=>{foreach(var pair in new[]{Tuple.Create(GDConsts.yarBlueGreenTiles,"BG"),Tuple.Create(GDConsts.yarGreenTiles,"G")})foreach(byte t in pair.Item1)want.Add("remap:"+(t&127)+":"+pair.Item2+":"+d+":"+(t>127?2:1));};
   if(animate){want.Add("wild:17:15");want.Add("flush");for(int i=0;i<9;i++){remaps(true);want.Add("wild:"+GDConsts.yarViewX[i]+":"+GDConsts.yarViewY[i]);want.Add("flush");}}remaps(false);if(animate){want.Add("wild:23:25");want.Add("flush");want.Add("pause:0");want.Add("pause:0");}Yes(log.SequenceEqual(want),"complete Yarash ordered trace/tiles/sheets/dither/pauses");
  }
  foreach(int members in new[]{0,1,2,3,4,8})foreach(byte delay in new byte[]{0,9,10,11,255}){
   Setup();EndingRecording();GDVars.ifcState=4;GDVars.virtualDisk=8;GDVars.gameDelay=delay;GDVars.zone1[C_Gbvm.PARTY_SIZE]=W(members);Ending.WinGame();
   var loads=log.Where(s=>s.StartsWith("load:")).ToArray();var expected=new List<string>{"load:FINAL8:1","load:FINAL8:3"};if(members>1)expected.Add("load:FINAL8:4");if(members>2)expected.Add("load:FINAL8:5");if(members>3)expected.Add("load:FINAL8:6");Yes(loads.SequenceEqual(expected),"member thresholds and disk name");
   int n=unchecked((byte)((ushort)((ushort)(10-delay)*5)/2));Yes(log.Count(s=>s=="image:3:5:2")== (n==0?256:n)&&log.Count(s=>s=="image:3:1:7")== (n==0?256:n)&&log.Count(s=>s=="image:3:1:8")==1,"Word then Byte cycle narrowing including zero256wrap");Yes(GDVars.ifcState==4&&log.IndexOf("ifc:7")>=0&&log.Last()=="ifc:4"&&log.Count(s=>s=="pause:"+delay*225)==3,"win restores interface after free and three pauses");
  }
  // Real DAX/Portrait/Blit dependency slice (no animation loader substitute).
  Setup();Ending.LoadAnim=Portrait.LoadAnimation;Ending.FreeAnim=Portrait.FreeAnimation;Ending.BlitClipped=Blit.BlitTileClipped;Ending.DisplayImage=Portrait.DisplayImageBuf;Ending.Flush=Blit.FlushView;Portrait.MaxAvailable=()=>1000000;
  GDVars.video.cellShift=3;GDVars.video.screenShift=3;GDVars.video.maskShift=3;GDVars.video.keyed=true;GDVars.video.colours=16;GDVars.video.ExpandFrames=(raw,off,m,c,p)=>PicVga.ExpandFrames(raw.Data.Select(x=>B(x)).ToArray(),off,m,c,p);GDVars.video.RemapColour=PicVga.RemapColour;
  var bytes=new byte[26];bytes[0]=1;bytes[5]=1;bytes[7]=1;bytes[13]=1;for(int j=0;j<8;j++)bytes[14+j]=B(j*34+1);for(int j=22;j<26;j++)bytes[j]=0x77;
  Archive("FINAL1",3,bytes);Blit.AllocPic(168,38,1,0,ref GDVars.dsViewImageBufferPtr);GDVars.clipFlag=1;Ending.PlayFinalAnim(3,3,3,1);Yes(GDVars.dsViewImageBufferPtr.data[16*304+16]==7,"real packed FINAL animation draws pixels");
 }
 static void Archive(string name,byte id,byte[] raw){var pack=new List<byte>();for(int at=0;at<raw.Length;at+=126){int n=Math.Min(126,raw.Length-at);pack.Add(B(n-1));pack.AddRange(raw.Skip(at).Take(n));}using(var w=new BinaryWriter(File.Create(Path.Combine(dir,name.ToLowerInvariant()+".dax")))){w.Write((ushort)9);w.Write(id);w.Write(0);w.Write(W(raw.Length));w.Write(W(pack.Count));w.Write(pack.ToArray());}DaxIo.ResetArchiveCache();GDVars.dsSuppressingInsertDiskFlag=true;}
}
