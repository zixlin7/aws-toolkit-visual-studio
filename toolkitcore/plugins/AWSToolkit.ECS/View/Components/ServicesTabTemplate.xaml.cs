using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Amazon.AWSToolkit.ECS.Model;

namespace Amazon.AWSToolkit.ECS.View.Components
{
    /// <summary>
    /// Interaction logic for ServicesTabTemplate.xaml
    /// </summary>
    public partial class ServicesTabTemplate : UserControl
    {
        public ServicesTabTemplate()
        {
            InitializeComponent();
        }

        private void onServiceURLClick(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                var uri = e.Uri.ToString();
                if (uri.EndsWith("*"))
                    uri = uri.Substring(0, uri.Length - 1);
                Process.Start(new ProcessStartInfo(uri));
                e.Handled = true;
            }
            catch (Exception ex)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error navigating to load balancer: " + ex.Message);
            }
        }

        private void onHealthCheckURLClick(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.ToString()));
                e.Handled = true;
            }
            catch (Exception ex)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error navigating to health check: " + ex.Message);
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var service = this.DataContext as ServiceWrapper;
            if (!service.EditMode)
            {
                var control = FindMainControl();
                control.DeleteService(this.DataContext as ServiceWrapper);
            }
            else
            {
                service.ResetEditMode();
                SwapButtonText();
            }
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            var service = this.DataContext as ServiceWrapper;
            if(service.EditMode)
            {
                // Todo Save serviec

                service.EditMode = false;
                SwapButtonText();
            }
            else
            {
                service.EditMode = true;
                SwapButtonText();
            }

        }

        private void SwapButtonText()
        {
            var service = this.DataContext as ServiceWrapper;
            if (!service.EditMode)
            {
                this._ctlEditBtn.Content = "Edit";
                this._ctlDeleteBtn.Content = "Delete";
            }
            else
            {
                this._ctlEditBtn.Content = "Save";
                this._ctlDeleteBtn.Content = "Cancel";
            }
        }

        private ViewClusterControl FindMainControl()
        {
            var current = VisualTreeHelper.GetParent(this);
            while (current != null && !(current is ViewClusterControl))
            {
                current = VisualTreeHelper.GetParent(current);
            }

            return current as ViewClusterControl;
        }
    }
}
