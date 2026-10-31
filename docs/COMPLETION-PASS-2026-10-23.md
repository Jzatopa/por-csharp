# Recorded effects and gameplay completion pass — 2026-10-23

## Acceptance scope

This is another evidence-backed integration/validation pass, not a declaration of100% game completion. Source inventory completion, native seeded outcomes and natural campaign completion remain distinct.

## Recorded effects

- Searched the available game directories and archives for SBSFX.DAT/SFXDQ.VOC. Neither was present. Upstream's public v1.3-841 DOS release was also inspected; it contains START.EXE/GAME.OVR/RAVFULL.DAX, not the effects pack.
- A local Pools of Darkness ZOUNDS.DAX is available. The new media-free `scripts/build-sbsfx.py` extracts the source-defined `pad2` crop (13–33ms at9000Hz, gain0.25,2ms fades), matching pinned upstream `tools/sbsfx.py` byte-for-byte for that crop.
- Built and installed a **partial, recorded-footstep-only** SBSFX.DAT into the normal writable desktop state. SHA256 `fdaa1fbe11c6d215431132225c5c1c61ac3d9ccb1bc8c92c8419b96ac263edf0`,193bytes, tune9PCM8. Other effects retain the original source-defined speaker fallback. No alternate recording was guessed or substituted.
- Full recorded coverage still requires SFXDQ.VOC from Dark Queen of Krynn or FRUA (or an existing complete SBSFX.DAT). That source was requested from the user; independent gameplay work continued. The builder supports both sources and emits per-entry hashes/provenance; it refuses incomplete coverage without explicit `--allow-partial`.
- Default audio mode is now `auto`: a valid installed pack selects Blaster, otherwise AdLib. Explicit `--audio adlib|tandy|speaker|blaster` still wins. `--effects` installs a supplied pack; `--install-effects-only` installs without opening SDL or touching character/roster recovery. A differing previous pack is backed up.
- Real native adventure replay with the pack reported34sampled-effect starts, passed map/save/load/rest/exit0, and checked all168original PoR files unchanged. Output capture is retained locally; physical listening is not claimed.

## Confirmed host defect repaired

The previous DesktopSound timer-uninstall facade stopped the virtual DSP too. Source hardware DMA is independent of the sequencer interrupt; SoundCommand's100ms auto-stop must not cut off a longer recording. A new300msPCM fixture through the real SndSb/SoundCommand path failed against the prior host with “Sample was truncated when the sequencer timer auto-stopped.”

Removed the extra DSP stop from timer uninstall. Sample stop/Play(0), mute and actual device disposal remain explicit. The native regression now observes the sample still active after auto-stop and naturally finished after its source duration. Synthetic PCM8 and Creative ADPCM4 native playback pass. Core sequencer/gameplay source was not changed for this fix.

Evidence: `evidence/completion-pass-2026-10-23/sb-tail-{baseline,fixed}.log` and SDL captures. This fixes a desktop integration error, not an original Pascal game rule.

## Gameplay evidence

- **Actual fleeing:** ordinary Slums encounter, real movement through a path to the arena edge and real Flee confirmation, no forced RUNNING/status changes. Recorded COMBAT_RESULT129, cleanup, return to exploration and F5/F8 equality. One synthetic precombat hero/location seed; not natural travel/campaign progression.
- **Two-character recovery:** strong fighter plus pre-seeded unconscious positive-HP companion. Real combat victory restores companion to OKAY/enabled withHP1; both characters remain in order with exact observed party fields across F5/F8. This is seeded-down-member recovery, not a naturally occurring knockout or revival proof.
- **Dropped-item collection:** Slums event entered from(8,0)east; native battle with one kobold leader plus fifteen kobolds and one PC (17occupied slots) reached victory. This is not the72-slot capacity boundary. A dropped item was transferred through the actual Take/Items menu, inventory grew from1to2, and the new item persisted through F5/F8. First controller attempt selected View rather than Take and is unaccepted. The accepted retry uses actual captured call-stack/menu context instead of inferring a menu solely from interface-state6. No treasure/enemy/status injection was used.

Native captures gained read-only audio counters, active-creature/position/movement, arena cost/occupancy, treasure snapshots and call-stack context at the actual SDL polling point. They do not alter game input, map state, statuses or outcomes. Existing individual replay scripts remain available.

## Completion gates still open

Natural quest/state progression across the campaign, late-game/final battle, naturally downed/revived parties, multi-character fleeing, morale/continue-battle variants, ranged/spell/item/turn-undead interactions, additional shops/temples/NPC/loot/economy combinations, wilderness and large-icon placements. Existing seeded tests must not be extrapolated into100% campaign coverage.

## Final verification

Final gate and accepted loot result are recorded below after completion. No original game files or personal saves are used as writable test fixtures. No public release or remote push is implied by this local pass.

## Accepted new lifecycle records

Visual review rejected the first flee lifecycle claim: its F5/F8 keys ran inside Character View and displayed “QUICK SAVE AND LOAD UNAVAILABLE.” Flee result129 itself is real, but party equality without a changed state is not serialization evidence. All three extended scenarios are being repeated with strict exploration-menu stack checks, an actual save-file hash change, a facing change before load, and a new READY-explore event after F8. The ledger temporarily removes these extended runs until the stronger replay evidence is accepted. Existing original3outcome records remain. `campaign_completion:false` is retained.

## Stronger replay acceptance completed

Accepted final runs: `flee-2`, `multi-party-2`, `loot-3`. Each proves an actual quicksave rewrite (seeded/written SHA256 differ), changed facing before F8, a fresh READY-explore boundary, restored facing/observed party fields, and the actual Explore.MapMainMenu stack without ViewChar/Camp/PostCbt submenus. Loaded party-name text passes a border-excluding pixel check. The rejected submenu/no-op run is refused by the same ledger validator. Final loaded views were inspected visually.

The coverage ledger again contains6accepted seeded outcome runs across2script areas. These final3replace the provisional extended records; they do not add duplicate coverage. Flee result129 survives the observed exploration/save-load continuation; companion recovery and acquired inventory persist in their respective runs.17creatures in the loot scenario is not72-slot capacity coverage. No natural campaign completion is claimed.

## Final gates

All14portable projects pass in the working tree and a clean code-only source checkout. Focused desktop checks pass201keyboard+7roster+12speaker+129full-audio checks (87mode/tune render pairs); the media-free pack builder passes15checks. The new300msPCM tail regression fails with the old extra DSP stop and passes with the repair; native PCM8/ADPCM4 and the actual recorded footstep through PulseAudio pass. Strict Python replay/ledger controllers compile and all final native lifecycle assertions pass.

Release publish succeeds; its OPL library allocates/renders/frees and published source/header/license/wrapper files match their checked-in bytes. All168original PoR fingerprints remain unchanged, and an idempotent install-only replay verifies personal save hashes unchanged. No core gameplay/sequencer source differs from the preceding accepted audio snapshot. No remote push or public release was performed.

Remaining external audio input: a full SBSFX.DAT or SFXDQ.VOC. Installed local193bytepack is partial (tune9 only), with synthesized fallback elsewhere. Remaining game acceptance: full natural campaign and the explicitly listed untested branch/action families. This pass does not establish100% completion.

## Audio-source requirement subsequently resolved

The follow-up upstream review located stock SFXDQ.VOC, built and installed the complete11cuepack, and verified allrealrecordings on SDLdisk/PulseAudio. The earlier missing-source/partial-pack statements above describe that pass's historical state, not the current installation. See [complete audio follow-up](UPSTREAM-AUDIO-AND-VALIDATION-2026-10-23.md). Campaign validation remains open.
