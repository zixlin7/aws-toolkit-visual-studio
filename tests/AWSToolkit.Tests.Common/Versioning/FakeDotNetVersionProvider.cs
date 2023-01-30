using Amazon.AWSToolkit.VersionInfo;

namespace Amazon.AWSToolkit.Tests.Common.Versioning
{
    public class FakeDotNetVersionProvider : IDotNetVersionProvider
    {
        public int? MajorVersion;

        public int? GetMajorVersion() => MajorVersion;
    }
}
