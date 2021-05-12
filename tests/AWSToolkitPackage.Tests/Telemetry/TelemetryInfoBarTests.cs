using Amazon.AWSToolkit.Tests.Common.Settings;
using Amazon.AWSToolkit.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using Xunit;

namespace AWSToolkitPackage.Tests.Telemetry
{
    [Collection(UIThreadFixtureCollection.CollectionName)]
    public class TelemetryInfoBarTests
    {
        private const string TelemetryEnabledBackingFieldName = "AnalyticsPermitted";
        private readonly UIThreadFixture _fixture;
        private readonly TelemetryInfoBar _sut;
        private readonly Mock<IVsInfoBarUIElement> _element = new Mock<IVsInfoBarUIElement>();
        private readonly FakeToolkitSettings _fakeToolkitSettings = FakeToolkitSettings.Create();

        public TelemetryInfoBarTests(UIThreadFixture fixture)
        {
            _fixture = fixture;

            _sut = new TelemetryInfoBar(_fakeToolkitSettings);
            _element.Setup(x => x.Advise(It.IsAny<IVsInfoBarUIEvents>(), out It.Ref<uint>.IsAny));
            _sut.RegisterInfoBarEvents(_element.Object);
            _fakeToolkitSettings.TelemetryEnabled = true;
        }

        [Fact]
        public void DisableClicked()
        {
            var actionItem = new Mock<IVsInfoBarActionItem>();
            actionItem.Setup(mock => mock.ActionContext).Returns(TelemetryInfoBar.ActionContexts.Disable);

            _sut.OnActionItemClicked(_element.Object, actionItem.Object);

            Assert.False(_fakeToolkitSettings.TelemetryEnabled);
            AssertInfoBarClosed();
        }

        [Fact]
        public void DontShowAgainClicked()
        {
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
