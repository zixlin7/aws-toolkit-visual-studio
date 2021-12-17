using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.SimpleWorkers;
using Amazon.AWSToolkit.ViewModels.Charts;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;

namespace Amazon.AWSToolkit.CloudFormation.ViewModels
{
    public class ResourceChartsViewModel : BaseModel
    {
        protected readonly IAWSToolkitShellProvider ToolkitShell;
        private string _resourceType;
        private string _resourceName;
        private ObservableCollection<MonitorGraphViewModel> _charts = new ObservableCollection<MonitorGraphViewModel>();

        public string ResourceType
        {
            get => _resourceType;
            set => SetProperty(ref _resourceType, value);
        }

        public string ResourceName
        {
            get => _resourceName;
            set => SetProperty(ref _resourceName, value);
        }

        public ObservableCollection<MonitorGraphViewModel> Charts
        {
            get => _charts;
            private set => SetProperty(ref _charts, value);
        }

        public virtual Task LoadAsync(CloudWatchMetrics metrics, int hours)
        {
            return Task.CompletedTask;
        }

        public ResourceChartsViewModel(IAWSToolkitShellProvider toolkitShell)
        {
            ToolkitShell = toolkitShell;
        }

        protected async Task LoadCloudWatchMetricAsync(
            string metricName, string metricNamespace,
            ICollection<Dimension> dimensions, CloudWatchMetrics.Aggregate statsAggregate, StandardUnit units,
            int hours, CloudWatchMetrics metrics, MonitorGraphViewModel viewModel)
        {
            var data = await metrics.LoadMetricsAsync(
                metricName, metricNamespace, dimensions, statsAggregate, units, hours);

            ToolkitShell.ExecuteOnUIThread(() => viewModel.ApplyMetrics(data));
        }
    }
}
