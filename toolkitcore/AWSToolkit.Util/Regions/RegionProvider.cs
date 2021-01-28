using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Regions.Manifest;
using Amazon.AWSToolkit.ResourceFetchers;
using Amazon.AWSToolkit.Tasks;
using log4net;

namespace Amazon.AWSToolkit.Regions
{
    public class RegionProvider : IRegionProvider
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(RegionProvider));

        public const string EndpointsVersion = "3";
        public const string EndpointsFile = "endpoints.json";
        public const string EndpointsBaseUrl = "https://idetoolkits.amazonwebservices.com/";

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

        public RegionProvider() : this(CreateLocalEndpointsFetcher(), CreateRemoteEndpointsFetcher())
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
            // TODO : account for Local Region somehow (eg: DynamoDB Local)
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
            return _endpoints.GetPartitionIdForRegion(regionId);
        }

        /// <summary>
        /// Look up what regions belong to a partition
        /// </summary>
        /// <param name="partitionId">Partition to look up</param>
        /// <returns>A list of regions, empty list if the partition was not known</returns>
        public IList<ToolkitRegion> GetRegions(string partitionId)
        {
            return _endpoints.GetRegions(partitionId);
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
        private static IResourceFetcher CreateRemoteEndpointsFetcher()
        {
            var options = new HostedFilesResourceFetcher.Options()
            {
                LoadFromDownloadCache = true,
                DownloadIfNewer = true,
                CloudFrontBaseUrl = EndpointsBaseUrl,
                ResourceValidator = IsValidStream,
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
                {
                    var endpoints = Endpoints.Load(stream);
                    if (endpoints == null)
                    {
                        throw new Exception("No endpoints data received.");
                    }

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
        /// Registers the given endpoints data as current
        /// </summary>
        private void Update(Endpoints endpoints)
        {
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));

            // TODO : Pass into .NET SDK
            RaiseRegionProviderUpdated();
        }

        private void RaiseRegionProviderUpdated()
        {
            RegionProviderUpdated?.Invoke(this, EventArgs.Empty);
        }
    }
}
