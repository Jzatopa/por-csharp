using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Text.Json;using Por.Core;
class JournalVerification
{
 static int assertions;static void Yes(bool x,string s){assertions++;if(!x)throw new Exception(s);}
 static string Roman(int n){string[] ones={"","I","II","III","IV","V","VI","VII","VIII","IX"},tens={"","X","XX","XXX","XL","L","LX","LXX","LXXX","XC"},hundreds={"","C","CC","CCC","CD","D","DC","DCC","DCCC","CM"};return string.Concat(Enumerable.Repeat("M",n/1000))+hundreds[n/100%10]+tens[n/10%10]+ones[n%10];}
 static void Main()
 {
  using(var doc=JsonDocument.Parse(File.ReadAllText("evidence/full-port-2026-10-21/journal-source-oracle.json")))
  {
   Func<short,short>[] counts={Journal1.LineCount,Journal2.LineCount,Journal3.LineCount,Journal4.LineCount};Func<short,short,string>[] chunkFuncs={Journal1.TextChunk,Journal2.TextChunk,Journal3.TextChunk,Journal4.TextChunk};var allLines=new Dictionary<int,List<string>>();var allCounts=new Dictionary<int,int>();int unit=0,chunks=0,bytes=0;
   foreach(var property in doc.RootElement.EnumerateObject())
   {
    var expectedCounts=property.Value.GetProperty("line_counts").EnumerateObject().ToDictionary(p=>int.Parse(p.Name),p=>p.Value.GetInt32());var allChunks=new Dictionary<int,SortedDictionary<int,string>>();
    for(int i=short.MinValue;i<=short.MaxValue;i++){int n;expectedCounts.TryGetValue(i,out n);Yes(counts[unit]((short)i)==n,"source linecount "+property.Name+" "+i);}
    foreach(var c in property.Value.GetProperty("chunks").EnumerateArray()){short index=c.GetProperty("index").GetInt16(),chunk=c.GetProperty("chunk").GetInt16();string text=c.GetProperty("text").GetString();Yes(chunkFuncs[unit](index,chunk)==text,"source exact chunk "+property.Name+" "+index+":"+chunk);Yes(text.Length<=255&&text.All(ch=>ch<=255),"source byte shortstring chunk");if(!allChunks.ContainsKey(index))allChunks.Add(index,new SortedDictionary<int,string>());allChunks[index].Add(chunk,text);chunks++;bytes+=text.Length;}
    foreach(var kv in allChunks){var lines=kv.Value.Values.SelectMany(t=>t.Split('\r')).ToList();Yes(lines.Count==expectedCounts[kv.Key],"source chunks line count");allLines.Add(kv.Key,lines);allCounts.Add(kv.Key,lines.Count);foreach(short invalid in new short[]{short.MinValue,-1,16,short.MaxValue})Yes(chunkFuncs[unit]((short)kv.Key,invalid)=="","invalid chunk empty");}
    Yes(chunkFuncs[unit](0,0)==""&&chunkFuncs[unit](short.MaxValue,0)==""&&chunkFuncs[unit](short.MinValue,0)=="","invalid index no text");unit++;
   }
   Yes(unit==4&&chunks==267&&bytes==43296,"complete journal source aggregate");Yes(typeof(JrnlList).GetMethod("EntryKind").ReturnType==typeof(byte),"source EntryKind Byte API");int[] proclamations={59,64,78,101,109,110,114,120,126,129,134,154,156,170,190,201,204,214};var images=new HashSet<int>{22,28,33,40,46,47,55,59,60,76};
   for(int i=short.MinValue;i<=short.MaxValue;i++){short index=(short)i;int number=i>=1&&i<=18?proclamations[i-1]:i>=19&&i<=76?i-18:i>=77&&i<=99?i-76:0;int kind=i>=1&&i<=18?C_Jrnl.KIND_PROCLAMATION:i>=19&&i<=76?C_Jrnl.KIND_ENTRY:i>=77&&i<=99?C_Jrnl.KIND_TALE:C_Jrnl.KIND_NONE;int n;allCounts.TryGetValue(i,out n);string label=kind==1?Roman(number):kind==2?"ENTRY "+number:kind==3?"TALE "+number:"";string title=kind==1?"PROCLAMATION "+Roman(number):kind==2?"JOURNAL ENTRY "+number:kind==3?"TAVERN TALE "+number:"";
    Yes(JrnlList.EntryNumber(index)==number,"entry number "+i);Yes(JrnlList.EntryKind(index)==kind,"entry kind "+i);Yes(JrnlList.EntryLabel(index)==label,"entry label "+i);Yes(JrnlList.EntryTitle(index)==title,"entry title "+i);Yes(JrnlList.LineCount(index)==n,"list dispatched count "+i);Yes(JrnlList.IsImageOnly(index)==images.Contains(i),"image flag "+i);Yes(JrnlList.TextLine(index,short.MinValue)==""&&JrnlList.TextLine(index,0)==""&&JrnlList.TextLine(index,short.MaxValue)=="","invalid lines "+i);
    if(n>0)for(int line=1;line<=n;line++)Yes(JrnlList.TextLine(index,(short)line)==allLines[i][line-1],"source textline "+i+":"+line);
   }
   Console.WriteLine("Journal source units=4 entries="+allCounts.Count+" chunks="+chunks+" source bytes="+bytes);
  }
  Console.WriteLine("Journal assertions="+assertions+" failures=0");
 }
}
