using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.DynamoDB.Controller;
using Amazon.AWSToolkit.DynamoDB.Util;

using log4net;

namespace Amazon.AWSToolkit.DynamoDB.View
{
    /// <summary>
    /// Interaction logic for StartLocalDynamoDBControl.xaml
    /// </summary>
    public partial class StartLocalDynamoDBControl : BaseAWSControl
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(StartLocalDynamoDBControl));
        StartLocalDynamoDBController _controller;
        public StartLocalDynamoDBControl(StartLocalDynamoDBController controller)
        {
            InitializeComponent();
            this._controller = controller;
        }

        public override bool SupportsBackGroundDataLoad => true;

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            return this._controller.Model;
        }

        public override string Title => "Connect to DynamoDB Local";

        public override bool Validated()
        {
            if (string.IsNullOrWhiteSpace(this._controller.Model.JavaPath) || !File.Exists(this._controller.Model.JavaPath))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("The \"Java Executable Path\" does not point to a valid directory where Java is installed");
                return false;
            }
            if (this._controller.Model.SelectedVersion == null)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("You must select a version of the DynamoDB Local to start.");
                return false;
            }
            if (!this._controller.Model.SelectedVersion.IsInstalled)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("The selected version of the DynamoDB Local is not installed.");
                return false;
            }


            return true;
        }

        public override bool OnCommit()
        {
            try
            {
                this._controller.Start();
                return true;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error installing DynamoDB Local", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error installing DynamoDB Local: " + e.Message);
                return false;
            }
        }

        private void OnBrowse(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Title = "Select Java Runtime (Java.exe)";
                dlg.CheckPathExists = true;
                dlg.DefaultExt = "exe";
                dlg.Filter = "Application (*.exe)|*.exe";

                if (dlg.ShowDialog().GetValueOrDefault())
                {
                    this._controller.Model.JavaPath = dlg.FileName;
                }
            }
            catch (Exception ex)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error browsing to java: " + ex.Message);
            }
        }

        private void OnInstall(object sender, RoutedEventArgs args)
        {
            try
            {
                this._ctlProgressbar.Visibility = System.Windows.Visibility.Visible;
                this._controller.InstallSelected(this.installProgress);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error installing DynamoDB Local", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error installing DynamoDB Local: " + e.Message);
            }
        }

        private void OnUninstall(object sender, RoutedEventArgs args)
        {
            try
            {
                this._controller.UninstallSelected();
            }
            catch (Exception e)
            {
                LOGGER.Error("Error uninstalling DynamoDB Local", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error uninstalling DynamoDB Local: " + e.Message);
            }
        }

        
        void installProgress(object sender, DynamoDBLocalManager.DownloadProgressEventArgs e)
        {
            this._ctlProgressbar.Visibility = System.Windows.Visibility.Visible;
            this._ctlProgressbar.Minimum = 0;
            this._ctlProgressbar.Maximum = e.TotalBytesReceived;
            this._ctlProgressbar.Value = e.BytesReceived;

            if (e.Complete)
            {
                this._ctlProgressbar.Visibility = System.Windows.Visibility.Hidden;
                if (e.Error != null)
                {
                    LOGGER.Error("Error installing DynamoDB Local", e.Error);
                    ToolkitFactory.Instance.ShellProvider.ShowError("Error installing DynamoDB Local: " + e.Error.Message);
                }

                this._controller.Model.CheckInstallState();
            }
        }
    }
}
