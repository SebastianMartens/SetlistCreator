# setlist-printing Specification

## Purpose

The Print View (`Components/Pages/PrintSetlist.razor`, `PrintSetlist.razor.css`, `wwwroot/app.js`, `MainLayout.razor.css`): shows one Saved Setlist as a one-page A4 portrait sheet with the font size fitted so the items fill the page, to be read on a stage floor.

## Requirements

### Requirement: Print Page Route
The Web app SHALL expose the Print View at `/print/{SetlistId}` where the id is a GUID (Interactive Server render mode). It SHALL load the Saved Setlist with that id. When none exists (or loading throws for any reason) it SHALL show "Setlist not found." with a link "Back to builder" to `/`.

#### Scenario: Unknown setlist
- **WHEN** a person opens `/print/<GUID with no Saved Setlist>`
- **THEN** the page shows "Setlist not found." and a "Back to builder" link; the HTTP status is still 200

#### Scenario: Non-GUID id
- **WHEN** a person opens `/print/abc`
- **THEN** the route does not match and the app's Not Found page is shown

### Requirement: Page Title
The browser title SHALL be "{venue name} — Print", or "Setlist — Print" when the venue name is empty or the Setlist is not found.

#### Scenario: Title with venue
- **WHEN** the Setlist's venue is "Grand Hall"
- **THEN** the page title is "Grand Hall — Print"

### Requirement: Print Content
The sheet SHALL show a header with the venue name (or "Setlist" when empty) and a details line consisting of the Gig Date formatted as `d MMMM yyyy` in the server's current culture (only when a date is set) and "Total: mm:ss", joined with " | ". Below it SHALL list every Setlist Item in order: Songs as "{n}. {name}" with the duration at the right (`mm:ss`, numbering counting Songs only), and Breaks as a divider with "name (mm:ss)" and no number.

#### Scenario: Header details
- **WHEN** a Setlist with venue "Grand Hall", date 2026-06-01 and 31:00 total duration is shown
- **THEN** the header reads "Grand Hall" and the details line reads "1 June 2026 | Total: 31:00" (month name in the server culture)

#### Scenario: No date
- **WHEN** the Setlist has no Gig Date
- **THEN** the details line is only "Total: mm:ss"

### Requirement: Print Controls
On screen the page SHALL show a "Print" button and a "Back to editor" link to `/?setlistId={id}`. These controls SHALL be hidden when printing. The "Print" button SHALL call the JavaScript function `window.setlistPrint(itemCount)`, which registers before/after-print handlers and opens the browser's print dialog.

#### Scenario: Back to editor
- **WHEN** a person selects "Back to editor"
- **THEN** the builder opens with this Setlist loaded

### Requirement: One-Page A4 Print Layout
Printing SHALL use A4 portrait with 15 mm page margins. The app navigation (sidebar and top row) SHALL be hidden when printing and the content padding removed. The print page container SHALL be 267 mm high and the list of items SHALL fill it, with each item row sharing the height equally and separated by a thin grey line. The header (venue and details) SHALL be hidden on screen and shown when printing.

#### Scenario: Printing hides navigation
- **WHEN** the page is printed
- **THEN** the sidebar, the top row and the Print/Back controls are not part of the output

### Requirement: Automatic Font Fitting
Before printing, `setlistPrint` SHALL add the class `setlist-print-sizing` to `body`, compute an initial font size from the list's height divided by the item count (item height / 1.1 line height, minus 2 px, limited to 10 to 100 px), and then, while that size is greater than 10 px, apply it as the list's inline font size and, if the content still overflows, reduce it by 1 px. The loop runs only while the size is above 10, and the size is applied only inside the loop, so the smallest size ever applied is 11 px; if the initial estimate is already 10 px, no inline font size is set at all and the list keeps its previous (CSS) font size. After printing it SHALL remove the class and restore the list's previous font size. With zero items the font size is not adjusted.

#### Scenario: Few items
- **WHEN** a Setlist with 5 items is printed
- **THEN** the font size is large (up to 100 px) so the items fill the page height

#### Scenario: Many items
- **WHEN** a Setlist has more items than fit even at the smallest applied size
- **THEN** the last inline font size applied is 11 px (or none when the initial estimate is already 10 px) and the overflowing items are clipped (the list has `overflow: hidden` when printing)

### Requirement: Screen Preview
On screen (not printing) the item list SHALL have a smaller font (0.9rem) and a maximum height of 45 % of the viewport, scrolling when longer.

#### Scenario: Long setlist on screen
- **WHEN** a Setlist with many items is shown on screen
- **THEN** the list scrolls inside a box of at most 45 % of the viewport height
