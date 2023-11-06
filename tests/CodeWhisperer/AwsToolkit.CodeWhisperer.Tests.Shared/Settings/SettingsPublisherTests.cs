using System.Collections.Generic;
using System.Linq;
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
        public readonly Dictionary<string, object> ConfigurationState = new Dictionary<string, object>();

        public FakeSettingsPublisher(
            IToolkitLspClient lspClient,
            ToolkitJoinableTaskFactoryProvider taskFactoryProvider) : base(lspClient, taskFactoryProvider)
        {
        }

        internal override Task LoadConfigurationStateAsync(Dictionary<string, object> configurationState)
        {
            foreach (var keyValuePair in ConfigurationState)
            {
                configurationState[keyValuePair.Key] = keyValuePair.Value;
            }

            return Task.CompletedTask;
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

            _lspClient.Status = LspClientStatus.Running;

            var taskFactoryProvider = new ToolkitJoinableTaskFactoryProvider(ThreadHelper.JoinableTaskContext);
            _sut = new FakeSettingsPublisher(_lspClient, taskFactoryProvider);
            _sut.ConfigurationState["sample-name"] = 1234;
        }

        [Fact]
        public async Task InitializedShouldSendConfigurationChanged()
        {
            await _lspClient.RaiseInitializedAsync();

            _lspClient.LspConfiguration.RaisedDidChangeConfigurations.Should().HaveCount(1);
        }

        [Fact]
        public async Task RequestWorkspaceConfigurationAsync()
        {
            var eventArgs = new WorkspaceConfigurationEventArgs();

            await _lspClient.RaiseRequestWorkspaceConfigurationAsync(eventArgs);

            eventArgs.Response.Should().BeEquivalentTo(_sut.ConfigurationState);
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
        }

        [Fact]
        public async Task RaiseDidChangeConfigurationAsync_ClientNotRunning()
        {
            _lspClient.Status = LspClientStatus.NotRunning;
            await _lspClient.RaiseInitializedAsync();
            await _sut.RaiseDidChangeConfigurationAsync();

            _lspClient.LspConfiguration.RaisedDidChangeConfigurations.Should().BeEmpty();
        }
    }
}
