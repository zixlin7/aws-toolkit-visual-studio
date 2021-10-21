namespace Amazon.AWSToolkit.Publish.Install
{
    public class InstallOptions
    {
        /// <summary>
        /// the dotnet toolpath that the aws.deploy.cli will be installed at
        /// https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools-how-to-use#use-the-tool-as-a-global-tool-installed-in-a-custom-location
        /// </summary>
        public string ToolPath { get; }

        /// <summary>
        /// the NuGet version range to install the aws.deploy.cli with
        /// https://docs.microsoft.com/en-us/nuget/concepts/package-versioning#version-ranges
        /// </summary>
        public string VersionRange { get; }

        public InstallOptions(string toolPath, string versionRange)
        {
            ToolPath = toolPath;
            VersionRange = versionRange;
        }

        public string GetCliInstallPath()
        {
            return $@"{ToolPath}\dotnet-aws.exe";
        }
    }
}
