using Amazon.AWSToolkit.Navigator.Node;

using Amazon.ElasticBeanstalk;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Nodes
{
    public interface IElasticBeanstalkRootViewModel : IServiceRootViewModel
    {
        IAmazonElasticBeanstalk BeanstalkClient { get; }

        void RemoveApplication(string applicationName);
    }
}
