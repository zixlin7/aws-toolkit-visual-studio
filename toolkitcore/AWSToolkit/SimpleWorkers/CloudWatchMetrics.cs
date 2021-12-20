using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Models;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;

namespace Amazon.AWSToolkit.SimpleWorkers
{
    public class CloudWatchMetrics
    {
        public enum Aggregate
        {
            Sum,
            Average,
            Minimum,
            Maximum,
        }

        private readonly IAmazonCloudWatch _cloudWatch;

        public CloudWatchMetrics(IAmazonCloudWatch cloudWatch)
        {
            _cloudWatch = cloudWatch;
        }

        public static double GetSumDatapoint(Datapoint datapoint) => datapoint.Sum;
        public static double GetAverageDatapoint(Datapoint datapoint) => datapoint.Average;
        public static double GetMaximumDatapoint(Datapoint datapoint) => datapoint.Maximum;
        public static double GetMinimumDatapoint(Datapoint datapoint) => datapoint.Minimum;

        private Func<Datapoint, double> GetAggregator(Aggregate statsAggregate)
        {
            switch (statsAggregate)
            {
                case Aggregate.Average:
                    return GetAverageDatapoint;
                case Aggregate.Sum:
                    return GetSumDatapoint;
                case Aggregate.Minimum:
                    return GetMinimumDatapoint;
                case Aggregate.Maximum:
                    return GetMaximumDatapoint;
                default:
                    throw new Exception($"Unsupported aggregate: {statsAggregate}");
            }
        }

        public async Task<IList<TimestampedValue<double>>> LoadMetricsAsync(
            string metricName,
            string metricNamespace,
            ICollection<Dimension> dimensions,
            Aggregate statsAggregate,
            StandardUnit units,
            int hours)
        {
            var request = new GetMetricStatisticsRequest
            {
                MetricName = metricName,
                Namespace = metricNamespace,
                Dimensions = dimensions.ToList(),
                Statistics = new List<string>() { statsAggregate.ToString() },
                Unit = units,
                Period = DeterminePeriod(hours),
                EndTimeUtc = DateTime.Now.ToUniversalTime(),
                StartTimeUtc = DateTime.Now.ToUniversalTime().AddHours(-hours)
            };

            var response = await _cloudWatch.GetMetricStatisticsAsync(request).ConfigureAwait(false);

            var aggregator = GetAggregator(statsAggregate);
            return response.Datapoints
                .Select(dataPoint => new TimestampedValue<double>(dataPoint.Timestamp, aggregator(dataPoint)))
                .ToList();
        }

        // scales the period (which must be in multiples of 60secs) so with
        // larger time periods, we don't overwhelm the graphs with data points
        private static int DeterminePeriod(int hoursToView)
        {
            int totalSeconds = hoursToView * 60 * 60;
            int periods = totalSeconds / Constants.MAX_ClOUDWATCH_DATAPOINTS;
            periods = Math.Max(periods, Constants.MIN_CLOUDWATCH_PERIOD);

            if (periods % 60 > 0)
            {
                periods = 60 * (1 + (periods / 60));
            }

            return periods;
        }
    }
}
