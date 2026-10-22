using Por.Core;
namespace Por.Desktop;
public sealed class BuildPlatform: IDataBuildPlatform
{
 readonly DesktopHost host;public BuildPlatform(DesktopHost h){host=h;}public long MaxAvailable=>host.AvailableBytes;
 public int DiskFree(byte drive)=>(int)Math.Min(int.MaxValue,new DriveInfo(Path.GetPathRoot(Directory.GetCurrentDirectory())).AvailableFreeSpace);
 public void ClearBottomLine(){TextRend.ClearBottomLine();}public void DisplayString(byte c,byte r,byte color,string text){TextRend.DisplayString(c,r,color,text);host.Pump();}
 public void Open(bool[] stale,bool writable){Console.WriteLine("STATE data-build");BldShow.Open(stale,writable);}public void Advance(ushort n){BldShow.Advance(n);host.Pump();}public void Reading(byte f,byte d){BldShow.Reading(f,d);}public void Writing(int n){BldShow.Writing(n);}public void Fixing(byte f,byte d){BldShow.Fixing(f,d);}public void Checking(){BldShow.Checking();}public void StartFamily(byte f){BldShow.StartFamily(f);}public void EndFamily(byte f,bool ok){BldShow.EndFamily(f,ok);host.Pump();}public void Finish(bool failed){BldShow.Finish(failed);Console.WriteLine("STATE data-built failed="+failed);}
}
