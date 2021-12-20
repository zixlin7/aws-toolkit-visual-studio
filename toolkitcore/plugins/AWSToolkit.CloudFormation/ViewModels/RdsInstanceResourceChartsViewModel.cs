using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.SimpleWorkers;
using Amazon.AWSToolkit.ViewModels.Charts;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;

namespace Amazon.AWSToolkit.CloudFormation.ViewModels
{
    public class RdsInstanceResourceChartsViewModel : ResourceChartsViewModel
    {
        private readonly MonitorGraphViewModel _cpuUsage = new MonitorGraphViewModel()
        {
            Title = ResourceGraphTitles.CpuUtilization, LabelFormatter = MonitorGraphViewModel.TicksTimeFormatter,
        };

        private readonly MonitorGraphViewModel _freeStorage = new MonitorGraphViewModel()
        {
            Title = ResourceGraphTitles.FreeSpace, LabelFormatter = MonitorGraphViewModel.TicksTimeFormatter,
        };

        public RdsInstanceResourceChartsViewModel(string dbInstanceId, IAWSToolkitShellProvider toolkitShell)
            : base(toolkitShell)
        {
            ResourceType = "RDS Instance";
            ResourceName = dbInstanceId;

            Charts.Add(_cpuUsage);
            Charts.Add(_freeStorage);
        }

        public override async Task LoadAsync(CloudWatchMetrics metrics, int hours)
        {
            List<Dimension> dimensions = new List<Dimension>
            {
                new Dimension() { Name = "DBInstanceIdentifier", Value = ResourceName }
            };

            await Task.WhenAll(
                LoadCloudWatchMetricAsync("CPUUtilization", "AWS/RDS",
                    dimensions, CloudWatchMetrics.Aggregate.Average, StandardUnit.Percent,
                    hours, metrics, _cpuUsage),
                LoadCloudWatchMetricAsync("FreeStorageSpace", "AWS/RDS",
                    dimensions, CloudWatchMetrics.Aggregate.Minimum, StandardUnit.Bytes,
                    hours, metrics, _freeStorage)
            );
        }
    }
}
