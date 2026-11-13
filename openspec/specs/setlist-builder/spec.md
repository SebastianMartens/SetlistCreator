# setlist-builder Specification

## Purpose

The Setlist Builder is the app's home page (`Components/Pages/Home.razor`, route `/`): a person assembles a Setlist from Imported Album songs, Breaks and Manual Songs, orders it, edits Gig Info and sees the Total Duration. Every modification is autosaved to the Saved Setlists. Backend rules for adding items are in `setlist-composition`; storage is in `setlist-persistence`.

## Requirements

### Requirement: Builder Page And Working Setlist
The Web app SHALL expose the builder at `/` (Interactive Server render mode, page title "Setlist Creator", heading "Setlist Creator"). Opening it without a `setlistId` query parameter SHALL start a new working Setlist (new id, no items, empty venue, no date) that is not stored until the first autosave. The page SHALL have a left column ("Album songs", "Break", "Manual song" forms) and a right column ("Current setlist").

#### Scenario: Fresh builder
- **WHEN** a person opens `/`
- **THEN** the right column shows empty "Venue name" and "Gig date" fields, "No items added yet." and a total duration of 00:00, and nothing has been written to the database yet

### Requirement: Opening A Saved Setlist
The builder SHALL accept an optional `setlistId` (GUID) query parameter. When present and different from the already loaded id, it SHALL load that Saved Setlist as the working Setlist and show "Saved setlist opened in builder.". If no Saved Setlist has that id it SHALL show the error "Unable to load setlist. It may have been deleted or does not exist." and keep the empty working Setlist; any other load failure SHALL show the exception message.

#### Scenario: Opening from Manage Setlists
- **WHEN** a person selects "Open in builder" for a Saved Setlist
- **THEN** the builder shows that setlist's venue, date and items and the success message

#### Scenario: Unknown id
- **WHEN** a person opens `/?setlistId=<GUID of a deleted setlist>`
- **THEN** the error "Unable to load setlist. It may have been deleted or does not exist." is shown

### Requirement: Adding Songs From An Album
When no search text is entered, the left column SHALL show an "Album" drop-down listing all Imported Albums (in library order) with the first album preselected, and a "Song" drop-down listing the selected album's songs with their durations (`Title (mm:ss)`) with the first song preselected. Changing the album SHALL reload the song list. "Add selected song" SHALL append the selected Song to the working Setlist and autosave. If no song is selected (for example no albums are imported) it SHALL show the error "Please select a song to add.".

#### Scenario: Add selected song
- **WHEN** a person selects an album and a song and chooses "Add selected song"
- **THEN** a Song item with that title and duration is appended to the working Setlist and the Setlist is saved

#### Scenario: No albums imported
- **WHEN** the album library is empty and a person chooses "Add selected song"
- **THEN** the error "Please select a song to add." is shown and the Setlist is unchanged

### Requirement: Searching Songs Across Albums
A "Search songs" text box SHALL filter songs across all Imported Albums as the person types. When the text is not empty/whitespace the album and song drop-downs SHALL be hidden and the page SHALL list up to 25 songs whose title contains the text (case-insensitive), each shown as title, " — album name (mm:ss)" with an "Add" button; if none match it SHALL show "No songs found.". Adding from the results SHALL append the song, autosave, clear the search text and move focus back to the search box.

#### Scenario: Search hit
- **WHEN** a person types "wipe" and an imported song is titled "Wipe Out"
- **THEN** the song is listed with its album name and duration, and "Add" appends it to the Setlist

#### Scenario: More than 25 matches
- **WHEN** more than 25 songs match the search text
- **THEN** only the first 25 (in album, then song order) are listed

### Requirement: Duplicate Song Marker
Songs (in the song drop-down and the search results) whose title equals (case-sensitive, exact) the name of a Song item already on the working Setlist SHALL be shown greyed out. Greyed-out songs SHALL remain selectable and addable.

#### Scenario: Song already on the setlist
- **WHEN** "Wipe Out" is already on the working Setlist and appears again in the search results
- **THEN** its row is greyed out but its "Add" button still works and adds a second item

### Requirement: Adding Breaks
The "Break" form SHALL have "Break name" (default "Break") and "Duration (mm:ss)" (default "1:00") inputs and an "Add break" button that appends a Break item and autosaves. Failures (invalid duration text or the backend rules in `setlist-composition`) SHALL be shown as an error alert with the exception's message.

#### Scenario: Default break
- **WHEN** a person chooses "Add break" without editing the defaults
- **THEN** a Break item named "Break" lasting 01:00 is appended

