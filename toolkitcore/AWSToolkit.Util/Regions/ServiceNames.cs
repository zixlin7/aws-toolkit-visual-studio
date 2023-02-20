using Amazon.Runtime;

namespace Amazon.AWSToolkit.Regions
{
    /// <summary>
    /// Service names for use with <see cref="IRegionProvider.IsServiceAvailable"/>.
    /// 
    /// Service names should be retrieved from <see cref="ClientConfig.RegionEndpointServiceName"/>
    /// where possible. This class is intended for services that the Toolkit doesn't have
    /// a package reference to.
    /// </summary>
    public static class ServiceNames
    {
        // ***** HEAR ME NOW AND BELIEVE ME LATER!!! *****
        // When you add anything here, you MUST add a test for it in AWSToolkit.Util.Tests/Regions/ServiceNameTests.cs
        // Don't even think about submitting a PR without it.
        public const string Beanstalk = "elasticbeanstalk";
        public const string CloudWatchLogs = "logs";
        public const string CodeCatalyst = "codecatalyst";
        public const string CodeCommit = "codecommit";
        public const string Ecs = "ecs";
        public const string Lambda = "lambda";
        public const string Xray = "xray";
    }
}
