using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AwsToolkit.CodeWhisperer.Credentials
{
    /// <summary>
    /// Represented properties associated with a <see cref="Connection"/>
    /// </summary>
    public class ConnectionProperties
    {
        public ICredentialIdentifier CredentialIdentifier { get; set; }

        public ToolkitRegion SsoRegion { get; set; }

        public string SsoStartUrl { get; set; }
    }
}
