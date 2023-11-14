param (
    [Parameter(Mandatory=$true)][string]$repoRoot
)

# vshwere can be found here for VS 15.2+
$VSWHERE = "${env:ProgramFiles(X86)}\Microsoft Visual Studio\Installer\vswhere.exe"

# Locates a Visual Studio install of at least version 17.0
$VSINSTALLDIR = & $VSWHERE -version 17.0 -property installationPath

# Developer shell entry point
$DEVSHELL = "${VSINSTALLDIR}\Common7\Tools\Launch-VsDevShell.ps1"
& $DEVSHELL

# Produces changelog notes before the release script pushes the candidate branch
$GENERATE_NOTES = "msbuild buildtools\changelog.proj /t:createRelease /p:ReleaseVersion=`%RELEASE_VERSION`%"

cd $repoRoot
python buildtools\release\release.py --hooks $GENERATE_NOTES
