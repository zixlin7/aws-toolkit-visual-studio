using System;
using System.Runtime.Versioning;

namespace Amazon.AWSToolkit.Util
{
    /// <summary>
    /// Utility methods around the <see cref="FrameworkName"/> class.
    /// </summary>
    public static class FrameworkNameHelper
    {
        public const string DotNetFrameworkIdentifier = ".NETFramework";
        // "DotNetCore" Includes .NET 5+
        public const string DotNetCoreIdentifier = ".NETCoreApp";
        public static readonly FrameworkName UnknownFramework = new FrameworkName("Unknown", new Version(0, 0, 0, 0));

        public static bool IsDotNetFramework(FrameworkName framework)
        {
            return framework?.Identifier == DotNetFrameworkIdentifier;
        }

        public static bool IsDotNetCore(FrameworkName framework)
        {
            return framework?.Identifier == DotNetCoreIdentifier;
        }
    }
}
