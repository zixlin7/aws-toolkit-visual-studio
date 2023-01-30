namespace Amazon.AWSToolkit.VersionInfo
{
    public interface IDotNetVersionProvider
    {
        /// <summary>
        /// Returns the major version number of .NET (Core/5+), or null the version cannot be determined.
        /// Example "7.0.100" -> 7 (.NET 7)
        /// </summary>
        int? GetMajorVersion();
    }

    public class DotNetVersionProvider : IDotNetVersionProvider
    {
        public int? GetMajorVersion() => DotNetVersion.GetMajorVersion();
    }
}
