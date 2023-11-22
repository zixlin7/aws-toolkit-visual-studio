using Amazon.AwsToolkit.Telemetry.Events.Generated;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Install
{
    /// <summary>
    /// Represents the result of the Lsp installation operation
    /// </summary>
    public class LspInstallResult
    {
        /// <summary>
        /// Path of installation
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Language server version installed
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Location where language server is installed from <see cref="LanguageServerLocation"/>
        /// </summary>
        public LanguageServerLocation Location { get; set; }
    }
}
