# Campaign combat validation

Status: incomplete. The current ledger records 18 fixture encounter outcomes and 13 overlapping wilderness adventure lifecycles, not thirty-one distinct encounters. No natural-route campaign completion is claimed.

## Accepted native outcomes

| Encounter | Evidence observed |
|---|---|
| Training-hall duel, script 11 (7,1) | Eight-round victory, XP award, opponent/combat-record cleanup, return to exploration, HP/XP preserved by real F5/F8 save/load |
| Slums, script 20 (13,1), four orcs | Four-round victory, cleanup, acquired silver, carried sword/platinum and HP/XP preserved by real F5/F8; character-name text visible after load |
| Slums defeat/recovery, normal level-1 fighter | Recorded game-over result `$80` and empty party; main-menu reload restores HP 20 / XP 0, no combat record and visible party-name text |

These use disposable synthetic saved parties and real native SDL/X11 keyboard input, scripts, renderer, dice/AI and serializers. Seeds are not natural campaign travel. Level-8 victory fixtures accelerate lifecycle coverage; they do not validate campaign balance. Original media and personal saves are not changed. Replay controllers terminate only their own test windows after assertions; this is not a claim of an in-game clean quit.

The static classifier analyzed all 29 original ECL script blocks, independently cross-checked against archive directories. It identified 1,301 fight-classified map squares and 1,466 unresolved event squares; these are not distinct encounter counts or runtime passes. Dynamic/state-dependent encounters and three unresolved wilderness geometry references remain outside this bounded scan. See [coverage ledger](campaign-combat-coverage.json).

## Replay

Requires Linux SDL2, Xvfb, xdotool, Python 3/Pillow, .NET 8 and your original media. Use fresh output paths.

```bash
python3 scripts/test-combat-smoke.py --complete --output /tmp/training-new-run
python3 scripts/test-combat-smoke.py --complete --script 20 --x 14 --y 1 --facing 6 --output /tmp/slums-new-run
python3 scripts/test-combat-smoke.py --complete --defeat --script 20 --x 14 --y 1 --facing 6 --output /tmp/defeat-new-run
scripts/dotnet.sh run --project tests/Por.EvtKind.Media -- /path/to/poolofrad /tmp/scan.md /tmp/inventory.json
python3 scripts/update-campaign-combat-coverage.py --inventory /tmp/inventory.json --evidence /tmp/training-new-run /tmp/slums-new-run --defeat-evidence /tmp/defeat-new-run
```

Pass `--data /path/to/poolofrad` to native replays when media is not in the launcher's usual location. Raw scans and captures remain local and are not bundled. The ledger distinguishes synthetic native outcomes from natural campaign progression.

## Review and remaining work

Independent Claude review confirmed the new victory/persistence evidence. Pinned Pascal corroborates the intentional flee result `$81`, disabled-monster defeat counting, and unconditional transient-modifier reset. These are not identified implementation defects.

Additional accepted seeded runs cover actual single-PC fleeing, a pre-seeded unconscious positive-HP ally recovering, and native dropped-item acquisition, each through actual exploration/save/load. The earlier six outcomes have expanded to 18 fixture outcomes in schema 4, including six-PC wilderness victory, explicit Magic Missile, Giant Strength potion, ranged firing/miss, Cure Light Wounds and Bless. Thirteen wilderness adventure entries overlap these outcomes. The latest six branches cover enhanced ranged damage, last-ammunition removal, ally healing, HP ceiling, seeded bandaging, and ineligible/once-only turning; see [expanded passes](EXPANDED-THREE-PASSES-2026-10-25.md). Ranged/healing use explicitly documented disposable pre-load variants; Bless uses the unchanged fixture. See [three action passes](THREE-ACTION-PASSES-2026-10-24.md). Pending: natural campaign progression and quest prerequisites; late-game/final battles; multi-PC flee and natural downing/revival; additional ranged/spell/item/status interactions and positive undead turning; additional wilderness types/large-icon/capacity cases; additional loss/recovery variants. These narrow cases do not establish full-game correctness. Defeat state is recorded at the actual game-over input boundary; timing-based controller attempts are not counted as passes.

See [six-character wilderness pass](WILDERNESS-PASS-2026-10-23.md) for fixture scope and the explicit stabilization of an undefined legacy read.
