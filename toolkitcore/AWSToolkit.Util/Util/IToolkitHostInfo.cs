namespace Amazon.AWSToolkit.Util
{
    /// <summary>
    /// Descriptive details about the Toolkit's host shell
    /// </summary>
    public interface IToolkitHostInfo
    {
        /// <summary>
        /// Name of Toolkit Host Shell
        /// Not intended as a user-friendly display name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Version of Host Shell (eg "2017")
        /// </summary>
        string Version { get; }
    }

    public class ToolkitHostInfo : IToolkitHostInfo
    {
        public string Name { get; }
        public string Version { get; }

        public ToolkitHostInfo(string name, string version)
        {
            Name = name;
            Version = version;
        }
    }

    /// <summary>
    /// Per-shell global constants for shells known at this time
    /// </summary>
    public static class ToolkitHosts
    {
        public static readonly IToolkitHostInfo AwsStudio = new ToolkitHostInfo("AWSToolkit.Studio", "2017");
        public static readonly IToolkitHostInfo Vs2008 = new ToolkitHostInfo("AWSToolkit.VS2008", "2008");
        public static readonly IToolkitHostInfo Vs2010 = new ToolkitHostInfo("AWSToolkitPackage.VS2010", "2010");
        public static readonly IToolkitHostInfo Vs2012 = new ToolkitHostInfo("AWSToolkitPackage.VS2012", "2012");
        public static readonly IToolkitHostInfo Vs2013 = new ToolkitHostInfo("AWSToolkitPackage.VS2013", "2013");
        public static readonly IToolkitHostInfo Vs2015 = new ToolkitHostInfo("AWSToolkitPackage.VS2015", "2015");
        public static readonly IToolkitHostInfo Vs2017 = new ToolkitHostInfo("AWSToolkitPackage.VS2017", "2017");
        public static readonly IToolkitHostInfo Vs2019 = new ToolkitHostInfo("AWSToolkitPackage.VS2019", "2019");

        public static readonly IToolkitHostInfo VsMinimumSupportedVersion = Vs2017;
    }
}
