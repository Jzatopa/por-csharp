// Integration support for accepted CharDisp/CharHlp and the direct Temple/SoundDrv routes.
namespace Por.Core
{
    public static class EasiestTenBindings
    {
        public static void BindUnits()
        {
            EclBindings.BindConvertedUnits();
            EclVm.Units.CharDisp = new DisplayAdapter();
            EclVm.Units.CharHlp = new HelpersAdapter();
            DirectTenBindings.BindGameUnits();
        }
        private sealed class DisplayAdapter : ICharDispUnit
        {
            public void DisplayParty(Types.TCharRecord c) { CharDisp.DisplayParty(c); }
            public void DisplayNoncombatInterface() { CharDisp.DisplayNoncombatInterface(); }
            public void SelectChar(string query,bool exit,ref Types.TCharRecord c) { CharDisp.SelectChar(query,exit,ref c); }
            public void PrintMiscInfo() { CharDisp.PrintMiscInfo(); }
        }
        private sealed class HelpersAdapter : ICharHlpUnit
        {
            public void PrependItemCopy(ref Types.TItemRec list,Types.TItemRec item) { CharHlp.PrependItemCopy(ref list,item); }
            public void PrependEffectCopy(ref Types.TEffectRec list,Types.TEffectRec effect) { CharHlp.PrependEffectCopy(ref list,effect); }
            public void ClearTreasure() { CharHlp.ClearTreasure(); }
            public bool HasEffect(Types.TCharRecord c,byte id,ref Types.TEffectRec effect) { return CharHlp.HasEffect(c,id,out effect); }
            public void ItemLabel(Types.TItemRec item,bool inventory) { CharHlp.ItemLabel(item,inventory); }
            public void RecalcStats(Types.TCharRecord c) { CharHlp.RecalcStats(c); }
        }
    }
}
