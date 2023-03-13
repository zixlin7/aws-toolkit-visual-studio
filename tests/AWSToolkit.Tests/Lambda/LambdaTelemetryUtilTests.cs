using System.Collections.Generic;

using Amazon.AWSToolkit.Lambda.Controller;
using Amazon.AWSToolkit.Lambda.Model;
using Amazon.AWSToolkit.Lambda.Util;
using Amazon.AWSToolkit.Telemetry.Model;

using Xunit;

namespace AWSToolkit.Tests.Lambda
{
    public class LambdaTelemetryUtilTests
    {
        public static IEnumerable<object[]> OriginatorData =>
            new List<object[]>
            {
                new object[] { UploadFunctionController.UploadOriginator.FromAWSExplorer,  CommonMetricSources.AwsExplorerMetricSource.ServiceNode},
                new object[] { UploadFunctionController.UploadOriginator.FromFunctionView, MetricSources.LambdaMetricSource.LambdaView },
                new object[] { UploadFunctionController.UploadOriginator.FromSourcePath, MetricSources.LambdaMetricSource.Project },
            };


        [Theory]
        [MemberData(nameof(OriginatorData))]
        public void AsMetricSource(UploadFunctionController.UploadOriginator originator,
            BaseMetricSource expectedSource)
        {
            Assert.Equal(expectedSource, originator.AsMetricSource());
        }
    }
}
