using System.Linq;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Lambda.Util;

using Xunit;

namespace AWSToolkit.Tests.Lambda
{
    public static class LambdaAssert
    {
        public static void MetricIsLambdaDeploy(Metrics metrics, Result expectedResult, LambdaTelemetryUtils.RecordLambdaDeployProperties expectedProperties)
        {
            Assert.Equal(1, metrics.Data.Count);

            var datum = metrics.Data.First();
                        
            Assert.Equal("lambda_deploy", datum.MetricName);
            Assert.Equal(expectedResult.ToString(), datum.Metadata["result"]);
            Assert.Equal(expectedProperties.RegionId, datum.Metadata["awsRegion"]);
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
