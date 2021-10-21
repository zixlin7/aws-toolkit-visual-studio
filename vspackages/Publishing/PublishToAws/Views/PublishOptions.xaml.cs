using System.Windows;
using System.Windows.Controls;

namespace Amazon.AWSToolkit.Publish.Views
{
    /// <summary>
    /// Interaction logic for PublishOptions.xaml
    /// </summary>
    public partial class PublishOptions : UserControl
    {
        public PublishOptions()
        {
            InitializeComponent();
        }
        private void OnOptionsClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.ContextMenu != null)
            {
                button.ContextMenu.DataContext = button.DataContext;
                button.ContextMenu.IsOpen = true;
                e.Handled = true;

            }
        }
    }
}
