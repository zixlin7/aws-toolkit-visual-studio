using System.Windows;
using Amazon.AWSToolkit.CommonUI.Converters;
using Xunit;

namespace AWSToolkit.Tests.CommonUI
{
    public class InverseBooleanToVisibilityConverterTests
    {
        private readonly InverseBooleanToVisibilityConverter _sut = new InverseBooleanToVisibilityConverter();

        [Theory]
        [InlineData(true, Visibility.Collapsed)]
        [InlineData(false, Visibility.Visible)]
        [InlineData(null, Visibility.Visible)]
        [InlineData("garbage", Visibility.Visible)]
        public void Convert(object value, Visibility expectedVisibility)
        {
            Assert.Equal(expectedVisibility, _sut.Convert(value, typeof(Visibility), null, null));
        }

        [Theory]
        [InlineData(Visibility.Collapsed, true)]
        [InlineData(Visibility.Visible, false)]
        [InlineData(Visibility.Hidden, false)]
        [InlineData(null, false)]
        [InlineData("garbage", false)]
        public void ConvertBack(object value, bool expectedResult)
        {
            Assert.Equal(expectedResult, _sut.ConvertBack(value, typeof(bool), null, null));
        }
    }
}