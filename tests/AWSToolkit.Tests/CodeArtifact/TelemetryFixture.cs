using System.Collections.Generic;
using System.Linq;
using Amazon.AWSToolkit.Lambda.Util;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Moq;
using Xunit;

namespace AWSToolkit.Tests.CodeArtifact
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

        public void AssertCodeArtifactMetrics(Metrics metrics, Result expectedResult, string metricName, string pkgType)
        {
            Assert.Equal(1, metrics.Data.Count);

            var datum = metrics.Data.First();

            Assert.Equal(metricName, datum.MetricName);
            Assert.Equal(expectedResult.ToString(), datum.Metadata["result"]);
            Assert.Equal(pkgType, datum.Metadata["packageType"]);
        }
    }
}