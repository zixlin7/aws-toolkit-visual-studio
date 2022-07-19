using Amazon.AWSToolkit.Publish.Models.Configuration;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Models
{
    public class VpcConnectorVpcConfigurationDetailTests
    {
        private readonly VpcConnectorVpcConfigurationDetail _sut = new VpcConnectorVpcConfigurationDetail();

        [Theory]
        [InlineData(false, "hello", false)]
        [InlineData(false, "", false)]
        [InlineData(false, null, false)]
        [InlineData(true, "hello", false)]
        [InlineData(true, "", true)]
        [InlineData(true, null, true)]
        [InlineData(true, 123, true)]
        public void ValidationMessage(bool isVisible, object value, bool expectedHasValidationMessage)
        {
            _sut.Visible = isVisible;
            _sut.Value = value;

            Assert.Equal(expectedHasValidationMessage, !string.IsNullOrWhiteSpace(_sut.ValidationMessage));
        }
    }
}
