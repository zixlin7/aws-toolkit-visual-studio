# This file contains utility functions related to compiling release builds

# BuildReleaseCandidate - produces a release candidate, stamped with the provided version
# Expected to be called from the root folder
function BuildReleaseCandidate {
    Param(
        [Parameter(Mandatory=$true)][string]$Version
    )

    echo "Building release candidate, version: $Version ..."
    msbuild buildtools\build.proj `
        /p:Configuration=Release `
        /p:UpdateVersions=true `
        /p:AWSToolkitVersionNumber=$Version `
        /t:update-version `
        /t:restore `
        /t:compile
}

Export-ModuleMember -Function BuildReleaseCandidate
