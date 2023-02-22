using System;
using System.Windows.Threading;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.SNS.Nodes;
using Amazon.AWSToolkit.SNS.View;
using Amazon.AWSToolkit.SNS.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.SNS.Util;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Telemetry;

namespace Amazon.AWSToolkit.SNS.Controller
{
    public class ViewTopicController : BaseContextCommand
    {
        private readonly ToolkitContext _toolkitContext;
        IAmazonSimpleNotificationService _snsClient;
        ViewTopicModel _model;
        Dispatcher _uiDispatcher;
        SNSTopicViewModel _snsTopicModel;
        string _title;
        string _topicARN;

        public ViewTopicController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public override ActionResults Execute(IViewModel model)
        {
            _snsTopicModel = model as SNSTopicViewModel;
            if (_snsTopicModel == null)
            {
                return new ActionResults().WithSuccess(false);
            }
            
            return Execute(_snsTopicModel.SNSClient, _snsTopicModel.TopicArn, _snsTopicModel.Name);
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
                _toolkitContext.ToolkitHost.OpenInEditor(control);

                return new ActionResults()
                        .WithSuccess(true);
            }
            catch (Exception e)
            {
                _toolkitContext.ToolkitHost.ShowError(string.Format("Error loading topic {0}: {1}", this._title, e.Message));
                return new ActionResults()
                        .WithSuccess(true);
            }
        }

        public ViewSubscriptionsControl CreateSubscriptionControl()
        {
            try
            {
                var controller = new ViewSubscriptionsController(_toolkitContext);
                var control = controller.CreateSubscriptionEntriesControl(
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
            ActionResults actionResults = null;

            void Invoke() => actionResults = PublishMessage();

            void Record(ITelemetryLogger _) => RecordPublishMessageMetric(actionResults);

            _toolkitContext.TelemetryLogger.InvokeAndRecord(Invoke, Record);
        }

        private ActionResults PublishMessage()
        {
            try
            {
                var connectionSettings = _snsTopicModel?.SNSRootViewModel?.AwsConnectionSettings;
                var controller = new PublishController(_toolkitContext, connectionSettings ,_snsClient, _topicARN);

                return controller.Execute();
            }
            catch (Exception e)
            {
                _toolkitContext.ToolkitHost.ShowError("Error publishing to topic: " + e.Message);
                return ActionResults.CreateFailed(e);
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
                _toolkitContext.ToolkitHost.ShowError("Error publishing to topic: " + e.Message);
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

        internal void RecordPublishMessageMetric(ActionResults result)
        {
            var connectionSettings = _snsTopicModel?.SNSRootViewModel?.AwsConnectionSettings;
            _toolkitContext.RecordSnsPublishMessage(result, connectionSettings);
        }
    }
}
