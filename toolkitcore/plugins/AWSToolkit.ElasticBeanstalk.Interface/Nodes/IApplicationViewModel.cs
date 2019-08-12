using Amazon.AWSToolkit.Navigator.Node;

using Amazon.ElasticBeanstalk;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Nodes
{
    public interface IApplicationViewModel : IViewModel
    {
        IAmazonElasticBeanstalk BeanstalkClient { get; }
    }
}
