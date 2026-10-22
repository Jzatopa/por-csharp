using System;using System.Linq;using Por.Core;
class VideoColdTests{
 static long checks;static Random r=new Random(62271);static void Yes(bool b,string m){checks++;if(!b)throw new Exception(m);}
 static Types.TImgBuf P(int w,int h,int f,bool mask){return new Types.TImgBuf{width=(ushort)w,height=(ushort)h,count=(byte)f,imgSize=(ushort)(w*h*4),transp=mask?new Types.TBytes():null};}
 static int Pixel(byte[] data,int cell,int p){int c=0;for(int k=0;k<4;k++)c|=((data[cell*4+k]>>(7-p))&1)<<k;return c;}
 static int Reverse(int n){int b=0;for(int k=0;k<8;k++)b=(b<<1)|((n>>k)&1);return b;}
 static void Ega(){
  PicEga.InitRowTables();for(int i=0;i<200;i++)Yes(GDVars.fbRowOfs[i]==i*40,"EGA rows");
  for(int t=0;t<200;t++){
   int w=1+r.Next(6),h=1+r.Next(10),f=1+r.Next(3),off=r.Next(40),tc=r.Next(256),cells=w*h*f;bool mask=t%2==0;var pic=P(w,h,f,mask);r.NextBytes(pic.data);var old=(byte[])pic.data.Clone();byte[] raw=new byte[off+cells*4+9];r.NextBytes(raw);var immutable=(byte[])raw.Clone();PicEga.ExpandFrames(raw,(ushort)off,t%3==0,(byte)tc,pic);
   for(int c=0;c<cells;c++){int m=0;for(int p=0;p<8;p++){int value=(raw[off+c*4+p/2]>>(p%2==0?4:0))&15;bool transparent=mask&&(p%2==0?value==(tc&15):value==tc);if(transparent)m|=128>>p;int expected=transparent?0:Pixel(old,c,p)|value;Yes(Pixel(pic.data,c,p)==expected,"EGA expand OR/mask");}if(mask)Yes(pic.transp[c]==m,"EGA mask");}Yes(raw.SequenceEqual(immutable),"source immutable");
   var mirror=P(w,h,f,mask);PicEga.MirrorFrame(pic,mirror);for(int y=0;y<h;y++)for(int x=0;x<w*8;x++)Yes(Pixel(mirror.data,y*w+x/8,x%8)==Pixel(pic.data,y*w+(w*8-1-x)/8,(w*8-1-x)%8),"EGA mirror first frame");if(mask)for(int y=0;y<h;y++)for(int x=0;x<w;x++)Yes(mirror.transp[y*w+x]==Reverse(pic.transp[y*w+w-1-x]),"EGA mirror mask");for(int i=w*h*4;i<mirror.data.Length;i++)Yes(mirror.data[i]==0,"mirror tail untouched");
   var table=new Types.TColorTab();for(int k=0;k<16;k++)table[k]=(byte)r.Next(256);old=(byte[])pic.data.Clone();PicEga.RecolorFrame(ref table,pic);for(int c=0;c<w*h;c++)for(int p=0;p<8;p++)Yes(Pixel(pic.data,c,p)==(table[Pixel(old,c,p)]&15),"EGA recolor");for(int i=w*h*4;i<pic.data.Length;i++)Yes(pic.data[i]==old[i],"recolor first frame only");
  }
  for(int t=0;t<100;t++){
   int w=1+r.Next(5),h=1+r.Next(6),f=1+r.Next(3),off=r.Next(32),from=r.Next(256),to=r.Next(256),cells=w*h*f;var pic=P(w,h,f,false);var scratch=P(w,h,f,false);r.NextBytes(pic.data);r.NextBytes(scratch.data);var expected=(byte[])scratch.data.Clone();var original=(byte[])pic.data.Clone();bool dither=t%2==0;int draw=0;Func<int> next=()=>unchecked((byte)((draw++*73+17)&255));
   for(int c=0;c<cells;c++){int stochastic=dither?next()&next():255;for(int p=0;p<8;p++)if(((stochastic>>(7-p))&1)!=0){int colour=0;for(int k=0;k<4;k++)colour|=((pic.data[off+c*4+k]>>(7-p))&1)<<k;if(colour==(from&15))for(int k=0;k<4;k++){int bit=128>>p;expected[c*4+k]=(byte)((expected[c*4+k]&~bit)|(((to>>k)&1)*bit));}}}
   int actual=0;Dice.RandomBelow=n=>{Yes(n==256,"EGA dither bound");return (actual++*73+17)&255;};PicEga.RemapColour(pic,scratch,(ushort)off,(byte)f,(byte)from,(byte)to,dither);Yes(scratch.data.SequenceEqual(expected),"EGA remap");Yes(pic.data.SequenceEqual(original),"remap source immutable");Yes(actual==draw,"EGA random order");
  }
  var row=P(1,8,1,false);for(int t=0;t<256;t++){var px=new Types.TPixelRow();for(int p=0;p<8;p++)px[p]=unchecked((byte)(t+p*41));PicEga.PutPixelRow(row,16,3,ref px);for(int p=0;p<8;p++)Yes(Pixel(row.data,7,p)==(px[p]&15),"EGA pixel row");}
 }
 static void Tdy(){
  PicTdy.InitRowTables();int[] banks={C_Video.GFX_BANK0_SEG,C_Video.GFX_BANK1_SEG,C_Video.GFX_BANK2_SEG,C_Video.GFX_BANK3_SEG};for(int y=0;y<200;y++)Yes(GDVars.tandyRowSeg[y]==banks[y%4]+(y/4)*10,"Tandy row banks");
  for(int t=0;t<200;t++){
   int w=1+r.Next(5),h=1+r.Next(10),f=1+r.Next(3),off=r.Next(30),tc=r.Next(256),size=w*h*4,total=size*f;bool mask=t%2==0;var p=P(w,h,f,mask);byte[] raw=new byte[off+total+9];r.NextBytes(raw);var before=(byte[])raw.Clone();byte[] masks=new byte[total];if(mask)for(int i=0;i<total;i++){masks[i]=(byte)r.Next(256);p.transp[i]=masks[i];}PicTdy.ExpandFrames(raw,(ushort)off,t%3==0,(byte)tc,p);
   for(int i=0;i<total;i++){int m=mask?masks[i]:0;if(mask){if(raw[off+i]>>4==tc)m=240;if((raw[off+i]&15)==tc)m=(m+15)&255;Yes(p.transp[i]==m,"Tandy mask source accumulation");}Yes(p.data[i]==(raw[off+i]&~m),"Tandy expand");}Yes(raw.SequenceEqual(before),"Tandy immutable");
   var mirror=P(w,h,f,mask);PicTdy.MirrorFrame(p,mirror);for(int y=0;y<h;y++)for(int x=0;x<w*4;x++){int b=p.data[y*w*4+w*4-1-x];Yes(mirror.data[y*w*4+x]==((b>>4)|((b&15)<<4)),"Tandy mirror");if(mask){b=p.transp[y*w*4+w*4-1-x];Yes(mirror.transp[y*w*4+x]==((b>>4)|((b&15)<<4)),"Tandy mirror mask");}}Yes(mirror.data.Skip(size).All(v=>v==0),"Tandy mirror first frame only");
   var tab=new Types.TColorTab();for(int i=0;i<16;i++)tab[i]=(byte)r.Next(256);var old=(byte[])p.data.Clone();PicTdy.RecolorFrame(ref tab,p);for(int i=0;i<p.data.Length;i++)Yes(p.data[i]==(i<size?unchecked((byte)((tab[old[i]>>4]<<4)|tab[old[i]&15])):old[i]),"Tandy recolor first frame");
  }
  for(int t=0;t<200;t++){
   int w=1+r.Next(4),h=1+r.Next(5),f=1+r.Next(3),off=r.Next(32),from=r.Next(256),to=r.Next(256),size=w*h*f*4;bool dither=t%2==0;var p=P(w,h,f,false);var scratch=P(w,h,f,false);r.NextBytes(p.data);r.NextBytes(scratch.data);var original=(byte[])p.data.Clone();var want=(byte[])scratch.data.Clone();int draws=0;for(int i=0;i<size;i++){if((p.data[i+off]&240)==((from<<4)&255)){if(!dither||draws++%4==0)want[i]=unchecked((byte)((want[i]&15)|((to<<4)&255)));}if((p.data[i+off]&15)==from){if(!dither||draws++%4==0)want[i]=unchecked((byte)((want[i]&240)|to));}}int actual=0;Dice.RandomBelow=n=>{Yes(n==4,"Tandy dither bound");return actual++%4;};PicTdy.RemapColour(p,scratch,(ushort)off,(byte)f,(byte)from,(byte)to,dither);Yes(scratch.data.SequenceEqual(want),"Tandy remap");Yes(p.data.SequenceEqual(original),"Tandy source immutable");Yes(actual==draws,"Tandy conditional random calls");
  }
  var row=P(1,8,1,false);for(int t=0;t<256;t++){var px=new Types.TPixelRow();for(int x=0;x<8;x++)px[x]=unchecked((byte)(t+x*33));PicTdy.PutPixelRow(row,16,3,ref px);for(int x=0;x<8;x++)Yes(((row.data[28+x/2]>>(x%2==0?4:0))&15)==(px[x]&15),"Tandy pixel row");}
 }
 static Types.TImgBuf C(int w,int h,int f,bool mask){var p=P(w,h,f,mask);p.imgSize=(ushort)(w*h*2);for(int i=0;i<8;i++)p.map[i]=(byte)(((i*2%4)<<4)|((i*2+1)%4));return p;}
 static int Cpix(byte[] b,int cell,int p){return(b[cell*2+p/4]>>(6-2*(p%4)))&3;}
 static int Mapped(int c,Types.TColorMap map){return(map[c/2]>>(c%2==0?4:0))&15;}
 static void Cga(){
  PicCga.InitRowTables();for(int i=0;i<200;i++)Yes(GDVars.cgaRowSeg[i]==(i/2)*5+(i%2==0?C_Video.GFX_BANK0_SEG:C_Video.GFX_BANK1_SEG),"CGA row banks");GDVars.video.cellShift=1;GDVars.video.maskShift=0;GDVars.video.RemapColour=PicCga.RemapColour;
  for(int t=0;t<200;t++){
   int w=1+r.Next(5),h=1+r.Next(10),f=1+r.Next(3),off=r.Next(30),tc=r.Next(256),cells=w*h*f;bool mask=t%2==0;var p=C(w,h,f,mask);byte[] raw=new byte[off+cells*4+9];r.NextBytes(raw);r.NextBytes(p.data);var original=(byte[])p.data.Clone();byte[] masks=new byte[cells*2];if(mask)for(int i=0;i<masks.Length;i++){masks[i]=(byte)r.Next(256);p.transp[i]=masks[i];}PicCga.ExpandFrames(raw,(ushort)off,t%3==0,(byte)tc,p);
   for(int c=0;c<cells;c++)for(int pixel=0;pixel<8;pixel++){int value=(raw[off+c*4+pixel/2]>>(pixel%2==0?4:0))&15,byteAt=c*2+pixel/4,shift=6-2*(pixel%4);bool transparent=mask&&(pixel%2==0?value==(tc&15):value==tc);int m=mask?((masks[byteAt]>>shift)&3):0;if(transparent)m=3;int expected=(Cpix(original,c,pixel)|Mapped(value,p.map))&~m;Yes(Cpix(p.data,c,pixel)==expected,"CGA expansion preserves OR and preexisting masks");if(mask)Yes(((p.transp[byteAt]>>shift)&3)==m,"CGA mask pairs");}
   var mirror=C(w,h,f,mask);PicCga.MirrorFrame(p,mirror);for(int y=0;y<h;y++)for(int x=0;x<w*8;x++)Yes(Cpix(mirror.data,y*w+x/8,x%8)==Cpix(p.data,y*w+(w*8-1-x)/8,(w*8-1-x)%8),"CGA mirrored pixels");if(mask)for(int y=0;y<h;y++)for(int x=0;x<w*8;x++){int left=y*w*8+x,right=y*w*8+w*8-1-x;Yes(((mirror.transp[left/4]>>(6-2*(left%4)))&3)==((p.transp[right/4]>>(6-2*(right%4)))&3),"CGA mirrored mask");}
   var tab=new Types.TColorTab();for(int i=0;i<16;i++)tab[i]=(byte)(15-i);var before=(byte[])p.data.Clone();GDVars.picBytes=0;PicCga.RecolorFrame(ref tab,p);for(int c=0;c<w*h;c++)for(int pixel=0;pixel<8;pixel++)Yes(Cpix(p.data,c,pixel)==3-Cpix(before,c,pixel),"CGA noncascading recolor");Yes(p.data.Skip(p.imgSize).SequenceEqual(before.Skip(p.imgSize)),"CGA recolor first frame only");Yes(GDVars.picBytes==0,"CGA recolor scratch freed");
  }
  for(int t=0;t<150;t++){
   int w=1+r.Next(5),h=1+r.Next(8),f=1+r.Next(3),off=r.Next(32),from=r.Next(16),to=r.Next(16),length=w*h*f*2;bool dither=t%2==0;var p=C(w,h,f,false);var scratch=C(w,h,f,false);r.NextBytes(p.data);r.NextBytes(scratch.data);var original=(byte[])p.data.Clone();var want=(byte[])scratch.data.Clone();int draws=0;for(int i=0;i<length;i++)for(int pixel=0;pixel<4;pixel++){int shift=6-2*pixel;if(((p.data[i+off]>>shift)&3)==Mapped(from,scratch.map)){if(!dither||draws++%4==0)want[i]=(byte)((want[i]&~(3<<shift))|(Mapped(to,scratch.map)<<shift));}}int actual=0;Dice.RandomBelow=n=>{Yes(n==4,"CGA dither bound");return actual++%4;};PicCga.RemapColour(p,scratch,(ushort)off,(byte)f,(byte)from,(byte)to,dither);Yes(scratch.data.SequenceEqual(want),"CGA remap");Yes(p.data.SequenceEqual(original),"CGA remap immutable");Yes(actual==draws,"CGA random order");
  }
  var row=C(1,8,1,false);for(int t=0;t<256;t++){var px=new Types.TPixelRow();for(int x=0;x<8;x++)px[x]=unchecked((byte)(t+x*33));PicCga.PutPixelRow(row,16,3,ref px);for(int x=0;x<8;x++)Yes(((row.data[22+x/4]>>(6-2*(x%4)))&3)==GDConsts.attrTable[px[x]&15],"CGA pixel row attribute mapping");}
 }
 static void Main(){Ega();Tdy();Cga();Console.WriteLine("Video cold source-derived assertions: "+checks);}
}
