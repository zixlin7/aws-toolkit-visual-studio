using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

using NugetVersionRange = NuGet.Versioning.VersionRange;

namespace Amazon.AWSToolkit.Publish.NuGet
{
    /// <summary>
    /// Repository of NuGetPackages.
    /// </summary>
    public class NuGetRepository
    {
        private readonly SourceCacheContext cache = new SourceCacheContext();
        private readonly SourceRepository repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");

        /// <summary>
        /// Gets the Best Version for the given package based on the versionRange passed in.
        /// https://docs.microsoft.com/en-us/nuget/concepts/package-versioning#version-ranges
        /// </summary>
        /// <param name="package">id of package</param>
        /// <param name="versionRange">version range used to determine best version</param> 
        /// <param name="cancellationToken">optional token used to signal cancelling operation</param>
        /// <returns>best version defined by the version range rules</returns>
        /// <exception cref="NoVersionFoundException">Thrown if no version is found in range</exception>
        public async Task<NuGetVersion> GetBestVersionInRangeAsync(string package, string versionRange, CancellationToken cancellationToken = default(CancellationToken))
        {
            var range = NugetVersionRange.Parse(versionRange);

            var versions = await GetAllVersionsAsync(package, cancellationToken);
            var bestMatchedVersion = range.FindBestMatch(versions);

            ThrowIfNoVersionFound(package, versionRange, bestMatchedVersion);

            return bestMatchedVersion;
        }

        private async Task<IEnumerable<NuGetVersion>> GetAllVersionsAsync(string package, CancellationToken cancellationToken)
        {
            FindPackageByIdResource resource = await repository.GetResourceAsync<FindPackageByIdResource>(cancellationToken);
            return await resource.GetAllVersionsAsync(package, cache, NullLogger.Instance, cancellationToken);
        }

        private void ThrowIfNoVersionFound(string package, string versionRange, NuGetVersion version)
        {
            if (version is null)
            {
                throw NoVersionFoundException.For(package, versionRange);
            }
        }
    }
}
