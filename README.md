# SetlistCreator

Tool to select songs for my band setlist.

## Projects

- `src/SetlistCreator.Backend`: C# domain and setlist management service
- `src/SetlistCreator.Web`: Blazor frontend for creating setlists
- `tests/SetlistCreator.Backend.Tests`: Unit tests for backend behavior

Setlists are persisted with LiteDB and can be managed from the `/setlists` page (update venue/date, copy, delete).

## Configuration

- `Discogs:Token`: Optional Discogs API token. In Azure App Service, set it as `Discogs__Token`.
- `Setlist:DatabasePath`: Optional LiteDB file path. Leave it empty locally to use the default app path. In Azure App Service, set it to the mounted persistent storage path such as `/home/data/setlists.db`.

## Azure

Use [scripts/azure-appservice-linux.ps1](scripts/azure-appservice-linux.ps1) as the starting point for Linux App Service provisioning and deployment.

Before deploying to Azure, confirm the target App Service runtime supports the current framework in `src/SetlistCreator.Web/SetlistCreator.Web.csproj`.
