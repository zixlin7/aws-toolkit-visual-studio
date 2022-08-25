using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Tests.Publishing.Util;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Models
{
    public class VpcConfigurationDetailTests
    {
        private readonly VpcConfigurationDetail _sut;
        private readonly ConfigurationDetail _sampleVpcIdDetail;
        private readonly ConfigurationDetail _sampleCreateNewDetail;
        private readonly ConfigurationDetail _sampleUseDefaultDetail;

        public VpcConfigurationDetailTests()
        {
            _sampleVpcIdDetail = ConfigurationDetailBuilder.Create()
                .WithId(VpcConfigurationDetail.ChildDetailIds.VpcId)
                .Build();

            _sampleCreateNewDetail = ConfigurationDetailBuilder.Create()
                .WithId(VpcConfigurationDetail.ChildDetailIds.CreateNew)
                .WithValue(true)
                .Build();

            _sampleUseDefaultDetail = ConfigurationDetailBuilder.Create()
                .WithId(VpcConfigurationDetail.ChildDetailIds.IsDefault)
                .WithValue(true)
                .Build();

            _sut = new VpcConfigurationDetail();
        }

        [Fact]
        public void RemoveChild()
        {
            _sut.AddChild(_sampleVpcIdDetail);

            _sut.ClearChildren();

            Assert.Empty(_sut.Children);
            Assert.Null(_sut.VpcIdDetail);
        }

        [Fact]
        public void AddChild()
        {
            _sut.AddChild(ConfigurationDetailBuilder.Create().WithSampleData().Build());
            Assert.Equal(1, _sut.Children.Count);
        }

        [Fact]
        public void AddChild_VpcIdDetail()
        {
            _sut.AddChild(_sampleVpcIdDetail);

            Assert.Equal(1, _sut.Children.Count);
            Assert.Equal(_sampleVpcIdDetail, _sut.VpcIdDetail);
        }

        [Fact]
        public void AddChild_CreateNewVpcDetail()
        {
            _sut.AddChild(_sampleCreateNewDetail);

            Assert.Equal(1, _sut.Children.Count);
            Assert.Equal(_sampleCreateNewDetail, _sut.CreateVpcDetail);
        }

        [Fact]
        public void AddChild_UseDefaultDetail()
        {
            _sut.AddChild(_sampleUseDefaultDetail);

            Assert.Equal(1, _sut.Children.Count);
            Assert.Equal(_sampleUseDefaultDetail, _sut.DefaultVpcDetail);
        }

        [Fact]
        public void SetVpcOption_Default()
        {
            _sampleUseDefaultDetail.Value = false;
            AddRequiredChildren();

            _sut.VpcOption = VpcOption.Default;

            Assert.True(Assert.IsType<bool>(_sampleUseDefaultDetail.Value));
            Assert.False(Assert.IsType<bool>(_sampleCreateNewDetail.Value));
        }

        [Fact]
        public void SetVpcOption_New()
        {
            _sampleCreateNewDetail.Value = false;
            AddRequiredChildren();

            _sut.VpcOption = VpcOption.New;

            Assert.False(Assert.IsType<bool>(_sampleUseDefaultDetail.Value));
            Assert.True(Assert.IsType<bool>(_sampleCreateNewDetail.Value));
        }

        [Fact]
        public void SetVpcOption_Existing()
        {
            AddRequiredChildren();

            _sut.VpcOption = VpcOption.Existing;

            Assert.False(Assert.IsType<bool>(_sampleUseDefaultDetail.Value));
            Assert.False(Assert.IsType<bool>(_sampleCreateNewDetail.Value));
        }

        [Theory]
        [InlineData(VpcOption.Default)]
        [InlineData(VpcOption.New)]
        public void SetVpcOption_WithInvalidVpcId(VpcOption vpcOption)
        {
            AddRequiredChildren();
            _sut.VpcOption = VpcOption.Existing;
            _sampleVpcIdDetail.Value = "some-value";
            _sampleVpcIdDetail.ValidationMessage = "its not valid";

            _sut.VpcOption = vpcOption;
            Assert.Equal("", _sampleVpcIdDetail.Value);
        }

        [Theory]
        [InlineData(true, false, VpcOption.Default)]
        [InlineData(false, true, VpcOption.New)]
        [InlineData(false, false, VpcOption.Existing)]
        public void AddRequiredChildrenUpdatesVpcOption(bool useDefault, bool createNew, VpcOption expectedVpcOption)
        {
            _sampleUseDefaultDetail.Value = useDefault;
            _sampleCreateNewDetail.Value = createNew;

            AddRequiredChildren();

            Assert.Equal(expectedVpcOption, _sut.VpcOption);
        }

        private void AddRequiredChildren()
        {
            _sut.AddChild(_sampleUseDefaultDetail);
            _sut.AddChild(_sampleCreateNewDetail);
            _sut.AddChild(_sampleVpcIdDetail);
        }
    }
}
