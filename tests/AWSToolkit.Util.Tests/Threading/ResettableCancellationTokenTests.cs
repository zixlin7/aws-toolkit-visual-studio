using System;

using Amazon.AWSToolkit.Threading;

using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Threading
{
    public class ResettableCancellationTokenTests : IDisposable
    {
        private readonly ResettableCancellationToken _sut = new ResettableCancellationToken();

        [Fact]
        public void Cancel()
        {
            var token = _sut.Token;

            Assert.False(_sut.IsCancellationRequested);
            Assert.False(token.IsCancellationRequested);

            _sut.Cancel();

            Assert.True(_sut.IsCancellationRequested);
            Assert.True(token.IsCancellationRequested);
        }

        [Fact]
        public void Reset()
        {
            var token = _sut.Token;

            var newToken = _sut.Reset();

            Assert.True(token.IsCancellationRequested);
            Assert.False(newToken.IsCancellationRequested);

            Assert.Equal(newToken, _sut.Token);
            Assert.NotEqual(token, _sut.Token);
        }

        public void Dispose()
        {
            _sut.Dispose();
        }
    }
}
