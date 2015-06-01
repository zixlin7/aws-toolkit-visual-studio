using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Amazon.AWSToolkit.SimpleWorkers;
using Amazon.AWSToolkit.ElasticBeanstalk.Controller;
using Amazon.AWSToolkit.ElasticBeanstalk.Model;

using Amazon.CloudWatch.Model;

using log4net;

namespace Amazon.AWSToolkit.ElasticBeanstalk.View.Components
{
    /// <summary>
    /// Interaction logic for MonitorGraphs.xaml
    /// </summary>
    public partial class MonitorGraphs
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(MonitorGraphs));

        EnvironmentStatusController _controller;

        public MonitorGraphs()
        {
            InitializeComponent();
            this._ctlPeriodPicker.ItemsSource = CloudWatchDataFetcher.MonitorPeriod.Periods;
            this._ctlPeriodPicker.SelectedItem = CloudWatchDataFetcher.MonitorPeriod.Periods.ToArray()[0];

        }

        public void Initialize(EnvironmentStatusController controller)
        {
            this._controller = controller;
        }

        private void onPeriodChanged(object sender, SelectionChangedEventArgs e)
        {
            // Don't load data an initial selection of combo box
            if (e.RemovedItems.Count == 0)
                return;

            LoadCloudWatchData();
        }

        public void LoadCloudWatchData()
        {
            try
            {
                if (this._ctlPeriodPicker.SelectedItem == null)
                    this._ctlPeriodPicker.SelectedIndex = 0;

                if (this._controller.Model.Status == BeanstalkConstants.STATUS_TERMINATED)
                {
                    resetGraphs();
                    return;
                }

                var period = this._ctlPeriodPicker.SelectedItem as CloudWatchDataFetcher.MonitorPeriod;
                var resources = this._controller.GetEnvironmentResourceDescription();

                List<Dimension> lbDimensions = new List<Dimension>();
                foreach (var item in resources.LoadBalancers)
                {
                    lbDimensions.Add(new Dimension() { Name = "LoadBalancerName", Value = item.Name });
                }

                List<Dimension> autoDimensions = new List<Dimension>();
                foreach (var item in resources.AutoScalingGroups)
                {
                    autoDimensions.Add(new Dimension() { Name = "AutoScalingGroupName", Value = item.Name });
                }

                this._controller.LoadCloudWatchData(this._ctlLatencyCountSeries, "AWS/ELB", "Latency", "Average", "Seconds", lbDimensions, period.HoursInPast);
                this._controller.LoadCloudWatchData(this._ctlRequestCountSeries, "AWS/ELB", "RequestCount", "Sum", "Count", lbDimensions, period.HoursInPast);
                this._controller.LoadCloudWatchData(this._ctlCPUUtilizationSeries, "AWS/EC2", "CPUUtilization", "Average", "Percent", autoDimensions, period.HoursInPast);
                this._controller.LoadCloudWatchData(this._ctlNetworkInSeries, "AWS/EC2", "NetworkIn", "Maximum", "Bytes", autoDimensions, period.HoursInPast);
                this._controller.LoadCloudWatchData(this._ctlNetworkOutSeries, "AWS/EC2", "NetworkOut", "Maximum", "Bytes", autoDimensions, period.HoursInPast);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error loading cloudwatch metric data", e);
                resetGraphs();
            }
        }

        void resetGraphs()
        {
            this._ctlLatencyCountSeries.ItemsSource = null;
            this._ctlRequestCountSeries.ItemsSource = null;
            this._ctlCPUUtilizationSeries.ItemsSource = null;
            this._ctlNetworkInSeries.ItemsSource = null;
            this._ctlNetworkOutSeries.ItemsSource = null;
        }
    }
}
