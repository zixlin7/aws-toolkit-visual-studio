using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using Microsoft.Win32;

namespace Amazon.AWSToolkit.CommonUI.DeploymentWizard.PageUI
{
    /// <summary>
    /// Interaction logic for DeploymentReviewPage.xaml
    /// </summary>
    internal partial class DeploymentReviewPage
    {
        public DeploymentReviewPage()
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

        public bool OpenStatusOnClose => this._launchStatusWindow.IsChecked == true;
    }
}
