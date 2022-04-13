using System.Windows;

using Amazon.AWSToolkit.CommonUI.Converters;

using Xunit;

namespace AWSToolkit.Tests.CommonUI
{
    public class HasValueVisibilityConverterTests
    {
        private readonly HasValueVisibilityConverter _converter = new HasValueVisibilityConverter();

        [Theory]
        [InlineData("", Visibility.Collapsed)]
        [InlineData(null, Visibility.Collapsed)]
        [InlineData(1, Visibility.Collapsed)]
        [InlineData(true, Visibility.Collapsed)]
        [InlineData("hello", Visibility.Visible)]
        public void Convert(object value, Visibility expectedVisibility)
        {
            Assert.Equal(expectedVisibility, _converter.Convert(value, typeof(Visibility), null, null));
        }
    }
}
