using System.Collections.Generic;

namespace Amazon.AWSToolkit.CommonUI.Charts
{
    public class CloudWatchDataFetcher
    {
        public class MonitorPeriod
        {
            static List<MonitorPeriod> _periods;
            public static IEnumerable<MonitorPeriod> Periods
            {
                get
                {
                    if (_periods == null)
                    {
                        _periods = new List<MonitorPeriod>();
                        _periods.Add(new MonitorPeriod("Last Hour", 1));
                        _periods.Add(new MonitorPeriod("Last 3 Hour", 3));
                        _periods.Add(new MonitorPeriod("Last 6 Hour", 6));
                        _periods.Add(new MonitorPeriod("Last 12 Hour", 12));
                        _periods.Add(new MonitorPeriod("Last 24 Hour", 24));
                        _periods.Add(new MonitorPeriod("Last 1 Week", 24 * 7));
                        _periods.Add(new MonitorPeriod("Last 2 Week", 24 * 14));
                    }

                    return _periods;
                }
            }

            public MonitorPeriod(string displayName, int hoursInPast)
            {
                this.DisplayName = displayName;
                this.HoursInPast = hoursInPast;
            }

            public string DisplayName { get; set; }
            public int HoursInPast { get; set; }

            public override string ToString()
            {
                return this.DisplayName;
            }
        }
    }
}
