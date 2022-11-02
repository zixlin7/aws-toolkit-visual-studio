using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Context;

using log4net;

using Microsoft;

// There are a lot of duplicated type names between these two namespaces, so use aliases for clarity
using ccm = Microsoft.VisualStudio.Shell.CodeContainerManagement;
using sh = Microsoft.VisualStudio.Shell;

namespace Amazon.AwsToolkit.SourceControl.CodeContainerProviders
{
    public abstract class BaseGitCodeContainerProvider : ccm.ICodeContainerProvider
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(BaseGitCodeContainerProvider));

        protected readonly Guid _codeContainerProviderId;
        protected readonly Guid _sccProviderId;

        protected readonly ToolkitContext _toolkitContext;
        protected readonly sh.IAsyncServiceProvider _asyncServiceProvider;

        protected BaseGitCodeContainerProvider(ToolkitContext toolkitContext, sh.IAsyncServiceProvider asyncServiceProvider, Guid sccProviderId)
        {
            Requires.NotNull(toolkitContext, nameof(toolkitContext));
            Requires.NotNull(asyncServiceProvider, nameof(asyncServiceProvider));
            Requires.NotEmpty(sccProviderId, nameof(sccProviderId));

            try
            {
                _codeContainerProviderId = new Guid(GetType().GetCustomAttribute<GuidAttribute>().Value);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Class {GetType().Name} must be declared with {nameof(GuidAttribute)}.", ex);
            }

            _toolkitContext = toolkitContext;
            _asyncServiceProvider = asyncServiceProvider;
            _sccProviderId = sccProviderId;
        }

        public async Task<ccm.CodeContainer> AcquireCodeContainerAsync(IProgress<sh.ServiceProgressData> downloadProgress, CancellationToken cancellationToken)
        {
            return await AcquireCodeContainerAsync(null, downloadProgress, cancellationToken);
        }

        public async Task<ccm.CodeContainer> AcquireCodeContainerAsync(ccm.RemoteCodeContainer onlineCodeContainer, IProgress<sh.ServiceProgressData> downloadProgress, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var cloneRepoData = await GetCloneRepositoryDataAsync(onlineCodeContainer, cancellationToken);
            if (cloneRepoData == null)
            {
                return null; // VS expects null if the clone operation is incomplete
            }

            cancellationToken.ThrowIfCancellationRequested();

            var git = new Git(_toolkitContext);
            if (!await git.CloneAsync(cloneRepoData.RemoteUri, cloneRepoData.LocalPath, false, downloadProgress, cancellationToken))
            {
                await HandleCloneRepositoryFailedAsync(cloneRepoData);
                return null;
            }

            // Too late to cancel now, if you add code below this line in the future that requires a CancellationToken, uncomment the line below.
            // cancellationToken = CancellationToken.None;

            // Make sure progress appears to be 100% complete
            downloadProgress.Report(new sh.ServiceProgressData(string.Empty, string.Empty, 1, 1));

            return CreateCodeContainer(cloneRepoData);
        }

        protected abstract Task<CloneRepositoryData> GetCloneRepositoryDataAsync(ccm.RemoteCodeContainer onlineCodeContainer, CancellationToken cancellationToken);

        protected virtual Task HandleCloneRepositoryFailedAsync(CloneRepositoryData cloneRepoData)
        {
            _toolkitContext.ToolkitHost.ShowError("Clone repository failed", $"Unable to clone {cloneRepoData.RepositoryName}.");
            return Task.CompletedTask;
        }

        private ccm.CodeContainer CreateCodeContainer(CloneRepositoryData cloneRepoData)
        {
            var lastAccessed = DateTimeOffset.UtcNow;

            var localProperties = new ccm.CodeContainerLocalProperties(
                cloneRepoData.LocalPath,
                ccm.CodeContainerType.Folder,
                new ccm.CodeContainerSourceControlProperties(
                    cloneRepoData.RepositoryName,
                    cloneRepoData.LocalPath,
                    _sccProviderId));

            var remoteCodeContainer = new ccm.RemoteCodeContainer(
                cloneRepoData.RepositoryName,
                _codeContainerProviderId,
                cloneRepoData.RemoteUri,
                cloneRepoData.BrowseOnlineUri,
                lastAccessed);

            return new ccm.CodeContainer(
                localProperties,
                remoteCodeContainer,
                cloneRepoData.IsFavorite,
                lastAccessed);
        }

        protected sealed class CloneRepositoryData
        {
            public readonly string RepositoryName;
            public readonly Uri RemoteUri;
            public readonly Uri BrowseOnlineUri;
            public readonly string LocalPath;
            public readonly bool IsFavorite;

            public CloneRepositoryData(string repositoryName, Uri remoteUri, string localPath, Uri browseOnlineUri = null, bool isFavorite = false)
            {
                Requires.NotNullOrWhiteSpace(repositoryName, nameof(repositoryName));
                Requires.NotNull(remoteUri, nameof(remoteUri));
                Requires.NotNullOrWhiteSpace(localPath, nameof(localPath));

                RepositoryName = repositoryName;
                RemoteUri = remoteUri;
                LocalPath = localPath;
                BrowseOnlineUri = browseOnlineUri;
                IsFavorite = isFavorite;
            }
        }
    }
}
