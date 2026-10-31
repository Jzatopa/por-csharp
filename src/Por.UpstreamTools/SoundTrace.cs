using Por.Core;
namespace Por.UpstreamTools;
// Managed SndTrc: explicit caller-owned Word buffer, low port byte in its high byte.
public sealed class SoundTrace : ISoundPortBus
{
 ushort[] buffer=Array.Empty<ushort>();int capacity;
 public int Count {get;private set;}
 public void SetTraceBuffer(ushort[] destination,ushort words)
 {
  ArgumentNullException.ThrowIfNull(destination);
  if(words>destination.Length)throw new ArgumentOutOfRangeException(nameof(words));
  buffer=destination;capacity=words;Count=0;
 }
 public byte Read(ushort port)=>0; // TRACE PORT_IN macro reads zero, not a hardware probe.
 public void Write(ushort port,byte value)
 {
  if(Count>=capacity)return; // Original trace_out drops writes once full.
  buffer[Count++]=unchecked((ushort)((port&255)<<8|value));
 }
}
