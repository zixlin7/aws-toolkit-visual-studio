using Amazon.AwsToolkit.Telemetry.Events.Generated;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Install
{
    /// <summary>
    /// Represents args required to record lsp installation metric
    /// </summary>
    public class RecordLspInstallerArgs
    {
        public string Id { get; set; }

        public LanguageServerLocation Location { get; set; }

        public string LanguageServerVersion { get; set; }

        public string ManifestSchemaVersion { get; set; }

        public long Duration { get; set; }
    }
}
