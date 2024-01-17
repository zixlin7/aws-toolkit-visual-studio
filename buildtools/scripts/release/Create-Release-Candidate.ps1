# This script is part of the Toolkit's release infrastructure.
# This script creates release candidate Vsix file(s), and copies them to the $DestinationFolder folder
# This script is intended to be run at the repo root.
param (
    # Indicates which version of Visual Studio is being targeted
    # Influences which vsix files will be copied to the destination folder
    # We do this because:
    # - a different toolchain is used to produce extensions for each major version of VS
    # - the resulting extensions have different filenames for each major version of VS
    [Parameter(Mandatory=$true)]
    [ValidateSet("2019", "2022")]
    [string]$VisualStudioVersion,
    [Parameter(Mandatory=$true)][string]$DestinationFolder,
    # The commit sha, must be the full 40-character value
    [Parameter(Mandatory=$true)][string]$CommitId,
    # Url of the repo
    # eg: https://github.com/aws/aws-toolkit-visual-studio-staging.git
    [Parameter(Mandatory=$true)][string]$RepoUrl,
    [Parameter(Mandatory=$true)][string]$StatusContext
)

# Globals

$thisScriptFolder = $MyInvocation.MyCommand.Path | Split-Path -Parent

# Imported methods

Import-Module $thisScriptFolder/../github/StatusFunctions.psm1 -Force
Import-Module $thisScriptFolder/../github/UrlFunctions.psm1 -Force
Import-Module $thisScriptFolder/BuildFunctions.psm1 -Force
Import-Module $thisScriptFolder/VersionFunctions.psm1 -Force

# Local Support methods

# Copy-Vsix - Copies final extension artifacts to the intended destination folder
function Copy-Vsix {
    Param(
        [Parameter(Mandatory=$true)][string]$VisualStudioVersion
    )

    echo "----------------------------------------"
    echo "Copying VSIX Files for VS Version: $VisualStudioVersion"

    # Ensure destination folder exists
    $dstFolder = Join-Path -Path $pwd -ChildPath $DestinationFolder
    New-Item -Path $dstFolder -ItemType "directory" -Force | Out-Null

    # Each VS version makes different files. Copy from appropriate locations and filenames.
    Switch -exact ($VisualStudioVersion) 
    {
        "2019" {
            # VS 2019 build chain outputs to /Deployment/16.0/Release
            $srcRoot = Join-Path -Path $pwd -ChildPath "Deployment"
            $srcRoot = Join-Path -Path $srcRoot -ChildPath "16.0"
            $srcRoot = Join-Path -Path $srcRoot -ChildPath "Release"

            $toolkitVs2019Vsix = Join-Path -Path $srcRoot -ChildPath "AWSToolkitPackage.vsix"
            Copy-Item -Path $toolkitVs2019Vsix -Destination $dstFolder
        }
        "2022" {
            # VS 2022 build chain outputs to /Deployment/17.0/Release
            $srcRoot = Join-Path -Path $pwd -ChildPath "Deployment"
            $srcRoot = Join-Path -Path $srcRoot -ChildPath "17.0"
            $srcRoot = Join-Path -Path $srcRoot -ChildPath "Release"

            $toolkitVs2022Vsix = Join-Path -Path $srcRoot -ChildPath "AWSToolkitPackage.v17.vsix"
            Copy-Item -Path $toolkitVs2022Vsix -Destination $dstFolder
        }
    }

    echo "Files copied to: $dstFolder"
    echo "----------------------------------------"
}

## ----- This script (Create-Release-Candidate) -----
$version = GetReleaseVersion
echo "----------------------------------------"
echo "Release candidate version: $version"

$repoProps = GetRepoProperties -RepoUrl $RepoUrl
$repoOwner = $repoProps.RepoOwner
$repoName = $repoProps.RepoName

echo "----------------------------------------"
echo "Repo: $RepoUrl"
echo "Owner: $repoOwner"
echo "Name: $repoName"
echo "----------------------------------------"

UpdateCommitStatus -RepoOwner $repoOwner -RepoName $repoName -CommitId $CommitId `
    -State pending -Context $StatusContext `
    -Description "Building..."

BuildReleaseCandidate -Version $version

UpdateCommitStatus -RepoOwner $repoOwner -RepoName $repoName -CommitId $CommitId `
    -State pending -Context $StatusContext `
    -Description "Post build: Copying files..."

Copy-Vsix -VisualStudioVersion $VisualStudioVersion

UpdateCommitStatus -RepoOwner $repoOwner -RepoName $repoName -CommitId $CommitId `
    -State pending -Context $StatusContext `
    -Description "Wrapping things up..."
