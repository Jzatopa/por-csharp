// CbtActs checks, part 1: the shared environment (fake CbtMap/FxEngine/Magic, logging UI call points, a seeded dice
// stream) and independent oracle models of every cbtacts.pas routine. The oracles were written from the Pascal text of
// src/cbtacts.pas, not from Rules/CbtActs.cs. Each check builds a scenario twice from one seed, runs the oracle on the
// first copy and the production routine on the second, and compares results, the ordered log of side-effecting calls and
// a snapshot of every creature and global the unit touches.
using System;
using System.Collections.Generic;
using System.Linq;
using Por.Core;
using static Por.Core.Types;

static partial class Tests
{
    // ---------- environment ----------
    static List<string> cbL = new List<string>();
    static void CbLog(string s) { cbL.Add(s); }
    static uint cbSeed;
    static int CbDR(int s) { unchecked { cbSeed = cbSeed * 1103515245u + 12345u; } if (s <= 0) return 0; return (int)((cbSeed >> 16) % (uint)s); }
    static byte ORoll(int n, int s) { int t = 0; for (int i = 0; i < n; i++) t += CbDR(s) + 1; return unchecked((byte)t); }
    static Dictionary<int, byte> cbTerr = new Dictionary<int, byte>();
    static HashSet<TCharRecord> cbHidden = new HashSet<TCharRecord>();
    static HashSet<int> cbBlocked = new HashSet<int>();
    static int cbHitOn = 11;                       // AttackRoll hits when the d20 stream gives >= this
    static Action<byte, TCharRecord> cbHook;       // effect side effects of TriggerEffectsWithCondition
    static Action<TCharRecord> cbHp0Hook;
    static Queue<KeyValuePair<byte, bool>> cbKeys = new Queue<KeyValuePair<byte, bool>>();
    static Queue<byte> cbYes = new Queue<byte>();
    static TCharRecord cbHoldVictim; static bool cbHoldNil;
    static Queue<byte> cbSpellPick = new Queue<byte>(); static TItemRec cbScroll; static bool cbUseResult; static bool cbCastNcResult;
    static int cbMenuLimit;

    static int CbSlot(TCharRecord c) { for (int i = 1; i <= GDVars.arenaCount; i++) if (GDVars.creatureArr[i] == c) return i; return 0; }
    static int Cheb(int x1, int y1, int x2, int y2) { return Math.Max(Math.Abs(x1 - x2), Math.Abs(y1 - y2)); }
    static string Nm(object c) { return c == null ? "nil" : ((TCharRecord)c).name; }

    class CbMap : ICbtActsCombatMap
    {
        public void SetCombatPalette() { CbLog("pal"); }
        public bool ViewCoordsValid(sbyte x, sbyte y) { return x >= 0 && x <= 6 && y >= 0 && y <= 6; }
        public bool CombatIconVisible(TCharRecord c, byte whole) { return !cbHidden.Contains(c); }
        public void ScrollView(sbyte x, sbyte y, byte d, byte dir) { CbLog("Scroll " + x + "," + y + " d" + d + " dir" + dir); }
        public sbyte CombatCellX(TCharRecord c) { return GDVars.arenaLoc[CbSlot(c)].x; }
        public sbyte CombatCellY(TCharRecord c) { return GDVars.arenaLoc[CbSlot(c)].y; }
        public byte CombatSlot(TCharRecord c) { return (byte)CbSlot(c); }
        public void RedrawArenaCell(sbyte x, sbyte y) { CbLog("Redraw " + x + "," + y); }
        public void BuildArenaArrays() { CbLog("BuildArena"); }
        public void GetArenaContents(sbyte x, sbyte y, ref byte cr, ref byte terr)
        {
            cr = 0;
            for (int i = 1; i <= GDVars.arenaCount; i++)
                if (GDVars.arenaLoc[i].size > 0 && GDVars.arenaLoc[i].x == x && GDVars.arenaLoc[i].y == y) cr = (byte)i;
            byte t; terr = cbTerr.TryGetValue(x * 100 + y, out t) ? t : (byte)5;
        }
        public void UpdateCombatMapTile(sbyte x, sbyte y, byte cr) { CbLog("Tile " + x + "," + y + " " + cr); }
        public void SetCreatureFacing(TCharRecord c, byte dir, byte f, byte e) { CbLog("Face " + c.name + " " + dir + " " + f + " " + e); if (e == 0) c.combatAddr.direction = dir; }
        public byte GetIconSize(TCharRecord c) { return 1; }
        public void CreatureDowned(TCharRecord c) { CbLog("Downed " + c.name); }
        public void DisplayMapViewCentered(TCharRecord c, byte d, bool cur) { CbLog("Center " + c.name + " " + d + " " + cur); }
        public void StartSpellPreview(TAimShape s, byte r, byte cell) { CbLog("Preview " + s + " " + r + " " + cell); }
        public void StopSpellPreview() { CbLog("StopPreview"); }
        public byte CountTargets(TCharRecord c, byte range)
        {
            int n = 0; int cs = CbSlot(c);
            for (int i = 1; i <= GDVars.arenaCount; i++)
            {
                var p = GDVars.creatureArr[i];
                if (p.enabled != 0 && GDVars.arenaLoc[i].size > 0 && p.hostile == CharHlp.EnemyTeam(c)
                    && Cheb(GDVars.arenaLoc[i].x, GDVars.arenaLoc[i].y, GDVars.arenaLoc[cs].x, GDVars.arenaLoc[cs].y) <= range)
                { n++; GDVars.dsPotentialTargetCreature[n] = (byte)i; }
            }
            return (byte)n;
        }
        public byte CalcDistance(TCharRecord c, TCharRecord o)
        {
            int a = CbSlot(c), b = CbSlot(o);
            return (byte)Cheb(GDVars.arenaLoc[a].x, GDVars.arenaLoc[a].y, GDVars.arenaLoc[b].x, GDVars.arenaLoc[b].y);
        }
    }

    class CbFx : ICbtActsEffects
    {
        public void TriggerEffectsWithCondition(byte cond, TCharRecord c) { CbLog("T" + cond + " " + c.name); if (cbHook != null) cbHook(cond, c); }
        public bool AttackRoll(TCharRecord a, TCharRecord d, byte ac) { CbLog("AR " + a.name + ">" + d.name + " ac" + ac); return CbDR(20) + 1 >= cbHitOn; }
        public void PutOutOfPlayWithStatus(TCharRecord c, byte s, string m) { CbLog("OutOfPlay " + c.name + " " + s + " " + m); c.enabled = 0; c.status = s; }
        public void RemoveInvisibility(TCharRecord c) { CbLog("Uninvis " + c.name); }
        public void RemoveEffectsHp0(TCharRecord c) { CbLog("Hp0 " + c.name); if (cbHp0Hook != null) cbHp0Hook(c); }
        public void RemoveEffect(TCharRecord c, byte id, object e) { CbLog("RemoveEffect " + c.name + " " + id + " " + (e == null ? "nil" : "eff")); }
        public TCharRecord HoldTarget(TCharRecord c, object p, bool removed, byte id, ref bool released)
        {
            CbLog("Hold " + c.name + " " + removed + " " + id);
            GDVars.dsOtherCreatureCurrentPtr = cbHoldVictim;
            return cbHoldNil ? null : cbHoldVictim;
        }
    }

    class CbMagic : ICbtActsMagic
    {
        public byte CastSpellMenu(ref int idx, ref bool notEmpty, ref TItemRec scroll) { CbLog("CastMenu " + idx + " " + notEmpty); scroll = cbScroll; return cbSpellPick.Dequeue(); }
        public void UseItemSpell(TItemRec item, byte spell, bool announce, ref bool done) { CbLog("UseItemSpell " + item.name + " " + spell + " " + announce + " " + done); done = cbUseResult; }
        public void CastSpellNC(byte spell, bool b, bool ai, ref bool sel) { CbLog("CastNC " + spell + " " + b + " " + ai + " " + sel); sel = cbCastNcResult; }
    }

    static CbMap CM = new CbMap();
    static CbFx CF = new CbFx();
    static CbMagic CG = new CbMagic();

