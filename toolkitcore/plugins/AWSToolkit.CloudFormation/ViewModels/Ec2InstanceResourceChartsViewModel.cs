using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.SimpleWorkers;
using Amazon.AWSToolkit.ViewModels.Charts;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;

namespace Amazon.AWSToolkit.CloudFormation.ViewModels
{
    public class Ec2InstanceResourceChartsViewModel : ResourceChartsViewModel
    {
        private readonly MonitorGraphViewModel _cpuUsage = new MonitorGraphViewModel()
        {
            Title = ResourceGraphTitles.CpuUtilization, LabelFormatter = MonitorGraphViewModel.TicksTimeFormatter,
        };

        private readonly MonitorGraphViewModel _networkIn = new MonitorGraphViewModel()
        {
            Title = ResourceGraphTitles.NetworkIn, LabelFormatter = MonitorGraphViewModel.TicksTimeFormatter,
        };

        private readonly MonitorGraphViewModel _networkOut = new MonitorGraphViewModel()
        {
            Title = ResourceGraphTitles.NetworkOut, LabelFormatter = MonitorGraphViewModel.TicksTimeFormatter,
        };

        public Ec2InstanceResourceChartsViewModel(string instanceId, IAWSToolkitShellProvider toolkitShell)
            : base(toolkitShell)
        {
            ResourceType = "EC2 Instance";
            ResourceName = instanceId;

            Charts.Add(_cpuUsage);
            Charts.Add(_networkIn);
            Charts.Add(_networkOut);
        }

        public override async Task LoadAsync(CloudWatchMetrics metrics, int hours)
        {
            List<Dimension> dimensions = new List<Dimension>
            {
                new Dimension() { Name = "InstanceId", Value = ResourceName }
            };

            await Task.WhenAll(
                LoadCloudWatchMetricAsync("CPUUtilization", "AWS/EC2",
                    dimensions, CloudWatchMetrics.Aggregate.Average, StandardUnit.Percent,
                    hours, metrics, _cpuUsage),
                LoadCloudWatchMetricAsync("NetworkIn", "AWS/EC2",
                    dimensions, CloudWatchMetrics.Aggregate.Maximum, StandardUnit.Bytes,
                    hours, metrics, _networkIn),
                LoadCloudWatchMetricAsync("NetworkOut", "AWS/EC2",
                    dimensions, CloudWatchMetrics.Aggregate.Maximum, StandardUnit.Bytes,
                    hours, metrics, _networkOut)
            );
        }
    }
}
