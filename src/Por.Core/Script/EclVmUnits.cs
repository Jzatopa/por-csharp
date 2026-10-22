// FILE: src/Por.Core/Script/EclVmUnits.cs
// Explicit boundary between the converted EclVm (eclvm.pas) and the Pascal units it calls that are not yet ported.
// Each interface lists exactly the routines eclvm.pas uses from one unit, with the Pascal parameter widths kept.
// When a unit is ported, bind an adapter that forwards to it. An unbound unit fails clearly
// (NotSupportedException naming the unit); nothing here pretends to succeed.
// Authored by Claude (supervisor-approved exception to the Qwen-first route), from pinned source 7616920.
using System;

namespace Por.Core
{
    /// <summary>EclVars: script operand decoding and GBVM reads/writes.</summary>
    public interface IEclVarsUnit
    {
        // procedure LoadEclVars(count: Byte)
        void LoadEclVars(byte count);
        // function CombineBytesToWord(lowByte, highByte: Byte): Word
        ushort CombineBytesToWord(byte lowByte, byte highByte);
        // function GetEclVar(idx: ShortInt): Word
        ushort GetEclVar(sbyte idx);
        // procedure WriteGbvm(address, value: Word)
        void WriteGbvm(ushort address, ushort value);
        // procedure WriteGbvmString(address: Word; s: String)
        void WriteGbvmString(ushort address, string s);
        // function ReadGbvm(address: Word): Word
        ushort ReadGbvm(ushort address);
        // procedure SkipEclCmd
        void SkipEclCmd();
    }

    /// <summary>EclMenu: script support routines (menus, monsters, comparisons, creature loading).</summary>
    public interface IEclMenuUnit
    {
        void EclGosub(byte varIdx);
        void EclCompareStr(string s1, string s2);
        void EclCompareInt(ushort v1, ushort v2);
        // function MaxMonsterDist(x, y, facing: Byte): Byte
        byte MaxMonsterDist(byte x, byte y, byte facing);
        // procedure DisplayMonster(sprId, picId, dist: Byte; var flags: Types.TMonFlags)
        void DisplayMonster(byte sprId, byte picId, byte dist, Types.TMonFlags flags);
        void DisplayPortrait(byte headId, byte bodyId);
        // procedure EclVerticalMenu(query: TStr40; menu: String; qCol, mCol, hlCol: Byte; ulCol, ulRow, lrCol, lrRow:
        //   ShortInt; list: PItemRec; addExit: Boolean; var redraw: Boolean; var idx: Integer; var sel: PItemRec)
        void EclVerticalMenu(string query, string menu, byte qCol, byte mCol, byte hlCol, sbyte ulCol, sbyte ulRow,
                             sbyte lrCol, sbyte lrRow, Types.TItemRec list, bool addExit, ref bool redraw,
                             ref short idx, ref Types.TItemRec sel);
        // function MenuHorizontalCS(query: TStr40; menu: String; qCol, mCol, hlCol: Byte; enterOk, animate: Boolean): Byte
        byte MenuHorizontalCS(string query, string menu, byte qCol, byte mCol, byte hlCol, bool enterOk, bool animate);
        void EclInit();
        void LoadEclScript(byte scriptId);
        // procedure InitCreature(id: Byte; var creature: PCharRecord; var inv: PItemRec; var effect: PEffectRec)
        void InitCreature(byte id, ref Types.TCharRecord creature, ref Types.TItemRec inv, ref Types.TEffectRec effect);
        // procedure MoveExtremes(var hiMove, loMove: Byte)
        void MoveExtremes(ref byte hiMove, ref byte loMove);
        // procedure RobMoney(fraction: Real; ...). Real is TP's 6-byte real; see docs: the porter of EclMenu must keep
        // Real48 truncation behaviour, the fraction is passed here as the double nearest the computed value.
        void RobMoney(double fraction, Types.TCharRecord creature);
        void RobItems(byte chance, Types.TCharRecord creature);
        void EclInflictDamage(ushort dmg, Types.TCharRecord creature);
        void AddCreatureToParty(byte id);
        void CharFightsAlone(sbyte mirror);
        void EclMoveForward();
    }

    public interface ISaveLoadUnit
    {
        void RemoveCreature(bool suppressSizeDec, bool freeAnim);
        // procedure CheckFileElseQuit(name: TPathStr)  (String[79])
        void CheckFileElseQuit(string name);
    }

    public interface ICharDispUnit
    {
        void DisplayParty(Types.TCharRecord hl);
        void DisplayNoncombatInterface();
        // procedure SelectChar(query: TStr40; addExit: Boolean; var sel: PCharRecord)
        void SelectChar(string query, bool addExit, ref Types.TCharRecord sel);
        void PrintMiscInfo();
    }

    public interface ICharHlpUnit
    {
        void PrependItemCopy(ref Types.TItemRec list, Types.TItemRec item);
        void PrependEffectCopy(ref Types.TEffectRec list, Types.TEffectRec effect);
        void ClearTreasure();
        // function HasEffect(creature: PCharRecord; id: Byte; var effect: PEffectRec): Boolean
        bool HasEffect(Types.TCharRecord creature, byte id, ref Types.TEffectRec effect);
        void ItemLabel(Types.TItemRec item, bool isInv);
        void RecalcStats(Types.TCharRecord creature);
    }

    public interface IFxEngineUnit
    {
        bool RollSavingThrow(Types.TCharRecord creature, sbyte saveType, sbyte modifier);
        bool AttackRollNoAttacker(Types.TCharRecord defender, byte sixtyMinusThac0);
        void AdvanceTime(byte amount, sbyte tUnit);
    }

    public interface IExploreUnit
    {
        char MapMainMenu();
        void ResolveMovement();
        // var quickLoadAsked: Boolean
        bool QuickLoadAsked { get; }
    }

    /// <summary>The single-routine units eclvm.pas calls.</summary>
    public interface IEclVmGameUnits
    {
        void ShopMainMenu();            // Shop
        void VaultMenu();               // VaultMnu
        void TempleMainMenu();          // Temple
        void PostCombat();              // PostCbt
        void CombatMain();              // Combat
        void CharGenMainMenu();         // CharGen.MainMenu
        void WinGame();                 // Ending
        void YarashComplete(bool animate); // Ending
        void CampMainMenu(ref bool interrupted); // Camp
        void DisplayMapView();          // MapView
        void SoundCommand(ushort cmd);  // SoundDrv
    }

    /// <summary>The bindings EclVm uses. A null member means that unit is not ported or not bound yet.</summary>
    public sealed class EclVmUnits
    {
        public IEclVarsUnit EclVars;
        public IEclMenuUnit EclMenu;
        public ISaveLoadUnit SaveLoad;
        public ICharDispUnit CharDisp;
        public ICharHlpUnit CharHlp;
        public IFxEngineUnit FxEngine;
        public IExploreUnit Explore;
        public IEclVmGameUnits Game;
    }
}
