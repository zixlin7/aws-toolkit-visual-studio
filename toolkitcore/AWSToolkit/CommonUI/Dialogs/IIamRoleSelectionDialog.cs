using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.CommonUI.Dialogs
{
    public interface IIamRoleSelectionDialog
    {
        /// <summary>
        /// The Selected Role ARN
        /// </summary>
        string RoleArn { get; set; }

        /// <summary>
        /// Credentials of account to show Roles for
        /// </summary>
        ICredentialIdentifier CredentialsId { get; set; }

        /// <summary>
        /// Region to associate the AWS SDK Client with
        /// </summary>
        ToolkitRegion Region { get; set; }

        /// <summary>
        /// Optional - if set, filters displayed roles to those including the provided service principal
        /// </summary>
        string ServicePrincipalFilter { get; set; }

        bool Show();
    }
}
