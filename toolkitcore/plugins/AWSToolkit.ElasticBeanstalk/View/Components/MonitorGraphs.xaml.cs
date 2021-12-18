using System;
using System.Collections.Generic;

using Amazon.AWSToolkit.ElasticBeanstalk.Controller;
using Amazon.AWSToolkit.ElasticBeanstalk.ViewModels;
using Amazon.AWSToolkit.SimpleWorkers;
using Amazon.CloudWatch.Model;

using log4net;

namespace Amazon.AWSToolkit.ElasticBeanstalk.View.Components
{
    /// <summary>
    /// Renders Beanstalk Environment CloudWatch Metrics charts
    /// </summary>
    public partial class MonitorGraphs
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(MonitorGraphs));

        EnvironmentStatusController _controller;

        private readonly MonitorGraphsViewModel _monitorGraphs;

        public MonitorGraphs()
        {
            _monitorGraphs = new MonitorGraphsViewModel();

            InitializeComponent();

            DataContext = _monitorGraphs;
            _monitorGraphs.GraphPeriod.PropertyChanged += GraphPeriod_PropertyChanged;
        }

        public void Initialize(EnvironmentStatusController controller)
        {
            this._controller = controller;
        }

        private void GraphPeriod_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            LoadCloudWatchData();
        }

        public void LoadCloudWatchData()
        {
            try
            {
                var period = _monitorGraphs.GraphPeriod.SelectedPeriod;
                if (period == null)
                {
                    return;
                }

                if (this._controller.Model.Status == BeanstalkConstants.STATUS_TERMINATED)
                {
                    resetGraphs();
                    return;
                }

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

                this._controller.LoadCloudWatchData(_monitorGraphs.Latency, "AWS/ELB", "Latency", CloudWatchMetrics.Aggregate.Average, "Seconds", lbDimensions, period.Hours);
                this._controller.LoadCloudWatchData(_monitorGraphs.Requests, "AWS/ELB", "RequestCount", CloudWatchMetrics.Aggregate.Sum, "Count", lbDimensions, period.Hours);
                this._controller.LoadCloudWatchData(_monitorGraphs.CpuUsage, "AWS/EC2", "CPUUtilization", CloudWatchMetrics.Aggregate.Average, "Percent", autoDimensions, period.Hours);
                this._controller.LoadCloudWatchData(_monitorGraphs.NetworkIn, "AWS/EC2", "NetworkIn", CloudWatchMetrics.Aggregate.Maximum, "Bytes", autoDimensions, period.Hours);
                this._controller.LoadCloudWatchData(_monitorGraphs.NetworkOut, "AWS/EC2", "NetworkOut", CloudWatchMetrics.Aggregate.Maximum, "Bytes", autoDimensions, period.Hours);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error loading cloudwatch metric data", e);
                resetGraphs();
            }
        }

        void resetGraphs()
        {
            _monitorGraphs.Latency.Values?.Clear();
            _monitorGraphs.Requests.Values?.Clear();
            _monitorGraphs.CpuUsage.Values?.Clear();
            _monitorGraphs.NetworkIn.Values?.Clear();
            _monitorGraphs.NetworkOut.Values?.Clear();
        }
    }
}
