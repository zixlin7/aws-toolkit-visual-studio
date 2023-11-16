#if VS2022_OR_LATER
using System.Threading.Tasks;

using Amazon.AWSToolkit.Tests.Common.Settings;
using Amazon.AWSToolkit.Util;
using Amazon.AWSToolkit.VisualStudio.SupportedVersion;

using AWSToolkitPackage.Tests.Utilities;

using Microsoft.VisualStudio.Sdk.TestFramework;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using Moq;

using Xunit;

namespace AWSToolkitPackage.Tests.SupportedVersion
{
    [Collection(TestProjectMockCollection.CollectionName)]
    public class SupportedVersionInfoBarTests
    {
        private readonly SupportedVersionInfoBar _sut;
        private readonly Mock<IVsInfoBarUIElement> _element = new Mock<IVsInfoBarUIElement>();
        private readonly FakeToolkitSettings _toolkitSettings = FakeToolkitSettings.Create();

        public SupportedVersionInfoBarTests(GlobalServiceProvider globalServiceProvider)
        {
            globalServiceProvider.Reset();
            var sampleVersionStrategy = CreateSampleVersionStrategy();
            _sut = new SupportedVersionInfoBar(sampleVersionStrategy);
            _element.Setup(x => x.Advise(It.IsAny<IVsInfoBarUIEvents>(), out It.Ref<uint>.IsAny));

            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                _sut.RegisterInfoBarEvents(_element.Object);
            });
        }

        [Fact]
        public async Task DontShowAgainClicked()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

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
#endif
