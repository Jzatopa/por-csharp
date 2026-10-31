# Pool of Radiance — C# desktop restoration preview

A source-guided C# port of the Talonius Pool of Radiance Pascal/assembly reconstruction, pinned to `7616920a9178fa32efd45939b9360e95933bbf49`.

All 165 inventoried source inputs and all 227 pinned repository files have explicit dispositions. This is **not a fully validated release**, and inventory completion is not a whole-game equivalence claim.

## October 31 update

See [what changed and the upstream review](docs/UPDATES-2026-10-31.md) for the changes included in this push, the two selectively incorporated upstream fixes, and the remaining upstream candidates. The C# baseline remains pinned; this is not a wholesale synchronization with current Talonius main.

A legitimate level-one-to-current-checkpoint campaign has reached checkpoint **0087**: Sokal Keep, Mendor's Library, Podal auction, Cadorna's treasure, Kovel Mansion and Wealthy Area have recorded resolutions. Ara is cleric5/28HP, Hero fighter5/44HP; all eight party members are healthy, and native save/reload was verified. Final battle and ending remain **unverified**. No campaign saves or private logs are bundled.

## Original project

This C# port is based on **Talonius’s Pool of Radiance Pascal/assembly reconstruction**:

- **Original repository:** https://forgejo.brazencrows.com/talonius/pool-of-radiance
- **Pinned source revision:** `7616920a9178fa32efd45939b9360e95933bbf49`
- Upstream describes its reconstruction as SSI’s DOS **Pool of Radiance v1.3**, with fixes. This port does not independently establish upstream’s historical byte-identity claims.
- The C# port supplies a managed engine and SDL2 desktop host; it does not include original game media or claim whole-game equivalence.

Credit for the source reconstruction belongs to the upstream project and its contributors. See [UPSTREAM.md](UPSTREAM.md) for attribution and unresolved licensing/permission status; no new license grant is implied.

## Run on Linux

Prerequisites: .NET 8 SDK, SDL2 (`libSDL2-2.0.so.0`), and your own original Pool of Radiance DOS game installation. No game archives, original executables, saves, SDK or native library binaries are bundled.

```bash
./play-desktop.sh --data /path/to/poolofrad
```

The launcher accepts a system `dotnet`, a local `.tools/dotnet/dotnet`, or an explicit `POR_DOTNET` executable. It does not download an SDK. Original media is read-only; generated archives and saves use a separate platform-local application-data directory. Optional flags: `--state DIRECTORY`, `--captures DIRECTORY`, `--short-intro`, `--no-audio`.

Automatic audio selects an installed Sound Blaster effects pack or AdLib with source-defined PC Speaker effects through SDL2; existing Settings → Music/Sound preferences are respected. Tandy and Sound Blaster modes are selectable; see the full audio setup below. F5/F8 quick-save/load, F6 Companion Tools when enabled, F12 framebuffer capture, Ctrl+S sound toggle.

## Verify without game data

```bash
bash scripts/test-portable.sh
```

This runs 17 self-contained console test projects; it needs neither game media nor the pinned Pascal checkout. The GitHub workflow runs that same command. Native desktop replays and legacy extended suites have additional requirements; see [testing](docs/TESTING.md).

## Status and provenance

Local native SDL/X11 checks cover title/menu/settings, the opening tour, initial exploration, map transitions, quick-save/load, camp/rest and clean exit. A system PulseAudio device and captured non-silent PCM were checked. Physical listening, full combat/campaign completion, Windows/macOS hosting and exact original-DOS equivalence are not established.

See [source comparison](docs/SOURCE-COMPARISON.md), [source dispositions](docs/source-dispositions.json), [attribution and licensing status](UPSTREAM.md). There is no blanket new license grant over the reconstructed game code or data. Resolve distribution permissions before public publication.

## Combat-entry repair

The reported script-11 combat-entry crash was traced to placement leaving `arenaCount` one past the final creature. Placement now publishes the actual retained-slot count before rendering; the candidate-inclusive count is preserved during placement probes. Portable regression tests exercise placement followed by occupancy and redraw, including the final creature and full-capacity/empty rosters. A native replay at script11, (7,1), instruction40107 now renders combat and advances from round0 to round1. See [repair and verification](docs/COMBAT-ENTRY.md) and [testing](docs/TESTING.md) for scope and replay instructions. This is not full-campaign or all-combat validation.

## Campaign combat validation started

Native seeded replays now cover training-hall and four-orc Slums victories, postcombat cleanup, real HP/XP/coin/item save-load checks, and Slums defeat/main-menu recovery. These are targeted synthetic-state checks, not natural campaign completion. See [campaign validation and replay](docs/CAMPAIGN-COMBAT.md) and [coverage ledger](docs/campaign-combat-coverage.json).

## Full desktop audio

Linux builds require a C compiler (`cc`) in addition to .NET 8 and SDL2. Automatic audio selects Blaster with an installed pack, otherwise AdLib. Select `--audio adlib|tandy|speaker|blaster`, or `--no-audio`. Sound Blaster sampled effects require an optional `--effects SBSFX.DAT` v2 pack; missing samples retain the source-defined PC Speaker fallback. No effects pack is bundled. See [audio setup](docs/FULL-DESKTOP-AUDIO-2026-10-22.md). Nuked-OPL3 is dynamically linked; its source, notices and LGPL license are in `third_party/nuked-opl3`.

### Recorded-effect packs

Default `--audio auto` selects Sound Blaster when a valid SBSFX.DAT is installed, otherwise AdLib. Use `--effects /path/to/SBSFX.DAT --install-effects-only` to install without launching. `scripts/build-sbsfx.py --pools /path/to/ZOUNDS.DAX --dqk /path/to/SFXDQ.VOC --out /tmp/SBSFX.DAT` builds the source-defined pack from user-owned recordings. A missing source is an error unless `--allow-partial` is explicit; partial packs retain fallback for missing cues. No recordings are included. See [current verification](docs/COMPLETION-PASS-2026-10-23.md).

### Upstream audio integration review

The complete upstream-defined11cuepack was built from stock FRUA recordings plus the source-defined Pools footstep, compared byte-for-byte with upstream and verified through native playback. Recordings remain external/local and are not bundled in this source repository. See [review, verification and remaining campaign gate](docs/UPSTREAM-AUDIO-AND-VALIDATION-2026-10-23.md).

All 227 pinned upstream files now have explicit dispositions (ports, managed platform replacements, preserved text resources or external test data). This is not 227 literal C# translations or full campaign validation. See [repository audit](docs/REPOSITORY-CLOSURE-2026-10-23.md).