    static void CbInstall()
    {
        CbtActs.CombatMap = CM; CbtActs.Effects = CF; CbtActs.MagicUnit = CG;
        Dice.RandomBelow = s => CbDR(s);
        TextRend.DelayMilliseconds = ms => CbLog("Delay " + ms);
        CbtActs.PrintNameStringCall = (c, s, r, p) => CbLog("PNS " + c.name + "|" + s + "|" + r + "|" + p);
        CbtActs.DisplayCreatureNameCall = (c, col, r, p) => CbLog("DCN " + c.name + " " + col + " " + r + " " + p);
        CbtActs.ClearMessageWindowCall = () => CbLog("CMW");
        CbtActs.CombatPrintCreatureInfoCall = c => CbLog("Info " + c.name);
        CbtActs.PrintNameStringAnimCall = (c, e, s) => CbLog("PNSA " + c.name + " " + e + " " + s);
        CbtActs.PrintBottomLineCall = s => CbLog("Bottom " + s);
        CbtActs.DisplayCombatInterfaceCall = () => CbLog("CombatIfc");
        CbtActs.CopyIconFacingCall = (a, b, c, m) => CbLog("CopyIcon " + a + " " + b + " " + c + " " + m);
        CbtActs.BuildIconFacingsCall = a => CbLog("BuildIcon " + a);
        CbtActs.CombatAnimateMovingIconCall = (a, b, c, d, f, p) => CbLog("Fly " + a + "," + b + ">" + c + "," + d + " " + f + " " + p);
        CbtActs.DisplayMessageWindowCall = (a, b, c, d, col, cl, s) => { CbLog("DMW " + a + " " + b + " " + c + " " + d + " " + col + " " + cl + " " + s); GDVars.curRow = (byte)(b + 1); };
        CbtActs.PauseOnlyCall = () => CbLog("Pause");
        CbtActs.DisplayStringCall = (c, r, col, s) => CbLog("DS " + c + " " + r + " " + col + " " + s);
        CbtActs.ClearBottomLineCall = () => CbLog("ClearBottom");
        CbtActs.MenuHorizontalCall = CbMenu;
        CbtActs.YesNoMenuCall = (q, a, b, c) => { CbLog("YesNo " + q + " " + a + " " + b + " " + c); return cbYes.Count > 0 ? cbYes.Dequeue() : (byte)'N'; };
        CbtActs.ClearBoxCall = (a, b, c, d) => CbLog("ClearBox " + a + " " + b + " " + c + " " + d);
        CbtActs.SoundCommandCall = s => CbLog("Snd " + s);
        CbtActs.SoundSetAutoStopCall = s => CbLog("AutoStop " + s);
        CbtActs.InflictDamageCall = CbInflict;
        CbtActs.RemoveSpellCall = (c, s) => CbLog("RemoveSpell " + c.name + " " + s);
        CbtActs.RecalcStatsCall = c => CbLog("Recalc " + c.name);
        CbtActs.CountTeamsCall = () => CbLog("CountTeams");
        CbtActs.PrependItemCopyCall = CbPrepend;
        CbtActs.RemoveItemCall = (c, it) => { CbLog("RemoveItem " + c.name + " " + it.name); for (int k = 0; k < c.equipped.Length; k++) if (c.equipped[k] == it) c.equipped[k] = null; };
        CbtActs.WalkLineCall = CbWalk;
        CbtActs.BuildTargetListCall = CbBuildTargets;
        CbtActs.TargetAreaCall = (x, y, r) => CbLog("TargetArea " + x + "," + y + " " + r);
        CbtActs.DexMissileAdjustCall = c => (sbyte)(c.dexCur - 15);
    }

    static byte CbMenu(string q, string m, byte a, byte b, byte c, bool re, bool an, ref bool ext)
    {
        CbLog("Menu " + q + "[" + m + "] " + a + " " + b + " " + c + " " + re + " " + an + " arrows" + MenuCore.arrowsToCaller);
        if (++cbMenuLimit > 400) throw new Exception("menu loop runaway");
        var k = cbKeys.Count > 0 ? cbKeys.Dequeue() : new KeyValuePair<byte, bool>((byte)'E', false);
        ext = k.Value; return k.Key;
    }
    static void CbInflict(TCharRecord c, byte dmg)
    {
        CbLog("Inflict " + c.name + " " + dmg);
        if (dmg >= c.hpCur) { int over = dmg - c.hpCur; c.hpCur = 0; c.enabled = 0; c.status = (byte)(over > 9 ? C_Status.DEAD : over > 0 ? C_Status.DYING : 4); }
        else c.hpCur = (byte)(c.hpCur - dmg);
    }
    static void CbPrepend(ref TItemRec list, TItemRec item)
    {
        CbLog("Prepend " + item.name);
        var n = new TItemRec { name = item.name + "'", itemType = item.itemType, quantity = item.quantity, equipped = item.equipped, next = list };
        list = n;
    }
    static bool CbWalk(sbyte x, sbyte y, ref sbyte tx, ref sbyte ty, ref ushort dist, TArena view)
    {
        CbLog("Walk " + x + "," + y + ">" + tx + "," + ty + " " + dist);
        bool ok = !cbBlocked.Contains(tx * 100 + ty);
        dist = (ushort)(2 * Cheb(x, y, tx, ty) + (ok ? 0 : 1));
        return ok;
    }
    static void CbBuildTargets(sbyte x, sbyte y, ushort range, byte filter, byte size, TArena view)
    {
        CbLog("BuildTargets " + x + "," + y + " " + range + " " + filter + " " + size);
        int n = 0;
        for (int i = 1; i <= GDVars.arenaCount; i++)
            if (GDVars.arenaLoc[i].size > 0 && Cheb(GDVars.arenaLoc[i].x, GDVars.arenaLoc[i].y, x, y) <= range)
            {
                n++;
                GDVars.targetList[n].idx = (byte)i; GDVars.targetList[n].dist = (byte)Cheb(GDVars.arenaLoc[i].x, GDVars.arenaLoc[i].y, x, y); GDVars.targetList[n].dir = 0;
            }
        // stale entries past the count must not be read
        for (int i = n + 1; i <= 72; i++) GDVars.targetList[i].idx = 99;
        GDVars.targetCount = (byte)n;
    }

    // Run a CbtActs group without leaking its globals into the groups registered after it (they expect, for example,
    // roundCounter 0 and their own dice and delay hosts).
    static void CbGuarded(Action group)
    {
        var rb = Dice.RandomBelow; var dm = TextRend.DelayMilliseconds; bool arrows = MenuCore.arrowsToCaller;
        var z1 = GDVars.zone1; var z0 = GDVars.zone0; var bi = GDVars.baseItems; var cc = GDVars.curChar; byte dc = GDVars.downedCount;
        var dn = (TDownedRec[])GDVars.downed.Clone(); var tr = GDVars.treasureItems; var lm = GDVars.lastMissileItem; var hm = (byte[])GDVars.hitsMade.Clone();
        byte pd = GDVars.pendingDamage, gdd = GDVars.gDamageDice, em = GDVars.elementMask, rc = GDVars.roundCounter, tc = GDVars.targetCount, stc = GDVars.spellTargetCount;
        bool drf = GDVars.doneRoundFlag, st = GDVars.stingArg, ui = GDVars.usingItem;
        var tl = (TCombatTarget[])GDVars.targetList.Data.Clone(); var tlv = tl.Select(t => t == null ? null : new TCombatTarget { idx = t.idx, dist = t.dist, dir = t.dir }).ToArray();
        var dtp = (TCharRecord[])GDVars.dsTargetCreaturePtr.Clone(); var dpt = (byte[])GDVars.dsPotentialTargetCreature.Clone();
        byte gm = GDVars.gMultiTarget, hp = GDVars.hostileHpPct, cur = GDVars.dsCurrentAttackCurrent, fl = GDVars.dsCurrentCreatureCalculatingFlag, sup = GDVars.dsCurrentSuppressTargetingFlag, row = GDVars.curRow, ac = GDVars.arenaCount;
        sbyte gx = GDVars.gTargetX, gy = GDVars.gTargetY;
        var sheet = GDVars.dsMoveIconSheet; var other = GDVars.dsOtherCreatureCurrentPtr; var arena = GDVars.arenaPtr; var cl = GDVars.creatureList;
        var ca = (TCharRecord[])GDVars.creatureArr.Clone(); var al = (TArenaLoc[])GDVars.arenaLoc.Clone();
        var cbFields = typeof(CbtActs).GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                                      .Where(f => !f.IsLiteral && !f.IsInitOnly).ToArray();
        var cbValues = cbFields.Select(f => f.GetValue(null)).ToArray();
        try { group(); }
        finally
        {
            Dice.RandomBelow = rb; TextRend.DelayMilliseconds = dm; MenuCore.arrowsToCaller = arrows;
            GDVars.zone1 = z1; GDVars.zone0 = z0; GDVars.baseItems = bi; GDVars.curChar = cc; GDVars.downedCount = dc;
            Array.Copy(dn, GDVars.downed, dn.Length); GDVars.treasureItems = tr; GDVars.lastMissileItem = lm; Array.Copy(hm, GDVars.hitsMade, hm.Length);
            GDVars.pendingDamage = pd; GDVars.gDamageDice = gdd; GDVars.elementMask = em; GDVars.roundCounter = rc; GDVars.targetCount = tc; GDVars.spellTargetCount = stc;
            GDVars.doneRoundFlag = drf; GDVars.stingArg = st; GDVars.usingItem = ui;
            for (int i = 0; i < tl.Length; i++) { GDVars.targetList.Data[i] = tl[i]; if (tl[i] != null) { tl[i].idx = tlv[i].idx; tl[i].dist = tlv[i].dist; tl[i].dir = tlv[i].dir; } }
            Array.Copy(dtp, GDVars.dsTargetCreaturePtr, dtp.Length); Array.Copy(dpt, GDVars.dsPotentialTargetCreature, dpt.Length);
            GDVars.gMultiTarget = gm; GDVars.hostileHpPct = hp; GDVars.dsCurrentAttackCurrent = cur; GDVars.dsCurrentCreatureCalculatingFlag = fl; GDVars.dsCurrentSuppressTargetingFlag = sup; GDVars.curRow = row; GDVars.arenaCount = ac;
            GDVars.gTargetX = gx; GDVars.gTargetY = gy;
            GDVars.dsMoveIconSheet = sheet; GDVars.dsOtherCreatureCurrentPtr = other; GDVars.arenaPtr = arena; GDVars.creatureList = cl;
            Array.Copy(ca, GDVars.creatureArr, ca.Length); Array.Copy(al, GDVars.arenaLoc, al.Length);
            for (int i = 0; i < cbFields.Length; i++) cbFields[i].SetValue(null, cbValues[i]);
        }
    }

    // ---------- world ----------
    const byte BOW = 40, ARMOR_PLATE = 51;
    static TCharRecord Cr(string name, int hostile, int x, int y)
    {
        var c = new TCharRecord { name = name, hostile = (sbyte)hostile, enabled = 1, status = C_Status.OKAY, hpCur = 20, hpMax = 20, combatAddr = new TCombatRec() };
        c.dexCur = 15; c.iconSize = 1;
        cbPos[c] = new KeyValuePair<int, int>(x, y);
        return c;
    }
    static Dictionary<TCharRecord, KeyValuePair<int, int>> cbPos = new Dictionary<TCharRecord, KeyValuePair<int, int>>();
    static TItemRec Item(string name, int type, int qty) { return new TItemRec { name = name, itemType = (byte)type, quantity = (byte)qty, equipped = 1 }; }

