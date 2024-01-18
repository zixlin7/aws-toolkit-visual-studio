using System;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
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
    public class EnabledStateManagerTests : IDisposable
    {
        private readonly FakeCodeWhispererClient _lspClient = new FakeCodeWhispererClient();
        private readonly FakeCodeWhispererSettingsRepository _settingsRepository = new FakeCodeWhispererSettingsRepository();

        private readonly EnabledStateManager _sut;

        public EnabledStateManagerTests(GlobalServiceProvider globalServiceProvider)
        {
            globalServiceProvider.Reset();

            var taskFactoryProvider = new ToolkitJoinableTaskFactoryProvider(ThreadHelper.JoinableTaskContext);

            _sut = new EnabledStateManager(_lspClient, _settingsRepository, taskFactoryProvider);
        }

        [Theory]
        [InlineData(true, LspClientStatus.SettingUp, true)]
        [InlineData(true, LspClientStatus.Running, false)]
        [InlineData(true, LspClientStatus.Error, true)]
        [InlineData(true, LspClientStatus.NotRunning, true)]
        [InlineData(false, LspClientStatus.SettingUp, false)]
        [InlineData(false, LspClientStatus.Running, false)]
        [InlineData(false, LspClientStatus.Error, false)]
        [InlineData(false, LspClientStatus.NotRunning, false)]
        public void SettingsUpdateAffectsClientStartup(bool isEnabledSetting, LspClientStatus clientStatus, bool expectedIsStarted)
        {
            _lspClient.IsStarted = false;
            _settingsRepository.Settings.IsEnabled = isEnabledSetting;
            _lspClient.Status = clientStatus;

            _settingsRepository.RaiseSettingsSaved();

            _lspClient.IsStarted.Should().Be(expectedIsStarted);
        }

        [Theory]
        [InlineData(true, LspClientStatus.SettingUp, true)]
        [InlineData(true, LspClientStatus.Running, true)]
        [InlineData(true, LspClientStatus.Error, true)]
        [InlineData(true, LspClientStatus.NotRunning, true)]
        [InlineData(false, LspClientStatus.SettingUp, false)]
        [InlineData(false, LspClientStatus.Running, false)]
        [InlineData(false, LspClientStatus.Error, true)]
        [InlineData(false, LspClientStatus.NotRunning, true)]
        public void SettingsUpdateAffectsClientShutdown(bool isEnabledSetting, LspClientStatus clientStatus, bool expectedIsStarted)
        {
            _lspClient.IsStarted = true;
            _settingsRepository.Settings.IsEnabled = isEnabledSetting;
            _lspClient.Status = clientStatus;

            _settingsRepository.RaiseSettingsSaved();

            _lspClient.IsStarted.Should().Be(expectedIsStarted);
        }

        public void Dispose()
        {
            _sut.Dispose();
        }
    }
}
