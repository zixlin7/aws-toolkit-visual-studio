using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.SNS.Nodes;
using Amazon.AWSToolkit.SNS.View;
using Amazon.AWSToolkit.SNS.Model;
using Amazon.AWSToolkit;

using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace Amazon.AWSToolkit.SNS.Controller
{
    public class CreateTopicController : BaseContextCommand
    {
        CreateTopicControl _control;
        CreateTopicModel _model;
        SNSRootViewModel _rootModel;
        ActionResults _results;

        public override ActionResults Execute(IViewModel model)
        {
            this._rootModel = model as SNSRootViewModel;
            if (this._rootModel == null)
                return new ActionResults().WithSuccess(false);

            this._model = new CreateTopicModel();
            this._control = new CreateTopicControl(this);
            ToolkitFactory.Instance.ShellProvider.ShowModal(this._control);

            if (this._results == null)
                return new ActionResults().WithSuccess(false);
            return this._results;
        }

        public CreateTopicModel Model
        {
            get { return this._model; }
        }

        public void Persist()
        {
            try
            {
                var request = new CreateTopicRequest() { Name = this._model.TopicName };

                var createResponse = this._rootModel.SNSClient.CreateTopic(request);
                this._model.TopicARN = createResponse.TopicArn;

                this._results = new ActionResults()
                    .WithSuccess(true)
                    .WithFocalname(this.Model.TopicName)
                    .WithParameters(new KeyValuePair<string, object>("CreatedTopic", createResponse.TopicArn))
                    .WithShouldRefresh(true);
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error creating bucket: " + e.Message);
                this._results = new ActionResults().WithSuccess(false);
            }
        }
    }
}
