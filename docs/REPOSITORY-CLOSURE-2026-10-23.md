# Original repository reconciliation — 2026-10-23

## Scope and terminology

Pinned original: `7616920a9178fa32efd45939b9360e95933bbf49`. This audit covers **every one of its 227 tracked files**, not just the game's `src/` directory. Later upstream commits are not silently included.

- `docs/source-dispositions.json`: 165 Pascal/assembly/include/configuration inputs. “Handled” includes managed build replacements, not only literal C# ports.
- `docs/repository-file-dispositions.json`: all 227 originals, including scripts, documentation, scenarios and saved fixtures. Every row has original SHA-256, role and a real target/resource disposition.
- No original media, saves or executables are bundled. The 28 upstream wilderness fixture files stay external, individually hashed and consumed through the verified native replay. They are data, not unconverted C# source.
- Historical support files are retained byte-for-byte under `references/upstream-pinned/`. Active .NET tools replace DOS compilation, overlay linking, emulator timing, CI and packaging; obsolete DOS behavior is not faked.
- **This is not a claim of 227 C# translations, exhaustive equivalence of every original assertion, identical SSI executables, or complete campaign validation.** Original test replacements can use different independently checked formulations; their exact scope stays in the source tracker.

## Stale sound tracker repaired

The earlier auxiliary tracker incorrectly said no CRC test or AdLib comparison existed. Both already existed in `Por.FinalThree.Tests` and were reverified:

- `tsndseq.pas`: all 58 SSI-recorded device/tune tuples, with tick count, write count and per-tick CRC; 174 assertions. Constants now live in generated C# `SoundGoldenRecords.cs`, extracted from the pinned Pascal EXPECTED table, so no private TSV is needed.
- `tsndopl.pas`: all 28 tunes, 400 ticks each, decoded Tandy/AdLib pitch, attenuation, active keys and noise. Added the original reset-write, sounding-tick and noisy-tick thresholds; 67,617 assertions.
- `sndtrc.pas`: existing driver TraceTick/OplReset plus a new caller-buffer `SoundTrace` port bus. Buffer count/reset, zero/full capacity, dropped excess writes and low-port-byte/Word packing are checked across capacities 0–64, including real driver/reset consumers.

The source helper is a managed trace adapter, not a DOS object file. Hardware reads deliberately return zero exactly as the original TRACE macro specifies.

## New conversions and supporting tools

- `bench/sndbench.pas` and `bench/vidbench.pas`: Qwen produced one initial file at a time; prompts, initial code, unchanged compile checks, response/token/timing records and supervisor repair differences are local in `evidence/repository-closure-2026-10-23/`. No correction request was sent to Qwen.
- Sound benchmark retains off/silent/title-music loops, tick-edge start and rounded relative loop-loss calculation. Its host actually drives the converted sequencer and displaced BIOS tick callback.
- Video benchmark retains 1,000 characters, 441 tiles, fill and fill/flush workloads with source ordering and rate formula. Its host calls the actual converted primitives through modeled CGA/EGA/Tandy RAM buses.
- Both default to the original 54 BIOS ticks. Short verification runs use 2 ticks per workload. **These measure the managed process/RAM bus, not DOSBox CPU cost, physical playback, or native display throughput.** Rates are not cross-platform performance-equivalence evidence.
- `build/ovlsizes.py` and `tests/ovlloads.py`: functional C# overlay-size/report parsers with malformed-input checks and synthetic source-contract tests. They remain legacy analysis tools; the managed game does not have DOS overlays.
- Six test/benchmark/configuration entries: both TPC configurations explicitly mapped in `TPC-MANAGED-REPLACEMENT.md`; all four five-field POOL configurations retained and parsed, with their actual source video drivers installed in tests. Desktop SDL presentation remains VGA.
- All 13 original scenario definition files are retained unchanged, embedded in the test project, and parsed with scope/directive validation. Includes `strip`, `put`, `mask`, build-specific instructions and screen/checkpoint directives. Preservation/parsing is **not execution of those scenarios**. The original replay wrapper called an external goldbox-player engine, which is not code supplied by this repository; native managed replays replace that host workflow.

## Managed build, package and release workflow

```bash
scripts/dotnet.sh build src/Por.Desktop
bash scripts/test-portable.sh
scripts/dotnet.sh run --project src/Por.UpstreamTools -- sndbench T
scripts/dotnet.sh run --project src/Por.UpstreamTools -- vidbench E
scripts/dotnet.sh run --project src/Por.UpstreamTools -- config tests/UpstreamScenarios/POOL-VGA.CFG
scripts/dotnet.sh run --project src/Por.UpstreamTools -- scenario tests/UpstreamScenarios/wilderness.txt
python3 scripts/package-desktop.py --version local-test --output /tmp/fresh-por-release
```

The local Linux package is made with `dotnet publish`, excludes game media/saves, includes native library source/licenses, and records file/archive hashes. `SOURCE_DATE_EPOCH` controls ZIP timestamps. The opt-in `scripts/release-desktop.py` defaults to a dry-run plan; `--execute` plus `GH_TOKEN` manages a GitHub **draft** and attaches new bytes before removing an older asset. It refuses an already published release. No remote write was performed or tested here.

Upstream SSI-byte reproduction remains a preserved historical reference. Managed outputs intentionally cannot be called SSI-identical DOS executables; source-integrity scans and clean source builds replace that build gate rather than pretending to reproduce SSI hashes.

## Checks and durable evidence

- New managed tool suite: source-loop workloads, cleanup, overlay reports, buffer bounds, all four configs and all 13 scenario inputs.
- Existing sound CRC/AdLib/timer/core slices are added to the evidence-free portable gate alongside the new tool project (17 projects total).
- Five real managed benchmark modes, source-derived independent helper comparisons, a published-package load, original/pinned-input hashes and an extracted source-only portable gate are recorded in `evidence/repository-closure-2026-10-23/result.json` when complete.
- The complete file audit is reproducible with `python3 scripts/reconcile-upstream.py --reference /path/to/pinned-upstream`. `--write` regenerates explicit reviewed dispositions and preserves historical non-media inputs; unknown files, hash drift or missing targets fail.

## After source closure

The campaign ledger is separate and remains incomplete: six seeded combat outcomes and one upstream-fixture wilderness replay. Natural quest progression, wilderness/large-party/action combinations, final battle and ending are the next validation work. No gameplay validation flags are promoted just because a source or supporting file has a disposition.
