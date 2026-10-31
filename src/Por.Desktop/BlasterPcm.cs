using Por.Core;
namespace Por.Desktop;
// Narrow virtual DSP/8237 bridge for the exact commands emitted by SndSb.
// No DOS physical memory access. DMA addresses refer only to allocated managed blocks.
public sealed class BlasterPcm : ISoundBlasterHost
{
 readonly List<SoundDmaBlock> blocks=new();readonly SpeakerPcm pcm;
 readonly int[] address=new int[4],length=new int[4],page=new int[4];
 int nextAddress=0x10000,channel=1,command,parameter,lo,time=156;bool high,ack,speaker;
 short[] wave=Array.Empty<short>();long phase;
 public long EffectsStarted {get;private set;}
 public bool SamplePlaying=>phase<wave.Length*(SpeakerPcm.SampleRate*(256L-time));
 public string BlasterEnvironment=>"A220 D1";
 public int MaxAvailable=>0x100000;
 public BlasterPcm(SpeakerPcm pcm){this.pcm=pcm;}
 public SoundDmaBlock Allocate(ushort size){var b=new SoundDmaBlock{Data=new byte[size],PhysicalAddress=nextAddress};nextAddress+=0x10000;blocks.Add(b);return b;}
 public void Free(SoundDmaBlock block)=>blocks.Remove(block);
 public byte ReadPort(ushort port)=>port switch{0x22C=>0,0x22E=>(byte)(ack?128:0),0x226=>0,0x22A=>Acknowledge(),_=>throw new NotSupportedException($"Blaster read {port:X}")};
 byte Acknowledge(){ack=false;return 0xAA;}
 public void WritePort(ushort port,byte value)
 {
  if(port==0x226){if(value==0){ack=true;Stop();command=parameter=0;}return;}
  if(port==0x22C){Dsp(value);return;}
  if(port==10){channel=value&3;return;}if(port==12){high=false;return;}if(port==11){if(value!=(0x48|channel))throw new NotSupportedException("Unsupported DMA mode");return;}
  int ch=Array.IndexOf(new ushort[]{0x87,0x83,0x81,0x82},port);if(ch>=0){page[ch]=value;return;}
  if(port<=7){ch=port/2;var array=(port&1)==0?address:length;array[ch]=high?(array[ch]&255)|(value<<8):value;high=!high;return;}
  throw new NotSupportedException($"Blaster write {port:X}");
 }
 void Dsp(byte value)
 {
  if(command==0x40){time=value;command=0;return;}
  if(command==0x14||command==0x75){if(parameter++==0){lo=value;return;}int count=1+lo+(value<<8);int format=command==0x75?1:0;command=parameter=0;StartDma(count,format);return;}
  switch(value){case 0xD0:Stop();break;case 0xD1:speaker=true;break;case 0x40:case 0x14:case 0x75:command=value;parameter=0;break;default:throw new NotSupportedException($"Unsupported DSP command {value:X}");}
 }
 void StartDma(int count,int format){int physical=(page[channel]<<16)|address[channel];var b=blocks.FirstOrDefault(b=>physical>=b.PhysicalAddress&&physical+count<=b.PhysicalAddress+b.Data.Length)??throw new InvalidOperationException("DSP transfer outside managed DMA block");if(count!=length[channel]+1)throw new InvalidOperationException("DSP/DMA transfer length mismatch");wave=Decode(b.Data.AsSpan(physical-b.PhysicalAddress,count),format);phase=0;EffectsStarted++;}
 // Creative 4-bit ADPCM: reference byte, high nibble first; four adaptive
 // quantizer levels; clamp in unsigned 8-bit space. Not IMA ADPCM.
 public static short[] Decode(ReadOnlySpan<byte> data,int format)
 {
  if(format==0){var result=new short[data.Length];for(int i=0;i<data.Length;i++)result[i]=(short)((data[i]-128)*128);return result;}
  if(format!=1||data.Length==0)throw new InvalidDataException("Unsupported/empty Sound Blaster sample");
  var decoded=new short[(data.Length-1)*2];int reference=data[0],scale=0,k=0;
  for(int i=1;i<data.Length;i++)for(int nib=1;nib>=0;nib--){int code=(data[i]>>(nib*4))&15,magnitude=code&7;int delta=scale==0?magnitude:(2*magnitude+1)<<(scale-1);reference=Math.Clamp(reference+((code&8)!=0?-delta:delta),0,255);decoded[k++]=(short)((reference-128)*128);if(magnitude>=5&&scale<3)scale++;else if(magnitude==0&&scale>0)scale--;}
  return decoded;
 }
 public int Sample(){int index=(int)(phase/(SpeakerPcm.SampleRate*(256L-time)));if(index>=wave.Length)return 0;phase+=1000000;return speaker&&pcm.Driver.State.OutputOn!=0?wave[index]:0;}
 public void Stop(){wave=Array.Empty<short>();phase=0;}
}
