using System;
using System.IO;

using Amazon.AWSToolkit.Credentials.IO;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Tests.Common.IO;
using Amazon.Runtime.CredentialManagement;

using Xunit;

namespace AWSToolkit.Tests.Credentials.IO
{
    public class ExtensionMethodsTests : IDisposable
    {
        private readonly SharedCredentialsFile _sut;

        private readonly TemporaryTestLocation _testLocation;

        private const string _expectedSsoSessionName = "test-session";

        private const string _expectedSsoSessionRegion = "us-west-2";

        private const string _expectedSsoRegistrationScopes = "test-scope-1,test-scope-2";

        private const string _expectedSsoSessionStartUrl = "https://amazon.com";

        private readonly string _expectedSsoSessionSectionName;

        private readonly string _expectedSsoSessionSectionHeader;

        public ExtensionMethodsTests()
        {
            _expectedSsoSessionSectionName = ProfileName.CreateSsoSessionProfileName(_expectedSsoSessionName);
            _expectedSsoSessionSectionHeader = $"[{_expectedSsoSessionSectionName}]";

            _testLocation = new TemporaryTestLocation();

            // Write credentials file to test/
            _sut = new SharedCredentialsFile(Path.Combine(_testLocation.TestFolder, "credentials"));
        }

        public void Dispose()
        {
            _testLocation.Dispose();
        }

        private string CreateSharedConfigFile(string contents = "", string path = null)
        {
            var configPath = Path.Combine(path ?? Path.GetDirectoryName(_sut.FilePath), ExtensionMethods._configFileName);

            // File.WriteAllText doesn't appear to be flushed and causes intermittent race conditions with tests, so do it forcefully
            using (var writer = new StreamWriter(configPath))
            {
                writer.Write(contents);
                writer.Flush();
            }

            return configPath;
        }

        #region GetSharedConfigFile test(s)
        [Fact]
        public void GetSharedConfigFileLoadsFromSharedConfigFileEnvVar()
        {
            var sharedConfigFileEnvVarValue = Environment.GetEnvironmentVariable(SharedCredentialsFile.SharedConfigFileEnvVar);

            try
            {
                // Set env var to test/output and write config file there
                var configPath = CreateSharedConfigFile(_expectedSsoSessionSectionHeader, _testLocation.OutputFolder);
                Environment.SetEnvironmentVariable(SharedCredentialsFile.SharedConfigFileEnvVar, configPath);

                var configFile = _sut.GetSharedConfigFile();

                Assert.NotNull(configFile);
                Assert.True(configFile.SectionExists(_expectedSsoSessionSectionName));
            }
            finally
            {
                // Be sure to rollback the env change or it will cause other tests to fail
                Environment.SetEnvironmentVariable(SharedCredentialsFile.SharedConfigFileEnvVar, sharedConfigFileEnvVarValue);
            }
        }

        [Fact]
        public void GetSharedConfigFileLoadsFromSameDirectoryAsCredentialsFile()
        {
            // Write config file to test/
            CreateSharedConfigFile(_expectedSsoSessionSectionHeader);

            var configFile = _sut.GetSharedConfigFile();

            Assert.NotNull(configFile);
            Assert.True(configFile.SectionExists(_expectedSsoSessionSectionName));
        }
        #endregion

        #region RegisterSsoSession test(s)
        [Theory]
        [InlineData(null, _expectedSsoSessionRegion, _expectedSsoSessionStartUrl)]
        [InlineData(_expectedSsoSessionName, "", _expectedSsoSessionStartUrl)]
        [InlineData(_expectedSsoSessionName, _expectedSsoSessionRegion, "             ")]
        public void RegisterSsoSessionFailsOnMissingData(string ssoSessionName, string ssoRegion, string ssoStartUrl)
        {
            Assert.Throws<ArgumentNullException>(() => _sut.RegisterSsoSession(null));

            Assert.Throws<ArgumentException>(() => _sut.RegisterSsoSession(new CredentialProfile(ssoSessionName,
                new CredentialProfileOptions()
                {
                    SsoRegion = ssoRegion,
                    SsoStartUrl = ssoStartUrl
                    // SsoRegistrationScopes is not required
                })));
        }

        [Fact]
        public void RegisterSsoSessionWritesSsoSessionToConfigFile()
        {
            var expectedSsoSession = new CredentialProfile(_expectedSsoSessionName,
                new CredentialProfileOptions()
                {
                    SsoRegion = _expectedSsoSessionRegion,
                    SsoSession = _expectedSsoSessionName,
                    SsoStartUrl = _expectedSsoSessionStartUrl,
                    SsoRegistrationScopes = _expectedSsoRegistrationScopes
                });

            CreateSharedConfigFile();

            _sut.RegisterSsoSession(expectedSsoSession);

            var configFile = _sut.GetSharedConfigFile();

            Assert.NotNull(configFile);
            Assert.True(configFile.TryGetSection(_expectedSsoSessionName, true, out var actualSsoSession));
            Assert.Equal(expectedSsoSession.Options.SsoRegion, actualSsoSession[ExtensionMethods._ssoRegionPropertyName]);
            Assert.Equal(expectedSsoSession.Options.SsoRegistrationScopes, actualSsoSession[ExtensionMethods._ssoRegistrationScopesName]);
            Assert.Equal(expectedSsoSession.Options.SsoStartUrl, actualSsoSession[ExtensionMethods._ssoStartUrlPropertyName]);
        }
        #endregion

        #region TryGetSsoSession test(s)
        [Fact]
        public void TryGetSsoSessionReturnsExistingSsoSession()
        {
            CreateSharedConfigFile(
                _expectedSsoSessionSectionHeader +
                $"\n{ExtensionMethods._ssoRegionPropertyName}={_expectedSsoSessionRegion}" +
                $"\n{ExtensionMethods._ssoStartUrlPropertyName}={_expectedSsoSessionStartUrl}");

            Assert.True(_sut.TryGetSsoSession(_expectedSsoSessionName, out var actualSsoSession));
            Assert.Equal(_expectedSsoSessionRegion, actualSsoSession.Options.SsoRegion);
            Assert.Equal(_expectedSsoSessionStartUrl, actualSsoSession.Options.SsoStartUrl);
        }

        [Fact]
        public void TryGetSsoSessionReturnsFalseOnNonExistingSsoSession()
        {
            CreateSharedConfigFile();

            Assert.False(_sut.TryGetSsoSession(_expectedSsoSessionName, out var _));
        }

        [Fact]
        public void TryGetSsoSessionReturnsFalseOnNonExistingConfigFile()
        {
            Assert.False(_sut.TryGetSsoSession(_expectedSsoSessionName, out var _));
        }
        #endregion
    }
}
