using System.Runtime.InteropServices;
namespace Por.Desktop;
// OPL2-compatible mode of Nuked-OPL3. Timers are modeled for the driver's detection
// handshake; rendering is handled by the chip emulator, not an approximation of FM.
public sealed class OplPcm : IDisposable
{
 [DllImport("por_opl")] static extern IntPtr por_opl_create();
 [DllImport("por_opl")] static extern void por_opl_destroy(IntPtr p);
 [DllImport("por_opl")] static extern void por_opl_write(IntPtr p,int r,int v);
 [DllImport("por_opl")] static extern int por_opl_sample(IntPtr p);
 IntPtr chip;byte address,status;
 void Ensure(){if(chip!=IntPtr.Zero)return;chip=por_opl_create();if(chip==IntPtr.Zero)throw new OutOfMemoryException("OPL emulator allocation failed");}
 public byte Read()=>status;
 public void Write(ushort port,byte value){if(port==0x388){address=value;return;}Ensure();if(address==4){if((value&0x80)!=0)status=0;else if((value&0x41)==1)status=0xC0;}por_opl_write(chip,address,value);}
 public int Sample()=>chip==IntPtr.Zero?0:por_opl_sample(chip);
 public void Dispose(){if(chip==IntPtr.Zero)return;por_opl_destroy(chip);chip=IntPtr.Zero;}
}
