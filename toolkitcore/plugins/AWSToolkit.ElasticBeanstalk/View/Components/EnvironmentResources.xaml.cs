using System;
using System.Threading;
using System.Windows;
using Amazon.AWSToolkit.ElasticBeanstalk.Controller;
using Amazon.AWSToolkit.CommonUI;

using log4net;

namespace Amazon.AWSToolkit.ElasticBeanstalk.View.Components
{
    /// <summary>
    /// Interaction logic for EnvironmentResources.xaml
    /// </summary>
    public partial class EnvironmentResources
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(EnvironmentResources));

        bool _turnedOffAutoScroll;
        EnvironmentStatusController _controller;
        bool _isLoading = false;

        public EnvironmentResources()
        {
            InitializeComponent();
        }

        public void Initialize(EnvironmentStatusController controller)
        {
            _controller = controller;
        }

        private void onLoad(object sender, RoutedEventArgs e)
        {
            if (!_turnedOffAutoScroll)
            {
                DataGridHelper.TurnOffAutoScroll(_ctlInstanceGrid);
                DataGridHelper.TurnOffAutoScroll(_ctlLoadBalancerGrid);
                DataGridHelper.TurnOffAutoScroll(_ctlAutoScalingGrid);
                DataGridHelper.TurnOffAutoScroll(_ctlTriggerGrid);
                _turnedOffAutoScroll = true;
            }
        }

        public void LoadEnviromentResourceData()
        {
            if (this._isLoading)
                return;

            this._isLoading = true;
            this._ctlLastRefresh.Text = "Loading";
            this.IsEnabled = false;

            ThreadPool.QueueUserWorkItem(x =>
                {
                    try
                    {
                        this._controller.RefreshResources();
                    }
                    catch (Exception e)
                    {
                        this._ctlLastRefresh.Text = e.Message;
                        LOGGER.Error("Error refreshing resources", e);
                    }
                    finally
                    {
                        this._isLoading = false;
                        this.Dispatcher.Invoke((Action)(() =>
                            {
                                this._ctlLastRefresh.Text = this._controller.Model.ResourcesUpdated.ToString();
                                this.IsEnabled = true;
                            }));
                    }
                });
        }
    }
}
