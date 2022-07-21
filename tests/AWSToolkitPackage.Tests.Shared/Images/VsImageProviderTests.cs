using System;

using Amazon.AWSToolkit.CommonUI.Images;
using Amazon.AWSToolkit.VisualStudio.Images;

using AWSToolkit.Tests.Common.VS;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell.Interop;

using Moq;

using Xunit;

namespace AWSToolkitPackage.Tests.Images
{
    [Collection(UIThreadFixtureCollection.CollectionName)]
    public class VsImageProviderTests
    {
        private readonly UIThreadFixture _fixture;

        private readonly Mock<IServiceProvider> _serviceProvider = new Mock<IServiceProvider>();
        private readonly Mock<IVsImageService2> _imageService = new Mock<IVsImageService2>();
        private readonly Mock<IVsUIObject> _image = new Mock<IVsUIObject>();
        private object _imageGetDataObject;

        private readonly VsImageProvider _sut;

        public VsImageProviderTests(UIThreadFixture fixture)
        {
            _fixture = fixture;

            _serviceProvider.Setup(mock => mock.GetService(It.IsAny<Type>())).Returns(_imageService.Object);
            _imageService.Setup(mock => mock.GetImage(It.IsAny<ImageMoniker>(), It.IsAny<ImageAttributes>()))
                .Returns(_image.Object);
            _image.Setup(mock => mock.get_Data(out _imageGetDataObject));

            _sut = new VsImageProvider(_serviceProvider.Object);
        }

        [Fact]
        public void GetImage()
        {
            ImageMoniker expectedImageMoniker = KnownMonikers.Edit;

            var image = _sut.GetImage(VsKnownImages.Edit, 16);
            Assert.Null(image);

            _serviceProvider.Verify(mock => mock.GetService(It.IsAny<Type>()), Times.Once);
            _imageService.Verify(mock => mock.GetImage(
                It.Is<ImageMoniker>(moniker => moniker.ValueEquals(expectedImageMoniker)),
                It.IsAny<ImageAttributes>()), Times.Once);
            _image.Verify(mock => mock.get_Data(out _imageGetDataObject), Times.Once);
        }
    }
}
