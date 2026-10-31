namespace Por.Desktop;
public static class EffectPack
{
 public static void Validate(byte[] pack)
 {
  if(pack.Length<6||pack.Length>0xFFF0||System.Text.Encoding.ASCII.GetString(pack,0,4)!="SBFX"||pack[4]!=2||pack[5]==0||6+7*pack[5]>pack.Length)throw new InvalidDataException("Invalid SBSFX.DAT v2 pack");
  int tableEnd=6+7*pack[5];var tunes=new HashSet<byte>();
  for(int i=6;i<tableEnd;i+=7){int length=pack[i+3]|pack[i+4]<<8,offset=pack[i+5]|pack[i+6]<<8;if(pack[i]<1||pack[i]>20||!tunes.Add(pack[i])||pack[i+1]>1||length<(pack[i+1]==1?2:1)||offset<tableEnd||offset+length>pack.Length)throw new InvalidDataException("Invalid SBSFX.DAT sample entry");}
 }
 public static string SelectMode(string requested,string dataDirectory)
 {
  if(requested!="auto")return requested;
  var packs=Directory.EnumerateFiles(dataDirectory).Where(f=>Path.GetFileName(f).Equals("SBSFX.DAT",StringComparison.OrdinalIgnoreCase)).ToArray();
  if(packs.Length>1)throw new IOException("Multiple SBSFX.DAT case variants in state directory");
  if(packs.Length==0)return "adlib";
  Validate(File.ReadAllBytes(packs[0]));return "blaster";
 }
 public static string Import(string source,string dataDirectory)
 {
  byte[] pack=File.ReadAllBytes(source);Validate(pack);
  var matches=Directory.EnumerateFiles(dataDirectory).Where(f=>Path.GetFileName(f).Equals("SBSFX.DAT",StringComparison.OrdinalIgnoreCase)).ToArray();
  if(matches.Length>1)throw new IOException("Multiple SBSFX.DAT case variants in state directory");
  string destination=matches.SingleOrDefault()??Path.Combine(dataDirectory,"SBSFX.DAT");
  if(new FileInfo(destination).LinkTarget!=null)throw new IOException("Effects destination must not be a symlink");
  if(File.Exists(destination)){if(File.ReadAllBytes(destination).SequenceEqual(pack))return destination;File.Copy(destination,destination+"."+Guid.NewGuid().ToString("N")+".bak");}
  File.WriteAllBytes(destination,pack);return destination;
 }
}
