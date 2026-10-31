# Full desktop audio — 2026-10-22

## Setup

Linux desktop audio now defaults to **AdLib** (OPL music with the source-defined PC Speaker effect path). No change to original arrangements or core gameplay rules.

```sh
./play-desktop.sh                       # AdLib
./play-desktop.sh --audio tandy         # SN76496 three tones + noise
./play-desktop.sh --audio speaker       # previous PC Speaker option
./play-desktop.sh --audio blaster       # AdLib + optional recorded effects
./play-desktop.sh --audio blaster --effects /path/to/SBSFX.DAT
./play-desktop.sh --no-audio
```

Existing Settings → Music/Sound preferences and Ctrl+S remain authoritative, including saved OFF settings. Relaunch to select another device. Sound Device is shown in the existing settings information.

Sound Blaster's optional `SBSFX.DAT` v2 **is not present in the tested PoR installation and is not bundled**. Missing entries use the converted source's existing speaker-effects fallback. The upstream pack builder uses additional SSI game recordings; it is not an original PoR sound asset. No substitute recordings or guessed asset mappings were added. `--effects` validates a supplied pack and installs it only into writable state data, preserving an existing differing pack as a unique `.bak`; original media and saves are not modified.

Dependencies: .NET8, SDL2, and a C compiler (`cc`) on Linux. Build automatically compiles/copies `libpor_opl.so`. Windows/macOS native builds are outside this Linux implementation.

## Implementation

- `SpeakerPcm` retains PIT/speaker timing and owns the desktop sound bus, rational 236.7Hz sequencer clock, and signed16 mono48kHz mixer. All mutation remains on the game thread; SDL consumes queued PCM.
- `TandyPcm` implements the SN76496 register latches,10-bit periods (zero means1024),2dB attenuation, three tone channels and periodic/white noise with the TI variant's feedback taps. This is register-level synthesis, not a physical speaker/cabinet measurement.
- `OplPcm` dynamically loads the unmodified [Nuked-OPL3](https://github.com/nukeykt/Nuked-OPL3) core, pinned to765ec962e473aeb767e4cba74ffdc8f588ffbfe8, in its default OPL2-compatible mode. Its detection status implements the handshake used by this driver, not a complete PC timer peripheral. Source, attribution and LGPL license are retained in `third_party/nuked-opl3`; the shared library is independently replaceable.
- `BlasterPcm` implements the exact DSP/DMA command subset emitted by `SndSb`: reset/detection, managed DMA block resolution,8-bit PCM and Creative4-bit ADPCM, time constants, start/stop and speaker enable. It does not access DOS memory or system hardware ports. ADPCM uses the reference byte, high-nibble-first decoding, adaptive quantization and saturation; it is not IMA ADPCM.
- Muting gates every source. Timer shutdown silences synthesis and stops sample playback. The existing clock-loop polling facade advances audio during blocking SoundDelay. No concurrent audio callback mutates core state.

PSG register/noise behavior was cross-checked against [MAME's primary implementation](https://github.com/mamedev/mame/blob/master/src/devices/sound/sn76496.cpp); Creative ADPCM step behavior against [DOSBox-X](https://github.com/joncampbell123/dosbox-x/blob/master/src/hardware/sblaster.cpp). Those source files were inspected as references, not imported. The imported OPL source is separately identified and licensed above.

## Verification

Local evidence: `evidence/full-audio-2026-10-22/` (not included in source-only exports).

- Previous12PC Speaker waveform/timer/mute/chunk checks retained,201keyboard and7roster checks retained.
- 126 new chip/DSP checks (including 87 mode/tune pairs) cover PSG frequency/attenuation/noise, OPL detection and title/battle output, mute/shutdown, chunk independence, PCM/ADPCM vectors, managed DMA duration/completion, effect-pack validation/import/backup, and rendering every available arrangement through each applicable backend. See `final-desktop-2.log` for exact counts.
- SDL disk-backend native title/effect probes passed for speaker, Tandy, AdLib and Sound Blaster. Sound Blaster uses explicitly synthetic PCM8/ADPCM4 fixtures to exercise real SndSb loading/playback, not a claim to possess an original recorded pack. A system PulseAudio AdLib probe passed; physical listening is not claimed.
- Native AdLib and Tandy adventure replays passed opening, exploration/map transition, F5/F8 position/facing/HP, party display, camp/rest and clean exit. Each checked168original files unchanged.
- The first Blaster fallback replay reached save/load and rest but its fixed-delay quit sequence timed out. This run is not accepted as a complete replay. Exit-menu framebuffer captures were added to the controller before retrying; `adventure-blaster-fallback-2/result.json` confirms the repeat passed all assertions and exit0. No engine behavior was changed for this retry.
- Full14-project portable regression gate passed before the final effect-pack import tests; the final source-only checkout gate is recorded below. This work does not establish full-campaign completion or original-DOS acoustic equivalence.

Reproduce focused checks:

```sh
scripts/dotnet.sh run --project tests/Por.Desktop.Tests
scripts/dotnet.sh run --project tests/Por.Desktop.Tests -- --audio-probe --audio-mode=adlib
scripts/dotnet.sh run --project tests/Por.Desktop.Tests -- --audio-probe --audio-mode=blaster --sample-fixture
python3 scripts/test-adventure-smoke.py --audio tandy --output /tmp/por-tandy-fresh
bash scripts/test-portable.sh
```

## Final result

All14portable test projects passed in the clean code-only source checkout (`snapshot-portable.log`, exit0). Final packaging added source/license copying only; the focused desktop suite was rerun afterward (`final-desktop-3.log`):201keyboard,7roster,12speaker,126full-audio checks, including87mode/tune render pairs, all pass.

Release publish succeeds. Its native library loads and allocates/renders/frees a chip; published OPL source/header/LICENSE/README and wrapper bytes match the checked-in files. Publish source/license delivery is automatic, not a manual packaging reminder. No original core source file differs from the accepted campaign snapshot.

Accepted native adventure modes: AdLib, Tandy, Blaster with missing-pack fallback; all exit0 with168original files unchanged. Recorded-effect playback is proven with synthetic PCM/ADPCM fixture data, not a real SSI recording pack. All4native SDL disk probes and system PulseAudio AdLib probe pass. Exact output counts and bounded sample peaks are retained in `audio-stats.json`. No clipping occurred in those captured probes/replays. Physical listening, every sound on every machine, optional real-pack content and full campaign completion remain unclaimed.

Changes are local; no remote push or public release was performed. Core/gameplay, COAB, Matrix and original media were not edited.

## Superseded default and sample-tail fix — 2026-10-23

Default selection is now `auto` (Blaster with a valid installed pack, otherwise AdLib). A verified recorded-footstep-only pack is installed locally; full sampled coverage is still missing its source recordings. Timer uninstall no longer truncates an independently playing DSP sample. See [current pass](COMPLETION-PASS-2026-10-23.md).

Complete recorded effects are now installed and verified; the later partial-pack limitation is superseded by [the full audio integration](UPSTREAM-AUDIO-AND-VALIDATION-2026-10-23.md).
