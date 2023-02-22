using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.SNS.View;
using Amazon.AWSToolkit.SNS.Model;
using Amazon.AWSToolkit.SNS.Util;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace Amazon.AWSToolkit.SNS.Controller
{
    public class PublishController
    {
        private readonly AwsConnectionSettings _connectionSettings;
        private readonly IAmazonSimpleNotificationService _snsClient;
        private readonly PublishModel _model;

        public PublishController(ToolkitContext toolkitContext, AwsConnectionSettings connectionSettings,
            IAmazonSimpleNotificationService snsClient, string topicARN)
        {
            ToolkitContext = toolkitContext;
            _connectionSettings = connectionSettings;
            _snsClient = snsClient;
            _model = new PublishModel(topicARN);
        }

        public PublishModel Model => _model;

        public ToolkitContext ToolkitContext { get; }

        public ActionResults Execute()
        {
            var control = new PublishControl(this);
            var result = ToolkitContext.ToolkitHost.ShowModal(control);
            return result ? new ActionResults().WithSuccess(true) : ActionResults.CreateCancelled();
        }

        public void Persist()
        {
            _snsClient.Publish(new PublishRequest()
            {
                TopicArn = _model.TopicARN, Subject = _model.Subject, Message = _model.Message
            });
        }

        internal void RecordMetric(ActionResults result)
        {
            ToolkitContext.RecordSnsPublishMessage(result, _connectionSettings);
        }
    }
}
