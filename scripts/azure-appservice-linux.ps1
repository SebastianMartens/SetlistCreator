param(
    [Parameter(Mandatory = $true)]
    [string]$Subscription,

    [Parameter(Mandatory = $true)]
    [string]$ResourceGroup,

    [Parameter(Mandatory = $true)]
    [string]$Location,

    [Parameter(Mandatory = $true)]
    [string]$PlanName,

    [Parameter(Mandatory = $true)]
    [string]$AppName,

    [Parameter(Mandatory = $true)]
    [string]$StorageAccount,

    [Parameter(Mandatory = $true)]
    [string]$ShareName,

    [Parameter(Mandatory = $true)]
    [string]$Runtime,

    [Parameter(Mandatory = $true)]
    [string]$DiscogsToken,

    [string]$MountPath = "/home/data",
    [string]$DatabasePath = "/home/data/setlists.db",
    [string]$PublishOutput = ".\\publish",
    [string]$PublishArchive = ".\\publish.zip"
)

$ErrorActionPreference = "Stop"

Write-Host "Logging into Azure and selecting subscription..."
az login
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
az appservice plan create --name $PlanName --resource-group $ResourceGroup --location $Location --sku B1 --is-linux

Write-Host "Available Linux runtimes:"
az webapp list-runtimes --linux --output table

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
Compress-Archive -Path (Join-Path $PublishOutput "*") -DestinationPath $PublishArchive -Force

Write-Host "Deploying application archive..."
az webapp deploy --resource-group $ResourceGroup --name $AppName --src-path $PublishArchive --type zip

Write-Host "Restarting web app..."
az webapp restart --resource-group $ResourceGroup --name $AppName

Write-Host "Deployment complete. Open the site and verify persistence across restart."
Write-Host "Browse: https://$AppName.azurewebsites.net"