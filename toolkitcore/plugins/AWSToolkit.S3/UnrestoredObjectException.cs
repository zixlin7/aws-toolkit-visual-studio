using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
