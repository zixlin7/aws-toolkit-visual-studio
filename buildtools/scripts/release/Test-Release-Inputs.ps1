# This script is part of the Toolkit's release infrastructure.
# This script performs some sanity checks using the current release pipeline state.
# Any issues would cause the release process to stop, preventing larger downstream problems.
# This script is intended to be run at the repo root.
param (
    # Represents the release version used to trigger the release pipeline
    # This is the version being released.
    # Expected format: "1.2.3.4"
    [Parameter(Mandatory = $true)]
    [string]$ReleaseVersion,
    # Represents the git commit of the build being released
    [Parameter(Mandatory = $true)]
    [string]$CommitId
)

# Globals

$thisScriptFolder = $MyInvocation.MyCommand.Path | Split-Path -Parent

# Imported methods

Import-Module $thisScriptFolder/TagFunctions.psm1 -Force
Import-Module $thisScriptFolder/VersionFunctions.psm1 -Force

# Local Support methods

# Test-Required-Input - throws if the given text is null, blank, or whitespace
function Test-Required-Input {
    Param(
        [Parameter(Mandatory = $true)][string]$Text
    )

    if (($Text -eq $null) -or ($Text.Trim() -eq "")) {
        throw "One or more required inputs is missing a value"
    }
}

# Test-Release-Version - Checks that the version we're intending to release 
# matches what is in the release manifest.
function Test-Release-Version {
    Param(
        [Parameter(Mandatory = $true)][string]$Version
    )

    Write-Output "Checking if $Version matches the vsix version..."
    Write-Output ""

    $manifestVersion = GetReleaseVersion
    Write-Output "Release manifest version: $manifestVersion"
    
    if ($Version -eq $manifestVersion) {
        Write-Output ""
        Write-Output "Release versions match."
    } 
    else {
        Write-Output ""
        throw "Version mismatch: Release pipeline is processing version $Version, but the vsix manifest reports $manifestVersion"
    }
    Write-Output "----------------------------------------"
}

# Test-Unique-Tag - Checks that the release tag isn't already in the repo, which implies this version has 
# already been used in a release.
function Test-Unique-Tag {
    Param(
        [Parameter(Mandatory = $true)][string]$ReleaseTag
    )

    Write-Output "Checking if $ReleaseTag was previously released and tagged..."
    Write-Output ""
    git rev-parse $ReleaseTag
    
    if ($LASTEXITCODE -eq 0) {
        Write-Output ""
        throw "Repo already contains tag $ReleaseTag - assuming this version has been previously released."
    } 
    else {
        Write-Output ""
        Write-Output "Tag $ReleaseTag not found. Assuming this is a good release version."
    }
    Write-Output "----------------------------------------"
}

## ----- This script (Test-Release-Inputs) -----

Write-Output "----------------------------------------"
Write-Output "Release Version: $ReleaseVersion"
Write-Output "CommitId: $CommitId"
Write-Output "----------------------------------------"

Test-Required-Input -Text $ReleaseVersion
Test-Required-Input -Text $CommitId

$releaseTag = CreateReleaseTag -ReleaseVersion $ReleaseVersion
Write-Output "Release Tag: $releaseTag"
Write-Output "----------------------------------------"

Test-Release-Version -Version $ReleaseVersion
Test-Unique-Tag -ReleaseTag $releaseTag

# If you get here, things look good
Write-Output "Validation: Pass!"
Write-Output "----------------------------------------"
