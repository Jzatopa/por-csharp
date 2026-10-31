// Regression: readying a Cloak of Displacement (item effect 89) calls EffDisplaced with p = nil via EffReadyItem.
// The port threw NullReferenceException (natural campaign crash 2026-10-29, state/logs/crash-20261029-042020-4880805.txt).
using System;
using Por.Core;
using static Por.Core.Types;

static partial class Tests
{
    static void EhDisplacedReadyNil()
    {
        var c = new TCharRecord();
        byte savedRound = GDVars.roundCounter; sbyte savedRoll = GDVars.gAttackRoll;
        try
        {
            GDVars.roundCounter = 0; GDVars.gAttackRoll = 0;
            EffHndlr.EffDisplaced(c, null, false);
            Eq(0, GDVars.gAttackRoll, "nil displaced record leaves attack roll untouched outside combat");
            GDVars.roundCounter = 3; GDVars.gAttackRoll = 12;
            EffHndlr.EffDisplaced(c, null, false);
            Eq(12, GDVars.gAttackRoll, "nil displaced record does not cancel an attack");
            var e = new TEffectRec { strength = 0x0C };
            GDVars.roundCounter = 3; GDVars.gAttackRoll = 12;
            EffHndlr.EffDisplaced(c, e, false);
            Eq(-1, GDVars.gAttackRoll, "real displaced record still cancels the first attack of a round");
            Eq(0x1C, e.strength, "real displaced record marks the round");
        }
        finally { GDVars.roundCounter = savedRound; GDVars.gAttackRoll = savedRoll; }
    }
}
