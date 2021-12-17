using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.SimpleWorkers;
using Amazon.AWSToolkit.ViewModels.Charts;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;

namespace Amazon.AWSToolkit.CloudFormation.ViewModels
{
    public class ApplicationLoadBalancerResourceChartsViewModel : ResourceChartsViewModel
    {
        private readonly string _loadBalancerArn;

        private readonly MonitorGraphViewModel _requests = new MonitorGraphViewModel()
        {
            Title = "Request Count", LabelFormatter = MonitorGraphViewModel.TicksTimeFormatter,
        };

        public ApplicationLoadBalancerResourceChartsViewModel(string loadBalancerArn,
            IAWSToolkitShellProvider toolkitShell)
            : base(toolkitShell)
        {
            _loadBalancerArn = loadBalancerArn;
            ResourceType = "Application Load Balancer";
            ResourceName = loadBalancerArn.Split('/').Last();

            Charts.Add(_requests);
        }

        public override async Task LoadAsync(CloudWatchMetrics metrics, int hours)
        {
            List<Dimension> dimensions = new List<Dimension>
            {
                new Dimension() { Name = "LoadBalancer", Value = _loadBalancerArn }
            };

            await Task.WhenAll(
                LoadCloudWatchMetricAsync("RequestCount", "AWS/ApplicationELB",
                    dimensions, CloudWatchMetrics.Aggregate.Sum, StandardUnit.Count,
                    hours, metrics, _requests)
            );
        }
    }
}
