using System;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials.Models
{
    /// <summary>
    /// Used with events signalling that a language server has requested
    /// the current auth connection information.
    /// </summary>
    public class ConnectionMetadataEventArgs : EventArgs
    {
        /// <summary>
        /// The handler sets this to the object that will be sent back to
        /// the language server as a response to its request.
        /// </summary>
        public ConnectionMetadata Response { get; set; }
    }
}
