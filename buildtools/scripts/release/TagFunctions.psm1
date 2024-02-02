# This file contains utility functions related to git tags

Set-Variable ReleaseCandidatePrefix -Option Constant -Value "release-v"

# CreateReleaseTag - creates and returns the git tag representing a release associated with the given release candidate tag
# Throws an error if the release candidate tag is not valid
# 
# Example:
# "release-v1.2.3.4" -> "1.2.3.4"
function CreateReleaseTag {
    Param(
        [Parameter(Mandatory = $true)][string]$ReleaseCandidateTag
    )

    $ReleaseCandidateTag = $ReleaseCandidateTag.Trim()

    if (-not $ReleaseCandidateTag.StartsWith($ReleaseCandidatePrefix)) {
        throw "Release candidate tag $ReleaseCandidateTag is not in the expected format. It should start with $ReleaseCandidatePrefix"
    }

    return $ReleaseCandidateTag.Substring($ReleaseCandidatePrefix.Length)
}

Export-ModuleMember -Function CreateReleaseTag
