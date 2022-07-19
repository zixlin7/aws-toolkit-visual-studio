using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.Models.Configuration;
using Amazon.AWSToolkit.Tests.Publishing.Util;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Models
{
    public class Ec2InstanceConfigurationDetailTests
    {
        private readonly Ec2InstanceConfigurationDetail _sut = new Ec2InstanceConfigurationDetail();
        private const string SampleInstanceTypeId = "t3.nano";

        [Fact]
        public void InstanceTypeId_Getter()
        {
            _sut.Value = SampleInstanceTypeId;
            Assert.Equal(SampleInstanceTypeId, _sut.InstanceTypeId);
        }

        [Fact]
        public void InstanceTypeId_Setter()
        {
            _sut.InstanceTypeId = SampleInstanceTypeId;
            Assert.Equal(SampleInstanceTypeId, _sut.Value);
        }
    }
}
