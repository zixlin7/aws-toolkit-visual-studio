using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CodeCatalyst;
using Amazon.AWSToolkit.CommonUI.Dialogs;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.SourceControl;
using Amazon.AwsToolkit.Telemetry.Events.Generated;

// There are a lot of duplicated type names between these two namespaces, so use aliases for clarity.
// If VS injects an unaliased using statement for any of these namespaces, remove it immediately.
using ccm = Microsoft.VisualStudio.Shell.CodeContainerManagement;
using sh = Microsoft.VisualStudio.Shell;
using Amazon.AwsToolkit.Telemetry.Events.Core;

namespace Amazon.AwsToolkit.SourceControl.CodeContainerProviders
{
    [Guid(CodeCatalystCodeContainerProviderId)]
    public class CodeCatalystCodeContainerProvider : GitCodeContainerProvider
    {
        public const string CodeCatalystCodeContainerProviderId = "0bc6e8f8-a3fd-4a83-89fe-07215f5facae";

        public const string CodeCatalystSccProviderId = "25c452c5-d427-4275-b735-b5092aad4b35";

        private static readonly Guid CodeCatalystSccProviderIdGuid = new Guid(CodeCatalystSccProviderId);

        private readonly ICloneCodeCatalystRepositoryDialog _dialog;

        public CodeCatalystCodeContainerProvider(ToolkitContext toolkitContext, sh.IAsyncServiceProvider asyncServiceProvider)
            : base(toolkitContext, asyncServiceProvider, CodeCatalystSccProviderIdGuid)
        {
            _dialog = _toolkitContext.ToolkitHost.GetDialogFactory().CreateCloneCodeCatalystRepositoryDialog();
        }

        protected override async Task StoreGitCredentialsAsync(CloneRepositoryData cloneRepoData, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Get PAT
            var codeCatalyst = _toolkitContext.ToolkitHost.QueryAWSToolkitPluginService(typeof(IAWSCodeCatalyst)) as IAWSCodeCatalyst;
            var pat = (await codeCatalyst.GetAccessTokensAsync(_dialog.ConnectionSettings, cancellationToken)).FirstOrDefault() ??
                      (await codeCatalyst.CreateDefaultAccessTokenAsync(null, _dialog.ConnectionSettings, cancellationToken));

            // username is in url, eg: https://foo@bar.org/repo/path, get "foo"
            var username = _dialog.CloneUrl.UserInfo;

            // Store PAT (in Windows credential store)
            var uri = cloneRepoData.RemoteUri;
            var gitCredentialKey = $"git:{uri.Scheme}://{uri.DnsSafeHost}";
            var gitCredentials = new GitCredentials(username, pat.Secret, gitCredentialKey);
            gitCredentials.Save();
        }

        protected override Task<CloneRepositoryData> GetCloneRepositoryDataAsync(ccm.RemoteCodeContainer onlineCodeContainer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(
                _dialog.Show() ?
                    new CloneRepositoryData(_dialog.RepositoryName, _dialog.CloneUrl, _dialog.LocalPath) :
                    null);
        }

        protected async override Task<CloneRepositoryData> HandleCloneRepositoryFailedAsync(CloneRepositoryData failedCloneRepoData, Exception cloneException)
        {
            if (cloneException?.Message?.ToLower().Contains("authentication failed") != true)
            {
                return await base.HandleCloneRepositoryFailedAsync(failedCloneRepoData, cloneException);
            }

            if (_toolkitContext.ToolkitHost.Confirm("Create access token",
                "Cloning may have failed due to an invalid access token.  Would you like the toolkit to recreate your access token and try again?"))
            {
                var codeCatalyst = _toolkitContext.ToolkitHost.QueryAWSToolkitPluginService(typeof(IAWSCodeCatalyst)) as IAWSCodeCatalyst;
                var pat = await codeCatalyst.CreateDefaultAccessTokenAsync(null, _dialog.ConnectionSettings);
                var remoteUri = new UriBuilder(failedCloneRepoData.RemoteUri) { Password = pat.Secret }.Uri;

                return new CloneRepositoryData(
                    failedCloneRepoData.RepositoryName,
                    remoteUri,
                    failedCloneRepoData.LocalPath,
                    failedCloneRepoData.BrowseOnlineUri,
                    failedCloneRepoData.IsFavorite);
            }

            return null;
        }

        protected override Task CloneCanceledAsync()
        {
            RecordClone(Result.Cancelled);
            return Task.CompletedTask;
        }

        protected override Task CloneCompletedAsync(CloneRepositoryData cloneRepoData)
        {
            _toolkitContext.ToolkitHost.OutputToHostConsole($"Successfully cloned repository {cloneRepoData.RepositoryName} to {cloneRepoData.LocalPath}", true);
            RecordClone(Result.Succeeded);
            return Task.CompletedTask;
        }

        protected override Task CloneFailedAsync(CloneRepositoryData cloneRepoData)
        {
            var repositoryName = cloneRepoData?.RepositoryName ?? "(unknown)";
            _toolkitContext.ToolkitHost.OutputToHostConsole($"Failed to clone repository: {repositoryName}. See toolkit logs for details.", true);
            RecordClone(Result.Failed);
            return Task.CompletedTask;
        }

        private void RecordClone(Result result)
        {
            var userId = _dialog.UserId;

            _toolkitContext.TelemetryLogger.RecordCodecatalystLocalClone(new CodecatalystLocalClone()
            {
                AwsAccount = MetadataValue.NotApplicable,
                AwsRegion = MetadataValue.NotApplicable,
                UserId = string.IsNullOrWhiteSpace(userId) ? MetadataValue.NotSet : userId,
                Result = result
            });
        }
    }
}
