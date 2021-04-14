using Amazon.AWSToolkit.Regions;
using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Regions
{
    public class ToolkitRegionEqualityTests
    {
        private ToolkitRegion _x = new ToolkitRegion() {PartitionId = "partition", DisplayName = "name", Id = "id",};
        private ToolkitRegion _y = new ToolkitRegion() {PartitionId = "partition", DisplayName = "name", Id = "id",};
        private ToolkitRegion _z = new ToolkitRegion() {PartitionId = "partition", DisplayName = "name", Id = "id",};
        private ToolkitRegion _alternate = new ToolkitRegion() {PartitionId = "qwerty", DisplayName = "uiop", Id = "asdf",};

        [Fact]
        public void Reflexive()
        {
            Assert.True(_x == _x);
            Assert.True(Equals(_x, _x));
            Assert.True(_x.Equals(_x));
            Assert.Equal(_x, _x);
        }

        [Fact]
        public void Symmetric()
        {
            Assert.Equal(_x, _y);
            Assert.Equal(_y, _x);
        }

        [Fact]
        public void Transitive()
        {
            Assert.Equal(_x, _y);
            Assert.Equal(_y, _z);
            Assert.Equal(_x, _z);
        }

        [Fact]
        public void NonEqual()
        {
            Assert.NotEqual(_x, _alternate);
        }

        [Fact]
        public void Case_Partition()
        {
            _y.PartitionId = _y.PartitionId.ToUpper();
            Assert.NotEqual(_x, _y);
        }

        [Fact]
        public void Case_Id()
        {
            _y.Id = _y.Id.ToUpper();
            Assert.NotEqual(_x, _y);
        }

        [Fact]
        public void Case_DisplayName()
        {
            _y.DisplayName = _y.DisplayName.ToUpper();
            Assert.NotEqual(_x, _y);
        }
    }
}
