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
        public const string Xray = "xray";
    }
}
