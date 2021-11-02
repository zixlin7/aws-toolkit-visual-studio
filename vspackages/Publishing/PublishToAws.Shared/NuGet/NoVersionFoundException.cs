using System;

namespace Amazon.AWSToolkit.Publish.NuGet
{
    public class NoVersionFoundException : Exception
    {
        public NoVersionFoundException(string message) : base(message) { }
        public NoVersionFoundException(string message, Exception e) : base(message, e) { }

        public static NoVersionFoundException For(string package, string versionRange)
        {
            return new NoVersionFoundException($"Package {package} does not have version in version range {versionRange}");
        }
    }
}
