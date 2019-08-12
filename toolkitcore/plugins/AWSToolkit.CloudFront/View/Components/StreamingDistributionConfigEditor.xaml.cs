using Amazon.AWSToolkit.CloudFront.Controller;

namespace Amazon.AWSToolkit.CloudFront.View.Components
{
    /// <summary>
    /// Interaction logic for StreamingDistributionConfigEditor.xaml
    /// </summary>
    public partial class StreamingDistributionConfigEditor
    {
        BaseDistributionConfigEditorController _controller;
        public StreamingDistributionConfigEditor()
        {
            InitializeComponent();
        }

        public void Initialize(BaseDistributionConfigEditorController controller)
        {
            this._controller = controller;
            this._ctlLogging.Initialize(controller);
            this._ctlTrustedSigners.Initialize(controller);
        }
    }
}
