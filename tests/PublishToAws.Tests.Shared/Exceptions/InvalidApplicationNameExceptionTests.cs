using Amazon.AWSToolkit.Publish;

using AWS.Deploy.ServerMode.Client;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Exceptions
{
    public class InvalidApplicationNameExceptionTests
    {
        private static readonly string SampleErrorMessage = $"{InvalidApplicationNameException.ErrorText} this is some error";
        private readonly ApiException<ProblemDetails> _sampleApiException;

        public InvalidApplicationNameExceptionTests()
        {
            ProblemDetails details = new ProblemDetails()
            {
                Detail = SampleErrorMessage,
                Status = 400,
            };

            _sampleApiException = new ApiException<ProblemDetails>(
                SampleErrorMessage,
                details.Status.Value,
                $"{{\"detail\":\"{details.Detail}\"}}",
                null,
                details,
                null);
        }

        [Fact]
        public void TryCreate()
        {
            Assert.True(InvalidApplicationNameException.TryCreate(_sampleApiException, out var exception));
            Assert.Equal(SampleErrorMessage, exception.Message);
        }

        [Fact]
        public void TryCreate_Non400StatusCode()
        {
            var apiException = new ApiException<ProblemDetails>(
                _sampleApiException.Message,
                500,
                _sampleApiException.Response,
                null,
                _sampleApiException.Result,
                null);

            Assert.False(InvalidApplicationNameException.TryCreate(apiException, out var exception));
        }

        [Fact]
        public void TryCreate_ResultWithoutInvalidNameMessage()
        {
            var apiException = new ApiException<ProblemDetails>(
                _sampleApiException.Message,
                _sampleApiException.StatusCode,
                "{}",
                null,
                new ProblemDetails()
                {
                    Status = 400,
                    Detail = "This is some other validation error",
                },
                null);

            Assert.False(InvalidApplicationNameException.TryCreate(apiException, out var exception));
        }

        [Fact]
        public void TryCreate_NullResult()
        {
            var apiException = new ApiException<ProblemDetails>(
                _sampleApiException.Message,
                _sampleApiException.StatusCode,
                "this is not json",
                null,
                null,
                null);

            Assert.False(InvalidApplicationNameException.TryCreate(apiException, out var exception));
        }
    }
}
