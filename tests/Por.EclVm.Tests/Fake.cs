// Test double for the units EclVm calls that are not ported yet. EclVars' operand decoding, GetEclVar, EclGosub and
// EclCompareInt are re-implemented here from eclvars.pas/eclmenu.pas so that scripts can run end to end; string
// operands ($80) use a test convention (low byte = index into Fake.Strings) instead of SSI's packed 6-bit text.
// Everything else is recorded in Log and answered from configurable values.
using System;
using System.Collections.Generic;
using Por.Core;

class Fake : IEclVarsUnit, IEclMenuUnit, ISaveLoadUnit, ICharDispUnit, ICharHlpUnit, IFxEngineUnit, IExploreUnit,
    IEclVmGameUnits
{
    public List<string> Log = new List<string>();
    public Dictionary<int, ushort> Mem = new Dictionary<int, ushort>();
    public Dictionary<int, string> MemStr = new Dictionary<int, string>();
    public string[] Strings = new string[256];
    // operand counts SkipEclCmd uses for the opcodes the tests skip over
    public Dictionary<int, int> SkipOperands = new Dictionary<int, int>();

    public Queue<byte> HMenu = new Queue<byte>();
    public Queue<short> VMenu = new Queue<short>();
    public List<string> HMenuTexts = new List<string>();
    public byte MaxDist = 2;
    public byte HiMove = 12, LoMove = 6;
    public Queue<bool> Saves = new Queue<bool>();
    public Queue<bool> Hits = new Queue<bool>();
    public List<Tuple<string, ushort>> Damage = new List<Tuple<string, ushort>>();
    public Queue<char> MapCommands = new Queue<char>();
    public bool QuickLoad = false;
    public Types.TCharRecord Picked;
    public Types.TCharRecord CreatureTemplate;
    public Types.TItemRec CreatureItems;
    public Types.TEffectRec CreatureEffects;
    public HashSet<string> EffectHolders = new HashSet<string>();
    public Action OnAddCreature;

    static byte B(int x) { return unchecked((byte)x); }

    // ---- EclVars ----
    public void LoadEclVars(byte count)
    {
        Log.Add("LoadEclVars:" + count);
        byte strIdx = 0;
        for (int i = 1; i <= count; i++)
        {
            byte t = GDVars.zone3[GDVars.eclIp + 1];
            byte lo = GDVars.zone3[GDVars.eclIp + 2];
            byte hi = 0;
            GDVars.eclIp = unchecked((ushort)(GDVars.eclIp + 2));
            if (t == 1 || t == 2 || t == 3)
            {
                GDVars.eclIp++;
                hi = GDVars.zone3[GDVars.eclIp];
            }
            else if (t == 0x80)
            {
                if (strIdx < Types.GB_STRING_LAST) strIdx++;
                GDVars.gbStrings[strIdx] = Strings[lo] ?? "";
            }
            if (i <= 64)
            {
                GDVars.dsGbvmType[i] = t;
                GDVars.dsGbvmLowerByte[i] = lo;
                if (t == 1 || t == 2 || t == 3) GDVars.dsGbvmUpperByte[i] = hi;
            }
        }
        GDVars.eclIp++;
    }
    public ushort CombineBytesToWord(byte lowByte, byte highByte) { return (ushort)(lowByte + highByte * 256); }
    public ushort GetEclVar(sbyte idx)
    {
        byte lo = GDVars.dsGbvmLowerByte[idx], hi = GDVars.dsGbvmUpperByte[idx];
        switch (GDVars.dsGbvmType[idx])
        {
            case 0: return lo;
            case 1: case 3: return ReadGbvm((ushort)(hi * 256 + lo));
            case 0x80: return 0;
            default: return CombineBytesToWord(lo, hi);
        }
    }
    public void WriteGbvm(ushort address, ushort value) { Log.Add("W:" + address.ToString("X4") + "=" + value); Mem[address] = value; }
    public void WriteGbvmString(ushort address, string s) { Log.Add("WS:" + address.ToString("X4") + "=" + s); MemStr[address] = s; }
    public ushort ReadGbvm(ushort address) { ushort v; return Mem.TryGetValue(address, out v) ? v : (ushort)0; }
    public void SkipEclCmd()
    {
        byte op = GDVars.zone3[GDVars.eclIp];
        Log.Add("Skip:" + op.ToString("X2"));
        int n;
        if (SkipOperands.TryGetValue(op, out n) && n > 0) LoadEclVars(B(n));
        else GDVars.eclIp++;
    }

    // ---- EclMenu ----
    public void EclGosub(byte varIdx)
    {
        var top = new Types.TEclRet { addr = GDVars.eclIp, next = GDVars.dsEclStackTopPtr };
        if (GDVars.dsEclStackTopPtr != null) GDVars.dsEclStackSecondPtr = GDVars.dsEclStackTopPtr;
        GDVars.dsEclStackTopPtr = top;
        GDVars.eclIp = CombineBytesToWord(GDVars.dsGbvmLowerByte[varIdx], GDVars.dsGbvmUpperByte[varIdx]);
        Log.Add("Gosub:" + varIdx);
    }
    public void EclCompareStr(string s1, string s2)
    {
        Log.Add("CmpStr:" + s1 + "|" + s2);
        int c = string.CompareOrdinal(s1, s2);
        Flags(c == 0, c != 0, c < 0, c > 0, c <= 0, c >= 0);
    }
    public void EclCompareInt(ushort v1, ushort v2)
    {
        Log.Add("CmpInt:" + v1 + "," + v2);
        Flags(v1 == v2, v1 != v2, v1 < v2, v1 > v2, v1 <= v2, v1 >= v2);
    }
    static void Flags(params bool[] f) { for (int i = 0; i < 6; i++) GDVars.cmpFlags[i] = f[i]; }
    public byte MaxMonsterDist(byte x, byte y, byte facing) { Log.Add("MaxDist:" + x + "," + y + "," + facing); return MaxDist; }
    public void DisplayMonster(byte sprId, byte picId, byte dist, Types.TMonFlags flags) { Log.Add("Monster:" + sprId + "," + picId + "," + dist); }
    public void DisplayPortrait(byte headId, byte bodyId) { Log.Add("Portrait:" + headId + "," + bodyId); }
    public void EclVerticalMenu(string query, string menu, byte qCol, byte mCol, byte hlCol, sbyte ulCol, sbyte ulRow,
        sbyte lrCol, sbyte lrRow, Types.TItemRec list, bool addExit, ref bool redraw, ref short idx, ref Types.TItemRec sel)
    {
        var names = new List<string>();
        for (var p = list; p != null; p = p.next) names.Add(p.name);
        Log.Add("VMenu:" + ulRow + ":" + string.Join("/", names));
        idx = VMenu.Dequeue();
        sel = idx < 0 ? null : MenuCore.FindListMember(idx, list);
    }
    public byte MenuHorizontalCS(string query, string menu, byte qCol, byte mCol, byte hlCol, bool enterOk, bool animate)
    {
        HMenuTexts.Add(menu + "|" + enterOk + "|" + mCol + "|" + hlCol + "|" + animate + "|" + query);
        Log.Add("HMenu:" + menu);
        return HMenu.Dequeue();
    }
    public void EclInit() { Log.Add("EclInit"); }
    public void LoadEclScript(byte scriptId) { Log.Add("LoadEclScript:" + scriptId); }
    public void InitCreature(byte id, ref Types.TCharRecord creature, ref Types.TItemRec inv, ref Types.TEffectRec effect)
    {
        Log.Add("InitCreature:" + id);
        creature = CreatureTemplate; inv = CreatureItems; effect = CreatureEffects;
    }
    public void MoveExtremes(ref byte hiMove, ref byte loMove) { Log.Add("MoveExtremes"); hiMove = HiMove; loMove = LoMove; }
    public void RobMoney(double fraction, Types.TCharRecord creature) { Log.Add("RobMoney:" + fraction.ToString("R") + ":" + creature.name); }
    public void RobItems(byte chance, Types.TCharRecord creature) { Log.Add("RobItems:" + chance + ":" + creature.name); }
    public void EclInflictDamage(ushort dmg, Types.TCharRecord creature) { Log.Add("Dmg:" + creature.name + "=" + dmg); Damage.Add(Tuple.Create(creature.name, dmg)); }
    public void AddCreatureToParty(byte id) { Log.Add("AddCreature:" + id); if (OnAddCreature != null) OnAddCreature(); }
    public void CharFightsAlone(sbyte mirror) { Log.Add("FightsAlone:" + mirror); }
    public void EclMoveForward() { Log.Add("MoveForward"); }

    // ---- SaveLoad ----
    public void RemoveCreature(bool suppressSizeDec, bool freeAnim) { Log.Add("RemoveCreature:" + suppressSizeDec + "," + freeAnim); }
    public void CheckFileElseQuit(string name) { Log.Add("CheckFile:" + name); }

    // ---- CharDisp ----
    public void DisplayParty(Types.TCharRecord hl) { Log.Add("DisplayParty:" + (hl == null ? "nil" : hl.name)); }
    public void DisplayNoncombatInterface() { Log.Add("Noncombat"); }
    public void SelectChar(string query, bool addExit, ref Types.TCharRecord sel) { Log.Add("SelectChar:" + query + ":" + addExit); sel = Picked; }
    public void PrintMiscInfo() { Log.Add("MiscInfo"); }

    // ---- CharHlp ----
    public void PrependItemCopy(ref Types.TItemRec list, Types.TItemRec item)
    {
        var n = new Types.TItemRec { name = item.name, itemType = item.itemType, weight = item.weight, value = item.value, next = list };
        list = n;
        Log.Add("PrependItem:" + item.name + ":" + item.itemType);
    }
    public void PrependEffectCopy(ref Types.TEffectRec list, Types.TEffectRec effect)
    {
        list = new Types.TEffectRec { id = effect.id, duration = effect.duration, next = list };
        Log.Add("PrependEffect:" + effect.id);
    }
    public void ClearTreasure() { Log.Add("ClearTreasure"); }
    public bool HasEffect(Types.TCharRecord creature, byte id, ref Types.TEffectRec effect) { Log.Add("HasEffect:" + creature.name + ":" + id); return EffectHolders.Contains(creature.name); }
    public void ItemLabel(Types.TItemRec item, bool isInv) { Log.Add("ItemLabel:" + item.itemType); }
    public void RecalcStats(Types.TCharRecord creature) { Log.Add("Recalc:" + creature.name); }

    // ---- FxEngine ----
    public bool RollSavingThrow(Types.TCharRecord creature, sbyte saveType, sbyte modifier) { Log.Add("Save:" + creature.name + ":" + saveType + ":" + modifier); return Saves.Dequeue(); }
    public bool AttackRollNoAttacker(Types.TCharRecord defender, byte v) { Log.Add("Attack:" + defender.name + ":" + v); return Hits.Dequeue(); }
    public void AdvanceTime(byte amount, sbyte tUnit) { Log.Add("Time:" + amount + ":" + tUnit); }

    // ---- Explore ----
    public char MapMainMenu() { Log.Add("MapMenu"); return MapCommands.Dequeue(); }
    public void ResolveMovement() { Log.Add("Move"); GDVars.gMapX++; }
    public bool QuickLoadAsked { get { return QuickLoad; } }

    // ---- single-routine units ----
    public void ShopMainMenu() { Log.Add("Shop"); }
    public void VaultMenu() { Log.Add("Vault"); }
    public void TempleMainMenu() { Log.Add("Temple"); }
    public void PostCombat() { Log.Add("PostCombat"); }
    public void CombatMain() { Log.Add("Combat"); }
    public void CharGenMainMenu() { Log.Add("Training"); }
    public void WinGame() { Log.Add("WinGame"); }
    public void YarashComplete(bool animate) { Log.Add("Yarash:" + animate); }
    public void CampMainMenu(ref bool interrupted) { Log.Add("Camp"); interrupted = CampInterrupted; }
    public bool CampInterrupted;
    public void DisplayMapView() { Log.Add("MapView"); }
    public void SoundCommand(ushort cmd) { Log.Add("Sound:" + cmd); }

    public void Bind()
    {
        EclVm.Units = new EclVmUnits
        {
            EclVars = this, EclMenu = this, SaveLoad = this, CharDisp = this, CharHlp = this, FxEngine = this,
            Explore = this, Game = this
        };
    }
}
