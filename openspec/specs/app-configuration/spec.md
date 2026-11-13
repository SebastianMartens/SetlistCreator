# app-configuration Specification

## Purpose

The configuration keys and service wiring of the Web app (`src/SetlistCreator.Web/Program.cs`, `appsettings*.json`): which settings exist, what they do and how the app starts.

## Requirements

### Requirement: Setlist Database Path Setting
The app SHALL read the configuration key `Setlist:DatabasePath` and pass it to `AddSetlistServices`. When the key is absent (null) the LiteDB file defaults to `setlists.db` in the application's base directory. In Azure App Service the value is supplied through the environment variable `Setlist__DatabasePath`. A key that is present but empty is passed through as an empty string and makes the persistence layer fail on first use (see `setlist-persistence`).

#### Scenario: Key set
- **WHEN** `Setlist:DatabasePath` is `/home/data/setlists.db`
- **THEN** all Saved Setlists and Imported Albums are stored in that file

#### Scenario: Key present but empty
- **WHEN** `Setlist:DatabasePath` is an empty string (as in the checked-in base `appsettings.json`)
- **THEN** pages that resolve `ISetlistService` fail with `ArgumentException` "The path is empty." and respond with HTTP 500 (observed when running the Development environment without overriding the key, e.g. via the environment variable `Setlist__DatabasePath`)

### Requirement: Discogs Token Setting
The app SHALL read `Discogs:Token`. When the value is not blank the Discogs HTTP client SHALL send the header `Authorization: Discogs token=<value>` on every request, and the Import Albums page treats Discogs as configured; otherwise no header is sent and Discogs is treated as not configured (see `album-import`). In Azure App Service the value is supplied through `Discogs__Token`.

#### Scenario: Token present
- **WHEN** `Discogs:Token` is a non-blank string
- **THEN** Discogs requests carry the `Authorization` header and the Discogs search box is available

#### Scenario: Token absent
- **WHEN** `Discogs:Token` is missing or blank
- **THEN** the Discogs source is reported as not configured

### Requirement: Music Service HTTP Clients
The app SHALL register typed `HttpClient`s for `MusicBrainzService` (`https://musicbrainz.org/ws/2/`), `DiscogsService` (`https://api.discogs.com/`), `ITunesService` (`https://itunes.apple.com/`) and `DeezerService` (`https://api.deezer.com/`), each with the SetlistCreator User-Agent. The services SHALL be injected into the Import Albums page by their concrete types.

#### Scenario: Client base address
- **WHEN** the Deezer source requests `search/artist?...`
- **THEN** the request goes to `https://api.deezer.com/search/artist?...`

### Requirement: Web Host Pipeline
The app SHALL be a Blazor Server application (Razor components with interactive server components). Outside the Development environment unhandled exceptions SHALL be handled by re-executing `/Error`; status-code responses (such as 404) SHALL re-execute `/not-found`; anti-forgery and static asset mapping SHALL be enabled. The default development URL SHALL be `http://localhost:5218` (launch profile `http`, environment `Development`).

#### Scenario: Local launch
- **WHEN** the project is started with `dotnet run` using the `http` launch profile
- **THEN** the app listens on `http://localhost:5218` in the Development environment and opens a browser

### Requirement: Checked-In Default Settings
The checked-in `appsettings.json` SHALL contain `Discogs:Token` (a non-blank value that the commit history calls a dummy token), `Setlist:DatabasePath` set to an empty string, default log level Information with `Microsoft.AspNetCore` at Warning, and `AllowedHosts` of `*`. `appsettings.Development.json` SHALL only set the same logging levels. Because the checked-in token is non-blank, Discogs is reported as configured unless the value is overridden (for example blank) by the environment.

#### Scenario: Default Discogs state
- **WHEN** the app runs with the checked-in settings and no override of `Discogs:Token`
- **THEN** Discogs requests are sent with the placeholder token (whether Discogs accepts that token is outside the app's control), and the "token not configured" alert is not shown

#### Scenario: Default logging
- **WHEN** the app starts in any environment
- **THEN** ASP.NET Core framework logs below Warning are suppressed
