using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.ECS.Nodes
{
    public class ECSFeatureViewMetaNode : AbstractMetaNode
    {
        public ActionHandlerWrapper.ActionHandler OnView
        {
            get;
            set;
        }
    }
}
