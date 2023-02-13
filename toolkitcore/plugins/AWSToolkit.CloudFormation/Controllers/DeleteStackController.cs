using System;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.CloudFormation.Nodes;
using Amazon.AWSToolkit.CloudFormation.Util;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Telemetry;
using Amazon.CloudFormation.Model;

namespace Amazon.AWSToolkit.CloudFormation.Controllers
{
    public class DeleteStackController : BaseContextCommand
    {
        private readonly ToolkitContext _toolkitContext;

        public DeleteStackController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public override ActionResults Execute(IViewModel model)
        {
            ActionResults actionResults = null;

            void Invoke() => actionResults = DeleteStack(model);

            void Record(ITelemetryLogger _)
            {
                var viewModel = model as CloudFormationStackViewModel;
                var awsConnectionSettings = viewModel?.CloudFormationRootViewModel?.AwsConnectionSettings;
                _toolkitContext.RecordCloudFormationDelete(actionResults, awsConnectionSettings);
            }

            _toolkitContext.TelemetryLogger.InvokeAndRecord(Invoke, Record);
            return actionResults;
        }

        private ActionResults DeleteStack(IViewModel model)
        {
            var stackModel = model as CloudFormationStackViewModel;
            if (stackModel == null)
            {
                return ActionResults.CreateFailed(new ToolkitException("Unable to find CloudFormation stack data",
                    ToolkitException.CommonErrorCode.InternalMissingServiceState));
            }

            var msg = $"Are you sure you want to delete the {model.Name} stack?";
            if (!_toolkitContext.ToolkitHost.Confirm("Delete Stack", msg))
            {
                return ActionResults.CreateCancelled();
            }

            try
            {
                var request = new DeleteStackRequest() { StackName = stackModel.StackName };
                stackModel.CloudFormationClient.DeleteStack(request);
                return new ActionResults()
                    .WithSuccess(true)
                    .WithFocalname(model.Name)
                    .WithShouldRefresh(true);
            }
            catch (Exception e)
            {
                _toolkitContext.ToolkitHost.ShowError($"Error deleting stack:{Environment.NewLine}{e.Message}");
                return ActionResults.CreateFailed(e);
            }

        }
    }
}
