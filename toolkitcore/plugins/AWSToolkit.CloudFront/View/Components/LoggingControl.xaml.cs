using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using Amazon.AWSToolkit.CloudFront.Controller;

namespace Amazon.AWSToolkit.CloudFront.View.Components
{
    /// <summary>
    /// Interaction logic for LoggingControl.xaml
    /// </summary>
    public partial class LoggingControl
    {
        BaseDistributionConfigEditorController _controller;
        public LoggingControl()
        {
            InitializeComponent();
        }

        public void Initialize(BaseDistributionConfigEditorController controller)
        {
            this._controller = controller;

            if (this._controller is CreateStreamingDistributionController || this._controller is EditStreamingDistributionConfigController)
            {
                this._ctlIsLoggingCookies.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        protected void onCreateLoggingBucketClick(object sender, RoutedEventArgs e)
        {
            this._controller.CreateLoggingBucket();
        }

        void onRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            string url = "https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/AccessLogs.html";
            Process.Start(new ProcessStartInfo(url));
            e.Handled = true;
        }
    }
}
