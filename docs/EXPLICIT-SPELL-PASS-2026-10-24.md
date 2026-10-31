# Explicit combat spell validation — 2026-10-24

## Accepted scope

The unchanged upstream save G passes one explicit native spell-casting lifecycle: Raven selects memorized **Magic Missile** from Cast, cycles real Aim/Next targets to a hostile displacer beast and selects Target. The spell is consumed, the selected enemy loses 9 HP (30→21), and no PC loses HP during that action. The same battle then reaches ordinary Quick-AI victory, exploration, save/load, sidecar re-save and clean exit. This is fixture-based validation of one action, **not exhaustive spell or natural campaign validation**. No gameplay rule change was needed.

## Replay

```bash
python3 scripts/test-wilderness-native.py --spell \
  --fixture /path/to/pinned-upstream/tests/scenarios/saves/wild \
  --data /path/to/poolofrad --effects /path/to/SBSFX.DAT \
  --output /tmp/por-explicit-spell-fresh
bash scripts/test-portable.sh
```

`--spell` implies `--combat`, using the existing managed deterministic seed 1. The replay loads all 28 unchanged external fixture files through actual Load/G, walks the actual wilderness to the step-11 encounter and skips earlier manual PC actions through Done/Quit until Raven's turn. It does not seed spell slots, force statuses, alter opponents, or use Alt-X. Other PCs subsequently use ordinary Quick AI. Managed fixed-seed repeatability does not establish DOS RNG equivalence.

## Source and instrumentation

Pinned Pascal revision remains `7616920a9178fa32efd45939b9360e95933bbf49`. `combat.pas` ManualControl invokes CastSpellCombat; `cbtacts.pas` selects the spell and targets; `magic.pas` CastSpellNC removes the memorized spell and MagicMissileHandler resolves damage. Their C# counterparts are `Combat.ManualControl`, `CbtActs.CastSpellCombat`, `Magic.CastSpellNC` and `Magic.MagicMissileHandler`.

The native F12 snapshot now copies the existing target-list identities/hostility/distances and resolved spell-target names. This is read-only host observability, not a gameplay override. The controller derives the first hostile entry from that source-generated list, sends the corresponding Next keys and confirms through Target. The delivered target image was inspected: DISPLACER BEAST, HP30, Magic Missile target menu. Aim initially includes the caster; blindly choosing the initial target damages the caster and is not acceptable enemy-action evidence.

## Evidence

Accepted: `evidence/explicit-spell-2026-10-24/pass-3/`.

- Real Cast/spell-selection and Aim target stacks captured. Raven's one memorized spell 15 becomes zero. The retained target is DISPLACER BEAST.
- Before/after casting remain round 0; PC HP is unchanged; exactly one enemy loses HP. The immediate next input boundary is another manual CombatMenu, isolating damage from subsequent AI combat.
- Four-round victory; real postcombat LootMenu boundary; all six PCs survive and receive XP. Gins ends at HP3. All combat records are cleared before saving.
- F5 creates a new 13,137-byte quicksave; another movement changes wilderness coordinates; F8 reaches a new exploration boundary and restores party, spells, effects, items, clock, position and facing. Raven's spent Magic Missile remains absent. All item/spell/effect sidecars survive re-save unchanged.
- Loaded image inspected directly; all six name rows pass border-excluding white/light-cyan text checks (241/106/139/190/154/187 pixels).
- Camp/Save/cancel/Quit/Yes exits 0. All 168 original media files and all 28 upstream fixture files remain hash-identical. Personal save directories are never used.
- `pass-2/` also completed this lifecycle before the final oracle strengthening; final `pass-3/` matches loaded state and quicksave SHA-256. It is repeatability evidence, not a second ledger scenario.
- Initial `pass-1/` deliberately remains unaccepted: the first Aim target was Raven, so a real consumed spell damaged Raven rather than a hostile. The hostile-damage assertion rejected it.
- The actual coverage validator accepts the final replay and rejects six planted faults: unchanged memorized spell, no hostile damage, friendly target, friendly damage, changed combat round and nonmanual casting context. Reproducer: `evidence/explicit-spell-2026-10-24/check-oracle.py`; results: `oracle-mutations.json`.
- All 17 portable test projects pass; output: `evidence/explicit-spell-2026-10-24/portable.log`.

## Tracker and limits

The ledger retains all seven earlier encounter outcomes and adds one explicit Magic Missile lifecycle: **8 fixture encounter outcomes**, plus **3 wilderness adventure replays**. These categories overlap, not eleven unique battles. Script 26 has two distinct accepted action routes against the same encounter type. Natural campaign completion remains false.

Other spells, ranged actions, item use, turn undead, additional effects/recovery/economy branches, large icons/full capacity, natural story progression, final battle and native ending remain open. No physical monitor/audio claim or remote publication is made by this pass.
