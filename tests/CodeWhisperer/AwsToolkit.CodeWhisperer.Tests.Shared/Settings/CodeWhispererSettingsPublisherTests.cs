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
    [Collection(VsMockCollection.CollectionName)]
    public class CodeWhispererSettingsPublisherTests
    {
        private readonly FakeCodeWhispererClient _lspClient = new FakeCodeWhispererClient();
        private readonly FakeCodeWhispererSettingsRepository _settingsRepository = new FakeCodeWhispererSettingsRepository();
        private readonly CodeWhispererSettingsPublisher _sut;

        public CodeWhispererSettingsPublisherTests(GlobalServiceProvider serviceProvider)
        {
            serviceProvider.Reset();

            _lspClient.Status = LspClientStatus.Running;

            var taskFactoryProvider = new ToolkitJoinableTaskFactoryProvider(ThreadHelper.JoinableTaskContext);
            _sut = new CodeWhispererSettingsPublisher(_settingsRepository, _lspClient, taskFactoryProvider);

            _settingsRepository.Settings.IncludeSuggestionsWithReferences = false;
            _settingsRepository.Settings.ShareCodeWhispererContentWithAws = false;
        }

        [Fact]
        public async Task SettingsSavedShouldSendConfigurationChanged()
        {
            await InitializeLspClientAsync();

            _settingsRepository.RaiseSettingsSaved();

            _lspClient.LspConfiguration.RaisedDidChangeConfigurations.Should().HaveCount(1);
        }

        [Fact]
        public async Task RaiseDidChangeConfigurationAsync()
        {
            await InitializeLspClientAsync();

            await _sut.RaiseDidChangeConfigurationAsync();

            _lspClient.LspConfiguration.RaisedDidChangeConfigurations.Should().HaveCount(1);
        }

        [Fact]
        public async Task RequestWorkspaceConfigurationAsync()
        {
            var eventArgs = new WorkspaceConfigurationEventArgs();

            await _lspClient.RaiseRequestWorkspaceConfigurationAsync(eventArgs);

            eventArgs.Response.Should().Contain(
                CodeWhispererSettingsNames.IncludeSuggestionsWithCodeReferences,
                _settingsRepository.Settings.IncludeSuggestionsWithReferences);
            eventArgs.Response.Should().Contain(
                CodeWhispererSettingsNames.ShareCodeWhispererContentWithAws,
                _settingsRepository.Settings.ShareCodeWhispererContentWithAws);
        }

        private async Task InitializeLspClientAsync()
        {
            // initialize the client so that the sut can
            // communicate with ILspConfiguration
            await _lspClient.RaiseInitializedAsync();

            // flush the raised object, so we test from a clean state
            _lspClient.LspConfiguration.RaisedDidChangeConfigurations.Clear();
        }
    }
}
