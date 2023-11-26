using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Context;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace Amazon.AWSToolkit.VisualStudio.Services
{
    /// <summary>
    /// Allows us to dependency inject the ToolkitContext into systems.
    /// This is primarily intended for use with MEF components, which
    /// can be activated independently from the main AWS Toolkit package.
    ///
    /// MEF components can Import a IToolkitContextProvider and wait
    /// for this object to have a ToolkitContext as part of their initialization routine.
    /// </summary>
    [Export(typeof(IToolkitContextProvider))]
    internal class ToolkitContextProvider : IToolkitContextProvider, IDisposable
    {
        private static readonly object _onInitializedLock = new object();

        private Action _onInitialized;

        private bool _isDisposed;
        private ToolkitContext _toolkitContext;
        private readonly ManualResetEvent _waitForContextSync = new ManualResetEvent(false);

        [ImportingConstructor]
        public ToolkitContextProvider()
        {
        }

        public void Initialize(ToolkitContext toolkitContext)
        {
            lock (_onInitializedLock)
            {
                if (HasToolkitContext())
                {
                    throw new InvalidOperationException("ToolkitContext was already initialized");
                }

                _toolkitContext = toolkitContext;
            }

            _waitForContextSync.Set();

            _onInitialized?.Invoke();
            _onInitialized = null;
        }

        public bool HasToolkitContext()
        {
            return _toolkitContext != null;
        }

        public ToolkitContext GetToolkitContext()
        {
            return _toolkitContext ?? throw new InvalidOperationException("ToolkitContext Provider has not been initialized");
        }

        public Task<ToolkitContext> WaitForToolkitContextAsync()
        {
            return WaitForToolkitContextAsync(-1);
        }

        public async Task<ToolkitContext> WaitForToolkitContextAsync(int timeoutMs)
        {
            // Avoid the risk of deadlocking on the UI thread
            // TODO : IDE-11472 : Check for UI Thread (requires tests that juggle threads)
            // ThreadHelper.ThrowIfOnUIThread();

            await TaskScheduler.Default;

            // Calling HasToolkitContext is an optimization to avoid native code calls from WaitOne
            return HasToolkitContext() || _waitForContextSync.WaitOne(timeoutMs)
                ? GetToolkitContext()
                : throw new TimeoutException("ToolkitContext was not available.");
        }

        public void RegisterOnInitializedCallback(Action callback)
        {
            lock (_onInitializedLock)
            {
                if (HasToolkitContext())
                {
                    callback.Invoke();
                }
                else
                {
                    _onInitialized += callback;
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _waitForContextSync.Dispose();
                }

                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
