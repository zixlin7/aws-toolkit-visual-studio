using System.Windows;
using System.Windows.Controls;

namespace Amazon.AwsToolkit.CodeWhisperer.Margins
{
    /// <summary>
    /// The control placed in the CodeWhisperer margin
    /// </summary>
    public partial class CodeWhispererMarginControl : UserControl
    {
        private CodeWhispererMarginViewModel _viewModel => DataContext as CodeWhispererMarginViewModel;

        public CodeWhispererMarginControl()
        {
            InitializeComponent();
        }

        private void OnPreviewMouseDown(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                if (button.ContextMenu != null)
                {
                    _viewModel.UpdateKeyBindings();
                    button.ContextMenu.PlacementTarget = button;
                    button.ContextMenu.IsOpen = true;
                }

                e.Handled = true;
            }
        }

    }
}