    static void CbReset(uint seed)
    {
        CbInstall();
        cbL.Clear(); cbSeed = seed; cbTerr.Clear(); cbHidden.Clear(); cbBlocked.Clear(); cbHitOn = 11; cbHook = null; cbHp0Hook = null;
        cbKeys.Clear(); cbYes.Clear(); cbHoldVictim = null; cbHoldNil = false; cbSpellPick.Clear(); cbScroll = null; cbUseResult = false; cbCastNcResult = false;
        cbMenuLimit = 0; cbPos.Clear();
        GDVars.zone1 = new TGbvmZone1(); GDVars.zone0 = new TGbvmZone0State();
        var bi = new TBaseItems();
        for (int i = 0; i <= 128; i++) { bi[i].range = 1; bi[i].rateOfFire = 1; bi[i].dmgLgRolls = 1; bi[i].dmgLgDice = 12; bi[i].dmgSmMod = 1; bi[i].dmgLgMod = 3; }
        bi[BOW].range = 8; bi[BOW].props = C_Item.PROP_FIRES_AMMO | C_Item.PROP_ARROWS; bi[BOW].rateOfFire = 4;
        bi[C_Item.DART].range = 4; bi[C_Item.DART].props = C_Item.PROP_THROWN | C_Item.PROP_STR_BONUS; bi[C_Item.DART].rateOfFire = 6;
        bi[C_Item.HAND_AXE].range = 3; bi[C_Item.HAND_AXE].props = C_Item.PROP_THROWN | C_Item.PROP_STR_BONUS;
        bi[C_Item.SLING].range = 6; bi[C_Item.SLING].props = C_Item.PROP_FIRES_AMMO | C_Item.PROP_DEX_MISSILE; bi[C_Item.SLING].rateOfFire = 0;
        bi[C_Item.JAVELIN].range = 2; bi[C_Item.JAVELIN].props = C_Item.PROP_THROWN;   // ranged, damaging, not throwable (no STR bonus)
        bi[C_Item.HOLY_WATER_VIAL].range = 3; bi[C_Item.HOLY_WATER_VIAL].props = C_Item.PROP_THROWN | C_Item.PROP_STR_BONUS;
        bi[C_Item.SPEAR].range = 0;                                                      // range 0: CombatAimMenu's 255 wrap
        GDVars.baseItems = bi;
        GDVars.curChar = null; GDVars.downedCount = 0;
        for (int i = 0; i < GDVars.downed.Length; i++) GDVars.downed[i] = new TDownedRec();
        GDVars.treasureItems = null; GDVars.lastMissileItem = null;
        GDVars.hitsMade[1] = GDVars.hitsMade[2] = 0; GDVars.pendingDamage = 0; GDVars.gDamageDice = 0; GDVars.elementMask = 9;
        GDVars.doneRoundFlag = false; GDVars.roundCounter = 0; GDVars.stingArg = false; GDVars.targetCount = 0;
        for (int i = 0; i < GDVars.dsTargetCreaturePtr.Length; i++) GDVars.dsTargetCreaturePtr[i] = null;
        for (int i = 0; i < GDVars.dsPotentialTargetCreature.Length; i++) GDVars.dsPotentialTargetCreature[i] = 0;
        GDVars.spellTargetCount = 0; GDVars.gMultiTarget = 0; GDVars.gTargetX = 0; GDVars.gTargetY = 0; GDVars.hostileHpPct = 77;
        GDVars.dsMoveIconSheet = new TImgBuf { imgSize = 64 }; GDVars.usingItem = false; GDVars.dsOtherCreatureCurrentPtr = null;
        GDVars.dsCurrentAttackCurrent = 0; GDVars.dsCurrentCreatureCalculatingFlag = 7; GDVars.dsCurrentSuppressTargetingFlag = 0; GDVars.curRow = 0;
        CbtActs.attacksMade[1] = CbtActs.attacksMade[2] = 0; oAttacksMade[1] = oAttacksMade[2] = 0;
        MenuCore.arrowsToCaller = false;
        GDVars.arenaPtr = new TArena { ox = 0, oy = 0 };
        for (int i = 0; i <= Types.ARENA_SLOTS; i++) { GDVars.creatureArr[i] = null; GDVars.arenaLoc[i] = new TArenaLoc(); }
        GDVars.arenaLoc[0].x = 0; GDVars.arenaLoc[0].y = 0;
        GDVars.arenaCount = 0; GDVars.creatureList = null;
    }
    static void Place(params TCharRecord[] cs)
    {
        GDVars.arenaCount = (byte)cs.Length;
        for (int i = 0; i < cs.Length; i++)
        {
            GDVars.creatureArr[i + 1] = cs[i];
            GDVars.arenaLoc[i + 1].x = (sbyte)cbPos[cs[i]].Key; GDVars.arenaLoc[i + 1].y = (sbyte)cbPos[cs[i]].Value; GDVars.arenaLoc[i + 1].size = 1;
            cs[i].nextCharAddr = i + 1 < cs.Length ? cs[i + 1] : null;
        }
        GDVars.creatureList = cs.Length > 0 ? cs[0] : null;
    }

    static List<string> CbSnap()
    {
        var s = new List<string>();
        var seen = new List<TCharRecord>();
        for (int i = 1; i <= GDVars.arenaCount; i++) seen.Add(GDVars.creatureArr[i]);
        for (var p = GDVars.creatureList; p != null; p = p.nextCharAddr) if (!seen.Contains(p)) seen.Add(p);
        foreach (var c in seen)
        {
            var k = c.combatAddr;
            s.Add(c.name + " en" + c.enabled + " st" + c.status + " h" + c.hostile + " hp" + c.hpCur + " att" + c.curAttacks + "/" + c.curAttacks2
                  + " rolls" + c.curRollsA[1] + "," + c.curRollsA[2] + " dice" + c.curDiceA[1] + "," + c.curDiceA[2] + " mod" + c.curModA[1] + "," + c.curModA[2]);
            s.Add(c.name + " cast" + k.spellCast + " ca" + k.castAllowed + " iu" + k.itemUseAllowed + " ini" + k.initiative + " ph" + k.attackPhase
                  + " sw" + k.sweepBudget + " mv" + k.movement + " g" + k.guarding + " ma" + k.madeAttack + " dir" + k.direction + " oth" + Nm(k.other)
                  + " fc" + k.flankCount + " tu" + k.turned + " ut" + k.usedTurnUndead + " fs" + k.flankSpread);
            string eq = "";
            for (int e = 0; e < c.equipped.Length; e++) if (c.equipped[e] != null) eq += e + ":" + c.equipped[e].name + "x" + c.equipped[e].quantity + " ";
            s.Add(c.name + " eq " + eq);
            for (var f = c.effectsAddr; f != null; f = f.next) s.Add(c.name + " eff " + f.id + " s" + f.strength);
        }
        for (int i = 1; i <= GDVars.arenaCount; i++) s.Add("loc" + i + " " + GDVars.arenaLoc[i].x + "," + GDVars.arenaLoc[i].y + " sz" + GDVars.arenaLoc[i].size);
        s.Add("pd" + GDVars.pendingDamage + " gdd" + GDVars.gDamageDice + " em" + GDVars.elementMask + " hits" + GDVars.hitsMade[1] + "," + GDVars.hitsMade[2]
              + " am" + CbtActs.attacksMade[1] + "," + CbtActs.attacksMade[2]);
        s.Add("cur" + GDVars.dsCurrentAttackCurrent + " flag" + GDVars.dsCurrentCreatureCalculatingFlag + " sup" + GDVars.dsCurrentSuppressTargetingFlag
              + " done" + GDVars.doneRoundFlag + " curChar" + Nm(GDVars.curChar));
        s.Add("stc" + GDVars.spellTargetCount + " multi" + GDVars.gMultiTarget + " tgt" + GDVars.gTargetX + "," + GDVars.gTargetY + " hpct" + GDVars.hostileHpPct
              + " allies" + GDVars.zone1[C_Gbvm.ATTACKED_ALLIES] + " dist" + GDVars.arenaPtr.distMode + " curs" + GDVars.arenaPtr.cursorOn + "/" + GDVars.arenaPtr.cursorSize);
        string t = ""; for (int i = 1; i <= 6; i++) t += Nm(GDVars.dsTargetCreaturePtr[i]) + " ";
        s.Add("targets " + t);
        string tr = ""; for (var it = GDVars.treasureItems; it != null; it = it.next) tr += it.name + "x" + it.quantity + "e" + it.equipped + " ";
        s.Add("treasure " + tr + " last " + (GDVars.lastMissileItem == null ? "nil" : GDVars.lastMissileItem.name));
        return s;
    }

    // oracle-side mirror of the unit's attacksMade
    static byte[] oAttacksMade = new byte[3];

    static void CbBoth(string what, Action build, Func<string> oracle, Func<string> prod)
    {
        build();
        string er = oracle();
        var el = new List<string>(cbL);
        // the oracle keeps its own attacksMade; publish it into the snapshot slot the production run fills
        CbtActs.attacksMade[1] = oAttacksMade[1]; CbtActs.attacksMade[2] = oAttacksMade[2];
        var es = CbSnap();
        build();
        string ar = prod();
        var al = new List<string>(cbL);
        var asn = CbSnap();
        EqS(er, ar, what + " result");
        int n = Math.Min(el.Count, al.Count);
        for (int i = 0; i < n; i++) EqS(el[i], al[i], what + " log line " + i);
        Eq(el.Count, al.Count, what + " log length");
        Eq(es.Count, asn.Count, what + " snapshot length");
        for (int i = 0; i < es.Count; i++) EqS(es[i], asn[i], what + " snapshot");
    }

