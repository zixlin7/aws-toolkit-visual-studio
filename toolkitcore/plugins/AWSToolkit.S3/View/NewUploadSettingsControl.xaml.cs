using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.S3.Controller;

namespace Amazon.AWSToolkit.S3.View
{
    /// <summary>
    /// Interaction logic for NewUploadSettingsControl.xaml
    /// </summary>
    public partial class NewUploadSettingsControl : BaseAWSControl
    {
        NewUploadSettingsController _controller;

        public NewUploadSettingsControl(NewUploadSettingsController controller)
        {
            this._controller = controller;
            this.DataContext = this._controller.Model;
            InitializeComponent();
        }

        public override string Title => "Upload Settings";
    }
}
