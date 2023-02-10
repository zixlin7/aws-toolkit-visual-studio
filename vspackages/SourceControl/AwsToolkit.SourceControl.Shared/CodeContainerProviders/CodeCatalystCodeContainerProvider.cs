using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.CodeCatalyst;
using Amazon.AWSToolkit.CommonUI.Dialogs;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.SourceControl;
using Amazon.AWSToolkit.Urls;

// There are a lot of duplicated type names between these two namespaces, so use aliases for clarity.
// If VS injects an unaliased using statement for any of these namespaces, remove it immediately.
using ccm = Microsoft.VisualStudio.Shell.CodeContainerManagement;
using sh = Microsoft.VisualStudio.Shell;

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

        protected override async Task UpdateStoredGitCredentialsAsync(Uri uri, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Get PAT
            var codeCatalyst = _toolkitContext.ToolkitHost.QueryAWSToolkitPluginService(typeof(IAWSCodeCatalyst)) as IAWSCodeCatalyst;

            var pat = (await codeCatalyst.GetAccessTokensAsync(_dialog.ConnectionSettings, cancellationToken))
                .FirstOrDefault();

            // save pat to credential store
            if (pat == null)
            {
                await StoreNewGitCredentialsAsync(uri, cancellationToken);
                return;
            }
            await StoreGitCredentialsAsync(pat.Secret, uri, cancellationToken);
        }

        private async Task StoreNewGitCredentialsAsync(Uri uri, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var codeCatalyst = _toolkitContext.ToolkitHost.QueryAWSToolkitPluginService(typeof(IAWSCodeCatalyst)) as IAWSCodeCatalyst;

            var pat = await codeCatalyst.CreateDefaultAccessTokenAsync(null, _dialog.ConnectionSettings, cancellationToken);

            await StoreGitCredentialsAsync(pat.Secret, uri, cancellationToken);
        }

        private Task StoreGitCredentialsAsync(string patSecret, Uri uri, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // username is in url, eg: https://foo@bar.org/repo/path, get "foo"
            var username = _dialog.CloneUrl.UserInfo;

            // Store PAT (in Windows credential store)
            var gitCredentialKey = $"git:{uri.Scheme}://{uri.DnsSafeHost}";
            using (var gitCredentials = new GitCredentials(username, patSecret, gitCredentialKey))
            {
                gitCredentials.Save();
            }

            return Task.CompletedTask;
        }

        protected override Task<CloneRepositoryData> GetCloneRepositoryDataAsync(ccm.RemoteCodeContainer onlineCodeContainer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(
                _dialog.Show() ?
                    new CloneRepositoryData(_dialog.RepositoryName, _dialog.CloneUrl, _dialog.LocalPath) :
                    null);
        }

        protected override async Task<CloneRepositoryData> HandleCloneRepositoryFailedAsync(CloneRepositoryData failedCloneRepoData, Exception cloneException)
        {
            if (cloneException?.Message?.ToLower().Contains("authentication failed") != true)
            {
                return await base.HandleCloneRepositoryFailedAsync(failedCloneRepoData, cloneException);
            }

            var recreatePat = _toolkitContext.ToolkitHost.ConfirmWithLinks("Create access token",
$@"Cloning may have failed due to an invalid <a href=""{AwsUrls.CodeCatalystUserGuidePatConcept}"">access token</a>.
Recreate access token and retry?");

            if (recreatePat)
            {
                await StoreNewGitCredentialsAsync(failedCloneRepoData.RemoteUri);

                return failedCloneRepoData;
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
            _toolkitContext.ToolkitHost.OutputToHostConsole(
                $"Failed to clone repository: {repositoryName}. See '{SourceControlGitOutputWindowPaneName}' Output window pane and toolkit logs for details.", true);
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
