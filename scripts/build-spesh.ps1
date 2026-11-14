param([string]$Version = '3.1.0.4')
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
# Without a manifest Jellyfin treats manual installs as non-updating local plugins.
[ordered]@{
    guid = '7d374fa1-67cd-4de8-9f93-fd825c14b502'
    name = 'Spesh Base'
    owner = 'Spesh Media'
    overview = 'The Jellyfin server foundation for Spesh Media'
    description = 'Spesh Base provides settings sync, themes, ratings and Seerr integrations for Spesh Media.'
    category = 'General'
    version = $Version
    targetAbi = '12.4.0.0'
    status = 'Active'
    autoUpdate = $true
    assemblies = @('Spesh.Base.dll', 'SharpCompress.dll')
} | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $stage 'meta.json')
$zip = Join-Path $out "Spesh.Base-$Version.zip"
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip -Force
Write-Host "Packaged $zip"
