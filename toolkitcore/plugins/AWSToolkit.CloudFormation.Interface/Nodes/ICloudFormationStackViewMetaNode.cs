using Amazon.AWSToolkit.Navigator;

namespace Amazon.AWSToolkit.CloudFormation.Nodes
{
    public interface ICloudFormationStackViewMetaNode
    {
        ActionHandlerWrapper.ActionHandler OnDelete { get; }
        ActionHandlerWrapper.ActionHandler OnOpen { get; }
        ActionHandlerWrapper.ActionHandler OnCreateConfig { get; }
    }
}
