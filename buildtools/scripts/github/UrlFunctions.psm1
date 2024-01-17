# This file contains utility functions related to GitHub URLs

# GetRepoProperties - This function takes a repo url and splits out the owner and name properties.
# Example:
# - Input: https://github.com/aws/aws-toolkit-visual-studio-staging.git
# - Output:
#       - RepoOwner: aws
#       - RepoName: aws-toolkit-visual-studio-staging
function GetRepoProperties {
    param (
        [Parameter(Mandatory=$true)][string]$RepoUrl
    )

    # eg: https://github.com/aws/aws-toolkit-visual-studio-staging.git
    #     0     1 2          3   4
    $repoChunks = $RepoUrl -Split "/"
    $repoOwner = $repoChunks[3]

    # aws-toolkit-visual-studio-staging.git
    $repoName = ($repoChunks[4] -Split ".", 0, "SimpleMatch")[0]

    return @{
        RepoOwner = $repoOwner;
        RepoName = $repoName;
    }
}

Export-ModuleMember -Function GetRepoProperties
