using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.S3.Controller;

using log4net;

namespace Amazon.AWSToolkit.S3.View
{
    /// <summary>
    /// Interaction logic for CreatePresignedURLControl.xaml
    /// </summary>
    public partial class CreatePresignedURLControl : BaseAWSControl
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(CreatePresignedURLControl));

        CreatePresignedURLController _controller;

        public CreatePresignedURLControl(CreatePresignedURLController controller)
        {
            this._controller = controller;
            InitializeComponent();
            this.DataContext = this._controller.Model;
        }

        public override string Title => "Create Pre-Signed URL";

        private void onGenerateClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.GenerateURL();
            }
            catch (Exception e)
            {
                LOGGER.Error("Error creating pre-signed URL", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error creating pre-signed URL: " + e.Message);
            }
        }

        private void onCopyClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                System.Windows.Clipboard.SetText(this._controller.Model.FullURL);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error copying pre-signed URL", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error copying pre-signed URL: " + e.Message);
            }
        }

        private void onRequestNavigate(object sender, RequestNavigateEventArgs evnt)
        {
            try
            {
                Process.Start(new ProcessStartInfo(this._controller.Model.FullURL));
                evnt.Handled = true;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error navigating pre-signed URL", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error navigating pre-signed URL: " + e.Message);
            }
        }

    }
}
