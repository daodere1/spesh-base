param([string]$Version = '3.1.0.3')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$out = Join-Path $root 'artifacts'
$stage = Join-Path $out $Version
if (Test-Path $stage) { throw "Package staging already exists: $stage. Choose a new version or remove that staging folder." }
New-Item -ItemType Directory -Path $stage -Force | Out-Null
dotnet publish (Join-Path $root 'Jellyfin/backend/Moonfin.Server.csproj') -c Release -o (Join-Path $out 'publish') "-p:Version=$Version" "-p:AssemblyVersion=$Version" "-p:FileVersion=$Version"
if ($LASTEXITCODE -ne 0) { throw 'Publish failed' }
dotnet run --project (Join-Path $root 'Jellyfin/tools/verify-plugin') -c Release -- (Join-Path $out 'publish/Spesh.Base.dll') $Version
if ($LASTEXITCODE -ne 0) { throw 'Plugin resource/version verification failed' }
Copy-Item (Join-Path $out 'publish/Spesh.Base.dll') $stage
Copy-Item (Join-Path $out 'publish/SharpCompress.dll') $stage
Copy-Item (Join-Path $root 'LICENSE') $stage
$zip = Join-Path $out "Spesh.Base-$Version.zip"
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip -Force
Write-Host "Packaged $zip"
