using System;

namespace Amazon.AWSToolkit.Exceptions
{
    public class IamToolkitException : ToolkitException
    {
        public enum IamErrorCode
        {
            IamCreateRole,
            IamAttachRolePolicy,
        }

        public IamToolkitException(string message, IamErrorCode errorCode, Exception e) : base(message,
            errorCode.ToString(), e)
        {

        }
    }
}