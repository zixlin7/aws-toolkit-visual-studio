using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using AwsToolkit.VsSdk.Common.CommonUI.Converters;

using Xunit;

namespace AwsToolkit.Vs.Tests.VsSdk.Common
{
    public class ImageThemeConverterTests
    {
        private readonly ImageThemeConverter _sut = new ImageThemeConverter();

        public static IEnumerable<object[]> InvalidImageThemeData = new List<object[]>
        {
            new object[] { new object[] { null } },
            new object[] { new object[] { Colors.Black } },
            new object[] { new object[] { Brushes.Black } },
            new object[] { new object[] { 3, "hello" } },
        };

        [Theory]
        [MemberData(nameof(InvalidImageThemeData))]
        public void Convert_WhenInvalid(object[] values)
        {
            Assert.Equal(DependencyProperty.UnsetValue, Convert(values));
        }

        [StaFact]
        public void Convert_WhenValid()
        {
            UserControl parent = new UserControl() { Background = Brushes.DarkBlue };
            UserControl control = new UserControl();
            parent.Content = control;

            var originalImage = CreateEmptyBitmap();
            var values = new object[] { originalImage, control };
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
