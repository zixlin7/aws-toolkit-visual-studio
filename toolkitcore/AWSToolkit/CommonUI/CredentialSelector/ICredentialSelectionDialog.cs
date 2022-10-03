using System;
using System.Collections.Generic;

using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.CommonUI.CredentialSelector
{
    public interface ICredentialSelectionDialog : IDisposable
    {
        bool IncludeLocalRegions { get; set; }

        IList<AwsConnectionType> ConnectionTypes { get; set; }

        ICredentialIdentifier CredentialIdentifier { get; set; }

        ToolkitRegion Region { get; set; }

        bool Show();
    }
}
