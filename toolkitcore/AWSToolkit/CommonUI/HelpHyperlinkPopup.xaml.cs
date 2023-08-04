using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Amazon.AWSToolkit.CommonUI
{
    /// <summary>
    /// Represents a help tooltip with the ability to surface a hyperlink
    /// </summary>
    public partial class HelpHyperlinkPopup : ContentControl
    {
        public HelpHyperlinkPopup()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Represents the content to be presented in the tooltip popup
        /// </summary>
        public DataTemplate HelpContent
        {
            get => (DataTemplate) GetValue(HelpContentProperty);
            set => SetValue(HelpContentProperty, value);
        }

        /// <summary>
        /// Identifies the HelpContent dependency property.
        /// </summary>
        public static readonly DependencyProperty HelpContentProperty = DependencyProperty.Register(
            nameof(HelpContent),
            typeof(DataTemplate),
            typeof(HelpHyperlinkPopup));

        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            ToolTipPopup.IsOpen = true;
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (!ToolTipPopup.IsMouseOver && !HelpAction.IsMouseOver)
            {
                ToolTipPopup.IsOpen = false;
            }
        }
    }
}
