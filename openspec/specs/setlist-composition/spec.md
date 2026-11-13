# setlist-composition Specification

## Purpose

The backend rules for putting Songs and Breaks onto a working Setlist and computing its Total Duration. Implemented in `InMemorySetlistService` (`AddSong`, `AddBreak`, `CalculateTotalDuration`) and delegated to by `LiteDbSetlistService`; models are `Setlist`, `SetlistItem`, `SetlistItemType`, `Song`. These operations only change the in-memory `Setlist` object passed in; persistence is described in `setlist-persistence`.

## Requirements

### Requirement: Setlist Model
A `Setlist` SHALL have an `Id` (`Guid`, a new random value on creation), a `VenueName` (string, empty by default), an optional `GigDate` (`DateOnly?`) and an ordered list of `SetlistItem`s. A `SetlistItem` SHALL consist of a type (`Song` or `Break`), a name and a duration.

#### Scenario: New setlist defaults
- **WHEN** a `Setlist` is created with no arguments
- **THEN** it has a fresh non-empty id, an empty venue name, no gig date and no items

### Requirement: Adding A Song
`AddSong(setlist, song)` SHALL reject a null setlist or song with `ArgumentNullException` and otherwise append a Setlist Item of type `Song` whose name is the Song's title and whose duration is the Song's duration. The item SHALL NOT retain the Song's id or album name. Adding the same Song (or a Song with the same title) twice is allowed and results in two items.

#### Scenario: Adding an imported song
- **WHEN** a Song titled "Wipe Out" lasting 2:40 is added to a Setlist
- **THEN** the Setlist has a new last item of type Song with name "Wipe Out" and duration 2:40

#### Scenario: Same song twice
- **WHEN** the same Song is added twice
- **THEN** the Setlist contains two separate items for it

### Requirement: Adding A Break
`AddBreak(setlist, name, duration)` SHALL reject a null setlist with `ArgumentNullException`, reject a null/empty/whitespace name with `ArgumentException` ("Break name is required."), and reject a duration of zero or less with `ArgumentOutOfRangeException` ("Duration must be greater than zero."). Otherwise it SHALL append a Setlist Item of type `Break` with the trimmed name and the given duration.

#### Scenario: Valid break
- **WHEN** a break named " Pause " of 10 minutes is added
- **THEN** a last item of type Break with name "Pause" and duration 10:00 is appended

#### Scenario: Invalid break
- **WHEN** a break is added with a whitespace name, or with a duration of zero
- **THEN** `ArgumentException` respectively `ArgumentOutOfRangeException` is thrown and the Setlist is unchanged

### Requirement: Total Duration
`CalculateTotalDuration(setlist)` SHALL reject a null setlist with `ArgumentNullException` and otherwise return the sum of the durations of all Setlist Items, Songs and Breaks alike (zero for an empty Setlist).

#### Scenario: Songs and breaks are summed
- **WHEN** a Setlist has a 3:00 song and a 15:00 break
- **THEN** the total duration is 18:00
