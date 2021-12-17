using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.ViewModels.Charts;

namespace Amazon.AWSToolkit.ElasticBeanstalk.ViewModels
{
    public class MonitorGraphsViewModel : BaseModel
    {
        private MonitorGraphViewModel _latency = new MonitorGraphViewModel()
        {
            Title = "Latency (Seconds)",
            LabelFormatter = MonitorGraphViewModel.TicksTimeFormatter,
        };

        public MonitorGraphViewModel Latency
        {
            get => _latency;
            private set => SetProperty(ref _latency, value);
        }

        private MonitorGraphViewModel _requests = new MonitorGraphViewModel()
        {
            Title = "Request Count",
            LabelFormatter = MonitorGraphViewModel.TicksTimeFormatter,
        };

        public MonitorGraphViewModel Requests
        {
            get => _requests;
            private set => SetProperty(ref _requests, value);
        }

        private MonitorGraphViewModel _networkIn = new MonitorGraphViewModel()
        {
            Title = "Max Network In (Bytes)",
            LabelFormatter = MonitorGraphViewModel.TicksTimeFormatter,
        };

        public MonitorGraphViewModel NetworkIn
        {
            get => _networkIn;
            private set => SetProperty(ref _networkIn, value);
        }

        private MonitorGraphViewModel _networkOut = new MonitorGraphViewModel()
        {
            Title = "Max Network Out (Bytes)",
            LabelFormatter = MonitorGraphViewModel.TicksTimeFormatter,
        };

        public MonitorGraphViewModel NetworkOut
        {
            get => _networkOut;
            private set => SetProperty(ref _networkOut, value);
        }

        private MonitorGraphViewModel _cpuUsage = new MonitorGraphViewModel()
        {
            Title = "CPU Utilization",
            LabelFormatter = MonitorGraphViewModel.TicksTimeFormatter,
        };

        public MonitorGraphViewModel CpuUsage
        {
            get => _cpuUsage;
            private set => SetProperty(ref _cpuUsage, value);
        }
    }
}
