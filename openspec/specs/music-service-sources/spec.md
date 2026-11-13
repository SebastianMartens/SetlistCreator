# music-service-sources Specification

## Purpose

Defines how each Music Source (MusicBrainz, Discogs, iTunes, Deezer) turns its public web API into artist results, album results and Albums with Songs for the importer. All four are classes in `src/SetlistCreator.Web/Services/` implementing `IMusicSearchService`; the UI that drives them is described in `album-import`.

## Requirements

### Requirement: Common Music Source Contract
Every Music Source SHALL implement `IMusicSearchService` with three operations: search artists by text (returning `ArtistResult` with id, name, optional disambiguation), list an artist's albums by artist id (returning `AlbumResult` with id, title, optional year string), and fetch one album with its tracks given the album result id and title (returning an `Album` whose Songs carry the album title as their album name). Every Song produced SHALL get a new random `Guid` id on each fetch. A track whose duration is missing SHALL get the Default Duration of three minutes; MusicBrainz, iTunes and Deezer also apply it when the duration is zero, and Discogs also applies it when the duration text cannot be parsed (a literal `0:00` from Discogs yields a zero duration). Request failures (network errors, non-success HTTP status) SHALL propagate as exceptions to the caller.

#### Scenario: Track without a duration
- **WHEN** a source returns a track with no length or a length of zero
- **THEN** the resulting Song has a duration of 3 minutes

#### Scenario: Songs carry the album name
- **WHEN** an album is fetched with title "Surfits Live"
- **THEN** every resulting Song has `AlbumName` "Surfits Live"

### Requirement: MusicBrainz Source
The MusicBrainz source SHALL call `https://musicbrainz.org/ws/2/` with JSON output. Artist search SHALL use `artist?query=<text>&limit=10` and map id, name and disambiguation. Album listing SHALL use `release?artist=<mbid>&type=album&status=official&limit=100`, map the release id, title and the first four characters of the release date as year (null when the date is shorter than four characters), and order results by year. Album fetch SHALL use `release/<mbid>?inc=recordings`, flatten the tracks of all media in order, use the track length in milliseconds as duration, and produce an Album whose id is the release id parsed as a `Guid`.

#### Scenario: Importing a release
- **WHEN** a release with two media is fetched
- **THEN** the album contains the tracks of both media in order, and the album id equals the release MBID

#### Scenario: Release listing contains all official releases
- **WHEN** an artist has several official releases of the same album (different editions)
- **THEN** each release is listed as its own album result (no de-duplication)

### Requirement: Discogs Source
The Discogs source SHALL call `https://api.discogs.com/` and report itself as configured only when an `Authorization` header is present on its HTTP client (see `app-configuration`). Artist search SHALL use `database/search?type=artist&per_page=10`. Album listing SHALL use `artists/<id>/releases?per_page=100&sort=year&sort_order=asc` and return every entry (masters and releases, regardless of the artist's role), with an album result id of the form `<type>:<id>` (for example `master:123` or `release:456`) and the year as text. Album fetch SHALL call `masters/<id>` when the type is not `release` (also for a bare numeric id) and `releases/<id>` when it is `release`, drop tracklist entries whose type is `heading` or whose title is blank, parse durations of the form `m:ss` (anything else, including blank and `h:mm:ss`, becomes the Default Duration), and produce an Album whose id is a deterministic `Guid` derived from the numeric Discogs id.

#### Scenario: Master versus release id
- **WHEN** an album result id `release:456` is fetched
- **THEN** the source requests `releases/456`; for `master:123` it requests `masters/123`

#### Scenario: Tracklist headings are skipped
- **WHEN** the Discogs tracklist contains entries of type `heading`
- **THEN** those entries do not become Songs

#### Scenario: Same numeric id yields the same album id
- **WHEN** the same Discogs numeric id is imported twice
- **THEN** both imports produce the same album `Guid` (so the second replaces the first in the library)

### Requirement: iTunes Source
The iTunes source SHALL call `https://itunes.apple.com/`. Artist search SHALL use `search?entity=musicArtist&limit=10` and keep results with wrapper type `artist` and an artist id. Album listing SHALL use `lookup?id=<artistId>&entity=album&limit=200`, keep results with wrapper type `collection` and collection type `Album`, use the collection id as album result id, the first four characters of the release date as year, and order by year. Album fetch SHALL use `lookup?id=<collectionId>&entity=song`, keep results with wrapper type `track` and a non-blank track name, use `trackTimeMillis` as duration, and produce an Album whose id is a deterministic `Guid` derived from the collection id (the 8 bytes of the `long` placed at the start of a 16-byte array).

#### Scenario: Only albums are listed
- **WHEN** the lookup returns the artist entry itself or collections whose collection type is not `Album`
- **THEN** only entries with collection type `Album` become album results

### Requirement: Deezer Source
The Deezer source SHALL call `https://api.deezer.com/`. Artist search SHALL use `search/artist?limit=10`. Album listing SHALL use `artist/<id>/albums?limit=100`, keep entries whose `record_type` is `album`, use the first four characters of `release_date` as year, and order by year. Album fetch SHALL use `album/<id>/tracks` (with no explicit limit or paging), use the track `duration` in seconds, and produce an Album whose id is a deterministic `Guid` derived from the album id (the 8 bytes of the `long` placed at the start of a 16-byte array, the same derivation as the iTunes source).

#### Scenario: Singles and EPs are excluded
- **WHEN** Deezer returns albums with record type `single` or `ep`
- **THEN** they are not listed as album results

### Requirement: HTTP Client Identification
Every Music Source HTTP client SHALL send a `User-Agent` header of the form `SetlistCreator/1.0 (+https://github.com/SebastianMartens/SetlistCreator)`.

#### Scenario: User agent
- **WHEN** any Music Source sends a request
- **THEN** the request carries the SetlistCreator user agent
