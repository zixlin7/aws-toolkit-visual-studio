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
    [Guid(CodeCatalystCodeContainerProviderId)]
    public class CodeCatalystCodeContainerProvider : BaseGitCodeContainerProvider
    {
        public const string CodeCatalystCodeContainerProviderId = "0bc6e8f8-a3fd-4a83-89fe-07215f5facae";

        public const string CodeCatalystSccProviderId = "25c452c5-d427-4275-b735-b5092aad4b35";

        private static readonly Guid CodeCatalystSccProviderIdGuid = new Guid(CodeCatalystSccProviderId);

        public CodeCatalystCodeContainerProvider(ToolkitContext toolkitContext, sh.IAsyncServiceProvider asyncServiceProvider)
            : base(toolkitContext, asyncServiceProvider, CodeCatalystSccProviderIdGuid) { }

        protected override Task<CloneRepositoryData> GetCloneRepositoryDataAsync(ccm.RemoteCodeContainer onlineCodeContainer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var dialog = _toolkitContext.ToolkitHost.GetDialogFactory().CreateCloneCodeCatalystRepositoryDialog();

            return Task.FromResult(dialog.Show() ? new CloneRepositoryData(dialog.RepositoryName, dialog.CloneUrl, dialog.LocalPath) : null);
        }
    }
}
