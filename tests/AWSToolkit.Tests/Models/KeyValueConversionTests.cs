using Amazon.AWSToolkit.Models;

using Xunit;

namespace AWSToolkit.Tests.Models
{
    public class KeyValueConversionTests
    {
        [Theory]
        [InlineData("sampleKey", "sampleValue", "sampleKey=sampleValue")]
        [InlineData("sampleKey", "", "sampleKey=")]
        public void ToAssignmentString(string key, string value, string expectedString)
        {
            var keyValue = new KeyValue(key, value);
            Assert.Equal(expectedString, KeyValueConversion.ToAssignmentString(keyValue));
        }

        [Theory]
        [InlineData("sampleKey=sampleValue", "sampleKey", "sampleValue")]
        [InlineData("sampleKey=", "sampleKey", "")]
        [InlineData("sampleKey", "sampleKey", "")]
        public void FromAssignmentString(string assignmentString, string expectedKey, string expectedValue)
        {
            var expectedKeyValue = new KeyValue(expectedKey, expectedValue);
            var keyValue = KeyValueConversion.FromAssignmentString(assignmentString);
            Assert.Equal(expectedKeyValue, keyValue);
        }
    }
}
