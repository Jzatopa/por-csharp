# Pool of Radiance — C# desktop restoration preview

A source-guided C# port of the Talonius Pool of Radiance Pascal/assembly reconstruction, pinned to `7616920a9178fa32efd45939b9360e95933bbf49`.

All 129 pinned source inputs have an explicit disposition. This is **not a fully validated playable release**, and source inventory completion is not a whole-game equivalence claim.

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

## Known issue — combat entry

A reported encounter at script 11, map (7,1), crashes with a `NullReferenceException` in `CbtMap.ScrollView` while drawing the initial combat screen. An arena-count/empty-slot boundary is suspected, not yet confirmed by a regression test. This snapshot includes the startup wiring review changes but does **not** repair that crash.
