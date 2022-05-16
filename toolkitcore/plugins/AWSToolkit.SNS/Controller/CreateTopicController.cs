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
            _rootModel = model as SNSRootViewModel;
            if (_rootModel == null)
            {
                return new ActionResults().WithSuccess(false);
            }

            _model = new CreateTopicModel();
            _control = new CreateTopicControl(this);

            if (!_toolkitContext.ToolkitHost.ShowInModalDialogWindow(_control, MessageBoxButton.OKCancel))
            {
                return new ActionResults()
                    .WithCancelled(true)
                    .WithSuccess(false);
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
                return new ActionResults().WithSuccess(false);
            }
        }
    }
}
