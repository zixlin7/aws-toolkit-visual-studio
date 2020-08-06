using Amazon.AWSToolkit.Settings;
using Amazon.AWSToolkit.Tests.Common.Settings;
using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Settings
{
    public class DynamoDbSettingsTest
    {
        private const string PortPersistenceField = "LastDynamoDBConfiguredPort";
        private readonly FakeSettingsPersistence _settingsPersistence = new FakeSettingsPersistence();
        private readonly DynamoDbSettings _sut;

        public DynamoDbSettingsTest()
        {
            _sut = new DynamoDbSettings(_settingsPersistence);
        }

        [Fact]
        public void GetPort()
        {
            _settingsPersistence.PersistenceData[PortPersistenceField] = "4321";
            Assert.Equal(4321, _sut.Port);
        }

        [Fact]
        public void GetPortReturnsDefault()
        {
            Assert.Equal(8000, _sut.Port);
        }

        [Fact]
        public void SetPort()
        {
            _sut.Port = 1234;
            Assert.Equal("1234",
                _settingsPersistence.PersistenceData[PortPersistenceField]);
        }
    }
}