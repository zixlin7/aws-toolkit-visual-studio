using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.SimpleWorkers;
using Amazon.AWSToolkit.ViewModels.Charts;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;

namespace Amazon.AWSToolkit.CloudFormation.ViewModels
{
    public class LambdaResourceChartsViewModel : ResourceChartsViewModel
    {
        private readonly MonitorGraphViewModel _invokes = new MonitorGraphViewModel()
        {
            Title = ResourceGraphTitles.Invocations, LabelFormatter = MonitorGraphViewModel.TicksTimeFormatter,
        };

        private readonly MonitorGraphViewModel _duration = new MonitorGraphViewModel()
        {
            Title = ResourceGraphTitles.Duration, LabelFormatter = MonitorGraphViewModel.TicksTimeFormatter,
        };

        private readonly MonitorGraphViewModel _errors = new MonitorGraphViewModel()
        {
            Title = ResourceGraphTitles.Errors, LabelFormatter = MonitorGraphViewModel.TicksTimeFormatter,
        };

        private readonly MonitorGraphViewModel _throttles = new MonitorGraphViewModel()
        {
            Title = ResourceGraphTitles.Throttles, LabelFormatter = MonitorGraphViewModel.TicksTimeFormatter,
        };

        public LambdaResourceChartsViewModel(string functionName, IAWSToolkitShellProvider toolkitShell)
            : base(toolkitShell)
        {
            ResourceType = "Lambda Function";
            ResourceName = functionName;

            Charts.Add(_invokes);
            Charts.Add(_duration);
            Charts.Add(_errors);
            Charts.Add(_throttles);
        }

        public override async Task LoadAsync(CloudWatchMetrics metrics, int hours)
        {
            List<Dimension> dimensions = new List<Dimension>
            {
                new Dimension() { Name = "FunctionName", Value = ResourceName }
            };

            await Task.WhenAll(
                LoadCloudWatchMetricAsync("Invocations", "AWS/Lambda",
                    dimensions, CloudWatchMetrics.Aggregate.Sum, StandardUnit.Count,
                    hours, metrics, _invokes),
                LoadCloudWatchMetricAsync("Duration", "AWS/Lambda",
                    dimensions, CloudWatchMetrics.Aggregate.Average, StandardUnit.Milliseconds,
                    hours, metrics, _duration),
                LoadCloudWatchMetricAsync("Errors", "AWS/Lambda",
                    dimensions, CloudWatchMetrics.Aggregate.Sum, StandardUnit.Count,
                    hours, metrics, _errors),
                LoadCloudWatchMetricAsync("Throttles", "AWS/Lambda",
                    dimensions, CloudWatchMetrics.Aggregate.Sum, StandardUnit.Count,
                    hours, metrics, _throttles)
            );
        }
    }
}
