using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Amazon.AWSToolkit.ECS.Controller;
using log4net;

namespace Amazon.AWSToolkit.ECS.View.Components
{
    public partial class PushCommandsControl
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(PushCommandsControl));

        public string PowerShellTabHeader
        {
            get { return "AWS PowerShell Tools"; }
        }

        public string AwsCliTabHeader
        {
            get { return "AWS CLI"; }
        }

        public string DotNetCliTabHeader
        {
            get { return "Dotnet CLI"; }
        }

        ViewRepositoryController _controller;

        public PushCommandsControl()
        {
            InitializeComponent();
        }

        public void Initialize(ViewRepositoryController controller)
        {
            this._controller = controller;
        }

        private void HelpLink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                ToolkitFactory.Instance.ShellProvider.OpenInBrowser(e.Uri.ToString(), true);
            }
            catch (Exception ex)
            {
                LOGGER.Error("Failed to launch process to go to endpoint " + e.Uri, ex);
            }
            e.Handled = true;
        }

        private void OnClick_CopyGetLoginCommand(object sender, RoutedEventArgs e)
        {
            var currentTab = _ctlTools.Items[_ctlTools.SelectedIndex] as TabItem;
            string command = null;
            if (currentTab.Header.Equals(PowerShellTabHeader))
                command = _controller.Model.PowerShellGetLoginCommand;
            else if (currentTab.Header.Equals(AwsCliTabHeader))
                command = _controller.Model.AwsCliGetLoginCommand;

            if (!string.IsNullOrEmpty(command))
            {
                Clipboard.SetText(command);
            }
        }

        private void OnClick_CopyRunLoginCommand(object sender, RoutedEventArgs e)
        {
            var currentTab = _ctlTools.Items[_ctlTools.SelectedIndex] as TabItem;
            string command = null;
            if (currentTab.Header.Equals(PowerShellTabHeader))
                command = _controller.Model.PowerShellRunLoginCommand;
            else if (currentTab.Header.Equals(AwsCliTabHeader))
                command = _controller.Model.AwsCliRunLoginCommand;

            if (!string.IsNullOrEmpty(command))
            {
                Clipboard.SetText(command);
            }
        }

        private void OnClick_CopyDockerBuildCommand(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(_controller.Model.DockerBuildCommand);
        }

        private void OnClick_CopyDockerTagCommand(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(_controller.Model.DockerTagCommand);
        }

        private void OnClick_CopyDockerPushCommand(object sender, RoutedEventArgs e)
        {
            // this will need to detect dotnet cli tab
            Clipboard.SetText(_controller.Model.DockerPushCommand);
        }

    }
}
