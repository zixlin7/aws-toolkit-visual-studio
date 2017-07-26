<#
.Synopsis
    Signs a Visual Studio extensibility package.
.DESCRIPTION
    Signs a Visual Studio extensibility package specified in the VsixPackage parameter. 
    The script expects the following environment variables to be set on the build server 
    prior to use:
    DOTNET_BUILD_CERTFILE   				The path to the pfx file to sign with
    DOTNET_BUILD_CERTFILE_PASSWORD 			The encrypted password materials for the pfx (in KMS)
    DOTNET_BUILD_CERTFILE_PROFILE			The credential profile owning the KMS key for decryption
    DOTNET_BUILD_CERTFILE_PROFILE_LOCATION	Credential file holding the specified profile
    DOTNET_BUILD_CERTFILE_REGION			Region holding the decryption key materials

    The script also requires the vsixsigntool.exe be available in the same folder as the
    scriptfile. This tool can be downloaded from nuget: https://www.nuget.org/packages/Microsoft.VSSDK.Vsixsigntool
#>

[CmdletBinding()]
Param
(
    [Parameter(Mandatory=$true, ValueFromPipeline=$true)]
    [string[]]$VsixPackage
)

Begin
{
    $encryptedBytes = [System.Convert]::FromBase64String($env:DOTNET_BUILD_CERTFILE_PASSWORD)
	
    $encryptedMemoryStreamToDecrypt = New-Object System.IO.MemoryStream($encryptedBytes, 0, $encryptedBytes.Length)
    $decryptedMemoryStream = Invoke-KMSDecrypt -CiphertextBlob $encryptedMemoryStreamToDecrypt -ProfileName $env:DOTNET_BUILD_CERTFILE_PROFILE -ProfilesLocation $env:DOTNET_BUILD_CERTFILE_PROFILE_LOCATION -Region $env:DOTNET_BUILD_CERTFILE_REGION

    $certPassword = [System.Text.Encoding]::UTF8.GetString($decryptedMemoryStream.Plaintext.ToArray())
}

Process
{
    Write-Verbose "Signing $VsixPackage"
    & $PSScriptRoot\vsixsigntool.exe sign /f $env:DOTNET_BUILD_CERTFILE /p $certPassword /sha1 "d2038c3d2e59604d9ad05674b21b53ea40ace63f" /t "http://timestamp.digicert.com" $VsixPackage 

    Write-Verbose "...verifying signature"
    & $PSScriptRoot\vsixsigntool.exe verify /f $env:DOTNET_BUILD_CERTFILE /p $certPassword /sha1 "d2038c3d2e59604d9ad05674b21b53ea40ace63f" $VsixPackage
}