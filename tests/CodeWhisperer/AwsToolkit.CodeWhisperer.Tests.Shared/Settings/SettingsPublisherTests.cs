using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Configuration;
using Amazon.AwsToolkit.CodeWhisperer.Settings;
using Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.Clients;
using Amazon.AwsToolkit.CodeWhisperer.Tests.TestUtilities;
using Amazon.AwsToolkit.VsSdk.Common.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.Sdk.TestFramework;
using Microsoft.VisualStudio.Shell;

using Xunit;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Settings
{
    internal class FakeSettingsPublisher : SettingsPublisher
    {
        public object Configuration;

        public FakeSettingsPublisher(
            IToolkitLspClient lspClient,
            ToolkitJoinableTaskFactoryProvider taskFactoryProvider) : base(lspClient, taskFactoryProvider)
        {
        }

        internal override Task<object> GetConfigurationAsync()
        {
            return Task.FromResult(Configuration);
        }
    }

    [Collection(VsMockCollection.CollectionName)]
    public class SettingsPublisherTests
    {
        private readonly FakeToolkitLspClient _lspClient = new FakeToolkitLspClient();
        private readonly FakeSettingsPublisher _sut;

        public SettingsPublisherTests(GlobalServiceProvider serviceProvider)
        {
            serviceProvider.Reset();

            var taskFactoryProvider = new ToolkitJoinableTaskFactoryProvider(ThreadHelper.JoinableTaskContext);
            _sut = new FakeSettingsPublisher(_lspClient, taskFactoryProvider)
            {
                Configuration = new { Name = "sample-name", Value = 1234, }
            };
        }

        [Fact]
        public async Task InitializedShouldSendConfigurationChanged()
        {
            await _lspClient.RaiseInitializedAsync();

            _lspClient.LspConfiguration.RaisedDidChangeConfigurations.Should().HaveCount(1);
            _lspClient.LspConfiguration.RaisedDidChangeConfigurations.Should().Contain(_sut.Configuration);
        }

        [Fact]
        public async Task RequestWorkspaceConfigurationAsync()
        {
            var eventArgs = new WorkspaceConfigurationEventArgs();

            await _lspClient.RaiseRequestWorkspaceConfigurationAsync(eventArgs);

            eventArgs.Configuration.Should().Be(_sut.Configuration);
        }

        [Fact]
        public async Task RaiseDidChangeConfigurationAsync_Uninitialized()
        {
            await _sut.RaiseDidChangeConfigurationAsync();

            _lspClient.LspConfiguration.RaisedDidChangeConfigurations.Should().BeEmpty();
        }

        [Fact]
        public async Task RaiseDidChangeConfigurationAsync()
        {
            await _lspClient.RaiseInitializedAsync();
            _lspClient.LspConfiguration.RaisedDidChangeConfigurations.Clear();

            await _sut.RaiseDidChangeConfigurationAsync();

            _lspClient.LspConfiguration.RaisedDidChangeConfigurations.Should().HaveCount(1);
            _lspClient.LspConfiguration.RaisedDidChangeConfigurations.Should().Contain(_sut.Configuration);
        }
    }
}
