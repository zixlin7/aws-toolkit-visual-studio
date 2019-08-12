using Amazon.S3;
using Amazon.AWSToolkit.S3.Model;
using Amazon.AWSToolkit.S3.View;

namespace Amazon.AWSToolkit.S3.Controller
{
    public class RestoreObjectPromptController
    {
        IAmazonS3 _s3Client;
        RestoreObjectPromptModel _model;

        public RestoreObjectPromptController(IAmazonS3 s3Client)
        {
            this._s3Client = s3Client;
            this._model = new RestoreObjectPromptModel();
        }

        public RestoreObjectPromptModel Model => this._model;

        public bool Execute()
        {
            var control = new RestoreObjectPromptControl(this);
            return ToolkitFactory.Instance.ShellProvider.ShowModal(control);
        }
    }
}
