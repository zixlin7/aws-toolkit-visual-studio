﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;
using System.Windows.Input;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.VisualStudio.FirstRun.Controller;
using Amazon.AWSToolkit.VisualStudio.FirstRun.Model;
using log4net;
using Microsoft.VisualStudio.Shell;
using Amazon.AWSToolkit.MobileAnalytics;

namespace Amazon.AWSToolkit.VisualStudio.FirstRun.View
{
    /// <summary>
    /// Interaction logic for FirstRunControl.xaml
    /// </summary>
    public partial class FirstRunControl
    {
        readonly FirstRunController _controller;

        readonly ILog LOGGER = LogManager.GetLogger(typeof(FirstRunControl));

        public FirstRunControl()
            : this(null)
        {
        }

        public FirstRunControl(FirstRunController controller)
        {
            this._controller = controller;
            this.DataContext = this._controller.Model;

            InitializeComponent();
            ThemeUtil.ThemeChange += ThemeUtilOnThemeChange;

            // if the user is in VS2013 or VS2015, they will have been asked about
            // analytics when running the installer
            var shellVersion = controller.HostPackage.ToolkitShellProviderService.ShellVersion;
            if (shellVersion == "2013" || shellVersion == "2015")
                _analyticsPanel.Visibility = Visibility.Collapsed;

            // match the logo header 'text' to the theme on startup
            ThemeUtilOnThemeChange(null, null);
        }

        private void ThemeUtilOnThemeChange(object sender, EventArgs eventArgs)
        {
            var logo = ThemeUtil.GetLogoImageSource("logo_aws");
            if (logo != null)
            {
                _ctlLogoImage.Source = logo;
            }
        }

        public override string Title
        {
            get { return "AWS Getting Started"; }    
        }

        public override string UniqueId
        {
            get { return "AWSGettingStarted"; }
        }

        private FirstRunModel Model { get { return _controller.Model; } }

        private void OnClickSaveAndClose(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;
            var autoExit = true;

            try
            {
                Model.Save();

                ToolkitEvent evnt = new ToolkitEvent();
                evnt.AddProperty(AttributeKeys.FirstExperienceSaveCredentialsStatus, ToolkitEvent.COMMON_STATUS_SUCCESS);
                SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);
            }
            catch (Exception ex)
            {
                ToolkitEvent evnt = new ToolkitEvent();
                evnt.AddProperty(AttributeKeys.FirstExperienceSaveCredentialsStatus, ToolkitEvent.COMMON_STATUS_FAILURE);
                SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);

                autoExit = false;
                LOGGER.Error("Error during save of credentials", ex);
            }

            if (autoExit)
            {
                if (Model.OpenAWSExplorerOnClosing)
                {
                    this._controller.HostPackage.ShellDispatcher.Invoke(() =>
                    {
                        this._controller.HostPackage.ShowExplorerWindow();
                    });
                }

                this._controller.HostPackage.ShellDispatcher.Invoke(() =>
                {
                    var svc = this._controller.HostPackage.GetVSShellService(typeof(SAWSToolkitShellProvider)) as IAWSToolkitShellProvider;
                    svc.CloseEditor(this);
                });
            }

            Cursor = Cursors.Arrow;
        }

        private void ImportAWSCredentials_OnClick(object sender, RoutedEventArgs e)
        {
            var csvFilename = ShowFileOpenDialog("Import AWS Credentals from Csv File");
            if (csvFilename != null)
            {
                if (Model.AwsCredentialsFromCsv(csvFilename))
                {
                    var evnt = new ToolkitEvent();
                    evnt.AddProperty(AttributeKeys.FirstExperienceImport, ToolkitEvent.COMMON_STATUS_SUCCESS);
                    SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);
                }
            }
        }

        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            LaunchBrowserToUrl(e.Uri);
            e.Handled = true;
        }

        private void LaunchBrowserToUrl(Uri uri)
        {
            try
            {
                ToolkitEvent evnt = new ToolkitEvent();
                evnt.AddProperty(AttributeKeys.FirstExperienceLinkClick, uri.ToString());
                SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);


                _controller.OpenInBrowser(uri.ToString());
            }
            catch (Exception ex)
            {
                LOGGER.Error("Failed to launch process to go to endpoint " + uri, ex);
            }
        }

        private static string ShowFileOpenDialog(string title)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".csv",
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                CheckPathExists = true,
                Title = title
            };

            var result = dlg.ShowDialog();
            if (result.GetValueOrDefault())
                return dlg.FileName;

            return null;
        }
    }

}
