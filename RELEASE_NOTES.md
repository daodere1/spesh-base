# Spesh Base 3.1.0.5

- Adds an optional administrator-configured direct Sonarr calendar fallback. Seerr remains preferred; episode calendars can work without a Seerr admin session or with Seerr disabled.
- Keeps Sonarr API keys on the server. Direct connections use HTTPS or local-network HTTP and do not follow redirects.
- Adds non-secret `sonarrCalendarAvailable` and `directSonarrCalendarAvailable` capabilities. Existing authenticated Sonarr calendar routes remain supported, with a new `/Spesh/Sonarr/Calendar` alias.
- Preserves UTC air times, calendar permissions, date-window limits, caching, user-library annotations, existing Seerr requests/queues, settings, and the bundled Cinematic theme.

Configure the optional connection in **Spesh Base → Calendar**. It is disabled by default; this update does not discover or import credentials. See [the API and configuration contract](docs/SONARR_CALENDAR.md).

Requires Jellyfin 12.4.0 and a server restart after installation. No web frontend redesign or native client is included. Existing hosted frontend persistence is retained.

Validation: 1,479 Core tests and an isolated Jellyfin 12.4/Sonarr fixture test passed, including authentication, UTC timestamps, aliases, cache sharing, date clamping, and restart persistence.

GPL-3.0. Upstream copyright and license attribution remain in the source and package.
