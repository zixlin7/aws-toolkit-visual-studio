using System;
using Amazon.AWSToolkit.Settings;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Tests.Common.Settings;
using Amazon.AWSToolkit.VisualStudio.FirstRun.Controller;
using Moq;
using Xunit;

namespace AWSToolkitPackage.Tests.FirstRun
{
    public class FirstRunControllerTests
    {
        private readonly FirstRunController _sut;

        private readonly Mock<IToolkitSettingsWatcher> _settingsWatcher = new Mock<IToolkitSettingsWatcher>();
        private readonly Mock<IAWSToolkitShellProvider> _shellProvider = new Mock<IAWSToolkitShellProvider>();
        private readonly FakeSettingsPersistence _settingsPersistence = new FakeSettingsPersistence();

        public FirstRunControllerTests()
        {
            ToolkitSettings.Initialize(_settingsPersistence);
            ToolkitSettings.Instance.TelemetryEnabled = true;

            _shellProvider.Setup(mock => mock.ExecuteOnUIThread(It.IsAny<Action>()))
                .Callback<Action>(action => action());

            _sut = new FirstRunController(null, _settingsWatcher.Object, _shellProvider.Object);
        }

        [Fact]
        public void SettingsChangesUpdatesControllerModel()
        {
            _sut.Execute();

            ToolkitSettings.Instance.TelemetryEnabled = true;
            RaiseSettingsChanged();
            Assert.True(_sut.Model.CollectAnalytics);

            ToolkitSettings.Instance.TelemetryEnabled = false;
            RaiseSettingsChanged();
            Assert.False(_sut.Model.CollectAnalytics);
        }

        private void RaiseSettingsChanged()
        {
            _settingsWatcher.Raise(mock => mock.SettingsChanged += null, EventArgs.Empty);
        }
    }
}