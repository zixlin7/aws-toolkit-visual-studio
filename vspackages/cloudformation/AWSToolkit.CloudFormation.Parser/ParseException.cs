using System;

namespace Amazon.AWSToolkit.CloudFormation.Parser
{
    public class ParseException : Exception
    {
        internal ParseException(string message) : base(message) { }
    }
}