    // ---------- oracles (from src/cbtacts.pas) ----------
    static bool OEnd(TCharRecord c) { c.combatAddr.initiative = 0; c.combatAddr.spellCast = 0; c.combatAddr.guarding = 0; c.combatAddr.movement = 0; return true; }
    static sbyte OX(TCharRecord c) { return CM.CombatCellX(c); }
    static sbyte OY(TCharRecord c) { return CM.CombatCellY(c); }
    static byte OCurAtt(TCharRecord c, int k) { return k == 1 ? c.curAttacks : c.curAttacks2; }
    static void OSetAtt(TCharRecord c, int k, int v) { if (k == 1) c.curAttacks = (byte)v; else c.curAttacks2 = (byte)v; }

    static byte OAdjusted(byte n) { int v = n; if (GDVars.roundCounter % 2 == 1) v = (v + 1) & 0xFF; return (byte)(v / 2); }

    static byte OCalcMove(TCharRecord c)
    {
        int m = c.movementCur;
        if (c.hostile == 0) m = (m + GDVars.zone1[C_Gbvm.MOD_PARTY_MOVE]) & 0xFF;
        GDVars.dsCurrentCreatureCalculatingFlag = 1;
        if (m < 1 || m > 96) m = 1;
        GDVars.dsCurrentAttackCurrent = (byte)((m * 2) & 0xFF);
        CF.TriggerEffectsWithCondition(18, c);
        GDVars.dsCurrentCreatureCalculatingFlag = 0;
        return GDVars.dsCurrentAttackCurrent;
    }

    static void OSetCur(TCharRecord c)
    {
        bool ranged = false; int saved = c.curAttacks; TItemRec w = null;
        if (CharHlp.HasRangedWeapon(c) && CharHlp.FindDamagingRangedWeapon(c, ref w))
        {
            ranged = true;
            int rof = GDVars.baseItems[c.equipped[0].itemType].rateOfFire;
            GDVars.dsCurrentAttackCurrent = (byte)(rof < 2 ? 2 : rof);
        }
        else GDVars.dsCurrentAttackCurrent = c.attacks;
        GDVars.dsCurrentCreatureCalculatingFlag = 0;
        CF.TriggerEffectsWithCondition(18, c);
        int adj = OAdjusted(GDVars.dsCurrentAttackCurrent);
        if (ranged && w != null) { int q = w.quantity > 1 ? w.quantity : 1; if (q < adj) adj = q; }
        bool made = c.combatAddr.madeAttack != 0;
        if (!made || adj < saved || (adj < saved * 2 && !ranged)) c.curAttacks = (byte)adj;
    }

    static void ORoundStart(TCharRecord c)
    {
        var k = c.combatAddr;
        k.spellCast = 0; k.castAllowed = 1; k.itemUseAllowed = 1; k.madeAttack = 0; k.attackPhase = 2;
        OSetCur(c);
        GDVars.dsCurrentAttackCurrent = c.attacks2;
        GDVars.dsCurrentCreatureCalculatingFlag = 0;
        CF.TriggerEffectsWithCondition(18, c);
        c.curAttacks2 = OAdjusted(GDVars.dsCurrentAttackCurrent);
        k.sweepBudget = c.attackLevel;
        if (c.enabled == 0) k.initiative = 0;
        else
        {
            int ini = unchecked((sbyte)(ORoll(1, 6) + (c.dexCur - 15)));
            if (ini < 1) ini = 1;
            if ((((c.hostile + 1) & GDVars.zone1[C_Gbvm.SURPRISE]) & 0xFF) != 0) ini = (sbyte)(ini - 6);
            if (ini < 0 || ini > 20) ini = 0;
            k.initiative = (sbyte)ini;
        }
        k.movement = OCalcMove(c);
    }

    static byte ODir(TCharRecord a, TCharRecord b)
    {
        for (int d = 0; d < 256; d++) if (Pathing.IsDirToward(OX(a), OY(a), OX(b), OY(b), (byte)d)) return (byte)d;
        throw new Exception("no direction");
    }

    static bool OBackstab(TCharRecord att, TCharRecord def)
    {
        var w = att.equipped[C_Equip.WEAPON]; var a = att.equipped[C_Equip.ARMOR];
        bool kit = att.classLevels[C_Level.THIEF] > 0 && (a == null || a.itemType == C_Item.LEATHER_ARMOR)
                   && (w == null || w.itemType == C_Item.CLUB || w.itemType == C_Item.DAGGER || (w.itemType >= C_Item.BROAD_SWORD && w.itemType <= C_Item.SHORT_SWORD));
        return kit && def.combatAddr.flankCount > 1 && ODir(att, def) == def.combatAddr.direction;
    }

    static void OWeaponDamage(TCharRecord att, TCharRecord def, int idx)
    {
        GDVars.gDamageDice = att.curRollsA[idx];
        int dmg = att.curModA[idx] + ORoll(att.curRollsA[idx], att.curDiceA[idx]);
        dmg = Math.Max(0, Math.Min(255, dmg));
        int pd = dmg;
        if (OBackstab(att, def)) pd = (pd * ((att.classLevels[C_Level.THIEF] + 3) / 4 + 1)) & 0xFF;
        int r = att.classLevels[C_Level.RANGER];
        if (r > 0 && CM.CalcDistance(att, def) <= 1)
            for (int i = 1; i <= 16; i++)
                if (def.name == GDConsts.rangerFoes[i]) pd = pd > 255 - r ? 255 : (pd + r) & 0xFF;
        GDVars.pendingDamage = (byte)pd;
        GDVars.elementMask = 0;
        CF.TriggerEffectsWithCondition(4, att);
        CF.TriggerEffectsWithCondition(5, def);
    }

    static void OResult(TCharRecord att, TCharRecord def, int type, int dmg, int disp, bool hit)
    {
        string v = type == 2 ? "-Backstabs-" : type == 3 ? "slays helpless" : "Attacks";
        CbtActs.PrintNameStringCall(att, v, 10, false);
        CbtActs.DisplayCreatureNameCall(def, 23, 12, false);
        string m = type == 1 ? "(from behind) " : "";
        if (hit)
        {
            m = type == 3 ? "with one cruel blow" : m + "Hitting for " + disp + (disp == 1 ? " point " : " points ") + "of damage";
            CbtActs.InflictDamageCall(def, (byte)dmg);
        }
        else m += "and Misses";
        CbtActs.DisplayMessageWindowCall(23, 13, 38, 16, 10, true, m);
        int row = GDVars.curRow + 1;
        if (dmg > 0)
        {
            def.combatAddr.castAllowed = 0;
            CbtActs.PauseOnlyCall();
            if (def.combatAddr.spellCast > 0)
            {
                CbtActs.PrintNameStringCall(def, "lost a spell", 12, true);
                CbtActs.RemoveSpellCall(def, def.combatAddr.spellCast);
                def.combatAddr.spellCast = 0;
            }
        }
        else CbtActs.PauseOnlyCall();
        if (def.enabled == 0)
        {
            CbtActs.PrintNameStringCall(def, "goes down", (byte)row, false);
            row += 2;
            if (def.status == C_Status.DYING) CbtActs.DisplayStringCall(23, (byte)row, 10, "and is Dying");
            if (def.status == C_Status.DEAD || def.status == C_Status.STONED || def.status == C_Status.GONE) CbtActs.PrintNameStringCall(def, "is killed", (byte)row, false);
            CF.RemoveEffectsHp0(def);
            CF.TriggerEffectsWithCondition(13, def);
            if (def.enabled == 0) CM.CreatureDowned(def); else CbtActs.PauseOnlyCall();
        }
        CbtActs.ClearMessageWindowCall();
    }

    static byte OModDef(TCharRecord att, TCharRecord def, byte defense)
    {
        int dist = (sbyte)CM.CalcDistance(att, def);
        int sub = CharHlp.HasRangedWeapon(att) ? (GDVars.baseItems[att.equipped[0].itemType].range - 1) / 3 : dist;
        int d = defense;
        if (dist - sub > 0) { dist -= sub; d += 2; }
        if (dist - sub > 0) { d += 3; }
        return (byte)(d & 0xFF);
    }

