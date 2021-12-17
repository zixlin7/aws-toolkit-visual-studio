using System;
using System.Collections.Generic;
using System.Linq;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Models;

using LiveCharts;
using LiveCharts.Defaults;

namespace Amazon.AWSToolkit.ViewModels.Charts
{
    public class MonitorGraphViewModel : BaseModel
    {
        public static string TicksTimeFormatter(double value)
        {
            if (double.IsNaN(value))
            {
                return string.Empty;
            }

            var ticks = Math.Max((long) value, DateTime.MinValue.Ticks);
            var dateTime = new DateTime(ticks);
            return dateTime.ToString("t");
        }

        private string _title;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private ChartValues<DateTimePoint> _values;

        public ChartValues<DateTimePoint> Values
        {
            get => _values;
            set => SetProperty(ref _values, value);
        }

        public Func<double, string> LabelFormatter { get; set; }

        private double _steps = double.NaN;
        public double Steps
        {
            get => _steps;
            private set => SetProperty(ref _steps, value);
        }

        public void ApplyMetrics(IList<TimestampedValue<double>> values)
        {
            Values = new ChartValues<DateTimePoint>(values
                    .Select(x => new DateTimePoint(x.DateTime, x.Value))
                    .OrderBy(point => point.DateTime));

            Steps = CalculateSteps(values);
        }

        private static double CalculateSteps(IList<TimestampedValue<double>> values)
        {
            if (!values.Any())
            {
                // NaN == Let the chart handle it. https://lvcharts.net/App/examples/v1/Wpf/Axes#separators
                return double.NaN;
            }

            var hourSpan = Math.Max(1, GetHoursSpanned(values));
            // Tick per 10 minutes or longer (don't clutter the chart)
            var minutesPerNotch = TimeSpan.FromHours(hourSpan).TotalMinutes / 6;
            return TimeSpan.FromMinutes(minutesPerNotch).Ticks;
        }

        private static int GetHoursSpanned(IList<TimestampedValue<double>> values)
        {
            if (!values.Any())
            {
                return 0;
            }

            var maxTime = values.Max(x => x.DateTime);
            var minTime = values.Min(x => x.DateTime);
            return (int) Math.Ceiling((maxTime - minTime).TotalHours);
        }
    }
}
