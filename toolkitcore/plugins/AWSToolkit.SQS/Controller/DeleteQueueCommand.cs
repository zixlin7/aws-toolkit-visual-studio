using System;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.SQS.Nodes;
using Amazon.AWSToolkit.SQS.Util;
using Amazon.AWSToolkit.Telemetry;
using Amazon.SQS.Model;

namespace Amazon.AWSToolkit.SQS.Controller
{
    public class DeleteQueueCommand : BaseContextCommand
    {
        private readonly ToolkitContext _toolkitContext;

        public DeleteQueueCommand(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public override ActionResults Execute(IViewModel model)
        {
            ActionResults actionResults = null;

            void Invoke() => actionResults = DeleteQueue(model);

            void Record(ITelemetryLogger _)
            {
                var viewModel = model as SQSQueueViewModel;
                var awsConnectionSettings = viewModel?.SQSRootViewModel?.AwsConnectionSettings;
                _toolkitContext.RecordSqsDeleteQueue(actionResults, awsConnectionSettings);
            }

            _toolkitContext.TelemetryLogger.InvokeAndRecord(Invoke, Record);
            return actionResults;
        }

        private ActionResults DeleteQueue(IViewModel model)
        {
            var queueModel = model as SQSQueueViewModel;
            if (queueModel == null)
            {
                return ActionResults.CreateFailed(new ToolkitException("Unable to find SQS Queue data",
                    ToolkitException.CommonErrorCode.InternalMissingServiceState));
            }

            var msg = $"Are you sure you want to delete the {model.Name} queue?";
            if (!_toolkitContext.ToolkitHost.Confirm("Confirm SQS Queue Delete", msg))
            {
                return ActionResults.CreateCancelled();
            }
            
            try
            {
                queueModel.SQSClient.DeleteQueue(new DeleteQueueRequest() { QueueUrl = queueModel.QueueUrl });
                return new ActionResults().WithSuccess(true);
            }
            catch (Exception e)
            {
                _toolkitContext.ToolkitHost.ShowError($"Error deleting queue {model.Name}:{Environment.NewLine}{e.Message}");
                return ActionResults.CreateFailed(e);
            }
        }
    }
}
