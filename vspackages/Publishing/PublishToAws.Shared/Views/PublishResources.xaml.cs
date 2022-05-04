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
            if (!(DataContext is PublishProjectViewModel viewModel))
            {
                return;
            }

            try
            {
                viewModel.OpenUrlCommand.Execute(e.Uri.AbsoluteUri);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to open url", ex);
            }
        }
    }
}
