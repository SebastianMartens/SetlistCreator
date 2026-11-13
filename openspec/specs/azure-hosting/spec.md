# azure-hosting Specification

## Purpose

How the app is hosted on Azure App Service (Linux) and which scripts provision and deploy it (`scripts/azure-appservice-linux-init.ps1`, `scripts/azure-appservice-linux-deploy.ps1`, `scripts/azure-appservice-linux.ps1`). The scripts are PowerShell scripts that call the Azure CLI (`az`). This spec describes what the scripts do as written; they are not automated by CI.

## Requirements

### Requirement: One-Time Provisioning Script
`azure-appservice-linux-init.ps1` SHALL, unless `-SkipLogin` is given, run `az login` for the configured tenant and select the subscription. It SHALL then create: a resource group, a StorageV2 / Standard_LRS storage account, an Azure Files share, a Linux App Service plan with SKU F1, and a web app on the plan with runtime `DOTNETCORE|10.0` (default). It SHALL mount the Azure Files share into the web app at `/home/data` (storage type AzureFiles, custom id `setlistdata`), set the app settings `ASPNETCORE_ENVIRONMENT=Production`, `Discogs__Token=<token parameter>` and `Setlist__DatabasePath=/home/data/setlists.db`, and enable filesystem application and web-server logging at Information level. Default parameter values are: resource group `setlistcreator-linux-rg`, location `westeurope`, plan `setlistcreator-linux-plan`, app `setlistcreator-linux-app`, storage account `setlistcreatorlinuxsa`, share `setlistdata`.

#### Scenario: Persistent database location
- **WHEN** provisioning has completed
- **THEN** the web app has the share mounted at `/home/data` and `Setlist__DatabasePath` points to `/home/data/setlists.db`, so the LiteDB file survives restarts and redeployments

#### Scenario: Discogs token supplied
- **WHEN** the script is run with a `-DiscogsToken` value
- **THEN** it is stored in the web app settings as `Discogs__Token`

### Requirement: Deployment Script
`azure-appservice-linux-deploy.ps1` SHALL, unless `-SkipLogin` is given, log in and select the subscription; run `dotnet publish src/SetlistCreator.Web/SetlistCreator.Web.csproj -c Release -o .\publish`; delete any previous `publish.zip`; create `publish.zip` from the contents of the publish folder using `tar.exe` (failing if `tar.exe` is not available or the archive step fails); deploy it with `az webapp deploy --type zip`; restart the web app; and print the URL `https://<AppName>.azurewebsites.net`.

#### Scenario: Repeatable deployment
- **WHEN** the deploy script is run again after a code change
- **THEN** the app is republished, redeployed as a zip and restarted, without recreating Azure resources

#### Scenario: Publish artifacts are not committed
- **WHEN** the deploy script has run
- **THEN** the generated `publish/` folder and `publish.zip` are ignored by git (`.gitignore` patterns `[Pp]ublish/` and `[Pp]ublish*.zip`)

### Requirement: Combined Wrapper Script
`azure-appservice-linux.ps1` SHALL log in once, select the subscription, run the provisioning script and then the deployment script (both with `-SkipLogin`) with the same parameters, and print the app URL.

#### Scenario: Full setup in one run
- **WHEN** the wrapper script is run
- **THEN** resources are provisioned first and the application is deployed afterwards

### Requirement: Runtime And Framework Alignment
The App Service runtime SHALL support the target framework of `src/SetlistCreator.Web/SetlistCreator.Web.csproj` (`net10.0`); the README tells operators to confirm this before deploying.

#### Scenario: Runtime check
- **WHEN** the Web project's target framework changes
- **THEN** the `Runtime` parameter of the scripts must be checked against `az webapp list-runtimes --os linux` (the init script prints that list while running)
