using System;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Lambda.Nodes;
using Amazon.AWSToolkit.Context;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Telemetry;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Lambda.Util;

namespace Amazon.AWSToolkit.Lambda.Controller
{
    public class DeleteFunctionController : BaseContextCommand
    {
        private readonly ToolkitContext _toolkitContext;

        public DeleteFunctionController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public override ActionResults Execute(IViewModel model)
        {
            ActionResults actionResults = null;

            void Invoke() => actionResults = DeleteFunction(model);

            void Record(ITelemetryLogger _)
            {
                var viewModel = model as LambdaFunctionViewModel;
                var awsConnectionSettings = viewModel?.LambdaRootViewModel?.AwsConnectionSettings;
                _toolkitContext.RecordLambdaDelete(actionResults, awsConnectionSettings);
            }

            _toolkitContext.TelemetryLogger.InvokeAndRecord(Invoke, Record);
            return actionResults;
        }

        private ActionResults DeleteFunction(IViewModel model)
        {
            var functionModel = model as LambdaFunctionViewModel;
            if (functionModel == null)
            {
                return ActionResults.CreateFailed(new ToolkitException("Unable to find Lambda function data",
                    ToolkitException.CommonErrorCode.InternalMissingServiceState));
            }

            var msg = $"Are you sure you want to delete the {model.Name} cloud function?";

            if (!_toolkitContext.ToolkitHost.Confirm("Delete Function", msg))
            {
                return ActionResults.CreateCancelled();
            }
            
            try
            {
                functionModel.LambdaClient.DeleteFunction(functionModel.Name);
                return new ActionResults()
                    .WithSuccess(true)
                    .WithFocalname(model.Name)
                    .WithShouldRefresh(true);
            }
            catch (Exception e)
            {
                _toolkitContext.ToolkitHost.ShowError($"Error deleting function:{Environment.NewLine}{e.Message}");
                return ActionResults.CreateFailed(e);
            }
        }
    }
}
