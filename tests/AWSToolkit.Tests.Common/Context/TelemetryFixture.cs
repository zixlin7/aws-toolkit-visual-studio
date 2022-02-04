using System.Collections.Generic;
using System.Linq;

using Amazon.AwsToolkit.Telemetry.Events.Core;

using Moq;

namespace Amazon.AWSToolkit.Tests.Common.Context
{
    public class TelemetryFixture
    {
        public readonly Mock<ITelemetryLogger> TelemetryLogger = new Mock<ITelemetryLogger>();
        public readonly List<Metrics> LoggedMetrics = new List<Metrics>();

        public TelemetryFixture()
        {
            TelemetryLogger.Setup(mock => mock.Record(It.IsAny<Metrics>()))
                .Callback<Metrics>(metrics => { LoggedMetrics.Add(metrics); });
        }

        public void AssertTelemetryRecordCalls(int count)
        {
            TelemetryLogger.Verify(mock => mock.Record(It.IsAny<Metrics>()), Times.Exactly(count));
        }

        public IList<Metrics> GetMetricsByMetricName(string metricName)
        {
            return LoggedMetrics
                .Where(metrics => metrics.Data.Any(datum => datum.MetricName == metricName))
                .ToList();
        }
    }
}
