#!/usr/bin/env bash
# Self-contained source/contract gates: no game installation, Pascal checkout or private evidence.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 TZ=UTC
"$ROOT/scripts/dotnet.sh" build src/Por.Desktop --nologo -v:q
python3 scripts/test-sbsfx.py
for project in Adventure Desktop EclPair Upstream FinalThree CbtMap FxEngine Integration Types Global Pathing GameIo Border Blit Sym8x8 Debug VideoCold; do
  project_args=()
  if [[ "$project" == EclPair ]]; then project_args=(-- WildernessCompatibility); fi
  if [[ "$project" == FinalThree ]]; then project_args=(-- SoundGolden Adlib SoundTimer SoundCore); fi
  "$ROOT/scripts/dotnet.sh" run --project "tests/Por.$project.Tests" --no-launch-profile "${project_args[@]}"
done
