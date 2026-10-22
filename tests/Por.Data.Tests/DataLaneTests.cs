using System;using System.IO;using Por.Core;
public static class DataLaneTests {
 static int count; static void Eq(object a,object b){count++;if(!object.Equals(a,b))throw new Exception("Mismatch "+a+" vs "+b);}
 static string SanRef(string a,string b){a=a.Substring(0,Math.Min(255,a.Length));b=b.Substring(0,Math.Min(255,b.Length));int lim=(a.Length-b.Length+1)&255;for(int pos=1;pos<=lim;pos++){int at=pos-1;string piece=at>=a.Length?"":a.Substring(at,Math.Min(b.Length,a.Length-at));if(piece==b&&at<a.Length)a=a.Remove(at,Math.Min(b.Length,a.Length-at));}return a;}
 static byte[] Ref(byte[] a,int len){var o=new byte[len];int s=0,d=0;while(s<a.Length&&d<len){int b=a[s++];if(b<127){int n=Math.Min(Math.Min(b+1,a.Length-s),len-d);for(int i=0;i<n;i++)o[d++]=a[s++];}else if(b>128&&s<a.Length){int n=Math.Min(256-b,len-d);byte v=a[s++];for(int i=0;i<n;i++)o[d++]=v;}}return o;}
 static void Check(byte[] a,int len){var x=DaxIo.DecompressDax(a,len);var y=Ref(a,len);Eq(x.Length,y.Length);for(int i=0;i<len;i++)Eq(x[i],y[i]);}
 public static void Main(){Eq(StrUtil.NumStr(int.MinValue),"-2147483648");Eq(StrUtil.ZeroPad2(65535),"35");Eq(StrUtil.PadLeft("abcd",2),"ab");Eq(StrUtil.PadRight("a",3),"a  ");Eq(StrUtil.PadLeft("a",3),"  a");
 for(int c=0;c<256;c++)for(int n=0;n<12;n++)Check(new byte[]{(byte)c,42,43,44},n);var r=new Random(392);for(int i=0;i<2000;i++){var a=new byte[r.Next(128)];r.NextBytes(a);Check(a,r.Next(256));}
 for(int k=0;k<1000;k++){string a="",b="";for(int j=r.Next(40);j>0;j--)a+=(char)('a'+r.Next(3));for(int j=r.Next(6);j>0;j--)b+=(char)('a'+r.Next(3));Eq(DaxIo.SanitizeString(a,b),SanRef(a,b));}Eq(DaxIo.SanitizeString("aaaa","a"),"aa");Eq(DaxIo.SanitizeString("abc","abcde"),"abc");
 var p=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".dat");try{var b=new byte[]{1,2,3};Eq(FileUtil.WriteRecord(p,b,3),true);var z=new byte[3];Eq(FileUtil.ReadRecord(p,z,3),true);Eq(z[2],(byte)3);Eq(FileUtil.ReadRecord(p,new byte[2],2),false);FileUtil.DeleteFile(p);Eq(File.Exists(p),false);Eq(FileUtil.ReadRecord(p,b,3),false);}finally{File.Delete(p);}
 string[] prefix={"ECL","GEO","WALLDEF","PIC","SPRIT","CPIC","FINAL","8X8D","ITEM","MON","MON","MON","HEAD","BODY"};string[] suffix={"","","","","","","","","","CHA","ITM","SPC","",""};for(byte f=1;f<=14;f++)for(byte d=1;d<=8;d++){string expected=prefix[f-1]+d+suffix[f-1];Eq(DataArc.SourceName(f,d),expected);byte dd=0;Eq(DataArc.MatchFamily(expected.ToLowerInvariant(),ref dd),f);Eq(dd,d);} byte disk=99;Eq(DataArc.MatchFamily("mon5cha",ref disk),(byte)10);Eq(disk,(byte)5);Eq(DataArc.MatchFamily("invalid",ref disk),(byte)0);Eq(disk,(byte)5);Eq(DataArc.SourceName(14,8),"BODY8");Eq(DataArc.MergedName(10),"MONCHA");
 Console.WriteLine("DataLane independent assertions passed: "+count);
 }
}
