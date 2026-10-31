# Explicit combat item validation — 2026-10-24

## Accepted scope

The unchanged upstream wilderness save G now has one accepted native **Potion of Giant Strength** lifecycle. Actual exploration Trade moves the potion from Morrigan to Blair. In combat Blair unreadies the shield, readies the potion and selects Use. The potion is removed, STR rises **16→21**, and timed strength effect 38 appears. No creature loses HP and no memorized spell changes during the isolated action, which remains in round 0. Ordinary Quick-AI victory, cleanup, save/change/load, sidecar re-save and exit 0 follow. This validates one item action, not natural campaign completion or all item effects. No gameplay-rule change was needed.

## Why ranged remains open

The retained equipment audit decodes the original item table and all six fixture inventories: the party carries arrows (type 73) and a crossbow (type 46), but no bow and no crossbow bolts (type 28). A matching ranged launcher/ammunition pair is absent. No inventory was silently seeded to claim a ranged success. The same unchanged fixture instead provides the carried potion. Its class mask excludes Morrigan but permits Blair, so the replay uses real Trade; the two-hand rule also requires freeing Blair's shield hand. These are controller requirements, not engine defects.

## Replay

```bash
python3 scripts/test-wilderness-native.py --item \
  --fixture /path/to/pinned-upstream/tests/scenarios/saves/wild \
  --data /path/to/poolofrad --effects /path/to/SBSFX.DAT \
  --output /tmp/por-explicit-item-fresh
bash scripts/test-portable.sh
```

`--item` implies combat and managed deterministic seed 1. The six PCs load through native Load/G, perform Trade through actual menus, and walk to the step-11 encounter against two displacer beasts. Manual Done/Quit skips earlier actors until Blair receives a turn. No item counts, abilities, opponents, HP or combat statuses are forced. Managed fixed-seed repeatability does not establish DOS RNG parity. The launcher, native SDL window and OS keyboard input run under an isolated virtual X display; physical monitor/listening validation is not claimed.

## Source and observability

Pinned Pascal revision: `7616920a9178fa32efd45939b9360e95933bbf49`. `Magic.UseItemSpell` consumes charges and removes depleted items; `GiantStrengthHandler` sets strength to 21 and installs the timed restoration effect. These contracts remain in `src/Por.Core/Rules/Magic.cs`. `ViewChar` supplies actual Trade/Ready/Use menus. Only read-only desktop capture observability was expanded with `strCur` and `strExc`; production gameplay rules are unchanged.

## Evidence and acceptance

Accepted encounter evidence: `evidence/explicit-item-2026-10-24/pass-4/`. Fresh repeat: `pass-5/`; independently rerun review: `pass-6-reviewed/`. Repeats count as one scenario, not additional coverage.

- Retained native snapshots prove the original potion is transferred unchanged, readied, announced through `Magic.UseItemSpell`, consumed, and replaced by the source-defined strength effect. The before/after boundaries are actual manual CombatMenu inputs in the same round; all creature HP and memorized spells remain unchanged.
- The seven-round victory returns all six PCs to exploration with increased XP and cleared combat records. The quicksave fingerprint changes from absent to a real 13,137-byte file. A movement alters position before real F8 restores party, position, facing and clock.
- Blair remains STR21 with effect 38 duration 43 after load; the spent potion remains absent. Item/spell/effect sidecars survive a further real save byte-for-byte. The loaded six-name panel and readied-item capture were visually inspected.
- Camp/Save/cancel/Quit/Yes exits 0. All 168 original media files and 28 external fixture files are hash-identical; personal saves are not used.
- `check-oracle.py` runs the actual coverage validator and rejects seven planted faults: potion not consumed, strength not applied, effect missing, friendly damage, changed round, nonmanual context and potion restored after load. Positive evidence is accepted.
- Earlier `pass-1`, `pass-2` and `pass-3` have no accepted result and remain outside the ledger. Their captures are retained; they are not game-rule failures or successful scenarios.
- Portable test and extracted-source results are recorded separately in `result.json` and `clean-source-result.json` beside the retained logs.

## Tracker and limits

The ledger contains **9 fixture encounter outcomes** and **4 overlapping wilderness adventure replays**, not thirteen distinct battles. This pass adds only the explicit Giant Strength item lifecycle; earlier evidence is retained. The repeat saves share SHA-256 `183dc775c6a4507a2da0b467a5d5b4c4b2227df45a91a5784b1f8c7b9b465d50`.

Ranged actions, additional spells/items, turn undead, further recovery/economy branches, large icons/full capacity, natural quest progression, final battle and native ending remain open. Source closure (165 source/configuration inputs, 227 repository dispositions) is separate from runtime campaign validation. No remote publication or changes to COAB/Matrix are part of this pass.
