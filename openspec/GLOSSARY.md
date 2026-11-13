# Ubiquitous Language Glossary

This glossary defines the shared vocabulary (Domain-Driven Design "ubiquitous
language") for SetlistCreator: the domain of building, saving and printing
setlists for a band's live performances. Use these terms consistently in
conversation, OpenSpec artifacts, and code/UI naming where practical.

Terms are grouped by area. Cross-references to other terms in this glossary
are written in **bold**. The glossary describes the vocabulary as the code uses
it today; planned concepts are marked as such.

## Setlists

### Setlist
An ordered list of **Setlist Item**s for one musical performance, plus
**Gig Info**. Identified by a `Guid`. The same concept exists in two states:
the *working setlist* that the **Setlist Builder** edits in memory, and a
**Saved Setlist** stored in the database. Code: `Setlist`.

### Setlist Item
One entry on a **Setlist**: either a **Song** entry or a **Break** entry,
distinguished by its Setlist Item Type (`Song` or `Break`). A Setlist Item has
only a name and a duration; it is a *snapshot* copy and keeps no link back to
the **Song** or **Album** it came from. Code: `SetlistItem`, `SetlistItemType`.

### Song
A titled piece of music with a duration, optionally tagged with the name of the
**Album** it belongs to. Songs come from an **Imported Album** or are created as
a **Manual Song**. Code: `Song`.

### Break
A named pause on a **Setlist** (default name "Break", default length one minute
in the builder). Has a name and a duration but no **Song**. Code:
`SetlistItem.FromBreak`, `SetlistItemType.Break`.

### Manual Song
A **Song** the user types in (title and duration) instead of importing it from a
**Music Source**. It has no **Album**. Today it only exists as a **Setlist
Item** inside the setlist it was added to; it is not stored anywhere else (known
gap, issue #7). Code: `ISetlistService.CreateManualSong`. *Planned (issue #7):*
a persisted pool of manual songs in a dummy album called "Setlist Creator Manual
Songs".

### Gig Info
The two descriptive attributes of a **Setlist**: **Venue Name** and **Gig
Date**. Edited in the **Setlist Builder** and on the **Manage Setlists** page.

### Venue Name
Free-text name of the place where the **Setlist** is played. Never null (empty
string when unset). Trimmed by the LiteDB-backed service on save and on update, and by the
in-memory service on update only. Used as the heading on the **Print View**.

### Gig Date
The optional date (no time of day) of the performance. Code: `DateOnly?`.

### Total Duration
The sum of the durations of all **Setlist Item**s on a **Setlist**, songs and
**Break**s alike. Shown as `mm:ss` in the UI.

### Saved Setlist
A **Setlist** that has been written to the database. Listed on the **Manage
Setlists** page and reopened by id in the **Setlist Builder**.

### Autosave
The **Setlist Builder** saves the working setlist after every change that
modifies it, so there is no separate save button in normal use.

### Copy (of a Setlist)
A new **Saved Setlist** with a new id and the same **Gig Info** and **Setlist
Item**s as the original.

## Albums & Music Sources

### Album
A named collection of **Song**s, identified by a `Guid`. Code: `Album`.

### Imported Album
An **Album** fetched from a **Music Source** and stored in the app's database,
so its **Song**s can be picked in the **Setlist Builder**. Re-importing an album
with the same id replaces the stored one.

### Music Source
One of the four external services albums can be imported from: MusicBrainz,
Discogs, iTunes and Deezer. Each is a client implementing
`IMusicSearchService`. Discogs additionally needs an API token.

### Artist Result / Album Result
The lightweight search hits a **Music Source** returns before anything is
imported: an **Artist Result** (id, name, optional disambiguation) and an
**Album Result** (source-specific id, title, optional release year). Code:
`ArtistResult`, `AlbumResult`.

### Default Duration
Three minutes. A **Music Source** substitutes it for any track whose duration is
missing, zero or unparseable. Exception: a Discogs track with the literal duration
`0:00` parses successfully and keeps a zero duration.

### Deterministic Album Id
The `Guid` an **Imported Album** gets derived from the source's own numeric id
(Discogs, iTunes, Deezer) or taken from the MusicBrainz release id, so importing
the same release twice replaces instead of duplicating it. **Song** ids are
random on every import.

### Tracklist Preview
The expandable list of an **Album Result**'s tracks on the Import Albums page,
fetched on demand and cached for the current artist selection.

## UI

### Setlist Builder
The home page (`/`): pick **Song**s from **Imported Album**s, add **Break**s and
**Manual Song**s, reorder items, edit **Gig Info**, and see the **Total
Duration**. Code: `Home.razor`.

### Manage Setlists
The page at `/setlists` listing all **Saved Setlist**s with sortable columns and
actions: save **Gig Info**, open in builder, **Copy**, delete.

### Import Albums
The page at `/import` for searching a **Music Source**, previewing a
**Tracklist Preview**, importing and removing **Imported Album**s.

### Print View
The page at `/print/{id}` that shows one **Saved Setlist** in a one-page A4
layout for use on stage. Its font size is fitted by script so all items fill the
page. Code: `PrintSetlist.razor`, `window.setlistPrint` in `app.js`.

### Duplicate Song Marker
In the **Setlist Builder**, a **Song** whose title already appears on the working
**Setlist** is greyed out in the album and search lists. It can still be added
again.
