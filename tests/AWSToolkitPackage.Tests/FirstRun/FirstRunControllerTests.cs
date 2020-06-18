using System;
using System.Collections.Generic;
using Amazon.AWSToolkit.Settings;
using Amazon.AWSToolkit.Shared;
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
        private readonly Mock<SettingsPersistence> _persistenceManager = new Mock<SettingsPersistence>();
        private readonly Dictionary<string, string> _persistenceData = new Dictionary<string, string>();

        public FirstRunControllerTests()
        {
            SetupPersistenceManager();
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

        /// <summary>
        /// Simulates saving settings to disk (ToolkitSettings.Instance is backed by this)
        /// </summary>
        private void SetupPersistenceManager()
        {
            _persistenceManager.Setup(mock => mock.GetSetting(It.IsAny<string>()))
                .Returns<string>(name =>
                {
                    if (_persistenceData.TryGetValue(name, out string value))
                    {
                        return value;
                    }

                    return null;
                });

            _persistenceManager.Setup(mock => mock.SetSetting(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((name, value) =>
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        _persistenceData.Remove(name);
                    }
                    else
                    {
                        _persistenceData[name] = value;
                    }
                });

            ToolkitSettings.Initialize(_persistenceManager.Object);
        }
    }
}