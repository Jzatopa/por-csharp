# TPC.CFG disposition

The pinned12-line Turbo Pascal compiler configuration is build input, not a runtime unit. It is replaced by `src/Por.Core/Por.Core.csproj`, explicit source numeric/serialization contracts and the checked test projects. No TPC.CFG is executed or copied into the runtime.

| Original option | Managed disposition |
|---|---|
| `/U.;INCLUDES` | Source units included recursively by the SDK project; no DOS include search path |
| `/L` | Legacy compiler/linker switch; no direct managed counterpart. Exact compiler-version interpretation is not independently verified. |
| `/$F+` | Far calls replaced by managed methods/delegates |
| `/$A-` | Packed records serialized explicitly; CLR object layout is not written to disk |
| `/$E+`, `/$N-` | Pascal real emulation/FPU selection replaced by documented managed arithmetic; Real48 support remains explicit |
| `/GD` | Legacy map/build-output selection replaced by SDK artifacts; no DOS map-output compatibility claim |
| `/$D+`, `/$L-` | SDK debug symbols/diagnostics; no original overlay linker metadata |
| `/$R-` | No unchecked managed array indexing; original indices have explicit windows/accessors |
| `/$S-` | Managed runtime owns stack safety; no DOS stack-check switch |
| `/$V+` | Source short-string limits enforced at translation/serialization boundaries; not CLR string layout |

The core project's `CheckForOverflowUnderflow=false` does not alone establish Pascal arithmetic fidelity. Tests compile the production sources with checked arithmetic to expose missing explicit narrowings. This disposition is a checked build-configuration replacement, not another C# conversion or gameplay validation. The original config remains read-only at the pinned source revision.

## Test and benchmark compiler configurations

`tests/TPC.CFG` and `bench/TPC.CFG` contain the same 12 switches. Their only difference from the game configuration is the unit search path `/U.;..\SRC;..\SRC\INCLUDES`, replaced by explicit project references to `Por.Core` and SDK source inclusion. The table above applies to every remaining switch. `tests/Por.Upstream.Tests/Por.Upstream.Tests.csproj` replaces the test build and `src/Por.UpstreamTools/Por.UpstreamTools.csproj` builds both benchmarks. Far calls, DOS object linking and emulator CPU speed are deliberately not reproduced by the managed toolchain. This is a platform-specific configuration replacement, not a claim that an unknown legacy switch was translated to a guessed SDK flag.
