# Spesh Base

The shared server foundation for Spesh Media, built on the upstream revision recorded in `upstream.json`. Spesh Base supports Jellyfin only.

Spesh Base has its own plugin ID, `Spesh.Base.dll`, configuration and data folder, and GitHub release feed. Existing client API contracts remain compatible, with `/Spesh` routes also available. Do not run the upstream plugin alongside Spesh Base because compatibility routes overlap.

## Jellyfin update repository

Add **Spesh Base** in Dashboard > Plugins > Repositories:

`https://raw.githubusercontent.com/jonlukemrodriguez-ui/spesh-base/master/manifest.json`

Install Spesh Base from the catalog after the first release is published. Future releases update through Jellyfin's normal plugin update task, subject to the server's automatic update settings and restart requirements.

## Build and release

Run `./scripts/build-spesh.ps1`. Tag releases `spesh-v3.1.0.2` (four-part version); GitHub Actions tests, packages and publishes a release, then updates the catalog with the ZIP checksum. No upstream release URLs are advertised in the Spesh catalog.

The package currently targets the upstream Jellyfin 10.10 ABI. Jellyfin 12 compatibility must be verified before replacing the production plugin. No server deployment or migration has been performed. Existing upstream configuration/data must be backed up and deliberately copied to the Spesh configuration/data location before removing the old installation.

The upstream Flutter web app is not bundled. Supply a separately built Spesh Media web frontend before enabling web hosting. Native client branding and service URL migration remain separate client work. The upstream hosted push relay is off by default; configure an owned relay or direct push credentials if needed.

## Upstream and license

Derived from the [upstream source](https://github.com/Moonfin-Client/Plugin). Original copyright and GPL-3.0 license notices are retained in LICENSE and source. Spesh Base is maintained independently for Spesh Media and ships only a Jellyfin plugin.
