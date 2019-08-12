using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Nodes
{
    public interface IApplicationViewMetaNode : IMetaNode
    {
        ActionHandlerWrapper.ActionHandler OnApplicationStatus { get; set; }
        ActionHandlerWrapper.ActionHandler OnDeleteApplication { get; set; }
    }
}
