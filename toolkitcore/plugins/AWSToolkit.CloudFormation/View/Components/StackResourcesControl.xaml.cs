using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CloudFormation.Controllers;
using Amazon.AWSToolkit.CloudFormation.Model;
using log4net;

namespace Amazon.AWSToolkit.CloudFormation.View.Components
{
    /// <summary>
    /// Interaction logic for StackResourcesControl.xaml
    /// </summary>
    public partial class StackResourcesControl
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(StackResourcesControl));

        bool _isLoading = false;
        ViewStackController _controller;

        public StackResourcesControl()
        {
            InitializeComponent();
        }

        public void Initialize(ViewStackController controller)
        {
            this._controller = controller;
        }

        public void LoadResources(bool async)
        {
            if (this._isLoading)
                return;

            this._isLoading = true;
            this._ctlLastRefresh.Text = "Loading";
            this.IsEnabled = false;

            var callback = (WaitCallback)(x =>
            {
                bool error = false;
                try
                {
                    this._controller.RefreshStackResources();
                }
                catch (Exception e)
                {
                    error = true;
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        this._ctlLastRefresh.Text = e.Message;
                    }));
                    LOGGER.Error("Error refreshing resources", e);
                }
                finally
                {
                    this._isLoading = false;
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        if (!error)
                            this._ctlLastRefresh.Text = "";
                        this.IsEnabled = true;
                    }));
                }
            });

            if (async)
                ThreadPool.QueueUserWorkItem(callback);
            else
                callback(null);
        }

        private void onLoaded(object sender, RoutedEventArgs e)
        {
            DataGridHelper.TurnOffAutoScroll(this._ctlEC2DataGrid);
        }

        private void onGridContextMenu(object sender, RoutedEventArgs evnt)
        {
            try
            {
                if (this._ctlEC2DataGrid.SelectedItems.Count == 0)
                    return;

                ContextMenu menu = new ContextMenu();

                MenuItem connect = new MenuItem() { Header = "Connect" };
                connect.Icon = IconHelper.GetIcon(this.GetType().Assembly, "Amazon.AWSToolkit.CloudFormation.Resources.EmbeddedImages.connect.png");
                connect.Click += this.onConnectClick;

                MenuItem deploymentLog = new MenuItem() { Header = "Deployment Log" };
                deploymentLog.Click += this.onDeploymentLogClick;
                deploymentLog.Icon = IconHelper.GetIcon(this.GetType().Assembly, "Amazon.AWSToolkit.CloudFormation.Resources.EmbeddedImages.scroll.png");

                menu.Items.Add(connect);
                if(this._controller.Model.VSToolkitDeployedFieldsVisibility == Visibility.Visible)
                    menu.Items.Add(deploymentLog);

                menu.PlacementTarget = this;
                menu.IsOpen = true;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error with context menu", e);
            }
        }

        private void onConnectClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                if (this._ctlEC2DataGrid.SelectedItems.Count != 1)
                    return;

                var instance = this._ctlEC2DataGrid.SelectedItem as RunningInstanceWrapper;
                if (instance == null)
                    return;

                this._controller.ConnectToInstance(instance);
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error connecting to instance");
                LOGGER.Error("Error connecting to instance", e);
            }
        }

        private void onDeploymentLogClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                if (this._ctlEC2DataGrid.SelectedItems.Count != 1)
                    return;

                var instance = this._ctlEC2DataGrid.SelectedItem as RunningInstanceWrapper;
                if (instance == null)
                    return;

                this._controller.ViewDeploymentLog(instance);
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error getting deployment logs");
                LOGGER.Error("Error getting deployment logs", e);
            }
        }
          
    }
}
