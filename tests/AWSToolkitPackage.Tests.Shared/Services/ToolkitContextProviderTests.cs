using System;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.AWSToolkit.VisualStudio.Services;

using Xunit;

namespace AWSToolkitPackage.Tests.Services
{
    public class ToolkitContextProviderTests
    {
        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();
        private readonly ToolkitContextProvider _sut = new ToolkitContextProvider();

        [Fact]
        public void InitializeThrowsOnSubsequentCalls()
        {
            _sut.Initialize(_toolkitContextFixture.ToolkitContext);
            Assert.Throws<InvalidOperationException>(() => _sut.Initialize(_toolkitContextFixture.ToolkitContext));
        }

        [Fact]
        public void HasToolkitContext_WithContext()
        {
            _sut.Initialize(_toolkitContextFixture.ToolkitContext);
            Assert.True(_sut.HasToolkitContext());
        }

        [Fact]
        public void HasToolkitContext_WithoutContext()
        {
            Assert.False(_sut.HasToolkitContext());
        }

        [Fact]
        public void GetToolkitContext_WithContext()
        {
            _sut.Initialize(_toolkitContextFixture.ToolkitContext);
            Assert.Equal(_toolkitContextFixture.ToolkitContext, _sut.GetToolkitContext());
        }

        [Fact]
        public void GetToolkitContext_WithoutContext()
        {
            Assert.Throws<InvalidOperationException>(() => _sut.GetToolkitContext());
        }

        [Fact]
        public async Task WaitForToolkitContextAsync_WithContext()
        {
            _sut.Initialize(_toolkitContextFixture.ToolkitContext);
            Assert.Equal(_toolkitContextFixture.ToolkitContext, await _sut.WaitForToolkitContextAsync());
        }

        [Fact]
        public async Task WaitForToolkitContextAsync_WithoutContext()
        {
            var shortTimeoutMs = 250;
            await Assert.ThrowsAsync<TimeoutException>(async () => await _sut.WaitForToolkitContextAsync(shortTimeoutMs));
        }

        [Fact]
        public void RegisterInitializedCallback()
        {
            var timesCalled = 0;

            void callback() => timesCalled++;

            _sut.RegisterOnInitializedCallback(callback);
            _sut.Initialize(_toolkitContextFixture.ToolkitContext);

            Assert.Equal(1, timesCalled);
        }
    }
}