    static bool ODoAttack(TCharRecord att, TCharRecord def, bool fromBehind)
    {
        bool behind = fromBehind, done, anyHit = false, downed = false;
        GDVars.hitsMade[1] = 0; GDVars.hitsMade[2] = 0; oAttacksMade[1] = 0; oAttacksMade[2] = 0;
        GDVars.pendingDamage = 0;
        att.combatAddr.madeAttack = 1;
        if (CharHlp.IsHelpless(def))
        {
            CbtActs.SoundCommandCall(GDConsts.soundIds.combat[C_Sound.IMPACT]);
            while (att.combatAddr.attackPhase > 1 && OCurAtt(att, att.combatAddr.attackPhase) == 0) att.combatAddr.attackPhase--;
            oAttacksMade[att.combatAddr.attackPhase]++;
            OResult(att, def, 3, def.hpCur > 250 ? 255 : def.hpCur + 5, 1, true);
            CF.RemoveInvisibility(att);
            att.curAttacks = 0; att.curAttacks2 = 0;
            done = true;
        }
        else
        {
            var w = att.equipped[C_Equip.WEAPON];
            if (w != null && (def.iconDimensions > 128 || (def.iconDimensions & 7) > 1))
            {
                var b = GDVars.baseItems[w.itemType];
                att.curRollsA[1] = b.dmgLgRolls; att.curDiceA[1] = b.dmgLgDice;
                att.curModA[1] = unchecked((sbyte)(att.curModA[1] - b.dmgSmMod + b.dmgLgMod));
            }
            CbtActs.RecalcStatsCall(def);
            CF.TriggerEffectsWithCondition(11, def);
            int defense;
            if (OBackstab(att, def)) defense = (def.acBehind - 2) & 0xFF;
            else
            {
                if (def.combatAddr.flankCount > 1 && ODir(att, def) == def.combatAddr.direction && def.combatAddr.flankSpread > 4) behind = true;
                defense = behind ? def.acBehind : def.acCur;
            }
            defense = OModDef(att, def, (byte)defense);
            int type = OBackstab(att, def) ? 2 : behind ? 1 : 0;
            for (int ph = att.combatAddr.attackPhase; ph >= 1; ph--)
                while (OCurAtt(att, ph) > 0 && !downed)
                {
                    OSetAtt(att, ph, OCurAtt(att, ph) - 1);
                    att.combatAddr.attackPhase = (byte)ph;
                    oAttacksMade[ph]++;
                    if (CF.AttackRoll(att, def, (byte)defense) || CharHlp.IsHelpless(def))
                    {
                        anyHit = true;
                        TEffectRec img;
                        if (CharHlp.HasEffect(def, C_Effect.MIRROR_IMAGE, out img) && ORoll(1, (img.strength + 1) & 0xFF) > 1)
                        {
                            CbtActs.SoundCommandCall(GDConsts.soundIds.combat[C_Sound.MISS]);
                            OResult(att, def, type, 0, 0, false);
                            CbtActs.PrintNameStringCall(def, "lost an image", 10, true);
                            img.strength--;
                            if (img.strength == 0) CF.RemoveEffect(def, C_Effect.MIRROR_IMAGE, null);
                        }
                        else
                        {
                            GDVars.hitsMade[ph]++;
                            CbtActs.SoundCommandCall(GDConsts.soundIds.combat[C_Sound.IMPACT]);
                            OWeaponDamage(att, def, ph);
                            OResult(att, def, type, GDVars.pendingDamage, GDVars.pendingDamage, true);
                            if (def.enabled != 0) CF.TriggerEffectsWithCondition((byte)(ph + 1), att);
                            else downed = true;
                        }
                    }
                }
            if (!anyHit)
            {
                CbtActs.SoundCommandCall(GDConsts.soundIds.combat[C_Sound.MISS]);
                OResult(att, def, type, 0, 0, false);
            }
            done = att.curAttacks == 0 && att.curAttacks2 == 0;
            att.combatAddr.sweepBudget = 0;
        }
        if (done) done = OEnd(att);
        return done;
    }

    static void ONote(TCharRecord att, TCharRecord def)
    {
        def.combatAddr.flankCount++;
        int rel = (ODir(def, att) - def.combatAddr.direction + 8) % 8;
        if (rel > 4) rel = 8 - rel;
        def.combatAddr.flankSpread = (byte)((def.combatAddr.flankSpread + rel) % 8);
    }

    static void OAnim(TCharRecord att, TCharRecord def, TItemRec w)
    {
        int dir = ODir(att, def), frames = 1, pace = 10, sym = 13;
        int t = w.itemType;
        if (t == C_Item.DART || t == C_Item.JAVELIN || t == C_Item.QUARREL || t == C_Item.SPEAR || t == C_Item.ARROW)
        {
            if (dir % 2 == 1)
            {
                if (dir == 3 || dir == 5) CbtActs.CopyIconFacingCall(14, 1, 0, dir == 5);
                else CbtActs.CopyIconFacingCall(14, 0, 0, dir == 7);
            }
            else CbtActs.CopyIconFacingCall((byte)(13 + dir % 4), (byte)(dir / 4), 0, false);
        }
        else if (t == C_Item.HAND_AXE || t == C_Item.CLUB || t == C_Item.HAMMER) { CbtActs.BuildIconFacingsCall(16); frames = 4; pace = 50; }
        else if (t == C_Item.HOLY_WATER_VIAL || t == C_Item.FLASK_OF_OIL) { CbtActs.BuildIconFacingsCall(17); frames = 4; pace = 50; }
        else
        {
            if (t == C_Item.SLING) sym = 14;
            CbtActs.CopyIconFacingCall((byte)(sym + 7), 0, 0, false);
            CbtActs.CopyIconFacingCall((byte)(sym + 7), 1, 1, false);
            frames = 2; pace = 20;
        }
        CbtActs.SoundSetAutoStopCall(0);
        CbtActs.SoundCommandCall(GDConsts.soundIds.combat[C_Sound.RANGED_START]);
        CbtActs.CombatAnimateMovingIconCall(OX(att), OY(att), OX(def), OY(def), (byte)frames, (byte)pace);
        CbtActs.SoundCommandCall(GDConsts.soundIds.combat[C_Sound.RANGED_END]);
        CbtActs.SoundSetAutoStopCall(10);
    }

    static bool OCombatAttack(TCharRecord att, TCharRecord def, bool fromBehind, TItemRec weapon)
    {
        if (def.combatAddr.flankCount < 3 && !fromBehind)
        {
            byte d0 = ODir(def, att);
            if (CM.CombatIconVisible(def, 0)) CM.SetCreatureFacing(def, d0, 0, 0); else def.combatAddr.direction = d0;
        }
        byte d1 = ODir(att, def);
        CbtActs.CombatPrintCreatureInfoCall(att);
        CM.SetCreatureFacing(att, d1, 1, 0);
        att.combatAddr.other = def;
        TextRend.DelayMilliseconds(100);
        if (weapon != null) OAnim(att, def, weapon);
        if (att.equipped[0] != null && att.equipped[0].itemType == C_Item.SLING) OAnim(att, def, att.equipped[0]);
        bool done = true;
        if (att.curAttacks > 0 || att.curAttacks2 > 0)
        {
            var saved = GDVars.curChar; GDVars.curChar = att;
            done = ODoAttack(att, def, fromBehind);
            if (weapon != null)
            {
                if (weapon.quantity > 0) weapon.quantity = (byte)((weapon.quantity - oAttacksMade[1]) & 0xFF);
                if (weapon.quantity == 0)
                {
                    if (CharHlp.HasThrowableWeapon(att) && weapon.special != C_Effect.SPIRIT_HAMMER_GONE)
                    {
                        CbtActs.PrependItemCopyCall(ref GDVars.treasureItems, weapon);
                        GDVars.treasureItems.equipped = 0;
                        CbtActs.RemoveItemCall(att, weapon);
                        GDVars.lastMissileItem = GDVars.treasureItems;
                    }
                    else CbtActs.RemoveItemCall(att, weapon);
                }
            }
            CbtActs.RecalcStatsCall(att);
            GDVars.curChar = saved;
        }
        if (done) done = OEnd(att);
        if (CM.CombatIconVisible(att, 0)) { CM.SetCreatureFacing(att, att.combatAddr.direction, 1, 1); CM.SetCreatureFacing(att, att.combatAddr.direction, 0, 0); }
        return done;
    }

    static bool OCanAttack(TCharRecord c, TCharRecord o)
    {
        if (o == null) return false;
        if (c == o) return true;
        GDVars.dsCurrentSuppressTargetingFlag = 0;
        CF.TriggerEffectsWithCondition(1, o);
        if (GDVars.dsCurrentSuppressTargetingFlag == 0)
        {
            var s = c.combatAddr.other; c.combatAddr.other = o;
            CF.TriggerEffectsWithCondition(0, c);
            c.combatAddr.other = s;
        }
        return GDVars.dsCurrentSuppressTargetingFlag == 0;
    }

    static void OGuarding(TCharRecord c)
    {
        int cnt = CM.CountTargets(c, 1);
        for (int i = 1; i <= cnt; i++)
        {
            if (c.enabled == 0) continue;
            var p = GDVars.creatureArr[GDVars.dsPotentialTargetCreature[i]];
            if (p.combatAddr.guarding != 0 && !CharHlp.IsHelpless(p))
            {
                CM.ScrollView(OX(c), OY(c), 2, 8);
                p.combatAddr.guarding = 0;
                ONote(p, c);
                OCombatAttack(p, c, false, null);
            }
        }
    }

    static void OMove(TCharRecord c, byte dir)
    {
        int cost = dir % 2 == 1 ? 3 : 2;
        if (cost > c.combatAddr.movement) { c.combatAddr.movement = 0; return; }
        c.combatAddr.movement = (byte)(c.combatAddr.movement - cost);
        int slot = CbSlot(c);
        if (slot == 0) { c.combatAddr.movement = 0; return; }
        int x = GDVars.arenaLoc[slot].x, y = GDVars.arenaLoc[slot].y;
        int nx = x + GDConsts.dirDX[dir], ny = y + GDConsts.dirDY[dir];
        byte delta = 1;
        if (c.quickfight != 0)
        {
            delta = 3;
            if (!CM.ViewCoordsValid((sbyte)(nx - GDVars.arenaPtr.ox), (sbyte)(ny - GDVars.arenaPtr.oy))) CM.ScrollView((sbyte)x, (sbyte)y, 2, 8);
        }
        CM.UpdateCombatMapTile(0, 0, (byte)slot);
        GDVars.arenaLoc[slot].x = (sbyte)nx; GDVars.arenaLoc[slot].y = (sbyte)ny;
        CM.BuildArenaArrays();
        CM.ScrollView((sbyte)nx, (sbyte)ny, delta, 8);
        c.combatAddr.flankCount = 0; c.combatAddr.flankSpread = 0;
        CbtActs.SoundCommandCall(GDConsts.soundIds.combat[C_Sound.STEP]);
        OGuarding(c);
        if (c.enabled == 0 || CharHlp.IsHelpless(c)) c.combatAddr.movement = 0;
    }

