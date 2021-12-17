using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;

using Amazon.AWSToolkit.CloudFormation.Controllers;
using Amazon.AWSToolkit.CloudFormation.ViewModels;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.SimpleWorkers;
using Amazon.AWSToolkit.Tasks;
using Amazon.CloudFormation.Model;

using log4net;

namespace Amazon.AWSToolkit.CloudFormation.View.Components
{
    /// <summary>
    /// Shows charts of common CloudWatch Metrics for certain resource types within a CloudFormation Stack
    /// </summary>
    public partial class StackMonitoringControl
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(StackMonitoringControl));

        ViewStackController _controller;

        public StackMonitoringControl()
        {
            InitializeComponent();
            this._ctlPeriodPicker.ItemsSource = CloudWatchDataFetcher.MonitorPeriod.Periods;
            this._ctlPeriodPicker.SelectedItem = CloudWatchDataFetcher.MonitorPeriod.Periods.ToArray()[0];
        }

        public void Initialize(ViewStackController controller)
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
                IAWSToolkitShellProvider toolkitShell = ToolkitFactory.Instance.ShellProvider;

                var period = this._ctlPeriodPicker.SelectedItem as CloudWatchDataFetcher.MonitorPeriod;
                if (period == null)
                    return;

                this._ctlMainPanel.Children.Clear();
                var resources = this._controller.GetStackResources();
                if (resources == null)
                    return;

                var vms = resources
                    .Where(resource => resource.ResourceStatus == CloudFormationConstants.CreateCompleteStatus)
                    .Select(resource => CreateResourceChartsViewModel(resource, toolkitShell))
                    .Where(vm => vm != null)
                    .ToList();

                vms.ForEach(vm =>
                {
                    var chart = new ResourceCharts.ResourceCharts()
                    {
                        DataContext = vm,
                    };
                    _ctlMainPanel.Children.Add(chart);
                });

                LoadCharts(vms, period.HoursInPast);
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error loading cloud watch data for stack resources: " + e.Message);
                LOGGER.Error("Error loading cloud watch data for stack resources", e);
            }
        }

        private ResourceChartsViewModel CreateResourceChartsViewModel(StackResource resource,
            IAWSToolkitShellProvider toolkitShell)
        {
            switch (resource.ResourceType)
            {
                case "AWS::ElasticLoadBalancing::LoadBalancer":
                    return new LoadBalancerResourceChartsViewModel(resource.PhysicalResourceId, toolkitShell);
                case "AWS::ElasticLoadBalancingV2::LoadBalancer":
                    return new ApplicationLoadBalancerResourceChartsViewModel(resource.PhysicalResourceId, toolkitShell);
                case "AWS::AutoScaling::AutoScalingGroup":
                    return new AutoScalingResourceChartsViewModel(resource.PhysicalResourceId, toolkitShell);
                case "AWS::EC2::Instance":
                    return new Ec2InstanceResourceChartsViewModel(resource.PhysicalResourceId, toolkitShell);
                case "AWS::RDS::DBInstance":
                    return new RdsInstanceResourceChartsViewModel(resource.PhysicalResourceId, toolkitShell);
                case "AWS::Lambda::Function":
                    return new LambdaResourceChartsViewModel(resource.PhysicalResourceId, toolkitShell);
            }

            return null;
        }

        private void LoadCharts(List<ResourceChartsViewModel> resourceChartsViewModels, int hours)
        {
            var metrics = new CloudWatchMetrics(_controller.CloudWatchClient);
            resourceChartsViewModels.ForEach(vm => Task.Run(() => vm.LoadAsync(metrics, hours).LogExceptionAndForget()));
        }
    }
}
