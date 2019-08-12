using System.Windows;

namespace Amazon.AWSToolkit.RDS.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for ModifyDBInstanceReviewPage.xaml
    /// </summary>
    public partial class ModifyDBInstanceReviewPage
    {
        public ModifyDBInstanceReviewPage()
        {
            InitializeComponent();
        }

        public bool ApplyImmediately => this._btnApplyImmediately.IsChecked == true;

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
