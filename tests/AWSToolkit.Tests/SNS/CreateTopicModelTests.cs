using Amazon.AWSToolkit.SNS.Model;

using Xunit;

namespace AWSToolkit.Tests.SNS
{
    public class CreateTopicModelTests
    {
        private readonly CreateTopicModel _sut = new CreateTopicModel();

        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData("   ", true)]
        [InlineData("hello", false)]
        [InlineData("Hello", false)]
        [InlineData("hello-World", false)]
        [InlineData("Hello_world", false)]
        [InlineData("helloWorld1234", false)]
        [InlineData(" hello", true)]
        [InlineData("hello ", true)]
        [InlineData("hello world", true)]
        [InlineData("hello!world", true)]
        public void TopicNameErrorValidation(string topicName, bool expectedHasError)
        {
            _sut.TopicName = topicName;

            Assert.Equal(expectedHasError, ModelHasError());
        }

        private bool ModelHasError()
        {
            return !string.IsNullOrWhiteSpace(_sut.AsIDataErrorInfo.Error);
        }
    }
}
