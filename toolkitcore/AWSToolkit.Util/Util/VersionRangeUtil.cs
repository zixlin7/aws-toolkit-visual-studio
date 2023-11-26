using System;

namespace Amazon.AWSToolkit.Util
{
    public class VersionRangeUtil
    {
        public static VersionRange Create(string start, string end)
        {
            var startVersion = Version.Parse(start);
            var endVersion = Version.Parse(end);
            return new VersionRange(startVersion, endVersion);
        }
    }
}
