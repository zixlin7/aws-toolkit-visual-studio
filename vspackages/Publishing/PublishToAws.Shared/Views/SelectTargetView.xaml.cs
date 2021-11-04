using System.Windows;
using System.Windows.Controls;

using Amazon.AWSToolkit.Publish.ViewModels;

namespace Amazon.AWSToolkit.Publish.Views
{
    /// <summary>
    /// View panel that is shown when a user is selecting their publish target.
    /// Data bound to <see cref="PublishToAwsDocumentViewModel"/>
    /// </summary>
    public partial class SelectTargetView : UserControl
    {
        public SelectTargetView()
        {
            InitializeComponent();
        }

        private void OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (((UIElement) sender).Visibility == Visibility.Visible)
            {
                if (!(DataContext is PublishToAwsDocumentViewModel viewModel))
                {
                    return;
                }
            }
        }
    }
}
