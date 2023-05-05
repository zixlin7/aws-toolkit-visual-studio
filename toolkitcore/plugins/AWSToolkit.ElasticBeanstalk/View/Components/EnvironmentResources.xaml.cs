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
            if (_controller.Model.IsLoading)
            {
                return;
            }

            IsEnabled = false;

            ThreadPool.QueueUserWorkItem(async _ =>
            {
                await _controller.RefreshResourcesAsync();
                Dispatcher.Invoke((Action) (() =>
                {
                    IsEnabled = true;
                }));
            });
        }
    }
}
