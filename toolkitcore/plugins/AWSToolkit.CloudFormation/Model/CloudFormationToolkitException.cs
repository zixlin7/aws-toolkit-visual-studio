using System;

using Amazon.AWSToolkit.Exceptions;

namespace Amazon.AWSToolkit.CloudFormation.Model
{
    public class CloudFormationToolkitException : ToolkitException
    {
        public enum CloudFormationErrorCode
        {
            ChangeSetFailed
        }

        public CloudFormationToolkitException(string message, CloudFormationErrorCode errorCode) : this(message, errorCode, null) { }

        public CloudFormationToolkitException(string message, CloudFormationErrorCode errorCode, Exception e)
            : base(message, errorCode.ToString(), e)
        {
        }
    }
}
