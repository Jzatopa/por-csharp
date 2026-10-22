// Mechanically transcribed from pinned fxengine.pas by scripts/generate-fxengine-conditions.py.
namespace Por.Core
{
    public static partial class FxEngine
    {
        private static byte[] ConditionEffects(byte cond)
        {
            switch(cond)
            {
                case C_Cond.ATTACKER_VETO: return new byte[]{C_Effect.GAZE_BLOCK};
                case C_Cond.TARGET_VETO: return new byte[]{63,C_Effect.BLINKING,C_Effect.INVISIBLE,C_Effect.DUST_DISAPPEAR};
                case C_Cond.HIT_PHASE_1: return new byte[]{C_Effect.DRAIN_ONE,C_Effect.DRAIN_TWO,C_Effect.ROT_TOUCH,C_Effect.GHOUL_PARA,C_Effect.FIRE_TOUCH,C_Effect.ACID_SQUIRT,C_Effect.BLOOD_ATTACH,C_Effect.REAR_CLAW_RAKE,C_Effect.BITE_ATTACH};
                case C_Cond.HIT_PHASE_2: return new byte[]{C_Effect.POISON_HIT0,C_Effect.POISON_HIT4,C_Effect.POISON_HIT2,C_Effect.KREEN_PARA,C_Effect.GHOUL_PARA,C_Effect.DRIDER_PARA,C_Effect.POISON_HIT_M2,C_Effect.FIRE_TOUCH,C_Effect.DRAIN_ONE,C_Effect.DRAIN_TWO,C_Effect.ROT_TOUCH};
                case C_Cond.WEAPON_DAMAGE_ATT: return new byte[]{C_Effect.ENFEEBLE,C_Effect.UNDEAD_SLAYER,C_Effect.FLAME_TONGUE};
                case C_Cond.WEAPON_DAMAGE_DEF: return new byte[]{C_Effect.PROT_MISSILES,C_Effect.KREEN_DODGE,C_Effect.BOULDER_DODGE,C_Effect.TROLL_HEAL,C_Effect.HALF_PIERCE,C_Effect.HALF_MAGICAL,C_Effect.NEED_MAGIC,C_Effect.SILVER_MAGIC,C_Effect.NEED_SILVER,C_Effect.HALF_BASH,60,C_Effect.MUMMY_FIRE,C_Effect.HOLY_WATER};
                case C_Cond.NONWEAPON_DAMAGE: return new byte[]{C_Effect.EFREETI_FIRE,C_Effect.RING_FIRE_RES,C_Effect.MUMMY_FIRE,60,C_Effect.IMMUNE_MM,C_Effect.RESIST_COLD,C_Effect.RESIST_FIRE,C_Effect.MAGIC_RES50,C_Effect.MAGIC_RES85,C_Effect.NO_FIRE,C_Effect.HALF_ELEC,C_Effect.HALF_COLD,C_Effect.SHIELD,C_Effect.HALF_FIRE,C_Effect.TROLL_HEAL,C_Effect.MIRROR_IMAGE};
                case C_Cond.TURN_START: return new byte[]{C_Effect.SNAKE_CHARM,C_Effect.PARALYSIS,C_Effect.SLEEP,C_Effect.HELPLESS};
                case C_Cond.COMBAT_START: return new byte[]{C_Effect.CAN_FIGHT_ON,C_Effect.DRAGON_FEAR,C_Effect.MUMMY_FEAR,C_Effect.DISPLACED,C_Effect.CAMOUFLAGE,C_Effect.RING_INVIS};
                case C_Cond.EFFECT_IMMUNITY: return new byte[]{C_Effect.MAGIC_RES50,C_Effect.MAGIC_RES85,C_Effect.ELF_RESIST,C_Effect.NO_SLEEP_CHARM,C_Effect.NO_PARALYSIS,C_Effect.NO_COLD,C_Effect.NO_PARA_POISON,C_Effect.NO_FIRE,C_Effect.HALF_ELF_RESIST,C_Effect.NO_CHARM_POISON};
                case C_Cond.ATTACK_ROLL_ATT: return new byte[]{C_Effect.BLESS,C_Effect.CURSE,C_Effect.BLIND,C_Effect.BESTOW_CURSE,C_Effect.PRAYER,C_Effect.UNDEAD_SLAYER,C_Effect.FLAME_TONGUE,C_Effect.GNOME_THAC0,C_Effect.DWARF_THAC0};
                case C_Cond.DEFENDER_AC: return new byte[]{C_Effect.BLIND,C_Effect.SHIELD,C_Effect.PROT_EVIL,C_Effect.PROT_GOOD,C_Effect.PROT_EVIL_10FT,C_Effect.PROT_GOOD_10FT,C_Effect.STINK_COUGH};
                case C_Cond.SAVING_THROW: return new byte[]{C_Effect.PROT_EVIL,C_Effect.PROT_GOOD,C_Effect.RESIST_COLD,C_Effect.SHIELD,C_Effect.RESIST_FIRE,C_Effect.BLIND,C_Effect.BESTOW_CURSE,C_Effect.PROT_EVIL_10FT,C_Effect.PROT_GOOD_10FT,C_Effect.PRAYER,C_Effect.RING_FIRE_RES,C_Effect.NO_PARA_POISON,C_Effect.NO_CHARM_POISON,C_Effect.CON_PSN_SAVE,C_Effect.CON_SPELL_SAVE};
                case C_Cond.HP_ZERO: return new byte[]{C_Effect.CAN_FIGHT_ON,C_Effect.TROLL_BURN,C_Effect.GASEOUS,C_Effect.BLOOD_DRAIN,C_Effect.BITE_HOLD};
                case C_Cond.MONSTER_SPECIAL: return new byte[]{C_Effect.PETRIFY_GAZE,C_Effect.CHARM_GAZE,C_Effect.DRAGON_BREATH,C_Effect.ANHKHEG_ACID};
                case C_Cond.ACTION_START: return new byte[]{C_Effect.SILENCE,C_Effect.STINK_COUGH,C_Effect.BITE_HOLD,C_Effect.BLOOD_DRAIN,C_Effect.CHARM};
                case C_Cond.ATTACK_ROLL_DEF: return new byte[]{C_Effect.INVISIBLE,C_Effect.DUST_DISAPPEAR,C_Effect.BLINKING,C_Effect.DWARF_AC_GIANT,C_Effect.GNOME_AC_BUG,C_Effect.DISPLACED};
                case C_Cond.MORALE_CHECK: return new byte[]{C_Effect.BLESS,C_Effect.CURSE,C_Effect.CHARM};
                case C_Cond.MOVE_ATTACK_RATE: return new byte[]{C_Effect.HASTED,C_Effect.SLOWED,C_Effect.IMMOBILE};
                case C_Cond.ROUND_END: return new byte[]{C_Effect.REGEN3,C_Effect.SPIRIT_HAMMER,C_Effect.CAMOUFLAGE,C_Effect.RING_INVIS,C_Effect.CHARM};
                default:return new byte[0];
            }
        }
        public static void TriggerEffectsWithCondition(byte cond,Types.TCharRecord creature)
        {
            foreach(byte id in ConditionEffects(cond))TriggerEffect(creature,id);
        }
    }
}
