using Amazon.AWSToolkit.Credentials.Core;

namespace Amazon.AWSToolkit.CloudWatch
{
    /// <summary>
    /// Entry-point for other AWS Resources to view associated log events
    /// Note: This should not be here. This should be in the AWSToolkit.CloudWatch.Interface assembly in the PluginInterfaces folder.
    /// As a workaround to fix IDE-6946 this was put here until IDE-4975
    /// can untangle the knot that is the homegrown plugin solution gone awry.
    /// </summary>
    public interface ILogEventsViewer
    {
        void View(string logGroup, string logStream, AwsConnectionSettings connectionSettings);
    }
}
