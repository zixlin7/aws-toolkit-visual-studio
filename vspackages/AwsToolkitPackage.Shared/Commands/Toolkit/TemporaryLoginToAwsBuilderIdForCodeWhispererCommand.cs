#if VS2022
using System;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials.Models;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Sono;

using log4net;

using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;

namespace Amazon.AWSToolkit.VisualStudio.Commands.Toolkit
{
    // TEMPORARY - Menu command class for Login to AWS Builder ID for CodeWhisperer development, remove before merging to main
    public class TemporaryLoginToAwsBuilderIdForCodeWhispererCommand : BaseCommand<TemporaryLoginToAwsBuilderIdForCodeWhispererCommand>
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(TemporaryLoginToAwsBuilderIdForCodeWhispererCommand));

        private readonly ToolkitContext _toolkitContext;

        public TemporaryLoginToAwsBuilderIdForCodeWhispererCommand(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public static Task<TemporaryLoginToAwsBuilderIdForCodeWhispererCommand> InitializeAsync(
            ToolkitContext toolkitContext,
            Guid menuGroup, int commandId,
            AsyncPackage package)
        {
            return InitializeAsync(
                () => new TemporaryLoginToAwsBuilderIdForCodeWhispererCommand(toolkitContext),
                menuGroup, commandId,
                package);
        }

        protected override void Execute(object sender, EventArgs e)
        {
            try
            {
                _toolkitContext.ToolkitHost.OutputToHostConsole("Attempting to update AWS Builder ID bearer token on LSP.");

                // See SonoCredentialProviderFactory.Initialize and SonoCredentialIdentifier ctor for credId name details
                var credId = _toolkitContext.CredentialManager.GetCredentialIdentifierById($"{SonoCredentialProviderFactory.FactoryId}:default");
                var region = _toolkitContext.RegionProvider.GetRegion(RegionEndpoint.USEast1.SystemName);
                var toolkitCreds = _toolkitContext.CredentialManager.GetToolkitCredentials(credId, region);

                if (!toolkitCreds.GetTokenProvider().TryResolveToken(out var awsToken))
                {
                    NotifyError("Cannot resolve AWS Builder ID bearer token.");
                    return;
                }

                var componentModel = Package.GetService<SComponentModel, IComponentModel>();
                var cwClient = componentModel.GetService<ICodeWhispererLspClient>();

                IToolkitLspCredentials lspCreds = null;
                try
                {
                    lspCreds = cwClient.CreateToolkitLspCredentials();
                }
                catch
                {
                    NotifyError("Cannot CreateToolkitLspCredentials.  Be sure a C# file is open in an editor so LSP is running.");
                    return;
                }

                lspCreds.UpdateToken(new BearerToken() { Token = awsToken.Token });

                _toolkitContext.ToolkitHost.OutputToHostConsole("Updated AWS Builder ID bearer token on LSP.");
            }
            catch (Exception ex)
            {
                NotifyError("Failed to update AWS Builder ID bearer token on LSP.", ex);
            }
        }

        private void NotifyError(string message, Exception ex = null)
        {
            _logger.Error(message, ex);
            _toolkitContext.ToolkitHost.OutputToHostConsole(message);
            _toolkitContext.ToolkitHost.ShowError(message, ex?.Message ?? message);
        }

        protected override void BeforeQueryStatus(OleMenuCommand menuCommand, EventArgs e)
        {
            try
            {
                menuCommand.Visible = true;
            }
            catch
            {
                _logger.Error("Cannot show Log into AWS Builder ID for CodeWhisperer menu item.");
                // Swallow error for stability -- menu will not be visible
                // do not log - this is invoked each time the menu is opened
            }
        }
    }
}
#endif
