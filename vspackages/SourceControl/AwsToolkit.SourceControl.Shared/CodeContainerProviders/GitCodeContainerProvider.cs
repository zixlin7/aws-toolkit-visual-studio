﻿using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Shared;

using log4net;

using Microsoft;

// There are a lot of duplicated type names between these two namespaces, so use aliases for clarity.
// If VS injects an unaliased using statement for any of these namespaces, remove it immediately.
using ccm = Microsoft.VisualStudio.Shell.CodeContainerManagement;
using sh = Microsoft.VisualStudio.Shell;

namespace Amazon.AwsToolkit.SourceControl.CodeContainerProviders
{
    public abstract class GitCodeContainerProvider : ccm.ICodeContainerProvider
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(GitCodeContainerProvider));

        protected const string SourceControlGitOutputWindowPaneName = "Source Control - Git";

        protected readonly Guid _codeContainerProviderId;
        protected readonly Guid _sccProviderId;

        protected readonly ToolkitContext _toolkitContext;
        protected readonly sh.IAsyncServiceProvider _asyncServiceProvider;

        protected GitCodeContainerProvider(ToolkitContext toolkitContext, sh.IAsyncServiceProvider asyncServiceProvider, Guid sccProviderId)
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
            CloneRepositoryData initialCloneRepoData = null;
            bool suppressExceptions = false;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var cloneRepoData = await GetCloneRepositoryDataAsync(onlineCodeContainer, cancellationToken);
                if (cloneRepoData == null)
                {
                    await CloneCanceledAsync();
                    return null; // VS expects null if the clone operation is incomplete
                }

                // Keep a copy of the original clone repo data to report failure messages
                initialCloneRepoData = cloneRepoData;

                cancellationToken.ThrowIfCancellationRequested();

                await UpdateStoredGitCredentialsAsync(cloneRepoData.RemoteUri, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                cloneRepoData = await CloneAsync(cloneRepoData, cancellationToken);
                if (cloneRepoData == null)
                {
                    await CloneFailedAsync(initialCloneRepoData);
                    return null;
                }

                // After this point, the clone has succeeded. Everything after this point should not fail (or should not report
                // a failure). Suppress exceptions, which will raise a debug assert, alerting Toolkit developers that there
                // are problems to address.
                suppressExceptions = true;

                // Too late to cancel now, while this is currently not used, leave here for future coders to be aware of the cut line
                cancellationToken = CancellationToken.None;

                // Make sure progress appears to be 100% complete
                downloadProgress.Report(new sh.ServiceProgressData(string.Empty, string.Empty, 1, 1));
                await CloneCompletedAsync(cloneRepoData);

                return CreateCodeContainer(cloneRepoData);
            }
            catch (OperationCanceledException)
            {
                await CloneCanceledAsync();
                throw;
            }
            catch (Exception ex)
            {
                var repositoryName = initialCloneRepoData?.RepositoryName ?? "(unknown)";
                _logger.Error($"Failed to clone repository: {repositoryName}", ex);

                if (!suppressExceptions)
                {
                    await CloneFailedAsync(initialCloneRepoData);
                    _toolkitContext.ToolkitHost.ShowError("Unable to clone repo", $"Error cloning repo: {ex.Message}");
                }

                Debug.Assert(!suppressExceptions,
                    "Clone handling has unexpectedly failed after the point when cloning is complete. The Toolkit implementation is wrong",
                    ex.Message);

                throw;
            }
        }

        protected virtual Task CloneCanceledAsync()
        {
            return Task.CompletedTask;
        }

        protected virtual Task CloneCompletedAsync(CloneRepositoryData cloneRepoData)
        {
            return Task.CompletedTask;
        }

        protected virtual Task CloneFailedAsync(CloneRepositoryData cloneRepoData)
        {
            return Task.CompletedTask;
        }

        private async Task<CloneRepositoryData> CloneAsync(CloneRepositoryData cloneRepoData, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _toolkitContext.ToolkitHost.OpenShellWindowAsync(ShellWindows.Output);

            var git = new GitService(_toolkitContext);
            while (true)
            {
                var returned = await TryCloneAsync(cloneRepoData, git, cancellationToken);
                cloneRepoData = returned.CloneRepoData;
                if (!returned.Retry)
                {
                    return cloneRepoData;
                }
            }
        }

        private sealed class TryCloneAsyncReturnValue
        {
            public readonly bool Retry;
            public readonly CloneRepositoryData CloneRepoData;

            public TryCloneAsyncReturnValue(bool retry, CloneRepositoryData cloneRepoData)
            {
                Retry = retry;
                CloneRepoData = cloneRepoData;
            }
        }

        private async Task<TryCloneAsyncReturnValue> TryCloneAsync(
            CloneRepositoryData cloneRepoData, GitService git, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await git.CloneAsync(
                    cloneRepoData.RemoteUri,
                    cloneRepoData.LocalPath,
                    false,
                    cancellationToken);

                return new TryCloneAsyncReturnValue(false, cloneRepoData);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);

                cloneRepoData = await HandleTryCloneFailedAsync(cloneRepoData, ex);
                return new TryCloneAsyncReturnValue(cloneRepoData != null, cloneRepoData);
            }
        }

        private async Task<CloneRepositoryData> HandleTryCloneFailedAsync(CloneRepositoryData cloneRepoData, Exception cloneException)
        {
            try
            {
                return await HandleCloneRepositoryFailedAsync(cloneRepoData, cloneException);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                _toolkitContext.ToolkitHost.ShowError("Unexpected error when handling clone repository failure.  Check toolkit logs for details.");

                return null;
            }
        }

        protected abstract Task UpdateStoredGitCredentialsAsync(Uri uri, CancellationToken cancellationToken);

        protected abstract Task<CloneRepositoryData> GetCloneRepositoryDataAsync(ccm.RemoteCodeContainer onlineCodeContainer, CancellationToken cancellationToken);

        /// <summary>
        /// Override this method to perform additional error handling and clone retry.
        /// </summary>
        /// <param name="failedCloneRepoData">The repo data of the failed clone operation.</param>
        /// <param name="cloneException">The exception of the failed clone operation.</param>
        /// <returns>New repo data to retry the clone operation or null to not retry.</returns>
        /// <remarks>
        /// The base implementation ensures the Output tool window is open to show git clone output and displays a basic error message dialog.
        /// It does not attempt to retry the clone operation.  To retry the clone operation, create a new CloneRepositoryData object with
        /// corrected values and return it from this method.  If the subsequent retry fails, this method will be called again.  Retry behavior
        /// can be repeated over and over until the clone is successful or this method returns null.
        /// </remarks>
        protected virtual async Task<CloneRepositoryData> HandleCloneRepositoryFailedAsync(CloneRepositoryData failedCloneRepoData, Exception cloneException)
        {
            var nl = Environment.NewLine;

            await OpenSourceControlGitOutputWindowPaneAsync();

            _toolkitContext.ToolkitHost.ShowError("Clone repository failed",
                $"Failed to clone {failedCloneRepoData.RepositoryName}.{nl}{nl}{cloneException.Message}{nl}{nl}See Output window for details.");

            return null;
        }

        protected async Task<bool> OpenSourceControlGitOutputWindowPaneAsync()
        {
            return await _toolkitContext.ToolkitHost.OpenOutputWindowPaneAsync(SourceControlGitOutputWindowPaneName);
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

            return new ccm.CodeContainer(
                localProperties,
                null,
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
