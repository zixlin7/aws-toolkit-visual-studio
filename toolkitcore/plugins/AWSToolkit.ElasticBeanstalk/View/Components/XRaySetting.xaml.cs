using System.Windows.Navigation;

namespace Amazon.AWSToolkit.ElasticBeanstalk.View.Components
{
    /// <summary>
    /// Interaction logic for XRaySetting.xaml
    /// </summary>
    public partial class XRaySetting
    {
        public XRaySetting()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            ToolkitFactory.Instance.ShellProvider.OpenInBrowser(e.Uri.ToString(), false);
        }
    }
}
