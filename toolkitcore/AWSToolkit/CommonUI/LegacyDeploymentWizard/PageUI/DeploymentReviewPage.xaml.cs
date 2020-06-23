using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.PageUI
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

        // TODO : Switch this to use <BooleanToVisibilityConverter x:Key="Bool2VisConverter" /> in XAML
        public bool IsNETCoreProjectType
        {
            get => this._ctlDotnetCliTools.Visibility == Visibility.Visible;
            set => this._ctlDotnetCliTools.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        }

        public bool SaveBeanstalkTools => this._ctlDotnetCliPersistSettings.IsChecked.GetValueOrDefault();
    }
}
