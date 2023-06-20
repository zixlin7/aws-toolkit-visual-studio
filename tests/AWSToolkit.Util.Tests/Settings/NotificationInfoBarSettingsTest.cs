using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Settings;
using Amazon.AWSToolkit.Tests.Common.IO;

using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Settings
{
    public class NotificationInfoBarSettingsTest
    {
        private readonly TemporaryTestLocation _testLocation = new TemporaryTestLocation();
        private readonly FileSettingsRepository<NotificationSettings> _notificationRepository;
        private readonly NotificationSettings _defaultSettings = new NotificationSettings();
        private readonly string _filePath;

        public NotificationInfoBarSettingsTest()
        {
            _filePath = $@"{_testLocation.InputFolder}\NotificationInfoBarSettings.json";
            _notificationRepository = new FileSettingsRepository<NotificationSettings>(_filePath);
        }

        public void Dispose()
        {
            _testLocation.Dispose();
        }

        [Fact]
        public async Task GetSettings_WhenNoDefaultValueAndFileDoesNotExist()
        {
            var settings = await _notificationRepository.GetOrDefaultAsync();

            Assert.Null(settings);
        }

        [Fact]
        public async Task GetSettings_WhenDefaultValueAndFileDoesNotExist()
        {
            var settings = await _notificationRepository.GetOrDefaultAsync(_defaultSettings);

            Assert.Equal(_defaultSettings, settings);
        }

        [Fact]
        public async Task GetSettings()
        {
            var json = "{\"dismissedNotifications\":[{\"notificationId\":\"01234\",\"dismissedOn\":1680332400},{\"notificationId\":\"56789\",\"dismissedOn\":1680332400}]}";
            WriteJsonFile(json);

            var expectedSettings = CreateSampleNotificationSettings();

            var settings = await _notificationRepository.GetOrDefaultAsync(_defaultSettings);

            Assert.Equal(expectedSettings, settings);
        }

        [Fact]
        public async Task GetSettings_Throws_WhenFileLocked()
        {
            using (FileStream _ = CreateLockedFileStream(_filePath))
            {
                await Assert.ThrowsAsync<SettingsException>(() => _notificationRepository.GetOrDefaultAsync());
            }
        }

        [Fact]
        public async Task SaveSettings()
        {
            var settings = CreateSampleNotificationSettings();

            _notificationRepository.Save(settings);

            Assert.True(File.Exists(_filePath));

            var actualSettings = await _notificationRepository.GetOrDefaultAsync();

            Assert.Equal(settings, actualSettings);
        }

        [Fact]
        public async Task GetSettings_Throws_WhenInvalidJson()
        {
            WriteJsonFile(@"{ ""InvalidJson"":");

            await Assert.ThrowsAsync<SettingsException>(() => _notificationRepository.GetOrDefaultAsync());
        }


        [Fact]
        public void SaveSettings_Throws_WhenFileLocked()
        {
            using (FileStream _ = CreateLockedFileStream(_filePath))
            {
                Assert.Throws<SettingsException>(() => _notificationRepository.Save(new NotificationSettings()));
            }
        }

        private NotificationSettings CreateSampleNotificationSettings()
        {
            var dismissedNotifications = new List<NotificationSettings.DismissedNotification>()
            {
                new NotificationSettings.DismissedNotification() {NotificationId = "01234", DismissedOn = 1680332400},
                new NotificationSettings.DismissedNotification() {NotificationId = "56789", DismissedOn = 1680332400}
            };

            return new NotificationSettings()
            {
                DismissedNotifications = dismissedNotifications
            };
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
