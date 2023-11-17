using System.ComponentModel.Composition;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Telemetry.Model;

namespace Amazon.AwsToolkit.CodeWhisperer.Telemetry
{
    /// <summary>
    /// MEF Component interface responsible for handling telemetry event notification from the language server.
    /// The publisher's implementation is largely internal, but this interface provides us with
    /// a clean stub point where needed.
    /// </summary>
    public interface ICodeWhispererTelemetryEventPublisher
    {
    }

    [Export(typeof(ICodeWhispererTelemetryEventPublisher))]
    internal class CodeWhispererTelemetryEventPublisher : TelemetryEventPublisher, ICodeWhispererTelemetryEventPublisher
    {
        [ImportingConstructor]
        public CodeWhispererTelemetryEventPublisher(ICodeWhispererLspClient lspClient,
            IToolkitContextProvider toolkitContextProvider) : base(lspClient, toolkitContextProvider)
        {
        }

        internal override MetricDatum CreateMetricDatumWithRequiredData(string metricName)
        {
            var datum = new MetricDatum
            {
                Metadata = { [MetadataKeys.AwsAccount] = MetadataValue.NotApplicable },
                Unit = Unit.None,
                Passive = true
            };
            return datum;
        }
    }
}
