using System;

namespace Amazon.AWSToolkit.Exceptions
{
    public class NodeNotFoundException : Exception
    {
        public NodeNotFoundException(string message) : base(message) { }
        public NodeNotFoundException(string message, Exception e) : base(message, e) { }
    }
}
