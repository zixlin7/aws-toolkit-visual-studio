# This function sets the next release version in the manifest used by the release pipeline
param (
    # Release version to apply
    [Parameter(Mandatory = $true)][string]$Version
)

# Globals

$thisScriptFolder = $MyInvocation.MyCommand.Path | Split-Path -Parent

# Imported methods

Import-Module $thisScriptFolder/VersionFunctions.psm1 -Force

## ----- This script (Create-Release-Candidate) -----

Write-Output "----------------------------------------"
Write-Output "Setting next release version: $Version"

SetReleaseVersion -Version $Version
Write-Output "----------------------------------------"
