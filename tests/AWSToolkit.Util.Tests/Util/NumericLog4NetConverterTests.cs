using System;

using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Util
{
    public class NumericLog4NetConverterTests
    {
        public NumericLog4NetConverter converter = new NumericLog4NetConverter();

        [Theory]
        [InlineData(typeof(int), false)]
        [InlineData(typeof(bool), false)]
        [InlineData(typeof(object), false)]
        [InlineData(typeof(string), true)]
        public void CanConvertFrom(Type source, bool expectedValue)
        {
            var value = converter.CanConvertFrom(source);
            Assert.Equal(expectedValue, value);
        }

        [Fact]
        public void ConvertFrom()
        {
            var value = converter.ConvertFrom("10");
            Assert.Equal(10, value);
        }

        [Theory]
        [InlineData("false")]
        [InlineData("hello world")]
        [InlineData("123 hello")]
        public void ConvertFrom_Throws(object source)
        {
            Assert.Throws<FormatException>(() => converter.ConvertFrom(source));
        }
    }
}
