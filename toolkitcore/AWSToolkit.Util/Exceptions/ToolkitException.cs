using Amazon.Runtime;
using System;

namespace Amazon.AWSToolkit.Exceptions
{
    public class ToolkitException : Exception
    {
        public enum CommonErrorCode
        {
            UnexpectedError,
            InternalMissingServiceState
        }

        public string Code { get; }

        public string ServiceErrorCode { get; }

        public string ServiceStatusCode { get; }

        public ToolkitException(string message, CommonErrorCode code)
            : this(message, code, null)
        {

        }

        public ToolkitException(string message, CommonErrorCode code, Exception e)
            : this(message, code.ToString(), e)
        {

        }

        protected ToolkitException(string message, string errorCode, Exception e)
            : base(message)
        {
            this.Code = errorCode;

            var serviceException = e as AmazonServiceException;
            if (serviceException != null)
            {
                this.ServiceErrorCode = serviceException.ErrorCode;
                this.ServiceStatusCode = serviceException.StatusCode.ToString();
            }
        }
    }
}
