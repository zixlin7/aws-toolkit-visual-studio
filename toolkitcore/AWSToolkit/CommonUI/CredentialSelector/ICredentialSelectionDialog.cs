using System;

using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.CommonUI.CredentialSelector
{
    public interface ICredentialSelectionDialog : IDisposable
    {
        ICredentialIdentifier CredentialIdentifier { get; set; }

        ToolkitRegion Region { get; set; }

        bool Show();
    }
}
