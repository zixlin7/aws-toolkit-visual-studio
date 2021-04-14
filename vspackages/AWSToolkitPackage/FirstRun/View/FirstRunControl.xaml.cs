using System;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Navigation;
using System.Windows.Input;

using Amazon.AWSToolkit.Account.Controller;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.VisualStudio.FirstRun.Controller;
using Amazon.AWSToolkit.VisualStudio.FirstRun.Model;
using log4net;
using Amazon.AWSToolkit.MobileAnalytics;
using Amazon.AWSToolkit.Settings;
using Amazon.AwsToolkit.Telemetry.Events.Generated;

namespace Amazon.AWSToolkit.VisualStudio.FirstRun.View
{
    /// <summary>
    /// Interaction logic for FirstRunControl.xaml
    /// </summary>
    public partial class FirstRunControl
    {
        readonly FirstRunController _controller;

        readonly ILog LOGGER = LogManager.GetLogger(typeof(FirstRunControl));

        private bool _saveFirstRunModel = true;

        public FirstRunControl()
            : this(null)
        {
        }

        public FirstRunControl(FirstRunController controller)
        {
            this._controller = controller;
            this.DataContext = this._controller.Model;

            this.Loaded += FirstRunControl_Loaded;
            
            InitializeComponent();
            ThemeUtil.ThemeChange += ThemeUtilOnThemeChange;

            // match the logo header 'text' to the theme on startup
            ThemeUtilOnThemeChange(null, null);
        }

        private void FirstRunControl_Loaded(object sender, RoutedEventArgs e)
        {
            ToolkitSettings.Instance.HasUserSeenFirstRunForm = true;

            this.Loaded -= FirstRunControl_Loaded;
        }

        private void ThemeUtilOnThemeChange(object sender, EventArgs eventArgs)
        {
            var logo = ThemeUtil.GetLogoImageSource("logo_aws");
            if (logo != null)
            {
                _ctlLogoImage.Source = logo;
            }
        }

        public override string Title => "AWS Getting Started";

        public override string UniqueId => "AWSGettingStarted";

        private FirstRunModel Model => _controller.Model;

        public override void OnEditorOpened(bool success)
        {
            ToolkitFactory.Instance.TelemetryLogger.RecordAwsHelpQuickstart(new AwsHelpQuickstart()
            {
                Result = success ? Result.Succeeded : Result.Failed,
            });
        }

        private void OnClickSaveAndClose(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;
            var autoExit = true;

            try
            {
                if (_saveFirstRunModel)
                {
                    Model.Save();
                }
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
                Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    if (Model.OpenAWSExplorerOnClosing)
                    {
                        this._controller.HostPackage.ShowExplorerWindow();
                    }

                    var svc = this._controller.HostPackage.GetVSShellService(typeof(SAWSToolkitShellProvider)) as IAWSToolkitShellProvider;
                    svc.CloseEditor(this);
                });
            }

            Cursor = Cursors.Arrow;
        }

        private void ImportAWSCredentials_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var csvFilename = ShowFileOpenDialog("Import AWS Credentials from Csv File");
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
            catch(Exception ex)
            {
                LOGGER.Error("Error importing AWS credentials", ex);
            }
        }

        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            LaunchBrowserToUrl(e.Uri);
            e.Handled = true;
        }

        private void OnRequestCreateDialog(object sender, RequestNavigateEventArgs e)
        {
            var toolkitContext = this._controller.ToolkitContext;
            var command = new RegisterAccountController(toolkitContext.CredentialManager,
                toolkitContext.CredentialSettingsManager, toolkitContext.ConnectionManager,
                toolkitContext.RegionProvider);
            var results  = command.Execute();
            if (results.Success)
            {
                _saveFirstRunModel = false;
                SaveAndCloseButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            }
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

        private void AccessKey_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            this._controller.Model.AccessKey = this.AccessKey.Password;
        }

        private void SecretKey_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            this._controller.Model.SecretKey = this.SecretKey.Password;
        }
    }

}
