using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Amazon.AWSToolkit.Publish.ViewModels;

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

        private async void HealthCheck_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!(DataContext is PublishToAwsDocumentViewModel vm)) { return; }

                if (vm.DeployToolController == null) { return; }

                var response = await vm.DeployToolController.HealthAsync();
                ToolkitFactory.Instance.ShellProvider.ShowMessage("Publish Healthcheck",
                    $"Health check result: {response.Status}");
            }
            catch (Exception exception)
            {
                ToolkitFactory.Instance.ShellProvider.OutputToHostConsole(exception.Message, true);
            }
        }
    }
}
