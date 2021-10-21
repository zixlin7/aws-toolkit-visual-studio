using System.Runtime.Versioning;

namespace Amazon.AWSToolkit.Tests.Common.SampleData
{
    public static class SampleFrameworkNames
    {
        public static readonly FrameworkName DotNetFramework472 = new FrameworkName(".NETFramework,Version=v4.7.2");
        public static readonly FrameworkName DotNet5 = new FrameworkName(".NETCoreApp,Version=v5.0");
        public static readonly FrameworkName Garbage = new FrameworkName("Garbage,Version=v1.2.3");
    }
}
