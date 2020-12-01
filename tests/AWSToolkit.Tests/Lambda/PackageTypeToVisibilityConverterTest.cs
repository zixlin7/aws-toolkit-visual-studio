using Amazon.AWSToolkit.Lambda.Util;
using Amazon.Lambda;
using System.Globalization;
using System.Windows;
using Xunit;

namespace AWSToolkit.Tests.Lambda
{
    public class PackageTypeToVisibilityConverterTest
    {
        private readonly PackageTypeToVisibilityConverter _sut = new PackageTypeToVisibilityConverter();

        [Fact]
        public void ConvertMatch()
        {
            Assert.Equal(
                Visibility.Visible,
                _sut.Convert(PackageType.Zip, null, PackageType.Zip, CultureInfo.CurrentCulture));
        }

        [Fact]
        public void ConvertMismatch()
        {
            Assert.Equal(
                Visibility.Collapsed,
                _sut.Convert(PackageType.Zip, null, PackageType.Image, CultureInfo.CurrentCulture));
        }

        [Fact]
        public void HandlesBadInput()
        {
            Assert.Equal(
                DependencyProperty.UnsetValue,
                _sut.Convert(PackageType.Zip, null, null, CultureInfo.CurrentCulture));
            Assert.Equal(
                DependencyProperty.UnsetValue,
                _sut.Convert(PackageType.Zip, null, "garbage", CultureInfo.CurrentCulture));

            Assert.Equal(
                DependencyProperty.UnsetValue,
                _sut.Convert(null, null, PackageType.Image, CultureInfo.CurrentCulture));
            Assert.Equal(
                DependencyProperty.UnsetValue,
                _sut.Convert("garbage", null, PackageType.Image, CultureInfo.CurrentCulture));
        }
    }
}