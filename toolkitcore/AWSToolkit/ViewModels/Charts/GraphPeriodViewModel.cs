using System;
using System.Collections.ObjectModel;
using System.Linq;

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.ViewModels.Charts
{
    public class MonitorPeriod
    {
        public string DisplayName { get; set; }
        public int Hours { get; set; }
    }

    public class GraphPeriodViewModel : BaseModel
    {
        private readonly ObservableCollection<MonitorPeriod> _periods = new ObservableCollection<MonitorPeriod>();
        private MonitorPeriod _selectedPeriod;

        public ObservableCollection<MonitorPeriod> Periods => _periods;

        public MonitorPeriod SelectedPeriod
        {
            get => _selectedPeriod;
            set => SetProperty(ref _selectedPeriod, value);
        }

        public GraphPeriodViewModel()
        {
            _periods.Add(new MonitorPeriod() { DisplayName = "Last Hour", Hours = 1 });
            _periods.Add(new MonitorPeriod() { DisplayName = "Last 3 Hours", Hours = 3 });
            _periods.Add(new MonitorPeriod() { DisplayName = "Last 6 Hours", Hours = 6 });
            _periods.Add(new MonitorPeriod() { DisplayName = "Last 12 Hours", Hours = 12 });
            _periods.Add(new MonitorPeriod() { DisplayName = "Last 24 Hours", Hours = 24 });
            _periods.Add(new MonitorPeriod() { DisplayName = "Last 1 Week", Hours = (int) TimeSpan.FromDays(7).TotalHours });
            _periods.Add(new MonitorPeriod() { DisplayName = "Last 2 Weeks", Hours = (int) TimeSpan.FromDays(14).TotalHours });

            _selectedPeriod = _periods.First();
        }
    }
}
