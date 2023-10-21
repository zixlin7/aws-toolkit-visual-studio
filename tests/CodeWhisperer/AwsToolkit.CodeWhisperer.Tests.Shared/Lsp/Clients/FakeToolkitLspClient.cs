using System;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Configuration;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials;
using Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.Configuration;
using Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.Credentials;

using Microsoft.VisualStudio.Threading;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.Clients
{
    public class FakeToolkitLspClient : IToolkitLspClient
    {
        public readonly FakeLspConfiguration LspConfiguration = new FakeLspConfiguration();
        public readonly FakeToolkitLspCredentials CredentialsProtocol = new FakeToolkitLspCredentials();

        public event AsyncEventHandler<EventArgs> InitializedAsync;
        public event AsyncEventHandler<WorkspaceConfigurationEventArgs> RequestWorkspaceConfigurationAsync;

        public IToolkitLspCredentials CreateToolkitLspCredentials()
        {
            return CredentialsProtocol;
        }

        public ILspConfiguration CreateLspConfiguration()
        {
            return LspConfiguration;
        }

        public async Task RaiseInitializedAsync()
        {
            await InitializedAsync.InvokeAsync(this, EventArgs.Empty);
        }

        public async Task RaiseRequestWorkspaceConfigurationAsync(WorkspaceConfigurationEventArgs args)
        {
            await RequestWorkspaceConfigurationAsync.InvokeAsync(this, args);
        }
    }
}
