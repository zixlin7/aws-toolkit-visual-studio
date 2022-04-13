using System;
using System.IO;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Publish.PublishSetting;
using Amazon.AWSToolkit.Settings;
using Amazon.AWSToolkit.Tests.Common.IO;

using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Publish.PublishSetting
{
    public class FilePublishSettingsRepositoryTest : IDisposable
    {

        private readonly TemporaryTestLocation _testLocation = new TemporaryTestLocation();
        private readonly IPublishSettingsRepository _publishRepository;
        private readonly string _filePath;

        public FilePublishSettingsRepositoryTest()
        {
            _filePath = $@"{_testLocation.InputFolder}\PublishSettings.json";
            _publishRepository = new FilePublishSettingsRepository(_filePath);
        }

        public void Dispose()
        {
            _testLocation.Dispose();
        }

        [Fact]
        public async Task ShouldGetPublishSettings()
        {
            // arrange.
            var json =
                @"{ ""DeployServer"": { ""PortRange"": { ""Start"": 20000, ""End"": 20001 } }, ""ProxySettings"": { ""Host"": ""This-is-the-host"" }, ""ShowPublishBanner"": false}";
            WriteJsonFile(json);

            var expectedSettings = new PublishSettings()
            {
                DeployServer = new DeployServerSettings(new PortRange(20000, 20001)),
                ShowPublishBanner = false,
            };

            // act.
            var settings = await _publishRepository.GetAsync();

            // assert.
            Assert.Equal(expectedSettings, settings);
        }

        [Fact]
        public async Task ShouldGetDefaultSettingsIfFileDoesNotExist()
        {
            // act.
            var settings = await _publishRepository.GetAsync();

            // assert.
            Assert.Equal(PublishSettings.CreateDefault(), settings);
        }

        [Fact]
        public async Task ShouldGetDefaultSettingsIfObjectDoesNotExist()
        {
            // arrange.
            WriteJsonFile(@"{ ""dummy"": { ""jsonkey"": ""jsonValue"" } }");

            // act.
            var settings = await _publishRepository.GetAsync();
            // assert.
            Assert.Equal(PublishSettings.CreateDefault(), settings);
        }

        [Fact]
        public async Task ShouldGetDefaultSettingsIfOnePropertyMissing()
        {
            var json = @"{ ""DeployServer"": { ""PortRange"": { ""Start"": 20000, ""End"": 20001 } }, ""ProxySettings"": { ""Host"": ""This-is-the-host"" }}";
            WriteJsonFile(json);

            var expectedSettings = new PublishSettings()
            {
                DeployServer = new DeployServerSettings(new PortRange(20000, 20001)),
                ShowPublishBanner = true,
            };

            // act.
            var settings = await _publishRepository.GetAsync();

            // assert.
            Assert.Equal(expectedSettings, settings);
        }

        [Fact]
        public async Task ShouldGetNonDefaultSettingsIfPresent()
        {
            var json =
                @"{ ""DeployServer"": { ""PortRange"": { ""Start"": 20000, ""End"": 20001 }, ""AlternateCliPath"": ""mypath"", ""LoggingEnabled"": true, ""AdditionalArguments"": ""args"" }, ""ProxySettings"": { ""Host"": ""This-is-the-host"" }}";
            WriteJsonFile(json);

            var expectedSettings = new PublishSettings()
            {
                DeployServer = new DeployServerSettings(new PortRange(20000, 20001))
                {
                    AlternateCliPath = "mypath", AdditionalArguments = "args", LoggingEnabled = true
                },
                ShowPublishBanner = true,
            };

            // act.
            var settings = await _publishRepository.GetAsync();

            // assert.
            Assert.Equal(expectedSettings, settings);
        }

        [Fact]
        public async Task ShouldThrowOnGetWhenFileLocked()
        {
            // arrange.
            using (FileStream _ = CreateLockedFileStream(_filePath))
            {
                // act + assert
                await Assert.ThrowsAsync<SettingsException>(() => _publishRepository.GetAsync());
            }
        }

        [Fact]
        public async Task ShouldThrowIfUnableToDeserializeJsonOnGet()
        {
            // arrange.
            WriteJsonFile(@"{ ""InvalidJson"":");

            // act + assert
            await Assert.ThrowsAsync<SettingsException>(() => _publishRepository.GetAsync());
        }

        [Fact]
        public async Task ShouldSavePublishSettings()
        {
            // arrange.
            var settings = new PublishSettings()
            {
                DeployServer = new DeployServerSettings(new PortRange(20000, 20001)),
                ShowPublishBanner = false,
            };

            // act.
            _publishRepository.Save(settings);

            // assert.
            Assert.True(File.Exists(_filePath));

            var actualSettings = await _publishRepository.GetAsync();

            Assert.Equal(settings, actualSettings);
        }

        [Fact]
        public async Task ShouldNotSaveSettingsWithDefaultValues()
        {
            // arrange.
            var settings = new PublishSettings()
            {
                DeployServer = new DeployServerSettings(new PortRange(20000, 20001)) { AlternateCliPath = null },
                ShowPublishBanner = false,
            };

            // act.
            _publishRepository.Save(settings);

            // assert.
            Assert.True(File.Exists(_filePath));

            var actualSettings = await _publishRepository.GetAsync();
            var expectedSettings = new PublishSettings()
            {
                DeployServer = new DeployServerSettings(new PortRange(20000, 20001)), ShowPublishBanner = false,
            };

            Assert.Equal(expectedSettings, actualSettings);
        }

        [Fact]
        public void ShouldThrowOnSaveWhenFileLocked()
        {
            // arrange.
            using (FileStream _ = CreateLockedFileStream(_filePath))
            {
                // act + assert
                Assert.Throws<SettingsException>(() => _publishRepository.Save(new PublishSettings()));
            }
        }

        private FileStream CreateLockedFileStream(string filePath)
        {
            return new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }

        private void WriteJsonFile(string json)
        {
            File.WriteAllText(_filePath, json);
        }
    }
}
