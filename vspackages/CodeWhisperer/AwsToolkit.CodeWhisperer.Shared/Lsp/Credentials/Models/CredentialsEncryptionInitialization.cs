namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials.Models
{
    /// <summary>
    /// Model for the payload sent to the language server when passing
    /// credentials encryption details at startup.
    /// </summary>
    internal class CredentialsEncryptionInitialization
    {
        internal static class Modes
        {
            public static string Jwt = "JWT";
            public static string PasetoV3Local = "v3.local";
        }

        /// <summary>
        /// Allows language servers to maintain backwards compatibility, and check if they support this payload.
        /// </summary>
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// How the Toolkit’s credentials messages will be encoded.
        /// <see cref="Modes"/>
        /// </summary>
        public string Mode { get; set; }

        /// <summary>
        /// Encryption key used to encode credentials messages for this session.
        /// </summary>
        public string Key { get; set; }
    }
}
