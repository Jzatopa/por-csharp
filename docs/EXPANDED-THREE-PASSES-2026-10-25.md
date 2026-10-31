# Three expanded native-validation passes — 2026-10-25

## Scope

Each requested pass covers **two distinct native runtime branches**, compared with one branch per pass in the previous batch. Every branch completes a separate combat/cleanup/save/change/load/re-save/exit lifecycle; fresh repeats are repeatability evidence, not additional coverage. This is doubled branch scope, not a claim about exact elapsed runtime or natural campaign progress.

| Pass | Branch A | Branch B |
|---|---|---|
| 1 — Ranged completion | Real short-range aimed volley damages the selected displacer beast **30→4 HP**, arrows **40→38**, no friendly HP change during the action. | Last two arrows are fired and the exhausted ammunition item is **removed**, not retained as a zero/underflow stack. The item stays absent after combat/save/load. |
| 2 — Healing boundaries | Gins casts Cure Light Wounds on **another PC**, Morrigan: **41→44 HP**; caster HP unchanged and one cure spent. | Gins self-heals **26→28 HP**, reaching maximum HP without exceeding it, and spends one cure. A separate real healing-service regression requests eight HP at 26/28 and verifies clipping at 28. |
| 3 — Stabilization and turning eligibility | Actual Done/Bandage changes seeded Shadow **DYING→UNCONSCIOUS**, resets bleeding counter, and leaves **0 HP/disabled**. It neither heals nor revives; that state survives cleanup/load. | Gins attempts Turn Undead against the real **non-undead** beasts: used-turn flag **0→1**, no hostile/friendly HP/status/effect changes. In the next round a second Turn key is ignored and produces no second resolution event. |

Accepted primary directories under `evidence/expanded-three-2026-10-25/`: `ranged-hit-3`, `ranged-empty-2`, `heal-ally-2`, `heal-cap-2`, `bandage-2`, `turn-ineligible-2`. Separate fresh repeats and final gate summaries are recorded in `repeatability.json` and `result.json`. All six match isolated action/outcome/party/world and main-save SHA-256. Five match every item/spell/effect sidecar byte; the ally-healing repeat differs only in one source-rendered inventory-name cache. The raw difference is retained and classified below, not claimed byte-identical.

### Lifecycle outcomes

The six primary victories complete in **3 / 6 / 5 / 5 / 4 / 5 rounds**, respectively. Each returns to actual wilderness exploration, awards XP, clears combat records and performs real F5, movement, F8 and sidecar re-save before in-game exit 0. Final-party state is not assumed undamaged: the last-ammunition branch leaves Shadow unconscious at 0 HP, correctly loaded and rendered with a red name. The bandaging branch also preserves an unconscious 0-HP Shadow. Those are not resurrection or natural knockout proofs.

## Synthetic setup, original preservation and limits

All native runs use the real launcher, SDL window, OS keyboard input, original game scripts, rules, renderer and serializers in disposable state. They begin with the pinned upstream six-PC save G, load via native Load/G, and reach the two-beast wilderness encounter on step 11 using managed deterministic seed 1. This does not establish DOS RNG/route parity, campaign balance, physical monitor/audio validation or full-game completion.

Each pre-load variant is recorded with exact changed offsets/values and original/variant SHA-256 hashes; acceptance reconstructs the original byte stream from the retained variant and verifies its pinned fixture fingerprint.

- **Ranged hit:** copied crossbow record becomes a long bow, with synthetic numerical **+5 bow and +5 arrows**. Native movement establishes short range before aiming. Movement path, spent movement, initial aim candidates and actual resolved target identity/coordinates are retained. No attack roll, HP or hostile is forced. This validates that enhanced ranged fixture, not ordinary starting gear, native equipment purchase, item-name presentation or balance.
- **Ammunition depletion:** copied bow has +5, carried arrows retain their original +1 and are reduced to two **before loading**. The observed initial volley misses but expends/removes the item; damage is proved separately by branch A.
- **Ally healing:** one copied Gins Bless slot becomes Cure Light Wounds and copied Morrigan HP becomes maximum minus ten.
- **HP ceiling:** the copied cure slot is the same, with Gins HP set to maximum minus two. Native observation proves the HP ceiling; the separate eight-HP service regression proves an actual excess-healing request is clipped, without inventing an uncaptured native die roll.
- **Bandaging:** copied Shadow HP/status/enabled are seeded as 0/DYING/disabled. The downed ally is placed by the real combat setup; no combat status is forced after loading. This is seeded stabilization, not natural injury/bleeding coverage.
- **Turning:** the original fixture is unchanged. The beasts' captured undead level is zero. Positive undead turning/destruction remains open.

All **168 original media files and 28 upstream fixture files** remain unchanged; personal saves are unused. COAB and Matrix remain untouched. No game-rule repair or remote publication is part of these passes.

## Replay and acceptance

