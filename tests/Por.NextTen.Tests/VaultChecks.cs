// Vault checks, written from vault.pas: file layout built by an independent writer in the tests.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Por.Core;

static partial class Tests
{
    static Types.TItemRec RandItem()
    {
        var it = new Types.TItemRec();
        var c = new char[rng.Next(41)]; for (int i = 0; i < c.Length; i++) c[i] = (char)(32 + rng.Next(95));
        it.name = new string(c);
        it.inUse = (byte)rng.Next(256); it.itemType = (byte)rng.Next(256);
        for (int i = 1; i <= 3; i++) it.nameIds[i] = (byte)rng.Next(256);
        it.plus = (sbyte)rng.Next(-128, 128); it.saveBonus = (sbyte)rng.Next(-128, 128);
        it.equipped = (byte)rng.Next(256); it.unidMask = (byte)rng.Next(256); it.cursed = (byte)rng.Next(256);
        it.weight = (ushort)rng.Next(65536); it.quantity = (byte)rng.Next(256); it.value = (ushort)rng.Next(65536);
        for (int i = 0; i < 3; i++) it.variantData[i] = (byte)rng.Next(256);
        return it;
    }
    static string ItemDiff(Types.TItemRec a, Types.TItemRec b)
    {
        if (a.name != b.name) return "name"; if (a.inUse != b.inUse) return "inUse"; if (a.itemType != b.itemType) return "type";
        for (int i = 1; i <= 3; i++) if (a.nameIds[i] != b.nameIds[i]) return "nameIds" + i;
        if (a.plus != b.plus) return "plus"; if (a.saveBonus != b.saveBonus) return "saveBonus"; if (a.equipped != b.equipped) return "equipped";
        if (a.unidMask != b.unidMask) return "unid"; if (a.cursed != b.cursed) return "cursed"; if (a.weight != b.weight) return "weight";
        if (a.quantity != b.quantity) return "qty"; if (a.value != b.value) return "value";
        for (int i = 0; i < 3; i++) if (a.variantData[i] != b.variantData[i]) return "variant" + i;
        return null;
    }
    // independent writer of the packed TItemRec
    static byte[] PackItem(Types.TItemRec it)
    {
        var m = new MemoryStream(); var w = new BinaryWriter(m);
        byte[] nm = new byte[40]; for (int i = 0; i < it.name.Length && i < 40; i++) nm[i] = (byte)it.name[i];
        w.Write((byte)Math.Min(it.name.Length, 40)); w.Write(nm);
        w.Write(it.inUse); w.Write(0); w.Write(it.itemType);
        w.Write(it.nameIds[1]); w.Write(it.nameIds[2]); w.Write(it.nameIds[3]);
        w.Write(it.plus); w.Write(it.saveBonus); w.Write(it.equipped); w.Write(it.unidMask); w.Write(it.cursed);
        w.Write(it.weight); w.Write(it.quantity); w.Write(it.value);
        w.Write(it.variantData[0]); w.Write(it.variantData[1]); w.Write(it.variantData[2]);
        return m.ToArray();
    }
    static byte[] PackVault(byte version, byte visited, int[] money, IEnumerable<Types.TItemRec> items, byte sigLen = 4, string sig = "VALT")
    {
        var m = new MemoryStream(); var w = new BinaryWriter(m);
        w.Write(sigLen); w.Write(System.Text.Encoding.ASCII.GetBytes(sig)); w.Write(version); w.Write(visited);
        foreach (int v in money) w.Write(v);
        foreach (var it in items) w.Write(PackItem(it));
        return m.ToArray();
    }
    static List<Types.TItemRec> ItChain(Types.TItemRec h) { var l = new List<Types.TItemRec>(); for (var p = h; p != null; p = p.next) l.Add(p); return l; }
    static Types.TItemRec ItLink(IList<Types.TItemRec> l) { for (int i = 0; i < l.Count; i++) l[i].next = i + 1 < l.Count ? l[i + 1] : null; return l.Count > 0 ? l[0] : null; }
    static void SetMoney(params int[] v) { for (int i = 0; i < 7; i++) GDVars.treasureMoney.Data[i] = v[i]; }
    static string F(string n) { return Directory.GetFiles(smDir).Concat(Directory.GetDirectories(smDir)).FirstOrDefault(x => Path.GetFileName(x).ToLower() == n.ToLower()); }

