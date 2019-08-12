using System.Windows;

namespace Amazon.AWSToolkit.CloudFormation.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for CreateStackReviewPage.xaml
    /// </summary>
    internal partial class CreateStackReviewPage
    {
        public CreateStackReviewPage()
        {
            InitializeComponent();
        }

        public void AddReviewPanel(string reviewPanelHeader, FrameworkElement reviewPanel)
        {
            this._reviewPanelsContainer.AddReviewPanel(reviewPanelHeader, reviewPanel);
        }

        public void ClearPanels()
        {
            this._reviewPanelsContainer.ClearPanels();
        }

    }
}
