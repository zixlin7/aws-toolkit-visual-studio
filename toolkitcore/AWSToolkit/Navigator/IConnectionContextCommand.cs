using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.Navigator
{
    /// (AWS Explorer) context commands that require an AWS Connection (credentials + region).
    /// 
    /// This class is an alternative to <see cref="IContextCommand"/>, where <see cref="Execute"/>
    /// does not require objects from the AWS Explorer
    /// (typically <see cref="Node.IViewModel"/> via <see cref="NavigatorControl"/>).
    public interface IConnectionContextCommand
    {
        ICredentialIdentifier CredentialIdentifier { get; }
        ToolkitRegion Region { get; }

        ActionResults Execute();
    }
}
