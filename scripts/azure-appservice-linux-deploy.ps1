param(
    [Parameter(Mandatory = $false)]
    [string]$Subscription = "63dee7c2-af00-429a-b2de-e4a867f1e9cc",

    [Parameter(Mandatory = $false)]
    [string]$ResourceGroup = "setlistcreator-linux-rg",

    [Parameter(Mandatory = $false)]
    [string]$AppName = "setlistcreator-linux-app",

    [string]$PublishOutput = ".\publish",
    [string]$PublishArchive = ".\publish.zip",
    [switch]$SkipLogin
)

$ErrorActionPreference = "Stop"

if (-not $SkipLogin)
{
    Write-Host "Logging into Azure and selecting subscription..."
    az login --tenant 8dd85f0a-a4dc-4a23-9fa3-9a54b07eb2be
    az account set --subscription $Subscription
}

Write-Host "Publishing application..."
dotnet publish .\src\SetlistCreator.Web\SetlistCreator.Web.csproj -c Release -o $PublishOutput

$archivePath = if ([System.IO.Path]::IsPathRooted($PublishArchive))
{
    $PublishArchive
}
else
{
    [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $PublishArchive))
}

if (Test-Path $archivePath)
{
    Remove-Item $archivePath -Force
}

Write-Host "Creating deployment archive..."
if (-not (Get-Command tar.exe -ErrorAction SilentlyContinue))
{
    throw "tar.exe is required to create a Linux-compatible deployment zip on Windows."
}

Push-Location $PublishOutput
try
{
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
az webapp deploy --resource-group $ResourceGroup --name $AppName --src-path $archivePath --type zip

Write-Host "Restarting web app..."
az webapp restart --resource-group $ResourceGroup --name $AppName

Write-Host "Deployment complete."
Write-Host "Browse: https://$AppName.azurewebsites.net"