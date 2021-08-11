using System;

using Amazon.AWSToolkit.CommonUI.Converters;

using Xunit;

namespace AWSToolkit.Tests.CommonUI
{
    public class NullableInverseBooleanConverterTests
    {
        private readonly NullableInverseBooleanConverter _sut = new NullableInverseBooleanConverter();

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(null, null)]
        public void Convert(object value, bool? expectedValue)
        {
            Assert.Equal(expectedValue, _sut.Convert(value, typeof(bool?), null, null));
        }

        [Fact]
        public void ConvertThrowException()
        {
            Assert.Throws<InvalidOperationException>(() => _sut.Convert(true, typeof(bool), null, null));
        }


        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(null, null)]
        public void ConvertBack(object value, bool? expectedValue)
        {
            Assert.Equal(expectedValue, _sut.Convert(value, typeof(bool?), null, null));
        }
    }
}
