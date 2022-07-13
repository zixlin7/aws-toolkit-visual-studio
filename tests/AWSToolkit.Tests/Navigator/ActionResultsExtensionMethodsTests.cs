using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Navigator;

using Xunit;

namespace AWSToolkit.Tests.Navigator
{
    public class ActionResultsExtensionMethodsTests
    {
        [Fact]
        public void AsTelemetryResult_Null()
        {
            ActionResults results = null;
            Assert.Equal(Result.Failed, results.AsTelemetryResult());
        }

        [Fact]
        public void AsTelemetryResult_Success()
        {
            Assert.Equal(Result.Succeeded, new ActionResults().WithSuccess(true).AsTelemetryResult());
        }

        [Fact]
        public void AsTelemetryResult_Failed()
        {
            Assert.Equal(Result.Failed, new ActionResults().WithSuccess(false).AsTelemetryResult());
        }

        [Fact]
        public void AsTelemetryResult_Cancelled()
        {
            Assert.Equal(Result.Cancelled, new ActionResults().WithSuccess(false).WithCancelled(true).AsTelemetryResult());
        }
    }
}
