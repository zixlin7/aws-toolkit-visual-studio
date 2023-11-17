using System;

using Amazon.AWSToolkit.Exceptions;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp
{
    public class LspToolkitException : ToolkitException
    {
        public enum LspErrorCode
        {
            InvalidVersionManifest,
            UnexpectedManifestError,
            UnexpectedManifestFetchError,
            NoValidLspFallback,
            NoSystemCompatibleLspVersion,
            NoCompatibleLspVersion
        }

        public LspToolkitException(string message, LspErrorCode errorCode) : this(message, errorCode, null) { }

        public LspToolkitException(string message, LspErrorCode errorCode, Exception e)
            : base(message, errorCode.ToString(), e) { }
    }
}
