# app-navigation Specification

## Purpose

The shared layout of the Web app: the navigation menu, the page shell, and the not-found and error pages (`Components/Layout/*`, `Components/Routes.razor`, `Components/Pages/NotFound.razor`, `Components/Pages/Error.razor`).

## Requirements

### Requirement: Navigation Menu
Every page SHALL be rendered inside `MainLayout` with a sidebar containing a top row with the brand "Setlist Creator" (with a note icon) linking to the site root, and a navigation list with three entries: "Setlist Builder" (`/`, highlighted only on an exact match), "Manage Setlists" (`/setlists`) and "Import Albums" (`/import`). The entry of the current page SHALL be highlighted. On narrow screens the menu SHALL be collapsible through a toggler, and selecting an entry SHALL close it.

#### Scenario: Reaching each page
- **WHEN** a person selects "Manage Setlists" in the menu
- **THEN** the app navigates to `/setlists` and that entry is highlighted

### Requirement: Print Page Has No Dedicated Menu Entry
The Print View (`/print/{id}`) SHALL be reachable only from the Setlist Builder's "Print view" button (and by direct URL); it is not listed in the menu.

#### Scenario: Menu entries
- **WHEN** a person looks at the navigation menu
- **THEN** there is no entry for printing

### Requirement: Not Found Page
Unknown routes SHALL respond with HTTP 404 and render the page `/not-found` in the main layout with the heading "Not Found" and the text "Sorry, the content you are looking for does not exist.".

#### Scenario: Unknown URL
- **WHEN** a person opens `/does-not-exist`
- **THEN** the response status is 404 and the Not Found page is shown

### Requirement: Error Page
The page `/Error` SHALL show "Error." and "An error occurred while processing your request.", the request id when one is available, and a notice about the Development environment. It is the exception handler target outside the Development environment.

#### Scenario: Production exception
- **WHEN** an unhandled exception occurs on a request in the Production environment
- **THEN** the request is re-executed at `/Error` and the error page is shown

### Requirement: Blazor Error UI And Reconnect
The layout SHALL include the standard Blazor error banner ("An unhandled error has occurred." with "Reload" and dismiss controls) and the app SHALL include the standard Blazor Server reconnect modal (`ReconnectModal`) shown when the server circuit connection is lost.

#### Scenario: Unhandled client-side error
- **WHEN** an unhandled exception occurs in a component
- **THEN** the yellow banner "An unhandled error has occurred." appears with a "Reload" link

### Requirement: UI Language
All user-visible text in the app SHALL be English. Dates in the print header follow the server's current culture.

#### Scenario: Language
- **WHEN** any page is displayed
- **THEN** labels, buttons and messages are in English
