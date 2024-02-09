# This function calculates the next release version based on the current value, and requested increment.
param (
    # Indicates what type of increment to apply to the release version.
    [Parameter(Mandatory = $true)]
    [ValidateSet("Major", "Minor", "Build", "Revision", "Custom")]
    [string]$VersionIncrement,
    # Overrides the increment logic, setting the next version to this value.
    # Used when $VersionIncrement is set to "Custom"
    [string]$CustomVersion = ""
)

# Globals

$thisScriptFolder = $MyInvocation.MyCommand.Path | Split-Path -Parent

# Imported methods

Import-Module $thisScriptFolder/VersionFunctions.psm1 -Force

## ----- This script (Get-Next-Version) -----

if ($VersionIncrement -eq "Custom") {
    if (($CustomVersion -eq $null) -or ($CustomVersion.Trim() -eq "")) {
        throw "Custom version is missing"
    }

    return (New-Object System.Version($CustomVersion)).ToString()
}
else {
    $currentVersionStr = GetReleaseVersion
    $currentVersion = New-Object System.Version($currentVersionStr)

    $major = $currentVersion.Major
    $minor = $currentVersion.Minor
    $build = $currentVersion.Build
    $revision = $currentVersion.Revision

    Switch ($VersionIncrement) {
        "Major" {
            $major += 1
            $minor = 0
            $build = 0
            $revision = 0
        }
        "Minor" {
            $minor += 1
            $build = 0
            $revision = 0
        }
        "Build" {
            $build += 1 
            $revision = 0
        }
        "Revision" { $revision += 1 }
    }

    return (New-Object System.Version($major, $minor, $build, $revision)).ToString()
}
