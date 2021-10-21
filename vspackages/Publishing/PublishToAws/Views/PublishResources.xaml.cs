using System;
using System.Windows.Controls;
using System.Windows.Navigation;

using Amazon.AWSToolkit.Publish.ViewModels;

using log4net;

namespace Amazon.AWSToolkit.Publish.Views
{
    /// <summary>
    /// Interaction logic for PublishResources.xaml
    /// </summary>
    public partial class PublishResources : UserControl
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(PublishResources));

        public PublishResources()
        {
            InitializeComponent();
        }

        private void OnResourceLinkNavigate(object sender, RequestNavigateEventArgs e)
        {
            if (!(DataContext is PublishToAwsDocumentViewModel viewModel))
            {
                return;
            }

            try
            {
                viewModel.PublishContext.ToolkitShellProvider.OpenInBrowser(e.Uri.AbsoluteUri, true);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to open url", ex);
                viewModel.PublishContext.ToolkitShellProvider.OutputToHostConsole("Error opening url in browser");
            }
        }
    }
}
