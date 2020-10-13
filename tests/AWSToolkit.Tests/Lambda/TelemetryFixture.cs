using System.Collections.Generic;
using System.Linq;
using Amazon.AWSToolkit.Lambda.Util;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Moq;
using Xunit;

namespace AWSToolkit.Tests.Lambda
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

        public void AssertDeployLambdaMetrics(Metrics metrics, Result expectedResult, LambdaTelemetryUtils.RecordLambdaDeployProperties expectedProperties)
        {
            Assert.Equal(1, metrics.Data.Count);

            var datum = metrics.Data.First();
                        
            Assert.Equal("lambda_deploy", datum.MetricName);
            Assert.Equal(expectedResult.ToString(), datum.Metadata["result"]);
            Assert.Equal(expectedProperties.RegionId, datum.Metadata["regionId"]);
            Assert.Equal(expectedProperties.NewResource ? "true" : "false", datum.Metadata["initialDeploy"]);
            Assert.Equal(expectedProperties.Runtime.Value, datum.Metadata["runtime"]);

            if (string.IsNullOrEmpty(expectedProperties.TargetFramework))
            {
                Assert.False(datum.Metadata.ContainsKey("platform"));
            }
            else
            {
                Assert.Equal(expectedProperties.TargetFramework, datum.Metadata["platform"]);
            }
        }
    }
}