using System.Text.Json;
namespace Por.UpstreamTools;
public static class Program
{
 public static int Main(string[] args)
 {
  try{
   if(args.Length==0)throw new ArgumentException("Commands: config FILE | scenario FILE | ovlsizes EXE MAP | ovlloads REPORT | sndbench [T|P] [TICKS] | vidbench [C|E|T] [TICKS]");
   int ticks=args.Length>2&&(args[0]=="sndbench"||args[0]=="vidbench")?int.Parse(args[2]):54;
   switch(args[0]){
    case "config":Console.WriteLine(JsonSerializer.Serialize(LegacyConfig.Parse(File.ReadAllText(args[1]))));break;
    case "scenario":Console.WriteLine(JsonSerializer.Serialize(Scenario.Parse(File.ReadAllText(args[1]))));break;
    case "ovlsizes":foreach(var size in LegacyTools.OverlaySizes(File.ReadAllBytes(args[1]),File.ReadAllText(args[2])).OrderByDescending(p=>p.Value))Console.WriteLine($"{size.Key} {size.Value}");break;
    case "ovlloads":foreach(string row in LegacyTools.OverlayReport(File.ReadAllText(args[1])))Console.WriteLine(row);break;
    case "sndbench":{var host=new ManagedSoundBenchmarkHost();Console.WriteLine(JsonSerializer.Serialize(new{scope="managed sequencer/RAM bus, not DOS cycles or physical playback",rows=SoundBenchmark.Run(host,args.Length>1?args[1][0]:'P',ticks),host.Writes,host.Interrupts}));break;}
    case "vidbench":{var host=new ManagedVideoBenchmarkHost();Console.WriteLine(JsonSerializer.Serialize(new{scope="managed source primitives/RAM graphics bus, not DOS or native display throughput",rows=VideoBenchmark.Run(host,args.Length>1?args[1][0]:'C',ticks),host.Writes,host.PortWrites}));break;}
    default:throw new ArgumentException("Unknown command: "+args[0]);
   }
   return 0;
  }catch(Exception ex){Console.Error.WriteLine(ex);return 2;}
 }
}
