using System.Collections.ObjectModel;

using Amazon.AWSToolkit.ViewModels.Charts;

namespace Amazon.AWSToolkit.CloudFormation.ViewModels
{
    public class StackMonitoringViewModel
    {
        private readonly GraphPeriodViewModel _graphPeriods = new GraphPeriodViewModel();

        private readonly ObservableCollection<ResourceChartsViewModel> _charts =
            new ObservableCollection<ResourceChartsViewModel>();

        public GraphPeriodViewModel GraphPeriod => _graphPeriods;
        public ObservableCollection<ResourceChartsViewModel> Charts => _charts;
    }
}
