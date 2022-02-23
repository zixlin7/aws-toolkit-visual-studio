using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Viewers
{
    public interface IBeanstalkEnvironmentViewer
    {
        void View(string environmentName, ICredentialIdentifier identifier, ToolkitRegion region);
    }
}
