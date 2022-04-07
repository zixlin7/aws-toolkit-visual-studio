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
        public AwsConnectionSettings ConnectionSettings { get; }

        protected readonly ToolkitContext _toolkitContext;

        protected BaseConnectionContextCommand(ToolkitContext toolkitContext, AwsConnectionSettings connectionSettings)
        {
            _toolkitContext = toolkitContext;
            ConnectionSettings = connectionSettings;
        }

        public abstract ActionResults Execute();
    }
}
