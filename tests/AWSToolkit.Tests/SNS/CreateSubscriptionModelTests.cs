using System.Linq;
using Amazon;
using Amazon.AWSToolkit.SNS.Model;
using Xunit;

namespace AWSToolkit.Tests.SNS
{
    public class CreateSubscriptionModelTests
    {
        private readonly CreateSubscriptionModel _sut;

        public CreateSubscriptionModelTests()
        {
            _sut = new CreateSubscriptionModel(RegionEndpoint.USEast1.SystemName);
        }

        [Fact]
        public void AddSqsEndpoint()
        {
            _sut.AddSqsEndpoint("aaaUrl", "aaaArn");
            _sut.AddSqsEndpoint("bbbUrl", "bbbArn");

            var sqsEndpoints = _sut.PossibleSQSEndpoints.ToList();
            Assert.Equal(2, sqsEndpoints.Count);
            Assert.Contains("aaaArn", sqsEndpoints);
            Assert.Contains("bbbArn", sqsEndpoints);
        }

        [Fact]
        public void EndpointSqsUrl()
        {
            _sut.AddSqsEndpoint("aaaUrl", "aaaArn");

            _sut.Endpoint = "aaaArn";
            Assert.Equal("aaaUrl", _sut.EndpointSqsUrl);

            _sut.Endpoint = "foo";
            Assert.Null(_sut.EndpointSqsUrl);
        }
    }
}
