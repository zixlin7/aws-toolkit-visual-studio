﻿using System.Collections.Generic;
using System.Linq;

namespace Amazon.AWSToolkit.Regions.Manifest
{
    public static class EndpointsExtensionMethods
    {
        /// <summary>
        /// Looks up the Id of the Partition a region is associated with.
        /// Assumption: region Ids are unique and can only belong to max one partition.
        /// </summary>
        /// <param name="regionId">Id of Region to look up</param>
        /// <returns>Id of partition that contains region, null if no partition found.</returns>
        public static string GetPartitionIdForRegion(this Endpoints endpoints, string regionId)
        {
            return endpoints.Partitions.FirstOrDefault(p => p.Regions.ContainsKey(regionId))?.Id;
        }

        /// <summary>
        /// Look up what regions belong to a partition
        /// </summary>
        /// <param name="partitionId">Partition to look up</param>
        /// <returns>A list of regions, empty list if the partition was not known</returns>
        public static IList<ToolkitRegion> GetRegions(this Endpoints endpoints, string partitionId)
        {
            var partition = endpoints.Partitions.FirstOrDefault(p => p.Id == partitionId);

            if (partition == null)
            {
                return new List<ToolkitRegion>();
            }

            return partition.Regions.Select(keyValue => new ToolkitRegion()
            {
                Id = keyValue.Key,
                DisplayName = keyValue.Value.Description,
                PartitionId = partition.Id,
            }).ToList();
        }

        /// <summary>
        /// Indicates whether or not a service is available in a region
        /// </summary>
        public static bool ContainsService(this Endpoints endpoints, ServiceName serviceName, string regionId)
        {
            var partitionId = endpoints.GetPartitionIdForRegion(regionId);
            if (partitionId == null) { return false; }

            var partition = endpoints.Partitions.FirstOrDefault(p => p.Id == partitionId);
            if (partition == null) { return false; }

            if (!partition.Services.TryGetValue(serviceName.Value, out var service))
            {
                return false;
            }

            return service.Endpoints.ContainsKey(regionId);
        }
    }
}
