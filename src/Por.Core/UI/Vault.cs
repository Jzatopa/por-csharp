// Source: Talonius pool-of-radiance pinned 7616920a (src/vault.pas); Qwen3.8:27b initial draft, supervisor repaired.
using System;
using System.IO;
using System.Collections.Generic;


namespace Por.Core
{
    public static class Vault
    {
        public const int MAX_ITEMS = 100;
        private const string SESSION_FILE = "tmpvault.dat";
        private const string VAULT_SIG = "VALT";
        private const int VAULT_VERSION = 1;
        private const int COPY_BUF = 512;
        private const byte Yellow = 14;
        private static byte visited;

        private static string SlotFile(byte letter)
        {
            return GDVars.dsSaveDirectory + "vault" + (char)letter + ".dat";
        }

        private static void CopyOrDelete(string src, string dst)
        {
            if (!FileUtil.OpenReadOnly(out System.IO.FileStream fIn, src))
            {
                FileUtil.DeleteFile(dst);
                return;
            }

            bool ok = true;
            System.IO.FileStream fOut = null;
            try
            {
                fOut = new System.IO.FileStream(dst, System.IO.FileMode.Create, System.IO.FileAccess.Write);
            }
            catch (System.IO.IOException)
            {
                ok = false;
            }
            catch (System.UnauthorizedAccessException)
            {
                ok = false;
            }
            catch (System.ArgumentException)
            {
                ok = false;
            }
            catch (System.NotSupportedException)
            {
                ok = false;
            }

            if (ok)
            {
                byte[] buf = new byte[COPY_BUF];
                int n;
                do
                {
                    try
                    {
                        n = fIn.Read(buf, 0, COPY_BUF);
                    }
                    catch (System.IO.IOException)
                    {
                        n = 0;
                    }

                    if (n > 0)
                    {
                        try
                        {
                            fOut.Write(buf, 0, n);
                        }
                        catch (System.IO.IOException)
                        {
                            n = 0;
                        }
                    }
                } while (n > 0);

                fOut.Close();
            }

            fIn.Close();
        }

        public static void ForgetSessionVault()
        {
            FileUtil.DeleteFile(GDVars.dsSaveDirectory + SESSION_FILE);
        }

        public static void CommitToSlot(byte letter)
        {
            CopyOrDelete(GDVars.dsSaveDirectory + SESSION_FILE, SlotFile(letter));
        }

        public static void RestoreFromSlot(byte letter)
        {
            CopyOrDelete(SlotFile(letter), GDVars.dsSaveDirectory + SESSION_FILE);
        }

        private static ushort ItemCount()
        {
            ushort n = 0;
            Types.TItemRec p = GDVars.treasureItems;
            while (p != null)
            {
                n = unchecked((ushort)(n + 1));
                p = p.next;
            }
            return n;
        }

        public static bool HasRoom()
        {
            return ItemCount() < MAX_ITEMS;
        }

        public static void DepositItem(Types.TCharRecord creature, Types.TItemRec item)
        {
            CharHlp.AppendItemCopy(ref GDVars.treasureItems, item);
            CharHlp.RemoveItem(creature, item);
        }

        internal static byte[] ItemToBytes(Types.TItemRec it)
        {
            byte[] b = new byte[63];
            string name = it.name ?? "";
            int len = name.Length < 40 ? name.Length : 40;
            b[0] = unchecked((byte)len);
            for (int i = 0; i < len; i++)
                b[1 + i] = unchecked((byte)name[i]);
            b[41] = it.inUse;
            b[42] = 0; b[43] = 0; b[44] = 0; b[45] = 0;
            b[46] = it.itemType;
            b[47] = it.nameIds[1];
            b[48] = it.nameIds[2];
            b[49] = it.nameIds[3];
            b[50] = unchecked((byte)it.plus);
            b[51] = unchecked((byte)it.saveBonus);
            b[52] = it.equipped;
            b[53] = it.unidMask;
            b[54] = it.cursed;
            b[55] = unchecked((byte)(it.weight & 0xFF));
            b[56] = unchecked((byte)((it.weight >> 8) & 0xFF));
            b[57] = it.quantity;
            b[58] = unchecked((byte)(it.value & 0xFF));
            b[59] = unchecked((byte)((it.value >> 8) & 0xFF));
            b[60] = it.variantData[0];
            b[61] = it.variantData[1];
            b[62] = it.variantData[2];
            return b;
        }

