using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

        public bool LaunchRDSInstancesWindow
        {
            get { return _launchInstancesWindow.IsChecked == true; }
        }
    }
}
