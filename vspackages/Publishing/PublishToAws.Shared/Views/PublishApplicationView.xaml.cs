using System.Windows;
using System.Windows.Controls;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Publish.ViewModels;

namespace Amazon.AWSToolkit.Publish.Views
{
    /// <summary>
    /// View panel that is shown while a publish is in progress.
    /// Data bound to <see cref="PublishProjectViewModel"/>
    /// </summary>
    public partial class PublishApplicationView : UserControl
    {
        public PublishApplicationView()
        {
            InitializeComponent();
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!(sender is DependencyObject control))
            {
                return;
            }

            var scrollViewer = UIUtils.FindVisualParent<ScrollViewer>(control);

            scrollViewer?.ScrollToEnd();
        }
    }
}
