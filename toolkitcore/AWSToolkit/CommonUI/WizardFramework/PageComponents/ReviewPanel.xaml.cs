using System.Windows;
using System.Windows.Controls;

namespace Amazon.AWSToolkit.CommonUI.WizardFramework.PageComponents
{
    /// <summary>
    /// Interaction logic for ReviewPanel.xaml
    /// </summary>
    public partial class ReviewPanel
    {
        public ReviewPanel()
        {
            InitializeComponent();
        }

        public string PanelHeader
        {
            set { this._reviewSectionHeader.Text = value; }
        }

        public FrameworkElement PanelContent
        {
            set { this._reviewSectionContent.Content = value; }
        }
    }
}
