using Amazon.AWSToolkit.SQS.Util;

using Xunit;

namespace AWSToolkit.Tests.SQS
{
    public class SqsHelpersTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("testqueue")]
        [InlineData("sample.fifo.queue")]
        [InlineData("fifo.samplequeue")]
        public void IsFifo_False(string queueName)
        {
            Assert.False(SqsHelpers.IsFifo(queueName));
        }

        [Fact]
        public void IsFifo_True()
        {
            Assert.True(SqsHelpers.IsFifo("samplequeue.fifo"));
        }
    }
}
