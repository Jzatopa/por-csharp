# Wilderness and six-character persistence pass — 2026-10-23

## Tracker review

The source tracker contains 165 inputs, 154 handled and 11 open. The open entries are test/configuration/benchmark inputs, including the sound trace/digest tests; this pass does not mark them converted. All 129 game-source inputs remain dispositioned, which is not campaign completion.

The campaign tracker retains its six accepted seeded combat outcomes across scripts 11 and 20. Schema 3 adds `native_adventure_scenarios`, separate from combat outcomes. One upstream-fixture wilderness replay for script 26 is accepted. `campaign_completion` remains false and `natural_campaign_runs` remains empty.

## Reachable crash and explicit compatibility decision

An unchanged upstream six-character save G loads and renders central wilderness, but its first northward movement crashed with `CELL_35F read is undefined` in the real ECL COMPARE path.

Pinned `src/eclvars.pas:422` and current upstream commit `7b76dfd4cf4e784e78d874727d5b6a37c65e433b` leave the result unassigned. Ordinary `objdump -m i8086` inspection of original `game.ovr` corroborates this: the reader at file offsets `0x86DE..0x8830` compares address `0x35F` at `0x87C0`, reaches epilogue `0x8828` without assigning its result, then returns the stale word at `SS:[BP-2]`. These are file offsets, not live DOS addresses. This was static inspection, not REA output or a live DOS trace.

The managed reader now deliberately returns zero for this one cell. **Zero is a deterministic compatibility policy, not a recovered or source-defined SSI return value.** It avoids a fatal managed exception without exposing arbitrary stack contents. All other unsupported/write-only read guards remain. The source tracker records this exception to equivalence.

A connected regression runs actual operand decoding, `GetEclVar`, `ReadGbvm`, and COMPARE for both address operand tags and all four cardinal facings. It failed on the previous exception and passes with this policy. The existing negative tests still check other undefined reads. The portable entry point runs the 24 connected compatibility checks only: the older whole EclVars suite also reads historical local string/skip oracles and is not labeled evidence-free.

## Accepted native evidence

`evidence/wilderness-pass-2026-10-23/native-4/` contains captures, snapshots, log, output saves and `result.json`.

- Copies all 28 pinned upstream fixture files unchanged into disposable state, then uses native Load/G. No world or character fields are patched.
- Confirms six living party members, central wilderness interface state 3, script 26, and a visible wilderness map/party panel.
- Real north/east/south moves change wilderness coordinates `(14,27) → (14,26) → (15,26) → (15,27)` and advance the game clock. Wilderness coordinates are zone0 party fields, not town `gMapX/gMapY`.
- F5 creates a new 13,137-byte quicksave; a further northward move changes position; F8 reaches a new exploration boundary and restores coordinates, facing, clock and all six party snapshots.
- Snapshots include HP/status/enabled/XP, coins, full item records, known/memorized spells and effects. The six inventories contain 13/4/3/3/2/3 items.
- A second real F5 after loading preserves all 13 item, usual-spell and effect sidecars byte-for-byte.
- Border-free name rows show 241/106/139/190/154/187 white/light-cyan text pixels. Loaded image inspected directly.
- Leaves through Camp/Save/cancel/Quit/Yes with exit code 0. All 168 original media files and all 28 upstream fixture files hash-identical afterward. Personal save state was not used.
- Installed full sampled pack selected Blaster; this is not another all-cue playback test.

Attempts 1–3 are retained but not accepted: attempt 1 assumed town interface state 4; attempt 2 found the real crash; attempt 3 completed movement/load but its image oracle expected dark instead of light cyan. Only attempt 4 enters the tracker.

## Verification

- EclVars: 916,988 assertions, including the deliberately non-source-equivalent compatibility checks; before-fix failure retained.
- EclMenu: 2,011,308 source-derived assertions.
- EclVm: 569,151 assertions, zero failed groups.
- Acceptance oracle rejects five planted faults: unchanged-state/no-op load, Character View submenu, lost memorized-spell state, blank name panel, and missing persisted sidecar.
- Existing 14-project portable gate passed. EclPair’s dedicated media-free `WildernessCompatibility` entry point is now included so the new regression also runs in the 15-project portable/CI gate; final clean-source result recorded in the local evidence summary.

```bash
python3 scripts/test-wilderness-native.py \
  --fixture /path/to/pinned-upstream/tests/scenarios/saves/wild \
  --data /path/to/poolofrad --effects /path/to/SBSFX.DAT \
  --output /tmp/por-wilderness-fresh
bash scripts/test-portable.sh
```

The new ledger option is `--wilderness-evidence DIRECTORY`. Supply all previously accepted combat directories when regenerating the whole ledger; the summarizer does not merge an old output automatically. Upstream saves, original media and local capture evidence are not bundled into the source snapshot.

## Still open

This is a fixture-based native navigation/persistence pass, not travel to the wilderness through a natural campaign. Wilderness combat, large icons/full arena capacity, larger-party combat and natural downing/revival, broader ranged/spell/item actions, quest prerequisites, final battle and ending remain unvalidated. The 11 auxiliary source-tracker entries remain open. Next focused targets are wilderness encounter entry/lifecycle and the unmatched sound trace/digest tests.
