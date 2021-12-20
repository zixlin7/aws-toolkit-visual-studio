using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.CommonUI.Dialogs
{
    public interface IVpcSelectionDialog
    {
        /// <summary>
        /// The Selected VPC's ID
        /// </summary>
        string VpcId { get; set; }

        /// <summary>
        /// Credentials of account to show VPCs for
        /// </summary>
        ICredentialIdentifier CredentialsId { get; set; }

        /// <summary>
        /// Region to associate the AWS SDK Client with
        /// </summary>
        ToolkitRegion Region { get; set; }

        bool Show();
    }
}
