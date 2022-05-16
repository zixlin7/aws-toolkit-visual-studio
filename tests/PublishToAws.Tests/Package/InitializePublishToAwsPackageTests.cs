using System;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Publish.Package;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Package
{
    public class InitializePublishToAwsPackageTests
    {
        private readonly InitializePublishToAwsPackage _sut = new InitializePublishToAwsPackage();

        public InitializePublishToAwsPackageTests()
        {
        }

        [Fact]
        public async Task Initialize()
        {
            var eventArgs = await Assert.RaisesAsync<EventArgs>(
                listener => _sut.Initialize += listener,
                listener => _sut.Initialize -= listener,
                async () => await _sut.InitializePackageAsync(null, null)
            );

            Assert.NotNull(eventArgs);
            Assert.Equal(_sut, eventArgs.Sender);
        }

        [Fact]
        public async Task InitializeMultipleTimes()
        {
            await _sut.InitializePackageAsync(null, null);

            await Assert.ThrowsAsync<Exception>(async () =>
            {
                await _sut.InitializePackageAsync(null, null);
            });
        }
    }
}
