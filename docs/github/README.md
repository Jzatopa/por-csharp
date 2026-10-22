# Pool of Radiance — C# desktop restoration preview

A source-guided C# port of the Talonius Pool of Radiance Pascal/assembly reconstruction, pinned to `7616920a9178fa32efd45939b9360e95933bbf49`.

All 129 pinned source inputs have an explicit disposition. This is **not a fully validated playable release**, and source inventory completion is not a whole-game equivalence claim.

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

PC Speaker music and effects are enabled by default through SDL2; existing Settings → Music/Sound preferences are respected. Tandy/AdLib synthesis is not enabled. F5/F8 quick-save/load, F6 Companion Tools when enabled, F12 framebuffer capture, Ctrl+S sound toggle.

## Verify without game data

```bash
bash scripts/test-portable.sh
```

This runs 14 self-contained console test projects; it needs neither game media nor the pinned Pascal checkout. The GitHub workflow runs that same command. Native desktop replays and legacy extended suites have additional requirements; see [testing](docs/TESTING.md).

## Status and provenance

Local native SDL/X11 checks cover title/menu/settings, the opening tour, initial exploration, map transitions, quick-save/load, camp/rest and clean exit. A system PulseAudio device and captured non-silent PCM were checked. Physical listening, full combat/campaign completion, Windows/macOS hosting and exact original-DOS equivalence are not established.

See [source comparison](docs/SOURCE-COMPARISON.md), [source dispositions](docs/source-dispositions.json), [attribution and licensing status](UPSTREAM.md). There is no blanket new license grant over the reconstructed game code or data. Resolve distribution permissions before public publication.

## Combat-entry repair

The reported script-11 combat-entry crash was traced to placement leaving `arenaCount` one past the final creature. Placement now publishes the actual retained-slot count before rendering; the candidate-inclusive count is preserved during placement probes. Portable regression tests exercise placement followed by occupancy and redraw, including the final creature and full-capacity/empty rosters. A native replay at script11, (7,1), instruction40107 now renders combat and advances from round0 to round1. See [repair and verification](docs/COMBAT-ENTRY.md) and [testing](docs/TESTING.md) for scope and replay instructions. This is not full-campaign or all-combat validation.
