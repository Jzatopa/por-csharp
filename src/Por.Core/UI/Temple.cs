// Direct conversion of pinned src/temple.pas (7616920a).
using System;
using static Por.Core.Types;
namespace Por.Core
{
    // Algorithms in FxEngine/Magic remain unconverted; a host must supply these actual operations.
    public interface ITempleEffects
    {
        bool HasDisease(TCharRecord c);
        void RemoveDiseases(TCharRecord c);
        void RemoveEffect(TCharRecord c, byte id, TEffectRec effect);
        bool RestoreHitPoints(TCharRecord c, byte hp, bool message);
        void RemoveCurseSpell();
    }
    public static class Temple
    {
        public static ITempleEffects Effects;
        private static ITempleEffects Fx { get { if (Effects == null) throw new NotSupportedException("Temple: FxEngine/Magic services not bound."); return Effects; } }
        // Defaults are real converted dependencies. Internal seams permit source-ordered UI verification.
        internal static Func<string,byte> ConfirmAnywayCall = ConfirmCureAnyway;
        internal static Func<string,ushort,byte> ConfirmCostCall = ConfirmCureCost;
        internal static Action DisplayInterface = CharDisp.DisplayNoncombatInterface;
        internal static Action<TCharRecord> DisplayParty = CharDisp.DisplayParty;
        internal static byte ConfirmCureAnyway(string msg)
        {
            CharDisp.PrintNameString(GDVars.curChar,msg,0,false);
            byte c = MenuCore.YesNoMenu("cast cure anyway: ",13,10,15);
            CharDisp.ClearMessageWindow(); return c;
        }
        internal static byte ConfirmCureCost(string spell, ushort cost)
        {
            if (spell.Length > 255) spell = spell.Substring(0,255);
            spell += " will only cost " + StrUtil.NumStr(cost) + " gold pieces.";
            if (spell.Length > 255) spell = spell.Substring(0,255);
            TextRend.ShowMessage(10,true,spell);
            byte c = MenuCore.YesNoMenu("pay for cure ",13,15,10);
            if (c == 'Y' && !Money.PayGold(GDVars.curChar,cost)) c = (byte)'N';
            if (c == 'Y') { CharDisp.ClearMessageWindow(); CharDisp.PrintNameString(GDVars.curChar,"is cured.",0,true); }
            return c;
        }
        private static bool Has(byte id) { TEffectRec effect; return CharHlp.HasEffect(GDVars.curChar,id,out effect); }
        internal static void CureBlindness()
        {
            byte c = (byte)'Y';
            if (!Has(C_Effect.BLIND)) c = ConfirmAnywayCall("is not blind.");
            if (c == 'Y' && ConfirmCostCall("Cure Blindness",1000) == 'Y') Fx.RemoveEffect(GDVars.curChar,C_Effect.BLIND,null);
        }
        internal static void CureDisease()
        {
            byte c = (byte)'Y';
            if (!Fx.HasDisease(GDVars.curChar)) c = ConfirmAnywayCall("is not Diseased.");
            if (c == 'Y' && ConfirmCostCall("Cure Disease",1000) == 'Y') Fx.RemoveDiseases(GDVars.curChar);
        }
        internal static void CureWounds(byte spell)
        {
            if (spell < 1 || spell > 3) return;
            string name = spell == 1 ? "Cure Light Wounds" : spell == 2 ? "Cure Serious Wounds" : "Cure Critical Wounds";
            ushort cost = (ushort)(spell == 1 ? 100 : spell == 2 ? 350 : 600);
            byte bonus = (byte)(spell == 1 ? 0 : spell == 2 ? 1 : 3);
            if (ConfirmCostCall(name,cost) == 'Y') Fx.RestoreHitPoints(GDVars.curChar,unchecked((byte)(Dice.RollDice(spell,8)+bonus)),false);
        }
        internal static void RaiseDead()
        {
            TCharRecord c = GDVars.curChar;
            bool dead = c.status == C_Status.DEAD || c.status == C_Status.ANIMATED;
            byte answer = (byte)'Y';
            if (!dead) answer = ConfirmAnywayCall("is not dead.");
            if (answer != 'Y' || ConfirmCostCall("Raise Dead",5500) != 'Y' || !dead) return;
            GDVars.effectLock = 1;
            Fx.RemoveEffect(c,C_Effect.ANIMATE_DEAD,null); Fx.RemoveEffect(c,C_Effect.POISONED,null);
            GDVars.effectLock = 0;
            c.hpCur = 1; c.status = C_Status.OKAY; c.enabled = 1;
            Stats.Count(c,Stats.RAISED,1);
            if (c.conCur > AbilAdj.SCORE_MIN)
            {
                c.conCur--;
                short loss = unchecked((short)(ClassCal.ConHpTotal(c,unchecked((byte)(c.conCur+1)))-ClassCal.ConHpTotal(c,c.conCur)));
                if (loss >= c.hpMax) loss = unchecked((short)(c.hpMax-1));
                if (loss > 0) c.hpMax = unchecked((byte)(c.hpMax-loss));
            }
        }
        internal static void NeutralizePoison()
        {
            bool found = Has(C_Effect.POISONED);
            bool slowed = Has(C_Effect.SLOW_POISON_END) || Has(C_Effect.SLOW_POISON_HIT);
            byte c = (byte)'Y';
            if (!found) c = ConfirmAnywayCall("is not poisoned.");
            if (c != 'Y' || ConfirmCostCall("Neutralize Poison",1000) != 'Y') return;
            GDVars.effectLock = 1;
            Fx.RemoveEffect(GDVars.curChar,C_Effect.POISONED,null);
            Fx.RemoveEffect(GDVars.curChar,C_Effect.SLOW_POISON_END,null);
            Fx.RemoveEffect(GDVars.curChar,C_Effect.SLOW_POISON_HIT,null);
            GDVars.effectLock = 0;
            if (found && !slowed && GDVars.curChar.status == C_Status.DEAD)
            { GDVars.curChar.hpCur = 1; GDVars.curChar.status = C_Status.OKAY; GDVars.curChar.enabled = 1; }
        }
        internal static void RemoveCurse()
        {
            bool cursed = false;
            for (TItemRec item = GDVars.curChar.itemsAddr; item != null && !cursed; item = item.next) cursed = item.cursed != 0;
            byte c = (byte)'Y';
            if (!cursed && !Has(C_Effect.BESTOW_CURSE)) c = ConfirmAnywayCall("is not cursed.");
            if (c == 'Y' && ConfirmCostCall("Remove Curse",3500) == 'Y')
            { GDVars.dsTargetCreaturePtr[1] = GDVars.curChar; Fx.RemoveCurseSpell(); }
        }
        internal static void StoneToFlesh()
        {
            byte c = (byte)'Y';
            if (GDVars.curChar.status != C_Status.STONED) c = ConfirmAnywayCall("is not stoned.");
            if (c == 'Y' && ConfirmCostCall("Stone to Flesh",2000) == 'Y' && GDVars.curChar.status == C_Status.STONED)
            { GDVars.curChar.status = C_Status.OKAY; GDVars.curChar.enabled = 1; GDVars.curChar.hpCur = 1; }
        }
        public static void HealMenu()
        {
            TItemRec list = null; MenuCore.AllocStringList(10,ref list);
            byte i = 1;
            for (TItemRec p = list; p != null; p = p.next) { p.name = GDConsts.templeSpells[i++]; p.inUse = 0; }
            TItemRec sel = list;
            TextRend.ClearRect(0,24,39,24); bool redraw = true;
            Border.DrawWindow(C_Msg.TEXT_LEFT,C_Msg.TEXT_TOP,C_Msg.TEXT_RIGHT,C_Msg.TEXT_BOTTOM,0,15,"");
            Border.DrawWindow(1,1,38,1,0,15,""); short idx = 0; bool done = false;
            do
            {
                string s = GDVars.curChar.name + ", how can we help you?"; if (s.Length > 40) s = s.Substring(0,40);
                TextRend.DisplayString(1,1,15,s);
                Border.DrawWindow(C_Msg.TEXT_LEFT,C_Msg.MESSAGE_TOP,C_Msg.TEXT_RIGHT,C_Msg.TEXT_BOTTOM,0,15,"");
                byte c = MenuCore.MenuVertical("","Heal Exit",13,10,15,2,4,38,15,list,false,ref redraw,ref idx,ref sel);
                if (c == 'H' || c == C_Keys.ENTER)
                {
                    switch (idx)
                    {
                        case 0: CureBlindness(); break; case 1: CureDisease(); break;
                        case 2: CureWounds(1); break; case 3: CureWounds(2); break; case 4: CureWounds(3); break;
                        case 5: NeutralizePoison(); break; case 6: RaiseDead(); break; case 7: RemoveCurse(); break;
                        case 8: StoneToFlesh(); break; case 9: done = true; break;
                    }
                }
                else if (c == C_Keys.NONE) done = true;
            } while (!done);
            MenuCore.FreeStringList(ref list); TextRend.ClearRect(0,0,39,24); DisplayInterface(); DisplayParty(GDVars.curChar);
        }
        public static void TempleMainMenu() { Shop.CounterMenu(true,HealMenu); }
    }
}
