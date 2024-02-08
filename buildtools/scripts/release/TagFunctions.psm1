# This file contains utility functions related to git tags

# CreateReleaseTag - creates and returns the git tag representing a release associated with the given release candidate tag
# Throws an error if the release candidate tag is not valid
# 
# Example:
# "1.2.3.4" -> "1.2.3.4"
function CreateReleaseTag {
    Param(
        [Parameter(Mandatory = $true)][string]$ReleaseVersion
    )

    # The tag is the same as the version.
    # We aren't prepending 'v' or anything like this.
    $ReleaseVersion = $ReleaseVersion.Trim()
    return $ReleaseVersion
}

Export-ModuleMember -Function CreateReleaseTag
