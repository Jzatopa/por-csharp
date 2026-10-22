# Verification

## Portable CI slice

`bash scripts/test-portable.sh` builds core/desktop and runs Adventure, Desktop, CbtMap, FxEngine, Integration, Types, Global, Pathing, GameIo, Border, Blit, Sym8x8, Debug and VideoCold tests. These use synthetic inputs and source-contract oracles embedded in code. Desktop PCM checks run without opening SDL unless `--audio-probe` is supplied. Test-generated temporary files and synthetic map pictures are not original game assets.

The workflow is prepared but has not run on GitHub until the repository is uploaded and Actions executes it.

## Source rescan

Obtain the pinned upstream checkout separately, then run:

```bash
python3 scripts/recompare-source.py --reference /path/to/pinned-checkout --output /tmp/source-audit.json
```

The scan checks all source hashes, target file existence and routine identifier presence plus explicit reviewed rename/inlining/platform mappings. It is structural, not semantic equivalence or original-binary execution.

## Native replay

Requires Linux SDL2, Xvfb, xdotool, Python 3 with Pillow, and original game media. State is disposable and original hashes are compared before/after.

```bash
python3 scripts/test-adventure-smoke.py --data /path/to/poolofrad --output /tmp/por-native-replay
python3 scripts/test-desktop-smoke.py --data /path/to/poolofrad --output /tmp/por-startup-replay
```

For captured rather than physical audio, set `SDL_AUDIODRIVER=disk` and `SDL_DISKAUDIOFILE=/tmp/por-audio.raw`. For a system-device probe run `scripts/dotnet.sh run --project tests/Por.Desktop.Tests -- --audio-probe`.

## Historical extended suites

All current C# test source is retained. Some older suites consume local `evidence/` oracle files or original media and are intentionally not part of the evidence-free CI slice. Raw model exchanges, media-derived vectors, historical captures and personal working evidence are excluded from this source snapshot. They must not be mistaken for fixtures included here. The complete extended verification history stays in the local working project.

## Limits

Large assertion counts measure individual comparisons, not distinct gameplay scenarios. Source-derived oracles can share source misunderstandings. A passing host replay does not validate the full campaign, every combat spell/encounter, all hardware or original Turbo Pascal arithmetic equivalence.
