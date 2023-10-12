﻿using System;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Install;
using Amazon.AwsToolkit.CodeWhisperer.Tests.Settings;
using Amazon.AWSToolkit.CommonUI.Notifications;
using Amazon.AWSToolkit.Tests.Common.Context;

using Moq;

using Xunit;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.Install
{
    public class CodeWhispererInstallerTests : IDisposable
    {
        private readonly FakeCodeWhispererSettingsRepository _settingsRepository =
            new FakeCodeWhispererSettingsRepository();

        private readonly ToolkitContextFixture _contextFixture = new ToolkitContextFixture();
        private readonly Mock<ITaskStatusNotifier> _taskNotifier = new Mock<ITaskStatusNotifier>();
        private readonly CodeWhispererInstaller _sut;

        public CodeWhispererInstallerTests()
        {
            _sut = new CodeWhispererInstaller(_contextFixture.ToolkitContext, _settingsRepository);
        }

        [Fact]
        public async Task DownloadAsync_WhenCancelled()
        {
            var tokenSource = new CancellationTokenSource();
            tokenSource.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await _sut.ExecuteAsync(_taskNotifier.Object, tokenSource.Token));
        }

        [Fact]
        public async Task DownloadAsync_WhenLocalOverride()
        {
            _settingsRepository.Settings.LanguageServerPath = "test-local-path/abc.exe";
            var path = await _sut.ExecuteAsync(_taskNotifier.Object);

            Assert.Equal(_settingsRepository.Settings.LanguageServerPath, path);
        }

        public void Dispose()
        {
            _settingsRepository?.Dispose();
        }
    }
}
