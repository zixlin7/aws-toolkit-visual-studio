using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.Navigator
{
    /// <summary>
    /// Base class for commands that require an AWS Connection (credentials + region).
    /// 
    /// This class is an alternative to <see cref="BaseContextCommand"/>, where <see cref="Execute"/>
    /// does not require objects from the AWS Explorer
    /// (<see cref="Node.IViewModel"/> via <see cref="NavigatorControl"/>).
    /// </summary>
    public abstract class BaseConnectionContextCommand : IConnectionContextCommand
    {
        public ICredentialIdentifier CredentialIdentifier { get; }
        public ToolkitRegion Region { get; }

        protected readonly ToolkitContext _toolkitContext;

        protected BaseConnectionContextCommand(ToolkitContext toolkitContext, ICredentialIdentifier credentialIdentifier, ToolkitRegion region)
        {
            _toolkitContext = toolkitContext;
            CredentialIdentifier = credentialIdentifier;
            Region = region;
        }

        public abstract ActionResults Execute();
    }
}
