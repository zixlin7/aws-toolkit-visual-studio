using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.AWSToolkit.ECS.Model;
using Amazon.AWSToolkit.ECS.Nodes;
using Amazon.AWSToolkit.ECS.View;
using Amazon.ECS.Model;
using log4net;

namespace Amazon.AWSToolkit.ECS.Controller
{
    public class ViewClusterController : FeatureController<ViewClusterModel>
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ViewClusterController));

        ViewClusterControl _control;

        protected override void DisplayView()
        {
            this._control = new ViewClusterControl(this);
            ToolkitFactory.Instance.ShellProvider.OpenInEditor(this._control);
        }

        public void LoadModel()
        {
            var clusterViewModel = this.FeatureViewModel as ClusterViewModel;
            if (clusterViewModel == null)
                throw new InvalidOperationException("Expected ClusterViewModel type for FeatureViewModel");

            try
            {
                if (!clusterViewModel.Cluster.IsLoaded)
                {
                    var request = new DescribeClustersRequest
                    {
                        Clusters = new List<string>
                        {
                            clusterViewModel.Cluster.ClusterArn
                        }
                    };
                    ((Amazon.Runtime.Internal.IAmazonWebServiceRequest)request).AddBeforeRequestHandler(AWSToolkit.Constants.AWSExplorerDescribeUserAgentRequestEventHandler);
                    var response = this.ECSClient.DescribeClusters(request);

                    clusterViewModel.Cluster.LoadFrom(response.Clusters.FirstOrDefault());
                }

                this.Model.Cluster = clusterViewModel.Cluster;
            }
            catch (Exception e)
            {
                var msg = "Failed to query details for cluster with ARN " + clusterViewModel.Cluster.ClusterArn;
                LOGGER.Error(msg, e);
                ToolkitFactory.Instance.ShellProvider.ShowError(msg, "Resource Query Failure");
            }
        }
    }
}
