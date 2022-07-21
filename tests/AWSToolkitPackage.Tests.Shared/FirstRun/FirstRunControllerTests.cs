using System;

using Amazon.AWSToolkit.Context;
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
        private readonly ToolkitSettings _toolkitSettings = FakeToolkitSettings.Create();
        private readonly ToolkitContext _toolkitContext = new ToolkitContext();

        public FirstRunControllerTests()
        {
            _toolkitSettings.TelemetryEnabled = true;

            _shellProvider.Setup(mock => mock.ExecuteOnUIThread(It.IsAny<Action>()))
                .Callback<Action>(action => action());

            _sut = new FirstRunController(null, _settingsWatcher.Object, _toolkitContext, _shellProvider.Object, _toolkitSettings);
        }

        [Fact]
        public void SettingsChangesUpdatesControllerModel()
        {
            _sut.Execute();

            _toolkitSettings.TelemetryEnabled = true;
            RaiseSettingsChanged();
            Assert.True(_sut.Model.CollectAnalytics);

            _toolkitSettings.TelemetryEnabled = false;
            RaiseSettingsChanged();
            Assert.False(_sut.Model.CollectAnalytics);
        }

        private void RaiseSettingsChanged()
        {
            _settingsWatcher.Raise(mock => mock.SettingsChanged += null, EventArgs.Empty);
        }
    }
}
