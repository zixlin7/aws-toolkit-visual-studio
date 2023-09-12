using System;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Context;

namespace Amazon.AWSToolkit.Tests.Common.Context
{
    /// <summary>
    /// A fake ToolkitContextProvider that wraps a ToolkitContext mocked through
    /// ToolkitContextFixture.
    /// </summary>
    public class FakeToolkitContextProvider : IToolkitContextProvider
    {
        private readonly ToolkitContext _toolkitContext;
        public bool HaveToolkitContext = true;
        public Action Initialized;

        public FakeToolkitContextProvider(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public bool HasToolkitContext()
        {
            return HaveToolkitContext;
        }

        public ToolkitContext GetToolkitContext()
        {
            return _toolkitContext;
        }

        public Task<ToolkitContext> WaitForToolkitContextAsync()
        {
            return Task.FromResult(GetToolkitContext());
        }

        public Task<ToolkitContext> WaitForToolkitContextAsync(int timeoutMs)
        {
            return WaitForToolkitContextAsync();
        }

        public void RegisterOnInitializedCallback(Action callback)
        {
            Initialized += callback;
        }

        public void RaiseInitialized()
        {
            Initialized?.Invoke();
        }
    }
}
