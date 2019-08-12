using Amazon.AWSToolkit.SNS.View;
using Amazon.AWSToolkit.SNS.Model;
using Amazon.SimpleNotificationService;

namespace Amazon.AWSToolkit.SNS.Controller
{
    public class EditDisplayNameController
    {
        IAmazonSimpleNotificationService _snsClient;
        EditDisplayNameModel _model;

        public EditDisplayNameController(IAmazonSimpleNotificationService snsClient, string topicARN)
            : this(snsClient, new EditDisplayNameModel())
        {
            this._model.TopicARN = topicARN;
        }

        public EditDisplayNameController(IAmazonSimpleNotificationService snsClient, EditDisplayNameModel model)
        {
            this._snsClient = snsClient;
            this._model = model;
        }

        public EditDisplayNameModel Model => this._model;

        public bool Execute()
        {
            var control = new EditDisplayNameControl(this);
            return ToolkitFactory.Instance.ShellProvider.ShowModal(control);
        }

        public void Persist()
        {
            this._snsClient.SetDisplayName(this._model.TopicARN, this._model.DisplayName);
        }

    }
}
