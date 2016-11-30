using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.CloudFormation.Parser
{
    public class ParseException : Exception
    {
        internal ParseException(string message) : base(message) { }
    }
}
