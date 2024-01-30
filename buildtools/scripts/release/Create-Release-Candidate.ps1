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
    # Text applied to GitHub status while this task is being performed
    # Don't pass quotes through here. It doesn't handle it properly
    [Parameter(Mandatory=$true)][string]$StatusContext,
    # Name of S3 Bucket to upload VSIX files to, for the purpose 
    # of starting the signing process.
    [Parameter(Mandatory=$true)][string]$ArchiveBucket
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

    $srcRoot = GetVsToolkitOutputFolder $VisualStudioVersion
    $filenames = GetVsToolkitVsixFileNames $VisualStudioVersion

    foreach ($filename in $filenames) 
    {
        $vsixPath = Join-Path -Path $srcRoot -ChildPath $filename
        Copy-Item -Path $vsixPath -Destination $dstFolder
    }

    echo "Files copied to: $dstFolder"
    echo "----------------------------------------"
}

# Copy-Vsix-To-Archive - Copies the produced VSIX files from the destination folder to the short term archive
# (copying them to the archive kicks off the build signing processes)
function Copy-Vsix-To-Archive {
    echo "----------------------------------------"
    echo "Copying VSIX Files to short term archive: $ArchiveBucket"

    $srcRoot = $DestinationFolder
    $filenames = GetVsToolkitVsixFileNames $VisualStudioVersion

    foreach ($filename in $filenames) 
    {
        $vsixPath = Join-Path -Path $DestinationFolder -ChildPath $filename
        $objectKey = "unsigned/$CommitId/$filename"

        echo "$vsixPath -> $objectKey"
        aws s3 cp $vsixPath s3://$ArchiveBucket/$objectKey
    }

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
Copy-Vsix-To-Archive

UpdateCommitStatus -RepoOwner $repoOwner -RepoName $repoName -CommitId $CommitId `
    -State pending -Context $StatusContext `
    -Description "Wrapping things up..."
