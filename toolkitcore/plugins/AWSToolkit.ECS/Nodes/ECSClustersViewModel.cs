using Amazon.AWSToolkit.ECS.Model;
using log4net;

namespace Amazon.AWSToolkit.ECS.Nodes
{
    public class ECSClustersViewModel : FeatureViewModel, IECSClustersViewModel
    {
        readonly static ILog LOGGER = LogManager.GetLogger(typeof(ECSClustersViewModel));

        public ECSClustersViewModel(ECSClustersViewMetaNode metaNode, ECSRootViewModel viewModel)
            : base(metaNode, viewModel, "Clusters")
        {
        }

        protected override string IconName
        {
            get
            {
                return "Amazon.AWSToolkit.ECS.Resources.EmbeddedImages.clusters.png";
            }
        }

        public override string ToolTip
        {
            get
            {
                return "Manage clusters of Amazon EC2 instances running your containers";
            }
        }

    }
}
