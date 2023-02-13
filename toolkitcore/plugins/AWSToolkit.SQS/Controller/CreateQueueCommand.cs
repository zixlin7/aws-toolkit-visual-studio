using System;
using System.Collections.Generic;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.SQS.View;
using Amazon.AWSToolkit.SQS.Model;
using Amazon.AWSToolkit.SQS.Nodes;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.SQS.Util;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Telemetry;

namespace Amazon.AWSToolkit.SQS.Controller
{
    public class CreateQueueCommand : BaseContextCommand
    {
        private readonly ToolkitContext _toolkitContext;

        public CreateQueueCommand(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public override ActionResults Execute(IViewModel model)
        {
            ActionResults actionResults = null;

            void Invoke() => actionResults = CreateQueue(model);

            void Record(ITelemetryLogger _)
            {
                var viewModel = model as SQSRootViewModel;
                var awsConnectionSettings = viewModel?.AwsConnectionSettings;
                _toolkitContext.RecordSqsCreateQueue(actionResults, awsConnectionSettings);
            }

            _toolkitContext.TelemetryLogger.InvokeAndRecord(Invoke, Record);
            return actionResults;
        }

        private ActionResults CreateQueue(IViewModel model)
        {
            var queueModel = model as SQSRootViewModel;
            if (queueModel == null)
            {
                return ActionResults.CreateFailed(new ToolkitException("Unable to find SQS Queue data",
                    ToolkitException.CommonErrorCode.InternalMissingServiceState));
            }

            var control = new CreateQueueControl(new CreateQueueControlModel { SQSClient = queueModel.SQSClient });
            if (!_toolkitContext.ToolkitHost.ShowModal(control))
            {
                return ActionResults.CreateCancelled();
            }
            
            CreateQueueResponse response = null;
            try
            {
                var request = new CreateQueueRequest()
                {
                    QueueName = control.Model.QueueName,
                    Attributes = new Dictionary<string, string>() { { "VisibilityTimeout", control.Model.DefaultVisiblityTimeout.ToString() } }
                };

                if (control.Model.DefaultDelaySeconds >= 0)
                {
                    request.Attributes.Add("DelaySeconds", control.Model.DefaultDelaySeconds.ToString());
                }

                if (control.Model.UseRedrivePolicy)
                {
                    var queueArn = queueModel.SQSClient.GetQueueARN(queueModel.Region.Id, control.Model.DeadLetterQueueUrl);
                    var redrivePolicy = $"{{\"maxReceiveCount\":\"{control.Model.MaxReceives}\", \"deadLetterTargetArn\":\"{queueArn}\"}}";
                    request.Attributes.Add("RedrivePolicy", redrivePolicy);
                }

                response = queueModel.SQSClient.CreateQueue(request);
                return new ActionResults()
                    .WithSuccess(true)
                    .WithFocalname(control.Model.QueueName)
                    .WithShouldRefresh(true)
                    .WithParameters(new KeyValuePair<string, object>(SQSActionResultsConstants.PARAM_QUEUE_URL, response.QueueUrl));

            }
            catch (Exception e)
            {
                _toolkitContext.ToolkitHost.ShowError($"Error creating queue:{Environment.NewLine}{e.Message}");
                return ActionResults.CreateFailed(e);
            }
        }
    }
}
