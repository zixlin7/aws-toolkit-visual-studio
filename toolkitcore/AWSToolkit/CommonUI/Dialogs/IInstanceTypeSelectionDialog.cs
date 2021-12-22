using System.Collections.Generic;

using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.CommonUI.Dialogs
{
    public interface IInstanceTypeSelectionDialog
    {
        /// <summary>
        /// ID of the selected EC2 Instance Type
        /// </summary>
        string InstanceTypeId { get; set; }

        /// <summary>
        /// User-modifiable text to show only entries matching this value
        /// </summary>
        string Filter { get; set; }

        /// <summary>
        /// Restricts which instances are shown. Not user-modifiable.
        /// Leave empty to show all instance types (default).
        /// </summary>
        IList<string> Architectures { get; }

        /// <summary>
        /// Credentials of account to show Instance Types for
        /// </summary>
        ICredentialIdentifier CredentialsId { get; set; }

        /// <summary>
        /// Region to associate the AWS SDK Client with
        /// </summary>
        ToolkitRegion Region { get; set; }

        bool Show();
    }
}
