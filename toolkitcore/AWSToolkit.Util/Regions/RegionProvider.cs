using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Regions.Manifest;
using Amazon.AWSToolkit.ResourceFetchers;
using Amazon.AWSToolkit.Tasks;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.Runtime;
using log4net;

namespace Amazon.AWSToolkit.Regions
{
    /// <summary>
    /// Responsible for Region/Partition/Endpoint functionality
    ///
    /// Retrieves endpoints data from local and remote sources, and maintains a
    /// mapping that can be queried by the Toolkit.
    ///
    /// This system also maintains Local regions. A local region allows the Toolkit
    /// to connect to locally hosted "service" endpoints like DynamoDB Local. Each partition
    /// retrieved from endpoints data will be automatically assigned a Local endpoint.
    /// This way, a "local" region can be accessed from any partition (usually via
    /// the AWS Explorer).
    /// Local endpoints for all partitions will share the same service url details.
    /// </summary>
    public class RegionProvider : IRegionProvider
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(RegionProvider));

        public const string EndpointsVersion = "3";
        public const string EndpointsFile = "endpoints.json";
        public const string EndpointsBaseUrl = "https://idetoolkits.amazonwebservices.com/";
        public const string LocalRegionIdPrefix = "toolkit-local-";

        /// <summary>
        /// Fires when the provider has an update to its region definitions.
        /// Allows this class to load local definitions quickly, then retrieve from online sources, which may be slower.
        /// </summary>
        /// <example>
        /// The AWS Explorer would want to refresh its list of available regions when this fires.
        /// </example>
        public event EventHandler RegionProviderUpdated;

        private Endpoints _endpoints = new Endpoints();
        private readonly IResourceFetcher _localEndpointsFetcher;
        private readonly IResourceFetcher _remoteEndpointsFetcher;

        /// <summary>
        /// Keyed by partition Id, contains the partition's "Local" region
        /// </summary>
        private Dictionary<string, ToolkitRegion> _localRegionsByPartitionId = new Dictionary<string, ToolkitRegion>();

        /// <summary>
        /// Keyed by service name, contains local service url values
        /// </summary>
        private readonly Dictionary<string, string> _localServiceUrls = new Dictionary<string, string>();

        public RegionProvider(ITelemetryLogger telemetryLogger)
            : this(CreateLocalEndpointsFetcher(), CreateRemoteEndpointsFetcher(telemetryLogger))
        {
        }


        public RegionProvider(IResourceFetcher localEndpointsFetcher, IResourceFetcher remoteEndpointsFetcher)
        {
            _localEndpointsFetcher = localEndpointsFetcher;
            _remoteEndpointsFetcher = remoteEndpointsFetcher;
        }

        /// <summary>
        /// Starts the process of loading manifest data.
        /// To keep startup times lean, data is loaded from local sources first,
        /// then loaded from online sources asynchronously. The event <see cref="RegionProviderUpdated"/>
        /// informs interested components when there are updates.
        /// </summary>
        public void Initialize()
        {
            LoadLocalResource();
            LoadRemoteResourceAsync().LogExceptionAndForget();
        }

        /// <summary>
        /// Look up what Partition a region belongs to
        /// </summary>
        /// <param name="regionId">Region to look up</param>
        /// <returns>Partition Id, null if no partition was found</returns>
        public string GetPartitionId(string regionId)
        {
            var partitionId = _endpoints.GetPartitionIdForRegion(regionId);
            return partitionId ?? _localRegionsByPartitionId.Values.FirstOrDefault(x => x.Id == regionId)?.PartitionId;
        }

        /// <summary>
        /// Look up what regions belong to a partition
        /// </summary>
        /// <param name="partitionId">Partition to look up</param>
        /// <returns>A list of regions, empty list if the partition was not known</returns>
        public IList<ToolkitRegion> GetRegions(string partitionId)
        {
            if (partitionId == null)
            {
                return new List<ToolkitRegion>();
            }

            var regions = _endpoints.GetRegions(partitionId);

            if (_localRegionsByPartitionId.TryGetValue(partitionId, out var localRegion))
            {
                regions.Add(localRegion);
            }

            return regions;
        }

        /// <summary>
        /// Retrieves list of available partitions
        /// </summary>
        public IList<Partition> GetPartitions()
        {
            return _endpoints.Partitions;
        }

        /// <summary>
        /// Retrieves a <see cref="Partition"/> for a given partition Id
        /// </summary>
        /// <param name="partitionId">Id of partition to look up</param>
        /// <returns>Corresponding <see cref="Partition"/> value, null if partition is not known</returns>
        public Partition GetPartition(string partitionId)
        {
            return _endpoints.GetPartition(partitionId);
        }

        /// <summary>
        /// Retrieve a <see cref="ToolkitRegion"/> for a given region Id.
        /// </summary>
        /// <param name="regionId">Id of region to look up</param>
        /// <returns>Corresponding <see cref="ToolkitRegion"/> value, null if region is not known</returns>
        public ToolkitRegion GetRegion(string regionId)
        {
            if (string.IsNullOrWhiteSpace(regionId))
            {
                return null;
            }
            var partitionId = GetPartitionId(regionId);
            if (partitionId == null)
            {
                return null;
            }

            return GetRegions(partitionId).FirstOrDefault(r => r.Id == regionId);
        }

        /// <summary>
        /// Indicates if the region represents local endpoints or not
        /// </summary>
        public bool IsRegionLocal(string regionId)
        {
            return regionId.StartsWith(LocalRegionIdPrefix);
        }

        /// <summary>
        /// Specifies that the given service uses the given url for local regions
        /// </summary>
        /// <param name="serviceName">The service name to store a local url for. See <see cref="ClientConfig.RegionEndpointServiceName"/></param>
        /// <param name="serviceUrl">Url to associate with the given service for local regions</param>
        public void SetLocalEndpoint(string serviceName, string serviceUrl)
        {
            _localServiceUrls[serviceName] = serviceUrl;
        }

        /// <summary>
        /// Queries the toolkit for a local service url for the given service
        /// </summary>
        /// <param name="serviceName">The service name to query a local url for. See <see cref="ClientConfig.RegionEndpointServiceName"/></param>
        public string GetLocalEndpoint(string serviceName)
        {
            if (_localServiceUrls.TryGetValue(serviceName, out var url))
            {
                return url;
            }

            return null;
        }

        /// <summary>
        /// Indicates whether or not a service is available in a region
        /// </summary>
        /// <param name="serviceName">Name of service to check for in a region. See <see cref="ClientConfig.RegionEndpointServiceName"/> or endpoints.json for expected values.</param>
        /// <param name="regionId">Region to check if service is available in</param>
        /// <returns>True if the service is available in the specified region, False otherwise</returns>
        public bool IsServiceAvailable(string serviceName, string regionId)
        {
            if (IsRegionLocal(regionId))
            {
                return _localServiceUrls.ContainsKey(serviceName) &&
                       !string.IsNullOrWhiteSpace(_localServiceUrls[serviceName]);
            }
            else
            {
                return _endpoints.IsServiceAvailable(serviceName, regionId);
            }
        }

        /// <summary>
        /// Creates a resource fetcher that gets the manifest from local sources.
        /// Intended to load quickly.
        /// </summary>
        private static IResourceFetcher CreateLocalEndpointsFetcher()
        {
            var hostedFilesSettings = new HostedFilesSettings();

            var downloadCacheFetcher = new RelativeFileResourceFetcher(hostedFilesSettings.DownloadedCacheFolder);
            var conditionalDownloadCacheFetcher = new ConditionalResourceFetcher(downloadCacheFetcher, IsValidStream);

            return new ChainedResourceFetcher()
                .Add(conditionalDownloadCacheFetcher)
                .Add(new AssemblyResourceFetcher());
        }

        /// <summary>
        /// Creates a resource fetcher that loads from the usual hosted files convention.
        /// Typically accesses online urls.
        /// </summary>
        private static IResourceFetcher CreateRemoteEndpointsFetcher(ITelemetryLogger telemetryLogger)
        {
            var options = new HostedFilesResourceFetcher.Options()
            {
                LoadFromDownloadCache = true,
                DownloadIfNewer = true,
                CloudFrontBaseUrl = EndpointsBaseUrl,
                ResourceValidator = IsValidStream,
                TelemetryLogger = telemetryLogger
            };

            return new HostedFilesResourceFetcher(options);
        }

        /// <summary>
        /// Responsible for loading endpoints data from local sources
        /// </summary>
        private void LoadLocalResource()
        {
            FetchEndpoints(_localEndpointsFetcher);
        }

        /// <summary>
        /// Responsible for loading endpoints data from remote sources
        /// </summary>
        private async Task LoadRemoteResourceAsync()
        {
            await Task.Run(() =>
            {
                FetchEndpoints(_remoteEndpointsFetcher);
            });
        }

        /// <summary>
        /// Retrieves endpoints manifest and applies it to the provider.
        /// </summary>
        private void FetchEndpoints(IResourceFetcher endpointsFetcher)
        {
            try
            {
                using (var stream = endpointsFetcher.Get(EndpointsFile))
                using (var streamCopy = new MemoryStream())
                {
                    stream.CopyTo(streamCopy);
                    streamCopy.Position = 0;

                    Endpoints endpoints = null;

                    // Endpoints.Load destroys the stream, give it a copy
                    using (var endpointsStream = new MemoryStream(streamCopy.GetBuffer()))
                    {
                        endpoints = Endpoints.Load(endpointsStream);
                        if (endpoints == null)
                        {
                            throw new Exception("No endpoints data received.");
                        }
                    }

                    UpdateSdk(streamCopy);
                    Update(endpoints);
                }
            }
            catch (Exception e)
            {
                Logger.Error(
                    "Error fetching endpoints data. The Toolkit may have trouble accessing services in some regions.",
                    e);
                Debug.Assert(false, "No endpoint data retrieved - this should never happen, as all fetchers have a stable, local fallback");
            }
        }

        /// <summary>
        /// Indicates whether or not the stream contains a valid endpoints file
        /// </summary>
        private static bool IsValidStream(Stream stream)
        {
            try
            {
                var endpoints = Endpoints.Load(stream);
                return endpoints != null && endpoints.Version == EndpointsVersion;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Updates the AWS SDK with the provided endpoints.json data
        /// </summary>
        /// <param name="stream"></param>
        private void UpdateSdk(Stream stream)
        {
            try
            {
                Logger.Debug("Updating AWS SDK with endpoints data...");
                RegionEndpoint.Reload(stream);
                Logger.Debug("Finished updating AWS SDK with endpoints data");
            }
            catch (Exception e)
            {
                Logger.Error("Failed to update the AWS SDK region endpoints. Toolkit may have difficulties accessing services.", e);
            }
        }

        /// <summary>
        /// Registers the given endpoints data as current
        /// </summary>
        private void Update(Endpoints endpoints)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            _localRegionsByPartitionId = endpoints.Partitions
                // Craft a "Local" region for each partition
                .Select(p => new {PartitionId = p.Id, LocalRegion = new ToolkitRegion()
                {
                    // Eg: aws-cn -> toolkit-local-aws-cn
                    Id = $"{LocalRegionIdPrefix}{p.Id}",
                    DisplayName = "Local (localhost)",
                    PartitionId = p.Id,
                },})
                .ToDictionary(x => x.PartitionId, x => x.LocalRegion);

            _endpoints = endpoints;

            RaiseRegionProviderUpdated();
        }

        private void RaiseRegionProviderUpdated()
        {
            RegionProviderUpdated?.Invoke(this, EventArgs.Empty);
        }
    }
}
