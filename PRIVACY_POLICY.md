# Spesh Base data and integrations

Spesh Base runs as a plugin on your Jellyfin server. Configuration, synced user settings and enabled feature data are stored on that server under the server administrator's control. Configuration can contain integration credentials; protect server files and backups.

Enabled integrations make requests to their configured services, such as Seerr, TMDB, MDBList and game metadata providers. Those services receive the requests required for the selected feature. The upstream hosted push relay is disabled and has no default URL. Administrators can configure their own relay or direct push credentials.

Spesh Base updates are downloaded from this project's GitHub releases through Jellyfin's plugin update system. The server administrator controls plugin configuration, data retention, integrations and access to server logs.
