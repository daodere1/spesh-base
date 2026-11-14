# Contributing to Spesh Base

Spesh Base is the Jellyfin server foundation for Spesh Media. All development, testing and releases target Jellyfin.

## Setup and checks

Use the .NET 8 SDK and PowerShell 7. From the repository root:

```powershell
dotnet test Jellyfin/server-plugin.sln -c Release
./scripts/build-spesh.ps1
```

The backend lives in `Jellyfin/backend`; feature code and its tests live in `shared` and are compiled into the Jellyfin projects. The separate Spesh Media web frontend is not bundled here.

## Changes

Keep existing client API contracts compatible. Synced settings must match client keys and remain nullable when unset. Configuration is serialized as XML, so avoid dictionaries in configuration models. Admin settings must be loaded and saved together.

Test changes against a separate Jellyfin server before production installation. Include the Spesh Base version, Jellyfin version, affected client and relevant validation in pull requests. Never include credentials or private server data.

## Releases

The `spesh-v<four-part-version>` tag triggers the Spesh Base release workflow. It runs the Jellyfin tests, packages the plugin and publishes the GitHub release and update catalog. Catalog entries must retain the Spesh Base plugin ID and point to Spesh-owned release assets.

## Attribution and license

Spesh Base derives from the [upstream source](https://github.com/Moonfin-Client/Plugin); its revision is recorded in `upstream.json`. Preserve original copyright notices and the GPL-3.0 license. Contributions use the same license; see [LICENSE](LICENSE).
