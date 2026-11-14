# Build Spesh Base for Jellyfin. Catalog publication is handled by GitHub Actions.
param([string]$Version = '3.1.0.2')
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot '../scripts/build-spesh.ps1') -Version $Version
