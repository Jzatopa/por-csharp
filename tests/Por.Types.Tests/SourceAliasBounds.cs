using System; using System.Reflection; using Por.Core;
public static class SourceAliasBounds {
 public static int Run(){int n=0;
 Check("TIconCols",new int[]{1},new int[]{6});n++;
 Check("TSpellSlots",new int[]{0},new int[]{20});n++;
 Check("TBytes",new int[]{0},new int[]{65000});n++;
 Check("TColorMap",new int[]{0},new int[]{7});n++;
 Check("TPixelRow",new int[]{0},new int[]{7});n++;
 Check("TColorTab",new int[]{0},new int[]{15});n++;
 Check("TTargetList",new int[]{1},new int[]{72});n++;
 Check("TTurnUndeadTable",new int[]{1,1},new int[]{10,10});n++;
 Check("TGbvmZone0",new int[]{18688},new int[]{19711});n++;
 Check("TGbvmZone1",new int[]{27392},new int[]{28415});n++;
 Check("TGbStrings",new int[]{1},new int[]{20});n++;
 Check("TBaseItems",new int[]{0},new int[]{128});n++;
 Check("TCgaBank",new int[]{0,0},new int[]{99,79});n++;
 Check("TTandyBank",new int[]{0,0},new int[]{49,159});n++;
 Check("TClock",new int[]{0},new int[]{6});n++;
 Check("TAcAdjust",new int[]{0},new int[]{4});n++;
 Check("TByteArr",new int[]{0},new int[]{65534});n++;
 Check("TCellBytes",new int[]{0},new int[]{7});n++;
 Check("TMonFlags",new int[]{0},new int[]{1});n++;
 Check("TZone2",new int[]{38656},new int[]{39167});n++;
 Check("TZone3",new int[]{C_Gbvm.SCRIPT_FIRST},new int[]{C_Gbvm.SCRIPT_LAST});n++;
 Check("TMoney",new int[]{0},new int[]{6});n++;
 Check("TCgaRow",new int[]{0},new int[]{39});n++;
 Check("TFontArr",new int[]{0,0},new int[]{176,7});n++;
 Check("TTdyRow",new int[]{0},new int[]{159});n++;
 Check("TGeoPlane",new int[]{0,0},new int[]{15,15});n++;
 Check("TView3dWallDef",new int[]{1,0},new int[]{5,155});n++;
 Check("TWallSlices",new int[]{1,0},new int[]{15,155});n++;
return n;}
 static void Check(string name,int[] lo,int[] hi){
 var t=typeof(Types).GetNestedType(name);var o=Activator.CreateInstance(t);var a=(Array)t.GetField("Data").GetValue(o);var idx=t.GetProperty("Item");
 if(a.GetType().GetElementType().IsArray){if(a.Length!=hi[0]+1)throw new Exception(name+" outer padding");for(int i=lo[0];i<=hi[0];i++)if(((Array)a.GetValue(i)).Length!=hi[1]+1)throw new Exception(name+" inner shape");}
 else {if(a.Rank!=hi.Length)throw new Exception(name+" rank");for(int d=0;d<hi.Length;d++)if(a.GetLength(d)!=hi[d]+1)throw new Exception(name+" source shape dim"+d);}
 object[] low=new object[lo.Length],high=new object[hi.Length];for(int d=0;d<lo.Length;d++){low[d]=lo[d];high[d]=hi[d];}idx.GetValue(o,low);idx.GetValue(o,high);
 for(int d=0;d<lo.Length;d++){var bad=(object[])low.Clone();bad[d]=lo[d]-1;Reject(idx,o,bad,name);bad[d]=hi[d]+1;Reject(idx,o,bad,name);}
 }
 static void Reject(PropertyInfo p,object o,object[] indices,string name){try{p.GetValue(o,indices);}catch(TargetInvocationException ex)when(ex.InnerException is IndexOutOfRangeException || ex.InnerException is ArgumentOutOfRangeException){return;}throw new Exception(name+" invalid label accepted");}
}
