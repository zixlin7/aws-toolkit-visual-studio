namespace Amazon.AWSToolkit.Util.Tests
{
    /// <summary>
    /// RegionEndPointsManager is set up to be a singleton, which isn't too Depencency Injection friendly.
    /// This class lets unit tests instantiate their own custom RegionEndPointsManager object without impacting other tests.
    /// NOTE: Only useful for testing code that does not reference RegionEndPointsManager.Instance
    /// </summary>
    internal class RegionEndPointsManagerTestExtension : RegionEndPointsManager
    {
        internal static IRegionEndPointsManager CreateRegionEndPointsManager(S3FileFetcher fileFetcher)
        {
            var regionEndPointsManager = new RegionEndPointsManagerTestExtension(fileFetcher);
            regionEndPointsManager.LoadEndPoints();
            return regionEndPointsManager;
        }

        private RegionEndPointsManagerTestExtension(S3FileFetcher fileFetcher)
        {
            FileFetcher = fileFetcher;
            LocalRegion = CreateLocalRegionEndPoints(fileFetcher);
        }
    }
}
