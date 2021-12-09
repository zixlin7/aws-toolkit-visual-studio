using Amazon.AWSToolkit.EC2.Model;

using Xunit;

namespace AWSToolkit.Tests.Ec2
{
    public class VpcEntityTests
    {
        private readonly VpcEntity _sut = new VpcEntity();

        [Fact]
        public void Description_Default()
        {
            _sut.Name = "foo";
            _sut.IsDefault = true;

            Assert.Equal("(Default)", _sut.Description);
        }

        [Fact]
        public void Description_Name()
        {
            _sut.Name = "foo";

            Assert.Equal("foo", _sut.Description);
        }

        [Fact]
        public void Description_NullName()
        {
            _sut.Name = null;
            Assert.Equal(string.Empty, _sut.Description);
        }
    }
}
