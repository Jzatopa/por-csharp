# Pascal/C# recompare — 2026-10-22

Reference: Talonius Pool of Radiance, pinned revision `7616920a9178fa32efd45939b9360e95933bbf49`. This pass compares against that known input, not an unreviewed update to upstream HEAD.

## Structural check

All **129** pinned input hashes match the source ledger. Every named target exists. The first scan exposed an explanatory annotation inside an EvtKind target filename; the ledger now stores the real EclBindings.cs path. No missing production file was hidden by that annotation.

The comment/string-stripped scan inventories **1,305 distinct Pascal routine identifiers** within units. Thirteen do not survive under the same C# name and were explicitly checked:

| Unit | Helpers | C# mapping |
|---|---|---|
| GameBoot | CountOvrRead | Counting OverlayReader closure; managed desktop startup separately replaces DOS overlay loading. |
| MenuCore | ParseMenu | Inlined HorizontalMenu option parsing. |
| MenuCore | FirstNonBlank, LastNonBlank | Inlined Highlight leading/trailing-space scans. |
| MenuCore | HighlightLine, UnhighlightLine | Highlight with true/false. |
| MenuCore | LastLabelBefore | Source DrawPage discards its result; the pure unused computation is omitted. |
| Options | KeyName, OnOff | Keys table and flag-to-ON/OFF expressions. |
| Options | MoveTo, Arrow | Inlined selection/redraw and speed/toggle branches. |
| SndSb | Physical | Explicit platform PhysicalAddress, including offset in DMA operations. |
| TextRend | FindNewIndex | Inlined space skip after wrapping. |

Identifier occurrences are **not implementation or semantic equivalence proof**. The scan cannot verify arbitrary branches, aliasing, call order or numeric conversions.

## Targeted source review

The pass also reread source and target control flow around:

- Exploration barrier updates, strength/exceptional-strength door tables, lock picking, spell-slot lookup, movement and quick-load handoff.
- Combat round/initiative selection, manual controls, movement, and cleanup.
- Postcombat experience/treasure shares, cleanup and NPC shares.
- Vertical-menu whitespace/paging helpers, text wrapping and options navigation.
- Startup overlay adaptation and required platform-service composition.

No new gameplay-code mismatch was established in this targeted slice. Apparent candidates were checked against Pascal before editing: for example, AskQuickLoad's repeated file check and combat menus' use of doneRoundFlag already exist in the source and were not silently changed.

## Follow-up repair pass

The Linux launcher previously required an ignored `.tools/dotnet` tree, so a clean checkout could not launch even with a system SDK. Shared SDK selection now supports the local SDK, system dotnet or POR_DOTNET, and the portable/native replay entry points use it. One builder-media test now takes POR_MEDIA_ROOT or the standard user-profile default instead of a private absolute path.

A portable, evidence-free 14-project gate, CI workflow, source-only snapshot manifest and explicit provenance/permission status are prepared. No new gameplay algorithm was invented to manufacture a repair finding.

## Evidence boundaries

Source-derived, checked-arithmetic, rendering, VM, serialization and sequencer regression suites and native desktop replay are separate evidence from this structural scan. Raw local logs and media-dependent oracle files are deliberately not distributed in this source-only snapshot; the full working project retains them.

Known differences/limits remain: CLR replaces DOS overlays/EMS and allocation lifecycle; Random is not a Turbo Pascal RNG-equivalence claim; Linux SDL2 provides VGA/input/PC Speaker output; physical speaker acoustics and OPL/Tandy output are not implemented here. MemShow's DOS-only measurements and packed nonzero equipped-item DOS pointers still require explicit platform handling. Full campaign and all combat paths are not validated.
