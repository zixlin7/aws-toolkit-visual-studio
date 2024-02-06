# This script takes a changelog summary json (found in the repo's \.changes\ folder) and
# produces a markdown rendering of it.
param (
    [Parameter(Mandatory = $true)][string]$Source,
    [Parameter(Mandatory = $true)][string]$Destination
)

$json = Get-Content -Path $Source | ConvertFrom-Json

$version = $json.Version
$date = $json.Date

Out-File -FilePath $Destination -Encoding utf8 -InputObject "## $version ($date)"
Out-File -FilePath $Destination -Encoding utf8 -Append -InputObject ""

foreach ($entry in $json.Entries) {
    $type = $entry.Type
    $description = $entry.Description
    Out-File -FilePath $Destination -Encoding utf8 -Append -InputObject "- **$type** $description"
}
