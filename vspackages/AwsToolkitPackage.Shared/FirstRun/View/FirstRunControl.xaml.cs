using System;
using System.ComponentModel;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Navigation;
using System.Windows.Input;

using Amazon.AWSToolkit.Account.Controller;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.VisualStudio.FirstRun.Controller;
using Amazon.AWSToolkit.VisualStudio.FirstRun.Model;
using log4net;
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
            this._controller.Model.PropertyChanged += OnPropertyChanged;
            this.Loaded += FirstRunControl_Loaded;

            InitializeComponent();
            ThemeUtil.ThemeChange += ThemeUtilOnThemeChange;

            // match the logo header 'text' to the theme on startup
            ThemeUtilOnThemeChange(null, null);
            Unloaded += OnUnloaded;
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
            _controller.RecordAwsHelpQuickstartMetric(success);
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
                _controller.RecordAwsModifyCredentialsMetric(true, CredentialModification.Add);
            }
            catch (Exception ex)
            {
                autoExit = false;
                LOGGER.Error("Error during save of credentials", ex);
                _controller.RecordAwsModifyCredentialsMetric(false, CredentialModification.Add);
            }

            if (autoExit)
            {
                Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    if (Model.OpenAWSExplorerOnClosing)
                    {
                        await _controller.ShowAwsExplorerAsync();
                    }

                    _controller.CloseEditor(this);
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
                    Model.AwsCredentialsFromCsv(csvFilename);
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
            var command = new RegisterAccountController(toolkitContext);
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

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Unloaded -= OnUnloaded;
            _controller.Model.PropertyChanged -= OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_controller.Model.AccessKey))
            {
                UpdatePassword(AccessKey, _controller.Model.AccessKey);
            }

            if (e.PropertyName == nameof(_controller.Model.SecretKey))
            {
                UpdatePassword(SecretKey, _controller.Model.SecretKey);
            }
        }

        private void UpdatePassword(PasswordBox passwordBox, string password)
        {
            if (passwordBox.Password != password)
            {
                passwordBox.Password = password;
            }
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
