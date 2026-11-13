# setlist-persistence Specification

## Purpose

How Setlists are stored, read, copied, updated and deleted. The production implementation is `LiteDbSetlistService` (LiteDB 5 file database, collection `setlists`); `InMemorySetlistService` provides the same operations over a list; `LiteDbSetlistService` holds a private instance of it and delegates the non-persistent operations to it (composition; both classes are sealed). Dependency injection is set up by `AddSetlistServices(databasePath)` in `ServiceCollectionExtensions`. Album persistence is in `album-library`; Manual Songs are not persisted (see `manual-songs`).

## Requirements

### Requirement: Service Registration
`AddSetlistServices(services, databasePath = null)` SHALL register `ISetlistService` as a scoped service that creates `new LiteDbSetlistService(databasePath)`. In the Blazor Server app each user circuit (and each prerender request) therefore gets its own service instance.

#### Scenario: One service instance per scope
- **WHEN** two different DI scopes resolve `ISetlistService`
- **THEN** they receive different `LiteDbSetlistService` instances that read and write the same database file

### Requirement: Database File
`LiteDbSetlistService` SHALL use the given `databasePath`, or `setlists.db` in `AppContext.BaseDirectory` when the path is null. It SHALL create the file's parent directory when missing and SHALL open the database in LiteDB shared-connection mode, opening and disposing a connection for each operation. An empty (non-null) path is not treated as "unset" and makes construction fail with `ArgumentException` ("The path is empty.").

Note: current behavior, known defect; changing it requires a spec change.

#### Scenario: Default location
- **WHEN** the service is created with a null path
- **THEN** the database file is `setlists.db` next to the application binaries

#### Scenario: Directory is created
- **WHEN** the path points into a directory that does not exist
- **THEN** the directory is created before the database is opened

### Requirement: Saving A Setlist
`SaveSetlist(setlist)` SHALL reject null with `ArgumentNullException`, upsert the Setlist by id (replacing any stored Setlist with that id including all its items), store the venue name trimmed (null becomes empty), store the Gig Date as a date at midnight, store each item's type, name and duration (as ticks), and return the Setlist's id.

#### Scenario: Round trip
- **WHEN** a Setlist with venue "Grand Hall", date 2026-06-01, one Song and one 10-minute Break "Pause" is saved and read back
- **THEN** the venue, date, two items (types, names, durations, order) are identical

#### Scenario: Saving twice
- **WHEN** a Setlist is saved, items are removed in memory, and it is saved again
- **THEN** exactly one Saved Setlist with that id exists and it has the reduced item list

#### Scenario: Venue is trimmed on save
- **WHEN** a Setlist with venue "  Club  " is saved
- **THEN** reading it back yields venue "Club"

### Requirement: Reading Saved Setlists
`GetSavedSetlists()` SHALL return all stored Setlists (order is the database's id order, not guaranteed to be creation order). `GetSavedSetlist(id)` SHALL return the stored Setlist or throw `KeyNotFoundException` with message "No setlist with id '<id>' was found.". Returned Setlists are new objects; changing them does not change the stored data until saved.

#### Scenario: Missing setlist
- **WHEN** `GetSavedSetlist` is called with an unknown id
- **THEN** `KeyNotFoundException` is thrown

### Requirement: Copying A Saved Setlist
`CopySavedSetlist(id)` SHALL insert a new stored Setlist with a new random id and the same venue name, Gig Date and items (same order) as the source, and return it. The copy SHALL NOT receive a modified name. An unknown id SHALL throw `KeyNotFoundException`.

#### Scenario: Copy
- **WHEN** a Saved Setlist "Arena" with two items is copied
- **THEN** there are two Saved Setlists, the copy has a different id, venue "Arena" and two items

### Requirement: Updating Gig Info
`UpdateGigInfo(id, venueName, gigDate)` SHALL load the stored Setlist (unknown id throws `KeyNotFoundException`), set the venue name trimmed (null becomes empty) and the Gig Date (null clears it), and update the stored document. Items are unchanged.

#### Scenario: Clearing the date
- **WHEN** `UpdateGigInfo(id, "  Z ", null)` is called
- **THEN** the stored venue is "Z" and the Gig Date is empty

### Requirement: Deleting A Saved Setlist
`DeleteSavedSetlist(id)` SHALL remove the stored Setlist with that id and SHALL NOT fail when the id does not exist.

#### Scenario: Delete unknown id
- **WHEN** `DeleteSavedSetlist` is called with an id that is not stored
- **THEN** no exception is thrown and nothing changes

### Requirement: Non-Persistent Operations Mutate Only The Passed Setlist
`AddSong`, `AddBreak`, `CalculateTotalDuration` and `CreateManualSong` on `LiteDbSetlistService` SHALL delegate to the in-memory implementation and SHALL NOT write to the database; a Setlist changed by `AddSong`/`AddBreak` is stored only when `SaveSetlist` is called.

#### Scenario: Adding without saving
- **WHEN** an item is added to a Setlist through `AddSong` but `SaveSetlist` is not called
- **THEN** reading the Setlist back from the database (if it was saved earlier) does not contain the new item

### Requirement: In-Memory Implementation Semantics
`InMemorySetlistService` SHALL keep Saved Setlists in a list and return deep copies from `GetSavedSetlists`/`GetSavedSetlist`/`CopySavedSetlist`; it SHALL NOT trim the venue name on `SaveSetlist` (only `UpdateGigInfo` trims). It is not registered in the app's DI container (used only as the private delegate inside `LiteDbSetlistService` and directly in tests).

#### Scenario: Copy isolation
- **WHEN** a Setlist returned by `InMemorySetlistService.GetSavedSetlist` is modified
- **THEN** the stored copy is unchanged
