using System;

using Amazon.AWSToolkit.Exceptions;

namespace Amazon.AwsToolkit.CodeWhisperer.Credentials
{
    public class LspConnectionToolkitException : ToolkitException
    {
        public enum LspConnectionErrorCode
        {
            UnexpectedLspCredentialSetError
        }

        public LspConnectionToolkitException(string message, LspConnectionErrorCode errorCode) : this(message,
            errorCode,
            null)
        {
        }

        public LspConnectionToolkitException(string message, LspConnectionErrorCode errorCode, Exception e)
            : base(message, errorCode.ToString(), e)
        {
        }
    }
}
