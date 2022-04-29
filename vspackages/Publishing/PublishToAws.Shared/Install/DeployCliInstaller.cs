using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Publish.NuGet;
using Amazon.AWSToolkit.Publish.Util;

namespace Amazon.AWSToolkit.Publish.Install
{
    public class DeployCliInstaller
    {
        private readonly InstallOptions _options;
        private readonly NuGetRepository _nuGetRepository;
        private readonly InstallDeployCli _installDeployCli;
        private readonly GetCurrentCliVersion _getCurrentCliVersion;

        public DeployCliInstaller(InstallOptions options, NuGetRepository nuGetRepository)
        {
            _options = options;
            _nuGetRepository = nuGetRepository;

            _installDeployCli = new InstallDeployCli(options);
            _getCurrentCliVersion = new GetCurrentCliVersion(options);
        }

        public async Task<InstallResult> InstallAsync(CancellationToken cancellationToken)
        {
            var previouslyInstalled = IsInstalled();
            if (previouslyInstalled && await NoUpdateAvailableAsync(cancellationToken))
            {
                return InstallResult.Skipped;
            }

            await _installDeployCli.ExecuteAsync(cancellationToken);
            return previouslyInstalled ? InstallResult.Updated : InstallResult.Installed;
        }

        private bool IsInstalled()
        {
            return File.Exists(_options.GetCliInstallPath());
        }

        private async Task<bool> NoUpdateAvailableAsync(CancellationToken cancellationToken)
        {
            return !await UpdateIsAvailableAsync(cancellationToken);
        }

        private async Task<bool> UpdateIsAvailableAsync(CancellationToken cancellationToken)
        {
            var currentVersion = await _getCurrentCliVersion.ExecuteAsync(cancellationToken);
            var latestSupportedVersion = await _nuGetRepository.GetBestVersionInRangeAsync(PublishToAwsConstants.DeployToolPackageName, _options.VersionRange, cancellationToken);
            return currentVersion < latestSupportedVersion;
        }
    }
}
