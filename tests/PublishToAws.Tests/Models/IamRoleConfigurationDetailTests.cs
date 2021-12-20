using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Tests.Publishing.Util;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Models
{
    public class IamRoleConfigurationDetailTests
    {
        private readonly IamRoleConfigurationDetail _sut;
        private readonly ConfigurationDetail _sampleRoleArnDetail;
        private readonly ConfigurationDetail _sampleCreateNewDetail;

        public IamRoleConfigurationDetailTests()
        {
            _sampleRoleArnDetail = ConfigurationDetailBuilder.Create()
                .WithId(IamRoleConfigurationDetail.ChildDetailIds.RoleArn)
                .Build();

            _sampleCreateNewDetail = ConfigurationDetailBuilder.Create()
                .WithId(IamRoleConfigurationDetail.ChildDetailIds.CreateNew)
                .WithValue(true)
                .Build();

            _sut = new IamRoleConfigurationDetail();
        }

        [Fact]
        public void RemoveChild()
        {
            _sut.AddChild(_sampleRoleArnDetail);

            _sut.ClearChildren();

            Assert.Empty(_sut.Children);
            Assert.Null(_sut.RoleArnDetail);
        }

        [Fact]
        public void AddChild()
        {
            _sut.AddChild(ConfigurationDetailBuilder.Create().WithSampleData().Build());
            Assert.Equal(1, _sut.Children.Count);
        }

        [Fact]
        public void AddChild_RoleArnDetail()
        {
            _sut.AddChild(_sampleRoleArnDetail);

            Assert.Equal(1, _sut.Children.Count);
            Assert.Equal(_sampleRoleArnDetail, _sut.RoleArnDetail);
        }

        [Fact]
        public void AddChild_CreateNewRoleDetail()
        {
            _sut.AddChild(_sampleCreateNewDetail);

            Assert.Equal(1, _sut.Children.Count);
            Assert.Equal(_sampleCreateNewDetail.Value, _sut.CreateNewRole);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CreateNewRole(bool value)
        {
            _sut.AddChild(_sampleCreateNewDetail);

            _sut.CreateNewRole = value;
            Assert.Equal(value, _sampleCreateNewDetail.Value);
        }
    }
}