        private static byte[] BuildHeader()
        {
            byte[] h = new byte[35];
            h[0] = 4;
            h[1] = (byte)'V';
            h[2] = (byte)'A';
            h[3] = (byte)'L';
            h[4] = (byte)'T';
            h[5] = VAULT_VERSION;
            h[6] = 1;
            for (int i = 0; i < 7; i++)
            {
                int v = GDVars.treasureMoney.Data[i];
                h[7 + i * 4 + 0] = unchecked((byte)(v & 0xFF));
                h[7 + i * 4 + 1] = unchecked((byte)((v >> 8) & 0xFF));
                h[7 + i * 4 + 2] = unchecked((byte)((v >> 16) & 0xFF));
                h[7 + i * 4 + 3] = unchecked((byte)((v >> 24) & 0xFF));
            }
            return h;
        }

        public static void ReadVault()
        {
            visited = 0;
            for (int i = 0; i < 7; i++)
                GDVars.treasureMoney.Data[i] = 0;

            if (!FileUtil.OpenReadOnly(out System.IO.FileStream f, GDVars.dsSaveDirectory + SESSION_FILE))
                return;

            try
            {
                byte[] head = new byte[35];
                int n = 0;
                try
                {
                    n = f.Read(head, 0, 35);
                }
                catch (IOException)
                {
                    n = 0;
                }

                bool ok = (n == 35)
                    && (head[0] == 4)
                    && (head[1] == (byte)'V')
                    && (head[2] == (byte)'A')
                    && (head[3] == (byte)'L')
                    && (head[4] == (byte)'T')
                    && (head[5] == VAULT_VERSION);

                if (ok)
                {
                    visited = head[6];
                    for (int i = 0; i < 7; i++)
                    {
                        int v = unchecked((int)(head[7 + i * 4 + 0]
                            | (head[7 + i * 4 + 1] << 8)
                            | (head[7 + i * 4 + 2] << 16)
                            | (head[7 + i * 4 + 3] << 24)));
                        GDVars.treasureMoney.Data[i] = v;
                    }

                    byte[] itemBuf = new byte[63];
                    while (true)
                    {
                        int rn = 0;
                        try
                        {
                            rn = f.Read(itemBuf, 0, 63);
                        }
                        catch (IOException)
                        {
                            rn = 0;
                        }
                        if (rn != 63)
                            break;
                        if (HasRoom())
                            CharHlp.AppendItemCopy(ref GDVars.treasureItems, EclVm.ItemFromBytes(itemBuf, 0));
                    }
                }
            }
            finally
            {
                f.Close();
            }
        }

        public static void WriteVault()
        {
            SaveMenu.CheckSaveDir();

            // The Pascal source writes each record separately; do not narrow a whole vault's length to Word.
            bool ok = true;
            try
            {
                using (var file = new FileStream(GDVars.dsSaveDirectory + SESSION_FILE, FileMode.Create, FileAccess.Write))
                {
                    byte[] header = BuildHeader(); file.Write(header, 0, header.Length);
                    for (Types.TItemRec p = GDVars.treasureItems; p != null; p = p.next)
                    {
                        byte[] record = ItemToBytes(p); file.Write(record, 0, record.Length);
                    }
                }
            }
            catch (IOException) { ok = false; }
            catch (UnauthorizedAccessException) { ok = false; }
            catch (ArgumentException) { ok = false; }
            catch (NotSupportedException) { ok = false; }
            if (!ok) TextRend.DisplayAndPause(Yellow, "The vault's ledger was not written.");

            CharHlp.ClearTreasure();
        }

        public static bool WasVisited()
        {
            return visited != 0;
        }
    }
}
