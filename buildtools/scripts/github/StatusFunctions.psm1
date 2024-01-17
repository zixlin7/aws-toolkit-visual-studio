# This file contains utility functions related to GitHub commit Statuses

# UpdateCommitStatus - This function creates or updates a GitHub commit status.
# Reference: https://docs.github.com/en/rest/commits/statuses?apiVersion=2022-11-28#create-a-commit-status
# Prerequisites:
# - access to GitHub CLI
# - GitHub permissions, either already established, or with a GITHUB_TOKEN environment variable
function UpdateCommitStatus {
    param (
        # eg: 'aws'
        [Parameter(Mandatory=$true)][string]$RepoOwner,
        # eg: 'aws-toolkit-visual-studio-staging'
        [Parameter(Mandatory=$true)][string]$RepoName,
        # The commit sha, must be the full 40-character value
        [Parameter(Mandatory=$true)][string]$CommitId,
        [Parameter(Mandatory=$true)]
        [ValidateSet("error", "failure", "pending", "success")]
        [string]$State,
        # The primary Status name (think "Heading 1")
        # Don't pass quotes through here. It doesn't handle it properly
        [Parameter(Mandatory=$false)][string]$Context = 'default',
        # The secondary Status name (think "Heading 2")
        # Don't pass quotes through here. It doesn't handle it properly
        [Parameter(Mandatory=$false)][string]$Description = $null,
        # URL to associate with status, if any. Eg: build logs
        [Parameter(Mandatory=$false)][string]$TargetUrl = $null
    )

    $acceptHeader = "Accept: application/vnd.github+json"
    $apiVersionHeader = "X-GitHub-Api-Version: 2022-11-28"

    $url = "/repos/$RepoOwner/$RepoName/statuses/$CommitId"
    $context = $Context
    $description = $Description

    echo "----- Updating Commit Status for $CommitId -----"

    # Omit the target_url otherwise the status link is unconditionally active
    # I couldn't figure out how to do this in a better way.
    if ($TargetUrl) 
    {
        gh api --method POST -H ""$acceptHeader"" -H ""$apiVersionHeader"" ""$url"" -f state=$State -f ""context=$context"" -f ""description=$description"" -f target_url=$TargetUrl
    }
    else
    {
        gh api --method POST -H ""$acceptHeader"" -H ""$apiVersionHeader"" ""$url"" -f state=$State -f ""context=$context"" -f ""description=$description""
    }

    # GitHub CLI output doesn't write out a final newline. Let's force one.
    echo ""
    echo "----- Done Updating Status for $CommitId -----"
}

Export-ModuleMember -Function UpdateCommitStatus
