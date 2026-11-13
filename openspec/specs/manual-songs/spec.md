# manual-songs Specification

## Purpose

Describes Manual Songs: songs typed in by hand (title and duration) rather than imported from a Music Source. This baseline spec documents the CURRENT behavior including a known gap: a Manual Song is not stored anywhere except as a name and duration inside the Setlist it was added to. Persisting Manual Songs for reuse is planned in issue #7 and will modify this spec.

## Requirements

### Requirement: Creating A Manual Song
`ISetlistService.CreateManualSong(title, duration)` SHALL reject a null/empty/whitespace title with `ArgumentException` ("Title is required.") and a duration of zero or less with `ArgumentOutOfRangeException` ("Duration must be greater than zero."). Otherwise it SHALL return a new `Song` with a new random id, the trimmed title, the given duration and no album name. `LiteDbSetlistService.CreateManualSong` SHALL delegate to the in-memory implementation.

#### Scenario: Valid manual song
- **WHEN** `CreateManualSong("  Home Grown  ", 4 minutes)` is called
- **THEN** a Song with title "Home Grown", duration 4:00 and no album name is returned

#### Scenario: Invalid manual song
- **WHEN** `CreateManualSong(" ", 1 minute)` or `CreateManualSong("Manual", 0)` is called
- **THEN** `ArgumentException` respectively `ArgumentOutOfRangeException` is thrown

### Requirement: Manual Songs Are Not Persisted
Note: current behavior, known gap tracked in issue #7; fixing it will modify this requirement.

`CreateManualSong` SHALL NOT store the created Song in the Album library or in any database collection. A Manual Song SHALL exist beyond the call only as a Setlist Item (type `Song`, name, duration) after the caller adds it to a Setlist, and therefore only as long as that Setlist exists (in memory) or is saved (see `setlist-persistence`). Manual Songs SHALL NOT appear in the Setlist Builder's album selector or song search, and a Manual Song added to one Setlist cannot be picked again for another Setlist.

#### Scenario: Manual song is not reusable
- **WHEN** a Manual Song is added to a Setlist and the app later creates a new Setlist
- **THEN** the Manual Song is not offered in the album selector or search of the new Setlist

#### Scenario: Manual song survives only via a saved setlist
- **WHEN** a Manual Song is added to a Setlist in the builder
- **THEN** it is saved only because the builder autosaves the Setlist (see `setlist-builder`), as an ordinary Song item; deleting that Saved Setlist removes the Manual Song entirely

#### Scenario: Manual song is not in the library
- **WHEN** a Manual Song has been created and a new service instance is created over the same database
- **THEN** `GetAlbums` and `GetSongsForAlbum` return no trace of it
