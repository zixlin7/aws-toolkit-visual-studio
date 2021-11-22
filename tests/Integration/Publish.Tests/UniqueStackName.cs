using System;

namespace Amazon.AWSToolkit.Tests.Integration.Publishing
{
    public class UniqueStackName
    {
        public static string CreateWith(string prefix)
        {
            return $"{prefix}-{CreateGuid()}";
        }

        private static string CreateGuid()
        {
            var guid = Guid.NewGuid().ToString();
            return guid.Substring(0, 4);
        }
    }
}
