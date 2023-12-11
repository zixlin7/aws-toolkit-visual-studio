using System;

namespace Amazon.AWSToolkit.Exceptions
{
    public class ConnectionToolkitException : ToolkitException
    {
        public enum ConnectionErrorCode
        {
            UnexpectedSignOutError,
            UnexpectedSigninError,
            NoValidToken,
            UnexpectedErrorOnSave,
            NoProfilesToSave
        }

        public ConnectionToolkitException(string message, ConnectionErrorCode errorCode) : this(message, errorCode,
            null)
        {
        }

        public ConnectionToolkitException(string message, ConnectionErrorCode errorCode, Exception e)
            : base(message, errorCode.ToString(), e)
        {
        }
    }
}
