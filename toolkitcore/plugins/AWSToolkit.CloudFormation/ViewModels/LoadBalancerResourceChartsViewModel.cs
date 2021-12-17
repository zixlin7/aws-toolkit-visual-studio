using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.SimpleWorkers;
using Amazon.AWSToolkit.ViewModels.Charts;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;

namespace Amazon.AWSToolkit.CloudFormation.ViewModels
{
    public class LoadBalancerResourceChartsViewModel : ResourceChartsViewModel
    {
        private readonly MonitorGraphViewModel _latency = new MonitorGraphViewModel()
        {
            Title = ResourceGraphTitles.Latency, LabelFormatter = MonitorGraphViewModel.TicksTimeFormatter,
        };

        private readonly MonitorGraphViewModel _requests = new MonitorGraphViewModel()
        {
            Title = ResourceGraphTitles.Requests, LabelFormatter = MonitorGraphViewModel.TicksTimeFormatter,
        };

        public LoadBalancerResourceChartsViewModel(string loadBalancerName, IAWSToolkitShellProvider toolkitShell)
            : base(toolkitShell)
        {
            ResourceType = "Load Balancer";
            ResourceName = loadBalancerName;

            Charts.Add(_latency);
            Charts.Add(_requests);
        }

        public override async Task LoadAsync(CloudWatchMetrics metrics, int hours)
        {
            List<Dimension> dimensions = new List<Dimension>
            {
                new Dimension() { Name = "LoadBalancerName", Value = ResourceName }
            };

            await Task.WhenAll(
                LoadCloudWatchMetricAsync("Latency", "AWS/ELB",
                    dimensions, CloudWatchMetrics.Aggregate.Average, StandardUnit.Seconds,
                    hours, metrics, _latency),
                LoadCloudWatchMetricAsync("RequestCount", "AWS/ELB",
                    dimensions, CloudWatchMetrics.Aggregate.Sum, StandardUnit.Count,
                    hours, metrics, _requests)
            );
        }
    }
}
