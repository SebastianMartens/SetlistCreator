# azure-hosting Specification

## Purpose

How the app is hosted on Azure App Service (Linux) and which scripts provision and deploy it (`scripts/azure-appservice-linux-init.ps1`, `scripts/azure-appservice-linux-deploy.ps1`, `scripts/azure-appservice-linux.ps1`). The scripts are PowerShell scripts that call the Azure CLI (`az`). This spec describes what the scripts do as written; they are not automated by CI.

## Requirements

### Requirement: One-Time Provisioning Script
`azure-appservice-linux-init.ps1` SHALL require the caller to pass explicit values for `-Subscription`, `-ResourceGroup`, `-Location`, `-PlanName`, `-AppName`, `-StorageAccount`, `-ShareName`, `-Runtime` and `-DiscogsToken`: all nine are declared `[Parameter(Mandatory = $true)]`, so the default values written in the script (for example resource group `setlistcreator-linux-rg`, location `westeurope`, runtime `DOTNETCORE|10.0`) are never used, PowerShell prompts for a missing mandatory value, and `-DiscogsToken` must be a non-empty string (the parameter has no `[AllowEmptyString()]`, so an empty value is rejected). Only `-MountPath` (default `/home/data`), `-DatabasePath` (default `/home/data/setlists.db`) and the `-SkipLogin` switch are optional.

Unless `-SkipLogin` is given, the script SHALL run `az login` for the configured tenant and select the subscription. It SHALL then create: a resource group, a StorageV2 / Standard_LRS storage account, an Azure Files share, a Linux App Service plan with SKU F1, and a web app on the plan with the given runtime. It SHALL mount the Azure Files share into the web app at the mount path (storage type AzureFiles, custom id `setlistdata`), set the app settings `ASPNETCORE_ENVIRONMENT=Production`, `Discogs__Token=<token parameter>` and `Setlist__DatabasePath=<database path>`, and enable filesystem application and web-server logging at Information level.

Note: current behavior, known defect (the unused defaults in the script suggest a different intent); changing it requires a spec change.

#### Scenario: Persistent database location
- **WHEN** provisioning has completed with the default `-MountPath` and `-DatabasePath`
- **THEN** the web app has the share mounted at `/home/data` and `Setlist__DatabasePath` points to `/home/data/setlists.db`, so the LiteDB file survives restarts and redeployments

#### Scenario: Discogs token supplied
- **WHEN** the script is run with a non-empty `-DiscogsToken` value
- **THEN** it is stored in the web app settings as `Discogs__Token`

#### Scenario: Missing or empty mandatory value
- **WHEN** the script is run without `-ResourceGroup`, or with `-DiscogsToken ""`
- **THEN** PowerShell prompts for the missing value respectively rejects the empty token, instead of falling back to the defaults written in the script

### Requirement: Deployment Script
`azure-appservice-linux-deploy.ps1` has optional parameters with working defaults (`Mandatory = $false`: subscription, resource group `setlistcreator-linux-rg`, app `setlistcreator-linux-app`, `.\publish`, `.\publish.zip`). It SHALL, unless `-SkipLogin` is given, log in and select the subscription; run `dotnet publish src/SetlistCreator.Web/SetlistCreator.Web.csproj -c Release -o .\publish`; delete any previous `publish.zip`; create `publish.zip` from the contents of the publish folder using `tar.exe` (failing if `tar.exe` is not available or the archive step fails); deploy it with `az webapp deploy --type zip`; restart the web app; and print the URL `https://<AppName>.azurewebsites.net`.

#### Scenario: Repeatable deployment
- **WHEN** the deploy script is run again after a code change
- **THEN** the app is republished, redeployed as a zip and restarted, without recreating Azure resources

#### Scenario: Publish artifacts are not committed
- **WHEN** the deploy script has run
- **THEN** the generated `publish/` folder and `publish.zip` are ignored by git (`.gitignore` patterns `[Pp]ublish/` and `[Pp]ublish*.zip`)

### Requirement: Combined Wrapper Script
`azure-appservice-linux.ps1` SHALL log in once, select the subscription, run the provisioning script and then the deployment script (both with `-SkipLogin`) with the same parameters, and print the app URL. Like the provisioning script it declares `Subscription`, `ResourceGroup`, `Location`, `PlanName`, `AppName`, `StorageAccount`, `ShareName`, `Runtime` and `DiscogsToken` as mandatory, so the same caller-must-pass and non-empty-token rules apply; `MountPath`, `DatabasePath`, `PublishOutput` and `PublishArchive` are optional with defaults.

#### Scenario: Full setup in one run
- **WHEN** the wrapper script is run
- **THEN** resources are provisioned first and the application is deployed afterwards

### Requirement: Runtime And Framework Alignment
The App Service runtime SHALL support the target framework of `src/SetlistCreator.Web/SetlistCreator.Web.csproj` (`net10.0`); the README tells operators to confirm this before deploying.

#### Scenario: Runtime check
- **WHEN** the Web project's target framework changes
- **THEN** the `Runtime` parameter of the scripts must be checked against `az webapp list-runtimes --os linux` (the init script prints that list while running)
