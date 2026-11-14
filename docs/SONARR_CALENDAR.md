# Direct Sonarr calendar fallback

Available in Spesh Base 3.1.0.5. Direct Sonarr is disabled until an administrator configures it.

## Administrator configuration

Under **Spesh Base → Calendar**, configure:

| Plugin configuration field | Default | Meaning |
| --- | --- | --- |
| `SonarrCalendarEnabled` | `false` | Allow the direct, read-only calendar fallback. |
| `SonarrUrl` | empty | Base URL reachable from Jellyfin, including a proxy path such as `http://localhost:8989/sonarr`. |
| `SonarrApiKey` | empty | Sonarr API key, stored only in the administrator's server configuration. |

URLs must use HTTP or HTTPS and cannot contain user credentials, query parameters, or fragments. HTTPS supports remote hosts. Plain HTTP is permitted only when every resolved address is local/private, using Core's existing network-address policy. The direct HTTP client does not follow redirects. Keys are sent in the `X-Api-Key` header, never a URL.

Seerr remains the preferred calendar source when configured and successful. If its connection/settings/admin session or calendar request is unavailable, a valid direct Sonarr connection is used. With Seerr disabled, direct Sonarr works without any Seerr session. A disabled/unconfigured fallback preserves existing Seerr behavior.

This configuration does not enable direct Radarr, Sonarr queue operations, media requests, or arbitrary Sonarr proxying.

## Client capabilities

Authenticated `GET /Spesh/Ping` (existing `/Moonfin/Ping` alias) includes:

```json
{
  "calendarAvailable": true,
  "sonarrCalendarAvailable": true,
  "directSonarrCalendarAvailable": true,
  "calendarDaysBack": 14,
  "calendarDaysAhead": 90
}
```

- `calendarAvailable` / `sonarrCalendarAvailable`: Seerr or direct Sonarr is configured, and this user passes the calendar admin-only policy.
- `directSonarrCalendarAvailable`: valid direct configuration is enabled, and this user passes the same policy.
- These are configuration capabilities, not connectivity or authentication health probes. No Sonarr URL or key is returned.
- `seerrAvailable` and existing queue/request capabilities are unchanged. Gate Radarr and Seerr request features separately.
- Clients may use `sonarrCalendarAvailable ?? calendarAvailable` for compatibility; Sonarr calendar access must not require `seerrAvailable` or a Seerr login.

## Calendar request

The existing client endpoint remains supported:

```text
GET /Moonfin/Seerr/Sonarr/Calendar?start=2026-11-14&end=2026-12-14&unmonitored=false
GET /Spesh/Seerr/Sonarr/Calendar?start=2026-11-14&end=2026-12-14&unmonitored=false
```

The additional clean alias is:

```text
GET /Spesh/Sonarr/Calendar?start=2026-11-14&end=2026-12-14&unmonitored=false
```

All require normal Jellyfin authentication. Existing `CalendarAdminsOnly`, `CalendarDaysBack`, `CalendarDaysAhead`, cache TTL, and per-user library annotations/filtering apply identically. Requests outside the configured window are clamped; an empty window returns `[]`.

The response remains the Sonarr v3 calendar JSON array with the existing `moonfinItemId`, `moonfinEpisodeId`, and `moonfinLibraryIds` annotations. `airDateUtc` is preserved, including the `Z`/offset supplied by Sonarr. Core does not invent a time from `airDate` or Jellyfin date-only metadata. Clients convert explicit UTC times to their display timezone and treat missing times as unknown.

Only successful raw calendar responses are cached, before user-specific annotations. Window and unmonitored selection are distinct cache keys. Changing connection details or credentials partitions the cache so an old server's response is not reused. A cache TTL of zero bypasses caching.

Authentication/policy failures remain 401/403; unconfigured calendars return 503; unreachable or disallowed direct transports return 502. Upstream Sonarr error statuses may also be returned. Do not interpret a configured capability flag as proof the upstream server is currently reachable.

## Validation

The candidate adds tests for Seerr-first selection, fallback without Seerr/admin session, network failures, invalid URLs/header injection, credential rotation/cache partitioning, UTC payload preservation, authentication attributes/route aliases, non-secret capability serialization, reverse-proxy query construction, and HTTP public-network refusal.

Run `scripts/test-sonarr-calendar-host.ps1 -Version <local-candidate>` after building a local candidate. It starts the synthetic fixture in `Jellyfin/tests/fixtures/sonarr-calendar-server.cjs`, invokes an isolated Jellyfin host, checks the shared cache/window behavior, and stops both processes. It requires Node.js and the local Jellyfin 12.4 executable. This test uses the synthetic key `spesh-isolated-sonarr-key`; never point the lower-level test harness at a real Sonarr instance. No production credentials are read or needed.
