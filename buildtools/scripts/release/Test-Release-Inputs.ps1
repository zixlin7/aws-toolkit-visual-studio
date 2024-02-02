# This script is part of the Toolkit's release infrastructure.
# This script performs some sanity checks using the current release pipeline state.
# Any issues would cause the release process to stop, preventing larger downstream problems.
# This script is intended to be run at the repo root.
param (
    # Represents the release candidate tag used to trigger the release pipeline
    # The tag is associated with the commit of the build being released.
    # Expected format: "release-v<release-version>" eg "release-v1.23.45.0"
    [Parameter(Mandatory = $true)]
    [string]$ReleaseCandidateTag,
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

# Test-Release-Version - Checks that the given release tag (which is expected to represent the release version)
# matches what is in the release manifest (which is expected to be the version of the release candidate).
function Test-Release-Version {
    Param(
        [Parameter(Mandatory = $true)][string]$ReleaseTag
    )

    Write-Output "Checking if $ReleaseTag matches the vsix version..."
    Write-Output ""

    $manifestVersion = GetReleaseVersion
    Write-Output "Release manifest version: $manifestVersion"
    
    if ($ReleaseTag -eq $manifestVersion) {
        Write-Output ""
        Write-Output "Release versions match."
    } 
    else {
        Write-Output ""
        throw "Version mismatch: Release pipeline is processing version $ReleaseTag, but the vsix manifest reports $manifestVersion"
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
Write-Output "Release Candidate Tag: $ReleaseCandidateTag"
Write-Output "CommitId: $CommitId"
Write-Output "----------------------------------------"

Test-Required-Input -Text $ReleaseCandidateTag
Test-Required-Input -Text $CommitId

$releaseTag = CreateReleaseTag -ReleaseCandidateTag $ReleaseCandidateTag
Write-Output "Release Tag: $releaseTag"
Write-Output "----------------------------------------"

Test-Release-Version -ReleaseTag $releaseTag
Test-Unique-Tag -ReleaseTag $releaseTag

# If you get here, things look good
Write-Output "Validation: Pass!"
Write-Output "----------------------------------------"
