using System;
using System.Windows;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.ECS.Nodes;
using Amazon.AWSToolkit.ECS.Util;
using Amazon.AWSToolkit.ECS.View;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Telemetry;
using Amazon.ECS.Model;

namespace Amazon.AWSToolkit.ECS.Controller
{
    public class DeleteClusterController : BaseContextCommand
    {
        private readonly ToolkitContext _toolkitContext;

        public DeleteClusterController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public override ActionResults Execute(IViewModel model)
        {
            ActionResults actionResults = null;

            void Invoke() => actionResults = DeleteCluster(model);

            void Record(ITelemetryLogger _)
            {
                var viewModel = model as ClusterViewModel;
                var awsConnectionSettings = viewModel?.RootViewModel?.EcsRootViewModel?.AwsConnectionSettings;
                _toolkitContext.RecordEcsDeleteCluster(actionResults, awsConnectionSettings);
            }

            _toolkitContext.TelemetryLogger.InvokeAndRecord(Invoke, Record);
            return actionResults;
        }

        public ActionResults DeleteCluster(IViewModel model)
        {
            var clusterModel = model as ClusterViewModel;
            if (clusterModel == null)
            {
                return ActionResults.CreateFailed(new ToolkitException("Unable to find ECS cluster data",
                        ToolkitException.CommonErrorCode.InternalMissingServiceState));
            }

            var control = new DeleteClusterControl(clusterModel.Name);
            if (!_toolkitContext.ToolkitHost.ShowModal(control, MessageBoxButton.YesNo))
            {
                return ActionResults.CreateCancelled();
            }

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
                _toolkitContext.ToolkitHost.ShowError($"Error deleting cluster:{Environment.NewLine}{e.Message}");
                return ActionResults.CreateFailed(e);
            }
        }
    }
}
