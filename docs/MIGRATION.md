# Spesh Base migration on Jellyfin 12.4

Spesh Base 3.1.0.3 targets Jellyfin 12.4.0 and .NET 10. Earlier Spesh Base packages retain their original 10.10 catalog entries; the 12.4 package has a separate ABI requirement. Do not change the catalog ABI to force an older binary onto a newer host.

## What has been validated

- The full regression suite runs against Jellyfin 12.4 packages.
- A separate Jellyfin 12.4 process loads the packaged plugin and its dependencies.
- Authenticated `/Spesh` and legacy client routes work; anonymous access is rejected.
- Settings and uploaded themes round-trip between API aliases and survive a clean server restart.
- The admin configuration page loads.
- A prepared configuration deserializes into the new plugin model; user JSON and theme checksums are checked separately.

This does not establish playback or external Seerr behavior in every Spesh Media app. Those are the next client acceptance checks, after the server switch. No migration tool below stops or modifies the production server.

## Prepare a snapshot

Use a new, private snapshot directory outside the source. The following example matches a standard Windows installation; substitute the actual installed plugin directory if its version differs.

```powershell
$configurations = 'C:/ProgramData/Jellyfin/Server/plugins/configurations'
$oldPlugin = 'C:/ProgramData/Jellyfin/Server/plugins/Moonbase_3.1.0.0'
$snapshot = './artifacts/migration-fresh'
dotnet run --project Jellyfin/tools/prepare-migration -c Release -- $configurations $snapshot $oldPlugin
dotnet run --project Jellyfin/tools/verify-migration -c Release -- $snapshot
```

The tools never overwrite an existing snapshot or modify source files. They reject linked data files, malformed active user JSON, invalid configuration XML and a source that changes during copying. For the final production switch, stop Jellyfin first and create a fresh snapshot so settings cannot change between verification and installation.

The snapshot contains:

| Path | Contents |
| --- | --- |
| `original/configurations` | Unmodified configuration and plugin user data |
| `original/plugin` | Original plugin binaries and frontend assets, when the plugin directory was supplied |
| `prepared/configurations/Spesh.Base.xml` | Configuration for the new assembly |
| `prepared/configurations/SpeshBase` | Copied settings, themes, caches and other plugin data |
| `report.json`, `verification.json`, `COMPLETE` | Verification summaries and completion marker |

These folders contain credentials and private user settings. Keep them private. `artifacts/` is excluded from Git; do not attach the snapshot to issues or upload it as a release asset.

User-data files retain their names and contents. The conversion preserves unknown XML fields and changes only `PushRelayEnabled`, `PushRelayUrl` and `PushRelayAppKey`, disabling the upstream relay and removing its endpoint/key. The prepared recovery configuration is also written under the new filename. Direct push credentials, integration settings, theme indexes, user IDs and device profiles are preserved. An owned relay can be configured after migration.

## Production switch

1. Stop the installed Jellyfin service or process and confirm it has stopped. Do not stop the isolated test server in place of production.
2. Prepare and verify a **fresh** snapshot using the commands above. Require both successful exits and a `COMPLETE` marker. Preserve the original server backup as well.
3. Move the old base-plugin directory entirely outside Jellyfin's `plugins` tree. Keep it for rollback. Do not run both base plugins together: the compatibility routes overlap.
4. Confirm there is no existing `Spesh.Base.xml`, `SpeshBase` data directory or active Spesh Base installation. If any exist, stop and reconcile them rather than overwriting them.
5. Copy the two prepared configuration entries into Jellyfin's `plugins/configurations` directory. Retain the original configuration and original data directory for rollback.
6. Extract the verified `Spesh.Base-3.1.0.3.zip` into a new plugin directory, such as `plugins/Spesh Base_3.1.0.3`. Ship `Spesh.Base.dll` and `SharpCompress.dll` together. If retaining an existing hosted frontend, copy its contents into `plugins/configurations/SpeshBase/web`; this persistent location survives plugin updates. Restore the service account's required read/write permissions on copied data.
7. Start Jellyfin. Wait until both the protected plugin routes and hosted web page are ready; Jellyfin's public version endpoint can respond while startup is still in progress. Confirm Spesh Base is Active, inspect startup logs and check settings and themes from one existing app before changing its API routes.
8. Add the Spesh Base repository from the README and check the available update version. Retire the old base-plugin update feed if it is no longer needed.

The separately built Spesh Media web frontend is not bundled in this package. Keep the old frontend in the backup and persistent web directory when maintaining existing access; rebuilding it is separate work. Native client branding and route migration also remain separate.

Windows service control requires an administrator session. Keep backup access permissions compatible with the service account without unnecessarily changing folder ownership. Version 3.1.0.3's original ZIP does not include `meta.json`; Jellyfin defaults such manual installs to `autoUpdate: false`. To enable updates for that installation, set its generated metadata's `autoUpdate` field to `true` while the service is stopped, then start Jellyfin. New packages built by the updated script include an update-enabled manifest; catalog installation still respects any existing local opt-out.

## Rollback

Stop Jellyfin. Move the Spesh Base plugin directory outside the plugin tree, retain its new data for diagnosis, and restore the old plugin directory. The original configuration and data were not changed by preparation. If a production switch changed them, restore those two entries from the fresh snapshot before starting Jellyfin again. Never load both base-plugin assemblies together.

## Repeat the isolated host check

On a Windows machine with Jellyfin 12.4 installed:

```powershell
./scripts/build-spesh.ps1
./scripts/test-jellyfin-host.ps1 -ThemePath /path/to/spesh-theme.json
```

The host test uses a new data directory under `artifacts`, binds to loopback on port 18097, creates an independent test account, checks APIs and restart persistence, then stops only its own process. It does not open the production database or load production credentials. The optional theme argument exercises upload, readback and persistence; omit it to run the remaining checks.
