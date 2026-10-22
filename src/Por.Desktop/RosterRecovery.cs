using Por.Core;
namespace Por.Desktop;
// Host-only crash recovery. Validated standalone character files remain untouched; preserve the old roster before appending orphan names.
public static class RosterRecovery
{
 public static string[] Recover(string directory)
 {
  string roster=Path.Combine(directory,"charlist.txt");var files=Directory.EnumerateFiles(directory).ToArray();string existing=files.SingleOrDefault(p=>Path.GetFileName(p).Equals("charlist.txt",StringComparison.OrdinalIgnoreCase));if(existing!=null)roster=existing;
  if(new FileInfo(roster).LinkTarget!=null)throw new IOException("Character roster must not be a symlink.");
  var encoding=System.Text.Encoding.GetEncoding(28591);var old=File.Exists(roster)?File.ReadAllBytes(roster):Array.Empty<byte>();var text=encoding.GetString(old);var names=new HashSet<string>(text.Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries).Select(n=>n.StartsWith("* ")?n.Substring(2):n),StringComparer.OrdinalIgnoreCase);var recovered=new List<string>();
  foreach(string file in files.OrderBy(p=>p,StringComparer.Ordinal))
  {
   if(!Path.GetExtension(file).Equals(".cha",StringComparison.OrdinalIgnoreCase)||new FileInfo(file).LinkTarget!=null)continue;byte[] bytes=File.ReadAllBytes(file);if(bytes.Length!=285||bytes[0]==0||bytes[0]>15)continue;
   var chars=bytes.Skip(1).Take(bytes[0]).ToArray();if(chars.Any(c=>c<32||c>126))continue;string name=System.Text.Encoding.ASCII.GetString(chars);string basename=DaxIo.SanitizeFileName(DaxIo.SanitizeString(name,"."));if(!Path.GetFileNameWithoutExtension(file).Equals(basename,StringComparison.OrdinalIgnoreCase)||!names.Add(name))continue;recovered.Add(name);
  }
  if(recovered.Count==0)return Array.Empty<string>();
  if(File.Exists(roster)){string backup=roster+".before-recovery-"+Guid.NewGuid().ToString("N")+".bak";File.WriteAllBytes(backup,old);}
  string temp=roster+".recover-"+Guid.NewGuid().ToString("N");using(var output=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)){output.Write(old);if(old.Length>0&&old[old.Length-1]!=10&&old[old.Length-1]!=13)output.Write(new byte[]{13,10});foreach(string name in recovered)output.Write(System.Text.Encoding.ASCII.GetBytes(name+"\r\n"));output.Flush(true);}File.Move(temp,roster,true);return recovered.ToArray();
 }
}
