using System; using Por.Core;
public static class SourcePointers { public static int Run() { int n=0;
 Check("TCharRecord", "effectsAddr", typeof(Types.TEffectRec)); n++;
 Check("TCharRecord", "itemsAddr", typeof(Types.TItemRec)); n++;
 Check("TCharRecord", "nextCharAddr", typeof(Types.TCharRecord)); n++;
 Check("TCharRecord", "combatAddr", typeof(Types.TCombatRec)); n++;
 Check("TCombatRec", "other", typeof(Types.TCharRecord)); n++;
 Check("TDownedRec", "cr", typeof(Types.TCharRecord)); n++;
 Check("TEffectRec", "next", typeof(Types.TEffectRec)); n++;
 Check("TMenuNode", "next", typeof(Types.TItemRec)); n++;
 Check("TItemRec", "next", typeof(Types.TItemRec)); n++;
 Check("TImgBuf", "transp", typeof(Types.TBytes)); n++;
 Check("TImgBuf", "more", typeof(Types.TImgBuf)); n++;
 Check("TPortSlot", "pic", typeof(Types.TImgBuf)); n++;
 Check("TEclRet", "next", typeof(Types.TEclRet)); n++;
 Check("TAnimFrame", "pic", typeof(Types.TImgBuf)); n++;
 Check("TStinkRec", "caster", typeof(Types.TCharRecord)); n++;
 Check("TStinkRec", "next", typeof(Types.TStinkRec)); n++;
return n; } static void Check(string r,string m,Type expected) {
 var t=typeof(Types).GetNestedType(r);var f=t.GetField(m);var p=t.GetProperty(m);var actual=f!=null?f.FieldType:p!=null?p.PropertyType:null;
 if(actual!=expected)throw new Exception(r+"."+m+" pointer type expected "+expected+" got "+actual);
 var obj=Activator.CreateInstance(t);if((f!=null?f.GetValue(obj):p.GetValue(obj))!=null)throw new Exception(r+"."+m+" pointer should start nil");
 }}