    static void OOpportunity(TCharRecord c, byte dir)
    {
        int slot = CbSlot(c);
        if (slot == 0) return;
        int before = CM.CountTargets(c, 1);
        if (before == 0) return;
        var adj = new List<int>();
        for (int i = 1; i <= before; i++) adj.Add(GDVars.dsPotentialTargetCreature[i]);
        GDVars.arenaLoc[slot].x += GDConsts.dirDX[dir]; GDVars.arenaLoc[slot].y += GDConsts.dirDY[dir];
        int after = CM.CountTargets(c, 1);
        GDVars.arenaLoc[slot].x -= GDConsts.dirDX[dir]; GDVars.arenaLoc[slot].y -= GDConsts.dirDY[dir];
        var stay = new HashSet<int>();
        for (int j = 1; j <= after; j++) stay.Add(GDVars.dsPotentialTargetCreature[j]);
        foreach (int a0 in adj)
        {
            if (stay.Contains(a0) || c.enabled == 0) continue;
            var p = GDVars.creatureArr[a0];
            TEffectRec e;
            if (CharHlp.IsHelpless(p) || !OCanAttack(p, c) || CharHlp.HasEffect(p, C_Effect.BLOOD_DRAIN, out e) || CharHlp.HasEffect(p, C_Effect.BITE_HOLD, out e)) continue;
            bool attacked = false;
            for (int a = p.combatAddr.direction + 6; a <= p.combatAddr.direction + 10; a++)
            {
                if (attacked) continue;
                if (p.combatAddr.initiative > 0 || p.combatAddr.flankCount == 0 || Pathing.IsDirToward(OX(p), OY(p), OX(c), OY(c), (byte)(a % 8)))
                {
                    int ph = p.attacks == 0 ? 2 : 1;
                    if (p.curAttacks > 0) ph = 1;
                    if (p.curAttacks2 > 0) ph = 2;
                    if (OCurAtt(p, ph) == 0) OSetAtt(p, ph, 1);
                    p.combatAddr.attackPhase = (byte)ph;
                    var so = p.combatAddr.other;
                    GDVars.doneRoundFlag = OCombatAttack(p, c, true, null);
                    attacked = true;
                    p.combatAddr.other = so;
                    if (c.enabled != 0) CbtActs.CombatPrintCreatureInfoCall(c);
                }
            }
        }
    }

    static byte OHighest(TCharRecord c)
    {
        int best = 0;
        for (var p = GDVars.creatureList; p != null; p = p.nextCharAddr)
            if (p.hostile == (c.hostile == 0 ? 1 : 0) && p.enabled != 0) best = Math.Max(best, OCalcMove(p) / 2);
        return (byte)best;
    }

    static bool OFlee(TCharRecord c)
    {
        bool ok;
        if (CM.CountTargets(c, 255) == 0) ok = true;
        else
        {
            int half = OCalcMove(c) / 2; int en = OHighest(c);
            ok = en < half || (en == half && ORoll(1, 2) == 1);
        }
        if (ok) CF.PutOutOfPlayWithStatus(c, 3, "Got Away"); else CbtActs.PrintBottomLineCall("Escape is blocked");
        return OEnd(c);
    }

    static bool OSweep(TCharRecord att, TCharRecord def)
    {
        if (att.curAttacks >= att.combatAddr.sweepBudget || def.levelHighest != 0 || CM.CalcDistance(att, def) != 1) return false;
        int cnt = CM.CountTargets(att, 1), weak = 0, di = 0;
        for (int i = 1; i <= cnt; i++)
        {
            var p = GDVars.creatureArr[GDVars.dsPotentialTargetCreature[i]];
            if (p == def) di = i;
            if (p.levelHighest == 0) weak++;
        }
        if (di == 0 || weak <= att.curAttacks) return false;
        weak = Math.Min(weak, att.combatAddr.sweepBudget);
        CbtActs.PrintNameStringCall(att, "sweeps", 10, true);
        if (GDVars.creatureArr[GDVars.dsPotentialTargetCreature[1]] != def)
        {
            GDVars.dsPotentialTargetCreature[di] = GDVars.dsPotentialTargetCreature[1];
            GDVars.dsPotentialTargetCreature[1] = (byte)CbSlot(def);
        }
        for (int i = 1; i <= cnt && weak > 0; i++)
        {
            var p = GDVars.creatureArr[GDVars.dsPotentialTargetCreature[i]];
            if (p.levelHighest != 0) continue;
            ONote(att, p);
            att.curAttacks = 1;
            GDVars.doneRoundFlag = OCombatAttack(att, p, false, null);
            weak--;
        }
        return true;
    }

    static sbyte OTurnLevel(TCharRecord c)
    {
        int l = Math.Max(c.classLevels[C_Level.CLERIC], c.classLevels[C_Level.PALADIN] - 2);
        return (sbyte)Math.Max(l, 0);
    }

    static TCharRecord OFindTurn(TCharRecord c)
    {
        int best = 13; TCharRecord t = null;
        int cnt = CM.CountTargets(c, 255);
        for (int i = 1; i <= cnt; i++)
        {
            var p = GDVars.creatureArr[GDVars.dsPotentialTargetCreature[i]];
            if (p.combatAddr.turned == 0 && p.levelUndead > 0 && p.levelUndead < best) { best = p.levelUndead; t = p; }
        }
        return t;
    }

    static void OTurn(TCharRecord c)
    {
        CbtActs.PrintNameStringCall(c, "turns undead...", 10, false);
        CbtActs.ClearBottomLineCall(); CbtActs.PauseOnlyCall();
        bool any = false;
        c.combatAddr.usedTurnUndead = 1;
        int destroys = 6, attempts = ORoll(1, 12), roll = ORoll(1, 20);
        int tl = OTurnLevel(c);
        int col = tl >= 1 && tl <= 8 ? tl : (tl >= 9 && tl <= 13 ? 9 : 10);
        while (true)
        {
            var t = OFindTurn(c);
            if (t == null || attempts == 0) break;
            int row = Math.Min((int)t.levelUndead, 10);
            int tv = GDConsts.turnTable[row, col];
            if (roll < Math.Abs(tv)) break;
            any = true;
            CM.DisplayMapViewCentered(t, 3, false);
            CbtActs.CombatPrintCreatureInfoCall(t);
            if (tv > 0) { t.combatAddr.turned = 1; CbtActs.PrintNameStringAnimCall(t, true, "is turned"); }
            else { CbtActs.PrintNameStringCall(t, "Is destroyed", 10, false); CM.CreatureDowned(t); t.status = C_Status.GONE; t.enabled = 0; }
            if (destroys > 0) destroys--;
            attempts--;
            if (attempts == 0 && destroys > 0 && tv < 0) attempts = 1;
            CbtActs.ClearMessageWindowCall();
        }
        if (!any) CbtActs.PrintBottomLineCall("Nothing Happens...");
        CbtActs.CountTeamsCall();
        GDVars.doneRoundFlag = OEnd(c);
        CbtActs.ClearMessageWindowCall();
    }

    static void OHpPct()
    {
        int max = 0, cur = 0;
        for (var p = GDVars.creatureList; p != null; p = p.nextCharAddr)
            if (p.hostile == 1) { if (p.enabled != 0) cur = (cur + p.hpCur) & 0xFFFF; max = (max + p.hpMax) & 0xFFFF; }
        if (max > 0) GDVars.hostileHpPct = (byte)((cur * 20 / max * 5) & 0xFF);
    }

    static bool OCheckAlly(TCharRecord att, TCharRecord def)
    {
        if ((def.hostile == 0 ? 1 : 0) == att.hostile || att.quickfight != 0) return true;
        if (CbtActs.YesNoMenuCall("Attack Ally: ", 13, 10, 15) != 'Y') return false;
        GDVars.zone1[C_Gbvm.ATTACKED_ALLIES] = 1;
        for (var p = GDVars.creatureList; p != null; p = p.nextCharAddr)
            if (p.status == C_Status.OKAY && p.npc >= 0x80) { p.hostile = 1; p.combatAddr.other = null; }
        CbtActs.CountTeamsCall();
        return true;
    }

    static bool OFindCure(TCharRecord c, ref object tgt)
    {
        object best = null; int downIdx = 0, bestHp = 255;
        byte cr = 0, terr = 0;
        for (int d = 0; d <= 8; d++)
        {
            sbyte nx = (sbyte)(OX(c) + GDConsts.dirDX[d]), ny = (sbyte)(OY(c) + GDConsts.dirDY[d]);
            CM.GetArenaContents(nx, ny, ref cr, ref terr);
            if (cr > 0)
            {
                var t = GDVars.creatureArr[cr]; tgt = t;
                if (t.hostile == c.hostile && t.hpCur < t.hpMax && (t.hpCur < bestHp || (t == c && t.hpCur < t.hpMax / 2))) { best = t; bestHp = t.hpCur; }
            }
            else if (terr == C_Terr.CORPSE)
                for (int j = 1; j <= GDVars.downedCount; j++)
                {
                    var dr = GDVars.downed[j];
                    if (dr.cr != null && dr.cr.status != C_Status.DEAD && dr.cr.status != C_Status.STONED && dr.cr.status != C_Status.GONE && dr.x == nx && dr.y == ny) downIdx = j;
                }
        }
        tgt = (bestHp < 8 || downIdx == 0) ? best : GDVars.downed[downIdx].cr;
        return tgt != null;
    }

