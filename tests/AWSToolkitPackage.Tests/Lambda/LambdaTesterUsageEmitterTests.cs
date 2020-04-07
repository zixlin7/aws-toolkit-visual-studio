using System;
using Amazon.AWSToolkit.MobileAnalytics;
using Amazon.AWSToolkit.VisualStudio.Lambda;
using Moq;
using Xunit;

namespace AWSToolkitPackage.Tests.Lambda
{
    public class LambdaTesterUsageEmitterTests
    {
        private readonly LambdaTesterUsageEmitter _sut;
        private readonly Mock<ISimpleMobileAnalytics> _metrics = new Mock<ISimpleMobileAnalytics>();

        public LambdaTesterUsageEmitterTests()
        {
            this._sut = new LambdaTesterUsageEmitter(_metrics.Object);
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
            _sut.EmitIfLambdaTester(processName, processId);

            var expectedCallCount = metricExpected ? 1 : 0;

            _metrics.Verify(
                x => x.QueueEventToBeRecorded(It.IsAny<ToolkitEvent>()),
                Times.Exactly(expectedCallCount)
            );
        }

        [Theory]
        [InlineData("dotnet-lambda-test-tool", AttributeKeys.DotnetLambdaTestToolLaunch_UnknownVersion)]
        [InlineData("dotnet-lambda-test-tool-2.1", AttributeKeys.DotnetLambdaTestToolLaunch_2_1)]
        [InlineData("dotnet-lambda-test-tool-3.1", AttributeKeys.DotnetLambdaTestToolLaunch_3_1)]
        public void EmitLambdaTestToolMetric(string processName, AttributeKeys expectedAttributeKey)
        {
            _sut.EmitIfLambdaTester(processName, 123);

            _metrics.Verify(
                x => x.QueueEventToBeRecorded(
                    It.Is<ToolkitEvent>(evnt =>
                        evnt.Attributes.ContainsKey(expectedAttributeKey.ToString()))
                ),
                Times.Once()
            );
        }

        [Theory]
        [InlineData(123, 123, false)]
        [InlineData(123, 124, true)]
        public void EmitIfLambdaTesterProducesMetricForDifferentProcessIds(
            int processId, 
            int subsequentProcessId,
            bool subsequentMetricExpected)
        {
            _sut.EmitIfLambdaTester("dotnet-lambda-test-tool", processId);
            _sut.EmitIfLambdaTester("dotnet-lambda-test-tool", subsequentProcessId);

            var expectedCallCount = subsequentMetricExpected ? 2 : 1;

            _metrics.Verify(
                x => x.QueueEventToBeRecorded(It.IsAny<ToolkitEvent>()),
                Times.Exactly(expectedCallCount)
            );
        }
    }
}