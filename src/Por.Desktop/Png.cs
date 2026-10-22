using System.Buffers.Binary;using System.IO.Compression;
namespace Por.Desktop;
internal static class Png
{
 static void Word(Stream s,int n){Span<byte>b=stackalloc byte[4];BinaryPrimitives.WriteInt32BigEndian(b,n);s.Write(b);}
 static void Chunk(Stream s,string name,byte[] bytes){Word(s,bytes.Length);var tag=System.Text.Encoding.ASCII.GetBytes(name);s.Write(tag);s.Write(bytes);uint crc=0xffffffff;foreach(byte b in tag.Concat(bytes)){crc^=b;for(int k=0;k<8;k++)crc=(crc>>1)^((crc&1)!=0?0xedb88320u:0);}Word(s,unchecked((int)~crc));}
 public static void Save(string path,uint[] pixels,int w,int h)
 {
  using var s=File.Create(path);s.Write(new byte[]{137,80,78,71,13,10,26,10});using var head=new MemoryStream();Word(head,w);Word(head,h);head.Write(new byte[]{8,2,0,0,0});Chunk(s,"IHDR",head.ToArray());using var data=new MemoryStream();using(var z=new ZLibStream(data,CompressionLevel.SmallestSize,true)){for(int y=0;y<h;y++){z.WriteByte(0);for(int x=0;x<w;x++){uint p=pixels[y*w+x];z.WriteByte((byte)(p>>16));z.WriteByte((byte)(p>>8));z.WriteByte((byte)p);}}}Chunk(s,"IDAT",data.ToArray());Chunk(s,"IEND",Array.Empty<byte>());
 }
}
