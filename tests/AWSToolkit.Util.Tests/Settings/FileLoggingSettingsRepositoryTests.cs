using System;
using System.IO;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Settings;
using Amazon.AWSToolkit.Tests.Common.IO;

using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Settings
{
    public class FileLoggingSettingsRepositoryTests : IDisposable
    {
        private readonly TemporaryTestLocation _testLocation = new TemporaryTestLocation();
        private readonly ISettingsRepository<LoggingSettings> _loggingRepository;
        private readonly LoggingSettings _defaultSettings = new LoggingSettings();
        private readonly string _filePath;

        public FileLoggingSettingsRepositoryTests()
        {
            _filePath = $@"{_testLocation.InputFolder}\LoggingSettings.json";
            _loggingRepository = new FileSettingsRepository<LoggingSettings>(_filePath);
        }

        public void Dispose()
        {
            _testLocation.Dispose();
        }

        [Fact]
        public async Task GetSettings_WhenNoDefaultValueAndFileDoesNotExist()
        {
            var settings = await _loggingRepository.GetOrDefaultAsync();

            Assert.Null(settings);
        }

        [Fact]
        public async Task GetSettings_WhenDefaultValueAndFileDoesNotExist()
        {
            var settings = await _loggingRepository.GetOrDefaultAsync(_defaultSettings);

            Assert.Equal(_defaultSettings, settings);
        }

        [Fact]
        public async Task GetSettings()
        {
            var json =
                @"{ ""LogFileRetentionMonths"": 2, ""MaxLogDirectorySizeMb"": 200, ""MaxLogFileSizeMb"": 5, ""MaxFileBackups"": 3}";
            WriteJsonFile(json);
            var expectedSettings = new LoggingSettings()
            {
                LogFileRetentionMonths = 2, MaxLogDirectorySizeMb = 200, MaxLogFileSizeMb = 5, MaxFileBackups = 3
            };

            var settings = await _loggingRepository.GetOrDefaultAsync(_defaultSettings);

            Assert.Equal(expectedSettings, settings);
        }

        [Fact]
        public async Task GetSettings_WhenObjectDoesNotExist()
        {
            WriteJsonFile(@"{ ""dummy"": { ""jsonkey"": ""jsonValue"" } }");

            var settings = await _loggingRepository.GetOrDefaultAsync(_defaultSettings);

            Assert.Equal(_defaultSettings, settings);
        }

        [Fact]
        public async Task GetSettings_WhenPropertyMissing()
        {
            var json =
                @"{ ""LogFileRetentionMonths"": 2, ""MaxLogDirectorySizeMb"": 200}";
            WriteJsonFile(json);

            var expectedSettings = new LoggingSettings()
            {
                LogFileRetentionMonths = 2, MaxLogDirectorySizeMb = 200, MaxLogFileSizeMb = 10, MaxFileBackups = 10
            };

            var settings = await _loggingRepository.GetOrDefaultAsync(_defaultSettings);

            Assert.Equal(expectedSettings, settings);
        }

        [Fact]
        public async Task GetSettings_Throws_WhenFileLocked()
        {
            using (FileStream _ = CreateLockedFileStream(_filePath))
            {
                await Assert.ThrowsAsync<SettingsException>(() => _loggingRepository.GetOrDefaultAsync());
            }
        }

        [Fact]
        public async Task GetSettings_Throws_WhenInvalidJson()
        {
            WriteJsonFile(@"{ ""InvalidJson"":");

            await Assert.ThrowsAsync<SettingsException>(() => _loggingRepository.GetOrDefaultAsync());
        }

        [Fact]
        public async Task SaveSettings()
        {
            var settings = new LoggingSettings()
            {
                LogFileRetentionMonths = 2, MaxLogDirectorySizeMb = 200, MaxLogFileSizeMb = 5, MaxFileBackups = 4
            };
            _loggingRepository.Save(settings);

            Assert.True(File.Exists(_filePath));

            var actualSettings = await _loggingRepository.GetOrDefaultAsync();

            Assert.Equal(settings, actualSettings);
        }


        [Fact]
        public void SaveSettings_Throws_WhenFileLocked()
        {
            using (FileStream _ = CreateLockedFileStream(_filePath))
            {
                Assert.Throws<SettingsException>(() => _loggingRepository.Save(new LoggingSettings()));
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
