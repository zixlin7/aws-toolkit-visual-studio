using System;
using System.Diagnostics;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.VsSdk.Common.OutputWindow;
using Amazon.AWSToolkit.Telemetry;

using Microsoft.VisualStudio.Shell.Interop;

using Newtonsoft.Json;

namespace Amazon.AWSToolkit.VisualStudio.Telemetry
{
    /// <summary>
    /// Writes metrics details to a VS OutputWindow Pane, if a pane was created.
    /// </summary>
    public class MetricsOutputWindow : OutputWindow, IMetricsOutputWindow
    {
        private const string WindowName = "AWS Toolkit Metrics";
        public static readonly Guid WindowPaneId = new Guid("9E07E6E4-24C1-4E8A-BE36-4E99E6882D61");

        public MetricsOutputWindow(IVsOutputWindow outputWindowManager)
            : base(WindowPaneId, WindowName, outputWindowManager)
        {
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
    }
}
