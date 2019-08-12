using System;
using System.Windows.Threading;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.SNS.Nodes;
using Amazon.AWSToolkit.SNS.View;
using Amazon.AWSToolkit.SNS.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace Amazon.AWSToolkit.SNS.Controller
{
    public class ViewTopicController : BaseContextCommand
    {
        IAmazonSimpleNotificationService _snsClient;
        ViewTopicModel _model;
        Dispatcher _uiDispatcher;
        SNSTopicViewModel _snsTopicModel;
        string _title;
        string _topicARN;


        public override ActionResults Execute(IViewModel model)
        {
            this._snsTopicModel = model as SNSTopicViewModel;
            if (this._snsTopicModel == null)
                return new ActionResults().WithSuccess(false);

            return Execute(this._snsTopicModel.SNSClient, this._snsTopicModel.TopicArn, this._snsTopicModel.Name);
        }

        public ActionResults Execute(IAmazonSimpleNotificationService snsClient, string topicARN, string title)
        {
            try
            {
                this._snsClient = snsClient;
                this._model = new ViewTopicModel();
                this._topicARN = topicARN;
                this._title = title;

                this._model.SubscriptionModel.OwningTopicARN = this._topicARN;

                ViewTopicControl control = new ViewTopicControl(this, this._model);
                control.SetTitle(this._title);
                control.SetUniqueId(topicARN);
                this._uiDispatcher = control.Dispatcher;
                ToolkitFactory.Instance.ShellProvider.OpenInEditor(control);

                return new ActionResults()
                        .WithSuccess(true);
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError(string.Format("Error loading topic {0}: {1}", this._title, e.Message));
                return new ActionResults()
                        .WithSuccess(true);
            }
        }

        public ViewSubscriptionsControl CreateSubscriptionControl()
        {
            try
            {
                var controller = new ViewSubscriptionsController();
                ViewSubscriptionsControl control = controller.CreateSubscriptionEntriesControl(
                    this._snsTopicModel.SNSRootViewModel, this._model.SubscriptionModel, this._topicARN);
                return control;
            }
            catch
            {
                return null;
            }
        }

        public void PublishToTopic()
        {
            try
            {
                PublishController controller = new PublishController(this._snsClient, this._topicARN);
                controller.Execute();
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error publishing to topic: " + e.Message);
            }
        }

        public void EditDisplayName()
        {
            try
            {
                EditDisplayNameModel model = new EditDisplayNameModel(this._model.TopicARN, this._model.DisplayName);
                EditDisplayNameController controller = new EditDisplayNameController(this._snsClient, model);
                if (controller.Execute())
                {
                    this._model.DisplayName = model.DisplayName;
                }
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error publishing to topic: " + e.Message);
            }
        }

        public ViewTopicModel Model => this._model;

        public void LoadModel()
        {
            GetTopicAttributesResponse response = this._snsClient.GetTopicAttributes(
                    new GetTopicAttributesRequest() { TopicArn = this._topicARN });

            this._model.TopicARN = this._topicARN;
            this._model.TopicOwner = response.GetOwner();
            this._model.DisplayName = response.GetDisplayName();
        }
    }
}
