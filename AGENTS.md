# SetlistCreator

.NET tool for building and printing band setlists. Songs and durations are imported from
music services (MusicBrainz, Discogs, iTunes, Deezer); setlists are saved in an embedded
LiteDB file; the print view produces a one-page A4 sheet. Hosted on Azure App Service.
See `README.md` for the feature overview and screenshots.

## Commands

The solution is `SetlistCreator.slnx` (not a `.sln`), .NET 10 SDK required.

```powershell
# Build everything (Backend, Web, Backend.Tests)
dotnet build SetlistCreator.slnx

# Run all tests
dotnet test SetlistCreator.slnx

# Run a single test (by name)
dotnet test tests/SetlistCreator.Backend.Tests --filter "FullyQualifiedName~CreateManualSong"

# Run the web app - http://localhost:5218 (launch profile "http", Development)
dotnet run --project src/SetlistCreator.Web
```

Local run gotcha: the checked-in `src/SetlistCreator.Web/appsettings.json` sets
`Setlist:DatabasePath` to an empty string, which makes every page that needs the database fail
with "The path is empty." (HTTP 500). Until that is fixed, override it when running locally, e.g.
`$env:Setlist__DatabasePath = "$env:TEMP\setlists.db"` (the file is created on first use). Keep the file outside
the repo: `*.db` is not git-ignored, so a database inside the working tree could be committed by accident.

`.vscode/` has `build`, `publish` and `watch` tasks and a "`.NET Core Launch (web)`" launch config.
If `dotnet build` fails with a locked `SetlistCreator.Web.exe`, a running instance of the app is
still holding it - stop it first.

### Known test state

`dotnet test SetlistCreator.slnx` currently reports 6 failing and 2 passing tests (baseline when
OpenSpec was introduced, issue #9). The failing tests in `UnitTest1.cs` and
`LiteDbSetlistServiceTests.cs` call `GetAlbums().First()` on a service that starts with no albums
(no albums are seeded any more), so they throw "Sequence contains no elements". Fixing them is a
separate task; do not treat them as regressions of your change.

## Architecture

Two projects in one solution (`SetlistCreator.slnx`), plus one test project:

- **`src/SetlistCreator.Backend`** (class library) - domain and persistence, no web dependencies.
  Models: `Setlist` (mutable class), `SetlistItem` (record, type `Song` or `Break`), `Song`,
  `Album` (records). `ISetlistService` is the single service contract (albums, adding items,
  total duration, saved-setlist CRUD). `InMemorySetlistService` implements it over lists.
  `LiteDbSetlistService` (both classes are sealed) holds a private `InMemorySetlistService` and
  delegates the non-persistent operations to it (composition, not inheritance); it persists albums
  (collection `albums`) and saved setlists (collection `setlists`) in a LiteDB 5 file. DI entry point:
  `AddSetlistServices(databasePath)` registers `ISetlistService` as scoped (one instance per
  Blazor circuit).
- **`src/SetlistCreator.Web`** (Blazor Server, Interactive Server render mode) - UI and music
  service clients. Pages in `Components/Pages/`: `Home.razor` (setlist builder, `/`),
  `Setlists.razor` (manage saved setlists, `/setlists`), `ImportAlbums.razor` (`/import`),
  `PrintSetlist.razor` (`/print/{id}`, fitting script in `wwwroot/app.js`). `Services/` holds the
  four `IMusicSearchService` implementations (`MusicBrainzService`, `DiscogsService`,
  `ITunesService`, `DeezerService`), registered as typed `HttpClient`s in `Program.cs`.
- **`tests/SetlistCreator.Backend.Tests`** (xunit) - tests for the backend services only. There
  are no Web/UI tests.

Data flow: the Import Albums page fetches an `Album` from a music source and stores it through
`ISetlistService.ImportAlbum`; the builder offers those albums' songs, copies chosen songs into a
`Setlist` as `SetlistItem`s (name + duration snapshots) and autosaves via `SaveSetlist`. Manual
songs are NOT persisted as songs (known gap, issue #7).

Configuration (env var form for Azure in brackets): `Discogs:Token` [`Discogs__Token`] and
`Setlist:DatabasePath` [`Setlist__DatabasePath`]; default DB file is `setlists.db` next to the
binaries.

Hosting: Azure App Service on Linux (the scripts use runtime `DOTNETCORE|10.0` and plan SKU F1; the
resource names, location and runtime are mandatory parameters of the init and wrapper scripts and must be
passed explicitly, as must a non-empty Discogs token) with an Azure
Files share mounted at `/home/data` that holds the LiteDB file. Provisioning and deployment are
PowerShell scripts that use the Azure CLI: `scripts/azure-appservice-linux-init.ps1` (one-time),
`scripts/azure-appservice-linux-deploy.ps1` (publish zip + `az webapp deploy`) and
`scripts/azure-appservice-linux.ps1` (wrapper for both). The deploy script writes `publish/` and
`publish.zip` into the repo root; both are git-ignored - never commit them. Infra and deploy
scripts are changed only on request; agents never run them.

UI text is currently English (no German UI convention in this repo).

## Domain vocabulary & spec workflow

This repo uses **OpenSpec** for spec-driven changes (`openspec/` - `specs/`, `changes/`,
`GLOSSARY.md`, `config.yaml`). Use the `openspec-*` skills and `/opsx:*` commands
(`.claude/commands/opsx/`) to propose, apply and archive changes rather than editing specs by hand.
The specs in `openspec/specs/` are baseline specs describing the behavior as built when OpenSpec
was introduced (issue #9); new behavior changes them through an OpenSpec change. Capabilities:
`album-import`, `music-service-sources`, `album-library`, `setlist-composition`,
`setlist-builder`, `manual-songs`, `setlist-persistence`, `setlist-management`, `setlist-printing`,
`app-configuration`, `app-navigation`, `azure-hosting`. Validate with `openspec validate --all`.

`openspec/GLOSSARY.md` defines the project's ubiquitous language (Setlist, Setlist Item, Song,
Break, Manual Song, Album, Imported Album, Music Source, Gig Info, Saved Setlist, Print View, ...) -
read it before naming new concepts in code, UI or specs, and keep usage consistent with it.
