# Spesh Base

The shared server foundation for Spesh Media, forked from Moonbase 3.1.0 plus upstream master at the commit recorded in `upstream.json`.

Spesh Base has its own plugin ID, `Spesh.Base.dll`, configuration and data folder, and GitHub release feed. Internal Moonfin namespaces, embedded resource names, JSON fields and `/Moonfin` routes remain for compatibility; `/Spesh` routes are also available. Do not run both plugins together: their compatibility routes overlap.

## Jellyfin update repository

Add **Spesh Base** in Dashboard > Plugins > Repositories:

`https://raw.githubusercontent.com/jonlukemrodriguez-ui/spesh-base/master/manifest.json`

Install Spesh Base from the catalog after the first release is published. Future releases update through Jellyfin's normal plugin update task, subject to the server's automatic update settings and restart requirements.

## Build and release

Run `./scripts/build-spesh.ps1`. Tag releases `spesh-v3.1.0.1` (four-part version); GitHub Actions tests, packages and publishes a release, then updates the catalog with the ZIP checksum. No upstream release URLs are advertised in the Spesh catalog.

The package currently targets the upstream Jellyfin 10.10 ABI. Jellyfin 12 compatibility must be verified before replacing the production plugin. No server deployment or migration has been performed. Existing Moonfin configuration/data must be backed up and deliberately copied to the Spesh configuration/data location before removing the old installation.

The upstream Flutter web app is not bundled. Supply a separately built Spesh Media web frontend before enabling web hosting. Native client branding and service URL migration remain separate client work. Hosted Moonfin push is off by default; configure an owned relay or direct push credentials if needed.

## Upstream and license

Derived from [Moonfin-Client/Plugin](https://github.com/Moonfin-Client/Plugin). Original copyright and GPL-3.0 license are retained in LICENSE and source. See README.upstream.md for upstream capabilities. This fork is maintained independently for Spesh Media; Emby and Go ports are retained as upstream source and are not part of the Spesh release pipeline.
