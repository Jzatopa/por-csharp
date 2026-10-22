using System;using System.IO;using Por.Core;
public static class ArchiveTests{
 static int n;static void Eq(object a,object b){n++;if(!Equals(a,b))throw new Exception(a+" != "+b);}
 static byte[] Archive(ushort header,byte id,int off,ushort raw,ushort packed,byte[] payload){using(var m=new MemoryStream()){using(var w=new BinaryWriter(m)){w.Write(header);w.Write(id);w.Write(off);w.Write(raw);w.Write(packed);w.Write(payload);return m.ToArray();}}}
 public static void Main(){string p=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".dax");try{
 File.WriteAllBytes(p,Archive(9,255,0,1,2,new byte[]{0,33}));Eq(DataArc.FileLength(p),13);Eq(DataArc.ArchiveIntact(p),true);Eq(DataArc.HasBlock(p,255),true);Eq(DataArc.HasBlock(p,1),false);
 File.WriteAllBytes(p,Archive(9,255,-1,1,2,new byte[]{0,33}));Eq(DataArc.ArchiveIntact(p),false);
 File.WriteAllBytes(p,Archive(9,254,0,1,2,new byte[]{0,33}));Eq(DataArc.ArchiveIntact(p),false);Eq(DataArc.HasBlock(p,254),true);
 File.WriteAllBytes(p,new byte[]{9,0,255});Eq(DataArc.HasBlock(p,255),false);Eq(DataArc.ArchiveIntact(p),false);
 File.WriteAllBytes(p,Archive(10,255,0,1,2,new byte[]{0,33}));Eq(DataArc.ArchiveIntact(p),false);Eq(DataArc.HasBlock(p,255),true);
 File.Delete(p);Eq(DataArc.FileLength(p),-1);Eq(DataArc.ArchiveIntact(p),false);
 }finally{File.Delete(p);}Console.WriteLine("Archive assertions passed: "+n);}
}
