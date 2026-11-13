# album-library Specification

## Purpose

The set of Imported Albums the app holds, so their Songs can be offered in the Setlist Builder. Implemented by `ISetlistService.GetAlbums/GetSongsForAlbum/ImportAlbum/RemoveAlbum` in `InMemorySetlistService` (in-memory list) and `LiteDbSetlistService` (which mirrors changes into the LiteDB collection `albums`). The UI is described in `album-import` and `setlist-builder`.

## Requirements

### Requirement: Album Lookup
`GetAlbums` SHALL return all Imported Albums in the order they are held in memory. `GetSongsForAlbum(albumId)` SHALL return the Songs of the album with that id in their stored order, or an empty list when no such album exists.

#### Scenario: Unknown album id
- **WHEN** `GetSongsForAlbum` is called with an id that matches no Imported Album
- **THEN** an empty list is returned and no exception is thrown

#### Scenario: Fresh library
- **WHEN** a service instance is created over an empty database
- **THEN** `GetAlbums` returns an empty list (no albums are seeded)

### Requirement: Importing An Album Replaces By Id
`ImportAlbum(album)` SHALL reject a null album with `ArgumentNullException`. If an Imported Album with the same id exists it SHALL be removed first, and the new album SHALL then be appended at the end of the list, so a re-imported album moves to the end of `GetAlbums`.

#### Scenario: Re-importing the same album
- **WHEN** an album with an id that is already in the library is imported again with different songs
- **THEN** the library contains exactly one album with that id, holding the newly imported songs, and it is listed last

### Requirement: Removing An Album
`RemoveAlbum(albumId)` SHALL remove the album with that id and SHALL NOT fail when no such album exists. Removing an album SHALL NOT change any Saved Setlist, because Setlist Items hold copies of name and duration only.

#### Scenario: Removing an album used in a setlist
- **WHEN** an album is removed whose Songs were added to a Saved Setlist
- **THEN** the Saved Setlist keeps all its items unchanged

### Requirement: Albums Are Persisted In LiteDB
`LiteDbSetlistService` SHALL write an imported album to the LiteDB collection `albums` (id, name, and songs with id, title, duration ticks and album name) by upsert on the album id, and SHALL delete the album document when the album is removed. When a `LiteDbSetlistService` instance is constructed it SHALL load all stored album documents into its in-memory list, and `GetAlbums`/`GetSongsForAlbum` SHALL be answered from that in-memory list afterwards.

#### Scenario: Albums survive a restart
- **WHEN** an album is imported through one `LiteDbSetlistService` and a new instance is created over the same database file
- **THEN** the new instance's `GetAlbums` contains that album with its songs and durations

#### Scenario: Other service instances do not see later changes
- **WHEN** an album is imported through one `LiteDbSetlistService` instance while another instance created earlier exists over the same file
- **THEN** the earlier instance's `GetAlbums` does not include the album until a new instance is created (the in-memory list is only loaded in the constructor)
