#!/usr/bin/env bash
# Build Spesh Base for Jellyfin with PowerShell 7.
set -euo pipefail
script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
exec pwsh -NoProfile -File "$script_dir/../scripts/build-spesh.ps1" -Version "${1:-3.1.0.2}"
