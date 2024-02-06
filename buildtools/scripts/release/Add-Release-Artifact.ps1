# This script is part of the Toolkit's release infrastructure.
# It attaches all files in a given folder to a GitHub release.
# The release pipeline creates the GitHub release using the Create-Release-Tag PowerShell script.
param (
    # The release tag to create
    [Parameter(Mandatory = $true)][string]$ReleaseTag,
    # All contents in the specific folder will be uploaded to the release
    [Parameter(Mandatory = $true)][string]$Folder,
    # Repo owner, 'aws' in aws/aws-toolkit-visual-studio-staging
    [Parameter(Mandatory = $true)][string]$RepoOwner,
    # Repo name, 'aws-toolkit-visual-studio-staging' in aws/aws-toolkit-visual-studio-staging
    [Parameter(Mandatory = $true)][string]$RepoName
)

function ThrowIfLastExitFailed {
    if ($LASTEXITCODE -ne 0) {
        throw "The last command failed to run successfully"
    } 
}

## ----- This script (Create-Release-Tag) -----

Write-Output "----------------------------------------"
Write-Output "Adding files to Git Release: $ReleaseTag"

# repo format: OWNER/REPO, eg: aws/aws-toolkit-visual-studio-staging
$repo = "$RepoOwner/$RepoName"
Write-Output "In repo: $repo"

# Find the files to be uploaded
$files = Get-ChildItem $folder | ForEach-Object { $_.FullName }
Write-Output "Files:" $files

gh release upload $ReleaseTag @($files) --repo $repo --clobber 
ThrowIfLastExitFailed

Write-Output "----------------------------------------"
