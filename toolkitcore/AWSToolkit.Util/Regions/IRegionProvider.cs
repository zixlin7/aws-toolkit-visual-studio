using System;
using System.Collections.Generic;

namespace Amazon.AWSToolkit.Regions
{
    /// <summary>
    /// Responsible for Region/Partition/Endpoint functionality
    /// </summary>
    public interface IRegionProvider
    {
        /// <summary>
        /// Fires when the provider has an update to its region definitions.
        /// Allows for deferred loading, refreshes, etc.
        /// </summary>
        /// <example>
        /// The AWS Explorer would want to refresh its list of available regions when this fires.
        /// </example>
        event EventHandler RegionProviderUpdated;

        /// <summary>
        /// Look up what Partition a region belongs to
        /// </summary>
        /// <param name="regionId">Region to look up</param>
        /// <returns>Partition Id, null if no partition was found</returns>
        string GetPartitionId(string regionId);

        /// <summary>
        /// Look up what regions belong to a partition
        /// </summary>
        /// <param name="partitionId">Partition to look up</param>
        /// <returns>A list of regions, empty list if the partition was not known</returns>
        IList<ToolkitRegion> GetRegions(string partitionId);

        /// <summary>
        /// Indicates whether or not a service is available in a region
        /// </summary>
        /// TODO : Set up when we move away from the old serviceendpoints.xml data
        // bool ContainsService(ServiceName service, string regionId);
    }
}
