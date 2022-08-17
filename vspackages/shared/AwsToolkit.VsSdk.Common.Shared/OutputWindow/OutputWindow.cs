using System;
using System.Collections.Concurrent;
using System.Text;

using Amazon.AWSToolkit.Util;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

using Task = System.Threading.Tasks.Task;

namespace Amazon.AwsToolkit.VsSdk.Common.OutputWindow
{
    /// <summary>
    /// Writes text to a VS OutputWindow Pane, if a pane was created.
    /// </summary>
    public class OutputWindow : IDisposable
    {
        private const int WriteQueuedTextIntervalMs = 333;
        private readonly IVsOutputWindow _outputWindowManager;
        private IVsOutputWindowPane _outputWindowPane;

        private Guid _windowPaneId;
        private readonly string _name;

        private readonly ThrottleDispatcher _throttleDispatcher = new ThrottleDispatcher();
        private volatile ConcurrentQueue<string> _queuedMessages = new ConcurrentQueue<string>();

        public OutputWindow(Guid windowPaneId, string name, IVsOutputWindow outputWindowManager)
        {
            _windowPaneId = windowPaneId;
            _name = name;
            _outputWindowManager = outputWindowManager;
        }

        public async Task InitializeAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (_outputWindowManager.CreatePane(
                    ref _windowPaneId,
                    _name,
                    Convert.ToInt32(true),
                    Convert.ToInt32(false)) != VSConstants.S_OK)
            {
                return;
            }

            _outputWindowManager.GetPane(ref _windowPaneId, out _outputWindowPane);

            // Output any queued up messages
            _throttleDispatcher.Throttle(WriteQueuedTextIntervalMs, _ => WriteQueuedText());
        }

        public void Show()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _outputWindowPane.Activate();
        }

        public void Dispose()
        {
            _throttleDispatcher.Dispose();
            DeletePane();
        }

        private void DeletePane()
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                _outputWindowManager?.DeletePane(ref _windowPaneId);
                _outputWindowPane = null;
            });
        }

        public void WriteText(string message)
        {
            _queuedMessages.Enqueue(message);
            _throttleDispatcher.Throttle(WriteQueuedTextIntervalMs, _ => WriteQueuedText());
        }

        private void WriteQueuedText()
        {
            // Wait for the pane to be instantiated
            if (_outputWindowPane == null) { return; }

            var queuedText = new StringBuilder();
            while (_queuedMessages.TryDequeue(out string text))
            {
                queuedText.AppendLine(text);
            }

            _outputWindowPane.OutputStringThreadSafe(queuedText.ToString());
        }
    }
}
