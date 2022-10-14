using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Context;

// There are a lot of duplicated type names between these two namespaces, so use aliases for clarity
using ccm = Microsoft.VisualStudio.Shell.CodeContainerManagement;
using sh = Microsoft.VisualStudio.Shell;

namespace Amazon.AwsToolkit.SourceControl.CodeContainerProviders
{
    [Guid(CodeCommitCodeContainerProviderId)]
    public class CodeCommitCodeContainerProvider : BaseGitCodeContainerProvider
    {
        public const string CodeCommitCodeContainerProviderId = "c886324b-bce8-4eab-84f3-449e59f32736";

        public const string CodeCommitSccProviderId = "112b81bf-8dd8-45e2-b5fb-2b5d62206ae6";

        private static readonly Guid CodeCommitSccProviderIdGuid = new Guid(CodeCommitSccProviderId);

        public CodeCommitCodeContainerProvider(ToolkitContext toolkitContext, sh.IAsyncServiceProvider asyncServiceProvider)
            : base(toolkitContext, asyncServiceProvider, CodeCommitSccProviderIdGuid) { }

        protected override Task<CloneRepositoryData> GetCloneRepositoryDataAsync(ccm.RemoteCodeContainer onlineCodeContainer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var dialog = _toolkitContext.ToolkitHost.GetDialogFactory().CreateCloneCodeCommitRepositoryDialog();

            return Task.FromResult(dialog.Show() ? new CloneRepositoryData(dialog.RepositoryName, dialog.RemoteUri, dialog.LocalPath) : null);
        }
    }
}
