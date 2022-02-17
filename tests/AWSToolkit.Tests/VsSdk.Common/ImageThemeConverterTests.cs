using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using AwsToolkit.VsSdk.Common.CommonUI.Converters;

using Xunit;

using Object = System.Object;

namespace AWSToolkit.Tests.VsSdk.Common
{
    public class ImageThemeConverterTests
    {
        private readonly ImageThemeConverter _sut = new ImageThemeConverter();

        public static IEnumerable<object[]> InvalidImageThemeData = new List<object[]>
        {
            new object[] { new object[] { null } },
            new object[] { new object[] { Colors.Black } },
            new object[] { new object[] { 3, "hello" } },
            new object[] { new object[] { CreateEmptyBitmap(), 78 } },
            new object[] { new object[] { Colors.Black, CreateEmptyBitmap() } },
        };

        [Theory]
        [MemberData(nameof(InvalidImageThemeData))]
        public void Convert_WhenInvalid(object[] values)
        {
            Assert.Equal(DependencyProperty.UnsetValue, Convert(values));
        }

        [Fact]
        public void Convert_WhenValid()
        {
            var originalImage = CreateEmptyBitmap();
            var values = new Object[] { originalImage, Colors.Black };
            var themedImage = Convert(values);
            Assert.NotEqual(themedImage, originalImage);
        }

        private static BitmapSource CreateEmptyBitmap()
        {
            return BitmapSource.Create(1, 1, 1, 1, PixelFormats.BlackWhite, null, new byte[] { 0 }, 1);
        }

        private object Convert(object[] values)
        {
            return _sut.Convert(values, typeof(BitmapSource), null, null);
        }
    }
}
