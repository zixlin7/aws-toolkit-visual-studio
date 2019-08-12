using System.Windows;

namespace Amazon.AWSToolkit.RDS.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for LaunchDBInstanceReviewPage.xaml
    /// </summary>
    public partial class LaunchDBInstanceReviewPage 
    {
        public LaunchDBInstanceReviewPage()
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

        public bool LaunchRDSInstancesWindow => _launchInstancesWindow.IsChecked == true;
    }
}
