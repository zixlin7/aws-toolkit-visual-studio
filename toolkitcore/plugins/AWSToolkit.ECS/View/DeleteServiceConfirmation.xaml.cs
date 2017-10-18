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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Amazon.ElasticLoadBalancingV2.Model;
using Amazon.AWSToolkit.CommonUI;
using log4net;
using Amazon.AWSToolkit.ECS.Controller;
using Amazon.AWSToolkit.ECS.Model;

namespace Amazon.AWSToolkit.ECS.View
{
    /// <summary>
    /// Interaction logic for DeleteServiceConfirmation.xaml
    /// </summary>
    public partial class DeleteServiceConfirmation
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(DeleteServiceConfirmation));

        public bool CanDeleteLoadBalancer => _controller.LoadBalancer != null;
        public bool CanDeleteListener => _controller.Listener != null;
        public bool CanDeleteTargetGroup => _controller.TargetGroup != null;
        private DeleteServiceConfirmationController _controller;

        public DeleteServiceConfirmation()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        
        public DeleteServiceConfirmation(DeleteServiceConfirmationController controller)
            : this()
        {
            this._controller = controller;

            if (!this.CanDeleteLoadBalancer && !this.CanDeleteListener && !this.CanDeleteTargetGroup)
            {
                this._ctlELBConfirmPanel.Visibility = Visibility.Collapsed;
                this.Height -= 100;
            }
            else
            {
                if (!this.CanDeleteLoadBalancer)
                    this._ctlDeleteLoadBalancer.Visibility = Visibility.Collapsed;
                else
                {
                    this.DeleteLoadbalancer = true;
                    this._ctlDeleteLoadBalancer.Content = "Load Balancer: " + this._controller.LoadBalancer.LoadBalancerName;
                }

                if (!this.CanDeleteListener)
                    this._ctlDeleteListener.Visibility = Visibility.Collapsed;
                else
                {
                    this.DeleteListener = true;
                    this._ctlDeleteListener.Content = "Listener: " + this._controller.Listener.Port + " (" + this._controller.Listener.Protocol + ")";
                }

                if (!this.CanDeleteTargetGroup)
                    this._ctlDeleteTargetGroup.Visibility = Visibility.Collapsed;
                else
                {
                    this.DeleteTargetGroup = true;
                    this._ctlDeleteTargetGroup.Content = "Target Group: " + this._controller.TargetGroup.TargetGroupName;
                }
            }
        }

        private void onLoaded(object sender, RoutedEventArgs e)
        {
        }

        public override string Title => "Confirm deleting " + this._controller.Service.ServiceName;

        public override bool OnCommit()
        {
            try
            {
                var host = FindHost<OkCancelDialogHost>();
                if (host != null)
                    host.IsOkEnabled = false;
                this._controller.DeleteService();
            }
            catch (Exception e)
            {
                LOGGER.Error("Error deleting service", e);
                AppendOutputMessage("Error deleting service", e.Message);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error deleting vpc: " + e.Message);
            }

            return false;
        }

        bool _deleteLoadbalancer;
        public bool DeleteLoadbalancer
        {
            get { return this._deleteLoadbalancer; }
            set
            {
                this._deleteLoadbalancer = value;
                NotifyPropertyChanged("DeleteLoadbalancer");
            }
        }

        bool _deleteListener;
        public bool DeleteListener
        {
            get { return this._deleteListener; }
            set
            {
                this._deleteListener = value;
                NotifyPropertyChanged("DeleteListener");
            }
        }

        bool _deleteTargetGroup;
        public bool DeleteTargetGroup
        {
            get { return this._deleteTargetGroup; }
            set
            {
                this._deleteTargetGroup = value;
                NotifyPropertyChanged("DeleteTargetGroup");
            }
        }

        public void DeleteAsyncComplete(bool success)
        {
            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.Invoke((System.Action)(() =>
            {
                var host = FindHost<OkCancelDialogHost>();
                if (host == null)
                    return;

                if (!success)
                    host.IsOkEnabled = true;
                else
                    host.Close(true);
            }));
        }

        public void AppendOutputMessage(string message, params object[] args)
        {
            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.BeginInvoke((System.Action)(() =>
            {
                string line = string.Format(message, args);

                ToolkitFactory.Instance.ShellProvider.UpdateStatus(line);
                ToolkitFactory.Instance.ShellProvider.OutputToHostConsole(line, true);

                var body = this._ctlOutputLog.Text;
                if (!string.IsNullOrEmpty(body))
                    body += "\r\n";

                body += line;
                this._ctlOutputLog.Text = body;
                this._ctlOutputLog.ScrollToEnd();
            }));
        }
    }
}
