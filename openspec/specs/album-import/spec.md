# album-import Specification

## Purpose

Lets a person search a Music Source for an artist, browse that artist's albums, preview tracklists, and import albums into the app so their songs can be used in the Setlist Builder. This spec describes the Import Albums page (`Components/Pages/ImportAlbums.razor`) as built; how each Music Source maps its API to artists, albums and songs is in `music-service-sources`, and how imported albums are stored is in `album-library`.

## Requirements

### Requirement: Import Albums Page
The Web app SHALL expose the Import Albums page at the route `/import`, rendered with the Interactive Server render mode, with the page title "Import Albums". The page SHALL offer four Music Source buttons in the order MusicBrainz, Discogs, iTunes, Deezer, with MusicBrainz selected when the page opens, and the selected source SHALL be highlighted.

#### Scenario: Opening the page
- **WHEN** a person navigates to `/import`
- **THEN** the page shows the heading "Import Albums", the "Imported albums" section, the "Search & import" section, and the MusicBrainz button highlighted as the active source

### Requirement: Imported Albums List
The page SHALL list all Imported Albums currently held by the album library, ordered by album name, showing each album's name and its song count ("N songs"). When there are none it SHALL show "No albums imported yet.". Each entry SHALL have a "Remove" button that removes that album from the library immediately, without a confirmation step.

#### Scenario: No albums imported
- **WHEN** the album library is empty
- **THEN** the page shows "No albums imported yet."

#### Scenario: Removing an imported album
- **WHEN** a person selects "Remove" next to an imported album
- **THEN** the album is removed from the album library and disappears from the list without further confirmation

### Requirement: Artist Search
The page SHALL provide an "Artist name" text box and a "Search" button. Searching SHALL do nothing when the text is empty or whitespace. Otherwise it SHALL clear any previous artist/album results and messages, show "Loading…" while waiting, trim the text, and query the active Music Source for artists. Pressing the Enter key in the text box SHALL run the search handler, but with whatever text has already been committed to the page state: the text box uses `@bind` (the `change` event, fired on blur or Enter) together with an `@onkeydown` handler, so the keydown for the first Enter after typing is handled before the new text is committed and may search the previously committed text (an empty text is ignored, so nothing visibly happens until a second Enter or until the field is left). This follows from Blazor's event semantics and has not been confirmed in a browser. Note: current behavior, known defect; changing it requires a spec change. The Search button SHALL be disabled while a request is loading.

#### Scenario: Searching with results
- **WHEN** a person enters an artist name and selects "Search"
- **THEN** the page lists the matching artists under an "Artists" heading, each showing the artist name and, when the source supplies one, a " — {disambiguation}" suffix

#### Scenario: Searching with no results
- **WHEN** the active Music Source returns no artists for the query
- **THEN** the page shows `No artists found for "<query>".`

#### Scenario: Search fails
- **WHEN** the Music Source request throws (for example a network error or HTTP error status)
- **THEN** the page shows an error alert "Search failed: {exception message}" and the loading state ends

#### Scenario: Enter right after typing
- **WHEN** a person types a new artist name and presses Enter without leaving the field first
- **THEN** the first Enter may search the previously committed text instead of the new one

#### Scenario: Empty query
- **WHEN** a person selects "Search" with an empty or whitespace-only query
- **THEN** nothing happens (no request is made and existing results stay)

### Requirement: Album Listing For A Selected Artist
Selecting an artist in the results SHALL highlight it, clear previous album results, messages and tracklist previews, and load that artist's albums from the active Music Source. The albums SHALL be shown under the heading "Albums — {artist name}", each with its title and, if known, the release year in parentheses.

#### Scenario: Artist has albums
- **WHEN** a person selects an artist
- **THEN** the page lists the artist's albums with title and "(year)" where a year is known

#### Scenario: Artist has no albums
- **WHEN** the Music Source returns no albums for the selected artist
- **THEN** the page shows "No official albums found for this artist."

#### Scenario: Album listing fails
- **WHEN** loading the albums throws
- **THEN** the page shows an error alert "Failed to load albums: {exception message}"

### Requirement: Tracklist Preview
Each listed album SHALL have a toggle button (▼ collapsed, ▲ expanded). Expanding SHALL fetch the album with its tracks from the active Music Source, show "Loading tracklist…" meanwhile, and then show the tracks as an ordered list. A track's duration SHALL be shown as " — m:ss" only when it is greater than zero. A fetched tracklist SHALL be cached for the current artist selection so re-expanding does not fetch again. If the fetch fails the album SHALL silently collapse and no error is shown. Collapsing SHALL hide the list but keep the cache.

#### Scenario: Previewing a tracklist
- **WHEN** a person expands an album
- **THEN** the tracks of that album are listed in order with their durations

#### Scenario: Preview fetch fails
- **WHEN** fetching the tracks for the preview throws
- **THEN** the album collapses again and no error message is displayed

#### Scenario: Re-expanding a cached preview
- **WHEN** a person collapses and expands an album whose tracklist was already fetched
- **THEN** the tracklist is shown without another request to the Music Source

### Requirement: Importing An Album
Each listed album SHALL have an import button. Selecting it SHALL fetch the album with its tracks from the active Music Source and store it in the album library. On success the page SHALL show `"{album title}" imported with {N} songs.` and the button SHALL read "Imported ✓" (green) for the remainder of the page session. While the import runs the button SHALL read "Importing…" and be disabled. On failure the page SHALL show an error alert "Import failed: {exception message}". Importing an album whose id is already in the library SHALL replace the stored album (see `album-library`).

#### Scenario: Successful import
- **WHEN** a person selects "Import" for an album
- **THEN** the album with its songs is saved to the album library, the success message is shown, the "Imported albums" list includes it, and the button shows "Imported ✓"

#### Scenario: Import fails
- **WHEN** fetching or storing the album throws
- **THEN** an error alert "Import failed: {exception message}" is shown and the button returns to "Import"

#### Scenario: Imported marker is per page session
- **WHEN** a person reopens the Import Albums page and searches again
- **THEN** albums imported in an earlier page session are not marked "Imported ✓" (the marker is not derived from the stored library), although they appear in the "Imported albums" list

### Requirement: Switching Music Source
Selecting a different Music Source button SHALL make it active and reset the artist results, selected artist, album results, search text, messages and tracklist previews.

#### Scenario: Changing the source
- **WHEN** a person selects the "Deezer" button after searching on MusicBrainz
- **THEN** the artist and album results and the search text are cleared and subsequent searches use Deezer

### Requirement: Discogs Requires A Configured Token
When Discogs is the active source and no Discogs token is configured, the page SHALL show an error alert "Discogs token not configured. Add your personal access token to `appsettings.json` under `Discogs:Token`." with a link to discogs.com/settings/developers, and SHALL NOT show the search box or any results.

#### Scenario: Discogs without a token
- **WHEN** a person selects Discogs and `Discogs:Token` is not set
- **THEN** the alert is shown and the artist search input is not rendered

#### Scenario: Discogs with a token
- **WHEN** `Discogs:Token` is set and Discogs is selected
- **THEN** the artist search input is shown like for the other sources
