using System;

namespace Amazon.AWSToolkit.Exceptions
{
    public class LambdaToolkitException : ToolkitException
    {
        public enum LambdaErrorCode
        {
            LambdaCreateFunction,
            LambdaUpdateFunctionConfig,
            LambdaUpdateFunctionCode,
            NoLambdaSourcePath
        }

        public LambdaToolkitException(string message, LambdaErrorCode errorCode) : this(message, errorCode, null) { }

        public LambdaToolkitException(string message, LambdaErrorCode errorCode, Exception e) : base(message,
            errorCode.ToString(), e)
        {
        }
    }
}
