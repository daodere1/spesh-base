# Spesh Cinematic

Reference direction: cinema-black canvas, a compact rounded left rail, large landscape hero, rounded artwork and row containers, muted metadata, gold accents, and a blue-to-gold focus ring.

## Review

Open `cinematic-preview.html` in a browser. The interactive prototype includes home, searchable sample titles, a monthly agenda, and a details modal with season selection. It uses sample content and Unsplash landscape photography; it does not connect to Jellyfin or play media. `cinematic-preview.jpg` records the browser review.

## Shared appearance

`../themes/spesh-cinematic.theme.json` is the schema-v1 theme, embedded in Base and included in authenticated theme sync. Existing administrator uploads with the same ID take precedence. Existing profiles are not changed.

`../themes/spesh-cinematic.profile.json` is an appearance-only **profile fragment**, not a full plugin configuration or direct import payload. Merge the desired fields into a Desktop/TV profile through the existing settings editor; do not replace a complete saved profile with this fragment. It proposes left navigation, a trailer-capable spotlight hero, landscape thumbnails, and restrained focus scaling.

Both maintained Flutter clients bundle the same fallback theme, selectable under Appearance → Theme. Their Material cards, inputs, media-card outlines, and shared focus outlines now support the Cinematic treatment. Clients without the new renderer use the schema's solid blue focus fallback.

## Implementation boundary

The prototype's grouped shelves, centered details modal, agenda composition, and responsive navigation remain preview layouts. They are not installed as a new production frontend by this change. The production calendar and details routes still use their existing client implementations. Base supplies themes and settings; each client owns its screen layout.

No production deployment, playback changes, account changes, or profile migration is performed by this design work.


## Trailer and branding follow-up

The profile fragment selects the existing trailer-capable media bar (stored compatibility value: moonfin), with mediaBarTrailerPreview enabled. Banner mode is intentionally not selected because it renders artwork only. Saved trailer audio preferences, available sources, and device performance limits still apply. The HTML prototype does not play trailers.

The client branding pass replaces visible Moonfin/Moonbase names with SpeshTV/Spesh Base, switches logo rendering to SpeshTV assets and wordmarks, removes unused upstream logo assets and install screenshots, and points project/support actions to the SpeshTV repository. API routes, serialized preference IDs, and license notices retain compatibility.
