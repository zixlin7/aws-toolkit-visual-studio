namespace AwsToolkit.VsSdk.Common.Settings
{
    /// <summary>
    /// Lsp settings
    /// </summary>
    public interface ILspSettings
    {
        /// <summary>
        /// Provides Toolkit developers a way to side-load the language server into the Toolkit.
        /// This way we don't need a specific version of a language server in order to test things out.
        /// This is also an escape hatch in case we need to troubleshoot a test build with a customer.
        /// </summary>
        string LanguageServerPath { get; set; }

        /// <summary>
        /// Provides Toolkit developers a way to load the lsp version manifest from a local folder
        /// This provides a way to test things locally while the remote version is still under development
        /// </summary>
        string VersionManifestFolder { get; set; }

    }
}
