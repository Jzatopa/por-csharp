#!/usr/bin/env bash
# Shared SDK selection for a working tree or a clean checkout; never downloads a compiler.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
if [[ -n "${POR_DOTNET:-}" ]]; then
  SDK="$POR_DOTNET"
elif [[ -x "$ROOT/.tools/dotnet/dotnet" ]]; then
  SDK="$ROOT/.tools/dotnet/dotnet"
elif command -v dotnet >/dev/null 2>&1; then
  SDK="$(command -v dotnet)"
else
  echo "Install the .NET 8 SDK, or set POR_DOTNET to its dotnet executable." >&2
  exit 127
fi
exec "$SDK" "$@"
