using System.Windows;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Publish.ViewModels;

namespace Amazon.AWSToolkit.Publish.Views
{
    /// <summary>
    /// Interaction logic for ReenableOldPublishDialog.xaml
    /// </summary>
    public partial class ReenableOldPublishDialog : BaseAWSControl
    {
        public ReenableOldPublishDialog(PublishToAwsDocumentViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        public override string Title => "Previous AWS publishing experience is now available";
    }
}
