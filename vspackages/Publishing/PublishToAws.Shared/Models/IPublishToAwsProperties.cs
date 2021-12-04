using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.Publish.Models
{
    public interface IPublishToAwsProperties
    {
        ICredentialIdentifier CredentialsId { get; }
        ToolkitRegion Region { get; }
    }
}