    static void VaultFiles()
    {
        SmSetup();
        try
        {
            // forget
            File.WriteAllBytes(Fp("tmpvault.dat"), new byte[10]);
            Vault.ForgetSessionVault(); Yes(F("tmpvault.dat") == null, "forget deletes");
            Vault.ForgetSessionVault();
            foreach (int size in new[] { 0, 1, 2, 511, 512, 513, 1023, 1024, 1025, 5000, 70000 })
                foreach (char L in "AQ")
                {
                    var data = new byte[size]; rng.NextBytes(data);
                    File.WriteAllBytes(Fp("tmpvault.dat"), data);
                    // destination missing, then longer existing destination, then shorter
                    foreach (int dstLen in new[] { -1, size + 700, size > 3 ? size - 3 : 0 })
                    {
                        if (dstLen >= 0) File.WriteAllBytes(Fp("vault" + L + ".dat"), Enumerable.Repeat((byte)0xEE, dstLen).ToArray()); else if (F("vault" + L + ".dat") != null) File.Delete(F("vault" + L + ".dat"));
                        Vault.CommitToSlot((byte)L);
                        Yes(File.ReadAllBytes(F("vault" + L + ".dat")).SequenceEqual(data), "commit copy " + size + " dst " + dstLen);
                        Yes(File.ReadAllBytes(F("tmpvault.dat")).SequenceEqual(data), "source untouched");
                    }
                    // restore
                    var other = new byte[size + 5]; rng.NextBytes(other);
                    File.WriteAllBytes(Fp("vault" + L + ".dat"), other);
                    File.WriteAllBytes(Fp("tmpvault.dat"), new byte[900]);
                    Vault.RestoreFromSlot((byte)L);
                    Yes(File.ReadAllBytes(F("tmpvault.dat")).SequenceEqual(other), "restore copy " + size);
                    // missing source deletes the destination (present or not)
                    File.Delete(F("vault" + L + ".dat"));
                    Vault.RestoreFromSlot((byte)L); Yes(F("tmpvault.dat") == null, "restore without slot file forgets");
                    Vault.RestoreFromSlot((byte)L); Yes(F("tmpvault.dat") == null, "again");
                    File.WriteAllBytes(Fp("vault" + L + ".dat"), new byte[3]);
                    Vault.CommitToSlot((byte)L); Yes(F("vault" + L + ".dat") == null, "commit with empty session deletes slot file");
                }
            // commit/restore touch only the named slot
            File.WriteAllBytes(Fp("tmpvault.dat"), new byte[] { 1, 2, 3 }); File.WriteAllBytes(Fp("vaultB.dat"), new byte[] { 9 });
            Vault.CommitToSlot((byte)'A'); Yes(File.ReadAllBytes(F("vaultB.dat")).SequenceEqual(new byte[] { 9 }), "other slot untouched");
        }
        finally { SmDone(); }
    }

    static void VaultItems()
    {
        SmSetup();
        try
        {
            for (int n = 0; n <= 101; n++)
            {
                GDVars.treasureItems = ItLink(Enumerable.Range(0, n).Select(i => RandItem()).ToList());
                Eq(n < 100 ? 1 : 0, Vault.HasRoom() ? 1 : 0, "room with " + n);
            }
            GDVars.treasureItems = null; Yes(Vault.HasRoom(), "room empty");
            // deposit from head, middle, tail, only
            for (int round = 0; round < 300; round++)
            {
                var ch = new Types.TCharRecord();
                var pack = Enumerable.Range(0, 1 + rng.Next(5)).Select(i => RandItem()).ToList(); ItLink(pack); ch.itemsAddr = pack[0];
                var tre = Enumerable.Range(0, rng.Next(4)).Select(i => RandItem()).ToList(); GDVars.treasureItems = ItLink(tre);
                int pick = rng.Next(pack.Count); var item = pack[pick];
                var snapshot = PackItem(item); var before = tre.ToList();
                Vault.DepositItem(ch, item);
                var vault = ItChain(GDVars.treasureItems);
                Eq(before.Count + 1, vault.Count, "deposit count");
                for (int i = 0; i < before.Count; i++) Yes(ReferenceEquals(vault[i], before[i]), "existing treasure kept in order");
                var copy = vault[vault.Count - 1];
                Yes(!ReferenceEquals(copy, item), "deposit is a copy");
                Yes(PackItem(copy).SequenceEqual(snapshot), "copy has the item's fields");
                Yes(copy.next == null, "copy ends the list");
                var left = ItChain(ch.itemsAddr); var expect = pack.Where(x => !ReferenceEquals(x, item)).ToList();
                Yes(left.SequenceEqual(expect), "item removed from pack, others in order");
                item.name = "CHANGED"; Yes(copy.name != "CHANGED", "independent");
            }
        }
        finally { GDVars.treasureItems = null; SmDone(); }
    }

