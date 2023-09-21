using System.Windows;
using System.Windows.Controls;

namespace Amazon.AwsToolkit.CodeWhisperer.Margins
{
    /// <summary>
    /// The control placed in the CodeWhisperer margin
    /// </summary>
    public partial class CodeWhispererMarginControl : UserControl
    {
        public CodeWhispererMarginControl()
        {
            InitializeComponent();
        }

        private void OnClickButton(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement button)
            {
                button.ContextMenu.IsOpen = true;
            }
        }
    }
}
