# Combat-entry repair — 2026-10-22

## Cause and change

The reported crash occurred at script 11, map (7,1), ECL instruction 40107: `CbtMap.ScrollView` dereferenced a null creature while drawing the initial arena.

The pinned Pascal `PlaceAllCreatures` initializes arenaCount to one and increments it after each retained creature, leaving the next-free insertion index. Managed rendering and other arena consumers iterate the count inclusively. The prior isolated placement oracle expected 17 for 16 creatures and missed this composition failure.

Placement now publishes `slot - 1` when it ends. Its candidate-inclusive count during placement is unchanged, so collision/occupancy probes still see the current creature. No renderer null-skip hides an invalid count. An unplaceable party member is still retained with zero icon size; source behavior for disabled members and creature removal is otherwise unchanged. This is an intentional repair of an inherited source boundary bug, not exact-Pascal equivalence.

## Verification

- Portable CbtMap `CombatEntry`: 210 assertions pass. Real placement feeds real occupancy and view redraw for empty, one, two, sixteen, full-capacity and unplaceable-PC cases; verifies final-slot identity, occupancy and rendering. Restoring the old count boundary fails this regression.
- All 14 portable test projects pass; core/desktop build: zero warnings/errors.
- Extended combat/sound regression: 782,010 assertions pass. VM regression: 569,151 assertions pass.
- Native SDL/X11 replay reaches script11, (7,1), instruction40107 with two arena slots. Initial rendering completes; Done/Guard and enemy-turn scheduling advance round0 to round1 while remaining in combat. Framebuffer inspected.
- All 168 original game-media hashes unchanged. Personal saves are not used or modified.

Run:

```bash
bash scripts/test-portable.sh
python3 scripts/test-combat-smoke.py --data /path/to/poolofrad --output /tmp/por-combat-new-run
```

The native replay creates a synthetic hero and a real quick-save in a disposable directory, then seeds only that save's previous-script and position fields to script11 at (6,1), facing east. Real load, script execution and movement trigger the encounter at (7,1). This is not validation of natural travel to that location. Captures now expose arena count and round number for diagnosis. The replay terminates its own test window after the assertion; a clean in-game quit is not claimed by this combat check.

## Limits

This validates the reported entry failure and one combat-round transition, not every encounter, attack/spell, combat conclusion or the full campaign. Earlier exploratory replay attempts did not pass: a travel route missed the encounter, and a controller assertion wrongly assumed the initial round was numbered one. Neither required further engine changes; the accepted replay uses an explicit fixture and the actual zero-based initial round.

Original project and pinned revision remain documented in [UPSTREAM.md](../UPSTREAM.md).
