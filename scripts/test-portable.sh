#!/usr/bin/env bash
# Self-contained source/contract gates: no game installation, Pascal checkout or private evidence.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 TZ=UTC
"$ROOT/scripts/dotnet.sh" build src/Por.Desktop --nologo -v:q
for project in Adventure Desktop CbtMap FxEngine Integration Types Global Pathing GameIo Border Blit Sym8x8 Debug VideoCold; do
  "$ROOT/scripts/dotnet.sh" run --project "tests/Por.$project.Tests" --no-launch-profile
done
