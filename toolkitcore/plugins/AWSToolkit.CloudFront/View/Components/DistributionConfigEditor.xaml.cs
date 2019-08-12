using Amazon.AWSToolkit.CloudFront.Controller;

namespace Amazon.AWSToolkit.CloudFront.View.Components
{
    /// <summary>
    /// Interaction logic for DistributionConfigEditor.xaml
    /// </summary>
    public partial class DistributionConfigEditor
    {
        BaseDistributionConfigEditorController _controller;
        public DistributionConfigEditor()
        {
            InitializeComponent();
        }

        public void Initialize(BaseDistributionConfigEditorController controller)
        {
            this._controller = controller;
            this._ctlLogging.Initialize(controller);
            this._ctlS3Origin.Initialize(controller);
            this._ctlTrustedSigners.Initialize(controller);
        }
    }
}
