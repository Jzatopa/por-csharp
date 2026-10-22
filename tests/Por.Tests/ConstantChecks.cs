using System;
using System.IO;
using System.Reflection;
namespace PortVerification {
internal static class ConstantChecks {
 public static int Run(string path) {
  int count=0;
  foreach(string line in File.ReadAllLines(path)) {
   string[] parts=line.Split('\t');
   Type type=typeof(Por.Core.StrUtil).Assembly.GetType("Por.Core."+parts[0],true);
   FieldInfo field=type.GetField(parts[1],BindingFlags.Public|BindingFlags.Static);
   if(field==null)throw new Exception("Missing constant "+parts[0]+"."+parts[1]);
   long actual=Convert.ToInt64(field.GetValue(null)),expected=long.Parse(parts[2],System.Globalization.CultureInfo.InvariantCulture);
   if(actual!=expected)throw new Exception("Constant mismatch "+line+" got "+actual);count++;
  }
  return count;
 }
}
}
