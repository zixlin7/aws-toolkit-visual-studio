using Amazon.AWSToolkit.Settings;
using Amazon.AWSToolkit.Tests.Common.Settings;
using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Settings
{
    public class MobileAnalyticsSettingsTest
    {
        private const string LastUsedCognitoIdentityPoolIdPersistenceField =
            ToolkitSettingsConstants.AnalyticsMostRecentlyUsedCognitoIdentityPoolId;

        private const string CognitoIdentityIdPersistenceField = ToolkitSettingsConstants.AnalyticsCognitoIdentityId;

        private readonly FakeSettingsPersistence _settingsPersistence = new FakeSettingsPersistence();
        private readonly MobileAnalyticsSettings _sut;

        public MobileAnalyticsSettingsTest()
        {
            _sut = new MobileAnalyticsSettings(_settingsPersistence);
        }

        [Fact]
        public void GetLastUsedCognitoIdentityPoolId()
        {
            _settingsPersistence.PersistenceData[LastUsedCognitoIdentityPoolIdPersistenceField] = "hello";
            Assert.Equal("hello", _sut.LastUsedCognitoIdentityPoolId);
        }

        [Fact]
        public void GetLastUsedCognitoIdentityPoolIdReturnsDefault()
        {
            Assert.Null(_sut.LastUsedCognitoIdentityPoolId);
        }

        [Fact]
        public void SetLastUsedCognitoIdentityPoolId()
        {
            _sut.LastUsedCognitoIdentityPoolId = "hi";
            Assert.Equal("hi",
                _settingsPersistence.PersistenceData[LastUsedCognitoIdentityPoolIdPersistenceField]);
        }

        [Fact]
        public void GetCognitoIdentityId()
        {
            _settingsPersistence.PersistenceData[CognitoIdentityIdPersistenceField] = "hello";
            Assert.Equal("hello", _sut.CognitoIdentityId);
        }

        [Fact]
        public void GetCognitoIdentityIdReturnsDefault()
        {
            Assert.Null(_sut.CognitoIdentityId);
        }

        [Fact]
        public void SetCognitoIdentityId()
        {
            _sut.CognitoIdentityId = "hi";
            Assert.Equal("hi",
                _settingsPersistence.PersistenceData[CognitoIdentityIdPersistenceField]);
        }
    }
}