using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Configuration;
using Amazon.AwsToolkit.CodeWhisperer.Settings;
using Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.Clients;
using Amazon.AwsToolkit.CodeWhisperer.Tests.TestUtilities;
using Amazon.AwsToolkit.VsSdk.Common.Tasks;

using AwsToolkit.VsSdk.Common.Settings.CodeWhisperer;

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

            var taskFactoryProvider = new ToolkitJoinableTaskFactoryProvider(ThreadHelper.JoinableTaskContext);
            _sut = new CodeWhispererSettingsPublisher(_settingsRepository, _lspClient, taskFactoryProvider);

            _settingsRepository.Settings.IncludeSuggestionsWithReferences = false;
        }

        [Fact]
        public async Task SettingsSavedShouldSendConfigurationChanged()
        {
            await InitializeLspClientAsync();

            _settingsRepository.RaiseSettingsSaved();

            _lspClient.LspConfiguration.RaisedDidChangeConfigurations.Should().HaveCount(1);
            AssertConfigurationMatches(
                _lspClient.LspConfiguration.RaisedDidChangeConfigurations[0],
                _settingsRepository.Settings);
        }

        [Fact]
        public async Task RaiseDidChangeConfigurationAsync()
        {
            await InitializeLspClientAsync();

            await _sut.RaiseDidChangeConfigurationAsync();

            _lspClient.LspConfiguration.RaisedDidChangeConfigurations.Should().HaveCount(1);
            AssertConfigurationMatches(
                _lspClient.LspConfiguration.RaisedDidChangeConfigurations[0],
                _settingsRepository.Settings);
        }

        private async Task InitializeLspClientAsync()
        {
            // initialize the client so that the sut can
            // communicate with ILspConfiguration
            await _lspClient.RaiseInitializedAsync();

            // flush the raised object, so we test from a clean state
            _lspClient.LspConfiguration.RaisedDidChangeConfigurations.Clear();
        }

        private void AssertConfigurationMatches(object actualConfiguration, CodeWhispererSettings expectedSettings)
        {
            actualConfiguration.Should()
                .BeOfType<ConfigurationResponse>();

            var configurationResponse = actualConfiguration as ConfigurationResponse;
            configurationResponse.Aws.CodeWhisperer.IncludeSuggestionsWithCodeReferences
                .Should().Be(expectedSettings.IncludeSuggestionsWithReferences);
        }
    }
}
