using Amazon.AWSToolkit.Publish.Util;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Util
{
    public class DisposingActionTests
    {
        [Fact]
        public void Dispose()
        {
            bool wasCalled = false;
            void OnDispose() => wasCalled = true;

            var sut = new DisposingAction(OnDispose);
            sut.Dispose();

            Assert.True(wasCalled);
        }
    }
}
