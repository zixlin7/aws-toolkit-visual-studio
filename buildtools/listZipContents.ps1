# This script writes a list of VSIX file contents to a text file
param (
    [Parameter(Mandatory=$true)][string]$vsixFileName,
    [Parameter(Mandatory=$true)][string]$outputFileName
)

function List-Zip-Contents {

    Param(
        [Parameter(Mandatory=$true)][string]$vsixFileName,
        [Parameter(Mandatory=$true)][string]$outputFileName
    )

    try {

        [Reflection.Assembly]::LoadWithPartialName('System.IO.Compression.FileSystem')
        $zip = [IO.Compression.ZipFile]::OpenRead($vsixFileName)
        $zip.Entries.FullName | Sort-Object | Out-File -FilePath $outputFileName
        $zip.Dispose()
    } catch {
        if($configuration == "Release" -Or !(Test-Path $outputPath)) {
            # if release mode or the local path doesn't exist
            throw $_
        } else {
            Write-Host "Failed to look at VSIX contents"
        }
    }
}

List-Zip-Contents -vsixFileName $vsixFileName -outputFileName $outputFileName
