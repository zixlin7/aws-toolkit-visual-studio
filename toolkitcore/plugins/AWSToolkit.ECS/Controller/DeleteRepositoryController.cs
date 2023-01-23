using System;
using System.Windows;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.ECS.Nodes;
using Amazon.AWSToolkit.ECS.Util;
using Amazon.AWSToolkit.ECS.View;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.ECR.Model;

namespace Amazon.AWSToolkit.ECS.Controller
{
    public class DeleteRepositoryController : BaseContextCommand
    {
        private readonly ToolkitContext _toolkitContext;

        public DeleteRepositoryController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public override ActionResults Execute(IViewModel model)
        {
            var result = DeleteRepository(model);
            RecordMetric(result, model);
            return result;
        }

        public ActionResults DeleteRepository(IViewModel model)
        {
            var repositoryModel = model as RepositoryViewModel;
            if (repositoryModel == null)
            {
                return ActionResults.CreateFailed(new ToolkitException("Unable to find IAM group data",
                            ToolkitException.CommonErrorCode.InternalMissingServiceState));
            }


            var control = new DeleteRepositoryControl(repositoryModel.Name);
            if (!_toolkitContext.ToolkitHost.ShowModal(control, MessageBoxButton.YesNo))
            {
                return ActionResults.CreateCancelled();
            }
            try
            {
                repositoryModel.ECRClient.DeleteRepository(new DeleteRepositoryRequest
                {
                    RepositoryName = repositoryModel.RepositoryName,
                    Force = control.ForceDelete
                });

                repositoryModel.RootViewModel.RemoveRepositoryInstance(repositoryModel.RepositoryName);
                return new ActionResults().WithSuccess(true);
            }
            catch (Exception e)
            {
                _toolkitContext.ToolkitHost.ShowError($"Error deleting repository:{Environment.NewLine}{e.Message}");
                return ActionResults.CreateFailed(e);
            }
        }

        private void RecordMetric(ActionResults result, IViewModel model)
        {
            var viewModel = model as RepositoryViewModel;
            var awsConnectionSettings = viewModel?.RootViewModel?.EcsRootViewModel?.AwsConnectionSettings;
            _toolkitContext.RecordEcrDeleteRepository(result, awsConnectionSettings);
        }
    }
}
