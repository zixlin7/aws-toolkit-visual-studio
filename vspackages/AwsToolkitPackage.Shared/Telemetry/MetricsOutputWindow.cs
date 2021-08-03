using System;
using System.Diagnostics;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Telemetry;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using Newtonsoft.Json;

using Task = System.Threading.Tasks.Task;

namespace Amazon.AWSToolkit.VisualStudio.Telemetry
{
    /// <summary>
    /// Writes metrics details to a VS OutputWindow Pane, if a pane was created.
    /// </summary>
    public class MetricsOutputWindow : IDisposable, IMetricsOutputWindow
    {
        private readonly IVsOutputWindow _outputWindowManager;
        private IVsOutputWindowPane _outputWindowPane;

        private Guid _windowPaneGuid = GuidList.MetricsOutputWindowPane;

        public MetricsOutputWindow(IVsOutputWindow outputWindowManager)
        {
            _outputWindowManager = outputWindowManager;
        }

        public async Task CreatePane()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (_outputWindowManager.CreatePane(
                ref _windowPaneGuid,
                "AWS Toolkit Metrics",
                Convert.ToInt32(true),
                Convert.ToInt32(false)) != VSConstants.S_OK)
            {
                return;
            }

            _outputWindowManager.GetPane(ref _windowPaneGuid, out _outputWindowPane);
        }

        public void DeletePane()
        {
            _outputWindowManager.DeletePane(ref _windowPaneGuid);
            _outputWindowPane = null;
        }

        public void Output(Metrics telemetryMetric)
        {
            try
            {
                var json = JsonConvert.SerializeObject(telemetryMetric, Formatting.Indented);
                WriteText(json);
            }
            catch (Exception e)
            {
                Debug.Assert(false, "Failed to output the Toolkit metric", e.ToString());
            }
        }

        public void Dispose()
        {
            DeletePane();
        }

        private void WriteText(string message)
        {
            _outputWindowPane?.OutputStringThreadSafe($"{message}{Environment.NewLine}");
        }
    }
}
