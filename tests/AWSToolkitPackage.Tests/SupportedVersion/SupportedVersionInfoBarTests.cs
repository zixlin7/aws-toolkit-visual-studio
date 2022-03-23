using Amazon.AWSToolkit.Tests.Common.Settings;
using Amazon.AWSToolkit.Util;
using Amazon.AWSToolkit.VisualStudio.SupportedVersion;

using Microsoft.VisualStudio.Shell.Interop;

using Moq;

using Xunit;

namespace AWSToolkitPackage.Tests.SupportedVersion
{
    [Collection(UIThreadFixtureCollection.CollectionName)]
    public class SupportedVersionInfoBarTests
    {
        private readonly UIThreadFixture _fixture;
        private readonly SupportedVersionInfoBar _sut;
        private readonly Mock<IVsInfoBarUIElement> _element = new Mock<IVsInfoBarUIElement>();
        private readonly FakeToolkitSettings _toolkitSettings = FakeToolkitSettings.Create();

        public SupportedVersionInfoBarTests(UIThreadFixture fixture)
        {
             _fixture = fixture;
             var sampleVersionStrategy = CreateSampleVersionStrategy();
             _sut = new SupportedVersionInfoBar(sampleVersionStrategy);
            _element.Setup(x => x.Advise(It.IsAny<IVsInfoBarUIEvents>(), out It.Ref<uint>.IsAny));
            _sut.RegisterInfoBarEvents(_element.Object);
        }

        [Fact]
        public void DontShowAgainClicked()
        {
            var actionItem = new Mock<IVsInfoBarActionItem>();
            actionItem.Setup(mock => mock.ActionContext).Returns(SupportedVersionInfoBar.ActionContexts.DontShowAgain);

            _sut.OnActionItemClicked(_element.Object, actionItem.Object);
            AssertInfoBarClosed();
        }

        private void AssertInfoBarClosed()
        {
            _element.Verify(mock => mock.Close(), Times.Once);
        }
        private SupportedVersionStrategy CreateSampleVersionStrategy()
        {
            return new SupportedVersionStrategy(ToolkitHosts.Vs2017, ToolkitHosts.Vs2017, _toolkitSettings);
        }
    }
}
