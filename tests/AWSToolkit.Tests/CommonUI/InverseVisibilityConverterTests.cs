using System.Windows;

using Amazon.AWSToolkit.CommonUI.Converters;

using Xunit;

namespace AWSToolkit.Tests.CommonUI
{
    public class InverseVisibilityConverterTests
    {

        private readonly InverseVisibilityConverter _sut = new InverseVisibilityConverter();

        [Theory]
        [InlineData(Visibility.Visible, Visibility.Collapsed)]
        [InlineData(Visibility.Collapsed, Visibility.Visible)]

        public void Convert(object value, Visibility expectedValue)
        {
            Assert.Equal(expectedValue, _sut.Convert(value, typeof(Visibility), null, null));
        }



        [Theory]
        [InlineData(Visibility.Visible, Visibility.Collapsed)]
        [InlineData(Visibility.Collapsed, Visibility.Visible)]
        public void ConvertBack(object value, Visibility expectedValue)
        {
            Assert.Equal(expectedValue, _sut.Convert(value, typeof(bool?), null, null));
        }
    }
}
