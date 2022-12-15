using Amazon.AWSToolkit.Credentials.Core;

namespace Amazon.AWSToolkit.Beanstalk
{
    // This should not be here. This should be in the AWSToolkit.Beanstalk.Interface assembly in the PluginInterfaces folder.
    // As a workaround to fix issues like IDE-6946 this was put here until IDE-4975
    // can untangle the knot that is the homegrown plugin solution gone awry.
    public interface IBeanstalkViewer
    {
        /// <summary>
        /// Views the specified Beanstalk Environment in a document tab
        /// </summary>
        void ViewEnvironment(string environmentName, AwsConnectionSettings connectionSettings);
    }
}
