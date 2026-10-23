# Verification

## Portable CI slice

`bash scripts/test-portable.sh` builds core/desktop and runs Adventure, Desktop, CbtMap, FxEngine, Integration, Types, Global, Pathing, GameIo, Border, Blit, Sym8x8, Debug and VideoCold tests. These use synthetic inputs and source-contract oracles embedded in code. Desktop PCM checks run without opening SDL unless `--audio-probe` is supplied. Test-generated temporary files and synthetic map pictures are not original game assets.

GitHub Actions runs the same portable lane. Native and extended checks below are separate from that lane.

## Source rescan

Obtain the pinned upstream checkout separately, then run:

```bash
python3 scripts/recompare-source.py --reference /path/to/pinned-checkout --output /tmp/source-audit.json
```

The scan checks all source hashes, target file existence and routine identifier presence plus explicit reviewed rename/inlining/platform mappings. It is structural, not semantic equivalence or original-binary execution.

## Native replay

Requires Linux SDL2, Xvfb, xdotool, Python 3 with Pillow, and original game media. State is disposable and original hashes are compared before/after.

```bash
python3 scripts/test-adventure-smoke.py --data /path/to/poolofrad --output /tmp/por-native-replay
python3 scripts/test-desktop-smoke.py --data /path/to/poolofrad --output /tmp/por-startup-replay
python3 scripts/test-combat-smoke.py --data /path/to/poolofrad --output /tmp/por-combat-replay
```

The combat replay uses a synthetic hero, creates a real quick-save, and changes only that disposable save's previous-script and position fields to seed the reported script-11 encounter. It checks combat entry and a later round through native keyboard input, not the natural travel route or campaign completion. The portable CbtMap `CombatEntry` group separately checks placement-to-redraw for zero, one, two, sixteen and maximum-slot rosters, plus an unplaceable party member.

For captured rather than physical audio, set `SDL_AUDIODRIVER=disk` and `SDL_DISKAUDIOFILE=/tmp/por-audio.raw`. For a system-device probe run `scripts/dotnet.sh run --project tests/Por.Desktop.Tests -- --audio-probe`.

## Historical extended suites

All current C# test source is retained. Some older suites consume local `evidence/` oracle files or original media and are intentionally not part of the evidence-free CI slice. Raw model exchanges, media-derived vectors, historical captures and personal working evidence are excluded from this source snapshot. They must not be mistaken for fixtures included here. The complete extended verification history stays in the local working project.

## Limits

Large assertion counts measure individual comparisons, not distinct gameplay scenarios. Source-derived oracles can share source misunderstandings. A passing host replay does not validate the full campaign, every combat spell/encounter, all hardware or original Turbo Pascal arithmetic equivalence.

## Campaign combat lifecycle

Use `python3 scripts/test-combat-smoke.py --complete` for a seeded duel victory through postcombat and save/load; specify `--script 20 --x 14 --y 1 --facing 6` for the Slums battle. Completion mode uses a synthetic level-8 fighter; the default entry/round smoke keeps the level-1 fixture. The level-1 fixture now uses the original internal AC convention (50 represents displayed AC 10). Capture snapshots include creature HP/status/XP, coins and item state; character-name white pixels are checked inside the text area, excluding decorative borders. The coverage summarizer revalidates accepted evidence instead of treating an existing result file alone as proof. See [campaign scope](CAMPAIGN-COMBAT.md).

Add `--defeat` to completion mode for a normal-level-1 Slums party-loss/main-menu-reload case. It records the actual game-over input boundary before queued input can clear the transient result. The default victory replay still requires XP growth and matching postcombat saved/loaded party state; a duel defeat cannot count as a victory merely because its partyDefeated flag is false.
