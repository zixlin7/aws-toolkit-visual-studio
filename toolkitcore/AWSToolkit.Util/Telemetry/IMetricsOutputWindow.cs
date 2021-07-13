using Amazon.AwsToolkit.Telemetry.Events.Core;

namespace Amazon.AWSToolkit.Telemetry
{
    /// <summary>
    /// Writes Metrics details to an output window
    /// Insulates the Toolkit from the VS SDK based implementation.
    /// </summary>
    public interface IMetricsOutputWindow
    {
        /// <summary>
        /// Write the metric details to the output window
        /// </summary>
        /// <param name="telemetryMetric"></param>
        void Output(Metrics telemetryMetric);
    }
}
