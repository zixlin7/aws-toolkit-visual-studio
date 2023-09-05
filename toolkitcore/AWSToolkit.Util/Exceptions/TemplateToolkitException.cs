using System;

namespace Amazon.AWSToolkit.Exceptions
{
    public class TemplateToolkitException : ToolkitException
    {
        public enum TemplateErrorCode
        {
            InvalidFormat
        }

        public TemplateToolkitException(string message, TemplateErrorCode errorCode) : this(message, errorCode, null)
        {
        }

        public TemplateToolkitException(string message, TemplateErrorCode errorCode, Exception e) : base(message,
            errorCode.ToString(), e)
        {
        }
    }
}
