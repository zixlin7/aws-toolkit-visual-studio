using Amazon.AWSToolkit.ViewModels.Charts;

namespace Amazon.AWSToolkit.CloudFormation.ViewModels
{
    public class DesignSampleResourceChartsViewModel : ResourceChartsViewModel
    {
        public DesignSampleResourceChartsViewModel()
            : base(null)
        {
            ResourceName = "Sample Resource Name";
            ResourceType = "Sample Resource Type";

            var vm = new MonitorGraphViewModel() { Title = "Sample Chart", };

            Charts.Add(vm);
            Charts.Add(vm);
            Charts.Add(vm);
            Charts.Add(vm);
        }
    }
}
