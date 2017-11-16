using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Amazon.AWSToolkit.ECS.Nodes;
using Amazon.AWSToolkit.ECS.View;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.ECS.Model;

namespace Amazon.AWSToolkit.ECS.Controller
{
    public class DeleteClusterController : BaseContextCommand
    {
        public override ActionResults Execute(IViewModel model)
        {
            var clusterModel = model as ClusterViewModel;
            if (clusterModel == null)
                return new ActionResults().WithSuccess(false);

            var control = new DeleteClusterControl(clusterModel.Name);
            if (ToolkitFactory.Instance.ShellProvider.ShowModal(control, MessageBoxButton.YesNo))
            {
                try
                {
                    clusterModel.ECSClient.DeleteCluster(new DeleteClusterRequest
                    {
                        Cluster = clusterModel.Name
                    });

                    clusterModel.RootViewModel.RemoveClusterInstance(clusterModel.Name);
                    return new ActionResults().WithSuccess(true);
                }
                catch (Exception e)
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("Error deleting cluster: " + e.Message);
                }
            }

            return new ActionResults().WithSuccess(false);
        }
    }
}