    static void VaultRead()
    {
        SmSetup();
        try
        {
            Vault.ReadVault(); Yes(!Vault.WasVisited(), "missing: not visited");
            SetMoney(5, 4, 3, 2, 1, 6, 7); GDVars.treasureItems = ItLink(new List<Types.TItemRec> { RandItem() }); var keep = GDVars.treasureItems;
            Vault.ReadVault();
            for (int i = 0; i < 7; i++) Eq(0, GDVars.treasureMoney.Data[i], "missing: money zeroed");
            Yes(ReferenceEquals(GDVars.treasureItems, keep) && keep.next == null, "missing: items kept");
            for (int round = 0; round < 400; round++)
            {
                GDVars.treasureItems = null;
                var existing = Enumerable.Range(0, rng.Next(3) == 0 ? rng.Next(99) : rng.Next(3)).Select(i => RandItem()).ToList(); GDVars.treasureItems = ItLink(existing);
                var fileItems = Enumerable.Range(0, rng.Next(6) == 0 ? rng.Next(110) : rng.Next(5)).Select(i => RandItem()).ToList();
                int[] money = Enumerable.Range(0, 7).Select(i => rng.Next(3) == 0 ? int.MinValue + rng.Next(5) : rng.Next(2) == 0 ? rng.Next() : -rng.Next()).ToArray();
                byte visited = (byte)rng.Next(256);
                var bytes = PackVault(1, visited, money, fileItems).ToList();
                int tail = rng.Next(4) == 0 ? 1 + rng.Next(62) : 0; for (int i = 0; i < tail; i++) bytes.Add((byte)rng.Next(256));
                File.WriteAllBytes(Fp("tmpvault.dat"), bytes.ToArray());
                SetMoney(9, 9, 9, 9, 9, 9, 9);
                Vault.ReadVault();
                Eq(visited != 0 ? 1 : 0, Vault.WasVisited() ? 1 : 0, "visited flag");
                for (int i = 0; i < 7; i++) Eq(money[i], GDVars.treasureMoney.Data[i], "money " + i);
                var got = ItChain(GDVars.treasureItems);
                int room = Math.Max(0, 100 - existing.Count);
                int added = Math.Min(room, fileItems.Count);
                Eq(existing.Count + added, got.Count, "item count after read");
                for (int i = 0; i < existing.Count; i++) Yes(ReferenceEquals(got[i], existing[i]), "existing first");
                for (int i = 0; i < added; i++) { string d = ItemDiff(fileItems[i], got[existing.Count + i]); Yes(d == null, "item " + i + " " + d); }
            }
            // rejected headers
            var good = PackVault(1, 1, new int[] { 1, 2, 3, 4, 5, 6, 7 }, new Types.TItemRec[] { RandItem() });
            Action<string, byte[]> rejected = (what, data) =>
            {
                File.WriteAllBytes(Fp("tmpvault.dat"), data); GDVars.treasureItems = null; SetMoney(8, 8, 8, 8, 8, 8, 8);
                Vault.ReadVault();
                Yes(!Vault.WasVisited() && GDVars.treasureItems == null, what + ": nothing taken");
                for (int i = 0; i < 7; i++) Eq(0, GDVars.treasureMoney.Data[i], what + ": money zero");
            };
            rejected("short header", good.Take(34).ToArray());
            rejected("empty", new byte[0]);
            rejected("version 0", PackVault(0, 1, new int[7], new Types.TItemRec[0]));
            rejected("version 2", PackVault(2, 1, new int[7], new Types.TItemRec[0]));
            rejected("sig length 3", PackVault(1, 1, new int[7], new Types.TItemRec[0], 3));
            rejected("sig VALU", PackVault(1, 1, new int[7], new Types.TItemRec[0], 4, "VALU"));
            rejected("sig valt", PackVault(1, 1, new int[7], new Types.TItemRec[0], 4, "valt"));
            // a partial last item (62 bytes, 1 byte, 0 bytes) is dropped; a zero visited byte means not visited
            foreach (int partial in new[] { 0, 1, 62 })
            {
                var one = new List<Types.TItemRec> { RandItem(), RandItem() };
                var data = PackVault(1, 0, new int[7], one).Concat(PackItem(RandItem()).Take(partial)).ToArray();
                File.WriteAllBytes(Fp("tmpvault.dat"), data); GDVars.treasureItems = null;
                Vault.ReadVault();
                Eq(2, ItChain(GDVars.treasureItems).Count, "partial tail " + partial); Yes(!Vault.WasVisited(), "visited byte 0");
            }
            // header only is a visited, empty vault
            File.WriteAllBytes(Fp("tmpvault.dat"), PackVault(1, 1, new int[] { 1, 2, 3, 4, 5, 6, 7 }, new Types.TItemRec[0]));
            GDVars.treasureItems = null; Vault.ReadVault(); Yes(Vault.WasVisited() && GDVars.treasureItems == null && GDVars.treasureMoney.Data[6] == 7, "header only");
        }
        finally { GDVars.treasureItems = null; SmDone(); }
    }

