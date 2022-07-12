using System;

namespace Amazon.AWSToolkit.Exceptions
{
    public class UserCanceledException : Exception
    {
        public UserCanceledException(string message) : base(message) { }
        public UserCanceledException(string message, Exception e) : base(message, e) { }
    }
}
