# Upstream review and complete recorded effects — 2026-10-23

## Upstream findings

Reviewed pinned Talonius revision7616920a9178fa32efd45939b9360e95933bbf49 and checked current upstream HEAD7b76dfd4cf4e784e78d874727d5b6a37c65e433b. The current `tools/sbsfx.py` is byte-identical to the pinned builder. Current sndsb.pas additionally has FPC/native-compatibility changes; those were reviewed, not blindly imported into the pinned C# port.

[Upstream builder](https://forgejo.brazencrows.com/talonius/pool-of-radiance/src/commit/7b76dfd4cf4e784e78d874727d5b6a37c65e433b/tools/sbsfx.py) maps stock Dark Queen/FRUA Creative4-bitADPCM recordings to effect cues1–8/10–11, with cue9 from the verified Pools of Darkness pad2 crop. `drive_c` is ignored and SBSFX.DAT is deliberately not shipped in git. `tests/regress.sh` stages it next to the DOS build for Sound Blaster runs. This is an extra sampled-audio enhancement, not a missing original PoR file.

## Located and installed

Located stock `unlimadv/DISK1/SFXDQ.VOC` in the publicly linked [Unlimited Adventures archive](https://archive.org/details/msdos_Unlimited_Adventures_1993), following the download link on the [archive-hosting game page](https://www.oldgame.cz/en/unlimited-adventures). Only the VOC member was read; no downloaded executable was run. Source/provenance remains local, outside source-only exports.

- Archive5,579,603bytes; SHA1 `1e84440f997370185829401cde1d9c165c22e810`, matches the archive service metadata.
- VOC30,910bytes;13type1 Creative ADPCM blocks, SHA256 `0c090b0b002d1a0a9af581c4273ad8713bdcd0787a634d41194ef584d0db0de2`.
- Complete pack29,642bytes; all source-defined cues1–11, no missing entries; SHA256 `a1653c373308c360a89558c716fe65b3738d28aa038dd04f041397a21f36569b`.
- Every sample tuple, cue mapping, shared sample offset and output byte independently matches the pinned upstream builder. Cues6and11 share the same Hit payload as upstream requires.
- The planning document says29,912bytes and mentions a50msstep, while actual current/pinned builder crops13–33ms (180samples). The270-byte difference is consistent with that30ms@9000Hz change. Acceptance uses byte equality to the actual builder, not the stale planning size.

Runtime uses the normal per-user state path `data/SBSFX.DAT`; the source VOC is retained in `audio-sources/SFXDQ.VOC`. Exact local paths and hashes are in local `evidence/upstream-audio-2026-10-23/installation.json`. The former193-byte footstep-only pack was automatically preserved as a unique backup before replacement. Default auto mode selects Blaster; no extra launch flags are needed. Explicit audio modes and saved mute preferences remain available.

## Campaign and final battle

Upstream `README.md`, `tests/regress.sh` and `tests/scenarios/README.md` distinguish pure unit checks from bounded emulator scenarios. The harness uses deterministic NORAND starts, golden/candidate screen text and checkpoint save hashes, and requires a new scenario to pass golden-vs-golden twice before trusting it.

Existing combat is a bounded Podal Plaza replay; wildfight ends in combat. A complete wilderness save G is committed in `tests/scenarios/saves/wild/`; other scenario seed saves normally come from external goldbox-player. These are reusable validation fixtures, not a recorded natural campaign route.

Pinned `docs/refactor-plan.md:605` explicitly lists an ending scenario as unfinished. The ending routine itself uses ECL PROGRAM -> Ending.WinGame, FINAL picture blocks1/3–6 and animations2/7/8, then restores exploration. Local originals already contain final5.dax blocks1–8, and writable data has the identical original plus generated FINAL.DAX. No missing ending-art download is required. Presence is not proof of final combat or naturally reaching the ending.

The C# ledger remains `campaign_completion:false`. Existing synthetic ending/PROGRAM tests do not establish a native final battle or a full natural campaign. This source review closes the audio-data requirement, not the campaign-validation gate.

## Verification

Local evidence is under `evidence/upstream-audio-2026-10-23/`: source/archive hashes,13VOCblock metadata, full pack manifest/upstream byte comparison, installation/backup/save preservation, native effect logs and SDL PCM. All11cues must start as sampled effects and finish at their source-derived duration; see final results below. Personal saves and168original PoR files are verified unchanged. No downloaded recordings are bundled in git/source exports and no remote push is performed.

## Final results

All11source-defined recordings start as sampled effects and complete at the source-derived duration in both SDLdisk and normal PulseAudio runs. The longest tested recording (Fireball, cue10) lasts about2.128seconds and is not cut off by the100mssequencer auto-stop. All129desktop audio checks plus retained12speaker/201keyboard/7rosterchecks pass in those probe runs.

The native launcher replay with the fullpack passes opening/exploration/map transition/F5-F8/party display/camp-rest/exit0. Actual sampled-effect counter is nonzero; output statistics and WAV captures are retained locally in result.json. All168original fingerprints and personal-save preservation checks pass. The audio-data blocker is closed; campaign/final-battle acceptance remains open.

Current upstream's complete scenario tree additionally contains bane.txt, banetemple.txt and stojanow.txt; these are potential bounded quest-state replays, not a natural-campaign/ending completion record. Its current refactor-plan still explicitly lists the missing ending scenario. No existing source checkout, gameplay rule, personal save, COAB or Matrix file was modified for this integration. Only optional full-pack probe coverage and documentation were added to source.

A bounded read-only collaborator independently reviewed pinned upstream campaign fixtures and source paths; its findings were checked against current tree/roadmap locally. The current Bane replay also explicitly seeds a party/NPC/location and uses STING Alt-X to defeat its battle: it is quest-state regression evidence, not natural victory/campaign evidence. Auxiliary token usage is unmeasured.
