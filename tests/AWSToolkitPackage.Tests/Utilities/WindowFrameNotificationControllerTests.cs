using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.VisualStudio.Utilities;

using Microsoft.VisualStudio;

using Moq;

using Xunit;

namespace AWSToolkitPackage.Tests.Utilities
{
    public class WindowFrameNotificationControllerTests
    {
        private readonly WindowFrameNotificationController _sut;
        private readonly Mock<IAWSToolkitControl> _awsControl = new Mock<IAWSToolkitControl>();

        public WindowFrameNotificationControllerTests()
        {
            _sut = new WindowFrameNotificationController(_awsControl.Object);
        }

        [Theory]
        [InlineData(true, VSConstants.S_OK)]
        [InlineData(false, VSConstants.E_ABORT)]
        public void OnClose(bool controlResult, int valueExpected)
        {
            uint val = 0;
            _awsControl.Setup(x => x.CanClose()).Returns(controlResult);

            Assert.Equal(valueExpected, _sut.OnClose(ref val));
        }
    }
}
