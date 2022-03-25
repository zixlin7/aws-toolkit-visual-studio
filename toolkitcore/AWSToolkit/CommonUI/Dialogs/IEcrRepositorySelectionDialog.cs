using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.CommonUI.Dialogs
{
    public interface IEcrRepositorySelectionDialog
    {
        /// <summary>
        /// Name of the selected ECR Repository
        /// </summary>
        string RepositoryName { get; set; }

        /// <summary>
        /// Credentials of account to show Repositories for
        /// </summary>
        ICredentialIdentifier CredentialsId { get; set; }

        /// <summary>
        /// Region to load Repositories from
        /// </summary>
        ToolkitRegion Region { get; set; }

        bool Show();
    }
}
