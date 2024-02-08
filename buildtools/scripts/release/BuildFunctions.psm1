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

function GetVsToolkitOutputFolder {
    Param(
        # Indicates which version of Visual Studio is being targeted
        # Influences which vsix files will be copied to the destination folder
        # We do this because:
        # - a different toolchain is used to produce extensions for each major version of VS
        # - the resulting extensions have different filenames for each major version of VS
        [Parameter(Mandatory=$true)]
        [ValidateSet("2019", "2022")]
        [string]$VisualStudioVersion
    )    

    Switch -exact ($VisualStudioVersion) 
    {
        "2019" 
        {
            return GetVs2019OutputFolder
        }
        "2022" 
        {
            return GetVs2022OutputFolder
        }
        default
        {
            throw "Unknown VS Version: " + $VisualStudioVersion
        }
    }
}

# Assumption: Script is being invoked from the repo root
function GetVs2019OutputFolder {
    # VS 2019 build chain outputs to /Deployment/16.0/Release
    $srcRoot = Join-Path -Path $pwd -ChildPath "Deployment"
    $srcRoot = Join-Path -Path $srcRoot -ChildPath "16.0"
    $srcRoot = Join-Path -Path $srcRoot -ChildPath "Release"

    return $srcRoot 
}

# Assumption: Script is being invoked from the repo root
function GetVs2022OutputFolder {
    # VS 2022 build chain outputs to /Deployment/17.0/Release
    $srcRoot = Join-Path -Path $pwd -ChildPath "Deployment"
    $srcRoot = Join-Path -Path $srcRoot -ChildPath "17.0"
    $srcRoot = Join-Path -Path $srcRoot -ChildPath "Release"

    return $srcRoot 
}

function GetVsToolkitVsixFileNames {
    Param(
        # Indicates which version of Visual Studio is being targeted
        # Influences which vsix files will be copied to the destination folder
        # We do this because:
        # - a different toolchain is used to produce extensions for each major version of VS
        # - the resulting extensions have different filenames for each major version of VS
        [Parameter(Mandatory=$true)]
        [ValidateSet("2019", "2022")]
        [string]$VisualStudioVersion
    )    

    Switch -exact ($VisualStudioVersion) 
    {
        "2019" 
        {
            return GetVs2019VsixFileNames
        }
        "2022" 
        {
            return GetVs2022VsixFileNames
        }
        default
        {
            throw "Unknown VS Version: " + $VisualStudioVersion
        }
    }
}

function GetVs2019VsixFileNames {
    return [string[]] @("AWSToolkitPackage.vsix")
}

function GetVs2022VsixFileNames {
    return [string[]] @("AWSToolkitPackage.v17.vsix")
}

Export-ModuleMember -Function BuildReleaseCandidate
Export-ModuleMember -Function GetVsToolkitOutputFolder
Export-ModuleMember -Function GetVsToolkitVsixFileNames
