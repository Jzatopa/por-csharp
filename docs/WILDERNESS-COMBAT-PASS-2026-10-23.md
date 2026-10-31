# Six-character wilderness combat pass — 2026-10-23

## Result and scope

The unchanged pinned upstream save G now passes an actual native wilderness encounter lifecycle: six PCs, two displacer beasts, ordinary Quick AI combat, victory, cleanup, exploration continuation, save/load and clean desktop exit. This is external-fixture coverage, **not natural full-campaign progression**. No gameplay rule change was needed in this pass.

Source references remain pinned to `7616920a9178fa32efd45939b9360e95933bbf49`; `tests/scenarios/wildfight.txt` is the route inspiration, not an assertion that our random sequence matches the DOS program.

## Reproducible path

```bash
python3 scripts/test-wilderness-native.py --combat \
  --fixture /path/to/pinned-upstream/tests/scenarios/saves/wild \
  --data /path/to/poolofrad --effects /path/to/SBSFX.DAT \
  --output /tmp/por-wilderness-combat-fresh
bash scripts/test-portable.sh
```

The new optional desktop `--deterministic` flag forwards `NORAND` through the existing startup seam and seeds the existing managed `Random(1)` implementation. Default launches remain nondeterministic. This **does not promise original DOS RNG parity**; the source upstream scenario describes step 32, whereas this managed replay encounters beasts on step 11. It changes no world/save/party fields and enables no STING/victory cheat. Actual keys select Combat, then Quick-all through Alt-Q; no Alt-X is used.

## Evidence

Accepted `evidence/wilderness-combat-2026-10-23/pass-1/`:

- All 28 upstream save files copied unchanged into disposable state; real Load/G imports all six PCs.
- Native Up/Right/Down/Left walk encounters two displacer beasts on step 11. Wilderness arena occupancy is 8, **not full capacity**.
- Battle entry and actual round progression recorded; round 4 ends with result 0 and the actual `PostCbt.LootMenu` call stack.
- All six PCs survive. HP changes MORRIGAN 51→48, GINS 15→10, BLAIR 22→17; other PCs retain 18/21/22 HP. XP increases for every PC, including multi-class BLAIR's source-distributed 5294→5346.
- Actual wilderness exploration menu resumes with all combat records cleared. No forced combat statuses or seeded creature edits.
- F5 creates a fresh 13,137-byte quicksave. A further move changes coordinates; F8 reaches a new exploration boundary and restores party, position, facing and clock. A subsequent write preserves all item/spell/effect sidecar hashes.
- Loaded image directly reviewed; all six border-excluding name rows contain 241/106/139/190/154/187 white/light-cyan pixels.
- Camp/Save/cancel/Quit/Yes exits 0. All 168 original media and 28 upstream fixture files remain hash-identical. Personal save state is never used.
- Installed SBSFX pack selects Blaster. This replay is not a new physical-listening or all-cue audio proof.

A second fresh replay (`pass-2/`) independently reaches the same step 11 encounter, round 4 victory, identical loaded party and identical world-save SHA-256; it is a repeatability check, not an additional tracker scenario. Comparison: `evidence/wilderness-combat-2026-10-23/repeat-comparison.json`.

The updated acceptance helper rejects seven planted faults: Character View submenu, unchanged altered position, lost memorized spells, no XP award, zero combat rounds, flee mislabeled victory, and blank party-name panel. Results: `evidence/wilderness-combat-2026-10-23/oracle-mutations.json`.

The full media-free portable gate passes all **17 projects**; output: `evidence/wilderness-combat-2026-10-23/portable.log`.

## Tracker

`campaign-combat-coverage.json` preserves all earlier accepted entries and now records **7 fixture encounter outcomes**, plus **2 wilderness adventure replays** (navigation-only and this combat replay). The combat replay appears in both appropriate categories; these are overlapping categories, not nine distinct encounters. Script 26 now has one wilderness victory lifecycle. Repository closure remains separately 165/165 source inputs and 227/227 file dispositions; those counts do not certify gameplay.

## Remaining

Natural campaign route, story prerequisites, final battle and ending remain unvalidated. Further wilderness encounters, large icons, full arena capacity, natural downing/bleeding/revival, multi-PC fleeing, and explicit ranged/spell/item/turn-undead action scenarios remain open. AI activity in a general battle does not establish exhaustive action-family coverage.
