using Amazon.AWSToolkit.Tests.Common.Settings;
using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Settings
{
    public class SettingsPersistenceBaseTests
    {
        private const string PropertyName = "Name";
        private readonly FakeSettingsPersistence _sut = new FakeSettingsPersistence();

        [Fact]
        public void GetIntWithExistingProperty()
        {
            _sut.PersistenceData[PropertyName] = "1234";
            Assert.Equal(1234, _sut.GetInt(PropertyName));
        }

        [Fact]
        public void GetIntWithNonExistingProperty()
        {
            Assert.Equal(0, _sut.GetInt(PropertyName));
        }

        [Fact]
        public void GetIntWithNonExistingPropertyAndDefault()
        {
            Assert.Equal(4321, _sut.GetInt(PropertyName, 4321));
        }

        [Fact]
        public void SetInt()
        {
            _sut.SetInt(PropertyName, 8888);
            Assert.Equal("8888", _sut.PersistenceData[PropertyName]);
        }
    }
}