using System.Globalization;
using System.Windows;

using Amazon.AWSToolkit.Lambda.Model;
using Amazon.AWSToolkit.Lambda.Util;

using Xunit;

namespace AWSToolkit.Tests.Lambda
{
    public class ArchitectureToBoolConverterTests
    {
        private readonly ArchitectureToBoolConverter _sut = new ArchitectureToBoolConverter();

        [Fact]
        public void Convert_ShouldReturnTrueWhenMatch()
        {
            Assert.Equal(true, Convert(LambdaArchitecture.Arm, LambdaArchitecture.Arm));
        }

        [Fact]
        public void Convert_ShouldReturnFalseWhenNull()
        {
            Assert.Equal(false, Convert(null, LambdaArchitecture.Arm));
        }

        [Fact]
        public void Convert_ShouldReturnFalseWhenMismatch()
        {
            Assert.Equal(false, Convert(LambdaArchitecture.X86, LambdaArchitecture.Arm));
        }

        [Fact]
        public void ConvertBack_ShouldReturnParameterWhenTrue()
        {
            Assert.Equal(LambdaArchitecture.X86, ConvertBack(true, LambdaArchitecture.X86));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(false)]
        [InlineData("non-boolean-value")]
        public void ConvertBack_ShouldReturnUnset(object value)
        {
            Assert.Equal(DependencyProperty.UnsetValue, ConvertBack(value, LambdaArchitecture.X86));
        }

        private object Convert(object value, object parameter)
        {
            return _sut.Convert(value, typeof(bool?), parameter, CultureInfo.CurrentCulture);
        }

        private object ConvertBack(object value, object parameter)
        {
            return _sut.ConvertBack(value, typeof(LambdaArchitecture), parameter, CultureInfo.CurrentCulture);
        }
    }
}
