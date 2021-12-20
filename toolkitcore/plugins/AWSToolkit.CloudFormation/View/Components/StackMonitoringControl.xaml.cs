using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        private StackMonitoringViewModel _viewModel;

        public StackMonitoringControl()
        {
            _viewModel = new StackMonitoringViewModel();

            InitializeComponent();

            DataContext = _viewModel;
            _viewModel.GraphPeriod.PropertyChanged += GraphPeriod_PropertyChanged;
            DataContextChanged += StackMonitoringControl_DataContextChanged;
        }

        private void GraphPeriod_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            LoadCloudWatchData();
        }

        private void StackMonitoringControl_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (_viewModel?.GraphPeriod != null)
            {
                _viewModel.GraphPeriod.PropertyChanged -= GraphPeriod_PropertyChanged;
            }

            _viewModel = e.NewValue as StackMonitoringViewModel;

            if (_viewModel?.GraphPeriod != null)
            {
                _viewModel.GraphPeriod.PropertyChanged += GraphPeriod_PropertyChanged;
            }
        }

        public void Initialize(ViewStackController controller)
        {
            this._controller = controller;
        }

        public void LoadCloudWatchData()
        {
            try
            {
                IAWSToolkitShellProvider toolkitShell = ToolkitFactory.Instance.ShellProvider;

                if (_viewModel.GraphPeriod.SelectedPeriod == null)
                {
                    return;
                }

                _viewModel?.Charts.Clear();
                var resources = this._controller.GetStackResources();
                if (resources == null)
                    return;

                var vms = resources
                    .Where(resource => resource.ResourceStatus == CloudFormationConstants.CreateCompleteStatus)
                    .Select(resource => CreateResourceChartsViewModel(resource, toolkitShell))
                    .Where(vm => vm != null)
                    .ToList();

                vms.ForEach(vm => _viewModel?.Charts.Add(vm));
                LoadCharts(vms, _viewModel.GraphPeriod.SelectedPeriod.Hours);
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
