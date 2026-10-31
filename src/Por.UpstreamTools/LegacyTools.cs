using System.Buffers.Binary;
using System.Text.Json;
using System.Text.RegularExpressions;
using Por.Core;
namespace Por.UpstreamTools;

// Input files remain compatible; DOS installation paths are metadata, never host write destinations.
public sealed record LegacyConfig(char Video, char Sound, string GamePath, string SavePath, bool FullIntro)
{
 public string AudioMode => Sound switch {'P'=>"speaker",'T'=>"tandy",'A'=>"adlib",'S'=>"blaster",'N'=>"disabled",_=>throw new InvalidDataException("Unknown sound letter")};
 public static LegacyConfig Parse(string text)
 {
  var lines=text.Replace("\r","").Split('\n');
  if(lines.Length<5||lines[0].Length!=1||lines[1].Length!=1||lines[4].Length!=1)throw new InvalidDataException("Expected five POOL.CFG fields");
  char video=char.ToUpperInvariant(lines[0][0]),sound=char.ToUpperInvariant(lines[1][0]),intro=char.ToUpperInvariant(lines[4][0]);
  if(!"CETV".Contains(video)||!"PTASN".Contains(sound)||!"FN".Contains(intro))throw new InvalidDataException("Unknown POOL.CFG mode");
  return new(video,sound,lines[2],lines[3],intro=='F');
 }
 public void InstallEngineVideo()
 {
  switch(Video){case 'C':VidCga.Install();break;case 'E':VidEga.Install();break;case 'T':VidTdy.Install();break;case 'V':VidVga.Install();break;default:throw new InvalidDataException();}
 }
}

public static class LegacyTools
{
 public const int OverlayBuffer=100352;
 public static Dictionary<string,int> OverlaySizes(byte[] exe,string map)
 {
  if(exe.Length<10||exe[0]!='M'||exe[1]!='Z')throw new InvalidDataException("Expected unpacked MZ image");
  int header=BinaryPrimitives.ReadUInt16LittleEndian(exe.AsSpan(8))*16;
  if(header>exe.Length)throw new InvalidDataException("Truncated MZ header");
  var sizes=new Dictionary<string,int>();
  foreach(string line in map.Split('\n')){
   var m=Regex.Match(line,@"^\s*([0-9A-F]{5})H\s+[0-9A-F]{5}H\s+([0-9A-F]{5})H\s+(\w+)\s+CODE");
   if(!m.Success||Convert.ToInt32(m.Groups[2].Value,16)==0)continue;
   int start=checked(header+Convert.ToInt32(m.Groups[1].Value,16));
   if(start<0||start+2>exe.Length)throw new InvalidDataException("Segment outside image");
   if(exe[start]!=0xCD||exe[start+1]!=0x3F)continue;
   if(start+12>exe.Length)throw new InvalidDataException("Truncated overlay stub");
   sizes[m.Groups[3].Value]=BinaryPrimitives.ReadUInt16LittleEndian(exe.AsSpan(start+8))+BinaryPrimitives.ReadUInt16LittleEndian(exe.AsSpan(start+10));
  }
  return sizes;
 }
 public static int? OverlayLoads(JsonElement build)
 {
  if(!build.TryGetProperty("checkpoints",out var checkpoints)||!checkpoints.TryGetProperty("overlay-loads",out var loads)||!loads.TryGetProperty("text",out var lines))return null;
  foreach(var line in lines.EnumerateArray()) {var m=Regex.Match(line.GetString(),@"overlay loads (\d+)",RegexOptions.IgnoreCase);if(m.Success)return int.Parse(m.Groups[1].Value);}
  return null;
 }
 public static IEnumerable<string> OverlayReport(string json)
 {
  using var doc=JsonDocument.Parse(json);
  foreach(var run in doc.RootElement.EnumerateObject().OrderBy(p=>p.Name,StringComparer.Ordinal)){
   int? golden=run.Value.TryGetProperty("golden",out var g)?OverlayLoads(g):null,candidate=run.Value.TryGetProperty("candidate",out var c)?OverlayLoads(c):null;
   if(golden!=null||candidate!=null)yield return $"{run.Name,-12} golden {golden?.ToString()??"None"}  candidate {candidate?.ToString()??"None"}"+(golden!=null&&candidate!=null?$"  ({candidate-golden:+0;-0;+0})":"");
  }
 }
}
