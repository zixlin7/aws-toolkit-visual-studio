using Amazon.AWSToolkit.S3.View;
using Amazon.AWSToolkit.S3.Model;

namespace Amazon.AWSToolkit.S3.Controller
{
    public class NewUploadSettingsController
    {
        NewUploadSettingsModel _model;
        public bool Execute()
        {
            this._model = new NewUploadSettingsModel();
            NewUploadSettingsControl control = new NewUploadSettingsControl(this);
            return ToolkitFactory.Instance.ShellProvider.ShowModal(control);
        }

        public NewUploadSettingsModel Model => this._model;
    }
}
