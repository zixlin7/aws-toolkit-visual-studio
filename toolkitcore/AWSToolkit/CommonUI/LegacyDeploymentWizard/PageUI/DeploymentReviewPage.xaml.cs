using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using Microsoft.Win32;

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

        public bool OpenStatusOnClose
        {
            get { return this._launchStatusWindow.IsChecked == true; }
        }

        public bool IsNETCoreProjectType
        {
            get
            {
                return this._ctlDotnetCliTools.Visibility == Visibility.Visible;
            }
            set
            {
                if(value)
                {
                    this._ctlAWSDeployPanel.Visibility = Visibility.Collapsed;
                    this._ctlDotnetCliTools.Visibility = Visibility.Visible;
                }
                else
                {
                    this._ctlAWSDeployPanel.Visibility = Visibility.Visible;
                    this._ctlDotnetCliTools.Visibility = Visibility.Collapsed;
                }
            }
        }


        public string ConfigFileDestination
        {
            get
            {
                if (this._generateConfiguration.IsChecked.GetValueOrDefault())
                {
                    return this._configurationPath.Text;
                }
                return string.Empty;
            }
        }

        public bool SaveBeanstalkTools
        {
            get
            {
                return this._ctlDotnetCliPersistSettings.IsChecked.GetValueOrDefault();
            }
        }

        private void Browse(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Title = "Save Configuration to File",
                Filter = "Text Files|*.txt|All Files|*.*",
                FileName = this._configurationPath.Text,
                OverwritePrompt = true
            };
            if (dlg.ShowDialog().GetValueOrDefault())
            {
                this._configurationPath.Text = dlg.FileName;
                this._generateConfiguration.IsChecked = true;
            }
        }

        private void onHelpLinkNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(this._helpLink.NavigateUri.AbsoluteUri));
                e.Handled = true;
            }
            catch (Exception ex)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error navigating to object: " + ex.Message);
            }
        }
    }
}