```bash
python3 scripts/test-wilderness-native.py --action ranged-hit \
  --fixture /path/to/pinned-upstream/tests/scenarios/saves/wild \
  --data /path/to/poolofrad --effects /path/to/SBSFX.DAT \
  --output /tmp/ranged-hit-fresh
# Other actions, with independent fresh output directories:
# ranged-empty, heal-ally, heal-cap, bandage, turn-ineligible
python3 scripts/validate-expanded-actions.py /tmp/ranged-hit-fresh /tmp/ranged-empty-fresh \
  /tmp/heal-ally-fresh /tmp/heal-cap-fresh /tmp/bandage-fresh /tmp/turn-ineligible-fresh
python3 scripts/check-expanded-action-mutations.py /tmp/ranged-hit-fresh /tmp/ranged-empty-fresh \
  /tmp/heal-ally-fresh /tmp/heal-cap-fresh /tmp/bandage-fresh /tmp/turn-ineligible-fresh \
  --output /tmp/expanded-oracle-results.json
bash scripts/test-portable.sh
```

`native_expanded_actions.py` extends the replay controller, not gameplay. Optional read-only action-boundary diagnostics now cover returned Turn and actual Bandage resolution; desktop snapshots include HP maximum, undead level, quickfight, used-turn and bleeding counters. Normal runtime has no observer. A focused source-service regression adds twelve checks covering detection without mutation, exactly one bandage per action, enemy exclusion, bleeding reset, no free healing/revival, healing cap and combat stabilization without skipping the wake lifecycle.

Acceptance independently requires actual manual menu/call context, the selected eligible target, action-specific resource/flag transitions, isolated HP/status/effect changes, ordinary victory/cleanup, restored full party/world/clock, spent-resource persistence, original/variant hash checks, sidecar integrity, visible six-name rendering and exit 0. Disabled names are checked in the **LightRed color used by CharDisp**, still excluding the border and heading.

The same actual oracle rejects **54 planted faults** (nine per branch): missing resources/flags, missing/wrong effects, friendly damage, changed round, nonmanual context, lost persisted or once-only state, wrong target, mislabeled variant and modified originals. Mutated resolution snapshots remain consistent with their duplicated traces, avoiding rejection solely on duplicate mismatch. Full working-tree and extracted code-only test gates are recorded separately from native coverage.

## Repeatability cache distinction

The ally-healing runs have identical party state and main save SHA-256, but item record 3 of `CHRDATQ1.itm` carries ` Yes  Chain Mail +1 ` versus `Chain Mail +1 `. `CharHlp.ItemLabel` explicitly clears/rebuilds the Pascal String[40] and prefixes the equipped marker only for inventory display; the pinned Pascal source documents the same behavior. Rendering can therefore leave different cached labels after otherwise equivalent play.

`compare-native-repeats.py` retains the raw differences, permits classification only within item-record bytes 0–40, and requires **all bytes 41–62** (flags, links, type/name IDs, bonuses, readiness, quantities, weight/value, variant) to match. It rejects a planted change at each of those 22 payload offsets. Other `.mem`/`.spc` sidecars must match exactly. Every run separately verifies its own sidecars survive real load/re-save byte-for-byte. This is semantic repeatability with one documented display-cache difference, not complete cross-run binary identity.

## Rejected attempts and controller corrections

- `ranged-hit-1` completed a lifecycle but missed at long range; it is **not hit evidence**. Shorter native range and explicitly stronger copied arrows define the accepted hit fixture.
- `ranged-empty-1` completed combat/save/load but its UI oracle did not yet accept the source-defined red name of an unconscious member. It is not an accepted complete lifecycle.
- `heal-ally-1` entered a ViewChar item submenu during generic Return/auto-key spam and exceeded the controller budget. The controller now branches on the actual call stack, sends automatic-combat keys only from the manual CombatMenu, and exits unexpected item submenus explicitly. No gameplay defect is inferred from that attempt.
- `turn-ineligible-2` paused at the source-defined **Continue Battle** confirmation after both beasts went down. The owned live replay was inspected and continued with native No input; its existing evidence was retained, not restarted. Subsequent controllers explicitly decline that confirmation. This proves the decline path, not Continue/Yes or positive undead turning.
- `ranged-hit-2` and earlier successful runs remain useful bounded evidence but are not counted again. The accepted primary list above defines ledger additions.

## Coverage accounting and remaining work

The ledger records **18 fixture encounter-outcome runs**, with **13 overlapping wilderness adventure lifecycles**. These are not thirty-one distinct encounters and do not add new original encounter identities merely because a fixture/action differs. Exactly six branches are added to the previous twelve outcomes. Source closure remains separately reconciled at 165 inputs / 227 repository dispositions.

Open gates include positive undead turning/destruction, naturally acquired equipment, natural injury/bleeding/revival, additional ranged/status/capacity/large-icon interactions, quest prerequisites, natural campaign progression and final battle/ending. `campaign_completion` remains false. No unattended campaign run is implied.
