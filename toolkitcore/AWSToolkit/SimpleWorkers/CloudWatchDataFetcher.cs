using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows.Controls.DataVisualization.Charting;

using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;

using log4net;

namespace Amazon.AWSToolkit.SimpleWorkers
{
    public class CloudWatchDataFetcher
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(CloudWatchDataFetcher));

        IAmazonCloudWatch _cwClient;
        LineSeries _series;
        string _metricNamespace;
        string _metricName;
        string _stats;
        string _units;
        List<Dimension> _dimensions;
        int _hoursToView;

        public CloudWatchDataFetcher(IAmazonCloudWatch cwClient, LineSeries series, 
            string metricNamespace, string metricName, string stats, string units, 
            List<Dimension> dimensions, int hoursToView)
        {
            this._cwClient = cwClient;
            this._series = series;
            this._metricNamespace = metricNamespace;
            this._metricName = metricName;
            this._stats = stats;
            this._units = units;
            this._dimensions = dimensions;
            this._hoursToView = hoursToView;
        }

        public void Execute(object state)
        {
            try
            {
                var request = new GetMetricStatisticsRequest
                    {
                        MetricName = this._metricName,
                        Namespace = this._metricNamespace,
                        Dimensions = this._dimensions,
                        Statistics = new List<string>() { this._stats },
                        Unit = this._units,
                        Period = determinePeriod(this._hoursToView),
                        EndTimeUtc = DateTime.Now.ToUniversalTime(),
                        StartTimeUtc = DateTime.Now.ToUniversalTime().AddHours(-this._hoursToView)
                    };

                var response = this._cwClient.GetMetricStatistics(request);

                List<DataItem> dataPoints = new List<DataItem>();
                double min = double.MaxValue;
                double max = double.MinValue;
                foreach (var item in response.Datapoints)
                {
                    double point;
                    switch (this._stats)
                    {
                        case "Sum":
                            point = item.Sum;
                            break;
                        case "Average":
                            point = item.Average;
                            break;
                        case "Maximum":
                            point = item.Maximum;
                            break;
                        case "Minimum":
                            point = item.Minimum;
                            break;
                        default:
                            point = 0;
                            break;
                    }
                    dataPoints.Add(new DataItem(item.Timestamp, point));

                    if (point > max)
                        max = point;
                    if (point < min)
                        min = point;
                }

                if (dataPoints.Count == 0)
                    return;

                max = max * 1.1;
                min = min * .9;

                ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((Action)(() =>
                {
                    var yaxis = this._series.DependentRangeAxis as LinearAxis;
                    var xaxis = this._series.IndependentAxis as LinearAxis;

                    //if (max > yaxis.Maximum)
                    //    yaxis.Maximum = max;
                    //if (min < yaxis.Minimum)
                    //    yaxis.Minimum = min;

                    if (min > yaxis.Maximum)
                    {
                        yaxis.Maximum = max;
                        yaxis.Minimum = min;
                    }
                    else
                    {
                        yaxis.Minimum = min;
                        yaxis.Maximum = max;
                    }

                    double interval = (yaxis.Maximum.GetValueOrDefault() - yaxis.Minimum.GetValueOrDefault()) / 5.0;
                    if (interval > 1)
                        interval = Math.Ceiling(interval);
                    yaxis.Interval = interval;
                    this._series.ItemsSource = dataPoints;
                }));
            }
            catch (Exception e)
            {
                LOGGER.Error("Error loading cloudwatch data.", e);
            }
        }

        // scales the period (which must be in multiples of 60secs) so with
        // larger time periods, we don't overwhelm the graphs with data points
        public static int determinePeriod(int hoursToView)
        {
            int totalSeconds = hoursToView * 60 * 60;
            int periods = totalSeconds / Constants.MAX_ClOUDWATCH_DATAPOINTS;
            if (periods < Constants.MIN_CLOUDWATCH_PERIOD)
                periods = Constants.MIN_CLOUDWATCH_PERIOD;

            return periods;
        }

        internal class DataItem
        {
            public DateTime Date { get; private set; }
            public double Value { get; private set; }
            public DataItem(DateTime date, double place)
            {
                Date = date;
                Value = place;
            }
        }

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
