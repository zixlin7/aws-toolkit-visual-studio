using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.ECS.Nodes
{
    public class FeatureViewMetaNode : AbstractMetaNode
    {
        public ActionHandlerWrapper.ActionHandler OnView
        {
            get;
            set;
        }
    }
}
