using System;
using System.Windows;
using System.Windows.Controls;

using Amazon.AWSToolkit.Publish.ViewModels;
using Amazon.AWSToolkit.Tasks;

namespace Amazon.AWSToolkit.Publish.Views
{
    /// <summary>
    /// Temporary view panel during initial feature development
    /// TODO : remove before finalizing publish feature
    /// </summary>
    public partial class PublishDocumentHeader : UserControl
    {
        public PublishDocumentHeader()
        {
            InitializeComponent();
        }

        private void HealthCheck_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!(DataContext is PublishToAwsDocumentViewModel vm)) { return; }

                if (vm.DeployToolController == null) { return; }

                vm.JoinableTaskFactory.RunAsync(async () =>
                {
                    var response = await vm.DeployToolController.HealthAsync();
                    ToolkitFactory.Instance.ShellProvider.ShowMessage("Publish Healthcheck",
                        $"Health check result: {response.Status}");
                }).Task.LogExceptionAndForget();
            }
            catch (Exception exception)
            {
                ToolkitFactory.Instance.ShellProvider.OutputToHostConsole(exception.Message, true);
            }
        }
    }
}
