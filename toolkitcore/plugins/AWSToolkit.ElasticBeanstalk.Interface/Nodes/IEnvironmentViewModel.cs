using Amazon.AWSToolkit.Navigator.Node;

using Amazon.ElasticBeanstalk;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Nodes
{
    public interface IEnvironmentViewModel : IViewModel
    {
        IAmazonElasticBeanstalk BeanstalkClient { get; }
    }
}
