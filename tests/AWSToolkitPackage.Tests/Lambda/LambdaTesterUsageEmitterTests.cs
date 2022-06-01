using System;
using System.Linq;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.VisualStudio.Lambda;
using Moq;
using Xunit;

namespace AWSToolkitPackage.Tests.Lambda
{
    public class LambdaTesterUsageEmitterTests
    {
        private readonly LambdaTesterUsageEmitter _sut;
        private readonly Mock<ITelemetryLogger> TelemetryLogger = new Mock<ITelemetryLogger>();

        public LambdaTesterUsageEmitterTests()
        {
            this._sut = new LambdaTesterUsageEmitter(TelemetryLogger.Object);
        }

        [Fact]
        public void ThrowsWithNullMetrics()
        {
            Assert.Throws<ArgumentNullException>(() => new LambdaTesterUsageEmitter(null));
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("qwerty", false)]
        [InlineData("dotnet-lambda-test-tool", true)]
        [InlineData("dotnet-lambda-test-tool_21", true)]
        [InlineData("xxxdotnet-lambda-test-toolxxx", true)]
        public void IsLambdaTesterTheory(string processName, bool isLambdaTester)
        {
            Assert.Equal(isLambdaTester, LambdaTesterUsageEmitter.IsLambdaTester(processName));
        }

        [Theory]
        [InlineData("not-the-test-tool", 123, false)]
        [InlineData("dotnet-lambda-test-tool", -1, false)]
        [InlineData("dotnet-lambda-test-tool", 123, true)]
        public void EmitIfLambdaTesterProducesMetric(string processName, int processId, bool metricExpected)
        {
            _sut.EmitIfLambdaTester(processName, processId, true);

            var expectedCallCount = metricExpected ? 1 : 0;
            TelemetryLogger.Verify(mock => mock.Record(It.IsAny<Metrics>()), Times.Exactly(expectedCallCount));
            
        }

        [Theory]
        [InlineData("dotnet-lambda-test-tool", "unknown")]
        [InlineData("dotnet-lambda-test-tool-2.1", "dotnetcore2.1")]
        [InlineData("dotnet-lambda-test-tool-3.1", "dotnetcore3.1")]
        [InlineData("dotnet-lambda-test-tool-5.0", "dotnet5.0")]
        [InlineData("dotnet-lambda-test-tool-6.0", "dotnet6")]
        public void EmitLambdaTestToolMetric(string processName, string expectedRuntime)
        {
            _sut.EmitIfLambdaTester(processName, 123, true);

            TelemetryLogger.Verify(mock => mock.Record(It.Is<Metrics>(metrics=> metrics.Data.First().Metadata.Values.Contains(expectedRuntime)))
                , Times.Once);
        }

        [Theory]
        [InlineData(123, 123, false)]
        [InlineData(123, 124, true)]
        public void EmitIfLambdaTesterProducesMetricForDifferentProcessIds(
            int processId, 
            int subsequentProcessId,
            bool subsequentMetricExpected)
        {
            _sut.EmitIfLambdaTester("dotnet-lambda-test-tool", processId, true);
            _sut.EmitIfLambdaTester("dotnet-lambda-test-tool", subsequentProcessId, true);

            var expectedCallCount = subsequentMetricExpected ? 2 : 1;
            TelemetryLogger.Verify(mock => mock.Record(It.IsAny<Metrics>()), Times.Exactly(expectedCallCount));
        }
    }
}
