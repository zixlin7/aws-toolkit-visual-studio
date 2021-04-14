using System;
using System.Collections.Generic;
using Amazon.AWSToolkit.Regions.Manifest;
using Amazon.Runtime;

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
        /// Retrieve a <see cref="ToolkitRegion"/> for a given region Id.
        /// </summary>
        /// <param name="regionId">Id of region to look up</param>
        /// <returns>Corresponding <see cref="ToolkitRegion"/> value, null if region is not known</returns>
        ToolkitRegion GetRegion(string regionId);

        /// <summary>
        /// Retrieves the list of available partitions
        /// </summary>
        IList<Partition> GetPartitions();

        /// <summary>
        /// Retrieves a <see cref="Partition"/> for a given partition Id
        /// </summary>
        /// <param name="partitionId">Id of partition to look up</param>
        /// <returns>Corresponding <see cref="Partition"/> value, null if partition is not known</returns>
        Partition GetPartition(string partitionId);
     
        /// <summary>
        /// Indicates if the region represents local endpoints or not
        /// </summary>
        bool IsRegionLocal(string regionId);

        /// <summary>
        /// Specifies that the given service uses the given url for local regions
        /// </summary>
        /// <param name="serviceName">The service name to store a local url for. See <see cref="ClientConfig.RegionEndpointServiceName"/></param>
        /// <param name="serviceUrl">Url to associate with the given service for local regions</param>
        void SetLocalEndpoint(string serviceName, string serviceUrl);

        /// <summary>
        /// Queries the toolkit for a local service url for the given service
        /// </summary>
        /// <param name="serviceName">The service name to query a local url for. See <see cref="ClientConfig.RegionEndpointServiceName"/></param>
        string GetLocalEndpoint(string serviceName);

        /// <summary>
        /// Indicates whether or not a service is available in a region
        /// </summary>
        /// <param name="serviceName">Name of service to check for in a region. See <see cref="ClientConfig.RegionEndpointServiceName"/> or endpoints.json for expected values.</param>
        /// <param name="regionId">Region to check if service is available in</param>
        /// <returns>True if the service is available in the specified region, False otherwise</returns>
        bool IsServiceAvailable(string serviceName, string regionId);
    }
}
