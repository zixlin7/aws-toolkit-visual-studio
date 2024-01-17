# This file contains utility functions related to release versions

# GetReleaseVersion - looks up the currently defined build version from the release manifest
# Expected to be called from the root folder
function GetReleaseVersion {
    # Assumption: we're being called from the root folder
    $configPath = Join-Path -Path $pwd -ChildPath "buildtools"
    $configPath = Join-Path -Path $configPath -ChildPath "release"
    $configPath = Join-Path -Path $configPath -ChildPath "config.json"
    $releaseConfig = Get-Content -Raw $configPath | ConvertFrom-Json
    return $releaseConfig.version
}

Export-ModuleMember -Function GetReleaseVersion
