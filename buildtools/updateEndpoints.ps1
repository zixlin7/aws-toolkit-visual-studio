param (
    [string]$endpointsFileName = "ServiceEndPoints.xml",
    [Parameter(Mandatory=$true)][string]$configuration,
    [Parameter(Mandatory=$true)][string]$s3BucketName,
    [Parameter(Mandatory=$true)][string]$s3BucketRegion,
    [Parameter(Mandatory=$true)][string]$hostedfilesFolder
)

$endpointsFilePath = "$hostedfilesFolder\$endpointsFileName"
$endpointsUrl = "http://$s3BucketName.s3.$s3BucketRegion.amazonaws.com/$endpointsFileName"
try {
    Invoke-WebRequest -UseBasicParsing -o $endpointsFilePath $endpointsUrl
} catch {
    if($configuration == "Release" -Or !(Test-Path $endpointsFilePath)) {
        # if release mode or the local path doesn't exist
        throw $_
    } else {
        Write-Host "warning: Failed to download new endpoints file"
    }
}