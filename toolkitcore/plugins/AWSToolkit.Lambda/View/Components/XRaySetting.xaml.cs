using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Lambda.Controller;
using System.Windows.Navigation;

namespace Amazon.AWSToolkit.Lambda.View.Components
{
    /// <summary>
    /// Interaction logic for XRaySetting.xaml
    /// </summary>
    public partial class XRaySetting
    {

        public ViewFunctionController Controller { get; private set; }


        public XRaySetting()
        {
            InitializeComponent();
            this._ctlImage.DataContext = this;
        }
        public void Initialize(ViewFunctionController controller)
        {
            Controller = controller;
        }

        public System.Windows.Media.ImageSource TraceTimeline => IconHelper.GetIcon(this.GetType().Assembly, "Amazon.AWSToolkit.Lambda.Resources.EmbeddedImages.trace-timeline.png").Source;

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            ToolkitFactory.Instance.ShellProvider.OpenInBrowser(e.Uri.ToString(), false);
        }
    }
}
