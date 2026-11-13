# setlist-management Specification

## Purpose

The Manage Setlists page (`Components/Pages/Setlists.razor`, route `/setlists`) lists all Saved Setlists and lets a person sort them, edit their Gig Info, open them in the Setlist Builder, copy them and delete them.

## Requirements

### Requirement: Saved Setlist Table
The Web app SHALL expose the page at `/setlists` (Interactive Server render mode, title and heading "Manage Setlists"). It SHALL load all Saved Setlists when the page opens and show them in a table with columns "Date", "Venue name", "Items" (item count, songs and breaks), "Total duration" (`mm:ss`, minutes not capped at 59) and "Actions". When there are none it SHALL show "No saved setlists yet.".

#### Scenario: Setlists exist
- **WHEN** two Saved Setlists exist
- **THEN** the table shows two rows with editable date and venue fields, the item count and total duration

#### Scenario: No setlists
- **WHEN** no Saved Setlist exists
- **THEN** the page shows "No saved setlists yet." instead of a table

### Requirement: Sorting
Clicking the "Date", "Venue name", "Items" or "Total duration" column header SHALL sort the rows by that column ascending; clicking the active header again SHALL toggle between ascending and descending. The active header SHALL show ▲ (ascending) or ▼ (descending). The initial sort SHALL be "Date" ascending. For the date column, Setlists without a Gig Date SHALL always be listed after those with a date, in both directions. Sorting happens in the page only; the stored data is unchanged.

#### Scenario: Default order
- **WHEN** the page opens with dated and undated Setlists
- **THEN** dated Setlists appear first in ascending date order, followed by undated ones

#### Scenario: Toggling direction
- **WHEN** a person clicks the "Venue name" header twice
- **THEN** the rows are sorted by venue name ascending, then descending, and the header indicator changes from ▲ to ▼

### Requirement: Editing Gig Info
Each row SHALL have a date input and a venue text box prefilled with the stored values. Editing them SHALL change only page state until "Save details" is selected, which SHALL call `UpdateGigInfo` for that Setlist, reload the list from storage and show "Setlist details updated.". An empty or unparseable date SHALL be treated as no date. Reloading SHALL discard unsaved edits in all other rows (current behavior, known defect; changing it requires a spec change).

#### Scenario: Saving details
- **WHEN** a person changes the venue of one row and selects "Save details"
- **THEN** the Setlist's venue is updated in storage and "Setlist details updated." is shown

#### Scenario: Clearing the date
- **WHEN** a person clears the date input and selects "Save details"
- **THEN** the Setlist's Gig Date becomes empty and it sorts among the undated Setlists

#### Scenario: Unsaved edits in another row
- **WHEN** a person edits row A and row B, and saves row A
- **THEN** row B's unsaved edit is discarded by the reload

### Requirement: Opening In The Builder
"Open in builder" SHALL navigate to `/?setlistId={id}`.

#### Scenario: Open
- **WHEN** a person selects "Open in builder" on a row
- **THEN** the browser navigates to `/?setlistId=<that setlist's id>` and the builder loads it

### Requirement: Copying
"Copy" SHALL call `CopySavedSetlist`, reload the list and show "Setlist copied.". The copy has the same venue, date and items under a new id.

#### Scenario: Copy a setlist
- **WHEN** a person selects "Copy" on a row
- **THEN** a new row with the same venue, date, item count and duration appears and "Setlist copied." is shown

### Requirement: Deleting
"Delete" SHALL call `DeleteSavedSetlist` immediately, with no confirmation step, reload the list and show "Setlist deleted.".

#### Scenario: Delete a setlist
- **WHEN** a person selects "Delete" on a row
- **THEN** the row disappears and "Setlist deleted." is shown

### Requirement: Error Reporting
When saving details, copying or deleting throws, the page SHALL show the exception's message in an error alert. Starting any of these three actions SHALL first clear both the success and error messages.

#### Scenario: Failure
- **WHEN** copying a Setlist that was deleted from another browser tab
- **THEN** the error alert shows "No setlist with id '<id>' was found."
