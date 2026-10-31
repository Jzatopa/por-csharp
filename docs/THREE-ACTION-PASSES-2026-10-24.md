# Three bounded native action passes — 2026-10-24

## Results

| Pass | Isolated manual action | Completed lifecycle |
|---|---|---|
| Ranged fire, miss branch | Morrigan unreadies shield and sword, readies long bow and arrows, aims at a real hostile at range 16; arrows **40→38**, all creature HP unchanged. Actual attack target pointer agrees with the selected hostile and position. | Four-round victory; arrows **32** remain after combat and real save/load; cleanup, sidecar re-save and exit 0. This is **not ranged hit/damage coverage**. |
| Cure Light Wounds | Gins selects the memorized spell, targets self, consumes one spell and heals **18→21 HP**, with all other creature HP/effects unchanged at resolution. | Five-round victory; later enemy attacks leave Gins at **8 HP**, correctly saved/loaded. The cure remains spent; cleanup, sidecar re-save and exit 0. |
| Bless and melee exclusion | Gins selects an existing Bless. Casting time lets the beasts approach before targeting. One spell is spent; **Morgan, Raven and Shadow** receive effect 1 for six rounds. The engaged front line/caster and hostiles do not. No HP changes during resolution. | Six-round victory; expired Bless effects are absent after cleanup/load and the spent spell stays spent. Sidecar re-save and exit 0. |

These are three fixture-based validation passes, not three natural campaign milestones. No game-rule repair was needed. Full campaign/final battle/ending and ranged hit/damage remain open.

## Fixture scope and preservation

All runs copy the pinned upstream six-PC wilderness save G into fresh disposable state before native loading. Original media and upstream files are never edited.

- **Ranged** deliberately changes only one copied item record: incompatible crossbow type 46 becomes an unenchanted long bow type 43, with matching name/readiness fields cleared/set. Existing arrows are retained. This is synthetic equipment, **not shop acquisition**; inherited weight/value are not evidence about retail bow pricing.
- **Healing** deliberately changes copied `CHRDATG2.SAV`: one memorized Bless becomes Cure Light Wounds (spell 3), and current HP becomes maximum HP minus ten, observed as 18/28 after loading. This is a seeded injury/spell, **not natural damage followed by rest/memorization**.
- **Bless** uses the unchanged original fixture and its already memorized spell.

Each `fixture-variant.json` records exact byte offsets, old/new values and before/after hashes. The acceptance oracle reconstructs each original file from the retained disposable variant and checks it against the original fixture hash. All **168 original media files and 28 upstream fixture files** remain unchanged. Personal saves are not used. COAB and Matrix are untouched.

## Evidence and replay

Accepted evidence under `evidence/three-passes-2026-10-24/`:

- `ranged-4/`, repeated action/outcome/save comparison with `ranged-3/`.
- `heal-3/`, fresh repeat of `heal-2/`.
- `bless-4/`, fresh repeat of `bless-3/`.
- `repeatability.json`, `oracle-mutations.json`, `result.json`, regression logs and clean-source manifest verification.

Fresh repeats match initial/action/outcome/loaded party state, encounter and round boundaries, world/clock, sidecars and save SHA-256. The only normalized capture fields are the newly added read-only selected-target diagnostics in combat records. Repeats count as repeatability evidence, not additional scenarios.

```bash
python3 scripts/test-wilderness-native.py --action ranged \
  --fixture /path/to/pinned-upstream/tests/scenarios/saves/wild \
  --data /path/to/poolofrad --effects /path/to/SBSFX.DAT \
  --output /tmp/ranged-fresh
# Repeat with --action heal and --action bless, each with a fresh output directory.
python3 scripts/validate-three-actions.py /tmp/ranged-fresh /tmp/heal-fresh /tmp/bless-fresh
python3 scripts/check-three-action-mutations.py /tmp/ranged-fresh /tmp/heal-fresh /tmp/bless-fresh \
  --output /tmp/action-oracle-results.json
bash scripts/test-portable.sh
```

These run the real launcher, SDL window and OS keyboard input on isolated virtual X displays. Managed deterministic RNG uses seed 1 and encounters the two beasts on movement 11; **DOS RNG/route parity is not established**. Physical monitor/listening validation is not claimed.

## Diagnostics and independent acceptance

An optional `--trace-combat-actions` host flag installs a read-only callback at the existing combat adapter's returned manual aim/cast boundaries. Normal runtime has no observer. The callback captures before the next creature action, including delayed spell resolution; it does not supply input, roll dice or change combat state. A focused adapter regression checks post-service observation, unchanged HP, no duplicate event, exclusion of quick-AI casting and inert null-host behavior. Selected attack identity/position is now included in desktop capture diagnostics.

This resolves two controller assumptions without changing gameplay:

1. Delayed screen captures can include enemy damage after a successful cure. Acceptance uses the actual resolved action capture, not that later screen.
2. `AimTargetMenu` reuses the global scratch target list for its range test. Ranged selection is checked against the **initial** aiming-list capture and actual resolved attack target, not the mutated scratch list. The displayed hostile/range and loaded party screens were inspected.

The resolution framebuffer is captured before the next character-info redraw; healing proof is the read-only state delta, not a claim that the old aim panel already shows the new HP. Loaded-party rendering is checked separately for all six visible names.

The actual acceptance oracle requires resource decrement, correct target/effect or exact miss outcome, unchanged friendly state, same combat round, manual call context, ordinary victory/cleanup, changed world state before real F8, restored party/world, sidecar hashes, visible loaded names and exit 0. **27 planted faults** are rejected across the three records: unspent/restored resources, incorrect/missing effects, friendly damage, changed round, nonmanual context, wrong target, mislabeled fixture and modified inputs. Duplicated resolution snapshots are mutated consistently, so rejection does not rely on a trivial duplicate mismatch.

Earlier incomplete attempts are retained but excluded: `ranged-1` did not unready the sword; `ranged-2` completed the firing lifecycle but predates final diagnostics; `heal-1` captured subsequent enemy damage; `bless-1` polled before casting finished; `bless-2` incorrectly expected the engaged caster to receive Bless. These are controller/acceptance assumptions, not confirmed engine-rule defects.

## Coverage accounting

Ledger schema 4 records **12 fixture encounter outcomes** and **7 overlapping six-PC wilderness adventure lifecycles**. These categories must not be summed into nineteen distinct encounters. Nine prior accepted outcomes are retained; exactly three new action scenarios are added. Source closure (165 inputs/227 repository dispositions) is a separate claim.

Full working-tree and externally extracted code-only portable gates are recorded separately in the evidence result. No remote upload is part of this pass.
