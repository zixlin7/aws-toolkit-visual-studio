# This script is part of the Toolkit's release infrastructure.
# It:
#   - creates a git tag on the commit that produced the build that was released
#   - pushes the tag to the repo
#   - creates a GitHub Release, setting the description as this release's changelog
#     No builds are attached to the release. Other scripts do this.
# This script is intended to be run at the repo root.
param (
    # the name of the git remote to push the tag to
    [Parameter(Mandatory = $true)][string]$GitRemoteName,
    # The release tag to create
    [Parameter(Mandatory = $true)][string]$ReleaseTag,
    # The commit to associate the tag and release with
    [Parameter(Mandatory = $true)][string]$CommitId,
    # Path to a file containing markdown representing the release notes.
    # This is assigned to the Git Release's description.
    [Parameter(Mandatory = $true)][string]$ReleaseNotesPath,
    # Repo owner, 'aws' in aws/aws-toolkit-visual-studio-staging
    [Parameter(Mandatory = $true)][string]$RepoOwner,
    # Repo name, 'aws-toolkit-visual-studio-staging' in aws/aws-toolkit-visual-studio-staging
    [Parameter(Mandatory = $true)][string]$RepoName
)

# Globals

$thisScriptFolder = $MyInvocation.MyCommand.Path | Split-Path -Parent

# Imported methods

Import-Module $thisScriptFolder/VersionFunctions.psm1 -Force

# Local Support methods

function ThrowIfLastExitFailed {

    if ($LASTEXITCODE -ne 0) {
        throw "The last command failed to run successfully"
    } 
}

## ----- This script (Create-Release-Tag) -----

$releaseVersion = GetReleaseVersion

Write-Output "----------------------------------------"
Write-Output "Creating Git tag: $ReleaseTag"
Write-Output "On commit: $CommitId"

git tag -a $ReleaseTag $CommitId -m "Release tag created from automation"
ThrowIfLastExitFailed

Write-Output "----------------------------------------"
Write-Output "Pushing tag to remote: $GitRemoteName"
git push $GitRemoteName $ReleaseTag
ThrowIfLastExitFailed

Write-Output "----------------------------------------"
Write-Output "Creating Git Release: $ReleaseTag"

# repo format: OWNER/REPO, eg: aws/aws-toolkit-visual-studio-staging
$repo = "$RepoOwner/$RepoName"
Write-Output "In repo: $repo"

gh release create $ReleaseTag --repo $repo --verify-tag --title $releaseVersion --notes-file $ReleaseNotesPath
ThrowIfLastExitFailed

Write-Output "----------------------------------------"