#### Scenario: Invalid duration text
- **WHEN** a person enters "abc" as the break duration and chooses "Add break"
- **THEN** the error "Enter duration as mm:ss (e.g. 3:30)." is shown and nothing is added

### Requirement: Duration Input Format
Duration text for breaks and manual songs SHALL be parsed as `m:ss` / `mm:ss` (seconds 0 to 59) or as a single positive integer meaning whole minutes. Any other text, including seconds of 60 or more, a lone "0", or more than one colon, SHALL fail with the message "Enter duration as mm:ss (e.g. 3:30).". A parsed duration of zero or less (for example "0:00") SHALL be rejected by the backend with "Duration must be greater than zero. (Parameter 'duration')".

#### Scenario: Minutes only
- **WHEN** the duration text is "5"
- **THEN** the duration is 5 minutes

#### Scenario: Seconds out of range
- **WHEN** the duration text is "3:75"
- **THEN** the format error message is shown

### Requirement: Adding Manual Songs From The Builder
The "Manual song" form SHALL have "Song title" (empty) and "Duration (mm:ss)" (default "4:00") inputs and an "Add manual song" button that creates a Manual Song (see `manual-songs`), appends it to the working Setlist as a Song item, clears the title box (the duration text stays) and autosaves. Failures SHALL be shown as an error alert with the exception's message.

#### Scenario: Add a manual song
- **WHEN** a person enters "Home Grown" with duration "3:30" and chooses "Add manual song"
- **THEN** a Song item "Home Grown" lasting 03:30 is appended, the title box is cleared and the Setlist is saved

#### Scenario: Missing title
- **WHEN** the title is blank and "Add manual song" is chosen
- **THEN** the error "Title is required. (Parameter 'title')" is shown

### Requirement: Current Setlist Display
The right column SHALL list the working Setlist's items in order. Song items SHALL show a running number counting only Song items ("1.", "2.", ...), the name and the duration as `mm:ss` (minutes are not capped at 59). Break items SHALL be shown as a divider line with "name (mm:ss)" and no number. The page SHALL show "Total duration (songs + breaks): mm:ss". Each item SHALL have a remove (✕) button that removes it and autosaves.

#### Scenario: Numbering skips breaks
- **WHEN** the Setlist is Song A, Break, Song B
- **THEN** Song A is numbered "1.", the break has no number, and Song B is numbered "2."

#### Scenario: Removing an item
- **WHEN** a person selects ✕ on an item
- **THEN** it is removed from the Setlist, the numbering and total are updated and the Setlist is saved

### Requirement: Reordering By Drag And Drop
Each item row SHALL be draggable. Dropping a dragged item on another row SHALL move it to the position directly before the row it was dropped on and autosave. Dropping an item on itself SHALL have no effect. Dropping after the last item is not possible (an item can only be placed before another row).

#### Scenario: Moving an item down
- **WHEN** items are A, B, C, D and A is dropped on C
- **THEN** the order becomes B, A, C, D

#### Scenario: Moving an item up
- **WHEN** items are A, B, C, D and D is dropped on B
- **THEN** the order becomes A, D, B, C

### Requirement: Editing Gig Info In The Builder
The right column SHALL show a "Venue name" text box and a "Gig date" date input bound to the working Setlist; a committed change of either (on change, i.e. when the field loses focus or a date is picked) SHALL autosave.

#### Scenario: Entering a venue
- **WHEN** a person types a venue name and leaves the field
- **THEN** the Setlist (even if it has no items yet) is saved with that venue name

### Requirement: Autosave
The builder SHALL save the working Setlist through `ISetlistService.SaveSetlist` after adding a song, break or manual song, after removing or reordering an item, and after a committed Gig Info change. A save failure SHALL be shown as an error alert with the exception's message. There is no explicit "Save" button in the UI.

#### Scenario: First change creates the saved setlist
- **WHEN** a person adds the first item to a fresh working Setlist
- **THEN** a Saved Setlist with the working Setlist's id now exists and appears on the Manage Setlists page

### Requirement: Opening The Print View
The "Print view" button SHALL be disabled (tooltip "Add at least one item to print.") while the working Setlist has no items. When enabled (tooltip "Save and open print view") it SHALL save the Setlist and navigate to `/print/{id}`; a failure SHALL be shown as an error alert.

#### Scenario: Printing an empty setlist
- **WHEN** the working Setlist has no items
- **THEN** the "Print view" button is disabled

#### Scenario: Printing
- **WHEN** a person selects "Print view" on a Setlist with items
- **THEN** the Setlist is saved and the browser navigates to `/print/{setlist id}`
