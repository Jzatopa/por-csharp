#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")" && pwd)"
"$ROOT/scripts/dotnet.sh" build "$ROOT/src/Por.Desktop/Por.Desktop.csproj" --nologo -v:q
exec "$ROOT/scripts/dotnet.sh" "$ROOT/src/Por.Desktop/bin/Debug/net8.0/Por.Desktop.dll" "$@"
