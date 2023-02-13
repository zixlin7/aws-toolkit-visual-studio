using System;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.SNS.Nodes;
using Amazon.AWSToolkit.SNS.Util;
using Amazon.AWSToolkit.Telemetry;
using Amazon.SimpleNotificationService.Model;

namespace Amazon.AWSToolkit.SNS.Controller
{
    public class DeleteTopicController : BaseContextCommand
    {
        private readonly ToolkitContext _toolkitContext;

        public DeleteTopicController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public override ActionResults Execute(IViewModel model)
        {
            ActionResults actionResults = null;

            void Invoke() => actionResults = DeleteTopic(model);

            void Record(ITelemetryLogger _)
            {
                var viewModel = model as SNSTopicViewModel;
                var awsConnectionSettings = viewModel?.SNSRootViewModel?.AwsConnectionSettings;
                _toolkitContext.RecordSnsDeleteTopic(actionResults, awsConnectionSettings);
            }

            _toolkitContext.TelemetryLogger.InvokeAndRecord(Invoke, Record);
            return actionResults;
        }


        private ActionResults DeleteTopic(IViewModel model)
        {
            var topicModel = model as SNSTopicViewModel;
            if (topicModel == null)
            {
                return ActionResults.CreateFailed(new ToolkitException("Unable to find SNS Topic data",
                    ToolkitException.CommonErrorCode.InternalMissingServiceState));
            }

            var msg = $"Are you sure you want to delete the {model.Name} topic?";
            if (!_toolkitContext.ToolkitHost.Confirm("Delete Topic", msg))
            {
                return ActionResults.CreateCancelled();
            }
            
            try
            {
                var request = new DeleteTopicRequest() { TopicArn = topicModel.TopicArn };
                topicModel.SNSClient.DeleteTopic(request);
                return new ActionResults()
                    .WithSuccess(true)
                    .WithFocalname(model.Name)
                    .WithShouldRefresh(true);
            }
            catch (Exception e)
            {
                _toolkitContext.ToolkitHost.ShowError("Error deleting topic: " + e.Message);
                return ActionResults.CreateFailed(e);
            }
        }
    }
}
