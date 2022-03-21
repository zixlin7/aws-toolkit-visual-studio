using System;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Telemetry;

using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Telemetry
{
    public class ToolkitTelemetryEventsSupplementalTests
    {
        [Theory]
        [InlineData("This is some test text just to be sure this method being tested splits correctly.", 10)]
        [InlineData("12345", 5)]
        [InlineData("12345", 6)]
        [InlineData("12345", 1)]
        public void SplitAndAddMetadataSplitsALargeValueWithCorrectIndices(string text, int length)
        {
            var sut = new MetricDatum();
            var key = "testkey";
            var expectedSplits = Math.Ceiling((float) text.Length / length);

            sut.SplitAndAddMetadata(key, text, length);

            Assert.True(sut.Metadata.ContainsKey(key));
            Assert.Equal(expectedSplits, sut.Metadata.Keys.Count);

            for (int i = 1; i < expectedSplits; ++i)
            {
                var localkey = key + i;
                Assert.True(sut.Metadata.ContainsKey(localkey));
                Assert.Contains(sut.Metadata[localkey], text);
            }

            Assert.False(sut.Metadata.ContainsKey(key + "0"));
            Assert.False(sut.Metadata.ContainsKey(key + expectedSplits));
        }
    }
}
