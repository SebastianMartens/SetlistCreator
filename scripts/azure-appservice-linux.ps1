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

Write-Host "Logging into Azure and selecting subscription..."
az login --tenant 8dd85f0a-a4dc-4a23-9fa3-9a54b07eb2be
az account set --subscription $Subscription

Write-Host "Creating resource group..."
az group create --name $ResourceGroup --location $Location

Write-Host "Creating storage account..."
az storage account create --name $StorageAccount --resource-group $ResourceGroup --location $Location --sku Standard_LRS --kind StorageV2

Write-Host "Retrieving storage key..."
$storageKey = az storage account keys list --resource-group $ResourceGroup --account-name $StorageAccount --query "[0].value" --output tsv

Write-Host "Creating Azure Files share..."
az storage share create --name $ShareName --account-name $StorageAccount --account-key $storageKey

Write-Host "Creating Linux App Service plan..."
az appservice plan create --name $PlanName --resource-group $ResourceGroup --location $Location --sku F1 --is-linux

Write-Host "Available Linux runtimes:"
az webapp list-runtimes --os linux --output table

Write-Host "Creating web app..."
az webapp create --resource-group $ResourceGroup --plan $PlanName --name $AppName --runtime $Runtime

Write-Host "Mounting Azure Files share..."
az webapp config storage-account add --resource-group $ResourceGroup --name $AppName --custom-id setlistdata --storage-type AzureFiles --account-name $StorageAccount --share-name $ShareName --access-key $storageKey --mount-path $MountPath

Write-Host "Applying app settings..."
az webapp config appsettings set --resource-group $ResourceGroup --name $AppName --settings ASPNETCORE_ENVIRONMENT=Production Discogs__Token=$DiscogsToken Setlist__DatabasePath=$DatabasePath

Write-Host "Enabling filesystem logs..."
az webapp log config --resource-group $ResourceGroup --name $AppName --application-logging filesystem --level information --web-server-logging filesystem

Write-Host "Publishing application..."
dotnet publish .\src\SetlistCreator.Web\SetlistCreator.Web.csproj -c Release -o $PublishOutput

if (Test-Path $PublishArchive)
{
    Remove-Item $PublishArchive -Force
}

Write-Host "Creating deployment archive..."
if (-not (Get-Command tar.exe -ErrorAction SilentlyContinue))
{
    throw "tar.exe is required to create a Linux-compatible deployment zip on Windows."
}

Push-Location $PublishOutput
try
{
    $archivePath = Resolve-Path (Join-Path ".." (Split-Path $PublishArchive -Leaf))
    tar.exe -a -c -f $archivePath *
    if ($LASTEXITCODE -ne 0)
    {
        throw "Failed to create deployment archive using tar.exe."
    }
}
finally
{
    Pop-Location
}

Write-Host "Deploying application archive..."
az webapp deploy --resource-group $ResourceGroup --name $AppName --src-path $PublishArchive --type zip #--enriched-errors true

Write-Host "Restarting web app..."
az webapp restart --resource-group $ResourceGroup --name $AppName

Write-Host "Deployment complete. Open the site and verify persistence across restart."
Write-Host "Browse: https://$AppName.azurewebsites.net"