    static bool OAreaHits(sbyte x, sbyte y, byte r)
    {
        bool res = false;
        CbtActs.BuildTargetListCall(x, y, r, 255, 1, GDVars.arenaPtr);
        int enemy = GDVars.curChar.hostile == 0 ? 1 : 0;
        for (int i = 1; i <= GDVars.targetCount; i++) if (GDVars.creatureArr[GDVars.targetList[i].idx].hostile != enemy) res = true;
        return res;
    }

    static bool OAcquire(TCharRecord c, byte range, byte relaxed, byte forceNew)
    {
        var p = c.combatAddr.other;
        if (forceNew != 0 || (p != null && (p.hostile == c.hostile || p.enabled == 0 || !OCanAttack(c, p)))) c.combatAddr.other = null;
        bool found = c.combatAddr.other != null;
        for (int pass = 0; pass < 2 && !found; pass++)
        {
            if (pass == 1 && forceNew == 0) GDVars.arenaPtr.distMode = 1;
            int tries = 20; int cnt = CM.CountTargets(c, range);
            while (tries > 0 && !found && cnt > 0)
            {
                tries--;
                int pick = ORoll(1, cnt);
                if (GDVars.dsPotentialTargetCreature[pick] == 0) continue;
                var q = GDVars.creatureArr[GDVars.dsPotentialTargetCreature[pick]];
                if ((relaxed != 0 && GDVars.arenaPtr.distMode != 0) || OCanAttack(c, q)) { found = true; c.combatAddr.other = q; }
                else
                {
                    GDVars.dsPotentialTargetCreature[pick] = 0;
                    bool left = false;
                    for (int i = 1; i <= cnt; i++) if (GDVars.dsPotentialTargetCreature[i] > 0) left = true;
                    if (!left) cnt = 0;
                }
            }
        }
        GDVars.arenaPtr.distMode = 0;
        return found;
    }

    static byte OAimMarker(int spell)
    {
        if (spell >= 1 && spell <= 67 && GDConsts.spellTable[spell].offensive == 1 && GDConsts.spellTable[spell].target == 0) return 18;
        return 22;
    }

    static void OClear(TAimTarget t) { t.cr = null; t.x = 0; t.y = 0; t.pad = 0; }

    static bool OPick(int spell, bool ai, byte ground, TAimTarget tgt)
    {
        bool found = false;
        if (!ai)
        {
            found = OAimMenu(GDVars.curChar, CharHlp.CalcSpellRange((byte)spell), 0, ground, tgt);
            GDVars.curChar.combatAddr.other = (TCharRecord)tgt.cr;
        }
        else if (GDConsts.spellTable[spell].offensive == 0)
        {
            tgt.cr = GDVars.curChar;
            if (spell != C_Spell.CURE_LIGHT_WOUNDS || OFindCure(GDVars.curChar, ref tgt.cr))
            { tgt.x = OX((TCharRecord)tgt.cr); tgt.y = OY((TCharRecord)tgt.cr); found = true; }
        }
        else
        {
            for (int tries = 20; tries > 0 && !found; tries--)
            {
                if (!OAcquire(GDVars.curChar, CharHlp.CalcSpellRange((byte)spell), 0, 1)) continue;
                var p = GDVars.curChar.combatAddr.other;
                bool ok = true;
                if (GDConsts.spellTable[spell].aiRadius > 0 && OAreaHits(OX(p), OY(p), GDConsts.spellTable[spell].aiRadius)) ok = false;
                if (CharHlp.IsHelpless(p)) for (int i = 1; i <= 4; i++) if (GDConsts.spellTable[spell].effect == GDConsts.helplessFx[i]) ok = false;
                if (ok) { tgt.cr = GDVars.curChar.combatAddr.other; tgt.x = OX(p); tgt.y = OY(p); found = true; }
            }
        }
        if (found) { GDVars.gTargetX = tgt.x; GDVars.gTargetY = tgt.y; } else OClear(tgt);
        return found;
    }

    static bool OCastMenu(int spell, bool b)
    {
        bool cast = true;
        GDVars.spellTargetCount = 0; GDVars.gMultiTarget = 0;
        GDVars.gTargetX = OX(GDVars.curChar); GDVars.gTargetY = OY(GDVars.curChar);
        int area = GDConsts.spellTable[spell].area % 16;
        var sel = new TAimTarget();
        if (area == 0) { GDVars.dsTargetCreaturePtr[1] = GDVars.curChar; GDVars.spellTargetCount = 1; }
        else if (area == 15)
        {
            if (!b) CM.StartSpellPreview(TAimShape.AimBlast, 0, OAimMarker(spell));
            bool picked = OPick(spell, b, 0, sel);
            CM.StopSpellPreview();
            if (!picked) cast = false;
            else if (GDVars.curChar.combatAddr.other != null) { GDVars.dsTargetCreaturePtr[1] = GDVars.curChar.combatAddr.other; GDVars.spellTargetCount = 1; }
            else { CbtActs.TargetAreaCall(GDVars.gTargetX, GDVars.gTargetY, 0); GDVars.gMultiTarget = 1; }
        }
        else if (area >= 8)
        {
            if (!b)
            {
                int r = GDConsts.spellTable[spell].area % 8;
                if (spell == 51) CM.StartSpellPreview(TAimShape.AimBolt, 0, 19);
                else if (spell == 34) CM.StartSpellPreview(TAimShape.AimCloud, 0, 22);
                else if (spell == 47 || spell == 64) CM.StartSpellPreview(TAimShape.AimBlast, (byte)(GDVars.zone0.view3dFlag == 0 ? 2 : r), 23);
                else CM.StartSpellPreview(TAimShape.AimBlast, (byte)r, OAimMarker(spell));
            }
            bool picked = OPick(spell, b, 1, sel);
            CM.StopSpellPreview();
            if (picked) { CbtActs.TargetAreaCall(GDVars.gTargetX, GDVars.gTargetY, (byte)(GDConsts.spellTable[spell].area % 8)); GDVars.gMultiTarget = 1; }
            else cast = false;
        }
        else
        {
            int remaining = GDConsts.spellTable[spell].area % 4 + 1, k = 0;
            while (remaining > 0)
            {
                if (!b) CM.StartSpellPreview(TAimShape.AimBlast, 0, OAimMarker(spell));
                bool picked = OPick(spell, b, 0, sel);
                CM.StopSpellPreview();
                if (!picked) { remaining--; continue; }
                bool dup = false;
                for (int j = 1; j <= k; j++) if (GDVars.dsTargetCreaturePtr[j] == sel.cr) dup = true;
                if (!dup) { k++; GDVars.dsTargetCreaturePtr[k] = (TCharRecord)sel.cr; remaining--; }
                else if (!b) CbtActs.PrintBottomLineCall("Already been targeted");
                else remaining--;
                CM.RedrawArenaCell(OX(GDVars.dsTargetCreaturePtr[k]), OY(GDVars.dsTargetCreaturePtr[k]));
            }
            GDVars.spellTargetCount = (byte)k;
            if (k == 0) cast = false;
        }
        return cast;
    }

    static bool OCastSpell(int spell, int ai)
    {
        var caster = GDVars.curChar;
        bool cast = false;
        int id = spell; TItemRec scroll = null;
        if (id == 0)
        {
            int idx = -1; bool ne = true;
            id = CG.CastSpellMenu(ref idx, ref ne, ref scroll);
            if (id == 0) scroll = null;
        }
        if (id > 0 && GDConsts.spellTable[id].whenCast == 0) { CbtActs.PrintBottomLineCall("Camp Only Spell"); id = 0; scroll = null; }
        if (scroll != null)
        {
            CG.UseItemSpell(scroll, (byte)id, true, ref cast);
            CbtActs.RecalcStatsCall(caster);
            OSetCur(caster);
            if (!cast) CbtActs.DisplayCombatInterfaceCall();
            return cast;
        }
        if (ai == 0) { CbtActs.DisplayCombatInterfaceCall(); CM.DisplayMapViewCentered(caster, 3, true); CbtActs.CombatPrintCreatureInfoCall(caster); }
        if (id > 0)
        {
            int ct = GDConsts.spellTable[id].castTime / 3;
            if (ct == 0) { CG.CastSpellNC((byte)id, ai != 0, true, ref cast); cast = OEnd(caster); }
            else
            {
                cast = true;
                CbtActs.PrintNameStringCall(caster, "Begins Casting", 10, true);
                caster.combatAddr.spellCast = (byte)id;
                caster.combatAddr.initiative = caster.combatAddr.initiative > ct ? (sbyte)(caster.combatAddr.initiative - ct) : (sbyte)1;
            }
        }
        return cast;
    }

    static byte ODirKey(int ch)
    {
        switch (ch) { case 72: return 0; case 73: return 1; case 77: return 2; case 81: return 3; case 80: return 4; case 79: return 5; case 75: return 6; case 71: return 7; }
        return 8;
    }

    static byte OAimTargetMenu(TCharRecord cur, TCharRecord tgt, int range, int wm, out byte dir)
    {
        int dist = CM.CalcDistance(cur, tgt);
        if (wm != 0) CbtActs.DisplayStringCall(0, 23, 10, "Range = " + dist + "  ");
        string t = "";
        if (dist <= range)
        {
            TItemRec w = null;
            if (wm == 0) t = "Target ";
            else if (cur != tgt && (!CharHlp.HasRangedWeapon(cur) || (CharHlp.FindDamagingRangedWeapon(cur, ref w) && (CM.CountTargets(cur, 1) == 0 || CharHlp.HasThrowableWeapon(cur))))) t = "Target ";
        }
        string menu = "Next Prev Manual " + t + "Center Exit";
        CM.DisplayMapViewCentered(tgt, 3, true);
        CbtActs.CombatPrintCreatureInfoCall(tgt);
        byte ch; bool ext;
        while (true)
        {
            MenuCore.arrowsToCaller = true;
            ext = false;
            ch = CbtActs.MenuHorizontalCall("Aim: ", menu, 13, 10, 15, true, false, ref ext);
            dir = ext ? ODirKey(ch) : (byte)8;
            if (!ext || dir != 8) break;
        }
        return ch;
    }

