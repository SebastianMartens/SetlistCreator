param(
    [Parameter(Mandatory = $true)]
    [string]$Subscription = "63dee7c2-af00-429a-b2de-e4a867f1e9cc",

    [Parameter(Mandatory = $true)]
    [string]$ResourceGroup = "setlistcreator-linux-rg",

    [Parameter(Mandatory = $true)]
    [string]$Location = "westeurope", ##"germanywestcentral",

    [Parameter(Mandatory = $true)]
    [string]$PlanName = "setlistcreator-linux-plan",

    [Parameter(Mandatory = $true)]
    [string]$AppName = "setlistcreator-linux-app",

    [Parameter(Mandatory = $true)]
    [string]$StorageAccount = "setlistcreatorlinuxsa",

    [Parameter(Mandatory = $true)]
    [string]$ShareName = "setlistdata",

    [Parameter(Mandatory = $true)]
    [string]$Runtime = "DOTNETCORE|10.0",

    [Parameter(Mandatory = $true)]
    [string]$DiscogsToken = "",

    [string]$MountPath = "/home/data",
    [string]$DatabasePath = "/home/data/setlists.db",
    [string]$PublishOutput = ".\\publish",
    [string]$PublishArchive = ".\\publish.zip"
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $PSCommandPath
$initScript = Join-Path $scriptRoot "azure-appservice-linux-init.ps1"
$deployScript = Join-Path $scriptRoot "azure-appservice-linux-deploy.ps1"

Write-Host "Logging into Azure and selecting subscription..."
az login --tenant 8dd85f0a-a4dc-4a23-9fa3-9a54b07eb2be
az account set --subscription $Subscription

Write-Host "Running Azure initialization..."
& $initScript `
    -Subscription $Subscription `
    -ResourceGroup $ResourceGroup `
    -Location $Location `
    -PlanName $PlanName `
    -AppName $AppName `
    -StorageAccount $StorageAccount `
    -ShareName $ShareName `
    -Runtime $Runtime `
    -DiscogsToken $DiscogsToken `
    -MountPath $MountPath `
    -DatabasePath $DatabasePath `
    -SkipLogin

Write-Host "Running application deployment..."
& $deployScript `
    -Subscription $Subscription `
    -ResourceGroup $ResourceGroup `
    -AppName $AppName `
    -PublishOutput $PublishOutput `
    -PublishArchive $PublishArchive `
    -SkipLogin

Write-Host "Initialization and deployment complete."
Write-Host "Browse: https://$AppName.azurewebsites.net"