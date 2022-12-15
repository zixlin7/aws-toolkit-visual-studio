using System;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Model
{
    public enum BeanstalkViewerExceptionCode
    {
        EnvironmentNotFound,
        TooManyEnvironments,
    }

    public class BeanstalkViewerException : Exception
    {
        public BeanstalkViewerExceptionCode ErrorCode { get; }

        public BeanstalkViewerException(BeanstalkViewerExceptionCode errorCode) : this(errorCode, null) { }

        public BeanstalkViewerException(BeanstalkViewerExceptionCode errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}