    static void VaultWrite()
    {
        SmSetup();
        try
        {
            for (int round = 0; round < 300; round++)
            {
                string dir = smDir + "sv" + round + Path.DirectorySeparatorChar + "inner" + Path.DirectorySeparatorChar;
                if (round % 3 == 0) dir = smDir; // existing directory
                GDVars.dsSaveDirectory = dir;
                var items = Enumerable.Range(0, rng.Next(5) == 0 ? rng.Next(101) : rng.Next(4)).Select(i => RandItem()).ToList();
                foreach (var it in items) it.next = new Types.TItemRec(); // stale links must not be written
                var ex = PackVault(1, 1, null ?? new int[0], new Types.TItemRec[0]);
                int[] money = Enumerable.Range(0, 7).Select(i => rng.Next(2) == 0 ? rng.Next() : -rng.Next()).ToArray();
                var expected = PackVault(1, 1, money, items);
                var staleCopy = items.Select(it => it.next).ToList();
                GDVars.treasureItems = null; { var l = items.ToList(); for (int i = 0; i < l.Count; i++) l[i].next = i + 1 < l.Count ? l[i + 1] : null; GDVars.treasureItems = l.Count > 0 ? l[0] : null; }
                SetMoney(money);
                int readsBefore = smio.Reads;
                Vault.WriteVault();
                string path = Directory.GetFiles(dir).Single(x => Path.GetFileName(x).ToLower() == "tmpvault.dat");
                Yes(File.ReadAllBytes(path).SequenceEqual(expected), "vault bytes round " + round);
                Eq(readsBefore, smio.Reads, "no prompt");
                Yes(GDVars.treasureItems == null, "treasure cleared");
                for (int i = 0; i < 7; i++) Eq(0, GDVars.treasureMoney.Data[i], "money cleared");
            }
            // round trip through ReadVault
            GDVars.dsSaveDirectory = smDir;
            var orig = Enumerable.Range(0, 12).Select(i => RandItem()).ToList(); var copy = orig.Select(it => ItemFromPacked(PackItem(it))).ToList();
            GDVars.treasureItems = ItLink(orig); SetMoney(1, -2, 3, -4, 5, -6, 7);
            Vault.WriteVault(); GDVars.treasureItems = null; Vault.ReadVault();
            Yes(Vault.WasVisited(), "visited after write"); Eq(-6, GDVars.treasureMoney.Data[5], "money back");
            var back = ItChain(GDVars.treasureItems); Eq(12, back.Count, "items back");
            for (int i = 0; i < 12; i++) Yes(ItemDiff(copy[i], back[i]) == null, "round trip item " + i);
            // a failed write still empties the pool and tells the player
            GDVars.treasureItems = ItLink(new List<Types.TItemRec> { RandItem() }); SetMoney(1, 1, 1, 1, 1, 1, 1);
            File.Delete(F("tmpvault.dat")); Directory.CreateDirectory(Fp("tmpvault.dat"));
            var e = new Scr(); e.Put(0, 24, 14, "The vault's ledger was not written.");
            string shot = null; smio.Keys.Enqueue(13);
            smio.OnRead = () => { shot = smscr.Diff(e, 24, 24); };
            Vault.WriteVault(); smio.OnRead = null;
            Yes(shot == null, "failure message: " + shot);
            Yes(GDVars.treasureItems == null && GDVars.treasureMoney.Data.All(x => x == 0), "failed write still clears the pool");
        }
        finally { GDVars.treasureItems = null; SmDone(); }
    }
    static Types.TItemRec ItemFromPacked(byte[] b) { return EclVm.ItemFromBytes(b, 0); }
}
