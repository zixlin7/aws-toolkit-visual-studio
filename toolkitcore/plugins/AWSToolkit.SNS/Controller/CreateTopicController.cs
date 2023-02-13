using System;
using System.Collections.Generic;
using System.Windows;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.SNS.Nodes;
using Amazon.AWSToolkit.SNS.View;
using Amazon.AWSToolkit.SNS.Model;
using Amazon.SimpleNotificationService.Model;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.SNS.Util;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Telemetry;

namespace Amazon.AWSToolkit.SNS.Controller
{
    public class CreateTopicController : BaseContextCommand
    {
        private readonly ToolkitContext _toolkitContext;
        private CreateTopicControl _control;
        private CreateTopicModel _model;
        private SNSRootViewModel _rootModel;

        public CreateTopicController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public override ActionResults Execute(IViewModel model)
        {
            ActionResults actionResults = null;

            void Invoke() => actionResults = CreateTopic(model);

            void Record(ITelemetryLogger _) => RecordMetric(actionResults);

            _toolkitContext.TelemetryLogger.InvokeAndRecord(Invoke, Record);
            return actionResults;
        }

        private ActionResults CreateTopic(IViewModel model)
        {
            _rootModel = model as SNSRootViewModel;
            if (_rootModel == null)
            {
                return ActionResults.CreateFailed(new ToolkitException("Unable to find SNS Topic data",
                    ToolkitException.CommonErrorCode.InternalMissingServiceState));
            }

            _model = new CreateTopicModel();
            _control = new CreateTopicControl(this);
            
            if (!_toolkitContext.ToolkitHost.ShowInModalDialogWindow(_control, MessageBoxButton.OKCancel))
            {
                return ActionResults.CreateCancelled();
            }

            return Persist();
        }

        public CreateTopicModel Model => _model;

        private ActionResults Persist()
        {
            try
            {
                var request = new CreateTopicRequest() { Name = _model.TopicName };

                var createResponse = _rootModel.SNSClient.CreateTopic(request);

                return new ActionResults()
                    .WithSuccess(true)
                    .WithFocalname(Model.TopicName)
                    .WithParameters(new KeyValuePair<string, object>("CreatedTopic", createResponse.TopicArn))
                    .WithShouldRefresh(true);
            }
            catch (Exception e)
            {
                _toolkitContext.ToolkitHost.ShowError($"Error creating SNS Topic:{Environment.NewLine}{e.Message}");
                return ActionResults.CreateFailed(e);
            }
        }

        public void RecordMetric(ActionResults result)
        {
            var awsConnectionSettings = _rootModel?.AwsConnectionSettings;
            _toolkitContext.RecordSnsCreateTopic(result, awsConnectionSettings);
        }
    }
}
