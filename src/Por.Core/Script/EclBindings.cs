// Explicit adapters for the two converted units; does not install unported gameplay services.
using System;
namespace Por.Core
{
    public static class EclBindings
    {
        public static void BindConvertedUnits()
        {
            if (EclVm.Units == null) EclVm.Units = new EclVmUnits();
            EclVm.Units.EclVars = new VarsAdapter();
            // EvtKind (the Companion Tools map's event kinds) decodes through the same EclVars
            EvtKind.EclVarsUnit = EclVm.Units.EclVars;
            EclVm.Units.EclMenu = new MenuAdapter();
            EclVars.diskHook = DaxIo.PushState;
            EclVars.wallSetHook = View3D.LoadWallSet;
            EclVars.RecalcStats = creature =>
            {
                if (EclVm.Units.CharHlp == null) throw new NotSupportedException("CharHlp.RecalcStats is not bound.");
                EclVm.Units.CharHlp.RecalcStats(creature);
            };
        }
        private sealed class VarsAdapter : IEclVarsUnit
        {
            public void LoadEclVars(byte count) { EclVars.LoadEclVars(count); }
            public ushort CombineBytesToWord(byte lowByte, byte highByte) { return EclVars.CombineBytesToWord(lowByte, highByte); }
            public ushort GetEclVar(sbyte idx) { return EclVars.GetEclVar(idx); }
            public void WriteGbvm(ushort address, ushort value) { EclVars.WriteGbvm(address, value); }
            public void WriteGbvmString(ushort address, string s) { EclVars.WriteGbvmString(address, s); }
            public ushort ReadGbvm(ushort address) { return EclVars.ReadGbvm(address); }
            public void SkipEclCmd() { EclVars.SkipEclCmd(); }
        }
        private sealed class MenuAdapter : IEclMenuUnit
        {
            public void EclGosub(byte varIdx) { EclMenu.EclGosub(varIdx); }
            public void EclCompareStr(string s1, string s2) { EclMenu.EclCompareStr(s1, s2); }
            public void EclCompareInt(ushort v1, ushort v2) { EclMenu.EclCompareInt(v1, v2); }
            public byte MaxMonsterDist(byte x, byte y, byte facing) { return EclMenu.MaxMonsterDist(x, y, facing); }
            public void DisplayMonster(byte sprId, byte picId, byte dist, Types.TMonFlags flags) { EclMenu.DisplayMonster(sprId, picId, dist, flags); }
            public void DisplayPortrait(byte headId, byte bodyId) { EclMenu.DisplayPortrait(headId, bodyId); }
            public void EclVerticalMenu(string query, string menu, byte qCol, byte mCol, byte hlCol, sbyte ulCol, sbyte ulRow, sbyte lrCol, sbyte lrRow, Types.TItemRec list, bool addExit, ref bool redraw, ref short idx, ref Types.TItemRec sel) { EclMenu.EclVerticalMenu(query, menu, qCol, mCol, hlCol, ulCol, ulRow, lrCol, lrRow, list, addExit, ref redraw, ref idx, ref sel); }
            public byte MenuHorizontalCS(string query, string menu, byte qCol, byte mCol, byte hlCol, bool enterOk, bool animate) { return EclMenu.MenuHorizontalCS(query, menu, qCol, mCol, hlCol, enterOk, animate); }
            public void EclInit() { EclMenu.EclInit(); }
            public void LoadEclScript(byte scriptId) { EclMenu.LoadEclScript(scriptId); }
            public void InitCreature(byte id, ref Types.TCharRecord creature, ref Types.TItemRec inv, ref Types.TEffectRec effect) { EclMenu.InitCreature(id, ref creature, ref inv, ref effect); }
            public void MoveExtremes(ref byte hiMove, ref byte loMove) { EclMenu.MoveExtremes(ref hiMove, ref loMove); }
            public void RobMoney(double fraction, Types.TCharRecord creature) { EclMenu.RobMoney(fraction, creature); }
            public void RobItems(byte chance, Types.TCharRecord creature) { EclMenu.RobItems(chance, creature); }
            public void EclInflictDamage(ushort dmg, Types.TCharRecord creature) { EclMenu.EclInflictDamage(dmg, creature); }
            public void AddCreatureToParty(byte id) { EclMenu.AddCreatureToParty(id); }
            public void CharFightsAlone(sbyte mirror) { EclMenu.CharFightsAlone(mirror); }
            public void EclMoveForward() { EclMenu.EclMoveForward(); }
        }
    }
}
