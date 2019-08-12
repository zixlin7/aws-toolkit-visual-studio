using System;

namespace Amazon.AWSToolkit.S3
{
    public class UnrestoredObjectException : Exception
    {
        public UnrestoredObjectException(string message)
            : base(message)
        {
        }
    }
}
