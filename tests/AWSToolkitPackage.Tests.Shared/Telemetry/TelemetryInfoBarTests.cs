#if VS2022_OR_LATER
using System.Threading.Tasks;

using Amazon.AWSToolkit.Tests.Common.Settings;
using Amazon.AWSToolkit.VisualStudio.Telemetry;

using AWSToolkitPackage.Tests.Utilities;

using Microsoft.VisualStudio.Sdk.TestFramework;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using Moq;

using Xunit;

namespace AWSToolkitPackage.Tests.Telemetry
{
    [Collection(TestProjectMockCollection.CollectionName)]
    public class TelemetryInfoBarTests
    {
        private const string TelemetryEnabledBackingFieldName = "AnalyticsPermitted";
        private readonly TelemetryInfoBar _sut;
        private readonly Mock<IVsInfoBarUIElement> _element = new Mock<IVsInfoBarUIElement>();
        private readonly FakeToolkitSettings _fakeToolkitSettings = FakeToolkitSettings.Create();

        public TelemetryInfoBarTests(GlobalServiceProvider globalServiceProvider)
        {
            globalServiceProvider.Reset();

            _sut = new TelemetryInfoBar(_fakeToolkitSettings);
            _element.Setup(x => x.Advise(It.IsAny<IVsInfoBarUIEvents>(), out It.Ref<uint>.IsAny));

            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                _sut.RegisterInfoBarEvents(_element.Object);
            });

            _fakeToolkitSettings.TelemetryEnabled = true;
        }

        [Fact]
        public async Task DisableClicked()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var actionItem = new Mock<IVsInfoBarActionItem>();
            actionItem.Setup(mock => mock.ActionContext).Returns(TelemetryInfoBar.ActionContexts.Disable);

            _sut.OnActionItemClicked(_element.Object, actionItem.Object);

            Assert.False(_fakeToolkitSettings.TelemetryEnabled);
            AssertInfoBarClosed();
        }

        [Fact]
        public async Task DontShowAgainClicked()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var actionItem = new Mock<IVsInfoBarActionItem>();
            actionItem.Setup(mock => mock.ActionContext).Returns(TelemetryInfoBar.ActionContexts.DontShowAgain);

            Assert.True(_fakeToolkitSettings.TelemetryEnabled);
            _sut.OnActionItemClicked(_element.Object, actionItem.Object);
            AssertInfoBarClosed();
        }

        private void AssertInfoBarClosed()
        {
            _element.Verify(mock => mock.Close(), Times.Once);
        }
    }
}
#endif
