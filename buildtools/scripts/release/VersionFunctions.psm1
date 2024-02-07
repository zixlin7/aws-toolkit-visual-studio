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

# SetReleaseVersion - adjusts the currently defined build version in the release manifest
# Expected to be called from the root folder
function SetReleaseVersion {
    param (
        # Indicates what type of increment to apply to the release version.
        [Parameter(Mandatory = $true)][string]$Version
    )
    
    # Assumption: we're being called from the root folder
    $configPath = Join-Path -Path $pwd -ChildPath "buildtools"
    $configPath = Join-Path -Path $configPath -ChildPath "release"
    $configPath = Join-Path -Path $configPath -ChildPath "config.json"
    $releaseConfig = Get-Content -Raw $configPath | ConvertFrom-Json
    $releaseConfig.version = $Version
    $releaseConfig | ConvertTo-Json -Depth 32 | Set-Content -Path $configPath
}

Export-ModuleMember -Function GetReleaseVersion
Export-ModuleMember -Function SetReleaseVersion
