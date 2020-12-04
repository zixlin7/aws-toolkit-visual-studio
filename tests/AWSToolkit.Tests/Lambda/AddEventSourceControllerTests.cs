using Amazon.AWSToolkit.Lambda.Controller;
using Xunit;

namespace AWSToolkit.Tests.Lambda
{
    public class AddEventSourceControllerTests
    {
        private readonly string _functionArn;
        private const string AccountNumber = "122333444455";

        public AddEventSourceControllerTests()
        {
            _functionArn = $"arn:aws:lambda:us-west-2:{AccountNumber}:function:testfunction";
        }

        [Theory]
        [InlineData("arn:aws:sns:us-west-2:122333444455:testsns", "sns.amazonaws.com")]
        [InlineData("arn:aws:events:us-west-2:122333444455:rule/testevent", "events.amazonaws.com")]
        public void ValidateEventSourcePermissionRequest(string sourceArn, string principal)
        {
            var request = AddEventSourceController.CreateAddEventSourcePermissionRequest(sourceArn, principal, _functionArn, AccountNumber);
            Assert.Equal(sourceArn, request.SourceArn);
            Assert.Equal(_functionArn, request.FunctionName);
            Assert.Equal(principal, request.Principal);
            Assert.Equal("lambda:InvokeFunction", request.Action);
            Assert.EndsWith("-vstoolkit", request.StatementId);
            Assert.Null(request.SourceAccount);
        }

        [Fact]
        public void ValidateS3EventSourcePermissionRequest()
        {
            var sourceArn = "arn.aws.s3:::tests3";
            var request = AddEventSourceController.CreateAddEventSourcePermissionRequest(sourceArn, "s3.amazonaws.com", _functionArn, AccountNumber);
            Assert.Equal(AccountNumber, request.SourceAccount);
            Assert.Equal(sourceArn, request.SourceArn);
            Assert.Equal(_functionArn, request.FunctionName);
            Assert.Equal($"s3.amazonaws.com", request.Principal);
            Assert.Equal("lambda:InvokeFunction", request.Action);
            Assert.EndsWith("-vstoolkit", request.StatementId);
        }
    }
}
