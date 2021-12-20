# This script downloads service endpoints files used by the Toolkit
# The endpoints files are bundled as resources within the Toolkit at build time
param (
    [string]$endpointsJsonFileName = "endpoints.json",
    [Parameter(Mandatory=$true)][string]$configuration,
    [Parameter(Mandatory=$true)][string]$hostedfilesFolder
)

# Downloads the endpoints files in a consistent manner
function Get-Endpoints-File {

    Param(
        [Parameter(Mandatory=$true)][string]$configuration,
        [Parameter(Mandatory=$true)][string]$outputPath,
        [Parameter(Mandatory=$true)][string]$url
    )

    try {
        Write-Host "Downloading endpoints file:`r`n`tFrom: $url`r`n`tTo: $outputPath"

        Invoke-WebRequest -UseBasicParsing -o $outputPath $url
    } catch {
        if($configuration == "Release" -Or !(Test-Path $outputPath)) {
            # if release mode or the local path doesn't exist
            throw $_
        } else {
            Write-Host "Warning: Failed to download new endpoints file from $url"
        }
    }
}

# Retrieve the new JSON based endpoints file (endpoints.json)
$endpointsJsonFullPath = "$hostedfilesFolder\$endpointsJsonFileName"
$endpointsJsonUrl = "https://idetoolkits.amazonwebservices.com/$endpointsJsonFileName"
Get-Endpoints-File -configuration $configuration -outputPath $endpointsJsonFullPath -url $endpointsJsonUrl
