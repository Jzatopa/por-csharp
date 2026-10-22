using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using Por.Core;
class Program
{
 static int assertions;
 static void Equal(bool value,string label){assertions++;if(!value)throw new Exception(label);}
 static void Main(){
  using(var document=JsonDocument.Parse(File.ReadAllText("evidence/full-port-2026-10-21/journal-source-oracle.json"))){
   var source=document.RootElement.GetProperty("Journal1");
   var counts=source.GetProperty("line_counts").EnumerateObject().ToDictionary(x=>int.Parse(x.Name),x=>x.Value.GetInt32());
   var texts=new Dictionary<(short,short),string>();
   foreach(var chunk in source.GetProperty("chunks").EnumerateArray())texts.Add((chunk.GetProperty("index").GetInt16(),chunk.GetProperty("chunk").GetInt16()),chunk.GetProperty("text").GetString());
   for(int i=short.MinValue;i<=short.MaxValue;i++){
    int count;counts.TryGetValue(i,out count);
    Equal(Journal1.LineCount((short)i)==count,"LineCount "+i);
    for(short c=0;c<10;c++){
     string expected;texts.TryGetValue(((short)i,c),out expected);
     Equal(Journal1.TextChunk((short)i,c)==(expected??""),"TextChunk "+i+":"+c);
    }
   }
   foreach(short index in Enumerable.Range(1,28).Select(x=>(short)x)){
    var lines=new List<string>();
    for(int c=short.MinValue;c<=short.MaxValue;c++){
     string expected;texts.TryGetValue((index,(short)c),out expected);
     string actual=Journal1.TextChunk(index,(short)c);
     Equal(actual==(expected??""),"all chunk indices "+index+":"+c);
     if(expected!=null){lines.AddRange(actual.Split('\r'));Equal(actual.Length<=255&&actual.All(ch=>ch<=255),"byte string");}
    }
    Equal(lines.Count==counts[index],"assembled lines "+index);
   }
   Equal(counts.Count==28&&texts.Count==71&&texts.Values.Sum(x=>x.Length)==12071,"source totals");
   Equal(typeof(Journal1).GetMethod("LineCount").ReturnType==typeof(short),"16-bit count API");
   Console.WriteLine("Journal1: entries=28 chunks=71 bytes=12071 assertions="+assertions+" failures=0");
  }
 }
}