    static bool OConfirm(TCharRecord cur, TCharRecord tgt, int wm, TAimTarget sel)
    {
        bool done = !(wm == 1 && !OCheckAlly(cur, tgt));
        if (!done) { OClear(sel); return false; }
        sel.cr = tgt; sel.x = OX(tgt); sel.y = OY(tgt);
        GDVars.arenaPtr.cursorOn = false;
        CM.ScrollView((sbyte)(GDVars.arenaPtr.ox + 3), (sbyte)(GDVars.arenaPtr.oy + 3), 3, 8);
        if (wm == 1)
        {
            if (OSweep(cur, tgt)) done = OEnd(cur);
            else
            {
                ONote(cur, tgt);
                TItemRec w = null;
                if (CharHlp.HasRangedWeapon(cur) && CharHlp.FindDamagingRangedWeapon(cur, ref w) && CharHlp.HasThrowableWeapon(cur) && CM.CalcDistance(cur, tgt) == 1) w = null;
                done = OCombatAttack(cur, tgt, false, w);
            }
        }
        return done;
    }

    static bool OManual(TCharRecord cur, TCharRecord under, int range, int wm, int ground, int dir, TAimTarget sel)
    {
        OClear(sel);
        int cx = OX(under), cy = OY(under);
        int ch = ' '; bool done = false;
        GDVars.arenaPtr.cursorOn = true; GDVars.arenaPtr.cursorSize = 1;
        while (ch != 0 && ch != 'E' && ch != 'T')
        {
            CM.ScrollView((sbyte)cx, (sbyte)cy, 3, (byte)dir);
            cx = Math.Max(0, Math.Min(49, cx + GDConsts.dirDX[dir]));
            cy = Math.Max(0, Math.Min(24, cy + GDConsts.dirDY[dir]));
            byte cr = 0, terr = 0;
            CM.GetArenaContents((sbyte)cx, (sbyte)cy, ref cr, ref terr);
            sbyte wx = (sbyte)cx, wy = (sbyte)cy; ushort dist = 255;
            bool valid = CbtActs.WalkLineCall(OX(cur), OY(cur), ref wx, ref wy, ref dist, GDVars.arenaPtr);
            if (valid) { if (wm != 0) CbtActs.DisplayStringCall(0, 23, 10, "Range = " + (dist / 2) + "  "); }
            else if (wm != 0) CbtActs.ClearBoxCall(0, 23, 39, 23);
            int d2 = dist / 2;
            under = null;
            if (valid)
            {
                if (cr > 0) under = GDVars.creatureArr[cr];
                else if (terr == C_Terr.CORPSE)
                    for (int j = 1; j <= GDVars.downedCount; j++) if (GDVars.downed[j].x == cx && GDVars.downed[j].y == cy) under = GDVars.downed[j].cr;
            }
            if (under != null) CbtActs.CombatPrintCreatureInfoCall(under); else CbtActs.ClearBoxCall(23, 1, 38, 21);
            if (range < d2 || GDConsts.terrainInfo[terr].cost == 255) valid = false;
            if (under != null)
            {
                if (!OCanAttack(cur, under)) valid = false;
                TItemRec w = null;
                if (wm == 1)
                {
                    if (cur == under || (cr == 0 && terr == C_Terr.CORPSE)) valid = false;
                    else if (CharHlp.HasRangedWeapon(cur) && (!CharHlp.FindDamagingRangedWeapon(cur, ref w) || (CM.CountTargets(cur, 1) > 0 && !CharHlp.HasThrowableWeapon(cur)))) valid = false;
                }
            }
            else if (ground == 0) valid = false;
            MenuCore.arrowsToCaller = true;
            ch = CbtActs.MenuHorizontalCall("(Use Cursor keys) ", valid ? "Target Center Exit" : "Center Exit", 13, 10, 15, true, false, ref GDVars.doneRoundFlag);
            if (ch == 'T')
            {
                GDVars.arenaPtr.cursorOn = false;
                if (valid)
                {
                    sel.x = (sbyte)cx; sel.y = (sbyte)cy; sel.cr = under;
                    if (wm == 1) done = OConfirm(cur, under, wm, sel); else done = true;
                }
                if (!valid || !done) { CM.RedrawArenaCell((sbyte)cx, (sbyte)cy); done = false; OClear(sel); }
            }
            else if (ch == 0 || ch == 'E') { CM.RedrawArenaCell((sbyte)cx, (sbyte)cy); OClear(sel); done = false; }
            else if (ch == 'C') { CM.ScrollView((sbyte)cx, (sbyte)cy, 0, 8); dir = 8; }
            else dir = ODirKey(ch);
        }
        return done;
    }

    static bool OAimMenu(TCharRecord c, int range, int wm, int ground, TAimTarget tgt)
    {
        var tl = new List<int>();
        int idx = 1; int curX = 0, curY = 0;
        Action build = () =>
        {
            CbtActs.BuildTargetListCall(OX(c), OY(c), 127, 255, 1, GDVars.arenaPtr);
            tl.Clear(); tl.Add(0);
            for (int i = 1; i <= GDVars.targetCount; i++) tl.Add(GDVars.targetList[i].idx);
        };
        Func<int, bool, TCharRecord> adv = (step, anim) =>
        {
            if (anim) { curX = GDVars.arenaLoc[tl[idx]].x; curY = GDVars.arenaLoc[tl[idx]].y; }
            else CM.RedrawArenaCell((sbyte)curX, (sbyte)curY);
            int n = tl.Count - 1;
            idx = (idx + step) & 0xFF;
            if (idx == 0) idx = n;
            if (idx > n) idx = 1;
            int nx = GDVars.arenaLoc[tl[idx]].x, ny = GDVars.arenaLoc[tl[idx]].y;
            if (anim) { CbtActs.CombatAnimateMovingIconCall((sbyte)curX, (sbyte)curY, (sbyte)nx, (sbyte)ny, 1, 0); curX = nx; curY = ny; }
            return GDVars.creatureArr[tl[idx]];
        };
        CbtActs.CopyIconFacingCall(25, 0, 0, false);
        OClear(tgt);
        bool done = false;
        int eff = range;
        if (eff == 255) eff = c.equipped[0] != null ? (GDVars.baseItems[c.equipped[0].itemType].range - 1) & 0xFF : 1;
        if (eff == 0 || eff == 255) eff = 1;
        build();
        var p = adv(0, true);
        int skip = 1, mstep = 0, ch = ' ';
        while (!done && ch != 0 && ch != 'E')
        {
            if (!OCanAttack(c, p)) { p = adv(skip, false); continue; }
            byte kd;
            ch = OAimTargetMenu(c, p, eff, wm, out kd);
            if (kd != 8) ch = 'M';
            if (ch == 'N') { skip = 1; mstep = 1; }
            else if (ch == 'P') { skip = -1; mstep = -1; }
            else if (ch == 'M') { done = OManual(c, p, eff, wm, ground, kd, tgt); CbtActs.CopyIconFacingCall(25, 0, 0, false); build(); mstep = 0; }
            else if (ch == 'T') { done = OConfirm(c, p, wm, tgt); CbtActs.CopyIconFacingCall(25, 0, 0, false); build(); mstep = 0; }
            else if (ch == 'C') { CM.ScrollView(OX(p), OY(p), 0, 8); mstep = 0; }
            CM.RedrawArenaCell(OX(p), OY(p));
            p = adv(mstep, !done && ch != 0 && ch != 'E');
        }
        if (wm != 0) CbtActs.ClearBoxCall(0, 23, 39, 23);
        return done;
    }

    static void ORake(TCharRecord c)
    {
        if (GDVars.hitsMade[1] != 2 || c.combatAddr.other.enabled == 0) return;
        c.combatAddr.attackPhase = 2; c.curAttacks2 = 2; c.curRollsA[2] = 2; c.curDiceA[2] = 4; c.curAttacks = 0;
        CbtActs.PrintNameStringCall(c, "Rakes...", 10, true);
        OCombatAttack(c, c.combatAddr.other, false, null);
        OEnd(c);
    }

    static void OBite(TCharRecord c, object p, bool removed)
    {
        bool rel = false;
        if (CF.HoldTarget(c, p, removed, 74, ref rel) == null) return;
        c.curAttacks = 1; c.curAttacks2 = 0; c.curRollsA[1] = 3; c.curDiceA[1] = 4;
        OCombatAttack(c, GDVars.dsOtherCreatureCurrentPtr, true, null);
        OEnd(c);
        if (GDVars.dsOtherCreatureCurrentPtr.enabled == 0) CF.RemoveEffect(c, 74, p);
    }

    static bool OSting()
    {
        if (!GDVars.stingArg) return false;
        CbtActs.PrintBottomLineCall("The Gods intervene!");
        for (var p = GDVars.creatureList; p != null; p = p.nextCharAddr)
        {
            if (p.hostile == 1)
            {
                p.enabled = 0; p.status = C_Status.DEAD;
                int s = CbSlot(p); if (s > 0) GDVars.arenaLoc[s].size = 0;
            }
            OEnd(p);
        }
        CM.ScrollView((sbyte)(GDVars.arenaPtr.ox + 3), (sbyte)(GDVars.arenaPtr.oy + 3), 255, 8);
        return true;
    }
}
